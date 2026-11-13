using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
public class CombatCabinMotionTests {
 [Test] public void PoseLeanKeepsOpaqueSupportPointFixed(){
  var baseline=new Vector2(0,-12);var foot=new Vector2(67,-147);
  foreach(float angle in new[]{-1.5f,-.7f,0,.7f,1.5f}){var at=CombatCabinPoseGeometry.AnchorForRotation(baseline,foot,angle);var rotated=(Vector2)(Quaternion.Euler(0,0,angle)*(Vector3)foot);Assert.Less(Vector2.Distance(baseline+foot,at+rotated),.001f);}
 }
 [TestCase(.92f)] [TestCase(.86f)]
 public void SmallerCabinFiguresKeepSupportAnchorAndState(float scale){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("cabin-scale",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));view.SetCaptureFigureScale(1);var actor=(RectTransform)go.transform.Find("ApprovedBattle/BattleActors/op_kyle");var before=actor.TransformPoint(new Vector3(75,-160,0));string state=driver.StateJson;
   view.SetCaptureFigureScale(scale);Assert.Less(Vector3.Distance(before,actor.TransformPoint(new Vector3(75,-160,0))),.001f);Assert.AreEqual(scale,actor.localScale.x);Assert.AreEqual(state,driver.StateJson);
   driver.SetPaused(true);view.Refresh();var paused=actor.position;driver.Advance(10000000,20);view.Refresh();Assert.AreEqual(paused,actor.position);
  }finally{Object.DestroyImmediate(go);}
 }
}
