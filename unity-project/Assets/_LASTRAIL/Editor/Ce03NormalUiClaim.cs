#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
public sealed partial class Ce03NormalUiRecorder {
 string ValidateClaimSources(JObject sources,Ce03OriginalStart original,JObject completion,JObject finish){
  string raw=File.ReadAllText(Ce03NormalUiCapture.Arg("-ce03OriginalSourceBundle"));var prior=Ce03NormalUiCapture.Read(Ce03NormalUiCapture.Arg("-ce03OriginalSourceBundle"));
  var response=Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>((string)original.Copy()["responseRaw"],new Newtonsoft.Json.JsonSerializerSettings{DateParseHandling=Newtonsoft.Json.DateParseHandling.None});
  return VerifyClaimSourceBinding(sources,prior,raw,response,completion,finish,Ce03NormalUiCapture.Read(SessionPath("claim-source-review.json")),File.ReadAllText(Path.Combine(output,"finish-ack.json")),original.RunId,bundleHash);
 }
 public static string VerifyClaimSourceBinding(JObject sources,JObject prior,string raw,JObject response,JObject completion,JObject finish,JObject review,string finishRaw,string runId,string claimHash){
  string priorHash=Ce03CommandContract.Sha(raw);
  if(Canon(prior)!=raw||(string)completion["algorithmSourceSha256"]!=priorHash||(string)finish["sourceBundleSha256"]!=priorHash)throw new InvalidOperationException("Original execution source mismatch");
  if((string)response["combat"]["profile"]["baseProfile"]["executionEvidence"]["algorithmSourceSha256"]!=priorHash)throw new InvalidOperationException("Accepted execution source mismatch");
  if(Ce03CommandContract.Sha(Canon(sources))!=claimHash)throw new InvalidOperationException("Claim source hash mismatch");
  var oldFiles=((JArray)prior["files"]).ToDictionary(f=>(string)f["path"]);var newFiles=((JArray)sources["files"]).ToDictionary(f=>(string)f["path"]);
  if(!oldFiles.Keys.OrderBy(x=>x).SequenceEqual(newFiles.Keys.OrderBy(x=>x)))throw new InvalidOperationException("Claim source file set changed");
  string[] allowed={"Assets/_LASTRAIL/Editor/Ce03NormalUiClaim.cs","Assets/_LASTRAIL/Scripts/Presentation/MissionShell/RunLifecycleSnapshot.cs"};
  foreach(var f in oldFiles){if(Ce03CommandContract.Sha((string)f.Value["content"])!=(string)f.Value["sha256"])throw new InvalidOperationException("Original source content mismatch");if(!allowed.Contains(f.Key)&&!JToken.DeepEquals(f.Value,newFiles[f.Key]))throw new InvalidOperationException("Change outside reviewed claim scope");}
  if(!review.Properties().Select(p=>p.Name).OrderBy(x=>x).SequenceEqual(new[]{"version","runId","originalSourceSha256","claimSourceSha256","finishAckSha256"}.OrderBy(x=>x)))throw new FormatException("Unexpected claim review fields");
  if((string)review["version"]!="ce03-claim-source-review-v1"||(string)review["runId"]!=runId||(string)review["originalSourceSha256"]!=priorHash||(string)review["claimSourceSha256"]!=claimHash||(string)review["finishAckSha256"]!=Ce03CommandContract.Sha(finishRaw))throw new InvalidOperationException("Reviewed claim source binding missing");
  return priorHash;
 }
 async Task Claim(Ce03LoopbackTransport wire,GameApiAuthSessionSnapshot session,JObject sources){
  // Trusted Node must have accepted the terminal artifacts before this stage is launched.
  var original=new Ce03OriginalStart(File.ReadAllText(SessionPath("original-start.json")),session.AccountId,new Uri(((string)connection["origin"]).TrimEnd('/')+"/"));
  var completion=Ce03NormalUiCapture.Read(Path.Combine(output,"completion.json"));if((string)completion["runId"]!=original.RunId)throw new InvalidOperationException("Original completion mismatch");
  var finish=Ce03NormalUiCapture.Read(Path.Combine(output,"finish-ack.json"));if((string)finish["version"]!="ce03-trusted-finish-ack-v1"||(string)finish["runId"]!=original.RunId||(string)finish["modelSha256"]!=(string)completion["modelSha256"])throw new InvalidOperationException("Trusted terminal finish acknowledgement missing");
  string originalSourceHash=ValidateClaimSources(sources,original,completion,finish);
  using(var runs=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),new Ce03EditorClaimJournal(SessionPath("ui-claim-journal.json"),session.AccountId),()=>session,()=>1)){
   account.BindRunRecovery(runs);BuildUi();StartVideo();await Pause(24);deck.OpenRunLifecycle(original.RunId);await Wait(()=>!account.RunResultsLoading&&!account.LifecycleLoading);
   await Pause(30);if(!account.CanClaimRunReward(original.RunId))throw new InvalidOperationException("Actual finished run has no claimable reward: "+account.LifecycleNotice+" "+account.RunResultNotice);Click("ClaimRunReward");await Wait(()=>account.VerifiedRunReceipt!=null);await Pause(36);
   if(account.Lifecycle?.RewardStatus!="CLAIMED")throw new InvalidOperationException("Actual claim was not acknowledged");Click("CloseLobbyDialog");await Pause(36);StopVideo();Ce03ConsumerEvidenceRunner.VerifySources(sources);
   Ce03NormalUiCapture.Atomic(Path.Combine(output,"claim-ui.json"),Canon(new JObject{["runId"]=original.RunId,["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["sourceBundleSha256"]=bundleHash,["originalSourceSha256"]=originalSourceHash,["rewardStatus"]=account.Lifecycle.RewardStatus,["explicitUiClaim"]=true,["returnedToLobby"]=!deck.IsDialogOpen,["receipt"]=account.VerifiedRunReceipt.DeepClone(),["frames"]=frames,["finishSentByUnity"]=false}));
  }
 }
}
#endif
