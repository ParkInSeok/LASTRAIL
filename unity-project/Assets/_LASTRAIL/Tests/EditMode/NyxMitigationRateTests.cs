#if UNITY_EDITOR
using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class NyxMitigationRateTests {
  static JObject V(int n)=>new JObject{["numerator"]=n.ToString(),["denominator"]="1"};
  static JObject S(string id,int bps)=>new JObject{["sourceId"]=id,["valueBps"]=bps};
  [Test] public void ExplicitCurrentArrivalAppliesMitigationOnceAndRejectsUnversionedExtra(){
   var launch=new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=V(1000),["appliedOffense"]=new JArray(),["penetration"]=new JArray()};
   var arrival=new JObject{["version"]="combat.projectile-arrival.mitigation.v2.20261107",["defense"]=V(250),["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=Input(250)["reductions"],["mitigationRateReduction"]=new JArray(S("zone",800))};
   var shot=CombatProjectileAttackSnapshot.Capture(launch);Assert.AreEqual(880,shot.Evaluate(arrival).ScaleAndFloor(10000));Assert.AreEqual(440,shot.EvaluatePiercing(arrival,5000).ScaleAndFloor(10000));
   Assert.Throws<ArgumentException>(()=>shot.EvaluateWithMitigationRateReduction(arrival,new JArray(S("zone",800))));arrival["version"]=CombatProjectileAttackSnapshot.Version;Assert.Throws<ArgumentException>(()=>shot.Evaluate(arrival));
  }
  static JObject Input(int defense)=>new JObject{["version"]=CombatNumericDefenseBudget.Version,["base"]=V(1000),["defense"]=V(defense),["effects"]=new JArray(),["weakening"]=new JArray(),["penetration"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
  static CombatNumericDefenseResult Resolve(JObject input,JArray sources){
   var method=typeof(CombatNumericDefenseBudget).GetMethod("ResolveWithMitigationRateReduction");Assert.NotNull(method,"Approved mitigation-rate path is missing");
   try{return (CombatNumericDefenseResult)method.Invoke(null,new object[]{input,sources});}catch(TargetInvocationException e){throw e.InnerException;}
  }
  [TestCase(250,0,800)][TestCase(250,800,880)][TestCase(250,1500,950)][TestCase(53,0,949)][TestCase(53,800,1000)][TestCase(53,1500,1000)]
  public void ApprovedExamplesKeepDefenseUnchanged(int defense,int bps,int expected){var input=Input(defense);string before=input.ToString();var result=Resolve(input,new JArray(S("nyx-zone",bps)));Assert.AreEqual(expected,result.ToVitalsMagnitude());Assert.AreEqual(V(defense).ToString(),result.Snapshot()["effectiveDefense"].ToString());Assert.AreEqual(before,input.ToString());}
  [Test] public void StrongestSourceCapsAtFifteenPointsAndRunsBeforeOtherReductions(){
   var input=Input(250);input["reductions"]["independent"]=new JArray(new JObject{["groupId"]="other",["valueBps"]=5000});Assert.AreEqual(475,Resolve(input,new JArray(S("a",800),S("b",9000),S("b",9000))).ToVitalsMagnitude());Assert.AreEqual(400,CombatNumericDefenseBudget.Resolve(input).ToVitalsMagnitude());
  }
  [Test] public void DefenseWeakeningAndPenetrationPrecedeMitigationAndHaveSeparateCaps(){
   var input=Input(1000);input["weakening"]=new JArray(S("weaken",3000));input["penetration"]=new JArray(S("pierce",6000));var result=Resolve(input,new JArray(S("nyx",800)));Assert.AreEqual("280",(string)result.Snapshot()["effectiveDefense"]["numerator"]);Assert.AreEqual(861,result.ToVitalsMagnitude());
  }
  [Test] public void SourceConflictOrMalformedValueIsRejected(){var input=Input(250);Assert.Throws<ArgumentException>(()=>Resolve(input,new JArray(S("same",800),S("same",1500))));Assert.Throws<ArgumentException>(()=>Resolve(input,new JArray(S("negative",-1))));input["weakening"]=new JArray(S("same",800));Assert.Throws<ArgumentException>(()=>Resolve(input,new JArray(S("same",800))));}
  [Test] public void NewOpportunityUsesStrongestPeriodIncreaseWithoutChangingReservedTimings(){
   var period=CombatExactValue.From(V(1800000));var reserved=CombatActionTiming.BasicOpportunityBoundaries(0,1800000,300000,300000,period,50000);
   var method=typeof(CombatActionTiming).GetMethod("BasicPeriodWithNyxDelay");Assert.NotNull(method);
   var increased=(CombatExactValue)method.Invoke(null,new object[]{period,new[]{new CombatTimingSource("active",1500),new CombatTimingSource("ultimate",2500)}});
   Assert.AreEqual("2250000",(string)increased.Snapshot()["numerator"]);var next=CombatActionTiming.BasicOpportunityBoundaries(0,1800000,300000,300000,increased,50000);
   Assert.AreEqual((6L,12L,36L),reserved);Assert.AreEqual((8L,15L,45L),next);Assert.AreEqual("1800000",(string)period.Snapshot()["numerator"]);
  }
 }
}
#endif
