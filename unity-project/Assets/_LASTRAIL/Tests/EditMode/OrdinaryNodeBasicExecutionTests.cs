using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryNodeBasicExecutionTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Hidden).GetValue(owner);
 static object Call(object owner,string method,params object[] args){try{return owner.GetType().GetMethods(Hidden).Single(m=>m.Name==method&&m.GetParameters().Length==args.Length).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 sealed class Fixture {
  internal readonly object Base,Projectiles,Execution,Sources;
  internal readonly OrdinaryFiveCombatFactory Factory;
  internal readonly BCombatLocalModel Health;
  internal readonly string Target,Second,Actor;
  internal int Attacks,Observations;internal long EnabledAt;internal long LoseFirstAt=long.MaxValue;
  internal string Mode="RANGED";
  internal bool SecondIsMelee;
  internal bool MissingAttack;
  internal bool Contact=true;
  internal readonly long StartTick;
  internal Fixture(string actor="op_reina"):this(actor,0){}
  internal Fixture(string actor,long startTick,bool begin=true,bool contactSource=false){
   StartTick=startTick;
   Actor=actor;var type=typeof(OrdinaryNodeProjectileOwnerTests).GetNestedType("Fixture",BindingFlags.NonPublic);
   Base=Activator.CreateInstance(type,Hidden,null,new object[]{false},null);Factory=(OrdinaryFiveCombatFactory)Get(Base,"Factory");Health=(BCombatLocalModel)Get(Base,"Health");Projectiles=Get(Base,"Owner");Target=(string)Get(Base,"Target");
   Second=((JObject)JObject.Parse(Health.CaptureLocal())["vitals"]).Properties().Select(p=>p.Name).Single(id=>id!=Target&&!id.StartsWith("op_")&&id!=Factory.Original.Train.TrainId);
   foreach(var a in Factory.Original.Actors.Where(a=>a.OperatorId!=actor))Health.ApplyResolvedDamage("disable-"+a.OperatorId,a.OperatorId,10000,0);
   var sourcesType=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryNodeBasicSources");Assert.NotNull(sourcesType,"Missing trusted node BASIC source boundary.");
   var args=new object[]{
    (Func<string,long,JObject>)Observe,
    (Func<string,long,CombatExactValue>)((id,tick)=>CombatExactValue.From(new JObject{["numerator"]=(Factory.Original.Basic.Profiles.Single(p=>p.OperatorId==id).OriginalIntervalMs*1000).ToString(),["denominator"]="1"})),
    (Func<string,string,long,JObject>)((id,target,tick)=>{Attacks++;return MissingAttack?null:Attack();}),
    (Func<string,long,JObject>)((id,tick)=>Defense()),
    (Func<string,long,string>)((target,tick)=>SecondIsMelee&&target==Second?"MELEE":Mode)};
   if(contactSource)args=args.Concat(new object[]{(Func<string,long,bool>)((target,tick)=>Contact)}).ToArray();
   Sources=Activator.CreateInstance(sourcesType,Hidden,null,args,null);
   var executionType=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryNodeBasicExecution");Assert.NotNull(executionType);
   Execution=Activator.CreateInstance(executionType,Hidden,null,new[]{(object)Factory,Health,Projectiles,Sources,startTick},null);
   if(begin)Call(Execution,"Step",startTick);
  }
  internal JObject Observe(string actor,long tick){
   Observations++;
   var o=(JObject)Call(Base,"Observation",tick,actor);var first=(JObject)o["targets"][0];first["eligible"]=tick>=EnabledAt&&tick<LoseFirstAt;
   var second=(JObject)first.DeepClone();second["id"]=Second;second["eligible"]=tick>=EnabledAt;second["anchorMm"]=new JArray(1000,500,0);second["feetMm"]=new JArray(1000,0,0);
   ((JArray)o["targets"]).Add(second);return o;
  }
  internal long Due=>StartTick+Factory.Original.Actors.Single(a=>a.OperatorId==Actor).Slot+1;
  internal long Commit=>Due+(Factory.Original.Basic.Profiles.Single(p=>p.OperatorId==Actor).WindupMs.Value+16)/17;
  internal void Step(long tick)=>Call(Execution,"Step",tick);
  internal void Through(long from,long to){for(long t=from;t<=to;t++)Step(t);}
  internal JObject Capture()=>(JObject)Call(Execution,"Capture");
 }
 static JObject Attack()=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=new JObject{["numerator"]="100",["denominator"]="1"},["appliedOffense"]=new JArray(),["penetration"]=new JArray()};
 [TestCase("op_reina")][TestCase("op_sera")][TestCase("op_mira")][TestCase("op_kyle")][TestCase("op_eve")]
 public void DelayedStartOffsetsOriginalPreparationAndLaunchIntoSameKernel(string actor){
  var f=new Fixture(actor,1200);int hp=f.Health.Hp(f.Target);
  f.Through(1201,f.Due-1);Assert.AreEqual("READY",(string)((JObject)Call(f.Execution,"ReadActor",actor))["phase"]);
  f.Step(f.Due);var prepared=(JObject)Call(f.Execution,"ReadActor",actor);Assert.AreEqual("PREPARING",(string)prepared["phase"]);Assert.AreEqual(f.Due,(long)prepared["startedAt"]);Assert.AreEqual(f.Commit,(long)prepared["commit"]);
  f.Through(f.Due+1,f.Commit-1);Assert.AreEqual(0,f.Attacks);f.Step(f.Commit);Assert.AreEqual(1,f.Attacks);Assert.AreEqual(hp,f.Health.Hp(f.Target));
  f.Through(f.Commit+1,f.Commit+16);Assert.Less(f.Health.Hp(f.Target),hp);
 }
 [TestCase(false)][TestCase(true)] public void DelayedClonePreservesStartBoundaryAndPreparedSchedule(bool prepared){
  var f=new Fixture("op_reina",1200,begin:false);if(prepared)f.Through(1200,f.Due);
  var before=f.Capture();var health=(BCombatLocalModel)Call(f.Health,"CloneOwnedNode");var projectiles=Call(f.Projectiles,"CloneOwned",health);var clone=Call(f.Execution,"CloneOwned",health,projectiles);
  Assert.IsTrue(JToken.DeepEquals(before,(JObject)Call(clone,"Capture")));
  long next=prepared?f.Due+1:1200;Assert.Throws<ArgumentException>(()=>Call(clone,"Step",1199L));Assert.Throws<ArgumentException>(()=>Call(clone,"Step",next+1));
  for(long t=next;t<=f.Commit+16;t++)Call(clone,"Step",t);
  Assert.Less(health.Hp(f.Target),f.Health.Hp(f.Target));Assert.IsTrue(JToken.DeepEquals(before,f.Capture()));
 }
 static JObject Defense()=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=new JObject{["numerator"]="0",["denominator"]="1"},["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
 [TestCase("op_reina")][TestCase("op_sera")][TestCase("op_mira")][TestCase("op_kyle")][TestCase("op_eve")]
 public void OriginalDueAndWindupLaunchOnePhysicalAttackIntoSameEnemyKernel(string actor){
  var f=new Fixture(actor);int hp=f.Health.Hp(f.Target),other=f.Health.Hp(f.Second);
  f.Through(1,f.Commit-1);Assert.AreEqual(0,f.Attacks);Assert.AreEqual(hp,f.Health.Hp(f.Target));
  f.Step(f.Commit);Assert.AreEqual(1,f.Attacks);Assert.AreEqual(hp,f.Health.Hp(f.Target),"Commit launches; it must not apply remote damage.");
  f.Through(f.Commit+1,f.Commit+16);Assert.Less(f.Health.Hp(f.Target),hp);Assert.AreEqual(other,f.Health.Hp(f.Second),"Original order wins over nearer second enemy.");Assert.AreEqual(1,f.Attacks);
 }
 [Test] public void NoTargetWaitDoesNotAccumulateMissedOpportunities(){var f=new Fixture();f.EnabledAt=100;f.Through(1,99);Assert.AreEqual(0,f.Attacks);f.Through(100,135);Assert.AreEqual(1,f.Attacks);}
 [Test] public void CommitReacquiresOnceInOriginalOrderWhenPreparedTargetBecomesInvalid(){var f=new Fixture();f.LoseFirstAt=f.Commit;int hp=f.Health.Hp(f.Second),first=f.Health.Hp(f.Target);f.Through(1,f.Commit+10);Assert.AreEqual(1,f.Attacks);Assert.Less(f.Health.Hp(f.Second),hp);Assert.AreEqual(first,f.Health.Hp(f.Target));}
 [TestCase(false)][TestCase(true)] public void SourceDeathCancelsPreparationButPreservesAlreadyLaunchedShot(bool fired){
  var f=new Fixture();long stop=fired?f.Commit:f.Due;f.Through(1,stop);int hp=f.Health.Hp(f.Target);f.Health.ApplyResolvedDamage("source-death",f.Actor,10000,stop);f.Through(stop+1,f.Commit+16);
  Assert.AreEqual(fired?1:0,f.Attacks);if(fired)Assert.Less(f.Health.Hp(f.Target),hp);else Assert.AreEqual(hp,f.Health.Hp(f.Target));
 }
 [TestCase(true)][TestCase(false)] public void MeleeContactResolvesOnceAtCommitWithoutCreatingFlight(bool touching){
  var f=new Fixture("op_kyle",0,true,true);f.Mode="MELEE";f.Contact=touching;int hp=f.Health.Hp(f.Target);
  f.Through(1,f.Commit-1);Assert.AreEqual(hp,f.Health.Hp(f.Target));
  f.Step(f.Commit);Assert.AreEqual(touching?hp-100:hp,f.Health.Hp(f.Target));Assert.AreEqual(touching?1:0,f.Attacks);
  Assert.AreEqual(0,((JArray)f.Capture()["flights"]).Count);Assert.AreEqual("RECOVERY",(string)((JObject)Call(f.Execution,"ReadActor","op_kyle"))["phase"]);
  f.Step(f.Commit);Assert.AreEqual(touching?hp-100:hp,f.Health.Hp(f.Target));
 }
 [Test] public void MeleeWindupNeverSwitchesToProjectileWhenContactIsLost(){
  var f=new Fixture("op_kyle",0,true,true);f.Mode="MELEE";int hp=f.Health.Hp(f.Target);f.Through(1,f.Due);
  f.Mode="RANGED";f.Contact=false;f.Through(f.Due+1,f.Commit);Assert.AreEqual(hp,f.Health.Hp(f.Target));Assert.AreEqual(0,f.Attacks);Assert.AreEqual(0,((JArray)f.Capture()["flights"]).Count);
 }
 [Test] public void KyleMeleeNeverBecomesAnUnapprovedRangedSubstitute(){var f=new Fixture("op_kyle");f.Mode="MELEE";f.Through(1,f.Due-1);Assert.Throws<NotSupportedException>(()=>f.Step(f.Due));Assert.AreEqual(0,f.Attacks);}
 [Test] public void KyleCannotHideAnEligibleMeleeCandidateBehindFirstRangedCandidate(){var f=new Fixture("op_kyle");f.SecondIsMelee=true;f.Through(1,f.Due-1);Assert.Throws<NotSupportedException>(()=>f.Step(f.Due));Assert.AreEqual(0,f.Attacks);}
 [Test] public void MissingAttackEffectsCannotBecomeAnImplicitNoEffectsHit(){var f=new Fixture();f.MissingAttack=true;f.Through(1,f.Commit-1);int hp=f.Health.Hp(f.Target);Assert.Throws<ArgumentException>(()=>f.Step(f.Commit));Assert.AreEqual(hp,f.Health.Hp(f.Target));}
 [Test] public void ClonePreservesPreparedScheduleWithIndependentKernelAndProjectiles(){
  var f=new Fixture();f.Through(1,f.Due);var before=f.Capture();var health=(BCombatLocalModel)Call(f.Health,"CloneOwnedNode");var projectiles=Call(f.Projectiles,"CloneOwned",health);var clone=Call(f.Execution,"CloneOwned",health,projectiles);
  Assert.IsTrue(JToken.DeepEquals(before,(JObject)Call(clone,"Capture")));for(long t=f.Due+1;t<=f.Commit+16;t++)Call(clone,"Step",t);
  Assert.IsTrue(JToken.DeepEquals(before,f.Capture()));Assert.Less(health.Hp(f.Target),f.Health.Hp(f.Target));
 }
 [Test] public void DuplicateClockDoesNotReadSourcesOrRepeatOpportunityAndClockGapsReject(){
  var f=new Fixture();f.Through(1,f.Commit);var before=f.Capture();int reads=f.Observations,attacks=f.Attacks;
  f.Step(f.Commit);Assert.AreEqual(reads,f.Observations);Assert.AreEqual(attacks,f.Attacks);Assert.IsTrue(JToken.DeepEquals(before,f.Capture()));
  Assert.Throws<ArgumentException>(()=>f.Step(f.Commit-1));Assert.Throws<ArgumentException>(()=>f.Step(f.Commit+2));
 }
}
