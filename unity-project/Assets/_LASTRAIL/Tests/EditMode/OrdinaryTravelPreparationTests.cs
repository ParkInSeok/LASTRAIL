using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using LastRail.Presentation.MissionShell;

namespace LastRail.Tests.EditMode {
 public sealed class OrdinaryTravelPreparationTests {
  sealed class Storage:IAndroidKeystoreNativeStoragePort {
   AndroidKeystoreEncryptedTokenPayload saved;
   public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>true;public bool UsedPlayerPrefsFallback=>false;public string ProviderName=>"TEST_ONLY";
   public AndroidKeystoreEncryptedTokenPayload Encrypt(string value)=>new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(Encoding.UTF8.GetBytes(value)),"test");
   public string Decrypt(AndroidKeystoreEncryptedTokenPayload value)=>Encoding.UTF8.GetString(Convert.FromBase64String(value.CipherTextBase64));
   public bool Save(AndroidKeystoreEncryptedTokenPayload value){saved=value;return true;}
   public AndroidKeystoreEncryptedTokenPayload Load()=>saved;
   public bool Clear(){saved=null;return true;}
  }
  sealed class Wire:IRunApiHttpTransport {
   public int Count;
   public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply=r=>Task.FromResult(Response(r));
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Count++;return Reply(request);}
  }
  static BTravelCommand Command()=>BTravelCommand.Read(new JObject{["version"]=BTravelCommand.Version,["requestId"]="first",["destinationEventId"]="destination",["nodeId"]="node",["mapRevision"]=2,["expectedRevision"]=0,["kind"]="TRAVEL_THROTTLE",["bps"]=5000}.ToString());
  static RunApiHttpResponse Response(RunApiHttpRequest request){
   var command=JObject.Parse(request.BodyJson);var kind=(string)command["kind"];
   return RunApiHttpResponse.Success(200,new JObject{["version"]=BTravelCommand.Version,["requestId"]=command["requestId"],["destinationEventId"]="destination",["nodeId"]="node",["mapRevision"]=2,
    ["revision"]=(long)command["expectedRevision"]+1,["acceptedAtMs"]=1000,
    ["state"]=new JObject{["tick"]=0,["distanceMm"]=0,["remainderUnits"]=0,["debtTicks"]=0,["paused"]=kind=="RECOVER"||kind=="PAUSE"&&(bool)command["value"],["throttleBps"]=(long?)command["bps"]??5000}}.ToString());
  }
  static DurableRunClient Client(Storage storage,Wire wire){var session=SupportInquiryTests.Session("a");return new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),new EncryptedRunOperationJournal(storage),()=>session,()=>1,()=>new AppBuildSnapshot("travel-test",1));}
  static BOperationDestinationIntent Destination(long revision=1,string key="destination")=>new BOperationDestinationIntent("a","run",key,revision,"edge");
  static Task<BTravelReceipt> Confirm(OrdinaryTravelOwner owner,BOperationDestinationIntent intent,string node="node"){
   var method=typeof(OrdinaryTravelOwner).GetMethod("ConfirmDestinationAsync");Assert.NotNull(method,"The product owner must consume the original destination response.");
   return (Task<BTravelReceipt>)method.Invoke(owner,new object[]{intent,node});
  }
  static BOperationDestinationReceipt DestinationReceipt(OrdinaryTravelOwner owner){
   var property=typeof(OrdinaryTravelOwner).GetProperty("DestinationReceipt");Assert.NotNull(property);return (BOperationDestinationReceipt)property.GetValue(owner);
  }
  static bool NeedsRecovery(OrdinaryTravelOwner owner){var property=typeof(OrdinaryTravelOwner).GetProperty("NeedsRecovery");Assert.NotNull(property);return (bool)property.GetValue(owner);}
  static bool ContinueAfterArrival(OrdinaryTravelOwner owner){var method=typeof(OrdinaryTravelOwner).GetMethod("ContinueAfterArrival");Assert.NotNull(method);return (bool)method.Invoke(owner,null);}
  [Test] public async Task DestinationLostTravelReplyExposesRecoveryUntilOriginalAccepted(){
   var wire=new Wire{Reply=r=>Task.FromResult(ScopedResponse(r))};using var client=Client(new Storage(),wire);using var owner=new OrdinaryTravelOwner(client,"a","run",()=>true);
   Assert.IsFalse(NeedsRecovery(owner));await Confirm(owner,Destination());Assert.IsFalse(NeedsRecovery(owner));
   wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(503,"{\"errorCode\":\"outcome_unknown\"}"));Assert.IsNull(await owner.SetThrottleAsync(6000));Assert.IsTrue(NeedsRecovery(owner));
   wire.Reply=r=>Task.FromResult(ScopedResponse(r));Assert.NotNull(await owner.RecoverAsync());Assert.IsFalse(NeedsRecovery(owner));
  }
  [TestCase(false)][TestCase(true)] public async Task ArrivalContinuationRequiresCommittedNodeStartAndSendsNoTravelResume(bool supplied){
   var route=MapRoute();var wire=new Wire();using var client=Client(new Storage(),wire);await SeedMapArrival(client,wire,route);
   using var owner=new OrdinaryTravelOwner(client,"a",route.RunId,()=>true);await owner.RestoreAsync();Assert.IsFalse(ContinueAfterArrival(owner));Assert.IsTrue(owner.PauseRequested);
   var delayed=new TaskCompletionSource<RunApiHttpResponse>();RunApiHttpRequest pending=null;wire.Reply=r=>{pending=r;return delayed.Task;};var commit=owner.CommitMapArrivalAsync(route);
   Assert.IsFalse(ContinueAfterArrival(owner));var response=JObject.Parse(MapResponse(pending).BodyJson);
   if(supplied){
    // Rebind the explicitly unsupported transport fixture to this pinned arrival; it cannot authorize combat.
    var entry=(JObject)OrdinaryNodeStartWireTests.Fixture().Response["receipt"]["nodeStart"].DeepClone();var snapshot=(JObject)entry["snapshot"];
    entry["runId"]=route.RunId;entry["nodeId"]=route.Capture()["pendingNode"]["nodeId"];entry["entryMapRevision"]=route.MapRevision+1;
    foreach(var key in new[]{"accountId","runId","nodeId","entryMapRevision"})snapshot[key]=entry[key].DeepClone();
    snapshot["startSnapshotHash"]=route.Capture()["startSnapshotHash"];entry["snapshotHash"]=LastRail.Battle.BDirectAttackInput.Digest(snapshot).Substring(7);response["receipt"]["nodeStart"]=entry;
   }
   delayed.SetResult(RunApiHttpResponse.Success(200,response.ToString()));Assert.NotNull(await commit);
   Assert.AreEqual(supplied,ContinueAfterArrival(owner));Assert.AreEqual(!supplied,owner.PauseRequested);Assert.AreEqual(2,wire.Count);
   if(supplied){Assert.IsTrue(ContinueAfterArrival(owner));await owner.PauseAsync();Assert.IsTrue(owner.PauseRequested);Assert.AreEqual(2,wire.Count);}
  }
  static RunApiHttpResponse DestinationResponse(RunApiHttpRequest request,bool initial=true){
   var body=JObject.Parse(request.BodyJson);var key=request.Headers.Single(h=>h.Name=="Idempotency-Key").Value;
   var p=new JObject{["version"]="operation.map-destination.v1",["requestId"]=key,["revision"]=(long)body["expectedRevision"]+1,["phase"]="TRAVELLING",["pendingEdgeId"]=body["edgeId"]};
   if(initial)p["travelInitial"]=new JObject{["version"]=BTravelCommand.Version,["requestId"]=key,["destinationEventId"]=key,["nodeId"]="node",["mapRevision"]=p["revision"],["revision"]=0,["acceptedAtMs"]=1000,
    ["state"]=new JObject{["tick"]=0,["distanceMm"]=0,["remainderUnits"]=0,["debtTicks"]=0,["paused"]=false,["throttleBps"]=5000}};
   return RunApiHttpResponse.Success(200,p.ToString());
  }
  static RunApiHttpResponse ScopedResponse(RunApiHttpRequest request){
   if(request.EndpointPath.EndsWith("/operation-map/destination"))return DestinationResponse(request);
   var p=JObject.Parse(Response(request).BodyJson);var command=JObject.Parse(request.BodyJson);
   foreach(var key in new[]{"destinationEventId","nodeId","mapRevision"})p[key]=command[key].DeepClone();return RunApiHttpResponse.Success(200,p.ToString());
  }
  [Test] public async Task DestinationFreshInitialEnablesFirstInputWithoutRecoveryOrResume(){
   var wire=new Wire{Reply=r=>Task.FromResult(ScopedResponse(r))};using var client=Client(new Storage(),wire);using var owner=new OrdinaryTravelOwner(client,"a","run",()=>true);
   var first=await Confirm(owner,Destination());Assert.IsFalse(first.State.Paused);Assert.AreEqual(0,first.Revision);Assert.AreEqual(1,wire.Count);
   Assert.NotNull(DestinationReceipt(owner));Assert.AreSame(first,await owner.RestoreAsync());Assert.AreSame(first,await Confirm(owner,Destination()));Assert.AreEqual(1,wire.Count);
   var changed=await owner.SetThrottleAsync(6000);Assert.AreEqual(6000,changed.State.ThrottleBps);Assert.AreEqual(2,wire.Count);
  }
  [Test] public async Task DestinationMissingInitialRetainsAcceptanceAndBlocksNewKey(){
   var wire=new Wire{Reply=r=>Task.FromResult(DestinationResponse(r,false))};using var client=Client(new Storage(),wire);using var owner=new OrdinaryTravelOwner(client,"a","run",()=>true);
   Assert.IsNull(await Confirm(owner,Destination()));Assert.NotNull(DestinationReceipt(owner));Assert.IsNull(owner.Latest);
   Assert.IsNull(await Confirm(owner,Destination(1,"another")));Assert.IsNull(await owner.SetThrottleAsync(6000));Assert.IsNull(await owner.RestoreAsync());Assert.AreEqual(1,wire.Count);
  }
  [Test] public async Task DestinationMissingInitialAfterRestartBlocksEvenNewerRequestedKey(){
   var storage=new Storage();var wire=new Wire{Reply=r=>Task.FromResult(DestinationResponse(r,false))};
   using(var client=Client(storage,wire))using(var owner=new OrdinaryTravelOwner(client,"a","run",()=>true))await Confirm(owner,Destination());
   using var restoredClient=Client(storage,wire);using var restored=new OrdinaryTravelOwner(restoredClient,"a","run",()=>true);
   Assert.IsNull(await Confirm(restored,Destination(3,"newer-key")));Assert.AreEqual("destination",DestinationReceipt(restored).Intent.CommandId);
   Assert.IsNull(restored.Latest);Assert.AreEqual(1,wire.Count);
  }
  [Test] public async Task DestinationSingleFlightRetainsPauseDuringAcceptance(){
   var delayed=new TaskCompletionSource<RunApiHttpResponse>();RunApiHttpRequest pending=null;var wire=new Wire{Reply=r=>{pending=r;return delayed.Task;}};
   using var client=Client(new Storage(),wire);using var owner=new OrdinaryTravelOwner(client,"a","run",()=>true);
   var first=Confirm(owner,Destination());Assert.AreSame(first,Confirm(owner,Destination(1,"other")));var pause=owner.PauseAsync();Assert.IsTrue(owner.PauseRequested);
   wire.Reply=r=>Task.FromResult(ScopedResponse(r));delayed.SetResult(DestinationResponse(pending));await first;await pause;
   Assert.IsTrue(owner.Latest.State.Paused);Assert.AreEqual(2,wire.Count);
  }
  [Test] public async Task DestinationLateAcceptanceCannotPublishIntoOldOwner(){
   var delayed=new TaskCompletionSource<RunApiHttpResponse>();RunApiHttpRequest pending=null;var wire=new Wire{Reply=r=>{pending=r;return delayed.Task;}};
   using var client=Client(new Storage(),wire);bool current=true;using var owner=new OrdinaryTravelOwner(client,"a","run",()=>current);
   var first=Confirm(owner,Destination());current=false;delayed.SetResult(DestinationResponse(pending));Assert.ThrowsAsync<InvalidOperationException>(async()=>await first);
   Assert.IsNull(owner.Latest);Assert.IsNull(DestinationReceipt(owner));Assert.NotNull(client.Operations.Single().AcceptedResponseJson);
  }
  [Test] public async Task DestinationRestartRecoversBeforeExplicitResume(){
   var storage=new Storage();var wire=new Wire{Reply=r=>Task.FromResult(ScopedResponse(r))};
   using(var client=Client(storage,wire))using(var owner=new OrdinaryTravelOwner(client,"a","run",()=>true))await Confirm(owner,Destination());
   using var restoredClient=Client(storage,wire);using var restored=new OrdinaryTravelOwner(restoredClient,"a","run",()=>true);
   var receipt=await restored.RestoreAsync();Assert.IsTrue(receipt.State.Paused);Assert.NotNull(DestinationReceipt(restored));Assert.AreEqual(2,wire.Count);
   Assert.AreSame(receipt,await restored.RestoreAsync());Assert.IsFalse((await restored.ResumeAsync()).State.Paused);Assert.AreEqual(3,wire.Count);
  }
  [Test] public async Task DestinationLaterMapRevisionSupersedesOldTravelOnRestore(){
   var wire=new Wire{Reply=r=>Task.FromResult(ScopedResponse(r))};using var client=Client(new Storage(),wire);await client.TravelAsync("run",Command());
   using(var first=new OrdinaryTravelOwner(client,"a","run",()=>true))await Confirm(first,Destination(3,"later-destination"));
   using var restored=new OrdinaryTravelOwner(client,"a","run",()=>true);var receipt=await restored.RestoreAsync();
   Assert.AreEqual("later-destination",receipt.DestinationEventId);Assert.AreEqual(4,receipt.MapRevision);Assert.IsTrue(receipt.State.Paused);Assert.AreEqual(3,wire.Count);
  }
  [Test] public async Task DestinationOriginalInitialCannotOverwriteNewerConfirmedTravel(){
   var storage=new Storage();var wire=new Wire{Reply=r=>Task.FromResult(ScopedResponse(r))};
   using(var client=Client(storage,wire))using(var owner=new OrdinaryTravelOwner(client,"a","run",()=>true)){
    await Confirm(owner,Destination());await owner.SetThrottleAsync(6000);
   }
   wire.Reply=r=>{var command=JObject.Parse(r.BodyJson);Assert.AreEqual(1,(long)command["expectedRevision"]);var p=JObject.Parse(ScopedResponse(r).BodyJson);p["state"]["throttleBps"]=6000;return Task.FromResult(RunApiHttpResponse.Success(200,p.ToString()));};
   using var restoredClient=Client(storage,wire);using var restored=new OrdinaryTravelOwner(restoredClient,"a","run",()=>true);
   var receipt=await restored.RestoreAsync();Assert.AreEqual(2,receipt.Revision);Assert.AreEqual(6000,receipt.State.ThrottleBps);Assert.AreEqual(3,wire.Count);
  }
  [Test] public async Task DestinationInvalidInitialRetainsReceiptAndDisablesCommands(){
   var wire=new Wire{Reply=r=>{var p=JObject.Parse(DestinationResponse(r).BodyJson);p["travelInitial"]["nodeId"]="another-node";return Task.FromResult(RunApiHttpResponse.Success(200,p.ToString()));}};
   using var client=Client(new Storage(),wire);using var owner=new OrdinaryTravelOwner(client,"a","run",()=>true);
   Assert.ThrowsAsync<ArgumentException>(async()=>await Confirm(owner,Destination()));Assert.NotNull(DestinationReceipt(owner));Assert.IsNull(owner.Latest);
   Assert.IsNull(await owner.SetThrottleAsync(6000));Assert.IsNull(await Confirm(owner,Destination(1,"new-key")));Assert.AreEqual(1,wire.Count);
  }
  [Test] public async Task MissingAuthorityDoesNotInventInitialStateOrSendRequest(){
   var wire=new Wire();using var client=Client(new Storage(),wire);using var owner=new OrdinaryTravelOwner(client,"a","run",()=>true);
   Assert.IsNull(await owner.RestoreAsync());Assert.IsNull(owner.Latest);Assert.IsNull(await owner.SetThrottleAsync(5000));Assert.AreEqual(0,wire.Count);
  }
  [Test] public async Task FreshOwnerRecoversConfirmedStateOnceAndReentryReusesIt(){
   var wire=new Wire();using var client=Client(new Storage(),wire);Assert.NotNull(await client.TravelAsync("run",Command()));
   using var owner=new OrdinaryTravelOwner(client,"a","run",()=>true);var recovered=await owner.RestoreAsync();
   Assert.IsTrue(recovered.State.Paused);Assert.AreEqual(2,recovered.Revision);Assert.AreSame(recovered,await owner.RestoreAsync());Assert.AreEqual(2,wire.Count);
   Assert.IsNull(await owner.SetThrottleAsync(5000));Assert.AreEqual(2,wire.Count);
   Assert.IsNull(await owner.SetThrottleAsync(6000));Assert.AreEqual(2,wire.Count);
  }
  [Test] public async Task PendingResumeRetainsPauseIntentAndSuppressesSecondInput(){
   var wire=new Wire();using var client=Client(new Storage(),wire);await client.TravelAsync("run",Command());
   using var owner=new OrdinaryTravelOwner(client,"a","run",()=>true);await owner.RestoreAsync();
   var delayed=new TaskCompletionSource<RunApiHttpResponse>();RunApiHttpRequest pending=null;
   wire.Reply=r=>{pending=r;return delayed.Task;};var resume=owner.ResumeAsync();
   var pause=owner.PauseAsync();Assert.IsTrue(owner.PauseRequested);Assert.IsNull(await owner.SetThrottleAsync(6000));
   wire.Reply=r=>Task.FromResult(Response(r));delayed.SetResult(Response(pending));await resume;await pause;
   Assert.IsTrue(owner.Latest.State.Paused);Assert.AreEqual(4,wire.Count);Assert.AreEqual(1,client.Operations.Count);
  }
  [Test] public async Task LateResponseCannotPublishIntoReplacedOwner(){
   var wire=new Wire();using var client=Client(new Storage(),wire);await client.TravelAsync("run",Command());
   bool current=true;using var owner=new OrdinaryTravelOwner(client,"a","run",()=>current);await owner.RestoreAsync();var baseline=owner.Latest;
   var delayed=new TaskCompletionSource<RunApiHttpResponse>();RunApiHttpRequest pending=null;
   wire.Reply=r=>{pending=r;return delayed.Task;};var resume=owner.ResumeAsync();current=false;delayed.SetResult(Response(pending));
   Assert.ThrowsAsync<InvalidOperationException>(async()=>await resume);Assert.AreSame(baseline,owner.Latest);
  }
  [Test] public async Task LostResumeRecoversOriginalBeforeNewRecoveryAndNeverAutomaticallyResumes(){
   var wire=new Wire();using var client=Client(new Storage(),wire);await client.TravelAsync("run",Command());
   using(var first=new OrdinaryTravelOwner(client,"a","run",()=>true)){
    await first.RestoreAsync();wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(503,"{\"errorCode\":\"outcome_unknown\"}"));
    Assert.IsNull(await first.ResumeAsync());
   }
   var original=client.Operations.Single().BodyJson;var sent=new System.Collections.Generic.List<string>();
   wire.Reply=r=>{sent.Add(r.BodyJson);return Task.FromResult(Response(r));};
   using var reopened=new OrdinaryTravelOwner(client,"a","run",()=>true);var result=await reopened.RestoreAsync();
   Assert.AreEqual(original,sent[0]);Assert.AreEqual("RECOVER",(string)JObject.Parse(sent[1])["kind"]);
   Assert.AreEqual(2,sent.Count);Assert.IsTrue(result.State.Paused);
  }
  [Test] public async Task KnownConflictRetriesOriginalThenRecoversLatestWithoutSecondRecovery(){
   var wire=new Wire();using var client=Client(new Storage(),wire);await client.TravelAsync("run",Command());
   var sent=new System.Collections.Generic.List<string>();
   wire.Reply=r=>{
    sent.Add(r.BodyJson);var command=JObject.Parse(r.BodyJson);
    if((long)command["expectedRevision"]!=1)return Task.FromResult(Response(r));
    var current=JObject.Parse(Response(r).BodyJson);current["requestId"]="other-accepted";current["revision"]=3;
    return Task.FromResult(RunApiHttpResponse.Success(409,new JObject{["accepted"]=false,["errorCode"]="combat_b_travel_revision_conflict",["current"]=current}.ToString()));
   };
   using var owner=new OrdinaryTravelOwner(client,"a","run",()=>true);
   Assert.IsNull(await owner.RestoreAsync());var rejected=client.Operations.Single().BodyJson;
   var recovered=await owner.RecoverAsync();Assert.AreEqual(4,recovered.Revision);Assert.IsTrue(recovered.State.Paused);
   Assert.AreEqual(rejected,sent[1]);Assert.AreEqual(3,(long)JObject.Parse(sent[2])["expectedRevision"]);
   Assert.AreEqual("RECOVER",(string)JObject.Parse(sent[2])["kind"]);Assert.AreEqual(3,sent.Count);
  }
  static string BoundaryPause(RunApiHttpRequest request){
   var p=JObject.Parse(Response(request).BodyJson);string hash=new string('a',64);
   p["pauseIntent"]=true;p["state"]["paused"]=false;p["state"]["tick"]=4;
   p["arrival"]=new JObject{["version"]=BArrivalReportRequest.Version,["evidenceRef"]="B_ARRIVAL:proof",["requestId"]=p["requestId"],
    ["arrivalAccepted"]=true,["progressAuthorized"]=true,["combatTruth"]="NOT_PROVEN",["verificationLevel"]="ORIGINAL_PINNED_TRAVEL_COMMAND_REPLAY",["missingSources"]=new JArray(),
    ["observation"]=new JObject{["version"]="combat.b-boundary-arrival-observation.v1.20261020",["accountId"]="a",["runId"]="run",["nodeId"]="node",["mapRevision"]=2,
     ["nativeContextId"]="native",["departurePinHash"]=hash,["nodeSourceHash"]=hash,["operationBindingHash"]=hash,["evidenceRef"]="B_ARRIVAL:proof",["originalEventId"]="arrival",
     ["sequence"]=3,["sourceRevision"]=3,["checkpointHash"]="sha256:"+hash,["destination"]=new JObject{["eventId"]="destination",["mapRevision"]=2,["receiptHash"]=hash,["edgeId"]="edge"},
     ["arrival"]=new JObject{["eventId"]="arrival",["sequence"]=3,["tick"]=4},["travelStopped"]=true,["combatTruth"]="NOT_PROVEN"}};
   return p.ToString();
  }
  [Test] public async Task SavedBoundaryArrivalPreservesEvidenceAndPauseIntentWithoutReplacingJournalReceipt(){
   var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,BoundaryPause(r)))};
   using var client=Client(new Storage(),wire);var command=JObject.Parse(Command().BodyJson);command.Remove("bps");command["kind"]="PAUSE";command["value"]=true;
   Assert.NotNull(await client.TravelAsync("run",BTravelCommand.Read(command.ToString())));
   var original=client.Operations.Single();
   wire.Reply=r=>throw new InvalidOperationException("Confirmed arrival evidence must not be replaced by another travel request.");
   using var owner=new OrdinaryTravelOwner(client,"a","run",()=>true);var result=await owner.RestoreAsync();
   Assert.IsFalse(result.IsCurrentMap);Assert.AreEqual("B_ARRIVAL:proof",result.ArrivalEvidenceRef);Assert.IsTrue(owner.PauseRequested);
   Assert.AreSame(result,await owner.RestoreAsync());Assert.AreSame(result,await owner.RecoverAsync());Assert.AreEqual(1,wire.Count);
   Assert.AreEqual(original.BodyJson,client.Operations.Single().BodyJson);Assert.AreEqual(original.AcceptedResponseJson,client.Operations.Single().AcceptedResponseJson);
  }
  [Test] public void ProductOwnerExposesExplicitTravelRecoveryWithoutReplacingCombatPreparation(){
   var runtime=typeof(AccountScreenRuntime);
   Assert.NotNull(runtime.GetMethod("PrepareOrdinaryTravelAsync"),"The existing lobby entry must recover original travel commands before preparing combat.");
   Assert.NotNull(runtime.GetMethod("ReadOrdinaryTravel"));
   Assert.NotNull(runtime.GetMethod("PrepareOrdinaryFiveCombatAsync"));
  }
  static BRouteBinding MapRoute()=>BRouteConsumerTests.Read((JObject)BRouteConsumerTests.Sample()["pendingBinding"].DeepClone());
  static async Task SeedMapArrival(DurableRunClient client,Wire wire,BRouteBinding route){
   var p=route.Capture();var command=JObject.Parse(Command().BodyJson);command.Remove("bps");command["kind"]="PAUSE";command["value"]=true;
   command["nodeId"]=p["pendingNode"]["nodeId"].DeepClone();command["mapRevision"]=route.MapRevision;
   wire.Reply=r=>{
    var receipt=JObject.Parse(BoundaryPause(r));receipt["nodeId"]=command["nodeId"].DeepClone();receipt["mapRevision"]=route.MapRevision;
    var o=receipt["arrival"]["observation"];o["runId"]=route.RunId;o["nodeId"]=command["nodeId"].DeepClone();o["mapRevision"]=route.MapRevision;
    o["nativeContextId"]=p["nativeContextId"].DeepClone();o["departurePinHash"]=p["departurePinHash"].DeepClone();o["operationBindingHash"]=p["bindingHash"].DeepClone();
    o["destination"]["mapRevision"]=route.MapRevision;o["destination"]["edgeId"]=p["pendingEdgeId"].DeepClone();
    return Task.FromResult(RunApiHttpResponse.Success(200,receipt.ToString()));
   };
   Assert.NotNull(await client.TravelAsync(route.RunId,BTravelCommand.Read(command.ToString())));
  }
  static RunApiHttpResponse MapResponse(RunApiHttpRequest request){
   var command=JObject.Parse(request.BodyJson)["command"];
   return RunApiHttpResponse.Success(200,new JObject{["version"]=BRouteBinding.Version,["committed"]=true,["receipt"]=new JObject{
    ["version"]="operation.map-receipt.v1",["eventId"]=command["eventId"].DeepClone(),["revision"]=(long)command["expectedRevision"]+1,["verificationId"]="verified-arrival",
    ["state"]=new JObject{["version"]="operation.map-progress.v1",["traversedEdgeIds"]=new JArray(command["edgeId"].DeepClone()),["pendingEdgeId"]=null,["phase"]="ACTIVE",["holdIds"]=new JArray()}}}.ToString());
  }
  [Test] public async Task MapArrivalConsumesExactEvidenceOnceAndKeepsTravelPauseReceipt(){
   var route=MapRoute();var wire=new Wire();using var client=Client(new Storage(),wire);await SeedMapArrival(client,wire,route);
   wire.Reply=r=>Task.FromResult(MapResponse(r));using var owner=new OrdinaryTravelOwner(client,"a",route.RunId,()=>true);
   var result=await owner.CommitMapArrivalAsync(route);Assert.IsTrue(result.Committed);Assert.IsNull(result.NodeStart);Assert.IsTrue(owner.PauseRequested);
   Assert.AreEqual(2,client.Operations.Count);Assert.AreEqual(2,wire.Count);Assert.AreSame(result,await owner.CommitMapArrivalAsync(route));Assert.AreEqual(2,wire.Count);
   Assert.AreEqual("B_ARRIVAL:proof",owner.Latest.ArrivalEvidenceRef);
  }
  [Test] public async Task MapArrivalLostReplyRecoversSameKeyAndBindingFromJournal(){
   var route=MapRoute();var wire=new Wire();using var client=Client(new Storage(),wire);await SeedMapArrival(client,wire,route);
   string original=null;wire.Reply=r=>{original=r.BodyJson;return Task.FromResult(RunApiHttpResponse.Success(503,"{\"errorCode\":\"outcome_unknown\"}"));};
   using(var owner=new OrdinaryTravelOwner(client,"a",route.RunId,()=>true))Assert.IsNull(await owner.CommitMapArrivalAsync(route));
   wire.Reply=r=>{Assert.AreEqual(original,r.BodyJson);return Task.FromResult(MapResponse(r));};
   using var reopened=new OrdinaryTravelOwner(client,"a",route.RunId,()=>true);Assert.IsTrue((await reopened.CommitMapArrivalAsync(null)).Committed);Assert.IsTrue(reopened.PauseRequested);
   Assert.AreEqual(2,client.Operations.Count);
  }
  [Test] public async Task MismatchedMapEdgeCannotConsumeArrivalEvidence(){
   var route=MapRoute();var wire=new Wire();using var client=Client(new Storage(),wire);await SeedMapArrival(client,wire,route);
   var p=route.Capture();p["pendingEdgeId"]="different-edge";p.Remove("packetHash");p["packetHash"]=LastRail.Battle.BDirectAttackInput.Digest(p).Substring(7);
   var other=BRouteConsumerTests.Read(p);using var owner=new OrdinaryTravelOwner(client,"a",route.RunId,()=>true);
   Assert.ThrowsAsync<InvalidOperationException>(async()=>await owner.CommitMapArrivalAsync(other));Assert.AreEqual(1,wire.Count);
  }
  [Test] public async Task DelayedMapArrivalKeepsPauseAndDoesNotPublishIntoReplacedOwner(){
   var route=MapRoute();var wire=new Wire();using var client=Client(new Storage(),wire);await SeedMapArrival(client,wire,route);
   var delayed=new TaskCompletionSource<RunApiHttpResponse>();RunApiHttpRequest pending=null;wire.Reply=r=>{pending=r;return delayed.Task;};
   bool current=true;using var owner=new OrdinaryTravelOwner(client,"a",route.RunId,()=>current);var task=owner.CommitMapArrivalAsync(route);
   Assert.AreSame(task,owner.CommitMapArrivalAsync(route));await owner.PauseAsync();Assert.IsTrue(owner.PauseRequested);
   current=false;delayed.SetResult(MapResponse(pending));Assert.ThrowsAsync<InvalidOperationException>(async()=>await task);Assert.IsNull(owner.MapArrival);
   Assert.AreEqual(2,wire.Count);Assert.AreEqual(2,client.Operations.Count);
  }
 }
}
