using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryBasicSourceTests {
 public static JObject Fixture(){var x=OrdinaryFiveSeraFactoryTests.Sources().Start;x["ordinaryBasicSource"]=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/ordinary-basic-producer-fixture.json"))["source"].DeepClone();return x;}
 [Test] public void V2KeepsKyleContactSeparateAndUsesEveCarrierWithoutChangingV1(){
  var x=Fixture();x["ordinaryBasicSource"]=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/ordinary-basic-v2-producer-fixture.json"))["source"].DeepClone();
  var input=Read(x);Assert.AreEqual(5,input.Basic.Profiles.Count);
  Assert.AreEqual(3,Read(Fixture()).Basic.Profiles.Count);
  var kyle=input.Basic.Profiles[3];Assert.AreEqual("ACTUAL_HAND_EMISSION_FOR_RANGED",kyle.LaunchSource);
  Assert.AreEqual(1800,kyle.MeleeMaximumDistanceMm);Assert.AreEqual(120,kyle.MeleeContactRadiusMm);Assert.AreEqual("ACTUAL_FIST_ANCHOR_AT_COMMIT",kyle.MeleeContactSource);
  Assert.AreEqual(30000,kyle.SpeedMmPerSecond);Assert.AreEqual(0,kyle.AdjacentCars);
  var period=CombatExactValue.From(new JObject{["numerator"]="1800000",["denominator"]="1"});
  Assert.AreEqual((6L,13L,36L),kyle.Boundaries(0,period,50000));
  Assert.AreEqual("ACTUAL_BASIC_CARRIER_MUZZLE",input.Basic.Profiles[4].LaunchSource);
 }
 static object Basic(OrdinaryFiveStartInput input){var p=typeof(OrdinaryFiveStartInput).GetProperty("Basic");Assert.That(p,Is.Not.Null);return p.GetValue(input);}
 [Test] public void V3AddsMiraTimingWithoutBackfillingHistoricalPackets(){
  var x=Fixture();x["ordinaryBasicSource"]=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/ordinary-basic-v3-producer-fixture.json"))["source"].DeepClone();
  var source=Read(x).Basic;var mira=source.Profiles[2];
  Assert.AreEqual(280,mira.WindupMs);Assert.AreEqual(320,mira.RecoveryMs);
  Assert.AreEqual((6L,12L,36L),mira.Boundaries(0,CombatExactValue.From(new JObject{["numerator"]="1800000",["denominator"]="1"}),50000));
  Assert.AreEqual(1,source.GetType().GetProperty("MiraPrimaryShotsPerOpportunity").GetValue(source));
  Assert.IsNull(Read(Fixture()).Basic.Profiles[2].WindupMs);
 }
 static string Hash(JToken value){using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes((string)typeof(BasicCombatSimulation).GetMethod("StableJson",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{value})))).Replace("-","").ToLowerInvariant();}
 static OrdinaryFiveStartInput Read(JObject x)=>OrdinaryFiveStartInput.Read(x,"a","future-run");
 [Test] public void ActualProducerSourceIsReadAndHistoricalAbsenceIsNotBackfilled(){
  var x=Fixture();var input=Read(x);Assert.NotNull(Basic(input));x.Remove("ordinaryBasicSource");Assert.IsNull(Basic(Read(x)));Assert.NotNull(Basic(input));
 }
 [TestCase("value")][TestCase("account")][TestCase("run")][TestCase("kit")][TestCase("origin")][TestCase("version")][TestCase("null")]
 public void ChangedOrRehashedSourceRejects(string field){
  var x=Fixture();var source=(JObject)x["ordinaryBasicSource"];var packet=(JObject)source["packet"];
  if(field=="null")x["ordinaryBasicSource"]=JValue.CreateNull();
  else{if(field=="value")packet["profiles"][0]["ballistics"]["radiusMm"]=99;
   else packet[field=="account"?"accountId":field=="run"?"runId":field=="kit"?"kitHash":field=="origin"?"originHash":"version"]="wrong";
   source["basicHash"]=Hash(packet);}
  Assert.Throws<ArgumentException>(()=>Read(x));
 }
 [Test] public void NewPinChangesFactoryIdentityButAbsenceStillUsesHistoricalConfiguration(){
  var sources=OrdinaryFiveSeraFactoryTests.Sources();var oldFactory=new OrdinaryFiveSeraFactory(sources.Input,sources.Stats,500000);
  var pinned=new OrdinaryFiveSeraFactory(Read(Fixture()),sources.Stats,500000);
  Assert.AreNotEqual(oldFactory.ConfigurationVersion,pinned.ConfigurationVersion);
  Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(pinned.Create().CaptureLocalState(),oldFactory.Create));
 }
 [Test] public void TypedProfilesUsePinnedTimingAndDoNotInventMissingMiraTiming(){
  var x=Fixture();var input=Read(x);var reina=input.Basic.Profiles[0];
  Assert.AreEqual("op_reina.basic",reina.AbilityId);Assert.AreEqual(72000,reina.SpeedMmPerSecond);Assert.AreEqual(35,reina.RadiusMm);
  var period=CombatExactValue.From(new JObject{["numerator"]="1400000",["denominator"]="1"});
  Assert.AreEqual((4L,8L,28L),reina.Boundaries(0,period,50000));
  Assert.Throws<InvalidOperationException>(()=>input.Basic.Profiles[2].Boundaries(0,period,50000));
  x["ordinaryBasicSource"]["packet"]["profiles"][0]["ballistics"]["radiusMm"]=999;Assert.AreEqual(35,reina.RadiusMm);
 }
 [Test] public void ProductJournalCannotBindAFactoryWithADifferentBasicPin(){
  var old=OrdinaryFiveSeraFactoryTests.Sources();var x=Fixture();x["operationPreviewId"]="preview";
  var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]="future-run",["runStatus"]="ACTIVE",["startResponse"]=x,["errorCode"]=null};
  var pending=new LastRail.Presentation.MissionShell.PendingAccountAction("a","departure-start","preview",0,"key",new JObject{["operationPreviewId"]="preview"}.ToString(),response.ToString());
  var store=CombatCheckpointStore.CreateExplicitLocalQaFile("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/BasicPinTests/"+Guid.NewGuid().ToString("N")+".json");
  Assert.Throws<ArgumentException>(()=>new OrdinaryFiveSeraCheckpoint(store,pending,new OrdinaryFiveSeraFactory(old.Input,old.Stats,500000),new LastRail.Diagnostics.AppBuildSnapshot("test",1)));
 }
}
