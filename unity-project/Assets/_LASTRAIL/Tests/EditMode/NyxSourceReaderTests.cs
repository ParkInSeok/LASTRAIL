using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class NyxSourceReaderTests {
 internal static JObject Contract()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/nyx-node-restart-source-contract-20261107.json"));
 internal static string Hash(JToken value)=>BDirectAttackInput.Digest(value).Substring(7);
 // Parser fixture only: no actual START is issued or made executable from this producer sample.
 internal static JObject Input(){var source=Contract();var origin=source["ordinaryStatSource"]["origin"];var start=new JObject{["accepted"]=true,["startStatus"]="STARTED",["runId"]=origin["runId"].DeepClone()};
  foreach(string key in new[]{"ordinaryStatSource","ordinaryKitSource","ordinaryBasicSource","ordinaryRelocationSource","admission"})start[key]=source[key].DeepClone();
  start["admission"]["formationRevision"]=origin["formationRevision"].DeepClone();start["admission"]["loadoutRevision"]=origin["loadoutRevision"].DeepClone();return start;
 }
 static OrdinaryFiveStartInput Read(JObject input)=>OrdinaryFiveStartInput.Read(input,(string)input["ordinaryStatSource"]["origin"]["accountId"],(string)input["runId"]);
 [Test] public void Approved48mCompanionIsPinnedByNewRelocationWithoutChangingBasic(){
  var input=Input();var source=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/nyx-48m-owned-source-contract-20261107.json"));input["ordinaryRelocationSource"]=source["ordinaryRelocationSource"].DeepClone();
  var read=Read(input);Assert.AreEqual("combat.ordinary-relocation.v10.20261107",read.Relocation.SourceVersion);Assert.AreEqual(24000,read.Basic.Profiles.Single(x=>x.OperatorId=="op_nyx").MaximumDistanceMm);
  Assert.AreEqual("126e5387830a0e71c9dcc55dccf1f2fca9c5f33b02355e284204dad687b5999b",read.Relocation.GetType().GetProperty("ZoneSelectionHash").GetValue(read.Relocation));
  var companion=input["ordinaryRelocationSource"]["packet"]["zoneSelectionSource"];companion["packet"]["geometry"]["maximumDistanceMm"]=48001;companion["sourceHash"]=Hash(companion["packet"]);input["ordinaryRelocationSource"]["relocationHash"]=Hash(input["ordinaryRelocationSource"]["packet"]);Assert.Throws<ArgumentException>(()=>Read(input));
 }
 [Test] public void NewPinnedSourcesReadRequestedRosterAndKeepSharedTiming(){
  var input=Input();var read=Read(input);CollectionAssert.AreEqual(new[]{"op_reina","op_kyle","op_eve","op_sera","op_nyx"},read.Actors.Select(a=>a.OperatorId));
  Assert.AreEqual("combat.ordinary-five-kit.v3.20261107",read.KitSourceVersion);Assert.NotNull(read.ReinaBarrage);
  Assert.AreEqual("combat.ordinary-basic.v5.20261107",read.Basic.SourceVersion);Assert.AreEqual("combat.ordinary-relocation.v9.20261107",read.Relocation.SourceVersion);
  var p=read.Basic.Profiles.Single(x=>x.OperatorId=="op_nyx");Assert.AreEqual((6L,12L,36L),p.Boundaries(0,CombatExactValue.From(new JObject{["numerator"]="1800000",["denominator"]="1"}),50000));Assert.AreEqual(24000,p.MaximumDistanceMm);Assert.AreEqual(48000,p.SpeedMmPerSecond);
  Assert.AreEqual(2,read.Relocation.SkillScopes.Count(x=>x.AbilityId.StartsWith("op_nyx.")));Assert.IsFalse(read.Relocation.SkillScopes.Any(x=>x.AbilityId.StartsWith("op_mira.")));
  input["ordinaryKitSource"]["packet"]["operators"][4]["abilities"][0]["mitigationRateReductionBps"]=0;Assert.AreEqual(800,(int)read.Actors[4].Ability("op_nyx.active")["mitigationRateReductionBps"]);
 }
 [Test] public void SourceOnlyContractCannotPretendToBeIssuedStart(){var c=Contract();Assert.IsFalse((bool)c["canStart"]);Assert.Throws<ArgumentException>(()=>OrdinaryFiveStartInput.Read(c,"a","nyx-run"));}
 [TestCase("kit")][TestCase("basic")][TestCase("relocation")][TestCase("old-basic")][TestCase("old-relocation")]
 public void RehashedModifiedDefinitionOrSupersededVersionIsRejected(string fault){
  var input=Input();if(fault=="kit"){var p=input["ordinaryKitSource"]["packet"];p["operators"][4]["abilities"][0]["mitigationRateReductionBps"]=700;input["ordinaryKitSource"]["kitHash"]=Hash(p);}
  else if(fault.Contains("basic")){var p=input["ordinaryBasicSource"]["packet"];if(fault=="old-basic")p["version"]="combat.ordinary-basic.v4.20261107";else p["profiles"][4]["ballistics"]["speedMmPerSecond"]=999;input["ordinaryBasicSource"]["basicHash"]=Hash(p);}
  else {var p=input["ordinaryRelocationSource"]["packet"];if(fault=="old-relocation")p["version"]="combat.ordinary-relocation.v8.20261107";else p["nyxZone"]["followsTarget"]=true;input["ordinaryRelocationSource"]["relocationHash"]=Hash(p);}
  Assert.Throws<ArgumentException>(()=>Read(input));
 }
 [Test] public void NyxCoreObservationRequiresExplicitOriginVersionAndExactSlotOrder(){
  var f=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/Stats589/fixture.json"));var row=(JObject)f["first"];var packet=(JObject)row["packet"];var state=(JObject)row["state"];
  string[] ids={"op_reina","op_kyle","op_eve","op_sera","op_nyx"};for(int i=0;i<5;i++){packet["actors"][i]["operatorId"]=ids[i];packet["actors"][i]["slot"]=i;state["actors"][i]["operatorId"]=ids[i];}
  void Pin(){packet["sourceHash"]=Hash(state);var body=(JObject)packet.DeepClone();body.Remove("packetHash");packet["packetHash"]=Hash(body);}Pin();
  var method=typeof(OrdinaryFiveCoreStatSnapshot).GetMethods().SingleOrDefault(m=>m.Name=="Read"&&m.GetParameters().Length==7);Assert.NotNull(method,"Explicit new origin reader required");
  object ReadCore(){try{return method.Invoke(null,new object[]{packet,state,(string)packet["packetHash"],(string)packet["accountId"],(string)packet["runId"],(string)packet["originHash"],"combat.ordinary-five-core-stats.v2.20261107"});}catch(TargetInvocationException e){throw e.InnerException;}}
  Assert.NotNull(ReadCore());Assert.Throws<ArgumentException>(()=>OrdinaryFiveCoreStatSnapshot.Read(packet,state,(string)packet["packetHash"],(string)packet["accountId"],(string)packet["runId"],(string)packet["originHash"]));
  packet["actors"][0]["slot"]=1;packet["actors"][1]["slot"]=0;Pin();Assert.Throws<ArgumentException>(()=>ReadCore());
 }
}
