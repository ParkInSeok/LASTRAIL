using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 // Owns approval lifetime and durable handoff; existing reward controllers still own spin/settlement.
 public sealed class AdSkipUseController:IDisposable {
  readonly IRunApiHttpTransport wire;readonly IAccountActionJournal journal;readonly AdSkipAccountService entitlement;
  readonly Func<GameApiAuthSessionSnapshot> session;readonly Func<long> generation;readonly Func<PendingAccountAction,Task> resume;
  long lifetime;bool disposed;public bool Busy{get;private set;}public string Notice{get;private set;}="";public event Action Changed;
  public AdSkipUseController(IRunApiHttpTransport wire,IAccountActionJournal journal,AdSkipAccountService entitlement,Func<GameApiAuthSessionSnapshot> session,Func<long> generation,Func<PendingAccountAction,Task> resume){this.wire=wire;this.journal=journal;this.entitlement=entitlement;this.session=session;this.generation=generation;this.resume=resume;}
  bool Current(GameApiAuthSessionSnapshot s,long g,long l)=>!disposed&&l==lifetime&&ReferenceEquals(s,session())&&g==generation()&&s!=null&&s.AccountStatus=="ACTIVE"&&s.AccessTokenExpiresAt>DateTime.UtcNow;
  public Task UseOfflineAsync()=>Execute("OFFLINE",null,null,false);
  public Task UseCombatAsync(string run,string preview)=>Execute("COMBAT",run,preview,false);
  public Task RetryAsync()=>Execute(null,null,null,true);
  public void Reset(){lifetime++;Busy=false;Notice="";}
  public void Dispose(){disposed=true;Reset();Changed=null;}
  async Task Execute(string kind,string run,string preview,bool retry){
   var s=session();long g=generation(),l=lifetime;if(Busy||!Current(s,g,l))return;Busy=true;Notice="이용권 사용을 확인하고 있습니다.";Changed?.Invoke();
   try{
    if(journal is not IAccountRewardApprovalJournal atomic)throw new InvalidOperationException("Secure approval storage unavailable");
    var row=journal.Read(s.AccountId);
    if(row?.Kind!="reward-approval"){
     if(retry)return;
     // Previously approved/fixed/claimed opportunities remain recoverable after revocation.
     if(row!=null&&(kind=="OFFLINE"&&row.Kind.StartsWith("ad-")&&row.Kind!="ad-create"||kind=="COMBAT"&&row.Kind.StartsWith("combat-ad-")&&row.Kind!="combat-ad-create"&&row.Target==run)){
      var existing=GameApiJson.ReadObject(row.Payload);if((string)existing["status"]!="AD_PENDING"){await resume(row);return;}
     }
     var request=RewardApprovalRequest.Create(s.AccountId,kind,run,preview,row);
     await entitlement.RefreshAsync();if(!Current(s,g,l))return;
     var snapshot=entitlement.Snapshot;
     if(snapshot?.Owned!=true){Notice=snapshot==null?"이용권 확인 필요 · 다시 확인해 주세요.":"이용권을 보유하지 않았습니다. 기본 보상 또는 광고를 선택할 수 있습니다.";return;}
     if((kind=="OFFLINE"?snapshot.OfflineAvailability:snapshot.CombatAvailability)!="AVAILABLE"){Notice="현재 이 기능에 이용권을 사용할 수 없습니다. 기본 보상을 확인해 주세요.";return;}
     var pending=new PendingAccountAction(s.AccountId,"reward-approval",request.Target,0,Guid.NewGuid().ToString("N"),request.Json);
     if(row==null?!journal.Save(pending):!atomic.ReplaceApproval(row,pending))throw new InvalidOperationException("Approval request not persisted");row=pending;Changed?.Invoke();
    }
    var saved=RewardApprovalRequest.Parse(s.AccountId,row.Payload);
    if(!retry&&(saved.Kind!=kind||kind=="COMBAT"&&(saved.Run!=run||saved.Preview!=preview))){Notice="이전 요청 결과를 먼저 확인해 주세요.";return;}
    var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/ad-skip/uses/authorize",saved.Body,new[]{new RunApiHttpHeader("Idempotency-Key",row.Key),new RunApiHttpHeader("X-Reward-Contract",RewardOpportunityContract.Version)}),s.AccessToken));
    if(!Current(s,g,l))return;
    if(response.IsNetworkError||response.StatusCode!=200){
     Notice="사용 결과 확인이 필요합니다. 같은 요청을 다시 확인해 주세요.";
     if(!response.IsNetworkError){var error=GameApiJson.ReadObject(response.BodyJson);string code=(string)error["errorCode"];
      if(error["accepted"]?.Type==JTokenType.Boolean&&!(bool)error["accepted"]&&new[]{"ad_skip_entitlement_required","ad_skip_entitlement_revoked","ad_reward_policy_unconfigured","offline_reward_not_claimable","reward_preview_not_claimable","reward_preview_expired"}.Contains(code)){
       if(saved.Prior==null?!journal.Remove(row):!atomic.ReplaceApproval(row,saved.Prior))throw new InvalidOperationException("Denied approval not released");
       if(code=="ad_skip_entitlement_required"||code=="ad_skip_entitlement_revoked")entitlement.Reset();
       Notice="이용권 사용이 승인되지 않았습니다. 기본 보상을 확인해 주세요.";
      }
     }return;
    }
    PendingAccountAction next;
    if(saved.Kind=="OFFLINE"){var intent=OfflineAdIntent.Parse(response.BodyJson);next=new PendingAccountAction(s.AccountId,"ad-intent",intent.Id,0,row.Key,intent.Json);}
    else{var intent=CombatAdIntent.Parse(response.BodyJson,saved.Run,saved.Preview);next=new PendingAccountAction(s.AccountId,"combat-ad-intent",saved.Run,0,row.Key,intent.Json);}
    saved.ValidateApproved(next);if(!atomic.ReplaceApproval(row,next))throw new InvalidOperationException("Approved handoff not persisted");
    Notice="";await resume(next);
   }catch(Exception){if(Current(s,g,l))Notice="요청 기록을 유지했습니다. 사용 결과를 다시 확인해 주세요.";}
   finally{if(l==lifetime&&!disposed){Busy=false;Changed?.Invoke();}}
  }
 }
}
