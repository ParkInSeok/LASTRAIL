using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
public class CombatProductionRigTests {
 [Test] public void ApprovedRuntimeBindingInstallsAllFiveWithoutCaptureFlagsAndKeepsModelExact(){
  var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");Assert.IsNotNull(assets.OperatorRigs);var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var reference=CombatPresentationConnectionTests.Fresh();var go=new GameObject("production-rig",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,assets);Assert.AreEqual(5,go.GetComponentsInChildren<CombatMultipartRig>().Length);Assert.AreEqual(.86f,view.CabinFigureScale);
   for(int i=0;i<30;i++){driver.Advance(500000,1);reference.Advance(500000,1);view.Refresh();Assert.AreEqual(reference.CaptureLocalState(),driver.StateJson);}driver.SetPaused(true);view.Refresh();var before=view.CaptureRigStates().ToString();driver.Advance(10000000,20);view.Refresh();Assert.AreEqual(before,view.CaptureRigStates().ToString());
  }finally{Object.DestroyImmediate(go);}
 }
}
