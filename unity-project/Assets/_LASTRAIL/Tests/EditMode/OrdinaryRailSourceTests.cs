using System;
using System.IO;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryRailSourceTests {
 public static JObject Export()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/RailRoof797/source.json"));
 static OrdinaryFiveStartInput Read(JObject e)=>OrdinaryFiveStartInput.Read((JObject)e["originalStart"],(string)e["accountId"],(string)e["runId"]);
 [Test] public void ActualV3SourceBindsThreeDistinctPhysicalIdsAndInitialization(){var e=Export();var s=Read(e);Assert.IsTrue(s.Rail.GeometryBound);Assert.AreEqual(3,s.Rail.PhysicalInstanceIds.Count);Assert.AreNotEqual(s.Train.TrainId,s.Rail.PhysicalInstanceIds[0]);Assert.NotNull(OrdinaryInitializationReceipt.Validate(e["initializationResponse"].ToString(),s));var model=OrdinaryFiveSeraFactory.FromInitialization(s,e["initializationResponse"].ToString(),500000).Create();var restored=BasicCombatSimulation.RestoreLocalState(model.CaptureLocalState(),()=>OrdinaryFiveSeraFactory.FromInitialization(Read(e),e["initializationResponse"].ToString(),500000).Create());Assert.AreEqual(model.CaptureLocalState(),restored.CaptureLocalState());}
 [TestCase("id"),TestCase("geometry"),TestCase("position"),TestCase("policy"),TestCase("slot"),TestCase("motion")]
 public void RehashedForeignDefinitionsAndStateAreRejected(string kind){var e=Export();var source=e["originalStart"]["ordinaryRailSource"];var p=source["packet"];
  if(kind=="id")p["physicalConsist"]["instanceIds"][0]="00000000000000000000000000000000";
  if(kind=="geometry")p["geometry"]["railSegmentId"]="another";
  if(kind=="position")p["physicalConsist"]["initialFrontArcMm"]=59000;
  if(kind=="policy")p["definitionVersion"]="unknown";
  if(kind=="slot")p["selectedCarriages"][0]["slot"]=1;
  if(kind=="motion")p["initialMotion"]=new JObject{["speed"]=20000};
  source["sourceHash"]=BDirectAttackInput.Digest(p).Substring(7);Assert.Throws<ArgumentException>(()=>Read(e));
 }
 [Test] public void InitializationCannotDropOrSubstituteRailBinding(){var e=Export();var s=Read(e);((JObject)e["initializationResponse"]["initializationReceipt"]).Remove("ordinaryRailBinding");Assert.Throws<FormatException>(()=>OrdinaryInitializationReceipt.Validate(e["initializationResponse"].ToString(),s));}
 [Test] public void HistoricalSourceAbsenceRemainsAbsent(){var e=Export();((JObject)e["originalStart"]).Remove("ordinaryRailSource");((JObject)e["initializationResponse"]["initializationReceipt"]).Remove("ordinaryRailBinding");var s=Read(e);Assert.IsNull(s.Rail);Assert.NotNull(OrdinaryInitializationReceipt.Validate(e["initializationResponse"].ToString(),s));}
 [Test] public void BoundCheckpointCannotRestoreWithRailRemoved(){var e=Export();var factory=OrdinaryFiveSeraFactory.FromInitialization(Read(e),e["initializationResponse"].ToString(),500000);var snapshot=factory.Create().CaptureLocalState();((JObject)e["originalStart"]).Remove("ordinaryRailSource");((JObject)e["initializationResponse"]["initializationReceipt"]).Remove("ordinaryRailBinding");var missing=OrdinaryFiveSeraFactory.FromInitialization(Read(e),e["initializationResponse"].ToString(),500000);Assert.Catch(()=>BasicCombatSimulation.RestoreLocalState(snapshot,missing.Create));}
}
