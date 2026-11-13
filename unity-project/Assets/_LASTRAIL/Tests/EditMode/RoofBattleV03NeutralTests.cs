#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using LastRail.Presentation.RoofBattle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode {
 public sealed class RoofBattleV03NeutralTests {
  [UnityTearDown]public IEnumerator LeaveRuntime(){if(Application.isPlaying)yield return new ExitPlayMode();}
  [UnityTest]public IEnumerator CommittedClockDrivesOnlyEveBodyAcrossPauseFailureAndRebind(){
   yield return new EnterPlayMode();
   yield return RunRuntime();
  }
  static IEnumerator RunRuntime(){
   var assets=AssetDatabase.LoadAssetAtPath<FourAgentV03Assets>("Assets/_LASTRAIL/UI/CombatAnimations/FourAgentV03/ApprovedFourAgentV03.asset");Assert.NotNull(assets);
   var go=new GameObject("V03NativeEveOnly");var raw=RoofBattleLocalStorageTests.SourceEnvelope();var text=new TextAsset(raw.ToString());
   try {
    var scene=go.AddComponent<RoofBattleLocalScene>();scene.Initialize(text,BDirectAttackInput.Digest(raw),"v03-eve-"+Guid.NewGuid().ToString("N"),null,AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),"D:/Project/LASTRAIL/QA/FourAgentV03-20261019/SceneStore-"+Guid.NewGuid().ToString("N"));scene.ConfigureV03(assets);scene.SetAutoAdvance(false);
    yield return null;yield return null;Assert.True(scene.Ready,scene.LastError);
    void BodyMatchesClock(){var state=scene.Snapshot;long ms=checked((long)state["tick"]*(long)state["stepTimeSpanTicks"])/10000;var frame=assets.CreateCatalog().Sample("op_eve",1,false,0,ms,new Vector2(570,1005));var body=go.GetComponentsInChildren<RawImage>().Where(x=>x.gameObject.activeInHierarchy).ToArray();Assert.That(body.Length,Is.EqualTo(1));Assert.That(body[0].texture,Is.SameAs(assets.Texture((string)frame["path"])));Assert.That(scene.View.VisibleLayers,Is.EqualTo(1));Assert.That(scene.View.SpatialFxCount,Is.Zero);}
    BodyMatchesClock();scene.AdvanceManual(2500000,250);Assert.True(scene.LastCommandSucceeded,scene.LastError);Assert.That((long)scene.Snapshot["tick"],Is.EqualTo(250));Assert.That(scene.HitCount,Is.EqualTo(1));BodyMatchesClock();
    scene.View.QaClickPause();var held=scene.Snapshot["tick"].DeepClone();scene.AdvanceManual(2500000,250);Assert.That(JToken.DeepEquals(held,scene.Snapshot["tick"]),Is.True);BodyMatchesClock();scene.View.QaClickPause();
    go.SetActive(false);Assert.That(scene.View.VisibleLayers,Is.Zero);scene.View.QaClickAdvance();Assert.That(JToken.DeepEquals(held,scene.Snapshot["tick"]),Is.True);go.SetActive(true);yield return null;Assert.That(scene.LastDeliveredEventCount,Is.Zero);BodyMatchesClock();
    scene.QaFailNextSave();LogAssert.Expect(LogType.Error,new Regex("Roof LOCAL command/source failure:.*",RegexOptions.Singleline));scene.AdvanceManual(2500000,250);Assert.False(scene.LastCommandSucceeded);Assert.That(JToken.DeepEquals(held,scene.Snapshot["tick"]),Is.True);BodyMatchesClock();scene.RestoreOriginal();Assert.True(scene.LastCommandSucceeded,scene.LastError);Assert.That(scene.LastDeliveredEventCount,Is.Zero);BodyMatchesClock();
    scene.View.QaClickSpeed();scene.AdvanceManual(10000,2);Assert.That((long)scene.Snapshot["tick"],Is.EqualTo((long)held+2));BodyMatchesClock();Assert.Throws<InvalidOperationException>(()=>scene.ConfigureV03(assets));
   }finally{UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(text);}
   yield return null;
  }
 }
}
#endif
