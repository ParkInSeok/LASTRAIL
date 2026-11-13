using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using LastRail.Auth;
using LastRail.Presentation.AugmentSelection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace LastRail.Tests.EditMode {
 public class BNativeLifestealTests {
  // Original native executor and unchanged stats; selection/route envelopes are
  // explicitly synthetic. This is not evidence of live issuance or admission.
  internal static BRouteBinding Route(BScoutGunnerNodeSnapshot source){
   var p=(JObject)BRouteConsumerTests.Sample()["pendingBinding"].DeepClone();var raw=source.Capture();
   foreach(string k in new[]{"accountId","runId","operationId","selectedRisk"})p[k]=raw["origin"][k].DeepClone();
   p["originalNativeOrigin"]=raw["origin"].DeepClone();p["nativeContextId"]=raw["nativeContextId"].DeepClone();p["nativeNodeSource"]=raw;p["pendingNode"]["nodeId"]=source.NodeId;p["pendingNode"]["visitDepth"]=source.VisitDepth;p["pendingNode"]["completedWaveContract"]["nodeId"]=source.NodeId;
   p["phase"]="ACTIVE";p["runStatus"]="ACTIVE";p["currentNode"]=p["pendingNode"].DeepClone();p["pendingNode"]=null;p["pendingEdgeId"]=null;
   Pin(p);return BRouteBinding.Read(p.ToString(),(string)p["packetHash"],(string)p["accountId"],(string)p["runId"]);
  }
  [Test] public void PendingArrivalCannotInstallApplied(){
   var source=BWaveBoardingRuntimeTests.Source();var b=BWaveBoardingRuntimeTests.WaveBinding(source);var loop=BDirectAttackBasicLoop.CreateWaveRuntime(b,source,BWaveBoardingRuntimeTests.Paths(b.Composition.Attack),10000);var route=Route(source);var p=route.Capture();p["phase"]="TRAVELLING";p["pendingNode"]=p["currentNode"].DeepClone();p["pendingEdgeId"]="pending-edge";p["currentNode"]=null;Pin(p);var pending=BRouteBinding.Read(p.ToString(),(string)p["packetHash"],route.AccountId,route.RunId);var before=loop.Capture();
   Assert.Throws<TargetInvocationException>(()=>Apply(loop,pending,Original(route)));Assert.IsTrue(JToken.DeepEquals(before,loop.Capture()));
  }
  [Test] public async System.Threading.Tasks.Task PersistentNodeRetainsOriginalAfterSaveFailureAndRecoversLostAck(){
   var configure=typeof(BRouteSessionController).GetMethod("ConfigureLocalCheckpoint");Assert.NotNull(configure,"Node checkpoint installation missing");
   var source=BBoardingEventsTests.Source(0);var route=Route(source.OriginalSource);var b=BRouteNativeTests.Binding(source);var g=BOrdinaryGeometryTests.Geometry(b);var m=global::CombatRailJourneyTests.Config();m["offers"]=new JArray();m["throttleMaximumBps"]=13000;m["throttleCruiseBps"]=10000;var motion=BTravelRuntimeConfiguration.LocalValidation(m,BDirectAttackInput.Digest(m));var composition=new BRouteNativeComposition(route,source,b,g,motion,10000);
   JObject saved=null;bool loseAck=false,failBefore=false;long storedRevision=0;
   using(var c=new BRouteSessionController(route.AccountId,route.RunId)){
    await c.RefreshAsync(()=>System.Threading.Tasks.Task.FromResult(route));composition.AttachTo(c);
    Action<JObject,long> save=(p,expected)=>{if(failBefore)throw new System.IO.IOException("before save");Assert.AreEqual(storedRevision,expected);saved=(JObject)p.DeepClone();storedRevision++;if(loseAck)throw new System.IO.IOException("lost save ACK");};
    configure.Invoke(c,new object[]{"native-lifesteal-checkpoint",new Func<JObject>(()=>saved==null?null:(JObject)saved.DeepClone()),save,new Func<JObject,BDirectAttackBasicLoop>(composition.RestoreNative)});
    var apply=typeof(BRouteSessionController).GetMethod("ApplyO22");Assert.NotNull(apply);var original=Original(route);var before=c.Capture();failBefore=true;
    Assert.Throws<TargetInvocationException>(()=>apply.Invoke(c,new object[]{original}));Assert.IsTrue(JToken.DeepEquals(before,c.Capture()));failBefore=false;
    typeof(BRouteSessionController).GetMethod("RecoverLocalCheckpoint").Invoke(c,null);loseAck=true;
    Assert.Throws<TargetInvocationException>(()=>apply.Invoke(c,new object[]{original}));Assert.IsTrue(JToken.DeepEquals(before,c.Capture()));Assert.Throws<InvalidOperationException>(()=>c.Advance("blocked",10000,1));
    Assert.Throws<InvalidOperationException>(()=>c.CaptureOriginalRuntimeObservation(),"An uncertain saved HP state must not be reported as a completed original");
    loseAck=false;typeof(BRouteSessionController).GetMethod("RecoverLocalCheckpoint").Invoke(c,null);var recovered=c.Capture();apply.Invoke(c,new object[]{original});Assert.IsTrue(JToken.DeepEquals(recovered,c.Capture()));Assert.AreEqual(original.ApplicationId,(string)recovered["nodeSession"]["runtime"]["state"]["direct"]["o22Application"]["applicationId"]);
   }
  }
  [Test] public async System.Threading.Tasks.Task DisposalDuringSaveDoesNotResurrectNativeOwner(){
   var source=BBoardingEventsTests.Source(0);var route=Route(source.OriginalSource);var b=BRouteNativeTests.Binding(source);var g=BOrdinaryGeometryTests.Geometry(b);var m=global::CombatRailJourneyTests.Config();m["offers"]=new JArray();m["throttleMaximumBps"]=13000;m["throttleCruiseBps"]=10000;var motion=BTravelRuntimeConfiguration.LocalValidation(m,BDirectAttackInput.Digest(m));var composition=new BRouteNativeComposition(route,source,b,g,motion,10000);
   using(var c=new BRouteSessionController(route.AccountId,route.RunId)){
    await c.RefreshAsync(()=>System.Threading.Tasks.Task.FromResult(route));composition.AttachTo(c);JObject saved=null;bool close=false;
    c.ConfigureLocalCheckpoint("dispose-during-save",()=>saved,(p,_)=>{saved=(JObject)p.DeepClone();if(close)c.Dispose();},composition.RestoreNative);
    var node=typeof(BRouteSessionController).GetField("node",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);close=true;try{c.SetPaused("close-in-save",true);}catch(ObjectDisposedException){}
    Assert.IsNull(typeof(BNodeSessionController).GetField("runtime",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(node),"Disposed node must not reacquire a simulation after callback returns");
   }
  }
  sealed class Wire:IGameApiAuthenticatedHttpTransport {
   internal string Reply,ArrivalReply;internal JObject ActiveRoute;internal int Calls,ArrivalCalls,RouteReads;internal System.Threading.Tasks.TaskCompletionSource<GameApiAuthenticatedHttpResult> SelectionWait;
   public System.Threading.Tasks.Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request,GameApiAccessToken token){
    if(request.Method=="GET"){Assert.IsTrue(request.EndpointPath.EndsWith("/b-route-binding"));RouteReads++;return System.Threading.Tasks.Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,ActiveRoute.ToString(),new[]{new RunApiHttpHeader("ETag",(string)ActiveRoute["packetHash"])})));}
    if(request.EndpointPath.EndsWith("/b-route/transition")){ArrivalCalls++;return System.Threading.Tasks.Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,ArrivalReply)));}
    Calls++;return SelectionWait?.Task??System.Threading.Tasks.Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply)));
   }
  }
  [TestCase(false,false,false)][TestCase(true,false,false)][TestCase(true,true,false)][TestCase(true,false,true)] public async System.Threading.Tasks.Task ActualScreenSelectionCommitsNativeApplicationAndRecoversSameRequestAfterLostSaveAck(bool currentCatalog,bool loseResumeAck,bool closeSelection){
   var hostType=typeof(AugmentSelectionRuntime).Assembly.GetType("LastRail.Presentation.AugmentSelection.BRouteAugmentScreen");Assert.NotNull(hostType,"Actual node screen host missing");
   var source=BBoardingEventsTests.Source(0);var route=Route(source.OriginalSource);var b=BRouteNativeTests.Binding(source);var g=BOrdinaryGeometryTests.Geometry(b);var m=global::CombatRailJourneyTests.Config();m["offers"]=new JArray();m["throttleMaximumBps"]=13000;m["throttleCruiseBps"]=10000;var motion=BTravelRuntimeConfiguration.LocalValidation(m,BDirectAttackInput.Digest(m));var composition=new BRouteNativeComposition(route,source,b,g,motion,10000);
   var pendingPacket=route.Capture();pendingPacket["phase"]="TRAVELLING";pendingPacket["pendingNode"]=pendingPacket["currentNode"].DeepClone();pendingPacket["pendingNode"]["eventKind"]="AUGMENT";pendingPacket["currentNode"]=null;pendingPacket["pendingEdgeId"]="fixture-arrival-edge";Pin(pendingPacket);var pendingRoute=BRouteBinding.Read(pendingPacket.ToString(),(string)pendingPacket["packetHash"],route.AccountId,route.RunId);
   var activePacket=route.Capture();activePacket["currentNode"]["eventKind"]="AUGMENT";activePacket["mapRevision"]=route.MapRevision+1;Pin(activePacket);route=BRouteBinding.Read(activePacket.ToString(),(string)activePacket["packetHash"],route.AccountId,route.RunId);
   var arrival=BRouteCommand.FromOriginal(pendingRoute,new JObject{["kind"]="ARRIVE",["eventId"]="fixture-arrival",["expectedRevision"]=pendingRoute.MapRevision,["edgeId"]="fixture-arrival-edge",["evidenceRef"]="FIXTURE_NOT_OPERATING_AUTHORITY"});
   var root=new GameObject("Actual B node selection host",typeof(RectTransform),typeof(Canvas));
   try{using(var c=new BRouteSessionController(route.AccountId,route.RunId)){
    await c.RefreshAsync(()=>System.Threading.Tasks.Task.FromResult(pendingRoute));bool lost=false;
    string nativeDirectory="D:/Project/LASTRAIL/LocalTemp/native-node-"+Guid.NewGuid().ToString("N");var file=new LastRail.Presentation.RoofBattle.RoofBattleLocalFileStore(nativeDirectory,"ui-native",BNodeSessionController.CheckpointVersion);
    var host=root.AddComponent(hostType);
    hostType.GetMethod("AttachLocalNative").Invoke(host,new object[]{composition,c,"ui-native",new Func<JObject>(file.Load),new Action<JObject,long>((p,expected)=>{file.Save(p,expected);var last=p["node"]["runtime"]["commands"].Last;if(lost&&(!loseResumeAck||(string)last?["kind"]=="HOLD"&&(bool)last["held"]==false))throw new System.IO.IOException("ACK lost after durable native save");})});
    c.Advance("native-enemy-before-selection",100000000,10000);
    var original=Original(route,currentCatalog);var packet=JObject.Parse(original.Capture());var confirmed=JObject.Parse(original.SelectionJson);var initial=(JObject)confirmed.DeepClone();initial["state"]="SELECTING";initial["grantRevision"]=0;initial["selectedCandidateId"]=null;
    var wire=new Wire{Reply=new JObject{["version"]="combat.augment-select.v1.20261019",["committed"]=true,["result"]=new JObject{["outcome"]="SELECTED",["snapshot"]=confirmed,["approval"]=null,["applicationOriginal"]=packet}}.ToString()};
    wire.ActiveRoute=route.Capture();wire.ArrivalReply=new JObject{["version"]=BRouteBinding.Version,["committed"]=true,["receipt"]=new JObject{["version"]="operation.map-receipt.v1",["eventId"]=arrival.EventId,["revision"]=route.MapRevision,["state"]=new JObject{["version"]="operation.map-progress.v1",["traversedEdgeIds"]=new JArray("fixture-arrival-edge"),["pendingEdgeId"]=null,["phase"]="ACTIVE",["holdIds"]=new JArray()},["verificationId"]="FIXTURE_NOT_OPERATING_AUTHORITY"},["augment"]=initial}.ToString();
    var auth=new GameApiAuthSessionSnapshot(route.AccountId,"ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));var scope=new AugmentSelectionScope(new AugmentSelectionOwner(route.AccountId,"TEST"),route.RunId,"grant");
    string savedOriginal=null,path="D:/Project/LASTRAIL/LocalTemp/native-selection-"+Guid.NewGuid().ToString("N");var store=new CombatO22ApplicationStore(path+".original",()=>savedOriginal,s=>savedOriginal=s);
    var crypto=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(global::CombatO22StoreTests).GetNestedType("TestCrypto",BindingFlags.NonPublic),true);
    Func<string,string,AugmentCardPresentation> definitions=(version,id)=>id=="O22"?HealingAugmentPresentation.FromPinned(version,(JObject)packet["definition"]["selectedDefinition"],(JObject)packet["definition"]["composition"]):new AugmentCardPresentation(id,"시험 후보","시험 정의",null);
    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
    await (System.Threading.Tasks.Task)hostType.GetMethod("ArriveAsync").Invoke(host,new object[]{c,arrival,scope.Owner,wire,new Func<GameApiAuthSessionSnapshot>(()=>auth),new Func<bool>(()=>true),crypto,path,store,definitions,font});
    Assert.AreEqual(1,wire.ArrivalCalls);Assert.AreEqual(1,wire.RouteReads);Assert.AreEqual(0,wire.Calls);
    Assert.IsTrue((bool)c.Capture()["nodeSession"]["runtime"]["state"]["direct"]["clock"]["paused"],"Selection must hold the original native clock until durable APPLIED");
    Button Button(string name)=>root.GetComponentsInChildren<Button>(true).Single(x=>x.name==name);
    if(!closeSelection){Button("Select1").onClick.Invoke();Assert.AreEqual(0,wire.Calls,"Unsupported native effect must be blocked before sending a choice");}
    int expectedCalls=closeSelection?1:2;
    if(closeSelection){
     wire.SelectionWait=new System.Threading.Tasks.TaskCompletionSource<GameApiAuthenticatedHttpResult>();Button("Select0").onClick.Invoke();root.SetActive(false);
     Assert.IsNotNull(new AugmentSelectionFileJournal(scope,crypto,path).Load(scope.StorageKey));Assert.IsTrue((bool)c.Capture()["nodeSession"]["runtime"]["state"]["direct"]["clock"]["paused"]);
     wire.SelectionWait.SetResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,wire.Reply)));
     for(int i=0;i<40&&savedOriginal==null;i++)await System.Threading.Tasks.Task.Yield();
     Assert.IsNotNull(savedOriginal,"Closing the View must not discard the transaction owner's arriving original");
     for(int i=0;i<40&&new AugmentSelectionFileJournal(scope,crypto,path).Load(scope.StorageKey)!=null;i++)await System.Threading.Tasks.Task.Yield();
     Assert.IsNull(new AugmentSelectionFileJournal(scope,crypto,path).Load(scope.StorageKey));
     root.SetActive(true);
     await (System.Threading.Tasks.Task)hostType.GetMethod("ArriveAsync").Invoke(host,new object[]{c,arrival,scope.Owner,wire,new Func<GameApiAuthSessionSnapshot>(()=>auth),new Func<bool>(()=>true),crypto,path,store,definitions,font});
     Assert.AreEqual(1,wire.Calls);Assert.AreEqual(1,wire.RouteReads);
    }else{
    var before=c.Capture();lost=true;Button("Select0").onClick.Invoke();Assert.IsNotNull(savedOriginal);if(!loseResumeAck)Assert.IsTrue(JToken.DeepEquals(before,c.Capture()));Assert.IsTrue((bool)c.Capture()["nodeSession"]["runtime"]["state"]["direct"]["clock"]["paused"]);Assert.IsNotNull(new AugmentSelectionFileJournal(scope,crypto,path).Load(scope.StorageKey));
    lost=false;((BRouteAugmentScreen)host).Close();
    await (System.Threading.Tasks.Task)hostType.GetMethod("ArriveAsync").Invoke(host,new object[]{c,arrival,scope.Owner,wire,new Func<GameApiAuthSessionSnapshot>(()=>auth),new Func<bool>(()=>true),crypto,path,store,definitions,font});
    Assert.IsNotNull(((BRouteAugmentScreen)host).View,"Stored acquisition must keep original request cleanup reachable until the journal is completed");
    Assert.AreEqual(1,wire.Calls);Assert.AreEqual(1,wire.RouteReads);
    Button("Retry").onClick.Invoke();var applied=c.Capture();Assert.AreEqual(2,wire.Calls);Assert.IsNull(new AugmentSelectionFileJournal(scope,crypto,path).Load(scope.StorageKey));Assert.IsTrue(JToken.DeepEquals(applied,c.Capture()));Assert.AreEqual(original.ApplicationId,(string)applied["nodeSession"]["runtime"]["state"]["direct"]["o22Application"]["applicationId"]);
    }
    Assert.IsNull(((BRouteAugmentScreen)host).View,"Verified APPLIED/checkpoint returns to combat, not an old confirmed selection screen");
    Assert.AreEqual("COMBAT_READY",((BRouteAugmentScreen)host).ArrivalState);
    var native=(JObject)c.Capture()["nodeSession"]["runtime"];Assert.IsFalse((bool)native["state"]["direct"]["clock"]["paused"],"Durable APPLIED releases only the selection hold");var hp=native["state"]["direct"]["allies"]["vitals"]["op_kyle"];int hpBefore=(int)hp["hp"];Assert.Greater(hpBefore,0);Assert.Less(hpBefore,(int)hp["maxHp"],"Native enemy actions must create the actual deficit");bool attacked=false;
    foreach(var life in ((JArray)native["boarding"]["lives"]).Where(x=>(bool)x["registered"]&&(bool)x["targetable"])){
     string target=(string)life["id"];var frame=c.ObserveOrdinaryGeometry("op_kyle",target,(long)native["state"]["direct"]["clock"]["completedSteps"],(long)native["spatialSource"]["revision"],g.Hash);if(!(bool)frame["eligible"])continue;
     c.ApplyWaveBasic("selected-o22-real-basic","op_kyle",target,(long)life["generation"]);var after=c.Capture();var hit=after["nodeSession"]["runtime"]["results"].Last["hits"][0];int gain=(int)hit["lifesteal"]["actualHpGain"];Assert.Greater(gain,0);Assert.AreEqual((int)hit["hp"]["hpLoss"]*12/100,gain);Assert.AreEqual(hpBefore+gain,(int)after["nodeSession"]["runtime"]["state"]["direct"]["allies"]["vitals"]["op_kyle"]["hp"]);
     Assert.IsTrue(JToken.DeepEquals(after["nodeSession"],file.Load()["node"]));TestContext.WriteLine("Native selection/action/disk evidence: "+nativeDirectory+"; actual damage="+hit["hp"]["hpLoss"]+"; actual heal="+gain+"; before="+hpBefore);
     using(var restarted=new BRouteSessionController(route.AccountId,route.RunId)){await restarted.RefreshAsync(()=>System.Threading.Tasks.Task.FromResult(route));composition.AttachPersistentTo(restarted,"ui-native",file.Load,file.Save);Assert.IsTrue(JToken.DeepEquals(after["nodeSession"],restarted.Capture()["nodeSession"]));var restored=restarted.Capture();restarted.ApplyWaveBasic("selected-o22-real-basic","op_kyle",target,(long)life["generation"]);Assert.IsTrue(JToken.DeepEquals(restored,restarted.Capture()));}
     attacked=true;break;
    }
    Assert.IsTrue(attacked,"Original geometry must supply an actual eligible target");
    root.SetActive(false);Assert.AreEqual(expectedCalls,wire.Calls);
   }}finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  static string Hash(JToken p)=>BDirectAttackInput.Digest(p).Substring(7);
  static void Pin(JObject p){p.Remove("packetHash");p["packetHash"]=Hash(p);}
  internal static CombatO22ApplicationOriginal Original(BRouteBinding route,bool currentCatalog=false){
   var p=currentCatalog?global::O22CatalogV8Tests.Packet():global::CombatO22OriginalTests.Packet();var d=(JObject)p["definition"];var s=(JObject)d["selectionSource"];string start=(string)route.Capture()["startSnapshotHash"];
   foreach(var x in new[]{p,d,s}){x["accountId"]=route.AccountId;x["runId"]=route.RunId;}
   s["stageId"]=route.NativeNode.NodeId;s["selection"]["stageId"]=route.NativeNode.NodeId;
   d["startSnapshotHash"]=start;s["startSnapshotHash"]=start;s["selection"]["runId"]=route.RunId;Pin(s);Pin(d);
   p["applicationId"]="o22:"+Hash(new JObject{["accountId"]=route.AccountId,["runId"]=route.RunId,["eventId"]="grant",["environment"]="TEST",["expectedStoreRevision"]=2,["expectedCatalogVersion"]=s["catalogVersion"].DeepClone(),["expectedStartSnapshotHash"]=start});
   return CombatO22ApplicationOriginal.Read(p.ToString(),route.AccountId,route.RunId,"grant","TEST",start);
  }
  static void Apply(BDirectAttackBasicLoop loop,BRouteBinding route,CombatO22ApplicationOriginal original){var m=typeof(BDirectAttackBasicLoop).GetMethod("ApplyO22");Assert.NotNull(m,"Native APPLIED command missing");m.Invoke(loop,new object[]{route,original});}
  [TestCase(false)][TestCase(true)] public void SelectedOriginalChangesActualNativeHitHpAndRestoresWithoutSecondHeal(bool currentCatalog){
   Assert.NotNull(typeof(BDirectAttackBasicLoop).GetMethod("ApplyO22"),"Native APPLIED command missing");
   var source=BWaveBoardingRuntimeTests.Source();var binding=BWaveBoardingRuntimeTests.ManualOperators(BWaveBoardingRuntimeTests.WaveBinding(source,BWaveBoardingRuntimeTests.HitPacket(source,true,true)));var paths=BWaveBoardingRuntimeTests.Paths(binding.Composition.Attack);
   var loop=BDirectAttackBasicLoop.CreateWaveRuntime(binding,source,paths,10000);var route=Route(source);var original=Original(route,currentCatalog);
   loop.Advance("enemy-first",100000000,10000);int before=loop.Hp("op_mira");Assert.Greater(before,0);Assert.Less(before,1520,"Actual enemy hit must create the deficit");
   Apply(loop,route,original);var target=source.Waves[0].Instances.First(i=>loop.Hp(i.InstanceId)>0);
   var result=loop.ApplyWaveBasic("original-hit","op_mira",target.InstanceId,1);var hit=result.Hits.Single().Attack;
   int expected=(int)((long)hit.Hp.HpLoss*1200/10000);Assert.Greater(expected,0);Assert.AreEqual(before+expected,loop.Hp("op_mira"));
   var saved=loop.Capture();Apply(loop,route,original);Assert.IsTrue(JToken.DeepEquals(saved,loop.Capture()));
   var restored=BDirectAttackBasicLoop.RestoreWaveRuntime(saved,BDirectAttackInput.Digest(saved),binding,source,paths,10000);Assert.IsTrue(JToken.DeepEquals(saved,restored.Capture()));
   restored.ApplyWaveBasic("original-hit","op_mira",target.InstanceId,1);Assert.IsTrue(JToken.DeepEquals(saved,restored.Capture()));
   var corrupt=(JObject)saved.DeepClone();corrupt["state"]["direct"]["allies"]["vitals"]["op_mira"]["hp"]=(int)corrupt["state"]["direct"]["allies"]["vitals"]["op_mira"]["hp"]+1;
   Assert.Catch(()=>BDirectAttackBasicLoop.RestoreWaveRuntime(corrupt,BDirectAttackInput.Digest(corrupt),binding,source,paths,10000));
  }
 }
}
