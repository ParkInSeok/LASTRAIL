using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Main-thread command intent owner. Caller supplies trusted server responses; no transport/HP execution here.
 public sealed class WorldBossRailController:IDisposable {
  WorldBossRailSnapshot state;readonly string account;JObject pending;string previewRound,previewSide;bool detached,needsRefresh;
  public WorldBossRailSnapshot State=>state;
  public JObject Pending=>pending==null?null:(JObject)pending.DeepClone();
  public bool AwaitingAuthorityRefresh=>needsRefresh;
  public event Action<JObject> CommandRequested;
  public event Action Changed;
  public WorldBossRailController(WorldBossRailSnapshot initial,string ownerAccount){state=initial??throw new ArgumentNullException(nameof(initial));var d=state.Definition;if(ownerAccount!=(string)d["trainAccountId"]&&ownerAccount!=(string)d["operatorAccountId"])throw new ArgumentException("Unknown role account");account=ownerAccount;}
  void Live(){if(detached)throw new ObjectDisposedException(nameof(WorldBossRailController));}
  JToken Round(string id)=>((JArray)state.Capture()["rounds"]).SingleOrDefault(r=>(string)r["roundId"]==id)??throw new ArgumentException("Unknown round");
  JToken Config(string id)=>((JArray)state.Definition["rounds"]).Single(r=>(string)r["roundId"]==id);
  public void PreviewRoute(string roundId,string side){Live();if(side!="LEFT"&&side!="RIGHT")throw new ArgumentException("Only two routes");var r=Round(roundId);if((string)r["status"]!="OPEN"||(bool)r["confirmed"])throw new InvalidOperationException("Route not previewable");previewRound=roundId;previewSide=side;Changed?.Invoke();}
  public JObject ConfirmPreview(string commandId){Live();if(previewSide==null)throw new InvalidOperationException("Preview required");return Begin(previewRound,commandId,new JObject{["kind"]="CONFIRM_ROUTE",["side"]=previewSide});}
  public JObject Internal(string roundId,string actionId,string commandId)=>Begin(roundId,commandId,new JObject{["kind"]="INTERNAL",["actionId"]=actionId});
  JObject Begin(string roundId,string key,JObject action){
   Live();if(needsRefresh||pending!=null)throw new InvalidOperationException("Reconcile or refresh original command first");
   var d=state.Definition;var r=Round(roundId);var c=Config(roundId);bool train=(string)action["kind"]=="CONFIRM_ROUTE";
   if(account!=(string)d[train?"trainAccountId":"operatorAccountId"])throw new InvalidOperationException("rail_role_forbidden");
   long elapsed=state.Now-(long)d["startedAt"];if(elapsed>=180000||(string)r["status"]!="OPEN"||elapsed<(long)c[train?"previewAt":"internalOpenAt"]||elapsed>=(long)c[train?"lockAt":"internalCloseAt"]||train&&(bool)r["confirmed"]||!train&&((long)c["phase"]==1||r["internalAction"].Type!=JTokenType.Null))throw new InvalidOperationException("rail_window_closed");
   var body=new JObject{["version"]=WorldBossRailSnapshot.Version,["attemptId"]=state.AttemptId,["contentVersion"]=d["contentVersion"].DeepClone(),["roundId"]=roundId,["commandId"]=key,["expectedRevision"]=state.Revision,["action"]=action};
   WorldBossRailSnapshot.ValidateCommand(body,state.AttemptId,(string)d["contentVersion"]);pending=(JObject)body.DeepClone();CommandRequested?.Invoke(Pending);if(!detached)Changed?.Invoke();return (JObject)body.DeepClone();
  }
  public JObject OriginalForReconciliation(){Live();if(pending==null)throw new InvalidOperationException("No pending original");return Pending;}
  public void ApplyAuthoritativeSnapshot(JObject snapshot)=>Apply(snapshot,true);
  void Apply(JObject snapshot,bool notify){
   Live();var candidate=new WorldBossRailSnapshot(snapshot,state.Definition,state.AttemptId);if(candidate.Revision<state.Revision||candidate.Now<state.Now)throw new ArgumentException("Stale authority snapshot");
   var old=(JArray)state.Capture()["receipts"];var records=(JArray)candidate.Capture()["receipts"];foreach(var rec in old)if(!records.Any(r=>JToken.DeepEquals(r,rec)))throw new ArgumentException("Original receipts changed");
   bool resolved=false;if(pending!=null){var rec=records.SingleOrDefault(r=>(string)r["accountId"]==account&&(string)r["commandId"]==(string)pending["commandId"]);if(rec!=null){if((string)rec["digest"]!=WorldBossRailSnapshot.Canonical(pending))throw new ArgumentException("Original body changed");WorldBossRailSnapshot.ValidateReceipt(rec["receipt"] as JObject,pending);resolved=true;}}
   state=candidate;needsRefresh=false;if(resolved){pending=null;previewRound=previewSide=null;}if(notify)Changed?.Invoke();
  }
  // Only a correlated definitive rejection from the trusted authority port can release the original.
  // Timeouts/unknown errors are not rejected commands and leave Pending untouched.
  public void RejectOriginal(JObject originalBody,string definitiveError,JObject authoritativeSnapshot){
   Live();if(pending==null||WorldBossRailSnapshot.Canonical(originalBody)!=WorldBossRailSnapshot.Canonical(pending))throw new ArgumentException("Rejection does not match original");
   if(!new[]{"rail_revision_conflict","rail_window_closed","rail_round_not_open","rail_attempt_ended","rail_actor_unavailable","rail_role_forbidden","rail_definition_conflict","rail_route_already_confirmed","rail_internal_already_accepted","rail_command_key_conflict"}.Contains(definitiveError))throw new ArgumentException("Not a definitive rail rejection");
   var records=authoritativeSnapshot?["receipts"] as JArray;if(records==null||records.Any(r=>(string)r["accountId"]==account&&(string)r["commandId"]==(string)pending["commandId"]))throw new ArgumentException("Resolve stored original receipt first");
   Apply(authoritativeSnapshot,false);pending=null;previewRound=previewSide=null;Changed?.Invoke();
  }
  public void AcceptOriginalReceipt(JObject receipt,JObject snapshot){
   Live();if(pending==null)throw new InvalidOperationException("No pending original");WorldBossRailSnapshot.ValidateReceipt(receipt,pending);
   var records=snapshot?["receipts"] as JArray;if(records==null||!records.Any(r=>(string)r["accountId"]==account&&(string)r["digest"]==WorldBossRailSnapshot.Canonical(pending)&&JToken.DeepEquals(r["receipt"],receipt)))throw new ArgumentException("Receipt not in original server snapshot");ApplyAuthoritativeSnapshot(snapshot);
  }
  public string CaptureLocal(){Live();return new JObject{["schema"]="world-boss.rail-client.v1",["accountId"]=account,["snapshot"]=state.Capture(),["pending"]=pending==null?JValue.CreateNull():(JToken)Pending,["previewRound"]=previewRound,["previewSide"]=previewSide}.ToString(Newtonsoft.Json.Formatting.None);}
  // A local replica is never admission proof. New intents require authority refresh; original reconciliation remains possible.
  public static WorldBossRailController RestoreLocal(string json,JObject trustedDefinition,string expectedAttempt,string expectedAccount){
   if(json==null||json.Length>2097152)throw new ArgumentException("Client checkpoint limit");var x=JObject.Parse(json);WorldBossRailSnapshot.Fields(x,"schema","accountId","snapshot","pending","previewRound","previewSide");WorldBossRailSnapshot.Need((string)x["schema"]=="world-boss.rail-client.v1"&&(string)x["accountId"]==expectedAccount);var c=new WorldBossRailController(new WorldBossRailSnapshot(x["snapshot"] as JObject,trustedDefinition,expectedAttempt),expectedAccount);
   if(x["pending"].Type!=JTokenType.Null){var p=x["pending"] as JObject;WorldBossRailSnapshot.ValidateCommand(p,expectedAttempt,(string)trustedDefinition["contentVersion"]);WorldBossRailSnapshot.Need((long)p["expectedRevision"]<=c.state.Revision&&expectedAccount==(string)trustedDefinition[(string)p["action"]["kind"]=="CONFIRM_ROUTE"?"trainAccountId":"operatorAccountId"]);c.Round((string)p["roundId"]);c.pending=(JObject)p.DeepClone();}
   if(x["previewSide"].Type!=JTokenType.Null){WorldBossRailSnapshot.Need(x["previewSide"].Type==JTokenType.String&&x["previewRound"].Type==JTokenType.String);c.previewSide=(string)x["previewSide"];c.previewRound=(string)x["previewRound"];WorldBossRailSnapshot.Need(c.previewSide=="LEFT"||c.previewSide=="RIGHT");c.Round(c.previewRound);}else WorldBossRailSnapshot.Need(x["previewRound"].Type==JTokenType.Null);
   c.needsRefresh=true;return c;
  }
  public void Dispose(){if(detached)return;detached=true;CommandRequested=null;Changed=null;}
 }
}