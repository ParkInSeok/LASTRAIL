using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
namespace LastRail.Run {
 public sealed class BResultReportDeliveryResult {
  public BLocalObservationDeliveryStatus Status{get;}public BResultConsistencyReceipt Receipt{get;}
  internal BResultReportDeliveryResult(BLocalObservationDeliveryStatus status,BResultConsistencyReceipt receipt){Status=status;Receipt=receipt;}
 }
 // Report delivery has a separate cursor from arbitrary local forwarding.
 // Server receipt and local callback completion remain different facts.
 internal sealed class BResultReportDelivery:IDisposable {
  readonly BRuntimeObservationDelivery delivery;readonly Func<BRouteRuntimeObservation> capture;
  readonly Dictionary<string,string> requestHashes=new Dictionary<string,string>();
  readonly Dictionary<string,string> sourceRequests=new Dictionary<string,string>();
  readonly Dictionary<string,BResultConsistencyReceipt> receipts=new Dictionary<string,BResultConsistencyReceipt>();
  internal BResultReportDelivery(Func<BRouteRuntimeObservation> capture){this.capture=capture;delivery=new BRuntimeObservationDelivery(capture);}
  internal async Task<BResultReportDeliveryResult> SendAsync(BRouteRuntimeObservation original,BResultReportRequest request,BResultReportHttpClient client,GameApiAccessToken token,string recoverRef,CancellationToken cancellation){
   if(original==null||request==null||client==null)throw new ArgumentNullException();
   var exact=new BResultReportRequest(original,request.RequestId);
   if(request.OriginalDeliveryKey!=original.DeliveryKey||request.BodyJson!=exact.BodyJson)throw new ArgumentException("b_result_delivery_original_conflict");
   string hash=BDirectAttackInput.Digest(request.Capture());
   if(requestHashes.TryGetValue(request.RequestId,out var old)&&old!=hash||sourceRequests.TryGetValue(original.DeliveryKey,out var prior)&&prior!=request.RequestId)throw new ArgumentException("b_result_delivery_request_conflict");
   if(recoverRef!=null&&receipts.TryGetValue(request.RequestId,out var accepted)&&accepted.EvidenceRef!=recoverRef)throw new ArgumentException("b_result_recovery_reference_conflict");
   BResultConsistencyReceipt incoming=null;
   long ticket=delivery.Generation;
   var status=await delivery.ForwardAsync(original,async(x,ct)=>{
    requestHashes[request.RequestId]=hash;sourceRequests[original.DeliveryKey]=request.RequestId;
    incoming=recoverRef==null?await client.SubmitAsync(request,token,ct):await client.RecoverAsync(request,recoverRef,token,ct);
   },cancellation);
   if(delivery.Generation!=ticket||capture().DeliveryKey!=original.DeliveryKey)return new BResultReportDeliveryResult(BLocalObservationDeliveryStatus.Superseded,null);
   if(status==BLocalObservationDeliveryStatus.Forwarded)receipts[request.RequestId]=incoming;
   receipts.TryGetValue(request.RequestId,out var receipt);
   return new BResultReportDeliveryResult(status,status==BLocalObservationDeliveryStatus.Forwarded||status==BLocalObservationDeliveryStatus.AlreadyForwarded?receipt:null);
  }
  internal void Invalidate()=>delivery.Invalidate();
  internal void InvalidateChangedSources()=>delivery.InvalidateChangedSources();
  public void Dispose(){try{delivery.Dispose();}finally{receipts.Clear();requestHashes.Clear();sourceRequests.Clear();}}
 }
}
