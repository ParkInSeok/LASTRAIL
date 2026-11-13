using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using LastRail.Tests.EditMode;
using LastRail.Presentation.MissionShell;
using LastRail.Diagnostics;
public class OrdinaryInitializationClientTests {
 [TestCase(6,false)][TestCase(7,false)][TestCase(8,false)][TestCase(8,true)] public async Task LatestProductPreparationReusesSixTargetOwnerAndOriginalReceipt(int version,bool fullFive){
  var e=JObject.Parse(File.ReadAllText(version==8?"D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json":version==7?"D:/Project/LASTRAIL/QA/CommanderS1-20260922/ReferenceDefinitions809/source.json":Root+"TransitionCaller802/source.json"));var start=(JObject)e["originalStart"];string run=(string)e["runId"],account=(string)e["accountId"],preview=(string)start["operationPreviewId"];
  var input=OrdinaryFiveStartInput.Read(start,account,run);Assert.AreEqual("combat.first-battle-encounter-source.v"+version+".20261028",input.FirstBattle.SourceVersion);
  var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=run,["runStatus"]="ACTIVE",["startResponse"]=start,["errorCode"]=null};
  var pending=new PendingAccountAction(account,"departure-start",preview,0,"latest-start",new JObject{["operationPreviewId"]=preview}.ToString(),response.ToString(Newtonsoft.Json.Formatting.None));
  var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,e["initializationResponse"].ToString(Newtonsoft.Json.Formatting.None)))};var session=SupportInquiryTests.Session(account);
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>1);
  string saved=null;int writes=0;var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/transition-"+Guid.NewGuid()+".json",()=>saved,s=>{saved=s;writes++;});var build=new AppBuildSnapshot("transition-qa",1);string progressed;
  using(var screen=new AccountScreenRuntime(wire,null,()=>session,()=>1)){
   screen.BindRunRecovery(client);screen.SynchronizeSession();typeof(AccountScreenRuntime).GetProperty("Pending").SetValue(screen,pending);
   await screen.EnsureDepartureRecoveryAsync(run);await screen.EnsureDepartureRecoveryAsync(run);Assert.Zero(wire.Requests.Count,"Opening the screen reuses retained START.");
   var driver=await PrepareCurrent(screen);
   if(fullFive){
    Assert.AreEqual(6,((JArray)driver.HudState["actors"]).Count);Assert.AreEqual("AWAITING_ENCOUNTER",(string)driver.HudState["executionPhase"]);
    string before=driver.StateJson;int beforeWrites=writes;driver.Advance(500000,1);Assert.AreEqual(before,driver.StateJson);Assert.AreEqual(beforeWrites,writes);
   }else{
    var damage=OrdinaryDamageOwnerTests.Damage(input.Train.TrainId,"TRAIN");damage["command"]["accountId"]=account;damage["command"]["runId"]=run;
    driver.ScheduleResolvedOrdinaryEffect(damage);driver.Advance(500000,1);
   }
   progressed=driver.StateJson;int count=writes;
   Assert.AreSame(driver,await PrepareCurrent(screen));Assert.AreEqual(progressed,driver.StateJson);Assert.AreEqual(count,writes);
   if(fullFive){
    screen.ReleaseOrdinaryCombatPreparation();
    Assert.Throws<InvalidOperationException>(()=>driver.SetSpeed(3));
    Assert.ThrowsAsync<OrdinaryNodeStartUnavailableException>(async()=>await PrepareCurrent(screen));
    Assert.AreEqual(count,writes);Assert.AreEqual(progressed,driver.StateJson);
   }
  }
  using var reopened=new AccountScreenRuntime(wire,null,()=>session,()=>1);reopened.BindRunRecovery(client);reopened.SynchronizeSession();typeof(AccountScreenRuntime).GetProperty("Pending").SetValue(reopened,pending);
  if(fullFive)Assert.ThrowsAsync<OrdinaryNodeStartUnavailableException>(async()=>await PrepareCurrent(reopened),"A previous checkpoint is not a node-start snapshot.");
  else{var restored=await PrepareCurrent(reopened);Assert.AreEqual(progressed,restored.StateJson);}
  Assert.AreEqual(1,wire.Requests.Count);Assert.IsTrue(wire.Requests.All(r=>r.Method=="POST"));
  Task<CombatSceneDriver> PrepareCurrent(AccountScreenRuntime screen)=>fullFive?screen.PrepareOrdinaryFiveCombatAsync(run,store,build,500000):screen.PrepareOrdinaryCombatAsync(run,store,build,500000);
 }
 [TestCase(false)][TestCase(true)] public async Task LatestSixTargetCheckpointSurvivesCommitInterruptionWithoutReinitializing(bool progressed){
  var e=OrdinaryTrainInitialStateTests.Export();var start=(JObject)e["originalStart"];var input=OrdinaryFiveStartInput.Read(start,"a",(string)e["runId"]);
  var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,e["initializationResponse"].ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  var init=await client.InitializeOrdinaryAsync(input,"train-key");Assert.NotNull(init);
  var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=input.RunId,["runStatus"]="ACTIVE",["startResponse"]=start,["errorCode"]=null};
  var original=new PendingAccountAction("a","departure-start",(string)start["operationPreviewId"],0,"start-key",new JObject{["operationPreviewId"]=start["operationPreviewId"]}.ToString(Newtonsoft.Json.Formatting.None),response.ToString(Newtonsoft.Json.Formatting.None));
  var factory=OrdinaryFiveSeraFactory.FromInitialization(input,init.AcceptedResponseJson,500000);
  string stored=null;int writes=0;var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/train-initial-"+Guid.NewGuid()+".json",()=>stored,s=>{stored=s;writes++;});
  OrdinaryFiveSeraCheckpoint BindingNow()=>new OrdinaryFiveSeraCheckpoint(store,original,factory,new AppBuildSnapshot("train-initial-test",1),init);
  var binding=BindingNow();journal.Confirm=false;
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await binding.OpenAfterFirstCommitAsync(client,()=>true));Assert.AreEqual(1,writes);Assert.AreEqual(0,journal.Rows.Single().OrdinaryCheckpointRevision);
  if(progressed){var saved=binding.Restore();saved.Simulation.Advance(500000,1);binding.Save(saved.Simulation,saved.Revision);}
  var before=binding.Restore();int savedWrites=writes;journal.Confirm=true;
  var driver=await BindingNow().OpenAfterFirstCommitAsync(client,()=>true);
  Assert.AreEqual(before.Simulation.CaptureLocalState(),driver.StateJson);Assert.AreEqual(savedWrites,writes);Assert.AreEqual(before.Revision,journal.Rows.Single().OrdinaryCheckpointRevision);Assert.AreEqual(1,wire.Requests.Count);
  Assert.AreEqual(6,((JArray)JObject.Parse(driver.StateJson)["actors"]).Count);
  stored="corrupt";Assert.ThrowsAsync<Newtonsoft.Json.JsonReaderException>(async()=>await BindingNow().OpenAfterFirstCommitAsync(client,()=>true));Assert.AreEqual(savedWrites,writes);
 }
 const string Root="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/";
 [TestCase(false)][TestCase(true)] public async Task NodeEntryRecoversUncertainClaimWithOriginalKeyBeforeAnyInitialization(bool available){
  var journal=new Journal();var original=EndOperation(PendingRunKind.Claim,"future-run",false);journal.Rows.Add(original);
  var confirmed=EndOperation(PendingRunKind.Claim,"future-run",true);
  var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(available?200:503,available?confirmed.AcceptedResponseJson:"{}"))};
  var session=SupportInquiryTests.Session("a");using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>1);
  using var screen=new AccountScreenRuntime(wire,null,()=>session,()=>1);screen.BindRunRecovery(client);screen.SynchronizeSession();typeof(AccountScreenRuntime).GetProperty("Pending").SetValue(screen,ProductStart());
  int writes=0;var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/node-entry-"+Guid.NewGuid()+".json",()=>null,s=>writes++);
  Assert.ThrowsAsync<OrdinaryNodeStartUnavailableException>(async()=>await screen.PrepareOrdinaryFiveCombatAsync("future-run",store,new AppBuildSnapshot("test",1),500000));
  Assert.AreEqual(0,writes);Assert.AreEqual(1,wire.Requests.Count);Assert.AreEqual(original.BodyJson,journal.Rows.Single().BodyJson);Assert.AreEqual(original.IdempotencyKey,journal.Rows.Single().IdempotencyKey);
  Assert.AreEqual(available,journal.Rows.Single().AcceptedResponseJson!=null);
  if(available){Assert.ThrowsAsync<InvalidOperationException>(async()=>await screen.PrepareOrdinaryFiveCombatAsync("future-run",store,new AppBuildSnapshot("test",1),500000));Assert.AreEqual(1,wire.Requests.Count);}
  await Task.CompletedTask;
 }
 static PendingAccountAction ProductStart(){
  var start=OrdinaryFiveSeraFactoryTests.Sources().Start;
  start["ordinaryBasicSource"]=JObject.Parse(File.ReadAllText(Root+"ordinary-basic-v3-producer-fixture.json"))["source"];
  start["ordinaryRelocationSource"]=JObject.Parse(File.ReadAllText(Root+"ordinary-relocation-v6-producer-fixture.json"))["source"];
  var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]="future-run",["runStatus"]="ACTIVE",["startResponse"]=start,["errorCode"]=null};
  return new PendingAccountAction("a","departure-start","preview",0,"start-key","{\"operationPreviewId\":\"preview\"}",response.ToString(Newtonsoft.Json.Formatting.None));
 }
 static Task<CombatSceneDriver> Prepare(AccountScreenRuntime state,CombatCheckpointStore store){
  var method=typeof(AccountScreenRuntime).GetMethod("PrepareOrdinaryCombatAsync");Assert.NotNull(method,"Product account caller must join initialization and durable opening.");
  return (Task<CombatSceneDriver>)method.Invoke(state,new object[]{"future-run",store,new AppBuildSnapshot("caller-test",1),500000L});
 }
 [Test] public async Task ProductCallerJoinsInitializationAndCheckpointAndRevokesOnAccountChange(){
  var f=Fixture();var journal=new Journal();var gate=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>gate.Task};var session=SupportInquiryTests.Session("a");
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>1);
  using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);state.BindRunRecovery(client);state.SynchronizeSession();typeof(AccountScreenRuntime).GetProperty("Pending").SetValue(state,ProductStart());
  string saved=null;int writes=0;var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/caller-"+Guid.NewGuid()+".json",()=>saved,s=>{saved=s;writes++;});
  var first=Prepare(state,store);Assert.AreSame(first,Prepare(state,store));Assert.AreEqual(1,wire.Requests.Count);Assert.IsNull(saved);
  gate.SetResult(RunApiHttpResponse.Success(200,f.Response.ToString()));var driver=await first;
  Assert.AreEqual(1,writes);Assert.AreEqual(1,journal.Rows.Single().OrdinaryCheckpointRevision);Assert.AreSame(driver,await Prepare(state,store));Assert.AreEqual(1,wire.Requests.Count);
  driver.Advance(500000,1);Assert.AreEqual(1,driver.Tick);session=SupportInquiryTests.Session("other");state.SynchronizeSession();
  Assert.Throws<InvalidOperationException>(()=>driver.Advance(500000,1));Assert.AreEqual(2,writes);
 }
 [Test] public async Task ProductCallerRetriesFailedStorageAndRestoresProgressWithoutAnotherInitialization(){
  var f=Fixture();var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};var session=SupportInquiryTests.Session("a");
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>1);
  string saved=null;bool fail=true;var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/caller-"+Guid.NewGuid()+".json",()=>saved,s=>{if(fail)throw new IOException("injected");saved=s;});
  string progressed;
  using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>1)){
   state.BindRunRecovery(client);state.SynchronizeSession();typeof(AccountScreenRuntime).GetProperty("Pending").SetValue(state,ProductStart());
   Assert.ThrowsAsync<InvalidOperationException>(async()=>await Prepare(state,store));Assert.IsNull(saved);Assert.AreEqual(0,journal.Rows.Single().OrdinaryCheckpointRevision);
   fail=false;var driver=await Prepare(state,store);driver.Advance(1000000,2);progressed=driver.StateJson;Assert.AreEqual(1,wire.Requests.Count);
  }
  using var restored=new AccountScreenRuntime(wire,null,()=>session,()=>1);restored.BindRunRecovery(client);restored.SynchronizeSession();typeof(AccountScreenRuntime).GetProperty("Pending").SetValue(restored,ProductStart());
  var reopened=await Prepare(restored,store);Assert.AreEqual(progressed,reopened.StateJson);Assert.AreEqual(1,wire.Requests.Count);
 }
 [TestCase(false)][TestCase(true)] public void ProductCallerDoesNotSaveLateInitializationAfterOwnerOrTerminalChange(bool terminal){
  var f=Fixture();var journal=new Journal();var gate=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>gate.Task};var session=SupportInquiryTests.Session("a");
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>1);
  using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);state.BindRunRecovery(client);state.SynchronizeSession();typeof(AccountScreenRuntime).GetProperty("Pending").SetValue(state,ProductStart());
  int writes=0;var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/caller-"+Guid.NewGuid()+".json",()=>null,s=>writes++);
  var pending=Prepare(state,store);if(terminal)journal.Rows.Add(EndOperation(PendingRunKind.Finish,"future-run",true));else{session=SupportInquiryTests.Session("other");state.SynchronizeSession();}
  gate.SetResult(RunApiHttpResponse.Success(200,f.Response.ToString()));Assert.ThrowsAsync<InvalidOperationException>(async()=>await pending);Assert.AreEqual(0,writes);
 }
 static (OrdinaryFiveStartInput Input,JObject Response) Fixture(){
  var fixture=JObject.Parse(File.ReadAllText(Root+"Five700-cross-process/player-input.json"));var start=(JObject)fixture["start"];
  start["ordinaryBasicSource"]=JObject.Parse(File.ReadAllText(Root+"ordinary-basic-v3-producer-fixture.json"))["source"];
  start["ordinaryRelocationSource"]=JObject.Parse(File.ReadAllText(Root+"ordinary-relocation-v6-producer-fixture.json"))["source"];
  var input=OrdinaryFiveStartInput.Read(start,"a","future-run");var stats=fixture["stats"]["packet"];
  var receipt=new JObject{["version"]="combat.ordinary-phase-initialization.v1.20261023",["runId"]=input.RunId,["phase"]="RUNNING",["stats"]=stats,["sourceState"]=fixture["stats"]["state"],
   ["health"]=new JObject{["revision"]=1,["state"]=new JObject{["targets"]=new JArray(stats["actors"].Select(a=>new JObject{["id"]=a["operatorId"],["health"]=new JObject{["hp"]=a["currentHp"],["maxHp"]=a["effectiveStats"]["maxHp"]}}))}},
   ["ordinaryKitBinding"]=new JObject{["version"]=OrdinaryFiveStartInput.KitVersion,["originHash"]=input.OriginHash,["kitHash"]=input.KitHash},
   ["ordinaryBasicBinding"]=new JObject{["version"]=input.Basic.SourceVersion,["originHash"]=input.OriginHash,["kitHash"]=input.KitHash,["basicHash"]=input.Basic.BasicHash},
   ["ordinaryRelocationBinding"]=new JObject{["version"]=input.Relocation.SourceVersion,["originHash"]=input.OriginHash,["basicHash"]=input.Basic.BasicHash,["relocationHash"]=input.Relocation.RelocationHash}};
  return(input,new JObject{["accepted"]=true,["runtimeExecutionVerified"]=false,["initializationReceipt"]=receipt});
 }
 sealed class Journal:IRunOperationJournal,IOrdinaryCheckpointJournal {
  internal readonly List<PendingRunOperation> Rows=new List<PendingRunOperation>();internal bool Save=true,Confirm=true;
  public Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account)=>Task.FromResult<IReadOnlyList<PendingRunOperation>>(Rows.Where(r=>r.AccountId==account).ToArray());
  public Task<bool> PutAsync(PendingRunOperation op){if(Save)Rows.Add(op);return Task.FromResult(Save);}
  public Task<bool> ConfirmAsync(PendingRunOperation op,string json){if(Confirm)Rows[Rows.IndexOf(op)]=op.Confirmed(json);return Task.FromResult(Confirm);}
  public Task<bool> RemoveConfirmedAsync(string a,PendingRunKind k,string key)=>throw new NotSupportedException();
  public Task<bool> ConfirmFirstCheckpointAsync(PendingRunOperation op,long revision){
   if(!Confirm)return Task.FromResult(false);var row=Rows.Single(r=>r.IdempotencyKey==op.IdempotencyKey);
   Rows[Rows.IndexOf(row)]=(PendingRunOperation)typeof(PendingRunOperation).GetMethod("WithOrdinaryCheckpoint",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(row,new object[]{revision});return Task.FromResult(true);
  }
 }
 [Test] public void HistoricalReceiptIsRetainedButCannotSupplyNewInitialFactory(){
  var f=Fixture();((JObject)f.Response["initializationReceipt"]).Remove("sourceState");var json=f.Response.ToString();
  Assert.IsNull(OrdinaryInitializationReceipt.Validate(json,f.Input));
  Assert.Throws<InvalidOperationException>(()=>OrdinaryFiveSeraFactory.FromInitialization(f.Input,json,500000));
 }
 [Test] public async Task ChangedInitialSourceCannotBeConfirmed(){
  var f=Fixture();f.Response["initializationReceipt"]["sourceState"]["accountId"]="other";
  var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  Assert.IsNull(await client.InitializeOrdinaryAsync(f.Input,"key"));Assert.IsNull(journal.Rows.Single().AcceptedResponseJson);
 }
 sealed class Wire:IRunApiHttpTransport {
  internal readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();internal Func<Task<RunApiHttpResponse>> Reply;
  public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests.Add(request);return Reply();}
 }
 static PendingRunOperation EndOperation(PendingRunKind kind,string run,bool confirmed){
  PendingRunOperation op;
  if(kind==PendingRunKind.Claim)op=PendingRunOperation.Claim("a",RunRewardClaimRequest.Create(run,"preview","terminal-key-123456"),new AppBuildSnapshot("test",1),DateTimeOffset.UtcNow);
  else{
   var body=new JObject{["runId"]=run,["encounterType"]="ORDINARY_15_WAVE",["currentStage"]="Complete",["currentWaveNumber"]=15,["completedWaveCount"]=15,["outcome"]="VICTORY",["tacticSelections"]=new JArray(),["runtimeStateJson"]=new JObject{["runId"]=run}.ToString(),["clientCompletedAt"]="2026-10-26T00:00:00.000Z"};
   var request=(RunResultSubmitRequest)Activator.CreateInstance(typeof(RunResultSubmitRequest),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{run,"terminal-key-123456",body.ToString()},null);
   op=PendingRunOperation.Finish("a",request,new AppBuildSnapshot("test",1),DateTimeOffset.UtcNow);
  }
  if(!confirmed)return op;
  var receipt=kind==PendingRunKind.Claim?new JObject{["runId"]=run,["rewardPreviewId"]="preview",["rewardClaimId"]="claim",["status"]="CLAIMED",["serverClaimedAt"]="2026-10-26T00:00:00.000Z",["balanceVersion"]="b1",["rewardLines"]=new JArray(),["errorCode"]=null}:new JObject{["accepted"]=true,["runId"]=run,["resultStatus"]="ACCEPTED",["rewardPreviewId"]="preview",["balanceVersion"]="b1",["serverReceivedAt"]="2026-10-26T00:00:00.000Z"};
  return op.Confirmed(receipt.ToString());
 }
 [TestCase(PendingRunKind.Finish,false)][TestCase(PendingRunKind.Finish,true)][TestCase(PendingRunKind.Claim,false)][TestCase(PendingRunKind.Claim,true)]
 public async Task CompletionRecordBlocksInitializationAndFirstCheckpointWithoutSending(PendingRunKind kind,bool confirmed){
  var f=Fixture();var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  Assert.NotNull(await client.InitializeOrdinaryAsync(f.Input,"key"));journal.Rows.Add(EndOperation(kind,f.Input.RunId,confirmed));
  Assert.IsNull(await client.InitializeOrdinaryAsync(f.Input,"key"));
  Assert.IsNull(await client.CommitFirstOrdinaryCheckpointAsync(f.Input,"key",_=>throw new Exception("Must not initialize after completion intent.")));Assert.AreEqual(1,wire.Requests.Count);
  Assert.AreEqual(0,journal.Rows.Single(r=>r.Kind==PendingRunKind.InitializeOrdinary).OrdinaryCheckpointRevision);
 }
 [Test] public async Task CompletionDuringFirstSaveCannotReleaseLateInitialization(){
  var f=Fixture();var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  Assert.NotNull(await client.InitializeOrdinaryAsync(f.Input,"key"));var save=new TaskCompletionSource<long>();var pending=client.CommitFirstOrdinaryCheckpointAsync(f.Input,"key",_=>save.Task);
  journal.Rows.Add(EndOperation(PendingRunKind.Finish,f.Input.RunId,true));save.SetResult(1);
  Assert.IsNull(await pending);Assert.AreEqual(0,journal.Rows.Single(r=>r.Kind==PendingRunKind.InitializeOrdinary).OrdinaryCheckpointRevision);Assert.AreEqual(1,wire.Requests.Count);
 }
 [Test] public async Task AnotherRunCompletionDoesNotBlockThisInitialization(){
  var f=Fixture();var journal=new Journal();journal.Rows.Add(EndOperation(PendingRunKind.Finish,"other-run",true));var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  Assert.NotNull(await client.InitializeOrdinaryAsync(f.Input,"key"));Assert.NotNull(await client.CommitFirstOrdinaryCheckpointAsync(f.Input,"key",_=>Task.FromResult(1L)));Assert.AreEqual(1,wire.Requests.Count);
 }
 [Test] public async Task LateInitializationResponseIsRetainedButNotReleasedAfterCompletion(){
  var f=Fixture();var journal=new Journal();var reply=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>reply.Task};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  var pending=client.InitializeOrdinaryAsync(f.Input,"key");journal.Rows.Add(EndOperation(PendingRunKind.Finish,f.Input.RunId,true));reply.SetResult(RunApiHttpResponse.Success(200,f.Response.ToString()));
  Assert.IsNull(await pending);Assert.NotNull(journal.Rows.Single(r=>r.Kind==PendingRunKind.InitializeOrdinary).AcceptedResponseJson);Assert.AreEqual(1,wire.Requests.Count);
 }
 [Test]public async Task LossRetriesOriginalPostAndConfirmedReceiptIsReused(){
  var f=Fixture();var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.NetworkError("synthetic loss"))};
  using(var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1)){
   Assert.IsNull(await client.InitializeOrdinaryAsync(f.Input,"original-key"));Assert.AreEqual(1,journal.Rows.Count);
   Assert.IsNull(await client.RetryAsync(journal.Rows[0]));Assert.AreEqual(1,wire.Requests.Count,"A bare retry must require the retained START.");
  }
  wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()));
  using(var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1)){
   var result=await client.InitializeOrdinaryAsync(f.Input,"original-key");Assert.NotNull(result?.AcceptedResponseJson);
   Assert.AreEqual(LobbyRunSelectionKind.Recovery,LobbyRunSelection.Resolve("a",journal.Rows,null,null,null,null,null).Kind);
   Assert.NotNull(await client.InitializeOrdinaryAsync(f.Input,"original-key"));Assert.AreEqual(2,wire.Requests.Count);
   Assert.IsNull(await client.InitializeOrdinaryAsync(f.Input,"replacement-key"));Assert.AreEqual(2,wire.Requests.Count);
   foreach(var request in wire.Requests){Assert.AreEqual("POST",request.Method);Assert.AreEqual("/v1/runs/future-run/ordinary-initialization",request.EndpointPath);Assert.AreEqual("{}",request.BodyJson);Assert.AreEqual("original-key",request.Headers.Single(h=>h.Name=="Idempotency-Key").Value);}
  }
 }
 [TestCase("run")][TestCase("binding")][TestCase("hp")][TestCase("flag")][TestCase("missingAttack")]
 public async Task InvalidReceiptRemainsUnconfirmed(string change){
  var f=Fixture();var r=f.Response["initializationReceipt"];if(change=="run")r["runId"]="wrong";if(change=="binding")r["ordinaryRelocationBinding"]["relocationHash"]="wrong";if(change=="hp")r["health"]["state"]["targets"][0]["health"]["hp"]=1;if(change=="flag")f.Response["runtimeExecutionVerified"]=true;
  if(change=="missingAttack"){
   ((JObject)r["stats"]["actors"][0]["effectiveStats"]).Remove("attack");var body=(JObject)r["stats"].DeepClone();body.Remove("packetHash");
   var canonical=(string)typeof(BasicCombatSimulation).GetMethod("StableJson",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{body});
   using var sha=System.Security.Cryptography.SHA256.Create();r["stats"]["packetHash"]=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(canonical))).Replace("-","").ToLowerInvariant();
  }
  var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  Assert.IsNull(await client.InitializeOrdinaryAsync(f.Input,"key"));Assert.IsNull(journal.Rows.Single().AcceptedResponseJson);
 }
 [Test]public async Task SaveFailureAndDifferentAccountSendNothing(){
  var f=Fixture();var journal=new Journal{Save=false};var wire=new Wire{Reply=()=>throw new Exception("Must not send")};var session=SupportInquiryTests.Session("a");
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>1);
  Assert.IsNull(await client.InitializeOrdinaryAsync(f.Input,"key"));session=SupportInquiryTests.Session("other");Assert.IsNull(await client.InitializeOrdinaryAsync(f.Input,"key"));Assert.IsEmpty(wire.Requests);
 }
 [Test]public async Task ReceiptSaveRetryReusesValidatedResponseWithoutAnotherPost(){
  var f=Fixture();var journal=new Journal{Confirm=false};var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  Assert.IsNull(await client.InitializeOrdinaryAsync(f.Input,"key"));journal.Confirm=true;
  Assert.NotNull(await client.InitializeOrdinaryAsync(f.Input,"key"));Assert.AreEqual(1,wire.Requests.Count);
 }
 [Test]public async Task PriorSessionReplyDoesNotConfirmOrPublish(){
  var f=Fixture();var journal=new Journal();var pending=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>pending.Task};long generation=1;
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>generation);
  var task=client.InitializeOrdinaryAsync(f.Input,"key");Assert.AreEqual(1,wire.Requests.Count);generation++;
  pending.SetResult(RunApiHttpResponse.Success(200,f.Response.ToString()));Assert.IsNull(await task);Assert.IsNull(journal.Rows.Single().AcceptedResponseJson);Assert.IsEmpty(client.Operations);
 }
 [TestCase("bad key")][TestCase("")][TestCase("한글")]
 public async Task UnsupportedKeysNeverSend(string key){
  var f=Fixture();var journal=new Journal();var wire=new Wire{Reply=()=>throw new Exception("Must not send")};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  Assert.IsNull(await client.InitializeOrdinaryAsync(f.Input,key));Assert.IsEmpty(wire.Requests);Assert.IsEmpty(journal.Rows);
 }
 static OrdinaryFiveSeraCheckpoint Binding(CombatCheckpointStore store,OrdinaryFiveStartInput input,PendingRunOperation initialized){
  var source=OrdinaryFiveSeraFactoryTests.Sources();var start=source.Start;
  start["ordinaryBasicSource"]=JObject.Parse(File.ReadAllText(Root+"ordinary-basic-v3-producer-fixture.json"))["source"];
  start["ordinaryRelocationSource"]=JObject.Parse(File.ReadAllText(Root+"ordinary-relocation-v6-producer-fixture.json"))["source"];
  var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=input.RunId,["runStatus"]="ACTIVE",["startResponse"]=start,["errorCode"]=null};
  var original=new PendingAccountAction("a","departure-start","preview",0,"start-key","{\"operationPreviewId\":\"preview\"}",response.ToString(Newtonsoft.Json.Formatting.None));
  return new OrdinaryFiveSeraCheckpoint(store,original,OrdinaryFiveSeraFactory.FromInitialization(input,initialized.AcceptedResponseJson,500000),new AppBuildSnapshot("first-checkpoint-test",1),initialized);
 }
 [Test]public async Task FirstCommitPrecedesOpenAndProgressIsNeverReset(){
  var f=Fixture();var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  var initialized=await client.InitializeOrdinaryAsync(f.Input,"key");Assert.AreEqual(0,initialized.OrdinaryCheckpointRevision);
  var directory="D:/Project/LASTRAIL/LocalTemp/Unity/first-checkpoint-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(directory);
  var path=Path.Combine(directory,"checkpoint.json");var store=CombatCheckpointStore.CreateExplicitLocalQaFile(path);var binding=Binding(store,f.Input,initialized);
  var first=binding.OpenAfterFirstCommitAsync(client,()=>true);Assert.AreSame(first,binding.OpenAfterFirstCommitAsync(client,()=>true));var driver=await first;
  Assert.IsTrue(File.Exists(path));Assert.AreEqual(1,journal.Rows.Single().OrdinaryCheckpointRevision);Assert.AreEqual(0,driver.Tick);
  driver.Advance(500000,1);var progressed=driver.StateJson;
  var restored=await Binding(store,f.Input,journal.Rows.Single()).OpenAfterFirstCommitAsync(client,()=>true);Assert.AreEqual(progressed,restored.StateJson);Assert.AreEqual(1,wire.Requests.Count);
  File.Delete(path);
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await Binding(store,f.Input,journal.Rows.Single()).OpenAfterFirstCommitAsync(client,()=>true));Assert.IsFalse(File.Exists(path));
 }
 [Test]public async Task FailedCommitMarkerNeverOpensAndRetryKeepsFirstSavedState(){
  var f=Fixture();var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  var initialized=await client.InitializeOrdinaryAsync(f.Input,"key");journal.Confirm=false;
  string saved=null;int writes=0;var directory="D:/Project/LASTRAIL/LocalTemp/Unity/first-checkpoint-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(directory);
  var store=new CombatCheckpointStore(Path.Combine(directory,"checkpoint.json"),()=>saved,value=>{saved=value;writes++;});
  var binding=Binding(store,f.Input,initialized);bool current=true;
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await binding.OpenAfterFirstCommitAsync(client,()=>current));Assert.AreEqual(1,writes);Assert.AreEqual(0,journal.Rows.Single().OrdinaryCheckpointRevision);
  journal.Confirm=true;var driver=await binding.OpenAfterFirstCommitAsync(client,()=>current);Assert.AreEqual(1,writes);Assert.AreEqual(0,driver.Tick);
  current=false;Assert.Throws<InvalidOperationException>(()=>binding.OpenAfterFirstCommitAsync(client,()=>true));
 }
 [Test]public async Task RealJournalRetainsFirstCommitAndDoesNotDeleteItsProof(){
  var f=Fixture();var native=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(RunOperationJournalTests).GetNestedType("Native",System.Reflection.BindingFlags.NonPublic),true);
  var journal=new EncryptedRunOperationJournal(native);var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  var op=await client.InitializeOrdinaryAsync(f.Input,"key");Assert.AreEqual(0,(await new EncryptedRunOperationJournal(native).ReadAsync("a")).Single().OrdinaryCheckpointRevision);
  Assert.IsTrue(await journal.ConfirmFirstCheckpointAsync(op,1));var saved=(await new EncryptedRunOperationJournal(native).ReadAsync("a")).Single();Assert.AreEqual(1,saved.OrdinaryCheckpointRevision);
  Assert.IsFalse(await journal.ConfirmFirstCheckpointAsync(op,2));Assert.IsFalse(await journal.RemoveConfirmedAsync("a",op.Kind,op.IdempotencyKey));Assert.IsFalse(await client.HandoffAsync(saved,_=>Task.FromResult(true)));
 }
 [Test]public async Task HistoricalReceiptWithoutStartProofCannotCreateFirstCheckpoint(){
  var f=Fixture();var native=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(RunOperationJournalTests).GetNestedType("Native",System.Reflection.BindingFlags.NonPublic),true);
  var journal=new EncryptedRunOperationJournal(native);var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  await client.InitializeOrdinaryAsync(f.Input,"key");var old=Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(native.Decrypt(native.Load()),new Newtonsoft.Json.JsonSerializerSettings{DateParseHandling=Newtonsoft.Json.DateParseHandling.None});((JObject)old["records"][0]).Remove("ordinaryCheckpointRevision");Assert.IsTrue(native.Save(native.Encrypt(old.ToString())));
  int writes=0;Assert.IsNull(await client.CommitFirstOrdinaryCheckpointAsync(f.Input,"key",_=>{writes++;return Task.FromResult(1L);}));Assert.AreEqual(0,writes);
  Assert.IsNull((await journal.ReadAsync("a")).Single().OrdinaryCheckpointRevision);
 }
 [TestCase(0L)][TestCase(1L)]public async Task CanonicalReceiptMustMatchBindingBeforeSaveOrRestore(long marker){
  var f=Fixture();var journal=new Journal();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,f.Response.ToString()))};
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>SupportInquiryTests.Session("a"),()=>1);
  var initialized=await client.InitializeOrdinaryAsync(f.Input,"key");var directory="D:/Project/LASTRAIL/LocalTemp/Unity/first-checkpoint-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(directory);
  int writes=0;var store=new CombatCheckpointStore(Path.Combine(directory,"checkpoint.json"),()=>null,_=>writes++);var binding=Binding(store,f.Input,initialized);
  f.Response["initializationReceipt"]["health"]["revision"]=2;
  journal.Rows[0]=(PendingRunOperation)Activator.CreateInstance(typeof(PendingRunOperation),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,
   new object[]{initialized.Kind,initialized.AccountId,initialized.TargetId,initialized.IdempotencyKey,initialized.BodyJson,initialized.Build,initialized.ComposedAt,f.Response.ToString(),null,(long?)marker},null);
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await binding.OpenAfterFirstCommitAsync(client,()=>true));Assert.AreEqual(0,writes);Assert.AreEqual(marker,journal.Rows.Single().OrdinaryCheckpointRevision);
 }
}
