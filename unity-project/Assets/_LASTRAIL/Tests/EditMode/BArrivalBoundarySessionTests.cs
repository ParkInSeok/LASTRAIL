using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class BArrivalBoundarySessionTests {
  static Type Owner {get{var t=typeof(BRouteBinding).Assembly.GetType("LastRail.Run.BArrivalBoundarySession");Assert.NotNull(t,"Persistent boundary session is missing");return t;}}
  static object Call(object o,string name,params object[] args)=>Owner.GetMethod(name).Invoke(o,args);
  static string Hash(JToken p)=>BDirectAttackInput.Digest(p).Substring(7);
  static void Rehash(JObject p,string field="packetHash"){var c=(JObject)p.DeepClone();c.Remove(field);p[field]=Hash(c);}
  internal static BDepartureSourceBundle Fixture(){
   var p=BSameRunSourceTests.Packet();var route=p["nativeNodeSource"]["routeBinding"];
   var s=new JObject{["version"]="combat.b-arrival-boundaries.v1.20261020",["origin"]=p["originalOrigin"].DeepClone(),["definition"]=new JObject{
    ["definitionId"]="session-fixture-only",["definitionVersion"]="test-1",["stepTicks"]=1000000,["executorVersion"]="combat.b-arrival-linear-fixed-step.v1.20261020",["speedHalfUnits"]=2,
    ["configuration"]=new JObject{["version"]="rail-journey-v1",["selectionScaleBps"]=10000,["recoveryMs"]=100,["baseSpeedMillimetersPerSecond"]=1000,["throttleMinimumBps"]=5000,["throttleMaximumBps"]=13000,["throttleCruiseBps"]=10000,["offers"]=new JArray()},
    ["boundaries"]=new JArray(new JObject{["edgeId"]=route["pendingEdgeId"].DeepClone(),["fromNodeId"]="entry",["toNodeId"]=route["pendingNode"]["nodeId"].DeepClone(),["distanceMm"]=250})}};
   Rehash(s,"sourceHash");p["originalCombatSource"]["profile"]["definition"]["bArrivalBoundarySource"]=s;
   p["runtimeProfileHash"]=Hash(p["originalCombatSource"]["profile"]);p["assignment"]["runtimeProfileHash"]=p["runtimeProfileHash"].DeepClone();
   Rehash((JObject)p["assignment"]);p["combatSourceHash"]=Hash(p["originalCombatSource"]);Rehash(p);return BSameRunSourceTests.Read(p);
  }
  internal static BOperationDestinationReceipt Destination(BRouteBinding route){
   var r=route.Capture();var i=new BOperationDestinationIntent(route.AccountId,route.RunId,"destination-test",route.MapRevision-1,(string)r["pendingEdgeId"]);
   return BOperationDestinationReceipt.Read(new JObject{["version"]="operation.map-destination.v1",["requestId"]=i.CommandId,["revision"]=route.MapRevision,["phase"]="TRAVELLING",["pendingEdgeId"]=i.EdgeId}.ToString(),i);
  }
  static object Open(BDepartureSourceBundle s,Func<JObject> load,Action<JObject,long> save,string context="boundary-session")=>Owner.GetMethod("Open").Invoke(null,new object[]{s,s.NativeNode.Route,Destination(s.NativeNode.Route),context,load,save});
  static JObject Advance(string id,long elapsed)=>new JObject{["kind"]="ADVANCE",["eventId"]=id,["elapsed"]=elapsed,["maximumSteps"]=10000};
  [Test] public void DiskCheckpointRestartsAndUncertainAckRecoversOriginalWithoutDoubleAdvance(){
   var s=Fixture();string dir="D:/Project/LASTRAIL/LocalTemp/ArrivalSessionTests/"+Guid.NewGuid().ToString("N");
   var store=new LastRail.Presentation.RoofBattle.RoofBattleLocalFileStore(dir,"boundary-session",(string)Owner.GetField("Version").GetRawConstantValue());
   bool loseAck=false;int saves=0;
   Func<JObject> load=store.Load;
   Action<JObject,long> save=(p,expected)=>{store.Save(p,expected);saves++;if(loseAck)throw new IOException("ACK lost after durable write");};
   var o=Open(s,load,save);Call(o,"Execute",Advance("one",1000000));var before=(JObject)Call(o,"Capture");
   loseAck=true;Assert.Throws<TargetInvocationException>(()=>Call(o,"Execute",Advance("two",1000000)));
   Assert.IsTrue(JToken.DeepEquals(before,Call(o,"Capture") as JObject));
   Assert.Throws<TargetInvocationException>(()=>Call(o,"Execute",Advance("blocked",1000000)));
   Assert.Throws<TargetInvocationException>(()=>Call(o,"CaptureExport","blocked"));
   loseAck=false;Call(o,"Recover");Assert.AreEqual(2,(long)((JObject)Call(o,"Capture"))["runtime"]["clock"]["completedSteps"]);
   int count=saves;Call(o,"Execute",Advance("two",1000000));Assert.AreEqual(count,saves);
   Call(o,"Dispose");var restarted=Open(s,load,save);Call(restarted,"Execute",Advance("three",1000000));
   Assert.AreEqual(3,(long)((JObject)Call(restarted,"CaptureExport","report"))["report"]["travel"]["arrival"]["tick"]);Call(restarted,"Dispose");
  }
  [Test] public void SaveCallbackCannotReenterOrPublishAfterDisposal(){
   var s=Fixture();JObject stored=null;object o=null;bool callback=false;
   Action<JObject,long> save=(p,e)=>{stored=(JObject)p.DeepClone();if(callback){Assert.Throws<TargetInvocationException>(()=>Call(o,"Execute",Advance("nested",1)));Call(o,"Dispose");}};
   o=Open(s,()=>stored,save);callback=true;
   Assert.Throws<TargetInvocationException>(()=>Call(o,"Execute",Advance("outer",1000000)));
   Assert.Throws<TargetInvocationException>(()=>Call(o,"CaptureExport","late"));
   var reopened=Open(s,()=>stored,(p,e)=>stored=(JObject)p.DeepClone());Assert.AreEqual(1,(long)((JObject)Call(reopened,"Capture"))["runtime"]["clock"]["completedSteps"]);Call(reopened,"Dispose");
  }
  [Test] public void ForeignContextOrMissingBoundaryCannotReuseCheckpoint(){
   var s=Fixture();JObject stored=null;var o=Open(s,()=>stored,(p,e)=>stored=(JObject)p.DeepClone());Call(o,"Execute",Advance("one",1));Call(o,"Dispose");
   Assert.Throws<TargetInvocationException>(()=>Open(s,()=>stored,(p,e)=>{}, "other-context"));
   var absent=BSameRunSourceTests.Read(BSameRunSourceTests.Packet());
   Assert.Throws<TargetInvocationException>(()=>Open(absent,()=>null,(p,e)=>{}));
  }
 }
}
