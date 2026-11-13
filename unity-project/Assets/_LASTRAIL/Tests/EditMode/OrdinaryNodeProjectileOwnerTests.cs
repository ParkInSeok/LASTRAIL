using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryNodeProjectileOwnerTests {
 [Test] public void DirectHealthEqualityMatchesExportAndRejectsDivergingClone(){
  var f=new Fixture();var export=JObject.Parse(f.Health.CaptureLocal());Assert.IsTrue(JToken.DeepEquals(export,(JToken)Call(f.Health,"CaptureState")));var identity=(JObject)Call(f.Health,"CaptureIdentity");Assert.AreEqual((string)export["schema"],(string)identity["schema"]);Assert.IsTrue(JToken.DeepEquals(export["definition"],identity["definition"]));var copy=(BCombatLocalModel)Call(f.Health,"CloneOwnedNode");
  Assert.IsTrue((bool)Call(f.Health,"HasSameState",copy));Assert.IsTrue(JToken.DeepEquals(JObject.Parse(f.Health.CaptureLocal()),JObject.Parse(copy.CaptureLocal())));
  copy.ApplyResolvedDamage("different-hit",f.Target,1,1L);
  Assert.IsFalse((bool)Call(f.Health,"HasSameState",copy));Assert.IsFalse(JToken.DeepEquals(JObject.Parse(f.Health.CaptureLocal()),JObject.Parse(copy.CaptureLocal())));
 }
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 sealed class Fixture {
  internal readonly OrdinaryFiveCombatFactory Factory;
  internal readonly BCombatLocalModel Health;
  internal readonly string Target;
  internal readonly object Owner;
  internal bool Current=true;
  internal Fixture(bool differentNode=false){
   var source=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
   if(differentNode){var node=source.Response["receipt"]["nodeStart"];node["snapshot"]["runtime"]["effects"]["unsupportedEffect"]="different-original";node["snapshotHash"]=BDirectAttackInput.Digest(node["snapshot"]).Substring(7);}
   var result=BRouteCommandResult.Read(200,source.Response.ToString(),source.Command);
   Factory=OrdinaryFiveCombatFactory.FromNodeStart(source.Start,result.NodeStart,result.Scaling,170000);
   Health=(BCombatLocalModel)typeof(BasicCombatSimulation).GetField("ordinaryHealth",Hidden).GetValue(Factory.Create());
   Target=result.NodeStart.Enemies.Gunners[0].InstanceId;
   var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryNodeProjectileOwner");
   Assert.NotNull(type,"Missing node-owned physical projectile lifetime.");
   Owner=Activator.CreateInstance(type,Hidden,null,new object[]{Factory,Health,(Func<bool>)(()=>Current)},null);
  }
  internal JObject Observation(long tick=0,string actor="op_reina"){
   var p=Factory.Original.Basic.Profiles.Single(x=>x.OperatorId==actor);
   var profile=new JObject{["operatorId"]=actor,["abilityId"]=p.AbilityId,["launchSource"]=p.LaunchSource,["ballistics"]=new JObject{
    ["speedMmPerSecond"]=p.SpeedMmPerSecond,["radiusMm"]=p.RadiusMm,["maxDistanceMm"]=p.MaximumFlightDistanceMm,["adjacentCars"]=p.AdjacentCars,
    ["maxFlightUs"]=(p.FlightNumeratorMs*1000+p.FlightDenominator-1)/p.FlightDenominator}};
   var basis=new JObject{["testOnlyMeasuredFrame"]="explicit-synthetic-not-product-authoring"};
   return new JObject{["version"]=MiraJudgementSceneAdapter.ObservationVersion,["tick"]=tick,["revision"]=tick+1,
    ["attemptGeneration"]=Factory.Attempt.Generation,["nodeId"]=Factory.Attempt.NodeId,["snapshotHash"]=Factory.Attempt.SnapshotHash,
    ["profile"]=profile,["profileHash"]=BDirectAttackInput.Digest(profile),["basis"]=basis,["basisHash"]=BDirectAttackInput.Digest(basis),["layoutHash"]="explicit-test-layout",
    ["source"]=new JObject{["id"]=actor,["generation"]=1,["executionEntityId"]=actor+":body",["domain"]="test-roof",["slot"]=0,["muzzleMm"]=new JArray(0,500,0)},
    ["targets"]=new JArray(new JObject{["id"]=Target,["generation"]=1,["domain"]="test-roof",["slot"]=0,["eligible"]=true,["anchorMm"]=new JArray(5000,500,0),["feetMm"]=new JArray(5000,0,0),["heightMm"]=1800,["radiusMm"]=300}),["blockers"]=new JArray()};
  }
  internal bool Launch(JObject observation=null,JObject attack=null)=> (bool)Call(Owner,"Launch","event-one","op_reina","projectile-one",Target,observation??Observation(),BDirectAttackInput.Digest(observation??Observation()),attack??Attack());
  internal JObject Advance(JObject begin,JObject end,JObject defense=null)=>(JObject)Call(Owner,"Advance","projectile-one",begin,end,defense??Defense());
 }
 static object Call(object owner,string method,params object[] args){try{return owner.GetType().GetMethods(Hidden).Single(m=>m.Name==method&&m.GetParameters().Length==args.Length).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static JObject Attack()=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=new JObject{["numerator"]="100",["denominator"]="1"},["appliedOffense"]=new JArray(),["penetration"]=new JArray()};
 static JObject Defense()=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=new JObject{["numerator"]="0",["denominator"]="1"},["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
 [Test] public void ResolvedFlightKeepsReplayProofWithoutRetainingObservationBuffers(){
  var f=new Fixture();var begin=f.Observation();var end=f.Observation(5);f.Launch(begin);var result=f.Advance(begin,end);int hp=f.Health.Hp(f.Target);
  var shot=((JArray)((JObject)Call(f.Owner,"Capture"))["shots"])[0];
  Assert.IsNull(shot["latestObservation"]);Assert.IsNull(shot["launch"]);Assert.IsNull(shot["lastAdvance"]);Assert.IsNull(shot["ray"]);
  Assert.IsFalse(f.Launch(begin));Assert.IsTrue(JToken.DeepEquals(result,f.Advance(begin,end)));Assert.AreEqual(hp,f.Health.Hp(f.Target));
  var changed=(JObject)begin.DeepClone();changed["revision"]=(long)changed["revision"]+1;Assert.Throws<ArgumentException>(()=>f.Launch(changed));
  Assert.Throws<InvalidOperationException>(()=>f.Advance(begin,f.Observation(6)));
  var copyHealth=(BCombatLocalModel)Call(f.Health,"CloneOwnedNode");var copy=Call(f.Owner,"CloneOwned",copyHealth);
  Assert.IsTrue(JToken.DeepEquals(result,(JObject)Call(copy,"Advance","projectile-one",begin,end,Defense())));Assert.AreEqual(hp,copyHealth.Hp(f.Target));
 }
 [Test] public void ActualSweepAppliesOnceToSameKernelAndRetainsAttributionAfterSourceDeath(){
  var f=new Fixture();var a=f.Observation();int before=f.Health.Hp(f.Target);Assert.IsTrue(f.Launch(a));Assert.IsFalse(f.Launch(a));
  f.Health.ApplyResolvedDamage("source-down","op_reina",10000,0);
  var z=f.Observation(5);var hit=f.Advance(a,z);Assert.AreEqual("HIT",(string)hit["kind"]);Assert.Less(f.Health.Hp(f.Target),before);
  var hp=f.Health.Hp(f.Target);Assert.IsTrue(JToken.DeepEquals(hit,f.Advance(a,z)));Assert.AreEqual(hp,f.Health.Hp(f.Target));
  var capture=(JObject)Call(f.Owner,"Capture");var text=capture.ToString();StringAssert.Contains("op_reina.basic",text);StringAssert.Contains("op_reina:body",text);StringAssert.Contains("event-one",text);StringAssert.Contains("projectile-one",text);
 }
 [Test] public void InterceptionCandidatesRetainActualFlightThroughItsFinalAdvanceTick(){
  var f=new Fixture();var begin=f.Observation();
  Assert.IsFalse((bool)Call(f.Owner,"HasInterceptionCandidates",0L));
  Assert.IsTrue(f.Launch(begin));
  // Query a later tick so this assertion requires the live flight, not just its launch tick.
  Assert.IsTrue((bool)Call(f.Owner,"HasInterceptionCandidates",1L));
  int before=f.Health.Hp(f.Target);var result=f.Advance(begin,f.Observation(5));
  Assert.AreEqual("HIT",(string)result["kind"]);Assert.Less(f.Health.Hp(f.Target),before);
  Assert.IsTrue((bool)Call(f.Owner,"HasInterceptionCandidates",5L));
  Assert.IsFalse((bool)Call(f.Owner,"HasInterceptionCandidates",6L));
 }
 [TestCase("generation")][TestCase("miss")][TestCase("blocker")][TestCase("expired")]
 public void NonContactsNeverApplyRemoteDamage(string reason){
  var f=new Fixture();var a=f.Observation();var z=f.Observation(reason=="expired"?30:5);var begin=(JObject)a.DeepClone();
  if(reason=="generation")z["targets"][0]["generation"]=2;
  if(reason=="miss"||reason=="expired")z["targets"][0]["feetMm"]=new JArray(5000,0,50000);
  if(reason=="blocker"){
   // At 42.5ms (half the 5*17ms segment), the 72m/s ray reaches x3060
   // while the translated solid crosses z0. It is clear of launch LOS.
   ((JArray)a["blockers"]).Add(new JObject{["id"]="solid",["centerMm"]=new JArray(3060,500,5000),["sizeMm"]=new JArray(500,1500,500)});
   begin=(JObject)a.DeepClone();((JArray)z["blockers"]).Add(new JObject{["id"]="solid",["centerMm"]=new JArray(3060,500,-5000),["sizeMm"]=new JArray(500,1500,500)});
  }
  f.Launch(a);int before=f.Health.Hp(f.Target);var result=f.Advance(begin,z);
  Assert.AreNotEqual("HIT",(string)result["kind"]);Assert.AreEqual(before,f.Health.Hp(f.Target));
  if(reason=="expired")Assert.AreEqual("EXPIRED",(string)result["kind"]);
  if(reason=="blocker")Assert.AreEqual("BLOCKED",(string)result["kind"]);
 }
 [TestCase("speedMmPerSecond")][TestCase("radiusMm")][TestCase("maxDistanceMm")][TestCase("maxFlightUs")]
 public void RehashedBallisticsCannotReplaceOriginalProfile(string field){var f=new Fixture();var o=f.Observation();o["profile"]["ballistics"][field]=(long)o["profile"]["ballistics"][field]+1;o["profileHash"]=BDirectAttackInput.Digest(o["profile"]);Assert.Throws<ArgumentException>(()=>f.Launch(o));}
 [Test] public void ExplicitAttackSourcesAndCurrentAttemptAreRequired(){var f=new Fixture();var attack=Attack();attack.Remove("appliedOffense");Assert.Throws<ArgumentException>(()=>f.Launch(attack:attack));f.Current=false;Assert.Throws<InvalidOperationException>(()=>f.Launch());}
 [Test] public void CloneOwnsIndependentFlightAndPublishesOnlyIntoClonedKernel(){
  var f=new Fixture();var a=f.Observation();f.Launch(a);var before=(JObject)Call(f.Owner,"Capture");
  var health=(BCombatLocalModel)Call(f.Health,"CloneOwnedNode");var copy=Call(f.Owner,"CloneOwned",health);int originalHp=f.Health.Hp(f.Target);
  Call(copy,"Advance","projectile-one",a,f.Observation(5),Defense());Assert.Less(health.Hp(f.Target),originalHp);Assert.AreEqual(originalHp,f.Health.Hp(f.Target));Assert.IsTrue(JToken.DeepEquals(before,(JObject)Call(f.Owner,"Capture")));
 }
 [Test] public void ConstructorRejectsKernelFromAnotherOriginalNode(){
  var f=new Fixture();var other=new Fixture(true);
  var error=Assert.Throws<TargetInvocationException>(()=>Activator.CreateInstance(f.Owner.GetType(),Hidden,null,new object[]{f.Factory,other.Health,(Func<bool>)(()=>true)},null));
  Assert.IsInstanceOf<ArgumentException>(error.InnerException);
 }
 [TestCase("op_reina")][TestCase("op_sera")][TestCase("op_mira")][TestCase("op_kyle")][TestCase("op_eve")]
 public void OriginalPhysicalProjectileProfilesUseActualSweep(string actor){
  var f=new Fixture();var begin=f.Observation(actor:actor);var end=f.Observation(12,actor);int hp=f.Health.Hp(f.Target);
  Assert.IsTrue((bool)Call(f.Owner,"Launch","actor-event",actor,"actor-projectile",f.Target,begin,BDirectAttackInput.Digest(begin),Attack()));
  var result=(JObject)Call(f.Owner,"Advance","actor-projectile",begin,end,Defense());
  Assert.AreEqual("HIT",(string)result["kind"]);Assert.AreEqual(actor+".basic",(string)result["abilityId"]);Assert.Less(f.Health.Hp(f.Target),hp);
  // Kyle here exercises only his original RANGED projectile, never melee permission.
 }
 [Test] public void CloneRequiresSeparateEquivalentKernel(){
  var f=new Fixture();Assert.Throws<ArgumentException>(()=>Call(f.Owner,"CloneOwned",f.Health));
  var altered=(BCombatLocalModel)Call(f.Health,"CloneOwnedNode");altered.ApplyResolvedDamage("changed-before-clone",f.Target,1,0);
  Assert.Throws<ArgumentException>(()=>Call(f.Owner,"CloneOwned",altered));
 }
 [TestCase(false)][TestCase(true)] public void SameTickCannotReplaceLastCommittedGeometry(bool afterAdvance){
  var f=new Fixture();var a=f.Observation();f.Launch(a);var begin=a;
  if(afterAdvance){begin=f.Observation(1);f.Advance(a,begin);}
  var changed=(JObject)begin.DeepClone();changed["targets"][0]["feetMm"]=new JArray(0,0,0);
  var before=f.Health.CaptureLocal();var flight=(JObject)Call(f.Owner,"Capture");
  Assert.Throws<ArgumentException>(()=>f.Advance(changed,f.Observation(5)));
  Assert.AreEqual(before,f.Health.CaptureLocal());Assert.IsTrue(JToken.DeepEquals(flight,(JObject)Call(f.Owner,"Capture")));
 }
}
