#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public sealed class MiraJudgementNativeStateTests {
  MiraJudgementSceneAdapter adapter;
  static string Hash(JObject p)=>BDirectAttackInput.Digest(p);
  static JObject Data(string n)=>JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"_LASTRAIL/Data/Combat/"+n+".json")));
  void Create(){var p=Data("MiraJudgementProfileV1");var l=Data("MiraJudgementLayoutV1");adapter=MiraJudgementSceneAdapter.Create(p,Hash(p),l,Hash(l));}
  static object Call(object o,string name,params object[] args){var m=o.GetType().GetMethod(name);Assert.That(m,Is.Not.Null,name+" is not implemented.");try{return m.Invoke(o,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  Transform Node(string id)=>adapter.GetComponentsInChildren<Transform>().Single(t=>t.name==id);
  JObject Capture(long tick=0,long rev=1)=>(JObject)Call(adapter,"CaptureNativeState",tick,rev);
  void Restore(JObject p,string hash=null)=>Call(adapter,"RestoreNativeState",p,hash??Hash(p));
  void Box(string id="FIRST_TEST_APPROVED_BOX",long x=4000,long z=3000)=>Call(adapter,"AddApprovedTestBox",id,x,z);
  [Test]public void SerializedFractionalNativePoseMustRestoreWithSameFloatAndWirePin(){
   Create();var feet=Node("MiraFeet");feet.localPosition+=new Vector3(.0001f,.0002f,.0003f);feet.localRotation=Quaternion.Euler(0,-45,0);var position=feet.localPosition;var rotation=feet.localRotation;var original=Capture();var saved=JObject.Parse(original.ToString());Assert.That(Hash(saved),Is.EqualTo(Hash(original)));feet.localPosition+=Vector3.right;Restore(saved);Assert.That(feet.localPosition,Is.EqualTo(position));Assert.That(feet.localRotation,Is.EqualTo(rotation));Assert.That(JToken.DeepEquals(Capture(),saved),Is.True);
  }
  [Test]public void EditedBodyDimensionsCannotReuseTheApprovedProfileVersion(){
   Create();Node("FIRST_TEST_CAR_A").GetComponent<BoxCollider>().size=new Vector3(13,2.4f,3);
   Assert.Throws<InvalidOperationException>(()=>adapter.Observe(0,1));
  }
  [Test]public void EditedMuzzleSocketCannotReuseTheApprovedProfileVersion(){
   Create();Node("JudgementMuzzle_NotArtSocket").localPosition+=Vector3.right;
   Assert.Throws<InvalidOperationException>(()=>adapter.Observe(0,1));
  }
  [Test]public void FirstJudgementSourcePoseSupportsYawButCannotSilentlyPitchTheWeaponBasis(){
   Create();Node("MiraFeet").localRotation=Quaternion.Euler(10,-90,0);
   Assert.Throws<InvalidOperationException>(()=>adapter.Observe(0,1));
  }
  [Test]public void FractionalNativePositionAndNonCardinalYawRoundTripWithoutLosingMuzzleObservation(){
   Create();Node("MiraFeet").localRotation=Quaternion.Euler(0,-45,0);
   var foot=Node("MiraFeet").position;foot.z=.0001f;Node("MiraFeet").position=foot;
   var state=Capture();Restore(state);Assert.That(JToken.DeepEquals(Capture(),state),Is.True);
  }
  [Test]public void TangentContactMustNotChangeWhenTheSameFixedRayIntervalIsPartitioned(){
   Create();var m=Node("JudgementMuzzle_NotArtSocket").position;
   Node("FIRST_TEST_SCOUT").position=m+new Vector3(3.6f,-1.1f,4.8f);
   var launch=adapter.Observe(0,1);var ray=MiraJudgementGeometry.CreateRay(launch,Hash(launch),"FIRST_TEST_SCOUT");
   Node("FIRST_TEST_SCOUT").position=m+new Vector3(2.088f,-1.1f,2.184f);
   var a=adapter.Observe(1,2);var b=adapter.Observe(2,3);var c=adapter.Observe(3,4);
   var whole=MiraJudgementGeometry.Trace(ray,Hash(ray),a,c,50000,125000);
   var first=MiraJudgementGeometry.Trace(ray,Hash(ray),a,b,50000,60015);
   var second=MiraJudgementGeometry.Trace(ray,Hash(ray),b,c,60015,125000);
   Assert.That((string)whole["kind"],Is.EqualTo("HIT"));
   Assert.That(new[]{(string)first["kind"],(string)second["kind"]},Does.Contain("HIT"));
  }
  [Test]public void NativeRestoreRejectsChangedParentBeforeApplyingLocalPoses(){
   Create();var snapshot=Capture();var target=Node("FIRST_TEST_SCOUT");target.SetParent(adapter.transform,true);
   var before=target.position;Assert.Throws<InvalidOperationException>(()=>Restore(snapshot));
   Assert.That(target.position,Is.EqualTo(before));Assert.That(target.parent,Is.EqualTo(adapter.transform));
  }
  [TearDown]public void Cleanup(){if(adapter!=null)UnityEngine.Object.DestroyImmediate(adapter.gameObject);}
  [Test]public void NativePoseRestoreRewindsMovedCarriagesActorsAndFrozenContact(){
   Create();Box();var original=adapter.Observe(0,1);var state=Capture();var ray=MiraJudgementGeometry.CreateRay(original,Hash(original),"FIRST_TEST_SCOUT");
   var contact=MiraJudgementGeometry.Trace(ray,Hash(ray),original,adapter.Observe(1,2),0,500000);
   Node("FIRST_TEST_CAR_B").position+=new Vector3(10,0,0);Node("MiraFeet").rotation=Quaternion.Euler(0,-180,0);Node("FIRST_TEST_APPROVED_BOX").position+=new Vector3(3,0,0);
   Restore(state);Assert.That(JToken.DeepEquals(adapter.Observe(0,1),original),Is.True);
   Assert.That(JToken.DeepEquals(Capture(),state),Is.True);
   var replay=MiraJudgementGeometry.Trace(ray,Hash(ray),adapter.Observe(0,1),adapter.Observe(1,2),0,500000);
   Assert.That(JToken.DeepEquals(contact,replay),Is.True);
  }
  [Test]public void RestoreRequiresIndependentPinAndLeavesAllLiveTransformsUntouched(){
   Create();var state=Capture();Node("MiraFeet").position+=Vector3.right;
   var before=Capture();Assert.Throws<ArgumentException>(()=>Restore(state,"sha256:"+new string('0',64)));
   Assert.That(JToken.DeepEquals(Capture(),before),Is.True);
  }
  [Test]public void PoseObservationMismatchRejectsBeforeLivePublication(){
   Create();var state=Capture();state["poses"][0]["positionNativeLocalUnits"][0]=9999;Node("MiraFeet").position+=Vector3.right;
   var before=Capture();Assert.Throws<ArgumentException>(()=>Restore(state));
   Assert.That(JToken.DeepEquals(Capture(),before),Is.True);
  }
  [Test]public void RestoreCannotOverwriteTheOriginalGenerationOwner(){
   Create();long generation=1;adapter.BindGenerations(id=>generation);var state=Capture();generation=2;
   var before=Capture();Assert.Throws<ArgumentException>(()=>Restore(state));
   Assert.That(JToken.DeepEquals(Capture(),before),Is.True);Assert.That(generation,Is.EqualTo(2));
  }
  [Test]public void DuplicatePoseIdentityAndObliqueBoxesRejectWithoutChangingLiveState(){
   Create();Box();var state=Capture();((JArray)state["poses"]).Add(state["poses"][0].DeepClone());
   var before=Capture();Assert.Throws<ArgumentException>(()=>Restore(state));Assert.That(JToken.DeepEquals(Capture(),before),Is.True);
   state=Capture();var box=((JArray)state["poses"]).Single(t=>(string)t["id"]=="FIRST_TEST_APPROVED_BOX");
   var q=Quaternion.Euler(0,30,0);box["rotation"]=new JArray(q.x,q.y,q.z,q.w);
   Assert.Throws<ArgumentException>(()=>Restore(state));Assert.That(JToken.DeepEquals(Capture(),before),Is.True);
  }
  [Test]public void BasisChangeNeedsOriginalOwnerRebaseRatherThanSilentSnapshotTeleport(){
   Create();var state=Capture();adapter.transform.position+=Vector3.right;
   var before=Capture();Assert.Throws<ArgumentException>(()=>Restore(state));Assert.That(JToken.DeepEquals(Capture(),before),Is.True);
  }
  [Test]public void ApprovedTestBoxHasItsOwnColliderAndCannotReuseAnActorIdentity(){
   Create();Box();var b=Node("FIRST_TEST_APPROVED_BOX").GetComponent<BoxCollider>();
   Assert.That(b.size,Is.EqualTo(new Vector3(1,.8f,1.2f)));Assert.That(b.center,Is.EqualTo(new Vector3(0,2.8f,0)));
   Assert.Throws<ArgumentException>(()=>Box("FIRST_TEST_SCOUT"));Assert.That(((JArray)adapter.Observe(0,1)["blockers"]).Count,Is.EqualTo(3));
  }
  [TestCase(6000L,125000L,117500L)][TestCase(12000L,250000L,242500L)][TestCase(24000L,500000L,492500L)]
  public void MuzzleToAnchorDistanceAndFirstCapsuleSurfaceContactAreDifferent(long mm,long anchorUs,long contactUs){
   Create();var muzzle=Node("JudgementMuzzle_NotArtSocket").position;
   // Explicit distance fixture, not a claim about actual carriage placement.
   Node("FIRST_TEST_SCOUT").position=muzzle+new Vector3(0,-1.1f,mm/1000f);
   var a=adapter.Observe(0,1);var ray=MiraJudgementGeometry.CreateRay(a,Hash(a),"FIRST_TEST_SCOUT");
   Assert.That((double)ray["anchorTravelUs"],Is.EqualTo(anchorUs).Within(.000001));
   Assert.That(MiraJudgementGeometry.Position(ray,anchorUs),Is.EqualTo(((JArray)ray["anchorMm"]).Select(v=>(long)v).ToArray()));
   var contact=MiraJudgementGeometry.Trace(ray,Hash(ray),a,adapter.Observe(1,2),0,500000);
   Assert.That((string)contact["kind"],Is.EqualTo("HIT"));Assert.That((double)contact["elapsedUs"],Is.EqualTo(contactUs).Within(.000001));
  }
  [Test]public void ApprovedMovingBoxCrossingCanBlockAfterAnInitiallyClearLaunch(){
   Create();Box(x:4000,z:3000);Node("FIRST_TEST_SCOUT").position=Node("JudgementMuzzle_NotArtSocket").position+new Vector3(0,-1.1f,6);
   var a=adapter.Observe(0,1);var ray=MiraJudgementGeometry.CreateRay(a,Hash(a),"FIRST_TEST_SCOUT");
   Node("FIRST_TEST_APPROVED_BOX").position=new Vector3(-4,.8f,3);
   var c=MiraJudgementGeometry.Trace(ray,Hash(ray),a,adapter.Observe(1,2),0,125000);
   Assert.That((string)c["kind"],Is.EqualTo("BLOCKED"));Assert.That((string)c["instanceId"],Is.EqualTo("FIRST_TEST_APPROVED_BOX"));
  }
 }
}
#endif