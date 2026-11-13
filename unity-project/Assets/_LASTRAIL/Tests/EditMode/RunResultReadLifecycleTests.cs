using System;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class RunResultReadLifecycleTests {
  sealed class Wire:IRunApiHttpTransport {
   public int Reads;public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Reads++;return Reply(request);}
  }
  static JObject Data(string status="CLAIMED",string run="run_test"){
   var data=(JObject)typeof(RunLifecycleTests).GetMethod("Data",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{status});
   data["runId"]=run;data["startResponse"]["runId"]=run;if(data["reward"]["receipt"] is JObject receipt)receipt["runId"]=run;return data;
  }
  static Task Ensure(AccountScreenRuntime state,string run){var method=typeof(AccountScreenRuntime).GetMethod("EnsureRunResultsAsync");Assert.That(method,Is.Not.Null,"Result entry must reuse validated terminal state");return (Task)method.Invoke(state,new object[]{run});}
  [TestCase(false)][TestCase(true)] public async Task SameRunInFlightReadsAreJoined(bool result){
   var gate=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=_=>gate.Task};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   var first=result?state.ReadRunResultsAsync("run_test"):state.RefreshRunLifecycleAsync("run_test");
   var second=result?state.ReadRunResultsAsync("run_test"):state.RefreshRunLifecycleAsync("run_test");
   Assert.That(wire.Reads,Is.EqualTo(1));Assert.That(second,Is.SameAs(first));
   gate.SetResult(RunApiHttpResponse.Success(200,Data().ToString()));await Task.WhenAll(first,second);
  }
  [TestCase("CLAIMED")][TestCase("EXPIRED")] public async Task TerminalEntryReusesButExplicitRefreshReads(string status){
   var wire=new Wire{Reply=_=>Task.FromResult(RunApiHttpResponse.Success(200,Data(status).ToString()))};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   await state.ReadRunResultsAsync("run_test");for(int i=0;i<5;i++)await Ensure(state,"run_test");Assert.That(wire.Reads,Is.EqualTo(1));
   await state.ReadRunResultsAsync("run_test");Assert.That(wire.Reads,Is.EqualTo(2));
  }
  [Test] public async Task ActiveRunReusesObservationWithoutBecomingFinal(){
   var wire=new Wire{Reply=_=>Task.FromResult(RunApiHttpResponse.Success(200,Data("NOT_AVAILABLE").ToString()))};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   await state.ReadRunResultsAsync("run_test");await Ensure(state,"run_test");Assert.That(wire.Reads,Is.EqualTo(1));Assert.That(state.VerifiedRunReceipt,Is.Null);Assert.That(state.Lifecycle.NextAction,Is.EqualTo("RECOVER_COMBAT_CONSUMER"));
  }
  [Test] public async Task ConfirmedTerminalCannotRegressToActive(){
   string response=Data().ToString();var wire=new Wire{Reply=_=>Task.FromResult(RunApiHttpResponse.Success(200,response))};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   await state.RefreshRunLifecycleAsync("run_test");var terminal=state.Lifecycle;response=Data("NOT_AVAILABLE").ToString();await state.RefreshRunLifecycleAsync("run_test");
   Assert.That(state.Lifecycle,Is.SameAs(terminal));Assert.That(state.Lifecycle.NextAction,Is.EqualTo("RETURN_TO_LOBBY"));Assert.That(state.LifecycleNotice,Does.Contain("일치하지"));
  }
  [Test] public async Task ExpiredRewardCannotRegressToClaimable(){
   string response=Data("EXPIRED").ToString();var wire=new Wire{Reply=_=>Task.FromResult(RunApiHttpResponse.Success(200,response))};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   await state.RefreshRunLifecycleAsync("run_test");response=Data("CLAIMABLE").ToString();await state.RefreshRunLifecycleAsync("run_test");
   Assert.That(state.Lifecycle.RewardStatus,Is.EqualTo("EXPIRED"));Assert.That(state.CanClaimRunReward("run_test"),Is.False);Assert.That(state.LifecycleNotice,Does.Contain("일치하지"));
  }
  [Test] public async Task StandaloneLifecycleCannotReuseAnotherRunsReceipt(){
   var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,Data("CLAIMED",r.EndpointPath.Contains("run_other")?"run_other":"run_test").ToString()))};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   await state.ReadRunResultsAsync("run_test");await state.RefreshRunLifecycleAsync("run_other");await Ensure(state,"run_other");
   Assert.That(wire.Reads,Is.EqualTo(2));Assert.That((string)state.VerifiedRunReceipt["runId"],Is.EqualTo("run_other"));
  }
  [Test] public async Task DifferentRunAndLateResponseCannotReplaceCurrentTerminal(){
   var old=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=r=>r.EndpointPath.Contains("run_old")?old.Task:Task.FromResult(RunApiHttpResponse.Success(200,Data().ToString()))};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   var pending=state.ReadRunResultsAsync("run_old");await state.ReadRunResultsAsync("run_test");old.SetResult(RunApiHttpResponse.Success(200,Data("NOT_AVAILABLE","run_old").ToString()));await pending;
   Assert.That(state.Lifecycle.RunId,Is.EqualTo("run_test"));Assert.That(state.VerifiedRunReceipt,Is.Not.Null);Assert.That(state.RunResultsLoading,Is.False);
  }
  [Test] public async Task FailedVerificationRetainsTerminalButDoesNotBecomeReusableSuccess(){
   bool fail=false;var wire=new Wire{Reply=_=>Task.FromResult(fail?RunApiHttpResponse.NetworkError("injected"):RunApiHttpResponse.Success(200,Data().ToString()))};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   await state.ReadRunResultsAsync("run_test");fail=true;await state.ReadRunResultsAsync("run_test");Assert.That(state.Lifecycle.ConfirmedOutcome,Is.EqualTo("VICTORY"));
   fail=false;await Ensure(state,"run_test");Assert.That(wire.Reads,Is.EqualTo(3));Assert.That(state.VerifiedRunReceipt,Is.Not.Null);
  }
  [Test] public async Task CommandInvalidationFencesOldReadAndStartsNewVerification(){
   var old=new TaskCompletionSource<RunApiHttpResponse>();int calls=0;var wire=new Wire{Reply=_=>++calls==1?old.Task:Task.FromResult(RunApiHttpResponse.Success(200,Data().ToString()))};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   var pending=state.ReadRunResultsAsync("run_test");typeof(AccountScreenRuntime).GetMethod("InvalidateRunReads",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(state,null);
   await state.ReadRunResultsAsync("run_test");old.SetResult(RunApiHttpResponse.Success(200,Data("NOT_AVAILABLE").ToString()));await pending;
   Assert.That(calls,Is.EqualTo(2));Assert.That(state.Lifecycle.NextAction,Is.EqualTo("RETURN_TO_LOBBY"));Assert.That(state.VerifiedRunReceipt,Is.Not.Null);
  }
 }
}
