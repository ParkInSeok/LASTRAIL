using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Frozen original report; callers persist this request before sending or retrying.
 public sealed class BResultReportRequest {
  public const string Version="combat.b-result-report.v1.20261006";
  public const string V2Version="combat.b-result-report.v2.20261006";
  public const int ByteLimit=1800000,V2ByteLimit=6000000;
  readonly JObject body;
  public string AccountId{get;}public string RunId{get;}public string RequestId{get;}public string OriginalDeliveryKey{get;}public string BodyJson{get;}
  public string ReportVersion=>(string)body["version"];public int RequestByteLimit=>ReportVersion==V2Version?V2ByteLimit:ByteLimit;
  public BResultReportRequest(BRouteRuntimeObservation original,string requestId){
   if(original==null)throw new ArgumentNullException(nameof(original));original.RequireOriginalCommand();
   if(string.IsNullOrWhiteSpace(requestId)||requestId.Length>200||requestId.Any(c=>c<33||c>126))throw new ArgumentException("b_result_request_id_invalid");
   var route=original.OriginalRoute;AccountId=(string)route["accountId"];RunId=(string)route["runId"];RequestId=requestId;OriginalDeliveryKey=original.DeliveryKey;
   var nativeVersion=(string)original.OriginalCheckpoint["version"];if(nativeVersion!=BDirectAttackBasicLoop.TravelVersion&&nativeVersion!=BDirectAttackBasicLoop.TravelEventVersion)throw new ArgumentException("b_result_native_version_unsupported");
   body=new JObject{["version"]=nativeVersion==BDirectAttackBasicLoop.TravelEventVersion?V2Version:Version,["requestId"]=requestId,["reportOriginMapRevision"]=route["mapRevision"].DeepClone(),["checkpointHash"]=original.CheckpointHash,["nodeSession"]=original.OriginalNodeSession};
   // Preserve native v1/v2 verbatim. Server support determines acceptance.
   BodyJson=body.ToString(Formatting.None);
   if(Encoding.UTF8.GetByteCount(BodyJson)>RequestByteLimit)throw new BResultReportHttpException(413,"combat_b_result_body_too_large",false);
   if(ReportVersion==V2Version&&Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(body))>V2ByteLimit)throw new BResultReportHttpException(413,"combat_b_result_body_too_large",false);
  }
  public JObject Body()=>(JObject)body.DeepClone();
  public JObject Capture()=>new JObject{["accountId"]=AccountId,["runId"]=RunId,["originalDeliveryKey"]=OriginalDeliveryKey,["body"]=Body()};
  public static BResultReportRequest Restore(JObject saved,string independentHash,BRouteRuntimeObservation original){
   if(saved==null||BDirectAttackInput.Digest(saved)!=independentHash)throw new ArgumentException("b_result_request_restore_hash");
   var request=new BResultReportRequest(original,(string)saved["body"]?["requestId"]);
   if(!JToken.DeepEquals(saved,request.Capture()))throw new ArgumentException("b_result_request_restore_conflict");return request;
  }
 }
 public sealed class BResultReportHttpException:Exception {
  public int StatusCode{get;}public string ErrorCode{get;}public bool CanRetrySameRequest{get;}public bool RequiresMapRefresh=>StatusCode==409;public bool RequiresAuthentication=>StatusCode==401;
  public BResultReportHttpException(int status,string code,bool retry):base("b_result_http:"+status+":"+code){StatusCode=status;ErrorCode=code;CanRetrySameRequest=retry;}
 }
 public sealed class BResultConsistencyReceipt {
  public const string Version="combat.b-result-consistency-receipt.v1.20261006";
  public const string V2Version="combat.b-result-consistency-receipt.v2.20261006";
  readonly JObject original;
  public string EvidenceRef=>(string)original["evidenceRef"];public bool ProgressAuthorized=>(bool)original["progressAuthorized"];public JObject Capture()=>(JObject)original.DeepClone();
  BResultConsistencyReceipt(JObject value){original=(JObject)value.DeepClone();}
  public static BResultConsistencyReceipt Read(string json,BResultReportRequest request){
   var p=SafeTransitionWire.Parse(json);var fields=new[]{"version","evidenceRef","requestId","reportOriginMapRevision","originalEventId","commandRevision","nativeSequence","checkpointHash","completedWaves","verificationLevel","combatTruth","fullNativeReplayPerformed","arrivalAccepted","progressAuthorized","missingSources","rewardsAuthorized"};bool v2=request.ReportVersion==BResultReportRequest.V2Version;BDirectAttackInput.Fields(p,v2?fields.Concat(new[]{"originalJournal","finalCommandId"}).ToArray():fields);
   var body=request.Body();var native=body["nodeSession"]["runtime"];var commands=(JArray)native["commands"];
   BDirectAttackInput.Nat(p,"commandRevision",1,10000);BDirectAttackInput.Nat(p,"completedWaves",0,2);BDirectAttackInput.Nat(p,"nativeSequence",0);BDirectAttackInput.Nat(p,"reportOriginMapRevision",1);
   var terminalCommand=v2?(string)native["originalEvents"]?["records"]?.Last?["binding"]?["originalCommand"]?["eventId"]:(string)commands.Last["eventId"];
   if(v2){BResultJournalReceipt.RequireMatches(p["originalJournal"] as JObject,native["originalEvents"] as JObject);if((string)p["finalCommandId"]!=(string)commands.Last["eventId"])throw new ArgumentException("b_result_final_command_conflict");}
   if((string)p["version"]!=(v2?V2Version:Version)||(string)p["requestId"]!=request.RequestId||!JToken.DeepEquals(p["reportOriginMapRevision"],body["reportOriginMapRevision"])||(string)p["checkpointHash"]!=(string)body["checkpointHash"]||(string)p["originalEventId"]!=terminalCommand||(long?)p["commandRevision"]!=commands.Count||!JToken.DeepEquals(p["nativeSequence"],native["sequence"])||(long?)p["completedWaves"]!=((JArray)native["waveEntry"]["events"]).Count(x=>(string)x["kind"]=="WAVE_CLEAR")||(string)p["verificationLevel"]!="STRUCTURE_SOURCE_RESULT_CONSISTENCY"||(string)p["combatTruth"]!="NOT_PROVEN")throw new ArgumentException("b_result_receipt_original_conflict");
   foreach(string field in new[]{"fullNativeReplayPerformed","arrivalAccepted","rewardsAuthorized"})if(p[field]?.Type!=JTokenType.Boolean||(bool)p[field])throw new ArgumentException("b_result_receipt_authority_conflict");
   var reference=(string)p["evidenceRef"];if(p["evidenceRef"]?.Type!=JTokenType.String||string.IsNullOrEmpty(reference)||reference.Length>200||reference.Any(char.IsControl)||p["progressAuthorized"]?.Type!=JTokenType.Boolean||!(p["missingSources"] is JArray missing)||missing.Any(x=>x.Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)x))||(bool)p["progressAuthorized"]&&missing.Count>0)throw new ArgumentException("b_result_receipt_schema_conflict");
   return new BResultConsistencyReceipt(p);
  }
 }
 public sealed class BResultReportHttpClient {
  readonly IGameApiAuthenticatedHttpTransport transport;
  public BResultReportHttpClient(IGameApiAuthenticatedHttpTransport original){transport=original??throw new ArgumentNullException(nameof(original));}
  static string Path(BResultReportRequest request)=>"/v1/runs/"+Uri.EscapeDataString(request.RunId)+"/b-result-reports";
  public async Task<BResultConsistencyReceipt> SubmitAsync(BResultReportRequest request,GameApiAccessToken token,CancellationToken cancellation=default){
   if(request==null)throw new ArgumentNullException(nameof(request));var response=await Send(new RunApiHttpRequest("POST",Path(request),request.BodyJson,new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader("X-B-Result-Version",request.ReportVersion),new RunApiHttpHeader("Idempotency-Key",request.RequestId)}),token,cancellation);return BResultConsistencyReceipt.Read(response.BodyJson,request);
  }
  public async Task<BResultConsistencyReceipt> RecoverAsync(BResultReportRequest request,string evidenceRef,GameApiAccessToken token,CancellationToken cancellation=default){
   if(request==null||string.IsNullOrEmpty(evidenceRef)||evidenceRef.Length>200||evidenceRef.Any(char.IsControl))throw new ArgumentException("b_result_recovery_reference_invalid");
   var response=await Send(new RunApiHttpRequest("GET",Path(request)+"/"+Uri.EscapeDataString(evidenceRef),null,new[]{new RunApiHttpHeader("X-B-Result-Version",request.ReportVersion)}),token,cancellation);
   var p=SafeTransitionWire.Parse(response.BodyJson,request.RequestByteLimit+16384);BDirectAttackInput.Fields(p,"receipt","report");if(!JToken.DeepEquals(p["report"],request.Body()))throw new ArgumentException("b_result_recovery_report_conflict");var receipt=BResultConsistencyReceipt.Read(p["receipt"].ToString(),request);if(receipt.EvidenceRef!=evidenceRef)throw new ArgumentException("b_result_recovery_reference_conflict");return receipt;
  }
  Task<RunApiHttpResponse> Send(RunApiHttpRequest request,GameApiAccessToken token,CancellationToken cancellation)=>SendOriginalAsync(transport,request,token,cancellation);
  internal static async Task<RunApiHttpResponse> SendOriginalAsync(IGameApiAuthenticatedHttpTransport transport,RunApiHttpRequest request,GameApiAccessToken token,CancellationToken cancellation){
   cancellation.ThrowIfCancellationRequested();var result=await transport.SendAsync(request,token);cancellation.ThrowIfCancellationRequested();
   // Borrowed transport has no per-request cancellation contract. Remote commit may survive local cancellation.
   if(result.Outcome==GameApiAuthenticatedHttpOutcome.NetworkError)throw new BResultReportHttpException(0,"network_error",true);
   if(result.Outcome==GameApiAuthenticatedHttpOutcome.AuthRequired)throw new BResultReportHttpException(401,result.ErrorCode,false);
   if(result.Response.StatusCode!=200)throw new BResultReportHttpException(result.Response.StatusCode,RunApiErrorJsonAdapter.OptionalString(result.Response.BodyJson,"errorCode"),false);return result.Response;
  }
 }
}
