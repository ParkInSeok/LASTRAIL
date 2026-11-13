using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    public enum CombatEquipmentOwnerKind { Operator,Turret,Sentry,Carriage,Train }
    public enum CombatIncomingDamageKind { OrdinaryDirect,OrdinaryDot,FixedMechanic,SelfDamage,Failure,TrainTransfer }
    public sealed class CombatEquippedItem
    {
        public string OwnerId { get; }
        public string InstanceId { get; }
        public string SetTag { get; }
        public CombatEquippedItem(string ownerId,string instanceId,string setTag)
        {
            if(!ValidId(ownerId)||!ValidId(instanceId)||setTag!=null&&!ValidId(setTag)) throw new ArgumentException("Invalid resolved equipment identity/tag.");
            OwnerId=ownerId;InstanceId=instanceId;SetTag=setTag;
        }
        internal static bool ValidId(string value)=>!string.IsNullOrWhiteSpace(value)&&value.Length<=400;
    }
    // Immutable resolved view of one owner's actual three slots. No catalog, inventory or HP ownership.
    public sealed class CombatEquipmentLoadout
    {
        readonly Dictionary<string,int> counts=new Dictionary<string,int>(StringComparer.Ordinal);
        public string OwnerId { get; }
        public CombatEquipmentOwnerKind OwnerKind { get; }
        public bool HasThreeDistinctTags { get; }
        public CombatEquipmentLoadout(string ownerId,CombatEquipmentOwnerKind ownerKind,CombatEquippedItem[] slots)
        {
            if(!CombatEquippedItem.ValidId(ownerId)||!Enum.IsDefined(typeof(CombatEquipmentOwnerKind),ownerKind)||slots==null||slots.Length!=3) throw new ArgumentException("One resolved owner and exactly three slots required.");
            OwnerId=ownerId;OwnerKind=ownerKind;
            var identities=new HashSet<string>(StringComparer.Ordinal);
            foreach(var item in (CombatEquippedItem[])slots.Clone())
            {
                // Empty slot or absent tag is a valid unequipped/ineligible condition.
                if(item==null) continue;
                if(item.OwnerId!=ownerId) throw new ArgumentException("Equipment belongs to a different owner.");
                if(!identities.Add(item.InstanceId)) throw new ArgumentException("Equipment instance cannot occupy two slots.");
                if(item.SetTag==null) continue;
                counts.TryGetValue(item.SetTag,out var count);counts[item.SetTag]=count+1;
            }
            HasThreeDistinctTags=counts.Count==3;
        }
        public int CountTag(string tag)=>tag!=null&&counts.TryGetValue(tag,out var count)?count:0;
    }
    public static class CombatOc01Policy
    {
        public const string Policy="oc01-reconciliation-final-v01";
        public static int ReductionBps(CombatEquipmentLoadout loadout,bool resolvedTraitActive,CombatIncomingDamageKind kind,bool pipelineAllowsReduction)
        {
            if(loadout==null||!Enum.IsDefined(typeof(CombatIncomingDamageKind),kind)) throw new ArgumentException("Explicit resolved loadout and damage kind required.");
            return resolvedTraitActive&&pipelineAllowsReduction&&loadout.OwnerKind==CombatEquipmentOwnerKind.Operator&&loadout.HasThreeDistinctTags&&IsOrdinary(kind)?1000:0;
        }
        static bool IsOrdinary(CombatIncomingDamageKind kind)=>kind==CombatIncomingDamageKind.OrdinaryDirect||kind==CombatIncomingDamageKind.OrdinaryDot;
        // Only the existing ordinary numeric-defense pipeline may enter here; excluded routes keep their original processing.
        // Source inputs and trait eligibility must already be pinned to the run/confirmed equipment result.
        public static CombatNumericDefenseResult ResolveOrdinary(JObject originalBudget,string targetOwnerId,CombatEquipmentLoadout loadout,bool resolvedTraitActive,CombatIncomingDamageKind kind,bool pipelineAllowsReduction)
        {
            if(loadout==null||targetOwnerId!=loadout.OwnerId||!IsOrdinary(kind)||originalBudget==null) throw new ArgumentException("Matching target and ordinary damage budget required.");
            var bps=ReductionBps(loadout,resolvedTraitActive,kind,pipelineAllowsReduction);
            var budget=(JObject)originalBudget.DeepClone();
            if(!(budget["reductions"] is JObject reductions)||!(reductions["independent"] is JArray independent)) throw new ArgumentException("Existing independent reduction group required.");
            foreach(var token in independent)
                if(token is JObject group&&(string)group["groupId"]=="OC01") throw new ArgumentException("OC01 must be resolved once from actual equipment.");
            if(bps!=0) independent.Add(new JObject{["groupId"]="OC01",["valueBps"]=bps});
            return CombatNumericDefenseBudget.Resolve(budget);
        }
    }
}
