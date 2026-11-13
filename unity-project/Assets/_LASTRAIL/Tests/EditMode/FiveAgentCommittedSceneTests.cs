#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public class FiveAgentCommittedSceneTests {
  static object Source(CombatSceneDriver driver)=>Activator.CreateInstance(FiveAgentVisualReferenceTests.Type("FiveAgentCommittedSceneSource"),driver);
  static object Call(object value,string name,params object[] args)=>FiveAgentVisualReferenceTests.Call(value,name,args);
  static JObject Capture(object sample)=>(JObject)Call(sample,"Capture");
  [Test]public void ActualSceneDriverObservationPreservesCommittedStateRevisionAndAttackFacts(){
   int saves=0;var driver=new CombatSceneDriver(global::CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>{saves++;return r+1;},()=>true);driver.Open();
   for(int i=0;i<200&&((JArray)JObject.Parse(driver.StateJson)["attacks"]).Count==0;i++)driver.Advance(500000,1);
   var raw=driver.CaptureCommittedState();var source=Source(driver);int originalSaves=saves;
   var sample=Capture(Call(source,"Observe"));var original=JObject.Parse(raw.StateJson);
   Assert.That((string)sample["classification"],Is.EqualTo("COMMITTED_BASIC_SCENE_NOT_B_NATIVE_OR_PHASE_MAPPING"));
   Assert.That((long)sample["revision"],Is.EqualTo(raw.Revision));Assert.That(JToken.DeepEquals(sample["originalState"],original),Is.True);
   Assert.That(((JArray)sample["originalState"]["attacks"]).Count,Is.GreaterThan(0));
   sample["originalState"]["clock"]["completed"]=999999;
   Assert.That(driver.StateJson,Is.EqualTo(raw.StateJson));Assert.That(saves,Is.EqualTo(originalSaves));
   Assert.That((long)Capture(Call(source,"Observe"))["tick"],Is.EqualTo(driver.Tick));
  }
  [Test]public void ClockPortUsesActualPausedClockAndDoesNotAdvanceOrSaveCombat(){
   int saves=0;var driver=new CombatSceneDriver(global::CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>{saves++;return r+1;},()=>true);driver.Open();driver.Advance(500000*3,3);
   var source=Source(driver);var feed=new FiveAgentVisualFeed();bool hold=false;long ms=-1;feed.Hold+=v=>hold=v;feed.Clock+=v=>ms=v;
   var before=driver.StateJson;int count=saves;var sample=Capture(Call(source,"PublishClock",feed));
   var clock=JObject.Parse(before)["clock"];Assert.That(ms,Is.EqualTo((long)clock["completed"]*(long)clock["step"]/TimeSpan.TicksPerMillisecond));Assert.That(hold,Is.False);Assert.That(driver.StateJson,Is.EqualTo(before));Assert.That(saves,Is.EqualTo(count));
   driver.SetPaused(true);Call(source,"PublishClock",feed);Assert.That(hold,Is.True);Assert.That((bool)Capture(Call(source,"Observe"))["paused"],Is.True);
   driver.SetPaused(false);Call(source,"PublishClock",feed);Assert.That(hold,Is.False);
  }
  [Test]public void FailedSaveOrLostOwnerCannotPublishCandidateClock(){
   bool fail=false,owner=true;var driver=new CombatSceneDriver(global::CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>fail?throw new IOException("fixture failed save"):r+1,()=>owner);driver.Open();var source=Source(driver);var feed=new FiveAgentVisualFeed();int clocks=0;bool held=false;feed.Clock+=_=>clocks++;feed.Hold+=v=>held=v;
   Call(source,"PublishClock",feed);var before=driver.StateJson;fail=true;Assert.Throws<IOException>(()=>driver.Advance(500000,1));
   Assert.Throws<InvalidOperationException>(()=>Call(source,"PublishClock",feed));Assert.That(clocks,Is.EqualTo(1));Assert.That(held,Is.True);Assert.That(driver.StateJson,Is.EqualTo(before));
   fail=false;driver.Open();owner=false;Assert.Throws<InvalidOperationException>(()=>Call(source,"Observe"));
  }
  static bool Project(RectTransform surface,Transform target,out Vector2 point){var args=new object[]{surface,target,null};bool result=(bool)FiveAgentVisualReferenceTests.Type("FiveAgentVisualAnchorProjection").GetMethod("TryProject").Invoke(null,args);point=(Vector2)args[2];return result;}
  [TestCase(RenderMode.ScreenSpaceOverlay)][TestCase(RenderMode.ScreenSpaceCamera)][TestCase(RenderMode.WorldSpace)]public void BorrowedCanvasAndAnchorProduceLocalTopLeftCoordinatesWithoutNewCamera(RenderMode mode){
   var root=new GameObject("BorrowedActualCanvas",typeof(RectTransform),typeof(Canvas));var camGo=new GameObject("BorrowedCamera",typeof(Camera));var anchor=new GameObject("BorrowedAnchor");
   try{var rect=root.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(941,1672);var canvas=root.GetComponent<Canvas>();canvas.renderMode=mode;var camera=camGo.GetComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=836;if(mode!=RenderMode.ScreenSpaceOverlay)canvas.worldCamera=camera;
    Canvas.ForceUpdateCanvases();int cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;float fov=camera.fieldOfView,ortho=camera.orthographicSize;
    anchor.transform.position=rect.TransformPoint(new Vector3(rect.rect.xMin+200,rect.rect.yMax-300,0));
    Assert.That(Project(rect,anchor.transform,out var p),Is.True);Assert.That(p.x,Is.EqualTo(200).Within(.02));Assert.That(p.y,Is.EqualTo(300).Within(.02));
    anchor.transform.position=rect.TransformPoint(new Vector3(rect.rect.xMin+237,rect.rect.yMax-311,0));Assert.That(Project(rect,anchor.transform,out p),Is.True);Assert.That(p.x,Is.EqualTo(237).Within(.02));Assert.That(p.y,Is.EqualTo(311).Within(.02));
    Assert.That(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length,Is.EqualTo(cameras));Assert.That(camera.fieldOfView,Is.EqualTo(fov));Assert.That(camera.orthographicSize,Is.EqualTo(ortho));
   }finally{UnityEngine.Object.DestroyImmediate(anchor);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(camGo);}
  }
  [Test]public void NestedRotatedScaledSurfaceUsesBorrowedCanvasBasis(){
   var root=new GameObject("ActualOverlay",typeof(RectTransform),typeof(Canvas));var surfaceObject=new GameObject("NestedSurface",typeof(RectTransform));var anchor=new GameObject("BorrowedNestedAnchor");
   try{root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var surface=surfaceObject.GetComponent<RectTransform>();surface.SetParent(root.transform,false);surface.sizeDelta=new Vector2(400,600);surface.localPosition=new Vector3(27,-39,0);surface.localScale=new Vector3(1.7f,.8f,1);surface.localRotation=Quaternion.Euler(0,0,18);Canvas.ForceUpdateCanvases();
    anchor.transform.position=surface.TransformPoint(new Vector3(surface.rect.xMin+90,surface.rect.yMax-110,0));
    Assert.That(Project(surface,anchor.transform,out var p),Is.True);Assert.That(p.x,Is.EqualTo(90).Within(.02));Assert.That(p.y,Is.EqualTo(110).Within(.02));
    var original=anchor.transform;UnityEngine.Object.DestroyImmediate(anchor);Assert.That(Project(surface,original,out _),Is.False);
   }finally{if(anchor!=null)UnityEngine.Object.DestroyImmediate(anchor);UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test]public void MissingCameraBehindCameraAndInactiveAnchorRejectWithoutFallback(){
   var root=new GameObject("ExplicitWorldCanvas",typeof(RectTransform),typeof(Canvas));var camGo=new GameObject("ActualCamera",typeof(Camera));var anchor=new GameObject("OriginalAnchor");
   try{var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;var rect=root.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(941,1672);
    Assert.That(Project(rect,anchor.transform,out _),Is.False);var camera=camGo.GetComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);canvas.worldCamera=camera;
    anchor.transform.position=new Vector3(0,0,-11);Assert.That(Project(rect,anchor.transform,out _),Is.False);
    anchor.transform.position=Vector3.zero;anchor.SetActive(false);Assert.That(Project(rect,anchor.transform,out _),Is.False);
   }finally{UnityEngine.Object.DestroyImmediate(anchor);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(camGo);}
  }
 }
}
#endif
