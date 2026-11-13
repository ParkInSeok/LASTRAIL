#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using LastRail.Presentation.RoofBattle;
using LastRail.Presentation.Audio;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.AugmentSelection {
 // Explicit LOCAL QA composition with separate fixture-native and loopback-HTTP
 // cases. No operating admission or Android encrypted persistence claims.
 public sealed class BMeasuredAcquisitionLocalPlayer : MonoBehaviour {
  [SerializeField] TextAsset sourceAsset;
  [SerializeField] string sourceHash,outputDirectory;
  [SerializeField] TMP_FontAsset font;
  BMeasuredAcquisitionLocalFlow flow;BRouteAugmentScreen screen;TextMeshProUGUI status;
  AugmentLoopbackLocalTrial httpTrial;
  readonly Dictionary<string,GameObject> markers=new Dictionary<string,GameObject>();
  CancellationTokenSource lifetime;
  bool backgroundBefore,ownsBackgroundSetting;
  bool e2eDiagnostics;
  void Awake(){if(Application.isPlaying){backgroundBefore=Application.runInBackground;Application.runInBackground=true;ownsBackgroundSetting=true;}}
  public void Initialize(TextAsset source,string hash,TMP_FontAsset suppliedFont,string directory){sourceAsset=source;sourceHash=hash;font=suppliedFont;outputDirectory=directory;}
  async void Start(){
   e2eDiagnostics=Environment.GetCommandLineArgs().Contains("--lastrail-augment-e2e");
   lifetime=new CancellationTokenSource();
   try {
    if(!sourceAsset||!font)throw new InvalidOperationException("Explicit LOCAL source/font required");
    BMeasuredAcquisitionLocalFlow.ValidateDirectory(outputDirectory);
    string audioOutput=Environment.GetEnvironmentVariable("LASTRAIL_AUDIO_TRIAL_EVIDENCE");
    if(!string.IsNullOrEmpty(audioOutput)){BMeasuredAcquisitionLocalFlow.ValidateDirectory(audioOutput);outputDirectory=audioOutput;}
    string augmentOutput=Environment.GetEnvironmentVariable("LASTRAIL_AUGMENT_TRIAL_EVIDENCE");
    if(!string.IsNullOrEmpty(augmentOutput)){BMeasuredAcquisitionLocalFlow.ValidateDirectory(augmentOutput);outputDirectory=augmentOutput;}
    var audio=GetComponent<BMeasuredCombatAudio>();
    if(audio)audio.Begin(Environment.GetCommandLineArgs().Contains("--lastrail-audio-silent"),Environment.GetCommandLineArgs().Contains("--lastrail-audio-missing-hit"));
    var canvasObject=new GameObject("ExplicitLocalSelectionCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
    canvasObject.transform.SetParent(transform,false);canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
    var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1000,900);scaler.matchWidthOrHeight=1;
    screen=canvasObject.AddComponent<BRouteAugmentScreen>();
    var textObject=new GameObject("LocalEvidenceStatus",typeof(RectTransform),typeof(TextMeshProUGUI));textObject.transform.SetParent(canvasObject.transform,false);
    var rect=textObject.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(20,-10);rect.sizeDelta=new Vector2(-40,85);
    status=textObject.GetComponent<TextMeshProUGUI>();status.font=font;status.fontSize=20;status.alignment=TextAlignmentOptions.TopLeft;status.color=Color.white;
    if(Environment.GetCommandLineArgs().Contains("--lastrail-input-probe"))await AugmentChoiceLocalProbe.RunAsync(canvasObject.transform,font,outputDirectory,Capture,lifetime.Token);
    string bootstrap=Environment.GetEnvironmentVariable("LASTRAIL_AUGMENT_HTTP_BOOTSTRAP");
    if(!string.IsNullOrEmpty(bootstrap)){
     httpTrial=new AugmentLoopbackLocalTrial(bootstrap,outputDirectory);
     await httpTrial.RunAsync(canvasObject.transform,font,Environment.GetCommandLineArgs().Contains("--lastrail-measured-acquisition-auto"),Capture,message=>status.text="EXPLICIT LOCAL HTTP QA\n"+message,lifetime.Token);
     httpTrial.Dispose();Application.Quit(0);return;
    }
    var source=BMeasuredAcquisitionLocalSource.Read(JObject.Parse(sourceAsset.text),sourceHash);
    flow=new BMeasuredAcquisitionLocalFlow(source,screen,font,outputDirectory,(message,native)=>{if(this){status.text="EXPLICIT LOCAL QA · NOT OPERATING\n"+message;if(native!=null)PresentMarkers(native);}},
     audio?(Action<JObject,bool>)audio.Observe:null);
    bool restore=Environment.GetCommandLineArgs().Contains("--lastrail-measured-acquisition-restore");
    bool endToEnd=Environment.GetCommandLineArgs().Contains("--lastrail-augment-e2e");
    if(restore){await flow.RestoreOnlyAsync(lifetime.Token);await Capture("restore.png",lifetime.Token);if(endToEnd)await ShowNodeResult(canvasObject.transform);if(audio)audio.Finish(outputDirectory);Application.Quit(0);return;}
    await flow.OpenSelectionAsync(lifetime.Token);
    var selectionRect=(RectTransform)screen.View.transform;
    selectionRect.anchorMin=selectionRect.anchorMax=new Vector2(.5f,1);selectionRect.anchoredPosition=new Vector2(-450,-100);
    await Capture("selection.png",lifetime.Token);
    if(Environment.GetCommandLineArgs().Contains("--lastrail-measured-acquisition-auto")){
     await Task.Delay(1500,lifetime.Token);
     screen.View.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Select0").onClick.Invoke();
    }
    await flow.CompleteAfterSelectionAsync(lifetime.Token);
    if(audio)await audio.VerifyPauseAndReturn();
    await Capture("healed.png",lifetime.Token);
    if(endToEnd)await ShowNodeResult(canvasObject.transform);
    if(audio)audio.Finish(outputDirectory,true);
    Application.Quit(0);
   }catch(OperationCanceledException){ }
   catch(Exception error){
    Debug.LogException(error);if(status)status.text="LOCAL QA FAILED\n"+error.Message;
    try{BMeasuredAcquisitionLocalFlow.ValidateDirectory(outputDirectory);File.WriteAllText(Path.Combine(outputDirectory,"failure-"+Guid.NewGuid().ToString("N")+".json"),new JObject{["classification"]="LOCAL_QA_FAILURE",["error"]=error.ToString()}.ToString());}catch(Exception write){Debug.LogException(write);}
    Application.Quit(1);
   }
  }
  async Task Capture(string name,CancellationToken cancellation){
   await Task.Delay(250,cancellation);
   var camera=Camera.main?Camera.main:throw new InvalidOperationException("LOCAL capture camera missing");
   var canvas=screen.GetComponent<Canvas>();var mode=canvas.renderMode;var canvasCamera=canvas.worldCamera;float plane=canvas.planeDistance,aspect=camera.aspect,scale=canvas.scaleFactor;
   var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
   var target=new RenderTexture(1280,900,24);Texture2D pixels=null;
   try{
    // Hidden Windows Players may not have a capturable swapchain. Render the
    // same owned scene and Canvas explicitly; never substitute a mock image.
    camera.targetTexture=target;camera.aspect=1280f/900;
    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.scaleFactor=1;
    Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
    pixels=new Texture2D(1280,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,900),0,0);pixels.Apply();
    byte[] png=pixels.EncodeToPNG();if(png==null||png.Length==0)throw new IOException("LOCAL camera capture failed");
    File.WriteAllBytes(Path.Combine(outputDirectory,name),png);
    File.WriteAllText(Path.Combine(outputDirectory,"capture-method.txt"),"Actual Player camera render with the same owned Canvas temporarily in ScreenSpaceCamera mode; not an OS-window screenshot.");
   }finally{
    canvas.renderMode=mode;canvas.worldCamera=canvasCamera;canvas.planeDistance=plane;canvas.scaleFactor=scale;camera.targetTexture=previousTarget;camera.aspect=aspect;RenderTexture.active=previousActive;
    target.Release();Destroy(target);if(pixels)Destroy(pixels);
   }
  }
  void PresentMarkers(JObject native){
   foreach(var marker in markers.Values)marker.SetActive(false);
   var spatial=native["spatialSource"];if(spatial==null)return;
   foreach(var row in ((JArray)spatial["operators"]).Concat((JArray)spatial["positions"])){
    string id=(string)row["id"];var point=row["positionMm"] as JArray;if(point==null)continue;
    if(!markers.TryGetValue(id,out var marker)){
     marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="LOCAL observed position "+id;marker.transform.SetParent(transform,false);
     Destroy(marker.GetComponent<Collider>());marker.transform.localScale=Vector3.one*.55f;markers.Add(id,marker);
    }
    marker.SetActive(true);marker.transform.position=new Vector3((long)point[0]/1000f,(long)point[1]/1000f+.28f,(long)point[2]/1000f);
   }
  }
  async Task ShowNodeResult(Transform parent){
   var result=await flow.CompleteNodeResultAsync(lifetime.Token);
   var view=AugmentNodeResultView.Create(parent,font);view.Bind(result);
   Button Find(string name)=>view.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
   Find("AugmentEffectsEntry").onClick.Invoke();await Capture("node-result.png",lifetime.Token);
   Find("AugmentResultClose").onClick.Invoke();
   if(!ReferenceEquals(result,await flow.CompleteNodeResultAsync(lifetime.Token)))throw new InvalidOperationException("Result reopening replaced the completed projection");
   Find("AugmentEffectsEntry").onClick.Invoke();Find("AugmentResultCard0").onClick.Invoke();await Capture("node-result-details.png",lifetime.Token);
   view.Unbind();
   File.WriteAllText(Path.Combine(outputDirectory,"result-view.json"),new JObject{["classification"]="LOCAL_NATIVE_RESULT_UI_NOT_SERVER_SETTLEMENT",["resultReused"]=true,["entryOpened"]=true,["detailsOpened"]=true,["viewUnbound"]=true,["physicalPointerInput"]=false,["actualHpGain"]=result.Entries[0].Metrics[0].Value}.ToString());
  }
  void TraceLifetime(string stage){if(e2eDiagnostics)Debug.Log("LOCAL_E2E_LIFETIME "+stage);}
  void OnApplicationQuit(){TraceLifetime("application-quit callback");}
  void OnDisable(){TraceLifetime("disable begin");lifetime?.Cancel();flow?.Dispose();httpTrial?.Dispose();TraceLifetime("disable complete");}
  void OnDestroy(){TraceLifetime("destroy begin");lifetime?.Cancel();flow?.Dispose();httpTrial?.Dispose();lifetime?.Dispose();if(ownsBackgroundSetting)Application.runInBackground=backgroundBefore;TraceLifetime("destroy complete");}
 }

 public sealed class BMeasuredAcquisitionLocalFlow:IDisposable {
  readonly BMeasuredAcquisitionLocalSource source;readonly BRouteAugmentScreen screen;readonly TMP_FontAsset font;readonly string directory,context;
  readonly Action<string,JObject> display;readonly CombatO22ApplicationStore originals;readonly RoofBattleLocalFileStore checkpoint;
  readonly Action<JObject,bool> observeAudio;
  readonly BRouteNativeComposition composition;readonly LocalSelectTransport wire;
  AugmentNodeResult completedResult;
  BRouteSessionController controller;AugmentSelectionFileJournal journal;AugmentSelectionScope scope;bool disposed;
  const string AttackCommand="LOCAL-measured-selected-o22-original-basic";
  public static void ValidateDirectory(string path){
   if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path)||!Path.GetFullPath(path).StartsWith(Path.GetFullPath("D:/Project/LASTRAIL")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Explicit LOCAL output must remain in D:/Project/LASTRAIL");
   Directory.CreateDirectory(path);
  }
  public BMeasuredAcquisitionLocalFlow(BMeasuredAcquisitionLocalSource source,BRouteAugmentScreen screen,TMP_FontAsset font,string directory,Action<string,JObject> display,Action<JObject,bool> observeAudio=null){
   this.observeAudio=observeAudio;
   this.source=source??throw new ArgumentNullException(nameof(source));this.screen=screen?screen:throw new ArgumentNullException(nameof(screen));this.font=font?font:throw new ArgumentNullException(nameof(font));
   ValidateDirectory(directory);this.directory=Path.GetFullPath(directory);this.display=display??throw new ArgumentNullException(nameof(display));context="LOCAL-measured-acquisition:"+source.SourceHash;
   string originalPath=Path.Combine(this.directory,"original-acquisition.json");
   originals=new CombatO22ApplicationStore(originalPath,()=>ReadText(originalPath),raw=>WriteAtomic(originalPath,raw));
   checkpoint=new RoofBattleLocalFileStore(Path.Combine(this.directory,"checkpoint"),context,BNodeSessionController.CheckpointVersion);
   composition=new BRouteNativeComposition(source.CombatRoute,source.Boarding,source.Binding,source.Geometry,source.Motion,10000);
   wire=new LocalSelectTransport(source);
  }
  void Alive(CancellationToken cancellation){cancellation.ThrowIfCancellationRequested();if(disposed)throw new ObjectDisposedException(nameof(BMeasuredAcquisitionLocalFlow));}
  public async Task OpenSelectionAsync(CancellationToken cancellation){
   Alive(cancellation);if(File.Exists(Path.Combine(directory,"evidence.json")))throw new InvalidOperationException("Existing evidence requires explicit restore mode");
   controller=new BRouteSessionController(source.NoncombatRoute.AccountId,source.NoncombatRoute.RunId);
   await controller.RefreshAsync(()=>Task.FromResult(source.NoncombatRoute));Alive(cancellation);
   scope=new AugmentSelectionScope(new AugmentSelectionOwner(source.Original.AccountId,source.Environment),source.Original.RunId,source.Original.GrantEventId);
   string journalPath=Path.Combine(directory,"selection-journal.json");
   journal=new AugmentSelectionFileJournal(scope,journalPath,()=>ReadText(journalPath),raw=>WriteAtomic(journalPath,raw));
   // Test credentials are consumed only by the in-process fixture transport below.
   var auth=new GameApiAuthSessionSnapshot(scope.Owner.AccountId,"ACTIVE",new GameApiAccessToken("lst1.explicit-local-qa-only"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("explicit-local-qa-only"),DateTime.UtcNow.AddDays(1));
   var original=JObject.Parse(source.Original.Capture());
   AugmentCardPresentation Card(string version,string id)=>id=="O22"?HealingAugmentPresentation.FromPinned(version,(JObject)original["definition"]["selectedDefinition"],(JObject)original["definition"]["composition"]):new AugmentCardPresentation(id,"LOCAL candidate","Explicit source candidate",null);
   await screen.OpenExplicitLocalQaAsync(controller,scope,source.InitialSelection,wire,()=>auth,()=>!disposed,journal,originals,Card,font);Alive(cancellation);
   display("Select a supplied card. SELECT uses a local fixture response; storage and native combat are real.",null);
  }
  public async Task CompleteAfterSelectionAsync(CancellationToken cancellation){
   Alive(cancellation);
   for(int i=0;screen.ArrivalState!="ACQUIRED_PENDING_COMBAT";i++){Alive(cancellation);if(i>=3000)throw new TimeoutException("Original selection did not complete");await Task.Delay(100,cancellation);}
   Need(journal.Load(scope.StorageKey)==null&&wire.Calls==1,"Selection must durably finish exactly once");
   Need(controller.LocalCombatOutcome==null,"Noncombat selection must not create native combat");
   var acquired=originals.LoadForRun(source.Original.AccountId,source.Original.RunId,source.Environment,source.Original.StartSnapshotHash);
   Need(acquired!=null&&acquired.Capture()==source.Original.Capture(),"Exact acquisition was not persisted");
   await controller.RefreshAsync(()=>Task.FromResult(source.CombatRoute));Alive(cancellation);
   screen.AttachLocalNativeWithAcquisition(composition,controller,context,checkpoint.Load,checkpoint.Save,originals,source.Environment);
   observeAudio?.Invoke(Native(),true);
   string actor=source.Binding.Composition.Attack.Formation.Slots[0].OperatorId;JObject successful=null,hit=null;string targetId=null;long targetGeneration=0;int beforeHp=0;
   for(int step=0;step<600;step++){
    Alive(cancellation);var native=Native();var vitals=native["state"]["direct"]["allies"]["vitals"][actor];
    Need(vitals!=null,"Explicit source must supply Kyle");int hp=(int)vitals["hp"];
    if(hp>0&&hp<(int)vitals["maxHp"]){
     foreach(var life in ((JArray)native["boarding"]["lives"]).Where(x=>(bool)x["registered"]&&(bool)x["targetable"])){
      string target=(string)life["id"];long generation=(long)life["generation"];
      var geometry=controller.ObserveOrdinaryGeometry(actor,target,(long)native["state"]["direct"]["clock"]["completedSteps"],(long)native["spatialSource"]["revision"],(string)native["spatialSource"]["geometryHash"]);
      if(!(bool)geometry["eligible"])continue;
      beforeHp=hp;controller.ApplyWaveBasic(AttackCommand,actor,target,generation);var after=Native();
      observeAudio?.Invoke(after,false);
      hit=((JArray)after["results"].Last["hits"]).SingleOrDefault() as JObject;
      Need(hit!=null&&(long?)hit["lifesteal"]?["actualHpGain"]>0,"Original eligible basic did not produce positive O22 healing");
      Need((int)after["state"]["direct"]["allies"]["vitals"][actor]["hp"]==beforeHp+(int)hit["lifesteal"]["actualHpGain"],"Original HP gain differs from receipt");
      successful=after;targetId=target;targetGeneration=generation;break;
     }
    }
    if(successful!=null)break;
    Need(controller.LocalCombatOutcome=="RUNNING"&&hp>0,"Native combat ended before a positive eligible heal");
    controller.Advance("LOCAL-original-advance:"+step,1000000,10000);
    var advanced=Native();observeAudio?.Invoke(advanced,false);
    display("Native clock / measured geometry · waiting for actual enemy damage and an eligible basic hit",advanced);
    await Task.Yield();
   }
   Need(successful!=null,"No positive native O22 heal within bounded original advance");
   // LOCAL QA capture window: no native clock advancement, extra command or gameplay hold.
   if(observeAudio!=null)await Task.Delay(1500,cancellation);
   if(observeAudio!=null){
    controller.SetPaused("LOCAL-audio-trial-pause",true);observeAudio(Native(),false);
    controller.SetPaused("LOCAL-audio-trial-resume",false);observeAudio(Native(),false);
   }
   var node=(JObject)controller.Capture()["nodeSession"];
   var storedNode=checkpoint.Load()["node"];
   if(!JToken.DeepEquals(node,storedNode)){
    WriteAtomic(Path.Combine(directory,"mismatch-live.json"),node.ToString());
    WriteAtomic(Path.Combine(directory,"mismatch-disk.json"),storedNode.ToString());
    throw new InvalidOperationException("Disk checkpoint differs from committed native state; snapshots retained in "+directory);
   }
   var evidence=new JObject{["version"]="unity.measured-acquisition-player-qa.v1.20261020",["classification"]="EXPLICIT_LOCAL_FIXTURE_SELECTION_REAL_NATIVE_AND_FILE_IO",
    ["sourceHash"]=source.SourceHash,["selectionCalls"]=wire.Calls,["acquisitionState"]="ACQUIRED_PENDING_COMBAT",
    ["applicationId"]=acquired.ApplicationId,["beforeHp"]=beforeHp,["hit"]=hit,["nodeHash"]=BDirectAttackInput.Digest(node),
    ["attackCommand"]=AttackCommand,["actorId"]=actor,["targetId"]=targetId,["targetGeneration"]=targetGeneration,["operatingAdmission"]=false,
    ["storage"]="LOCAL_QA_FILE_NOT_ANDROID_KEYSTORE",["fixtureIssuer"]=true,["independentArrivalVerified"]=false,["sameProcessRestartVerified"]=false,
    ["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id};
   WriteAtomic(Path.Combine(directory,"evidence.json"),evidence.ToString(Formatting.None));
   await RestartAndVerify(evidence,cancellation);
   evidence["sameProcessRestartVerified"]=true;WriteAtomic(Path.Combine(directory,"evidence.json"),evidence.ToString(Formatting.None));
   display("Positive native O22 heal "+hit["lifesteal"]["actualHpGain"]+" · durable restart identical · duplicate command no second effect",Native());
  }
  JObject Native()=>(JObject)controller.Capture()["nodeSession"]["runtime"];
  public async Task<AugmentNodeResult> CompleteNodeResultAsync(CancellationToken cancellation){
   Alive(cancellation);if(completedResult!=null)return completedResult;
   for(int step=0;controller.LocalCombatOutcome=="RUNNING";step++){
    Alive(cancellation);if(step>=20)throw new TimeoutException("Original native node did not end within bounded LOCAL advance");
    var beforeNative=Native();
    if(source.Binding.Composition.Attack.Formation.Slots.All(s=>(int)beforeNative["state"]["direct"]["allies"]["vitals"][s.OperatorId]["hp"]==0)){
     WriteAtomic(Path.Combine(directory,"node-result-pending.json"),new JObject{["classification"]="LOCAL_NATIVE_NOT_TERMINAL",["outcome"]=controller.LocalCombatOutcome,["reason"]="ALL_SUPPLIED_OPERATORS_DOWN_NO_SUPPLIED_TERMINAL_ROUTE",["checkpointHash"]=BDirectAttackInput.Digest((JObject)controller.Capture()["nodeSession"]),["tick"]=beforeNative["state"]["direct"]["clock"]["completedSteps"],["sourceHash"]=source.SourceHash}.ToString());
     throw new NotSupportedException("Original LOCAL spatial route has no living operator target and no supplied terminal path; result not fabricated");
    }
    // Feed elapsed time to the existing clock. Do not force victory, HP or speed.
    long beforeTick=(long)beforeNative["state"]["direct"]["clock"]["completedSteps"];
    controller.Advance("LOCAL-e2e-terminal-advance-1000s:"+beforeTick,1000000000,10000);
    // Scripted LOCAL input at 1000 native-second intervals, not real-time gameplay
    // or a new autoattack rule. Native authority owns all intervening events.
    if(controller.LocalCombatOutcome=="RUNNING"){
     var native=Native();string actor=source.Binding.Composition.Attack.Formation.Slots[0].OperatorId;
     long readyTick=(long)native["state"]["direct"]["next"][actor];
     if((long)native["state"]["direct"]["clock"]["completedSteps"]>=readyTick&&(int)native["state"]["direct"]["allies"]["vitals"][actor]["hp"]>0){
      foreach(var life in ((JArray)native["boarding"]["lives"]).Where(x=>(bool)x["registered"]&&(bool)x["targetable"])){
       string target=(string)life["id"];long tick=(long)native["state"]["direct"]["clock"]["completedSteps"];
       var geometry=controller.ObserveOrdinaryGeometry(actor,target,tick,(long)native["spatialSource"]["revision"],(string)native["spatialSource"]["geometryHash"]);
       if(!(bool)geometry["eligible"])continue;
       controller.ApplyWaveBasic("LOCAL-e2e-basic:"+tick+":"+target,actor,target,(long)life["generation"]);break;
      }
     }
    }
    display("LOCAL native combat advancing to its own terminal outcome",Native());await Task.Yield();
   }
   Alive(cancellation);
   var observation=controller.CaptureOriginalRuntimeObservation();var raw=JObject.Parse(source.Original.Capture());
   var card=HealingAugmentPresentation.FromPinned(source.InitialSelection.CatalogVersion,(JObject)raw["definition"]["selectedDefinition"],(JObject)raw["definition"]["composition"]);
   // Card version comes from the same accepted selection snapshot as its display.
   var result=NativeO22NodeResult.Read(observation,source.Original,card);
   var node=(JObject)controller.Capture()["nodeSession"];
   Need(JToken.DeepEquals(node,checkpoint.Load()["node"]),"Final result must follow durable checkpoint");
   WriteAtomic(Path.Combine(directory,"node-result.json"),new JObject{["classification"]="LOCAL_NATIVE_TERMINAL_NOT_SERVER_SETTLEMENT",["runId"]=result.RunId,["nodeId"]=result.NodeId,["finalSequence"]=result.FinalSequence,["outcome"]=controller.LocalCombatOutcome,["nodeHash"]=BDirectAttackInput.Digest(node),["nativeHash"]=observation.CheckpointHash,["applicationId"]=source.Original.ApplicationId,["actualHpGain"]=result.Entries[0].Metrics[0].Value,["coverage"]=result.Entries[0].Coverage.ToString(),["selectionCalls"]=wire.Calls}.ToString());
   completedResult=result;display("LOCAL native "+controller.LocalCombatOutcome+" · recorded recovery "+result.Entries[0].Metrics[0].Value,Native());return result;
  }
  public async Task RestoreOnlyAsync(CancellationToken cancellation){
   Alive(cancellation);var raw=ReadText(Path.Combine(directory,"evidence.json"))??throw new InvalidOperationException("Original completed evidence missing");
   var evidence=JObject.Parse(raw);Need((string)evidence["sourceHash"]==source.SourceHash,"Restart source changed");
   await RestartAndVerify(evidence,cancellation,true);display("Restored original checkpoint and acquisition; duplicate hit changed nothing.",Native());
   WriteAtomic(Path.Combine(directory,"process-restart.json"),new JObject{["sourceHash"]=source.SourceHash,["nodeHash"]=evidence["nodeHash"].DeepClone(),["restoredIdentically"]=true,["duplicateChangedState"]=false,["classification"]="EXPLICIT_LOCAL_OWNER_RESTORE",
    ["originalProcessId"]=evidence["processId"]?.DeepClone(),["restoreProcessId"]=System.Diagnostics.Process.GetCurrentProcess().Id}.ToString());
  }
  async Task RestartAndVerify(JObject evidence,CancellationToken cancellation,bool allowCommittedProgress=false){
   controller?.Dispose();controller=new BRouteSessionController(source.CombatRoute.AccountId,source.CombatRoute.RunId);
   await controller.RefreshAsync(()=>Task.FromResult(source.CombatRoute));Alive(cancellation);
   screen.AttachLocalNativeWithAcquisition(composition,controller,context,checkpoint.Load,checkpoint.Save,originals,source.Environment);
   var before=controller.Capture();
   if(allowCommittedProgress){
    // Attachment already verified/replayed the authoritative latest checkpoint.
    // A derived UI report may be absent or stale after cancellation/write failure.
    var originalHits=((JArray)before["nodeSession"]["runtime"]["results"]).SelectMany(r=>(JArray)r["hits"]).Where(h=>(string)h["eventId"]==(string)evidence["attackCommand"]).ToArray();
    Need(originalHits.Length==1&&JToken.DeepEquals(originalHits[0],evidence["hit"]),"Recovered checkpoint changed the original confirmed hit");
    evidence["nodeHash"]=BDirectAttackInput.Digest(before["nodeSession"]);
   }else Need(BDirectAttackInput.Digest(before["nodeSession"])==(string)evidence["nodeHash"],"Restart changed original node");
   observeAudio?.Invoke((JObject)before["nodeSession"]["runtime"],true);
   Need((string)evidence["actorId"]==source.Binding.Composition.Attack.Formation.Slots[0].OperatorId,"Restart actor changed");
   controller.ApplyWaveBasic((string)evidence["attackCommand"],(string)evidence["actorId"],(string)evidence["targetId"],(long)evidence["targetGeneration"]);
   var duplicate=controller.Capture();observeAudio?.Invoke((JObject)duplicate["nodeSession"]["runtime"],false);
   Need(JToken.DeepEquals(before,duplicate),"Duplicate original hit changed state");
  }
  static void Need(bool yes,string message){if(!yes)throw new InvalidOperationException(message);}
  static string ReadText(string path)=>File.Exists(path)?File.ReadAllText(path,Encoding.UTF8):null;
  static void WriteAtomic(string path,string raw){
   string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{var bytes=new UTF8Encoding(false,true).GetBytes(raw);using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write)){f.Write(bytes,0,bytes.Length);f.Flush(true);}if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
   finally{if(File.Exists(temp))File.Delete(temp);}
  }
  public void Dispose(){if(disposed)return;disposed=true;controller?.Dispose();}
  sealed class LocalSelectTransport:IGameApiAuthenticatedHttpTransport {
   readonly BMeasuredAcquisitionLocalSource source;public int Calls{get;private set;}
   string acceptedKey,acceptedBody;
   public LocalSelectTransport(BMeasuredAcquisitionLocalSource source){this.source=source;}
   public Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request,GameApiAccessToken token){
    if(request.Method!="POST"||request.EndpointPath!="/v1/runs/"+Uri.EscapeDataString(source.Original.RunId)+"/augment-grants/"+Uri.EscapeDataString(source.Original.GrantEventId)+"/select")throw new InvalidOperationException("Only explicit local SELECT is supplied");
    var p=JObject.Parse(request.BodyJson);var c=p["command"] as JObject;
    if(p.Count!=2||(string)p["version"]!=AugmentSelectHttpPort.Version||c==null||c.Count!=4
     ||(string)c["eventId"]!=source.InitialSelection.EventId||c["expectedRevision"]?.Type!=JTokenType.Integer||(long)c["expectedRevision"]!=source.InitialSelection.GrantRevision
     ||c["slotIndex"]?.Type!=JTokenType.Integer||(int)c["slotIndex"]!=0||(string)c["candidateId"]!=source.InitialSelection.Slots[0].CandidateId)
     throw new InvalidOperationException("Local fixture SELECT differs from supplied original candidate/revision");
    string key=request.Headers.Single(h=>h.Name=="Idempotency-Key").Value;
    if(acceptedKey!=null&&(key!=acceptedKey||request.BodyJson!=acceptedBody))throw new InvalidOperationException("Local fixture SELECT original-key conflict");
    acceptedKey=key;acceptedBody=request.BodyJson;
    Calls++;return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,source.SelectResponseJson)));
   }
  }
 }
}
#endif
