#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class NyxZoneExecutionTests {
  static JObject V(string n)=>new JObject{["numerator"]=n,["denominator"]="1"};
  static JObject Attack(string id,long tick)=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=V("100"),["appliedOffense"]=new JArray(),["penetration"]=new JArray()};
  static JObject Defense(string id,long tick)=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=V("0"),["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
  static readonly string[] Ids={"op_nyx","op_kyle","op_reina","op_eve","op_sera"};
  static BCombatFormation Formation()=>new BCombatFormation("train",BCombatMode.Solo,BCombatBattlefield.ExternalVertical,Ids.Select((id,i)=>new BCombatSlot("car"+i,id)));
  static BCombatHpBinding[] Bindings()=>new[]{new BCombatHpBinding("train",10000)}.Concat(Ids.Select(id=>new BCombatHpBinding(id,10000))).Concat(Ids.Select((id,i)=>new BCombatHpBinding("car"+i,10000))).ToArray();
  static BCombatLocalModel Health()=>BCombatLocalModel.CreateWithCarriageTransfer(Formation(),Bindings(),1000,BCombatAuthority.UnitySolo);
  static BCombatLocalModel RestoreHealth(BCombatLocalModel h)=>BCombatLocalModel.RestoreLocal(h.CaptureLocal(),Formation(),Bindings(),1000,BCombatAuthority.UnitySolo);
  static object Invoke(object owner,string name,params object[] args){var method=typeof(NyxZoneExecution).GetMethod(name,BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static);Assert.NotNull(method,name);try{return method.Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  static NyxZoneMember M(string id,long gen=1,bool immune=false,bool protection=false)=>new NyxZoneMember(id,gen,immune,protection);
  static NyxZoneExecution Owner(BCombatLocalModel h)=>new NyxZoneExecution("test-node-generation-1",h,1000,Attack,Defense);
  [Test] public void OriginalSeventeenMillisecondStepRoundsAbsoluteDueTimeWithoutDrift(){
   var h=Health();using(var n=new NyxZoneExecution("step17",h,17,Attack,Defense)){n.CommitCreatedZone("z",10,false);long[] due={69,128,187,246,305,363};foreach(long tick in due){Assert.IsEmpty(n.Step(tick-1,new[]{M("op_kyle")}));Assert.AreEqual(1,n.Step(tick,new[]{M("op_kyle")}).Count);}Assert.IsFalse(n.Active);}
  }
  [Test] public void ZoneAttackFreezesAtFirstActualEffectAndReplacementCapturesAgain(){
   var h=Health();int reads=0;using(var n=new NyxZoneExecution("snapshot",h,1000,(id,t)=>{reads++;var value=Attack(id,t);value["completedAttack"]=V(reads==1?"100":"200");return value;},Defense)){
    n.CommitCreatedZone("a",0,false);n.Step(1,Array.Empty<NyxZoneMember>());Assert.AreEqual(0,reads);Assert.AreEqual(60,n.Step(2,new[]{M("op_kyle")}).Single().HpLoss);Assert.AreEqual(60,n.Step(3,new[]{M("op_kyle")}).Single().HpLoss);Assert.AreEqual(1,reads);
    n.CommitCreatedZone("b",3,false);Assert.AreEqual(120,n.Step(4,new[]{M("op_kyle")}).Single().HpLoss);Assert.AreEqual(2,reads);
   }
  }
  [Test] public void CurrentEffectsKeepExplicitKindsAndSeparateImmunities(){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("z",0,false);n.Step(0,new[]{new NyxZoneMember("op_kyle",2,true),new NyxZoneMember("op_sera",3,false,false,true)});
    var rows=(JArray)Invoke(n,"CaptureCurrentEffects",0L);Assert.AreEqual(2,rows.Count);
    Assert.AreEqual("MITIGATION_RATE_REDUCTION",(string)rows[0]["kind"]);Assert.AreEqual(800,(int)rows[0]["valueBps"]);Assert.AreEqual(2,(long)rows[0]["targetGeneration"]);
    Assert.AreEqual("NEW_BASIC_PERIOD_INCREASE",(string)rows[1]["kind"]);Assert.AreEqual(1500,(int)rows[1]["valueBps"]);Assert.IsTrue(rows.All(r=>(string)r["sourceKind"]=="NYX_ZONE"));
    Assert.Throws<InvalidOperationException>(()=>Invoke(n,"CaptureCurrentEffects",1L));n.Step(0,Array.Empty<NyxZoneMember>());Assert.IsEmpty((JArray)Invoke(n,"CaptureCurrentEffects",0L));
   }
  }
  [Test] public void FirstTickWaitsAndOneBudgetIsSplitAfterFocusThenStableSelection(){
   var h=Health();using(var n=Owner(h)){
    n.CommitCreatedZone("z",0,false);var members=new[]{M("op_sera"),M("op_reina"),M("op_kyle")};
    Assert.IsEmpty(n.Step(0,members,"op_sera"));var hit=n.Step(1,members,"op_sera");
    CollectionAssert.AreEqual(new[]{"op_sera","op_kyle"},hit.Select(x=>x.TargetId));CollectionAssert.AreEqual(new[]{30,30},hit.Select(x=>x.HpLoss));Assert.AreEqual(10000,h.Hp("op_reina"));
    Assert.IsEmpty(n.Step(1,members,"op_sera"));Assert.AreEqual(9970,h.Hp("op_sera"));
   }
  }
  [TestCase(false,6,60)][TestCase(true,8,120)] public void LastTickRunsBeforeExpiry(bool ultimate,int ticks,int firstLoss){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("z",0,ultimate);var m=new[]{M("op_kyle")};
    for(int i=1;i<=ticks;i++){var hit=n.Step(i,m);Assert.AreEqual(i<3?firstLoss:firstLoss*11/10,hit.Single().HpLoss);}
    Assert.IsFalse(n.Active);Assert.IsEmpty(n.Step(ticks+1,m));Assert.AreEqual(0,n.BasicPeriodIncreaseBps("op_kyle",1));
   }
  }
  [Test] public void UltimateCapsAtThreeAndDoesNotRedistributeUnusedDamage(){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("z",0,true);var hit=n.Step(1,new[]{M("op_kyle"),M("op_reina"),M("op_eve"),M("op_sera")});Assert.AreEqual(3,hit.Count);Assert.IsTrue(hit.All(x=>x.HpLoss==40));Assert.AreEqual(10000,h.Hp("op_sera"));}
  }
  [Test] public void EmptyTickIsDiscardedAndReplacementResetsLifetimeAndPassive(){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("a",0,false);n.Step(1,Array.Empty<NyxZoneMember>());Assert.AreEqual(60,n.Step(2,new[]{M("op_kyle")}).Single().HpLoss);
    n.CommitCreatedZone("b",2,true);Assert.IsEmpty(n.Step(2,new[]{M("op_kyle")}));Assert.AreEqual(120,n.Step(3,new[]{M("op_kyle")}).Single().HpLoss);
   }
  }
  [Test] public void ProtectionLeavingAndNewGenerationResetActualHpLossStreak(){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("z",0,true);var m=new[]{M("op_kyle")};n.Step(1,m);n.Step(2,m);
    Assert.AreEqual(0,n.Step(3,new[]{M("op_kyle",protection:true)}).Single().HpLoss);Assert.AreEqual(120,n.Step(4,m).Single().HpLoss);
    n.Step(5,Array.Empty<NyxZoneMember>());Assert.AreEqual(120,n.Step(6,m).Single().HpLoss);Assert.AreEqual(120,n.Step(7,new[]{M("op_kyle",2)}).Single().HpLoss);
   }
  }
  [TestCase("down")][TestCase("node")][TestCase("removed")] public void HigherPriorityEndCancelsFinalTick(string reason){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("z",0,false);var m=new[]{M("op_kyle")};for(int i=1;i<6;i++)n.Step(i,m);
    if(reason=="down")h.ApplyResolvedDamage("kill","op_nyx",10000,6);int before=h.Hp("op_kyle");Assert.IsEmpty(n.Step(6,m,nodeRunning:reason!="node",zonePresent:reason!="removed"));Assert.AreEqual(before,h.Hp("op_kyle"));Assert.IsFalse(n.Active);
   }
  }
  [Test] public void DelayUsesAllCurrentMembersIncludingUnselectedAndHonorsImmunity(){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("a",0,false);n.Step(0,new[]{M("op_kyle",immune:true),M("op_reina"),M("op_sera")});Assert.AreEqual(0,n.BasicPeriodIncreaseBps("op_kyle",1));Assert.AreEqual(1500,n.BasicPeriodIncreaseBps("op_sera",1));
    n.CommitCreatedZone("b",0,true);n.Step(0,new[]{M("op_sera")});Assert.AreEqual(2500,n.BasicPeriodIncreaseBps("op_sera",1));Assert.AreEqual(0,n.BasicPeriodIncreaseBps("op_sera",2));}
  }
  [Test] public void InvalidObservationOrSkippedTickCannotSpendDamage(){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("z",0,false);Assert.Throws<ArgumentException>(()=>n.Step(1,new[]{M("op_kyle"),M("op_kyle")}));Assert.AreEqual(10000,h.Hp("op_kyle"));Assert.Throws<InvalidOperationException>(()=>n.Step(2,new[]{M("op_kyle")}));}
  }
  [Test] public void FailedCreationDoesNotReplaceAndDisposedOwnerCannotRun(){
   var h=Health();var n=Owner(h);n.CommitCreatedZone("z",0,false);Assert.Throws<ArgumentException>(()=>n.CommitCreatedZone("",0,true));Assert.IsTrue(n.Active);n.Dispose();Assert.IsFalse(n.Active);Assert.Throws<ObjectDisposedException>(()=>n.Step(1,new[]{M("op_kyle")}));
  }
  [Test] public void MitigationIsSharedBeyondDamageCapAndImmunityIsSeparateFromDelay(){
   var h=Health();using(var n=Owner(h)){
    n.CommitCreatedZone("z",0,false);n.Step(0,new[]{M("op_kyle",immune:true),M("op_reina"),M("op_sera"),new NyxZoneMember("op_eve",1,mitigationRateImmune:true)});
    Assert.AreEqual(800,(int)n.MitigationRateReductionSources("op_kyle",1).Single()["valueBps"]);
    Assert.AreEqual(0,n.BasicPeriodIncreaseBps("op_kyle",1));Assert.IsEmpty(n.MitigationRateReductionSources("op_eve",1));Assert.AreEqual(1500,n.BasicPeriodIncreaseBps("op_eve",1));
    var a=Attack("other-ally",0);a["completedAttack"]=V("1000");var d=Defense("op_sera",0);d["defense"]=V("250");
    Assert.AreEqual(880,CombatProjectileAttackSnapshot.Capture(a).EvaluateWithMitigationRateReduction(d,n.MitigationRateReductionSources("op_sera",1)).ScaleAndFloor(10000));
    n.Step(1,new[]{M("op_kyle")});Assert.IsEmpty(n.MitigationRateReductionSources("op_sera",1));
   }
  }
  [Test] public void ShieldOnlyLossDoesNotCountForPassiveAndNoIntermediateRounding(){
   var h=Health();using(var n=Owner(h)){
    h.ApplyResolvedShield("shield","op_kyle",new CombatShieldSource("op_sera","test-shield",0),1000,3,0);
    n.CommitCreatedZone("z",0,false);var members=new[]{M("op_kyle")};Assert.AreEqual(0,n.Step(1,members).Single().HpLoss);Assert.AreEqual(0,n.Step(2,members).Single().HpLoss);Assert.AreEqual(60,n.Step(3,members).Single().HpLoss);
   }
   h=Health();using(var n=new NyxZoneExecution("fractional",h,1000,(id,t)=>{var a=Attack(id,t);a["completedAttack"]=V("47");return a;},Defense)){
    n.CommitCreatedZone("z",0,false);var hit=n.Step(1,new[]{M("op_kyle"),M("op_reina")});Assert.IsTrue(hit.All(x=>x.HpLoss==14));
   }
  }
  [Test] public void InvalidDefenseFailsBeforeAnyHpMutationAndRetiresFailedOwner(){
   var h=Health();using(var n=new NyxZoneExecution("failure",h,1000,Attack,(id,t)=>id=="op_reina"?null:Defense(id,t))){
    n.CommitCreatedZone("z",0,false);Assert.Throws<InvalidOperationException>(()=>n.Step(1,new[]{M("op_kyle"),M("op_reina")}));Assert.AreEqual(10000,h.Hp("op_kyle"));Assert.IsFalse(n.Active);Assert.Throws<InvalidOperationException>(()=>n.Step(1,new[]{M("op_kyle")}));
   }
  }
  [Test] public void SameTickExitClearsSharedEffectsWithoutRepeatingDamage(){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("z",0,false);var m=new[]{M("op_kyle")};n.Step(1,m);Assert.AreEqual(1,n.MitigationRateReductionSources("op_kyle",1).Count);
    Assert.IsEmpty(n.Step(1,Array.Empty<NyxZoneMember>()));Assert.IsEmpty(n.MitigationRateReductionSources("op_kyle",1));Assert.AreEqual(0,n.BasicPeriodIncreaseBps("op_kyle",1));Assert.AreEqual(9940,h.Hp("op_kyle"));}
  }
  [Test] public void CommandClonePreservesPassiveAndCannotRepeatCommittedTick(){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("z",0,false);var m=new[]{M("op_kyle")};n.Step(1,m);n.Step(2,m);var cache=(JObject)Invoke(n,"Capture");var restoredHealth=RestoreHealth(h);
    using(var restored=n.CloneOwned(restoredHealth,Attack,Defense)){
     Assert.IsEmpty(restored.Step(2,m));Assert.AreEqual(9880,restoredHealth.Hp("op_kyle"));Assert.AreEqual(66,restored.Step(3,m).Single().HpLoss);Assert.AreEqual(9880,h.Hp("op_kyle"));
     Assert.Throws<InvalidOperationException>(()=>restored.CommitCreatedZone("z",3,true));
     n.Step(3,m);Assert.IsTrue(JToken.DeepEquals((JObject)Invoke(n,"Capture"),(JObject)Invoke(restored,"Capture")));
    }
   }
  }
  [Test] public void CommandCloneRejectsSameOrDifferentHealthOwner(){
   var h=Health();using(var n=Owner(h)){n.CommitCreatedZone("z",0,false);n.Step(1,new[]{M("op_kyle")});
    Assert.Throws<ArgumentException>(()=>n.CloneOwned(h,Attack,Defense));Assert.Throws<ArgumentException>(()=>n.CloneOwned(Health(),Attack,Defense));
   }
  }
  [Test] public void NodeReentryStartsFreshAndExposesNoResumeApi(){
   var h=Health();var old=Owner(h);old.CommitCreatedZone("z",0,false);old.Step(1,new[]{M("op_kyle")});old.Dispose();
   var freshHealth=Health();using(var fresh=Owner(freshHealth)){Assert.IsFalse(fresh.Active);Assert.AreEqual(10000,freshHealth.Hp("op_kyle"));Assert.IsEmpty(fresh.Step(0,new[]{M("op_kyle")}));fresh.CommitCreatedZone("z",0,false);Assert.AreEqual(60,fresh.Step(1,new[]{M("op_kyle")}).Single().HpLoss);}
   Assert.Throws<ObjectDisposedException>(()=>old.Step(2,new[]{M("op_kyle")}));Assert.IsNull(typeof(NyxZoneExecution).GetMethod("RestoreOwned"));
  }
  [Test] public void OwnedCloneUsesIndependentHealthAndRetirementDoesNotAffectSuccessor(){
   var h=Health();var n=Owner(h);n.CommitCreatedZone("z",0,false);n.Step(1,new[]{M("op_kyle")});var nextHealth=RestoreHealth(h);
   using(var next=(NyxZoneExecution)Invoke(n,"CloneOwned",nextHealth,(Func<string,long,JObject>)Attack,(Func<string,long,JObject>)Defense)){
    n.Dispose();Assert.AreEqual(60,next.Step(2,new[]{M("op_kyle")}).Single().HpLoss);Assert.AreEqual(9940,h.Hp("op_kyle"));
   }
  }
 }
}
#endif
