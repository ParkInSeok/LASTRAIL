using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class OrdinaryNodeActionsTests {
 // Synthetic action values, not evidence of a live runtime producer or combat policy.
 public static JObject Fixture(OrdinaryFiveStartInput original) => new JObject {
  ["version"]="ordinary.node-actions.v1.20261030",["stepTimeSpanTicks"]=170000,["ultimateAuto"]=true,
  ["actors"]=new JArray(original.Actors.Reverse().Select(a=>new JObject {
   ["operatorId"]=a.OperatorId,["basicNextOpportunityInTicks"]=a.Slot+1,
   ["activeCooldownRemainingTicks"]=a.Slot+11,["ultimateCharge"]=a.Slot+21 })),
  ["stats"]=new JArray(original.Actors.Select(a=>new JObject {
   ["operatorId"]=a.OperatorId,["conditionalDefenseFlat"]=a.Slot,
   ["skillMagnitude"]=a.SkillMagnitude.Snapshot() }))
 };
 static OrdinaryFiveStartInput Original()=>OrdinaryFiveStartInput.Read(OrdinaryFiveStartInputTests.Fixture(),"account","run");
 static object Read(JObject wire,OrdinaryFiveStartInput original) {
  var type=typeof(OrdinaryNodeStart).Assembly.GetType("LastRail.Run.OrdinaryNodeActions");
  Assert.NotNull(type,"Missing immutable ordinary node actions reader");
  try{return type.GetMethod("Read").Invoke(null,new object[]{wire,original});}
  catch(TargetInvocationException e){throw e.InnerException;}
 }
 static object Get(object owner,string property)=>owner.GetType().GetProperty(property).GetValue(owner);
 [Test] public void ReadsExplicitValuesByIdentityAndPreservesTicksWithoutMutability() {
  var original=Original();var wire=Fixture(original);var result=Read(wire,original);
  Assert.AreEqual(170000L,Get(result,"StepTimeSpanTicks"));Assert.AreEqual(true,Get(result,"UltimateAuto"));
  var actors=((System.Collections.IEnumerable)Get(result,"Actors")).Cast<object>().ToArray();
  for(int i=0;i<5;i++) {
   Assert.AreEqual(original.Actors[i].OperatorId,Get(actors[i],"OperatorId"));Assert.AreEqual(i,Get(actors[i],"Slot"));
   Assert.AreEqual((long)i+1,Get(actors[i],"BasicNextOpportunityInTicks"));Assert.AreEqual((long)i+11,Get(actors[i],"ActiveCooldownRemainingTicks"));
   Assert.AreEqual(i+21,Get(actors[i],"UltimateCharge"));Assert.AreEqual((long)i,Get(actors[i],"ConditionalDefenseFlat"));
  }
  wire["actors"][4]["ultimateCharge"]=999;wire["stats"][0]["skillMagnitude"]["numerator"]="999";
  Assert.AreEqual(21,Get(actors[0],"UltimateCharge"));
  var magnitude=(CombatExactValue)Get(actors[0],"SkillMagnitude");Assert.IsTrue(JToken.DeepEquals(original.Actors[0].SkillMagnitude.Snapshot(),magnitude.Snapshot()));
  var list=(System.Collections.IList)Get(result,"Actors");Assert.Throws<NotSupportedException>(()=>list[0]=actors[1]);
 }
 [TestCase("version")][TestCase("stepTimeSpanTicks")][TestCase("ultimateAuto")][TestCase("actors")][TestCase("stats")]
 public void RejectsMissingRequiredField(string field){var original=Original();var wire=Fixture(original);wire.Remove(field);Assert.Throws<ArgumentException>(()=>Read(wire,original));}
 [TestCase("actors")][TestCase("stats")]
 public void RejectsMissingDuplicateAndForeignIdentities(string field) {
  var original=Original();var wire=Fixture(original);((JArray)wire[field]).RemoveAt(0);Assert.Throws<ArgumentException>(()=>Read(wire,original));
  wire=Fixture(original);wire[field][0]["operatorId"]=wire[field][1]["operatorId"].DeepClone();Assert.Throws<ArgumentException>(()=>Read(wire,original));
  wire=Fixture(original);wire[field][0]["operatorId"]="other";Assert.Throws<ArgumentException>(()=>Read(wire,original));
 }
 [TestCase(0L)][TestCase(-10000L)][TestCase(10001L)][TestCase(9007199254750000L)]
 public void RejectsInvalidStep(long value){var original=Original();var wire=Fixture(original);wire["stepTimeSpanTicks"]=value;Assert.Throws<ArgumentException>(()=>Read(wire,original));}
 [TestCase("basicNextOpportunityInTicks",-1L)][TestCase("activeCooldownRemainingTicks",-1L)]
 [TestCase("basicNextOpportunityInTicks",9007199254740992L)][TestCase("activeCooldownRemainingTicks",9007199254740992L)]
 [TestCase("ultimateCharge",-1L)][TestCase("ultimateCharge",2147483648L)]
 public void RejectsOutOfRangeAction(string field,long value){var original=Original();var wire=Fixture(original);wire["actors"][0][field]=value;Assert.Throws<ArgumentException>(()=>Read(wire,original));}
 [Test] public void AcceptsZeroAndMaximumRepresentableActionsWithoutAssumingChargeCap() {
  var original=Original();var wire=Fixture(original);wire["ultimateAuto"]=false;
  wire["actors"][0]["basicNextOpportunityInTicks"]=0;wire["actors"][0]["activeCooldownRemainingTicks"]=9007199254740991L;wire["actors"][0]["ultimateCharge"]=int.MaxValue;
  Assert.NotNull(Read(wire,original));
 }
 [Test] public void RejectsUnknownFieldsTypesAndMalformedStats() {
  var original=Original();var wire=Fixture(original);wire["extra"]=0;Assert.Throws<ArgumentException>(()=>Read(wire,original));
  wire=Fixture(original);wire["ultimateAuto"]="true";Assert.Throws<ArgumentException>(()=>Read(wire,original));
  wire=Fixture(original);wire["actors"][0]["ultimateCharge"]=1.5;Assert.Throws<ArgumentException>(()=>Read(wire,original));
  wire=Fixture(original);wire["stats"][0]["conditionalDefenseFlat"]=-1;Assert.Throws<ArgumentException>(()=>Read(wire,original));
  wire=Fixture(original);wire["stats"][0]["skillMagnitude"]["denominator"]="0";Assert.Throws<ArgumentException>(()=>Read(wire,original));
 }
 [TestCase("basicNextOpportunityInTicks")][TestCase("activeCooldownRemainingTicks")][TestCase("ultimateCharge")]
 public void RejectsMissingActionWithoutFallback(string field) {
  var original=Original();var wire=Fixture(original);((JObject)wire["actors"][0]).Remove(field);
  Assert.Throws<ArgumentException>(()=>Read(wire,original));
 }
 [Test] public void RejectsUnknownNestedFieldsAndMismatchedVersion() {
  var original=Original();var wire=Fixture(original);wire["actors"][0]["slot"]=0;Assert.Throws<ArgumentException>(()=>Read(wire,original));
  wire=Fixture(original);wire["stats"][0]["currentHp"]=100;Assert.Throws<ArgumentException>(()=>Read(wire,original));
  wire=Fixture(original);wire["version"]="future";Assert.Throws<ArgumentException>(()=>Read(wire,original));
 }
 [Test] public void RejectsIntegerBeyondLongRangeAtBoundary() {
  var original=Original();var wire=Fixture(original);wire["stepTimeSpanTicks"]=JToken.Parse("9999999999999999999999999999999999999999");
  Assert.Throws<ArgumentException>(()=>Read(wire,original));
 }
}
