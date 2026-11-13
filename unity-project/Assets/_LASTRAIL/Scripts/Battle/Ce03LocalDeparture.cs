using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Explicit disposable-host composition only; the production departure path is unchanged.
public enum Ce03EntryStatus { None, Preparing, PreparationFailed, UpdateRequired, Entered }
public enum Ce03PreparationBlock { None, SupportSelection, Verification }
public sealed class Ce03LocalDeparture : IDisposable {
 readonly Ce03LocalClient client;readonly Func<JObject,bool> verified;readonly Action<JObject,JObject> persistPrepare;readonly Action<Ce03OriginalStart> enter;
 readonly Func<bool> ownerCurrent;
 JObject selection,prepared,quote;bool busy,disposed;
 Ce03OriginalStart retainedStart;
 bool Current=>!disposed&&ownerCurrent();
 public Ce03EntryStatus EntryStatus {get;private set;}
 public Ce03PreparationBlock PreparationBlock {get;private set;}
 public Exception LastEntryError {get;private set;}
 public bool HasConfirmedStart=>retainedStart!=null||client.HasReceivedStart;
 public bool NeedsReceiptSave=>client.HasReceivedStart;
 public bool CanCancelUnsentStart=>Current&&!busy&&client.HasUnstoredStart;
 public string LockedOperation=>!CanChangeSelection?(string)selection?["operationId"]:null;
 public string LockedRisk=>!CanChangeSelection?(string)selection?["risk"]:null;
 public bool CancelUnsentStart(){if(!CanCancelUnsentStart)return false;try{if(!client.CancelUnsentStart())return false;Notice="전송하지 않은 출격 요청을 취소했습니다.";Notify();return true;}catch{Notice="출격 요청 기록을 정리하지 못했습니다. 원래 요청은 보존됩니다.";Notify();return false;}}
 public bool CanRetryEntry=>Current&&!busy&&retainedStart!=null&&EntryStatus==Ce03EntryStatus.PreparationFailed;
 void Notify(){if(Current)Changed?.Invoke();}
 public event Action Changed;
 string notice="지휘관 스킬 3개를 저장한 뒤 출발 비용을 확인해 주세요.";
 public string Notice {get=>client.HasUnstoredStart?"출격 준비 정보를 저장하지 못했습니다. 같은 요청으로 저장을 다시 시도해 주세요.":client.HasReceivedStart?"출격이 확정되었습니다. 원래 응답을 저장하려면 다시 시도해 주세요.":busy&&client.PendingKind=="start"?(client.IsConfirmingStart?"출격 결과를 확인하고 있습니다.":"출격 준비 중입니다."):notice;private set=>notice=value;}
 public bool Busy=>busy;
 public bool CanChangeSelection=>Current&&!busy&&!client.HasPending&&client.AwaitingCheckpointKey==null&&!client.RequiresCheckpointReconciliation&&retainedStart==null;
 public bool HasPrepare=>prepared!=null;
 public void InvalidateQuote(){if(!CanChangeSelection)return;quote=null;Notify();}
 public bool CanRetryStart=>Current&&!busy&&retainedStart==null&&client.PendingKind=="start";
 public JObject Prepared=>prepared==null?null:(JObject)prepared.DeepClone();
 public string ReadGuideFuelCost(string operation,string risk,long formation,long loadout,long ultimate){
  var wanted=new JObject{["operationId"]=operation,["risk"]=risk,["expectedFormationRevision"]=formation,["expectedLoadoutRevision"]=loadout,["expectedUltimateRevision"]=ultimate};
  return CanStart&&JToken.DeepEquals(selection,wanted)?(string)quote["quote"]?["fuelCost"]:null;
 }
 public DateTimeOffset? QuoteExpiresAt=>Current&&DateTimeOffset.TryParse((string)quote?["expiresAt"],out var expiry)?expiry:(DateTimeOffset?)null;
 public bool CanStart=>Current&&!busy&&retainedStart==null&&prepared!=null&&quote!=null&&!client.HasPending&&QuoteExpiresAt>DateTimeOffset.UtcNow&&verified(prepared);
 public Ce03LocalDeparture(Ce03LocalClient client,Func<JObject,bool> verified,Action<JObject,JObject> persistPrepare,Action<Ce03OriginalStart> enter,Func<bool> ownerCurrent=null){this.client=client??throw new ArgumentNullException(nameof(client));this.verified=verified??throw new ArgumentNullException(nameof(verified));this.persistPrepare=persistPrepare??throw new ArgumentNullException(nameof(persistPrepare));this.enter=enter??throw new ArgumentNullException(nameof(enter));this.ownerCurrent=ownerCurrent??(()=>true);client.StartProgressChanged+=Notify;if(client.PendingKind=="start")Notice="출격 결과를 아직 확인하지 못했습니다. 원래 요청을 다시 확인해 주세요.";}
 public void RestorePrepared(JObject originalSelection,JObject originalPrepared){if(!Current||busy||prepared!=null||retainedStart!=null)throw new InvalidOperationException();selection=(JObject)originalSelection.DeepClone();prepared=(JObject)originalPrepared.DeepClone();}
 // The owning run supplies its original key; never infer the active run from receipt order.
 public bool RestoreAwaitingCheckpoint(){var key=client.AwaitingCheckpointKey;if(key==null){if(client.RequiresCheckpointReconciliation)throw new InvalidOperationException("Legacy confirmed START requires the existing run owner's exact key and checkpoint reconciliation.");return false;}RestoreConfirmedStart(key);return true;}
 public void RestoreConfirmedStart(string originalKey){
  if(!Current||busy||retainedStart!=null)throw new InvalidOperationException("Departure cannot restore another confirmed run.");
  if(string.IsNullOrWhiteSpace(originalKey))throw new ArgumentException("Original START key required.",nameof(originalKey));
  var original=client.ReadRetainedStart(originalKey);
  retainedStart=original;quote=null;prepared=null;selection=null;LastEntryError=null;
  EntryStatus=Ce03EntryStatus.PreparationFailed;Notice="확정된 작전을 복원했습니다. 전투 자료를 다시 준비해 주세요.";Notify();
 }
 public async Task PreviewAsync(string operation,string risk,long formation,long loadout,long ultimate){
  if(!Current||busy||retainedStart!=null||client.AwaitingCheckpointKey!=null||client.RequiresCheckpointReconciliation)return;busy=true;quote=null;PreparationBlock=Ce03PreparationBlock.None;Notify();try{
   if(client.HasPending){if(client.PendingKind=="quote"){await client.RetryAsync();Notice="원래 견적 요청을 확인했습니다. 현재 편성의 비용을 다시 확인해 주세요.";}else Notice="원래 요청을 먼저 확인해 주세요.";return;}
   var wanted=new JObject{["operationId"]=operation,["risk"]=risk,["expectedFormationRevision"]=formation,["expectedLoadoutRevision"]=loadout,["expectedUltimateRevision"]=ultimate};
   long revision;
   if(prepared!=null&&JToken.DeepEquals(selection,wanted))revision=Math.Max(client.KnownSupportRevision??0,new Ce03CommandContract(prepared["commandContract"] as JObject).Revision);
   else{var support=await client.ReadSupportAsync();if(!Current)return;revision=(long)support["selection"]["revision"];if(revision<1||((JArray)support["selection"]["slots"]).Count!=3){PreparationBlock=Ce03PreparationBlock.SupportSelection;Notice="지휘관 스킬 3개를 선택하고 저장해 주세요.";return;}}
   if(prepared==null||!JToken.DeepEquals(selection,wanted)||new Ce03CommandContract(prepared["commandContract"] as JObject).Revision!=revision){var next=await client.PrepareAsync(wanted,revision);if(!Current)return;persistPrepare(wanted,next);selection=wanted;prepared=next;}
   if(!verified(prepared)){PreparationBlock=Ce03PreparationBlock.Verification;Notice="전투 데이터 확인을 기다리고 있습니다.";return;}
   var nextQuote=await client.QuoteAsync(wanted);if(!Current)return;quote=nextQuote;Notice="소모 연료 "+quote["quote"]?["fuelCost"]+" · 출발할 수 있습니다.";
  }catch{Notice="출발 준비를 확인하지 못했습니다. 원래 요청은 보존됩니다.";}finally{busy=false;Notify();}
 }
 public async Task StartAsync(){
  if(!CanStart&&!CanRetryStart)return;busy=true;Notice="출격 준비 중입니다.";Notify();try{
   var result=client.PendingKind=="start"?await client.RetryAsync():await client.StartPreparedAsync((string)quote["operationPreviewId"],prepared);
   if(!Current)return;
   retainedStart=client.ReadRetainedStart(client.AwaitingCheckpointKey);quote=null;prepared=null;selection=null;
  }catch{Notice="출격 결과를 아직 확인하지 못했습니다. 다시 확인하면 원래 출발 요청을 사용합니다.";}finally{busy=false;}
  if(Current&&retainedStart!=null)EnterRetained();else Notify();
 }
 public void RetryEntry(){if(CanRetryEntry)EnterRetained();}
 void EnterRetained(){
  if(!Current||retainedStart==null||busy)return;
  busy=true;EntryStatus=Ce03EntryStatus.Preparing;LastEntryError=null;Notice="확정된 작전의 전투 자료를 준비하고 있습니다.";Notify();
  try{if(!Current)return;enter(retainedStart);if(Current){EntryStatus=Ce03EntryStatus.Entered;Notice="전투에 진입합니다.";}}
  catch(NotSupportedException error){LastEntryError=error;EntryStatus=Ce03EntryStatus.UpdateRequired;Notice="이 작전의 전투 자료를 지원하는 앱 업데이트가 필요합니다. 확정된 작전은 보존됩니다.";}
  catch(Exception error){LastEntryError=error;EntryStatus=Ce03EntryStatus.PreparationFailed;Notice="전투 자료를 준비하지 못했습니다. 확정된 작전으로 다시 준비해 주세요.";}
  finally{busy=false;Notify();}
 }
 public void Dispose(){disposed=true;client.StartProgressChanged-=Notify;quote=null;prepared=null;selection=null;retainedStart=null;LastEntryError=null;}
}
}
