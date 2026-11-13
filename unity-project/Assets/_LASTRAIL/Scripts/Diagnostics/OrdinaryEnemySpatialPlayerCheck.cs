#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Diagnostics {
 public static class OrdinaryEnemySpatialPlayerCheck {
  public static void Run(string output,string fixturePath){
   void Need(bool value){if(!value)throw new InvalidOperationException("Enemy spatial Player contract check failed");}
   Need(Path.GetFullPath(fixturePath).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase));
   var fixture=JObject.Parse(File.ReadAllText(fixturePath));Need((string)fixture["scope"]=="SYNTHETIC_CONTRACT_FIXTURE_NOT_SERVER_RUN");
   var packet=(JObject)fixture["binding"];var binding=BRouteBinding.Read(packet.ToString(),(string)packet["packetHash"],(string)packet["accountId"],(string)packet["runId"]);
   var command=BRouteCommand.FromOriginal(binding,(JObject)fixture["command"]);var response=(JObject)fixture["response"];string original=response.ToString();
   var result=BRouteCommandResult.Read(200,original,command);var node=result.NodeStart;var spatial=node.Enemies.Spatial;Need(spatial!=null&&!node.CanStart&&spatial.PhysicalInstanceId==result.Scaling.IssuedEnemyTrainId);
   var definition=spatial.CaptureDefinition();Need(definition["muzzle"].Type==JTokenType.Null&&definition["hitVolume"].Type==JTokenType.Null&&(bool)definition["executionVerified"]==false);
   definition["placement"]["footAnchorYmm"]=0;Need((decimal)spatial.CaptureDefinition()["placement"]["footAnchorYmm"]==3213);
   var restart=new OrdinaryNodeRestart(node,"v2-player-restart",node.EntryMapRevision,node.Generation);
   var replay=new JObject{["accepted"]=true,["version"]="ordinary.node-restart.v1.20261028",["requestId"]=restart.Key,["accountId"]=node.AccountId,["runId"]=node.RunId,["nodeId"]=node.NodeId,["status"]="RESTARTED",["revision"]=node.EntryMapRevision+1,["generation"]=node.Generation+1,["snapshot"]=node.Capture()["snapshot"],["snapshotHash"]=node.SnapshotHash};
   Need(ReferenceEquals(node,restart.ReadResponse(replay.ToString()).Start));
   void Pin(JObject root){var n=root["receipt"]["nodeStart"];n["snapshotHash"]=BDirectAttackInput.Digest(n["snapshot"]).Substring(7);}
   var foreign=(JObject)response.DeepClone();foreign["receipt"]["nodeStart"]["snapshot"]["runtime"]["enemies"]["spatial"]["physicalInstanceId"]="foreign";Pin(foreign);
   bool rejected=false;try{BRouteCommandResult.Read(200,foreign.ToString(),command);}catch(ArgumentException){rejected=true;}Need(rejected);
   var legacy=(JObject)response.DeepClone();var enemies=(JObject)legacy["receipt"]["nodeStart"]["snapshot"]["runtime"]["enemies"];enemies["version"]=OrdinaryNodeEnemies.Version;enemies.Remove("spatial");Pin(legacy);
   var old=BRouteCommandResult.Read(200,legacy.ToString(),command).NodeStart;Need(old.Enemies!=null&&old.Enemies.Spatial==null&&!old.CanStart&&response.ToString()==original);
   File.WriteAllText(Path.Combine(output,"enemy-spatial-check.json"),new JObject{["passed"]=true,["scope"]="ACTUAL_PLAYER_READER_SYNTHETIC_ARRIVE_NOT_GEOMETRY_EXECUTION",["version"]=OrdinaryNodeEnemies.SpatialVersion,["definitionHash"]=spatial.DefinitionHash,["geometryDefinitionHash"]=spatial.GeometryDefinitionHash,["physicalInstanceId"]=spatial.PhysicalInstanceId,["foreignInstanceRejected"]=rejected,["v1Preserved"]=true,["restartSameOwner"]=true,["executionVerified"]=false}.ToString());
  }
 }
}
#endif
