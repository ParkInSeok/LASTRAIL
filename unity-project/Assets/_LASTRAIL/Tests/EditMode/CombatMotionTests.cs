using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
public class CombatMotionTests {
 [Test] public void EnemyEntersAndWindowTravelPausesWithSimulation(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  var go=new GameObject("MotionTest",typeof(RectTransform),typeof(Canvas));
  try{
   var view=go.AddComponent<ApprovedCombatView>();var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");view.Bind(driver,assets);
   var windows=go.GetComponentsInChildren<CombatWindowView>();Assert.AreEqual(2,windows.Length);Assert.IsTrue(windows.All(w=>w.Texture==assets.ExteriorMap));
   var enemy=go.GetComponentsInChildren<RectTransform>(true).First(r=>r.name.StartsWith("wave.1.interior."));float start=enemy.anchoredPosition.x;Assert.GreaterOrEqual(start,750);
   driver.Advance(10000000,20);view.Refresh();Assert.Less(enemy.anchoredPosition.x,start);Assert.Greater(view.TravelOffset,0);Assert.AreEqual(view.TravelOffset,windows[0].Travel);Assert.AreEqual(view.TravelOffset+136f/720f*.9f,windows[0].uvRect.x,.00001f);Assert.AreEqual(view.TravelOffset+423f/720f*.9f,windows[1].uvRect.x,.00001f);
   driver.SetPaused(true);float travel=view.TravelOffset;var position=enemy.anchoredPosition;driver.Advance(10000000,20);view.Refresh();Assert.AreEqual(travel,view.TravelOffset);Assert.AreEqual(position,enemy.anchoredPosition);

  }finally{Object.DestroyImmediate(go);}
 }
 [Test] public void WindowMapFollowsSkinCompositionWithoutChangingCombat(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  var go=new GameObject("Swap",typeof(RectTransform),typeof(Canvas));var t=new Texture2D(2,2);
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));string before=driver.StateJson;
   view.SetAppearance("map-test",t,"skin-test",t,t);view.Refresh();Assert.AreEqual(before,driver.StateJson);Assert.IsTrue(go.GetComponentsInChildren<CombatWindowView>().All(w=>w.Texture==t));
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(t);}
 }
}
