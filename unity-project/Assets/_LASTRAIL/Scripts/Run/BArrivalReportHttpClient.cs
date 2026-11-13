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
 // Immutable local original, not an accepted arrival. The run owner persists it before sending.
 public sealed class BArrivalReportRequest {
  public const string Version="combat.b-arrival-report.v2.20261020";
  public const int ByteLimit=1800000;
  readonly JObject body;
  public BRouteBinding Route{get;}
  public string RunId=>Route.RunId;
  public string RequestId=>(string)body["requestId"];
  public string BodyJson{get;}
  BArrivalReportRequest(JObject original,BRouteBinding route){body=(JObject)original.DeepClone();Route=route;BodyJson=body.ToString(Formatting.None);if(Encoding.UTF8.GetByteCount(BodyJson)>ByteLimit)throw new BResultReportHttpException(413,"combat_b_arrival_body_too_large",false);}
  public JObject Body()=>(JObject)body.DeepClone();
  public static BArrivalReportRequest FromExport(JObject export,BRouteBinding route){
   if(export==null||route==null)throw new ArgumentNullException();
   Need((string)export["provenance"]=="LOCAL_ORIGINAL_REQUIRES_INDEPENDENT_VERIFIER");
   var source=Obj(export["source"]);var checkpoint=Obj(export["checkpoint"]);var report=Obj(export["report"]);var binding=Obj(checkpoint["binding"]);var r=route.Capture();
   Need((string)r["phase"]=="TRAVELLING"&&(string)r["runStatus"]=="ACTIVE");
   foreach(var key in new[]{"accountId","runId","nativeContextId","departurePinHash","mapRevision"})Need(JToken.DeepEquals(binding[key],r[key]));
   Need(JToken.DeepEquals(binding["nodeId"],r["pendingNode"]?["nodeId"])&&JToken.DeepEquals(binding["edgeId"],r["pendingEdgeId"])&&JToken.DeepEquals(binding["origin"],r["originalNativeOrigin"]));
   var restored=BArrivalBoundaryTravel.Restore(source,(string)source["sourceHash"],binding,checkpoint,(string)export["checkpointHash"]);
   var id=SafeTransitionWire.Id(report,"requestId");Need(id.All(c=>c>=33&&c<=126));
   Need(JToken.DeepEquals(report,restored.CaptureReport(id)));
   return new BArrivalReportRequest(report,route);
  }
  internal static JObject Obj(JToken value)=>value as JObject??throw new ArgumentException("b_arrival_client_object_required");
  internal static void Need(bool condition){if(!condition)throw new ArgumentException("b_arrival_client_original_conflict");}
 }
 public sealed class BArrivalReportReceipt {
  readonly JObject original;readonly BArrivalReportRequest request;
  public string EvidenceRef=>(string)original["evidenceRef"];
  BArrivalReportReceipt(JObject value,BArrivalReportRequest request){original=(JObject)value.DeepClone();this.request=request;}
  public JObject Capture()=>(JObject)original.DeepClone();
  public BRouteCommand CreateArrivalCommand(string eventId)=>BRouteCommand.FromOriginal(request.Route,new JObject{["kind"]="ARRIVE",["eventId"]=eventId,["expectedRevision"]=request.Route.MapRevision,["edgeId"]=request.Route.Capture()["pendingEdgeId"].DeepClone(),["evidenceRef"]=EvidenceRef});
  public static BArrivalReportReceipt Read(string json,BArrivalReportRequest request){
   if(request==null)throw new ArgumentNullException(nameof(request));
   var p=SafeTransitionWire.Parse(json);BDirectAttackInput.Fields(p,"version","evidenceRef","requestId","arrivalAccepted","progressAuthorized","combatTruth","verificationLevel","observation","missingSources");
   void Need(bool ok)=>BArrivalReportRequest.Need(ok);
   Need((string)p["version"]==BArrivalReportRequest.Version&&(string)p["requestId"]==request.RequestId&&(string)p["combatTruth"]=="NOT_PROVEN"&&(string)p["verificationLevel"]=="ORIGINAL_PINNED_TRAVEL_COMMAND_REPLAY");
   SafeTransitionWire.Boolean(p,"arrivalAccepted",true);SafeTransitionWire.Boolean(p,"progressAuthorized",true);
   var reference=SafeTransitionWire.Id(p,"evidenceRef");Need(reference.StartsWith("B_ARRIVAL:",StringComparison.Ordinal)&&p["missingSources"] is JArray missing&&missing.Count==0);
   var o=BArrivalReportRequest.Obj(p["observation"]);BDirectAttackInput.Fields(o,"version","accountId","runId","nodeId","mapRevision","nativeContextId","departurePinHash","nodeSourceHash","operationBindingHash","evidenceRef","originalEventId","sequence","sourceRevision","checkpointHash","destination","arrival","travelStopped","combatTruth");
   var b=request.Body();var t=b["travel"];var r=request.Route.Capture();var a=t["arrival"];
   Need((string)o["version"]=="combat.b-boundary-arrival-observation.v1.20261020"&&(string)o["combatTruth"]=="NOT_PROVEN"&&(string)o["evidenceRef"]==reference);
   SafeTransitionWire.Boolean(o,"travelStopped",true);
   foreach(var key in new[]{"accountId","runId","nodeId","nativeContextId","nodeSourceHash"})Need(JToken.DeepEquals(o[key],t[key]));
   Need(JToken.DeepEquals(o["mapRevision"],b["reportOriginMapRevision"])&&JToken.DeepEquals(o["checkpointHash"],b["checkpointHash"])&&JToken.DeepEquals(o["departurePinHash"],r["departurePinHash"])&&JToken.DeepEquals(o["operationBindingHash"],r["bindingHash"]));
   Need(JToken.DeepEquals(o["originalEventId"],a["eventId"])&&JToken.DeepEquals(o["sequence"],a["commandSequence"])&&JToken.DeepEquals(o["sourceRevision"],a["commandSequence"]));
   var arrival=BArrivalReportRequest.Obj(o["arrival"]);BDirectAttackInput.Fields(arrival,"eventId","sequence","tick");
   Need(JToken.DeepEquals(arrival["eventId"],a["eventId"])&&JToken.DeepEquals(arrival["sequence"],a["commandSequence"])&&JToken.DeepEquals(arrival["tick"],a["tick"]));
   var destination=BArrivalReportRequest.Obj(o["destination"]);BDirectAttackInput.Fields(destination,"eventId","mapRevision","receiptHash","edgeId");
   Need(JToken.DeepEquals(destination["eventId"],b["destinationEventId"])&&JToken.DeepEquals(destination["mapRevision"],b["reportOriginMapRevision"])&&JToken.DeepEquals(destination["edgeId"],r["pendingEdgeId"]));
   var hash=(string)destination["receiptHash"];Need(hash!=null&&hash.Length==64&&hash.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f'));
   return new BArrivalReportReceipt(p,request);
  }
 }
 public sealed class BArrivalReportHttpClient {
  readonly IGameApiAuthenticatedHttpTransport transport;
  public BArrivalReportHttpClient(IGameApiAuthenticatedHttpTransport transport){this.transport=transport??throw new ArgumentNullException(nameof(transport));}
  static string Path(BArrivalReportRequest request)=>"/v1/runs/"+Uri.EscapeDataString(request.RunId)+"/b-arrival-reports";
  public async Task<BArrivalReportReceipt> SubmitAsync(BArrivalReportRequest request,GameApiAccessToken token,CancellationToken cancellation=default){
   if(request==null)throw new ArgumentNullException(nameof(request));
   var response=await BResultReportHttpClient.SendOriginalAsync(transport,new RunApiHttpRequest("POST",Path(request),request.BodyJson,new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader("X-B-Arrival-Version",BArrivalReportRequest.Version),new RunApiHttpHeader("Idempotency-Key",request.RequestId)}),token,cancellation);
   return BArrivalReportReceipt.Read(response.BodyJson,request);
  }
  public async Task<BArrivalReportReceipt> RecoverAsync(BArrivalReportRequest request,string evidenceRef,GameApiAccessToken token,CancellationToken cancellation=default){
   if(request==null||string.IsNullOrWhiteSpace(evidenceRef)||evidenceRef.Length>200||evidenceRef.Any(char.IsControl))throw new ArgumentException("b_arrival_recovery_reference_invalid");
   var response=await BResultReportHttpClient.SendOriginalAsync(transport,new RunApiHttpRequest("GET",Path(request)+"/"+Uri.EscapeDataString(evidenceRef),null,new[]{new RunApiHttpHeader("X-B-Arrival-Version",BArrivalReportRequest.Version)}),token,cancellation);
   var p=SafeTransitionWire.Parse(response.BodyJson,BArrivalReportRequest.ByteLimit+16384);BDirectAttackInput.Fields(p,"receipt","report");BArrivalReportRequest.Need(JToken.DeepEquals(p["report"],request.Body()));
   var receipt=BArrivalReportReceipt.Read(p["receipt"].ToString(),request);BArrivalReportRequest.Need(receipt.EvidenceRef==evidenceRef);return receipt;
  }
 }
}
