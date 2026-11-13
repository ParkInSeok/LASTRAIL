using System.IO;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryServerExportTests {
 const string Source="D:/Project/LASTRAIL/QA/CommanderS1-20260922/ordinary-initialization-consumer-source-20261026.json";
 [Test] public void SuppliedServerResponsePreservesFullHealthAndRestoresQueuedNativeCommand(){
  var export=JObject.Parse(File.ReadAllText(Source));
  Assert.AreEqual("SYNTHETIC_ACCOUNT_INTERNAL_START_PRODUCTION_INITIALIZATION_HTTP_NOT_PRODUCT_ADMISSION",(string)export["classification"]);
  Assert.IsFalse((bool)export["productAdmissionVerified"]);
  var original=OrdinaryFiveStartInput.Read((JObject)export["originalStart"],(string)export["accountId"],(string)export["runId"]);
  string receipt=export["initializationResponse"].ToString(Formatting.None);
  var stats=OrdinaryInitializationReceipt.Validate(receipt,original);Assert.NotNull(stats);Assert.AreEqual(5,original.Actors.Count);
  // Explicit test clock only; the export does not publish an ordinary execution step.
  var factory=OrdinaryFiveSeraFactory.FromInitialization(original,receipt,500000);var model=factory.Create();
  foreach(var actor in original.Actors)Assert.AreEqual(stats.Actor(actor.OperatorId).CurrentHp,model.GetHp(actor.OperatorId));
  model.ScheduleResolvedSeraCommand(OrdinaryFiveSeraFactoryTests.Cast());
  var restored=BasicCombatSimulation.RestoreLocalState(model.CaptureLocalState(),factory.Create);
  model.Advance(4000000,20);restored.Advance(4000000,20);
  Assert.AreEqual(model.CaptureLocalState(),restored.CaptureLocalState());
  var emissions=restored.ReadSeraEmissions();Assert.AreEqual(1,emissions.Count);
  Assert.AreEqual("REJECTED",(string)emissions[0]["receipt"]["status"]);
  Assert.AreEqual("NO_ELIGIBLE_TARGET",(string)emissions[0]["receipt"]["reason"]);
  foreach(var actor in original.Actors)Assert.AreEqual(stats.Actor(actor.OperatorId).CurrentHp,restored.GetHp(actor.OperatorId));
 }
}
