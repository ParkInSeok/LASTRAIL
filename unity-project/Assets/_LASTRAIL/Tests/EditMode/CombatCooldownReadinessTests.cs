using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class CombatCooldownReadinessTests {
 // Existing local fixture only; this is not ordinary-five Player acceptance evidence.
 static BasicCombatSimulation Fresh(){
  var p=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/ce03-frontline-policy-UtU7yJ/prepared.json"));
  var s=BasicCombatSimulation.FromPreparedProfileForLocalTest(p,(JObject)p["ownedSelection"],(string)p["ownedSelection"]["accountId"],1);
  s.ConfigureLocalOperatorAutomation(false);return s;
 }
 static JObject State(){
  var state=JObject.Parse(Fresh().CaptureLocalState());
  foreach(var actor in (JArray)state["actors"]){actor["ready"]=100000;actor["availableAt"]=0;}
  foreach(var skill in (JArray)state["skillReady"])skill["ready"]=0;
  state["operatorAutomation"]["charge"]["op_reina"]=100;return state;
 }
 static BasicCombatSimulation Restore(JObject state)=>BasicCombatSimulation.RestoreLocalState(state.ToString(),Fresh);
 static JObject Row(BasicCombatSimulation model,string collection="skills")=>(JObject)((JArray)model.ReadHudState()[collection]).Single(r=>(string)r["actor"]=="op_reina");
 [TestCase("ready")][TestCase("paused")][TestCase("target")][TestCase("recovery")][TestCase("down")][TestCase("queued")]
 public void ZeroCooldownReadyMatchesExistingEligibility(string condition){
  var state=State();var actor=((JArray)state["actors"]).Single(a=>(string)a["id"]=="op_reina");
  if(condition=="recovery")actor["recoveryUntil"]=20;
  if(condition=="down")actor["vitals"]["hp"]=0;
  if(condition=="target")foreach(var enemy in (JArray)state["actors"])if(((string)enemy["id"]).StartsWith("wave."))enemy["availableAt"]=10000;
  var model=Restore(state);if(condition=="paused")model.SetPaused(true);
  if(condition=="queued")model.RequestSkill("readiness-queued","op_reina","op_reina.active");
  foreach(var collection in new[]{"skills","ultimates"}){
   var row=Row(model,collection);Assert.AreEqual(0,(long)row["cooldownMs"]);Assert.AreEqual(condition=="ready",(bool)row["enabled"],condition+" fixture eligibility");
   Assert.AreEqual((bool)row["enabled"],(string)row["status"]=="READY",condition+" "+collection);
  }
 }
 [Test] public void CountdownUsesOnlyOwnerStepsAndReadsDoNotAdvanceOrDispatch(){
  var state=State();((JArray)state["skillReady"]).Single(s=>(string)s["key"]=="op_reina/op_reina.active")["ready"]=20;
  var model=Restore(state);Assert.AreEqual(1000,(long)Row(model)["cooldownMs"]);
  var before=model.CaptureLocalState();for(int i=0;i<20;i++)model.ReadHudState();Assert.AreEqual(before,model.CaptureLocalState());
  model.SetPaused(true);model.Advance(10000000,100);Assert.AreEqual(1000,(long)Row(model)["cooldownMs"]);
  model.SetPaused(false);model.SetSpeed(4);model.Advance(500000,10);
  Assert.AreEqual(2,model.Tick);Assert.AreEqual(900,(long)Row(model)["cooldownMs"]);Assert.AreNotEqual("READY",(string)Row(model)["status"]);
  var restored=Restore(JObject.Parse(model.CaptureLocalState()));Assert.AreEqual(Row(model)["cooldownMs"],Row(restored)["cooldownMs"]);
  Assert.IsEmpty((JArray)JObject.Parse(restored.CaptureLocalState())["commands"]);
 }
}
