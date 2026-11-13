#if UNITY_EDITOR
using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class CombatBasicOpportunityTimingTests {
  static (long commit,long recovery,long next) Read(long start,long period,long windup,long recovery,string effective,long step,string denominator="1") {
   var method=typeof(CombatActionTiming).GetMethod("BasicOpportunityBoundaries");
   Assert.That(method,Is.Not.Null,"Approved proportional opportunity timing is missing.");
   try{return ((long,long,long))method.Invoke(null,new object[]{start,period,windup,recovery,CombatExactValue.From(new JObject{["numerator"]=effective,["denominator"]=denominator}),step});}
   catch(TargetInvocationException e){throw e.InnerException;}
  }
  [TestCase(1400000,180000,220000,180,400,1400)]
  [TestCase(1200000,240000,260000,240,500,1200)]
  public void ApprovedBaseCycleIncludesPreparationAndRecovery(long c,long w,long r,long commit,long recovery,long next){
   Assert.That(Read(17,c,w,r,c.ToString(),1000),Is.EqualTo((17+commit,17+recovery,17+next)));
  }
  [Test] public void CumulativeRoundingDoesNotAddARecoveryTick(){
   Assert.That(Read(0,1400000,180000,220000,"700000",50000),Is.EqualTo((2L,4L,14L)));
  }
  [Test] public void RationalPeriodIsNotRoundedBeforeScaling(){
   Assert.That(Read(0,1200000,240000,260000,"250000",50000,"3"),Is.EqualTo((1L,1L,2L)));
  }
  [Test] public void ANewSpeedDoesNotMutateThePreviousOpportunity(){
   var saved=Read(0,1200000,240000,260000,"1200000",1000);
   var next=Read(saved.next,1200000,240000,260000,"600000",1000);
   Assert.That(saved,Is.EqualTo((240L,500L,1200L)));
   Assert.That(next,Is.EqualTo((1320L,1450L,1800L)));
  }
  [Test] public void SameTickBoundariesRemainEqualWithoutAnArtificialMinimumPerPhase(){
   Assert.That(Read(2,1200000,240000,260000,"1",50000),Is.EqualTo((3L,3L,3L)));
   Assert.That(Read(2,1200000,0,0,"1",50000),Is.EqualTo((2L,2L,3L)));
  }
  [Test] public void InvalidOrOverflowingTimelineIsRejected(){
   Assert.Throws<ArgumentException>(()=>Read(0,10,8,3,"10",1));
   Assert.Throws<ArgumentException>(()=>Read(-1,10,1,1,"10",1));
   Assert.Throws<ArgumentException>(()=>Read(0,10,1,1,"0",1));
   Assert.Throws<ArgumentException>(()=>Read(0,10,1,1,"10",0));
   Assert.Throws<OverflowException>(()=>Read(long.MaxValue,10,1,1,"10",1));
  }
 }
}
#endif
