using System;
using System.Linq;
using System.Reflection;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class ReinaBarrageFeedbackPresentationTests {
 static JObject Project(JArray input){
  var type=typeof(FocusFeedbackPresentation).Assembly.GetType("LastRail.Presentation.Battle.ReinaBarrageFeedbackPresentation");Assert.NotNull(type,"Missing independent barrage overlap presentation projection");
  try{return (JObject)type.GetMethod("Project",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{input});}catch(TargetInvocationException error){throw error.InnerException;}
 }
 // Synthetic presentation-only rows: they do not grant casts or provide geometry execution authority.
 static JObject Cast(string id,int nextPulse,string target="target",long generation=1,bool eligible=true,JToken protection=null)=>new JObject{
  ["commandId"]=id,["frame"]=new JObject{["id"]="frame-"+id,["generation"]=1,["originMm"]=new JArray(0,0,0),["basis"]=new JArray(new JArray(1,0,0),new JArray(0,1,0),new JArray(0,0,1))},
  ["centerMm"]=new JArray(5000,500,0),["radiusMm"]=3000,["nextPulse"]=nextPulse,
  ["targets"]=new JArray(new JObject{["id"]=target,["generation"]=generation,["eligible"]=eligible,["protected"]=protection??JValue.CreateNull()})};
 [Test]public void LatestInsertedCastIsPrimaryWithoutResettingOlderProgressOrGeometry(){
  var input=new JArray(Cast("z-old",2),Cast("a-new",0));var result=Project(input);var casts=(JArray)result["casts"];
  CollectionAssert.AreEqual(new[]{"z-old","a-new"},casts.Select(c=>(string)c["commandId"]));
  Assert.IsFalse((bool)casts[0]["isPrimary"]);Assert.IsTrue((bool)casts[0]["simplified"]);Assert.AreEqual(2,(int)casts[0]["remainingPulses"]);
  Assert.IsTrue((bool)casts[1]["isPrimary"]);Assert.IsFalse((bool)casts[1]["simplified"]);Assert.AreEqual(4,(int)casts[1]["remainingPulses"]);
  Assert.IsTrue(JToken.DeepEquals(input[0]["frame"],casts[0]["frame"]));Assert.IsTrue(JToken.DeepEquals(input[1]["centerMm"],casts[1]["centerMm"]));
 }
 [Test]public void SharedTargetGetsOneMarkerWithDistinctPerCastEligibilityAndProtection(){
  var result=Project(new JArray(Cast("old",3,eligible:false,protection:new JValue(false)),Cast("new",1,protection:new JValue(true))));
  var targets=(JArray)result["targets"];Assert.AreEqual(1,targets.Count);Assert.AreEqual("target",(string)targets[0]["id"]);
  var states=(JArray)targets[0]["casts"];Assert.AreEqual(2,states.Count);Assert.IsFalse((bool)states[0]["eligible"]);Assert.IsFalse((bool)states[0]["protected"]);Assert.AreEqual(1,(int)states[0]["remainingPulses"]);
  Assert.IsTrue((bool)states[1]["eligible"]);Assert.IsTrue((bool)states[1]["protected"]);Assert.IsTrue((bool)states[1]["isPrimary"]);
 }
 [Test]public void DifferentGenerationsAndDifferentTargetsAreNotMerged(){var older=Cast("old",0);((JArray)older["targets"]).Add(new JObject{["id"]="other",["generation"]=1,["eligible"]=true,["protected"]=false});var result=Project(new JArray(older,Cast("new",0,generation:2)));Assert.AreEqual(3,result["targets"].Count());}
 [Test]public void EndingOneCastPreservesOtherStateAndPromotesLatestRemaining(){
  var old=Cast("old",2);var newer=Cast("new",1);Project(new JArray(old,newer));var result=Project(new JArray(old));
  Assert.AreEqual(1,result["casts"].Count());Assert.AreEqual("old",(string)result["casts"][0]["commandId"]);Assert.IsTrue((bool)result["casts"][0]["isPrimary"]);Assert.AreEqual(2,(int)result["casts"][0]["remainingPulses"]);Assert.AreEqual(1,result["targets"][0]["casts"].Count());
  var empty=Project(new JArray());Assert.IsEmpty((JArray)empty["casts"]);Assert.IsEmpty((JArray)empty["targets"]);
 }
 [Test]public void ProjectionDoesNotMutateInputOrShareOutputOrInventPixels(){
  var input=new JArray(Cast("one",0));var before=input.DeepClone();var result=Project(input);Assert.IsTrue(JToken.DeepEquals(before,input));
  result["casts"][0]["frame"]["originMm"][0]=999;result["targets"][0]["casts"][0]["eligible"]=false;
  var again=Project(input);Assert.AreEqual(0,(int)again["casts"][0]["frame"]["originMm"][0]);Assert.IsTrue((bool)again["targets"][0]["casts"][0]["eligible"]);Assert.IsNull(again["casts"][0]["screenPosition"]);Assert.AreEqual(JTokenType.Null,again["targets"][0]["casts"][0]["protected"].Type);
 }
 [TestCase("duplicateCast")][TestCase("duplicateTarget")][TestCase("pulse")][TestCase("generation")][TestCase("eligible")][TestCase("protected")][TestCase("frame")][TestCase("center")]
 public void InvalidDisplaySourceFailsWithoutCoalescingItsIdentity(string fault){
  var input=new JArray(Cast("one",0));
  if(fault=="duplicateCast")input.Add(input[0].DeepClone());
  if(fault=="duplicateTarget")((JArray)input[0]["targets"]).Add(input[0]["targets"][0].DeepClone());
  if(fault=="pulse")input[0]["nextPulse"]=5;
  if(fault=="generation")input[0]["targets"][0]["generation"]=0;
  if(fault=="eligible")input[0]["targets"][0]["eligible"]=JValue.CreateNull();
  if(fault=="protected")((JObject)input[0]["targets"][0]).Remove("protected");
  if(fault=="frame")((JObject)input[0]).Remove("frame");
  if(fault=="center")input[0]["centerMm"]=new JArray(0,0);
  Assert.Throws<ArgumentException>(()=>Project(input));
 }
}
