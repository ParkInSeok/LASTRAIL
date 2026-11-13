using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryReplayInputProjectionTests {
 static OrdinaryFiveCombatFactory Factory(){var source=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json"));return OrdinaryFiveCombatFactory.FromInitialization(OrdinaryFiveStartInput.Read((JObject)source["originalStart"],(string)source["accountId"],(string)source["runId"]),source["initializationResponse"].ToString(),500000);}
 [Test] public void CommittedCommandsRebuildSameSequencesWithoutDuplicateOrFrameInputs(){
  var factory=Factory();var model=factory.Create();model.SetSpeed(3);string first=model.CaptureLocalState();
  model.SetSpeed(3);Assert.AreEqual(first,model.CaptureLocalState());model.SetPaused(true);model.SetPaused(false);model.SetUltimateAuto(true);
  model.Advance(5000000,20);Assert.AreEqual("REJECTED_PREPARATION",model.RequestSkill("blocked","op_kyle","op_kyle.active"));
  var input=model.ReadOrdinaryReplayInputs();var rows=(JArray)input["inputs"];Assert.AreEqual(4,rows.Count);
  for(int i=0;i<rows.Count;i++){Assert.AreEqual(i+1,(int)rows[i]["sequence"]);Assert.AreEqual(0,(long)rows[i]["tick"]);}
  Assert.IsFalse((bool)input["serverAcceptanceVerified"]);Assert.AreEqual(factory.Original.StartSnapshotHash,(string)input["startSnapshotHash"]);
  var state=model.CaptureLocalState();Assert.AreEqual(Ce03CommandContract.Sha(state),(string)input["checkpointSha256"]);
  var restored=BasicCombatSimulation.RestoreLocalState(state,factory.Create);Assert.IsTrue(JToken.DeepEquals(input,restored.ReadOrdinaryReplayInputs()));
  input["inputs"][0]["command"]["value"]=4;Assert.AreEqual(state,model.CaptureLocalState());Assert.AreEqual(3,(int)model.ReadOrdinaryReplayInputs()["inputs"][0]["command"]["value"]);
 }
 [Test] public void HistoricalNoOpOperationsRestoreExactlyButAreNotMeaningfulInputs(){
  var factory=Factory();var model=factory.Create();model.SetSpeed(3);var old=JObject.Parse(model.CaptureLocalState());
  ((JArray)old["operations"]).Add(new JObject{["kind"]="SPEED",["value"]=3});
  var restored=BasicCombatSimulation.RestoreLocalState(old.ToString(),factory.Create);
  Assert.IsTrue(JToken.DeepEquals(old,JObject.Parse(restored.CaptureLocalState())));Assert.AreEqual(1,((JArray)restored.ReadOrdinaryReplayInputs()["inputs"]).Count);
  string before=restored.CaptureLocalState();restored.SetSpeed(3);Assert.AreEqual(before,restored.CaptureLocalState());
  Assert.Throws<ArgumentOutOfRangeException>(()=>restored.SetSpeed(9));Assert.AreEqual(before,restored.CaptureLocalState());
 }
 [TestCase(false)][TestCase(true)] public void UnknownSaveOutcomeCannotExportUnconfirmedInputsAndReopenUsesActualFile(bool wroteBeforeFailure){
  var factory=Factory();string saved=null;long revision=0;bool fail=false;
  RestoredCombatCheckpoint Load()=>saved==null?null:(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{BasicCombatSimulation.RestoreLocalState(saved,factory.Create),revision},null);
  long Save(BasicCombatSimulation state,long expected){Assert.AreEqual(revision,expected);if(fail&&!wroteBeforeFailure)throw new IOException("before save");saved=state.CaptureLocalState();revision++;if(fail)throw new IOException("after save");return revision;}
  var driver=new CombatSceneDriver(factory.Create,Load,Save,()=>true);driver.Open();driver.SetSpeed(3);var first=driver.CaptureOrdinaryReplayInputs();
  fail=true;Assert.Throws<IOException>(()=>driver.SetUltimateAuto(true));Assert.Throws<InvalidOperationException>(()=>driver.CaptureOrdinaryReplayInputs());
  Assert.AreEqual(first.StateJson,driver.StateJson);fail=false;
  var reopened=new CombatSceneDriver(factory.Create,Load,Save,()=>true);reopened.Open();var actual=reopened.CaptureOrdinaryReplayInputs();
  Assert.AreEqual(revision,actual.Revision);Assert.AreEqual(saved,actual.StateJson);Assert.AreEqual(wroteBeforeFailure?2:1,((JArray)actual.Inputs["inputs"]).Count);
  Assert.AreEqual(Ce03CommandContract.Sha(saved),(string)actual.Inputs["checkpointSha256"]);
 }
}
