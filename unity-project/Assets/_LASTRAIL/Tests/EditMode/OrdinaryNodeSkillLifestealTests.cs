using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryNodeSkillLifestealTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 sealed class Proof : ICombatSkillLifestealAuthority {
  public string BindingId {get;set;}="TEST_ONLY_original_node_owner";
  internal bool Owned=true,Same=true,Allowed=true,Missing;
  internal ConfirmedSkillDamage Observed;
  public CombatSkillLifestealProof ResolveOriginal(ConfirmedSkillDamage damage,long tick){Observed=damage;return Missing?null:new CombatSkillLifestealProof("TEST_ONLY_evidence","TEST_ONLY_definition",Owned,Same,Allowed,10000);}
 }
 sealed class Fixture {
  internal OrdinaryFiveCombatFactory Factory;internal BCombatLocalModel Health;internal string Target;internal Proof Authority=new Proof();internal object Owner;
  internal Fixture(){var f=Activator.CreateInstance(typeof(OrdinaryNodeProjectileOwnerTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,new object[]{false},null);object Field(string key)=>f.GetType().GetField(key,Hidden).GetValue(f);
   Factory=(OrdinaryFiveCombatFactory)Field("Factory");Health=(BCombatLocalModel)Field("Health");Target=(string)Field("Target");
   var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryNodeSkillLifesteal");Assert.NotNull(type,"Node skill recovery must use its existing HP owner");
   Owner=Activator.CreateInstance(type,Hidden,null,new object[]{Factory,Health,Authority},null);
  }
  internal JObject Row(BCombatSingleTargetReceipt receipt)=>new JObject{["id"]="skill-hit-one",["runId"]=Factory.Original.RunId,["profileSha256"]=(string)typeof(BasicCombatSimulation).GetMethod("Digest",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Factory.ConfigurationVersion}),["commandId"]="original-cast",["abilityId"]="op_reina.ultimate",["sourceId"]="op_reina",["sourceKind"]="SKILL",["kind"]="DAMAGE",["targetId"]=Target,["targetGeneration"]=1,["nodeId"]=Factory.Attempt.NodeId,["attemptGeneration"]=Factory.Attempt.Generation,["globalSequence"]=1,["pulseIndex"]=0,["tick"]=receipt.Tick,["actualResult"]=new JObject{["hpLoss"]=receipt.HpLoss,["absorbed"]=receipt.Absorbed}};
  internal JObject Apply(JObject row,BCombatSingleTargetReceipt receipt){try{return (JObject)Owner.GetType().GetMethod("Apply",Hidden).Invoke(Owner,new object[]{row,receipt});}catch(TargetInvocationException e){throw e.InnerException;}}
 }
 [Test] public void SameDamageCannotHealAgainUnderAnotherPresentationIdentity(){var f=new Fixture();var damage=f.Health.ApplyResolvedDamage("actual-hit",f.Target,100,1);var first=f.Row(damage);var alias=(JObject)first.DeepClone();f.Apply(first,damage);int hp=f.Health.Hp("op_reina");alias["id"]="alias-for-same-damage";Assert.Throws<InvalidOperationException>(()=>f.Apply(alias,damage));Assert.AreEqual(hp,f.Health.Hp("op_reina"));}
 [Test] public void FreshCopyRetryAtDeficitCapReturnsOriginalDecision(){var f=new Fixture();var damage=f.Health.ApplyResolvedDamage("actual-hit",f.Target,300,1);var first=f.Row(damage);var retry=(JObject)first.DeepClone();var result=f.Apply(first,damage);Assert.AreEqual(30,(int)result["actualHpGain"]);Assert.IsTrue(JToken.DeepEquals(result,f.Apply(retry,damage)));Assert.AreEqual(100,f.Health.Hp("op_reina"));}
 [TestCase("pulseIndex")][TestCase("targetGeneration")]public void NodeProofNeverDefaultsMissingAttribution(string missing){var f=new Fixture();var damage=f.Health.ApplyResolvedDamage("actual-hit",f.Target,100,1);var row=f.Row(damage);row.Remove(missing);Assert.Throws<InvalidOperationException>(()=>f.Apply(row,damage));}
 [Test] public void OriginalSkillDamageHealsThroughSameKernelAndExposesAttemptAttribution(){var f=new Fixture();int before=f.Health.Hp("op_reina");var damage=f.Health.ApplyResolvedDamage("actual-hit",f.Target,100,1);var row=f.Row(damage);var result=f.Apply(row,damage);
  Assert.AreEqual(before+12,f.Health.Hp("op_reina"));Assert.AreEqual(12,(int)result["actualHpGain"]);Assert.AreEqual(0,(int)result["derivedExcess"]);
  var observed=f.Authority.Observed;Assert.AreEqual(f.Factory.Attempt.NodeId,observed.GetType().GetProperty("NodeId")?.GetValue(observed));Assert.AreEqual(f.Factory.Attempt.Generation,observed.GetType().GetProperty("AttemptGeneration")?.GetValue(observed));Assert.AreEqual("op_reina.ultimate",observed.GetType().GetProperty("AbilityId")?.GetValue(observed));
  Assert.IsTrue(JToken.DeepEquals(result,f.Apply(row,damage)));Assert.AreEqual(before+12,f.Health.Hp("op_reina"));
 }
 [TestCase("not-owned")][TestCase("generation")][TestCase("blocked")][TestCase("down")][TestCase("zero")]
 public void IneligibleOrZeroActualDamageCreatesNoRecovery(string reason){var f=new Fixture();f.Authority.Owned=reason!="not-owned";f.Authority.Same=reason!="generation";f.Authority.Allowed=reason!="blocked";if(reason=="down")f.Health.ApplyResolvedDamage("down","op_reina",10000,0);int before=f.Health.Hp("op_reina");var damage=f.Health.ApplyResolvedDamage("actual-hit",f.Target,reason=="zero"?0:100,1);var result=f.Apply(f.Row(damage),damage);Assert.AreEqual(before,f.Health.Hp("op_reina"));Assert.AreEqual(0,(int)result["actualHpGain"]);}
 [TestCase("missing-proof")][TestCase("binding")][TestCase("attempt")][TestCase("loss")][TestCase("derived")]
 public void MissingOrForeignEvidenceFailsWithoutHealing(string change){var f=new Fixture();var damage=f.Health.ApplyResolvedDamage("actual-hit",f.Target,100,1);var row=f.Row(damage);if(change=="missing-proof")f.Authority.Missing=true;if(change=="binding")f.Authority.BindingId="changed";if(change=="attempt")row["attemptGeneration"]=f.Factory.Attempt.Generation+1;if(change=="loss")row["actualResult"]["hpLoss"]=999;if(change=="derived")row["sourceKind"]="DERIVED";int before=f.Health.Hp("op_reina");Assert.Throws<InvalidOperationException>(()=>f.Apply(row,damage));Assert.AreEqual(before,f.Health.Hp("op_reina"));}
}
