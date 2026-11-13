using System;
using System.Reflection;
using LastRail.Gacha;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using TMPro;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public sealed class BoundedScreenLayoutTests {
  [TestCase(typeof(LobbyCommandDeck))]
  [TestCase(typeof(LobbyDestinationPages))]
  [TestCase(typeof(FormationPlanView))]
  [TestCase(typeof(GachaScreenRootBehaviour))]
  [TestCase(typeof(CodexArchiveScreenRootBehaviour))]
  [TestCase(typeof(RewardStoryScreenRootBehaviour))]
  public void ScreensHaveNoIdleFrameCallbacks(Type type){
   foreach(var name in new[]{"Update","LateUpdate","FixedUpdate"})
    Assert.That(type.GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic),Is.Null,name);
  }
  [TestCase("GachaScreen",typeof(GachaScreenRootBehaviour))]
  [TestCase("CodexArchiveScreen",typeof(CodexArchiveScreenRootBehaviour))]
  [TestCase("RewardReviewScreen",typeof(RewardStoryScreenRootBehaviour))]
  public void SizeAndForegroundCallbacksReapplyLayoutWithoutChangingState(string prefab,Type type){
   var root=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/"+prefab+".prefab"));
   try {
    var view=root.GetComponent(type);
    var title=(TMP_Text)type.GetField("titleText",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
    var sizeChanged=type.GetMethod("OnRectTransformDimensionsChange",BindingFlags.Instance|BindingFlags.NonPublic);
    Assert.That(sizeChanged,Is.Not.Null);
    sizeChanged.Invoke(view,null);
    var anchor=title.rectTransform.anchorMin;
    title.rectTransform.anchorMin=new Vector2(.123f,.456f);
    sizeChanged.Invoke(view,null);
    Assert.That(title.rectTransform.anchorMin,Is.EqualTo(anchor));
    root.SetActive(false);root.SetActive(true);
    type.GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{true});
    Assert.That(title.rectTransform.anchorMin,Is.EqualTo(anchor));
    Assert.That(type.GetProperty("CurrentState").GetValue(view),Is.Null);
   } finally { UnityEngine.Object.DestroyImmediate(root); }
  }
  [Test] public void LobbyResizeAndRecoveryDialogFitImmediatelyWithoutRenderPolling(){
   var root=new GameObject("Lobby",typeof(RectTransform),typeof(Canvas));
   try {
    ((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
    var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
    var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
    var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);
    deck.OpenDepartureRecovery("run_a");
    var dialog=(RectTransform)deck.transform.Find("LobbyDialogShield/LobbyDialog");
    Assert.That(dialog.localScale.x,Is.EqualTo(Mathf.Min(1576f/750,876f/840)).Within(.001));
    ((RectTransform)root.transform).sizeDelta=new Vector2(1200,900);
    Assert.That(deck.transform.Find("FixedControls").localScale.x,Is.EqualTo(.75f).Within(.001));
    root.SetActive(false);root.SetActive(true);
    Assert.That(deck.IsDialogOpen,Is.False);
    Assert.That(deck.transform.Find("FixedControls").localScale.x,Is.EqualTo(.75f).Within(.001));
   } finally { UnityEngine.Object.DestroyImmediate(root); }
  }
 }
}
