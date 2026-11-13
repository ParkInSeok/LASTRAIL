using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class FirstPlayableScalingTests {
 [TestCase("missing")][TestCase("version")][TestCase("hash")][TestCase("inline")][TestCase("account")][TestCase("risk")]
 public void V8RejectsUnresolvedOrChangedReferenceEvenWithRehashedPacket(string fault){
  var e=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json"));
  var start=(JObject)e["originalStart"];var source=(JObject)start["firstBattleEncounterSource"];var packet=(JObject)source["packet"];
  if(fault=="missing")packet.Remove("definitionRef");
  if(fault=="version")packet["definitionRef"]["version"]="unknown";
  if(fault=="hash")packet["definitionRef"]["definitionHash"]=new string('0',64);
  if(fault=="inline")packet["definition"]=new JObject{["runtimeSupported"]=true};
  if(fault=="account")packet["accountId"]="different";
  if(fault=="risk")packet["selectedRisk"]="different";
  source["sourceHash"]=Hash(packet);
  Assert.Throws<ArgumentException>(()=>OrdinaryFiveStartInput.Read(start,(string)e["accountId"],(string)e["runId"]));
 }
 [TestCase("I",4,"240","50","1")][TestCase("V",5,"552","385","4")][TestCase("III",9,"372","78","1")]
 public void ReferencedVariantsRespectOriginalRiskAndProgress(string risk,int depth,string hp,string damage,string denominator){
  var f=Fixture(depth:depth,referenced:true,risk:risk);var scale=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).Scaling;
  scale.RequireOriginal(OrdinaryFiveStartInput.Read(f.Start,"a",(string)f.Start["runId"]),f.Init.ToString());
  Assert.AreEqual(hp,(string)scale.GunnerHp.Snapshot()["numerator"]);
  Assert.AreEqual(damage,(string)scale.ArtilleryRawDamage.Snapshot()["numerator"]);Assert.AreEqual(denominator,(string)scale.ArtilleryRawDamage.Snapshot()["denominator"]);
 }
 [TestCase(false)][TestCase(true)] public async Task ReferencedDefinitionResolvesAndControllerRestoresExactArrival(bool staticSource){
  var f=Fixture(referenced:true,staticSource:staticSource);var input=OrdinaryFiveStartInput.Read(f.Start,"a",(string)f.Start["runId"]);
  Assert.IsNull(input.FirstBattle.Definition["scaling"]["variants"]);
  using(var c=new BRouteSessionController("a",input.RunId)){
   await c.RefreshAsync(()=>Task.FromResult(f.Command.Binding));await c.SendAsync(f.Command,()=>Task.FromResult(BRouteCommandResult.Read(200,f.Response.ToString(),f.Command)));
   var scaling=c.ReadFirstPlayableScaling(input,f.Init.ToString());Assert.AreEqual("300",(string)scaling.GunnerHp.Snapshot()["numerator"]);Assert.AreEqual(2,scaling.IssuedGunnerIds.Count);
   Assert.AreEqual((string)f.Response["receipt"]["firstPlayableScaling"]["enemySpatial"]["physicalInstanceId"],scaling.IssuedEnemyTrainId);
   Assert.IsFalse(input.Rail.PhysicalInstanceIds.Contains(scaling.IssuedEnemyTrainId));
   var state=c.Capture();using(var restored=BRouteSessionController.Restore(state,BDirectAttackInput.Digest(state),null,"a",input.RunId))
    Assert.IsTrue(JToken.DeepEquals(scaling.Capture(),restored.ReadFirstPlayableScaling(input,f.Init.ToString()).Capture()));
  }
 }
 [TestCase("missing")][TestCase("identity")][TestCase("geometry")][TestCase("definition")][TestCase("risk")][TestCase("applied")][TestCase("oldVersion")]
 public void ReferencedScalingRejectsChangedOriginalBindings(string field){
  var f=Fixture(referenced:true);var p=(JObject)f.Response["receipt"]["firstPlayableScaling"];
  if(field=="missing")p.Remove("enemySpatial");
  if(field=="identity")p["enemySpatial"]["physicalInstanceId"]=new string('0',32);
  if(field=="geometry")p["enemySpatial"]["geometryDefinitionHash"]=new string('0',64);
  if(field=="definition")p["enemySpatial"]["definitionHash"]=new string('0',64);
  if(field=="risk")p["riskDefinitionHash"]="sha256:"+new string('0',64);
  if(field=="applied")p["applied"]["gunnerHp"]["numerator"]="301";
  if(field=="oldVersion")p["version"]="combat.first-playable-scaling.v1.20261027";
  Pin(p,"bindingHash");Assert.Throws<ArgumentException>(()=>BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));
 }
 static string Hash(JToken p)=>BDirectAttackInput.Digest(p).Substring(7);
 static void Pin(JObject p,string key){p.Remove(key);p[key]=Hash(p);}
 // Synthetic route/arrival proof over the issued profile; this is not a product admission test.
 internal static (JObject Start,JObject Init,BRouteCommand Command,JObject Response) Fixture(int depth=5,bool includeTrain=true,bool latest=false,bool referenced=false,string risk="II",bool staticSource=false,JObject geometryReference=null){
  if(staticSource)referenced=true;
  var export=JObject.Parse(File.ReadAllText(staticSource?"D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json":referenced?"D:/Project/LASTRAIL/QA/CommanderS1-20260922/ReferenceDefinitions809/source.json":"D:/Project/LASTRAIL/QA/OperatorInformation-20261019/"+(latest?"TransitionCaller802/source.json":"TrainInitial777/consumed-export.json")));
  var start=(JObject)export["originalStart"];var source=(JObject)start["firstBattleEncounterSource"];start["risk"]=risk;source["packet"]["selectedRisk"]=risk;source["sourceHash"]=Hash(source["packet"]);
  if(geometryReference!=null)start["ordinaryGeometryReference"]=geometryReference.DeepClone();
  var init=(JObject)export["initializationResponse"];init["initializationReceipt"]["firstBattleEncounterBinding"]["sourceHash"]=source["sourceHash"].DeepClone();
  if(!includeTrain){var initialReceipt=(JObject)init["initializationReceipt"];var id=(string)initialReceipt["trainInitialState"]?["trainId"];start.Remove("ordinaryTrainSource");initialReceipt.Remove("ordinaryTrainBinding");initialReceipt.Remove("trainInitialState");foreach(var key in new[]{"targets","initialTargets"}){var rows=initialReceipt["health"]["state"][key] as JArray;foreach(var row in rows.Where(r=>(string)r["id"]==id).ToArray())row.Remove();}}
  var sample=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-route-sample-20261005.json"));var route=(JObject)sample["pendingBinding"];
  route["runId"]=start["runId"].DeepClone();route["selectedRisk"]=risk;route["startSnapshotHash"]=Hash(start);
  foreach(var key in new[]{"nativeContextId","departurePinHash","originalNativeOrigin","nativeNodeSource"})route[key]=JValue.CreateNull();route["nodeSourceStatus"]="ORIGINAL_NODE_REFS_ONLY";
  var node=(JObject)route["pendingNode"];var definition=staticSource?OrdinaryFiveStartInput.Read(start,"a",(string)start["runId"]).FirstBattle.Definition:(JObject)source["packet"]["definition"];node["visitDepth"]=depth;node["eventKind"]="COMBAT";node["encounterProfileRef"]=definition["profileId"].DeepClone();node["encounterVersion"]=definition["version"].DeepClone();node["encounterDefinition"]=definition.DeepClone();node["encounterDefinitionHash"]=Hash(definition);Pin(route,"packetHash");
  var binding=BRouteBinding.Read(route.ToString(),(string)route["packetHash"],"a",(string)start["runId"]);
  var command=BRouteCommand.FromOriginal(binding,new JObject{["kind"]="ARRIVE",["eventId"]="arrival",["expectedRevision"]=binding.MapRevision,["edgeId"]=route["pendingEdgeId"].DeepClone(),["evidenceRef"]="SYNTHETIC_PROOF"});
  var receipt=(JObject)sample["localAccepted"]["arrival"];string band=depth<=4?"EARLY":depth<=8?"MIDDLE":"LATE";
  var variants=referenced?JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/ReferenceDefinitions809/definitions.json"))["definitions"]["historicalProfileV3"]["scaling"]["variants"]:definition["scaling"]["variants"];
  var scale=new JObject{["version"]="combat.first-playable-scaling.v1.20261027",["accountId"]="a",["runId"]=start["runId"].DeepClone(),["encounterId"]="arrival",["nodeId"]=node["nodeId"].DeepClone(),["visitDepth"]=depth,["progressRevision"]=receipt["revision"].DeepClone(),["progressStateHash"]=Hash(receipt["state"]),["startSnapshotHash"]=Hash(start),["initializationResponseHash"]=Hash(init["initializationReceipt"]),["sourceHash"]=source["sourceHash"].DeepClone(),["profileId"]=definition["profileId"].DeepClone(),["profileVersion"]=definition["version"].DeepClone(),["profileHash"]=definition["definitionHash"].DeepClone(),["riskDefinitionVersion"]=definition["scaling"]["definitionVersion"].DeepClone(),["riskDefinitionHash"]=definition["scaling"]["definitionHash"].DeepClone(),["applied"]=variants.Single(v=>(string)v["risk"]==risk&&(string)v["progress"]==band).DeepClone(),["hpRiskApplications"]=1,["damageRiskApplications"]=1,["damageProgressApplications"]=1};if(referenced){scale["version"]="combat.first-playable-scaling.v2.20261028";var spatial=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/ReferenceDefinitions809/definitions.json"))["definitions"]["enemySpatial"];var id=Hash(new JObject{["version"]="combat.first-enemy-instance.v1.20261028",["accountId"]="a",["runId"]=start["runId"].DeepClone(),["encounterId"]="arrival",["sourceHash"]=source["sourceHash"].DeepClone(),["role"]="enemy-train",["definitionHash"]=spatial["definitionHash"].DeepClone()}).Substring(0,32);scale["enemySpatial"]=new JObject{["version"]=spatial["version"].DeepClone(),["definitionHash"]=spatial["definitionHash"].DeepClone(),["geometryDefinitionHash"]=spatial["geometry"]["definitionHash"].DeepClone(),["physicalInstanceId"]=id};}Pin(scale,"bindingHash");receipt["firstPlayableScaling"]=scale;
  return(start,init,command,new JObject{["version"]=BRouteBinding.Version,["committed"]=true,["receipt"]=receipt});
 }
 [TestCase(4,"EARLY")][TestCase(5,"MIDDLE")][TestCase(8,"MIDDLE")][TestCase(9,"LATE")]
 public void ArrivalConsumesExactVariantAndPinsBothOriginalReceipts(int depth,string band){var f=Fixture(depth);var r=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);var s=r.Scaling;Assert.NotNull(s);s.RequireOriginal(OrdinaryFiveStartInput.Read(f.Start,"a",(string)f.Start["runId"]),f.Init.ToString());Assert.AreEqual(band,(string)s.Capture()["applied"]["progress"]);Assert.AreEqual("300",(string)s.GunnerHp.Snapshot()["numerator"]);if(band=="MIDDLE"){Assert.AreEqual("253",(string)s.ArtilleryRawDamage.Snapshot()["numerator"]);Assert.AreEqual("4",(string)s.ArtilleryRawDamage.Snapshot()["denominator"]);}Assert.IsTrue(JToken.DeepEquals(f.Response,r.Capture()));}
 [TestCase("progressRevision")][TestCase("progressStateHash")][TestCase("encounterId")][TestCase("sourceHash")][TestCase("initializationResponseHash")][TestCase("applied")][TestCase("damageRiskApplications")]
 public void RehashedArrivalConflictsCannotReachExecution(string key){var f=Fixture();var p=(JObject)f.Response["receipt"]["firstPlayableScaling"];if(key=="applied")p[key]["artilleryRawDamage"]["numerator"]="252";else if(key=="progressRevision"||key=="damageRiskApplications")p[key]=99;else p[key]=new string('0',64);Pin(p,"bindingHash");Assert.Throws<ArgumentException>(()=>{var result=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);result.Scaling.RequireOriginal(OrdinaryFiveStartInput.Read(f.Start,"a",(string)f.Start["runId"]),f.Init.ToString());});}
 [Test] public void MissingScalingDoesNotSelectDefaultAndExplicitNullFails(){var f=Fixture();var r=(JObject)f.Response["receipt"];r.Remove("firstPlayableScaling");Assert.IsNull(BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).Scaling);r["firstPlayableScaling"]=JValue.CreateNull();Assert.Throws<ArgumentException>(()=>BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));}
 [Test] public void BossDepthCannotBorrowEarlyBand(){var f=Fixture(12);Assert.Throws<ArgumentException>(()=>BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));}
 [TestCase(false)][TestCase(true)] public async Task ControllerConsumesAndRestoresArrivalWithoutAnotherRead(bool latest){
  var f=Fixture(latest:latest);var input=OrdinaryFiveStartInput.Read(f.Start,"a",(string)f.Start["runId"]);var response=f.Init.ToString();
  using(var controller=new BRouteSessionController("a",input.RunId)){
   await controller.RefreshAsync(()=>Task.FromResult(f.Command.Binding));
   await controller.SendAsync(f.Command,()=>Task.FromResult(BRouteCommandResult.Read(200,f.Response.ToString(),f.Command)));
   var selected=controller.ReadFirstPlayableScaling(input,response);var saved=controller.Capture();
   if(latest){CollectionAssert.AreEqual(new[]{"c8f052c2b5a62d5d9c6524432bc1b0f4","0c74ed8ed7e3d1e56dd364975c1065b9"},selected.IssuedGunnerIds);Assert.AreEqual("c85ee3455e8a9860bbbb48f7379c9cfe",selected.IssuedHazardId);}else{Assert.IsNull(selected.IssuedGunnerIds);Assert.IsNull(selected.IssuedHazardId);}
   using(var restored=BRouteSessionController.Restore(saved,BDirectAttackInput.Digest(saved),null,"a",input.RunId)){
    var reopened=restored.ReadFirstPlayableScaling(input,response);Assert.AreEqual(selected.BindingHash,reopened.BindingHash);CollectionAssert.AreEqual(selected.IssuedGunnerIds,reopened.IssuedGunnerIds);Assert.AreEqual(selected.IssuedHazardId,reopened.IssuedHazardId);
    var changed=(JObject)f.Init.DeepClone();changed["initializationReceipt"]["firstBattleEncounterBinding"]["sourceHash"]=new string('0',64);
    Assert.Throws<FormatException>(()=>restored.ReadFirstPlayableScaling(input,changed.ToString()));
   }
  }
 }
}
