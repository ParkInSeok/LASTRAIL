#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
 public sealed class BMiraFirstTestSourceTests
 {
  static JObject Fixture()=>JObject.Parse(File.ReadAllText(Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../QA/MiraFirstStatic-20261015/server-packets.json"))));
  static JObject Entry(string name="base")=>(JObject)Fixture()["packets"][name].DeepClone();
  static JObject Packet(string name="base")=>(JObject)Entry(name)["packet"];
  // Independent originals come from the fixture supplier, never from candidate's nested claims.
  static object Read(JObject p,JObject original=null,string hash=null,string account="a",string run=null,JObject mira=null,string miraHash=null,string departureHash=null,string fixture="base") {
   var entry=Entry(fixture);var trusted=(JObject)entry["packet"];
   var type=typeof(CombatVitals).Assembly.GetType("LastRail.Battle.BMiraFirstTestSource");
   Assert.That(type,Is.Not.Null,"Independent Mira first-test static consumer is missing.");
   try{return type.GetMethod("Read").Invoke(null,new object[]{p,original??p,hash??(string)p?["packetHash"],account,run??(string)trusted["runId"],mira??(JObject)entry["originalMira"],miraHash??(string)entry["originalMira"]["packetHash"],departureHash??(string)entry["departurePin"]["pinHash"]});}
   catch(TargetInvocationException e){throw e.InnerException;}
  }
  static object Call(object o,string method)=>o.GetType().GetMethod(method).Invoke(o,null);
  static object Get(object o,string property)=>o.GetType().GetProperty(property).GetValue(o);
  static JObject Capture(object o)=>(JObject)Call(o,"Capture");
  static void Rehash(JObject p){var body=(JObject)p.DeepClone();body.Remove("packetHash");p["packetHash"]=BDirectAttackInput.Digest(body).Substring(7);}
  static void RehashComponent(JObject p){Rehash((JObject)p["originalComponent"]);p["originalComponentHash"]=p["originalComponent"]["packetHash"].DeepClone();Rehash(p);}
  [Test] public void ActualProducerStaticStatsAreAdmittedWithoutCreatingRuntimeReadiness() {
   var p=Packet();var s=Read(p);var stats=(JObject)Call(s,"StaticStats");
   Assert.That((long)stats["maxHp"],Is.EqualTo(1000L));Assert.That((string)stats["attack"]["numerator"],Is.EqualTo("30"));
   Assert.That((string)stats["defense"]["numerator"],Is.EqualTo("111"));Assert.That((long)stats["basicIntervalMs"],Is.EqualTo(1800L));
   Assert.That(Get(s,"StaticStatsReady"),Is.EqualTo(true));Assert.That(Get(s,"StartReady"),Is.EqualTo(false));
   Assert.That(JToken.DeepEquals(Capture(s),p),Is.True);
  }
  [Test] public void RealGrowthRemainsExactAndOwnBasicIsStillUnapplied() {
   var s=Read(Packet("grown"),fixture:"grown");var p=Capture(s);var stats=(JObject)Call(s,"StaticStats");
   Assert.That((long)stats["maxHp"],Is.EqualTo(1428L));Assert.That((string)stats["attack"]["numerator"],Is.EqualTo("16569"));
   Assert.That((string)stats["attack"]["denominator"],Is.EqualTo("400"));
   Assert.That((int)p["originalComponent"]["passive"]["effect"]["valueBps"],Is.EqualTo(1000));
   Assert.That((int)p["originalComponent"]["passive"]["nonEquipmentOutgoingCapBps"],Is.EqualTo(3000));
   Assert.That((bool)p["originalComponent"]["passive"]["appliedToStaticStats"],Is.False);
  }
  [Test] public void GenericUnknownResearchAndStrictBasicSourceStayUnpromoted() {
   var p=Packet();Read(p);Assert.That((string)p["originalComponent"]["research"]["status"],Is.EqualTo("SOURCE_OWNER_UNAVAILABLE"));
   var basic=(JObject)Entry()["originalMira"];var s=BMiraBasicSource.Read(basic,basic,(string)basic["packetHash"],"a");
   Assert.That(s.StartReady,Is.False);Assert.Throws<InvalidOperationException>(()=>s.RequireRuntimeReady());
  }
  [Test] public void WrongIndependentAccountRunHashMiraAndDepartureBindingsAreRejected() {
   var p=Packet();Assert.Throws<ArgumentException>(()=>Read(p,account:"b"));Assert.Throws<ArgumentException>(()=>Read(p,run:"other"));
   Assert.Throws<ArgumentException>(()=>Read(p,hash:new string('0',64)));Assert.Throws<ArgumentException>(()=>Read(p,miraHash:new string('0',64)));
   Assert.Throws<ArgumentException>(()=>Read(p,mira:(JObject)Entry("grown")["originalMira"]));Assert.Throws<ArgumentException>(()=>Read(p,departureHash:new string('0',64)));
  }
  [Test] public void RehashedCandidateCannotReplaceFrozenTrustedOriginal() {
   var original=Packet();var p=(JObject)original.DeepClone();p["effectiveStats"]["attack"]["numerator"]="999";Rehash(p);
   Assert.Throws<ArgumentException>(()=>Read(p,original,(string)original["packetHash"]));Assert.Throws<ArgumentException>(()=>Read(p,original,(string)p["packetHash"]));
  }
  [Test] public void NestedComponentHashMustCoverItsActualBody() {
   var p=Packet();p["originalComponent"]["runSnapshotHash"]=new string('0',64);Rehash(p);
   Assert.Throws<ArgumentException>(()=>Read(p));
  }
  [Test] public void SelfDeclaredNestedMiraCannotReplaceIndependentBasicOriginal() {
   var p=Packet();p["originalComponent"]["originalMira"]=Entry("grown")["originalMira"].DeepClone();
   p["originalComponent"]["originalMiraHash"]=p["originalComponent"]["originalMira"]["packetHash"].DeepClone();RehashComponent(p);
   Assert.Throws<ArgumentException>(()=>Read(p));
  }
  [Test] public void VersionProfileAndResearchExceptionAreLimitedToApprovedContract() {
   foreach(var path in new[]{"version","profile","research.status","research.policyVersion","research.policySource","statStage","startBinding"}) {
    var p=Packet();p.SelectToken(path).Replace("UNAPPROVED");Rehash(p);Assert.Throws<ArgumentException>(()=>Read(p),path);
   }
   var q=Packet();q["research"]["modifiers"]=new JArray();Rehash(q);Assert.Throws<ArgumentException>(()=>Read(q));
  }
  [Test] public void UnsupportedOrNonreadyPacketsNeverExposeStaticInput() {
   var p=Packet();p["unsupported"]=new JArray("FULL_EQUIPMENT_EFFECT_COMPOSITION_UNAVAILABLE");Rehash(p);Assert.Throws<ArgumentException>(()=>Read(p));
   p=Packet();p["staticStatsReady"]=false;p["effectiveStats"]=null;Rehash(p);Assert.Throws<ArgumentException>(()=>Read(p));
  }
  [Test] public void NonemptyActualMiraSlotsOrCommanderEffectsAreRejected() {
   var p=Packet();p["originalComponent"]["equipment"]["slots"][0]=new JObject();RehashComponent(p);Assert.Throws<ArgumentException>(()=>Read(p));
   p=Packet();p["originalComponent"]["equipment"]["equippedEmpty"]=false;RehashComponent(p);Assert.Throws<ArgumentException>(()=>Read(p));
   p=Packet();p["originalComponent"]["equipment"]["blocked"]=new JArray(new JObject());RehashComponent(p);Assert.Throws<ArgumentException>(()=>Read(p));
   p=Packet();p["originalComponent"]["commander"]["snapshot"]=new JObject{["ranks"]=new JObject{["x"]=1}};RehashComponent(p);Assert.Throws<ArgumentException>(()=>Read(p));
  }
  [Test] public void CoreStatsAndConfigurationMustMatchOriginalComponentEvidence() {
   foreach(var path in new[]{"effectiveStats.attack.numerator","configuration.inventoryHash","configuration.departurePinHash","configuration.commanderSourceHash"}) {
    var p=Packet();p.SelectToken(path).Replace("999");Rehash(p);Assert.Throws<ArgumentException>(()=>Read(p),path);
   }
   var q=Packet();q["configuration"]["inventoryRevision"]=99;Rehash(q);Assert.Throws<ArgumentException>(()=>Read(q));
  }
  [Test] public void StaticStatsCannotSilentlyApplyOwnBasicOrChangeOriginalBase() {
   var p=Packet();p["originalComponent"]["coreStats"]["attack"]["numerator"]="33";p["effectiveStats"]["attack"]["numerator"]="33";RehashComponent(p);
   Assert.Throws<ArgumentException>(()=>Read(p));
   p=Packet();p["originalComponent"]["passive"]["appliedToStaticStats"]=true;RehashComponent(p);Assert.Throws<ArgumentException>(()=>Read(p));
  }
  [Test] public void RuntimeFlagsMissingJudgementsAndUnknownFieldsCannotBePromoted() {
   foreach(var path in new[]{"runtimeSupported","canStart","originalComponent.runtimeSupported","originalComponent.canStart"}) {
    var p=Packet();p.SelectToken(path).Replace(true);RehashComponent(p);Assert.Throws<ArgumentException>(()=>Read(p),path);
   }
   var q=Packet();q["missing"]=new JArray();Rehash(q);Assert.Throws<ArgumentException>(()=>Read(q));
   q=Packet();q["critical"]=false;Rehash(q);Assert.Throws<ArgumentException>(()=>Read(q));
  }
  [Test] public void InputAndReturnedCopiesCannotRewriteFrozenStaticInput() {
   var p=Packet();var original=(JObject)p.DeepClone();var s=Read(p,original);p["effectiveStats"]["maxHp"]=1;original["profile"]="changed";
   ((JObject)Call(s,"StaticStats"))["maxHp"]=2;Capture(s)["effectiveStats"]["maxHp"]=3;
   Assert.That((long)((JObject)Call(s,"StaticStats"))["maxHp"],Is.EqualTo(1000L));
  }
  [Test] public void MissingOrOversizedInputIsRejected() {
   Assert.Throws<ArgumentException>(()=>Read(null,new JObject(),"bad"));var p=Packet();p["oversized"]=new string('x',1024*1024);Assert.Throws<ArgumentException>(()=>Read(p));
  }
  [Test] public void FixtureProducerPinsMatchActuallyExecutedServerModules() {
   Read(Packet());foreach(var pin in (JArray)Fixture()["pins"]) {
    var path=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/",(string)pin["file"]));
    using(var sha=System.Security.Cryptography.SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant(),Is.EqualTo((string)pin["sha256"]));
   }
  }
 }
}
#endif
