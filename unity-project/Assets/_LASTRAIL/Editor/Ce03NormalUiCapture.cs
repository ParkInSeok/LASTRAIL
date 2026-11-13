#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Explicit local UI preparation/START harness. No fixture approval or direct finish sender.
[InitializeOnLoad]
public static class Ce03NormalUiCapture {
 const string Key="LastRail.Ce03NormalUiCapture";
 static Ce03NormalUiCapture(){EditorApplication.playModeStateChanged+=Changed;}
 public static string Arg(string name){var a=Environment.GetCommandLineArgs();var n=Array.IndexOf(a,name);if(n<0||n+1>=a.Length)throw new ArgumentException("Missing explicit local QA argument");return a[n+1];}
 public static JObject Read(string path){using(var r=new JsonTextReader(new StringReader(File.ReadAllText(path))){DateParseHandling=DateParseHandling.None})return JObject.Load(r,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});}
 public static void Atomic(string path,string raw){var temporary=path+".pending";using(var s=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){var bytes=new System.Text.UTF8Encoding(false,true).GetBytes(raw);s.Write(bytes,0,bytes.Length);s.Flush(true);}if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);if(File.ReadAllText(path)!=raw)throw new IOException("QA durable readback differs");}
 public static void Run(){if(!Application.isBatchMode)throw new InvalidOperationException("Explicit batch QA only");var stage=Arg("-ce03UiStage");if(stage!="prepare"&&stage!="start"&&stage!="resume"&&stage!="claim"&&stage!="visual")throw new ArgumentException("Unsupported stage");Directory.CreateDirectory(Arg("-ce03UiOutput"));EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("CE03 normal local UI").AddComponent<Ce03NormalUiRecorder>();if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetInt(Key+".exit",1));}}
 public static void Finish(bool success){SessionState.SetInt(Key+".exit",success?0:1);EditorApplication.ExitPlaymode();}
}
public sealed partial class Ce03NormalUiRecorder:MonoBehaviour {
 string output,stage,bundleHash;JObject connection;AccountScreenRuntime account;Ce03LocalDeparture flow;Ce03LocalClient client;LobbyCommandDeck deck;EventSystem events;GameObject canvas;
 Camera camera;RenderTexture target;Texture2D pixels;System.Diagnostics.Process encoder;int frames,visibleFrames;bool recording;
 string SessionPath(string name)=>Path.Combine((string)connection["sessionDirectory"],name);
 static string Canon(JToken token)=>Ce03CommandContract.Canonical(token);
 async void Start(){try{await Execute();}catch(Exception e){if(output!=null)Ce03NormalUiCapture.Atomic(Path.Combine(output,stage+"-failure.json"),Canon(new JObject{["type"]=e.GetType().Name,["message"]="Local UI stage failed; no finish was sent."}));Debug.LogError("CE03 normal UI failed: "+e.GetType().Name+" "+e.Message+"\n"+e.StackTrace);StopVideo();Ce03NormalUiCapture.Finish(false);}}
 async Task Execute(){
  output=Ce03NormalUiCapture.Arg("-ce03UiOutput");stage=Ce03NormalUiCapture.Arg("-ce03UiStage");if(stage=="visual"){var visualSources=Ce03NormalUiCapture.Read(Ce03NormalUiCapture.Arg("-ce03SourceBundle"));Ce03ConsumerEvidenceRunner.VerifySources(visualSources);bundleHash=Ce03CommandContract.Sha(Canon(visualSources));BuildCamera();await VisualReplay(visualSources);Ce03NormalUiCapture.Finish(true);return;}connection=Ce03NormalUiCapture.Read(Ce03NormalUiCapture.Arg("-ce03Connection"));
  if((bool?)connection["disposable"]!=true||(string)connection["dataLifetime"]!="PROCESS"||DateTimeOffset.FromUnixTimeMilliseconds((long)connection["expiresAt"])<=DateTimeOffset.UtcNow)throw new InvalidOperationException("Local connection expired");
  string bundle=File.ReadAllText(Ce03NormalUiCapture.Arg("-ce03SourceBundle"));var sources=Ce03NormalUiCapture.Read(Ce03NormalUiCapture.Arg("-ce03SourceBundle"));if(Canon(sources)!=bundle)throw new FormatException("Noncanonical source");Ce03ConsumerEvidenceRunner.VerifySources(sources);bundleHash=Ce03CommandContract.Sha(bundle);
  var origin=new Uri(((string)connection["origin"]).TrimEnd('/')+"/");var wire=new Ce03LoopbackTransport(origin);string authorization=(string)connection["authorization"];if(!authorization.StartsWith("Bearer ",StringComparison.Ordinal))throw new FormatException("Bearer bootstrap required");var token=new GameApiAccessToken(authorization.Substring(7));
  var expiry=DateTimeOffset.FromUnixTimeMilliseconds((long)connection["expiresAt"]).UtcDateTime;
  if(stage=="resume"){BuildCamera();await Battle(true,sources,origin);Ce03NormalUiCapture.Finish(true);return;}
  var session=new GameApiAuthSessionSnapshot((string)connection["accountId"],"ACTIVE",token,expiry,new GameApiRefreshToken("explicit-local-no-refresh"),expiry);
  account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
  await Task.WhenAll(account.RefreshSharedAsync(),account.RefreshTrainLoadoutAsync(),account.RefreshUltimateAsync());if(stage!="claim"&&!account.CanPreviewDeparture)throw new InvalidOperationException("Actual local account hydration incomplete");
  if(stage=="claim"){await Claim(wire,session,sources);Ce03NormalUiCapture.Finish(true);return;}
  var journal=SessionPath("ui-client-journal.json");client=new Ce03LocalClient(origin,session.AccountId,wire,token,raw=>Ce03NormalUiCapture.Atomic(journal,raw),File.Exists(journal)?File.ReadAllText(journal):null);
  flow=new Ce03LocalDeparture(client,Verified,(selection,prepared)=>{Ce03NormalUiCapture.Atomic(SessionPath("ui-selection.json"),Canon(selection));Ce03NormalUiCapture.Atomic((string)connection["uiPreparePath"],Canon(prepared));},original=>Ce03NormalUiCapture.Atomic(SessionPath("original-start.json"),original.Json));
  if(stage=="start")flow.RestorePrepared(Ce03NormalUiCapture.Read(SessionPath("ui-selection.json")),Ce03NormalUiCapture.Read((string)connection["uiPreparePath"]));
  BuildUi();StartVideo();await Pause(24);
  Click("Departure");await Pause(12);Click("SelectDeparture0");await Pause(18);
  if(stage=="prepare"){
   Click("LocalCommandSelection");await Wait(()=>Find("Choice2")!=null&&Find("Choice2").interactable);
   foreach(var n in new[]{2,0,1}){Click("Choice"+n);await Pause(8);}Click("Save");await Wait(()=>!client.HasPending&&((JObject)JObject.Parse(client.JournalJson)["receipts"]).Properties().Any(p=>(string)p.Value["request"]["kind"]=="support"));await Pause(24);Click("Back");await Pause(12);Click("Departure");await Pause(12);Click("SelectDeparture0");await Pause(18);
  }
  Click("PreviewDeparture");await Wait(()=>!flow.Busy&&flow.HasPrepare);
  if(stage=="prepare"){if(flow.CanStart)throw new InvalidOperationException("Prepare stage must precede provider verification");}
  else {if(!flow.CanStart)throw new InvalidOperationException("Provider verification/quote unavailable");await Pause(24);Click("ConfirmDeparture");await Wait(()=>!flow.Busy&&File.Exists(SessionPath("original-start.json")));}
  await Pause(36);StopVideo();if(stage=="start")await Battle(false,sources,origin);Ce03ConsumerEvidenceRunner.VerifySources(sources);
  Ce03NormalUiCapture.Atomic(Path.Combine(output,stage+"-ui.json"),Canon(new JObject{["stage"]=stage,["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["sourceBundleSha256"]=bundleHash,["prepareSha256"]=flow.Prepared["prepareSha256"].DeepClone(),["frames"]=frames,["actualUiInput"]=true,["finishSent"]=false,["fullRestartVerified"]=false}));
  Ce03NormalUiCapture.Finish(true);
 }
 bool Verified(JObject prepared){var path=(string)connection["providerStatusPath"];if(!File.Exists(path))return false;var marker=Ce03NormalUiCapture.Read(path);return (string)marker["status"]=="VERIFIED"&&(string)marker["prepareSha256"]==(string)prepared["prepareSha256"]&&(string)marker["sourceBundleSha256"]==bundleHash;}
 void BuildUi(){
  BuildCamera();
  canvas=new GameObject("Actual lobby",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(941,1672);
  var safe=new GameObject("SafeArea",typeof(RectTransform)).GetComponent<RectTransform>();safe.SetParent(canvas.transform,false);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;
  var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");deck=LobbyCommandDeck.Attach(canvas,CombatEditorFonts.Resolve(font),"LOCAL QA",0,new[]{new MissionShellMissionRowState("opn_01_last_departure","마지막 출발 · LOCAL QA",RunEncounterType.Ordinary15Wave,true,"local.start","local.loading",false)},()=>{},()=>{});deck.BindAccount(account);deck.BindLocalCommandClient(client);deck.BindLocalDeparture(flow);
 }
 void BuildCamera(){events=new GameObject("Local UI events").AddComponent<EventSystem>();camera=new GameObject("Local UI camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;target=new RenderTexture(720,1280,24);camera.targetTexture=target;pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);}
 Button Find(string name)=>canvas.GetComponentsInChildren<Button>().SingleOrDefault(b=>b.name==name);
 void Click(string name){var b=Find(name);if(b==null||!b.interactable)throw new InvalidOperationException("Local UI control unavailable: "+name);var pointer=new PointerEventData(events){button=PointerEventData.InputButton.Left};ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);}
 async Task Wait(Func<bool> condition){var deadline=DateTime.UtcNow.AddSeconds(40);while(!condition()){if(DateTime.UtcNow>deadline)throw new TimeoutException();await Task.Yield();}}
 async Task Pause(int count){int end=Time.frameCount+count;await Wait(()=>Time.frameCount>=end);}
 void StartVideo(){frames=0;visibleFrames=0;var path=Path.Combine(output,stage+"-ui.mp4");if(File.Exists(path))throw new IOException("Fresh capture output required");encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/dlstj/AppData/Roaming/Python/Python39/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe",Arguments="-hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 720x1280 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p -movflags +faststart \""+path+"\"",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true});recording=true;Application.targetFrameRate=30;}
 void LateUpdate(){if(!recording)return;Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();RenderTexture.active=previous;var bytes=pixels.GetRawTextureData<byte>().ToArray();byte low=255,high=0;for(int i=0;i<bytes.Length;i+=97){if(bytes[i]<low)low=bytes[i];if(bytes[i]>high)high=bytes[i];}if(high>35&&high-low>20)visibleFrames++;encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);frames++;}
 void StopVideo(){recording=false;if(encoder==null)return;encoder.StandardInput.Close();if(!encoder.WaitForExit(10000)){encoder.Kill();throw new TimeoutException("Video encoder");}if(encoder.ExitCode!=0)throw new IOException("Video encode failed");encoder.Dispose();encoder=null;if(visibleFrames<Math.Max(1,frames/2))throw new InvalidOperationException("Capture has insufficient visible frames");}
 void OnDestroy(){if(deck!=null)deck.BindLocalDeparture(null);account?.Dispose();if(target!=null)target.Release();}
}
#endif
