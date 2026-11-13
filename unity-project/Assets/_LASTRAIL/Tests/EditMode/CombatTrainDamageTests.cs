using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
public class CombatTrainDamageTests {
 [Test] public void ActualTrainHealthBindsDamageAndRepairWithoutChangingModel(){
  var fresh=CombatPresentationConnectionTests.Fresh();string trainId=(string)((JArray)fresh.ReadHudState()["actors"]).Single(a=>(bool)a["train"])["id"];var raw=JObject.Parse(fresh.CaptureLocalState());var train=((JArray)raw["actors"]).Single(a=>(string)a["id"]==trainId);train["vitals"]["hp"]=(int)fresh.ReadHudState()["trainMaxHp"]/5;
  var initial=BasicCombatSimulation.RestoreLocalState(raw.ToString(),CombatPresentationConnectionTests.Fresh);
  var restored=(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{initial,1L},null);
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>restored,(s,r)=>r+1,()=>true);driver.Open();
  var go=new GameObject("train-damage",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();var before=driver.StateJson;view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   var type=typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatTrainDamageView");Assert.NotNull(type,"HP-linked train damage renderer missing");
   var damage=go.GetComponentInChildren(type);Assert.NotNull(damage);Assert.AreEqual(3,type.GetProperty("Stage").GetValue(damage));Assert.AreEqual(before,driver.StateJson);
   for(int i=0;i<10;i++)view.Refresh();Assert.AreEqual(before,driver.StateJson);
   var reference=BasicCombatSimulation.RestoreLocalState(before,CombatPresentationConnectionTests.Fresh);reference.RequestSkill("repair-damage",trainId,"emergency_repair");driver.RequestSkill("repair-damage",trainId,"emergency_repair");
   for(int i=0;i<100&&reference.CommandStatus("repair-damage")!="APPLIED";i++){reference.Advance(500000,1);driver.Advance(500000,1);view.Refresh();Assert.AreEqual(reference.CaptureLocalState(),driver.StateJson);}
   Assert.AreEqual("APPLIED",reference.CommandStatus("repair-damage"));Assert.Less((int)type.GetProperty("Stage").GetValue(damage),3,"Actual repair must reduce visible condition stage");
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void ThresholdsRepairAndPauseDoNotAccumulateDestruction(){
  var type=typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatTrainDamageView");Assert.NotNull(type);
  var go=new GameObject("train-damage",typeof(RectTransform));
  try{var component=go.AddComponent(type);var set=type.GetMethod("SetCondition");Assert.NotNull(set);
   int[] hp={100,71,70,41,40,21,20,1,0,100};int[] stages={0,0,1,1,2,2,3,3,4,0};
   for(int i=0;i<hp.Length;i++){set.Invoke(component,new object[]{hp[i],100,(float)i});Assert.AreEqual(stages[i],type.GetProperty("Stage").GetValue(component));}
   Assert.NotNull(go.GetComponent<CanvasRenderer>());
   var populate=type.GetMethod("OnPopulateMesh",BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
   using(var mesh=new UnityEngine.UI.VertexHelper()){
    set.Invoke(component,new object[]{15,100,5f});populate.Invoke(component,new object[]{mesh});Assert.Greater(mesh.currentVertCount,0);var before=new System.Collections.Generic.List<UIVertex>();mesh.GetUIVertexStream(before);
    set.Invoke(component,new object[]{15,100,5f});populate.Invoke(component,new object[]{mesh});var after=new System.Collections.Generic.List<UIVertex>();mesh.GetUIVertexStream(after);Assert.AreEqual(before.Count,after.Count);for(int i=0;i<before.Count;i++)Assert.AreEqual(before[i].position,after[i].position,"Same paused simulation time must not animate");
    set.Invoke(component,new object[]{100,100,6f});populate.Invoke(component,new object[]{mesh});Assert.AreEqual(0,mesh.currentVertCount,"Fully repaired skin must be clean");
   }
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
