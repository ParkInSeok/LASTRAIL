using System;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class CombatDangerAudioTests {
 static JObject State()=>new JObject{["tick"]=0,["outcome"]="Running",["paused"]=false};
 static JObject Obstacle(){var s=State();s["fullCombatReady"]=true;s["ordinaryObstacle"]=new JObject{["accountId"]="a",["runId"]="r",["nodeId"]="n",["attemptGeneration"]=1,["snapshotHash"]="h",["phase"]="OBSTACLE"};s["ordinaryObstacleExecution"]=new JObject{["phase"]="WARNING"};return s;}
 [Test] public void NativeWarningStartsOnceAfterPreparationAndEndsWithActualPhase(){
  var fixture=((object Owner,OrdinaryFiveCombatFactory Factory))typeof(OrdinaryNodeObstacleExecutionTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
  JObject View(long at)=>(JObject)fixture.Owner.GetType().GetMethod("CaptureView",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fixture.Owner,new object[]{at});
  var s=fixture.Factory.Create().ReadHudState();s["fullCombatReady"]=true;s["ordinaryObstacleExecution"]=View(0);
  var f=new CombatDangerAudioFeedback();f.Seed(s);Assert.IsNull(f.Observe(s,true));
  s["tick"]=1;s["ordinaryObstacleExecution"]=View(50);Assert.AreEqual("WARNING",(string)s["ordinaryObstacleExecution"]["phase"]);
  Assert.AreEqual("obstacle",f.Observe(s,true));Assert.IsTrue(f.Active);Assert.IsNull(f.Observe(s,true));
  s["ordinaryObstacleExecution"]=View(30000);Assert.IsNull(f.Observe(s,true));Assert.IsFalse(f.Active);
 }
 [TestCase("muted")][TestCase("paused")][TestCase("late-bind")][TestCase("hidden")][TestCase("not-ready")]
 public void SuppressedOrPastWarningNeverReplays(string reason){
  var s=Obstacle();var f=new CombatDangerAudioFeedback();f.Seed(s);s["tick"]=1;
  if(reason=="paused")s["paused"]=true;if(reason=="not-ready")s["fullCombatReady"]=false;
  if(reason=="late-bind")f=new CombatDangerAudioFeedback();
  if(reason=="late-bind"||reason=="hidden")f.Seed(s);else Assert.IsNull(f.Observe(s,reason!="muted"));
  s["paused"]=false;s["fullCombatReady"]=true;Assert.IsNull(f.Observe(s,true));
 }
 [Test] public void DifferentAttemptCannotPlayThroughOldCursor(){
  var s=Obstacle();var f=new CombatDangerAudioFeedback();f.Seed(s);s["tick"]=1;s["ordinaryObstacle"]["attemptGeneration"]=2;
  Assert.IsNull(f.Observe(s,true));Assert.IsFalse(f.Active);f.Seed(s);Assert.IsNull(f.Observe(s,true));
  var fresh=new CombatDangerAudioFeedback();s["tick"]=0;fresh.Seed(s);s["tick"]=1;Assert.AreEqual("obstacle",fresh.Observe(s,true));
 }
 [Test] public void ActualArtillerySpawnWarnsOnceAndLaunchEndsWarning(){
  var owner=new CombatRailArtillery(CombatRailArtilleryTests.Config());var s=State();s["railArtillery"]=owner.Capture();var f=new CombatDangerAudioFeedback();f.Seed(s);
  owner.Advance(1,2,0,false);s["railArtillery"]=owner.Capture();Assert.AreEqual("artillery:gun-2",f.Observe(s,true));Assert.IsTrue(f.Active);Assert.IsNull(f.Observe(s,true));
  owner.Advance(11,2,0,false);s["railArtillery"]=owner.Capture();Assert.IsNull(f.Observe(s,true));Assert.IsFalse(f.Active);
  owner.Advance(51,2,0,false);s["railArtillery"]=owner.Capture();Assert.IsNull(f.Observe(s,true));Assert.IsFalse(f.Active);
 }
 [TestCase("late-bind")][TestCase("destroyed")][TestCase("passed")][TestCase("terminal")][TestCase("paused")]
 public void OldOrCancelledArtilleryDoesNotWarn(string reason){
  var owner=new CombatRailArtillery(CombatRailArtilleryTests.Config());var s=State();s["railArtillery"]=owner.Capture();var f=new CombatDangerAudioFeedback();f.Seed(s);
  owner.Advance(1,2,0,false);if(reason=="destroyed")owner.Damage("gun-2",1000);owner.Advance(2,2,reason=="passed"?120000:0,reason=="terminal");
  s["railArtillery"]=owner.Capture();if(reason=="paused")s["paused"]=true;if(reason=="late-bind")f.Seed(s);
  Assert.IsNull(f.Observe(s,true));s["paused"]=false;Assert.IsNull(f.Observe(s,true));
 }
 [Test] public void WarningReferencesOriginalWithAuthoredInitialGain(){var audio=CombatEffectAudio.LoadResources();Assert.AreEqual("shield-warning-local.v02.warning.20261021",audio.WarningClip.name);Assert.AreEqual(.85f,audio.WarningGain);}
 [Test] public void ViewCallbackReentryAndRetirementDoNotReplayWarning(){
  var prepared=JObject.Parse(System.IO.File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/ce03-frontline-policy-UtU7yJ/prepared.json"));
  BasicCombatSimulation Fresh()=>BasicCombatSimulation.FromPreparedProfileForLocalTest(prepared,(JObject)prepared["ownedSelection"],(string)prepared["ownedSelection"]["accountId"],1);
  var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  var go=new UnityEngine.GameObject("WarningLifecycle",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
  try{
   var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));int played=0;view.PresentationCue+=(kind,id)=>{if(kind=="WARNING")played++;};
   var owner=new CombatRailArtillery(CombatRailArtilleryTests.Config());owner.Advance(1,2,0,false);
   var state=driver.HudState;state["railArtillery"]=owner.Capture();
   const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
   // Feed a real artillery snapshot through the existing view callback without changing gameplay.
   typeof(CombatSceneDriver).GetField("displayState",hidden).SetValue(driver,state);
   void Notify()=>typeof(ApprovedCombatView).GetMethod("OwnerDisplayChanged",hidden).Invoke(view,null);
   Notify();Assert.AreEqual(1,played);Notify();Assert.AreEqual(1,played);
   var audio=(UnityEngine.AudioSource)typeof(ApprovedCombatView).GetField("dangerAudio",hidden).GetValue(view);Assert.NotNull(audio);
   go.SetActive(false);Assert.IsFalse(audio.isPlaying);Notify();Assert.AreEqual(1,played);
   go.SetActive(true);Notify();Assert.AreEqual(1,played);
   driver.Retire();Assert.IsFalse(audio.isPlaying);Notify();Assert.AreEqual(1,played);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
