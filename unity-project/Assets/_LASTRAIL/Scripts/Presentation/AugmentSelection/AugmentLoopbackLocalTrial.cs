#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.AugmentSelection {
 // LOCAL HTTP acquisition consumer only. It cannot manufacture the missing
 // run-bound native combat source or turn server acquisition into APPLIED.
 public sealed class AugmentLoopbackLocalTrial:IDisposable {
  readonly UnityWebRequestRunApiHttpAdapter adapter;
  readonly AugmentSelectionScope scope;
  readonly AugmentSelectionFileJournal journal;
  readonly CombatO22ApplicationStore store;
  readonly GameApiAuthSessionSnapshot session;
  readonly RecordedWire wire;
  readonly string directory,startHash;
  readonly AugmentSelectionSnapshot initial;
  readonly AugmentCardPresentation card;
  readonly bool freeRerolls;
  AugmentSelectionRuntime runtime;
  bool disposed;
  public AugmentLoopbackLocalTrial(string bootstrapPath,string directory){
   BMeasuredAcquisitionLocalFlow.ValidateDirectory(directory);this.directory=directory;
   if(!Path.IsPathRooted(bootstrapPath)||!Path.GetFullPath(bootstrapPath).StartsWith("D:\\Project\\LASTRAIL\\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("LOCAL bootstrap path required");
   var b=SelectionJson.Object(File.ReadAllText(bootstrapPath));
   if((string)b["version"]!="augment.loopback-http.v1.20261021"||(string)b["classification"]!="EXPLICIT_LOCAL_SYNTHETIC_NOT_OPERATING"||(string)b["environment"]!="DEV"||(string)b["effectId"]!="O22")throw new ArgumentException("Unsupported LOCAL bootstrap");
   var uri=new Uri((string)b["baseUrl"]);if(uri.Scheme!="http"||uri.Host!="127.0.0.1"||uri.AbsolutePath!="/"||uri.UserInfo!=""||uri.Query!=""||uri.Fragment!="")throw new ArgumentException("Only explicit loopback HTTP is permitted");
   startHash=(string)b["startSnapshotHash"];if(BasicCombatSimulation.Digest((string)b["startSnapshotJson"])!=startHash)throw new ArgumentException("Original START hash differs");
   scope=new AugmentSelectionScope(new AugmentSelectionOwner((string)b["accountId"],(string)b["environment"]),(string)b["runId"],(string)b["eventId"]);
   initial=AugmentSelectionSnapshot.Parse(b["initialSelection"].ToString());if(initial.RunId!=scope.RunId||initial.EventId!=scope.EventId)throw new ArgumentException("Original grant differs");
   freeRerolls=Environment.GetCommandLineArgs().Contains("--lastrail-http-free-reroll");
   if(freeRerolls&&((string)b["rerollVersion"]!=AugmentSelectHttpPort.FreeRerollVersion||(string)b["rerollPath"]!="/v1/runs/"+Uri.EscapeDataString(scope.RunId)+"/augment-grants/"+Uri.EscapeDataString(scope.EventId)+"/reroll"))throw new ArgumentException("Original FREE reroll route differs");
   // This supplied readout has no resolved actor names. Do not infer targets.
   var readout=(JObject)b["targetReadout"];if((string)readout?["version"]!="augment.target-readout.v1.20261021"||(string)readout["runId"]!=initial.RunId||(string)readout["eventId"]!=initial.EventId||(string)readout["catalogVersion"]!=initial.CatalogVersion||(long?)readout["grantRevision"]!=initial.GrantRevision)throw new ArgumentException("Target readout binding differs");
   var row=((JArray)readout["cards"]).Single(x=>(string)x["candidateId"]=="O22");
   if((string)row["scope"]!="OPERATOR"||(string)row["definitionStatus"]!="AVAILABLE"||row["targets"]?.Type!=JTokenType.Null||(string)row["targetResolution"]!="SOURCE_NOT_SUPPLIED")throw new ArgumentException("Unsupported target source");
   card=HealingAugmentPresentation.FromPinned(initial.CatalogVersion,(JObject)row["definition"],null)??throw new ArgumentException("Pinned display definition unavailable");
   var expiry=DateTimeOffset.FromUnixTimeMilliseconds((long)b["tokenExpiresAt"]).UtcDateTime;
   if(expiry<=DateTime.UtcNow)throw new ArgumentException("LOCAL token expired; reopen original server and retain the request journal");
   session=new GameApiAuthSessionSnapshot(scope.Owner.AccountId,"ACTIVE",new GameApiAccessToken((string)b["accessToken"]),expiry,new GameApiRefreshToken("LOCAL-no-refresh-route"),expiry);
   string originalPath=Path.Combine(directory,"http-original.json"),journalPath=Path.Combine(directory,"http-selection-journal.json");
   store=new CombatO22ApplicationStore(originalPath,()=>Read(originalPath),raw=>Write(originalPath,raw));
   journal=new AugmentSelectionFileJournal(scope,journalPath,()=>Read(journalPath),raw=>Write(journalPath,raw));
   adapter=new UnityWebRequestRunApiHttpAdapter(uri.GetLeftPart(UriPartial.Authority),30);
   wire=new RecordedWire(new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(adapter)),directory);
  }
  public async Task RunAsync(Transform parent,TMP_FontAsset font,bool auto,Func<string,CancellationToken,Task> capture,Action<string> display,CancellationToken cancellation){
   var view=AugmentSelectionView.Create(parent,font);var rect=(RectTransform)view.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(-450,-100);
   runtime=new AugmentSelectionRuntime(view,scope,wire,()=>session,()=>!disposed,journal,store,startHash,(version,id)=>version==initial.CatalogVersion&&id=="O22"?card:null,null,freeRerolls);
   var retained=store.Load(scope.Owner.AccountId,scope.RunId,scope.EventId,scope.Owner.Environment,startHash);
   await runtime.OpenAsync(retained==null?initial:AugmentSelectionSnapshot.Parse(retained.SelectionJson));cancellation.ThrowIfCancellationRequested();display("Real loopback HTTP SELECT · original run retained · combat source not supplied");
   string process=System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
   await capture("http-selection-"+process+".png",cancellation);
   Button Find(string name)=>view.GetComponentsInChildren<Button>(true).Single(x=>x.name==name);
   bool freeUnchanged=false;
   if(auto&&freeRerolls&&!runtime.Completed&&!runtime.CommandPending){
    string before=runtime.CurrentSelection.Json;Find("Free0").onClick.Invoke();
    bool retriedFree=false;for(int i=0;runtime.CommandPending;i++){if(i>=600)throw new TimeoutException("FREE reroll unresolved; original journal retained");await Task.Delay(100,cancellation);var retry=Find("Retry");if(!retriedFree&&retry.gameObject.activeInHierarchy&&retry.interactable){retriedFree=true;retry.onClick.Invoke();}}
    freeUnchanged=runtime.CurrentSelection.Json==before;
    if(!freeUnchanged||wire.Rerolls<1||wire.Rerolls>2)throw new InvalidOperationException("Single O22 trial must retain candidate, revision and free count");
    await capture("http-free-no-candidate-"+process+".png",cancellation);
   }
   if(!runtime.Completed){Find("Details0").onClick.Invoke();await capture("http-details-"+process+".png",cancellation);Find("DetailClose").onClick.Invoke();}
   if(auto&&!runtime.Completed){var retry=Find("Retry");if(retry.gameObject.activeInHierarchy&&retry.interactable)retry.onClick.Invoke();else Find("Select0").onClick.Invoke();}
   bool retried=false;
   for(int i=0;!runtime.Completed;i++){
    cancellation.ThrowIfCancellationRequested();if(i>=600)throw new TimeoutException("Original HTTP selection did not complete; journal retained");await Task.Delay(100,cancellation);
    var retry=Find("Retry");if(auto&&!retried&&retry.gameObject.activeInHierarchy&&retry.interactable){retried=true;retry.onClick.Invoke();}
   }
   var original=store.Load(scope.Owner.AccountId,scope.RunId,scope.EventId,scope.Owner.Environment,startHash)??throw new InvalidOperationException("Original acquisition not persisted");
   if(journal.Load(scope.StorageKey)!=null||original.Applied)throw new InvalidOperationException("Acquisition boundary differs");
   int calls=wire.Calls;runtime.Dispose();
   runtime=new AugmentSelectionRuntime(view,scope,wire,()=>session,()=>!disposed,journal,store,startHash,(version,id)=>card);
   await runtime.OpenAsync(AugmentSelectionSnapshot.Parse(original.SelectionJson));
   if(!runtime.Completed||wire.Calls!=calls)throw new InvalidOperationException("Reopening acquisition issued another request");
   display("Server acquisition persisted and reopened · APPLIED/combat/result pending matching native source");await capture("http-acquired-"+process+".png",cancellation);
   Write(Path.Combine(directory,"http-player-"+System.Diagnostics.Process.GetCurrentProcess().Id+".json"),new JObject{["classification"]="REAL_LOOPBACK_HTTP_SYNTHETIC_START_ACQUISITION_ONLY",["runId"]=scope.RunId,["startSnapshotHash"]=startHash,["applicationId"]=original.ApplicationId,["postCount"]=wire.Calls,["rerollPostCount"]=wire.Rerolls,["selectPostCount"]=wire.Calls-wire.Rerolls,["singleCandidateFreeUnchanged"]=freeUnchanged,["getCount"]=0,["reopenRequests"]=wire.Calls-calls,["retainedOriginalReused"]=retained!=null,["droppedResponses"]=wire.Dropped,["droppedFreeResponses"]=wire.FreeDropped,["completionState"]=runtime.CompletionState,["runtimeApplied"]=false,["nativeCombatSourceSupplied"]=false,["physicalPointerInput"]=false,["automaticSelection"]=auto,["targetsSupplied"]=false}.ToString());
  }
  static string Read(string path)=>File.Exists(path)?File.ReadAllText(path):null;
  static void Write(string path,string raw){string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write)){byte[] data=new UTF8Encoding(false).GetBytes(raw);f.Write(data,0,data.Length);f.Flush(true);}if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public void Dispose(){if(disposed)return;disposed=true;runtime?.Dispose();adapter?.Dispose();}
  sealed class RecordedWire:IGameApiAuthenticatedHttpTransport {
   readonly IGameApiAuthenticatedHttpTransport inner;readonly string directory;public int Calls{get;private set;}public int Dropped{get;private set;}public int Rerolls{get;private set;}public int FreeDropped{get;private set;}
   public RecordedWire(IGameApiAuthenticatedHttpTransport inner,string directory){this.inner=inner;this.directory=directory;}
   public async Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request,GameApiAccessToken token){
    if(request.Method!="POST")throw new InvalidOperationException("Only original SELECT/FREE POST is supported");Calls++;
    bool reroll=request.EndpointPath.EndsWith("/reroll",StringComparison.Ordinal);if(reroll)Rerolls++;
    var result=await inner.SendAsync(request,token);
    if(result?.Response?.BodyJson!=null)Write(Path.Combine(directory,"http-response-"+System.Diagnostics.Process.GetCurrentProcess().Id+"-"+Calls+".json"),result.Response.BodyJson);
    if(reroll&&FreeDropped==0&&result?.Response?.StatusCode==200&&Environment.GetCommandLineArgs().Contains("--lastrail-http-drop-free-response")){FreeDropped++;Dropped++;throw new IOException("Explicit LOCAL FREE response-loss probe after real server commit");}
    if(!reroll&&Dropped-FreeDropped==0&&result?.Response?.StatusCode==200&&Environment.GetCommandLineArgs().Contains("--lastrail-http-drop-first-response")){Dropped++;throw new IOException("Explicit LOCAL response-loss probe after real server commit");}
    return result;
   }
  }
 }
}
#endif
