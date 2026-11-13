using System;
using LastRail.Battle;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class OverflowProjectileBallisticsTests {
  static Type Domain(){var t=typeof(CombatVitals).Assembly.GetType("LastRail.Battle.OverflowProjectileBallistics");Assert.IsNotNull(t);return t;}
  static object Call(string method,params object[] args)=>Domain().GetMethod(method).Invoke(null,args);
  [TestCase(166667,6000)][TestCase(333334,12000)][TestCase(666667,24000)][TestCase(700000,24000)]
  public void ApprovedSpeedClipsDistanceEvenWhenLifetimeRoundsUp(long microseconds,long millimetres){var distance=(CombatExactValue)Call("DistanceAtMicroseconds",microseconds);Assert.AreEqual(millimetres,(long)distance.Floor());}
  [Test]public void LifetimeRoundsToCombatStepWithoutIncreasingRange(){Assert.AreEqual(700000L,Call("MaximumLifetimeMicroseconds",50000L));Assert.AreEqual(667000L,Call("MaximumLifetimeMicroseconds",1000L));Assert.AreEqual(24000L,(long)((CombatExactValue)Call("DistanceAtMicroseconds",long.MaxValue)).Floor());}
  [Test]public void RadiusUsesExistingSweepKernelForReservedTargetAndThinBlocker(){
   var start=new long[]{0,500,180};var end=new long[]{2000,500,180};var feet=new long[]{1000,0,0};
   Assert.IsNotNull(Call("SweepReservedTarget",start,end,feet,feet,1000L,100L));start[2]=181;end[2]=181;Assert.IsNull(Call("SweepReservedTarget",start,end,feet,feet,1000L,100L));
   Assert.IsNotNull(Call("SweepBlocker",new long[]{0,500,0},new long[]{2000,500,0},new long[]{1000,500,0},new long[]{1000,500,0},new long[]{1,1000,1000}));
  }
  [Test]public void ImmediateHealingSurvivesDiscardedAttackBudget(){
   var patient=new CombatVitals(1000,0);patient.Damage(300,0,false);var confirmed=patient.ResolveLivingHeal(1000,true,true);Assert.AreEqual(1000,patient.Hp);Assert.AreEqual(300,confirmed.ActualHpGain);
   var parent=new CombatOverhealParent(new CombatVitals(1000,0),"parent",1000,false,true,false);var result=parent.AllocateTick(0,new[]{confirmed},null,false);Assert.AreEqual(0,result.AttackBudget);Assert.AreEqual(1000,patient.Hp);Assert.AreEqual(1080,parent.RemainingAttackBudget);
  }
 }
}
