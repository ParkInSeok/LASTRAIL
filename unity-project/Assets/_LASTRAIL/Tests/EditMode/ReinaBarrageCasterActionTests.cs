using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class ReinaBarrageCasterActionTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Field(object owner,string name)=>owner.GetType().GetField(name,Hidden).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Hidden).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args){try{return owner.GetType().GetMethods(Hidden).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);}catch(TargetInvocationException error){throw error.InnerException;}}
 // Existing synthetic node and 17ms geometry fixture; supplied charge is test eligibility, not a production recharge rule.
 sealed class Fixture {
  internal readonly object Basis,Execution;internal readonly BCombatLocalModel Health;internal readonly string Primary,Extra;
  internal Fixture(){Basis=Activator.CreateInstance(typeof(OrdinaryReinaBarrageExecutionTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,new object[]{170000L},null);Execution=Field(Basis,"Execution");Health=(BCombatLocalModel)Field(Basis,"Health");Primary=(string)Field(Basis,"Primary");Extra=(string)Field(Basis,"Extra");}
  internal string Request(string id,long tick,int syntheticAvailableCharge=100)=>(string)Call(Execution,"Request",id,tick,syntheticAvailableCharge);
  internal void Through(long from,long to)=>Call(Basis,"Through",from,to);
  internal JObject Actor=>(JObject)Call(Execution,"ReadActor","op_reina");
  internal IEnumerable<JObject> Damage=>((List<JObject>)Field(Basis,"Results")).Where(r=>(string)r["kind"]=="DAMAGE");
  internal IEnumerable<JObject> Commits=>((List<JObject>)Field(Basis,"Results")).Where(r=>(string)r["kind"]=="COMMIT");
 }
 [Test] public void OriginalRecoveryBoundaryReleasesActorWhileItsBarrageRemainsPending(){
  var f=new Fixture();Assert.AreEqual("ACCEPTED",f.Request("first",0));f.Through(0,65);
  Assert.AreEqual("RECOVERY",(string)f.Actor["phase"]);Assert.AreEqual("BUSY",f.Request("second",65));
  f.Through(66,66);Assert.AreEqual("IDLE",(string)f.Actor["phase"]);Assert.IsTrue((bool)f.Execution.GetType().GetProperty("HasPending",Hidden).GetValue(f.Execution));
  Assert.AreEqual("INSUFFICIENT_CHARGE",f.Request("second",66,99));Assert.AreEqual("ACCEPTED",f.Request("second",66,100));Assert.AreEqual("WINDUP",(string)f.Actor["phase"]);
 }
 [Test] public void TwoExplicitlyEligibleSyntheticCastsKeepSeparateFourPulseSchedulesAndAttackSnapshots(){
  var f=new Fixture();Set(f.Basis,"AttackValue",2L);int primaryHp=f.Health.Hp(f.Primary),extraHp=f.Health.Hp(f.Extra);
  f.Request("first",0);f.Through(0,66);Set(f.Basis,"AttackValue",4L);Set(f.Basis,"Selected",f.Extra);
  Assert.AreEqual("ACCEPTED",f.Request("second",66,100));f.Through(67,114);Set(f.Basis,"AttackValue",999L);
  Set(f.Basis,"Mutate",(Action<JObject>)(o=>o["source"]["muzzleMm"]=new JArray(50000,500,0)));
  f.Through(115,338);var first=f.Damage.Where(r=>(string)r["commandId"]=="first").ToArray();var second=f.Damage.Where(r=>(string)r["commandId"]=="second").ToArray();
  Assert.AreEqual(8,first.Length);Assert.AreEqual(8,second.Length);Assert.AreEqual(16,f.Damage.Select(r=>(string)r["eventId"]).Distinct().Count());Assert.AreEqual(2,f.Commits.Count());
  CollectionAssert.AreEqual(new long[]{131,178,225,272},first.Where(r=>(string)r["targetId"]==f.Primary).Select(r=>(long)r["tick"]));
  CollectionAssert.AreEqual(new long[]{197,244,291,338},second.Where(r=>(string)r["targetId"]==f.Extra).Select(r=>(long)r["tick"]));
  Assert.IsTrue(first.Where(r=>(string)r["targetId"]==f.Primary).All(r=>(int)r["receipt"]["hpLoss"]==5));Assert.IsTrue(second.Where(r=>(string)r["targetId"]==f.Extra).All(r=>(int)r["receipt"]["hpLoss"]==10));
  Assert.AreEqual(primaryHp-32,f.Health.Hp(f.Primary));Assert.AreEqual(extraHp-44,f.Health.Hp(f.Extra));Assert.AreEqual(2,(int)Field(f.Basis,"Captures"));
 }
 [Test] public void SyntheticReturnFromDownDoesNotReplayAlreadyCommittedPulses(){
  var f=new Fixture();f.Request("first",0);f.Through(0,48);f.Health.ApplyResolvedDamage("test-source-down","op_reina",10000,48);f.Through(49,131);
  Assert.AreEqual("DOWN",(string)f.Actor["phase"]);Assert.AreEqual("DOWN",f.Request("new-while-down",131));Assert.AreEqual(2,f.Damage.Count());
  // Exercise the existing HP owner's scheduled-recovery primitive; no revival admission/policy is asserted by this fixture.
  var vitals=Call(f.Health,"BorrowVitals","op_reina");Call(vitals,"RecoverFromDown",70);
  f.Through(132,272);Assert.AreEqual("IDLE",(string)f.Actor["phase"]);Assert.AreEqual(8,f.Damage.Count());Assert.AreEqual(8,f.Damage.Select(r=>(string)r["eventId"]).Distinct().Count());Assert.AreEqual(1,f.Commits.Count());Assert.AreEqual(1,(int)Field(f.Basis,"Captures"));
 }
}
