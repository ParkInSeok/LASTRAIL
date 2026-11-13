using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 public sealed partial class AccountScreenRuntime {
  const string AdPath="/v1/offline-rewards/ad-intents";
  bool adWriting;
  IRewardAdPresentation adPresentation;
  public OfflineAdIntent AdIntent {get;private set;}
  public string AdNotice {get;private set;}="";
  public bool AdProviderAvailable=>adPresentation?.Available==true;
  public bool HasPendingAd=>Pending?.Kind.StartsWith("ad-")==true;
  public bool CanSpinAd=>!Busy&&HasPendingAd&&AdIntent?.CanSpin==true;
  public void ConfigureRewardAds(IRewardAdPresentation provider){adPresentation=provider;Changed?.Invoke();}
  string refreshedAdReceipt;
  void ResetAd(){AdIntent=null;AdNotice="";refreshedAdReceipt=null;}
  bool AdCurrent(GameApiAuthSessionSnapshot original,long stamp)=>Current(original,stamp)&&original.AccountStatus=="ACTIVE"&&original.AccessTokenExpiresAt>DateTime.UtcNow;
  public Task BeginAdAsync()=>RunAdAsync("begin");
  public Task RecheckAdAsync()=>RunAdAsync("check");
  public Task SpinAdAsync()=>RunAdAsync("spin");
  public Task PresentAdAsync()=>RunAdAsync("present");
  async Task<RunApiHttpResponse> AdSend(GameApiAuthSessionSnapshot original,string method,string path,string key=null)=>await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest(method,path,null,key==null?new[]{new RunApiHttpHeader("X-Reward-Contract",RewardOpportunityContract.Version)}:new[]{new RunApiHttpHeader("Idempotency-Key",key),new RunApiHttpHeader("X-Reward-Contract",RewardOpportunityContract.Version),new RunApiHttpHeader("X-Ad-Contract",RewardAdStart.Version)}),original.AccessToken));
  void ReplaceAd(PendingAccountAction old,string kind,string key,OfflineAdIntent intent){
   var next=new PendingAccountAction(old.Account,kind,intent.Id,0,key,intent.Json);
   if(journal is not IAccountAdActionJournal atomic||!atomic.ReplaceAd(old,next))throw new InvalidOperationException("Action transition not persisted");Pending=next;
  }
  async Task RunAdAsync(string operation,bool approvedContinuation=false){
   SynchronizeSession();var original=owner;var stamp=ownerGeneration;if((approvedContinuation?RewardExecutionBusy:Busy)||!AdCurrent(original,stamp))return;
   adWriting=true;AdNotice="보급 상태를 확인하고 있습니다.";Changed?.Invoke();
   try{
    if(journal is not IAccountAdActionJournal)throw new InvalidOperationException("Secure atomic storage unavailable");
    Pending=journal.Read(original.AccountId);
    if(Pending!=null&&!HasPendingAd){AdNotice="이전 요청 결과를 먼저 확인해 주세요.";return;}
    bool newlyCreated=false;
    if(Pending==null){
     if(operation!="begin")return;
     if(!await ConfirmNewAdPolicyAsync()||!AdCurrent(original,stamp)){AdNotice="새 광고를 시작할 수 없습니다. 기본 보급은 유지됩니다.";return;}
     if(!AdProviderAvailable){AdNotice="현재 광고 보급을 이용할 수 없습니다. 기본 보급을 수령할 수 있습니다.";return;}
     var create=new PendingAccountAction(original.AccountId,"ad-create","",0,Guid.NewGuid().ToString("N"));
     if(!journal.Save(create))throw new InvalidOperationException();ResetAd();Pending=create;newlyCreated=true;
    }
    if(Pending.Kind=="ad-create"){
     var response=await AdSend(original,"POST",AdPath,Pending.Key);if(!AdCurrent(original,stamp))return;
     if(!AcceptAdResponse(response,Pending))return;
     var intent=OfflineAdIntent.Parse(response.BodyJson);ReplaceAd(Pending,"ad-intent",Pending.Key,intent);AdIntent=intent;
    }else{
     var pinned=OfflineAdIntent.Parse(Pending.Payload);
     // Spin replay uses the already-persisted key, including after an uncertain grant response.
     if(Pending.Kind=="ad-spin"){
      var lookup=await AdSend(original,"GET",AdPath+"/"+Pending.Target);if(!AdCurrent(original,stamp))return;
      if(!AcceptAdResponse(lookup,Pending))return;var restored=OfflineAdIntent.Parse(lookup.BodyJson);restored.RequireSame(pinned);ReplaceAd(Pending,"ad-spin",Pending.Key,restored);AdIntent=restored;
      if(restored.Status!="AD_VERIFIED"){AdNotice=restored.Status=="CLAIMED"?"기존 보급 수령 결과를 복구했습니다.":restored.Status=="EXPIRED"?"광고 확인 시간이 만료되었습니다.":restored.Status=="SUPPLY_CONSUMED"?"이미 수령한 보급입니다.":"광고 확인 중입니다. 잠시 후 재확인해 주세요.";if(restored.Status=="CLAIMED")InvalidateAdBalances();return;}
      var response=await AdSend(original,"POST",AdPath+"/"+Pending.Target+"/spin",Pending.Key);if(!AdCurrent(original,stamp))return;
      if(!AcceptAdResponse(response,Pending))return;
      AdIntent=restored.WithResult(response.BodyJson);ReplaceAd(Pending,"ad-spin",Pending.Key,AdIntent);AdNotice="보급 수령을 확인했습니다.";InvalidateAdBalances();return;
     }
     var read=await AdSend(original,"GET",AdPath+"/"+Pending.Target);if(!AdCurrent(original,stamp))return;
     if(!AcceptAdResponse(read,Pending))return;var latest=OfflineAdIntent.Parse(read.BodyJson);latest.RequireSame(pinned);
     ReplaceAd(Pending,Pending.Kind,Pending.Key,latest);AdIntent=latest;
    }
    if(AdIntent.Status=="CLAIMED"){AdNotice="기존 보급 수령 결과를 복구했습니다.";InvalidateAdBalances();return;}
    if(AdIntent.Status=="EXPIRED"||AdIntent.Status=="SUPPLY_CONSUMED"){AdNotice=AdIntent.Status=="EXPIRED"?"광고 확인 시간이 만료되었습니다.":"이미 수령한 보급입니다.";return;}
    if(operation=="spin"){
     if(!AdIntent.CanSpin){AdNotice="광고 확인 중입니다. 잠시 후 재확인해 주세요.";return;}
     ReplaceAd(Pending,"ad-spin",Guid.NewGuid().ToString("N"),AdIntent);
     var response=await AdSend(original,"POST",AdPath+"/"+Pending.Target+"/spin",Pending.Key);if(!AdCurrent(original,stamp))return;
     if(!AcceptAdResponse(response,Pending))return;var result=AdIntent.WithResult(response.BodyJson);ReplaceAd(Pending,"ad-spin",Pending.Key,result);AdIntent=result;AdNotice="보급 수령을 확인했습니다.";InvalidateAdBalances();return;
    }
    if((newlyCreated||operation=="present")&&Pending.Kind=="ad-intent"&&AdIntent.Status=="AD_PENDING"){
     if(!await ConfirmNewAdPolicyAsync()||!AdCurrent(original,stamp)){AdNotice="새 광고를 시작할 수 없습니다. 기존 요청은 재확인할 수 있습니다.";return;}
     if(!AdProviderAvailable){AdNotice="광고 연결을 이용할 수 없습니다. 기존 요청을 재확인할 수 있습니다.";return;}
     // Persist attempt before opening provider UI. Recovery never automatically presents a second ad.
     ReplaceAd(Pending,"ad-presented",Pending.Key,AdIntent);
     if(!await RewardAdStart.AuthorizeAsync(wire,original,"OFFLINE",AdIntent.Json,Pending.Key)||!AdCurrent(original,stamp)||RuntimeAds.Status!=AdRuntimePolicyStatus.Enabled){AdNotice="광고 시작 결과를 다시 확인해 주세요. 기존 요청을 유지했습니다.";return;}
     await adPresentation.ShowAsync(AdIntent.UserId,AdIntent.CustomData,AdIntent.AdUnit);if(!AdCurrent(original,stamp))return;
     var read=await AdSend(original,"GET",AdPath+"/"+Pending.Target);if(!AdCurrent(original,stamp))return;
     if(!AcceptAdResponse(read,Pending))return;var latest=OfflineAdIntent.Parse(read.BodyJson);latest.RequireSame(AdIntent);ReplaceAd(Pending,Pending.Kind,Pending.Key,latest);AdIntent=latest;
    }
    AdNotice=AdIntent.Status=="CLAIMED"?"기존 보급 수령 결과를 복구했습니다.":AdIntent.CanSpin?"서버 확인이 완료되었습니다. 룰렛을 돌려 주세요.":"광고 확인 중입니다. 잠시 후 재확인해 주세요.";
   }catch{if(Current(original,stamp))AdNotice="결과 확인이 필요합니다. 같은 요청을 재확인해 주세요.";}
   finally{
    if(AdCurrent(original,stamp)&&AdIntent?.Status=="CLAIMED"&&refreshedAdReceipt!=(string)AdIntent.Result["rewardTransactionId"]){
     try{await Task.WhenAll(RefreshSharedAsync(),RefreshOfflineAsync());if(AdCurrent(original,stamp)&&AccountStatus==AccountStateReadStatus.Ready&&Wallet?.Status==CrystalWalletReadStatus.Ready&&Offline!=null)refreshedAdReceipt=(string)AdIntent.Result["rewardTransactionId"];}catch{if(Current(original,stamp))AdNotice="보급 수령은 확인됐습니다. 보유 재화를 다시 확인해 주세요.";}
    }
    adWriting=false;if(!disposed)Changed?.Invoke();
   }
  }
  bool AcceptAdResponse(RunApiHttpResponse response,PendingAccountAction action){
   if(!response.IsNetworkError&&response.StatusCode==200)return true;
   AdNotice="결과 확인이 필요합니다. 같은 요청을 재확인해 주세요.";
   if(!response.IsNetworkError){var error=JObject.Parse(response.BodyJson);if(error["accepted"]?.Type==JTokenType.Boolean&&!(bool)error["accepted"]){
    var code=(string)error["errorCode"];
    if(action.Kind=="ad-create"&&(response.StatusCode==503&&code=="ad_reward_policy_unconfigured"||response.StatusCode==409&&(code=="ad_daily_limit_reached"||code=="offline_reward_not_claimable"||code=="ads_disabled_by_policy"||code=="ad_runtime_policy_changed"))){
     if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;AdIntent=null;AdNotice="현재 광고 보급을 이용할 수 없습니다. 기본 보급을 수령할 수 있습니다.";
    }else if(response.StatusCode==409&&code=="ad_verification_pending")AdNotice="광고 확인 중입니다. 잠시 후 재확인해 주세요.";
   }}return false;
  }
  void InvalidateAdBalances(){if(refreshedAdReceipt==(string)AdIntent?.Result?["rewardTransactionId"])return;Offline=null;Wallet=null;offlineRequest++;walletRequest++;accountRequest++;AccountStatus=AccountStateReadStatus.Loading;AccountDataInvalidated?.Invoke();}
  public void AcknowledgeAdResult(){SynchronizeSession();if(Busy||!HasPendingAd||Pending.Kind=="ad-create"||AdIntent==null||Pending.Target!=AdIntent.Id||!(AdIntent.Status=="CLAIMED"||AdIntent.Status=="EXPIRED"||AdIntent.Status=="SUPPLY_CONSUMED"))return;try{var saved=OfflineAdIntent.Parse(Pending.Payload);AdIntent.RequireSame(saved);if(saved.Status!=AdIntent.Status||!JToken.DeepEquals(saved.Result,AdIntent.Result))return;if(journal.Remove(Pending)){Pending=null;ResetAd();Changed?.Invoke();}}catch{AdNotice="결과 보관 상태를 확인하지 못했습니다.";Changed?.Invoke();}}
 }
}

