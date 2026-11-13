using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class ReinaPerformanceTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 [Test] public void CommittedChangesRenderWithoutPollingAndExplicitRefreshReusesProjection(){
  var input=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CutsceneProduction-20261020/reina/runway-final-v01/normal-contact-input.json"));
  var driver=CombatSceneDriver.ForOrdinaryNode(OrdinaryLiveExecutionTests.CreateFactory(input),()=>true);driver.Open();
  var host=new GameObject("Refresh ownership",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
  try{
   var view=host.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/OrdinaryPresentation"));
   var state=typeof(ApprovedCombatView).GetField("reinaDisplayState",Hidden);var first=state.GetValue(view);
   Canvas.ForceUpdateCanvases();Assert.AreSame(first,state.GetValue(view),"An unchanged render must not rebuild the HUD.");
   driver.SetRailThrottle(7000);Assert.AreNotSame(first,state.GetValue(view),"Commit must deliver without another frame.");
   var next=state.GetValue(view);view.Refresh();Canvas.ForceUpdateCanvases();Assert.AreSame(next,state.GetValue(view));
  }finally{driver.Retire();UnityEngine.Object.DestroyImmediate(host);}
 }
 [Test] public void MeasureSameOrdinaryBattleStateAndView(){
  var output=Environment.GetEnvironmentVariable("LASTRAIL_REINA_PROFILE");
  if(string.IsNullOrEmpty(output))Assert.Ignore("Explicit local profiling output required");
  Assert.True(Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase));
  const string root="D:/Project/LASTRAIL/QA/CutsceneProduction-20261020/reina/runway-final-v01/";
  var input=JObject.Parse(File.ReadAllText(root+"normal-contact-input.json"));
  var factory=OrdinaryLiveExecutionTests.CreateFactory(input);
  factory=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithLocalUninstalledInterceptor",Hidden).Invoke(factory,null);
  factory=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithAugmentOwnership",Hidden).Invoke(factory,new object[]{input["augmentOwnership"]});
  var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();driver.SetRailThrottle(7000);driver.SetUltimateAuto(true);
  var host=new GameObject("Reina performance measurement",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));var samples=new JArray();
  var clone=typeof(BasicCombatSimulation).GetMethod("CloneOwnedForScene",Hidden);
  var retire=typeof(BasicCombatSimulation).GetMethod("RetireOrdinaryNativeOwners",Hidden);
  var field=typeof(CombatSceneDriver).GetField("current",Hidden);
  void Measure(string name,Action action){long bytes=GC.GetAllocatedBytesForCurrentThread();var watch=System.Diagnostics.Stopwatch.StartNew();action();watch.Stop();samples.Add(new JObject{["name"]=name,["tick"]=driver.Tick,["ms"]=watch.Elapsed.TotalMilliseconds,["bytes"]=GC.GetAllocatedBytesForCurrentThread()-bytes});}
  try{
   while(driver.Tick<1300)driver.Advance(500000*20,20);
   var view=host.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/OrdinaryPresentation"));view.UseLandscapeLayout();
   for(int i=0;i<24;i++){
    var current=(BasicCombatSimulation)field.GetValue(driver);
    Measure("factory",()=>retire.Invoke(factory.Create(),null));
    Measure("clone",()=>retire.Invoke(clone.Invoke(current,new object[]{(Func<BasicCombatSimulation>)(typeof(CombatSceneDriver).GetField("sceneCopyFactory",Hidden)?.GetValue(driver)??(Func<BasicCombatSimulation>)factory.Create),null,false}),null));
    Measure("advance",()=>driver.Advance(500000,1));
    Measure("hud",()=>{var hud=driver.HudState;Assert.AreEqual(driver.Tick,(long)hud["tick"]);});
    Measure("view",view.Refresh);
    var hud=driver.HudState;var roofMethod=typeof(ApprovedCombatView).GetMethod("RefreshOrdinaryRoofEnemies",Hidden);
    var roofField=typeof(ApprovedCombatView).GetField("ordinaryRoof",Hidden);var roof=roofField.GetValue(view);
    typeof(OrdinaryRoofPresentation).GetField("enemyState",Hidden).SetValue(roof,null);
    Measure("roofForced",()=>roofMethod.Invoke(view,new object[]{hud}));
    Measure("viewUnchanged",view.Refresh);
   }
   File.WriteAllText(output,new JObject{["scope"]="EDITOR_SAME_INPUT_TICKS_1300_1324_NOT_PLAYER_FPS",["samples"]=samples,["state"]=JObject.Parse(driver.StateJson)}.ToString());
  }finally{driver.Retire();UnityEngine.Object.DestroyImmediate(host);}
 }
}
