using System;
using System.Threading.Tasks;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class AccountScreenRuntime
    {
        AdSkipAccountService adSkip;
        AdRuntimePolicyService runtimeAds;
        public AdRuntimePolicyService RuntimeAds
        {
            get{if(disposed)throw new ObjectDisposedException(nameof(AccountScreenRuntime));if(runtimeAds==null){runtimeAds=new AdRuntimePolicyService(wire,session,generation);runtimeAds.Changed+=AdSkipChanged;}return runtimeAds;}
        }
        AdSkipUseController adSkipUse;
        public AdSkipUseController AdSkipUse {
            get {if(disposed)throw new ObjectDisposedException(nameof(AccountScreenRuntime));if(adSkipUse==null){adSkipUse=new AdSkipUseController(wire,journal,AdSkip,session,generation,ResumeApprovedReward);adSkipUse.Changed+=AdSkipUseChanged;}return adSkipUse;}
        }
        void AdSkipUseChanged(){if(disposed)return;try{Pending=journal?.Read(owner?.AccountId);}catch{ActionNotice="요청 보관함을 확인하지 못했습니다.";}Changed?.Invoke();}
        public Task UseAdSkipOfflineAsync(){SynchronizeSession();return Busy?Task.CompletedTask:AdSkipUse.UseOfflineAsync();}
        public Task UseAdSkipCombatAsync(string run,string preview){SynchronizeSession();return Busy?Task.CompletedTask:AdSkipUse.UseCombatAsync(run,preview);}
        Task RetryAdSkipAsync(){SynchronizeSession();return Busy?Task.CompletedTask:AdSkipUse.RetryAsync();}
        async Task ResumeApprovedReward(PendingAccountAction row){
            var original=owner;var stamp=ownerGeneration;if(!Current(original,stamp)||row.Account!=original.AccountId)return;
            Pending=row;
            if(row.Kind.StartsWith("ad-")){
                AdIntent=OfflineAdIntent.Parse(row.Payload);AdNotice="";Changed?.Invoke();
                if(Current(original,stamp))await RunAdAsync("spin",true);
            }else{
                var json=LastRail.Auth.GameApiJson.ReadObject(row.Payload);var intent=CombatAdIntent.Parse(row.Payload,row.Target,(string)json["rewardPreviewId"]);
                CombatAds.AdoptApproved(intent);if(!Current(original,stamp))return;
                await CombatAds.SpinAsync(intent.RunId,intent.PreviewId);
                if(Current(original,stamp)&&CombatAds.Intent?.Status=="CLAIMED"){InvalidateRunReads();await ReadRunResultsAsync(intent.RunId);if(Current(original,stamp))await RefreshSharedAsync();}
            }
        }
        public AdSkipAccountService AdSkip
        {
            get
            {
                if(disposed)throw new ObjectDisposedException(nameof(AccountScreenRuntime));
                if(adSkip==null){adSkip=new AdSkipAccountService(wire,session,generation);adSkip.Changed+=AdSkipChanged;}
                return adSkip;
            }
        }
        void AdSkipChanged(){if(!disposed)Changed?.Invoke();}
        public Task RefreshAdSkipAsync(){SynchronizeSession();return Task.WhenAll(AdSkip.RefreshAsync(),RuntimeAds.RefreshAsync());}
        // Display navigation reuses observations; commands retain their fresh authorization path.
        public Task EnsureAdSkipDisplayAsync(){
            SynchronizeSession();
            return Task.WhenAll(AdSkip.Snapshot==null&&!AdSkip.Loading?AdSkip.RefreshAsync():Task.CompletedTask,
                RuntimeAds.Status==AdRuntimePolicyStatus.Unknown?RuntimeAds.RefreshAsync():Task.CompletedTask);
        }
        async Task<bool> ConfirmNewAdPolicyAsync()
        {
            var original=session();long stamp=generation();await RefreshAdSkipAsync();
            return Current(original,stamp)&&RuntimeAds.Status==AdRuntimePolicyStatus.Enabled&&AdSkip.Snapshot?.Owned==false;
        }
    }
}
