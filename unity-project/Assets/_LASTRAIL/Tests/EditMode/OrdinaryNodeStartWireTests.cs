using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryNodeStartWireTests {
 [TestCase(false)][TestCase(true)] public void OriginalActionValuesReachCommittedNodeWithoutStartingIncompleteRuntime(bool foreign){
  var f=Fixture();var entry=(JObject)f.Response["receipt"]["nodeStart"];var runtime=(JObject)entry["snapshot"]["runtime"];
  runtime["executionVersion"]=OrdinaryFiveCombatFactory.Version;runtime["determinism"]=new JObject{["version"]="ordinary.node-random.v1.20261029",["algorithm"]="XORSHIFT32_13_17_5_V1",["seed"]=123,["initialState"]=123};
  runtime["actions"]=OrdinaryNodeActionsTests.Fixture(f.Start);if(foreign)runtime["actions"]["actors"][0]["operatorId"]="other";
  entry["snapshotHash"]=BDirectAttackInput.Digest(entry["snapshot"]).Substring(7);
  var node=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart;
  if(foreign){Assert.Throws<ArgumentException>(()=>node.RequireOriginal(f.Start));return;}
  var actions=node.ReadActions(f.Start);Assert.AreEqual(21,actions.Actors[0].UltimateCharge);Assert.AreEqual(11,actions.Actors[0].ActiveCooldownRemainingTicks);Assert.IsTrue(actions.UltimateAuto);Assert.IsFalse(node.CanStart);
  var restarted=new OrdinaryNodeRestart(node,"actions-restart",node.EntryMapRevision,1).ReadResponse(RestartResponse(node,"actions-restart").ToString());
  Assert.AreEqual(21,restarted.Start.ReadActions(f.Start).Actors[0].UltimateCharge);
 }
 [TestCase(false)][TestCase(true)] public void RelocationDefinitionIsBoundWhenSuppliedWithoutRewritingHistoricalSources(bool changed){
  var f=Fixture();Assert.NotNull(f.Start.Relocation);var entry=(JObject)f.Response["receipt"]["nodeStart"];
  entry["snapshot"]["definitions"]["relocationHash"]=changed?"other":f.Start.Relocation.RelocationHash;
  entry["snapshotHash"]=BDirectAttackInput.Digest(entry["snapshot"]).Substring(7);
  var node=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart;
  if(changed)Assert.Throws<ArgumentException>(()=>node.RequireOriginal(f.Start));else Assert.DoesNotThrow(()=>node.RequireOriginal(f.Start));
 }
 internal static (OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response) Fixture(JObject geometryReference=null){
  var f=FirstPlayableScalingTests.Fixture(staticSource:true,geometryReference:geometryReference);var start=OrdinaryFiveStartInput.Read(f.Start,"a",(string)f.Start["runId"]);
  var node=f.Command.Binding.Capture()["pendingNode"]["nodeId"];var revision=f.Response["receipt"]["revision"];
  // Transport shape fixture only; runtime objects are explicitly synthetic and cannot authorize a host.
  var snapshot=new JObject{["version"]="ordinary.node-entry-state.v1.20261028",["accountId"]=start.AccountId,["runId"]=start.RunId,["startSnapshotHash"]=start.StartSnapshotHash,
   ["nodeId"]=node.DeepClone(),["entryMapRevision"]=revision.DeepClone(),["owners"]=new JObject{["healthRevision"]=1,["statRevision"]=1,["inventoryRevision"]=1},
   ["actors"]=new JArray(start.Actors.Select(a=>new JObject{["operatorId"]=a.OperatorId,["slot"]=a.Slot,["hp"]=70,["maxHp"]=100,["slots"]=new JArray(null,null,null),["effectiveStats"]=new JObject{["maxHp"]=100,["attack"]=new JObject{["numerator"]="1",["denominator"]="1"},["defense"]=1}})),
   ["train"]=new JObject{["trainId"]=start.Train.TrainId,["hp"]=800,["maxHp"]=1000},
   ["definitions"]=new JObject{["originHash"]=start.OriginHash,["kitHash"]=start.KitHash,["trainHash"]=start.Train.SourceHash,["railHash"]=start.Rail.SourceHash,["roofHash"]=start.Roof.SourceHash},
   ["modifierSource"]=new JObject(),["runtime"]=new JObject{["executionVersion"]="SYNTHETIC_UNSUPPORTED",["effects"]=new JObject(),["actions"]=new JObject(),["spatial"]=new JObject(),["enemies"]=new JObject(),["determinism"]=new JObject()}};
  f.Response["receipt"]["nodeStart"]=new JObject{["version"]="ordinary.node-start.v1.20261028",["accountId"]=start.AccountId,["runId"]=start.RunId,["nodeId"]=node.DeepClone(),["generation"]=1,["entryMapRevision"]=revision.DeepClone(),["snapshot"]=snapshot,["snapshotHash"]=BDirectAttackInput.Digest(snapshot).Substring(7)};
  return(start,f.Command,f.Response);
 }
 [TestCase("seed-negative")][TestCase("seed-overflow")][TestCase("seed-fraction")][TestCase("state-zero")][TestCase("state-mismatch")][TestCase("algorithm")][TestCase("version")][TestCase("extra")]
 public void CurrentExecutionCannotAcceptInvalidRandomOrigin(string fault){
  var f=Fixture();var entry=(JObject)f.Response["receipt"]["nodeStart"];var runtime=(JObject)entry["snapshot"]["runtime"];
  runtime["executionVersion"]=OrdinaryFiveCombatFactory.Version;
  var random=new JObject{["version"]="ordinary.node-random.v1.20261029",["algorithm"]="XORSHIFT32_13_17_5_V1",["seed"]=123,["initialState"]=123};runtime["determinism"]=random;
  if(fault=="seed-negative")random["seed"]=-1;if(fault=="seed-overflow")random["seed"]=4294967296L;if(fault=="seed-fraction")random["seed"]=1.5;
  if(fault=="state-zero")random["initialState"]=0;if(fault=="state-mismatch")random["initialState"]=124;
  if(fault=="algorithm")random["algorithm"]="OTHER";if(fault=="version")random["version"]="OTHER";if(fault=="extra")random["fallback"]=1;
  entry["snapshotHash"]=BDirectAttackInput.Digest(entry["snapshot"]).Substring(7);
  Assert.Throws<ArgumentException>(()=>BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));
 }
 [TestCase(0L,1831565813L)][TestCase(123L,123L)][TestCase(4294967295L,4294967295L)]
 public void OriginalRandomMatchesSharedCoreAndRestartDoesNotReroll(long seed,long state){
  var f=Fixture();var entry=(JObject)f.Response["receipt"]["nodeStart"];var runtime=(JObject)entry["snapshot"]["runtime"];
  runtime["executionVersion"]=OrdinaryFiveCombatFactory.Version;runtime["determinism"]=new JObject{["version"]="ordinary.node-random.v1.20261029",["algorithm"]="XORSHIFT32_13_17_5_V1",["seed"]=seed,["initialState"]=state};entry["snapshotHash"]=BDirectAttackInput.Digest(entry["snapshot"]).Substring(7);
  var start=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart;Assert.NotNull(start.ExecutionOrigin);Assert.IsFalse(start.CanStart);
  var random=start.ExecutionOrigin.CreateInitialRandom();Assert.AreEqual((uint)state,random.ExportState());uint first=random.NextUInt();random.NextUInt();
  var request=new OrdinaryNodeRestart(start,"seed-restart",start.EntryMapRevision,1);var restarted=request.ReadResponse(RestartResponse(start,request.Key).ToString());
  var fresh=restarted.Start.ExecutionOrigin.CreateInitialRandom();Assert.AreEqual(first,fresh.NextUInt());Assert.AreEqual(seed,(long)restarted.Start.ExecutionOrigin.Seed);
  var copy=start.Capture();copy["snapshot"]["runtime"]["determinism"]["seed"]=9;Assert.AreEqual(seed,(long)start.ExecutionOrigin.Seed);
 }
 [Test] public async Task ArrivalAcceptsNodeStartAlongsideExistingScaling(){
  var f=Fixture();var result=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);Assert.NotNull(result.Scaling);result.NodeStart.RequireOriginal(f.Start);Assert.IsFalse(result.NodeStart.CanStart);
  Assert.IsNull(result.NodeStart.ExecutionOrigin,"Historical unsupported source must not acquire a default seed");var copy=result.NodeStart.Capture();copy["snapshot"]["actors"][0]["hp"]=25;
  Assert.AreEqual(70,(int)result.NodeStart.Capture()["snapshot"]["actors"][0]["hp"]);
  using var controller=new BRouteSessionController(f.Start.AccountId,f.Start.RunId);await controller.RefreshAsync(()=>Task.FromResult(f.Command.Binding));
  await controller.SendAsync(f.Command,()=>Task.FromResult(result));Assert.AreEqual(result.NodeStart.SnapshotHash,controller.ReadOrdinaryNodeStart(f.Start).SnapshotHash);
 }
 [TestCase("account")][TestCase("node")][TestCase("generation")][TestCase("revision")][TestCase("hash")][TestCase("hp")][TestCase("actor")][TestCase("definition")][TestCase("null")][TestCase("runtime")]
 public void InvalidOrRehashedEntryCannotReachOriginalOwner(string fault){
  var f=Fixture();var entry=(JObject)f.Response["receipt"]["nodeStart"];var snapshot=(JObject)entry["snapshot"];
  if(fault=="account")entry["accountId"]="other";
  if(fault=="node")entry["nodeId"]="other";
  if(fault=="generation")entry["generation"]=2;
  if(fault=="revision")entry["entryMapRevision"]=999;
  if(fault=="hash")entry["snapshotHash"]=new string('0',64);
  if(fault=="hp")snapshot["actors"][0]["hp"]=101;
  if(fault=="actor")snapshot["actors"][0]["operatorId"]="other";
  if(fault=="definition")snapshot["definitions"]["roofHash"]="other";
  if(fault=="runtime")((JObject)snapshot["runtime"]).Remove("determinism");
  if(fault!="hash")entry["snapshotHash"]=BDirectAttackInput.Digest(snapshot).Substring(7);
  if(fault=="null")f.Response["receipt"]["nodeStart"]=JValue.CreateNull();
  Assert.Throws<ArgumentException>(()=>BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart.RequireOriginal(f.Start));
 }
 [Test] public void HistoricalArrivalHasNoInventedNodeStart(){var f=Fixture();((JObject)f.Response["receipt"]).Remove("nodeStart");Assert.IsNull(BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart);}
 static JObject RestartResponse(OrdinaryNodeStart start,string key)=>new JObject{["accepted"]=true,["version"]="ordinary.node-restart.v1.20261028",["requestId"]=key,["accountId"]=start.AccountId,["runId"]=start.RunId,["nodeId"]=start.NodeId,["status"]="RESTARTED",["revision"]=start.EntryMapRevision+1,["generation"]=2,["snapshot"]=start.Capture()["snapshot"],["snapshotHash"]=start.SnapshotHash};
 [Test] public void RestartRetainsSeventyAndEightHundredWithoutPromotingRuntimeSupport(){
  var f=Fixture();var start=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart;start.RequireOriginal(f.Start);
  var request=new OrdinaryNodeRestart(start,"original-key",start.EntryMapRevision,1);var body=JObject.Parse(request.BodyJson);Assert.AreEqual(3,body.Count);Assert.IsNull(body["snapshot"]);
  var result=request.ReadResponse(RestartResponse(start,request.Key).ToString());Assert.AreEqual(2,result.Generation);Assert.IsFalse(result.ResultAvailable);Assert.IsFalse(result.Start.CanStart);
  Assert.AreEqual(70,(int)result.Start.Capture()["snapshot"]["actors"][0]["hp"]);Assert.AreEqual(800,(int)result.Start.Capture()["snapshot"]["train"]["hp"]);
 }
 [TestCase("hp")][TestCase("generation")][TestCase("revision")][TestCase("key")][TestCase("account")]
 public void ChangedRestartCannotReplaceNodeOrigin(string fault){
  var f=Fixture();var start=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart;var request=new OrdinaryNodeRestart(start,"original-key",start.EntryMapRevision,1);var reply=RestartResponse(start,request.Key);
  if(fault=="hp"){reply["snapshot"]["actors"][0]["hp"]=25;reply["snapshotHash"]=BDirectAttackInput.Digest(reply["snapshot"]).Substring(7);}
  if(fault=="generation")reply["generation"]=1;
  if(fault=="revision")reply["revision"]=start.EntryMapRevision;
  if(fault=="key")reply["requestId"]="different";
  if(fault=="account")reply["accountId"]="other";
  Assert.Throws<ArgumentException>(()=>request.ReadResponse(reply.ToString()));
 }
 [Test] public void ConfirmedResultNeverOffersOldSnapshotToHost(){
  var f=Fixture();var start=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart;var request=new OrdinaryNodeRestart(start,"key",start.EntryMapRevision,1);var reply=RestartResponse(start,"key");
  reply["status"]="RESULT_AVAILABLE";reply["generation"]=JValue.CreateNull();reply.Remove("snapshot");reply.Remove("snapshotHash");reply["runStatus"]="ACTIVE";
  reply["state"]=new JObject{["version"]="operation.map-progress.v1",["traversedEdgeIds"]=new JArray(),["pendingEdgeId"]=null,["phase"]="READY",["holdIds"]=new JArray()};
  var result=request.ReadResponse(reply.ToString());Assert.IsTrue(result.ResultAvailable);Assert.IsNull(result.Start);Assert.IsNull(result.Generation);
 }
}
