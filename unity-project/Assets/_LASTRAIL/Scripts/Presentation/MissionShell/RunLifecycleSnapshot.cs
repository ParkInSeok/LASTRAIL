using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 public sealed class OrdinaryInitializationSnapshot {
  public string Status {get;} public long? Revision {get;} public string OriginHash {get;}
  public bool RuntimeExecutionVerified=>false;
  public bool MatchesOriginalStart {get;}
  OrdinaryInitializationSnapshot(JObject value,JObject start,string run){
   if(value==null||value.Count!=5||GameApiJson.String(value,"version")!="combat.ordinary-initialization-state.v1.20261025"||GameApiJson.Boolean(value,"runtimeExecutionVerified"))throw new FormatException("Invalid ordinary initialization metadata.");
   Status=RecurringContract.One(value,"status","PREPARED","RUNNING","UNAVAILABLE","NOT_APPLICABLE");
   if(Status=="PREPARED"||Status=="RUNNING"){
    Revision=AccountStateSnapshot.Number(value,"revision");OriginHash=GameApiJson.String(value,"originHash");
    if(Revision!=(Status=="PREPARED"?1:3)||!Regex.IsMatch(OriginHash,"\\A[a-f0-9]{64}\\z"))throw new FormatException("Invalid initialization revision or hash.");
    // Server phase projection can survive a failed START replay; preserve that review-only response.
    if(start!=null){
     if(start["ordinaryStatSource"] is not JObject source||source["originHash"]?.Type!=JTokenType.String||(string)source["originHash"]!=OriginHash||source["origin"] is not JObject origin||origin["runId"]?.Type!=JTokenType.String||(string)origin["runId"]!=run)throw new FormatException("Initialization does not match the original START.");
     MatchesOriginalStart=true;
    }
   }else{
    if(value["revision"]?.Type!=JTokenType.Null||value["originHash"]?.Type!=JTokenType.Null)throw new FormatException("Unavailable initialization must not supply a revision.");
    if(Status=="NOT_APPLICABLE"&&(start?.Property("ordinaryStatSource")!=null||start?.Property("ordinaryKitSource")!=null))throw new FormatException("Ordinary START cannot be historical initialization.");
   }
  }
  internal static OrdinaryInitializationSnapshot Read(JObject envelope,JObject start,string run)=>envelope.Property("ordinaryInitialization")==null?null:new OrdinaryInitializationSnapshot(envelope["ordinaryInitialization"] as JObject,start,run);
 }
 public sealed class CombatRewardReference {
  readonly JObject data;public string Verification=>"OPAQUE_SERVER_PIN";public JObject Json=>(JObject)data.DeepClone();
  public CombatRewardReference(JObject value,string balance,string operation,string risk){data=(JObject)(value??throw new FormatException()).DeepClone();if(data.Count!=8||GameApiJson.String(data,"version")!="run-reward-ref-v1"||GameApiJson.String(data,"balanceVersion")!=balance||GameApiJson.String(data,"operationId")!=operation||GameApiJson.String(data,"risk")!=risk||!DepartureContract.Risks.Contains(risk)||GameApiJson.String(data,"scope")!="BASE_RUN_REWARDS"||GameApiJson.String(data,"hashAlgorithm")!="SHA-256"||GameApiJson.String(data,"canonicalization")!="canonical-json-v1"||!Regex.IsMatch(GameApiJson.String(data,"configSha256"),"\\A[a-f0-9]{64}\\z"))throw new FormatException();}
  public static CombatRewardReference Optional(JObject envelope,string balance){if(envelope.Property("rewardReference")==null)return null;return new CombatRewardReference(GameApiJson.Object(envelope,"rewardReference"),balance,GameApiJson.String(envelope,"operationId"),GameApiJson.String(envelope,"risk"));}
 }
 public sealed class RunLifecycleSnapshot {
  public RunStartFormationSnapshot StartFormation {get;}
  public OrdinaryInitializationSnapshot OrdinaryInitialization {get;}
  LastRail.Battle.OrdinaryFiveStartInput ordinaryInput;
  public LastRail.Battle.OrdinaryFiveStartInput ReadOrdinaryInput(string expectedAccount){
   if(NextAction!="RECOVER_COMBAT_CONSUMER"||RunStatus!="ACTIVE")throw new InvalidOperationException("This lifecycle cannot supply an active combat START.");
   if(ordinaryInput!=null){if(ordinaryInput.AccountId!=expectedAccount)throw new ArgumentException("Original START account differs.");return ordinaryInput;}
   return ordinaryInput=LastRail.Battle.OrdinaryFiveStartInput.Read(data["startResponse"] as JObject,expectedAccount,RunId);
  }
  // Display only an existing recorded outcome whose owning run agrees. Unknown
  // pairs remain readable for recovery; they do not authorize a new terminal reason.
  public string ConfirmedOutcome {
   get {var outcome=(string)data["finish"]["outcome"];
    return RunStatus=="COMPLETED"&&outcome=="VICTORY"||RunStatus=="FAILED"&&outcome=="FAILURE"||RunStatus=="ABANDONED"&&outcome=="ABANDONED"?outcome:null;
   }
  }
  public string ResultTitle=>ConfirmedOutcome=="VICTORY"?"작전 성공":ConfirmedOutcome=="FAILURE"?"작전 실패":ConfirmedOutcome=="ABANDONED"?"작전 포기":"결과 확인 필요";
  // Lifecycle embeds the complete accepted START; ordinary API readers keep their 256 KiB limit.
  public const int MaximumLifecycleBytes=8*1024*1024;
  static JObject ReadLifecycle(string json){
   if(string.IsNullOrWhiteSpace(json)||Encoding.UTF8.GetByteCount(json)>MaximumLifecycleBytes)throw new FormatException("Invalid lifecycle JSON size.");
   // JObject drops comments between object properties, so inspect reader tokens first.
   using(var scan=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None,MaxDepth=32})while(scan.Read())if(scan.TokenType==JsonToken.Comment)throw new FormatException("Invalid lifecycle JSON comment.");
   using(var reader=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None,MaxDepth=32}){
    var root=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,CommentHandling=CommentHandling.Load});
    if(reader.Read()||root.Descendants().Any(t=>t.Type==JTokenType.Comment))throw new FormatException("Invalid lifecycle JSON content.");return root;
   }
  }
  readonly JObject data;public string RunId=>(string)data["runId"];public string RunStatus=>(string)data["runStatus"];public string NextAction=>(string)data["nextAction"];public string Disposition=>(string)data["disposition"];public string RewardStatus=>(string)data["reward"]["status"];public string PreviewId=>(string)data["reward"]["rewardPreviewId"];public JObject StartResponse=>data["startResponse"] is JObject o?(JObject)o.DeepClone():null;public JObject TransactionReceipt=>data["reward"]["receipt"] is JObject o?(JObject)o.DeepClone():null;public CombatRewardReference RewardReference{get;}public bool NewAttemptAuthorized=>false;public bool UiReturnAllowed=>true;
  static string NullableId(JObject o,string key){var value=RecruitmentJson.NullableString(o,key);if(value!=null)RecruitmentJson.Id(o,key);return value;}
  static long? NullableTime(JObject o,string key){if(o[key]?.Type==JTokenType.Null)return null;return AccountStateSnapshot.Number(o,key);}
  internal RunLifecycleSnapshot WithConfirmedClaim(LastRail.Run.RunCompleteClaimReceipt claim){
   var response=claim.Capture();
   if(ConfirmedOutcome==null||StartResponse==null||RunId!=(string)response["runId"]||PreviewId!=(string)response["rewardPreviewId"]||
      (string)data["startResponse"]["balanceVersion"]!=(string)response["balanceVersion"]||RewardStatus!="CLAIMABLE"&&RewardStatus!="CLAIMED")return null;
   var receipt=(JObject)response["transactionReceipt"].DeepClone();
   receipt["serverClaimedAt"]=DateTimeOffset.Parse((string)response["serverClaimedAt"]).ToUnixTimeMilliseconds();
   if(RewardStatus=="CLAIMED"&&!JToken.DeepEquals(data["reward"]["receipt"],receipt))return null;
   var next=(JObject)data.DeepClone();next["reward"]["status"]="CLAIMED";next["reward"]["claimId"]=response["rewardClaimId"];
   next["reward"]["receipt"]=receipt;next["nextAction"]=response["nextAction"];next["disposition"]="REWARDED";
   return new RunLifecycleSnapshot(next.ToString(),RunId);
  }
  public RunLifecycleSnapshot(string json,string expectedRun){data=ReadLifecycle(json);RecurringContract.Accept(data);if(GameApiJson.String(data,"version")!="run-lifecycle-v1"||RecruitmentJson.Id(data,"runId")!=expectedRun||GameApiJson.Boolean(data,"grantsOnRead")||GameApiJson.Boolean(data,"newAttemptAuthorized")||!GameApiJson.Boolean(data,"uiReturnAllowed"))throw new FormatException();var now=AccountStateSnapshot.Number(data,"serverTime");GameApiJson.String(data,"runStatus");var start=data["startResponse"];if(start is JObject original){if(!GameApiJson.Boolean(original,"accepted")||GameApiJson.String(original,"startStatus")!="STARTED"||GameApiJson.String(original,"runId")!=RunId)throw new FormatException();RewardReference=CombatRewardReference.Optional(original,GameApiJson.String(original,"balanceVersion"));}else if(start?.Type!=JTokenType.Null)throw new FormatException();
   StartFormation=RunStartFormationSnapshot.Read(start as JObject);
   OrdinaryInitialization=OrdinaryInitializationSnapshot.Read(data,start as JObject,RunId);
   var finish=GameApiJson.Object(data,"finish");var reward=GameApiJson.Object(data,"reward");var fs=RecurringContract.One(finish,"status","RECORDED","NOT_RECORDED");var outcome=RecruitmentJson.NullableString(finish,"outcome");var submitted=NullableTime(finish,"submittedAt");if((fs=="NOT_RECORDED")!=(outcome==null)||(outcome==null)!=(submitted==null))throw new FormatException();var preview=NullableId(reward,"rewardPreviewId");if(NullableId(finish,"rewardPreviewId")!=preview)throw new FormatException();var expires=NullableTime(reward,"expiresAt");var claim=NullableId(reward,"claimId");var rs=RecurringContract.One(reward,"status","NOT_AVAILABLE","CLAIMABLE","CLAIMED","EXPIRED","UNAVAILABLE");if(rs=="NOT_AVAILABLE"){if(preview!=null||expires!=null||claim!=null||reward["receipt"]?.Type!=JTokenType.Null)throw new FormatException();}else if(preview==null)throw new FormatException();
   if(rs=="CLAIMED"){var receipt=GameApiJson.Object(reward,"receipt");if(claim==null||RecruitmentJson.Id(receipt,"rewardTransactionId")!=claim||RecruitmentJson.Id(receipt,"runId")!=RunId)throw new FormatException();AccountStateSnapshot.Number(receipt,"serverClaimedAt");RecruitmentJson.Array(GameApiJson.Object(receipt,"rewardPlan"),"rewardLines");}else if(reward["receipt"]?.Type!=JTokenType.Null)throw new FormatException();if(rs=="CLAIMABLE"&&(expires==null||expires<=now||claim!=null))throw new FormatException();
   bool terminal=new[]{"COMPLETED","FAILED","ABANDONED"}.Contains(RunStatus)&&outcome!=null;var action=start.Type==JTokenType.Null||RunStatus=="QUARANTINED"?"REVIEW_REQUIRED":terminal&&rs=="CLAIMED"?"RETURN_TO_LOBBY":outcome==null&&preview==null&&RunStatus=="ACTIVE"?"RECOVER_COMBAT_CONSUMER":outcome==null&&preview==null&&RunStatus=="FINISH_PENDING"?"RESUME_FINISH":terminal&&rs=="CLAIMABLE"?"REWARD_REVIEW":"REVIEW_REQUIRED";var disposition=rs=="CLAIMED"?"REWARDED":rs=="EXPIRED"?"EXPIRED_UNCLAIMED":action=="REVIEW_REQUIRED"?"REVIEW_REQUIRED":"PENDING";if(GameApiJson.String(data,"nextAction")!=action||GameApiJson.String(data,"disposition")!=disposition)throw new FormatException();
  }
  public string Notice=>RewardStatus=="EXPIRED"?"보상 수령 기한이 지났습니다.\n로비로 돌아갈 수 있습니다.":NextAction=="REWARD_REVIEW"?"전투 결과가 확인되었습니다.\n보상을 확인하고 수령해 주세요.":NextAction=="RETURN_TO_LOBBY"?"보상 지급 기록을 확인했습니다.":NextAction=="RECOVER_COMBAT_CONSUMER"?(OrdinaryInitialization?.Status=="PREPARED"?"진행 중인 전투의 초기화 준비 기록이 있습니다.\n실행 준비 확인이 필요합니다.":OrdinaryInitialization?.Status=="RUNNING"?"전투 초기화 기록을 확인했습니다.\n실행 상태와 저장 기록 확인이 필요합니다.":"진행 중인 전투 기록이 있습니다.\n전투 복구 준비가 필요합니다."):NextAction=="RESUME_FINISH"?"전투 결과 전송 확인이 필요합니다.\n보관된 원래 요청으로 확인해 주세요.":"전투 기록을 추가로 확인해야 합니다.\n로비로 돌아가도 원래 기록은 유지됩니다.";
 }
}
