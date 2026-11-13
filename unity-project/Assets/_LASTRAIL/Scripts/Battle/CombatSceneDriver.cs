using System;
using LastRail.Run;
namespace LastRail.Battle
{
    // Single scene owner; publish only successfully saved transitions. Delegates
    // are trusted host ports, not admission proof or a replacement HTTP sender.
    public sealed class CombatSceneDriver
    {
        readonly Func<BasicCombatSimulation> factory;
        readonly Func<BasicCombatSimulation> sceneCopyFactory;
        readonly Func<RestoredCombatCheckpoint> load;
        readonly Func<BasicCombatSimulation,long,long> save;
        readonly Func<bool> ownerCurrent;
        readonly bool nodeInstance;
        bool retired;
        bool executionStarted;
        bool changing;
        bool OwnerCurrent=>!retired&&ownerCurrent();
        public bool IsNodeInstance=>nodeInstance;
        public bool IsRetired=>retired;
        public bool HasBegunExecution=>executionStarted||current?.HasOrdinaryExecutionStarted==true;
        public bool IsExecutionReady=>OwnerCurrent&&!RecoveryRequired&&current?.IsExecutionReady==true;
        BasicCombatSimulation current;
        long revision;
        Newtonsoft.Json.Linq.JObject displayState;
        bool notifyingState;
        public event Action StateChanged;
        // Borrowed by presentation consumers only; external callers retain isolated HudState snapshots.
        internal Newtonsoft.Json.Linq.JObject ReadDisplayState()=>displayState??(displayState=HudState);
        void NotifyStateChanged(){
            if(notifyingState)return;
            notifyingState=true;
            try{var handlers=StateChanged;if(handlers==null)return;foreach(Action handler in handlers.GetInvocationList()){
                if(StateChanged==null)break;
                try{handler();}catch(Exception error){LastPresentationFailure=error;}
            }}finally{notifyingState=false;}
        }
        public bool RecoveryRequired { get; private set; }
        public long Tick=>current?.Tick??0;
        public string StateJson=>current?.CaptureLocalState();
        public Newtonsoft.Json.Linq.JObject HudState
        {
            get{try{
                if(current==null)return null;
                return current.ReadHudState();
            }catch{bool changed=!RecoveryRequired;RecoveryRequired=true;if(changed&&!changing)NotifyStateChanged();throw;}}
        }
        public BasicCombatOutcome Outcome=>current?.Outcome??BasicCombatOutcome.Running;
        public event Action OrdinaryExecutionBegan;
        public event Action<Newtonsoft.Json.Linq.JObject> RailArtilleryStepCommitted;
        public event Action<Newtonsoft.Json.Linq.JObject> BarragePresentationEvent;
        readonly System.Collections.Generic.HashSet<PresentationHold> presentationHolds=new System.Collections.Generic.HashSet<PresentationHold>();
        bool publishingBarrage,deliveringBarrage;
        public Exception LastPresentationFailure { get; private set; }
        public bool HasPresentationHold=>OwnerCurrent&&presentationHolds.Count>0;
        sealed class PresentationHold:IDisposable {
            Action release;internal PresentationHold(Action release){this.release=release;}
            public void Dispose(){var old=release;release=null;old?.Invoke();}
        }
        // Only a successfully published commit may lend this clock; menu pause remains independent.
        public IDisposable AcquirePresentationHold(){
            if(!publishingBarrage||!OwnerCurrent||RecoveryRequired||current==null)return null;
            PresentationHold lease=null;lease=new PresentationHold(()=>presentationHolds.Remove(lease));presentationHolds.Add(lease);return lease;
        }
        void PublishBarrage(Newtonsoft.Json.Linq.JObject input){
            var subscribers=BarragePresentationEvent;if(subscribers==null)return;
            deliveringBarrage=true;publishingBarrage=(string)input["kind"]=="COMMIT";
            try{foreach(Action<Newtonsoft.Json.Linq.JObject> subscriber in subscribers.GetInvocationList()){
                if(!OwnerCurrent)break;
                var before=new System.Collections.Generic.HashSet<PresentationHold>(presentationHolds);
                try{subscriber((Newtonsoft.Json.Linq.JObject)input.DeepClone());}
                catch(Exception error){
                    LastPresentationFailure=new InvalidOperationException("Committed barrage presentation failed; combat remains authoritative.",error);
                    foreach(var lease in new System.Collections.Generic.List<PresentationHold>(presentationHolds))if(!before.Contains(lease))lease.Dispose();
                }
            }}finally{publishingBarrage=false;deliveringBarrage=false;}
        }
        public bool TryGetCommittedRevision(out long value)
        {
            value=0;if(!OwnerCurrent||current==null||RecoveryRequired)return false;
            value=revision;return true;
        }
        // Read only the state this owner successfully saved and published. Consumers
        // must not use an unconfirmed candidate or read through an owner change.
        public (long Revision, string StateJson) CaptureCommittedState()
        {
            if (!OwnerCurrent || current == null || RecoveryRequired)
                throw new InvalidOperationException("Combat committed source unavailable.");
            return (revision, current.CaptureLocalState());
        }
        public (long Revision,string StateJson,Newtonsoft.Json.Linq.JObject Inputs) CaptureOrdinaryReplayInputs()
        {
            var committed=CaptureCommittedState();
            var inputs=current.ReadOrdinaryReplayInputs();
            if(!OwnerCurrent||RecoveryRequired||revision!=committed.Revision)throw new InvalidOperationException("Combat input source changed.");
            return (committed.Revision,committed.StateJson,inputs);
        }
        public CombatSceneDriver(Func<BasicCombatSimulation> factory,Func<RestoredCombatCheckpoint> load,Func<BasicCombatSimulation,long,long> save,Func<bool> ownerCurrent)
        {this.factory=factory??throw new ArgumentNullException(nameof(factory));this.load=load??throw new ArgumentNullException(nameof(load));this.save=save??throw new ArgumentNullException(nameof(save));this.ownerCurrent=ownerCurrent??throw new ArgumentNullException(nameof(ownerCurrent));}
        CombatSceneDriver(OrdinaryFiveCombatFactory source,Func<bool> ownerCurrent)
        {
            if(source?.Attempt==null)throw new ArgumentException("Original node attempt required.");
            factory=source.Create;sceneCopyFactory=source.CreateForSceneCopy;this.ownerCurrent=ownerCurrent??throw new ArgumentNullException(nameof(ownerCurrent));nodeInstance=true;
        }
        public static CombatSceneDriver ForOrdinaryNode(OrdinaryFiveCombatFactory factory,Func<bool> ownerCurrent)=>new CombatSceneDriver(factory,ownerCurrent);
        // Invalidate before releasing live state; durable requests and receipts belong to the caller.
        public void Retire(){if(retired)return;executionStarted=HasBegunExecution;retired=true;current?.RetireOrdinaryNativeOwners();current=null;displayState=null;NotifyStateChanged();StateChanged=null;OrdinaryExecutionBegan=null;RailArtilleryStepCommitted=null;BarragePresentationEvent=null;presentationHolds.Clear();}
        public static CombatSceneDriver ForRetainedStart(CombatCheckpointStore store,PendingRunOperation originalStart,Func<BasicCombatSimulation> trustedFactory,Func<bool> ownerCurrent)
        {
            if(store==null||originalStart==null)throw new ArgumentNullException();
            return new CombatSceneDriver(trustedFactory,()=>store.RestoreWithRevision(originalStart,trustedFactory),(s,r)=>store.Save(originalStart,s,r),ownerCurrent);
        }
        public static CombatSceneDriver ForOrdinarySeraStart(OrdinaryFiveSeraCheckpoint checkpoint,Func<bool> ownerCurrent)
        {
            if(checkpoint==null)throw new ArgumentNullException(nameof(checkpoint));
            // Initial creation is an explicit coordinator Save(factory.Create(), 0); recovery must never reset a missing record.
            return new CombatSceneDriver(checkpoint.Factory.Create,()=>checkpoint.Restore()??throw new InvalidOperationException("Original ordinary checkpoint is required for recovery."),checkpoint.Save,ownerCurrent);
        }
        public static CombatSceneDriver ForOrdinaryCombatStart(OrdinaryFiveCombatCheckpoint checkpoint,Func<bool> ownerCurrent)
        {
            if(checkpoint==null)throw new ArgumentNullException(nameof(checkpoint));
            return new CombatSceneDriver(checkpoint.Factory.Create,()=>checkpoint.Restore()??throw new InvalidOperationException("Original ordinary checkpoint is required for recovery."),checkpoint.Save,ownerCurrent);
        }
        public Newtonsoft.Json.Linq.JObject ScheduleResolvedSeraCommand(Newtonsoft.Json.Linq.JObject request)=>Change(s=>s.ScheduleResolvedSeraCommand(request),false);
        public Newtonsoft.Json.Linq.JObject ScheduleResolvedOrdinaryEffect(Newtonsoft.Json.Linq.JObject request)=>Change(s=>s.ScheduleResolvedOrdinaryEffect(request),false);
        public void Open()
        {
            if(!OwnerCurrent)throw new InvalidOperationException("Combat owner changed.");
            if(nodeInstance){
                if(current!=null||RecoveryRequired)throw new InvalidOperationException("Node reentry requires a new accepted attempt.");
                var initial=factory();if(!OwnerCurrent)throw new InvalidOperationException("Combat owner changed.");
                current=initial;revision=1;displayState=null;NotifyStateChanged();return;
            }
            bool recovering=current!=null&&RecoveryRequired;
            RecoveryRequired=true;
            if(!OwnerCurrent)throw new InvalidOperationException("Combat owner changed.");
            var restored=load();
            if(recovering&&restored==null)throw new InvalidOperationException("Recovery requires the retained checkpoint; initial state cannot replace it.");
            var state=restored?.Simulation??factory();
            if(state==null)throw new InvalidOperationException("Missing combat configuration.");
            var nextRevision=restored?.Revision??save(state,0);
            if(recovering){if(!OwnerCurrent)throw new InvalidOperationException("Combat owner changed.");state.SetPaused(true);nextRevision=save(state,nextRevision);}
            if(!OwnerCurrent)throw new InvalidOperationException("Combat owner changed.");
            current=state;revision=nextRevision;RecoveryRequired=false;displayState=null;NotifyStateChanged();
        }
        T Change<T>(Func<BasicCombatSimulation,T> apply,bool allowPaused=true,bool ordinaryAdvance=false)
        {
            if(changing||notifyingState)throw new InvalidOperationException("Combat commands cannot reenter an active state change or notification.");
            long previousRevision=revision;bool previousRecovery=RecoveryRequired;
            changing=true;
            try{
            if(deliveringBarrage)throw new InvalidOperationException("Presentation callbacks cannot mutate the published combat step.");
            if(!OwnerCurrent){RecoveryRequired=true;throw new InvalidOperationException("Combat owner changed.");}
            if(current==null||RecoveryRequired)throw new InvalidOperationException("Combat requires restoration.");
            if(!allowPaused&&(current.IsPaused||HasPresentationHold))throw new InvalidOperationException("Resume combat before submitting commands.");
            Newtonsoft.Json.Linq.JToken before;BasicCombatSimulation candidate;
            // Ordinary Advance records its operation before mutation; preserve the isolated candidate without serializing all prior history.
            bool journalChange=nodeInstance&&ordinaryAdvance&&current.IsOrdinaryCombat;int operationCount=current.OrdinaryOperationCount;
            try{candidate=current.CloneOwnedForScene(sceneCopyFactory??factory,out before,!journalChange);}
            catch{current.RetireOrdinaryNativeOwners();RecoveryRequired=true;throw;}
            candidate.CollectRailStepInputs=RailArtilleryStepCommitted!=null;
            candidate.CollectBarragePresentationEvents=BarragePresentationEvent!=null;
            T result;Newtonsoft.Json.Linq.JToken after;
            try{result=apply(candidate);after=journalChange?null:candidate.CaptureSceneState();}
            catch{candidate.RetireOrdinaryNativeOwners();current.RetireOrdinaryNativeOwners();RecoveryRequired=true;throw;}
            if(journalChange?candidate.OrdinaryOperationCount==operationCount:Newtonsoft.Json.Linq.JToken.DeepEquals(after,before)){candidate.RetireOrdinaryNativeOwners();return result;}
            try{
                var next=nodeInstance?checked(revision+1):save(candidate,revision);
                if(!OwnerCurrent)throw new InvalidOperationException("Combat owner changed.");
                bool began = !HasBegunExecution && candidate.HasOrdinaryExecutionStarted;
                current.RetireOrdinaryNativeOwners();current=candidate;displayState=null;executionStarted=HasBegunExecution;revision=next;
                if (began)
                {
                    try { OrdinaryExecutionBegan?.Invoke(); }
                    catch (Exception error) { LastPresentationFailure = error; }
                }
                foreach(var input in candidate.RailStepInputs)RailArtilleryStepCommitted?.Invoke((Newtonsoft.Json.Linq.JObject)input.DeepClone());
                foreach(var input in candidate.BarragePresentationEvents){
                    if(!OwnerCurrent)break;
                    PublishBarrage(input);
                }
                return result;
            }catch{candidate.RetireOrdinaryNativeOwners();current?.RetireOrdinaryNativeOwners();RecoveryRequired=true;throw;}
            }finally{changing=false;if(revision!=previousRevision||RecoveryRequired!=previousRecovery)NotifyStateChanged();}
        }
        // Trusted resolved stat transition; no equipment authority or new UI binding.
        public void RequestLocalPartyEnd(bool threatOnLeft)=>Change(s=>{s.RequestLocalPartyEnd(threatOnLeft);return true;},false);
        public void ApplyResolvedMaxHp(string targetId,int finalMaxHp)=>Change(s=>{s.ApplyResolvedMaxHp(targetId,finalMaxHp);return true;});
        public void ApplyResolvedMaxHpBatch(System.Collections.Generic.IReadOnlyDictionary<string,int> finalMaxima)
            =>Change(s=>{s.ApplyResolvedMaxHpBatch(finalMaxima);return true;});
        public void Advance(long elapsedTimeSpanTicks,int maximumSteps){if(deliveringBarrage)throw new InvalidOperationException("Presentation callbacks cannot advance combat.");if(OwnerCurrent&&!RecoveryRequired&&(current?.IsPaused==true||HasPresentationHold))return;Change(s=>{s.Advance(elapsedTimeSpanTicks,maximumSteps);return true;},ordinaryAdvance:true);}
        public string RequestSkill(string commandId,string actorId,string abilityId)=>Change(s=>s.RequestSkill(commandId,actorId,abilityId),false);
        public void SetUltimateAuto(bool enabled)=>Change(s=>{s.SetUltimateAuto(enabled);return true;},false);
        // Trusted execution-owner delivery only; no UI command or public HTTP resolution upload.
        internal Newtonsoft.Json.Linq.JObject ResolveOrdinaryObstacle(Newtonsoft.Json.Linq.JObject resolution)=>Change(s=>s.ResolveOrdinaryObstacle(resolution),false);
        public void SetPaused(bool paused)=>Change(s=>{if(s.IsPaused!=paused)s.SetPaused(paused);return true;});
        public void SetSpeed(int halfUnits)=>Change(s=>{s.SetSpeed(halfUnits);return true;});
        public bool SetEnemyFocus(string id)=>Change(s=>s.SetEnemyFocus(id),false);
        public void PreviewRailRoute(int index)=>Change(s=>{s.PreviewRailRoute(index);return true;},false);
        public bool ConfirmRailRoute(string offerId,string optionId)=>Change(s=>s.ConfirmRailRoute(offerId,optionId),false);
        public void SetRailThrottle(int bps)=>Change(s=>{s.SetRailThrottle(bps);return true;},false);
    }
}
