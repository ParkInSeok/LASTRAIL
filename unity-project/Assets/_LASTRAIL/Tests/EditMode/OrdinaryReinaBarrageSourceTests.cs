using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryReinaBarrageSourceTests {
 const string Qa="D:/Project/LASTRAIL/QA/";
 // Actual producer definitions over synthetic START; never a product admission or runtime proof.
 public static JObject NewStart()=>(JObject)JObject.Parse(File.ReadAllText(Qa+"ReinaBarrage843/wire-producer-synthetic.json"))["start"].DeepClone();
 public static JObject NewStart(JObject sameOriginal){
  var source=NewStart();
  Assert.AreEqual((string)source["runId"],(string)sameOriginal["runId"]);
  Assert.IsTrue(JToken.DeepEquals(source["ordinaryStatSource"],sameOriginal["ordinaryStatSource"]),"Synthetic source must retain the exact original stats/account/run");
  var result=(JObject)sameOriginal.DeepClone();foreach(var key in new[]{"ordinaryKitSource","ordinaryBasicSource","ordinaryRelocationSource"})result[key]=source[key].DeepClone();return result;
 }
 static OrdinaryFiveStartInput Read(JObject start)=>OrdinaryFiveStartInput.Read(start,(string)start["ordinaryStatSource"]["origin"]["accountId"],(string)start["runId"]);
 static string Hash(JToken value)=>BDirectAttackInput.Digest(value).Substring(7);
 static JToken Ability(JObject start)=>start["ordinaryKitSource"]["packet"]["operators"].Single(o=>(string)o["operatorId"]=="op_reina")["abilities"].Single(a=>(string)a["id"]=="op_reina.ultimate");
 static object Property(OrdinaryFiveStartInput source,string name){var p=typeof(OrdinaryFiveStartInput).GetProperty(name);Assert.NotNull(p,"Missing original source property: "+name);return p.GetValue(source);}
 static JObject Legacy(string version){var start=OrdinaryBasicSourceTests.Fixture();start["ordinaryBasicSource"]=JObject.Parse(File.ReadAllText(Qa+"OperatorInformation-20261019/ordinary-basic-v3-producer-fixture.json"))["source"].DeepClone();start["ordinaryRelocationSource"]=JObject.Parse(File.ReadAllText(Qa+"OperatorInformation-20261019/ordinary-relocation-"+version+"-producer-fixture.json"))["source"].DeepClone();return start;}
 static void Rehash(JObject start){
  start["ordinaryKitSource"]["kitHash"]=Hash(start["ordinaryKitSource"]["packet"]);
  var basic=start["ordinaryBasicSource"];basic["packet"]["kitHash"]=start["ordinaryKitSource"]["kitHash"].DeepClone();basic["basicHash"]=Hash(basic["packet"]);
  if(start["ordinaryRelocationSource"] is JObject source){
   var packet=source["packet"];packet["kitHash"]=start["ordinaryKitSource"]["kitHash"].DeepClone();packet["basicHash"]=basic["basicHash"].DeepClone();
   foreach(var row in packet["basicScopes"]??new JArray())row["reference"]["basicHash"]=basic["basicHash"].DeepClone();
   foreach(var row in packet["skillScopes"]??new JArray())if(row["basicReference"] is JObject reference&&((string)reference["basicHash"])!="wrong")reference["basicHash"]=basic["basicHash"].DeepClone();
   source["relocationHash"]=Hash(packet);
  }
 }
 [Test]public void ReadsNewProducerWithoutMutatingOriginalAndReturnsIsolatedDefinition(){
  var start=NewStart();var before=start.DeepClone();var source=Read(start);
  Assert.AreEqual("combat.ordinary-five-kit.v2.20261102",Property(source,"KitSourceVersion"));
  Assert.AreEqual("combat.ordinary-relocation.v7.20261102",source.Relocation.SourceVersion);
  var definition=(JObject)Property(source,"ReinaBarrage");Assert.IsTrue(JToken.DeepEquals(Ability(start),definition));
  definition["primaryDamageBps"]=1;Assert.AreEqual(25000,(int)((JObject)Property(source,"ReinaBarrage"))["primaryDamageBps"]);
  Assert.IsTrue(JToken.DeepEquals(start,before));
 }
 [Test]public void NewSourceWithoutRelocationPreservesAbsenceForExecutionGate(){var start=NewStart();start.Remove("ordinaryRelocationSource");var input=Read(start);Assert.IsNull(input.Relocation);Assert.NotNull(Property(input,"ReinaBarrage"));}
 [Test]public void NewSourceCanBeReusedOnlyWithSameStaticOriginal(){var f=FirstPlayableScalingTests.Fixture(staticSource:true);Assert.NotNull(Read(NewStart(f.Start)));var other=(JObject)f.Start.DeepClone();other["runId"]="other";Assert.Throws<AssertionException>(()=>NewStart(other));}
 [TestCase("v1")][TestCase("v2")][TestCase("v3")][TestCase("v4")][TestCase("v5")][TestCase("v6")]
 public void LegacyDefinitionsRemainReadableWithoutNewBarrage(string version){var start=Legacy(version);var source=Read(start);Assert.AreEqual("combat.ordinary-five-kit.v1.20261023",Property(source,"KitSourceVersion"));Assert.IsNull(Property(source,"ReinaBarrage"));Assert.AreEqual("DIRECT_DAMAGE",(string)source.Actors.Single(a=>a.OperatorId=="op_reina").Ability("op_reina.ultimate")["effect"]);}
 [TestCase("damage")][TestCase("offset")][TestCase("definition")][TestCase("version")][TestCase("scope")][TestCase("nested")]
 public void RehashedNewDefinitionChangesReject(string fault){
  var start=NewStart();var ability=Ability(start);var packet=start["ordinaryRelocationSource"]["packet"];
  if(fault=="damage")ability["primaryDamageBps"]=25001;
  if(fault=="offset")ability["impactOffsetsMs"][0]=1399;
  if(fault=="definition")ability["definitionId"]="changed";
  if(fault=="version")start["ordinaryKitSource"]["packet"]["version"]="combat.ordinary-five-kit.v1.20261023";
  if(fault=="scope")packet["skillScopes"].Single(s=>(string)s["abilityId"]=="op_reina.ultimate")["barrage"]["consumption"]="ACCEPTANCE";
  if(fault=="nested")packet["skillScopes"].Single(s=>(string)s["abilityId"]=="op_mira.ultimate")["basicReference"]["basicHash"]="wrong";
  Rehash(start);Assert.Throws<ArgumentException>(()=>Read(start));
 }
 [TestCase("v1")][TestCase("v2")][TestCase("v3")][TestCase("v4")][TestCase("v5")][TestCase("v6")]
 public void NewKitCannotRehashAnOldRelocationIntoCompatibility(string version){
  var start=Legacy(version);var packet=start["ordinaryKitSource"]["packet"];
  var replacement=Ability(NewStart()).DeepClone();Ability(start).Replace(replacement);packet["version"]="combat.ordinary-five-kit.v2.20261102";
  packet["requiredEffectKinds"]=new JArray(packet["operators"].SelectMany(o=>o["abilities"]).Select(a=>(string)a["effect"]).Distinct().OrderBy(x=>x,StringComparer.Ordinal));
  Rehash(start);if(version=="v1"){((JObject)start["ordinaryRelocationSource"]["packet"]).Remove("kitHash");start["ordinaryRelocationSource"]["relocationHash"]=Hash(start["ordinaryRelocationSource"]["packet"]);}
  Assert.Throws<ArgumentException>(()=>Read(start));
 }
 [Test]public void OldKitCannotRehashNewRelocationIntoCompatibility(){
  var start=NewStart();Ability(start).Replace(Ability(Legacy("v6")).DeepClone());var packet=start["ordinaryKitSource"]["packet"];packet["version"]="combat.ordinary-five-kit.v1.20261023";
  packet["requiredEffectKinds"]=new JArray(packet["operators"].SelectMany(o=>o["abilities"]).Select(a=>(string)a["effect"]).Distinct().OrderBy(x=>x,StringComparer.Ordinal));Rehash(start);Assert.Throws<ArgumentException>(()=>Read(start));
 }
}
