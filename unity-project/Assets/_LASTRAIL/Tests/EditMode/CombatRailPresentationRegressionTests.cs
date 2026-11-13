using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public class CombatRailPresentationRegressionTests {
 [Test] public void ResizedActualCameraFitsFullConsoleBeforeRendering(){
  var p=new Ce03RailPreparation(CombatRailPreparationTests.Golden(),"a",1);var go=new GameObject("resize-test",typeof(RectTransform),typeof(Canvas));var cameraGo=new GameObject("camera",typeof(Camera));var a=new RenderTexture(720,1280,24);var b=new RenderTexture(720,1560,24);
  try{var d=new CombatSceneDriver(()=>p.CreateModel(1),()=>null,(s,r)=>r+1,()=>true);d.Open();var view=go.AddComponent<ApprovedCombatView>();view.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var camera=cameraGo.GetComponent<Camera>();camera.targetTexture=a;var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();view.Refresh();Canvas.ForceUpdateCanvases();
   camera.targetTexture=b;view.Refresh();Canvas.ForceUpdateCanvases();var corners=new Vector3[4];((RectTransform)go.transform.Find("ApprovedBattle")).GetWorldCorners(corners);foreach(var c in corners){var screen=camera.WorldToScreenPoint(c);Assert.That(screen.x,Is.InRange(-1f,721f));Assert.That(screen.y,Is.InRange(-1f,1561f));}
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(cameraGo);a.Release();b.Release();Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
 }
 [Test] public void GripOrientationAndCenterMatchPhysicalTravelAxis(){
  var p=new Ce03RailPreparation(CombatRailPreparationTests.Golden(),"a",1);var go=new GameObject("grip-test",typeof(RectTransform),typeof(Canvas));
  try{var d=new CombatSceneDriver(()=>p.CreateModel(1),()=>null,(s,r)=>r+1,()=>true);d.Open();go.AddComponent<ApprovedCombatView>().Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));Canvas.ForceUpdateCanvases();foreach(var slider in go.GetComponentsInChildren<Slider>()){var grip=(RectTransform)slider.handleRect.Find("MechanicalGrip");if(grip==null)continue;bool branch=slider.name=="BranchLever";Assert.AreEqual(new Vector2(.5f,.5f),slider.handleRect.pivot);Assert.That(Mathf.DeltaAngle(grip.localEulerAngles.z,branch?0:90),Is.EqualTo(0).Within(.1));var local=((RectTransform)slider.transform).InverseTransformPoint(grip.TransformPoint(grip.rect.center));var center=((RectTransform)slider.transform).rect.center;Assert.That(branch?local.y:local.x,Is.EqualTo(branch?center.y:center.x).Within(.5));}}
  finally{Object.DestroyImmediate(go);}
 }
 [Test] public void ArtilleryFullyPassesForEachConfiguredExposure(){foreach(long exposure in new[]{60000L,120000L,240000L}){var method=typeof(CombatRailWorldLayout).GetMethod("ArtilleryX");Assert.NotNull(method);float x=(float)method.Invoke(null,new object[]{exposure,0L,exposure});Assert.LessOrEqual(x+124,0);}}
}
