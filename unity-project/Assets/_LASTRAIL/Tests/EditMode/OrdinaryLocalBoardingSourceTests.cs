using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryLocalBoardingSourceTests {
 const string Root="D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/kyle-right-01/";
 const string Expected="bc2ce839afcb18a4cf43c6b7f81f774e3c499996a1b234e5feddc6dcbb1c7d0e";
 static JObject Envelope()=>JObject.Parse(File.ReadAllText(Root+"boarding-source-v01.json"));
 static object Read(JObject envelope,string expected=Expected){
  var input=(JObject)JObject.Parse(File.ReadAllText(Root+"plan-pointer-retry/source-response.json"))["source"];
  input["inputs"]=new JArray();var factory=OrdinaryLiveExecutionTests.CreateFactory(input);var original=factory.Original;
  var nodeInput=typeof(OrdinaryFiveCombatFactory).GetProperty("NodeInput",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(factory);
  var node=(OrdinaryNodeStart)nodeInput.GetType().GetProperty("Source",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(nodeInput);
  var type=typeof(OrdinaryFiveStartInput).Assembly.GetType("LastRail.Run.OrdinaryLocalBoardingSource");
  Assert.NotNull(type,"The issued companion needs a validated consumer before it can enter a live factory.");
  try{return type.GetMethod("Read").Invoke(null,new object[]{envelope,expected,original,node});}catch(TargetInvocationException e){throw e.InnerException;}
 }
 [Test] public void IssuedSourceRetainsFullWavesAndDoesNotGrantRuntimeAdmission(){
  var envelope=Envelope();var source=Read(envelope);var type=source.GetType();
  var boarding=(BBoardingEventsNodeSnapshot)type.GetProperty("Boarding").GetValue(source);
  CollectionAssert.AreEqual(new[]{8,7},boarding.OriginalSource.Waves.Select(w=>w.Count));
  Assert.AreEqual(15,boarding.OriginalSource.Waves.SelectMany(w=>w.Instances).Select(i=>i.InstanceId).Distinct().Count());
  Assert.IsFalse((bool)type.GetProperty("CanStart").GetValue(source));
  Assert.IsFalse((bool)type.GetProperty("RuntimeSupported").GetValue(source));
  envelope["packet"]["boarding"]["originalSource"]["waves"][0]["instances"][0]["hp"]="0";
  Assert.IsTrue(boarding.OriginalSource.Waves[0].Instances[0].Hp>0);
 }
 [Test] public void IndependentlyPinnedHashIsRequired(){Assert.Throws<ArgumentException>(()=>Read(Envelope(),new string('0',64)));}
 [TestCase("accountId")][TestCase("runId")][TestCase("nodeId")][TestCase("startSnapshotHash")]
 [TestCase("railSourceHash")][TestCase("roofSourceHash")][TestCase("physicalHostIds")]
 [TestCase("executionOrigin")][TestCase("canStart")][TestCase("registration")]
 public void ResealedCompanionCannotChangeOriginalBinding(string field){
  var envelope=Envelope();var packet=(JObject)envelope["packet"];
  if(field=="physicalHostIds")packet[field]=new JArray(((JArray)packet[field]).Reverse().Select(v=>v.DeepClone()));
  else if(field=="executionOrigin")packet[field]["determinism"]["seed"]=1;
  else if(field=="canStart")packet[field]=true;
  else packet[field]="changed";
  string hash=BDirectAttackInput.Digest(packet).Substring(7);envelope["sourceHash"]=hash;
  Assert.Throws<ArgumentException>(()=>Read(envelope,hash));
 }
 [Test] public void CompanionDoesNotBypassOriginalGeneratedWaveVerification(){
  var envelope=Envelope();var packet=(JObject)envelope["packet"];
  packet["boarding"]["originalSource"]["waves"][0]["instances"][0]["hp"]="1";
  string hash=BDirectAttackInput.Digest(packet).Substring(7);envelope["sourceHash"]=hash;
  Assert.Throws<ArgumentException>(()=>Read(envelope,hash));
 }
}
