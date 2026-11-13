using System;
using System.Linq;
using System.Text;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Run {
 // A reference to server-verifiable evidence, never a client claim of combat truth.
 public sealed class OrdinaryNodeCompletion {
  public string AccountId{get;}
  public string RunId{get;}
  public string Key{get;}
  public string BodyJson{get;}
  readonly OrdinaryNodeAttempt boundAttempt;
  public OrdinaryNodeCompletion(string account,string run,string key,string node,string evidence,long revision){
   Id(account);Id(run);
   if(!RunRequestKey.IsSupported(key,1)||key.Contains(" "))throw new ArgumentException("Invalid completion key.");
   var body=new JObject{["nodeId"]=node,["evidenceRef"]=evidence,["expectedRevision"]=revision};
   BodyJson=body.ToString(Formatting.None);ValidateBody(BodyJson);
   AccountId=account;RunId=run;Key=key;
  }
  // The caller supplies an independently verified evidence reference; binding is not combat proof.
  public OrdinaryNodeCompletion(string account,string run,string key,OrdinaryNodeAttempt attempt,string evidence)
   :this(account,run,key,attempt?.NodeId,evidence,attempt?.Revision??0){
   if(attempt==null||attempt.AccountId!=account||attempt.RunId!=run)throw new ArgumentException("Completion attempt scope differs.");
   boundAttempt=attempt;
   var body=SafeTransitionWire.Parse(BodyJson);body["attemptGeneration"]=attempt.Generation;
   BodyJson=body.ToString(Formatting.None);ValidateBody(BodyJson);
  }
  public void RequireCurrentAttempt(OrdinaryNodeAttempt current){
   if(boundAttempt==null||current==null||current.AccountId!=AccountId||current.RunId!=RunId||
    current.NodeId!=boundAttempt.NodeId||current.SnapshotHash!=boundAttempt.SnapshotHash||
    current.Generation!=boundAttempt.Generation||current.Revision!=boundAttempt.Revision)
    throw new ArgumentException("Completion no longer belongs to the current node attempt.");
  }
  static void Id(string value){if(string.IsNullOrEmpty(value)||value.Length>200)throw new ArgumentException("Invalid completion reference.");}
  internal static JObject ValidateBody(string json){
   if(json==null||Encoding.UTF8.GetByteCount(json)>4096)throw new ArgumentException("Completion body exceeds its wire budget.");
   var body=SafeTransitionWire.Parse(json);bool hasAttempt=body.Property("attemptGeneration")!=null;
   BDirectAttackInput.Fields(body,hasAttempt?new[]{"nodeId","evidenceRef","expectedRevision","attemptGeneration"}:new[]{"nodeId","evidenceRef","expectedRevision"});
   Id(BDirectAttackInput.Text(body,"nodeId"));Id(BDirectAttackInput.Text(body,"evidenceRef"));
   BDirectAttackInput.Nat(body,"expectedRevision",1,9007199254740991);
   if(hasAttempt)BDirectAttackInput.Nat(body,"attemptGeneration",1,9007199254740991);
   return body;
  }
  internal static JObject ValidateResponse(string json,string originalBody,string key){
   var request=ValidateBody(originalBody);var root=SafeTransitionWire.Parse(json);
   BDirectAttackInput.Fields(root,"accepted","receipt");SafeTransitionWire.Boolean(root,"accepted",true);
   var receipt=root["receipt"] as JObject;BDirectAttackInput.Fields(receipt,new[]{"version","eventId","revision","state","verificationId"}.Concat(receipt?.Property("ordinaryAutomation")==null?Array.Empty<string>():new[]{"ordinaryAutomation"}).ToArray());
   if((string)receipt["version"]!="operation.map-receipt.v1"||(string)receipt["eventId"]!=key||
    BDirectAttackInput.Nat(receipt,"revision",1,9007199254740991)!=checked((long)request["expectedRevision"]+1))throw new ArgumentException("Completion receipt differs from original request.");
   Id(BDirectAttackInput.Text(receipt,"verificationId"));
   var state=receipt["state"] as JObject;BDirectAttackInput.Fields(state,"version","traversedEdgeIds","pendingEdgeId","phase","holdIds");
   if((string)state["version"]!="operation.map-progress.v1"||state["pendingEdgeId"].Type!=JTokenType.Null||
    ((string)state["phase"]!="READY"&&(string)state["phase"]!="FINAL_NODE_COMPLETE"))throw new ArgumentException("Invalid completed node state.");
   SafeTransitionWire.Ids(state["traversedEdgeIds"]);SafeTransitionWire.Ids(state["holdIds"]);
   if(receipt.Property("ordinaryAutomation")!=null)OrdinaryRunAutomation.Read(receipt["ordinaryAutomation"]);
   return root;
  }
  public OrdinaryRunAutomation ReadAutomation(string response,OrdinaryFiveStartInput start){
   if(start==null||start.AccountId!=AccountId||start.RunId!=RunId)throw new ArgumentException("Completion source differs from original run.");
   var receipt=(JObject)ValidateResponse(response,BodyJson,Key)["receipt"];var automation=receipt.Property("ordinaryAutomation")==null?null:OrdinaryRunAutomation.Read(receipt["ordinaryAutomation"]);
   automation?.RequireOriginal(start);return automation;
  }
 }
}
