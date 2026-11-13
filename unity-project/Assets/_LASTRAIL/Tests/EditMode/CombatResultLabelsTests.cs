#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
public sealed class CombatResultLabelsTests {
 static JObject Read(JObject row) {
  var t=typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Battle.CombatResultLabels");
  Assert.That(t,Is.Not.Null,"Approved result-label projector missing");
  try{return (JObject)t.GetMethod("Read").Invoke(null,new object[]{row});}catch(TargetInvocationException e){throw e.InnerException;}
 }
 static JObject Row(string kind,int hp,int absorb,int heal,string critical="NOT_APPLICABLE")=>new JObject{["kind"]=kind,["actualResult"]=new JObject{["hpLoss"]=hp,["absorbed"]=absorb,["actualHpGain"]=heal,["criticalState"]=critical}};
 [TestCase(50,30,"50","30")][TestCase(0,100,"","100")][TestCase(0,0,"","")]
 public void ActualHpAndAbsorptionStaySeparateWithoutBudgetOrOverkill(int hp,int shield,string hpText,string shieldText){
  var row=Row("DAMAGE",hp,shield,0);row["actualResult"]["overkill"]=20;var before=row.DeepClone();var r=Read(row);
  Assert.That((string)r["hpText"],Is.EqualTo(hpText));Assert.That((string)r["shieldText"],Is.EqualTo(shieldText));
  Assert.That((bool)r["contact"],Is.True);Assert.That((bool)r["miss"],Is.False);Assert.That(JToken.DeepEquals(row,before),Is.True);
 }
 [TestCase(35,"+35")][TestCase(0,"")]public void HealingUsesOnlyActualGainNeverAttemptedOrOverflow(int gain,string text){var row=Row("HEAL",0,0,gain);row["actualResult"]["attempted"]=120;row["actualResult"]["overheal"]=85;Assert.That((string)Read(row)["healText"],Is.EqualTo(text));}
 [Test]public void CriticalAbsorptionAndControlImmunityDoNotHideDamage(){var row=Row("DAMAGE",80,30,0,"CRITICAL");row["actualResult"]["rejectedEffects"]=new JArray(new JObject{["effectId"]="fixture-control",["reason"]="IMMUNE"});var r=Read(row);Assert.That((bool)r["critical"],Is.True);Assert.That((string)r["hpText"],Is.EqualTo("80"));Assert.That(((JArray)r["rejectedEffects"]).Count,Is.EqualTo(1));Assert.That((string)Read(Row("DAMAGE",0,100,0,"CRITICAL"))["shieldText"],Is.EqualTo("100"));}
 [Test]public void MissingCriticalIsUnknownAndInvalidFactsAreRejected(){var row=Row("DAMAGE",1,0,0);((JObject)row["actualResult"]).Remove("criticalState");Assert.That((string)Read(row)["criticalState"],Is.EqualTo("UNKNOWN"));row["actualResult"]["hpLoss"]=-1;Assert.Throws<ArgumentException>(()=>Read(row));row["actualResult"]["hpLoss"]=1.5;Assert.Throws<ArgumentException>(()=>Read(row));}
 [Test]public void NativeMiraContactHasLabelsFromReceiptAtOriginalPointAndGeneration(){
  var scene=BMiraFirstNativeFixture.Scene();try{
   var obs=scene.Observe(0,1);var target=((JArray)obs["targets"])[0];target["eligible"]=true;target["generation"]=1;
   var ray=MiraJudgementGeometry.CreateRay(obs,BDirectAttackInput.Digest(obs),(string)target["id"]);
   var shot=new JObject{["id"]="fixture-fire",["targetId"]=target["id"].DeepClone(),["sourceGeneration"]=1,["targetGeneration"]=1,["launchTick"]=1,["status"]="FLYING",["ray"]=ray};
   var node=new JObject{["version"]=BDirectAttackBasicLoop.MiraFirstVersion,["bindingHash"]="fixture-binding",["state"]=new JObject{["direct"]=new JObject{["clock"]=new JObject{["completedSteps"]=1,["stepTicks"]=100000,["paused"]=false}}},["miraFirstTrial"]=new JObject{["contextHash"]="fixture-context",["lastObservation"]=obs,["shots"]=new JArray(shot),["contacts"]=new JArray()}};
   var c=new MiraFirstFeedbackController();JObject frame=null;c.Bind(x=>frame=x,()=>{},node);shot=(JObject)node["miraFirstTrial"]["shots"][0];shot["status"]="HIT";shot["contact"]=new JObject{["pointMm"]=new JArray(12800,2800,6000)};
   ((JArray)node["miraFirstTrial"]["contacts"]).Add(new JObject{["eventId"]="fixture-fire:hp",["hp"]=new JObject{["hpLoss"]=50,["absorbed"]=30}});
   var before=node.DeepClone();c.Observe(node);var p=((JArray)frame["pulses"]).Single(x=>(string)x["kind"]=="CONTACT");
   Assert.That(p["labels"],Is.Not.Null,"Original receipt label binding missing");Assert.That((string)p["labels"]["hpText"],Is.EqualTo("50"));Assert.That((string)p["labels"]["shieldText"],Is.EqualTo("30"));Assert.That((long)p["targetGeneration"],Is.EqualTo(1));Assert.That(JToken.DeepEquals(p["pointMm"],shot["contact"]["pointMm"]),Is.True);
   var go=new GameObject("receipt-label-view");try{var v=go.AddComponent<MiraFirstFeedbackView>();v.BindBasis(scene.transform);v.ApplyFrame(frame);var texts=go.GetComponentsInChildren<TextMesh>();Assert.That(texts.Select(t=>t.text),Does.Contain("50"));Assert.That(texts.Select(t=>t.text),Does.Contain("[30]"));Assert.That(texts.All(t=>t.GetComponent<Collider>()==null),Is.True);Assert.That(texts.All(t=>(t.transform.lossyScale-Vector3.one).sqrMagnitude<.000001f),Is.True,"Result labels must not inherit physical contact-core scale");}finally{UnityEngine.Object.DestroyImmediate(go);}
   c.Observe(node);Assert.That(((JArray)frame["pulses"]).Count,Is.Zero);c.Bind(x=>frame=x,()=>{},node);Assert.That(((JArray)frame["pulses"]).Count,Is.Zero);Assert.That(JToken.DeepEquals(node,before),Is.True);c.Unbind();
  }finally{UnityEngine.Object.DestroyImmediate(scene.gameObject);}
 }
 [Test]public void DecorationReductionPreservesCoreAndNeverChangesSourceFrame(){
  var scene=BMiraFirstNativeFixture.Scene();var go=new GameObject("readability-fixture");try{
   var v=go.AddComponent<MiraFirstFeedbackView>();v.BindBasis(scene.transform);
   var obs=scene.Observe(0,1);var f=new JObject{["version"]="mira.first-feedback-frame.v1.20261015",["basis"]=obs["basis"].DeepClone(),["tick"]=1,["flights"]=new JArray(new JObject{["id"]="fixture-flight",["kind"]="MAIN",["originMm"]=new JArray(0,0,0),["anchorMm"]=new JArray(0,0,1000),["positionMm"]=new JArray(0,0,500)}),["pulses"]=new JArray()};
   v.ApplyFrame(f);var before=f.DeepClone();var method=v.GetType().GetMethod("SetDecorationReduced");Assert.That(method,Is.Not.Null,"Core/decoration independent control missing");
   method.Invoke(v,new object[]{true});var rs=go.GetComponentsInChildren<Renderer>();Assert.That(rs.Single(r=>r.name=="Body").enabled,Is.True);Assert.That(rs.Single(r=>r.name=="ShortTrail").enabled,Is.False);
   v.SetVisible(false);method.Invoke(v,new object[]{false});Assert.That(rs.All(r=>!r.enabled),Is.True);v.SetVisible(true);Assert.That(rs.All(r=>r.enabled),Is.True);Assert.That(JToken.DeepEquals(f,before),Is.True);
  }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(scene.gameObject);}
 }
}
}
#endif
