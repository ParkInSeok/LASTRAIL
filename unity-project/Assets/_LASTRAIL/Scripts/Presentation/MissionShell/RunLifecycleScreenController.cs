using System;
using System.Threading.Tasks;
namespace LastRail.Presentation.MissionShell
{
    public sealed class RunLifecycleScreenController:IDisposable
    {
        readonly AccountScreenRuntime owner;readonly string run;readonly Func<bool> session;Action openAd;bool disposed;
        public RunLifecycleScreenController(AccountScreenRuntime owner,string run,Action openAd)
        {this.owner=owner;this.run=run;this.openAd=openAd;session=owner?.CaptureScreenSession()??(()=>true);}
        bool Current=>!disposed&&session();
        public RunLifecycleScreenState Display=>Current?new RunLifecycleScreenState(owner,run):null;
        public Task LoadAsync(bool reuse)=>!Current||owner==null?Task.CompletedTask:reuse?owner.EnsureRunResultsAsync(run):Task.WhenAll(owner.EnsureRunResultsAsync(run),owner.EnsureAdSkipDisplayAsync());
        public Task ClaimAsync(){if(!Current||owner==null)return Task.CompletedTask;if(owner.CombatAds.HasPending(run)){openAd?.Invoke();return Task.CompletedTask;}return owner.ClaimRunRewardAsync(run);}
        public Task RecoverAsync()=>!Current||owner==null?Task.CompletedTask:owner.RecoverFinishAsync(run);
        public Task RefreshAsync()=>!Current||owner==null?Task.CompletedTask:owner.CanRetryRunReceiptHandoff(run)?owner.RetryRunReceiptHandoffAsync(run):owner.ReadRunResultsAsync(run);
        public void Dispose(){disposed=true;openAd=null;}
    }
}
