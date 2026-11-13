#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class CombatProducerSupportBoundaryTests {
  const string Fixture="D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json";
  static JObject Original()=>JObject.Parse(File.ReadAllText(Fixture));
  static BasicCombatSimulation Build(JObject input)=>BasicCombatSimulation.FromPreparedProfileForLocalTest(input,(JObject)input["ownedSelection"],"a",1u);
  static void RejectWithoutMutation(JObject input){var before=input.DeepClone();Assert.Throws<ArgumentException>(()=>Build(input));Assert.IsTrue(JToken.DeepEquals(before,input));}
  [TestCase("equipment")][TestCase("research")][TestCase("extraPressure")]
  public void NewCalculatorsDoNotAuthorizeUnsupportedProducerVariants(string feature){var input=Original();input["draft"]["template"]["variants"][feature]="ON";RejectWithoutMutation(input);}
  [Test] public void NewCarriageDamageRouteRequiresItsOwnSupportedProfile(){var input=Original();input["draft"]["template"]["execution"]["carriageDamageEvents"]="SUPPORTED";RejectWithoutMutation(input);}
  [Test] public void LatestNumericDefenseCannotBeInjectedIntoOriginalArmorProfile(){var input=Original();input["draft"]["template"]["operators"][0]["stats"]["def"]=250;RejectWithoutMutation(input);}
  [TestCase("effectiveStatsVersion")][TestCase("roundingVersion")][TestCase("simulationVersion")]
  public void UnissuedDefinitionVersionsReject(string field){var input=Original();input["draft"][field]="TEST_ONLY_UNISSUED_CURRENT_POLICY";RejectWithoutMutation(input);}
  [Test] public void ExecutionHashCannotAuthorizeChangedOrUnissuedPolicy(){var input=Original();input["draft"]["template"]["execution"]["policySha256"]=new string('0',64);RejectWithoutMutation(input);input=Original();input["draft"]["executionPolicy"]["TEST_ONLY_modified"]=true;RejectWithoutMutation(input);}
  [Test] public void OriginalPreparedProfileRetainsStateAndContinuesIdentically(){var input=Original();var untouched=input.DeepClone();var model=Build(input);model.Advance(500000*20,20);var saved=model.CaptureLocalState();var restored=BasicCombatSimulation.RestoreLocalState(saved,()=>Build(input));model.Advance(500000*40,40);restored.Advance(500000*40,40);Assert.AreEqual(model.CaptureLocalState(),restored.CaptureLocalState());Assert.Greater(model.Events.Count,0);Assert.IsTrue(JToken.DeepEquals(untouched,input));}
  [Test] public void CurrentDefinitionCannotReinterpretOriginalCheckpoint(){var original=Original();var model=Build(original);model.Advance(500000*20,20);var saved=model.CaptureLocalState();var changed=(JObject)original.DeepClone();var row=changed["draft"]["template"]["operators"][0]["stats"];row["maxHp"]=(int)row["maxHp"]+1;Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(saved,()=>Build(changed)));Assert.AreEqual(saved,model.CaptureLocalState());Assert.AreEqual(saved,BasicCombatSimulation.RestoreLocalState(saved,()=>Build(original)).CaptureLocalState());}
 }
}
#endif
