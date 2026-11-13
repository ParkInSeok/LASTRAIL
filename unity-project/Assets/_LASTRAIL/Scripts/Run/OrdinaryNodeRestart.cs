using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Compose only from retained node entry; the caller must durably retain key/body before sending.
 public sealed class OrdinaryNodeRestart {
  readonly OrdinaryNodeStart original;
  public OrdinaryNodeStart Original=>original;
  public string Key{get;}public string BodyJson{get;}public long ExpectedRevision{get;}public long ExpectedGeneration{get;}
  public string EndpointPath=>"/v1/runs/"+Uri.EscapeDataString(original.RunId)+"/operation-map/restart";
  public OrdinaryNodeRestart(OrdinaryNodeStart start,string key,long revision,long generation){
   original=start??throw new ArgumentNullException(nameof(start));
   if(string.IsNullOrEmpty(key)||key.Length>128||key.Any(c=>c<33||c>126)||revision<start.EntryMapRevision||revision>=OrdinaryNodeStart.SafeMaximum||generation<start.Generation||generation>=OrdinaryNodeStart.SafeMaximum)throw new ArgumentException("ordinary_node_restart_request_invalid");
   Key=key;ExpectedRevision=revision;ExpectedGeneration=generation;
   BodyJson=new JObject{["nodeId"]=start.NodeId,["expectedRevision"]=revision,["expectedGeneration"]=generation}.ToString(Formatting.None);
  }
  public OrdinaryNodeRestartResult ReadResponse(string json){
   var root=SafeTransitionWire.Parse(json);bool restarted=(string)root["status"]=="RESTARTED";
   BDirectAttackInput.Fields(root,restarted?new[]{"accepted","version","requestId","accountId","runId","nodeId","status","revision","generation","snapshot","snapshotHash"}:new[]{"accepted","version","requestId","accountId","runId","nodeId","status","revision","generation","runStatus","state"});
   SafeTransitionWire.Boolean(root,"accepted",true);
   OrdinaryNodeStart.Need((string)root["version"]=="ordinary.node-restart.v1.20261028"&&(string)root["requestId"]==Key&&(string)root["accountId"]==original.AccountId&&(string)root["runId"]==original.RunId&&(string)root["nodeId"]==original.NodeId);
   long revision=OrdinaryNodeStart.Revision(root,"revision");
   if(restarted){
    OrdinaryNodeStart.Need(revision==ExpectedRevision+1&&OrdinaryNodeStart.Revision(root,"generation")==ExpectedGeneration+1&&(string)root["snapshotHash"]==original.SnapshotHash&&original.MatchesSnapshot(root["snapshot"]));
   }else{
    OrdinaryNodeStart.Need((string)root["status"]=="RESULT_AVAILABLE"&&root["generation"].Type==JTokenType.Null&&revision>=ExpectedRevision);SafeTransitionWire.Id(root,"runStatus");
    var state=root["state"] as JObject;BDirectAttackInput.Fields(state,"version","traversedEdgeIds","pendingEdgeId","phase","holdIds");
    OrdinaryNodeStart.Need((string)state["version"]=="operation.map-progress.v1"&&new[]{"READY","TRAVELLING","ACTIVE","FINAL_NODE_COMPLETE"}.Contains((string)state["phase"]));
    SafeTransitionWire.Ids(state["traversedEdgeIds"]);SafeTransitionWire.Ids(state["holdIds"]);if(state["pendingEdgeId"].Type!=JTokenType.Null)SafeTransitionWire.Id(state,"pendingEdgeId");
   }
   return new OrdinaryNodeRestartResult(root,restarted?original:null);
  }
 }
 public sealed class OrdinaryNodeRestartResult {
  readonly JObject original;
  internal OrdinaryNodeRestartResult(JObject owned,OrdinaryNodeStart start){original=owned;Start=start;}
  public OrdinaryNodeStart Start{get;}
  public bool ResultAvailable=>Start==null;
  public long Revision=>(long)original["revision"];
  public long? Generation=>(long?)original["generation"];
  public JObject Capture()=>(JObject)original.DeepClone();
 }
}
