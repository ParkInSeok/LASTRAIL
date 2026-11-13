#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public sealed class BNativeNodeHttpTests {
  static IEnumerator Wait(Task t){var end=DateTime.UtcNow.AddSeconds(30);while(!t.IsCompleted){Assert.Less(DateTime.UtcNow,end);yield return null;}}
  [UnityTest] public IEnumerator SameAuthenticatedNodeDrivesOriginalNativeDamageHazardsAndRestore(){
   var path=Environment.GetEnvironmentVariable("LASTRAIL_NATIVE_CONNECTION");if(string.IsNullOrEmpty(path))Assert.Ignore("Explicit isolated native source fixture required");var cfg=JObject.Parse(File.ReadAllText(path));var run=(string)cfg["runId"];var token=new GameApiAccessToken((string)cfg["token"]);
   using(var a=new UnityWebRequestRunApiHttpAdapter((string)cfg["origin"],10))using(var late=new UnityWebRequestRunApiHttpAdapter((string)cfg["delayedOrigin"],10))using(var c=new BRouteSessionController("a",run)){
    var transport=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(a));var client=new BNativeNodeSourceHttpClient(transport);var entry=client.ReadAsync("a",run,token);yield return Wait(entry);Assert.IsTrue(entry.IsFaulted);StringAssert.Contains("node_unsupported",entry.Exception.InnerException.Message);
    var intent=new BOperationDestinationIntent("a",run,"select",1,"entry:n1-0");var destination=new BOperationDestinationClient(transport);var choose=c.ConfirmDestinationAsync(intent,()=>destination.ConfirmAsync(intent,token));yield return Wait(choose);Assert.IsTrue(choose.GetAwaiter().GetResult());
    var get=client.ReadAsync("a",run,token);yield return Wait(get);var bundle=get.GetAwaiter().GetResult();var replay=client.ReadAsync("a",run,token,bundle.Hash);yield return Wait(replay);Assert.IsTrue(JToken.DeepEquals(bundle.Capture(),replay.GetAwaiter().GetResult().Capture()));
    var auth=client.ReadAsync("a",run,new GameApiAccessToken("lst1.invalid-native"));yield return Wait(auth);Assert.IsTrue(auth.IsFaulted);StringAssert.Contains("AuthRequired",auth.Exception.InnerException.Message);var stale=client.ReadAsync("a",run,token,new string('0',64));yield return Wait(stale);Assert.IsTrue(stale.IsFaulted);StringAssert.Contains("409",stale.Exception.InnerException.Message);
    var refresh=c.RefreshAsync(()=>Task.FromResult(bundle.Route));yield return Wait(refresh);Assert.IsTrue(refresh.GetAwaiter().GetResult());
    // LOCAL original owned-stat/buff/geometry/motion adapters; server-provided node and v2 remain unchanged.
    var binding=BRouteNativeTests.Binding(bundle.Boarding);var geometry=BOrdinaryGeometryTests.Geometry(binding);var rail=global::CombatRailJourneyTests.Config();rail["offers"]=new JArray();rail["throttleMaximumBps"]=13000;rail["throttleCruiseBps"]=10000;var motion=BTravelRuntimeConfiguration.LocalValidation(rail,BDirectAttackInput.Digest(rail));var composition=new BRouteNativeComposition(bundle.Route,bundle.Boarding,binding,geometry,motion,10000);composition.AttachTo(c);
    Assert.IsTrue(JToken.DeepEquals(bundle.Route.NativeNode.Capture(),bundle.Boarding.OriginalSource.Capture()));Assert.AreEqual(2,bundle.Boarding.OriginalSource.Waves.Count);Assert.Throws<InvalidOperationException>(()=>bundle.RequireAcceptedTiming());
    int manualHits=0;var damage=new JArray();c.Advance("http-native-entry",40000000,1000);
    for(int step=0;step<120;step++){
     var n=(JObject)c.Capture()["nodeSession"]["runtime"];if((string)((JArray)n["results"]).Last["outcome"]!="RUNNING")break;long tick=(long)n["state"]["direct"]["clock"]["completedSteps"];
     bool busy=((JArray)n["gunnerIntercept"]["interception"]["actions"]).Any(x=>(string)x["actorId"]=="op_kyle"&&((string)x["phase"]=="PREPARATION"||(string)x["phase"]=="RECOVERY"&&(long)x["endTick"]>tick));
     if(!busy&&(long)n["state"]["direct"]["next"]["op_kyle"]<=tick){foreach(var life in ((JArray)n["boarding"]["lives"]).Where(x=>(bool)x["registered"]&&(bool)x["targetable"]).ToArray()){string id=(string)life["id"];int before=(int)n["state"]["direct"]["enemies"][id]["hp"];if(before<=0)continue;var frame=c.ObserveOrdinaryGeometry("op_kyle",id,tick,(long)n["spatialSource"]["revision"],geometry.Hash);if(!(bool)frame["eligible"])continue;c.ApplyWaveBasic("http-native-basic-"+manualHits++,"op_kyle",id,(long)life["generation"]);int after=(int)c.Capture()["nodeSession"]["runtime"]["state"]["direct"]["enemies"][id]["hp"];Assert.Less(after,before);damage.Add(new JObject{["targetId"]=id,["beforeHp"]=before,["afterHp"]=after,["tick"]=tick});break;}}
     c.Advance("http-native-step-"+step,5000000,1000);if(step%5==0)yield return null;
    }
    Assert.Greater(manualHits,0);c.SetPaused("http-native-pause",true);var saved=c.Capture();using(var restored=BRouteSessionController.Restore(saved,BDirectAttackInput.Digest(saved),composition.RestoreNative,"a",run)){Assert.IsTrue(JToken.DeepEquals(saved,restored.Capture()));c.SetPaused("http-native-resume",false);restored.SetPaused("http-native-resume",false);c.Advance("http-native-post-restore",1000000,100);restored.Advance("http-native-post-restore",1000000,100);Assert.IsTrue(JToken.DeepEquals(c.Capture(),restored.Capture()));}
    var cmd=BRouteCommand.FromOriginal(bundle.Route,BRouteConsumerTests.Arrival(bundle.Route));var routeClient=new BRouteHttpClient(transport);var refusal=c.SendAsync(cmd,()=>routeClient.SendAsync(cmd,token));yield return Wait(refusal);Assert.IsTrue(refusal.GetAwaiter().GetResult());Assert.AreEqual(409,(int)c.Capture()["lastResultStatus"]);
    var delayed=new BNativeNodeSourceHttpClient(new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(late)));refresh=c.RefreshAsync(async()=> (await delayed.ReadAsync("a",run,token)).Route);Assert.AreEqual(1,late.InFlightCount);var beforeLate=c.Capture();c.Unbind();yield return Wait(refresh);Assert.IsFalse(refresh.GetAwaiter().GetResult());Assert.IsTrue(JToken.DeepEquals(beforeLate,c.Capture()));Assert.AreEqual(0,a.InFlightCount);Assert.AreEqual(0,late.InFlightCount);
    var final=c.Capture();var native=final["nodeSession"]["runtime"];File.WriteAllText("D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BNativeHttp-20261005/actual-http-native.json",new JObject{["evidence"]="ACTUAL_GAME_HTTP_NATIVE_SOURCE_SAME_NODE_EXISTING_NATIVE_LOCAL_OWNER_INPUTS",["bundle"]=bundle.Capture(),["binding"]=binding.Capture(),["geometry"]=geometry.Capture(),["motion"]=rail,["manualDamage"]=damage,["controller"]=final,["runtimeSources"]=c.DescribeRuntimeSources(),["completedWaves"]=native["travel"]["motion"]["completedWaves"].DeepClone(),["nativeOutcome"]=((JArray)native["results"]).Last["outcome"].DeepClone(),["acceptedInterceptionTimingFromServer"]=false,["localInterceptionPolicyOnly"]=true,["operatingAcceptedProof"]=false,["restoredExactly"]=true,["latePublication"]=false}.ToString());
   }
  }
 }
}
#endif
