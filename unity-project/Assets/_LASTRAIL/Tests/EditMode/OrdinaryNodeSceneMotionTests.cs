using System;
using System.Collections.Generic;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Real approved spatial prefabs with synthetic node transport. No muzzle or combat approval.
public sealed class OrdinaryNodeSceneMotionTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Field(object o,string name)=>o.GetType().GetField(name,Hidden).GetValue(o);
 static object Call(object o,string name,params object[] args){var m=o.GetType().GetMethod(name,Hidden);Assert.NotNull(m,"Missing original scene motion callback binding");try{return m.Invoke(o,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 sealed class Fixture:IDisposable {
  internal readonly GameObject Root=new GameObject("motion-test-basis");
  internal GameObject Allies,Enemy,Rail;
  internal OrdinaryFiveStartInput Start;internal OrdinaryNodeStart Node;internal OrdinaryNodeAttempt Attempt;internal FirstPlayableScaling Scaling;
  internal Dictionary<string,Transform> Frames=new Dictionary<string,Transform>();internal Transform RailStart;
  internal bool Current=true;internal object Owner;internal Func<bool> CurrentOverride;
  internal Fixture(bool spatial=true,bool combat=false){
   var method=typeof(OrdinaryNodeEnemiesTests).GetMethod(spatial?"SpatialFixture":"Fixture",BindingFlags.Static|BindingFlags.NonPublic);
   var f=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))method.Invoke(null,null);
   if(combat){
    var ready=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    Assert.AreEqual(f.Start.StartSnapshotHash,ready.Start.StartSnapshotHash);Assert.AreEqual(f.Start.RunId,ready.Start.RunId);
    var entry=ready.Response["receipt"]["nodeStart"];var existing=entry["snapshot"]["runtime"]["enemies"];var spatialEnemies=f.Response["receipt"]["nodeStart"]["snapshot"]["runtime"]["enemies"];
    Assert.IsTrue(JToken.DeepEquals(existing["scalingBindingHash"],spatialEnemies["scalingBindingHash"]));Assert.IsTrue(JToken.DeepEquals(existing["gunners"],spatialEnemies["gunners"]));
    entry["snapshot"]["runtime"]["enemies"]=spatialEnemies.DeepClone();entry["snapshotHash"]=BDirectAttackInput.Digest(entry["snapshot"]).Substring(7);f=ready;
   }
   Start=f.Start;Assert.AreEqual(58000,Start.Rail.InitialFrontArcMm);var result=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);Node=result.NodeStart;Scaling=result.Scaling;Attempt=OrdinaryNodeAttempt.FromEntry(Node);
   Allies=Load("OrdinaryRoofSpawnV01");Enemy=Load("OrdinaryEnemyTrainV01");Rail=Load("OrdinaryFiniteRail1200mV01");RailStart=Rail.transform.Find("Rail/RailStart");
   string[] roles={"Engine","Combat","Gun"};for(int i=0;i<3;i++)Frames.Add(Start.Rail.PhysicalInstanceIds[i],Allies.transform.Find(roles[i]));
   if(Node.Enemies.Spatial!=null)Frames.Add(Node.Enemies.Spatial.PhysicalInstanceId,Enemy.transform);
  }
  GameObject Load(string name){var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Content/Battle/"+name+"/"+name+".prefab");Assert.NotNull(asset);return UnityEngine.Object.Instantiate(asset,Root.transform,false);}
  internal void Create(){var type=typeof(BPhysicalSceneSource).Assembly.GetType("LastRail.Battle.OrdinaryNodeSceneMotion");Assert.NotNull(type,"Missing original-tick scene motion adapter");try{Owner=Activator.CreateInstance(type,new object[]{Start,Node,Attempt,RailStart,Frames,(Func<bool>)(()=>CurrentOverride?.Invoke()??Current)});}catch(TargetInvocationException e){throw e.InnerException;}}
  internal void Apply(long tick,long front){try{Owner.GetType().GetMethod("Apply").Invoke(Owner,new object[]{tick,front});}catch(TargetInvocationException e){throw e.InnerException;}}
  internal float X(int i)=>Frames[Start.Rail.PhysicalInstanceIds[i]].position.x;
  public void Dispose(){if(Owner is IDisposable d)d.Dispose();UnityEngine.Object.DestroyImmediate(Root);}
 }
 [Test] public void ApprovedSpatialPrefabsUseActualRailStartAndRetainUnregisteredWeapons(){using var f=new Fixture();Assert.AreEqual(-25,f.RailStart.position.x);Assert.AreEqual(27,f.X(0));Assert.AreEqual(14,f.X(1));Assert.AreEqual(1,f.X(2));Assert.AreEqual(43,f.Enemy.transform.position.x);Assert.AreEqual(JTokenType.Null,f.Node.Enemies.Spatial.CaptureDefinition()["muzzle"].Type);Assert.AreEqual(JTokenType.Null,f.Node.Enemies.Spatial.CaptureDefinition()["hitVolume"].Type);}
 [Test] public void OriginalFrontMovesAllFourBodiesAndTheirExistingRoofAnchors(){using var f=new Fixture();f.Create();f.Apply(0,58000);WritePose(f,0,58000);f.Apply(1,59000);Assert.AreEqual(28,f.X(0));Assert.AreEqual(15,f.X(1));Assert.AreEqual(2,f.X(2));Assert.AreEqual(44,f.Enemy.transform.position.x);Assert.AreEqual(40,f.Enemy.transform.Find("GunnerFoot_0").position.x);Assert.AreEqual(44,f.Enemy.transform.Find("GunnerFoot_1").position.x);Assert.AreEqual(18,f.Allies.transform.Find("Combat/SpawnFoot_0").position.x);Physics.SyncTransforms();var e=f.Enemy.transform.Find("BodyCollider").GetComponent<BoxCollider>();var p=f.Allies.transform.Find("Engine/BodyCollider").GetComponent<BoxCollider>();Assert.That(e.bounds.min.x-p.bounds.max.x,Is.EqualTo(4).Within(.00001));WritePose(f,1,59000);}
 static void WritePose(Fixture f,long tick,long front){Physics.SyncTransforms();TestContext.WriteLine(new JObject{["tick"]=tick,["frontArcMm"]=front,["bodyWorldXm"]=new JArray(f.X(0),f.X(1),f.X(2),f.Enemy.transform.position.x),["combatRoofFootWorldXm"]=f.Allies.transform.Find("Combat/SpawnFoot_0").position.x,["gunnerFeetWorldXm"]=new JArray(f.Enemy.transform.Find("GunnerFoot_0").position.x,f.Enemy.transform.Find("GunnerFoot_1").position.x),["bodyGapM"]=f.Enemy.transform.Find("BodyCollider").GetComponent<BoxCollider>().bounds.min.x-f.Allies.transform.Find("Engine/BodyCollider").GetComponent<BoxCollider>().bounds.max.x}.ToString(Newtonsoft.Json.Formatting.None));}
 [Test] public void SuppliedFrontIsAbsoluteAndDoesNotIntegrateAnIndependentClock(){using var f=new Fixture();f.Create();f.Apply(700,123000);Assert.AreEqual(92,f.X(0));Assert.AreEqual(108,f.Enemy.transform.position.x);f.Apply(900,123000);Assert.AreEqual(92,f.X(0));}
 [Test] public void SameTickDifferentFrontAndOlderTickAreRejectedBeforeMovingAnything(){using var f=new Fixture();f.Create();f.Apply(1,59000);f.Apply(1,59000);Assert.Throws<InvalidOperationException>(()=>f.Apply(1,60000));Assert.Throws<InvalidOperationException>(()=>f.Apply(0,58000));Assert.AreEqual(28,f.X(0));Assert.AreEqual(44,f.Enemy.transform.position.x);}
 [Test] public void RetiredAttemptAndDisposedOwnerCannotMoveBorrowedObjects(){using var f=new Fixture();f.Create();f.Current=false;Assert.Throws<InvalidOperationException>(()=>f.Apply(0,59000));Assert.AreEqual(27,f.X(0));f.Current=true;((IDisposable)f.Owner).Dispose();Assert.Throws<ObjectDisposedException>(()=>f.Apply(0,59000));Assert.IsTrue(f.Enemy);}
 [Test] public void HistoricalNodeWithoutSpatialSourceCannotAcquireCurrentEnemyGeometry(){using var f=new Fixture(false);Assert.IsNull(f.Node.Enemies.Spatial);Assert.Throws<ArgumentException>(f.Create);}
 [Test] public void EveryOriginalPhysicalIdentityIsRequired(){using var f=new Fixture();var id=f.Start.Rail.PhysicalInstanceIds[0];var frame=f.Frames[id];f.Frames.Remove(id);f.Frames.Add("foreign-body",frame);Assert.Throws<ArgumentException>(f.Create);}
 [Test] public void ChangedBodyDimensionsCannotBecomeImplicitApprovedGeometry(){using var f=new Fixture();f.Enemy.transform.Find("BodyCollider").GetComponent<BoxCollider>().size=Vector3.one;Assert.Throws<ArgumentException>(f.Create);}
 [Test] public void InitiallyMissingBodyIsRejectedButLaterDestructionRemainsObservable(){using var missing=new Fixture();UnityEngine.Object.DestroyImmediate(missing.Enemy);Assert.Throws<ArgumentException>(missing.Create);using var f=new Fixture();f.Create();f.Apply(0,58000);UnityEngine.Object.DestroyImmediate(f.Enemy);Assert.DoesNotThrow(()=>f.Apply(1,59000));Assert.AreEqual(28,f.X(0));Assert.IsFalse(f.Frames[f.Node.Enemies.Spatial.PhysicalInstanceId]);}
 [Test] public void MovedRailBasisOrChangedBodyFailsBeforePartialPosePublication(){using var f=new Fixture();f.Create();f.RailStart.position+=Vector3.right;Assert.Throws<InvalidOperationException>(()=>f.Apply(0,59000));Assert.AreEqual(27,f.X(0));using var changed=new Fixture();changed.Create();changed.Enemy.transform.Find("BodyCollider").GetComponent<BoxCollider>().size=Vector3.one;Assert.Throws<InvalidOperationException>(()=>changed.Apply(0,59000));Assert.AreEqual(27,changed.X(0));}
 [Test] public void SameTickCannotSilentlyRepairAnExternallyChangedBodyPose(){using var f=new Fixture();f.Create();f.Apply(0,58000);f.Enemy.transform.position+=Vector3.right;Assert.Throws<InvalidOperationException>(()=>f.Apply(0,58000));Assert.AreEqual(44,f.Enemy.transform.position.x);}
 [Test] public void AttemptInvalidationDuringValidationAndReentryCannotPublishPose(){using var f=new Fixture();f.Create();int calls=0;f.CurrentOverride=()=>++calls==1;Assert.Throws<InvalidOperationException>(()=>f.Apply(0,59000));Assert.AreEqual(27,f.X(0));f.CurrentOverride=()=>{f.Apply(0,59000);return true;};Assert.Throws<InvalidOperationException>(()=>f.Apply(0,59000));Assert.AreEqual(27,f.X(0));}
 [Test] public void OriginalModelMovesActualFourBodiesBeforeTestGunnerObservesTheFirstThreeTicks(){
  using var f=new Fixture(combat:true);f.Create();
  var factory=OrdinaryFiveCombatFactory.FromNodeStart(f.Start,f.Node,f.Scaling,170000,f.Attempt);
  factory=(OrdinaryFiveCombatFactory)Call(factory,"WithSceneMotion",(Action<long,long>)((OrdinaryNodeSceneMotion)f.Owner).Apply);
  // Existing explicit QA weapon observations are retained; this proves pose ordering, not weapon registration.
  var qa=Activator.CreateInstance(typeof(OrdinaryNodeGunnerExecutionTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,Array.Empty<object>(),null);
  var original=(OrdinaryFiveCombatFactory)Field(qa,"Factory");Assert.AreEqual(f.Start.StartSnapshotHash,original.Original.StartSnapshotHash);Assert.AreEqual(f.Scaling.BindingHash,((FirstPlayableScaling)typeof(OrdinaryFiveCombatFactory).GetProperty("Encounter",Hidden).GetValue(original)).BindingHash);
  var source=Field(qa,"Sources");var observe=(Func<string,long,JObject>)Field(source,"Observe");var observed=new HashSet<long>();
  var supplied=Activator.CreateInstance(source.GetType(),Hidden,null,new object[]{(Func<string,long,JObject>)((id,tick)=>{
   Assert.That(f.X(0),Is.EqualTo(27+tick*.34).Within(.00001));Assert.That(f.X(1),Is.EqualTo(14+tick*.34).Within(.00001));Assert.That(f.X(2),Is.EqualTo(1+tick*.34).Within(.00001));Assert.That(f.Enemy.transform.position.x,Is.EqualTo(43+tick*.34).Within(.00001));
   if(observed.Add(tick)&&(tick==0||tick==3))WritePose(f,tick,58000+tick*340);
   var o=observe(id,tick);o["nodeId"]=f.Node.NodeId;o["snapshotHash"]=f.Node.SnapshotHash;o["attemptGeneration"]=f.Attempt.Generation;return o;
  }),Field(source,"CaptureAttack"),Field(source,"CaptureDefense"),Field(source,"Interception")},null);
  factory=(OrdinaryFiveCombatFactory)Call(factory,"WithGunnerSources",supplied);var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>f.Current);driver.Open();
  try{driver.Advance(170000L*3,3);Assert.AreEqual(3,driver.Tick);CollectionAssert.AreEquivalent(new long[]{0,1,2,3},observed);Assert.AreEqual(0,(int)Field(qa,"Launches"),"Three original ticks precede the original full first attack period");Assert.IsFalse(driver.IsExecutionReady);Assert.IsFalse(driver.RecoveryRequired);}finally{driver.Retire();}
 }
 [TestCase(false)][TestCase(true)] public void OwnerCallbackPrecedesManualAndStepObservationsAndSurvivesCopiesPauseAndDebt(bool reenterPause){
  var f=Activator.CreateInstance(typeof(OrdinaryReinaBarrageExecutionTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,new object[]{170000L},null);
  var factory=OrdinaryReinaBarrageHostTests.NewFactory();var source=Field(Field(f,"Execution"),"sources");var observe=(Func<string,long,JObject>)Field(source,"Observe");
  long appliedTick=-1,appliedFront=-1;int applications=0,observations=0;bool failAfterPose=false,triedReentry=false;CombatSceneDriver driver=null;
  Action<long,long> apply=(tick,front)=>{appliedTick=tick;appliedFront=front;applications++;if(reenterPause&&!triedReentry){triedReentry=true;Assert.Throws<InvalidOperationException>(()=>driver.SetPaused(true),"A nested pause must not be silently overwritten by the outer candidate");}if(failAfterPose&&tick>10)throw new InvalidOperationException("Test source failure after scene pose publication");};
  factory=(OrdinaryFiveCombatFactory)Call(factory,"WithSceneMotion",apply);
  var boundSource=Activator.CreateInstance(source.GetType(),Hidden,null,new object[]{(Func<string,long,JObject>)((command,tick)=>{Assert.AreEqual(tick,appliedTick,"Scene pose must be applied before the original spatial observation");Assert.AreEqual(58000+tick*340,appliedFront,"Original fixed-step motion is the sole front source");observations++;var o=observe(command,tick);o["nodeId"]=factory.Attempt.NodeId;o["snapshotHash"]=factory.Attempt.SnapshotHash;o["attemptGeneration"]=factory.Attempt.Generation;return o;}),Field(source,"CaptureAttack"),Field(source,"CaptureDefense"),Field(source,"Lifesteal")},null);
  factory=(OrdinaryFiveCombatFactory)Call(factory,"WithReinaBarrageSources",boundSource);
  driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();driver.SetUltimateAuto(false);Assert.AreEqual(0,applications,"Construction and owned copies must not reset scene pose to tick zero");
  try{Assert.AreEqual("CASTING",driver.RequestSkill("motion-cast","op_reina","op_reina.ultimate"));Assert.Greater(observations,0);driver.Advance(170000L*10,2);Assert.AreEqual(2,driver.Tick);Assert.AreEqual(58680,appliedFront);int calls=applications;driver.SetPaused(true);driver.Advance(170000L*10,10);Assert.AreEqual(calls,applications);driver.SetPaused(false);driver.Advance(0,8);Assert.AreEqual(10,driver.Tick);Assert.AreEqual(61400,appliedFront);Assert.Greater(applications,calls);
   failAfterPose=true;Assert.Throws<InvalidOperationException>(()=>driver.Advance(170000,1));Assert.IsTrue(driver.RecoveryRequired);Assert.AreEqual(11,appliedTick);Assert.AreEqual(10,driver.Tick);calls=applications;Assert.Throws<InvalidOperationException>(()=>driver.Advance(170000,1));Assert.Throws<InvalidOperationException>(driver.Open);Assert.AreEqual(calls,applications,"A failed candidate cannot reuse the ahead-of-model scene without a new attempt");
  }finally{driver.Retire();}
 }
}
