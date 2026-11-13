using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
namespace LastRail.Presentation.MissionShell {public sealed partial class AccountScreenRuntime {
 long lifecycleRequest;Task lifecycleRead;string lifecycleReadRun;bool lifecycleVerified;
 public RunLifecycleSnapshot Lifecycle{get;private set;}
 public bool LifecycleLoading{get;private set;}
 public string LifecycleNotice{get;private set;}="";
 void InvalidateRunReads(){runResultsRequest++;runResultsRead=null;runResultsReadRun=null;lifecycleRequest++;lifecycleRead=null;lifecycleReadRun=null;lifecycleVerified=false;}
 void ResetLifecycle(){ResetRunResults();lifecycleRequest++;lifecycleRead=null;lifecycleReadRun=null;lifecycleVerified=false;Lifecycle=null;LifecycleLoading=false;LifecycleNotice="";}
 public Task RefreshRunLifecycleAsync(string runId){
  SynchronizeSession();if(!Current(owner,ownerGeneration)||string.IsNullOrEmpty(runId))return Task.CompletedTask;
  if(lifecycleReadRun!=runId){lifecycleRead=null;lifecycleReadRun=runId;}
  return ShareRead(ref lifecycleRead,()=>ReadRunLifecycleStateAsync(runId));
 }
 async Task ReadRunLifecycleStateAsync(string runId){
  var original=owner;var stamp=ownerGeneration;var request=++lifecycleRequest;
  // Retain confirmed termination on failed verification. It grants no new run.
  var terminal=Lifecycle?.RunId==runId&&Lifecycle.ConfirmedOutcome!=null?Lifecycle:null;
  Lifecycle=terminal;lifecycleVerified=false;LifecycleLoading=true;LifecycleNotice="전투 기록을 확인하고 있습니다.";Changed?.Invoke();
  try{
   RecruitmentJson.Id(new Newtonsoft.Json.Linq.JObject{["id"]=runId},"id");
   var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/runs/"+runId+"/lifecycle",null,null),original.AccessToken));
   if(!Current(original,stamp)||request!=lifecycleRequest)return;
   if(reply.IsNetworkError||reply.StatusCode!=200){LifecycleNotice="전투 기록을 불러오지 못했습니다. 원래 기록은 유지됩니다.";return;}
   var next=new RunLifecycleSnapshot(reply.BodyJson,runId);
   if(terminal!=null&&(next.ConfirmedOutcome!=terminal.ConfirmedOutcome||terminal.RewardStatus=="EXPIRED"&&next.RewardStatus!="EXPIRED"||terminal.RewardStatus=="CLAIMED"&&(next.RewardStatus!="CLAIMED"||next.PreviewId!=terminal.PreviewId||!Newtonsoft.Json.Linq.JToken.DeepEquals(next.TransactionReceipt,terminal.TransactionReceipt)))){
    LifecycleNotice="새 응답이 확정된 종료 기록과 일치하지 않습니다. 원래 기록을 유지합니다.";return;
   }
   Lifecycle=next;lifecycleVerified=true;LifecycleNotice=next.Notice;
  }catch{if(Current(original,stamp)&&request==lifecycleRequest)LifecycleNotice="전투 기록의 형식을 확인하지 못했습니다. 원래 기록은 유지됩니다.";}
  finally{if(Current(original,stamp)&&request==lifecycleRequest){LifecycleLoading=false;Changed?.Invoke();}}
 }
} }