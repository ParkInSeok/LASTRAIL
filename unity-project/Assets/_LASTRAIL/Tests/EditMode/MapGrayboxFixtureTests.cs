#if UNITY_EDITOR
using System;
using LastRail.Qa;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using System.Linq;
namespace LastRail.Tests.EditMode {
 public sealed class MapGrayboxFixtureTests {
  [Test] public void FixtureGraphicsProvideNativeRenderer(){var g=new GameObject("fixture",typeof(RectTransform));try{g.AddComponent<MapGrayboxFixtureGraphic>();Assert.That(g.GetComponent<CanvasRenderer>(),Is.Not.Null,"Custom graphics need an explicit CanvasRenderer");}finally{UnityEngine.Object.DestroyImmediate(g);}}
  [UnityTest] public IEnumerator SavedSceneRunsAndOwnsBindingsInPlayMode(){
   EditorSceneManager.OpenScene(MapGrayboxFixtureExport.ScenePath,OpenSceneMode.Single);yield return new EnterPlayMode();
   var c=UnityEngine.Object.FindFirstObjectByType<MapGrayboxFixtureController>();Assert.That(c,Is.Not.Null);Assert.That(c.Model,Is.Not.Null);Assert.That(c.BindingCount,Is.EqualTo(16));c.Bind();c.Bind();
   var camera=GameObject.Find("FixtureCamera").GetComponent<Camera>();var events=GameObject.Find("FixtureEventSystem").GetComponent<UnityEngine.EventSystems.EventSystem>();
   var module=events.GetComponent<InputSystemUIInputModule>();Assert.That(module,Is.Not.Null);Assert.That(module.point.action.enabled,Is.True);
   var originalSettings=InputSystem.settings;var fixtureSettings=UnityEngine.Object.Instantiate(originalSettings);InputSystem.settings=fixtureSettings;fixtureSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;fixtureSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
   var mouse=InputSystem.AddDevice<Mouse>();
   try{
    var button=c.GetComponentsInChildren<Button>().Single(b=>b.name=="Step");var rect=(RectTransform)button.transform;var position=(Vector2)camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));
    Canvas.ForceUpdateCanvases();InputSystem.QueueStateEvent(mouse,new MouseState{position=position});InputSystem.Update();module.Process();yield return null;
    InputSystem.QueueStateEvent(mouse,new MouseState{position=position}.WithButton(MouseButton.Left,true));InputSystem.Update();module.Process();Debug.Log("MAP_INPUT down mouse="+mouse.leftButton.ReadValue()+" action="+module.leftClick.action.ReadValue<float>()+" point="+module.point.action.ReadValue<Vector2>()+" expected="+position+" module="+events.currentInputModule+" raycast="+module.GetLastRaycastResult(mouse.deviceId).gameObject+" focused="+events.isFocused);yield return null;
    InputSystem.QueueStateEvent(mouse,new MouseState{position=position});InputSystem.Update();module.Process();yield return null;
    Assert.That(c.CommandCount,Is.EqualTo(1),"Input System mouse press/release must activate the native button");Assert.That(c.Model.Tick,Is.EqualTo(1));
   }finally{InputSystem.RemoveDevice(mouse);InputSystem.settings=originalSettings;UnityEngine.Object.Destroy(fixtureSettings);}
   c.LoadScenario();
   MapGrayboxFixtureExport.Click(c,camera,events,"Step40");MapGrayboxFixtureExport.Click(c,camera,events,"Direct");Assert.That(c.Model.Route,Is.EqualTo(0));Assert.That(c.CommandCount,Is.EqualTo(2));
   c.Unbind();MapGrayboxFixtureExport.Click(c,camera,events,"Bypass");Assert.That(c.CommandCount,Is.EqualTo(2));c.Bind();MapGrayboxFixtureExport.Click(c,camera,events,"Bypass");Assert.That(c.CommandCount,Is.EqualTo(3));Assert.That(c.Model.Route,Is.EqualTo(1));
   foreach(var graphic in c.GetComponentsInChildren<MapGrayboxFixtureGraphic>()){var mesh=graphic.canvasRenderer.GetMesh();Assert.That(mesh.vertexCount,Is.GreaterThan(0),graphic.name);}
   yield return new ExitPlayMode();
  }
  [Test] public void ObservedPlanAndServerTraceStatesMatch(){var report=MapGrayboxFixtureReplay.RunAll();Assert.That((int)report["geometryModules"],Is.EqualTo(6));Assert.That((int)report["planStepAttempts"],Is.EqualTo(9900));Assert.That((bool)report["productionReady"],Is.False);}
  [Test] public void WarningAndLockRespectInputBeforeStep(){var r=new MapGrayboxFixtureRun(new MapGrayboxFixtureGeometry());for(int i=0;i<39;i++)r.Step();Assert.That(r.Reserve(0),Is.False);r.Step();Assert.That(r.Reserve(0),Is.True);for(int i=40;i<79;i++)r.Step();Assert.That(r.Reserve(1),Is.True);Assert.That(r.Reserve(null),Is.True);r.Step();Assert.That(r.Reserve(0),Is.False);Assert.That(r.Route,Is.EqualTo(1));}
  [Test] public void OnsetBoundaryReproducedButNotResolved(){var r=new MapGrayboxFixtureRun(new MapGrayboxFixtureGeometry(),401);for(int i=0;i<40;i++)r.Step();r.Reserve(0);for(int i=40;i<441;i++)r.Step();Assert.That((int)r.Capture()["state"]["hits"],Is.EqualTo(1));Assert.That(r.Distance-100000,Is.EqualTo(341000));Assert.That(MapGrayboxFixtureRun.Scope,Does.Contain("UNRESOLVED"));}
  [Test] public void TypedCheckpointRestoresOnlyItsOwnerAndCopiesState(){var g=new MapGrayboxFixtureGeometry();var r=new MapGrayboxFixtureRun(g);for(int i=0;i<45;i++)r.Step();r.Reserve(0);var checkpoint=r.Checkpoint();var before=r.Capture();for(int i=0;i<400;i++)r.Step();r.Restore(checkpoint);MapGrayboxFixtureReplay.Equal(r.Capture(),before,"trusted restore");Assert.Throws<ArgumentException>(()=>new MapGrayboxFixtureRun(g).Restore(checkpoint));}
  [Test] public void PauseFreezeAndInvalidInputCannotPoisonState(){var r=new MapGrayboxFixtureRun(new MapGrayboxFixtureGeometry());r.Pause(true);var before=r.Capture();for(int i=0;i<100;i++)r.Step();MapGrayboxFixtureReplay.Equal(r.Capture(),before,"pause");Assert.Throws<ArgumentException>(()=>r.Throttle(0));Assert.Throws<ArgumentOutOfRangeException>(()=>r.DamageRock(-1));MapGrayboxFixtureReplay.Equal(r.Capture(),before,"bad input");}
 }
}
#endif
