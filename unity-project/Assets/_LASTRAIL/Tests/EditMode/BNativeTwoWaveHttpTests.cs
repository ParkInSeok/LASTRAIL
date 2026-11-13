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
 public sealed class BNativeTwoWaveHttpTests {
  const string Qa="D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BNativeTwoWave-20261005/";
  static IEnumerator Wait(Task t){var end=DateTime.UtcNow.AddSeconds(30);while(!t.IsCompleted){Assert.Less(DateTime.UtcNow,end);yield return null;}}
  static JObject Native(BRouteSessionController c)=>(JObject)c.Capture()["nodeSession"]["runtime"];
  static long Tick(JObject n)=>(long)n["state"]["direct"]["clock"]["completedSteps"];
  [UnityTest, Timeout(900000)] public IEnumerator ContinueOriginalHttpCheckpointWithOwnedRoofMovementThroughTerminal(){
   var path=Environment.GetEnvironmentVariable("LASTRAIL_NATIVE_CONNECTION");if(string.IsNullOrEmpty(path))Assert.Ignore("Explicit native fixture required");var cfg=JObject.Parse(File.ReadAllText(path));var run=(string)cfg["runId"];var token=new GameApiAccessToken((string)cfg["token"]);var prior=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BNativeHttp-20261005/actual-http-native.json"));
   using(var a=new UnityWebRequestRunApiHttpAdapter((string)cfg["origin"],10)){
    var transport=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(a));var destination=new BOperationDestinationClient(transport);var intent=new BOperationDestinationIntent("a",run,"select",1,"entry:n1-0");var choose=destination.ConfirmAsync(intent,token);yield return Wait(choose);choose.GetAwaiter().GetResult();
    var client=new BNativeNodeSourceHttpClient(transport);var get=client.ReadAsync("a",run,token);yield return Wait(get);var bundle=get.GetAwaiter().GetResult();Assert.IsTrue(JToken.DeepEquals(prior["bundle"],bundle.Capture()),"Fresh original fixture must reproduce exactly the trusted original HTTP bundle before restoring its checkpoint");
    var binding=BRouteNativeTests.Binding(bundle.Boarding);var geometry=BOrdinaryGeometryTests.Geometry(binding);var rail=(JObject)prior["motion"];var config=BTravelRuntimeConfiguration.LocalValidation(rail,BDirectAttackInput.Digest(rail));var composition=new BRouteNativeComposition(bundle.Route,bundle.Boarding,binding,geometry,config,10000);Assert.IsTrue(JToken.DeepEquals(binding.Capture(),prior["binding"]));Assert.IsTrue(JToken.DeepEquals(geometry.Capture(),prior["geometry"]));
    var roof=geometry.Definition.Capture();var roofMin=(JArray)roof["roofMinMm"];var roofMax=(JArray)roof["roofMaxMm"];Assert.AreEqual(0,(int)geometry.Capture()["instances"][0]["yawQuarterTurns"]);Assert.IsTrue(JToken.DeepEquals(geometry.Capture()["instances"][0]["positionMm"],new JArray(0,0,0)));var saved=(JObject)prior["controller"];using(var c=BRouteSessionController.Restore(saved,BDirectAttackInput.Digest(saved),composition.RestoreNative,"a",run)){
     var n=Native(c);var pending=((JArray)n["boarding"]["lives"]).Where(x=>(bool)x["registered"]&&(bool)x["targetable"]&&(int)n["state"]["direct"]["enemies"][(string)x["id"]]["hp"]>0).ToArray();Assert.AreEqual(2,pending.Length);
     var diagnostic=new JArray();foreach(var life in pending){string id=(string)life["id"];var frame=c.ObserveOrdinaryGeometry("op_kyle",id,Tick(n),(long)n["spatialSource"]["revision"],geometry.Hash);Assert.AreEqual(6000,(long)frame["distanceMm"]);Assert.AreEqual(1800,(long)frame["maximumDistanceMm"]);Assert.IsFalse((bool)frame["eligible"]);var before=c.Capture();var error=Assert.Throws<InvalidOperationException>(()=>c.ApplyWaveBasic("diagnose-outside-"+id,"op_kyle",id,(long)life["generation"]));Assert.AreEqual("b_attack_geometry_ineligible",error.Message);Assert.IsTrue(JToken.DeepEquals(before,c.Capture()));diagnostic.Add(frame);}
     var history=new JArray();int commands=0,hits=0;string movingTarget=null;BRouteSessionController mirror=null;JObject middle=null;bool inGapRestored=false;
     try{for(int step=0;step<600;step++){
      n=Native(c);string outcome=(string)((JArray)n["results"]).Last["outcome"];if(outcome!="RUNNING")break;long tick=Tick(n);
      var lives=((JArray)n["boarding"]["lives"]).Where(x=>(bool)x["registered"]&&(bool)x["targetable"]&&(int)n["state"]["direct"]["enemies"][(string)x["id"]]["hp"]>0).ToArray();
      var eligible=lives.Select(l=>new{Life=l,Frame=c.ObserveOrdinaryGeometry("op_kyle",(string)l["id"],tick,(long)n["spatialSource"]["revision"],geometry.Hash)}).ToArray();
      var target=eligible.FirstOrDefault(x=>(bool)x.Frame["eligible"])??eligible.Where(x=>(string)x.Life["phase"]=="Rooftop").OrderBy(x=>(long)x.Frame["distanceMm"]).FirstOrDefault();
      if(target!=null){string id=(string)target.Life["id"];bool busy=((JArray)n["gunnerIntercept"]["interception"]["actions"]).Any(x=>(string)x["actorId"]=="op_kyle"&&((string)x["phase"]=="PREPARATION"||(string)x["phase"]=="RECOVERY"&&(long)x["endTick"]>tick));
       if((bool)target.Frame["eligible"]&&!busy&&(long)n["state"]["direct"]["next"]["op_kyle"]<=tick){string eid="resume-basic-"+commands++;int before=(int)n["state"]["direct"]["enemies"][id]["hp"];c.ApplyWaveBasic(eid,"op_kyle",id,(long)target.Life["generation"]);mirror?.ApplyWaveBasic(eid,"op_kyle",id,(long)target.Life["generation"]);hits++;history.Add(new JObject{["kind"]="BASIC",["tick"]=tick,["targetId"]=id,["beforeHp"]=before,["afterHp"]=Native(c)["state"]["direct"]["enemies"][id]["hp"].DeepClone()});}
       else if(!(bool)target.Frame["eligible"]&&movingTarget!=id){var position=((JArray)n["spatialSource"]["positions"]).Single(x=>(string)x["id"]==id);var point=(JArray)position["positionMm"].DeepClone();Assert.AreEqual(0,(int)position["slot"],"This original fixture has one identity-transformed carriage");for(int axis=0;axis<3;axis++)point[axis]=Math.Max((long)roofMin[axis],Math.Min((long)roofMax[axis],(long)point[axis]));string eid="resume-roof-move-"+commands++;c.MoveOrdinaryOperator(eid,"op_kyle",(int)position["slot"],point,geometry.Hash);mirror?.MoveOrdinaryOperator(eid,"op_kyle",(int)position["slot"],point,geometry.Hash);movingTarget=id;history.Add(new JObject{["kind"]="MOVE",["eventId"]=eid,["tick"]=tick,["targetId"]=id,["destinationMm"]=point.DeepClone()});}
      }
      n=Native(c);if(mirror==null&&(long)n["waveEntry"]["gapUntil"]>0){middle=c.Capture();mirror=BRouteSessionController.Restore(middle,BDirectAttackInput.Digest(middle),composition.RestoreNative,"a",run);Assert.IsTrue(JToken.DeepEquals(middle,mirror.Capture()));inGapRestored=true;history.Add(new JObject{["kind"]="MID_GAP_RESTORE",["tick"]=Tick(n),["gapUntil"]=n["waveEntry"]["gapUntil"].DeepClone()});}
      string advance="resume-step-"+commands++;c.Advance(advance,2500000,1000);mirror?.Advance(advance,2500000,1000);if(step%10==0){var progress=Native(c);File.WriteAllText(Qa+"two-wave-progress.json",new JObject{["step"]=step,["tick"]=Tick(progress),["waveEntry"]=progress["waveEntry"].DeepClone(),["outcome"]=((JArray)progress["results"]).Last["outcome"].DeepClone(),["hp"]=progress["state"]["direct"]["hp"], ["manualHits"]=hits,["midGapRestored"]=inGapRestored}.ToString());yield return null;}
     }
     n=Native(c);var final=c.Capture();if(mirror!=null)Assert.IsTrue(JToken.DeepEquals(final,mirror.Capture()));var observation=c.DescribeRuntimeSources();
     File.WriteAllText(Qa+"two-wave-http-result.json",new JObject{["sourceBundle"]=bundle.Capture(),["originalCheckpointHash"]=BDirectAttackInput.Digest(saved),["diagnosticFrames"]=diagnostic,["history"]=history,["midGapSnapshot"]=middle??(JToken)JValue.CreateNull(),["controller"]=final,["runtimeSources"]=observation,["nativeOutcome"]=((JArray)n["results"]).Last["outcome"].DeepClone(),["completedWaves"]=n["travel"]["motion"]["completedWaves"].DeepClone(),["midGapRestored"]=inGapRestored,["mirrorFinalEqual"]=mirror!=null,["manualHitsAfterResume"]=hits,["boostApplied"]=false,["acceptedOperatingProof"]=false}.ToString());
     Assert.AreNotEqual("RUNNING",(string)((JArray)n["results"]).Last["outcome"],"Supported original movement/attacks must reach an observable terminal within bounded150s");
     if((string)((JArray)n["results"]).Last["outcome"]=="VICTORY"){Assert.AreEqual(2,(long)n["travel"]["motion"]["completedWaves"]);Assert.IsTrue(inGapRestored);}
    }finally{mirror?.Dispose();}
    }
    Assert.AreEqual(0,a.InFlightCount);
   }
  }
 }
}
#endif
