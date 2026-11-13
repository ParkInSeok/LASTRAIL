using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {public sealed class RunLifecycleTests {
 [TestCase("valid")][TestCase("review")][TestCase("unknown")][TestCase("original")][TestCase("accepted")][TestCase("outside")][TestCase("content")]
 public void ClaimSourceBindingCannotRewriteExecution(string kind){
  var method=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Ce03NormalUiRecorder")).First(t=>t!=null).GetMethod("VerifyClaimSourceBinding");
  Func<string,string,JObject> file=(path,content)=>new JObject{["path"]=path,["content"]=content,["sha256"]=LastRail.Battle.Ce03CommandContract.Sha(content)};
  var prior=new JObject{["files"]=new JArray(file("unchanged.cs","original"),file("Assets/_LASTRAIL/Editor/Ce03NormalUiClaim.cs","old"))};
  if(kind=="content")prior["files"][0]["sha256"]=new string('0',64);
  string raw=LastRail.Battle.Ce03CommandContract.Canonical(prior),oldHash=LastRail.Battle.Ce03CommandContract.Sha(raw);var current=(JObject)prior.DeepClone();current["files"][1]=file("Assets/_LASTRAIL/Editor/Ce03NormalUiClaim.cs","new");if(kind=="outside")current["files"][0]=file("unchanged.cs","tampered");
  string hash=LastRail.Battle.Ce03CommandContract.Sha(LastRail.Battle.Ce03CommandContract.Canonical(current));var response=JObject.Parse("{\"combat\":{\"profile\":{\"baseProfile\":{\"executionEvidence\":{}}}}}");response["combat"]["profile"]["baseProfile"]["executionEvidence"]["algorithmSourceSha256"]=kind=="accepted"?"bad":oldHash;
  var completion=new JObject{["algorithmSourceSha256"]=kind=="original"?"bad":oldHash};var finish=new JObject{["sourceBundleSha256"]=oldHash};string ack=finish.ToString();var review=new JObject{["version"]="ce03-claim-source-review-v1",["runId"]="run_test",["originalSourceSha256"]=oldHash,["claimSourceSha256"]=kind=="review"?"bad":hash,["finishAckSha256"]=LastRail.Battle.Ce03CommandContract.Sha(ack)};if(kind=="unknown")review["bypass"]=true;
  TestDelegate check=()=>method.Invoke(null,new object[]{current,prior,raw,response,completion,finish,review,ack,"run_test",hash});if(kind=="valid")Assert.DoesNotThrow(check);else Assert.Catch(check);
 }
 [Test] public void LargeOriginalStartFitsLifecycleButNotOrdinaryApi(){var data=Data("CLAIMABLE");data["startResponse"]["largeSnapshot"]=new string('x',400000);var raw=data.ToString(Newtonsoft.Json.Formatting.None);Assert.Throws<FormatException>(()=>GameApiJson.ReadObject(raw));Assert.That(new RunLifecycleSnapshot(raw,"run_test").NextAction,Is.EqualTo("REWARD_REVIEW"));}
 [Test] public void LifecycleByteBoundaryIsExact(){var data=Data();data["padding"]="";var raw=data.ToString(Newtonsoft.Json.Formatting.None);int remaining=8*1024*1024-System.Text.Encoding.UTF8.GetByteCount(raw);data["padding"]=new string('x',remaining);Assert.DoesNotThrow(()=>new RunLifecycleSnapshot(data.ToString(Newtonsoft.Json.Formatting.None),"run_test"));data["padding"]=new string('x',remaining-1)+"한";Assert.Throws<FormatException>(()=>new RunLifecycleSnapshot(data.ToString(Newtonsoft.Json.Formatting.None),"run_test"));}
 [TestCase("duplicate")][TestCase("trailing")][TestCase("comment")][TestCase("depth")]
 public void ExtendedReaderStillRejectsMalformedJson(string kind){var raw=Data().ToString(Newtonsoft.Json.Formatting.None);if(kind=="duplicate")raw=raw.Insert(1,"\"accepted\":true,");if(kind=="trailing")raw+="{}";if(kind=="comment")raw=raw.Insert(1,"/*bad*/");if(kind=="depth")raw=raw.Insert(1,"\"nested\":"+new string('[',33)+"0"+new string(']',33)+",");Assert.Catch(()=>new RunLifecycleSnapshot(raw,"run_test"));}
 static JObject Data(string status="NOT_AVAILABLE"){bool none=status=="NOT_AVAILABLE";bool claimed=status=="CLAIMED";return new JObject{["accepted"]=true,["version"]="run-lifecycle-v1",["serverTime"]=1000,["runId"]="run_test",["runStatus"]=none?"ACTIVE":"COMPLETED",["startResponse"]=new JObject{["accepted"]=true,["startStatus"]="STARTED",["runId"]="run_test",["balanceVersion"]="1"},["finish"]=new JObject{["status"]=none?"NOT_RECORDED":"RECORDED",["outcome"]=none?null:"VICTORY",["submittedAt"]=none?JValue.CreateNull():new JValue(500),["rewardPreviewId"]=none?null:"preview"},["reward"]=new JObject{["status"]=status,["rewardPreviewId"]=none?null:"preview",["expiresAt"]=none?JValue.CreateNull():new JValue(status=="CLAIMABLE"?2000:900),["claimId"]=claimed?"claim":null,["receipt"]=claimed?new JObject{["rewardTransactionId"]="claim",["runId"]="run_test",["serverClaimedAt"]=800,["rewardPlan"]=new JObject{["rewardLines"]=new JArray()}}:JValue.CreateNull()},["nextAction"]=none?"RECOVER_COMBAT_CONSUMER":claimed?"RETURN_TO_LOBBY":status=="CLAIMABLE"?"REWARD_REVIEW":"REVIEW_REQUIRED",["grantsOnRead"]=false,["newAttemptAuthorized"]=false,["uiReturnAllowed"]=true,["disposition"]=none||status=="CLAIMABLE"?"PENDING":claimed?"REWARDED":status=="EXPIRED"?"EXPIRED_UNCLAIMED":"REVIEW_REQUIRED"};}
 [TestCase("NOT_AVAILABLE")][TestCase("CLAIMABLE")][TestCase("CLAIMED")][TestCase("EXPIRED")][TestCase("UNAVAILABLE")]
 public void LifecycleStatesNeverAuthorizeNewRun(string status){var data=Data(status);var snapshot=new RunLifecycleSnapshot(data.ToString(),"run_test");Assert.That(snapshot.RewardStatus,Is.EqualTo(status));Assert.That(snapshot.UiReturnAllowed,Is.True);Assert.That(snapshot.NewAttemptAuthorized,Is.False);data["newAttemptAuthorized"]=true;Assert.Throws<FormatException>(()=>new RunLifecycleSnapshot(data.ToString(),"run_test"));}
 [TestCase("COMPLETED","VICTORY","작전 성공")]
 [TestCase("FAILED","FAILURE","작전 실패")]
 [TestCase("ABANDONED","ABANDONED","작전 포기")]
 [TestCase("COMPLETED","FAILURE","결과 확인 필요")]
 [TestCase("FAILED","VICTORY","결과 확인 필요")]
 [TestCase("ABANDONED","UNKNOWN","결과 확인 필요")]
 public void ResultReasonRequiresMatchingRecordedOutcome(string status,string outcome,string title){
  var data=Data("EXPIRED");data["runStatus"]=status;data["finish"]["outcome"]=outcome;
  var snapshot=new RunLifecycleSnapshot(data.ToString(),"run_test");
  var property=typeof(RunLifecycleSnapshot).GetProperty("ResultTitle");Assert.IsNotNull(property);
  Assert.That(property.GetValue(snapshot),Is.EqualTo(title));
  Assert.That(snapshot.Notice,Does.Contain("보상 수령 기한"));Assert.That(snapshot.Notice,Does.Not.Contain("재개 기한"));
 }
 [Test] public void ReceiptOwnershipAndMissingReceiptRejectSuccess(){var data=Data("CLAIMED");data["reward"]["receipt"]["runId"]="other";Assert.Throws<FormatException>(()=>new RunLifecycleSnapshot(data.ToString(),"run_test"));data["reward"]["receipt"]=null;Assert.Throws<FormatException>(()=>new RunLifecycleSnapshot(data.ToString(),"run_test"));}
 [Test] public void RewardPinIsOpaqueStrictAndCloned(){var value=new JObject{["version"]="run-reward-ref-v1",["balanceVersion"]="1",["operationId"]="op",["risk"]="I",["scope"]="BASE_RUN_REWARDS",["hashAlgorithm"]="SHA-256",["canonicalization"]="canonical-json-v1",["configSha256"]=new string('a',64)};var pin=new CombatRewardReference(value,"1","op","I");Assert.That(pin.Verification,Is.EqualTo("OPAQUE_SERVER_PIN"));value["balanceVersion"]="2";Assert.That((string)pin.Json["balanceVersion"],Is.EqualTo("1"));Assert.Throws<FormatException>(()=>new CombatRewardReference(value,"1","op","I"));}
 sealed class Wire:IRunApiHttpTransport {public int Writes;public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){if(r.Method!="GET")Writes++;return Reply(r);}}
 [Test] public async Task ReadOnlyLateResponseCannotCrossOwner(){var done=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=r=>done.Task};var session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddMinutes(10));long generation=1;using var state=new AccountScreenRuntime(wire,null,()=>session,()=>generation);var task=state.RefreshRunLifecycleAsync("run_test");generation++;state.SynchronizeSession();done.SetResult(RunApiHttpResponse.Success(200,Data().ToString()));await task;Assert.That(state.Lifecycle,Is.Null);Assert.That(wire.Writes,Is.Zero);Assert.That(state.CanConfirmDeparture,Is.False);}
} }
