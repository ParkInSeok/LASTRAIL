using System;
using LastRail.Battle;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class CombatAttributedLifestealTests {
  static object Owner(CombatVitals hp){var t=typeof(CombatVitals).Assembly.GetType("LastRail.Battle.CombatAttributedLifesteal");Assert.IsNotNull(t);return Activator.CreateInstance(t,hp);}
  static CombatLivingHealResult Apply(object owner,CombatDamageResult[] hits,int modifier=10000,bool allowed=true)=>(CombatLivingHealResult)owner.GetType().GetMethod("Apply").Invoke(owner,new object[]{hits,modifier,allowed});
  static CombatDamageResult Hit(int damage)=>new CombatVitals(damage,0).Damage(damage,0,false);
  [Test]public void AllAttributedHpDamageRecoversTwelvePercentWithoutWindowOrOpportunityCap(){
   var hp=new CombatVitals(10000,0);hp.Damage(9000,0,false);var owner=Owner(hp);
   Assert.AreEqual(1200,Apply(owner,new[]{Hit(10000)}).ActualHpGain);
   Assert.AreEqual(1200,Apply(owner,new[]{Hit(10000)}).ActualHpGain);Assert.AreEqual(3400,hp.Hp);
  }
  [Test]public void SplitDamageAndOneHealingModifierUseExistingIntegerRecovery(){
   var hp=new CombatVitals(1000,0);hp.Damage(500,0,false);var owner=Owner(hp);
   Assert.AreEqual(0,Apply(owner,new[]{Hit(5),Hit(5)}).ActualHpGain);Assert.AreEqual(60,Apply(owner,new[]{Hit(1000)},5000).ActualHpGain);
   Assert.AreEqual(0,Apply(owner,new[]{Hit(1000)},10000,false).ActualHpGain);
  }
  [Test]public void ExcessNeverFeedsShieldOrOverflowAndDownNeverRevives(){
   var hp=new CombatVitals(1000,0);hp.Damage(10,0,false);var owner=Owner(hp);var result=Apply(owner,new[]{Hit(1000)});
   Assert.AreEqual(10,result.ActualHpGain);Assert.AreEqual(0,result.Excess);
   var conversion=new CombatOverhealParent(hp,"parent",1000,true,true,false).AllocateTick(0,new[]{result},null,true);Assert.AreEqual(0,conversion.ShieldBudget);Assert.AreEqual(0,conversion.AttackBudget);
   Assert.AreEqual(0,Apply(owner,new[]{Hit(1000)}).ActualHpGain);hp.Damage(1000,0,false);Assert.AreEqual(0,Apply(owner,new[]{Hit(1000)}).ActualHpGain);Assert.AreEqual(0,hp.Hp);
  }
  [Test]public void ShieldsAndOverkillAreNotHpDamage(){
   var hp=new CombatVitals(1000,0);hp.Damage(500,0,false);var owner=Owner(hp);var enemy=new CombatVitals(100,10000);enemy.Shield(new CombatShieldSource("e","s",0),100,100,0);
   Assert.AreEqual(0,Apply(owner,new[]{enemy.Damage(100,0,false)}).ActualHpGain);Assert.AreEqual(12,Apply(owner,new[]{enemy.Damage(10000,0,false)}).ActualHpGain);
  }
  [Test]public void FractionalRequestKeepsPrecisionUntilFinalHealingModifier(){var hp=new CombatVitals(1000,0);hp.Damage(100,0,false);Assert.AreEqual(1,Apply(Owner(hp),new[]{Hit(5)},20000).ActualHpGain);}
 }
}
