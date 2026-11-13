using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using UnityEngine.TestTools;

public class CombatStateNotificationTests {
 static CombatSceneDriver Driver(Func<BasicCombatSimulation,long,long> save=null){var d=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,save??((s,r)=>r+1),()=>true);d.Open();return d;}
 static void Subscribe(CombatSceneDriver d,Action action){var e=typeof(CombatSceneDriver).GetEvent("StateChanged");Assert.NotNull(e,"Committed and recovery changes must notify their consumers.");e.AddEventHandler(d,action);}
 [Test] public void CommitAndRecoveryNotifyOnceWithoutPublishingFailedCandidate(){
  bool fail=false;var d=Driver((s,r)=>{if(fail)throw new InvalidOperationException("save failed");return r+1;});int notices=0;bool observedPause=false;
  Subscribe(d,()=>{notices++;observedPause=(bool)d.HudState["paused"];});
  d.SetPaused(true);Assert.AreEqual(1,notices);Assert.IsTrue(observedPause);
  d.SetPaused(true);d.Advance(500000,1);Assert.AreEqual(1,notices,"No change or paused clock must not publish.");
  fail=true;Assert.Throws<InvalidOperationException>(()=>d.SetPaused(false));Assert.AreEqual(2,notices);Assert.IsTrue(observedPause);Assert.IsTrue(d.RecoveryRequired);
  d.Retire();Assert.AreEqual(3,notices);
 }
 [Test] public void ObserverFailureDoesNotUndoCommitOrPreventOtherObservers(){
  var d=Driver();int notices=0;Subscribe(d,()=>throw new InvalidOperationException("view failed"));Subscribe(d,()=>notices++);
  Assert.DoesNotThrow(()=>d.SetPaused(true));Assert.AreEqual(1,notices);Assert.IsFalse(d.RecoveryRequired);Assert.IsTrue((bool)d.HudState["paused"]);Assert.NotNull(d.LastPresentationFailure);
 }
 [Test] public void DisplayReadReusesCommittedProjectionButPublicSnapshotRemainsIsolated(){
  var d=Driver();var method=typeof(CombatSceneDriver).GetMethod("ReadDisplayState",BindingFlags.Instance|BindingFlags.NonPublic);Assert.NotNull(method);
  var first=(JObject)method.Invoke(d,null);Assert.AreSame(first,method.Invoke(d,null));var external=d.HudState;external["paused"]=true;Assert.IsFalse((bool)first["paused"]);
  d.SetPaused(true);var next=(JObject)method.Invoke(d,null);Assert.AreNotSame(first,next);Assert.IsFalse((bool)first["paused"]);Assert.IsTrue((bool)next["paused"]);
 }
 [UnityTest] public System.Collections.IEnumerator ApprovedViewReceivesChangesWithoutFramePollingAndRebindsAfterDisable(){
  yield return new EnterPlayMode();
  var d=Driver();var go=new GameObject("NotifyView",typeof(RectTransform),typeof(Canvas));
  try {var v=go.AddComponent<ApprovedCombatView>();v.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   var notice=go.transform.Find("ApprovedBattle/Notice").GetComponent<TMP_Text>();
   d.SetPaused(true);Assert.AreEqual("일시 정지",notice.text);
   go.SetActive(false);d.SetPaused(false);Assert.AreEqual("일시 정지",notice.text,"Disabled consumer must be detached.");
   go.SetActive(true);Assert.AreEqual("",notice.text);d.SetPaused(true);Assert.AreEqual("일시 정지",notice.text);
  } finally {UnityEngine.Object.DestroyImmediate(go);d.Retire();}
  yield return new ExitPlayMode();
 }
 [UnityTest] public System.Collections.IEnumerator IdleHudDoesNotRepaintAndButtonReturnStopsSampling(){
  yield return new EnterPlayMode();
  var d=Driver();var go=new GameObject("IdleView",typeof(RectTransform),typeof(Canvas));
  try{
   var v=go.AddComponent<ApprovedCombatView>();v.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));d.SetPaused(true);
   yield return null;var notice=go.transform.Find("ApprovedBattle/Notice").GetComponent<TMP_Text>();int repaints=0;notice.RegisterDirtyVerticesCallback(()=>repaints++);
   yield return null;yield return null;Assert.AreEqual(0,repaints,"Idle paused HUD must not apply text each frame.");
   var button=new GameObject("Press",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(CombatInputButton));button.transform.SetParent(go.transform,false);
   var face=new GameObject("Face",typeof(RectTransform),typeof(UnityEngine.UI.Image));face.transform.SetParent(button.transform,false);
   var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(button.transform,false);
   int reads=0;double now=0;var press=button.AddComponent<CombatCommandPress>();press.Configure(face.GetComponent<UnityEngine.UI.Image>(),()=>{reads++;return now;});
   press.OnSubmit(null);Assert.Less(content.GetComponent<RectTransform>().anchoredPosition.y,0);
   now=.2;yield return null;yield return null;Assert.AreEqual(Vector2.zero,content.GetComponent<RectTransform>().anchoredPosition);
   int finished=reads;yield return null;yield return null;Assert.AreEqual(finished,reads,"A finished button animation must stop reading its clock.");
   var events=new GameObject("Events",typeof(UnityEngine.EventSystems.EventSystem));events.transform.SetParent(go.transform,false);
   press.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(events.GetComponent<UnityEngine.EventSystems.EventSystem>()){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,pointerId=1});
   Assert.IsTrue(press.Pressed);button.GetComponent<CombatInputButton>().interactable=false;Assert.IsFalse(press.Pressed,"Disabled input must cancel through the Selectable callback.");
  }finally{UnityEngine.Object.DestroyImmediate(go);d.Retire();}
  yield return new ExitPlayMode();
 }
 [Test] public void ActiveCutinFinishesFromRenderCallbackWithoutAnotherStateCommit(){
  var d=Driver();var go=new GameObject("CutinClock",typeof(RectTransform),typeof(Canvas));
  try{
   var v=go.AddComponent<ApprovedCombatView>();v.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));double now=0;v.SetCapturePresentationClock(()=>now);
   var policy=new CombatPresentationPolicy(()=>false,()=>true){Mode=CombatPresentationMode.Full};v.ConfigurePresentation(policy);policy.Begin("visual-only",0,.4,false);v.Refresh();Assert.IsTrue(v.FullCutinVisible);
   var before=d.StateJson;now=1;Canvas.ForceUpdateCanvases();Assert.IsFalse(v.FullCutinVisible);Assert.AreEqual(before,d.StateJson);
  }finally{d.Retire();UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void PausedDurationDoesNotAdvanceResultClockOnResume(){
  var d=Driver();var go=new GameObject("EffectPause",typeof(RectTransform),typeof(Canvas));
  try{
   var v=go.AddComponent<ApprovedCombatView>();v.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));double now=1;v.SetCapturePresentationClock(()=>now);v.Refresh();
   var clock=typeof(ApprovedCombatView).GetField("resultClock",BindingFlags.Instance|BindingFlags.NonPublic);
   d.SetPaused(true);float before=(float)clock.GetValue(v);now=20;d.SetPaused(false);Assert.AreEqual(before,(float)clock.GetValue(v),"Elapsed pause is not effect playback time.");
  }finally{d.Retire();UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void DisabledBranchCancelsGestureImmediatelyWithoutAFrame(){
  var go=new GameObject("DisabledLever",typeof(RectTransform));
  try{var lever=go.AddComponent<CombatBranchLever>();int commits=0;lever.Commit=()=>commits++;lever.BeginGesture(3);lever.interactable=false;lever.interactable=true;lever.EndGesture(3,true);Assert.AreEqual(0,commits);}
  finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void ReleaseWithoutClickExpiresBeforeTheNextGesture(){
  var go=new GameObject("ReleaseExpiry",typeof(RectTransform),typeof(Canvas));var events=new GameObject("Events",typeof(UnityEngine.EventSystems.EventSystem));
  try{var button=go.AddComponent<CombatInputButton>();int clicks=0;button.onClick.AddListener(()=>clicks++);
   var e=new UnityEngine.EventSystems.PointerEventData(events.GetComponent<UnityEngine.EventSystems.EventSystem>()){pointerId=1,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
   button.OnPointerDown(e);button.OnPointerUp(e);Canvas.ForceUpdateCanvases();button.OnPointerClick(e);Assert.AreEqual(0,clicks);
   e.pointerId=2;button.OnPointerDown(e);button.OnPointerUp(e);button.OnPointerClick(e);Assert.AreEqual(1,clicks);
  }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(events);}
 }
}
