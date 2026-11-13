using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public class CombatTravelRecoveryTests {
 [TestCase(2)] [TestCase(3)] [TestCase(4)]
 public void MotionUsesFractionalSavedClockAtEverySpeedAndRestoresExactly(int speed){
  var model=CombatPresentationConnectionTests.Fresh();model.SetSpeed(speed);double before=(double)model.ReadHudState()["motionSeconds"];
  model.Advance(100000,8);Assert.AreEqual(before+.01*speed/2,(double)model.ReadHudState()["motionSeconds"],.000001);
  model.SetPaused(true);var paused=model.CaptureLocalState();double time=(double)model.ReadHudState()["motionSeconds"];model.Advance(10000000,8);Assert.AreEqual(paused,model.CaptureLocalState());Assert.AreEqual(time,(double)model.ReadHudState()["motionSeconds"]);
  var restored=BasicCombatSimulation.RestoreLocalState(paused,CombatPresentationConnectionTests.Fresh);Assert.AreEqual(time,(double)restored.ReadHudState()["motionSeconds"]);restored.SetPaused(false);restored.Advance(100000,8);Assert.AreEqual(time+.01*speed/2,(double)restored.ReadHudState()["motionSeconds"],.000001);
 }
 [Test] public void RepeatingNativeGroundHasExactCycleAndStablePausedGeometry(){
  var go=new GameObject("belt",typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(720,273);var belt=go.AddComponent<CombatTravelGround>();
  try{var populate=typeof(CombatTravelGround).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);using(var vh=new VertexHelper()){
   belt.SetDistance(0);populate.Invoke(belt,new object[]{vh});var a=new System.Collections.Generic.List<UIVertex>();vh.GetUIVertexStream(a);
   belt.SetDistance(CombatTravelGround.Period);populate.Invoke(belt,new object[]{vh});var b=new System.Collections.Generic.List<UIVertex>();vh.GetUIVertexStream(b);Assert.Greater(a.Count,100);Assert.AreEqual(a.Count,b.Count);for(int i=0;i<a.Count;i++){Assert.AreEqual(a[i].position,b[i].position);Assert.AreEqual(a[i].color,b[i].color);}
   belt.SetDistance(1);Assert.AreEqual(1,belt.Phase);belt.SetDistance(1);Assert.AreEqual(1,belt.Phase);
  }}finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void ActiveTravelLayersPauseTogetherWithoutChangingState(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("travel-view",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var belts=go.GetComponentsInChildren<CombatTravelGround>();Assert.AreEqual(2,belts.Length);driver.Advance(170000,8);view.Refresh();Assert.IsTrue(belts.All(b=>b.Phase>0));driver.SetPaused(true);var phases=belts.Select(b=>b.Phase).ToArray();var before=driver.StateJson;driver.Advance(10000000,8);view.Refresh();Assert.AreEqual(before,driver.StateJson);CollectionAssert.AreEqual(phases,belts.Select(b=>b.Phase).ToArray());}
  finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void ReleasingLivePresentationStopsAudioAndDropsOwnedAssetReferences(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  var go=new GameObject("release-live-view",typeof(RectTransform),typeof(Canvas));
  try{
   var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   var audio=(AudioSource)typeof(ApprovedCombatView).GetField("presentationAudio",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
   audio.clip=LastRail.Resources.PreparedContent.Load<AudioClip>("CombatPresentation/Audio/op_kyle.active");
   view.ReleaseOwnedPresentationResources();view.ReleaseOwnedPresentationResources();
   Assert.IsNull(audio.clip);Assert.IsFalse(audio.isPlaying);
   foreach(string field in new[]{"assets","font","abilityAudio","effectAudio"})Assert.IsNull(typeof(ApprovedCombatView).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view),field);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }

}
