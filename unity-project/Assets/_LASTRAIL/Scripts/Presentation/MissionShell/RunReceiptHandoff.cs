using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class AccountScreenRuntime
    {
        [System.Flags]
        enum ClaimOwnerRecovery { None=0, Account=1, Wallet=2, Progress=4 }
        ConfirmedRunReceiptArchive runReceiptArchive;
        Task runReceiptHandoff;
        string receiptStorageRetryRun;
        long receiptHandoffGeneration;
        public bool RunReceiptHandoffLoading { get; private set; }
        public bool RunClaimApplicationComplete { get; private set; }
        bool runClaimRequiresOwners;

        // Prepare independent owners before publishing those that are valid.
        // A missing owner holds completion, not the other owners' confirmed state.
        bool TryApplyCompleteRunClaim(PendingRunOperation operation)
            => TryApplyRunClaimOwners(operation,out _);

        bool TryApplyRunClaimOwners(PendingRunOperation operation,out ClaimOwnerRecovery missing)
        {
            missing=ClaimOwnerRecovery.Account|ClaimOwnerRecovery.Wallet|ClaimOwnerRecovery.Progress;
            if(disposed||!Current(owner,ownerGeneration)||operation==null||operation.AccountId!=owner.AccountId||
                operation.ResponseVersion!=RunCompleteClaimReceipt.Version||operation.AcceptedResponseJson==null)return false;
            var receipt=RunCompleteClaimReceipt.Read(operation.AcceptedResponseJson,operation.AccountId,operation.TargetId,
                GameApiJson.String(GameApiJson.ReadObject(operation.BodyJson),"rewardPreviewId"),operation.IdempotencyKey);
            runClaimRequiresOwners=true;
            var data=receipt.Capture();var wallet=(JObject)data["owners"]["wallet"];
            var account=Account;
            bool accountReady=Account!=null&&AccountStatus==AccountStateReadStatus.Ready&&Account.TryApplyRunClaimFinancial(receipt,out account);
            var nextWallet=Wallet;
            bool walletReady=(string)wallet["kind"]=="UNCHANGED"||!WalletLoading&&Wallet!=null&&Wallet.AccountId==owner.AccountId&&Wallet.TryApplyClaim(wallet,out nextWallet);
            LastRail.Narrative.ConfirmedStoryProgress progress=null;
            bool progressReady=storyArchive!=null&&storyArchive.TryPrepareClaim((JObject)data["owners"]["progress"],out progress);
            missing=(accountReady?ClaimOwnerRecovery.None:ClaimOwnerRecovery.Account)|
                (walletReady?ClaimOwnerRecovery.None:ClaimOwnerRecovery.Wallet)|
                (progressReady?ClaimOwnerRecovery.None:ClaimOwnerRecovery.Progress);
            bool accountChanged=accountReady&&!ReferenceEquals(Account,account);
            bool walletChanged=walletReady&&!ReferenceEquals(Wallet,nextWallet);
            bool progressChanged=progressReady&&!ReferenceEquals(storyArchive.ConfirmedProgress,progress);
            if(accountChanged){Account=account;AccountErrorCode=null;accountRequest++;accountRead=null;}
            if(walletChanged){Wallet=nextWallet;walletRequest++;walletRead=null;WalletLoading=false;}
            if(progressChanged)storyArchive.CommitClaim(progress);
            if(progressChanged)storyArchive.NotifyClaim();
            if(accountChanged||walletChanged||progressChanged)Changed?.Invoke();
            return accountReady&&walletReady&&progressReady;
        }

        async Task<bool> ApplyRunClaimWithRecoveryAsync(PendingRunOperation operation)
        {
            if(operation?.ResponseVersion!=RunCompleteClaimReceipt.Version)return false;
            if(TryApplyRunClaimOwners(operation,out var missing))return true;
            var original=owner;var stamp=ownerGeneration;var epoch=receiptHandoffGeneration;
            if(!Current(original,stamp)||operation.AccountId!=original.AccountId)return false;
            // One bounded recovery of deficient owners only. Never reissue claim.
            await Task.WhenAll(
                (missing&ClaimOwnerRecovery.Account)!=0?RefreshAccountAsync():Task.CompletedTask,
                (missing&ClaimOwnerRecovery.Wallet)!=0?RefreshWalletAsync():Task.CompletedTask,
                (missing&ClaimOwnerRecovery.Progress)!=0?StoryArchive.RefreshAsync():Task.CompletedTask);
            if(!Current(original,stamp)||epoch!=receiptHandoffGeneration)return false;
            return TryApplyRunClaimOwners(operation,out _);
        }

        async Task<bool> TryConsumeConfirmedClaimResultAsync(PendingRunOperation operation)
        {
            if(operation?.ResponseVersion!=RunCompleteClaimReceipt.Version||operation.AcceptedResponseJson==null||
                !lifecycleVerified||Lifecycle==null)return false;
            var receipt=RunCompleteClaimReceipt.Read(operation.AcceptedResponseJson,owner.AccountId,operation.TargetId,
                GameApiJson.String(GameApiJson.ReadObject(operation.BodyJson),"rewardPreviewId"),operation.IdempotencyKey);
            var next=Lifecycle.WithConfirmedClaim(receipt);
            if(next==null)return false;
            var original=owner;var stamp=ownerGeneration;
            InvalidateRunReads();ResetRunReceiptHandoff();var request=runResultsRequest;
            Lifecycle=next;lifecycleVerified=true;LifecycleNotice=next.Notice;LifecycleLoading=false;
            verifiedRunReceipt=next.TransactionReceipt;RunRewardPreview=null;RunResultsLoading=true;
            try
            {
                bool applied=await ApplyRunClaimWithRecoveryAsync(operation);
                if(!Current(original,stamp)||request!=runResultsRequest)return true;
                await ArchiveRunReceiptAsync(operation.TargetId);
                if(!Current(original,stamp)||request!=runResultsRequest)return true;
                if(!RunClaimApplicationComplete&&receiptStorageRetryRun==null)
                    RunResultNotice=applied?"보상 지급은 확인됐습니다. 영수증의 기기 저장을 확인하지 못했습니다.":
                        "보상 지급은 확인됐습니다. 일부 상태를 아직 반영하지 못했습니다. 원래 기록으로 다시 확인해 주세요.";
            }
            finally{if(Current(original,stamp)&&request==runResultsRequest){RunResultsLoading=false;Changed?.Invoke();}}
            return true;
        }

        public async Task<bool> ConsumeArchivedRunClaimAsync(string runId)
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;var archive=runReceiptArchive;
            if(archive==null||string.IsNullOrEmpty(runId)||!Current(original,stamp))return false;
            var epoch=receiptHandoffGeneration;
            RunClaimApplicationComplete=false;
            var operation=await archive.ReadAsync(original.AccountId,runId);
            if(!Current(original,stamp)||archive!=runReceiptArchive||epoch!=receiptHandoffGeneration)return false;
            bool applied=await ApplyRunClaimWithRecoveryAsync(operation);
            if(!Current(original,stamp)||archive!=runReceiptArchive||epoch!=receiptHandoffGeneration)return false;
            RunClaimApplicationComplete=applied;
            Changed?.Invoke();
            return applied;
        }

        async Task RecoverArchivedClaimOwnersAsync(string runId)
        {
            var original=owner;var stamp=ownerGeneration;var request=runResultsRequest;
            RunResultsLoading=true;
            try
            {
                bool applied=await ConsumeArchivedRunClaimAsync(runId);
                if(!Current(original,stamp)||request!=runResultsRequest)return;
                RunResultNotice=applied?"":"보상 지급은 확인됐습니다. 일부 상태를 아직 반영하지 못했습니다. 원래 기록으로 다시 확인해 주세요.";
            }
            catch
            {
                if(Current(original,stamp)&&request==runResultsRequest)
                    RunResultNotice="보상 지급은 확인됐지만 기기에 보관된 기록을 반영하지 못했습니다. 원래 기록은 유지됩니다.";
            }
            finally
            {
                if(Current(original,stamp)&&request==runResultsRequest){RunResultsLoading=false;Changed?.Invoke();}
            }
        }

        public void BindRunReceiptArchive(ConfirmedRunReceiptArchive archive)
        {
            ResetRunReceiptHandoff();
            runReceiptArchive = archive;
        }

        void ResetRunReceiptHandoff()
        {
            receiptHandoffGeneration++;
            runReceiptHandoff = null;
            receiptStorageRetryRun = null;
            RunReceiptHandoffLoading = false;
            RunClaimApplicationComplete = false;
            runClaimRequiresOwners = false;
        }

        public bool CanRetryRunReceiptHandoff(string runId) => !disposed && !Busy && !RunResultsLoading &&
            !RunReceiptHandoffLoading && receiptStorageRetryRun == runId && runReceiptArchive != null;

        public Task RetryRunReceiptHandoffAsync(string runId)
        {
            SynchronizeSession();
            if (receiptStorageRetryRun != runId || RunResultsLoading || Busy) return Task.CompletedTask;
            return ArchiveRunReceiptAsync(runId);
        }

        Task ArchiveRunReceiptAsync(string runId)
        {
            if (disposed || runReceiptArchive == null || runRecovery == null || !lifecycleVerified ||
                Lifecycle?.RunId != runId || Lifecycle.RewardStatus != "CLAIMED" || verifiedRunReceipt == null) return Task.CompletedTask;
            return ShareRead(ref runReceiptHandoff, () => HandoffRunReceiptCoreAsync(runId));
        }

        async Task HandoffRunReceiptCoreAsync(string runId)
        {
            var saved = SavedRewardClaim(runId);
            if (saved?.AcceptedResponseJson == null) return;
            var original = owner; var stamp = ownerGeneration;
            var request = runResultsRequest; var epoch = receiptHandoffGeneration;
            var archive = runReceiptArchive;
            RunReceiptHandoffLoading = true;
            bool CurrentHandoff() => Current(original, stamp) && request == runResultsRequest && epoch == receiptHandoffGeneration;
            try
            {
                bool stored = await runRecovery.HandoffAsync(saved, archive.StoreAsync);
                if (!CurrentHandoff()) return;
                receiptStorageRetryRun = stored ? null : runId;
                RunClaimApplicationComplete=stored&&TryApplyCompleteRunClaim(saved);
                RunResultNotice = stored ? "" : "지급 영수증의 기기 저장을 완료하지 못했습니다. 원래 기록은 유지됩니다. 저장을 다시 시도해 주세요.";
                if(stored&&saved.ResponseVersion==RunCompleteClaimReceipt.Version&&!RunClaimApplicationComplete)
                    RunResultNotice="보상 지급은 확인됐습니다. 진행정보 등 일부 상태를 아직 반영하지 못했습니다. 원래 기록으로 다시 확인해 주세요.";
            }
            finally
            {
                if (CurrentHandoff()) { RunReceiptHandoffLoading = false; if (!RunResultsLoading) Changed?.Invoke(); }
            }
        }
    }
}
