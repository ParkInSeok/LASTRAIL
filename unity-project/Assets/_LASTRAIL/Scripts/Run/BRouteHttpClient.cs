using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 public sealed class BRouteCommand {
  readonly JObject command;public BRouteBinding Binding{get;}public bool IsWave{get;}public string EventId=>(string)command["eventId"];public string Kind=>IsWave?"WAVE_COMPLETE":(string)command["kind"];
  BRouteCommand(BRouteBinding binding,JObject c,bool wave){Binding=binding;command=(JObject)c.DeepClone();IsWave=wave;}
  public JObject Capture()=>new JObject{["version"]=BRouteBinding.Version,["command"]=command.DeepClone()};
  public static BRouteCommand FromOriginal(BRouteBinding binding,JObject command,bool wave=false){
   if(binding==null||command==null)throw new ArgumentNullException();var c=(JObject)command.DeepClone();SafeTransitionWire.Id(c,"eventId");SafeTransitionWire.Id(c,"evidenceRef");var b=binding.Capture();
   if(wave){BDirectAttackInput.Fields(c,"eventId","nodeId","waveOrdinal","expectedMapRevision","evidenceRef");BDirectAttackInput.Nat(c,"waveOrdinal",1,128);if(BDirectAttackInput.Nat(c,"expectedMapRevision",1)!=binding.MapRevision||!JToken.DeepEquals(c["nodeId"],(b["currentNode"] as JObject)?["nodeId"]))throw new ArgumentException("b_route_wave_context_conflict");}
   else{string kind=SafeTransitionWire.Id(c,"kind");string field=kind=="ARRIVE"?"edgeId":kind=="COMPLETE"?"nodeId":kind=="RELEASE_HOLD"?"holdId":throw new ArgumentException("b_route_use_original_destination");BDirectAttackInput.Fields(c,"kind","eventId","expectedRevision",field,"evidenceRef");SafeTransitionWire.Id(c,field);if(BDirectAttackInput.Nat(c,"expectedRevision",1)!=binding.MapRevision)throw new ArgumentException("b_route_command_revision_conflict");if(kind=="ARRIVE"&&!JToken.DeepEquals(c[field],b["pendingEdgeId"])||kind=="COMPLETE"&&!JToken.DeepEquals(c[field],(b["currentNode"] as JObject)?["nodeId"])||kind=="RELEASE_HOLD"&&!((JArray)b["holdIds"]).Any(h=>JToken.DeepEquals(h,c[field])))throw new ArgumentException("b_route_command_target_conflict");}
   return new BRouteCommand(binding,c,wave);
  }
 }
 public sealed class BRouteCommandResult {
  readonly JObject original;public int StatusCode{get;}public bool Committed=>(bool?)original["committed"]==true;public bool CanAuthorizeEquipment=>false;
  public bool AugmentPending=>(bool?)original["augmentPending"]==true;
  public BAugmentArrivalOriginal Augment{get;}
  public FirstPlayableScaling Scaling{get;}
  public OrdinaryNodeStart NodeStart{get;}
  public OrdinaryRunAutomation OrdinaryAutomation{get;}
  public long RequiredMapRevision=>Math.Max((long?)original["receipt"]?["revision"]??0,Augment?.CompletionRevision??0);
  BRouteCommandResult(int status,JObject p,BAugmentArrivalOriginal augment=null,FirstPlayableScaling scaling=null,BRouteCommand request=null){StatusCode=status;original=(JObject)p.DeepClone();Augment=augment;Scaling=scaling;if(original["receipt"] is JObject receipt){if(receipt.Property("nodeStart")!=null)NodeStart=OrdinaryNodeStart.ReadArrival(receipt["nodeStart"] as JObject,request,receipt,scaling);if(receipt.Property("ordinaryAutomation")!=null){if(request==null||request.Kind!="COMPLETE"||request.IsWave)throw new ArgumentException("ordinary_automation_completion_required");OrdinaryAutomation=OrdinaryRunAutomation.Read(receipt["ordinaryAutomation"]);}}}
  public JObject Capture()=>(JObject)original.DeepClone();
  public static BRouteCommandResult Read(int status,string json,BRouteCommand request){
   if(request==null)throw new ArgumentNullException(nameof(request));var p=SafeTransitionWire.Parse(json);
   if(status==409&&(string)p["errorCode"]=="combat_b_route_original_source_unavailable"){BDirectAttackInput.Fields(p,"accepted","errorCode","missingSources");SafeTransitionWire.Boolean(p,"accepted",false);SafeTransitionWire.Ids(p["missingSources"]);if(!((JArray)p["missingSources"]).Any())throw new ArgumentException("b_route_missing_sources_invalid");return new BRouteCommandResult(status,p);}
   bool pending=(bool?)p["augmentPending"]==true;
   if(status!=200&&!(pending&&status>=400&&status<=599))throw new InvalidOperationException("b_route_http_status:"+status+":"+(string)p["errorCode"]);
   if(pending){if(request.Kind!="ARRIVE"||status==200)throw new ArgumentException("b_augment_pending_invalid");BDirectAttackInput.Fields(p,"version","committed","receipt","augmentPending","errorCode");SafeTransitionWire.Id(p,"errorCode");}
   else if(p["augment"]!=null)BDirectAttackInput.Fields(p,"version","committed","receipt","augment");
   else BDirectAttackInput.Fields(p,"version","committed","receipt");
   if((string)p["version"]!=BRouteBinding.Version)throw new ArgumentException("b_route_commit_version_conflict");SafeTransitionWire.Boolean(p,"committed",true);var r=(JObject)p["receipt"];var c=request.Capture()["command"];
   if(request.IsWave){BDirectAttackInput.Fields(r,"version","eventId","nodeId","waveOrdinal","bindingHash","verificationId");if((string)r["version"]!="operation.b-wave-receipt.v1.20261004"||!JToken.DeepEquals(r["nodeId"],c["nodeId"])||!JToken.DeepEquals(r["waveOrdinal"],c["waveOrdinal"])||(string)r["bindingHash"]!=(string)request.Binding.Capture()["bindingHash"])throw new ArgumentException("b_route_wave_receipt_conflict");}
   else{BDirectAttackInput.Fields(r,new[]{"version","eventId","revision","state","verificationId"}.Concat(r.Property("firstPlayableScaling")==null?Array.Empty<string>():new[]{"firstPlayableScaling"}).Concat(r.Property("nodeStart")==null?Array.Empty<string>():new[]{"nodeStart"}).Concat(r.Property("ordinaryAutomation")==null?Array.Empty<string>():new[]{"ordinaryAutomation"}).ToArray());if((string)r["version"]!="operation.map-receipt.v1"||BDirectAttackInput.Nat(r,"revision",1)!=request.Binding.MapRevision+1)throw new ArgumentException("b_route_map_receipt_conflict");var s=(JObject)r["state"];BDirectAttackInput.Fields(s,"version","traversedEdgeIds","pendingEdgeId","phase","holdIds");SafeTransitionWire.Ids(s["traversedEdgeIds"]);SafeTransitionWire.Ids(s["holdIds"]);if((string)s["version"]!="operation.map-progress.v1"||s["pendingEdgeId"].Type!=JTokenType.Null||!new[]{"READY","TRAVELLING","ACTIVE","FINAL_NODE_COMPLETE"}.Contains((string)s["phase"])||request.Kind=="ARRIVE"&&(string)s["phase"]!="ACTIVE"||request.Kind=="COMPLETE"&&!new[]{"READY","FINAL_NODE_COMPLETE"}.Contains((string)s["phase"])||request.Kind=="RELEASE_HOLD"&&((string)s["phase"]!=(string)request.Binding.Capture()["phase"]||((JArray)s["holdIds"]).Any(h=>JToken.DeepEquals(h,c["holdId"]))))throw new ArgumentException("b_route_map_state_conflict");}
   if((string)r["eventId"]!=request.EventId)throw new ArgumentException("b_route_receipt_event_conflict");SafeTransitionWire.Id(r,"verificationId");return new BRouteCommandResult(status,p,p["augment"]==null?null:BAugmentArrivalOriginal.Read(p["augment"],request,r),r.Property("firstPlayableScaling")==null?null:FirstPlayableScaling.Read(r["firstPlayableScaling"],request,r),request);
  }
 }
 public sealed class BRouteHttpClient {
  readonly IGameApiAuthenticatedHttpTransport transport;public BRouteHttpClient(IGameApiAuthenticatedHttpTransport value){transport=value??throw new ArgumentNullException(nameof(value));}
  static string Path(string run){if(string.IsNullOrEmpty(run)||run.Length>200)throw new ArgumentException("b_route_run_invalid");return "/v1/runs/"+Uri.EscapeDataString(run);}
  public async Task<BRouteBinding> ReadAsync(string account,string run,GameApiAccessToken token,string expectedHash=null){var headers=new System.Collections.Generic.List<RunApiHttpHeader>{new RunApiHttpHeader("X-B-Route-Version",BRouteBinding.Version)};if(expectedHash!=null)headers.Add(new RunApiHttpHeader("If-Match",expectedHash));var r=await transport.SendAsync(new RunApiHttpRequest("GET",Path(run)+"/b-route-binding",null,headers),token);if(r.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded)throw new InvalidOperationException("b_route_http:"+r.Outcome+":"+r.ErrorCode);if(r.Response.StatusCode!=200)throw new InvalidOperationException("b_route_http_status:"+r.Response.StatusCode+":"+RunApiErrorJsonAdapter.OptionalString(r.Response.BodyJson,"errorCode"));return BRouteBinding.Read(r.Response.BodyJson,r.Response.Header("ETag")?.Trim('"'),account,run);}
  public async Task<BRouteCommandResult> SendAsync(BRouteCommand command,GameApiAccessToken token){if(command==null)throw new ArgumentNullException(nameof(command));var body=command.Capture().ToString(Newtonsoft.Json.Formatting.None);var r=await transport.SendAsync(new RunApiHttpRequest("POST",Path(command.Binding.RunId)+"/b-route/"+(command.IsWave?"wave-complete":"transition"),body,new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader("X-B-Route-Version",BRouteBinding.Version),new RunApiHttpHeader("Idempotency-Key",command.EventId)}),token);if(r.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded)throw new InvalidOperationException("b_route_http:"+r.Outcome+":"+r.ErrorCode);return BRouteCommandResult.Read(r.Response.StatusCode,r.Response.BodyJson,command);}
 }
}
