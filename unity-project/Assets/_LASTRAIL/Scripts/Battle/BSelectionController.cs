using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // One owner for next-selection state and command transitions. Server remains the account authority.
 public sealed class BSelectionController : IDisposable {
  Action<BSelectionRequest> send;Action changed;
  enum Phase {Unbound,Ready,InFlight,ReconcileRequired,AuthStopped,Disposed}
  Phase phase=Phase.Unbound;bool freshRead;
  readonly HashSet<string> conflictedKeys=new HashSet<string>(StringComparer.Ordinal);
  BSelectionRequest pending,original;JObject receipt;
  public BSelectionSnapshot State {get;private set;}
  public BSelectionFailure LastError {get;private set;}
  public BSelectionRequest OriginalRequest=>original;
  public JObject Receipt=>receipt==null?null:(JObject)receipt.DeepClone();
  public bool InFlight=>phase==Phase.InFlight;
  public string AccountId {get;private set;}
  internal void RequireAccount(string account){if(AccountId!=null&&AccountId!=account)throw new InvalidOperationException("selection_account_change_requires_new_owner");AccountId=account;}
  public void Bind(Action<BSelectionRequest> handler,Action updated=null){if(phase==Phase.Disposed)throw new ObjectDisposedException(nameof(BSelectionController));if(InFlight)throw new InvalidOperationException("selection_exchange_pending");send=handler??throw new ArgumentNullException(nameof(handler));changed=updated;freshRead=false;if(phase!=Phase.AuthStopped)phase=Phase.Ready;}
  public void Unbind(Action<BSelectionRequest> owner){if(send!=owner)return;send=null;changed=null;pending=null;freshRead=false;if(phase!=Phase.Disposed&&phase!=Phase.AuthStopped)phase=Phase.Unbound;}
  void Ready(bool reading=false){if(phase==Phase.Disposed)throw new ObjectDisposedException(nameof(BSelectionController));if(send==null||(phase!=Phase.Ready&&!(reading&&phase==Phase.ReconcileRequired)))throw new InvalidOperationException("selection_command_not_ready");}
  BSelectionRequest Dispatch(BSelectionRequest request){Ready(request.Method=="GET");pending=request;phase=Phase.InFlight;LastError=null;try{send(request);}catch{pending=null;phase=Phase.Ready;freshRead=false;throw;}return request;}
  public void Read()=>Dispatch(BSelectionRequest.Read());
  public BSelectionRequest Save(BSelectionDefinition selection,string key){Ready();if(State==null||!freshRead)throw new InvalidOperationException("selection_read_required");if(key!=null&&conflictedKeys.Contains(key))throw new InvalidOperationException("selection_new_key_required");var request=BSelectionRequest.Save(State.Revision,State.CatalogVersion,State.CatalogRevision,selection,key);original=request;return Dispatch(request);}
  public BSelectionRequest ReplayOriginal(){Ready();if(original==null)throw new InvalidOperationException("selection_original_missing");if(conflictedKeys.Contains(original.Key))throw new InvalidOperationException("selection_new_key_required");return Dispatch(original);}
  public void AcceptResponse(BSelectionRequest request,BSelectionHttpResponse response){
   if(phase==Phase.Disposed||pending!=request)return;
   BSelectionContract.Check(response.CacheControl=="no-store"&&response.Version==BSelectionRequest.Version);
   var body=response.Body;
   Phase next=Phase.Ready;bool nextFresh=false;
   if(response.Status!=200){LastError=new BSelectionFailure(response.Status,body);if(response.Status==401||response.Status==403)next=Phase.AuthStopped;else if(response.Status==409){next=Phase.ReconcileRequired;if(request.Key!=null)conflictedKeys.Add(request.Key);}}
   else if(request.Method=="GET"){State=new BSelectionSnapshot(body);LastError=null;nextFresh=true;}
   else{
    BSelectionContract.Fields(body,"accepted","version","receiptId","revision","changed","selection","savedCatalog","startReady");BSelectionContract.Closed(body);BSelectionContract.Text(body,"receiptId");BSelectionContract.Saved(body["savedCatalog"] as JObject);
    BSelectionContract.Check(body["accepted"].Type==JTokenType.Boolean&&(bool)body["accepted"]&&body["changed"].Type==JTokenType.Boolean);var definition=new BSelectionDefinition((JObject)body["selection"]);var source=request.Body;
    BSelectionContract.Check(JToken.DeepEquals(definition.Capture(),source["selection"])&&(string)body["savedCatalog"]["version"]==(string)source["catalogVersion"]&&(long)body["savedCatalog"]["revision"]==(long)source["expectedCatalogRevision"]&&BSelectionContract.Number(body,"revision")== (long)source["expectedRevision"]+((bool)body["changed"]?1:0));
    receipt=(JObject)body.DeepClone();if(State!=null&&State.Revision<(long)body["revision"]&&State.CatalogVersion==(string)body["savedCatalog"]["version"]&&State.CatalogRevision==(long)body["savedCatalog"]["revision"])State=State.WithReceipt(body);LastError=null;
   }
   pending=null;phase=next;freshRead=nextFresh;changed?.Invoke();
  }
  internal void TransportFailed(BSelectionRequest request){if(phase==Phase.Disposed||pending!=request)return;pending=null;phase=Phase.Ready;freshRead=false;/* Original immutable command survives a missing acknowledgement. */}
  public void Dispose(){if(phase==Phase.Disposed)return;phase=Phase.Disposed;send=null;changed=null;pending=null;freshRead=false;}
 }
}
