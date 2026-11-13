using System;
using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class CombatHealingAugmentsTests {
  static Type Type(string name){var t=typeof(CombatVitals).Assembly.GetType("LastRail.Battle."+name);Assert.IsNotNull(t,"Missing approved recovery domain "+name);return t;}
  static object Call(object target,string name,params object[] args){try{return target.GetType().GetMethod(name).Invoke(target,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  static object Get(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
  static object Window(CombatVitals owner)=>Activator.CreateInstance(Type("CombatLifestealWindow"),owner,"run/node/actor-generation",1000L);
  static int Leech(object window,long tick,CombatDamageResult hit,bool eligible=true)=>(int)Call(window,"Apply",tick,new[]{hit},eligible);
  static object Heal(CombatVitals target,int amount,bool original=true,bool allowed=true){var method=typeof(CombatVitals).GetMethod("ResolveLivingHeal");Assert.IsNotNull(method);return method.Invoke(target,new object[]{amount,original,allowed});}
  static object Parent(CombatVitals healer,int attack,bool shield=false,bool reflux=true,bool redistribute=false)=>Activator.CreateInstance(Type("CombatOverhealParent"),healer,"run/node/heal-parent",attack,shield,reflux,redistribute);
  static object Allocate(object parent,long tick,object heal,CombatVitals other=null,bool target=true){var a=Array.CreateInstance(Type("CombatLivingHealResult"),1);a.SetValue(heal,0);return Call(parent,"AllocateTick",tick,a,other,target);}
  [Test]public void DownRecoveryAndReplacementKeepLogicalActorWindowWhileNextNodeStartsFresh(){
   var actor=new CombatVitals(1000,0);actor.Damage(500,0,false);var window=new CombatLifestealWindow(actor,"run/node/actor",1000);
   var hit=new CombatVitals(1000,0).Damage(1000,0,false);Assert.AreEqual(80,window.Apply(50,new[]{hit},true));
   actor.Damage(1000,50,false);Assert.AreEqual(0,window.Apply(50,new[]{hit},true));
   typeof(CombatVitals).GetMethod("RecoverFromDown",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(actor,new object[]{200});
   Assert.AreEqual(0,window.Apply(50,new[]{hit},true),"Paused combat clock and recovery cannot reset budget");
   var replacement=new CombatVitals(1000,0);replacement.Damage(800,0,false);
   var restored=CombatLifestealWindow.Restore(replacement,"run/node/actor",window.Capture());Assert.AreEqual(0,restored.Apply(1049,new[]{hit},true));Assert.AreEqual(80,restored.Apply(1050,new[]{hit},true));
   Assert.Throws<ArgumentException>(()=>CombatLifestealWindow.Restore(replacement,"run/next/actor",window.Capture()));
   Assert.AreEqual(80,new CombatLifestealWindow(replacement,"run/next/actor",1000).Apply(0,new[]{hit},true));
  }
  [Test]public void NewCombinationCannotRewriteExistingHealingParent(){
   var actor=new CombatVitals(1000,0);var target=new CombatVitals(1000,0);
   var original=new CombatOverhealParent(actor,"old-parent",1000,true,false,false);
   Assert.AreEqual(350,original.AllocateTick(1,new[]{target.ResolveLivingHeal(700,true,true)},null,true).ShieldBudget);
   var acquired=new CombatOverhealParent(actor,"new-parent",1000,true,true,false);
   var oldRestored=CombatOverhealParent.Restore(actor,"old-parent",original.Capture());
   var oldResult=oldRestored.AllocateTick(2,new[]{target.ResolveLivingHeal(700,true,true)},null,true);
   Assert.AreEqual(350,oldResult.ShieldBudget);Assert.AreEqual(0,oldResult.AttackBudget);
   var newResult=acquired.AllocateTick(2,new[]{target.ResolveLivingHeal(700,true,true)},null,true);
   Assert.AreEqual(175,newResult.ShieldBudget);Assert.AreEqual(210,newResult.AttackBudget);
  }
  [Test]public void SameTickRecipientsShareOneParentBudget(){
   var actor=new CombatVitals(1000,0);var parent=new CombatOverhealParent(actor,"parent",100,false,true,false);
   var results=new[]{new CombatVitals(1000,0).ResolveLivingHeal(200,true,true),new CombatVitals(1000,0).ResolveLivingHeal(200,true,true)};
   Assert.AreEqual(150,parent.AllocateTick(10,results,null,true).AttackBudget);Assert.AreEqual(0,parent.RemainingAttackBudget);
  }
  [Test]public void CastTimeDeficitIsNotReservedAndMissingEnemyDoesNotUndoHealing(){
   var target=new CombatVitals(2000,0);target.Damage(300,0,false);
   target.ResolveLivingHeal(200,true,true); // Another effect resolves before this cast.
   var resolved=target.ResolveLivingHeal(1000,true,true);
   Assert.AreEqual(100,resolved.ActualHpGain);Assert.AreEqual(900,resolved.Excess);
   var parent=new CombatOverhealParent(new CombatVitals(1000,0),"cast",1000,false,true,false);
   Assert.AreEqual(0,parent.AllocateTick(1,new[]{resolved},null,false).AttackBudget);
   Assert.AreEqual(2000,target.Hp);Assert.AreEqual(960,parent.RemainingAttackBudget);
   Assert.AreEqual(0,parent.AllocateTick(2,new[]{target.ResolveLivingHeal(0,true,true)},null,true).AttackBudget);
   target.Damage(2000,0,false);Assert.AreEqual(0,target.ResolveLivingHeal(1000,true,true).Excess);
  }
  [Test]public void PulseAllocationDebitsOnceAndExhaustionKeepsHealingAndProtection(){
   var caster=new CombatVitals(2000,0);var recipient=new CombatVitals(4000,0);
   var parent=new CombatOverhealParent(caster,"three-ticks",1000,true,true,false);
   Assert.AreEqual(0,parent.AllocateTick(1,new[]{recipient.ResolveLivingHeal(3000,true,true)},null,false).AttackBudget);
   Assert.AreEqual(600,parent.RemainingAttackBudget);
   parent=CombatOverhealParent.Restore(caster,"three-ticks",parent.Capture());
   var second=parent.AllocateTick(2,new[]{recipient.ResolveLivingHeal(3000,true,true)},null,true);
   Assert.AreEqual(600,second.AttackBudget);Assert.AreEqual(750,second.ShieldBudget);
   Assert.Throws<ArgumentException>(()=>parent.AllocateTick(2,Array.Empty<CombatLivingHealResult>(),null,true));
   Assert.AreEqual(0,parent.RemainingAttackBudget);
   recipient.Damage(100,0,false);var healed=recipient.ResolveLivingHeal(3100,true,true);
   var third=parent.AllocateTick(3,new[]{healed},null,true);
   Assert.AreEqual(100,healed.ActualHpGain);Assert.AreEqual(4000,recipient.Hp);
   Assert.AreEqual(0,third.AttackBudget);Assert.AreEqual(750,third.ShieldBudget);
  }
  [Test]public void ResolvedReducedHealAndRejectedHealCannotConvertDeniedAmount(){
   var target=new CombatVitals(1000,0);target.Damage(300,0,false);
   // The original effect pipeline supplies H=500 after its 50% reduction.
   var reduced=target.ResolveLivingHeal(500,true,true);Assert.AreEqual(300,reduced.ActualHpGain);Assert.AreEqual(200,reduced.Excess);
   var parent=new CombatOverhealParent(new CombatVitals(1000,0),"parent",1000,false,true,false);
   Assert.AreEqual(120,parent.AllocateTick(0,new[]{reduced},null,true).AttackBudget);
   var denied=target.ResolveLivingHeal(1000,true,false);Assert.AreEqual(0,denied.Excess);
   var both=new CombatOverhealParent(new CombatVitals(1000,0),"denied-parent",1000,true,true,false);
   var allocation=both.AllocateTick(0,new[]{denied},null,true);Assert.AreEqual(0,allocation.ShieldBudget);Assert.AreEqual(0,allocation.AttackBudget);
  }
  [Test]public void DamageImmunityAndPartialShieldUseOnlyConfirmedHpLoss(){
   var actor=new CombatVitals(1000,0);actor.Damage(500,0,false);var window=new CombatLifestealWindow(actor,"run/node/actor",1000);
   var enemy=new CombatVitals(1000,10000);enemy.Shield(new CombatShieldSource("enemy","shield",0),200,1000,0);
   Assert.AreEqual(0,window.Apply(0,new[]{enemy.Damage(300,0,true)},true));Assert.AreEqual(200,enemy.ShieldRemaining(0));
   var hit=enemy.Damage(300,0,false);Assert.AreEqual(200,hit.Absorbed);Assert.AreEqual(100,hit.HpLoss);Assert.AreEqual(12,window.Apply(0,new[]{hit},true));
  }
  [Test]public void ActualHpLossOnlyAndRollingWindowHasNoBoundaryReset(){
   var caster=new CombatVitals(1000,10000);caster.Damage(500,0,false);var enemy=new CombatVitals(10000,10000);var w=Window(caster);
   Assert.AreEqual(60,Leech(w,100,enemy.Damage(500,100,false)));
   Assert.AreEqual(20,Leech(w,999,enemy.Damage(500,999,false)));
   Assert.AreEqual(0,Leech(w,1000,enemy.Damage(500,1000,false)));
   Assert.AreEqual(60,Leech(w,1100,enemy.Damage(500,1100,false)));Assert.AreEqual(640,caster.Hp);
  }
  [Test]public void ShieldOverkillDerivedAndDownDoNotMintHealing(){
   var caster=new CombatVitals(1000,10000);caster.Damage(500,0,false);var w=Window(caster);var enemy=new CombatVitals(100,10000);enemy.Shield(new CombatShieldSource("enemy","shield",0),100,2000,0);
   Assert.AreEqual(0,Leech(w,0,enemy.Damage(100,0,false)));Assert.AreEqual(12,Leech(w,0,enemy.Damage(1000,0,false)));
   Assert.AreEqual(0,Leech(w,0,new CombatVitals(1000,0).Damage(1000,0,false),false));
   caster.Damage(1000,0,false);Assert.AreEqual(0,Leech(w,1000,new CombatVitals(1000,0).Damage(1000,0,false)));Assert.AreEqual(0,caster.Hp);
  }
  [Test]public void OnlyActualGainConsumesBudgetAndSplitBodiesAggregate(){
   var caster=new CombatVitals(1000,0);var w=Window(caster);var enemy=new CombatVitals(1000,0);Assert.AreEqual(0,Leech(w,0,enemy.Damage(500,0,false)));
   caster.Damage(100,0,false);var parts=new[]{enemy.Damage(5,0,false),enemy.Damage(5,0,false)};Assert.AreEqual(1,Call(w,"Apply",0L,parts,true));
   Assert.AreEqual(58,Leech(w,0,enemy.Damage(490,0,false)));
  }
  [Test]public void WindowSnapshotRestoresRecentActualBudgetAndRejectsWrongScope(){
   var caster=new CombatVitals(1000,0);caster.Damage(500,0,false);var w=Window(caster);var hit=new CombatVitals(1000,0).Damage(1000,0,false);Assert.AreEqual(80,Leech(w,50,hit));
   var snapshot=Call(w,"Capture");var restored=Type("CombatLifestealWindow").GetMethod("Restore").Invoke(null,new[]{caster,"run/node/actor-generation",snapshot});
   Assert.AreEqual(0,Leech(restored,1049,hit));Assert.AreEqual(80,Leech(restored,1050,hit));
   Assert.Throws<ArgumentException>(()=>Call(restored,"Apply",1049L,new[]{hit},true));
   Assert.Throws<TargetInvocationException>(()=>Type("CombatLifestealWindow").GetMethod("Restore").Invoke(null,new[]{caster,"other-generation",snapshot}));
  }
  [TestCase(false,true,0,420)][TestCase(true,false,350,0)][TestCase(true,true,175,210)]
  public void OriginalExcessIsSplitOnce(bool shield,bool reflux,int protection,int damage){var target=new CombatVitals(1000,0);target.Damage(300,0,false);var h=Heal(target,1000);Assert.AreEqual(300,Get(h,"ActualHpGain"));Assert.AreEqual(700,Get(h,"Excess"));var p=Parent(new CombatVitals(1000,0),1000,shield,reflux);var a=Allocate(p,0,h);Assert.AreEqual(protection,Get(a,"ShieldBudget"));Assert.AreEqual(damage,Get(a,"AttackBudget"));Assert.Throws<ArgumentException>(()=>Allocate(p,0,h));}
  [Test]public void RedistributionUsesActualGainAndOnlyOnceBeforeSplitting(){var h=Heal(new CombatVitals(1000,0),700);var other=new CombatVitals(1000,0);other.Damage(100,0,false);var p=Parent(new CombatVitals(1000,0),1000,true,true,true);var a=Allocate(p,0,h,other);Assert.AreEqual(100,Get(a,"RedistributedHp"));Assert.AreEqual(150,Get(a,"ShieldBudget"));Assert.AreEqual(180,Get(a,"AttackBudget"));other.Damage(500,1,false);a=Allocate(p,1,Heal(new CombatVitals(1000,0),700),other);Assert.AreEqual(0,Get(a,"RedistributedHp"));}
  [Test]public void ParentCapDiscardAndRestoreDoNotRefill(){var healer=new CombatVitals(1000,0);var p=Parent(healer,100);var h=Heal(new CombatVitals(1000,0),1000);var a=Allocate(p,0,h,null,false);Assert.AreEqual(0,Get(a,"AttackBudget"));Assert.AreEqual(0L,Get(p,"RemainingAttackBudget"));var snapshot=Call(p,"Capture");var restored=Type("CombatOverhealParent").GetMethod("Restore").Invoke(null,new[]{healer,"run/node/heal-parent",snapshot});Assert.AreEqual(0,Get(Allocate(restored,1,h),"AttackBudget"));}
  [TestCase(false,true)][TestCase(true,false)]public void DerivedOrRejectedHealingDoesNotGenerateExcess(bool original,bool allowed){var target=new CombatVitals(1000,0);var h=Heal(target,1000,original,allowed);Assert.AreEqual(0,Get(h,"Excess"));Assert.AreEqual(0,Get(Allocate(Parent(target,1000),0,h),"AttackBudget"));}
 }
}
