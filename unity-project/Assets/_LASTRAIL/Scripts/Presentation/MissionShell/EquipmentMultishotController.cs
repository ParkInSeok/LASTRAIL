using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 // Invoke from the existing UI thread. Awaited reads retain that synchronization
 // context. Read delegates are supplied by the authenticated owner, never a View.
 public sealed class EquipmentMultishotController:IDisposable {
  public const string FrameVersion="equipment.multishot-display-frame.v2.20261016";
  sealed class Request {internal readonly CancellationTokenSource Cancellation=new CancellationTokenSource();}
  readonly string accountId,runId;
  readonly Dictionary<string,Request> requests=new Dictionary<string,Request>();
  readonly Dictionary<string,EquipmentMultishotModel> models=new Dictionary<string,EquipmentMultishotModel>();
  readonly Dictionary<string,string> notices=new Dictionary<string,string>();
  EquipmentMultishotPlacementPreviewModel preview;JObject originalBinding;
  Action<JObject> present;Action clear;long generation;bool disposed;
  public EquipmentMultishotController(string accountId,string runId){if(string.IsNullOrWhiteSpace(accountId)||string.IsNullOrWhiteSpace(runId))throw new ArgumentException("Equipment display owner required");this.accountId=accountId;this.runId=runId;}
  public void Bind(Action<JObject> show,Action clearOwnedView){if(disposed)throw new ObjectDisposedException(nameof(EquipmentMultishotController));if(show==null||clearOwnedView==null)throw new ArgumentNullException();Unbind();present=show;clear=clearOwnedView;Publish();}
  public void Bind(LobbyDestinationPages view){if(view==null)throw new ArgumentNullException(nameof(view));Bind(view.PresentEquipmentMultishot,view.ClearEquipmentMultishot);view.BindEquipmentMultishotClose(Unbind);}
  public void Unbind(){generation++;var ownedRequests=requests.Values.ToArray();requests.Clear();foreach(var request in ownedRequests){request.Cancellation.Cancel();request.Cancellation.Dispose();}models.Clear();notices.Clear();preview=null;originalBinding=null;var owned=clear;present=null;clear=null;owned?.Invoke();}
  static string Key(string basis)=>basis switch{"CURRENT_VERIFIED_EQUIPMENT"=>"current","ORIGINAL_LOCAL_TRIAL_START"=>"departure","CURRENT_VERIFIED_STORED_ITEM"=>"stored",_=>throw new ArgumentException("Explicit equipment display basis required")};
  void RequireOriginalBinding(JObject source){var binding=new JObject{["originalMiraHash"]=source["originalMiraHash"].DeepClone(),["departurePinHash"]=source["departurePinHash"].DeepClone()};if(originalBinding!=null&&!JToken.DeepEquals(originalBinding,binding))throw new ArgumentException("Equipment display original departure conflict");originalBinding=binding;}
  public Task RefreshAsync(string basis,Func<CancellationToken,Task<EquipmentMultishotModel>> read){string key=Key(basis);return ReadAsync(key,read,model=>{if(model==null||model.AccountId!=accountId||model.RunId!=runId||model.Basis!=basis)throw new ArgumentException("Equipment display source identity/basis conflict");if(basis!="CURRENT_VERIFIED_STORED_ITEM")RequireOriginalBinding(model.Capture());models[key]=model;},()=>models.Remove(key),"효과 정보를 확인하지 못했습니다.");}
  public Task RefreshPreviewAsync(Func<CancellationToken,Task<EquipmentMultishotPlacementPreviewModel>> read)=>ReadAsync("preview",read,model=>{if(model==null||model.AccountId!=accountId||model.RunId!=runId||model.CanApplyToCombat)throw new ArgumentException("Equipment preview owner/scope conflict");var binding=model.BindingSnapshot;if(binding!=null)RequireOriginalBinding(binding);preview=model;},()=>preview=null,"장착 미리보기를 확인하지 못했습니다.");
  void CancelRead(string key){if(!requests.TryGetValue(key,out var request))return;requests.Remove(key);request.Cancellation.Cancel();request.Cancellation.Dispose();}
  public void ClearPreview(){if(disposed)throw new ObjectDisposedException(nameof(EquipmentMultishotController));CancelRead("preview");preview=null;notices.Remove("preview");Publish();}
  async Task ReadAsync<T>(string key,Func<CancellationToken,Task<T>> read,Action<T> accept,Action invalidate,string failure){
   if(disposed)throw new ObjectDisposedException(nameof(EquipmentMultishotController));if(read==null)throw new ArgumentNullException(nameof(read));if(present==null)return;CancelRead(key);
   var request=new Request();requests[key]=request;long stamp=generation;var token=request.Cancellation.Token;
   bool Current()=>present!=null&&generation==stamp&&requests.TryGetValue(key,out var current)&&ReferenceEquals(current,request);
   try{var value=await read(token);if(!Current())return;accept(value);notices.Remove(key);Publish();}
   catch(OperationCanceledException)when(token.IsCancellationRequested){}
   catch(Exception){if(Current()){invalidate();notices[key]=failure;Publish();}}
   finally{if(requests.TryGetValue(key,out var current)&&ReferenceEquals(current,request)){requests.Remove(key);request.Cancellation.Dispose();}}
  }
  void Publish(){if(present==null)return;var frame=new JObject{["version"]=FrameVersion,["preview"]=preview==null?JValue.CreateNull():(JToken)preview.Render(),["previewNotice"]=notices.TryGetValue("preview",out var notice)?notice:""};foreach(var key in new[]{"current","departure","stored"}){frame[key]=models.TryGetValue(key,out var model)?(JToken)model.Render():JValue.CreateNull();frame[key+"Notice"]=notices.TryGetValue(key,out var text)?text:"";}present(frame);}
  public void Dispose(){if(disposed)return;Unbind();disposed=true;}
 }
}
