using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Existing public CONFIRM only. Physical track control and arrival proofs differ.
 public sealed class BOperationDestinationIntent {
  public string AccountId{get;} public string RunId{get;} public string CommandId{get;} public string EdgeId{get;} public long ExpectedRevision{get;}
  public BOperationDestinationIntent(string account,string run,string command,long revision,string edge){
   if(string.IsNullOrEmpty(account)||account.Length>200||string.IsNullOrEmpty(run)||run.Length>200||string.IsNullOrEmpty(edge)||edge.Length>200||!Regex.IsMatch(command??"","^[\\x21-\\x7e]{1,128}$")||revision<1||revision>=9007199254740991)throw new ArgumentException("b_destination_intent_invalid");
   AccountId=account;RunId=run;CommandId=command;ExpectedRevision=revision;EdgeId=edge;
  }
  public JObject Body()=>new JObject{["expectedRevision"]=ExpectedRevision,["edgeId"]=EdgeId};
  public JObject Capture()=>new JObject{["accountId"]=AccountId,["runId"]=RunId,["commandId"]=CommandId,["body"]=Body()};
 }
 public sealed class BOperationDestinationReceipt {
  readonly JObject packet;public BOperationDestinationIntent Intent{get;}public bool CanProveArrival=>false;public JObject TravelInitial=>packet["travelInitial"] is JObject initial?(JObject)initial.DeepClone():null;
  BOperationDestinationReceipt(JObject p,BOperationDestinationIntent i){packet=(JObject)p.DeepClone();Intent=i;}
  public JObject Capture()=>new JObject{["intent"]=Intent.Capture(),["response"]=packet.DeepClone()};
  public static BOperationDestinationReceipt Read(string json,BOperationDestinationIntent intent){
   if(intent==null)throw new ArgumentNullException(nameof(intent));var p=SafeTransitionWire.Parse(json);BDirectAttackInput.Fields(p,p.Property("travelInitial")==null?new[]{"version","requestId","revision","phase","pendingEdgeId"}:new[]{"version","requestId","revision","phase","pendingEdgeId","travelInitial"});if(p.Property("travelInitial")!=null&&!(p["travelInitial"] is JObject))throw new ArgumentException("b_destination_initial_invalid");
   if((string)p["version"]!="operation.map-destination.v1"||(string)p["requestId"]!=intent.CommandId||BDirectAttackInput.Nat(p,"revision",1)!=intent.ExpectedRevision+1||(string)p["phase"]!="TRAVELLING"||(string)p["pendingEdgeId"]!=intent.EdgeId)throw new ArgumentException("b_destination_response_conflict");return new BOperationDestinationReceipt(p,intent);
  }
  internal static BOperationDestinationReceipt Restore(JObject saved){var i=(JObject)saved["intent"];BDirectAttackInput.Fields(saved,"intent","response");BDirectAttackInput.Fields(i,"accountId","runId","commandId","body");var body=(JObject)i["body"];BDirectAttackInput.Fields(body,"expectedRevision","edgeId");return Read(saved["response"].ToString(),new BOperationDestinationIntent(BDirectAttackInput.Text(i,"accountId"),BDirectAttackInput.Text(i,"runId"),BDirectAttackInput.Text(i,"commandId"),BDirectAttackInput.Nat(body,"expectedRevision",1),BDirectAttackInput.Text(body,"edgeId")));}
 }
 public sealed class BOperationDestinationClient {
  readonly IGameApiAuthenticatedHttpTransport transport;public BOperationDestinationClient(IGameApiAuthenticatedHttpTransport t){transport=t??throw new ArgumentNullException(nameof(t));}
  public async Task<BOperationDestinationReceipt> ConfirmAsync(BOperationDestinationIntent intent,GameApiAccessToken token){
   if(intent==null)throw new ArgumentNullException(nameof(intent));var response=await transport.SendAsync(new RunApiHttpRequest("POST","/v1/runs/"+Uri.EscapeDataString(intent.RunId)+"/operation-map/destination",intent.Body().ToString(Newtonsoft.Json.Formatting.None),new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader("Idempotency-Key",intent.CommandId)}),token);
   if(response.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded)throw new InvalidOperationException("b_destination_http:"+response.Outcome+":"+response.ErrorCode);if(response.Response.StatusCode!=200)throw new InvalidOperationException("b_destination_http_status:"+response.Response.StatusCode+":"+RunApiErrorJsonAdapter.OptionalString(response.Response.BodyJson,"errorCode"));return BOperationDestinationReceipt.Read(response.Response.BodyJson,intent);
  }
 }
}
