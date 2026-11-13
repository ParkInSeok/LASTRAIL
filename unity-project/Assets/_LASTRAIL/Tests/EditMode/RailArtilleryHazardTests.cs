using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class RailArtilleryHazardTests {
 // Explicit geometry fixture, not an issued product rail or measured train.
 static JObject Definition()=>JObject.Parse("{\"version\":\"rail.artillery-hazard.v1.20261023\",\"runId\":\"qa-run\",\"eventId\":\"qa-shot\",\"trainId\":\"qa-train\",\"railSegmentId\":\"qa-rail\",\"carriageIds\":[\"front\",\"rear\"],\"startMm\":10000,\"endMm\":16000,\"previewAtMs\":0,\"timing\":{\"kind\":\"SINGLE\",\"impactAtMs\":8000},\"damage\":50}");
 static JObject Frame()=>JObject.Parse("{\"runId\":\"qa-run\",\"trainId\":\"qa-train\",\"atMs\":8000,\"spans\":[{\"carriageId\":\"front\",\"railSegmentId\":\"qa-rail\",\"startMm\":14000,\"endMm\":20000},{\"carriageId\":\"rear\",\"railSegmentId\":\"qa-rail\",\"startMm\":8000,\"endMm\":14000}]}");
 [Test] public void ExactV2PreservesFractionThroughCollisionDefenseAndRestore(){
  var d=Definition();d["version"]=RailArtilleryHazard.ExactVersion;d["damage"]=new JObject{["numerator"]="253",["denominator"]="4"};
  var owner=new RailArtilleryHazard(d);var result=owner.Resolve(Frame());Assert.IsTrue(JToken.DeepEquals(d["damage"],result["rawDamageIntent"]));
  var numeric=new JObject{["version"]=CombatNumericDefenseBudget.Version,["base"]=result["rawDamageIntent"].DeepClone(),["effects"]=new JArray(),["defense"]=new JObject{["numerator"]="1000",["denominator"]="1"},["weakening"]=new JArray(),["penetration"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
  var resolved=CombatNumericDefenseBudget.Resolve(numeric);Assert.AreEqual("253",(string)resolved.Snapshot()["exact"]["numerator"]);Assert.AreEqual("8",(string)resolved.Snapshot()["exact"]["denominator"]);Assert.AreEqual(31,resolved.ToVitalsMagnitude());
  var restored=RailArtilleryHazard.Restore(owner.Snapshot(),d);Assert.IsTrue(JToken.DeepEquals(result,restored.Resolve(Frame())));result["rawDamageIntent"]["numerator"]="1";Assert.AreEqual("253",(string)restored.Resolve(Frame())["rawDamageIntent"]["numerator"]);
 }
 [Test] public void ExactMissIsCanonicalZeroAndInvalidFractionsAreRejected(){
  var d=Definition();d["version"]=RailArtilleryHazard.ExactVersion;d["damage"]=new JObject{["numerator"]="253",["denominator"]="4"};var frame=Frame();foreach(var span in (JArray)frame["spans"])span["railSegmentId"]="other";
  Assert.IsTrue(JToken.DeepEquals(new JObject{["numerator"]="0",["denominator"]="1"},new RailArtilleryHazard(d).Resolve(frame)["rawDamageIntent"]));
  foreach(var invalid in new JToken[]{new JValue(63),new JValue(63.25),new JObject{["numerator"]="506",["denominator"]="8"},new JObject{["numerator"]="0",["denominator"]="1"},new JObject{["numerator"]="1",["denominator"]="0"}}){d["damage"]=invalid;Assert.Throws<ArgumentException>(()=>new RailArtilleryHazard(d));}
 }
 [Test] public void SeveralCarriagesProduceOneIntentAndRestoreKeepsOriginalEvent(){
  var definition=Definition();var owner=new RailArtilleryHazard(definition);var frame=Frame();var result=owner.Resolve(frame);
  Assert.AreEqual(50,(long)result["rawDamageIntent"]);Assert.AreEqual(2,((JArray)result["intersectingCarriageIds"]).Count);Assert.IsNull(owner.NextImpactAtMs);
  var spans=(JArray)frame["spans"];var first=spans[0];first.Remove();spans.Add(first);
  Assert.IsTrue(JToken.DeepEquals(result,owner.Resolve(frame)));Assert.AreEqual(1,((JArray)owner.Snapshot()["receipts"]).Count);
  var restored=RailArtilleryHazard.Restore(owner.Snapshot(),definition);Assert.IsTrue(JToken.DeepEquals(result,restored.Resolve(frame)));
  frame["spans"][0]["startMm"]=9000;Assert.Throws<ArgumentException>(()=>restored.Resolve(frame));
 }
 [TestCase(10000,true)][TestCase(9999,false)] public void RearCarriageBoundaryIsInclusive(long rearEnd,bool hit){
  var owner=new RailArtilleryHazard(Definition());var frame=Frame();frame["spans"][0]["railSegmentId"]="other-rail";frame["spans"][1]["endMm"]=rearEnd;
  var result=owner.Resolve(frame);Assert.AreEqual(hit?"HIT":"MISSED",(string)result["outcome"]);Assert.AreEqual(hit?50:0,(long)result["rawDamageIntent"]);
 }
 [Test] public void MissingCarriageWrongTimeAndTamperedSaveCannotAdvanceOwner(){
  var definition=Definition();var owner=new RailArtilleryHazard(definition);var missing=Frame();((JArray)missing["spans"]).RemoveAt(1);
  Assert.Throws<ArgumentException>(()=>owner.Resolve(missing));var early=Frame();early["atMs"]=7999;Assert.Throws<ArgumentException>(()=>owner.Resolve(early));Assert.AreEqual(8000,owner.NextImpactAtMs);
  var pending=owner.Snapshot();var restored=RailArtilleryHazard.Restore(pending,definition);restored.Resolve(Frame());var saved=restored.Snapshot();saved["receipts"][0]["result"]["rawDamageIntent"]=100;
  Assert.Throws<ArgumentException>(()=>RailArtilleryHazard.Restore(saved,definition));var changed=Definition();changed["damage"]=100;
  Assert.Throws<ArgumentException>(()=>RailArtilleryHazard.Restore(pending,changed));
 }
 [Test] public void OneIntentUsesExistingShieldOwnerAndAbsorptionIsStillAHit(){
  var owner=new RailArtilleryHazard(Definition());var result=owner.Resolve(Frame());var train=new CombatVitals(1000,10000,true);
  train.Shield(new CombatShieldSource("qa-train","qa-shield",0),100,1000,0);
  var damage=train.Damage((int)result["rawDamageIntent"],160,false);
  Assert.AreEqual(50,damage.Absorbed);Assert.AreEqual(0,damage.HpLoss);Assert.AreEqual(1000,train.Hp);Assert.AreEqual("HIT",(string)result["outcome"]);
 }
 [Test] public void ScopeDuplicatesAndExternalMutationCannotChangeIssuedHazard(){
  var definition=Definition();var owner=new RailArtilleryHazard(definition);definition["startMm"]=999999;
  var duplicate=Frame();((JArray)duplicate["spans"]).Add(duplicate["spans"][0].DeepClone());Assert.Throws<ArgumentException>(()=>owner.Resolve(duplicate));
  var other=Frame();other["runId"]="another-run";Assert.Throws<ArgumentException>(()=>owner.Resolve(other));Assert.AreEqual(8000,owner.NextImpactAtMs);
  var result=owner.Resolve(Frame());result["rawDamageIntent"]=5000;Assert.AreEqual(50,(long)owner.Resolve(Frame())["rawDamageIntent"]);
  var late=Frame();late["atMs"]=8001;Assert.That(Assert.Throws<ArgumentException>(()=>owner.Resolve(late)).Message,Is.EqualTo("rail_hazard_impact_time"));
 }
}
