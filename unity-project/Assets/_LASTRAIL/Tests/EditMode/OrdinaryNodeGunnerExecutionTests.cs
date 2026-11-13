using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryNodeGunnerExecutionTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object value,string name)=>value.GetType().GetField(name,Hidden).GetValue(value);
 static object Call(object owner,string method,params object[] args){try{return owner.GetType().GetMethods(Hidden).Single(m=>m.Name==method&&m.GetParameters().Length==args.Length).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 sealed class Fixture {
  internal readonly OrdinaryFiveCombatFactory Factory;internal readonly BCombatLocalModel Health;
  internal readonly object Sources,Execution;internal readonly string Gunner,Near,Far;
  internal int Launches;internal long TargetGeneration=1,FarDistance=12000;internal bool FarLos=true;
  internal long PeriodUs=2500000;internal int PeriodReads;
  internal string Interception="CLEAR",NoTarget="UNSUPPLIED",Motion="STATIONARY";
  internal bool MissingAttack;internal readonly List<JObject> Results=new List<JObject>();
  internal Fixture():this(0){}
  internal Fixture(long startTick):this(startTick,false){}
  internal Fixture(long startTick,bool delayed){
   var basicType=typeof(OrdinaryNodeProjectileOwnerTests).GetNestedType("Fixture",BindingFlags.NonPublic);var basis=Activator.CreateInstance(basicType,Hidden,null,new object[]{false},null);
   Factory=(OrdinaryFiveCombatFactory)Get(basis,"Factory");Health=(BCombatLocalModel)Get(basis,"Health");Gunner=(string)Get(basis,"Target");
   foreach(var id in ((JObject)JObject.Parse(Health.CaptureLocal())["vitals"]).Properties().Select(x=>x.Name).Where(id=>!id.StartsWith("op_")&&id!=Factory.Original.Train.TrainId&&id!=Gunner))Health.ApplyResolvedDamage("disable-other-gunner",id,10000,0);
   Near=Factory.Original.Actors.OrderBy(a=>a.Slot).First().OperatorId;Far=Factory.Original.Actors.OrderBy(a=>a.Slot).Skip(1).First().OperatorId;
   var sourceType=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryNodeGunnerSources");Assert.NotNull(sourceType,"Missing original v05 gunner sources.");
   var sourceArgs=new List<object>{(Func<string,long,JObject>)Observe,
    (Func<string,CombatExactValue,long,JObject>)((id,raw,tick)=>{Launches++;return MissingAttack?null:Attack(raw);}),
    (Func<string,long,JObject>)((id,tick)=>Defense()),
    (Func<string,long,JObject>)((shot,tick)=>new JObject{["status"]=Interception,["eventId"]="explicit-test-order:"+tick,["shotId"]=shot,["tick"]=tick})};
   if(delayed)sourceArgs.Add((Func<string,long,CombatExactValue>)((id,tick)=>{PeriodReads++;return CombatExactValue.From(new JObject{["numerator"]=PeriodUs.ToString(),["denominator"]="1"});}));
   Sources=Activator.CreateInstance(sourceType,Hidden,null,sourceArgs.ToArray(),null);
   var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryNodeGunnerExecution");Assert.NotNull(type);
   Execution=Activator.CreateInstance(type,Hidden,null,new[]{(object)Factory,Health,Sources,startTick},null);
  }
  internal JObject Observe(string gunner,long tick){
   JObject Target(string id,long distance,int car,bool los)=>new JObject{["id"]=id,["generation"]=TargetGeneration,["carOrder"]=car,["lineOfSight"]=los,["pointMm"]=new JArray(distance,1000,0)};
   return new JObject{["tick"]=tick,["revision"]=tick+1,["nodeId"]=Factory.Attempt.NodeId,["snapshotHash"]=Factory.Attempt.SnapshotHash,["attemptGeneration"]=Factory.Attempt.Generation,
    ["source"]=new JObject{["id"]=gunner,["generation"]=1,["executorId"]=gunner+":weapon",["muzzleMm"]=new JArray(0,1000,0),["motionStatus"]=Motion},
    ["noTargetStatus"]=NoTarget,["targets"]=new JArray(Target(Near,6000,0,true),Target(Far,FarDistance,1,FarLos))};
  }
  internal void Step(long tick){foreach(var row in (IReadOnlyList<JObject>)Call(Execution,"Step",tick))Results.Add(row);}
  internal void Through(long from,long to){for(long t=from;t<=to;t++)Step(t);}
  internal JObject Capture()=>(JObject)Call(Execution,"Capture");
 }
 [Test] public void TauntOverridesNormalTargetUntilReleasedWithoutChangingFlyingShot(){
  var f=new Fixture();
  var field=f.Execution.GetType().GetField("TauntTarget",Hidden);
  field.SetValue(f.Execution,(Func<string,long,string>)((id,generation)=>generation==1?f.Near:null));
  f.Through(0,118);var launched=f.Capture()["shots"].First();Assert.AreEqual(f.Near,(string)launched["targetId"]);
  field.SetValue(f.Execution,null);
  f.Through(119,260);Assert.IsTrue(f.Capture()["shots"].Any(s=>(string)s["targetId"]==f.Far));
  Assert.AreEqual(f.Near,(string)f.Capture()["shots"].First()["targetId"],"Released taunt must not retarget an already launched projectile.");
 }
 [Test] public void PresentationShotsUseOriginalLaunchCoordinatesWithoutChangingExecution(){
  var f=new Fixture();f.Through(0,118);var before=f.Capture();string hp=f.Health.CaptureLocal();
  var rows=(JArray)Call(f.Execution,"ReadPresentationShots",118L);Assert.AreEqual(1,rows.Count);
  var shot=before["shots"][0];Assert.AreEqual((string)shot["projectileId"],(string)rows[0]["projectileId"]);Assert.AreEqual("FLYING",(string)rows[0]["status"]);
  Assert.IsTrue(JToken.DeepEquals(shot["launchObservation"]["source"]["muzzleMm"],rows[0]["muzzleMm"]));
  Assert.IsTrue(JToken.DeepEquals(shot["launchObservation"]["targets"].Single(t=>(string)t["id"]==f.Far)["pointMm"],rows[0]["targetPointMm"]));
  rows[0]["muzzleMm"][0]=999;Assert.IsTrue(JToken.DeepEquals(before,f.Capture()));Assert.AreEqual(hp,f.Health.CaptureLocal());
  f.Through(119,148);before=f.Capture();Assert.AreEqual("HIT",(string)((JArray)Call(f.Execution,"ReadPresentationShots",148L))[0]["status"]);
  Assert.AreEqual(0,((JArray)Call(f.Execution,"ReadPresentationShots",208L)).Count);Assert.IsTrue(JToken.DeepEquals(before,f.Capture()));
 }
 [Test] public void LiveGunnerRemainsVisibleDuringPreparationAndHudReadsDoNotMutateState(){
  var model=OrdinaryLiveExecutionTests.CreateFactory(OrdinaryLiveExecutionTests.CreateInput()).Create();bool found=false;
  for(int step=0;step<3000&&!found;step++){
   model.Advance(170000,1);var hud=(JObject)Call(model,"ReadOrdinaryHud");
   foreach(var actor in hud["actors"].Where(a=>(bool)a["enemy"])){
    Assert.IsFalse((bool)actor["exterior"]);Assert.LessOrEqual((long)actor["availableAt"],model.Tick);
    if((string)actor["basicStatus"]!="WINDUP")continue;
    found=true;Assert.Greater((long)actor["nextLaunch"],model.Tick);Assert.IsTrue((bool)actor["attacking"]);
   }
  }
  Assert.IsTrue(found,"Original live gunner must reach preparation within the observed steps.");
  string before=model.CaptureLocalState();for(int i=0;i<3;i++){var hud=(JObject)Call(model,"ReadOrdinaryHud");Assert.IsInstanceOf<JArray>(hud["ordinaryGunnerProjectiles"]);}
  Assert.AreEqual(before,model.CaptureLocalState());
 }
 static JObject Attack(CombatExactValue raw)=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=raw.Snapshot(),["appliedOffense"]=new JArray(),["penetration"]=new JArray()};
 [Test] public void NyxDelayIsFrozenAtReservationAndNextReservationReadsCurrentPeriod(){
  var f=new Fixture(0,true);f.Step(0);Assert.AreEqual(1,f.PeriodReads);Assert.AreEqual(148,(long)((JObject)Call(f.Execution,"ReadActor",f.Gunner))["nextLaunch"]);
  f.PeriodUs=2000000;f.Through(1,147);Assert.AreEqual(0,f.Launches);Assert.AreEqual(1,f.PeriodReads);var before=f.Capture();
  var clonedHealth=Call(f.Health,"CloneOwnedNode");var clone=Call(f.Execution,"CloneOwned",clonedHealth);Assert.IsTrue(JToken.DeepEquals(before,(JObject)Call(clone,"Capture")));
  f.Step(148);Assert.AreEqual(1,f.Launches);Assert.AreEqual(2,f.PeriodReads);Assert.AreEqual(266,(long)((JObject)Call(f.Execution,"ReadActor",f.Gunner))["nextLaunch"]);
  f.Step(148);Assert.AreEqual(1,f.Launches);Assert.AreEqual(2,f.PeriodReads);f.Through(149,265);Assert.AreEqual(1,f.Launches);f.Step(266);Assert.AreEqual(2,f.Launches);
 }
 [Test] public void DelayedStartKeepsFullOriginalPeriodPreparationAndFlightIntoSameKernel(){
  var f=new Fixture(1200);int hp=f.Health.Hp(f.Far);f.Through(1200,1287);
  Assert.AreEqual("IDLE",(string)((JObject)Call(f.Execution,"ReadActor",f.Gunner))["phase"]);f.Step(1288);
  var prepared=(JObject)Call(f.Execution,"ReadActor",f.Gunner);Assert.AreEqual("WINDUP",(string)prepared["phase"]);Assert.AreEqual(1288,(long)prepared["startedAt"]);Assert.AreEqual(1318,(long)prepared["commit"]);
  f.Through(1289,1317);Assert.AreEqual(0,f.Launches);f.Step(1318);Assert.AreEqual(1,f.Launches);Assert.AreEqual(hp,f.Health.Hp(f.Far));
  f.Through(1319,1347);Assert.AreEqual(hp,f.Health.Hp(f.Far));f.Step(1348);Assert.Less(f.Health.Hp(f.Far),hp);Assert.AreEqual(1,f.Results.Count);
 }
 [TestCase(-1)][TestCase(90)][TestCase(118)] public void DelayedClonePreservesStartBoundaryPreparationAndFlight(int relativeTick){
  var f=new Fixture(1200);if(relativeTick>=0)f.Through(1200,1200+relativeTick);
  var before=f.Capture();var health=(BCombatLocalModel)Call(f.Health,"CloneOwnedNode");var clone=Call(f.Execution,"CloneOwned",health);
  Assert.IsTrue(JToken.DeepEquals(before,(JObject)Call(clone,"Capture")));long next=1200+relativeTick+1;
  Assert.Throws<ArgumentException>(()=>Call(clone,"BeginStep",1199L));Assert.Throws<ArgumentException>(()=>Call(clone,"ResolveStep",1199L));Assert.Throws<ArgumentException>(()=>Call(clone,"BeginStep",next+1));Assert.Throws<ArgumentException>(()=>Call(clone,"ResolveStep",next));
  for(long tick=next;tick<=1348;tick++)Call(clone,"Step",tick);
  Assert.Less(health.Hp(f.Far),f.Health.Hp(f.Far));Assert.IsTrue(JToken.DeepEquals(before,f.Capture()));
 }
 static JObject Defense()=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=new JObject{["numerator"]="0",["denominator"]="1"},["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
 [Test] public void OriginalPeriodWindupAndDistanceFlightHitFarthestOperatorOnlyOnce(){
  var f=new Fixture();int near=f.Health.Hp(f.Near),far=f.Health.Hp(f.Far);f.Through(0,117);Assert.AreEqual(0,f.Launches);
  f.Step(118);Assert.AreEqual(1,f.Launches);Assert.AreEqual(far,f.Health.Hp(f.Far));f.Through(119,147);Assert.AreEqual(far,f.Health.Hp(f.Far));
  f.Step(148);Assert.Less(f.Health.Hp(f.Far),far);Assert.AreEqual(near,f.Health.Hp(f.Near));Assert.AreEqual(1,f.Results.Count);
  f.Step(148);Assert.AreEqual(1,f.Results.Count);f.Through(149,235);Assert.AreEqual(1,f.Launches);f.Step(236);Assert.AreEqual(2,f.Launches);
  Assert.AreEqual(f.Gunner,(string)f.Results[0]["actorId"]);Assert.AreEqual(f.Gunner+":weapon",(string)f.Results[0]["executorId"]);Assert.AreEqual(f.Far,(string)f.Results[0]["targetId"]);
 }
 [TestCase("range")][TestCase("los")][TestCase("tie")]
 public void OriginalRangeLosAndCarOrderSelectTheEligibleOperator(string reason){var f=new Fixture();if(reason=="range")f.FarDistance=24001;if(reason=="los")f.FarLos=false;if(reason=="tie")f.FarDistance=6000;f.Through(0,135);Assert.AreEqual(1,f.Results.Count);Assert.AreEqual(f.Near,(string)f.Results[0]["targetId"]);}
 [TestCase("source-death")][TestCase("distance-exit")][TestCase("generation")]
 public void FrozenLaunchSurvivesSourceDeathAndDistanceExitButNotTargetGeneration(string change){var f=new Fixture();f.Through(0,118);int hp=f.Health.Hp(f.Far);if(change=="source-death")f.Health.ApplyResolvedDamage("gunner-down",f.Gunner,10000,118);if(change=="distance-exit")f.FarDistance=100000;if(change=="generation")f.TargetGeneration=2;f.Through(119,148);if(change=="generation"){Assert.AreEqual(hp,f.Health.Hp(f.Far));Assert.AreEqual(0,f.Results.Count);}else{Assert.Less(f.Health.Hp(f.Far),hp);Assert.AreEqual(1,f.Results.Count);}}
 [Test] public void PreparationTargetLossCancelsWithoutFreeRetarget(){var f=new Fixture();f.Through(0,90);f.FarLos=false;f.Through(91,148);Assert.AreEqual(0,f.Launches);Assert.AreEqual(0,f.Results.Count);}
 [Test] public void InterceptionAtImpactPreventsHpAndMissingPriorityCannotGuess(){var f=new Fixture();f.Through(0,147);int hp=f.Health.Hp(f.Far);f.Interception="UNRESOLVED";Assert.Throws<NotSupportedException>(()=>f.Step(148));Assert.AreEqual(hp,f.Health.Hp(f.Far));f.Interception="INTERCEPTED";f.Step(148);Assert.AreEqual(hp,f.Health.Hp(f.Far));Assert.AreEqual(0,f.Results.Count);}
 [Test] public void MissingMovementPathAndMissingAttackSnapshotFailClosed(){var f=new Fixture();f.Health.ApplyResolvedDamage("near-down",f.Near,10000,0);f.Health.ApplyResolvedDamage("far-down",f.Far,10000,0);Assert.Throws<NotSupportedException>(()=>f.Step(0));var other=new Fixture();other.MissingAttack=true;other.Through(0,117);Assert.Throws<ArgumentException>(()=>other.Step(118));}
 [Test] public void HostCanCommitLaunchBeforeSameTickAllyKillAndResolveOnlyAfter(){var f=new Fixture();f.Through(0,117);Call(f.Execution,"BeginStep",118L);Assert.AreEqual(1,f.Launches);f.Health.ApplyResolvedDamage("same-tick-kill",f.Gunner,10000,118);Call(f.Execution,"ResolveStep",118L);int hp=f.Health.Hp(f.Far);f.Through(119,148);Assert.Less(f.Health.Hp(f.Far),hp);}
 [TestCase(false)][TestCase(true)] public void CloneRetainsPreparationOrFlyingShotWithIndependentHp(bool flying){var f=new Fixture();long at=flying?118:90;f.Through(0,at);var before=f.Capture();var health=(BCombatLocalModel)Call(f.Health,"CloneOwnedNode");var clone=Call(f.Execution,"CloneOwned",health);Assert.IsTrue(JToken.DeepEquals(before,(JObject)Call(clone,"Capture")));for(long tick=at+1;tick<=148;tick++)Call(clone,"Step",tick);Assert.Less(health.Hp(f.Far),f.Health.Hp(f.Far));Assert.IsTrue(JToken.DeepEquals(before,f.Capture()));}
}
