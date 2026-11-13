#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public sealed class MiraFirstFeedbackTests {
  object controller;MiraJudgementSceneAdapter scene;JObject native,last;int calls,clears;
  static Type ControllerType()=>typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Battle.MiraFirstFeedbackController");
  object Call(string name,params object[] args){try{return controller.GetType().GetMethod(name).Invoke(controller,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  void Bind(){Call("Bind",(Action<JObject>)(p=>{last=p;calls++;}),(Action)(()=>clears++),native);}
  void Observe()=>Call("Observe",native);
  [SetUp]public void Create(){
   Assert.That(ControllerType(),Is.Not.Null,"Read-only feedback controller missing");controller=Activator.CreateInstance(ControllerType());calls=clears=0;last=null;scene=BMiraFirstNativeFixture.Scene();var observation=scene.Observe(0,1);var target=((JArray)observation["targets"])[0];target["eligible"]=true;target["generation"]=1;
   var ray=MiraJudgementGeometry.CreateRay(observation,BDirectAttackInput.Digest(observation),(string)target["id"]);
   // Presentation-only native-shape fixture: never represented as a fired original event.
   native=new JObject{["version"]=BDirectAttackBasicLoop.MiraFirstVersion,["bindingHash"]="test-binding",["state"]=new JObject{["direct"]=new JObject{["clock"]=new JObject{["completedSteps"]=1,["stepTicks"]=100000,["paused"]=false}}},
    ["miraFirstTrial"]=new JObject{["contextHash"]="test-context",["lastObservation"]=observation.DeepClone(),["shots"]=new JArray(new JObject{["id"]="original-fire",["targetId"]=(string)target["id"],["launchTick"]=1,["status"]="FLYING",["ray"]=ray}),["contacts"]=new JArray()}};
  }
  [TearDown]public void Cleanup(){if(scene!=null)UnityEngine.Object.DestroyImmediate(scene.gameObject);}
  JArray Shots=>(JArray)native["miraFirstTrial"]["shots"];
  JArray Pulses=>(JArray)last["pulses"];
  [Test]public void BindAndRebindRestoreFlyingBodyWithoutReplayingOldFlash(){Bind();Assert.That(((JArray)last["flights"]).Count,Is.EqualTo(1));Assert.That(Pulses.Count,Is.Zero);Bind();Assert.That(Pulses.Count,Is.Zero);Assert.That(clears,Is.EqualTo(1));}
  [Test]public void AcceptedNewFireDisplaysOnceAndNeverChangesNativeState(){var shot=Shots[0].DeepClone();Shots.Clear();Bind();Shots.Add(shot);var before=native.DeepClone();Observe();Assert.That(Pulses.Count(p=>(string)p["kind"]=="FIRE"),Is.EqualTo(1));Observe();Assert.That(Pulses.Count,Is.Zero);Assert.That(JToken.DeepEquals(native,before),Is.True);}
  [TestCase(43,0)][TestCase(10,33)][TestCase(0,43)][TestCase(0,0)]public void ContactKeepsShieldAndHpSeparateAndZeroIsNotMiss(int absorbed,int hpLoss){
   Bind();Shots[0]["status"]="HIT";Shots[0]["contact"]=new JObject{["pointMm"]=new JArray(12800,2800,6000)};
   ((JArray)native["miraFirstTrial"]["contacts"]).Add(new JObject{["eventId"]="original-fire:hp",["hp"]=new JObject{["absorbed"]=absorbed,["hpLoss"]=hpLoss,["hpAfter"]=100}});Observe();
   Assert.That(Pulses.Count(p=>(string)p["kind"]=="CONTACT"),Is.EqualTo(1));Assert.That(Pulses.Count(p=>(string)p["kind"]=="SHIELD"),Is.EqualTo(absorbed>0?1:0));Assert.That(Pulses.Count(p=>(string)p["kind"]=="HP"),Is.EqualTo(hpLoss>0?1:0));Observe();Assert.That(Pulses.Count,Is.Zero);
  }
  [TestCase("MISS",0)][TestCase("BLOCKED",1)]public void MissHasNoHitFlashOrNumberAndBlockStopsAtActualPoint(string status,int expected){Bind();Shots[0]["status"]=status;Shots[0]["contact"]=new JObject{["pointMm"]=new JArray(1,2,3)};Observe();Assert.That(Pulses.Count,Is.EqualTo(expected));Assert.That(((JArray)last["flights"]).Count,Is.Zero);if(expected>0)Assert.That(JToken.DeepEquals(Pulses[0]["pointMm"],new JArray(1,2,3)),Is.True);}
  [Test]public void HoldUsesSameOriginalTickPositionAndUnbindRejectsLateCallbacks(){Bind();var before=last["flights"].DeepClone();native["state"]["direct"]["clock"]["paused"]=true;Observe();Assert.That(JToken.DeepEquals(last["flights"],before),Is.True);Call("Unbind");int old=calls;Observe();Assert.That(calls,Is.EqualTo(old));Assert.That(clears,Is.EqualTo(1));}
  [Test]public void IndependentNativeViewOwnsOnlyItsRenderObjectsAndCanHideOrReleaseThem(){
   var t=typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Battle.MiraFirstFeedbackView");Assert.That(t,Is.Not.Null,"Independent native feedback view missing");var go=new GameObject("FirstFeedbackViewTest");
   try{var view=go.AddComponent(t);var bind=t.GetMethod("BindBasis");Assert.That(bind,Is.Not.Null,"Explicit geometry basis binding missing");bind.Invoke(view,new object[]{scene.transform});Call("Bind",(Action<JObject>)(p=>t.GetMethod("ApplyFrame").Invoke(view,new object[]{p})),(Action)(()=>t.GetMethod("Clear").Invoke(view,null)),native);
    Assert.That(go.GetComponentsInChildren<Renderer>().Length,Is.EqualTo(2));t.GetMethod("SetVisible").Invoke(view,new object[]{false});Assert.That(go.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),Is.True);
    t.GetMethod("SetVisible").Invoke(view,new object[]{true});Assert.That(go.GetComponentsInChildren<Renderer>().All(r=>r.enabled),Is.True);Call("Unbind");Assert.That(go.transform.childCount,Is.Zero);
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [Test]public void FutureSecondaryAndPiercingDisplayContractsStayDistinctWithoutCreatingShots(){
   Bind();var frame=(JObject)last.DeepClone();var primary=(JObject)frame["flights"][0];var secondary=(JObject)primary.DeepClone();secondary["id"]="display-fixture-secondary";secondary["kind"]="SECONDARY";var piercing=(JObject)primary.DeepClone();piercing["id"]="display-fixture-pierce";piercing["kind"]="PIERCE";((JArray)frame["flights"]).Add(secondary);((JArray)frame["flights"]).Add(piercing);
   var go=new GameObject("FutureDisplayContractOnly");try{var view=go.AddComponent<MiraFirstFeedbackView>();view.BindBasis(scene.transform);view.ApplyFrame(frame);var lines=go.GetComponentsInChildren<LineRenderer>();var main=lines.Single(l=>l.transform.parent.name.EndsWith("original-fire"));var sub=lines.Single(l=>l.transform.parent.name.EndsWith("display-fixture-secondary"));var pierce=lines.Single(l=>l.transform.parent.name.EndsWith("display-fixture-pierce"));Assert.That(sub.startWidth,Is.LessThan(main.startWidth));Assert.That((pierce.GetPosition(1)-pierce.GetPosition(0)).normalized,Is.EqualTo((main.GetPosition(1)-main.GetPosition(0)).normalized));Assert.That(Shots.Count,Is.EqualTo(1),"Display fixtures cannot create a native shot");}finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [Test]public void SerializedRotatedBasisMustRenderWithoutLosingTheOriginalFloatPose(){
   scene.transform.position=new Vector3(2,.1f,-1);scene.transform.rotation=Quaternion.Euler(0,15,0);native["miraFirstTrial"]["lastObservation"]=scene.Observe(0,1);Bind();var frame=JObject.Parse(last.ToString());var go=new GameObject("SerializedFeedbackBasis");try{var view=go.AddComponent<MiraFirstFeedbackView>();view.BindBasis(scene.transform);Assert.DoesNotThrow(()=>view.ApplyFrame(frame));Assert.That(go.GetComponentsInChildren<Renderer>().Length,Is.EqualTo(2));}finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [Test]public void WrongContextIsRejectedWithoutPublishingNewView(){Bind();int before=calls;native["miraFirstTrial"]["contextHash"]="changed";Assert.Throws<ArgumentException>(Observe);Assert.That(calls,Is.EqualTo(before));}
 }
}
#endif