using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using LastRail.Diagnostics;
using LastRail.Tests.EditMode;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryCurrentAttemptTests {
 static async Task<T> Bounded<T>(Task<T> task){var winner=await Task.WhenAny(task,Task.Delay(3000)).ConfigureAwait(false);Assert.AreSame(task,winner,"Preparation did not complete within 3 seconds");return await task.ConfigureAwait(false);}
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 sealed class Storage:IAndroidKeystoreNativeStoragePort {
  public AndroidKeystoreEncryptedTokenPayload Value;
  public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>true;public bool UsedPlayerPrefsFallback=>false;public string ProviderName=>"TEST_ONLY";
  public AndroidKeystoreEncryptedTokenPayload Encrypt(string value)=>new AndroidKeystoreEncryptedTokenPayload(value,"test");
  public string Decrypt(AndroidKeystoreEncryptedTokenPayload value)=>value.CipherTextBase64;
  public bool Save(AndroidKeystoreEncryptedTokenPayload value){Value=value;return true;}
  public AndroidKeystoreEncryptedTokenPayload Load()=>Value;
  public bool Clear()=>throw new AssertionException("Retained original request must survive.");
 }
 sealed class Wire:IGameApiAuthenticatedHttpTransport {
  public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
  public Func<RunApiHttpRequest,Task<GameApiAuthenticatedHttpResult>> Reply;
  public Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest r,GameApiAccessToken t){Requests.Add(r);return Reply(r);}
 }
 sealed class Fixture {
  internal readonly OrdinaryFiveStartInput Start;internal readonly BRouteCommandResult Arrival;internal readonly BRouteCommand ArrivalCommand;internal readonly JObject ArrivalResponse;
  internal readonly Storage Disk=new Storage();internal readonly Wire Transport=new Wire();
  internal OrdinaryNodeStart Node=>Arrival.NodeStart;internal bool Current=true;
  internal Fixture(){
   var f=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
   Start=f.Start;ArrivalCommand=f.Command;ArrivalResponse=f.Response;Arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
   Transport.Reply=r=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Response(r))));
  }
  internal string Response(RunApiHttpRequest r){var body=JObject.Parse(r.BodyJson);return new JObject{["accepted"]=true,["version"]="ordinary.node-restart.v1.20261028",["requestId"]=r.Headers.Single(h=>h.Name=="Idempotency-Key").Value,["accountId"]=Node.AccountId,["runId"]=Node.RunId,["nodeId"]=Node.NodeId,["status"]="RESTARTED",["revision"]=(long)body["expectedRevision"]+1,["generation"]=(long)body["expectedGeneration"]+1,["snapshot"]=Node.Capture()["snapshot"],["snapshotHash"]=Node.SnapshotHash}.ToString();}
  internal JObject Map(long generation=3){long revision=Node.EntryMapRevision+generation-1;return new JObject{
   ["version"]="operation.map-status.v1",["runId"]=Node.RunId,["runStatus"]="ACTIVE",["revision"]=revision,
   ["progress"]=new JObject{["phase"]="ACTIVE",["pendingEdgeId"]=null,["blockedByPendingTransactions"]=false},
   ["view"]=new JObject{["version"]="operation.map-view.v1",["graphId"]="graph",["graphRevision"]=1,["eventAssignmentVersion"]="events",["definitionVersion"]="defs",["profile"]="ORDINARY_12_NODE",["entryNodeId"]="entry",["currentNodeId"]=Node.NodeId,["traversedEdgeIds"]=new JArray("edge"),["nodes"]=new JArray(new JObject{["nodeId"]=Node.NodeId,["visitDepth"]=1,["eventKind"]="COMBAT",["publicInfo"]=new JObject{["name"]="First",["iconId"]="combat"},["access"]="CURRENT"}),["edges"]=new JArray(new JObject{["edgeId"]="edge",["fromNodeId"]="entry",["toNodeId"]=Node.NodeId,["traversed"]=true})},
   ["currentAttempt"]=new JObject{["version"]="ordinary.current-attempt.v1.20261102",["accountId"]=Node.AccountId,["runId"]=Node.RunId,["nodeId"]=Node.NodeId,["generation"]=generation,["mapRevision"]=revision,["snapshotHash"]=Node.SnapshotHash}};}
  internal object Projection(JObject map=null){var parsed=OperationMapStatus.Read((map??Map()).ToString(),Node.RunId);var p=typeof(OperationMapStatus).GetProperty("CurrentAttempt");Assert.NotNull(p,"Current attempt projection must be consumed from the existing map response");return p.GetValue(parsed);}
  internal OrdinaryNodeAttempt Attempt(JObject map=null){var p=Projection(map);var method=typeof(OrdinaryNodeAttempt).GetMethod("FromCurrent");Assert.NotNull(method,"Current projection must bind the immutable node origin");try{return (OrdinaryNodeAttempt)method.Invoke(null,new[]{(object)Node,p});}catch(TargetInvocationException e){throw e.InnerException;}}
  internal OrdinaryNodeRestartDelivery Delivery(OrdinaryNodeAttempt attempt)=>new OrdinaryNodeRestartDelivery(Node,Disk,Transport,()=>Current?new OrdinaryNodeRestartScope(Node.AccountId,Node.RunId,Node.NodeId,attempt.Revision,attempt.Generation):null);
  internal async Task Seed(bool pending=false){var owner=Delivery(OrdinaryNodeAttempt.FromEntry(Node));if(pending)Transport.Reply=r=>Task.FromResult(GameApiAuthenticatedHttpResult.NetworkError("lost"));await owner.DeliverAsync(new OrdinaryNodeRestart(Node,"retained-original-key",Node.EntryMapRevision,1),Token);Transport.Requests.Clear();Transport.Reply=r=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Response(r))));}
  internal OrdinaryNodePreparationOwner Preparation(OrdinaryNodeAttempt attempt){
   Func<OrdinaryNodeStart,Func<OrdinaryNodeRestartScope>,OrdinaryNodeRestartDelivery> delivery=(node,scope)=>new OrdinaryNodeRestartDelivery(node,Disk,Transport,scope);
   var ctor=typeof(OrdinaryNodePreparationOwner).GetConstructors().SingleOrDefault(c=>c.GetParameters().Length==8);Assert.NotNull(ctor,"Public preparation must consume current attempt without inferring old receipt generation");
   return (OrdinaryNodePreparationOwner)ctor.Invoke(new object[]{Start,Arrival,170000L,true,(Func<bool>)(()=>Current),(Func<GameApiAccessToken>)(()=>Token),delivery,attempt});
  }
 }
 static readonly GameApiAccessToken Token=new GameApiAccessToken("lst1.test.signature");
 static Task<OrdinaryNodeRestartResult> Restart(OrdinaryNodeRestartDelivery owner,OrdinaryNodeAttempt attempt){var method=typeof(OrdinaryNodeRestartDelivery).GetMethod("RestartFromCurrentAsync",new[]{typeof(OrdinaryNodeAttempt),typeof(GameApiAccessToken)});Assert.NotNull(method,"Current-authority restart and original-key recovery boundary required");return (Task<OrdinaryNodeRestartResult>)method.Invoke(owner,new object[]{attempt,Token});}
 [Test] public void ExistingMapCarriesCurrentGenerationWithoutRewritingNode(){var f=new Fixture();var map=f.Map();var attempt=f.Attempt(map);Assert.AreEqual(3,attempt.Generation);Assert.AreEqual(f.Node.EntryMapRevision+2,attempt.Revision);Assert.AreEqual(1,f.Node.Generation);map["currentAttempt"]["generation"]=99;Assert.AreEqual(3,attempt.Generation);}
 [TestCase("accountId")][TestCase("runId")][TestCase("nodeId")][TestCase("snapshotHash")][TestCase("mapRevision")][TestCase("generation")][TestCase("version")][TestCase("extra")]
 public void InvalidProjectionCannotBindOriginalNode(string fault){var f=new Fixture();var map=f.Map();var p=(JObject)map["currentAttempt"];if(fault=="mapRevision")p[fault]=(long)map["revision"]-1;else if(fault=="generation")p[fault]=0;else if(fault=="snapshotHash")p[fault]=new string('a',64);else p[fault]="foreign";Assert.Throws<ArgumentException>(()=>f.Attempt(map));}
 [TestCase(false)][TestCase(true)] public void NullAndHistoricalProjectionNeverManufactureGenerationOne(bool historical){var f=new Fixture();var map=f.Map();if(historical)map.Remove("currentAttempt");else map["currentAttempt"]=null;Assert.IsNull(f.Projection(map));}
 [TestCase("run")][TestCase("phase")] public void NonActiveMapCannotPublishCurrentAttempt(string mode){var f=new Fixture();var map=f.Map();if(mode=="run")map["runStatus"]="FINISH_PENDING";else map["progress"]["phase"]="READY";Assert.Throws<ArgumentException>(()=>f.Projection(map));}
 [Test] public async Task FreshProcessUsesCurrentThreeInsteadOfOldAcceptedTwo(){var f=new Fixture();await f.Seed();using var owner=f.Preparation(f.Attempt());var driver=await Bounded(owner.PrepareAsync());Assert.AreEqual(1,f.Transport.Requests.Count);var request=f.Transport.Requests.Single();Assert.AreEqual(3,(long)JObject.Parse(request.BodyJson)["expectedGeneration"]);Assert.AreEqual(f.Node.EntryMapRevision+2,(long)JObject.Parse(request.BodyJson)["expectedRevision"]);Assert.AreNotEqual("retained-original-key",request.Headers.Single(h=>h.Name=="Idempotency-Key").Value);Assert.AreEqual(800,(int)driver.HudState["trainHp"]);Assert.AreSame(driver,await Bounded(owner.PrepareAsync()));Assert.AreEqual(1,f.Transport.Requests.Count);}
 [Test] public async Task UnconfirmedOldRequestIsRecoveredWithExactKeyBeforeNewAttempt(){var f=new Fixture();await f.Seed(true);var old=JObject.Parse(f.Disk.Value.CipherTextBase64);var attempt=f.Attempt(f.Map(2));var result=await Restart(f.Delivery(attempt),attempt);Assert.AreEqual(2,f.Transport.Requests.Count);Assert.AreEqual((string)old["bodyJson"],f.Transport.Requests[0].BodyJson);Assert.AreEqual("retained-original-key",f.Transport.Requests[0].Headers.Single(h=>h.Name=="Idempotency-Key").Value);Assert.AreEqual(2,(long)JObject.Parse(f.Transport.Requests[1].BodyJson)["expectedGeneration"]);Assert.AreEqual(3,result.Generation);}
 [Test] public async Task UnclearOldRequestNeverCreatesAnotherKey(){var f=new Fixture();await f.Seed(true);var before=f.Disk.Value.CipherTextBase64;f.Transport.Reply=r=>Task.FromResult(GameApiAuthenticatedHttpResult.NetworkError("still-lost"));var attempt=f.Attempt();Assert.IsNull(await Restart(f.Delivery(attempt),attempt));Assert.AreEqual(1,f.Transport.Requests.Count);Assert.AreEqual(before,f.Disk.Value.CipherTextBase64);}
 [Test] public async Task SupersededOriginalRequestCannotBeReplacedAutomatically(){var f=new Fixture();await f.Seed(true);f.Transport.Reply=r=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(409,"{\"errorCode\":\"ordinary_node_restart_superseded\"}")));var attempt=f.Attempt();Assert.IsNull(await Restart(f.Delivery(attempt),attempt));Assert.AreEqual(1,f.Transport.Requests.Count);Assert.AreEqual("retained-original-key",(string)JObject.Parse(f.Disk.Value.CipherTextBase64)["key"]);Assert.IsTrue((bool)JObject.Parse(f.Disk.Value.CipherTextBase64)["invalidated"]);}
 [Test] public async Task OwnerChangeDuringConfirmationPersistsReceiptWithoutPublishing(){var f=new Fixture();var attempt=f.Attempt();f.Transport.Reply=r=>{f.Current=false;return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,f.Response(r))));};Assert.ThrowsAsync<InvalidOperationException>(async()=>await Restart(f.Delivery(attempt),attempt));Assert.AreEqual(1,f.Transport.Requests.Count);Assert.AreEqual(JTokenType.String,JObject.Parse(f.Disk.Value.CipherTextBase64)["responseJson"].Type);await Task.CompletedTask;}
 sealed class Journal:IRunOperationJournal {
  internal PendingRunOperation Row,Travel;
  public Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account)=>Task.FromResult<IReadOnlyList<PendingRunOperation>>(new[]{Row,Travel}.Where(r=>r!=null&&r.AccountId==account).ToArray());
  public Task<bool> PutAsync(PendingRunOperation op)=>throw new AssertionException("Re-entry must not create another ARRIVE or START.");
  public Task<bool> ConfirmAsync(PendingRunOperation op,string response)=>throw new AssertionException("Retained arrival is already confirmed.");
  public Task<bool> RemoveConfirmedAsync(string account,PendingRunKind kind,string key)=>throw new AssertionException("Original receipt must remain retained.");
 }
 sealed class MapWire:IRunApiHttpTransport {
  internal readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();internal Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;
  public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Requests.Add(r);return Reply(r);}
 }
 sealed class PublicFixture:IDisposable {
  internal readonly Fixture F=new Fixture();internal readonly MapWire MapHttp=new MapWire();internal readonly AccountScreenRuntime Screen;internal readonly DurableRunClient Client;
  internal GameApiAuthSessionSnapshot Session;internal long Generation=1;internal JObject Map;
  internal PublicFixture(bool arrivalTravel=false){
   Session=SupportInquiryTests.Session(F.Start.AccountId);Map=F.Map();
   MapHttp.Reply=r=>{Assert.AreEqual("GET",r.Method);Assert.That(r.EndpointPath,Does.EndWith("/operation-map"));return Task.FromResult(RunApiHttpResponse.Success(200,Map.ToString()));};
   var command=F.ArrivalCommand;
   if(arrivalTravel){
    // The node factory fixture intentionally strips native travel pins. This race
    // exercises the separately pinned synthetic travel path, never a production source.
    var route=command.Binding.Capture();var travelSample=(JObject)BRouteConsumerTests.Sample()["pendingBinding"];
    foreach(var key in new[]{"nativeContextId","departurePinHash","originalNativeOrigin"})route[key]=travelSample[key].DeepClone();
    foreach(var key in new[]{"accountId","runId","operationId","selectedRisk"})route["originalNativeOrigin"][key]=route[key].DeepClone();
    route.Remove("packetHash");route["packetHash"]=BDirectAttackInput.Digest(route).Substring(7);
    var binding=BRouteBinding.Read(route.ToString(Newtonsoft.Json.Formatting.None),(string)route["packetHash"],F.Start.AccountId,F.Start.RunId);
    var body=(JObject)command.Capture()["command"].DeepClone();body["evidenceRef"]="B_ARRIVAL:proof";command=BRouteCommand.FromOriginal(binding,body);
   }
   var journal=new Journal{Row=PendingRunOperation.ArriveMap(F.Start.AccountId,command,new AppBuildSnapshot("current-attempt-test",1),DateTimeOffset.UtcNow).Confirmed(F.ArrivalResponse.ToString())};
   if(arrivalTravel){
    var route=command.Binding.Capture();var request=BTravelCommand.Read(new JObject{["version"]=BTravelCommand.Version,["requestId"]="arrival-travel",["destinationEventId"]="destination",["nodeId"]=F.Node.NodeId,["mapRevision"]=command.Binding.MapRevision,["expectedRevision"]=0,["kind"]="ARRIVE"}.ToString());
    var wireRequest=new RunApiHttpRequest("POST","/fixture",request.BodyJson,null);
    var response=JObject.Parse((string)typeof(OrdinaryTravelPreparationTests).GetMethod("BoundaryPause",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{wireRequest}));
    response.Remove("pauseIntent");response["nodeId"]=F.Node.NodeId;response["mapRevision"]=command.Binding.MapRevision;
    var observation=response["arrival"]["observation"];observation["accountId"]=F.Start.AccountId;observation["runId"]=F.Start.RunId;observation["nodeId"]=F.Node.NodeId;observation["mapRevision"]=command.Binding.MapRevision;
    observation["nativeContextId"]=route["nativeContextId"].DeepClone();observation["departurePinHash"]=route["departurePinHash"].DeepClone();observation["operationBindingHash"]=route["bindingHash"].DeepClone();observation["destination"]["mapRevision"]=command.Binding.MapRevision;observation["destination"]["edgeId"]=route["pendingEdgeId"].DeepClone();
    foreach(var key in new[]{"accountId","runId","nativeContextId","originalEventId"})Assert.AreEqual(JTokenType.String,observation[key].Type,"Synthetic travel requires its original "+key);
    journal.Travel=PendingRunOperation.Travel(F.Start.AccountId,F.Start.RunId,request,new AppBuildSnapshot("current-attempt-test",1),DateTimeOffset.UtcNow).Confirmed(response.ToString(Newtonsoft.Json.Formatting.None));
   }
   Client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(MapHttp),journal,()=>Session,()=>Generation);
   Screen=new AccountScreenRuntime(MapHttp,null,()=>Session,()=>Generation);Screen.BindRunRecovery(Client);Screen.SynchronizeSession();
   var raw=FirstPlayableScalingTests.Fixture(staticSource:true).Start;Assert.AreEqual(F.Start.StartSnapshotHash,OrdinaryFiveStartInput.Read(raw,F.Start.AccountId,F.Start.RunId).StartSnapshotHash);
   var receipt=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=F.Start.RunId,["runStatus"]="ACTIVE",["startResponse"]=raw,["errorCode"]=null};
   string retained=receipt.ToString(Newtonsoft.Json.Formatting.None);Assert.LessOrEqual(retained.Length,65536,"The fixture must obey the existing retained START limit.");
   typeof(AccountScreenRuntime).GetProperty("Pending").SetValue(Screen,new PendingAccountAction(F.Start.AccountId,"departure-start",(string)raw["operationPreviewId"],0,"public-entry",new JObject{["operationPreviewId"]=raw["operationPreviewId"]}.ToString(Newtonsoft.Json.Formatting.None),retained));
  }
  internal Task<CombatSceneDriver> Prepare()=>Screen.PrepareOrdinaryNodeCombatAsync(F.Start.RunId,null,(node,scope)=>new OrdinaryNodeRestartDelivery(node,F.Disk,F.Transport,scope));
  public void Dispose(){Screen.Dispose();Client.Dispose();}
 }
 [TestCase(false)][TestCase(true)] public async Task PublicEntryUsesOneNecessaryMapReadOrExistingCacheAndNoConfirmationGet(bool cached){
  using var f=new PublicFixture();if(cached)await Bounded(f.Screen.ReadOrdinaryMapAsync(f.F.Start.RunId));int reads=f.MapHttp.Requests.Count;
  var driver=await Bounded(f.Prepare());Assert.AreEqual(cached?reads:reads+1,f.MapHttp.Requests.Count);Assert.AreEqual(1,f.MapHttp.Requests.Count);Assert.AreEqual(1,f.F.Transport.Requests.Count);Assert.AreEqual(3,(long)JObject.Parse(f.F.Transport.Requests[0].BodyJson)["expectedGeneration"]);
  Assert.AreSame(driver,await Bounded(f.Prepare()));Assert.AreEqual(1,f.MapHttp.Requests.Count);Assert.AreEqual(1,f.F.Transport.Requests.Count);
  f.Screen.ReleaseOrdinaryCombatPreparation();var next=await Bounded(f.Prepare());Assert.AreNotSame(driver,next);Assert.AreEqual(1,f.MapHttp.Requests.Count);Assert.AreEqual(1,f.F.Transport.Requests.Count,"Cancelling an unstarted preparation must reuse the accepted attempt.");
 }
 [TestCase("null")][TestCase("pending")][TestCase("inactive")] public void PublicEntryRejectsUnavailableProjectionBeforeRestartOrHpCreation(string reason){
  using var f=new PublicFixture();if(reason=="null")f.Map["currentAttempt"]=null;if(reason=="pending")f.Map["progress"]["blockedByPendingTransactions"]=true;if(reason=="inactive"){f.Map["currentAttempt"]=null;f.Map["runStatus"]="FINISH_PENDING";}
  Assert.ThrowsAsync<OrdinaryNodeStartUnavailableException>(()=>Bounded(f.Prepare()));Assert.AreEqual(1,f.MapHttp.Requests.Count);Assert.Zero(f.F.Transport.Requests.Count);Assert.IsNull(typeof(AccountScreenRuntime).GetField("ordinaryNodePreparation",Hidden).GetValue(f.Screen));
 }
 [Test] public void PublicEntryRejectsAccountReplacementDuringExistingMapRead(){using var f=new PublicFixture();f.MapHttp.Reply=r=>{f.Session=SupportInquiryTests.Session("other-account");f.Generation++;return Task.FromResult(RunApiHttpResponse.Success(200,f.Map.ToString()));};Assert.ThrowsAsync<InvalidOperationException>(()=>Bounded(f.Prepare()));Assert.AreEqual(1,f.MapHttp.Requests.Count);Assert.Zero(f.F.Transport.Requests.Count);}
 [Test] public async Task ConcurrentPublicEntryWaitingOnSameMapKeepsOneOwnerAndRestart(){
  using var f=new PublicFixture(true);var mapGate=new TaskCompletionSource<RunApiHttpResponse>();f.MapHttp.Reply=r=>mapGate.Task;
  var restartGate=new TaskCompletionSource<GameApiAuthenticatedHttpResult>();f.F.Transport.Reply=r=>restartGate.Task;try{
  var first=f.Prepare();var second=f.Prepare();Assert.AreEqual(1,f.MapHttp.Requests.Count);Assert.IsFalse(first.IsCompleted);Assert.IsFalse(second.IsCompleted);
  mapGate.SetResult(RunApiHttpResponse.Success(200,f.Map.ToString()));
  for(int i=0;i<8&&f.F.Transport.Requests.Count==0;i++)await Task.Yield();Assert.AreEqual(1,f.F.Transport.Requests.Count);
  restartGate.SetResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,f.F.Response(f.F.Transport.Requests[0]))));
  var one=await Bounded(first);var two=await Bounded(second);Assert.AreSame(one,two);Assert.IsFalse(one.IsRetired);
  var retained=(OrdinaryNodePreparationOwner)typeof(AccountScreenRuntime).GetField("ordinaryNodePreparation",Hidden).GetValue(f.Screen);
  Assert.AreSame(one,await Bounded(retained.PrepareAsync()));Assert.AreEqual(1,f.F.Transport.Requests.Count);Assert.AreEqual(1,f.MapHttp.Requests.Count);
 }finally{mapGate.TrySetCanceled();restartGate.TrySetCanceled();}
 }
 [Test] public async Task CancelBeforeMapResponseDoesNotSendRestartAndReselectionReusesMap(){
  using var f=new PublicFixture();var mapGate=new TaskCompletionSource<RunApiHttpResponse>();f.MapHttp.Reply=r=>mapGate.Task;try{
  var old=f.Prepare();f.Screen.ReleaseOrdinaryCombatPreparation();mapGate.SetResult(RunApiHttpResponse.Success(200,f.Map.ToString()));
  Assert.CatchAsync<InvalidOperationException>(()=>Bounded(old));Assert.Zero(f.F.Transport.Requests.Count);
  var next=await Bounded(f.Prepare());Assert.IsFalse(next.IsRetired);Assert.AreEqual(1,f.MapHttp.Requests.Count);Assert.AreEqual(1,f.F.Transport.Requests.Count);
 }finally{mapGate.TrySetCanceled();}
 }
 [Test] public async Task PublicLateCancelledAwaiterCannotRetireReselectedDriver(){
  using var f=new PublicFixture();var response=new TaskCompletionSource<GameApiAuthenticatedHttpResult>();f.F.Transport.Reply=r=>response.Task;try{
  var old=f.Prepare();Assert.AreEqual(1,f.F.Transport.Requests.Count);f.Screen.ReleaseOrdinaryCombatPreparation();var selected=f.Prepare();
  response.SetResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,f.F.Response(f.F.Transport.Requests.Single()))));
  Assert.CatchAsync<InvalidOperationException>(()=>Bounded(old));var driver=await Bounded(selected);Assert.IsFalse(driver.IsRetired);Assert.AreSame(driver,await Bounded(f.Prepare()));Assert.AreEqual(1,f.F.Transport.Requests.Count);Assert.AreEqual(1,f.MapHttp.Requests.Count);
 }finally{response.TrySetCanceled();}
 }
 [TestCase(false)][TestCase(true)] public async Task CancelWhileWaitingForDeliveryGateSendsOnlyForReselectedIntent(bool reselect){
  var f=new Fixture();using var owner=f.Preparation(f.Attempt());
  // Contend the real delivery boundary before any request/key has been created.
  var gate=(System.Threading.SemaphoreSlim)typeof(OrdinaryNodeRestartDelivery).GetField("Gate",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  bool held=gate.Wait(0);Assert.IsTrue(held,"A preceding test must release its delivery gate");
  try{
   var old=owner.PrepareAsync();Assert.IsFalse(old.IsCompleted);Assert.Zero(f.Transport.Requests.Count);
   owner.CancelPreparation();var selected=reselect?owner.PrepareAsync():null;
   gate.Release();held=false;
   Exception cancelled=null;try{await Bounded(old);}catch(Exception error){cancelled=error;}
   Assert.IsInstanceOf<InvalidOperationException>(cancelled,"The obsolete preparation must finish without blocking Unity continuations");
   if(reselect){var driver=await Bounded(selected);Assert.IsFalse(driver.IsRetired);Assert.AreEqual(1,f.Transport.Requests.Count);Assert.AreEqual(4,owner.CurrentAttempt.Generation);}
   else{Assert.Zero(f.Transport.Requests.Count,"Cancellation before transport must not issue a restart");Assert.IsNull(f.Disk.Value,"No unsent restart key should be manufactured");Assert.AreEqual(3,owner.CurrentAttempt.Generation);}
  }finally{if(held)gate.Release();}
 }

}
