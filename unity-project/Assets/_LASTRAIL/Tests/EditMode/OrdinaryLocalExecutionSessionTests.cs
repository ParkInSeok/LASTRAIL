using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryLocalExecutionSessionTests {
 // Transport double only; accepted checkpoints are produced by the real shared Execute entry point.
 sealed class Wire:IRunApiHttpTransport {
  internal readonly JObject Input;
  internal readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
  readonly Dictionary<string,RunApiHttpResponse> saved=new Dictionary<string,RunApiHttpResponse>();
  internal Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Send;
  internal Wire(JObject input){Input=(JObject)input.DeepClone();Send=r=>Task.FromResult(Reply(r));}
  public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests.Add(request);return Send(request);}
  internal RunApiHttpResponse Reply(RunApiHttpRequest request){
   string key=request.Headers.Single(h=>h.Name=="Idempotency-Key").Value;if(saved.TryGetValue(key,out var existing))return existing;
   var body=JObject.Parse(request.BodyJson);Assert.AreEqual(((JArray)Input["inputs"]).Count,(long)body["expectedSequence"]);
   foreach(var row in (JArray)body["inputs"])((JArray)Input["inputs"]).Add(row.DeepClone());
   var result=OrdinaryLocalExecutionReplay.Execute(Input);var response=RunApiHttpResponse.Success(200,new JObject{["accepted"]=true,["version"]="ordinary.local-execution-receipt.v1.20261106",["requestId"]=key,["evidenceRef"]="local:"+key,["result"]=result}.ToString());saved.Add(key,response);return response;
  }
 }
 sealed class Fixture:IDisposable {
  internal readonly CombatSceneDriver Driver;
  internal readonly OrdinaryFiveCombatFactory Factory;
  internal readonly Wire Wire;
  internal readonly IGameApiAuthenticatedHttpTransport Transport;
  internal readonly OrdinaryLocalExecutionSession Session;
  internal bool Current=true;
  internal static GameApiAccessToken Token()=>new GameApiAccessToken("lst1.test.signature");
  internal Fixture(){var input=OrdinaryLiveExecutionTests.CreateInput();Factory=OrdinaryLiveExecutionTests.CreateFactory(input);Driver=CombatSceneDriver.ForOrdinaryNode(Factory,()=>Current);Driver.Open();Wire=new Wire(input);Transport=new GameApiAuthenticatedHttpTransport(Wire);Session=new OrdinaryLocalExecutionSession(Driver,Factory.Attempt,Transport,"http://127.0.0.1:8789",Token,()=>Current);}
  public void Dispose(){Session.Dispose();Driver.Retire();}
 }
 [Test] public async Task ExplicitFlushUsesCommittedInputsBearerAndActualSharedReplayResult(){
  using var f=new Fixture();Assert.AreEqual(0,f.Wire.Requests.Count);var initial=await f.Session.FlushAsync();Assert.NotNull(initial);Assert.AreEqual(0,f.Session.AcceptedSequence);
  Assert.IsTrue(JToken.DeepEquals(initial,await f.Session.FlushAsync()));Assert.AreEqual(1,f.Wire.Requests.Count,"No new committed input reuses the prior receipt.");
  f.Driver.SetSpeed(3);f.Driver.Advance(340000,20);var receipt=await f.Session.FlushAsync();Assert.AreEqual(2,f.Session.AcceptedSequence);
  Assert.AreEqual(f.Driver.Tick,(long)receipt["result"]["tick"]);Assert.IsTrue(JToken.DeepEquals(JObject.Parse(f.Driver.StateJson),receipt["result"]["checkpoint"]));
  var request=f.Wire.Requests.Last();Assert.AreEqual("POST",request.Method);Assert.AreEqual(OrdinaryLocalExecutionSession.EndpointPath,request.EndpointPath);
  Assert.AreEqual("Bearer lst1.test.signature",request.Headers.Single(h=>h.Name=="Authorization").Value);Assert.AreEqual(0,(long)JObject.Parse(request.BodyJson)["expectedSequence"]);
  var commands=(JArray)JObject.Parse(request.BodyJson)["inputs"];CollectionAssert.AreEqual(new[]{"SPEED","ADVANCE"},commands.Select(c=>(string)c["command"]["kind"]));
 }
 [Test] public async Task LostResponseRetriesFrozenKeyAndBodyBeforeSendingLaterCommittedInputs(){
  using var f=new Fixture();f.Driver.SetSpeed(3);bool lose=true;f.Wire.Send=r=>{var response=f.Wire.Reply(r);if(lose){lose=false;return Task.FromResult(RunApiHttpResponse.NetworkError("lost test response"));}return Task.FromResult(response);};
  Assert.IsNull(await f.Session.FlushAsync());var pending=f.Session.Pending;Assert.NotNull(pending);f.Driver.Advance(340000,20);
  Assert.NotNull(await f.Session.FlushAsync());Assert.AreEqual(pending.BodyJson,f.Wire.Requests[1].BodyJson);Assert.AreEqual(pending.Key,f.Wire.Requests[1].Headers.Single(h=>h.Name=="Idempotency-Key").Value);Assert.AreEqual(1,f.Session.AcceptedSequence);
  Assert.NotNull(await f.Session.FlushAsync());Assert.AreEqual(2,f.Session.AcceptedSequence);Assert.AreEqual(1,(long)JObject.Parse(f.Wire.Requests[2].BodyJson)["expectedSequence"]);
 }
 [Test] public async Task DelayedFrozenAcknowledgementAllowsBoundedSuffixThenResumesAfterCapture(){
  using var f=new Fixture();await f.Session.FlushAsync();f.Driver.SetSpeed(3);
  var gate=new TaskCompletionSource<RunApiHttpResponse>();RunApiHttpResponse frozen=null;
  f.Wire.Send=r=>{frozen=f.Wire.Reply(r);return gate.Task;};var pendingWork=f.Session.FlushAsync();var batch=f.Session.Pending;
  var frozenState=f.Driver.StateJson;long frozenTick=f.Driver.Tick;Assert.IsTrue(f.Session.CanAcceptMoreInputs);
  for(int i=0;i<128;i++){Assert.IsTrue(f.Session.CanAcceptMoreInputs);if(i==0)f.Driver.Advance(340000,20);else f.Driver.SetRailThrottle(i%2==0?7000:13000);}
  Assert.Greater(f.Driver.Tick,frozenTick);
  Assert.IsFalse(f.Session.CanAcceptMoreInputs);Assert.AreSame(batch,f.Session.Pending);Assert.AreNotEqual(frozenState,f.Driver.StateJson);
  gate.SetResult(frozen);var ack=await pendingWork;Assert.AreEqual(1,f.Session.AcceptedSequence);
  Assert.IsTrue(JToken.DeepEquals(JObject.Parse(frozenState),ack["result"]["checkpoint"]));Assert.IsFalse(f.Session.CanAcceptMoreInputs,"Acknowledgement alone must not reset the uncaptured suffix bound.");
  f.Wire.Send=r=>Task.FromResult(f.Wire.Reply(r));var suffix=await f.Session.FlushAsync();
  Assert.IsTrue(f.Session.CanAcceptMoreInputs);Assert.AreEqual(129,f.Session.AcceptedSequence);
  var body=JObject.Parse(f.Wire.Requests.Last().BodyJson);Assert.AreEqual(1,(long)body["expectedSequence"]);Assert.AreEqual(128,((JArray)body["inputs"]).Count);
  Assert.IsTrue(JToken.DeepEquals(JObject.Parse(f.Driver.StateJson),suffix["result"]["checkpoint"]));
 }
 [Test] public async Task RetryDoesNotResetCapturedRevisionOrBypassSuffixBound(){
  using var f=new Fixture();f.Wire.Send=r=>{f.Wire.Reply(r);return Task.FromResult(RunApiHttpResponse.NetworkError("lost test response"));};
  Assert.IsNull(await f.Session.FlushAsync());var batch=f.Session.Pending;
  for(int i=0;i<128;i++)f.Driver.SetRailThrottle(i%2==0?7000:13000);
  Assert.IsFalse(f.Session.CanAcceptMoreInputs);Assert.IsNull(await f.Session.FlushAsync());
  Assert.AreSame(batch,f.Session.Pending);Assert.IsFalse(f.Session.CanAcceptMoreInputs);
 }
 [TestCase("key")][TestCase("checkpoint")]
 public async Task MismatchedSuccessfulResponsePreservesOriginalPendingBatch(string fault){
  using var f=new Fixture();f.Wire.Send=r=>{var original=f.Wire.Reply(r);var body=JObject.Parse(original.BodyJson);if(fault=="key")body["requestId"]="other";else body["result"]["checkpoint"]["motion"]["front"]=(long)body["result"]["checkpoint"]["motion"]["front"]+1;return Task.FromResult(RunApiHttpResponse.Success(200,body.ToString()));};
  Assert.ThrowsAsync<ArgumentException>(async()=>await f.Session.FlushAsync());var pending=f.Session.Pending;Assert.NotNull(pending);Assert.IsNull(f.Session.LastReceipt);
  f.Wire.Send=r=>Task.FromResult(f.Wire.Reply(r));Assert.NotNull(await f.Session.FlushAsync());Assert.AreEqual(pending.BodyJson,f.Wire.Requests.Last().BodyJson);
 }
 [TestCase(false)][TestCase(true)] public async Task DisposedOrReplacedSessionCannotAcceptLateResponse(bool dispose){
  using var f=new Fixture();var gate=new TaskCompletionSource<RunApiHttpResponse>();RunApiHttpResponse reply=null;f.Wire.Send=r=>{reply=f.Wire.Reply(r);return gate.Task;};
  var work=f.Session.FlushAsync();Assert.IsTrue(f.Session.InFlight);var pending=f.Session.Pending;
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await f.Session.FlushAsync());Assert.AreEqual(1,f.Wire.Requests.Count);
  if(dispose)f.Session.Dispose();else f.Current=false;Assert.IsFalse(f.Session.CanAcceptMoreInputs);gate.SetResult(reply);
  if(dispose)Assert.ThrowsAsync<ObjectDisposedException>(async()=>await work);else Assert.ThrowsAsync<InvalidOperationException>(async()=>await work);
  Assert.AreSame(pending,f.Session.Pending);Assert.IsNull(f.Session.LastReceipt);Assert.IsFalse(f.Session.InFlight);await Task.CompletedTask;
 }
 [TestCase("https://127.0.0.1:8789")][TestCase("http://example.com:8789")][TestCase("http://127.0.0.1:8789/other")][TestCase("http://user:pass@127.0.0.1:8789")]
 public void OnlyExplicitLoopbackHttpOriginIsAccepted(string origin){
  using var f=new Fixture();Assert.Throws<ArgumentException>(()=>new OrdinaryLocalExecutionSession(f.Driver,f.Factory.Attempt,f.Transport,origin,Fixture.Token,()=>true));Assert.AreEqual(0,f.Wire.Requests.Count);
 }
}
