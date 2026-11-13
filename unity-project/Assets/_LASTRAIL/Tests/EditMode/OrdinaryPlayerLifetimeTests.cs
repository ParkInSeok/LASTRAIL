using System;
using System.IO;
using System.Reflection;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
public sealed class OrdinaryPlayerLifetimeTests {
 [Test] public void NodeSceneCloseRetiresOwnerBeforeLateCallbacksAcrossThreeEntries(){
  var fixture=typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic);
  var f=((OrdinaryFiveStartInput Start,LastRail.Run.BRouteCommand Command,JObject Response))fixture.Invoke(null,null);
  var arrival=LastRail.Run.BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
  var factory=OrdinaryFiveCombatFactory.FromNodeStart(f.Start,arrival.NodeStart,arrival.Scaling,170000);
  var binding=LastRail.Resources.PreparedContent.Load<CombatPlayerAssetBinding>("CombatPresentation/OrdinaryBinding");
  var root=new GameObject("NodeLifetimeTest");var events=new GameObject("Input",typeof(EventSystem));int callbacks=0;
  try{
   var scene=root.AddComponent<CombatPlayerScene>();Action<CombatSceneResult> lateComplete=null;Action<Exception> lateFail=null;
   for(int i=0;i<3;i++){
    var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();
    using var controller=new OrdinaryPlayerBattleController(f.Start,driver,binding,()=>true);
    controller.Present(scene,events.GetComponent<EventSystem>(),()=>callbacks++,r=>callbacks++,e=>callbacks++);
    scene.SceneCamera.Render();Assert.IsFalse(scene.IsRunning,"Rendered preparation is not an executing battle");driver.SetPaused(true);string paused=driver.StateJson;driver.Advance(1000000,10);Assert.AreEqual(paused,driver.StateJson);Assert.IsTrue(controller.IsCurrent);
    lateComplete?.Invoke(null);lateFail?.Invoke(new Exception("previous attempt"));Assert.Zero(callbacks);Assert.AreEqual(paused,driver.StateJson);
    lateComplete=(Action<CombatSceneResult>)typeof(CombatPlayerScene).GetField("completed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(scene);
    lateFail=(Action<Exception>)typeof(CombatPlayerScene).GetField("failed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(scene);
    scene.Close();Assert.IsTrue(driver.IsRetired,"Closing the scene must invalidate the node execution owner");Assert.IsNull(driver.StateJson);Assert.IsFalse(controller.IsCurrent);
    Assert.Throws<InvalidOperationException>(()=>driver.SetSpeed(3));Assert.AreEqual(0,root.transform.childCount);
   }
   lateComplete(null);lateFail(new Exception("last attempt"));Assert.Zero(callbacks);
   var failedDriver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);failedDriver.Open();
   using var failedOwner=new OrdinaryPlayerBattleController(f.Start,failedDriver,binding,()=>true);
   failedOwner.Present(scene,events.GetComponent<EventSystem>(),()=>callbacks++,r=>callbacks++,e=>callbacks++);
   Assert.Throws<ArgumentOutOfRangeException>(()=>failedDriver.SetSpeed(1));Assert.IsTrue(failedDriver.RecoveryRequired);
   typeof(CombatPlayerScene).GetMethod("AdvanceFrame",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(scene,new object[]{0d});
   Assert.AreEqual(1,callbacks,"A failed command must notify and close even while execution is not ready");Assert.IsTrue(failedDriver.IsRetired);Assert.IsFalse(scene.IsOpen);
  }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(events);}
 }
 [Test] public void ClosedHostCannotNotifyOrReopenAndCannotCloseReplacement(){
  var source=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json"));
  var original=OrdinaryFiveStartInput.Read((JObject)source["originalStart"],(string)source["accountId"],(string)source["runId"]);
  var factory=OrdinaryFiveCombatFactory.FromInitialization(original,source["initializationResponse"].ToString(),500000);
  var driver=new CombatSceneDriver(factory.Create,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  var binding=LastRail.Resources.PreparedContent.Load<CombatPlayerAssetBinding>("CombatPresentation/OrdinaryBinding");Assert.NotNull(binding);
  var root=new GameObject("OrdinaryLifetimeTest");var events=new GameObject("Input",typeof(EventSystem));
  bool foreground=false;
  using var old=new OrdinaryPlayerBattleController(original,driver,binding,()=>true,()=>foreground);
  using var replacement=new OrdinaryPlayerBattleController(original,driver,binding,()=>true);
  try{
   var scene=root.AddComponent<CombatPlayerScene>();int callbacks=0;
   var pause=typeof(CombatPlayerScene).GetMethod("OnApplicationPause",BindingFlags.Instance|BindingFlags.NonPublic);pause.Invoke(scene,new object[]{true});
   old.Present(scene,events.GetComponent<EventSystem>(),()=>callbacks++,r=>callbacks++,e=>callbacks++);
   string initial=driver.StateJson;scene.SceneCamera.Render();Assert.IsFalse(scene.IsRunning);
   var advance=typeof(CombatPlayerScene).GetMethod("AdvanceFrame",BindingFlags.Instance|BindingFlags.NonPublic);
   var speed=scene.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="Speed");
   speed.onClick.Invoke();advance.Invoke(scene,new object[]{5d});Assert.AreEqual(initial,driver.StateJson);
   foreground=true;advance.Invoke(scene,new object[]{0d});Assert.IsFalse(scene.IsRunning);pause.Invoke(scene,new object[]{false});speed.onClick.Invoke();Assert.AreEqual(initial,driver.StateJson);
   scene.SceneCamera.Render();Assert.IsFalse(scene.IsRunning);Assert.AreEqual(initial,driver.StateJson);speed.onClick.Invoke();Assert.AreNotEqual(initial,driver.StateJson);
   var complete=(Action<CombatSceneResult>)typeof(CombatPlayerScene).GetField("completed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(scene);
   var fail=(Action<Exception>)typeof(CombatPlayerScene).GetField("failed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(scene);
   old.ClosePresentation();Assert.IsFalse(old.IsCurrent);Assert.IsFalse(scene.IsOpen);
   Assert.Throws<InvalidOperationException>(()=>old.Present(scene,events.GetComponent<EventSystem>(),null,null,null));
   replacement.Present(scene,events.GetComponent<EventSystem>(),null,null,null);
   complete(null);fail(new Exception("late"));Assert.AreEqual(0,callbacks);Assert.IsTrue(replacement.HasPresentation);
  }finally{replacement.ClosePresentation();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(events);}
 }
}
