#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode {
 public class BMeasuredAcquisitionLocalSourceTests {
  const string Root="D:/Project/LASTRAIL/";
  static string BytesHash(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
  static void Pin(JObject value){value.Remove("packetHash");value["packetHash"]=BDirectAttackInput.Digest(value).Substring(7);}
  // Editor authoring only. Runtime/Player never references this test assembly.
  public static JObject Envelope(){
   var boarding=BBoardingEventsTests.Source(0);var binding=BRouteNativeTests.Binding(boarding);var combat=BNativeLifestealTests.Route(boarding.OriginalSource);
   var noncombat=(LastRail.Run.BRouteBinding)typeof(BNoncombatAcquisitionTests).GetMethod("Augment",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{combat});
   var original=(LastRail.Run.CombatO22ApplicationOriginal)typeof(BNoncombatAcquisitionTests).GetMethod("Original",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{combat});
   var next=combat.Capture();next["mapRevision"]=noncombat.MapRevision+1;Pin(next);
   var profile=(JObject)JObject.Parse(File.ReadAllText(Root+"QA/CommanderS1-20260922/c3-owned-profile-observed-sql-test-20261001.json"))["profile"];
   profile["accountId"]=boarding.OriginalSource.Capture()["origin"]["accountId"].DeepClone();
   if(BDirectAttackInput.Digest(profile)!=(string)binding.Composition.Attack.Capture()["ownedProfileHash"])throw new InvalidOperationException("Exact native fixture profile differs");
   var confirmed=JObject.Parse(original.SelectionJson);var initial=(JObject)confirmed.DeepClone();
   initial["state"]="SELECTING";initial["grantRevision"]=(long)confirmed["grantRevision"]-1;initial["selectedCandidateId"]=null;
   var rail=global::CombatRailJourneyTests.Config();rail["offers"]=new JArray();rail["throttleMaximumBps"]=13000;rail["throttleCruiseBps"]=10000;
   string buffs=File.ReadAllText(Root+"QA/CombatBMigration-20261002/BuffConsumer/original-definitions.json");
   string geometryRoot=Root+"unity-project/Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoringV01/";
   string definition=File.ReadAllText(geometryRoot+"definition.json"),measurements=File.ReadAllText(geometryRoot+"measurements.json");
   return new JObject{
    ["version"]="unity.b-measured-acquisition-local-source.v1.20261020",
    ["classification"]="EXPLICIT_LOCAL_TEST_NOT_ADMITTED_RUN",["fixtureIssuer"]=true,["independentArrivalVerified"]=false,["environment"]="TEST",
    ["boardingSource"]=boarding.Capture(),["boardingTrust"]=BBoardingEventsTests.Trust(0),
    ["attack"]=binding.Composition.Attack.Capture(),["ownedProfile"]=profile,
    ["definitionIntake"]=JObject.Parse(File.ReadAllText(Root+"QA/CommanderS1-20260922/c3-definition-intake-20261001.json")),
    ["buffDefinitionsJson"]=buffs,["buffDefinitionsSha256"]=BytesHash(buffs),
    ["composition"]=binding.Composition.Capture(),["loop"]=binding.Capture(),["nativeBindingHash"]=binding.OriginalHash,
    ["noncombatRoute"]=noncombat.Capture(),["combatRoute"]=next,["initialSelection"]=initial,
    ["selectResponse"]=new JObject{["version"]="combat.augment-select.v1.20261019",["committed"]=true,
     ["result"]=new JObject{["outcome"]="SELECTED",["snapshot"]=confirmed,["approval"]=null,["applicationOriginal"]=JObject.Parse(original.Capture())}},
    ["railConfiguration"]=rail,["railHash"]=BDirectAttackInput.Digest(rail),["railSource"]="EXISTING_EXPLICIT_TEST_CONFIGURATION_NOT_OPERATING_RAIL",
    ["geometryDefinitionJson"]=definition,["geometryDefinitionSha256"]=BytesHash(definition),
    ["geometryMeasurementsJson"]=measurements,["geometryMeasurementsSha256"]=BytesHash(measurements)
   };
  }
  static Type Reader=>typeof(BDirectAttackInput).Assembly.GetType("LastRail.Presentation.AugmentSelection.BMeasuredAcquisitionLocalSource");
  static object Read(JObject p,string hash=null){Assert.NotNull(Reader,"Actual LOCAL Player source reader missing");return Reader.GetMethod("Read").Invoke(null,new object[]{p,hash??BDirectAttackInput.Digest(p)});}
  [Test] public void SerializedEnvelopeReconstructsExactUnchangedNativeBindingAndMeasuredDraft(){
   var p=Envelope();var source=Read(JObject.Parse(p.ToString()));
   var binding=(BDirectAttackBasicLoopBinding)Reader.GetProperty("Binding").GetValue(source);
   Assert.AreEqual(BRouteNativeTests.Binding(BBoardingEventsTests.Source(0)).OriginalHash,binding.OriginalHash);
   Assert.IsFalse(binding.Composition.Attack.OperationallyExecutable);
   var geometry=(BMeasuredPhysicalGeometry)Reader.GetProperty("Geometry").GetValue(source);
   Assert.AreEqual(new JArray(0,3200,0).ToString(),geometry.WorldAnchor(0,"Foot").ToString());
   Assert.NotNull(Reader.GetProperty("Original").GetValue(source));Assert.NotNull(Reader.GetProperty("Motion").GetValue(source));
   p["ownedProfile"]["accountId"]="mutated";Assert.AreEqual(binding.OriginalHash,((BDirectAttackBasicLoopBinding)Reader.GetProperty("Binding").GetValue(source)).OriginalHash);
  }
  [Test] public void SourcePinAndGeometryFileByteMismatchCannotReachNativeBinding(){
   var p=Envelope();string hash=BDirectAttackInput.Digest(p);p["environment"]="PROD";
   Assert.Throws<TargetInvocationException>(()=>Read(p,hash));
   p=Envelope();p["geometryDefinitionJson"]=(string)p["geometryDefinitionJson"]+" ";
   Assert.Throws<TargetInvocationException>(()=>Read(p));
  }
  [Test] public void RelabelledOperatingSourceAndForeignSelectionAreRejected(){
   var p=Envelope();p["classification"]="OPERATING";
   Assert.Throws<TargetInvocationException>(()=>Read(p));
   p=Envelope();p["initialSelection"]["stageId"]="foreign-node";
   Assert.Throws<TargetInvocationException>(()=>Read(p));
  }
 }
}
#endif
