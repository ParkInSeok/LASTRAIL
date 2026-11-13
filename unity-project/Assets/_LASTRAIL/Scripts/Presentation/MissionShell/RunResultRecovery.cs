using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 public sealed class RunRewardReview {
  readonly JObject data;public string RunId=>(string)data["runId"];public string PreviewId=>(string)data["rewardPreviewId"];public string Claimability=>(string)data["claimability"];public JArray Lines=>(JArray)data["rewardLines"].DeepClone();
  public RunRewardReview(string json,RunLifecycleSnapshot lifecycle){data=GameApiJson.ReadObject(json);if(RecruitmentJson.Id(data,"runId")!=lifecycle.RunId||RecruitmentJson.Id(data,"rewardPreviewId")!=lifecycle.PreviewId)throw new FormatException();var status=RecurringContract.One(data,"status","ACTIVE","CLAIMED","EXPIRED","INVALIDATED");var ability=RecurringContract.One(data,"claimability","CLAIMABLE","ALREADY_CLAIMED","EXPIRED","INVALIDATED");if(ability=="CLAIMABLE"&&status!="ACTIVE"||ability=="ALREADY_CLAIMED"&&status!="CLAIMED"||ability=="INVALIDATED"&&status!="INVALIDATED")throw new FormatException();GameApiJson.String(data,"balanceVersion");var original=lifecycle.StartResponse;if(original!=null&&(string)original["balanceVersion"]!=(string)data["balanceVersion"])throw new FormatException();RecruitmentJson.Utc(data,"expiresAt");RecruitmentJson.Utc(data,"serverCalculatedAt");ValidateLines(RecruitmentJson.Array(data,"rewardLines"));var claim=GameApiJson.Object(data,"claim");if(GameApiJson.Boolean(claim,"claimable")!=(ability=="CLAIMABLE"))throw new FormatException();var id=RecruitmentJson.NullableString(claim,"rewardClaimId");if(ability=="ALREADY_CLAIMED"){if(id==null)throw new FormatException();RecruitmentJson.Id(claim,"rewardClaimId");RecruitmentJson.Utc(claim,"claimedAt");}else if(id!=null||claim["claimedAt"]?.Type!=JTokenType.Null)throw new FormatException();}
  internal static void ValidateLines(JArray rows){foreach(var value in rows){var row=value as JObject??throw new FormatException();RecruitmentJson.Id(row,"id");GameApiJson.String(row,"kind");GameApiJson.String(row,"source");AccountStateSnapshot.Number(row,"quantity");}}
 }
 public sealed partial class AccountScreenRuntime {
  DurableRunClient runRecovery;long runResultsRequest;JObject verifiedRunReceipt;Task runResultsRead;string runResultsReadRun;
  public JObject VerifiedRunReceipt=>!RunResultsLoading&&verifiedRunReceipt!=null?(JObject)verifiedRunReceipt.DeepClone():null;
  public RunRewardReview RunRewardPreview{get;private set;}
  public string RunResultNotice{get;private set;}="";
  public bool RunResultsLoading{get;private set;}
  public void BindRunRecovery(DurableRunClient client){ResetOrdinaryPreparation();ResetOrdinaryTravel();ResetRunSelection();runRecovery=client;ResetRunResults();}
  void ResetRunResults(){ResetRunReceiptHandoff();runResultsRead=null;runResultsReadRun=null;runResultsRequest++;RunRewardPreview=null;verifiedRunReceipt=null;RunResultNotice="";RunResultsLoading=false;}
  public PendingRunOperation SavedFinish(string runId)=>runRecovery?.Operations.SingleOrDefault(o=>o.Kind==PendingRunKind.Finish&&o.TargetId==runId);
  public PendingRunOperation SavedRewardClaim(string runId)=>runRecovery?.Operations.SingleOrDefault(o=>o.Kind==PendingRunKind.Claim&&o.TargetId==runId);
  public bool CanRecoverFinish(string runId)=>!Busy&&!RunResultsLoading&&runRecovery!=null&&!runRecovery.Busy&&SavedFinish(runId)!=null;
  public bool CanClaimRunReward(string runId)=>CombatBasicAllowed(runId)&&!Busy&&!RunResultsLoading&&runRecovery!=null&&!runRecovery.Busy&&(SavedRewardClaim(runId)!=null&&SavedRewardClaim(runId).AcceptedResponseJson==null||lifecycleVerified&&Lifecycle?.RunId==runId&&Lifecycle.NextAction=="REWARD_REVIEW"&&RunRewardPreview?.RunId==runId&&RunRewardPreview.PreviewId==Lifecycle.PreviewId&&RunRewardPreview.Claimability=="CLAIMABLE"&&SavedRewardClaim(runId)==null);
  public Task EnsureRunResultsAsync(string runId){
   SynchronizeSession();if(!Current(owner,ownerGeneration))return Task.CompletedTask;
   if(runResultsReadRun==runId&&runResultsRead!=null&&!runResultsRead.IsCompleted)return runResultsRead;
   if(receiptStorageRetryRun==runId)return RetryRunReceiptHandoffAsync(runId);
   if(runClaimRequiresOwners&&!RunClaimApplicationComplete&&runReceiptArchive!=null&&lifecycleVerified&&Lifecycle?.RunId==runId&&Lifecycle.RewardStatus=="CLAIMED"&&verifiedRunReceipt!=null)
    return ShareRead(ref runResultsRead,()=>RecoverArchivedClaimOwnersAsync(runId));
   if(string.IsNullOrEmpty(RunResultNotice)&&!RunResultsLoading&&!LifecycleLoading&&lifecycleVerified&&Lifecycle?.RunId==runId&&Lifecycle.ConfirmedOutcome!=null&&
      (Lifecycle.RewardStatus=="CLAIMED"&&verifiedRunReceipt!=null&&(string)verifiedRunReceipt["runId"]==runId||Lifecycle.RewardStatus=="EXPIRED"))return Task.CompletedTask;
   if(lifecycleVerified&&Lifecycle?.RunId==runId){
    if(Lifecycle.RewardStatus=="CLAIMABLE"&&RunRewardPreview?.RunId==runId&&RunRewardPreview.PreviewId==Lifecycle.PreviewId)return Task.CompletedTask;
    if(runResultsReadRun!=runId){runResultsRead=null;runResultsReadRun=runId;}
    return ShareRead(ref runResultsRead,()=>ReadRunResultsCoreAsync(runId,true));
   }
   return ReadRunResultsAsync(runId);
  }
  public Task ReadRunResultsAsync(string runId){
   SynchronizeSession();if(!Current(owner,ownerGeneration)||string.IsNullOrEmpty(runId))return Task.CompletedTask;
   if(runResultsReadRun!=runId){runResultsRead=null;runResultsReadRun=runId;}
   return ShareRead(ref runResultsRead,()=>ReadRunResultsCoreAsync(runId));
  }
  async Task ReadRunResultsCoreAsync(string runId,bool reuseLifecycle=false){SynchronizeSession();var original=owner;var stamp=ownerGeneration;if(!Current(original,stamp))return;ResetRunReceiptHandoff();var request=++runResultsRequest;RunResultsLoading=true;RunRewardPreview=null;verifiedRunReceipt=null;RunResultNotice="";Changed?.Invoke();try{if(!reuseLifecycle&&runRecovery!=null)await runRecovery.RestoreAsync();if(!Current(original,stamp)||request!=runResultsRequest)return;if(!reuseLifecycle)await RefreshRunLifecycleAsync(runId);if(!Current(original,stamp)||request!=runResultsRequest)return;var lifecycle=Lifecycle;if(lifecycle?.RunId!=runId)return;var savedFinish=SavedFinish(runId);if(savedFinish?.AcceptedResponseJson!=null&&lifecycle.PreviewId!=null&&(string)GameApiJson.ReadObject(savedFinish.AcceptedResponseJson)["rewardPreviewId"]!=lifecycle.PreviewId)throw new FormatException();if(lifecycle.RewardStatus=="CLAIMABLE"){
   var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/runs/"+runId+"/reward-preview/"+lifecycle.PreviewId,null,null),original.AccessToken));if(!Current(original,stamp)||request!=runResultsRequest)return;if(reply.IsNetworkError||reply.StatusCode!=200){RunResultNotice="보상 내역을 불러오지 못했습니다. 다시 확인해 주세요.";return;}RunRewardPreview=new RunRewardReview(reply.BodyJson,lifecycle);
  }
  if(lifecycle.TransactionReceipt is JObject receipt){RunRewardReview.ValidateLines((JArray)receipt["rewardPlan"]["rewardLines"]);var saved=SavedRewardClaim(runId);if(saved?.AcceptedResponseJson!=null){var response=saved.ResponseVersion==RunCompleteClaimReceipt.Version?RunCompleteClaimReceipt.ReadJson(saved.AcceptedResponseJson):GameApiJson.ReadObject(saved.AcceptedResponseJson);if((string)response["rewardClaimId"]!=(string)receipt["rewardTransactionId"]||(string)response["rewardPreviewId"]!=lifecycle.PreviewId)throw new FormatException();}verifiedRunReceipt=(JObject)receipt.DeepClone();string localNotice=null;try{bool applied=saved!=null?await ApplyRunClaimWithRecoveryAsync(saved):await ConsumeArchivedRunClaimAsync(runId);if(!Current(original,stamp)||request!=runResultsRequest)return;if(!applied&&(saved==null||saved.ResponseVersion==RunCompleteClaimReceipt.Version))localNotice="보상 지급은 확인됐습니다. 일부 상태 반영을 완료하지 못했습니다. 원래 기록으로 다시 확인해 주세요.";}catch{localNotice="보상 지급은 확인됐지만 기기에 보관된 기록을 반영하지 못했습니다. 원래 기록은 유지됩니다.";}await ArchiveRunReceiptAsync(runId);if(localNotice!=null&&Current(original,stamp)&&request==runResultsRequest&&string.IsNullOrEmpty(RunResultNotice))RunResultNotice=localNotice;}
 }catch{if(Current(original,stamp)&&request==runResultsRequest){RunRewardPreview=null;verifiedRunReceipt=null;RunResultNotice="보상·복구 기록을 확인하지 못했습니다. 원래 기록은 유지됩니다.";}}finally{if(Current(original,stamp)&&request==runResultsRequest){RunResultsLoading=false;Changed?.Invoke();}}}
  public async Task RecoverFinishAsync(string runId){
   SynchronizeSession();var original=owner;var stamp=ownerGeneration;
   if(!CanRecoverFinish(runId)||!Current(original,stamp))return;
   var saved=SavedFinish(runId);string recoveryNotice=null;writing=true;
   RunResultNotice="보관된 결과 요청을 확인하고 있습니다.";Changed?.Invoke();
   try{
    var result=await runRecovery.RetryAsync(saved);if(!Current(original,stamp))return;InvalidateRunReads();
    if(result==null)recoveryNotice=runRecovery.LastErrorCode=="run_finish_request_conflict"?
     "보관된 요청과 서버 기록이 일치하지 않습니다. 원래 요청을 유지한 채 확인이 필요합니다.":
     runRecovery.LastErrorCode=="run_finish_receipt_unavailable"?
     "서버의 원래 결과 영수증을 확인할 수 없습니다. 원래 요청을 보관하고 있습니다.":
     "전송 결과가 아직 확인되지 않았습니다. 원래 요청으로 다시 확인해 주세요.";
   }finally{writing=false;if(!disposed)Changed?.Invoke();}
   if(!Current(original,stamp))return;
   var nextRead=runResultsRequest+1;await ReadRunResultsAsync(runId);
   if(Current(original,stamp)&&runResultsRequest==nextRead&&recoveryNotice!=null){
    RunResultNotice=string.IsNullOrEmpty(RunResultNotice)?recoveryNotice:RunResultNotice+"\n"+recoveryNotice;Changed?.Invoke();
   }
  }
  public async Task ClaimRunRewardAsync(string runId){SynchronizeSession();var original=owner;var stamp=ownerGeneration;if(!CanClaimRunReward(runId)||!Current(original,stamp))return;var saved=SavedRewardClaim(runId);bool completeContract=saved==null||saved.ResponseVersion==RunCompleteClaimReceipt.Version;var preview=RunRewardPreview;var submissionGeneration=runResultsRequest;bool consumed=false;writing=true;RunResultNotice="보상 지급을 확인하고 있습니다.";Changed?.Invoke();try{var result=saved!=null?await runRecovery.RetryAsync(saved):await runRecovery.ClaimCompleteAsync(RunRewardClaimRequest.Create(runId,preview.PreviewId,Guid.NewGuid().ToString("N")));if(!Current(original,stamp)||submissionGeneration!=runResultsRequest)return;if(result!=null)consumed=await TryConsumeConfirmedClaimResultAsync(result);if(!Current(original,stamp))return;if(!consumed){InvalidateRunReads();RunResultNotice=result==null?"지급 결과가 아직 확인되지 않았습니다. 원래 요청으로 다시 확인해 주세요.":"보상 지급 응답을 보관했습니다.";}}finally{writing=false;if(!disposed)Changed?.Invoke();}if(Current(original,stamp)&&!consumed){await ReadRunResultsAsync(runId);if(Current(original,stamp)&&!completeContract)await RefreshSharedAsync();}}
 }
}

