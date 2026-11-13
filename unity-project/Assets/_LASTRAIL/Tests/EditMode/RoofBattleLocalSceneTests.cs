#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public class RoofBattleLocalSceneTests {
  [Test] public void RoofCompositionHasNoAlwaysRunningFrameCallbacks(){
   foreach(string name in new[]{"Update","LateUpdate"})Assert.That(SceneType().GetMethod(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic),Is.Null,"Only an owned active simulation or one-shot rebind may request frames.");
  }
  static Type SceneType(){var t=typeof(BDirectAttackInput).Assembly.GetType("LastRail.Presentation.RoofBattle.RoofBattleLocalScene");Assert.That(t,Is.Not.Null,"Roof LOCAL scene not integrated");return t;}
  static object Call(object o,string name,params object[] args)=>FiveAgentVisualReferenceTests.Call(o,name,args);
  static T Read<T>(object o,string name)=>(T)o.GetType().GetProperty(name).GetValue(o);
  static JObject State(object o)=>Read<JObject>(o,"Snapshot");
  static Component Create(GameObject go,out TextAsset text) {
   var raw=RoofBattleLocalStorageTests.SourceEnvelope();text=new TextAsset(raw.ToString());
   var c=go.AddComponent(SceneType());
   Call(c,"Initialize",text,BDirectAttackInput.Digest(raw),"test-roof-"+Guid.NewGuid().ToString("N"),
    AssetDatabase.LoadAssetAtPath<FiveAgentVisualAssets>("Assets/_LASTRAIL/UI/CombatAnimations/FiveAgentReferenceV02/ApprovedVisualAssets.asset"),
    AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),
    "D:/Project/LASTRAIL/QA/FiveAgentVisual-20261017/RoofSceneStore-"+Guid.NewGuid().ToString("N"));
   Call(c,"SetAutoAdvance",false);return c;
  }
  [UnityTearDown]public IEnumerator LeaveRuntime(){if(Application.isPlaying)yield return new ExitPlayMode();}
  [UnityTest] public IEnumerator PauseAndDisableStopAutomaticCommandsAndResumeUsesUnscaledTime(){
   yield return new EnterPlayMode();
   var go=new GameObject("RoofBoundedLifetime");TextAsset text=null;float scale=Time.timeScale;
   try{
    var scene=Create(go,out text);yield return null;yield return null;
    var view=Read<object>(scene,"View");Call(view,"QaClickPause");Call(scene,"SetAutoAdvance",true);
    long revision=(long)State(scene)["revision"];
    yield return null;yield return null;
    Assert.That((long)State(scene)["revision"],Is.EqualTo(revision),"Paused owner must receive no elapsed commands.");
    go.SetActive(false);yield return null;yield return null;
    Assert.That((long)State(scene)["revision"],Is.EqualTo(revision));
    go.SetActive(true);yield return null;yield return null;
    Assert.That((long)State(scene)["revision"],Is.EqualTo(revision),"Rebind must preserve pause.");
    Time.timeScale=0;Call(view,"QaClickPause");long tick=(long)State(scene)["tick"];
    yield return null;yield return null;
    Assert.That((long)State(scene)["tick"],Is.GreaterThan(tick),"Native speed/debt use unscaled elapsed time.");
   }finally{Time.timeScale=scale;UnityEngine.Object.Destroy(go);if(text!=null)UnityEngine.Object.Destroy(text);}
   yield return null;
  }
  [UnityTest] public IEnumerator AddingBasicRootAfterBindingStopsRoofBeforeAnotherAutomaticCommand(){
   yield return new EnterPlayMode();
   var go=new GameObject("RoofStructuralBoundary");TextAsset text=null;GameObject basic=null;
   try{
    var scene=Create(go,out text);yield return null;yield return null;
    Call(scene,"SetAutoAdvance",true);long revision=(long)State(scene)["revision"];
    LogAssert.Expect(LogType.Error,new Regex("Roof LOCAL command/source failure:.*Loaded active Basic/CE03",RegexOptions.Singleline));
    basic=new GameObject("LateBasicRoot",typeof(BattleSceneRoot));
    yield return null;yield return null;
    Assert.That((long)State(scene)["revision"],Is.EqualTo(revision));
    StringAssert.Contains("Loaded active Basic/CE03",Read<string>(scene,"LastError"));
   }finally{UnityEngine.Object.Destroy(go);if(basic!=null)UnityEngine.Object.Destroy(basic);if(text!=null)UnityEngine.Object.Destroy(text);}
   yield return null;
  }
  [UnityTest]public IEnumerator ActualUiUsesOneNativeOwnerAndRebindRestoreDoNotReplay(){
   yield return new EnterPlayMode();yield return RunUi();
  }
  static IEnumerator RunUi(){
   var go=new GameObject("ExplicitRoofLocalTest");TextAsset text=null;
   try{
    var scene=Create(go,out text);yield return null;yield return null;
    Assert.That(Read<bool>(scene,"Ready"),Is.True,Read<string>(scene,"LastError"));
    var view=Read<object>(scene,"View");
    var camera=go.GetComponentsInChildren<Camera>().Single();Assert.That(camera.cullingMask,Is.Zero);
    Assert.That(camera.clearFlags,Is.EqualTo(CameraClearFlags.SolidColor));
    Call(view,"QaClickAdvance");
    Assert.That(Read<bool>(scene,"LastCommandSucceeded"),Is.True);
    Assert.That((long)State(scene)["tick"],Is.EqualTo(1));Assert.That((long)State(scene)["cursor"],Is.EqualTo(2));
    Assert.That((int)State(scene)["hp"]["enemy"],Is.EqualTo(675));Assert.That(Read<int>(scene,"HitCount"),Is.EqualTo(1));
    Assert.That(Read<int>(view,"SpatialFxCount"),Is.Zero);Assert.That(Read<int>(view,"NeutralActorCount"),Is.EqualTo(1));
    go.SetActive(false);Assert.That(Read<int>(view,"VisibleLayers"),Is.Zero);
    Call(view,"QaClickAdvance");Assert.That((long)State(scene)["tick"],Is.EqualTo(1));
    go.SetActive(true);yield return null;
    Assert.That(Read<int>(scene,"LastDeliveredEventCount"),Is.Zero);
    Call(view,"QaClickAdvance");Assert.That((long)State(scene)["tick"],Is.EqualTo(2));
    Call(scene,"QaFailNextSave");LogAssert.Expect(LogType.Error,new Regex("Roof LOCAL command/source failure:.*",RegexOptions.Singleline));
    Call(view,"QaClickAdvance");
    Assert.That(Read<bool>(scene,"LastCommandSucceeded"),Is.False);Assert.That((long)State(scene)["tick"],Is.EqualTo(2));
    Assert.That((bool)State(scene)["recoveryRequired"],Is.True);
    Call(view,"QaClickRestore");Assert.That(Read<bool>(scene,"LastCommandSucceeded"),Is.True);
    Assert.That((bool)State(scene)["recoveryRequired"],Is.False);Assert.That(Read<int>(scene,"LastDeliveredEventCount"),Is.Zero);
    Call(view,"QaClickAdvance");Assert.That((long)State(scene)["tick"],Is.EqualTo(3));
    Call(view,"QaClickSpeed");long beforeSpeed=(long)State(scene)["tick"];
    Call(view,"QaClickAdvance");Assert.That((long)State(scene)["tick"],Is.EqualTo(beforeSpeed+2),"One elapsed UI millisecond must consume both native 2x steps");
    Assert.That((long)State(scene)["nativeSnapshot"]["state"]["direct"]["clock"]["debtHalfTicks"],Is.Zero);
    Call(view,"QaClickSpeed");
   }finally{UnityEngine.Object.Destroy(go);if(text!=null)UnityEngine.Object.Destroy(text);}
   yield return null;
  }
  [UnityTest]public IEnumerator NativeVictoryStopsAutomaticCommandsAndDiskRevisions(){
   yield return new EnterPlayMode();yield return RunTerminal();
  }
  static IEnumerator RunTerminal(){
   var go=new GameObject("TerminalRoofLocal");TextAsset text=null;
   try{
    var scene=Create(go,out text);yield return null;yield return null;
    for(int n=0;n<6&&(string)State(scene)["outcome"]=="RUNNING";n++){
     Call(scene,"AdvanceManual",100000000L,10000);
     Assert.That(Read<bool>(scene,"LastCommandSucceeded"),Is.True,Read<string>(scene,"LastError"));
    }
    Assert.That((int)State(scene)["hp"]["enemy"],Is.Zero);
    Assert.That((string)State(scene)["outcome"],Is.Not.EqualTo("RUNNING"));
    long revision=(long)State(scene)["revision"];var original=State(scene)["nativeSnapshot"].DeepClone();
    Call(scene,"SetAutoAdvance",true);yield return null;yield return null;
    Assert.That((long)State(scene)["revision"],Is.EqualTo(revision),"Terminal auto loop must not append/save commands");
    Assert.That(JToken.DeepEquals(original,State(scene)["nativeSnapshot"]),Is.True);
   }finally{UnityEngine.Object.Destroy(go);if(text!=null)UnityEngine.Object.Destroy(text);}
   yield return null;
  }  [UnityTest]public IEnumerator LoadedBasicRootIsRejectedBeforeAnotherOwnerStarts(){
   yield return new EnterPlayMode();yield return RunExclusive();
  }
  static IEnumerator RunExclusive(){
   var legacy=new GameObject("ExistingBasicRoot",typeof(BattleSceneRoot));var go=new GameObject("RejectedRoof");TextAsset text=null;
   try{
    LogAssert.Expect(LogType.Error,new Regex("Roof LOCAL command/source failure:.*Loaded active Basic/CE03",RegexOptions.Singleline));
    var scene=Create(go,out text);yield return null;
    Assert.That(Read<bool>(scene,"Ready"),Is.False);
    Assert.That((int)((JObject)Call(scene,"Metadata"))["ownedNativeOwnerCount"],Is.Zero);
   }finally{UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(legacy);if(text!=null)UnityEngine.Object.Destroy(text);}
   yield return null;
  }
 }
}
#endif
