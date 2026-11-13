using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Next-refit read is a server snapshot. Original acknowledged result is separate from current state.
 public sealed class BRefitController : IDisposable {
  enum Phase{Unbound,Ready,InFlight,ReconcileRequired,AuthStopped,Disposed}
  Phase phase;bool freshRead;Action<BRefitRequest> send;Action changed;BRefitRequest pending,original;JObject receipt;
  readonly HashSet<string> conflictedKeys=new HashSet<string>(StringComparer.Ordinal);
  public BRefitSnapshot State {get;private set;}public BRefitRequest OriginalRequest=>original;public JObject Receipt=>receipt==null?null:(JObject)receipt.DeepClone();public BSelectionFailure LastError{get;private set;}public bool InFlight=>phase==Phase.InFlight;public string AccountId {get;private set;}
  public BRefitController(string account=null){if(account!=null){BRefitWire.Need(!string.IsNullOrWhiteSpace(account)&&account.Length<=128);AccountId=account;}}
  internal void RequireAccount(string account){if(AccountId!=null&&account!=AccountId)throw new InvalidOperationException("refit_account_requires_new_owner");AccountId=account;}
  public void Bind(Action<BRefitRequest> handler,Action update=null){if(phase==Phase.Disposed)throw new ObjectDisposedException(nameof(BRefitController));if(InFlight)throw new InvalidOperationException("refit_exchange_pending");send=handler??throw new ArgumentNullException(nameof(handler));changed=update;freshRead=false;if(phase!=Phase.AuthStopped)phase=Phase.Ready;}
  public void Unbind(Action<BRefitRequest> owner){if(send!=owner)return;send=null;changed=null;pending=null;freshRead=false;if(phase!=Phase.Disposed&&phase!=Phase.AuthStopped)phase=Phase.Unbound;}
  void Ready(bool read=false){if(phase==Phase.Disposed)throw new ObjectDisposedException(nameof(BRefitController));if(send==null||(phase!=Phase.Ready&&!(read&&phase==Phase.ReconcileRequired)))throw new InvalidOperationException("refit_not_ready");}
  BRefitRequest Dispatch(BRefitRequest request){Ready(request.Method=="GET");pending=request;phase=Phase.InFlight;LastError=null;try{send(request);}catch{TransportFailed(request);throw;}return request;}
  public void Read()=>Dispatch(BRefitRequest.Read());
  public BRefitRequest Save(JArray cells,string key){Ready();if(State==null||!freshRead)throw new InvalidOperationException("refit_fresh_read_required");if(key!=null&&conflictedKeys.Contains(key))throw new InvalidOperationException("refit_new_key_required");var request=BRefitRequest.Save(State,cells,key);original=request;receipt=null;return Dispatch(request);}
  public BRefitRequest ReplayOriginal(){Ready();if(original==null||conflictedKeys.Contains(original.Key))throw new InvalidOperationException("refit_original_unavailable");return Dispatch(original);}
  public void AcceptResponse(BRefitRequest request,BRefitHttpResponse response){
   if(phase==Phase.Disposed||pending!=request)return;
   BRefitWire.Need(response.CacheControl=="no-store"&&response.Version==BRefitRequest.Version);var body=response.Body;var next=Phase.Ready;bool fresh=false;
   BRefitSnapshot accepted=null;JObject result=null;BSelectionFailure failure=null;
   if(response.Status!=200){failure=new BSelectionFailure(response.Status,body);if(response.Status==401||response.Status==403)next=Phase.AuthStopped;else if(response.Status==409)next=Phase.ReconcileRequired;}
   else if(request.Method=="GET")accepted=new BRefitSnapshot(body,AccountId??throw new InvalidOperationException("refit_account_unbound"));
   else {result=request.ValidateReceipt(body);if(receipt!=null)BRefitWire.Need(JToken.DeepEquals(receipt,result));}
   // Boundary parsing succeeds entirely before any externally visible state is replaced.
   if(accepted!=null){State=accepted;fresh=true;}if(result!=null)receipt=result;if(failure!=null&&response.Status==409&&request.Key!=null)conflictedKeys.Add(request.Key);
   LastError=failure;pending=null;phase=next;freshRead=fresh;changed?.Invoke();
  }
  internal void TransportFailed(BRefitRequest request){if(phase==Phase.Disposed||pending!=request)return;pending=null;phase=Phase.Ready;freshRead=false;/* Original request/key/source survive uncertain commit. */}
  public void Dispose(){phase=Phase.Disposed;send=null;changed=null;pending=null;freshRead=false;}
 }
}