using System;
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEngine;
public class CombatShieldBarTests {
 static void Set(CombatMeter bar,int hp,int max,int shield){var method=typeof(CombatMeter).GetMethod("SetHealth");Assert.IsNotNull(method,"HP bars must accept live shield units");method.Invoke(bar,new object[]{hp,max,shield});}
 static float Shield(CombatMeter bar)=>(float)typeof(CombatMeter).GetProperty("ShieldValue").GetValue(bar);
 [TestCase(75,100,0,.75f,0f)]
 [TestCase(50,100,20,.5f,.2f)]
 [TestCase(100,100,40,100f/140,40f/140)]
 [TestCase(0,100,0,0f,0f)]
 [TestCase(0,100,20,0f,.2f)]
 [TestCase(1,int.MaxValue,1,1f/int.MaxValue,1f/int.MaxValue)]
 [TestCase(int.MaxValue,int.MaxValue,int.MaxValue,.5f,.5f)]
 public void BothSegmentsUseSameUnitsWithoutMinimumWidth(int hp,int max,int shield,float expectedHp,float expectedShield){
  var go=new GameObject("shield-bar",typeof(RectTransform));try{var bar=go.AddComponent<CombatMeter>();Set(bar,hp,max,shield);Assert.AreEqual(expectedHp,bar.Value);Assert.AreEqual(expectedShield,Shield(bar));if(shield>0)Assert.Greater(Shield(bar),0);Set(bar,50,100,0);Assert.AreEqual(0,Shield(bar));}finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void ActualAbsorptionExpiryPauseRestoreAreReadWithoutMutatingState(){
  var s=CombatPresentationConnectionTests.Fresh();s.ApplyResolvedShield("op_sera",new CombatShieldSource("test","layer1",0),100,25);s.ApplyResolvedShield("op_sera",new CombatShieldSource("test","layer2",0),100,100);s.ApplyResolvedShield("train_a",new CombatShieldSource("test","train",0),500,60);
  int Amount(string id)=>(int)((JArray)s.ReadHudState()["actors"]).Single(a=>(string)a["id"]==id)["shield"];
  var before=s.CaptureLocalState();Assert.AreEqual(200,Amount("op_sera"));Assert.AreEqual(before,s.CaptureLocalState());s.SetPaused(true);s.Advance(200000000,400);Assert.AreEqual(200,Amount("op_sera"));s.SetPaused(false);s.Advance(25*500000L,25);Assert.AreEqual(100,Amount("op_sera"));
  s.Advance(30*500000L,30);Assert.IsTrue(s.Events.Any(e=>e.Target=="op_sera"&&e.Absorbed>0));Assert.Less(Amount("op_sera"),100);var restored=BasicCombatSimulation.RestoreLocalState(s.CaptureLocalState(),CombatPresentationConnectionTests.Fresh);Assert.AreEqual(s.ReadHudState().ToString(),restored.ReadHudState().ToString());s.Advance(50*500000L,50);Assert.AreEqual(0,Amount("op_sera"));Assert.AreEqual(0,Amount("train_a"));
 }
 [Test] public void RuntimeOperatorAndTrainBarsUseActualShieldAndSubmitWhiteMesh(){
  var s=CombatPresentationConnectionTests.Fresh();s.ApplyResolvedShield("op_sera",new CombatShieldSource("test","bar",0),200,100);s.ApplyResolvedShield("train_a",new CombatShieldSource("test","bar",0),500,100);var state=s.CaptureLocalState();
  var driver=new CombatSceneDriver(()=>BasicCombatSimulation.RestoreLocalState(state,CombatPresentationConnectionTests.Fresh),()=>null,(m,r)=>r+1,()=>true);driver.Open();var go=new GameObject("shield-ui",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));Canvas.ForceUpdateCanvases();var bars=go.GetComponentsInChildren<CombatMeter>();var white=bars.Where(b=>typeof(CombatMeter).GetProperty("ShieldValue")!=null&&Shield(b)>0).ToArray();Assert.AreEqual(3,white.Length);
   foreach(var bar in white){var shield=bar.GetLayer("Shield");Assert.IsFalse(shield.raycastTarget);var mesh=shield.canvasRenderer.GetMesh();Assert.Greater(mesh.vertexCount,0);Assert.IsTrue(mesh.colors32.Any(c=>c.r>240&&c.g>240&&c.b>240));UnityEngine.Object.DestroyImmediate(mesh);}Assert.AreEqual(state,driver.StateJson);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void TerminalStateAndZeroHpNeverAdvanceOrInventShield(){
  var s=CombatPresentationConnectionTests.Fresh();var raw=JObject.Parse(s.CaptureLocalState());var train=((JArray)raw["actors"]).Single(a=>(string)a["id"]=="train_a");train["vitals"]["hp"]=0;raw["outcome"]="Failure";s=BasicCombatSimulation.RestoreLocalState(raw.ToString(),CombatPresentationConnectionTests.Fresh);var before=s.CaptureLocalState();var hud=s.ReadHudState();Assert.AreEqual(0,(int)hud["trainHp"]);Assert.AreEqual(0,(int)((JArray)hud["actors"]).Single(a=>(bool)a["train"])["shield"]);s.Advance(10000000,20);Assert.AreEqual(before,s.CaptureLocalState());
 }
}
