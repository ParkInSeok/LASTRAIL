#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public class FiveAgentVisualBindingTests {
  static object Create(string name,params object[] args)=>Activator.CreateInstance(FiveAgentVisualReferenceTests.Type(name),args);
  static object Call(object o,string name,params object[] args)=>FiveAgentVisualReferenceTests.Call(o,name,args);
  static Dictionary<string,int> Mapping()=>new Dictionary<string,int>{{"op_eve",2}};
  static BDirectAttackBasicLoopResult OriginalResult(out BDirectAttackBasicLoop root){var i=BEveBasicTests.Input();var binding=(BDirectAttackBasicLoopBinding)typeof(BEveBasicTests).GetMethod("LoopBinding",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{i,"BCB02"});root=new BDirectAttackBasicLoop(binding,1000);return root.Advance("original-eve-basic",10000,1);}
  [Test]public void OriginalAutomaticAttackResultDrivesOneImpactWithoutChangingNativeState(){var model=FiveAgentVisualReferenceTests.Model();var feed=Create("FiveAgentVisualFeed");var controller=Create("FiveAgentVisualController",model);JObject frame=null;Call(controller,"Bind",feed,(Action<JObject>)(f=>frame=f),(Action)(()=>{}),Mapping(),"fixture-original-node",0);var result=OriginalResult(out var root);var original=root.Capture();Func<string,Vector2?> projection=id=>new Vector2(340,800);Call(feed,"PublishResult",result,projection,(Func<long,long>)(tick=>tick));Assert.That(frame["layers"].Count(l=>(string)l["role"]=="authoritativeImpact"),Is.EqualTo(1));Assert.That((int)frame["receiptCount"],Is.EqualTo(1));Call(feed,"PublishResult",result,projection,(Func<long,long>)(tick=>tick));Assert.That((int)frame["receiptCount"],Is.EqualTo(1));Assert.That(JToken.DeepEquals(original,root.Capture()),Is.True);Call(controller,"Unbind");}
  [Test]public void RebindUnbindHoldAndTargetLossOwnOnlyPresentationListeners(){var m=FiveAgentVisualReferenceTests.Model();var feed=Create("FiveAgentVisualFeed");var c=Create("FiveAgentVisualController",m);int draws=0,clears=0;JObject frame=null;Action<JObject> show=f=>{frame=f;draws++;};Action clear=()=>clears++;Call(c,"Bind",feed,show,clear,Mapping(),"node",0);Call(c,"Bind",feed,show,clear,Mapping(),"node",0);int baseline=draws;var state=new CombatUnitAnimationState("op_eve",CombatUnitRole.Operator,"attack","real-cue");Call(feed,"PublishPose",state,3,1700L);Assert.That(draws,Is.EqualTo(baseline+1));var old=frame.DeepClone();Call(feed,"PublishHold",true);Call(feed,"PublishPose",state,1,1800L);Assert.That(JToken.DeepEquals(old,frame),Is.True);Call(feed,"PublishHold",false);Call(feed,"PublishCancel","op_eve");Assert.That(frame["layers"].All(l=>(string)l["role"]!="authoritativeImpact"),Is.True);Call(c,"Unbind");baseline=draws;Call(feed,"PublishPose",state,0,1900L);Assert.That(draws,Is.EqualTo(baseline));Assert.That(clears,Is.GreaterThan(0));}
  [Test]public void IdleCueUsesRestExposureRegardlessOfPriorAttackAge(){
   var model=(FiveAgentVisualReference)FiveAgentVisualReferenceTests.Model();var feed=new FiveAgentVisualFeed();using var c=new FiveAgentVisualController(model);JObject frame=null;
   c.Bind(feed,f=>frame=f,()=>{},Mapping(),"idle-node",0);
   feed.PublishPose(new CombatUnitAnimationState("op_eve",CombatUnitRole.Operator,"attack","attack-cue"),3,1850);
   var attack=frame["layers"].Single(l=>(string)l["role"]=="actor")["path"].Value<string>();
   feed.PublishPose(new CombatUnitAnimationState("op_eve",CombatUnitRole.Operator,"idle","idle-cue"),3,1850);
   var rest=model.SampleReference(3,0,false)["layers"].Single(l=>(int)l["slot"]==2&&(string)l["role"]=="actor")["path"].Value<string>();
   Assert.That((string)frame["layers"].Single(l=>(string)l["role"]=="actor")["path"],Is.EqualTo(rest));Assert.That(rest,Is.Not.EqualTo(attack));
  }
  [Test]public void ReceiptReplayProtectionSpansMoreThan1024HitsAndSameContextRebind(){
   var feed=new FiveAgentVisualFeed();using var c=new FiveAgentVisualController((FiveAgentVisualReference)FiveAgentVisualReferenceTests.Model());JObject frame=null;
   Action<JObject> show=f=>frame=f; c.Bind(feed,show,()=>{},Mapping(),"long-original-node",0);
   var original=OriginalResult(out var root).Hits[0].Attack;var snapshot=root.Capture();
   var ctor=typeof(BDirectAttackReceipt).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single();
   // Display-only receipt identities stress retention, not 1026 simulated gameplay attacks.
   var receipts=Enumerable.Range(0,1026).Select(n=>(BDirectAttackReceipt)ctor.Invoke(new object[]{"display-retention-fixture-"+n,original.SourceId,original.Tick,original.BeforeShield,original.Calculation,original.Hp})).ToArray();
   foreach(var receipt in receipts)feed.PublishImpact(receipt,new Vector2(340,800),10000);
   Assert.That((int)frame["receiptCount"],Is.EqualTo(1026));
   c.Bind(feed,show,()=>{},Mapping(),"long-original-node",0);
   feed.PublishImpact(receipts[0],new Vector2(340,800),10000);
   Assert.That((int)frame["receiptCount"],Is.EqualTo(1026));
   Assert.That(frame["layers"].Any(l=>(string)l["role"]=="authoritativeImpact"),Is.False,"Replayed old impact must not reappear after rebind");
   Assert.That(JToken.DeepEquals(snapshot,root.Capture()),Is.True);
   c.Bind(feed,show,()=>{},Mapping(),"different-node",0);Assert.That((int)frame["receiptCount"],Is.Zero);
  }
  [Test]public void OriginalImpactTargetLossAndHeldClockClearOnlyMatchingPresentation(){
   var feed=new FiveAgentVisualFeed();using var c=new FiveAgentVisualController((FiveAgentVisualReference)FiveAgentVisualReferenceTests.Model());JObject frame=null;
   c.Bind(feed,f=>frame=f,()=>{},Mapping(),"target-lifetime-node",0);
   var hit=OriginalResult(out var root).Hits[0].Attack;var original=root.Capture();
   feed.PublishImpact(hit,new Vector2(340,800),10000);
   feed.PublishTargetLost("different-target");Assert.That(frame["layers"].Any(l=>(string)l["role"]=="authoritativeImpact"),Is.True);
   var heldFrame=frame.DeepClone();feed.PublishHold(true);feed.PublishClock(10160);Assert.That(JToken.DeepEquals(heldFrame,frame),Is.True);
   feed.PublishHold(false);feed.PublishClock(10160);Assert.That(frame["layers"].Any(l=>(string)l["role"]=="authoritativeImpact"),Is.False);
   c.Bind(feed,f=>frame=f,()=>{},Mapping(),"new-target-node",0);
   feed.PublishImpact(hit,new Vector2(340,800),10000);feed.PublishTargetLost(hit.Hp.TargetId);
   Assert.That(frame["layers"].Any(l=>(string)l["role"]=="authoritativeImpact"),Is.False);
   feed.PublishImpact(hit,new Vector2(340,800),10000);Assert.That(frame["layers"].Any(l=>(string)l["role"]=="authoritativeImpact"),Is.False,"Target loss must not permit replayed impact");
   Assert.That(JToken.DeepEquals(original,root.Capture()),Is.True);
  }
  [Test]public void ComparisonCopiesCannotBeMappedToRuntimeRoster(){var c=Create("FiveAgentVisualController",FiveAgentVisualReferenceTests.Model());var feed=Create("FiveAgentVisualFeed");Assert.Throws<ArgumentException>(()=>Call(c,"Bind",feed,(Action<JObject>)(_=>{}),(Action)(()=>{}),new Dictionary<string,int>{{"op_kyle",3}},"node",0));}
 }
 public class FiveAgentVisualAssetTests {
  [Test]public void ScopedBuilderVerifiesEveryReferenceAndDeduplicatesAllPngBytes(){var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LastRail.Editor.FiveAgentVisualReferenceBuilder")).FirstOrDefault(t=>t!=null);Assert.That(type,Is.Not.Null,"Scoped visual consumer builder missing");var report=(JObject)type.GetMethod("Build").Invoke(null,null);Assert.That((int)report["verifiedReferences"],Is.EqualTo(309));Assert.That((int)report["pngPaths"],Is.EqualTo(295));Assert.That((int)report["uniquePngBytes"],Is.EqualTo(173));Assert.That((int)report["newTextures"]+(int)report["reusedTextures"],Is.EqualTo(173));Assert.That((bool)report["referenceBackgroundImported"],Is.False);}
 }
}
#endif