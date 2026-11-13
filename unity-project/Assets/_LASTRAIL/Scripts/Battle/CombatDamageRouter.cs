using System;
using System.Collections.Generic;

namespace LastRail.Battle
{
    public enum CombatDamageRoute { DirectTrain, CarriageTransfer }

    public sealed class CombatDamageReceipt
    {
        public string EventId { get; }
        public string TargetId { get; }
        public CombatDamageRoute Route { get; }
        public long LogicalTick { get; }
        public int Absorbed { get; }
        public int TargetHpLoss { get; }
        public int TransferToTrain { get; }
        public int TrainHpLoss { get; }
        // Eligible surviving down transition only, not a scheduled recovery.
        public bool CarriageDown { get; }
        public bool TerminalOnEntry { get; }
        public bool TrainDefeated { get; }
        internal CombatDamageReceipt(string eventId,string targetId,CombatDamageRoute route,long tick,
            int absorbed,int targetHpLoss,int transfer,int trainHpLoss,bool down,bool terminal,bool defeated)
        {
            EventId=eventId;TargetId=targetId;Route=route;LogicalTick=tick;Absorbed=absorbed;
            TargetHpLoss=targetHpLoss;TransferToTrain=transfer;TrainHpLoss=trainHpLoss;
            CarriageDown=down;TerminalOnEntry=terminal;TrainDefeated=defeated;
        }
    }

    public readonly struct CombatDamageApplication
    {
        public CombatDamageReceipt Receipt { get; }
        public bool Applied { get; }
        internal CombatDamageApplication(CombatDamageReceipt receipt,bool applied){Receipt=receipt;Applied=applied;}
    }

    // Inactive domain boundary: current Producers still reject carriage events.
    // One simulation owner keeps this router for its lifetime. It borrows vitals;
    // HP and shield remain solely in those instances. No timers, IO, UI or QA dependency.
    // Ledger durability/restore and producer activation require a later integration unit.
    public sealed class CombatDamageRouter
    {
        const int TransferBps=3000;
        const int BasisPoints=10000;
        const int MaximumIdentifierLength=128;
        sealed class Entry
        {
            internal readonly CombatDamageReceipt Receipt;
            internal readonly int Magnitude;
            internal readonly bool Protected;
            internal Entry(CombatDamageReceipt receipt,int magnitude,bool protection){Receipt=receipt;Magnitude=magnitude;Protected=protection;}
            internal bool Matches(CombatDamageRoute route,string target,int magnitude,long tick,bool protection)=>
                Receipt.Route==route&&Receipt.TargetId==target&&Magnitude==magnitude&&Receipt.LogicalTick==tick&&Protected==protection;
        }
        readonly string trainId;
        readonly CombatVitals train;
        readonly Dictionary<string,CombatVitals> carriages=new Dictionary<string,CombatVitals>(StringComparer.Ordinal);
        readonly Dictionary<string,Entry> ledger=new Dictionary<string,Entry>(StringComparer.Ordinal);
        readonly int eventCapacity;

        public CombatDamageRouter(string trainId,CombatVitals train,IReadOnlyDictionary<string,CombatVitals> carriages,int eventCapacity)
        {
            Identifier(trainId,nameof(trainId));
            if(train==null)throw new ArgumentNullException(nameof(train));
            if(carriages==null)throw new ArgumentNullException(nameof(carriages));
            if(eventCapacity<=0)throw new ArgumentOutOfRangeException(nameof(eventCapacity));
            this.trainId=trainId;this.train=train;this.eventCapacity=eventCapacity;
            var instances=new HashSet<CombatVitals>{train};
            foreach(var binding in carriages)
            {
                Identifier(binding.Key,nameof(carriages));
                if(binding.Key==trainId||binding.Value==null||!instances.Add(binding.Value)||this.carriages.ContainsKey(binding.Key))
                    throw new ArgumentException("Each target requires a distinct identity and HP owner.",nameof(carriages));
                this.carriages.Add(binding.Key,binding.Value);
            }
        }
        public CombatDamageApplication ApplyCarriageDamage(string eventId,string carriageId,int resolvedMagnitude,long logicalTick,bool protectedFromDamage)
        {
            Identifier(carriageId,nameof(carriageId));
            if(!carriages.TryGetValue(carriageId,out var target))throw new ArgumentException("Unknown carriage.",nameof(carriageId));
            return Apply(eventId,carriageId,target,CombatDamageRoute.CarriageTransfer,resolvedMagnitude,logicalTick,protectedFromDamage);
        }
        public CombatDamageApplication ApplyDirectTrainDamage(string eventId,int resolvedMagnitude,long logicalTick,bool protectedFromDamage)=>
            Apply(eventId,trainId,train,CombatDamageRoute.DirectTrain,resolvedMagnitude,logicalTick,protectedFromDamage);

        CombatDamageApplication Apply(string eventId,string targetId,CombatVitals target,CombatDamageRoute route,int magnitude,long tick,bool protection)
        {
            Identifier(eventId,nameof(eventId));
            if(magnitude<0)throw new ArgumentOutOfRangeException(nameof(magnitude));
            if(tick<0)throw new ArgumentOutOfRangeException(nameof(tick));
            if(ledger.TryGetValue(eventId,out var old))
            {
                if(!old.Matches(route,targetId,magnitude,tick,protection))throw new InvalidOperationException("Conflicting damage event.");
                return new CombatDamageApplication(old.Receipt,false);
            }
            if(ledger.Count>=eventCapacity)throw new InvalidOperationException("Damage event ledger is full; preserve state for owner recovery.");

            bool terminal=train.Hp==0,down=false;
            int absorbed=0,targetLoss=0,transfer=0,trainLoss=0;
            if(!terminal)
            {
                // Validate both owners before either can expire a shield or lose HP.
                // Same-event replay above remains valid after clocks advance.
                train.ValidateDamageTick(tick);target.ValidateDamageTick(tick);
                int before=target.Hp;
                var hit=target.Damage(magnitude,tick,protection);
                absorbed=hit.Absorbed;targetLoss=hit.HpLoss;
                if(route==CombatDamageRoute.CarriageTransfer)
                {
                    transfer=(int)((long)targetLoss*TransferBps/BasisPoints);
                    trainLoss=train.ApplyTransferredHpLoss(transfer,tick);
                    down=before>0&&target.Hp==0&&train.Hp>0;
                }
                else trainLoss=targetLoss;
            }
            var receipt=new CombatDamageReceipt(eventId,targetId,route,tick,absorbed,targetLoss,transfer,trainLoss,down,terminal,train.Hp==0);
            ledger.Add(eventId,new Entry(receipt,magnitude,protection));
            return new CombatDamageApplication(receipt,true);
        }
        static void Identifier(string value,string parameter)
        {
            if(string.IsNullOrWhiteSpace(value)||value.Length>MaximumIdentifierLength)throw new ArgumentException("A bounded nonempty identity is required.",parameter);
        }
    }
}
