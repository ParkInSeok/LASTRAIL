using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryNodeCombatFactoryTests {
 // Synthetic committed-node contract only; no live admission or native geometry.
 static (OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response) Fixture() {
  var f=OrdinaryNodeStartWireTests.Fixture();var parsed=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);var scale=parsed.Scaling;var profile=f.Command.Binding.Capture()["pendingNode"]["encounterDefinition"];
  var snapshot=(JObject)f.Response["receipt"]["nodeStart"]["snapshot"];var runtime=(JObject)snapshot["runtime"];
  foreach(var a in snapshot["actors"])a["effectiveStats"]["defense"]=new JObject{["numerator"]="7",["denominator"]="2"};
  runtime["executionVersion"]=OrdinaryFiveCombatFactory.Version;runtime["determinism"]=new JObject{["version"]="ordinary.node-random.v1.20261029",["algorithm"]="XORSHIFT32_13_17_5_V1",["seed"]=123,["initialState"]=123};
  runtime["actions"]=OrdinaryNodeActionsTests.Fixture(f.Start);runtime["effects"]=new JObject{["unsupportedEffect"]="retain-original"};runtime["spatial"]=new JObject{["unresolvedPlacement"]="retain-original"};
  runtime["enemies"]=new JObject{["version"]="ordinary.node-enemies.v1.20261031",["scalingBindingHash"]=scale.BindingHash,["actionDefinition"]=profile["enemies"]["definition"].DeepClone(),["gunners"]=new JArray(scale.IssuedGunnerIds.Select(id=>new JObject{["instanceId"]=id,["hp"]=scale.GunnerHp.Snapshot(),["maxHp"]=scale.GunnerHp.Snapshot(),["defense"]=profile["enemies"]["defense"].DeepClone(),["rawDamage"]=scale.GunnerRawDamage.Snapshot()})),["artillery"]=new JObject{["hazardId"]=scale.IssuedHazardId,["hp"]=scale.LauncherHp.Snapshot(),["maxHp"]=scale.LauncherHp.Snapshot(),["rawDamage"]=scale.ArtilleryRawDamage.Snapshot(),["definition"]=profile["artillery"].DeepClone(),["minimumThreatClearToPreviewMs"]=profile["minimumThreatClearToPreviewMs"].DeepClone(),["previewRequires"]=profile["previewRequires"].DeepClone()}};
  Pin(f.Response);return f;
 }
 static void Pin(JObject response){var entry=response["receipt"]["nodeStart"];entry["snapshotHash"]=BDirectAttackInput.Digest(entry["snapshot"]).Substring(7);}
 static OrdinaryFiveCombatFactory Factory(OrdinaryFiveStartInput start,BRouteCommandResult result,long step=170000) {
  var method=typeof(OrdinaryFiveCombatFactory).GetMethod("FromNodeStart",new[]{typeof(OrdinaryFiveStartInput),typeof(OrdinaryNodeStart),typeof(FirstPlayableScaling),typeof(long)});
  Assert.NotNull(method,"Missing node-start-only preparation factory");
  try{return (OrdinaryFiveCombatFactory)method.Invoke(null,new object[]{start,result.NodeStart,result.Scaling,step});}catch(TargetInvocationException e){throw e.InnerException;}
 }
 [Test] public void NodeHpTrainRandomAndActionValuesReachPreparationWithoutOpeningCombat() {
  var f=Fixture();var result=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);var factory=Factory(f.Start,result);var model=factory.Create();var hud=model.ReadHudState();
  Assert.AreEqual(123u,model.RandomState);Assert.AreEqual(800,(int)hud["trainHp"]);Assert.AreEqual(1000,(int)hud["trainMaxHp"]);Assert.IsTrue((bool)hud["ultimateAuto"]);
  foreach(var a in f.Start.Actors){var row=hud["actors"].Single(r=>(string)r["id"]==a.OperatorId);Assert.AreEqual(70,(int)row["hp"]);Assert.AreEqual(100,(int)row["maxHp"]);Assert.AreEqual(a.Slot+1,(long)row["basicNextOpportunityInTicks"]);Assert.AreEqual((a.Slot+11)*17,(long)hud["skills"].Single(r=>(string)r["actor"]==a.OperatorId)["cooldownMs"]);Assert.AreEqual(a.Slot+21,(int)hud["ultimates"].Single(r=>(string)r["actor"]==a.OperatorId)["charge"]);}
  CollectionAssert.AreEquivalent(result.NodeStart.Enemies.Gunners.Select(g=>g.InstanceId),hud["actors"].Where(r=>(bool)r["enemy"]).Select(r=>(string)r["id"]));
  Assert.IsFalse(result.NodeStart.CanStart);Assert.IsFalse((bool)hud["fullCombatReady"]);Assert.IsTrue(hud["skills"].All(r=>!(bool)r["enabled"]));
  CollectionAssert.Contains(hud["unsupported"].Select(t=>(string)t).ToArray(),"NODE_EFFECTS_AND_SPATIAL");
  var before=model.CaptureLocalState();model.Advance(10000000,20);Assert.AreEqual(before,model.CaptureLocalState());Assert.AreEqual("REJECTED_PREPARATION",model.RequestSkill("node-shield","op_kyle","op_kyle.active"));
  Assert.AreEqual("retain-original",(string)result.NodeStart.Capture()["snapshot"]["runtime"]["effects"]["unsupportedEffect"]);
  var output=Environment.GetEnvironmentVariable("LASTRAIL_NODE_PRESENTATION_SOURCE");if(!string.IsNullOrEmpty(output)){
   var path=System.IO.Path.GetFullPath(output);Assert.IsTrue(path.StartsWith(@"D:\Project\LASTRAIL\QA\",StringComparison.OrdinalIgnoreCase));Assert.IsFalse(System.IO.File.Exists(path));
   var raw=FirstPlayableScalingTests.Fixture(staticSource:true).Start;Assert.AreEqual(f.Start.StartSnapshotHash,OrdinaryFiveStartInput.Read(raw,f.Start.AccountId,f.Start.RunId).StartSnapshotHash);
   var assets=LastRail.Resources.PreparedContent.Load<CombatPlayerAssetBinding>("CombatPresentation/OrdinaryBinding");
   System.IO.File.WriteAllText(path,new JObject{["scope"]="ACTUAL_NODE_FACTORY_SYNTHETIC_ARRIVAL_NOT_LIVE_ENTRY",["originalStart"]=raw,["binding"]=f.Command.Binding.Capture(),["command"]=f.Command.Capture()["command"],["response"]=f.Response,["manifestHash"]=assets.ManifestHash}.ToString());
  }
 }
 [Test] public void FreshCreateReusesImmutableEntryAndRejectsMidNodeCheckpointReplay() {
  var f=Fixture();var result=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);var factory=Factory(f.Start,result);var model=factory.Create();var original=model.CaptureLocalState();
  model.SetPaused(true);var rejected=Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(model.CaptureLocalState(),factory.Create));Assert.IsInstanceOf<InvalidOperationException>(rejected.InnerException);StringAssert.Contains("restart-only",rejected.InnerException.Message);
  f.Response["receipt"]["nodeStart"]["snapshot"]["actors"][0]["hp"]=1;var copy=result.NodeStart.Capture();copy["snapshot"]["actors"][0]["hp"]=2;
  Assert.AreEqual(original,factory.Create().CaptureLocalState());Assert.AreEqual(123u,factory.Create().RandomState);
  var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/node-factory-rejected.json",()=>null,_=>Assert.Fail("Node must not write initialization store"));
  var error=Assert.Throws<InvalidOperationException>(()=>new OrdinaryFiveCombatCheckpoint(store,null,factory,null));StringAssert.Contains("Node entry",error.Message);
  Assert.Throws<InvalidOperationException>(()=>{var ignored=factory.InitialPacketHash;});
 }
 [TestCase("step")][TestCase("stats")][TestCase("enemy")][TestCase("random")]
 public void RejectsMissingOrMismatchedNodeInputsWithoutInitialReceiptFallback(string fault) {
  var f=Fixture();var snapshot=f.Response["receipt"]["nodeStart"]["snapshot"];
  if(fault=="stats")((JObject)snapshot["actors"][0]["effectiveStats"]).Remove("attack");if(fault=="enemy")snapshot["runtime"]["enemies"]=new JObject();if(fault=="random")snapshot["runtime"]["executionVersion"]="SYNTHETIC_UNSUPPORTED";
  Pin(f.Response);var result=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
  Assert.Throws<ArgumentException>(()=>Factory(f.Start,result,fault=="step"?500000:170000));
 }
 [Test] public void SameNodeDriverStagesCommandsWithoutAllowingExternalCheckpointRecovery() {
  var f=Fixture();var result=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);var factory=Factory(f.Start,result);int writes=0;
  var driver=new CombatSceneDriver(factory.Create,()=>null,(state,revision)=>{writes++;return revision+1;},()=>true);driver.Open();
  var before=driver.StateJson;driver.Advance(1000000,10);Assert.AreEqual(before,driver.StateJson);Assert.AreEqual(1,writes);Assert.IsFalse(driver.RecoveryRequired);
  driver.SetUltimateAuto(false);Assert.IsFalse((bool)driver.HudState["ultimateAuto"]);Assert.AreEqual(2,writes);
  driver.SetPaused(true);Assert.IsTrue((bool)driver.HudState["paused"]);Assert.AreEqual(3,writes);
  Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(driver.StateJson,factory.Create));
  Assert.IsTrue((bool)factory.Create().ReadHudState()["ultimateAuto"]);Assert.AreEqual(800,(int)driver.HudState["trainHp"]);
 }
 static BCombatLocalModel Health(BasicCombatSimulation model) {
  var field=typeof(BasicCombatSimulation).GetField("ordinaryHealth",BindingFlags.Instance|BindingFlags.NonPublic);
  Assert.NotNull(field,"Node simulation must use the existing B HP kernel");var value=field.GetValue(model) as BCombatLocalModel;Assert.NotNull(value);return value;
 }
 [Test] public void NodeDamageShieldAndHealUseOneKernelThroughSceneTransactions() {
  var f=Fixture();var factory=Factory(f.Start,BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));
  var driver=new CombatSceneDriver(factory.Create,()=>null,(state,revision)=>revision+1,()=>true);driver.Open();
  BasicCombatSimulation Current()=>(BasicCombatSimulation)typeof(CombatSceneDriver).GetField("current",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver);
  var health=Health(Current());string id=f.Start.Actors[0].OperatorId;
  health.ApplyResolvedShield("shared-shield",id,new CombatShieldSource(id,"test",0),20,50,0);
  var hit=health.ApplyResolvedDamage("shared-hit",id,40,0);Assert.AreEqual(20,hit.Absorbed);Assert.AreEqual(20,hit.HpLoss);
  Assert.AreEqual(50,(int)driver.HudState["actors"].Single(a=>(string)a["id"]==id)["hp"]);
  driver.SetPaused(true);var staged=Health(Current());Assert.AreNotSame(health,staged);Assert.AreEqual(50,staged.Hp(id));
  Assert.AreSame(hit,staged.ApplyResolvedDamage("shared-hit",id,40,0));Assert.AreEqual(50,staged.Hp(id));
  var heal=typeof(BCombatLocalModel).GetMethod("ApplyResolvedLivingSkillHeal");Assert.NotNull(heal);heal.Invoke(staged,new object[]{"shared-heal",id,10,0});
  Assert.AreEqual(60,(int)driver.HudState["actors"].Single(a=>(string)a["id"]==id)["hp"]);Assert.AreEqual(50,health.Hp(id),"A new transaction cannot mutate the previous HP root");
  Assert.AreEqual(70,Health(factory.Create()).Hp(id),"Fresh node entry retains original HP");
 }
 [Test] public void NodeHealthNeverInventsCarriageTargetsOrAcceptsLegacyReplay() {
  var f=Fixture();var factory=Factory(f.Start,BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));var model=factory.Create();var health=Health(model);
  var capture=JObject.Parse(health.CaptureLocal());Assert.AreEqual(8,((JObject)capture["vitals"]).Count);Assert.IsEmpty((JArray)capture["events"]);
  Assert.Throws<ArgumentException>(()=>health.Hp("car_combat"));
  Assert.Throws<ArgumentException>(()=>BCombatLocalModel.RestoreLocal(health.CaptureLocal(),null,Array.Empty<BCombatHpBinding>(),20,BCombatAuthority.UnitySolo));
 }
 [Test] public void OwnedNodeClonePreservesAdvancedRandomAndDoesNotPublishOnSaveFailure() {
  var f=Fixture();var factory=Factory(f.Start,BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));bool fail=false;
  var driver=new CombatSceneDriver(factory.Create,()=>null,(state,revision)=>{if(fail)throw new InvalidOperationException("save rejected");return revision+1;},()=>true);driver.Open();
  BasicCombatSimulation Current()=>(BasicCombatSimulation)typeof(CombatSceneDriver).GetField("current",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver);
  var field=typeof(BasicCombatSimulation).GetField("random",BindingFlags.Instance|BindingFlags.NonPublic);var rng=(LastRail.Core.Simulation.RunRandom)field.GetValue(Current());rng.NextUInt();field.SetValue(Current(),rng);
  driver.SetPaused(true);Assert.AreEqual(rng.ExportState(),Current().RandomState,"An in-memory transaction must not reset the random owner to node entry");
  string before=driver.StateJson;var root=Health(Current());fail=true;Assert.Throws<InvalidOperationException>(()=>driver.SetSpeed(3));
  Assert.AreSame(root,Health(Current()));Assert.AreEqual(before,driver.StateJson);Assert.IsTrue(driver.RecoveryRequired);
 }
 [Test] public void DifferentCommittedNodeSnapshotCannotShareFactoryIdentity() {
  var f=Fixture();var one=Factory(f.Start,BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));f.Response["receipt"]["nodeStart"]["snapshot"]["actors"][0]["hp"]=69;Pin(f.Response);
  var two=Factory(f.Start,BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));Assert.AreNotEqual(one.ConfigurationVersion,two.ConfigurationVersion);
 }
 [Test] public void NodeDriverHasExplicitMemoryLifetimeAndRejectsLateCommands() {
  var f=Fixture();var factory=Factory(f.Start,BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));bool owner=true;
  var create=typeof(CombatSceneDriver).GetMethod("ForOrdinaryNode");Assert.NotNull(create,"Node needs an explicit memory-only owner, not fake checkpoint delegates");
  var driver=(CombatSceneDriver)create.Invoke(null,new object[]{factory,(Func<bool>)(()=>owner)});driver.Open();
  string initial=driver.StateJson;Assert.Throws<InvalidOperationException>(()=>driver.Open());Assert.AreEqual(initial,driver.StateJson);
  driver.SetPaused(true);string paused=driver.StateJson;driver.Advance(10000000,10);Assert.AreEqual(paused,driver.StateJson);driver.SetPaused(false);
  driver.SetSpeed(3);Assert.IsTrue(driver.TryGetCommittedRevision(out var revision));Assert.Greater(revision,1);
  var retire=typeof(CombatSceneDriver).GetMethod("Retire");Assert.NotNull(retire);retire.Invoke(driver,null);retire.Invoke(driver,null);
  Assert.IsNull(driver.StateJson);Assert.IsFalse(driver.TryGetCommittedRevision(out _));
  Assert.Throws<InvalidOperationException>(()=>driver.SetSpeed(4));Assert.Throws<InvalidOperationException>(()=>driver.Open());
  var replacement=(CombatSceneDriver)create.Invoke(null,new object[]{factory,(Func<bool>)(()=>owner)});replacement.Open();Assert.AreEqual(initial,replacement.StateJson);
  owner=false;Assert.Throws<InvalidOperationException>(()=>replacement.SetSpeed(4));Assert.IsFalse(replacement.TryGetCommittedRevision(out _));
 }
 [Test] public void RestartUsesCurrentAttemptWithoutRewritingOriginalNode(){
  var f=Fixture();var arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);var node=arrival.NodeStart;
  var request=new OrdinaryNodeRestart(node,"attempt-test",node.EntryMapRevision,node.Generation);
  var response=new JObject{["accepted"]=true,["version"]="ordinary.node-restart.v1.20261028",["requestId"]=request.Key,["accountId"]=node.AccountId,["runId"]=node.RunId,["nodeId"]=node.NodeId,["status"]="RESTARTED",["revision"]=node.EntryMapRevision+1,["generation"]=2,["snapshot"]=node.Capture()["snapshot"],["snapshotHash"]=node.SnapshotHash};
  var attempt=OrdinaryNodeAttempt.FromRestart(request.ReadResponse(response.ToString()));
  var factory=OrdinaryFiveCombatFactory.FromNodeStart(f.Start,node,arrival.Scaling,170000,attempt);
  Assert.AreEqual(1,node.Generation);Assert.AreEqual(2,factory.Attempt.Generation);Assert.AreEqual(node.EntryMapRevision+1,attempt.Revision);
  Assert.AreNotEqual(Factory(f.Start,arrival).ConfigurationVersion,factory.ConfigurationVersion);
  var health=JObject.Parse(Health(factory.Create()).CaptureLocal());Assert.AreEqual(2,(long)health["definition"]["generation"]);Assert.AreEqual(800,(int)factory.Create().ReadHudState()["trainHp"]);
  f.Response["receipt"]["nodeStart"]["snapshot"]["actors"][0]["hp"]=69;Pin(f.Response);var other=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
  Assert.Throws<ArgumentException>(()=>OrdinaryFiveCombatFactory.FromNodeStart(f.Start,other.NodeStart,other.Scaling,170000,attempt));
 }
}
