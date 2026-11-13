using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class Ce03PlayerDepartureTests {
 // Synthetic transport fixture only; no claim of authenticated admission or Player execution.
 sealed class Wire:IRunApiHttpTransport {
  public readonly (JObject Prepared,JObject Response) Fixture=Ce03PlayerContractTests.Fixture();
  public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
  public TaskCompletionSource<RunApiHttpResponse> DelayedStart;
  public int LostStarts;
  public RunApiHttpResponse StartReply=>RunApiHttpResponse.Success(200,Ce03CommandContract.Canonical(Fixture.Response));
  public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Requests.Add(r);JObject result;
   if(r.EndpointPath=="/__local/ce03/start")return LostStarts-->0?Task.FromResult(RunApiHttpResponse.NetworkError("lost response")):DelayedStart?.Task??Task.FromResult(StartReply);
   if(r.EndpointPath=="/__local/ce03/support-loadout")result=new JObject{["selection"]=Fixture.Prepared["commandContract"]["support"].DeepClone()};
   else if(r.EndpointPath=="/__local/ce03/prepare")result=Fixture.Prepared;
   else if(r.EndpointPath=="/v1/operations/preview")result=new JObject{["accepted"]=true,["operationPreviewId"]=Fixture.Response["operationPreviewId"].DeepClone(),["expiresAt"]=DateTime.UtcNow.AddMinutes(2).ToString("O"),["quote"]=new JObject{["fuelCost"]=10}};
   else throw new InvalidOperationException("Unexpected transport call");
   return Task.FromResult(RunApiHttpResponse.Success(200,Ce03CommandContract.Canonical(result)));
  }
 }
 static Ce03LocalClient Client(Wire w)=>new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),_=>{});
 [Test] public async Task RecoveryAndContinueShareOneRunControllerAndRetireTogether(){
  var w=new Wire();var client=Client(w);await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
  var original=client.ReadRetainedStart(client.AwaitingCheckpointKey);string saved=null;bool current=true,combatAllowed=true;
  var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/entry-owner-test",()=>saved,raw=>saved=raw);
  var binding=UnityEditor.AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>("Assets/_LASTRAIL/QA/CombatAssetBinding/89e63936213b4ce090e6a03fbb98bbe7/Binding.asset");
  var type=typeof(Ce03LocalClient).Assembly.GetType("LastRail.Battle.Ce03PlayerRunOwner");Assert.IsNotNull(type,"Recovery and Continue need one shared current-run owner");
  var owner=Activator.CreateInstance(type,original,binding,store,client,(Func<bool>)(()=>current),(Func<bool>)(()=>combatAllowed));
  try{var resolve=type.GetMethod("Resolve");var first=resolve.Invoke(owner,new object[]{original});string initial=saved;
   Assert.AreSame(first,resolve.Invoke(owner,new object[]{original}));Assert.AreEqual(initial,saved);
   var other=original.Copy();other["key"]="different-original-key";var mismatched=new Ce03OriginalStart(Ce03CommandContract.Canonical(other),original.Owner,new Uri(original.Origin+"/"));
   Assert.Throws<TargetInvocationException>(()=>resolve.Invoke(owner,new object[]{mismatched}));Assert.AreEqual(initial,saved);
   combatAllowed=false;Assert.IsNull(resolve.Invoke(owner,new object[]{original}));Assert.AreEqual(initial,saved);
   current=false;Assert.Throws<TargetInvocationException>(()=>resolve.Invoke(owner,new object[]{original}));Assert.AreEqual(initial,saved);
  }finally{((IDisposable)owner).Dispose();}
  Assert.AreEqual(1,w.Requests.Count);
 }
 [Test] public async Task LegacyConfirmedJournalCannotSilentlyCreateAnotherDeparture(){
  var w=new Wire();var client=Client(w);await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
  var old=JObject.Parse(client.JournalJson);old.Remove("awaitingCheckpoint");string saved=Ce03CommandContract.Canonical(old);
  var reopened=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),_=>Assert.Fail("Ambiguous history must not be rewritten"),saved);
  using var flow=new Ce03LocalDeparture(reopened,_=>true,(_,__)=>{},_=>{});int sent=w.Requests.Count;
  await flow.PreviewAsync("opn_01_last_departure","I",1,1,1);
  Assert.AreEqual(sent,w.Requests.Count,"Old confirmed receipt requires explicit owner reconciliation");Assert.IsFalse(flow.CanChangeSelection);
  Assert.AreEqual(saved,reopened.JournalJson);
 }
 [TestCase(false,false)] [TestCase(true,false)] [TestCase(true,true)] public async Task SharedOwnerCanReopenClosedOrFailedPresentation(bool saveFailure,bool missingCheckpoint){
  var w=new Wire();var client=Client(w);await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
  var original=client.ReadRetainedStart(client.AwaitingCheckpointKey);string saved=null;bool fail=false;
  var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/reentry-failure-test",()=>saved,raw=>{if(fail)throw new IOException("injected checkpoint failure");saved=raw;});
  var binding=UnityEditor.AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>("Assets/_LASTRAIL/QA/CombatAssetBinding/89e63936213b4ce090e6a03fbb98bbe7/Binding.asset");
  using var owner=new Ce03PlayerRunOwner(original,binding,store,client,()=>true,()=>true);
  var go=new UnityEngine.GameObject("ReentryScene");var inputGo=new UnityEngine.GameObject("ReentryInput",typeof(UnityEngine.EventSystems.EventSystem));
  try{var scene=go.AddComponent<CombatPlayerScene>();int errors=0;Action<Exception> onError=e=>errors++;var input=inputGo.GetComponent<UnityEngine.EventSystems.EventSystem>();
   Assert.AreEqual(Ce03BattleEntry.Presented,owner.Present(original,scene,input,_=>{},onError));scene.SceneCamera.Render();string durable=saved;
   if(saveFailure){fail=true;var frame=typeof(CombatPlayerScene).GetMethod("AdvanceFrame",BindingFlags.Instance|BindingFlags.NonPublic);frame.Invoke(scene,new object[]{.2d});frame.Invoke(scene,new object[]{.2d});Assert.AreEqual(1,errors);fail=false;}
   else{typeof(CombatPlayerScene).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(scene,null);}
   if(missingCheckpoint){saved=null;Assert.Throws<IOException>(()=>owner.Present(original,scene,input,_=>{},onError));Assert.IsNull(saved);saved=durable;}
   Assert.AreEqual(Ce03BattleEntry.Presented,owner.Present(original,scene,input,_=>{},onError));Assert.AreEqual(durable,saved);Assert.AreEqual(1,w.Requests.Count);
  }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(inputGo);}
 }
 [Test] public async Task SameClientRepeatedHandoffsPreserveReceiptsWithoutPendingAccumulation(){
  var w=new Wire();var client=Client(w);var retained=new Dictionary<string,string>();
  var binding=UnityEditor.AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>("Assets/_LASTRAIL/QA/CombatAssetBinding/89e63936213b4ce090e6a03fbb98bbe7/Binding.asset");
  for(int i=0;i<3;i++){
   await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
   var original=client.ReadRetainedStart(client.AwaitingCheckpointKey);retained.Add(original.Key,original.Json);string checkpoint=null;
   var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/repeated-handoff-test",()=>checkpoint,raw=>checkpoint=raw);
   using var owner=new Ce03PlayerRunOwner(original,binding,store,client,()=>true,()=>true);owner.Resolve(original);
   Assert.IsNull(client.AwaitingCheckpointKey);Assert.IsFalse(client.HasReceivedStart);Assert.IsFalse(client.HasPending);
   foreach(var entry in retained)Assert.AreEqual(entry.Value,client.ReadRetainedStart(entry.Key).Json);
  }
  Assert.AreEqual(3,w.Requests.Count);Assert.AreEqual(3,((JObject)JObject.Parse(client.JournalJson)["receipts"]).Count);
 }
 [Test] public async Task TerminalCheckpointRoutesResultOnceAndMissingSavedCheckpointNeverReinitializes(){
  var w=new Wire();var client=Client(w);await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
  var original=client.ReadRetainedStart(client.AwaitingCheckpointKey);string saved=null;
  var binding=UnityEditor.AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>("Assets/_LASTRAIL/QA/CombatAssetBinding/89e63936213b4ce090e6a03fbb98bbe7/Binding.asset");
  var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/terminal-handoff-test",()=>saved,raw=>saved=raw);
  var model=original.CreateVerifiedPlayerFactory(binding)();long step=(long)JObject.Parse(model.CaptureLocalState())["clock"]["step"];model.Advance(step*10000,10000);Assert.AreNotEqual(BasicCombatOutcome.Running,model.Outcome);store.SaveLocalStart(original,model,0);
  using(var owner=new Ce03PlayerRunOwner(original,binding,store,client,()=>true,()=>true)){
   int results=0;Action<CombatSceneResult> done=r=>{results++;Assert.AreEqual(model.Tick,r.Tick);};
   Assert.AreEqual(Ce03BattleEntry.Result,owner.Present(original,null,null,done,null));Assert.AreEqual(Ce03BattleEntry.Result,owner.Present(original,null,null,done,null));Assert.AreEqual(1,results);Assert.IsFalse(owner.Resolve(original).HasPresentation);
  }
  string durable=saved;saved=null;using var reopened=new Ce03PlayerRunOwner(original,binding,store,client,()=>true,()=>true);
  Assert.Throws<IOException>(()=>reopened.Resolve(original));Assert.IsNull(saved);Assert.AreEqual(original.Json,client.ReadRetainedStart(original.Key).Json);Assert.AreEqual(1,w.Requests.Count);saved=durable;
 }
 [Test] public async Task ConfirmedButUncheckpointedStartReopensExactRunWithoutHttp(){
  var w=new Wire();var client=Client(w);
  await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
  var saved=client.JournalJson;var sent=w.Requests.Count;
  var reopened=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),_=>{},saved);
  Ce03OriginalStart entered=null;using var flow=new Ce03LocalDeparture(reopened,_=>true,(_,__)=>{},o=>entered=o);
  var restore=typeof(Ce03LocalDeparture).GetMethod("RestoreAwaitingCheckpoint");
  Assert.IsNotNull(restore,"Confirmed receipt must retain an exact-key checkpoint handoff after restart");
  Assert.AreEqual(true,restore.Invoke(flow,null));Assert.IsFalse(flow.CanChangeSelection);
  flow.RetryEntry();Assert.IsNotNull(entered);Assert.AreEqual((string)w.Fixture.Response["runId"],entered.RunId);
  Assert.AreEqual(sent,w.Requests.Count);Assert.AreEqual(saved,reopened.JournalJson);
 }
 [Test] public async Task LostStartResponseAutomaticallyRetriesOnceWithOriginalRequest(){
  var w=new Wire{LostStarts=1};var client=Client(w);
  await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
  Assert.AreEqual(2,w.Requests.Count);Assert.AreEqual(w.Requests[0].BodyJson,w.Requests[1].BodyJson);
  CollectionAssert.AreEqual(w.Requests[0].Headers.Select(h=>h.Value),w.Requests[1].Headers.Select(h=>h.Value));
  Assert.IsFalse(client.HasPending);Assert.AreEqual(1,((JObject)JObject.Parse(client.JournalJson)["receipts"]).Count);
 }
 [TestCase(false)] [TestCase(true)] public async Task CheckpointHandoffRequiresMatchingDurableStateAndPreservesReceiptOnCleanupFailure(bool writtenBeforeFailure){
  var w=new Wire();string journal=null,checkpoint=null;bool failCleanup=false;
  var client=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),raw=>{if(failCleanup){if(writtenBeforeFailure)journal=raw;throw new IOException("cleanup failed");}journal=raw;});
  await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
  var key=((JObject)JObject.Parse(journal)["receipts"]).Properties().Single().Name;var original=client.ReadRetainedStart(key);
  string receipt=original.Json,waiting=journal;int sent=w.Requests.Count;
  var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/retention-test",()=>checkpoint,raw=>checkpoint=raw);
  var complete=typeof(Ce03LocalClient).GetMethod("CompleteCheckpointHandoff");Assert.IsNotNull(complete,"Only a matching durable checkpoint can clear the handoff");
  Assert.Throws<TargetInvocationException>(()=>complete.Invoke(client,new object[]{original,store}));Assert.AreEqual(waiting,journal);
  var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>("Assets/_LASTRAIL/QA/CombatAssetBinding/89e63936213b4ce090e6a03fbb98bbe7/Binding.asset");
  using(var controller=new Ce03PlayerBattleController(original,asset,store,()=>true)){}
  var restored=store.RestoreLocalStartWithRevision(original,original.CreateVerifiedPlayerFactory(asset));
  long step=(long)JObject.Parse(restored.Simulation.CaptureLocalState())["clock"]["step"];restored.Simulation.Advance(step*12,12);store.SaveLocalStart(original,restored.Simulation,restored.Revision);
  string durable=checkpoint;var wrong=JObject.Parse(checkpoint);wrong["originalStart"]["key"]="different-original-key";checkpoint=Ce03CommandContract.Canonical(wrong);
  Assert.Throws<TargetInvocationException>(()=>complete.Invoke(client,new object[]{original,store}));Assert.AreEqual(waiting,journal);checkpoint=durable;failCleanup=true;
  Assert.Throws<TargetInvocationException>(()=>complete.Invoke(client,new object[]{original,store}));if(!writtenBeforeFailure)Assert.AreEqual(waiting,journal);else Assert.AreEqual(JTokenType.Null,JObject.Parse(journal)["awaitingCheckpoint"].Type);Assert.AreEqual(durable,checkpoint);
  failCleanup=false;complete.Invoke(client,new object[]{original,store});
  var reopened=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),_=>{},journal);
  using var flow=new Ce03LocalDeparture(reopened,_=>true,(_,__)=>{},_=>Assert.Fail("Already saved handoff must not reenter"));
  Assert.AreEqual(false,typeof(Ce03LocalDeparture).GetMethod("RestoreAwaitingCheckpoint").Invoke(flow,null));
  Assert.AreEqual(receipt,reopened.ReadRetainedStart(key).Json);Assert.AreEqual(durable,checkpoint);Assert.AreEqual(sent,w.Requests.Count);
 }
 [Test] public void RepeatedNetworkFailureStopsAfterOneRetryAndKeepsPending(){
  var w=new Wire{LostStarts=5};var client=Client(w);
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared));
  Assert.AreEqual(2,w.Requests.Count);Assert.IsTrue(client.HasPending);
 }
 [Test] public void ReopenedJournalDoesNotResetAutomaticRetryBudget(){
  var w=new Wire{LostStarts=10};string saved=null;
  var client=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),raw=>saved=raw);
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared));
  Assert.AreEqual(2,w.Requests.Count);var key=w.Requests[0].Headers.Single(h=>h.Name=="Idempotency-Key").Value;
  var reopened=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),raw=>saved=raw,saved);
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await reopened.RetryAsync());
  Assert.AreEqual(3,w.Requests.Count);Assert.AreEqual(key,w.Requests[2].Headers.Single(h=>h.Name=="Idempotency-Key").Value);
 }
 [Test] public async Task ReceiptStorageRetryDoesNotSendConfirmedRequestAgain(){
  var w=new Wire();bool fail=true;string saved=null;
  var client=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),raw=>{if(fail&&JObject.Parse(raw)["pending"].Type==JTokenType.Null){fail=false;throw new IOException("disk");}saved=raw;});
  Assert.ThrowsAsync<IOException>(async()=>await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared));
  Assert.IsTrue(client.HasPending);Assert.AreEqual(1,w.Requests.Count);await client.RetryAsync();
  Assert.AreEqual(1,w.Requests.Count);Assert.IsFalse(client.HasPending);Assert.AreEqual(JTokenType.Null,JObject.Parse(saved)["pending"].Type);
 }
 [Test] public async Task InitialStartJournalFailureRetainsSameRequestUntilSaved(){
  var w=new Wire();var writes=new List<string>();bool fail=true;
  var c=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),raw=>{writes.Add(raw);if(fail)throw new IOException("initial disk failure");});
  Assert.ThrowsAsync<IOException>(async()=>await c.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared));
  Assert.AreEqual(0,w.Requests.Count);Assert.IsTrue(c.HasPending);fail=false;await c.RetryAsync();
  Assert.AreEqual(writes[0],writes[1]);Assert.AreEqual(1,w.Requests.Count);Assert.IsFalse(c.HasPending);
 }
 [Test] public async Task OnlyUnsentUnstoredStartCanBeCancelled(){
  var w=new Wire();bool fail=true;
  var c=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),raw=>{if(fail)throw new IOException();});
  Assert.ThrowsAsync<IOException>(async()=>await c.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared));
  var cancel=typeof(Ce03LocalClient).GetMethod("CancelUnsentStart");Assert.IsNotNull(cancel);fail=false;
  Assert.AreEqual(true,cancel.Invoke(c,null));Assert.IsFalse(c.HasPending);Assert.AreEqual(0,w.Requests.Count);
  w.DelayedStart=new TaskCompletionSource<RunApiHttpResponse>();var sent=c.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
  Assert.AreEqual(false,cancel.Invoke(c,null));Assert.IsTrue(c.HasPending);w.DelayedStart.SetResult(w.StartReply);await sent;
  Assert.AreEqual(1,w.Requests.Count);
 }
 [Test] public async Task PlayerPrepareUsesExistingStartSenderAndRetainsOriginalReceipt(){
  var w=new Wire();var client=Client(w);await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
  Assert.AreEqual(1,w.Requests.Count);Assert.IsFalse(client.HasPending);
  Assert.AreEqual(1,((JObject)JObject.Parse(client.JournalJson)["receipts"]).Count);
 }
 [Test] public async Task RetainedPreparationReusesSupportWithoutAnotherGet(){
  var w=new Wire();using var flow=new Ce03LocalDeparture(Client(w),_=>true,(_,__)=>{},_=>{});
  var selection=new JObject{["operationId"]="opn_01_last_departure",["risk"]="I",["expectedFormationRevision"]=1,["expectedLoadoutRevision"]=1,["expectedUltimateRevision"]=1};
  flow.RestorePrepared(selection,w.Fixture.Prepared);
  await flow.PreviewAsync("opn_01_last_departure","I",1,1,1);
  Assert.IsTrue(flow.CanStart);Assert.AreEqual(1,w.Requests.Count);Assert.AreEqual("/v1/operations/preview",w.Requests[0].EndpointPath);
 }
 [Test] public async Task ConfirmedEntryFailureRetriesOnlyAssetsWithoutAnotherHttpRequest(){
  var w=new Wire();var client=Client(w);int entries=0;string original=null;
  var flow=new Ce03LocalDeparture(client,_=>true,(_,__)=>{},o=>{entries++;if(original==null)original=o.Json;else Assert.AreEqual(original,o.Json);if(entries==1)throw new IOException("injected missing asset");});
  await flow.PreviewAsync("opn_01_last_departure","I",1,1,1);await flow.StartAsync();
  Assert.AreEqual(1,entries);Assert.IsFalse(client.HasPending);Assert.IsFalse(flow.CanRetryStart);int sent=w.Requests.Count;string journal=client.JournalJson;
  var retry=typeof(Ce03LocalDeparture).GetMethod("RetryEntry");Assert.IsNotNull(retry,"Confirmed run needs an asset-only retry.");retry.Invoke(flow,null);
  Assert.AreEqual(2,entries);Assert.AreEqual(sent,w.Requests.Count);Assert.AreEqual(journal,client.JournalJson);await flow.StartAsync();Assert.AreEqual(sent,w.Requests.Count);
 }
 [Test] public async Task DisposedDepartureStoresLateReceiptButNeverEntersOrNotifiesOldView(){
  var w=new Wire{DelayedStart=new TaskCompletionSource<RunApiHttpResponse>()};var client=Client(w);int entries=0,changes=0;
  var flow=new Ce03LocalDeparture(client,_=>true,(_,__)=>{},_=>entries++);flow.Changed+=()=>changes++;
  await flow.PreviewAsync("opn_01_last_departure","I",1,1,1);var pending=flow.StartAsync();
  Assert.IsInstanceOf<IDisposable>(flow);((IDisposable)(object)flow).Dispose();int before=changes;
  w.DelayedStart.SetResult(w.StartReply);await pending;Assert.AreEqual(0,entries);Assert.AreEqual(before,changes);Assert.IsFalse(client.HasPending);
 }
 [Test] public async Task NewDepartureRestoresExactReceiptWithoutHttpOrNewStart(){
  var w=new Wire();var client=Client(w);
  await client.StartPreparedAsync((string)w.Fixture.Response["operationPreviewId"],w.Fixture.Prepared);
  string saved=client.JournalJson,key=((JObject)JObject.Parse(saved)["receipts"]).Properties().Single().Name;
  var restoredClient=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),_=>{},saved);
  int entries=0,sent=w.Requests.Count;
  using var flow=new Ce03LocalDeparture(restoredClient,_=>true,(_,__)=>{},o=>{entries++;Assert.AreEqual(client.ReadRetainedStart(key).Json,o.Json);});
  var restore=typeof(Ce03LocalDeparture).GetMethod("RestoreConfirmedStart");Assert.IsNotNull(restore,"Reopened departure needs exact-key original receipt restoration.");
  Assert.Throws<TargetInvocationException>(()=>restore.Invoke(flow,new object[]{"missing-key"}));Assert.IsFalse(flow.HasConfirmedStart);
  restore.Invoke(flow,new object[]{key});Assert.IsTrue(flow.HasConfirmedStart);Assert.IsTrue(flow.CanRetryEntry);Assert.IsFalse(flow.CanChangeSelection);
  await flow.StartAsync();Assert.AreEqual(0,entries);flow.RetryEntry();Assert.AreEqual(1,entries);
  Assert.AreEqual(sent,w.Requests.Count);Assert.AreEqual(saved,restoredClient.JournalJson);
 }
 [Test] public async Task UnsupportedEntryPreservesReceiptAndNeverRepeatsAutomatically(){
  var w=new Wire();var client=Client(w);int entries=0;
  using(var flow=new Ce03LocalDeparture(client,_=>true,(_,__)=>{},_=>{entries++;throw new NotSupportedException("internal unsupported build");})){
   await flow.PreviewAsync("opn_01_last_departure","I",1,1,1);await flow.StartAsync();string journal=client.JournalJson;int count=w.Requests.Count;
   Assert.AreEqual(Ce03EntryStatus.UpdateRequired,flow.EntryStatus);Assert.IsFalse(flow.CanRetryEntry);Assert.IsTrue(flow.Notice.Contains("업데이트"));Assert.IsFalse(flow.Notice.Contains("internal"));
   flow.RetryEntry();await flow.StartAsync();Assert.AreEqual(1,entries);Assert.AreEqual(count,w.Requests.Count);Assert.AreEqual(journal,client.JournalJson);
  }
 }
 [Test] public async Task ExistingDepartureButtonRetriesConfirmedEntryWithoutHttp(){
  var w=new Wire();int entries=0;using var flow=new Ce03LocalDeparture(Client(w),_=>true,(_,__)=>{},_=>{if(++entries==1)throw new IOException("missing");});
  await flow.PreviewAsync("opn_01_last_departure","I",1,1,1);await flow.StartAsync();int sent=w.Requests.Count;
  var root=new UnityEngine.GameObject("ConfirmedRetryUi",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));var safe=new UnityEngine.GameObject("SafeArea",typeof(UnityEngine.RectTransform));safe.transform.SetParent(root.transform,false);
  try{
   var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
   var deck=LastRail.Presentation.MissionShell.LobbyCommandDeck.Attach(root,font,"QA",0,new[]{new LastRail.Presentation.MissionShell.MissionShellMissionRowState("opn_01_last_departure","QA",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},null,null);deck.BindLocalDeparture(flow);deck.OpenOperations();
   root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="SelectDeparture0").onClick.Invoke();
   var button=root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="ConfirmDeparture");Assert.IsTrue(button.interactable);button.onClick.Invoke();Assert.AreEqual(2,entries);Assert.AreEqual(sent,w.Requests.Count);
   deck.BindLocalDeparture(null);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
