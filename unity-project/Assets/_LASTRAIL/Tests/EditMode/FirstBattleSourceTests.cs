using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class FirstBattleSourceTests {
 static JObject Export()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/ordinary-initialization-consumer-source-20261026.json"));
 static OrdinaryFiveStartInput Read(JObject start)=>OrdinaryFiveStartInput.Read(start,"a",(string)start["runId"]);
 static void Rehash(JObject start){var source=start["firstBattleEncounterSource"];source["sourceHash"]=BDirectAttackInput.Digest(source["packet"]).Substring(7);}
 [Test] public void StartConsumesIssuedProfileWithoutSelectingAnUnboundScale(){
  var export=Export();var input=Read((JObject)export["originalStart"]);
  Assert.NotNull(input.FirstBattle);Assert.IsFalse(input.FirstBattle.HasAppliedScaling);
  Assert.AreEqual("d8db615dfb8beef8aaca82a76544d3e2427a52970da15df9166f74af784ea4f3",input.FirstBattle.ProfileHash);
  var definition=input.FirstBattle.Definition;Assert.AreEqual(240,(int)definition["enemies"]["baseHpEach"]);
  var middle=definition["scaling"]["variants"].Single(v=>(string)v["risk"]=="II"&&(string)v["progress"]=="MIDDLE");
  Assert.AreEqual("253",(string)middle["artilleryRawDamage"]["numerator"]);Assert.AreEqual("4",(string)middle["artilleryRawDamage"]["denominator"]);
  definition["enemies"]["baseHpEach"]=0;Assert.AreEqual(240,(int)input.FirstBattle.Definition["enemies"]["baseHpEach"]);
 }
 [TestCase("scale")][TestCase("hp")][TestCase("scope")] public void RehashedUnsupportedChangesCannotEnterTheFactory(string change){
  var start=(JObject)Export()["originalStart"];var packet=start["firstBattleEncounterSource"]["packet"];
  if(change=="scale")packet["appliedScaling"]=new JObject{["risk"]="I",["progress"]="EARLY"};
  if(change=="hp")packet["definition"]["enemies"]["baseHpEach"]=999;
  if(change=="scope")packet["runId"]="other-run";Rehash(start);
  Assert.Throws<ArgumentException>(()=>Read(start));
 }
 [Test] public void MissingHistoryAndExplicitNullAreDifferentAndExistingModelBytesArePreserved(){
  var export=Export();var start=(JObject)export["originalStart"];var receipt=export["initializationResponse"].ToString();
  var factory=OrdinaryFiveSeraFactory.FromInitialization(Read(start),receipt,500000);var model=factory.Create();model.Advance(500000,1);var saved=model.CaptureLocalState();
  Assert.AreEqual(saved,BasicCombatSimulation.RestoreLocalState(saved,factory.Create).CaptureLocalState());
  start.Remove("firstBattleEncounterSource");Assert.IsNull(Read(start).FirstBattle);
  var historicalReceipt=JObject.Parse(receipt);((JObject)historicalReceipt["initializationReceipt"]).Remove("firstBattleEncounterBinding");
  var historical=OrdinaryFiveSeraFactory.FromInitialization(Read(start),historicalReceipt.ToString(),500000);Assert.AreEqual(saved,BasicCombatSimulation.RestoreLocalState(saved,historical.Create).CaptureLocalState(),"The original full START is pinned by the store; unchanged Sera state needs no schema migration.");
  start["firstBattleEncounterSource"]=JValue.CreateNull();Assert.Throws<ArgumentException>(()=>Read(start));
 }
 [TestCase("version")][TestCase("originHash")][TestCase("sourceHash")][TestCase("null")][TestCase("missing-source")][TestCase("extra")]
 public void InitializationBindingMustMatchItsOriginalSource(string change){
  var export=Export();var start=(JObject)export["originalStart"];var response=(JObject)export["initializationResponse"];var receipt=(JObject)response["initializationReceipt"];
  var binding=(JObject)receipt["firstBattleEncounterBinding"];Assert.NotNull(binding,"Use the issued binding, not a fabricated substitute.");
  if(change=="null")receipt["firstBattleEncounterBinding"]=JValue.CreateNull();
  else if(change=="missing-source")start.Remove("firstBattleEncounterSource");
  else binding[change]="different";
  Assert.Throws<FormatException>(()=>OrdinaryInitializationReceipt.Validate(response.ToString(),Read(start)));
 }
 [Test] public void HistoricalReceiptWithoutBindingRemainsUnmodified(){
  var export=Export();var response=(JObject)export["initializationResponse"];var receipt=(JObject)response["initializationReceipt"];receipt.Remove("firstBattleEncounterBinding");var before=response.ToString();
  Assert.NotNull(OrdinaryInitializationReceipt.Validate(before,Read((JObject)export["originalStart"])));Assert.AreEqual(before,response.ToString());Assert.IsNull(receipt.Property("firstBattleEncounterBinding"));
 }
 [Test] public void ProductCheckpointPinsProfileThroughItsRetainedOriginalStart(){
  var fixture=OrdinaryFiveSeraFactoryTests.Sources();var start=fixture.Start;
  start["firstBattleEncounterSource"]=Export()["originalStart"]["firstBattleEncounterSource"].DeepClone();var packet=start["firstBattleEncounterSource"]["packet"];
  start["risk"]=packet["selectedRisk"].DeepClone();
  packet["accountId"]=fixture.Input.AccountId;packet["runId"]=fixture.Input.RunId;packet["originHash"]=fixture.Input.OriginHash;packet["formationRevision"]=fixture.Input.FormationRevision;Rehash(start);
  var input=Read(start);var factory=new OrdinaryFiveSeraFactory(input,fixture.Stats,500000);
  var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=input.RunId,["runStatus"]="ACTIVE",["startResponse"]=start,["errorCode"]=null};
  var original=new PendingAccountAction("a","departure-start","preview",0,"start-key","{\"operationPreviewId\":\"preview\"}",response.ToString(Newtonsoft.Json.Formatting.None));
  string saved=null;var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/first-battle-"+Guid.NewGuid()+".json",()=>saved,s=>saved=s);var build=new AppBuildSnapshot("source-binding-test",1);
  var binding=new OrdinaryFiveSeraCheckpoint(store,original,factory,build);var model=factory.Create();model.Advance(500000,1);binding.Save(model,0);
  Assert.AreEqual(model.CaptureLocalState(),new OrdinaryFiveSeraCheckpoint(store,original,factory,build).Restore().Simulation.CaptureLocalState());
  var missingProfileFactory=new OrdinaryFiveSeraFactory(fixture.Input,fixture.Stats,500000);
  Assert.Throws<ArgumentException>(()=>new OrdinaryFiveSeraCheckpoint(store,original,missingProfileFactory,build));
 }
 [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)] public void FrozenHistoricalSourceVersionsRemainReadable(int version){
  // Reconstruct frozen producer publications, without promoting a historical run to the new profile.
  var start=(JObject)Export()["originalStart"];var packet=(JObject)start["firstBattleEncounterSource"]["packet"];
  var definition=(JObject)packet["definition"];definition["version"]="combat.first-playable-profile.v1.20261027";definition["artillery"]["hazardVersion"]="rail.artillery-hazard.v1.20261023";definition.Remove("definitionHash");definition["definitionHash"]=BDirectAttackInput.Digest(definition).Substring(7);packet["references"]["artillery"]["hazardVersion"]="rail.artillery-hazard.v1.20261023";
  if(version==4){packet["version"]="combat.first-battle-encounter-source.v4.20261027";Rehash(start);Assert.AreEqual((string)start["risk"],Read(start).FirstBattle.SelectedRisk);return;}
  packet.Remove("selectedRisk");packet.Remove("scalingPolicy");
  if(version==3){packet["version"]="combat.first-battle-encounter-source.v3.20261027";Rehash(start);Assert.IsNull(Read(start).FirstBattle.SelectedRisk);return;}
  packet["version"]="combat.first-battle-encounter-source.v"+version+".20261026";packet["status"]="DEFINITION_NOT_ISSUED";packet["definition"]=JValue.CreateNull();packet.Remove("appliedScaling");
  foreach(var property in ((JObject)packet["references"]).Properties().ToArray())property.Value=JValue.CreateNull();
  packet["missing"]=new JArray("APPROVED_ENCOUNTER_DEFINITION_AND_REFERENCES",version==1?"FIRST_ACCOUNT_ELIGIBILITY_AND_UNLOCK_POLICY":"ACTUAL_FIRST_ACCOUNT_ADMISSION_AND_REQUIRED_FEATURE_EXECUTION_PROOF","ACTUAL_GEOMETRY_AND_SPEED_WINDOW_VALIDATION","EXISTING_MISSED_TRIGGER_POLICY");
  if(version==1)packet.Remove("entryRequirements");Rehash(start);
  var source=Read(start).FirstBattle;Assert.IsNull(source.Definition);Assert.IsFalse(source.HasAppliedScaling);Assert.AreEqual("DEFINITION_NOT_ISSUED",source.Status);
 }
}
