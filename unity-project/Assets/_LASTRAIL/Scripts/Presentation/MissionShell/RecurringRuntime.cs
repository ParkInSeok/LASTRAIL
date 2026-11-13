using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {public sealed partial class AccountScreenRuntime {
 long recurringRequest,recurringEntry;bool recurringEntering;
 public RecurringSnapshot Recurring {get;private set;}
 public bool RecurringReady {get;private set;}
 public bool RecurringLoading {get;private set;}
 public string RecurringError {get;private set;}
 public string RecurringNotice {get;private set;}="정보를 불러올 수 없습니다.";
 JObject lastRecurringGrant;public JObject LastRecurringGrant=>lastRecurringGrant==null?null:(JObject)lastRecurringGrant.DeepClone();
 void ResetRecurring(){recurringRequest++;recurringEntry++;recurringEntering=false;Recurring=null;RecurringReady=false;RecurringLoading=false;RecurringError=null;RecurringNotice="정보를 불러올 수 없습니다.";lastRecurringGrant=null;}
 public async Task RefreshRecurringAsync(){SynchronizeSession();var original=owner;var stamp=ownerGeneration;var request=++recurringRequest;if(!Current(original,stamp))return;RecurringReady=false;RecurringLoading=true;RecurringError=null;Changed?.Invoke();try{
  var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/recurring/state",null,null),original.AccessToken));if(!Current(original,stamp)||request!=recurringRequest)return;
  if(reply.IsNetworkError||reply.StatusCode!=200){RecurringError=reply.IsNetworkError?"network_error":(string)GameApiJson.ReadObject(reply.BodyJson)["errorCode"];RecurringNotice="임무와 패스 정보를 불러오지 못했습니다.";return;}
  Recurring=new RecurringSnapshot(reply.BodyJson,original.AccountId);RecurringReady=true;RecurringNotice=Recurring.SyncPending?"새 진행 기록을 확인하고 있습니다.":"";
 }catch{if(Current(original,stamp)&&request==recurringRequest){RecurringError="invalid_response";RecurringNotice="임무와 패스 정보 형식을 확인하지 못했습니다.";}}finally{if(Current(original,stamp)&&request==recurringRequest){RecurringLoading=false;Changed?.Invoke();}}}
 public async Task EnterRecurringAsync(){SynchronizeSession();if(Busy||recurringEntering)return;var original=owner;var stamp=ownerGeneration;var entry=++recurringEntry;recurringEntering=true;try{
  if(Pending!=null){if(Pending.Kind!="recurring-login")return;await SubmitRecurringAsync(Pending.Kind,Pending.Target,Pending.Payload);if(!Current(original,stamp)||Pending!=null||ActionErrorCode!=null)return;}
  // A recovered login receipt can belong to yesterday. This entry also needs today's source.
  await SubmitRecurringAsync("recurring-login","login",null);
  if(Current(original,stamp)&&Pending==null&&ActionErrorCode==null)await SyncRecurringAsync();
 }finally{if(entry==recurringEntry)recurringEntering=false;}}
 public async Task SyncRecurringAsync(){SynchronizeSession();if(Busy||Pending!=null)return;var original=owner;var stamp=ownerGeneration;if(!Current(original,stamp))return;writing=true;RecurringReady=false;recurringRequest++;RecurringLoading=false;Changed?.Invoke();var success=false;try{
  var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/recurring/sync",null,null),original.AccessToken));if(!Current(original,stamp))return;
  if(reply.IsNetworkError||reply.StatusCode!=200){RecurringError=reply.IsNetworkError?"network_error":(string)GameApiJson.ReadObject(reply.BodyJson)["errorCode"];RecurringNotice="진행 정보를 갱신하지 못했습니다.";return;}
  var r=GameApiJson.ReadObject(reply.BodyJson);RecurringContract.Accept(r);AccountStateSnapshot.Number(r,"addedObjectives");AccountStateSnapshot.Number(r,"lastEventId");success=true;
 }catch{if(Current(original,stamp)){RecurringError="invalid_response";RecurringNotice="진행 갱신 결과를 확인하지 못했습니다.";}}finally{writing=false;if(!disposed)Changed?.Invoke();}if(success&&Current(original,stamp))await RefreshRecurringAsync();}
 public bool CanClaimRecurring(string kind,string target,string payload=null){if(Busy||Pending!=null||!RecurringReady||RecurringLoading||Recurring==null)return false;
  if(kind=="recurring-quest"){if(Recurring.Progress!=null)return Recurring.Progress.SystemState("QUESTS")["objectives"].Any(a=>(string)a["awardId"]==target&&(string)a["status"]=="EARNED");return Recurring.Awards.Any(a=>(string)a["id"]==target&&(string)a["system"]=="QUESTS"&&(string)a["status"]=="EARNED");}
  if(kind=="recurring-mail")return Recurring.Mail.Any(a=>(string)a["award_id"]==target&&(string)a["status"]=="CLAIMABLE");
  if(kind=="recurring-pass"){try{RecurringContract.Endpoint(kind,target,payload);var b=GameApiJson.ReadObject(payload);var p=Recurring.Policies.FirstOrDefault(a=>(string)a["policyId"]==target&&(string)a["system"]=="PASS50");var row=p?["rewardRows"]?[(int)b["level"]-1];var premium=(string)b["track"]=="PREMIUM";return row!=null&&(string)row[premium?"premiumState":"freeState"]=="EARNED"&&(!premium||row["cosmeticSlot"]?.Type==JTokenType.Null||row["cosmeticItemId"]?.Type==JTokenType.String);}catch{return false;}}
  return false;
 }
 public Task ClaimRecurringAsync(string kind,string target,string payload=null){SynchronizeSession();return CanClaimRecurring(kind,target,payload)?SubmitRecurringAsync(kind,target,payload):Task.CompletedTask;}
 async Task SubmitRecurringAsync(string kind,string target,string payload){SynchronizeSession();var original=owner;var stamp=ownerGeneration;if(Busy||!Current(original,stamp))return;writing=true;ActionErrorCode=null;ActionNotice="요청 결과를 확인하고 있습니다.";Changed?.Invoke();var refresh=false;try{
  var endpoint=RecurringContract.Endpoint(kind,target,payload);if(journal==null)throw new InvalidOperationException();var action=journal.Read(original.AccountId);if(action!=null&&(action.Kind!=kind||action.Target!=target||action.Payload!=payload)){Pending=action;ActionNotice="이전 요청 결과를 먼저 확인해 주세요.";return;}
  if(action==null){action=new PendingAccountAction(original.AccountId,kind,target,0,Guid.NewGuid().ToString("N"),payload);if(!journal.Save(action))throw new InvalidOperationException();}Pending=action;recurringRequest++;RecurringReady=false;RecurringLoading=false;
  var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST",endpoint,payload,new[]{new RunApiHttpHeader("Idempotency-Key",action.Key)}),original.AccessToken));if(!Current(original,stamp))return;
  if(!reply.IsNetworkError&&reply.StatusCode==200){var r=GameApiJson.ReadObject(reply.BodyJson);JObject grant=null;if(kind=="recurring-login"){RecurringContract.Accept(r);if(GameApiJson.String(r,"eventSource")!="SERVER_LOGIN"||!GameApiJson.Boolean(r,"refreshRequired"))throw new FormatException();AccountStateSnapshot.Number(r,"dayStart");}else grant=RecurringContract.Grant(r,action);
   if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;if(grant!=null)lastRecurringGrant=grant;ActionNotice=grant==null?"접속 기록을 확인했습니다.":"보상 수령을 확인했습니다.";refresh=true;
  }else if(!reply.IsNetworkError&&(reply.StatusCode==400||reply.StatusCode==403||reply.StatusCode==409)){var r=GameApiJson.ReadObject(reply.BodyJson);if(GameApiJson.Boolean(r,"accepted"))throw new FormatException();ActionErrorCode=GameApiJson.String(r,"errorCode");if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;ActionNotice="수령 조건이 변경되었습니다. 최신 상태를 확인해 주세요.";refresh=true;
  }else{ActionNotice="요청 결과 확인이 필요합니다. 같은 요청으로 재확인해 주세요.";}
 }catch{if(Current(original,stamp))ActionNotice="요청 결과를 확인하지 못했습니다. 같은 요청으로 재확인해 주세요.";}finally{writing=false;if(!disposed)Changed?.Invoke();}if(refresh&&Current(original,stamp))await Task.WhenAll(RefreshSharedAsync(),RefreshRecurringAsync());}
} }

