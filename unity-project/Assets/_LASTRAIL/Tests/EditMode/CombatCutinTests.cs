using System;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using Newtonsoft.Json.Linq;
public class CombatCutinTests {
 [Test] public void MarkerWarningSkipAndNextCastDoNotReopenFirstUse(){
  bool saved=false;var p=new CombatPresentationPolicy(()=>saved,()=>{saved=true;return true;});
  Assert.IsTrue(p.Begin("one",0,.6,true));Assert.IsFalse(p.Full);Assert.IsTrue(saved);
  p.Skip();Assert.IsFalse(p.Active);Assert.IsFalse(p.Begin("one",1,.6,false));
  p.Begin("two",2,.6,false);Assert.IsFalse(p.Full);
  p.Mode=CombatPresentationMode.Full;Assert.IsFalse(p.Full,"setting only applies to next cast");
  p.Begin("three",3,.6,false);Assert.IsTrue(p.Full);Assert.AreEqual(.4,p.Duration);
  p.Update(3.5);Assert.IsFalse(p.Active);
  var restored=new CombatPresentationPolicy(()=>saved,()=>true);restored.Begin("four",4,.6,false);Assert.IsFalse(restored.Full);
 }
 [Test] public void MarkerFailureAndShortWindupFailTowardSmallDecoration(){
  var p=new CombatPresentationPolicy(()=>false,()=>throw new Exception());
  p.Begin("one",0,.6,false);Assert.IsFalse(p.Full);p.Skip();p.Begin("two",1,.6,false);Assert.IsFalse(p.Full);
  var q=new CombatPresentationPolicy(()=>false,()=>true);q.Begin("short",0,.05,false);Assert.AreEqual(.05,q.Duration);
  var r=new CombatPresentationPolicy(()=>false,()=>true,true);r.Begin("restored",0,.6,false);Assert.IsFalse(r.Full);
 }
 [Test] public void ApprovedNamesAndReadOnlyCastIdentityExist(){
  Assert.AreEqual("최후의 방벽",CombatSkillNames.Get("op_kyle.ultimate"));
  var s=CombatPresentationConnectionTests.Fresh();var row=s.ReadHudState()["skills"][0];
  s.RequestSkill("presentation-identity",(string)row["actor"],(string)row["ability"]);
  for(int i=0;i<500&&s.CommandStatus("presentation-identity")=="QUEUED";i++)s.Advance(500000,1);
  foreach(var c in (JArray)s.ReadHudState()["casts"]){Assert.NotNull(c["ability"]);Assert.NotNull(c["windupTicks"]);}
 }
 [Test] public void SkipOnlyChangesPresentation(){
  var method=typeof(ApprovedCombatView).GetMethod("SkipCurrentPresentation");Assert.NotNull(method,"Current decoration must be skippable");
  var d=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);d.Open();
  var go=new GameObject("CutinTest",typeof(RectTransform),typeof(Canvas));
  try{var v=go.AddComponent<ApprovedCombatView>();v.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var before=d.StateJson;for(int i=0;i<10;i++)method.Invoke(v,null);Assert.AreEqual(before,d.StateJson);}finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
