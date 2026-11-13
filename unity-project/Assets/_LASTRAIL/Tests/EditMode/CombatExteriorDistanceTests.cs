using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public class CombatExteriorDistanceTests {
 [Test] public void UpperRoadRankSpacingSeparatesBuggyAndBikePaintedBodies(){
  float front=CombatExteriorLayout.ActorScale(1,1,"enemy_spike_cart_gunner"),rear=CombatExteriorLayout.ActorScale(3,1,"enemy_railbike_marauder");
  var a=CombatExteriorLayout.ApproachGround(1,1);var b=CombatExteriorLayout.ApproachGround(3,1);
  float frontTop=a.y-CombatExteriorLayout.WheelBottom("enemy_spike_cart_gunner")*front;
  Assert.Less(b.y,frontTop,"Upper trailing wheel bottom must stay above front vehicle roof");
  Assert.Greater(b.x,a.x,"Trailing actor stays farther along original upper road");
 }
 [Test] public void ActualExteriorHitFlashFollowsScaledTargetAndViewport(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("hit-anchor",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));RawImage flash=null;
   for(int i=0;i<500&&flash==null;i++){driver.Advance(500000,1);view.Refresh();Canvas.ForceUpdateCanvases();flash=go.GetComponentsInChildren<RawImage>().FirstOrDefault(a=>a.name=="ResolvedEffect");}
   Assert.NotNull(flash,"Need actual positive hit effect");Assert.AreEqual("Unit",flash.transform.parent.name);Assert.NotNull(flash.GetComponentInParent<RectMask2D>());
   var parent=flash.transform.parent as RectTransform;Assert.That(Vector3.Distance(parent.TransformPoint(parent.rect.center),flash.rectTransform.TransformPoint(flash.rectTransform.rect.center)),Is.LessThan(.01f));
   Assert.That(flash.transform.lossyScale.x,Is.LessThan(1));
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void RestoredFutureActorIsHiddenThenApproachesWithoutDelayingModel(){
  var model=CombatPresentationConnectionTests.Fresh();JToken future=null;
  for(int i=0;i<500&&future==null;i++){model.Advance(500000,1);future=((JArray)model.ReadHudState()["actors"]).FirstOrDefault(a=>(bool)a["enemy"]&&(bool)a["exterior"]&&(long)a["availableAt"]>model.Tick);}
  Assert.NotNull(future,"Need a real scheduled exterior actor");string id=(string)future["id"];long due=(long)future["availableAt"];
  var checkpoint=(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{model,1L},null);
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>checkpoint,(s,r)=>r+1,()=>true);driver.Open();var reference=BasicCombatSimulation.RestoreLocalState(driver.StateJson,CombatPresentationConnectionTests.Fresh);
  var go=new GameObject("future-entry",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var actor=go.transform.Find("ApprovedBattle/ExteriorViewport/ExteriorActors/"+id) as RectTransform;Assert.NotNull(actor);Assert.IsFalse(actor.gameObject.activeSelf);
   while(driver.Tick<due){reference.Advance(500000,1);driver.Advance(500000,1);view.Refresh();Assert.AreEqual(reference.CaptureLocalState(),driver.StateJson);}
   Assert.IsTrue(actor.gameObject.activeSelf);Assert.That(actor.GetComponent<CanvasGroup>().alpha,Is.GreaterThan(0));Assert.AreEqual(CombatExteriorLayout.ActorScale((int)future["slot"],0,(string)future["visual"]),actor.localScale.x,.001f);var position=actor.anchoredPosition;
   for(int i=0;i<8;i++){reference.Advance(500000,1);driver.Advance(500000,1);view.Refresh();Assert.AreEqual(reference.CaptureLocalState(),driver.StateJson);}
   Assert.AreNotEqual(position,actor.anchoredPosition);Assert.Greater(actor.localScale.x,CombatExteriorLayout.ActorScale((int)future["slot"],0,(string)future["visual"]));
   var rebuilt=new GameObject("restore-mid-entry",typeof(RectTransform),typeof(Canvas));
   try{var restoredView=rebuilt.AddComponent<ApprovedCombatView>();var before=driver.StateJson;restoredView.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var same=rebuilt.transform.Find("ApprovedBattle/ExteriorViewport/ExteriorActors/"+id) as RectTransform;
    Assert.AreEqual(actor.anchoredPosition,same.anchoredPosition,"Restore must preserve partial approach position");Assert.AreEqual(actor.localScale,same.localScale);Assert.AreEqual(actor.GetComponent<CanvasGroup>().alpha,same.GetComponent<CanvasGroup>().alpha);Assert.AreEqual(before,driver.StateJson);
   }finally{UnityEngine.Object.DestroyImmediate(rebuilt);}
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void RestoredFutureSpawnStillStartsAtItsAvailabilityBoundary(){
  Assert.AreEqual(12,CombatExteriorLayout.EntryBorn(true,10,12));
  Assert.AreEqual(10,CombatExteriorLayout.EntryBorn(true,10,10));
  Assert.AreEqual(9,CombatExteriorLayout.EntryBorn(true,10,9));
  Assert.AreEqual(9.75f,CombatExteriorLayout.EntryBorn(true,10,9.75f));
  Assert.AreEqual(10,CombatExteriorLayout.EntryBorn(false,10,9));
 }
 [Test] public void DistantTrainAndTurretsShareAClippedTransformWithoutChangingBattle(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("distant",typeof(RectTransform),typeof(Canvas));
  try{var before=driver.StateJson;var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   var viewport=go.transform.Find("ApprovedBattle/ExteriorViewport") as RectTransform;Assert.NotNull(viewport,"Distant viewport missing");Assert.NotNull(viewport.GetComponent<RectMask2D>());Assert.AreEqual(new Vector2(720,273),viewport.sizeDelta);
   var train=viewport.Find("TrainSkinLayer") as RectTransform;Assert.NotNull(train);Assert.That(train.rect.width*train.localScale.x,Is.InRange(288f,324f));
   var turrets=go.GetComponentsInChildren<RectTransform>().Where(t=>t.name.StartsWith("Turret_")).ToArray();Assert.IsNotEmpty(turrets);Assert.IsTrue(turrets.All(t=>t.parent==train));
   Assert.AreEqual(before,driver.StateJson);driver.SetPaused(true);view.Refresh();var position=train.position;driver.Advance(10000000,20);view.Refresh();Assert.AreEqual(position,train.position);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void ExteriorProjectionDoesNotChangeSimulationOrInteriorAnchors(){
  var reference=CombatPresentationConnectionTests.Fresh();var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("distant-state",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var kyle=go.transform.Find("ApprovedBattle/BattleActors/op_kyle") as RectTransform;Assert.NotNull(kyle);Assert.AreEqual(675,-kyle.anchoredPosition.y+160*kyle.localScale.y,.001f);
   for(int i=0;i<100;i++){if(i==30){reference.SetSpeed(4);driver.SetSpeed(4);}reference.Advance(500000,1);driver.Advance(500000,1);view.Refresh();Assert.AreEqual(reference.CaptureLocalState(),driver.StateJson);}
   var external=go.transform.Find("ApprovedBattle/ExteriorViewport/ExteriorActors");Assert.NotNull(external);Assert.IsTrue(external.GetComponentsInChildren<RectTransform>().Any(t=>t.name.StartsWith("wave.")));
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
