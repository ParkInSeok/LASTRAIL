#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public sealed class BOrdinaryLiveQaTests {
  const string Root="D:/Project/LASTRAIL/";
  internal static JObject Fixture(){var p=BInterceptSourceTests.Sample();var b=BOrdinaryGeometryTests.Stress(p);var g=BOrdinaryGeometryTests.Geometry(b);var profile=(JObject)JObject.Parse(File.ReadAllText(Root+"QA/CommanderS1-20260922/c3-owned-profile-observed-sql-test-20261001.json"))["profile"];profile["accountId"]="a";var stat=((JArray)profile["operators"]).Single(x=>(string)x["operatorId"]=="op_kyle")["permanentStats"];stat["attack"]=new JObject{["numerator"]="50000",["denominator"]="1"};stat["maxHp"]=1000000;stat["basicIntervalMs"]=10;Assert.AreEqual((string)b.Composition.Attack.Capture()["ownedProfileHash"],BDirectAttackInput.Digest(profile));return new JObject{["version"]="unity.combat-b.live-qa-fixture.v1.20261004",["provenance"]="EXPLICIT_LOCAL_TEST_NOT_ADMITTED_RUN",["timingSource"]=p,["trust"]=BInterceptSourceTests.Trust(p),["buffDefinitionsJson"]=File.ReadAllText(Root+"QA/CombatBMigration-20261002/BuffConsumer/original-definitions.json"),["profile"]=profile,["definitionIntake"]=JObject.Parse(File.ReadAllText(Root+"QA/CommanderS1-20260922/c3-definition-intake-20261001.json")),["attack"]=b.Composition.Attack.Capture(),["composition"]=b.Composition.Capture(),["routes"]=b.Capture(),["geometryDefinition"]=g.Definition.Capture(),["geometryInstances"]=g.Capture()};}
  [Test] public void ExportImmutableLiveQaSource(){var p=Fixture();File.WriteAllText(Root+"unity-project/Assets/_LASTRAIL/QA/CombatBGeometry/ordinary-b-live-fixture-v1.json",p.ToString());}
  [Test] public void LiveControllerMustExposeNativeRunPauseRestartAndDisposal(){var p=Fixture();using var c=BOrdinaryLiveQaController.Read(p,BDirectAttackInput.Digest(p));Assert.AreEqual("READY",c.Outcome);c.Start();c.Advance(1000000,false);Assert.AreEqual(50,c.Tick);c.SetPaused(true);var before=c.Capture();c.Advance(10000000,true);Assert.IsTrue(JToken.DeepEquals(before,c.Capture()));c.SetPaused(false);c.Advance(1000000,false);Assert.AreEqual(100,c.Tick);c.Restart();Assert.AreEqual(0,c.Tick);Assert.AreEqual(1000000,c.Hp);c.Dispose();Assert.Throws<ObjectDisposedException>(()=>c.Capture());Assert.Throws<ObjectDisposedException>(()=>c.Restart());}
  [Test] public void LiveFixtureTamperCannotCreateQaOwner(){var p=Fixture();string hash=BDirectAttackInput.Digest(p);p["profile"]["accountId"]="foreign";Assert.Throws<ArgumentException>(()=>BOrdinaryLiveQaController.Read(p,hash));Assert.Throws<ArgumentException>(()=>BOrdinaryLiveQaController.Read(p,BDirectAttackInput.Digest(p)));}
  [Test] public void CreateStandaloneLiveQaScene(){ExportImmutableLiveQaSource();AssetDatabase.Refresh();var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(string.IsNullOrEmpty(previous.path))EditorSceneManager.SaveScene(previous,"Assets/_LASTRAIL/QA/CombatBGeometry/qa-runner-origin-v02.unity");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);try{UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);var bootstrap=new GameObject("Ordinary_B_LOCAL_Live_QA").AddComponent<BOrdinaryLiveQaBootstrap>();bootstrap.fixture=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LASTRAIL/QA/CombatBGeometry/ordinary-b-live-fixture-v1.json");bootstrap.fixtureHash=BDirectAttackInput.Digest(JObject.Parse(bootstrap.fixture.text));Assert.IsTrue(EditorSceneManager.SaveScene(scene,"Assets/_LASTRAIL/QA/CombatBGeometry/ordinary-b-live-qa-v1.unity"));}finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
  [Test] public void HighFrameRateDoesNotConsumeNativeCommandBudget(){var p=Fixture();using var c=BOrdinaryLiveQaController.Read(p,BDirectAttackInput.Digest(p));c.Start();for(int i=0;i<24000;i++)c.SubmitElapsed(1000,false);Assert.AreEqual(1200,c.Tick);Assert.AreEqual(24,((JArray)c.Capture()["commands"]).Count);c.SetPaused(true);for(int i=0;i<5000;i++)c.SubmitElapsed(1000,false);Assert.AreEqual(1200,c.Tick);}
  static void CapturePlay(string name){string folder=Root+"QA/CombatBMigration-20261004/OrdinaryGeometry/live-play-v1";Directory.CreateDirectory(folder);var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();var previous=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(1280,720,24);var png=new Texture2D(1280,720,TextureFormat.RGB24,false);try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;png.ReadPixels(new Rect(0,0,1280,720),0,0);png.Apply();File.WriteAllBytes(folder+"/"+name+".png",png.EncodeToPNG());}finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(png);}}
  [UnityTest] public IEnumerator PlaySceneRunsOriginalNativeAndDisposesOnExit(){
   CreateStandaloneLiveQaScene();
   EditorSceneManager.OpenScene("Assets/_LASTRAIL/QA/CombatBGeometry/ordinary-b-live-qa-v1.unity");
   yield return new EnterPlayMode();
   var bootstrap=UnityEngine.Object.FindFirstObjectByType<BOrdinaryLiveQaBootstrap>();
   Assert.NotNull(bootstrap);Assert.IsNull(bootstrap.Failure);Assert.NotNull(bootstrap.Controller,"initial owner");
   bootstrap.StartQa();
   for(int i=0;i<20;i++)yield return null;
   Assert.NotNull(bootstrap.Controller,"owner after rendered frames");
   Assert.Greater(bootstrap.Controller.Tick,0,"Play Update must advance original native clock");
   bootstrap.Controller.SetPaused(true);long paused=bootstrap.Controller.Tick;
   for(int i=0;i<10;i++)yield return null;
   Assert.AreEqual(paused,bootstrap.Controller.Tick);var unchanged=bootstrap.Controller.Capture();foreach(string name in new[]{"body","roof","paths","actors","projectiles","labels"}){var group=bootstrap.transform.Find("live_native_QA_presentation/independent_"+name);Assert.NotNull(group);group.gameObject.SetActive(false);Assert.IsTrue(JToken.DeepEquals(unchanged,bootstrap.Controller.Capture()));group.gameObject.SetActive(true);}bootstrap.Controller.SetPaused(false);
   int n=0;bool flightCaptured=false;
   while(bootstrap.Controller.Outcome=="RUNNING"&&n++<700){bootstrap.Controller.Advance(1000000,true);if(!flightCaptured&&bootstrap.Controller.Capture()["gunnerIntercept"]["projectiles"].Any(p=>(string)p["terminal"]=="FLYING")){bootstrap.Controller.SetPaused(true);yield return null;Assert.IsTrue(bootstrap.Controller.Capture()["gunnerIntercept"]["projectiles"].Any(p=>(string)p["terminal"]=="FLYING"));CapturePlay("play-flight");File.WriteAllText(Root+"QA/CombatBMigration-20261004/OrdinaryGeometry/live-play-v1/native-play-flight.json",bootstrap.Controller.Capture().ToString());flightCaptured=true;bootstrap.Controller.SetPaused(false);}if(n%30==0)yield return null;}
   Assert.AreEqual("VICTORY",bootstrap.Controller.Outcome);Assert.IsNull(bootstrap.Failure);
   var end=bootstrap.Controller.Capture();
   Assert.IsTrue(flightCaptured);yield return null;CapturePlay("play-victory");
   Assert.AreEqual(2,end["waveEntry"]["events"].Count(x=>(string)x["kind"]=="WAVE_CLEAR"));
   string folder=Root+"QA/CombatBMigration-20261004/OrdinaryGeometry/live-play-v1";Directory.CreateDirectory(folder);
   File.WriteAllText(folder+"/native-play-final.json",end.ToString());
   bootstrap.RestartQa();Assert.AreEqual(0,bootstrap.Controller.Tick);
   var disposed=bootstrap.Controller;
   bootstrap.enabled=false;yield return null;
   Assert.IsNull(bootstrap.Controller);bool rejected=false;try{disposed.Capture();}catch(ObjectDisposedException){rejected=true;}Assert.IsTrue(rejected,"Released owner must reject observation");
   Assert.AreEqual(0,bootstrap.GetComponents<BOrdinaryLiveQaView>().Length);
   bootstrap.enabled=true;yield return null;
   Assert.IsNull(bootstrap.Failure);Assert.NotNull(bootstrap.Controller,"re-enabled owner");
   Assert.AreEqual("READY",bootstrap.Controller.Outcome);Assert.AreEqual(1,bootstrap.GetComponents<BOrdinaryLiveQaView>().Length);
   yield return new ExitPlayMode();
  }
 }
}
#endif
