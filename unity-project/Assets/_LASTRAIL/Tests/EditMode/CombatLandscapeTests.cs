using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public class CombatLandscapeTests {
 [TestCase(1600,900,0,0,1600,900)][TestCase(2400,1080,80,30,2240,1020)][TestCase(2048,1536,0,40,2048,1496)]
 public void LandscapeSafeAreaKeepsAllDesignCorners(int width,int height,int x,int y,int safeWidth,int safeHeight){var safe=new Rect(x,y,safeWidth,safeHeight);var fit=CombatSafeArea.FitDesign(safe,new Vector2(1600,900));Assert.That(fit.width/fit.height,Is.EqualTo(1600f/900).Within(.0001f));Assert.That(fit.xMin,Is.GreaterThanOrEqualTo(safe.xMin-.01f));Assert.That(fit.xMax,Is.LessThanOrEqualTo(safe.xMax+.01f));Assert.That(fit.yMin,Is.GreaterThanOrEqualTo(safe.yMin-.01f));Assert.That(fit.yMax,Is.LessThanOrEqualTo(safe.yMax+.01f));}
 [Test] public void LandscapeUsesExistingControlsWithoutChangingCommittedRun(){
  BasicCombatSimulation Fresh(){var s=CombatPresentationConnectionTests.Fresh();s.ConfigureRailJourney(CombatRailJourneyTests.Config());return s;}
  int saves=0;var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>{saves++;return r+1;},()=>true);driver.Open();var go=new GameObject("landscape",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));string before=driver.StateJson;int prior=saves;
   var method=typeof(ApprovedCombatView).GetMethod("UseLandscapeLayout");Assert.NotNull(method,"Missing landscape presentation binding");method.Invoke(view,null);view.Refresh();method.Invoke(view,null);
   var root=(RectTransform)go.transform.Find("ApprovedBattle");Assert.That(root.sizeDelta,Is.EqualTo(new Vector2(1600,900)));Assert.That(driver.StateJson,Is.EqualTo(before));Assert.That(saves,Is.EqualTo(prior));
   var sliders=go.GetComponentsInChildren<Slider>(true);var throttle=sliders.Single(s=>s.name=="TrainThrottle");float target=throttle.value<.5f?1:0;throttle.value=target;Assert.That(saves,Is.EqualTo(prior+1));view.Refresh();Assert.That(saves,Is.EqualTo(prior+1));
   // This small rail fixture has no selected consist. Exercise a late-created map overlay explicitly.
   var map=new GameObject("RouteMap",typeof(RectTransform)).GetComponent<RectTransform>();map.SetParent(root,false);map.anchorMin=map.anchorMax=map.pivot=new Vector2(0,1);map.sizeDelta=new Vector2(180,42);map.anchoredPosition=new Vector2(66,-1040);view.Refresh();
   foreach(string name in new[]{"RouteMap","BranchLever","TrainThrottle","Command0","Command1","Command2","Speed","Pause"}){var rect=go.GetComponentsInChildren<RectTransform>(true).Single(r=>r.name==name);var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var point in corners){var local=root.InverseTransformPoint(point);Assert.That(local.x,Is.InRange(root.rect.xMin-.1f,root.rect.xMax+.1f),name);Assert.That(local.y,Is.InRange(root.rect.yMin-.1f,root.rect.yMax+.1f),name);}}
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
