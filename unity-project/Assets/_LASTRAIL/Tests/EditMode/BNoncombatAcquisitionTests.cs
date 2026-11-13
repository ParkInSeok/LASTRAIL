using System;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode {
 public class BNoncombatAcquisitionTests {
  static void Pin(JObject p){p.Remove("packetHash");p["packetHash"]=BDirectAttackInput.Digest(p).Substring(7);}
  static BRouteBinding Augment(BRouteBinding combat){
   var p=combat.Capture();p["nativeNodeSource"]=null;p["nodeSourceStatus"]="ORIGINAL_NODE_REFS_ONLY";
   p["currentNode"]["nodeId"]="fixture-noncombat-augment";p["currentNode"]["eventKind"]="AUGMENT";
   p["currentNode"]["completedWaveContract"]["nodeId"]="fixture-noncombat-augment";
   p["currentNode"]["completedWaveContract"]["waves"]=null;Pin(p);
   return BRouteBinding.Read(p.ToString(),(string)p["packetHash"],combat.AccountId,combat.RunId);
  }
  static CombatO22ApplicationOriginal Original(BRouteBinding combat){
   var p=JObject.Parse(BNativeLifestealTests.Original(combat,true).Capture());var d=(JObject)p["definition"];var s=(JObject)d["selectionSource"];
   s["stageId"]="fixture-noncombat-augment";s["selection"]["stageId"]="fixture-noncombat-augment";Pin(s);Pin(d);
   return CombatO22ApplicationOriginal.Read(p.ToString(),combat.AccountId,combat.RunId,"grant","TEST",(string)combat.Capture()["startSnapshotHash"]);
  }
  [Test] public async Task NoncombatSelectionDoesNotRequireOrInventNativeCombat(){
   var route=Augment(BNativeLifestealTests.Route(BBoardingEventsTests.Source(0).OriginalSource));
   using(var c=new BRouteSessionController(route.AccountId,route.RunId)){
    await c.RefreshAsync(()=>Task.FromResult(route));var before=c.Capture();
    Assert.DoesNotThrow(()=>c.BeginAugmentSelection("grant"));
    Assert.IsTrue(JToken.DeepEquals(before,c.Capture()));Assert.IsNull(c.LocalCombatOutcome);
    Assert.Throws<InvalidOperationException>(()=>c.Advance("cannot-run",10000,1));
   }
  }
  [TestCase(false,false)][TestCase(true,false)][TestCase(true,true)] public async Task NextCombatInstallsStoredAcquisitionBeforePublicationAndRecoversLostSaveAck(bool loseAck,bool afterApplied){
   var method=typeof(BRouteNativeComposition).GetMethod("AttachPersistentWithAcquisitionTo");
   Assert.NotNull(method,"Next-combat original acquisition attachment missing");
   var source=BBoardingEventsTests.Source(0);var route=BNativeLifestealTests.Route(source.OriginalSource);
   var b=BRouteNativeTests.Binding(source);var g=BOrdinaryGeometryTests.Geometry(b);
   var m=global::CombatRailJourneyTests.Config();m["offers"]=new JArray();m["throttleMaximumBps"]=13000;m["throttleCruiseBps"]=10000;
   var motion=BTravelRuntimeConfiguration.LocalValidation(m,BDirectAttackInput.Digest(m));
   var composition=new BRouteNativeComposition(route,source,b,g,motion,10000);
   var original=Original(route);string raw=original.Capture();
   string path="D:/Project/LASTRAIL/LocalTemp/noncombat-acquisition-"+Guid.NewGuid().ToString("N");
   var originals=new CombatO22ApplicationStore(path+".original",()=>raw,_=>Assert.Fail("Reuse the already committed acquisition"));
   var file=new LastRail.Presentation.RoofBattle.RoofBattleLocalFileStore(path,"next-combat",BNodeSessionController.CheckpointVersion);
   using(var c=new BRouteSessionController(route.AccountId,route.RunId)){
    await c.RefreshAsync(()=>Task.FromResult(Augment(route)));
    var next=route.Capture();next["mapRevision"]=route.MapRevision+1;Pin(next);
    route=BRouteBinding.Read(next.ToString(),(string)next["packetHash"],route.AccountId,route.RunId);
    composition=new BRouteNativeComposition(route,source,b,g,motion,10000);
    await c.RefreshAsync(()=>Task.FromResult(route));var before=c.Capture();bool drop=loseAck;int published=0;
    c.Bind(p=>{if(p["nodeSession"]["runtime"].Type!=JTokenType.Null){published++;Assert.AreEqual(original.ApplicationId,(string)p["nodeSession"]["runtime"]["state"]["direct"]["o22Application"]["applicationId"],"No unbuffed intermediate runtime may be exposed");}});
    Action<JObject,long> save=(p,expected)=>{file.Save(p,expected);if(drop&&(!afterApplied||p["node"]["runtime"]["state"]["direct"]["o22Application"]?.Type==JTokenType.Object))throw new System.IO.IOException("Saved ACK lost");};
    void Attach()=>method.Invoke(composition,new object[]{c,"next-combat",new Func<JObject>(file.Load),save,originals,"TEST"});
    if(loseAck){Assert.Throws<TargetInvocationException>(()=>Attach());Assert.IsTrue(JToken.DeepEquals(before,c.Capture()));Assert.AreEqual(0,published);Assert.Throws<InvalidOperationException>(()=>c.Advance("blocked",10000,1));drop=false;}
    Attach();Assert.AreEqual(1,published);
    var state=c.Capture()["nodeSession"]["runtime"]["state"]["direct"]["o22Application"];
    Assert.AreEqual(source.OriginalSource.NodeId,(string)state["nodeId"]);
    Assert.AreEqual(original.ApplicationId,(string)state["applicationId"]);
    Assert.AreNotEqual(source.OriginalSource.NodeId,(string)JObject.Parse(original.SelectionJson)["stageId"]);
    var saved=c.Capture();c.ApplyO22(original);Assert.IsTrue(JToken.DeepEquals(saved,c.Capture()),"Repeated acquisition must not add effects");
    using(var restarted=new BRouteSessionController(route.AccountId,route.RunId)){
     await restarted.RefreshAsync(()=>Task.FromResult(route));
     method.Invoke(composition,new object[]{restarted,"next-combat",new Func<JObject>(file.Load),new Action<JObject,long>(file.Save),originals,"TEST"});
     Assert.IsTrue(JToken.DeepEquals(saved["nodeSession"],restarted.Capture()["nodeSession"]),"Restart reuses the same durable effect and original context");
    }
   }
  }
 }
}
