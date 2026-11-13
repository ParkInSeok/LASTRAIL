using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

// Synthetic registration only: real components, same original START/node/attempt, no product pin approval.
public sealed class OrdinaryReinaMeasuredSourcesTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Call(object o,string name,params object[] args){try{return o.GetType().GetMethod(name,Hidden).Invoke(o,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static object Field(object o,string name)=>o.GetType().GetField(name,Hidden).GetValue(o);
 sealed class Authority:ICombatSkillLifestealAuthority {
  public string BindingId=>"TEST_ONLY_MEASURED_NOT_OWNED";
  public CombatSkillLifestealProof ResolveOriginal(ConfirmedSkillDamage d,long tick)=>new CombatSkillLifestealProof("TEST_ONLY_MEASURED_PROOF","TEST_ONLY_ORIGINAL_PROOF",false,true,true,10000);
 }
 sealed class Fixture:IDisposable {
  internal readonly GameObject Root=new GameObject("TEST_ONLY_measured_reina");
  internal readonly OrdinaryFiveCombatFactory Factory;internal readonly OrdinaryNodeSceneGeometry Geometry;
  internal readonly Dictionary<string,Transform> Frames=new Dictionary<string,Transform>();
  internal readonly Authority Proof=new Authority();internal readonly Func<string,long,JObject> Attack,Defense;
  internal object Bridge,Sources;internal string Primary,Extra,FrameId;internal BoxCollider Wall;internal long Life=1;internal bool Current=true;internal Action<JObject> Mutate;
  internal Fixture(){
   JObject Anchor(string id,string role)=>new JObject{["id"]=id,["role"]=role,["positionMm"]=new JArray(0,500,0)};
   JObject Entity(string id,string role,string reference,JObject anchor)=>new JObject{["authoringId"]=id,["role"]=role,["definitionRef"]=reference,["framePath"]=id,["assetPath"]="Assets/TestOnly.prefab",["assetHash"]=new string('b',64),["anchors"]=new JArray(anchor),["shapes"]=new JArray()};
   var muzzle=Anchor("muzzle","EMISSION");muzzle["abilityId"]="op_reina.ultimate";
   var wall=Entity("wall","WORLD","wall",Anchor("origin","HIT"));wall["shapes"]=new JArray(new JObject{["id"]="solid",["role"]="OCCLUDER",["kind"]="BOX",["centerMm"]=new JArray(0,500,0),["sizeMm"]=new JArray(100,2000,2000)});
   var definitions=new JArray(Entity("reina","OPERATOR","op_reina",muzzle),Entity("train","TRAIN","train",Anchor("origin","HIT")),Entity("enemy0","ENEMY","enemy_spike_cart_gunner",Anchor("hit","HIT")),Entity("enemy1","ENEMY","enemy_spike_cart_gunner",Anchor("hit","HIT")),wall);
   var definition=new JObject{["version"]="ordinary.native-geometry-definition.v1.20261031",["definitionId"]="TEST_ONLY_REINA_MEASURED",["units"]="MILLIMETERS",["millimetersPerUnit"]=1000,["axis"]="X_FORWARD_Y_UP_Z_SIDE",["coordinateSpace"]="BOUND_FRAME_LOCAL",["source"]=new JObject{["scenePath"]="Assets/TestOnly.unity",["sceneHash"]=new string('a',64)},["entities"]=definitions};
   var bytes=definition.ToString(Newtonsoft.Json.Formatting.None);Factory=OrdinaryReinaBarrageHostTests.NewFactory(bytes);
   var input=Factory.GetType().GetProperty("NodeInput",Hidden).GetValue(Factory);var node=(OrdinaryNodeStart)input.GetType().GetProperty("Source",Hidden).GetValue(input);
   var ids=node.Enemies.Gunners.Select(g=>g.InstanceId).ToArray();Primary=ids[0];Extra=ids[1];
   // This retained v1 enemy fixture has no Spatial row. Use its actual original rail frame;
   // targets are projected into that frame, without inventing an enemy physical identity.
   FrameId=Factory.Original.Rail.PhysicalInstanceIds.First();
   Geometry=new OrdinaryNodeSceneGeometry(Factory.Original,node,Factory.Attempt,bytes,Root.transform,()=>Current);
   string[] runtime={"op_reina",FrameId,Primary,Extra,"wall"};float[] x={0,0,5,7,3};
   for(int i=0;i<definitions.Count;i++){
    var e=definitions[i];var frame=new GameObject((string)e["authoringId"]).transform;frame.SetParent(Root.transform,false);frame.localPosition=new Vector3(x[i],0,0);Frames.Add(runtime[i],frame);
    var anchor=new GameObject("anchor").transform;anchor.SetParent(frame,false);anchor.localPosition=new Vector3(0,.5f,0);
    var shapes=new Dictionary<string,Collider>();if(i==4){Wall=frame.gameObject.AddComponent<BoxCollider>();Wall.center=new Vector3(0,.5f,0);Wall.size=new Vector3(.1f,2,2);Wall.enabled=false;shapes.Add("solid",Wall);}
    Geometry.BindEntity(runtime[i],(string)e["authoringId"],frame,new Dictionary<string,Transform>{{(string)e["anchors"][0]["id"],anchor}},shapes,()=>Life);
   }
   Attack=(id,tick)=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=new JObject{["numerator"]="10",["denominator"]="1"},["appliedOffense"]=new JArray(),["penetration"]=new JArray()};
   Defense=(id,tick)=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=new JObject{["numerator"]="0",["denominator"]="1"},["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
  }
  internal JObject State(string command,long tick){
   JObject Target(string id,int order)=>new JObject{["id"]=id,["generation"]=999,["battleOrder"]=order,["carDelta"]=0,["eligible"]=true,["protected"]=false,["castLineOfSight"]=true,["localAnchorMm"]=new JArray(1,2,3)};
   var o=new JObject{["tick"]=tick,["revision"]=tick+1,["nodeId"]=Factory.Attempt.NodeId,["snapshotHash"]=Factory.Attempt.SnapshotHash,["attemptGeneration"]=Factory.Attempt.Generation,["source"]=new JObject{["id"]="op_reina",["executorId"]="op_reina:flare",["abilityId"]="op_reina.ultimate",["actionEligible"]=true,["generation"]=999,["muzzleMm"]=new JArray(99,99,99)},["frame"]=new JObject{["id"]=FrameId,["generation"]=999},["primaryTargetId"]=Primary,["targets"]=new JArray(Target(Primary,0),Target(Extra,1)),["solidOccluders"]=new JArray()};Mutate?.Invoke(o);return o;
  }
  internal void Bind(){var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryReinaMeasuredSources");Assert.NotNull(type,"Missing original-node measured Reina bridge");try{Bridge=Activator.CreateInstance(type,Hidden,null,new object[]{Factory,Geometry,(Func<string,long,JObject>)State,Attack,Defense,Proof},null);}catch(TargetInvocationException e){throw e.InnerException;}Sources=type.GetProperty("Sources",Hidden).GetValue(Bridge);}
  internal JObject Observe(long tick=0)=>((Func<string,long,JObject>)Field(Sources,"Observe"))("cast",tick);
  public void Dispose(){Geometry.Dispose();UnityEngine.Object.DestroyImmediate(Root);}
 }
 [Test] public void RealMeasurementOverridesClaimsAndUsesOriginalLifeGeneration(){using var f=new Fixture();f.Bind();var o=f.Observe();Assert.AreEqual(0,(int)o["source"]["muzzleMm"][0]);Assert.AreEqual(5000,(int)o["targets"][0]["localAnchorMm"][0]);Assert.AreEqual(1,(int)o["frame"]["generation"]);Assert.AreEqual(1,(int)o["targets"][0]["generation"]);f.Life=2;Assert.AreEqual(2,(int)f.Observe(1)["frame"]["generation"]);}
 [Test] public void ExistingEffectAndLifestealPortsArePassedThroughWithoutReplacement(){using var f=new Fixture();f.Bind();Assert.AreSame(f.Attack,Field(f.Sources,"CaptureAttack"));Assert.AreSame(f.Defense,Field(f.Sources,"CaptureDefense"));Assert.AreSame(f.Proof,Field(f.Sources,"Lifesteal"));}
 [Test] public void MeasuredSolidDeterminesCastLosDespiteSemanticClaims(){using var f=new Fixture();f.Bind();Assert.IsTrue((bool)f.Observe()["targets"][0]["castLineOfSight"]);f.Wall.enabled=true;var o=f.Observe(1);Assert.IsFalse((bool)o["targets"][0]["castLineOfSight"]);Assert.AreEqual(1,o["solidOccluders"].Count());}
 [Test] public void InactiveAndDestroyedEntitiesUseExistingSkipAndCancellationInputs(){using var f=new Fixture();f.Bind();f.Observe();f.Frames[f.Primary].gameObject.SetActive(false);Assert.IsFalse((bool)f.Observe(1)["targets"][0]["eligible"]);UnityEngine.Object.DestroyImmediate(f.Frames[f.FrameId].gameObject);Assert.IsFalse((bool)f.Observe(2)["frame"]["alive"]);}
 [Test] public void RotatedSolidCannotBeExpandedIntoAnInventedAxisAlignedBox(){using var f=new Fixture();f.Bind();f.Wall.enabled=true;f.Frames["wall"].localRotation=Quaternion.Euler(0,30,0);Assert.Throws<InvalidOperationException>(()=>f.Observe());}
 [Test] public void ForeignAttemptAndLostNativeOwnerFailClosed(){using var f=new Fixture();f.Bind();f.Mutate=o=>o["snapshotHash"]=new string('f',64);Assert.Throws<InvalidOperationException>(()=>f.Observe());f.Mutate=null;f.Current=false;Assert.Throws<InvalidOperationException>(()=>f.Observe(1));}
 [Test] public void ExistingEligibilityProtectionAndOrderRemainOriginalAuthority(){using var f=new Fixture();f.Bind();f.Mutate=o=>{o["targets"][0]["eligible"]=false;o["targets"][1]["protected"]=true;o["targets"][1]["battleOrder"]=42;};var o=f.Observe();Assert.IsFalse((bool)o["targets"][0]["eligible"]);Assert.IsTrue((bool)o["targets"][1]["protected"]);Assert.AreEqual(42,(int)o["targets"][1]["battleOrder"]);}
 [Test] public void CameraAndRendererCullingNeverRetirePhysicalFramesOrAlterDamageReceipts(){
  JArray Run(bool culled){using var f=new Fixture();f.Bind();
   var camera=new GameObject("TEST_ONLY_camera").AddComponent<Camera>();camera.transform.SetParent(f.Root.transform,false);camera.enabled=false;
   var visual=new GameObject("TEST_ONLY_visual",typeof(MeshRenderer));visual.transform.SetParent(f.Frames[f.Primary],false);
   var bound=(OrdinaryFiveCombatFactory)Call(f.Factory,"WithReinaBarrageSources",f.Sources);var d=CombatSceneDriver.ForOrdinaryNode(bound,()=>true);d.Open();d.SetUltimateAuto(false);
   try{d.RequestSkill("cast","op_reina","op_reina.ultimate");d.Advance(170000L*48,48);
    if(culled){camera.transform.position=new Vector3(10000,10000,-10000);camera.cullingMask=0;visual.GetComponent<MeshRenderer>().enabled=false;visual.SetActive(false);}
    d.Advance(170000L*224,224);var results=new JArray(d.HudState["effectResults"].Where(r=>(string)r["abilityId"]=="op_reina.ultimate"&&(string)r["kind"]=="DAMAGE").Select(r=>r.DeepClone()));
    Assert.AreEqual(8,results.Count);return results;
   }finally{d.Retire();}
  }
  Assert.IsTrue(JToken.DeepEquals(Run(false),Run(true)),"Camera visibility must not change original pulse or HP receipts.");
 }
 [Test] public void ActualMeasuredSourcesReachOriginalFactoryAndFourPulseHealthReceipts(){using var f=new Fixture();f.Bind();var bound=(OrdinaryFiveCombatFactory)Call(f.Factory,"WithReinaBarrageSources",f.Sources);var d=CombatSceneDriver.ForOrdinaryNode(bound,()=>true);d.Open();d.SetUltimateAuto(false);try{Assert.AreEqual("CASTING",d.RequestSkill("cast","op_reina","op_reina.ultimate"));d.Advance(170000L*48,48);d.Advance(170000L*224,224);var hits=d.HudState["effectResults"].Where(r=>(string)r["abilityId"]=="op_reina.ultimate"&&(string)r["kind"]=="DAMAGE").ToArray();Assert.AreEqual(8,hits.Length);CollectionAssert.AreEquivalent(new[]{0,1,2,3},hits.Select(r=>(int)r["pulseIndex"]).Distinct());Assert.IsTrue(hits.All(r=>r["healthReceipt"]!=null));}finally{d.Retire();}}
}
