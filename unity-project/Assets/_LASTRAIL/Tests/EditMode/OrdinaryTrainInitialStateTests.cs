using System;
using System.IO;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryTrainInitialStateTests {
 public static JObject Export()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/TrainInitial777/consumed-export.json"));
 static OrdinaryFiveStartInput Start(JObject e)=>OrdinaryFiveStartInput.Read((JObject)e["originalStart"],(string)e["accountId"],(string)e["runId"]);
 [Test] public void AuthoritativeSixTargetsReachExistingFactoryAndCheckpoint(){
  var e=Export();var start=Start(e);var f=OrdinaryFiveSeraFactory.FromInitialization(start,e["initializationResponse"].ToString(),500000);
  var model=f.Create();Assert.AreEqual(5000,model.GetHp(start.Train.TrainId));
  Assert.AreEqual(6,((JArray)JObject.Parse(model.CaptureLocalState())["actors"]).Count);
  model.Advance(500000,1);var saved=model.CaptureLocalState();Assert.AreEqual(saved,BasicCombatSimulation.RestoreLocalState(saved,f.Create).CaptureLocalState());
 }
 [TestCase("trainId")][TestCase("sourceHash")][TestCase("healthTargetId")][TestCase("healthRevision")][TestCase("logicalTimeMs")][TestCase("activeShields")]
 public void InitialBindingMustNotBeSubstituted(string field){var e=Export();var state=e["initializationResponse"]["initializationReceipt"]["trainInitialState"];state[field]=field=="healthRevision"||field=="logicalTimeMs"?new JValue(2):field=="activeShields"?new JArray("invented"):new JValue("wrong");Assert.Throws<FormatException>(()=>OrdinaryInitializationReceipt.Validate(e["initializationResponse"].ToString(),Start(e)));}
 [Test] public void MissingInitialStateCannotBecomeAnImplicitFullTrain(){var e=Export();((JObject)e["initializationResponse"]["initializationReceipt"]).Remove("trainInitialState");Assert.Throws<FormatException>(()=>OrdinaryInitializationReceipt.Validate(e["initializationResponse"].ToString(),Start(e)));}
}
