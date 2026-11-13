using System;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Gacha;
using LastRail.OfflineRewards;
using LastRail.Run;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class AccountScreenRuntime : IDisposable
    {
        readonly IRunApiHttpTransport wire;
        public DataLifecycleTrace LifecycleDiagnostics {get;}
        readonly IAccountActionJournal journal;
        readonly Func<GameApiAuthSessionSnapshot> session;
        readonly Func<long> generation;
        GameApiAuthSessionSnapshot owner;
        long ownerGeneration, walletRequest, offlineRequest, accountRequest,growthRequest;
        bool disposed, writing;
        PendingAccountAction localCompletion;
        string localCompletionNotice;
        Task accountRead,walletRead,growthRead;
        LastRail.Narrative.StoryArchiveReadModel storyArchive;
        public LastRail.Narrative.StoryArchiveReadModel StoryArchive
        {
            get
            {
                if(disposed)throw new ObjectDisposedException(nameof(AccountScreenRuntime));
                return storyArchive??(storyArchive=new LastRail.Narrative.StoryArchiveReadModel(wire,session,generation,LifecycleDiagnostics));
            }
        }
        public CrystalWalletRead Wallet {get;private set;}
        public AccountStateSnapshot Account {get;private set;}
        public AccountStateReadStatus AccountStatus {get;private set;}
        public string AccountErrorCode {get;private set;}
        public bool AccountIsStale=>Account!=null&&AccountStatus!=AccountStateReadStatus.Ready;
        public string AccountNotice=>
            AccountStatus==AccountStateReadStatus.Ready?"":
            (AccountIsStale?"이전 정보 · ":"")+
            (AccountStatus==AccountStateReadStatus.ApiUnavailable?"이 서버에서 계정 정보를 조회할 수 없습니다.":
             AccountStatus==AccountStateReadStatus.VersionMismatch?"게임 버전을 확인해 주세요.":
             AccountStatus==AccountStateReadStatus.AuthRequired?"로그인 상태를 확인해 주세요.":
             AccountStatus==AccountStateReadStatus.ContractMismatch?"계정 정보 형식을 확인하지 못했습니다.":
             AccountStatus==AccountStateReadStatus.Loading?"계정 정보 확인 중":"계정 정보를 불러오지 못했습니다.");
        public string BalanceText(int index)
        {
            if(index==2)return Wallet?.Status==CrystalWalletReadStatus.Ready?Wallet.TotalBalance.Value.ToString("N0"):"—";
            if(Account==null)return "—";
            var value=index==0?(Account.Fuel==null?"—":Account.Fuel.Number("current")+" / "+Account.Fuel.Number("max")):(Account.Currencies.TryGetValue("CREDITS",out var credits)?credits.ToString("N0"):"—");
            return value+(AccountIsStale&&value!="—"?" *":"");
        }
        public OfflineRewardPreviewDto Offline {get;private set;}
        public GachaPullResponseDto LastPull {get;private set;}
        public OfflineClaimReceipt LastOfflineReceipt {get;private set;}
        public FormationReceipt LastFormationReceipt {get;private set;}
        public string ActionErrorCode {get;private set;}
        public OperatorGrowthConfig Growth {get;private set;}
        public bool GrowthReady {get;private set;}
        public GrowthReceipt LastGrowthReceipt {get;private set;}
        public string GrowthNotice {get;private set;}="";
        public FormationDraft Formation {get;}
        public PendingAccountAction Pending {get;private set;}
        bool RewardExecutionBusy=>writing || passClaimAllRunning || adWriting || combatAds?.Busy==true;
        public bool Busy=>RewardExecutionBusy || adSkipUse?.Busy==true;
        public bool WalletLoading {get;private set;}
        public bool OfflineLoading {get;private set;}
        public string OfflineNotice {get;private set;}="";
        public string ActionNotice {get;private set;}="";
        public event Action Changed;
        public event Action AccountDataInvalidated;
        public AccountScreenRuntime(IRunApiHttpTransport wire,IAccountActionJournal journal,Func<GameApiAuthSessionSnapshot> session,Func<long> generation,IMonthlyAttendanceRegistry monthlyRegistry=null,DataLifecycleTrace lifecycle=null)
        {LifecycleDiagnostics=lifecycle;this.wire=wire??throw new ArgumentNullException(nameof(wire));this.journal=journal;this.session=session;this.generation=generation;this.monthlyRegistry=monthlyRegistry;Formation=new FormationDraft(this);}
        public void SynchronizeSession()
        {
            if(disposed)return;var next=session();var nextGeneration=generation();
            storyArchive?.SynchronizeSession();
            if(!ReferenceEquals(next,owner)||nextGeneration!=ownerGeneration)riskRunBinding?.Invalidate();
            if(ReferenceEquals(next,owner)&&nextGeneration==ownerGeneration)return;
            bool retain=owner!=null&&next!=null&&owner.AccountId==next.AccountId&&owner.AccountStatus=="ACTIVE"&&next.AccountStatus=="ACTIVE"&&nextGeneration==ownerGeneration;
            var retainedReceipt=retain&&localCompletion!=null?LastFormationReceipt:null;
            if(!retain){localCompletion=null;localCompletionNotice=null;ResetOrdinaryTravel();}
            var retainedAccount=retain?Account:null;
            var retainedAccountStatus=AccountStatus==AccountStateReadStatus.Loading?AccountStateReadStatus.NotLoaded:AccountStatus;
            var retainedAccountError=AccountErrorCode;
            var retainedWallet=retain&&(Wallet?.MutationSequence.HasValue==true||!WalletLoading&&Wallet?.Status==CrystalWalletReadStatus.Ready)?Wallet:null;
            if(retainedWallet!=null&&WalletLoading)retainedWallet=CrystalWalletRead.Parse(0,null).PreserveNewerBalances(retainedWallet);
            var retainedGrowth=retain&&GrowthReady?Growth:null;
            LifecycleDiagnostics?.Record(LifecycleData.Account,retain?LifecycleEvent.CredentialsChanged:LifecycleEvent.Invalidated,retain?LifecycleReason.CredentialChanged:LifecycleReason.SessionChanged,nextGeneration);
            if(retain){equipmentGeneration++;equipmentRead=null;}else ResetEquipmentInventory();owner=next;ownerGeneration=nextGeneration;walletRequest++;offlineRequest++;accountRequest++;combatAds?.Reset();adSkip?.Reset();adSkipUse?.Reset();runtimeAds?.Reset();
            accountRead=walletRead=growthRead=null;
            Account=null;AccountStatus=AccountStateReadStatus.NotLoaded;AccountErrorCode=null;
            Wallet=null;Offline=null;LastPull=null;Pending=null;OfflineLoading=false;OfflineNotice="";ActionNotice="";
            LastOfflineReceipt=null;LastFormationReceipt=null;ActionErrorCode=null;ResetAd();if(!retain)Formation.Reset();
            Growth=null;GrowthReady=false;LastGrowthReceipt=null;GrowthNotice="";growthRequest++;ResetRecruitment();ResetDeparture(retain);
            if(retain){RotateTrainLoadoutCredentials();RotateUltimateCredentials();}
            else{ResetTrainLoadout();ResetUltimate();}
            ResetMonthlyAttendance();ResetRecurring();ResetShop();ResetBulk();LastTicketExchange=null;WalletLoading=false;
            try{if(next!=null)Pending=journal?.Read(next.AccountId);RestoreDepartureResult();}catch{ActionNotice="요청 보관함을 확인하지 못했습니다.";}
            if(retainedAccount!=null){Account=retainedAccount;AccountStatus=retainedAccountStatus;AccountErrorCode=retainedAccountError;}Wallet=retainedWallet;Growth=retainedGrowth;GrowthReady=retainedGrowth!=null;
            if(retainedReceipt!=null)LastFormationReceipt=retainedReceipt;
            Changed?.Invoke();
        }
        bool Current(GameApiAuthSessionSnapshot original,long stamp)=>!disposed&&original!=null&&ReferenceEquals(original,session())&&stamp==generation();
        public Task RefreshSharedAsync()=>Task.WhenAll(RefreshAccountAsync(),RefreshWalletAsync(),RefreshGrowthAsync());
        // Display entry may reuse this owner's validated state. Explicit refresh and
        // all transaction/recovery callers retain their original forced reads.
        public Task EnsureSharedAsync()=>Task.WhenAll(EnsureAccountAsync(),EnsureWalletAsync(),EnsureGrowthAsync());
        public Task EnsureAccountAsync()=>EnsureRead(()=>AccountStatus==AccountStateReadStatus.Ready&&Account!=null,()=>RefreshAccountAsync(LifecycleReason.Entry),ref accountRead,LifecycleData.Account);
        public Task EnsureWalletAsync()=>EnsureRead(()=>!WalletLoading&&Wallet?.Status==CrystalWalletReadStatus.Ready,()=>RefreshWalletAsync(LifecycleReason.Entry),ref walletRead,LifecycleData.Wallet);
        public Task EnsureGrowthAsync()=>EnsureRead(()=>GrowthReady&&Growth!=null,()=>RefreshGrowthAsync(LifecycleReason.Entry),ref growthRead,LifecycleData.Growth);
        Task EnsureRead(Func<bool> ready,Func<Task> refresh,ref Task inFlight,LifecycleData data=LifecycleData.Other)
        {
            SynchronizeSession();if(disposed||!Current(owner,ownerGeneration))return Task.CompletedTask;
            if(ready()){LifecycleDiagnostics?.Record(data,LifecycleEvent.CacheHit,LifecycleReason.Entry,ownerGeneration);return Task.CompletedTask;}
            return refresh();
        }
        Task ShareRead(ref Task pending,Func<Task> read,LifecycleData data=LifecycleData.Other,LifecycleReason reason=LifecycleReason.ExplicitRefresh){
            if(pending!=null&&!pending.IsCompleted){LifecycleDiagnostics?.Record(data,LifecycleEvent.Joined,reason,ownerGeneration);return pending;}
            LifecycleDiagnostics?.Record(data,LifecycleEvent.Requested,reason,ownerGeneration);
            var completion=new TaskCompletionSource<bool>();pending=completion.Task;Complete();return completion.Task;
            async void Complete(){try{await read();completion.TrySetResult(true);}catch(Exception e){completion.TrySetException(e);}}
        }
        public Task RefreshGrowthAsync()=>RefreshGrowthAsync(LifecycleReason.ExplicitRefresh);
        Task RefreshGrowthAsync(LifecycleReason reason){SynchronizeSession();if(growthReadRevision!=growthRequest)growthRead=null;if(growthRead==null||growthRead.IsCompleted)growthReadRevision=growthRequest+1;return ShareRead(ref growthRead,ReadGrowthAsync,LifecycleData.Growth,reason);}
        async Task ReadGrowthAsync()
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;var request=++growthRequest;if(!Current(original,stamp))return;
            GrowthReady=false;GrowthNotice="성장 정보를 확인하고 있습니다.";Changed?.Invoke();
            try{var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/growth/config",null,null),original.AccessToken));
                if(!Current(original,stamp)||request!=growthRequest)return;
                if(response.IsNetworkError||response.StatusCode!=200){GrowthNotice="성장 정보를 불러오지 못했습니다.";}
                else{Growth=OperatorGrowthConfig.Parse(response.BodyJson);GrowthReady=true;GrowthNotice="";}
            }catch{if(!Current(original,stamp)||request!=growthRequest)return;GrowthNotice="성장 정보 형식을 확인하지 못했습니다.";}
            if(Current(original,stamp)&&request==growthRequest)Changed?.Invoke();
        }
        public bool CanGrow(string kind,string target)
        {
            if(Busy||Pending!=null||!GrowthReady||AccountStatus!=AccountStateReadStatus.Ready)return false;
            var cost=Growth?.Cost(kind,target,Account);return cost!=null&&cost.All(p=>Account.Currencies.TryGetValue(p.Key,out var balance)&&balance>=p.Value);
        }
        public Task GrowAsync(string kind,string target)
        {
            if(!CanGrow(kind,target))return Task.CompletedTask;
            var r=new JObject{["economyVersion"]=Growth.Policy.EconomyVersion};
            if(kind=="growth-core")r["targetLevel"]=Account.TrainingCoreSlots.Single(s=>s.Number("slot").ToString()==target).Number("trainedLevel")+1;
            return SubmitAsync(kind,target,0,r.ToString());
        }
        public bool CanReplaceCore(int slot,string operatorId)
        {
            return !Busy&&Pending==null&&GrowthReady&&AccountStatus==AccountStateReadStatus.Ready&&Account!=null&&slot>=0&&slot<5
                &&Account.Operators.Any(o=>o.Text("operatorId")==operatorId)&&!Account.TrainingCoreSlots.Any(s=>s.Text("operatorId")==operatorId);
        }
        public Task ReplaceCoreAsync(int slot,string operatorId)
        {
            if(!CanReplaceCore(slot,operatorId))return Task.CompletedTask;
            return SubmitAsync("growth-replace",slot.ToString(),0,new JObject{["economyVersion"]=Growth.Policy.EconomyVersion,["operatorId"]=operatorId}.ToString());
        }
        long accountReadRevision,walletReadRevision,growthReadRevision;
        public Task RefreshAccountAsync()=>RefreshAccountAsync(LifecycleReason.ExplicitRefresh);
        Task RefreshAccountAsync(LifecycleReason reason){SynchronizeSession();if(accountReadRevision!=accountRequest)accountRead=null;if(accountRead==null||accountRead.IsCompleted)accountReadRevision=accountRequest+1;return ShareRead(ref accountRead,ReadAccountAsync,LifecycleData.Account,reason);}
        async Task ReadAccountAsync()
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;var request=++accountRequest;
            if(!Current(original,stamp))return;
            AccountStatus=AccountStateReadStatus.Loading;AccountErrorCode=null;Changed?.Invoke();
            bool received=false;
            try{
                var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/account/state",null,null),original.AccessToken));
                received=true;
                if(!Current(original,stamp)||request!=accountRequest)return;
                if(!reply.IsNetworkError&&reply.StatusCode==200){
                    var next=AccountStateSnapshot.Parse(reply.BodyJson,original.AccountId);
                    Account=next.PreserveNewerFinancial(Account);AccountStatus=AccountStateReadStatus.Ready;AccountErrorCode=null;
                }else{
                    string error=null;try{error=GameApiJson.String(GameApiJson.ReadObject(reply.BodyJson),"errorCode",true);}catch{}
                    AccountErrorCode=error??(reply.IsNetworkError?"network_error":"http_"+reply.StatusCode);
                    AccountStatus=reply.StatusCode==404?AccountStateReadStatus.ApiUnavailable:
                        reply.StatusCode==426||error=="client_update_required"||error=="content_release_pin_required"||error=="content_release_pin_mismatch"?AccountStateReadStatus.VersionMismatch:
                        reply.StatusCode==401||reply.StatusCode==403?AccountStateReadStatus.AuthRequired:
                        error=="contract_violation"?AccountStateReadStatus.ContractMismatch:AccountStateReadStatus.Unavailable;
                }
            }catch{
                if(!Current(original,stamp)||request!=accountRequest)return;
                AccountStatus=received?AccountStateReadStatus.ContractMismatch:AccountStateReadStatus.Unavailable;AccountErrorCode=received?"invalid_response":"network_error";
            }
            if(Current(original,stamp)&&request==accountRequest)Changed?.Invoke();
        }
        public Task RefreshWalletAsync()=>RefreshWalletAsync(LifecycleReason.ExplicitRefresh);
        Task RefreshWalletAsync(LifecycleReason reason){SynchronizeSession();if(walletReadRevision!=walletRequest)walletRead=null;if(walletRead==null||walletRead.IsCompleted)walletReadRevision=walletRequest+1;return ShareRead(ref walletRead,ReadWalletAsync,LifecycleData.Wallet,reason);}
        async Task ReadWalletAsync()
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;var request=++walletRequest;if(!Current(original,stamp))return;WalletLoading=true;Changed?.Invoke();
            try{
                var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/wallet/crystals",null,null),original.AccessToken));
                if(!Current(original,stamp)||request!=walletRequest)return;
                Wallet=CrystalWalletRead.ParseForAccount(reply.IsNetworkError?0:reply.StatusCode,reply.BodyJson,original.AccountId).PreserveNewerBalances(Wallet);
            }catch{if(Current(original,stamp)&&request==walletRequest)Wallet=CrystalWalletRead.Parse(0,null).PreserveNewerBalances(Wallet);}
            if(Current(original,stamp)&&request==walletRequest){WalletLoading=false;Changed?.Invoke();}
        }
        public async Task RefreshOfflineAsync()
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;var request=++offlineRequest;if(!Current(original,stamp))return;
            OfflineLoading=true;OfflineNotice="보급 정보를 확인하고 있습니다.";Offline=null;Changed?.Invoke();
            var result=await new OfflineRewardHttpClient(wire,original.AccessToken).ReadAsync(OfflineRewardPreviewRequest.Create());
            if(!Current(original,stamp)||request!=offlineRequest)return;
            OfflineLoading=false;Offline=result.Response;
            OfflineNotice=result.Response!=null?(result.Outcome==OfflineRewardPreviewClientOutcome.Empty?"아직 수령할 보급이 없습니다.":""):
                result.ErrorCode=="auth_required"?"로그인 상태를 다시 확인해 주세요.":"보급 정보를 불러오지 못했습니다. 다시 확인해 주세요.";
            Changed?.Invoke();
        }
        public Task ClaimOfflineAsync()=>SubmitAsync("offline","",0);
        // The caller must supply an approved, server-listed banner. No fixture/default banner.
        // Recruitment UI stays gated until its banner query contract is bound.
        public Task PullAsync(string approvedBannerId,int count)=>SubmitAsync("gacha",approvedBannerId,count);
        public Task WriteFormationAsync(string kind,string preset,string payload)=>SubmitAsync(kind,preset,0,payload);
        public Task RetryPendingAsync(){SynchronizeSession();if(DepartureReceiptNeedsSave)return LookupDepartureStartAsync();if(localCompletion!=null){RetryLocalCompletion();return Task.CompletedTask;}return Pending==null?Task.CompletedTask:Pending.Kind=="reward-approval"?RetryAdSkipAsync():Pending.Kind.StartsWith("combat-ad-")?RestorePendingCombatAdAsync():Pending.Kind.StartsWith("ad-")?RecheckAdAsync():Pending.Kind=="recurring-bulk"?SubmitBulkStoredAsync(Pending.Payload):Pending.Kind=="shop-offer"?SubmitShopOfferAsync(Pending.Payload):Pending.Kind.StartsWith("recurring-")?SubmitRecurringAsync(Pending.Kind,Pending.Target,Pending.Payload):Pending.Kind.StartsWith("monthly-")?SubmitMonthlyAttendanceAsync(Pending.Kind,Pending.Target,Pending.Payload):Pending.Kind=="ticket-exchange"?SubmitTicketExchangeAsync(Pending.Payload):Pending.Kind=="ultimate-selection"?SubmitUltimateAsync(Pending.Payload):Pending.Kind.StartsWith("departure-")?SubmitDepartureAsync(Pending.Kind,Pending.Target,Pending.Payload):SubmitAsync(Pending.Kind,Pending.Target,Pending.Count,Pending.Payload);}
        // A verified terminal reply is not an unknown network outcome. Keep the
        // original journal until local cleanup succeeds; never resend it here.
        void RetryLocalCompletion()
        {
            if(Busy||localCompletion==null||!Current(owner,ownerGeneration))return;
            writing=true;
            try{FinishLocalCompletion();}
            finally{writing=false;if(!disposed)Changed?.Invoke();}
        }
        void FinishLocalCompletion()
        {
            try{
                if(journal.Remove(localCompletion)){
                    Pending=null;localCompletion=null;ActionNotice=localCompletionNotice;localCompletionNotice=null;return;
                }
            }catch{ /* The original durable request and verified result remain owned here. */ }
            ActionNotice=localCompletionNotice+" 기기 기록 저장을 다시 시도해 주세요.";
        }
        async Task SubmitAsync(string kind,string target,int count,string payload=null)
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;
            if(Busy||!Current(original,stamp))return;
            if(localCompletion!=null){ActionNotice="처리된 요청의 기기 기록 저장을 먼저 완료해 주세요.";Changed?.Invoke();return;}
            writing=true;ActionNotice="요청을 확인하고 있습니다.";ActionErrorCode=null;LastFormationReceipt=null;LastGrowthReceipt=null;LastTrainLoadoutReceipt=null;Changed?.Invoke();
            bool committed=false,formationComplete=false,formationApplied=false;
            try{
                if(journal==null)throw new InvalidOperationException("Secure storage unavailable.");
                var action=journal.Read(original.AccountId);
                if(action!=null&&(action.Kind!=kind||action.Target!=target||action.Count!=count||action.Payload!=payload))
                {ActionNotice="이전 요청 결과를 먼저 확인해 주세요.";Pending=action;return;}
                if(action==null){
                    if(kind=="offline"&&(Offline==null||Offline.PreviewStatus!=OfflineRewardPreviewStatus.Available))
                    {ActionNotice="수령 가능한 보급을 먼저 확인해 주세요.";return;}
                    if(kind.StartsWith("formation-")&&(AccountStatus!=AccountStateReadStatus.Ready||Account==null||!Account.ActivationSupported)){ActionNotice="최신 편성 정보를 먼저 확인해 주세요.";return;}
                    if(kind.StartsWith("growth-")&&(AccountStatus!=AccountStateReadStatus.Ready||Account==null||!GrowthReady)){ActionNotice="최신 성장 정보를 먼저 확인해 주세요.";return;}
                    if(kind=="train-loadout"&&(!TrainLoadoutReady||TrainLoadout==null||TrainLoadout.OpenRun)){ActionNotice="최신 장착 정보를 먼저 확인해 주세요.";return;}
                    action=new PendingAccountAction(original.AccountId,kind,target,count,Guid.NewGuid().ToString("N"),payload);
                    if(!journal.Save(action))throw new InvalidOperationException("Action was not persisted.");
                }
                if(!Current(original,stamp))return;Pending=action;
                bool terminal=false;string notice;
                if(kind=="train-loadout"){
                    var submitted=TrainLoadoutWrite.Validate(action.Target,action.Payload);submitted.Remove("expectedRevision");
                    trainDraft=TrainLoadoutWrite.ValidateSelection(submitted);trainDraftId=action.Target;trainDraftDirty=true;
                    var result=await TrainLoadoutWrite.SendAsync(wire,original.AccessToken,action);if(!Current(original,stamp))return;
                    LastTrainLoadoutReceipt=result.Receipt;ActionErrorCode=result.Error;committed=result.Receipt!=null;terminal=result.Terminal;if(committed)trainDraftDirty=false;
                    notice=committed?(result.Receipt.Changed?"차량 배치를 저장했습니다.":"이미 같은 차량 배치가 저장되어 있습니다."):result.Terminal?"장착 조건이 변경되었습니다. 선택은 유지하고 최신 상태를 확인합니다.":"장착 결과 확인이 필요합니다. 같은 요청으로 재확인해 주세요.";
                }else if(kind=="offline"){
                    var result=await new OfflineRewardHttpClient(wire,original.AccessToken).ClaimReceiptAsync(OfflineRewardClaimRequest.Create(action.Key));
                    if(!Current(original,stamp))return;
                    committed=result.Confirmed;
                    if(committed)LastOfflineReceipt=result;
                    terminal=committed||result.DefinitiveRejection;
                    notice=committed?"보급 수령을 확인했습니다.":result.DefinitiveRejection?"수령을 완료하지 못했습니다.":"결과 확인이 필요합니다. 같은 요청으로 다시 확인해 주세요.";
                }else if(kind.StartsWith("formation-")){
                    var result=await FormationWrite.SendAsync(wire,original.AccessToken,action);if(!Current(original,stamp))return;
                    LastFormationReceipt=result.Receipt;ActionErrorCode=result.Error;committed=result.Receipt!=null;terminal=result.Terminal;
                    formationComplete=committed&&result.Receipt.HasCommittedState;
                    if(formationComplete){
                        accountRequest++;accountRead=null;
                        AccountStateSnapshot nextAccount=null;
                        if(AccountStatus==AccountStateReadStatus.Ready&&Account!=null&&(kind=="formation-save"?Account.TryApplyFormationSave(result.Receipt,action.Payload,out nextAccount):Account.TryApplyFormationActivation(result.Receipt,action.Payload,out nextAccount))){
                            Account=nextAccount;AccountStatus=AccountStateReadStatus.Ready;AccountErrorCode=null;formationApplied=true;
                        }else if(AccountStatus!=AccountStateReadStatus.AuthRequired){AccountStatus=AccountStateReadStatus.Unavailable;AccountErrorCode="committed_state_conflict";}
                        if(result.Receipt.InvalidatesSelection){Departure=null;ResetUltimate();}
                    }
                    notice=committed?(kind=="formation-save"?"편성을 저장했습니다. 적용하려면 활성화를 눌러 주세요.":"편성을 활성화했습니다."):
                        result.Error=="formation_open_run"?"진행 중인 작전을 마친 후 활성화할 수 있습니다.":result.Terminal?"저장 상태가 변경되었거나 요청을 적용할 수 없습니다. 선택은 보존했습니다.":"결과 확인이 필요합니다. 같은 요청으로 다시 확인해 주세요.";
                }else if(kind.StartsWith("growth-")){
                    var result=await OperatorGrowthWrite.SendAsync(wire,original.AccessToken,action);if(!Current(original,stamp))return;
                    LastGrowthReceipt=result.Receipt;ActionErrorCode=result.Error;committed=result.Receipt!=null;terminal=result.Terminal;
                    notice=committed?"성장 결과를 반영했습니다.":result.Terminal?"성장 조건이 변경되었거나 재료가 부족합니다. 최신 정보를 확인해 주세요.":"성장 결과 확인이 필요합니다. 같은 요청으로 다시 확인해 주세요.";
                }else{
                    var request=GachaPullRequest.Create(action.Target,GachaPullCount.FromValue(action.Count),action.Key);
                    var result=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest(request.Method,request.EndpointPath,request.BodyJson,new[]{new RunApiHttpHeader("Idempotency-Key",action.Key)}),original.AccessToken));
                    if(!Current(original,stamp))return;
                    if(!result.IsNetworkError&&result.StatusCode==200){
                        var response=GameApiJson.ReadObject(result.BodyJson);
                        bool accepted=GameApiJson.Boolean(response,"accepted");var status=GameApiJson.String(response,"resultStatus");
                        if(((accepted&&status=="ACCEPTED")||(!accepted&&status=="DUPLICATE_REPLAY"))&&GameApiJson.String(response,"bannerId")==action.Target&&AccountStateSnapshot.Number(response,"pullCount")==action.Count){
                            var acknowledgedBatchId=RecruitmentJson.Id(response,"batchId");RecoveryBatchId=acknowledgedBatchId;committed=true;
                            try{LastPull=GachaPullResponseJsonAdapter.FromJson(result.BodyJson);}catch{LastPull=null;}
                            await RefreshBatchAsync(acknowledgedBatchId);if(!Current(original,stamp))return;
                            terminal=BatchReady&&SelectedBatch?.Complete==true&&SelectedBatch.Id==acknowledgedBatchId&&SelectedBatch.BannerId==action.Target&&SelectedBatch.Count==action.Count;
                        }else if(!GameApiJson.Boolean(response,"accepted")&&(string)response["resultStatus"]=="REJECTED"&&(string)response["bannerId"]==action.Target&&(string)response["errorCode"]!="auth_required")terminal=true;
                    }else if(!result.IsNetworkError&&(result.StatusCode==400||result.StatusCode==409)){
                        var response=GameApiJson.ReadObject(result.BodyJson);terminal=!GameApiJson.Boolean(response,"accepted");ActionErrorCode=(string)response["errorCode"];
                    }
                    notice=committed?(terminal?"모집 결과를 확인했습니다.":"모집은 처리됐으며 결과 확인이 필요합니다. 같은 기록으로 재확인해 주세요."):terminal?"모집을 완료하지 못했습니다.":"모집 결과 확인이 필요합니다. 같은 요청으로 다시 확인해 주세요.";
                }
                ActionNotice=notice;
                if(terminal&&kind.StartsWith("formation-")){localCompletion=action;localCompletionNotice=notice;FinishLocalCompletion();}
                else if(terminal&&(Pending==null||journal.Remove(action))){Pending=null;if(kind=="gacha")RecoveryBatchId=null;}
                if(committed&&!formationComplete){Wallet=null;Offline=null;walletRequest++;offlineRequest++;accountRequest++;AccountStatus=AccountStateReadStatus.Loading;AccountDataInvalidated?.Invoke();}
            }catch{if(Current(original,stamp))ActionNotice="요청 또는 보관 상태를 확인하지 못했습니다. 다시 확인해 주세요.";}
            finally{writing=false;if(!disposed)Changed?.Invoke();}
            if(Current(original,stamp)&&formationComplete){if(!formationApplied&&AccountStatus!=AccountStateReadStatus.AuthRequired)await RefreshAccountAsync();}
            else if(Current(original,stamp)&&(committed||(kind=="train-loadout"||kind.StartsWith("formation-")||kind.StartsWith("growth-"))&&ActionErrorCode!=null))await Task.WhenAll(RefreshSharedAsync(),RefreshOfflineAsync());
            if(Current(original,stamp)&&(kind=="train-loadout"&&(committed||ActionErrorCode!=null)||kind=="growth-train"&&committed))await RefreshTrainLoadoutAsync();
            if(Current(original,stamp)&&kind=="formation-activate"&&!formationComplete&&(committed||ActionErrorCode!=null)){Departure=null;await RefreshUltimateAsync();}
            if(Current(original,stamp)&&kind=="gacha"&&committed){await RefreshBannersAsync();if(Current(original,stamp))await SelectBannerAsync(target);}
        }
        public void Dispose(){ResetEquipmentInventory();if(disposed)return;ResetOrdinaryTravel();localCompletion=null;localCompletionNotice=null;LifecycleDiagnostics?.Record(LifecycleData.Account,LifecycleEvent.ReferenceReleased,LifecycleReason.OwnerDisposed,ownerGeneration);disposed=true;owner=null;accountRead=walletRead=growthRead=null;Formation.Reset();DisposeRiskRunBinding();runtimeAds?.Dispose();adSkip?.Dispose();adSkipUse?.Dispose();combatAds?.Dispose();storyArchive?.Dispose();ResetAd();ResetRecruitment();ResetTrainLoadout();ResetDeparture();ResetUltimate();ResetMonthlyAttendance();ResetRecurring();ResetShop();ResetBulk();LastTicketExchange=null;WalletLoading=false;Wallet=null;Account=null;Growth=null;Offline=null;LastPull=null;LastGrowthReceipt=null;LastFormationReceipt=null;LastOfflineReceipt=null;Pending=null;Changed=null;AccountDataInvalidated=null;}
        sealed class ConfirmedHttpTransport : IRunApiHttpTransport
        {
            readonly IRunApiHttpTransport inner;
            public ConfirmedHttpTransport(IRunApiHttpTransport inner){this.inner=inner;}
            public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {var r=await inner.SendAsync(request);return r.IsNetworkError||r.StatusCode==200?r:RunApiHttpResponse.NetworkError("Unconfirmed HTTP response.");}
        }
    }
}



