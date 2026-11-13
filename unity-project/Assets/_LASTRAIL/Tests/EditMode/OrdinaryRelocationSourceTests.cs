using System;
using System.IO;
using System.Reflection;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryRelocationSourceTests {
 static JObject Fixture(string version){var x=OrdinaryBasicSourceTests.Fixture();x["ordinaryBasicSource"]=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/ordinary-basic-v3-producer-fixture.json"))["source"].DeepClone();x["ordinaryRelocationSource"]=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/ordinary-relocation-"+version+"-producer-fixture.json"))["source"].DeepClone();return x;}
 static OrdinaryFiveStartInput Read(JObject x)=>OrdinaryFiveStartInput.Read(x,"a","future-run");
 static object Relocation(OrdinaryFiveStartInput input){var p=typeof(OrdinaryFiveStartInput).GetProperty("Relocation");Assert.NotNull(p);return p.GetValue(input);}
 static string Hash(JToken x){var canonical=(string)typeof(BasicCombatSimulation).GetMethod("StableJson",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{x});using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(canonical))).Replace("-","").ToLowerInvariant();}
 [TestCase("v1")][TestCase("v2")]public void ReadsProducerAndDoesNotBackfillHistory(string version){var x=Fixture(version);Assert.NotNull(Relocation(Read(x)));x.Remove("ordinaryRelocationSource");Assert.IsNull(Relocation(Read(x)));}
 [TestCase("null")][TestCase("basic")][TestCase("nested")][TestCase("scope")][TestCase("version")]
 public void RehashedChangesReject(string change){var x=Fixture("v2");var p=(JObject)x["ordinaryRelocationSource"]["packet"];if(change=="null")x["ordinaryRelocationSource"]=JValue.CreateNull();else{if(change=="basic")p["basicHash"]="wrong";if(change=="nested")p["basicScopes"][0]["reference"]["basicHash"]="wrong";if(change=="scope")p["skillScopes"][0]["basis"]="CURRENT_CARRIAGE";if(change=="version")p["version"]="future";x["ordinaryRelocationSource"]["relocationHash"]=Hash(p);}Assert.Throws<ArgumentException>(()=>Read(x));}
 [Test]public void RelocationPinChangesFactoryAndRejectsCrossRestore(){var x=Fixture("v2");var stats=OrdinaryFiveSeraFactoryTests.Sources().Stats;var with=new OrdinaryFiveSeraFactory(Read(x),stats,500000);x.Remove("ordinaryRelocationSource");var without=new OrdinaryFiveSeraFactory(Read(x),stats,500000);Assert.AreNotEqual(with.ConfigurationVersion,without.ConfigurationVersion);Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(with.Create().CaptureLocalState(),without.Create));}
 [Test]public void V2PreservesUnclassifiedAbilitiesAndFormationHealing(){var input=Read(Fixture("v2"));Assert.AreEqual(4,input.Relocation.UnclassifiedAbilityIds.Count);Assert.AreEqual("FORMATION_ROSTER_ORIGINAL_SLOT_TIEBREAK",input.Relocation.SkillScopes[6].Basis);Assert.IsFalse(Read(Fixture("v1")).Relocation.HasScopeMapping);}
 [Test]public void V1DoesNotAcceptAnUnboundV2KitField(){var x=Fixture("v1");var p=(JObject)x["ordinaryRelocationSource"]["packet"];p["kitHash"]="unbound";x["ordinaryRelocationSource"]["relocationHash"]=Hash(p);Assert.Throws<ArgumentException>(()=>Read(x));}
 [Test]public void V2PassiveOrderMustMatchOriginalFormation(){var x=Fixture("v2");var p=(JObject)x["ordinaryRelocationSource"]["packet"];p["passiveScopes"]=new JArray(((JArray)p["passiveScopes"]).Reverse());x["ordinaryRelocationSource"]["relocationHash"]=Hash(p);Assert.Throws<ArgumentException>(()=>Read(x));}
 [Test]public void V3ClassifiesFourScopesWithoutClaimingReinaDamageRecipients(){
  var input=Read(Fixture("v3"));Assert.AreEqual(0,input.Relocation.UnclassifiedAbilityIds.Count);
  Assert.AreEqual("COMPOSITE_SELF_SHIELD_AND_CURRENT_CARRIAGE_FRONT_THREATS",input.Relocation.SkillScopes[0].Basis);
  Assert.AreEqual("CURRENT_CARRIAGE_ORIGINAL_BASIC_RANGE",input.Relocation.SkillScopes[9].Basis);
  var scope=input.Relocation.SkillScopes[2];var method=scope.GetType().GetMethod("CaptureDefinition");Assert.NotNull(method);
  var definition=(JObject)method.Invoke(scope,null);Assert.AreEqual("CURRENT_ORDINARY_DIRECT_DAMAGE_CONTRACT_UNSUPPLIED",(string)definition["damageRecipients"]);
  definition["primaryTarget"]["maximumDistanceMm"]=1;Assert.AreEqual(24000,(int)((JObject)method.Invoke(scope,null))["primaryTarget"]["maximumDistanceMm"]);
  Assert.AreEqual(4,Read(Fixture("v2")).Relocation.UnclassifiedAbilityIds.Count);
 }
 [Test]public void V3MiraBasicReferenceMustMatchOriginal(){var x=Fixture("v3");var p=(JObject)x["ordinaryRelocationSource"]["packet"];p["skillScopes"][9]["basicReference"]["basicHash"]="wrong";x["ordinaryRelocationSource"]["relocationHash"]=Hash(p);Assert.Throws<ArgumentException>(()=>Read(x));}
 [Test]public void V4ReadsReinaRecipientsAndKeepsV3Unsupplied(){
  var input=Read(Fixture("v4"));var active=input.Relocation.SkillScopes[2].CaptureDefinition()["damageRecipients"];var ultimate=input.Relocation.SkillScopes[3].CaptureDefinition()["damageRecipients"];
  Assert.AreEqual(40000,(int)active["primaryDamageBps"]);Assert.AreEqual(JTokenType.Null,active["additional"].Type);
  Assert.AreEqual(100000,(int)ultimate["primaryDamageBps"]);Assert.AreEqual(4,(int)ultimate["maximumTargets"]);
  Assert.AreEqual(30000,(int)ultimate["additional"]["damageBps"]);Assert.AreEqual(3000,(int)ultimate["additional"]["radiusMm"]);Assert.IsFalse((bool)ultimate["redistributeUnusedDamage"]);
  Assert.AreEqual(JTokenType.String,Read(Fixture("v3")).Relocation.SkillScopes[3].CaptureDefinition()["damageRecipients"].Type);
 }
 [Test]public void V4RejectsRehashedRecipientAndNestedReferenceChanges(){foreach(var field in new[]{"damage","reference"}){var x=Fixture("v4");var p=(JObject)x["ordinaryRelocationSource"]["packet"];if(field=="damage")p["skillScopes"][3]["damageRecipients"]["additional"]["damageBps"]=100000;else p["skillScopes"][9]["basicReference"]["basicHash"]="wrong";x["ordinaryRelocationSource"]["relocationHash"]=Hash(p);Assert.Throws<ArgumentException>(()=>Read(x));}}
 [TestCase("v5")][TestCase("v6")]public void ReadsMiraArrivalWithoutBackfillingEarlierVersions(string version){
  var source=Read(Fixture(version)).Relocation;var flight=source.SkillScopes.Single(s=>s.AbilityId=="op_mira.ultimate").CaptureDefinition()["projectile"];
  Assert.AreEqual("FIXED_TARGET_DELAYED_ARRIVAL",(string)flight["mode"]);Assert.AreEqual(120,(int)flight["flightMs"]);
  Assert.AreEqual(16,(int)flight["parameters"]["shots"]);Assert.IsFalse((bool)flight["physicalCollisionAfterLaunch"]);
  Assert.AreEqual("MISS_NO_RETARGET",(string)flight["arrival"]["lostTarget"]);
  Assert.IsNull(Read(Fixture("v4")).Relocation.SkillScopes.Single(s=>s.AbilityId=="op_mira.ultimate").CaptureDefinition()["projectile"]);
 }
 [Test]public void V6ReadsIndependentDeviceFlightAndPreservesV5(){
  var source=Read(Fixture("v6")).Relocation;var scope=source.SkillScopes.Single(s=>s.AbilityId=="op_eve.active");var flight=scope.CaptureDefinition()["projectile"];
  Assert.AreEqual("SINGLE_UNGUIDED_PHYSICAL_PROJECTILE",(string)flight["mode"]);Assert.AreEqual(48000,(int)flight["speedMmPerSecond"]);
  Assert.AreEqual(50,(int)flight["radiusMm"]);Assert.AreEqual(18000,(int)flight["maximumFlightDistanceMm"]);Assert.AreEqual(375,(int)flight["maximumFlightMs"]);
  Assert.IsTrue((bool)flight["schedule"]["preserveNextShotOnEnhance"]);Assert.IsFalse((bool)flight["schedule"]["basicCharge"]);
  Assert.IsFalse((bool)flight["collision"]["friendlyBlocking"]);Assert.AreEqual("ONE_DAMAGE_THEN_TERMINATE",(string)flight["collision"]["firstEligibleEnemy"]);
  Assert.AreEqual("op_eve.active.projectile",(string)source.SkillScopes.Single(s=>s.AbilityId=="op_eve.ultimate").CaptureDefinition()["projectileReference"]);
  flight["radiusMm"]=999;Assert.AreEqual(50,(int)scope.CaptureDefinition()["projectile"]["radiusMm"]);
  Assert.IsNull(Read(Fixture("v5")).Relocation.SkillScopes.Single(s=>s.AbilityId=="op_eve.active").CaptureDefinition()["projectile"]);
 }
 [TestCase("v5","flight")][TestCase("v6","flight")][TestCase("v5","reference")][TestCase("v6","reference")]
 public void FlightVersionsRejectRehashedPolicyAndNestedPins(string version,string change){
  var x=Fixture(version);var p=(JObject)x["ordinaryRelocationSource"]["packet"];
  if(change=="reference")p["skillScopes"][9]["basicReference"]["basicHash"]="wrong";
  else if(version=="v5")p["skillScopes"][9]["projectile"]["flightMs"]=1;
  else p["skillScopes"][4]["projectile"]["schedule"]["basicCharge"]=true;
  x["ordinaryRelocationSource"]["relocationHash"]=Hash(p);Assert.Throws<ArgumentException>(()=>Read(x));
 }
 [Test]public void FlightVersionsCannotRestoreEachOthersCheckpoint(){
  var stats=OrdinaryFiveSeraFactoryTests.Sources().Stats;var older=new OrdinaryFiveSeraFactory(Read(Fixture("v5")),stats,500000);var newer=new OrdinaryFiveSeraFactory(Read(Fixture("v6")),stats,500000);
  Assert.AreNotEqual(older.ConfigurationVersion,newer.ConfigurationVersion);
  Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(older.Create().CaptureLocalState(),newer.Create));
 }
}
