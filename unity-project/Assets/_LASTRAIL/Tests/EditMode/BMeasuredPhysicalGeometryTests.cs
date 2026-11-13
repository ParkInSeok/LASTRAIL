using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class BMeasuredPhysicalGeometryTests {
  const string Root="D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoringV01/";
  static Type Owner {get{var t=typeof(BDirectAttackInput).Assembly.GetType("LastRail.Battle.BMeasuredPhysicalGeometry");Assert.NotNull(t,"Measured authored geometry reader missing");return t;}}
  static JObject Definition()=>JObject.Parse(File.ReadAllText(Root+"definition.json"));
  static JObject Measurements()=>JObject.Parse(File.ReadAllText(Root+"measurements.json"));
  static object Read(JObject d=null,JObject m=null){d=d??Definition();m=m??Measurements();return Owner.GetMethod("ReadAuthored").Invoke(null,new object[]{d,BDirectAttackInput.Digest(d),m,BDirectAttackInput.Digest(m)});}
  static object Call(object o,string name,params object[] args)=>Owner.GetMethod(name).Invoke(o,args);
  [Test] public void ActualAuthoredMillimetresAnchorsAndExplicitBridgesRemainExact(){
   var o=Read();Assert.IsTrue(JToken.DeepEquals(new JArray(350,4500,0),(JArray)Call(o,"WorldAnchor",0,"Fire")));
   Assert.IsTrue(JToken.DeepEquals(new JArray(44000,3200,0),(JArray)Call(o,"WorldAnchor",4,"Foot")));
   Assert.IsTrue(JToken.DeepEquals(new JArray(44100,200,300),(JArray)Call(o,"WorldPoint",4,new JArray(100,200,300))));
   Assert.AreEqual(8,((JArray)Call(o,"WorldBodyCorners",0)).Count);
   Assert.IsTrue(JToken.DeepEquals(new JArray(-5000,3200,-1500),((JArray)Call(o,"WorldRoof",0))[0]));
   Assert.IsTrue(JToken.DeepEquals(new JArray(new JArray(5000,3200,0),new JArray(6000,3200,0)),(JArray)Call(o,"Bridge",0,1)));
   Assert.IsTrue((bool)Call(o,"Connected",0,4));
   Assert.IsTrue(JToken.DeepEquals(new JArray(new JArray(0,0,-1500),new JArray(0,3200,-1500)),(JArray)Call(o,"WorldSocketPath",0,"left","climb")));
  }
  [Test] public void SparseAccountPlacementPreservesPhysicalSlotsAndDoesNotInventConnections(){
   var o=Read();var placement=BSameRunSourceTests.Read(BSameRunSourceTests.Packet()).PrepareFormation().Placement;
   var bound=(JObject)Call(o,"BindPlacement",placement,"actual-source-context");
   var instances=(JArray)bound["instances"];Assert.AreEqual(2,instances.Count);
   Assert.AreEqual(0,(int)instances[0]["physicalSlot"]);Assert.AreEqual("physical-0",(string)instances[0]["carriageInstanceId"]);
   Assert.AreEqual(4,(int)instances[1]["physicalSlot"]);Assert.AreEqual("physical-4",(string)instances[1]["carriageInstanceId"]);
   Assert.AreEqual(0,((JArray)bound["connections"]).Count);
   Assert.Throws<TargetInvocationException>(()=>Call(o,"WorldAnchor",5,"Foot"));
   Assert.Throws<TargetInvocationException>(()=>Call(o,"WorldAnchor",0,"Missing"));
   bound["instances"][0]["physicalSlot"]=3;
   Assert.AreEqual(0,(int)((JObject)Call(o,"BindPlacement",placement,"actual-source-context"))["instances"][0]["physicalSlot"]);
  }
  [TestCase("bytes"),TestCase("packet"),TestCase("unit"),TestCase("transform"),TestCase("duplicate"),TestCase("unknownVersion")]
  public void IndependentlyRehashedOuterInputCannotHideInvalidMeasurementOrUnsupportedDraft(string kind){
   var d=Definition();var m=Measurements();
   if(kind=="bytes")m["instances"][0]["measurement"]["geometryCanonicalJson"]+=" ";
   if(kind=="packet")m["instances"][0]["measurement"]["packet"]["anchors"][0]["positionMm"][0]=888;
   if(kind=="unit")d["bindings"][0]["millimetersPerUnit"]=1;
   if(kind=="transform")d["bindings"][0]["frameToNative"][1]=1;
   if(kind=="duplicate")d["bindings"][1]["templateSlot"]=0;
   if(kind=="unknownVersion")d["version"]="unknown";
   Assert.Throws<TargetInvocationException>(()=>Read(d,m));
  }
  [Test] public void CapturesAndWorldQueriesDoNotExposeMutableOriginals(){
   var d=Definition();var m=Measurements();var o=Read(d,m);var before=(JObject)Call(o,"Capture");
   d["bindings"][0]["frameToNative"][3]=900;m.Remove("instances");
   ((JArray)Call(o,"WorldAnchor",0,"Foot"))[0]=777;((JObject)Call(o,"Capture"))["unknown"]=true;
   Assert.IsTrue(JToken.DeepEquals(before,(JObject)Call(o,"Capture")));
  }
 }
}
