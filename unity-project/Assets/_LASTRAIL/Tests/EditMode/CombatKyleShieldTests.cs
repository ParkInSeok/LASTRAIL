using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class CombatKyleShieldTests {
 static JToken Kyle(CombatSceneDriver driver)=>driver.HudState["actors"].Single(a=>(string)a["id"]=="op_kyle");
 static void Capture(GameObject go,string name){
  var cameraGo=new GameObject("ShieldEvidenceCamera",typeof(Camera));var camera=cameraGo.GetComponent<Camera>();
  var target=new RenderTexture(1600,900,24);var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  var canvas=go.GetComponent<Canvas>();var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;
  try {camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
   System.IO.Directory.CreateDirectory("D:/Project/LASTRAIL/QA/KyleShieldUnity-20261109");System.IO.File.WriteAllBytes("D:/Project/LASTRAIL/QA/KyleShieldUnity-20261109/"+name+".png",pixels.EncodeToPNG());
  }finally{canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(cameraGo);}
 }
 [Test] public void ActualShieldSurvivesPoseAndPauseThenExpiresTogether(){
  var model=CombatPresentationConnectionTests.Fresh();
  model.ApplyResolvedShield("op_kyle",new CombatShieldSource("op_kyle","op_kyle.active",0),200,120);
  var initial=model.CaptureLocalState();
  var driver=new CombatSceneDriver(()=>BasicCombatSimulation.RestoreLocalState(initial,CombatPresentationConnectionTests.Fresh),()=>null,(_,r)=>r+1,()=>true);driver.Open();
  var go=new GameObject("KyleShieldLifecycle",typeof(RectTransform),typeof(Canvas));
  try {
   var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/OrdinaryPresentation"));view.UseLandscapeLayout();
   var fx=go.GetComponentInChildren<CombatKyleShieldView>(true);Assert.NotNull(fx);Assert.IsTrue(fx.Visible);Assert.IsFalse((bool)Kyle(driver)["casting"]);
   Assert.IsTrue(go.GetComponentsInChildren<CombatMeter>().Any(m=>m.ShieldValue>0));
   Capture(go,"unity-shield-active");
   var before=driver.StateJson;view.Refresh();Assert.AreEqual(before,driver.StateJson);
   driver.SetPaused(true);driver.Advance(100000000,200);view.Refresh();Assert.IsTrue(fx.Visible);Assert.AreEqual(200,(int)Kyle(driver)["kyleGuardShield"]);
   driver.SetPaused(false);driver.Advance(120*500000L,120);view.Refresh();Assert.AreEqual(0,(int)Kyle(driver)["kyleGuardShield"]);Assert.IsFalse(fx.Visible);Assert.IsFalse(fx.gameObject.activeSelf);
   Capture(go,"unity-shield-expired");
  } finally {UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void NativeDamageDepletesGuardWhileOtherSourceCanRemain(){
  var vitals=new CombatVitals(3024,10000);var key=new CombatShieldSource("op_kyle","op_kyle.active",0);
  vitals.Shield(key,544,120,0);vitals.Shield(new CombatShieldSource("other","other.active",0),100,240,0);
  var project=typeof(BasicCombatSimulation).GetMethod("KyleGuardAmount",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
  Assert.AreEqual(544,project.Invoke(null,new object[]{vitals,0L}));vitals.Damage(272,1,false);Assert.AreEqual(272,project.Invoke(null,new object[]{vitals,1L}));
  vitals.Damage(272,2,false);Assert.AreEqual(0,project.Invoke(null,new object[]{vitals,2L}));Assert.AreEqual(100,vitals.ShieldRemaining(2));Assert.AreEqual(3024,vitals.Hp);
  var go=new GameObject("DepletedGuard",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));
  try{var fx=go.AddComponent<CombatKyleShieldView>();fx.Render(true,.1f);Assert.IsTrue(fx.Visible);fx.Render((int)project.Invoke(null,new object[]{vitals,2L})>0,.1f);Assert.IsFalse(fx.Visible);Assert.IsTrue(go.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).All(i=>!i.enabled));}
  finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
