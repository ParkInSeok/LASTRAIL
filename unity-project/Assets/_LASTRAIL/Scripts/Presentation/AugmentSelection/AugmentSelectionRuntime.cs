using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
namespace LastRail.Presentation.AugmentSelection
{
    // Node-screen controller composition. The original node owner supplies the
    // verified grant, retained START, account generation and durable request journal.
    // Owns controller/presenter subscriptions; borrows View, transport and stores.
    public sealed class AugmentSelectionRuntime:IDisposable
    {
        AugmentSelectionView view;
        readonly AugmentSelectionScope scope;
        readonly AugmentSelectionController controller;
        AugmentSelectionPresenter presenter;
        readonly Func<string,string,AugmentCardPresentation> definitions;
        readonly CombatO22ApplicationStore originals;
        readonly string retainedStartHash;
        readonly bool applicationRequired;
        readonly bool freeRerolls;
        internal AugmentSelectionSnapshot CurrentSelection=>controller.Snapshot;
        internal bool CommandPending=>controller.Busy||controller.HasPending;
        readonly Action<AugmentSelectionScope,AugmentSelectionSnapshot,string> commitOriginal;
        bool disposed,acquired,applied,completionPublished;
        public bool Completed=>!disposed&&acquired&&(!applicationRequired||applied)&&!controller.Busy&&!controller.HasPending&&controller.Snapshot?.Confirmed==true;
        // Lifetime fact only: the original journal finished before its route
        // ceased to be current. It grants no effect or current-scope authority.
        public bool TransactionCompleted=>completionPublished;
        public string CompletionState=>!Completed?"PENDING":applied?"APPLIED":"ACQUIRED_PENDING_COMBAT";
        public event Action SelectionCompleted;
        public event Action SelectionApplied;
        public AugmentSelectionRuntime(AugmentSelectionView view,AugmentSelectionScope scope,IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<bool> ownerCurrent,IAugmentSelectionJournal journal,CombatO22ApplicationStore originals,string retainedStartHash)
            :this(view,scope,transport,session,ownerCurrent,journal,originals,retainedStartHash,null){}
        internal AugmentSelectionRuntime(AugmentSelectionView view,AugmentSelectionScope scope,IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<bool> ownerCurrent,IAugmentSelectionJournal journal,CombatO22ApplicationStore originals,string retainedStartHash,Func<string,string,AugmentCardPresentation> definitions,Action<CombatO22ApplicationOriginal> applyCommitted=null,bool freeRerolls=false)
        {
            this.scope=scope??throw new ArgumentNullException(nameof(scope));this.definitions=definitions;this.freeRerolls=freeRerolls;
            this.originals=originals??throw new ArgumentNullException(nameof(originals));this.retainedStartHash=retainedStartHash;applicationRequired=applyCommitted!=null;
            if(ownerCurrent==null)throw new ArgumentNullException(nameof(ownerCurrent));
            var port=new AugmentSelectHttpPort(transport,scope.Owner,session,ownerCurrent,freeRerolls);
            Action<CombatO22ApplicationOriginal> apply=applyCommitted==null?null:(Action<CombatO22ApplicationOriginal>)(original=>{applyCommitted(original);applied=true;});
            var receiver=new O22SelectionOriginalReceiver(originals,scope,retainedStartHash,()=>!disposed&&ownerCurrent(),apply);
            commitOriginal=(requestScope,selection,json)=>{receiver.Commit(requestScope,selection,json);acquired=true;};
            controller=new AugmentSelectionController(port,journal,()=>!disposed&&ownerCurrent()?scope.Owner:null,commitOriginal,id=>id=="O22");
            controller.Changed+=PublishCompletion;
            AttachView(view);
        }
        void PublishCompletion(){if(!completionPublished&&Completed){completionPublished=true;if(applied)SelectionApplied?.Invoke();if(!disposed)SelectionCompleted?.Invoke();}}
        // The transaction belongs to this runtime, not to a transient View.
        // Detach never cancels a sent command or clears its original journal.
        public void AttachView(AugmentSelectionView next){
            if(disposed)throw new ObjectDisposedException(nameof(AugmentSelectionRuntime));
            DetachView();view=next?next:throw new ArgumentNullException(nameof(next));
            presenter=new AugmentSelectionPresenter(controller,view,definitions,null,freeRerolls);
            view.Unavailable+=DetachView;presenter.Bind();
        }
        public void DetachView(){if(view)view.Unavailable-=DetachView;presenter?.Dispose();presenter=null;view=null;}
        // Original host supplies its pinned definition resolver; never load a new
        // catalog on screen entry. This factory does not issue a node or APPLIED.
        public static AugmentSelectionRuntime CreatePersistent(AugmentSelectionView view,AugmentSelectionScope scope,IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<bool> ownerCurrent,IAndroidKeystoreNativeStoragePort crypto,string journalPath,CombatO22ApplicationStore originals,string retainedStartHash,Func<string,string,AugmentCardPresentation> definitions)
        {
            if(definitions==null)throw new ArgumentNullException(nameof(definitions));
            return new AugmentSelectionRuntime(view,scope,transport,session,ownerCurrent,new AugmentSelectionFileJournal(scope,crypto,journalPath),originals,retainedStartHash,definitions);
        }
        public async Task OpenAsync(AugmentSelectionSnapshot originalGrant)
        {
            if(disposed)throw new ObjectDisposedException(nameof(AugmentSelectionRuntime));
            // A confirmed supplied grant may already have its durable original.
            // Revalidate it locally; the controller still owns pending-request
            // recovery and must finish that journal before publishing completion.
            if(originalGrant?.Confirmed==true&&originalGrant.SelectedCandidateId=="O22"){
                var original=originals.Load(scope.Owner.AccountId,scope.RunId,scope.EventId,scope.Owner.Environment,retainedStartHash);
                if(original!=null)commitOriginal(scope,originalGrant,original.Capture());
            }
            await controller.OpenFromSnapshotAsync(scope,originalGrant);
            if(!disposed&&controller.Snapshot?.Confirmed==true&&!controller.HasPending&&!controller.Busy&&!acquired)
                throw new InvalidOperationException("Confirmed selection requires its original acquisition or pending SELECT request");
        }
        public static AugmentSelectionRuntime CreatePersistentApplied(AugmentSelectionView view,AugmentSelectionScope scope,IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<bool> ownerCurrent,IAndroidKeystoreNativeStoragePort crypto,string journalPath,CombatO22ApplicationStore originals,string retainedStartHash,Func<string,string,AugmentCardPresentation> definitions,Action<CombatO22ApplicationOriginal> applyCommitted)
        {
            if(definitions==null||applyCommitted==null)throw new ArgumentNullException();
            return new AugmentSelectionRuntime(view,scope,transport,session,ownerCurrent,new AugmentSelectionFileJournal(scope,crypto,journalPath),originals,retainedStartHash,definitions,applyCommitted);
        }
        public void Dispose()
        {if(disposed)return;disposed=true;DetachView();controller.Changed-=PublishCompletion;SelectionApplied=null;SelectionCompleted=null;controller.Dispose();}
    }
}
