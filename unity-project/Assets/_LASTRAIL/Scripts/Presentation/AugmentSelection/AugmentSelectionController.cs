using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.AugmentSelection
{
    public sealed class AugmentSelectionController:IDisposable
    {
        readonly IAugmentSelectionPort port;
        readonly IAugmentSelectionJournal journal;
        readonly Func<AugmentSelectionOwner> owner;
        readonly Action<AugmentSelectionScope,AugmentSelectionSnapshot,string> commitApplicationOriginal;
        readonly Func<string,bool> canApply;
        AugmentSelectionScope scope;
        AugmentSelectionSnapshot snapshot;
        AugmentSelectionCommand pending;
        CancellationTokenSource cancellation=new CancellationTokenSource();
        long lifetime;
        bool busy,disposed;
        string notice="";
        bool Bound=>!disposed&&scope!=null&&ReferenceEquals(scope.Owner,owner());
        public AugmentSelectionSnapshot Snapshot=>Bound?snapshot:null;
        public bool Busy=>Bound&&busy;
        public bool HasPending=>Bound&&pending!=null;
        public string Notice=>Bound?notice:"";
        public event Action Changed;
        public AugmentSelectionController(IAugmentSelectionPort port,IAugmentSelectionJournal journal,Func<AugmentSelectionOwner> owner)
            :this(port,journal,owner,null){}
        // The original mode receiver validates against retained START and durably
        // saves the immutable acquisition before returning. This is not APPLIED.
        public AugmentSelectionController(IAugmentSelectionPort port,IAugmentSelectionJournal journal,Func<AugmentSelectionOwner> owner,Action<AugmentSelectionScope,AugmentSelectionSnapshot,string> commitApplicationOriginal)
            :this(port,journal,owner,commitApplicationOriginal,null){}
        public AugmentSelectionController(IAugmentSelectionPort port,IAugmentSelectionJournal journal,Func<AugmentSelectionOwner> owner,Action<AugmentSelectionScope,AugmentSelectionSnapshot,string> commitApplicationOriginal,Func<string,bool> canApply)
        {this.port=port??throw new ArgumentNullException(nameof(port));this.journal=journal??throw new ArgumentNullException(nameof(journal));this.owner=owner??throw new ArgumentNullException(nameof(owner));this.commitApplicationOriginal=commitApplicationOriginal;this.canApply=canApply;}
        bool Current(long stamp)=>stamp==lifetime&&Bound;
        public Task OpenAsync(string runId,string eventId)=>OpenCoreAsync(runId,eventId,null,null);
        // The host supplies a validated event snapshot from the original node response.
        // This is an internal intake, not a new wire contract or effect-application receipt.
        public async Task OpenFromSnapshotAsync(AugmentSelectionScope source,AugmentSelectionSnapshot supplied)
        {
            if(disposed)return;
            if(source==null||supplied==null||!ReferenceEquals(source.Owner,owner())||source.RunId!=supplied.RunId||source.EventId!=supplied.EventId)
                throw new ArgumentException("Supplied selection scope");
            if(Bound&&scope.RunId==source.RunId&&scope.EventId==source.EventId)RequireSnapshot(supplied);
            await OpenCoreAsync(source.RunId,source.EventId,source.Owner,supplied);
        }
        async Task OpenCoreAsync(string runId,string eventId,AugmentSelectionOwner suppliedOwner,AugmentSelectionSnapshot supplied)
        {
            if(disposed)return;Reset();var current=owner();if(current==null)return;
            if(suppliedOwner!=null&&!ReferenceEquals(suppliedOwner,current))return;
            scope=new AugmentSelectionScope(current,runId,eventId);
            try{string saved=journal.Load(scope.StorageKey);pending=saved==null?null:AugmentSelectionCommand.Restore(scope,saved);}
            catch(Exception){notice="저장된 요청을 확인할 수 없습니다.";busy=true;Changed?.Invoke();return;}
            if(!Bound)return;
            // Ambiguous persisted commands still use the original recovery read and key.
            if(supplied!=null){RequireSnapshot(supplied);snapshot=supplied;if(pending==null){Changed?.Invoke();return;}}
            await RefreshAsync();
        }
        public async Task RefreshAsync()
        {
            if(!Bound||busy)return;long stamp=lifetime;busy=true;notice="증강 상태 확인 중";Changed?.Invoke();
            try{if(!Current(stamp))return;await ReadCurrent(stamp);if(Current(stamp))notice=pending==null?"":"처리 결과 재확인이 필요합니다.";}
            catch(Exception){if(Current(stamp))notice="증강 상태를 확인하지 못했습니다.";}
            finally{if(Current(stamp)){busy=false;Changed?.Invoke();}}
        }
        async Task ReadCurrent(long stamp)
        {
            string json=await port.ReadAsync(scope,cancellation.Token);if(!Current(stamp))return;
            var next=AugmentSelectionSnapshot.Parse(json);
            RequireSnapshot(next);snapshot=next;
        }
        void RequireSnapshot(AugmentSelectionSnapshot next)
        {
            if(next.RunId!=scope.RunId||next.EventId!=scope.EventId)throw new FormatException("Read scope");
            snapshot?.RequireBinding(next);pending?.Before.RequireBinding(next);
            if(snapshot!=null&&(next.GrantRevision<snapshot.GrantRevision||snapshot.Confirmed&&!next.Confirmed))throw new FormatException("Read regressed");
            if(snapshot!=null&&next.GrantRevision==snapshot.GrantRevision&&!JToken.DeepEquals(JObject.Parse(next.Json),JObject.Parse(snapshot.Json)))throw new FormatException("Same revision changed facts");
        }
        public Task SelectAsync(int slot)=>Start("SELECT",slot,null);
        public Task RerollAsync(int slot,string mode)=>Start("REROLL",slot,mode);
        Task Start(string operation,int slot,string mode)
        {
            if(!Bound||busy||pending!=null||snapshot==null||snapshot.Confirmed)return Task.CompletedTask;
            try
            {
                if(operation=="SELECT"&&canApply!=null&&!canApply(snapshot.Slots[slot].CandidateId)){notice="현재 전투에서 지원하지 않는 선택입니다.";Changed?.Invoke();return Task.CompletedTask;}
                var next=AugmentSelectionCommand.Create(scope,operation,slot,mode,snapshot);
                journal.SaveNew(scope.StorageKey,next.Serialized);pending=next;
            }
            catch(Exception){
                // Persistence may have committed before its acknowledgement failed.
                // Recover that exact original; never manufacture a new request key.
                try{
                    string saved=journal.Load(scope.StorageKey);
                    var restored=saved==null?null:AugmentSelectionCommand.Restore(scope,saved);
                    if(restored!=null)snapshot.RequireBinding(restored.Before);
                    pending=restored;
                    notice=pending==null?"요청을 저장하지 못했거나 현재 사용할 수 없습니다.":"저장된 원요청을 다시 확인해 주세요.";
                }catch{busy=true;notice="저장된 요청을 확인할 수 없습니다. 화면을 다시 열어 복구해 주세요.";}
                Changed?.Invoke();return Task.CompletedTask;
            }
            return RetryAsync();
        }
        public async Task RetryAsync()
        {
            if(!Bound||busy||pending==null)return;
            if(pending.Operation=="SELECT"&&canApply!=null&&!canApply(pending.Before.Slots[pending.SlotIndex].CandidateId)){notice="현재 전투에서 지원하지 않는 선택입니다.";Changed?.Invoke();return;}
            long stamp=lifetime;var request=pending;var requestScope=scope;bool confirmedSelection=false;busy=true;notice=request.Operation=="SELECT"?"선택을 반영하고 있습니다.":"처리 결과 확인 중";Changed?.Invoke();
            try
            {
                if(!Current(stamp))return;
                string json=await port.ExecuteAsync(requestScope,request,cancellation.Token);if(!Current(stamp))return;
                var receipt=request.ValidateResult(json,requestScope);
                confirmedSelection=request.Operation=="SELECT"&&receipt.Confirmed;
                // A replay receipt is historical. Never regress the displayed current revision.
                snapshot?.RequireBinding(receipt);
                if(snapshot!=null&&receipt.GrantRevision==snapshot.GrantRevision&&!JToken.DeepEquals(JObject.Parse(receipt.Json),JObject.Parse(snapshot.Json)))throw new FormatException("Same revision changed receipt facts");
                if(snapshot!=null&&receipt.GrantRevision>snapshot.GrantRevision&&snapshot.Confirmed&&!receipt.Confirmed)throw new FormatException("Receipt regressed confirmation");
                var original=SelectionJson.Object(json)["applicationOriginal"];
                if(original!=null){if(commitApplicationOriginal==null)throw new InvalidOperationException("Original acquisition receiver is not installed");commitApplicationOriginal(requestScope,receipt,original.ToString());if(!Current(stamp))return;}
                if(snapshot==null||receipt.GrantRevision>snapshot.GrantRevision)snapshot=receipt;
                // The validated receipt supplies the complete selection state. CONFIRMED
                // records the decision only; applying combat effects remains the domain owner's job.
                journal.Complete(requestScope.StorageKey,request.Serialized);pending=null;notice="";
            }
            catch(AugmentSelectionRejectedException rejection)
            {
                if(!Current(stamp))return;
                try{
                    if(rejection.RequiresSuppliedSnapshot)
                    {
                        journal.Complete(requestScope.StorageKey,request.Serialized);pending=null;snapshot=null;
                        notice="요청이 거절되었습니다. 노드의 최신 선택 상태를 다시 받아 주세요.";
                    }
                    else {await ReadCurrent(stamp);if(!Current(stamp))return;journal.Complete(requestScope.StorageKey,request.Serialized);pending=null;notice="요청이 거절되어 최신 상태를 확인했습니다.";}
                }
                catch(Exception){if(Current(stamp))notice="요청 결과와 최신 상태를 다시 확인해 주세요.";}
            }
            catch(Exception){if(Current(stamp))notice=confirmedSelection?"선택한 효과를 복구하고 있습니다.":"처리 결과가 불확실합니다. 같은 요청을 다시 확인해 주세요.";}
            finally{if(Current(stamp)){busy=false;Changed?.Invoke();}}
        }
        public void Reset()
        {
            if(disposed)return;
            lifetime++;cancellation.Cancel();cancellation.Dispose();cancellation=new CancellationTokenSource();
            scope=null;snapshot=null;pending=null;busy=false;notice="";
            Changed?.Invoke();
        }
        public void Dispose(){if(disposed)return;Reset();disposed=true;cancellation.Dispose();Changed=null;}
    }
}
