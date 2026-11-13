#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Diagnostics {
// Drives the existing departure View callbacks and Controller with the real
// Unity transport. Credentials are read only from the disposable private input.
public static class Ce03DeparturePlayerTrial {
 sealed class ObservedTransport:IRunApiHttpTransport {
  readonly IRunApiHttpTransport inner;public int StartRequests {get;private set;}
  public ObservedTransport(IRunApiHttpTransport inner){this.inner=inner;}
  public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){if(request.EndpointPath=="/__local/ce03/start")StartRequests++;return inner.SendAsync(request);}
 }
 public static async Task<Ce03LocalClient> CreateEntry(CombatPlayerAssetBinding binding,string queue,int index){
  double deadline=Time.realtimeSinceStartupAsDouble+300;
  while(!File.Exists(Path.Combine(queue,"ui-ready-"+index))){if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("UI host not ready");await Task.Delay(100);}
  var config=JObject.Parse(File.ReadAllText(Path.Combine(queue,"ui.private.json")));
  var prepared=(JObject)config["prepared"];var selection=(JObject)config["selection"];
  string folder=Path.Combine(queue,"run-"+index);Directory.CreateDirectory(folder);
  string journal=Path.Combine(folder,"client-journal.json");
  string scenario=(string)config["scenarios"]?[index];
  bool stopAfterFailure=scenario=="receipt-write-failure-stop";
  bool initialFailure=scenario=="request-and-receipt-write-failure"||scenario=="request-write-failure-cancel",initialFailed=false;
  string attemptedPending=null;
  bool injectStorageFailure=scenario=="receipt-write-failure"||stopAfterFailure||scenario=="request-and-receipt-write-failure",storageFailed=false;
  void Persist(string raw){
   var value=JObject.Parse(raw);
   if(scenario=="checkpoint-cleanup-stop"&&value.Property("awaitingCheckpoint")!=null&&value["awaitingCheckpoint"].Type==JTokenType.Null)throw new IOException("Injected checkpoint handoff cleanup failure");
   if(initialFailure&&!initialFailed&&value["pending"] is JObject pendingWrite&&(string)pendingWrite["kind"]=="start"){
    initialFailed=true;attemptedPending=Ce03CommandContract.Canonical(value["pending"]);throw new IOException("Injected original request write failure before dispatch");
   }
   if(injectStorageFailure&&!storageFailed&&value["pending"]?.Type==JTokenType.Null&&((JObject)value["receipts"]).Properties().Any(p=>(string)p.Value["request"]?["kind"]=="start")){
    storageFailed=true;throw new IOException("Injected receipt persistence failure before replacement");
   }
   Ce03AcceptedPlayerTrial.Atomic(journal,raw);
  }
  var transport=new ObservedTransport(new Ce03LoopbackTransport(new Uri((string)config["origin"]+"/")));
  var client=new Ce03LocalClient(new Uri((string)config["origin"]+"/"),(string)config["owner"],
   transport,new GameApiAccessToken((string)config["accessToken"]),
   Persist,File.Exists(journal)?File.ReadAllText(journal):null);
  Ce03OriginalStart accepted=null;bool alive=true;Task command=null;
  using var flow=new Ce03LocalDeparture(client,p=>JToken.DeepEquals(p,prepared),
   (s,p)=>Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,"prepared.json"),Ce03CommandContract.Canonical(new JObject{["selection"]=s,["prepared"]=p})),
   original=>accepted=original,()=>alive);
  flow.RestorePrepared(selection,prepared);
  bool restoredConfirmed=flow.RestoreAwaitingCheckpoint();
  string mode=(string)JObject.Parse(File.ReadAllText(Path.Combine(queue,"config.json")))["mode"];
  JObject priorStop=null;
  if(mode=="journal-resume"&&!client.HasPending){
   string stopPath=new[]{"checkpoint-saved.json","cleanup-stopped.json","confirmed-stopped.json"}.Select(name=>Path.Combine(folder,name)).FirstOrDefault(File.Exists);
   if(stopPath==null)throw new InvalidOperationException("Confirmed resume requires prior stop evidence");
   priorStop=JObject.Parse(File.ReadAllText(stopPath));
   var original=client.ReadRetainedStart((string)priorStop["key"]);
   if((int)priorStop["processId"]==System.Diagnostics.Process.GetCurrentProcess().Id||original.RunId!=(string)priorStop["runId"]||restoredConfirmed&&client.AwaitingCheckpointKey!=original.Key)throw new InvalidOperationException("Prior process/run/key differs");
   if(!restoredConfirmed){if(!File.Exists(Path.Combine(folder,"checkpoint-saved.json")))throw new InvalidOperationException("Unfinished checkpoint marker missing");flow.RestoreConfirmedStart(original.Key);restoredConfirmed=true;}
  }
  var formation=Ce03PlayerAssetContract.Validate((JObject)prepared["playerAssetBinding"],(JObject)prepared["ownedSelection"]);
  using var assets=binding.Acquire(Ce03PlayerAssetContract.ManifestHash,formation);
  var root=new GameObject("ActualDepartureCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
  root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(941,1672);
  DeparturePreparationView view=null;var notices=new JArray();bool repeatedInputChecked=false;
  void Preview(){if(alive)command=flow.PreviewAsync((string)selection["operationId"],(string)selection["risk"],(long)selection["expectedFormationRevision"],(long)selection["expectedLoadoutRevision"],(long)selection["expectedUltimateRevision"]);}
  void Start(){if(alive){if(flow.CanRetryEntry){flow.RetryEntry();command=Task.CompletedTask;}else command=flow.StartAsync();}}
  void Render(){if(alive&&view!=null){notices.Add(flow.Notice);view.Render("확정 구간",(string)selection["risk"],flow.Notice,null,"서버 견적",false,!flow.Busy,flow.CanStart||flow.CanRetryStart||flow.CanRetryEntry,false,false,flow.NeedsReceiptSave?"확정 응답 저장 다시 시도":flow.CanRetryStart?"다시 확인":flow.CanRetryEntry?"전투 준비 다시 시도":"출격");}}
  try{
   view=DeparturePreparationView.Create(root.transform,assets.ReadAssets().Font,assets.ReadAssets().Frame,()=>{if(flow.CanCancelUnsentStart)flow.CancelUnsentStart();},()=>{},()=>{},()=>{},()=>{},()=>{},Preview,Start,()=>{},()=>{});
   flow.Changed+=Render;Render();
   if(client.HasPending){
    var stopped=JObject.Parse(File.ReadAllText(Path.Combine(folder,"pending-stopped.json")));
    if((int)stopped["processId"]==System.Diagnostics.Process.GetCurrentProcess().Id||
     (string)stopped["pendingHash"]!=Ce03CommandContract.Sha(Ce03CommandContract.Canonical(JObject.Parse(client.JournalJson)["pending"])))throw new InvalidOperationException("New-process original pending differs");
    Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,"journal-restored.json"),Ce03CommandContract.Canonical(new JObject{["priorProcessId"]=stopped["processId"],["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["pendingHash"]=stopped["pendingHash"],["lookupSent"]=false}));
   }else if(!restoredConfirmed){
    var preview=view.GetComponentsInChildren<Button>().Single(b=>b.name=="PreviewDeparture");
    preview.onClick.Invoke();if(command==null)throw new InvalidOperationException("Preview callback not bound");await command;
    if(!flow.CanStart)throw new InvalidOperationException("Real departure preview unavailable: "+flow.Notice);
   }
   var start=view.GetComponentsInChildren<Button>().Single(b=>b.name=="ConfirmDeparture");
   if(!start.interactable)throw new InvalidOperationException("Departure input disabled");
   command=null;start.onClick.Invoke();if(command==null)throw new InvalidOperationException("Start callback not bound");var originalCommand=command;if(!originalCommand.IsCompleted){start.onClick.Invoke();repeatedInputChecked=true;}await originalCommand;
   if(initialFailure){
    if(!initialFailed||!client.HasUnstoredStart||!flow.CanCancelUnsentStart||transport.StartRequests!=0||accepted!=null)throw new InvalidOperationException("Initial request persistence failure dispatched or lost original");
    var attempted=JObject.Parse(attemptedPending);
    Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,"request-write-failure.json"),Ce03CommandContract.Canonical(new JObject{["key"]=attempted["key"],["bodyHash"]=Ce03CommandContract.Sha(Ce03CommandContract.Canonical(attempted["body"])),["startRequests"]=transport.StartRequests}));
    if(scenario=="request-write-failure-cancel"){
     view.GetComponentsInChildren<Button>().Single(b=>b.name=="BackDeparture").onClick.Invoke();
     if(client.HasPending||transport.StartRequests!=0)throw new InvalidOperationException("Unsent cancellation failed");
     Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,"request-cancelled.json"),Ce03CommandContract.Canonical(new JObject{["startRequests"]=transport.StartRequests,["pending"]=false,["key"]=attempted["key"]}));return null;
    }
    command=null;start.onClick.Invoke();if(command==null)throw new InvalidOperationException("Request save retry not bound");var retryCommand=command;if(!retryCommand.IsCompleted){start.onClick.Invoke();repeatedInputChecked=true;}await retryCommand;
    var stored=(JObject)JObject.Parse(client.JournalJson)["pending"];
    if(stored==null||!JToken.DeepEquals(stored["key"],attempted["key"])||!JToken.DeepEquals(stored["body"],attempted["body"]))throw new InvalidOperationException("Initial request changed during save retry");
   }
   if(injectStorageFailure){
    if(!storageFailed||accepted!=null||!client.HasPending||!flow.CanRetryStart||!flow.HasConfirmedStart||!flow.Notice.Contains("확정"))throw new InvalidOperationException("Storage failure lost original pending request");
    string pending=Ce03CommandContract.Canonical(JObject.Parse(File.ReadAllText(journal))["pending"]);
    if(pending!=Ce03CommandContract.Canonical(JObject.Parse(client.JournalJson)["pending"]))throw new InvalidOperationException("Disk pending differs from memory");
    Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,"receipt-write-failure.json"),Ce03CommandContract.Canonical(new JObject{["pendingHash"]=Ce03CommandContract.Sha(pending),["acceptedViewEntries"]=0,["recovery"]=stopAfterFailure?"STOP_BEFORE_NEW_PROCESS":"EXPLICIT_EXISTING_BUTTON_SAME_PROCESS"}));
    if(stopAfterFailure){
     Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,"pending-stopped.json"),Ce03CommandContract.Canonical(new JObject{["pendingHash"]=Ce03CommandContract.Sha(pending),["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["key"]=JObject.Parse(pending)["key"]}));
     return null;
    }
    command=null;start.onClick.Invoke();if(command==null)throw new InvalidOperationException("Recovery callback not bound");await command;
   }
   if(accepted==null||client.HasPending||flow.EntryStatus!=Ce03EntryStatus.Entered)throw new InvalidOperationException("UI START did not resolve: "+flow.Notice);
   if(priorStop!=null){
    if(accepted.Key!=(string)priorStop["key"]||accepted.RunId!=(string)priorStop["runId"]||transport.StartRequests!=0)throw new InvalidOperationException("Confirmed recovery changed run or sent START");
    Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,"confirmed-restored.json"),Ce03CommandContract.Canonical(new JObject{["priorProcessId"]=priorStop["processId"],["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["key"]=accepted.Key,["runId"]=accepted.RunId,["startRequests"]=transport.StartRequests}));
   }
   if(scenario=="confirmed-receipt-stop"&&!restoredConfirmed){
    Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,"confirmed-stopped.json"),Ce03CommandContract.Canonical(new JObject{["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["key"]=accepted.Key,["runId"]=accepted.RunId,["journalHash"]=Ce03CommandContract.Sha(File.ReadAllText(journal))}));return null;
   }
   var reloaded=new Ce03LocalClient(new Uri((string)config["origin"]+"/"),(string)config["owner"],new Ce03LoopbackTransport(new Uri((string)config["origin"]+"/")),new GameApiAccessToken((string)config["accessToken"]),_=>throw new InvalidOperationException("Read-only reopen"),File.ReadAllText(journal));
   if(reloaded.ReadRetainedStart(accepted.Key).Json!=accepted.Json)throw new InvalidOperationException("Durable UI receipt differs");
   Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,"original-start.json"),accepted.Json);
   Ce03AcceptedPlayerTrial.Atomic(Path.Combine(queue,"entry-"+index+".json"),Ce03CommandContract.Canonical(new JObject{["runId"]=accepted.RunId,["key"]=accepted.Key}));
   Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,"ui-entry.json"),Ce03CommandContract.Canonical(new JObject{["transport"]="UnityWebRequest",["input"]="EXISTING_VIEW_BUTTON_CALLBACK",["journalReopened"]=true,["logicalStartRequests"]=transport.StartRequests,["notices"]=notices,["repeatedInputChecked"]=repeatedInputChecked,["runId"]=accepted.RunId,["key"]=accepted.Key}));
  }finally{alive=false;flow.Changed-=Render;UnityEngine.Object.Destroy(root);}
  await Task.Delay(100);
  return client;
 }
}
}
#endif
