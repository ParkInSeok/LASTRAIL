using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class BArrivalReportClientTests {
  static BRouteBinding Route;
  static BArrivalReportRequest Request(){
   var source=BArrivalBoundarySessionTests.Fixture();Route=source.NativeNode.Route;
   using(var owner=BArrivalBoundarySession.Open(source,Route,BArrivalBoundarySessionTests.Destination(Route),"http-fixture",()=>null,(p,r)=>{})){
    owner.Execute(new JObject{["kind"]="ADVANCE",["eventId"]="travel",["elapsed"]=3000000,["maximumSteps"]=10});
    return BArrivalReportRequest.FromExport(owner.CaptureExport("report-original"),Route);
   }
  }
  static JObject Receipt(BArrivalReportRequest request){
   var b=request.Body();var t=b["travel"];var route=Route.Capture();var arrival=t["arrival"];
   return new JObject{["version"]=BArrivalReportRequest.Version,["requestId"]=request.RequestId,["evidenceRef"]="B_ARRIVAL:fixture",
    ["arrivalAccepted"]=true,["progressAuthorized"]=true,["combatTruth"]="NOT_PROVEN",["verificationLevel"]="ORIGINAL_PINNED_TRAVEL_COMMAND_REPLAY",["missingSources"]=new JArray(),
    ["observation"]=new JObject{["version"]="combat.b-boundary-arrival-observation.v1.20261020",["accountId"]=t["accountId"].DeepClone(),["runId"]=t["runId"].DeepClone(),["nodeId"]=t["nodeId"].DeepClone(),["nativeContextId"]=t["nativeContextId"].DeepClone(),["mapRevision"]=b["reportOriginMapRevision"].DeepClone(),["departurePinHash"]=route["departurePinHash"].DeepClone(),["operationBindingHash"]=route["bindingHash"].DeepClone(),["nodeSourceHash"]=t["nodeSourceHash"].DeepClone(),["checkpointHash"]=b["checkpointHash"].DeepClone(),["evidenceRef"]="B_ARRIVAL:fixture",["originalEventId"]=arrival["eventId"].DeepClone(),["sequence"]=arrival["commandSequence"].DeepClone(),["sourceRevision"]=arrival["commandSequence"].DeepClone(),["travelStopped"]=true,["combatTruth"]="NOT_PROVEN",
     ["arrival"]=new JObject{["eventId"]=arrival["eventId"].DeepClone(),["sequence"]=arrival["commandSequence"].DeepClone(),["tick"]=arrival["tick"].DeepClone()},
     ["destination"]=new JObject{["eventId"]=b["destinationEventId"].DeepClone(),["mapRevision"]=b["reportOriginMapRevision"].DeepClone(),["edgeId"]=route["pendingEdgeId"].DeepClone(),["receiptHash"]=new string('a',64)}}};
  }
  sealed class Wire:IRunApiHttpTransport {
   public Func<RunApiHttpRequest,RunApiHttpResponse> Reply;public int Calls;
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Calls++;return Task.FromResult(Reply(request));}
  }
  [Test] public async Task SubmitUsesOriginalBodyAndReceiptFeedsExistingArriveCommandWithoutFollowupGet(){
   var request=Request();var before=request.Body();var wire=new Wire();
   wire.Reply=sent=>{Assert.AreEqual("POST",sent.Method);Assert.AreEqual("/v1/runs/"+Uri.EscapeDataString(request.RunId)+"/b-arrival-reports",sent.EndpointPath);Assert.IsTrue(JToken.DeepEquals(before,JObject.Parse(sent.BodyJson)));Assert.IsTrue(sent.Headers.Any(h=>h.Name=="Idempotency-Key"&&h.Value==request.RequestId));return RunApiHttpResponse.Success(200,Receipt(request).ToString());};
   var receipt=await new BArrivalReportHttpClient(new GameApiAuthenticatedHttpTransport(wire)).SubmitAsync(request,new GameApiAccessToken("lst1.test"));
   var command=receipt.CreateArrivalCommand("arrive-original");Assert.AreEqual("ARRIVE",command.Kind);Assert.AreEqual("B_ARRIVAL:fixture",(string)command.Capture()["command"]["evidenceRef"]);Assert.AreEqual(1,wire.Calls);
   before["requestId"]="changed";Assert.AreEqual("report-original",request.RequestId);
  }
  [TestCase("checkpointHash")][TestCase("nodeId")][TestCase("accountId")][TestCase("nativeContextId")]
  public void RejectsForeignObservation(string field){var request=Request();var receipt=Receipt(request);receipt["observation"][field]="foreign";Assert.Throws<ArgumentException>(()=>BArrivalReportReceipt.Read(receipt.ToString(),request));}
  [Test] public async Task RecoverRequiresSameReportAndOpaqueReference(){
   var request=Request();var wire=new Wire{Reply=sent=>{Assert.AreEqual("GET",sent.Method);Assert.IsTrue(sent.EndpointPath.EndsWith("/B_ARRIVAL%3Afixture"));return RunApiHttpResponse.Success(200,new JObject{["receipt"]=Receipt(request),["report"]=request.Body()}.ToString());}};
   var client=new BArrivalReportHttpClient(new GameApiAuthenticatedHttpTransport(wire));await client.RecoverAsync(request,"B_ARRIVAL:fixture",new GameApiAccessToken("lst1.test"));
   wire.Reply=sent=>{var report=request.Body();report["destinationEventId"]="other";return RunApiHttpResponse.Success(200,new JObject{["receipt"]=Receipt(request),["report"]=report}.ToString());};
   Assert.ThrowsAsync<ArgumentException>(async()=>await client.RecoverAsync(request,"B_ARRIVAL:fixture",new GameApiAccessToken("lst1.test")));
  }
  [Test] public void MissingIndependentAuthorityRemainsServerRejection(){
   var request=Request();var wire=new Wire{Reply=sent=>RunApiHttpResponse.Success(409,"{\"accepted\":false,\"errorCode\":\"combat_b_arrival_original_time_credit_and_accepted_event_owner_missing\"}")};
   var error=Assert.ThrowsAsync<BResultReportHttpException>(async()=>await new BArrivalReportHttpClient(new GameApiAuthenticatedHttpTransport(wire)).SubmitAsync(request,new GameApiAccessToken("lst1.test")));
   Assert.AreEqual(409,error.StatusCode);Assert.AreEqual(1,wire.Calls);
  }
  [Test] public async Task LostResponseRetriesSameOriginalBodyAndKeyWithoutManufacturingEvidence(){
   var request=Request();string body=null;var wire=new Wire();
   wire.Reply=sent=>{if(body==null){body=sent.BodyJson;return RunApiHttpResponse.NetworkError("ack lost");}Assert.AreEqual(body,sent.BodyJson);Assert.IsTrue(sent.Headers.Any(h=>h.Name=="Idempotency-Key"&&h.Value==request.RequestId));return RunApiHttpResponse.Success(200,Receipt(request).ToString());};
   var client=new BArrivalReportHttpClient(new GameApiAuthenticatedHttpTransport(wire));
   Assert.ThrowsAsync<BResultReportHttpException>(async()=>await client.SubmitAsync(request,new GameApiAccessToken("lst1.test")));
   Assert.AreEqual("B_ARRIVAL:fixture",(await client.SubmitAsync(request,new GameApiAccessToken("lst1.test"))).EvidenceRef);Assert.AreEqual(2,wire.Calls);
  }
  [Test] public void UnacceptedReceiptCannotCreateArrivalCommand(){var request=Request();var receipt=Receipt(request);receipt["arrivalAccepted"]=false;Assert.Throws<ArgumentException>(()=>BArrivalReportReceipt.Read(receipt.ToString(),request));}
 }
}
