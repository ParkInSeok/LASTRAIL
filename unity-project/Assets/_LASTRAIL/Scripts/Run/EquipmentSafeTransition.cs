using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Run {
 // Immutable diagnostic lineage only. No native state or equipment mutation.
 public sealed class EquipmentSafeTransitionContext {
  public const string Version="equipment.safe-transition-verification.v1.20261005";
  readonly JObject original;
  public string AccountId=>(string)original["lineage"]["accountId"];
  public string RunId=>(string)original["lineage"]["runId"];
  public string Hash=>(string)original["contextHash"];
  public bool CanAuthorizeMaintenance=>false;
  EquipmentSafeTransitionContext(JObject p){original=(JObject)p.DeepClone();}
  public JObject Capture()=>(JObject)original.DeepClone();
  public static EquipmentSafeTransitionContext Read(string json,string hash,string account,string run){
   var p=SafeTransitionWire.Parse(json);BDirectAttackInput.Fields(p,"version","contextHash","authority","phase","mapEventId","journalMode","lineage","accepted","runtimeSupported","authorization","runtimeEvidence");
   SafeTransitionWire.Common(p,account,run);SafeTransitionWire.Pin(p,"contextHash",hash);
   if(!new[]{"READY","TRAVELLING","ACTIVE","FINAL_NODE_COMPLETE"}.Contains((string)p["phase"]))throw new ArgumentException("safe_transition_phase_invalid");
   if(p["mapEventId"].Type!=JTokenType.Null)SafeTransitionWire.Id(p,"mapEventId");
   if(p["journalMode"].Type!=JTokenType.Null&&!new[]{"SAFE","CLOSED"}.Contains((string)p["journalMode"]))throw new ArgumentException("safe_transition_journal_invalid");
   return new EquipmentSafeTransitionContext(p);
  }
  public EquipmentSafeTransitionIntent CreateIntent(string commandId,string kind,string reason=null){
   if(original["mapEventId"].Type==JTokenType.Null)throw new InvalidOperationException("safe_transition_map_receipt_missing");
   if(!Regex.IsMatch(commandId??"","^[\\x21-\\x7e]{1,128}$")||kind!="ENTER"&&kind!="EXIT"||kind=="ENTER"&&!new[]{"BETWEEN_NODES","SAFE_EVENT"}.Contains(reason)||kind=="EXIT"&&reason!=null)throw new ArgumentException("safe_transition_intent_invalid");
   var l=original["lineage"];var body=new JObject{["version"]=Version,["commandId"]=commandId,["kind"]=kind,["nodeId"]=l["nodeId"].DeepClone(),["expectedMapRevision"]=l["mapRevision"].DeepClone(),["expectedJournalRevision"]=l["journalRevision"].DeepClone(),["expectedHoldIds"]=l["holdIds"].DeepClone(),["mapEventId"]=original["mapEventId"].DeepClone(),["expectedAdmissionHash"]=l["admissionHash"].DeepClone(),["expectedSnapshotHash"]=l["snapshotHash"].DeepClone()};if(kind=="ENTER")body["reason"]=reason;
   return new EquipmentSafeTransitionIntent(this,body);
  }
 }
 public sealed class EquipmentSafeTransitionIntent {
  readonly JObject body;public EquipmentSafeTransitionContext Context{get;}
  public string CommandId=>(string)body["commandId"];public string RequestHash=>SafeTransitionWire.Digest(body);
  internal EquipmentSafeTransitionIntent(EquipmentSafeTransitionContext context,JObject request){Context=context;body=(JObject)request.DeepClone();}
  public JObject Capture()=>(JObject)body.DeepClone();
 }
 public sealed class EquipmentSafeTransitionReceipt {
  readonly JObject original;public string Hash=>(string)original["transitionHash"];public bool CanAuthorizeMaintenance=>false;
  EquipmentSafeTransitionReceipt(JObject p){original=(JObject)p.DeepClone();}
  public JObject Capture()=>(JObject)original.DeepClone();
  public static EquipmentSafeTransitionReceipt Read(string json,string hash,EquipmentSafeTransitionIntent intent){
   if(intent==null)throw new ArgumentNullException(nameof(intent));var p=SafeTransitionWire.Parse(json);var c=intent.Capture();var context=intent.Context.Capture();
   BDirectAttackInput.Fields(p,"version","commandId","kind","reason","requestHash","accepted","serverTransitionVerified","runtimeSupported","authorization","journalChanged","clockChanged","authority","lineage","originalMapReceipt","runtimeEvidence","missingRuntime","clientObservationVerification","transitionHash");
   SafeTransitionWire.Common(p,intent.Context.AccountId,intent.Context.RunId);SafeTransitionWire.Pin(p,"transitionHash",hash);
   if((string)p["commandId"]!=intent.CommandId||(string)p["kind"]!=(string)c["kind"]||!JToken.DeepEquals(p["reason"],c["reason"]??JValue.CreateNull())||(string)p["requestHash"]!=intent.RequestHash||!JToken.DeepEquals(p["lineage"],context["lineage"])||!JToken.DeepEquals(p["authority"],context["authority"]))throw new ArgumentException("safe_transition_request_lineage_conflict");
   SafeTransitionWire.Boolean(p,"serverTransitionVerified",true);SafeTransitionWire.Boolean(p,"journalChanged",false);SafeTransitionWire.Boolean(p,"clockChanged",false);
   var missing=(string)c["reason"]=="SAFE_EVENT"?new JArray("ORIGINAL_COMBAT_TRAVEL_ALL_HAZARDS","ORIGINAL_ACCEPTED_SAFE_EVENT"):new JArray("ORIGINAL_COMBAT_TRAVEL_ALL_HAZARDS");
   if(!JToken.DeepEquals(p["missingRuntime"],missing)||(string)p["clientObservationVerification"]!="NOT_PERFORMED")throw new ArgumentException("safe_transition_runtime_claim_invalid");
   var mr=p["originalMapReceipt"] as JObject??throw new ArgumentException("safe_transition_receipt_invalid");BDirectAttackInput.Fields(mr,"version","eventId","revision","state","verificationId");
   var state=mr["state"] as JObject??throw new ArgumentException("safe_transition_state_invalid");BDirectAttackInput.Fields(state,"version","traversedEdgeIds","pendingEdgeId","phase","holdIds");
   if((string)mr["version"]!="operation.map-receipt.v1"||(string)state["version"]!="operation.map-progress.v1"||!JToken.DeepEquals(mr["eventId"],c["mapEventId"])||!JToken.DeepEquals(mr["revision"],c["expectedMapRevision"])||!JToken.DeepEquals(state["holdIds"],c["expectedHoldIds"])||(string)state["phase"]!=(string)context["phase"])throw new ArgumentException("safe_transition_map_receipt_conflict");
   SafeTransitionWire.Id(mr,"verificationId");SafeTransitionWire.Ids(state["traversedEdgeIds"]);if(state["pendingEdgeId"].Type!=JTokenType.Null)SafeTransitionWire.Id(state,"pendingEdgeId");
   return new EquipmentSafeTransitionReceipt(p);
  }
 }
 internal static class SafeTransitionWire {
  internal static string Digest(JToken p)=>BDirectAttackInput.Digest(p).Substring(7);
  sealed class StrictReader:JsonTextReader {internal StrictReader(TextReader r):base(r){}public override bool Read(){var result=base.Read();if(TokenType==JsonToken.Comment)throw new ArgumentException("safe_transition_comment_invalid");return result;}}
  internal static JObject Parse(string json,int maximumBytes=256*1024){if(maximumBytes<1||json==null||Encoding.UTF8.GetByteCount(json)>maximumBytes)throw new ArgumentException("safe_transition_capacity");using(var r=new StrictReader(new StringReader(json))){r.DateParseHandling=DateParseHandling.None;var p=JObject.Load(r,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});if(r.Read())throw new ArgumentException("safe_transition_trailing_invalid");return p;}}
  internal static string Id(JToken p,string key){var s=BDirectAttackInput.Text(p,key);if(s.Length>200||s.Length<1)throw new ArgumentException("safe_transition_id_invalid");return s;}
  internal static void Ids(JToken p){var a=p as JArray;if(a==null||a.Count>1000||a.Any(v=>v.Type!=JTokenType.String||((string)v).Length<1||((string)v).Length>200)||a.Select(v=>(string)v).Distinct().Count()!=a.Count)throw new ArgumentException("safe_transition_ids_invalid");}
  internal static void Boolean(JObject p,string key,bool expected){if(p[key]?.Type!=JTokenType.Boolean||(bool)p[key]!=expected)throw new ArgumentException("safe_transition_boolean_invalid");}
  internal static void Pin(JObject p,string key,string independent){var body=(JObject)p.DeepClone();body.Remove(key);if(!Regex.IsMatch(independent??"","^[a-f0-9]{64}$")||(string)p[key]!=independent||Digest(body)!=independent)throw new ArgumentException("safe_transition_hash_conflict");}
  internal static void Common(JObject p,string account,string run){
   if((string)p["version"]!=EquipmentSafeTransitionContext.Version||(string)p["authorization"]!="NOT_GRANTED_RUNTIME_UNSUPPORTED"||p["runtimeEvidence"]?.Type!=JTokenType.Null)throw new ArgumentException("safe_transition_contract_invalid");Boolean(p,"accepted",false);Boolean(p,"runtimeSupported",false);
   var l=p["lineage"] as JObject??throw new ArgumentException("safe_transition_lineage_invalid");BDirectAttackInput.Fields(l,"accountId","runId","nodeId","mapRevision","journalRevision","holdIds","admissionHash","snapshotHash","mapPlanHash");if(Id(l,"accountId")!=account||Id(l,"runId")!=run)throw new ArgumentException("safe_transition_identity_conflict");Id(l,"nodeId");BDirectAttackInput.Nat(l,"mapRevision",1);BDirectAttackInput.Nat(l,"journalRevision");Ids(l["holdIds"]);foreach(var k in new[]{"admissionHash","snapshotHash","mapPlanHash"})if(!Regex.IsMatch((string)l[k]??"","^[a-f0-9]{64}$"))throw new ArgumentException("safe_transition_source_hash_invalid");
   var a=p["authority"] as JObject??throw new ArgumentException("safe_transition_authority_invalid");BDirectAttackInput.Fields(a,"version","mode","combatOwner","combatClock","transactionOwner","resultVerificationOwner","clientReportProvesCombat");bool solo=(string)a["mode"]=="ORDINARY_SOLO";if((string)a["version"]!="combat.authority-split.v1"||!solo&&(string)a["mode"]!="ORDINARY_COOP"||(string)a["combatOwner"]!=(solo?"UNITY_LOCAL":"UNITY_HOST")||(string)a["combatClock"]!=(solo?"LOCAL_LOGICAL":"HOST_LOGICAL")||(string)a["transactionOwner"]!="SERVER"||(string)a["resultVerificationOwner"]!="SERVER")throw new ArgumentException("safe_transition_authority_invalid");Boolean(a,"clientReportProvesCombat",false);
  }
 }
 public sealed class EquipmentSafeTransitionHttpClient {
  readonly IGameApiAuthenticatedHttpTransport transport;public EquipmentSafeTransitionHttpClient(IGameApiAuthenticatedHttpTransport value){transport=value??throw new ArgumentNullException(nameof(value));}
  static string Path(string run){if(string.IsNullOrEmpty(run)||run.Length>200)throw new ArgumentException("safe_transition_run_invalid");return "/v1/runs/"+Uri.EscapeDataString(run)+"/equipment-safe-transition";}
  async Task<(string json,string hash)> Send(RunApiHttpRequest request,GameApiAccessToken token){var r=await transport.SendAsync(request,token);if(r.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded)throw new InvalidOperationException("safe_transition_http:"+r.Outcome+":"+r.ErrorCode);if(r.Response.StatusCode!=200)throw new InvalidOperationException("safe_transition_http_status:"+r.Response.StatusCode+":"+RunApiErrorJsonAdapter.OptionalString(r.Response.BodyJson,"errorCode"));return(r.Response.BodyJson,r.Response.Header("ETag")?.Trim('"'));}
  public async Task<EquipmentSafeTransitionContext> CaptureAsync(string account,string run,GameApiAccessToken token){var r=await Send(new RunApiHttpRequest("GET",Path(run),null,null),token);return EquipmentSafeTransitionContext.Read(r.json,r.hash,account,run);}
  public async Task<EquipmentSafeTransitionReceipt> VerifyAsync(EquipmentSafeTransitionIntent intent,GameApiAccessToken token){if(intent==null)throw new ArgumentNullException(nameof(intent));var body=intent.Capture().ToString(Formatting.None);if(Encoding.UTF8.GetByteCount(body)>18000)throw new ArgumentException("safe_transition_request_capacity");var r=await Send(new RunApiHttpRequest("POST",Path(intent.Context.RunId)+"/verify",body,new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader("X-Equipment-Safe-Transition-Version",EquipmentSafeTransitionContext.Version),new RunApiHttpHeader("Idempotency-Key",intent.CommandId)}),token);return EquipmentSafeTransitionReceipt.Read(r.json,r.hash,intent);}
 }
 // Owns publication generation and binding only; transport is borrowed.
 public sealed class EquipmentSafeTransitionOwner:IDisposable {
  string account,run;long generation;bool disposed;Action<EquipmentSafeTransitionReceipt> changed;
  public EquipmentSafeTransitionReceipt Current{get;private set;}
  public void Bind(string accountId,string runId,Action<EquipmentSafeTransitionReceipt> callback){Alive();if(string.IsNullOrEmpty(accountId)||string.IsNullOrEmpty(runId)||callback==null)throw new ArgumentException("safe_transition_binding_invalid");account=accountId;run=runId;changed=callback;Current=null;generation=checked(generation+1);}
  public void Unbind(){Alive();changed=null;Current=null;generation=checked(generation+1);}
  public async Task<bool> PublishAsync(EquipmentSafeTransitionIntent intent,Func<Task<EquipmentSafeTransitionReceipt>> verify){Alive();if(changed==null||intent==null||verify==null||intent.Context.AccountId!=account||intent.Context.RunId!=run)throw new ArgumentException("safe_transition_binding_conflict");long ticket=generation=checked(generation+1);var receipt=await verify();if(disposed||changed==null||ticket!=generation)return false;if(receipt==null||(string)receipt.Capture()["requestHash"]!=intent.RequestHash||(string)receipt.Capture()["lineage"]["accountId"]!=account||(string)receipt.Capture()["lineage"]["runId"]!=run)throw new ArgumentException("safe_transition_publication_conflict");Current=receipt;changed(receipt);return true;}
  void Alive(){if(disposed)throw new ObjectDisposedException(nameof(EquipmentSafeTransitionOwner));}
  public void Dispose(){if(disposed)return;disposed=true;changed=null;Current=null;generation=checked(generation+1);}
 }
}
