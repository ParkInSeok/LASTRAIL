using System;
using System.Reflection;
using System.IO;
using System.Text;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class BArrivalBoundaryTravelTests {
  static Type Owner {get {var t=typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Battle.BArrivalBoundaryTravel");Assert.NotNull(t,"Original pinned boundary travel producer is missing");return t;}}
  static object Call(object owner,string name,params object[] args)=>Owner.GetMethod(name).Invoke(owner,args);
  static string Hash(JToken value)=>BDirectAttackInput.Digest(value).Substring(7);
  static JObject Binding()=>new JObject{["accountId"]="test-account",["runId"]="test-run",["nodeId"]="node-b",["nativeContextId"]="test-context",["departurePinHash"]=new string('a',64),["mapRevision"]=3,["destinationEventId"]="destination-1",["edgeId"]="edge-ab",["origin"]=new JObject{["accountId"]="test-account",["runId"]="test-run",["pin"]="fixture-only"}};
  static JObject Source(){
   var s=new JObject{["version"]="combat.b-arrival-boundaries.v1.20261020",["origin"]=Binding()["origin"],["definition"]=new JObject{
    ["definitionId"]="explicit-test-only",["definitionVersion"]="test-1",["stepTicks"]=1000000,["executorVersion"]="combat.b-arrival-linear-fixed-step.v1.20261020",["speedHalfUnits"]=2,
    ["configuration"]=new JObject{["version"]="rail-journey-v1",["selectionScaleBps"]=10000,["recoveryMs"]=100,["baseSpeedMillimetersPerSecond"]=1000,["throttleMinimumBps"]=5000,["throttleMaximumBps"]=13000,["throttleCruiseBps"]=10000,["offers"]=new JArray()},
    ["boundaries"]=new JArray(new JObject{["edgeId"]="edge-ab",["fromNodeId"]="node-a",["toNodeId"]="node-b",["distanceMm"]=250})}};
   s["sourceHash"]=Hash(s);return s;
  }
  static object Create(){var s=Source();return Owner.GetMethod("Create").Invoke(null,new object[]{s,(string)s["sourceHash"],Binding()});}
  static JObject Advance(string id,long elapsed,int max=10000)=>new JObject{["kind"]="ADVANCE",["eventId"]=id,["elapsed"]=elapsed,["maximumSteps"]=max};
  [Test] public void StopsAtFirstCrossingRetainsDebtAndProducesBoundOriginalArrival(){
   var o=Create();Call(o,"Execute",Advance("advance-1",10000000));
   var report=(JObject)Call(o,"CaptureReport","report-1");var travel=(JObject)report["travel"];var result=travel["results"][0];
   Assert.AreEqual(3,(long)result["tick"]);Assert.AreEqual(300,(long)result["distanceMm"]);Assert.AreEqual(7000000,(long)result["debtTicks"]);
   Assert.AreEqual("sha256:"+Hash(travel),(string)report["checkpointHash"]);
   var identity=new JObject{["runId"]="test-run",["nodeId"]="node-b",["destinationEventId"]="destination-1",["boundarySourceHash"]=Source()["sourceHash"],["commandId"]="advance-1",["tick"]=3};
   Assert.AreEqual("B_ARRIVE:"+Hash(identity),(string)travel["arrival"]["eventId"]);
   var clock=(JObject)Call(o,"CaptureClock");Assert.AreEqual(14000000,(long)clock["debtHalfTicks"]);Assert.IsNull(clock["authority"]);
   var before=Call(o,"Capture").ToString();Call(o,"Execute",Advance("advance-1",10000000));Assert.AreEqual(before,Call(o,"Capture").ToString());
   Assert.Throws<TargetInvocationException>(()=>Call(o,"Execute",Advance("after",1)));
   Assert.Throws<TargetInvocationException>(()=>Call(o,"Execute",Advance("advance-1",1)));
   var export=(JObject)Call(o,"CaptureExport","report-1");
   Assert.IsTrue(JToken.DeepEquals(report,export["report"]));
   Assert.AreEqual((string)report["checkpointHash"],"sha256:"+Hash(JObject.Parse((string)export["travelCanonicalJson"])));
   var output=Environment.GetEnvironmentVariable("LASTRAIL_ARRIVAL_EXPORT");
   if(!string.IsNullOrEmpty(output)){
    var full=Path.GetFullPath(output);var root=Path.GetFullPath("D:/Project/LASTRAIL")+Path.DirectorySeparatorChar;
    Assert.IsTrue(full.StartsWith(root,StringComparison.OrdinalIgnoreCase),"Export must remain in active D project");
    using(var stream=new FileStream(full,FileMode.CreateNew,FileAccess.Write))
    using(var writer=new StreamWriter(stream,new UTF8Encoding(false)))writer.Write(export.ToString(Newtonsoft.Json.Formatting.None));
   }
  }
  [Test] public void PauseDoesNotCreditAndResumeUsesOnlyActualUnpausedCredit(){
   var o=Create();Call(o,"Execute",new JObject{["kind"]="PAUSE",["eventId"]="pause",["value"]=true});
   Call(o,"Execute",Advance("paused",9000000));Assert.AreEqual(0,(long)((JObject)Call(o,"CaptureClock"))["debtHalfTicks"]);
   Assert.Throws<TargetInvocationException>(()=>Call(o,"Execute",new JObject{["kind"]="TRAVEL_THROTTLE",["eventId"]="bad",["bps"]=12000}));
   Call(o,"Execute",new JObject{["kind"]="PAUSE",["eventId"]="resume",["value"]=false});Call(o,"Execute",Advance("active",2000000));
   Assert.AreEqual(2,(long)((JObject)Call(o,"CaptureClock"))["completedSteps"]);
   Assert.Throws<TargetInvocationException>(()=>Call(o,"CaptureReport","too-early"));
   Call(o,"Execute",Advance("cross",1000000));Assert.AreEqual(3,(long)((JObject)Call(o,"CaptureReport","done"))["travel"]["arrival"]["tick"]);
  }
  [Test] public void RestoresOriginalJournalAndRejectsTamperOrForeignPin(){
   var o=Create();Call(o,"Execute",Advance("partial",1500000,1));var saved=(JObject)Call(o,"Capture");var s=Source();
   var restored=Owner.GetMethod("Restore").Invoke(null,new object[]{s,(string)s["sourceHash"],Binding(),saved,"sha256:"+Hash(saved)});
   Assert.IsTrue(JToken.DeepEquals(saved,(JObject)Call(restored,"Capture")));saved["clock"]["completedSteps"]=999;
   Assert.Throws<TargetInvocationException>(()=>Owner.GetMethod("Restore").Invoke(null,new object[]{s,(string)s["sourceHash"],Binding(),saved,"sha256:"+Hash(saved)}));
   var foreign=Binding();foreign["origin"]["runId"]="foreign";
   Assert.Throws<TargetInvocationException>(()=>Owner.GetMethod("Create").Invoke(null,new object[]{s,(string)s["sourceHash"],foreign}));
   var copy=(JObject)Call(o,"Capture");copy["clock"]["paused"]=true;Assert.IsFalse((bool)((JObject)Call(o,"CaptureClock"))["paused"]);
  }
  [Test] public void FractionalRailMotionAndFailedCommandsPreserveExactOriginal(){
   var s=Source();s["definition"]["stepTicks"]=3;s["definition"]["configuration"]["baseSpeedMillimetersPerSecond"]=12345;s["definition"]["configuration"]["throttleCruiseBps"]=10001;
   var body=(JObject)s.DeepClone();body.Remove("sourceHash");s["sourceHash"]=Hash(body);
   var o=Owner.GetMethod("Create").Invoke(null,new object[]{s,(string)s["sourceHash"],Binding()});
   Call(o,"Execute",Advance("fraction",30,5));var checkpoint=(JObject)Call(o,"Capture");
   var row=checkpoint["travel"]["results"][0];
   Assert.AreEqual(0,(long)row["distanceMm"]);Assert.AreEqual(1851935175,(long)row["remainderUnits"]);Assert.AreEqual(15,(long)row["debtTicks"]);
   var before=checkpoint.ToString();
   Assert.Throws<TargetInvocationException>(()=>Call(o,"Execute",new JObject{["kind"]="TRAVEL_THROTTLE",["eventId"]="invalid",["bps"]=4999}));
   Assert.AreEqual(before,Call(o,"Capture").ToString());
   Assert.Throws<TargetInvocationException>(()=>Call(o,"Execute",Advance("overflow",9007199254740991)));
   Assert.AreEqual(before,Call(o,"Capture").ToString());
  }
 }
}
