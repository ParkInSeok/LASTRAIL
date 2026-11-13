#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Diagnostics {
 // Explicit LOCAL fixture. It exercises the real View/owners/Unity HTTP adapter;
 // it does not authenticate against the game server or run an economic command.
 public sealed partial class DataLifecycleLocalPlayerProbe:MonoBehaviour {
  [SerializeField] TextAsset responses;[SerializeField] TMP_FontAsset font;[SerializeField] string variant;
  public void Initialize(TextAsset source,TMP_FontAsset supplied,string label){responses=source;font=supplied;variant=label;}
  GameObject canvas;SceneMemoryRecorder memory;AccountScreenRuntime state;UnityWebRequestRunApiHttpAdapter adapter;Server server;
  async void Start(){
   string directory=Environment.GetEnvironmentVariable("LASTRAIL_DATA_LIFECYCLE_EVIDENCE");
   if(string.IsNullOrWhiteSpace(directory))return;
   try{
    if(!Path.GetFullPath(directory).Replace('\\','/').StartsWith("D:/Project/LASTRAIL/QA/",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Explicit D QA directory required");
    Directory.CreateDirectory(directory);Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
    string bootstrap=Environment.GetEnvironmentVariable("LASTRAIL_DATA_SERVER_BOOTSTRAP");
    if(!string.IsNullOrEmpty(bootstrap)){await RunServerTrial(directory,bootstrap);await CompleteArchiveLifetimeDiagnostic(directory);Cleanup();Application.Quit(0);return;}
    memory=SceneMemoryRecorder.CreateExternal(directory);memory.BeginWindow();await Task.Delay(1000);memory.Checkpoint("00-resident");
    server=new Server(JObject.Parse(responses.text));adapter=new UnityWebRequestRunApiHttpAdapter(server.Url,10);
    var wire=new Wire(new UnityWebRequestRunApiHttpTransport(adapter));var session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddHours(2));long generation=1;
    state=new AccountScreenRuntime(wire,null,()=>session,()=>generation);
    memory.BeginWindow();await state.EnsureSharedAsync();await state.StoryArchive.RefreshAsync();
    if(state.AccountStatus!=AccountStateReadStatus.Ready)throw new InvalidOperationException("Fixture account unavailable");
    canvas=new GameObject("LocalDataLifecycleCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
    var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(941,1672);scale.matchWidthOrHeight=1;
    var pages=LobbyDestinationPages.Create(canvas.transform,font,()=>{});pages.BindAccount(state);Action changed=()=>{if(pages!=null)pages.RefreshAccountView(state);};state.Changed+=changed;
    await Task.Delay(1000);memory.Checkpoint("01-injected-session-initial-reads");
    for(int cycle=0;cycle<5;cycle++){
     memory.BeginWindow();pages.Open(LobbyPage.Train);await Ready(()=>!state.TrainLoadoutLoading&&state.AccountStatus!=AccountStateReadStatus.Loading);Require(state.TrainLoadoutReady&&state.AccountStatus==AccountStateReadStatus.Ready,"Train read failed");await Task.Delay(250);pages.Close();
     pages.Open(LobbyPage.Formation);await Ready(()=>!state.UltimateLoading&&state.AccountStatus!=AccountStateReadStatus.Loading);Require(state.UltimateReady&&state.AccountStatus==AccountStateReadStatus.Ready,"Formation read failed");await Task.Delay(250);pages.Close();
     var story=StoryArchiveView.Create(canvas.transform,font,null,state.StoryArchive,()=>{});await Ready(()=>state.StoryArchive.Status!=LastRail.Narrative.StoryArchiveReadStatus.Loading);Require(state.StoryArchive.Status==LastRail.Narrative.StoryArchiveReadStatus.Ready&&story.CurrentState.CanOpen,"Story read failed");await Task.Delay(250);Destroy(story.gameObject);
     await Task.Delay(1000);memory.Checkpoint("cycle-"+cycle+"-return");
    }
    memory.BeginWindow();await state.RefreshAccountAsync();await Task.Delay(250);memory.Checkpoint("07-explicit-read-not-command");
    memory.BeginWindow();session=null;generation++;state.SynchronizeSession();await Task.Delay(250);memory.Checkpoint("08-authentication-invalidated");
    state.Changed-=changed;state.Dispose();state=null;Destroy(canvas);canvas=null;
    memory.BeginWindow();await Task.Delay(1500);memory.Checkpoint("09-references-released-normal-gc");
    File.WriteAllText(Path.Combine(directory,"result.json"),new JObject{["status"]="Passed",["variant"]=variant,["classification"]="ACTUAL_PLAYER_UNITY_HTTP_TO_SYNTHETIC_RESPONSE_FIXTURE_NOT_GAME_SERVER",["responseSha256"]=LastRail.Battle.BasicCombatSimulation.Digest(responses.text),["calls"]=new JArray(wire.Rows),["postCount"]=0,["retryCount"]=0,["localStoreReads"]=0,["forcedGC"]=false,["gcReclamationTimeMeasured"]=false,["textureCpuGpuSeparationMeasured"]=false,["notExecuted"]=new JArray("real login","recruitment","formation/growth/equipment command","run/augment/result/claim","DEV server diagnostic UI","mobile input"),["unity"]=Application.unityVersion,["graphicsApi"]=SystemInfo.graphicsDeviceType.ToString(),["screen"]=new JArray(Screen.width,Screen.height)}.ToString());
    Cleanup();Application.Quit(0);
   }catch(Exception e){if(!string.IsNullOrWhiteSpace(directory)&&Directory.Exists(directory))File.WriteAllText(Path.Combine(directory,"failure.txt"),e.ToString());Cleanup();Application.Quit(1);}
  }
  async Task RunServerTrial(string directory,string bootstrapPath){
   if(!Path.GetFullPath(bootstrapPath).Replace('\\','/').StartsWith("D:/Project/LASTRAIL/QA/CommanderS1-20260922/",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Explicit server trial required");
   var bootstrap=JObject.Parse(File.ReadAllText(bootstrapPath));var url=new Uri((string)bootstrap["baseUrl"]);
   Require(url.Scheme=="http"&&url.Host=="127.0.0.1"&&(string)bootstrap["persistence"]=="MEMORY_ONLY_RESTART_IS_NEW_TRIAL","Unexpected server boundary");
   if(Environment.GetEnvironmentVariable("LASTRAIL_RUN_CONFIRM_TRIAL")=="1"){await RunConfirmTrial(directory,bootstrap);return;}
   bool countersOnly=Environment.GetEnvironmentVariable("LASTRAIL_MEMORY_COUNTERS_ONLY")=="1";
   bool collectDiagnostic=Environment.GetEnvironmentVariable("LASTRAIL_MEMORY_COLLECT_DIAGNOSTIC")=="1";
   memory=SceneMemoryRecorder.CreateExternal(directory,!countersOnly);memory.BeginWindow();await Task.Delay(1000);memory.Checkpoint("00-resident");
   adapter=new UnityWebRequestRunApiHttpAdapter(url.ToString().TrimEnd('/'),10);var wire=new Wire(new UnityWebRequestRunApiHttpTransport(adapter));
   var session=new GameApiAuthSessionSnapshot((string)bootstrap["accountId"],"ACTIVE",new GameApiAccessToken((string)bootstrap["accessToken"]),DateTime.UtcNow.AddMinutes(10),new GameApiRefreshToken("qa-not-a-login-flow"),DateTime.UtcNow.AddMinutes(15));long generation=1;
   bool localSaveTrial=Environment.GetEnvironmentVariable("LASTRAIL_DATA_LOCAL_SAVE_FAILURE")=="1";
   var journal=new TrialJournal{FailRemove=localSaveTrial};
   state=new AccountScreenRuntime(wire,journal,()=>session,()=>generation);
   await state.EnsureAccountAsync();await state.EnsureGrowthAsync();Require(state.AccountStatus==AccountStateReadStatus.Ready&&state.GrowthReady,"Actual server owner read failed");
   canvas=new GameObject("ActualServerDataLifecycleCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
   var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(941,1672);scale.matchWidthOrHeight=1;
   var pages=LobbyDestinationPages.Create(canvas.transform,font,()=>{});pages.BindAccount(state);Action changed=()=>{if(pages!=null)pages.RefreshAccountView(state);};state.Changed+=changed;
   await Task.Delay(1000);memory.Checkpoint("01-authenticated-owners");
   for(int cycle=0;cycle<5;cycle++){
    memory.BeginWindow();pages.Open(LobbyPage.Train);await Ready(()=>!state.TrainLoadoutLoading&&state.AccountStatus!=AccountStateReadStatus.Loading);Require(state.TrainLoadoutReady,"Server train unavailable");await Task.Delay(250);pages.Close();
    pages.Open(LobbyPage.Formation);await Ready(()=>!state.UltimateLoading&&state.AccountStatus!=AccountStateReadStatus.Loading);Require(state.UltimateReady,"Server ultimate unavailable");await Task.Delay(250);pages.Close();await Task.Delay(500);memory.Checkpoint("cycle-"+cycle+"-return");
   }
   bool fastNavigation=Environment.GetEnvironmentVariable("LASTRAIL_DATA_FAST_NAVIGATION")=="1";
   JArray fastNavigationCalls=null;
   if(fastNavigation){
    int before=wire.Rows.Count;pages.Open(LobbyPage.Formation);
    var oldInput=pages.GetComponentsInChildren<Button>().Single(b=>b.name=="FormationSlot3").onClick;
    pages.Close();pages.Open(LobbyPage.Train);pages.Close();pages.Open(LobbyPage.Formation);
    oldInput.Invoke();Require(pages.SelectedSlot==0,"Old entry changed reopened selection");
    pages.GetComponentsInChildren<Button>().Single(b=>b.name=="FormationSlot2").onClick.Invoke();Require(pages.SelectedSlot==2,"Current input failed");
    pages.Close();await Ready(()=>!state.WalletLoading&&state.AccountStatus!=AccountStateReadStatus.Loading&&!state.TrainLoadoutLoading&&!state.UltimateLoading);
    fastNavigationCalls=new JArray(wire.Rows.Skip(before));
    // The local server explicitly does not support wallet reads. A 404 supplies
    // no reusable wallet state; retain those attempts as unsupported evidence.
    Require(fastNavigationCalls.All(r=>(string)r["path"]=="/v1/wallet/crystals"&&(int?)r["status"]==404),"Valid rapid re-entry made extra HTTP");
   }
   var command=(JObject)bootstrap["formationCommand"]["body"];bool legacy=variant=="FORMATION_BASELINE_PRE_V2";
   if(legacy)command.Remove("responseVersion");
   memory.BeginWindow();int commandStart=wire.Rows.Count;await state.WriteFormationAsync("formation-save","p",command.ToString());
   int localRetryCalls=0;
   if(localSaveTrial){
    Require(state.LastFormationReceipt!=null&&state.Pending!=null,"Expected verified save with failed local cleanup");
    int retryStart=wire.Rows.Count;await state.RetryPendingAsync();journal.FailRemove=false;await state.RetryPendingAsync();localRetryCalls=wire.Rows.Count-retryStart;
    if(variant.IndexOf("BASELINE",StringComparison.Ordinal)<0)Require(localRetryCalls==0,"Local cleanup retried HTTP");
   }
   Require(state.LastFormationReceipt!=null&&state.Pending==null,"Server save was not confirmed");
   Require(state.AccountStatus==AccountStateReadStatus.Ready&&state.Account.Presets.Any(p=>p.Text("id")=="p"&&p.Number("revision")==2),"Final preset missing");
   await Task.Delay(500);memory.Checkpoint("07-confirmed-save");var commandRows=new JArray(wire.Rows.Skip(commandStart));
   var ownerWeak=new WeakReference(state);var pagesWeak=new WeakReference(pages);
   memory.BeginWindow();session=null;generation++;state.SynchronizeSession();Require(state.Account==null&&state.TrainLoadout==null&&state.Ultimate==null,"Old account references retained");
   state.Changed-=changed;state.Dispose();state=null;Destroy(canvas);canvas=null;await Task.Delay(1500);memory.Checkpoint("09-references-released-normal-gc");
   var reachability=new JObject{["afterNaturalWait"]=new JObject{["ownerAlive"]=ownerWeak.IsAlive,["destroyedPagesManagedWrapperAlive"]=pagesWeak.IsAlive}};
   if(collectDiagnostic){
    GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();memory.Checkpoint("10-diagnostic-gc-probe-locals-retained",false);
    reachability["afterDiagnosticGcLocalsRetained"]=new JObject{["ownerAlive"]=ownerWeak.IsAlive,["destroyedPagesManagedWrapperAlive"]=pagesWeak.IsAlive};
    pages=null;changed=null;bootstrap=null;command=null;journal=null;
    GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();memory.Checkpoint("11-diagnostic-gc-probe-locals-released",false);
    reachability["afterDiagnosticGcLocalsReleased"]=new JObject{["ownerAlive"]=ownerWeak.IsAlive,["destroyedPagesManagedWrapperAlive"]=pagesWeak.IsAlive};
   }
   File.WriteAllText(Path.Combine(directory,"memory-diagnostic.json"),new JObject{["countersOnly"]=countersOnly,["diagnosticGcAfterNaturalMeasurements"]=collectDiagnostic,["reachability"]=reachability,["scope"]="Weak references identify survival, not full heap root graph; source chain inspected separately. Diagnostic GC is opt-in QA only."}.ToString());
   File.WriteAllText(Path.Combine(directory,"result.json"),new JObject{["status"]="Passed",["variant"]=variant,["classification"]="ACTUAL_PLAYER_AUTHENTICATED_HTTP_REAL_SERVER_APP_SYNTHETIC_SQL_MEMORY_ONLY",["calls"]=new JArray(wire.Rows),["commandCalls"]=commandRows,["fastNavigationChecked"]=fastNavigation,["fastNavigationCalls"]=fastNavigationCalls,["localSaveFailureInjected"]=localSaveTrial,["localRetryHttpCalls"]=localRetryCalls,["forcedGC"]=collectDiagnostic,["naturalMeasurementsBeforeDiagnosticGc"]=true,["journalPersistence"]="MEMORY_ONLY",["gcReclamationTimeMeasured"]=false,["textureCpuGpuSeparationMeasured"]=false,["notExecuted"]=new JArray("login flow","story endpoint not in server allowlist","recruitment interaction","run/augment/result/claim","process restart recovery","mobile","cache pressure"),["unity"]=Application.unityVersion,["screen"]=new JArray(Screen.width,Screen.height)}.ToString());
  }
  sealed class TrialJournal:IAccountActionJournal {
   PendingAccountAction action;public bool FailRemove;
   public PendingAccountAction Read(string account)=>action?.Account==account?action:null;
   public bool Save(PendingAccountAction value){if(action!=null)return false;action=value;return true;}
   public bool Remove(PendingAccountAction value){if(FailRemove||action!=value)return false;action=null;return true;}
  }
  static async Task Ready(Func<bool> ready){for(int i=0;!ready();i++){if(i>=200)throw new TimeoutException("Fixture read did not settle");await Task.Delay(50);}}
  static void Require(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
  void Cleanup(){state?.Dispose();state=null;adapter?.Dispose();adapter=null;server?.Dispose();server=null;if(memory!=null)Destroy(memory.gameObject);if(canvas!=null)Destroy(canvas);}
  void OnDestroy(){Cleanup();}
  sealed class Wire:IRunApiHttpTransport {
   public WeakReference ClaimResponse,ClaimBody;readonly IRunApiHttpTransport inner;public readonly List<JObject> Rows=new List<JObject>();
   public Wire(IRunApiHttpTransport inner){this.inner=inner;}
   public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){if(Rows.Count>=200)throw new InvalidOperationException("Bounded fixture trace exceeded");var row=new JObject{["method"]=request.Method,["path"]=request.EndpointPath,["clientMonotonicSeconds"]=Time.realtimeSinceStartupAsDouble};Rows.Add(row);var response=await inner.SendAsync(request);if(request.Method=="POST"&&request.EndpointPath.EndsWith("/claim",StringComparison.Ordinal)){ClaimResponse=new WeakReference(response);ClaimBody=new WeakReference(response.BodyJson);}row["status"]=response.StatusCode;row["networkError"]=response.IsNetworkError;row["responseBytes"]=Encoding.UTF8.GetByteCount(response.BodyJson??"");return response;}
  }
  sealed class Server:IDisposable {
   readonly TcpListener listener;readonly Task worker;volatile bool stopping;public string Url{get;}
   public Server(JObject packet){listener=new TcpListener(IPAddress.Loopback,0);listener.Start();Url="http://127.0.0.1:"+((IPEndPoint)listener.LocalEndpoint).Port;
    worker=Task.Run(async()=>{while(!stopping){TcpClient connection;try{connection=await listener.AcceptTcpClientAsync();}catch(SocketException)when(stopping){break;}catch(ObjectDisposedException)when(stopping){break;}
     using(connection){using var stream=connection.GetStream();using var reader=new StreamReader(stream,Encoding.ASCII,false,1024,true);string first=await reader.ReadLineAsync(),header;do{header=await reader.ReadLineAsync();}while(!string.IsNullOrEmpty(header));var parts=first.Split(' ');string body=parts[0]=="GET"?(string)packet[parts[1]]:null;int code=body==null?404:200;byte[] bytes=Encoding.UTF8.GetBytes(body??"{}");byte[] prefix=Encoding.ASCII.GetBytes("HTTP/1.1 "+code+" OK\r\nContent-Type: application/json\r\nContent-Length: "+bytes.Length+"\r\nConnection: close\r\n\r\n");await stream.WriteAsync(prefix,0,prefix.Length);await stream.WriteAsync(bytes,0,bytes.Length);}}
    });
   }
   public void Dispose(){if(stopping)return;stopping=true;listener.Stop();worker.GetAwaiter().GetResult();}
  }
 }
}
#endif
