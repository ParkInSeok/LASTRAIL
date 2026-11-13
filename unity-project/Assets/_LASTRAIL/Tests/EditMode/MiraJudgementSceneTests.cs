#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode
{
 public sealed class MiraJudgementSceneTests
 {
  Component adapter;
  static JObject Data(string name)=>JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"_LASTRAIL/Data/Combat/"+name+".json")));
  static string Hash(JObject value)=>BDirectAttackInput.Digest(value);
  static object Call(object target,string method,params object[] args){try{return target.GetType().GetMethod(method).Invoke(target,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  static object Geo(string method,params object[] args){
   var type=typeof(CombatVitals).Assembly.GetType("LastRail.Battle.MiraJudgementGeometry");
   Assert.That(type,Is.Not.Null,"Mira geometry observation/sweep adapter is missing.");
   try{return type.GetMethod(method).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}
  }
  void Create(){
   var type=typeof(CombatVitals).Assembly.GetType("LastRail.Battle.MiraJudgementSceneAdapter");
   Assert.That(type,Is.Not.Null,"Mira judgement scene authoring adapter is missing.");
   var p=Data("MiraJudgementProfileV1");var l=Data("MiraJudgementLayoutV1");
   try{adapter=(Component)type.GetMethod("Create").Invoke(null,new object[]{p,Hash(p),l,Hash(l)});}catch(TargetInvocationException e){throw e.InnerException;}
  }
  JObject Observe(long tick=0,long revision=1)=>(JObject)Call(adapter,"Observe",tick,revision);
  Transform Node(string name)=>adapter.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
  JObject Ray(JObject observation,string target="FIRST_TEST_SCOUT")=>(JObject)Geo("CreateRay",observation,Hash(observation),target);
  JObject Trace(JObject ray,JObject start,JObject end,long from,long to)=>(JObject)Geo("Trace",ray,Hash(ray),start,end,from,to);
  [TearDown]public void Cleanup(){if(adapter!=null)UnityEngine.Object.DestroyImmediate(adapter.gameObject);}
  [Test]public void NativeAuthoringUsesApprovedCapsulesBoxesAndSocketWithoutVisualArtReplacement(){
   Create();var p=Observe();var muzzle=(JArray)p["source"]["muzzleMm"];
   Assert.That((long)muzzle[0],Is.EqualTo(-180));Assert.That((long)muzzle[1],Is.EqualTo(3650));Assert.That((long)muzzle[2],Is.EqualTo(450));
   Assert.That(Node("FIRST_TEST_SCOUT").GetComponent<CapsuleCollider>().radius,Is.EqualTo(.3f));
   Assert.That(Node("FIRST_TEST_GUNNER").GetComponent<CapsuleCollider>().height,Is.EqualTo(1.8f));
   Assert.That(Node("FIRST_TEST_CAR_A").GetComponent<BoxCollider>().size,Is.EqualTo(new Vector3(12,2.4f,3)));
   Assert.That((string)p["provenance"],Is.EqualTo("APPROVED_FIRST_TEST_JUDGEMENT_MODEL_NOT_ART_MEASUREMENT"));
  }
  [TestCase(0,450,180)][TestCase(90,-180,450)][TestCase(180,-450,-180)][TestCase(270,180,-450)]
  public void MuzzleFollowsPoseYawAndFootTransform(int yaw,int x,int z){
   Create();Node("MiraFeet").localRotation=Quaternion.Euler(0,-yaw,0);
   // Authoring +X forward, +Z right; native Unity yaw sign is explicitly adapted.
   var p=Observe();Assert.That((long)p["source"]["muzzleMm"][0],Is.EqualTo(x));Assert.That((long)p["source"]["muzzleMm"][2],Is.EqualTo(z));
   Node("MiraFeet").localPosition+=new Vector3(2,0,3);var moved=Observe(1,2);
   Assert.That((long)moved["source"]["muzzleMm"][0],Is.EqualTo(x+2000));
   Assert.That((long)moved["source"]["muzzleMm"][2],Is.EqualTo(z+3000));
  }
  [Test]public void ObservationPreservesCommonTickRevisionAndNativeTransformMotion(){
   Create();var before=Observe();Node("FIRST_TEST_SCOUT").localPosition+=Vector3.right*2;
   var after=Observe(1,2);Assert.That((long)after["tick"],Is.EqualTo(1));Assert.That((long)after["revision"],Is.EqualTo(2));
   Assert.That((long)after["targets"][0]["feetMm"][0]-(long)before["targets"][0]["feetMm"][0],Is.EqualTo(2000));
  }
  [Test]public void RayFreezesMuzzleAndDirectionAndCanRestoreFromOriginalPin(){
   Create();var observation=Observe();var ray=Ray(observation);
   Node("MiraFeet").localPosition+=Vector3.right*20;
   var restored=(JObject)Geo("RestoreRay",ray,Hash(ray));
   Assert.That(JToken.DeepEquals(ray,restored),Is.True);
   var forged=(JObject)ray.DeepClone();forged["maximumDistanceMm"]=99999;
   Assert.Throws<ArgumentException>(()=>Geo("RestoreRay",forged,Hash(ray)));
  }
  [Test]public void RangeAndOriginalAdjacentDomainEligibilityAreBothRequired(){
   Create();Node("FIRST_TEST_SCOUT").localPosition=new Vector3(30,2.4f,6);Assert.Throws<InvalidOperationException>(()=>Ray(Observe()));
   var layout=Data("MiraJudgementLayoutV1");layout["targets"][0]["slot"]=3;
   ((JArray)layout["cars"]).Add(new JObject{["id"]="FIRST_TEST_CAR_FAR",["slot"]=3,["centerMm"]=new JArray(0,0,18000)});
   UnityEngine.Object.DestroyImmediate(adapter.gameObject);adapter=null;
   var type=typeof(CombatVitals).Assembly.GetType("LastRail.Battle.MiraJudgementSceneAdapter");
   var p=Data("MiraJudgementProfileV1");adapter=(Component)type.GetMethod("Create").Invoke(null,new object[]{p,Hash(p),layout,Hash(layout)});
   Assert.Throws<InvalidOperationException>(()=>Ray(Observe()));
  }
  [Test]public void ExistingCarriageBodyIsNotExemptFromLaunchOrSightChecks(){
   Create();Node("MiraFeet").localPosition=new Vector3(0,0,0);Assert.Throws<InvalidOperationException>(()=>Ray(Observe()));
  }
  [Test]public void StaticCapsuleSweepContactsBeforeItsAnchorAndNeverClaimsAnchorTravelAsHitTime(){
   Create();var start=Observe();var ray=Ray(start);var end=Observe(1,2);
   var contact=Trace(ray,start,end,0,500000);
   Assert.That((string)contact["kind"],Is.EqualTo("HIT"));
   Assert.That((double)contact["elapsedUs"],Is.LessThan((double)ray["anchorTravelUs"]));
   Assert.That((string)contact["instanceId"],Is.EqualTo("FIRST_TEST_SCOUT"));
  }
  [Test]public void MovingCrossingCapsuleUsesBothObservedTransformsNotEndPositionOnly(){
   Create();var start=Observe();var ray=Ray(start);Node("FIRST_TEST_SCOUT").localPosition+=new Vector3(4,0,0);
   var end=Observe(1,2);var contact=Trace(ray,start,end,0,500000);
   Assert.That((string)contact["kind"],Is.EqualTo("EXPIRED"));
   // Kernel separately validates a crossing hit whose endpoints both miss.
  }
  [Test]public void HoldElapsedIntervalProducesNoContactAndGenerationChangeIsNotRetargeted(){
   Create();var start=Observe();var ray=Ray(start);
   Assert.That((string)Trace(ray,start,start,0,0)["kind"],Is.EqualTo("NONE"));
   var changed=(JObject)start.DeepClone();changed["tick"]=1;changed["revision"]=2;changed["targets"][0]["generation"]=2;
   Assert.That((string)Trace(ray,start,changed,0,500000)["kind"],Is.EqualTo("TARGET_LOST"));
  }
  [Test]public void SameTimeSolidPrecedesTargetAndCloserContactAlwaysWins(){
   Create();
   var list=new JArray(new JObject{["time"]=.5,["kind"]="HIT",["instanceId"]="a"},
    new JObject{["time"]=.5,["kind"]="BLOCKED",["instanceId"]="z"});
   var selected=(JObject)Geo("SelectContact",list);Assert.That((string)selected["kind"],Is.EqualTo("BLOCKED"));
   ((JObject)list[0])["time"]=.499999;Assert.That((string)((JObject)Geo("SelectContact",list))["kind"],Is.EqualTo("HIT"));
  }
  [Test]public void SnapshotReplayOfIdenticalObservationsProducesIdenticalContact(){
   Create();var a=Observe();var r=Ray(a);var b=Observe(1,2);
   var one=Trace(r,a,b,0,500000);var two=Trace((JObject)r.DeepClone(),(JObject)a.DeepClone(),(JObject)b.DeepClone(),0,500000);
   Assert.That(JToken.DeepEquals(one,two),Is.True);
  }
  [Test]public void DebugOverlayToggleDoesNotChangeAuthorityObservation(){
   Create();var a=Observe();Call(adapter,"SetDebugOverlay",false);var b=Observe();
   Assert.That(JToken.DeepEquals(a,b),Is.True);
  }
  [Test]public void UnsupportedScaleOrRotatedBoxCannotSilentlyBecomeAnExpandedWorldAabb(){
   Create();Node("FIRST_TEST_CAR_B").localRotation=Quaternion.Euler(0,30,0);
   Assert.Throws<InvalidOperationException>(()=>Observe());
  }
 }
}
#endif