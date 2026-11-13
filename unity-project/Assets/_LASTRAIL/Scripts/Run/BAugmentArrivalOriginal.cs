using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Authenticated ARRIVE output only. This is not APPLIED or permission to start.
 // A replay carries the original grant-source envelope; first issuance carries
 // its snapshot directly. Preserve both without reissuing or changing the grant.
 public sealed class BAugmentArrivalOriginal {
  readonly JObject grant,source;
  public bool NoAward{get;}
  public long CompletionRevision{get;}
  public JObject CaptureGrant()=>grant==null?null:(JObject)grant.DeepClone();
  BAugmentArrivalOriginal(JObject value,JObject original,bool none,long revision){grant=value==null?null:(JObject)value.DeepClone();source=original==null?null:(JObject)original.DeepClone();NoAward=none;CompletionRevision=revision;}
  public void RequireEnvironment(string environment){if(source!=null&&(string)source["environment"]!=environment)throw new ArgumentException("b_augment_arrival_environment_conflict");}
  internal static BAugmentArrivalOriginal Read(JToken raw,BRouteCommand command,JObject arrival){
   if(command.Kind!="ARRIVE"||(string)command.Binding.Capture()["pendingNode"]?["eventKind"]!="AUGMENT"||!(raw is JObject p))throw new ArgumentException("b_augment_arrival_required");
   if((string)p["outcome"]=="NO_AWARD"){
    BDirectAttackInput.Fields(p,"outcome","catalogVersion","mapReceipt");var catalog=SafeTransitionWire.Id(p,"catalogVersion");
    var r=p["mapReceipt"] as JObject;BDirectAttackInput.Fields(r,"version","eventId","revision","state","verificationId");SafeTransitionWire.Id(r,"eventId");
    long revision=BDirectAttackInput.Nat(r,"revision",1);var s=r["state"] as JObject;BDirectAttackInput.Fields(s,"version","traversedEdgeIds","pendingEdgeId","phase","holdIds");
    if(revision!=(long)arrival["revision"]+1||(string)r["version"]!="operation.map-receipt.v1"||(string)r["verificationId"]!="augment-no-award:"+catalog||(string)s["version"]!="operation.map-progress.v1"||s["pendingEdgeId"].Type!=JTokenType.Null||!((string)s["phase"]=="READY"||(string)s["phase"]=="FINAL_NODE_COMPLETE")||!JToken.DeepEquals(s["traversedEdgeIds"],arrival["state"]["traversedEdgeIds"]))throw new ArgumentException("b_augment_zero_receipt_conflict");
    SafeTransitionWire.Ids(s["holdIds"]);return new BAugmentArrivalOriginal(null,null,true,revision);
   }
   JObject original=null,selection=p;
   if(p["selection"]!=null){
    BDirectAttackInput.Fields(p,"version","accountId","environment","runId","eventId","stageId","catalogVersion","storeRevision","issueHash","checkpointHash","startSnapshotHash","selection","sourceStatus","effectBinding","effectOwnershipEstablished","runtimeSupported","canStart","packetHash");
    SafeTransitionWire.Pin(p,"packetHash",(string)p["packetHash"]);
    if((string)p["version"]!="combat.augment-selection-source.v1.20261016"||(string)p["accountId"]!=command.Binding.AccountId||(string)p["startSnapshotHash"]!=(string)command.Binding.Capture()["startSnapshotHash"]||(string)p["sourceStatus"]!="ORIGINAL_GRANT_SELECTION_NOT_EFFECT_OWNERSHIP"||p["effectBinding"].Type!=JTokenType.Null)throw new ArgumentException("b_augment_grant_source_conflict");
    foreach(var key in new[]{"effectOwnershipEstablished","runtimeSupported","canStart"})SafeTransitionWire.Boolean(p,key,false);
    BDirectAttackInput.Nat(p,"storeRevision",1);SafeTransitionWire.Id(p,"environment");selection=p["selection"] as JObject??throw new ArgumentException("b_augment_snapshot_missing");
    foreach(var key in new[]{"runId","eventId","stageId","catalogVersion"})if(!JToken.DeepEquals(p[key],selection[key]))throw new ArgumentException("b_augment_snapshot_source_conflict");original=p;
   }
   if((string)selection["runId"]!=command.Binding.RunId||(string)selection["stageId"]!=(string)command.Binding.Capture()["pendingNode"]?["nodeId"])throw new ArgumentException("b_augment_grant_node_conflict");
   SafeTransitionWire.Id(selection,"eventId");SafeTransitionWire.Id(selection,"catalogVersion");
   return new BAugmentArrivalOriginal(selection,original,false,0);
  }
 }
}
