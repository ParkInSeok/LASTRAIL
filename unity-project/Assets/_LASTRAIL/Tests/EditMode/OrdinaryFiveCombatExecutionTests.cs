using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryFiveCombatExecutionTests {
 static (OrdinaryFiveStartInput Input,string Receipt) Original(){
  var fixture=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json"));
  return(OrdinaryFiveStartInput.Read((JObject)fixture["originalStart"],(string)fixture["accountId"],(string)fixture["runId"]),fixture["initializationResponse"].ToString());
 }
 static OrdinaryFiveCombatFactory Factory(){var s=Original();return OrdinaryFiveCombatFactory.FromInitialization(s.Input,s.Receipt,500000L);}
 [Test] public void OriginalFiveBindActualAssetsAndDisplayPreparation(){
  var factory=Factory();var driver=new CombatSceneDriver(factory.Create,()=>null,(state,revision)=>revision+1,()=>true);driver.Open();
  var root=new UnityEngine.GameObject("OrdinaryView",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
  try{
   var view=root.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   Assert.AreEqual(5,CombatFormationLayout.Positions(driver.HudState).Count);
   Assert.AreEqual("전투 진입 정보 대기 중",root.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t=>t.name=="Notice").text);
   var charges=root.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(t=>t.name=="ChargePercent").ToArray();Assert.AreEqual(5,charges.Length);Assert.IsTrue(charges.All(t=>t.text=="미확인"));
   Assert.IsTrue(driver.HudState["ultimates"].All(r=>r["charge"].Type==JTokenType.Null));Assert.IsTrue(driver.HudState["skills"].All(r=>r["cooldownMs"].Type==JTokenType.Null));
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public void OriginalFiveHaveDistinctKitHudAndSeparateTrain(){
  var factory=Factory();var model=factory.Create();var hud=model.ReadHudState();var actors=(JArray)hud["actors"];
  Assert.AreEqual(6,actors.Count);Assert.AreEqual(1,actors.Count(a=>(bool)a["train"]));
  CollectionAssert.AreEquivalent(factory.Original.Actors.Select(a=>a.OperatorId),actors.Where(a=>!(bool)a["train"]).Select(a=>(string)a["id"]));
  CollectionAssert.AreEquivalent(new[]{"SHIELD_AND_TAUNT","DIRECT_DAMAGE","SENTRY_CREATE","HEAL_ONE","FIXED_TARGET_BURST"},hud["skills"].Select(a=>(string)a["effect"]));
  Assert.AreEqual(5,((JArray)hud["ultimates"]).Count);Assert.AreEqual("UNAVAILABLE",(string)hud["manualsAvailability"]);
  Assert.AreEqual(3,actors.Count(a=>(int?)a["physicalOrder"]==1));Assert.AreEqual(2,actors.Count(a=>(int?)a["physicalOrder"]==2));
 }
 [Test] public void UnreadyPlayerFramesDoNotAccumulateTimeOperationsOrWrites(){
  var factory=Factory();var first=factory.Create();var saved=first.CaptureLocalState();int writes=0;
  var driver=new CombatSceneDriver(factory.Create,()=>null,(state,revision)=>{writes++;saved=state.CaptureLocalState();return revision+1;},()=>true);
  driver.Open();writes=0;for(int i=0;i<200;i++){driver.Advance(166667,8);Assert.AreEqual("AWAITING_ENCOUNTER",(string)driver.HudState["executionPhase"]);}
  Assert.AreEqual(0,driver.Tick);Assert.AreEqual(0,writes);Assert.AreEqual(first.CaptureLocalState(),driver.StateJson);
  Assert.AreEqual("REJECTED_PREPARATION",driver.RequestSkill("kyle","op_kyle","op_kyle.active"));Assert.AreEqual(0,writes);
 }
 [Test] public void ArrivalCannotSubstituteForMissingNativeGeometry(){
  // Existing synthetic ARRIVE fixture validates intake only; this is not product admission evidence.
  var f=FirstPlayableScalingTests.Fixture(staticSource:true);
  var source=OrdinaryFiveStartInput.Read(f.Start,"a",(string)f.Start["runId"]);
  var scaling=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).Scaling;
  var factory=OrdinaryFiveCombatFactory.FromInitialization(source,f.Init.ToString(),500000L,scaling);var model=factory.Create();var before=model.CaptureLocalState();
  model.Advance(10000000,20);Assert.AreEqual(before,model.CaptureLocalState());
  var hud=model.ReadHudState();Assert.AreEqual("AWAITING_NATIVE_GEOMETRY",(string)hud["executionPhase"]);Assert.IsFalse((bool)hud["fullCombatReady"]);
  CollectionAssert.AreEquivalent(scaling.IssuedGunnerIds,hud["actors"].Where(a=>(bool)a["enemy"]).Select(a=>(string)a["id"]));
  Assert.AreEqual("REJECTED_PREPARATION",model.RequestSkill("shield","op_kyle","op_kyle.active"));
  Assert.IsTrue(hud["actors"].All(a=>(int)a["shield"]==0));Assert.IsEmpty((JArray)hud["casts"]);
 }
 [Test] public void PreparationPauseAndOwnerStateRestoreExactlyWithoutOldSchemaUpgrade(){
  var factory=Factory();var model=factory.Create();model.SetPaused(true);model.SetSpeed(4);model.SetUltimateAuto(true);
  var saved=model.CaptureLocalState();var restored=BasicCombatSimulation.RestoreLocalState(saved,factory.Create);
  Assert.AreEqual(saved,restored.CaptureLocalState());Assert.IsTrue(JToken.DeepEquals(model.ReadHudState(),restored.ReadHudState()));
  var altered=JObject.Parse(saved);altered["actors"][0]["vitals"]["hp"]=1;
  Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(altered.ToString(),factory.Create));
  var source=Original();var historical=OrdinaryFiveSeraFactory.FromInitialization(source.Input,source.Receipt,500000L);
  Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(saved,historical.Create));
  Assert.AreEqual(historical.Create().CaptureLocalState(),BasicCombatSimulation.RestoreLocalState(historical.Create().CaptureLocalState(),historical.Create).CaptureLocalState());
 }
}
