using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryPreparationCancellationTests {
 static async Task<T> Bounded<T>(Task<T> task){var winner=await Task.WhenAny(task,Task.Delay(3000)).ConfigureAwait(false);Assert.AreSame(task,winner,"Preparation did not complete within 3 seconds");return await task.ConfigureAwait(false);}
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Field(object value,string name)=>value.GetType().GetField(name,Hidden).GetValue(value);
 static object Call(object value,string name,params object[] args){try{return value.GetType().GetMethod(name,Hidden).Invoke(value,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static IDisposable Fixture(bool recovered=true)=> (IDisposable)Activator.CreateInstance(typeof(OrdinaryNodePreparationOwnerTests).GetNestedType("Fixture",BindingFlags.NonPublic),new object[]{recovered,null});
 static OrdinaryNodePreparationOwner Owner(object f)=>(OrdinaryNodePreparationOwner)Field(f,"Owner");
 static void Cancel(OrdinaryNodePreparationOwner owner){var method=owner.GetType().GetMethod("CancelPreparation",Type.EmptyTypes);Assert.NotNull(method,"Preparation cancellation must retain an accepted unstarted attempt");method.Invoke(owner,null);}
 static System.Collections.Generic.List<RunApiHttpRequest> Requests(object f)=>(System.Collections.Generic.List<RunApiHttpRequest>)Field(Field(f,"Transport"),"Requests");
 static void Reply(object f,Func<RunApiHttpRequest,Task<GameApiAuthenticatedHttpResult>> send)=>Field(f,"Transport").GetType().GetField("Reply",Hidden).SetValue(Field(f,"Transport"),send);
 static GameApiAuthenticatedHttpResult Accepted(object f,RunApiHttpRequest r)=>GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,(string)Call(f,"Response",r)));
 [Test] public async Task AcceptedPreparationCancellationReleasesHpButReusesCurrentGeneration(){
  using var f=Fixture();var owner=Owner(f);var first=await Bounded(owner.PrepareAsync());long generation=owner.CurrentAttempt.Generation;Assert.AreEqual(1,Requests(f).Count);
  Cancel(owner);Assert.IsTrue(first.IsRetired);Assert.IsNull(first.StateJson);
  var next=await Bounded(owner.PrepareAsync());Assert.AreNotSame(first,next);Assert.IsFalse(next.IsRetired);Assert.AreEqual(generation,owner.CurrentAttempt.Generation);Assert.AreEqual(1,Requests(f).Count);Assert.AreEqual(800,(int)next.HudState["trainHp"]);
 }
 [Test] public async Task PresenterRetirementBeforeExecutionDoesNotIssueAnotherGeneration(){using var f=Fixture();var owner=Owner(f);var first=await Bounded(owner.PrepareAsync());first.Retire();var next=await Bounded(owner.PrepareAsync());Assert.AreNotSame(first,next);Assert.AreEqual(1,Requests(f).Count);Assert.AreEqual(2,owner.CurrentAttempt.Generation);}
 [Test] public async Task CancelDuringPostAndImmediateReselectionSharesConfirmationButOnlyNewIntentGetsDriver(){
  using var f=Fixture();var owner=Owner(f);var response=new TaskCompletionSource<GameApiAuthenticatedHttpResult>();Reply(f,r=>response.Task);try{
  var old=owner.PrepareAsync();Assert.AreEqual(1,Requests(f).Count);Cancel(owner);var current=owner.PrepareAsync();Assert.AreNotSame(old,current);Assert.AreEqual(1,Requests(f).Count);
  response.SetResult(Accepted(f,Requests(f).Single()));Assert.CatchAsync<InvalidOperationException>(()=>Bounded(old));
  var driver=await Bounded(current);Assert.IsFalse(driver.IsRetired);Assert.AreSame(driver,await Bounded(owner.PrepareAsync()));Assert.AreEqual(1,Requests(f).Count);Assert.AreEqual(2,owner.CurrentAttempt.Generation);
 }finally{response.TrySetCanceled();}
 }
 [Test] public async Task LateSuccessAfterCancellationIsRetainedWithoutCreatingDriverUntilReselection(){
  using var f=Fixture();var owner=Owner(f);var response=new TaskCompletionSource<GameApiAuthenticatedHttpResult>();Reply(f,r=>response.Task);try{var old=owner.PrepareAsync();Cancel(owner);
  response.SetResult(Accepted(f,Requests(f).Single()));Assert.CatchAsync<InvalidOperationException>(()=>Bounded(old));Assert.IsNull(Field(owner,"driver"));Assert.AreEqual(2,owner.CurrentAttempt.Generation);
  var next=await Bounded(owner.PrepareAsync());Assert.IsFalse(next.IsRetired);Assert.AreEqual(1,Requests(f).Count);
 }finally{response.TrySetCanceled();}
 }
 [Test] public async Task CancelledLostResponseRetainsOriginalKeyAndBodyForRetry(){
  using var f=Fixture();var owner=Owner(f);var response=new TaskCompletionSource<GameApiAuthenticatedHttpResult>();Reply(f,r=>response.Task);try{var old=owner.PrepareAsync();Cancel(owner);response.SetResult(GameApiAuthenticatedHttpResult.NetworkError("lost"));Assert.CatchAsync<InvalidOperationException>(()=>Bounded(old));
  Reply(f,r=>Task.FromResult(Accepted(f,r)));var next=await Bounded(owner.PrepareAsync());Assert.NotNull(next);Assert.AreEqual(2,Requests(f).Count);Assert.AreEqual(Requests(f)[0].BodyJson,Requests(f)[1].BodyJson);Assert.AreEqual(Requests(f)[0].Headers.Single(h=>h.Name=="Idempotency-Key").Value,Requests(f)[1].Headers.Single(h=>h.Name=="Idempotency-Key").Value);
 }finally{response.TrySetCanceled();}
 }
 [Test] public async Task ConfirmedNodeResultDuringCancellationNeverAutomaticallyCreatesCombat(){
  using var f=Fixture();var owner=Owner(f);var response=new TaskCompletionSource<GameApiAuthenticatedHttpResult>();Reply(f,r=>response.Task);try{var old=owner.PrepareAsync();Cancel(owner);
  var n=((BRouteCommandResult)Field(f,"Arrival")).NodeStart;var json=JObject.Parse((string)Call(f,"Response",Requests(f).Single()));json["status"]="RESULT_AVAILABLE";json["generation"]=null;json.Remove("snapshot");json.Remove("snapshotHash");json["runStatus"]="ACTIVE";json["state"]=new JObject{["version"]="operation.map-progress.v1",["traversedEdgeIds"]=new JArray(),["pendingEdgeId"]=null,["phase"]="READY",["holdIds"]=new JArray()};
  response.SetResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,json.ToString())));Assert.CatchAsync<InvalidOperationException>(()=>Bounded(old));Assert.IsNull(Field(owner,"driver"));
  var error=Assert.ThrowsAsync<OrdinaryNodeStartUnavailableException>(()=>Bounded(owner.PrepareAsync()));Assert.AreEqual(OrdinaryPreparationFailure.NodeResultAvailable,error.Failure);Assert.AreEqual(1,Requests(f).Count);await Task.CompletedTask;
 }finally{response.TrySetCanceled();}
 }
 [Test] public async Task LeavingActuallyAdvancedNativeAttemptRequiresRestartFromOriginalNode(){
  using var f=Fixture();var owner=Owner(f);var preparation=await Bounded(owner.PrepareAsync());
  var begun=typeof(CombatSceneDriver).GetProperty("HasBegunExecution");Assert.NotNull(begun,"The driver must expose committed native execution history");Assert.IsFalse((bool)begun.GetValue(preparation));
  var arrival=(BRouteCommandResult)Field(f,"Arrival");var original=(OrdinaryFiveStartInput)Field(f,"Start");var attempt=owner.CurrentAttempt;
  var factory=OrdinaryFiveCombatFactory.FromNodeStart(original,arrival.NodeStart,arrival.Scaling,170000,attempt);
  var active=OrdinaryNativeHostTests.CreateBegunNodeDriver(factory);Assert.IsTrue((bool)begun.GetValue(active));
  // Only the orchestration fixture substitutes the same-attempt genuinely advanced driver.
  preparation.Retire();owner.GetType().GetField("driver",Hidden).SetValue(owner,active);Cancel(owner);Assert.IsTrue(active.IsRetired);Assert.IsNull(active.StateJson);Assert.IsTrue((bool)begun.GetValue(active));
  var next=await Bounded(owner.PrepareAsync());Assert.AreEqual(2,Requests(f).Count);Assert.AreEqual(attempt.Generation+1,owner.CurrentAttempt.Generation);Assert.IsFalse((bool)begun.GetValue(next));Assert.AreEqual(800,(int)next.HudState["trainHp"]);
 }
}
