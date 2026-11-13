using System;
using System.IO;
using System.Reflection;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class BMeasuredNativeGeometryTests {
  static string Difference(JToken a,JToken b,string path="$"){
   if(a.Type!=b.Type)return path+": "+a.Type+" / "+b.Type;
   if(a is JObject ao){foreach(var p in ao.Properties()){var d=Difference(p.Value,b[p.Name],path+"."+p.Name);if(d!=null)return d;}}
   else if(a is JArray ar){for(int i=0;i<ar.Count;i++){var d=Difference(ar[i],b[i],path+"["+i+"]");if(d!=null)return d;}}
   else if(!JToken.DeepEquals(a,b))return path+": "+a+" / "+b+" ("+((JValue)a).Value?.GetType()+" / "+((JValue)b).Value?.GetType()+")";
   return null;
  }
  [Test] public void MeasuredDraftOriginalEnemyDamageAndO22HealSurviveNativeRestore(){
   var geometry=(BMeasuredPhysicalGeometry)Geometry();var source=BBoardingEventsTests.Source(0);var binding=BRouteNativeTests.Binding(source);
   var rail=global::CombatRailJourneyTests.Config();rail["offers"]=new JArray();rail["throttleMaximumBps"]=13000;rail["throttleCruiseBps"]=10000;
   var loop=BDirectAttackBasicLoop.CreateMeasuredTravelEventRuntime(binding,source,geometry,10000,rail,BDirectAttackInput.Digest(rail));
   loop.ApplyO22(BNativeLifestealTests.Route(source.OriginalSource),BNativeLifestealTests.Original(BNativeLifestealTests.Route(source.OriginalSource),true));
   bool healed=false;
   for(int n=0;n<60&&loop.Outcome=="RUNNING";n++){
    loop.Advance("measured-life-"+n,10000000,1000);var state=loop.Capture();var hp=state["state"]["direct"]["allies"]["vitals"]["op_kyle"];
    if((int)hp["hp"]<=0||(int)hp["hp"]>=(int)hp["maxHp"])continue;
    foreach(var life in state["boarding"]["lives"].Where(p=>(bool)p["registered"]&&(bool)p["targetable"])){
     string target=(string)life["id"];var spatial=state["spatialSource"];
     var frame=loop.ObserveOrdinaryGeometry("op_kyle",target,loop.Tick,(long)spatial["revision"],(string)spatial["geometryHash"]);if(!(bool)frame["eligible"])continue;
     int before=(int)hp["hp"];loop.ApplyWaveBasic("measured-original-hit","op_kyle",target,(long)life["generation"]);var saved=loop.Capture();var hit=saved["results"].Last["hits"][0];int gain=(int)hit["lifesteal"]["actualHpGain"];
     Assert.Greater(gain,0);Assert.AreEqual(before+gain,loop.Hp("op_kyle"));
     var parsed=JObject.Parse(saved.ToString());Assert.IsTrue(JToken.DeepEquals(saved,parsed),Difference(saved,parsed));
     var restored=BDirectAttackBasicLoop.RestoreMeasuredTravelRuntime(saved,BDirectAttackInput.Digest(saved),binding,source,geometry,10000,rail,BDirectAttackInput.Digest(rail));
     Assert.IsTrue(JToken.DeepEquals(saved,restored.Capture()));restored.ApplyWaveBasic("measured-original-hit","op_kyle",target,(long)life["generation"]);Assert.IsTrue(JToken.DeepEquals(saved,restored.Capture()));
     TestContext.WriteLine("Measured original damage="+hit["hp"]["hpLoss"]+" heal="+gain+" before="+before);healed=true;break;
    }if(healed)break;
   }Assert.IsTrue(healed,"Measured geometry and unchanged original stats must produce a real eligible hit and heal");
  }
  internal static object Geometry(){
   const string root="D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoringV01/";
   var type=typeof(BDirectAttackInput).Assembly.GetType("LastRail.Battle.BMeasuredPhysicalGeometry");Assert.NotNull(type);
   var d=JObject.Parse(File.ReadAllText(root+"definition.json"));var m=JObject.Parse(File.ReadAllText(root+"measurements.json"));
   return type.GetMethod("ReadAuthored").Invoke(null,new object[]{d,BDirectAttackInput.Digest(d),m,BDirectAttackInput.Digest(m)});
  }
  [Test] public void MeasuredAnchorsDriveOriginalNativeClockAndReplayWithoutTestGeometryConversion(){
   var geometry=Geometry();var method=typeof(BDirectAttackBasicLoop).GetMethod("CreateMeasuredTravelEventRuntime");
   Assert.NotNull(method,"Measured geometry must reach the existing native owner");
   var source=BBoardingEventsTests.Source(0);var binding=BRouteNativeTests.Binding(source);
   var rail=global::CombatRailJourneyTests.Config();rail["offers"]=new JArray();rail["throttleMaximumBps"]=13000;rail["throttleCruiseBps"]=10000;
   var loop=(BDirectAttackBasicLoop)method.Invoke(null,new object[]{binding,source,geometry,10000,rail,BDirectAttackInput.Digest(rail),false});
   loop.Advance("measured-native",20000000,10000);var saved=loop.Capture();var spatial=saved["spatialSource"];
   Assert.AreEqual("MEASURED_AUTHORED_DRAFT_NOT_RUNTIME_ADMITTED",(string)spatial["sourceStatus"]);
   foreach(var actor in spatial["operators"])Assert.AreEqual(3200,(long)actor["positionMm"][1]);
   Assert.Throws<InvalidOperationException>(()=>loop.MoveOrdinaryOperator("no-invented-speed","op_kyle",0,new JArray(0,3200,0),(string)spatial["geometryHash"]));
   Assert.IsTrue(JToken.DeepEquals(saved,loop.Capture()));
   var restore=typeof(BDirectAttackBasicLoop).GetMethod("RestoreMeasuredTravelRuntime");Assert.NotNull(restore);
   var restarted=(BDirectAttackBasicLoop)restore.Invoke(null,new object[]{saved,BDirectAttackInput.Digest(saved),binding,source,geometry,10000,rail,BDirectAttackInput.Digest(rail),false});
   Assert.IsTrue(JToken.DeepEquals(saved,restarted.Capture()));
  }
 }
}
