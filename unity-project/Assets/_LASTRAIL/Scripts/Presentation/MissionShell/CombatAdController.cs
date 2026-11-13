using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 public sealed class CombatAdController:IDisposable {
  readonly IRunApiHttpTransport wire;readonly IAccountActionJournal journal;readonly Func<GameApiAuthSessionSnapshot> session;readonly Func<long> generation;readonly Func<IRewardAdPresentation> provider;
  readonly Func<Task<bool>> confirmNewAd;readonly Func<bool> currentPolicy;
  bool disposed;long lifetime;public bool Busy{get;private set;}public CombatAdIntent Intent{get;private set;}public string Notice{get;private set;}="";public event Action Changed;
  public CombatAdController(IRunApiHttpTransport wire,IAccountActionJournal journal,Func<GameApiAuthSessionSnapshot> session,Func<long> generation,Func<IRewardAdPresentation> provider,Func<Task<bool>> confirmNewAd=null,Func<bool> currentPolicy=null){this.wire=wire;this.journal=journal;this.session=session;this.generation=generation;this.provider=provider;this.confirmNewAd=confirmNewAd;this.currentPolicy=currentPolicy;}
  public bool ProviderAvailable=>provider()?.Available==true;
  public void ReportStorageFailure(){Notice="요청 보관함을 확인하지 못했습니다. 기본 보상과 전투 기록을 확인해 주세요.";Changed?.Invoke();}
  public bool CanPresent(string run){try{return !Busy&&ProviderAvailable&&Intent?.RunId==run&&Intent.Status=="AD_PENDING"&&journal.Read(session()?.AccountId)?.Kind=="combat-ad-intent";}catch{return false;}}
  public bool HasPending(string run){try{var row=journal?.Read(session()?.AccountId);return row!=null&&row.Kind.StartsWith("combat-ad-")&&row.Target==run;}catch{return false;}}
  public Task BeginAsync(string run,string preview)=>Execute("begin",run,preview);
  public Task RefreshAsync(string run,string preview)=>Execute("read",run,preview);
  public Task SpinAsync(string run,string preview)=>Execute("spin",run,preview);
  internal void AdoptApproved(CombatAdIntent value){if(Busy)throw new InvalidOperationException("Reward operation in progress");Intent=value;Notice="";Changed?.Invoke();}
  public void Reset(){lifetime++;Intent=null;Notice="";}
  bool Current(GameApiAuthSessionSnapshot s,long g,long l)=>!disposed&&l==lifetime&&s!=null&&ReferenceEquals(session(),s)&&generation()==g&&s.AccountStatus=="ACTIVE"&&s.AccessTokenExpiresAt>DateTime.UtcNow;
  Task<RunApiHttpResponse> Send(GameApiAuthSessionSnapshot s,string method,string path,string key=null)=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest(method,path,null,key==null?new[]{new RunApiHttpHeader("X-Reward-Contract",RewardOpportunityContract.Version)}:new[]{new RunApiHttpHeader("Idempotency-Key",key),new RunApiHttpHeader("X-Reward-Contract",RewardOpportunityContract.Version),new RunApiHttpHeader("X-Ad-Contract",RewardAdStart.Version)}),s.AccessToken));
  PendingAccountAction Replace(PendingAccountAction old,string kind,string key,CombatAdIntent next){var row=new PendingAccountAction(old.Account,kind,old.Target,0,key,next.Json);if(journal is not IAccountAdActionJournal atomic||!atomic.ReplaceAd(old,row))throw new InvalidOperationException("ad storage transition failed");Intent=next;return row;}
  bool Accept(RunApiHttpResponse response,PendingAccountAction row){
   if(!response.IsNetworkError&&response.StatusCode==200)return true;
   Notice="결과 확인이 필요합니다. 같은 요청을 재확인해 주세요.";
   if(!response.IsNetworkError&&row.Kind=="combat-ad-create"){
    var error=JObject.Parse(response.BodyJson);var code=(string)error["errorCode"];
    if(error["accepted"]?.Type==JTokenType.Boolean&&!(bool)error["accepted"]&&
       (response.StatusCode==503&&code=="ad_reward_policy_unconfigured"||response.StatusCode==409&&(code=="ads_disabled_by_policy"||code=="ad_runtime_policy_changed"||code=="ad_daily_limit_reached"||code=="ad_preview_lifetime_insufficient"||code=="ad_clear_required"||code=="ad_reward_not_eligible"||code=="reward_preview_not_claimable"||code=="reward_preview_expired"))){
     if(!journal.Remove(row))throw new InvalidOperationException("ad storage removal failed");Intent=null;Notice="현재 광고 룰렛을 이용할 수 없습니다. 기본 보상을 확인해 주세요.";
    }
   }return false;
  }
  async Task Execute(string operation,string run,string preview){
   var s=session();var g=generation();var l=lifetime;if(Busy||!Current(s,g,l))return;
   Busy=true;Notice="전투 보상을 확인하고 있습니다.";Changed?.Invoke();
   try{
    if(journal is not IAccountAdActionJournal)throw new InvalidOperationException("secure storage unavailable");
    var row=journal.Read(s.AccountId);bool created=false;
    if(row!=null&&(!row.Kind.StartsWith("combat-ad-")||row.Target!=run)){Notice="이전 요청 결과를 먼저 확인해 주세요.";return;}
    if(row==null){
     if(operation!="begin"){if(Intent?.RunId==run)SetNotice();return;}Intent=null;if(!ProviderAvailable){Notice="현재 광고를 이용할 수 없습니다. 기본 보상을 받을 수 있습니다.";return;}
     if(confirmNewAd==null||!await confirmNewAd()||!Current(s,g,l)){Notice="새 광고를 시작할 수 없습니다. 기본 보상은 유지됩니다.";return;}
     row=new PendingAccountAction(s.AccountId,"combat-ad-create",run,0,Guid.NewGuid().ToString("N"),new JObject{["runId"]=run,["rewardPreviewId"]=preview}.ToString());
     if(!journal.Save(row))throw new InvalidOperationException("ad storage failed");created=true;
    }
    var pinnedJson=JObject.Parse(row.Payload);if((string)pinnedJson["rewardPreviewId"]!=preview)throw new FormatException("preview changed");
    var path="/v1/runs/"+run+"/reward-preview/"+preview+"/ad-intents";
    if(row.Kind=="combat-ad-create"){
     var response=await Send(s,"POST",path,row.Key);if(!Current(s,g,l))return;if(!Accept(response,row))return;
     var next=CombatAdIntent.Parse(response.BodyJson,run,preview);row=Replace(row,"combat-ad-intent",row.Key,next);
    }else{
     var pinned=CombatAdIntent.Parse(row.Payload,run,preview);var response=await Send(s,"GET",path+"/"+pinned.Id);if(!Current(s,g,l))return;if(!Accept(response,row))return;
     var next=CombatAdIntent.Parse(response.BodyJson,run,preview);next.RequireSame(pinned);row=Replace(row,row.Kind,row.Key,next);
    }
    if(Intent.Terminal){SetNotice();return;}
    if(operation=="spin"&&Intent.CanSpin){
     if(row.Kind!="combat-ad-spin")row=Replace(row,"combat-ad-spin",Guid.NewGuid().ToString("N"),Intent);
     var response=await Send(s,"POST",path+"/"+Intent.Id+"/spin",row.Key);if(!Current(s,g,l))return;if(!Accept(response,row))return;
     row=Replace(row,row.Kind,row.Key,Intent.WithResult(response.BodyJson));SetNotice();return;
    }
    // A restored create/intent never automatically repeats the provider presentation.
    if(operation=="begin"&&Intent.Status=="AD_PENDING"&&row.Kind=="combat-ad-intent"&&(created||ProviderAvailable)){
     if(confirmNewAd==null||!await confirmNewAd()||!Current(s,g,l)){Notice="새 광고를 시작할 수 없습니다. 기존 요청은 재확인할 수 있습니다.";return;}
     var ad=provider();if(ad?.Available!=true){Notice="광고 연결을 이용할 수 없습니다. 기본 보상을 받을 수 있습니다.";return;}
     row=Replace(row,"combat-ad-presented",row.Key,Intent);
     if(!await RewardAdStart.AuthorizeAsync(wire,s,"COMBAT",Intent.Json,row.Key)||!Current(s,g,l)||currentPolicy?.Invoke()==false){Notice="광고 시작 결과를 다시 확인해 주세요. 기존 요청을 유지했습니다.";return;}
     await ad.ShowAsync(Intent.UserId,Intent.CustomData,Intent.AdUnit);if(!Current(s,g,l))return;
     var response=await Send(s,"GET",path+"/"+Intent.Id);if(!Current(s,g,l))return;if(!Accept(response,row))return;
     var next=CombatAdIntent.Parse(response.BodyJson,run,preview);next.RequireSame(Intent);Replace(row,row.Kind,row.Key,next);
    }
    SetNotice();
   }catch{if(Current(s,g,l))Notice="요청 기록을 유지했습니다. 결과를 다시 확인해 주세요.";}
   finally{Busy=false;if(!disposed)Changed?.Invoke();}
  }
  void SetNotice(){Notice=Intent.Status=="CLAIMED"?"전투 보상을 수령했습니다.":Intent.Status=="REWARD_CONSUMED"?"이미 수령한 전투 보상입니다.":Intent.Status=="EXPIRED"?"광고 확인 시간이 만료되었습니다. 기본 보상을 확인해 주세요.":Intent.Terminal?"현재 룰렛을 진행할 수 없습니다. 전투 기록을 확인해 주세요.":Intent.CanSpin?"서버 확인이 완료되었습니다. 룰렛을 돌려 주세요.":"광고 확인 중입니다. 잠시 후 재확인해 주세요.";}
  // Acknowledgement releases the durable pending slot, not known server authority.
  // Keep terminal status until the next explicit attempt or session invalidation.
  public void Acknowledge(){if(Busy||Intent?.Terminal!=true)return;try{var row=journal.Read(session()?.AccountId);if(row==null||row.Target!=Intent.RunId||!row.Kind.StartsWith("combat-ad-")||row.Kind=="combat-ad-create")return;var saved=CombatAdIntent.Parse(row.Payload,Intent.RunId,Intent.PreviewId);saved.RequireSame(Intent);if(saved.Status!=Intent.Status)return;if(journal.Remove(row)){SetNotice();Changed?.Invoke();}}catch{Notice="결과 보관 상태를 확인하지 못했습니다.";Changed?.Invoke();}}
  public void Dispose(){disposed=true;Reset();Changed=null;}
 }
}
