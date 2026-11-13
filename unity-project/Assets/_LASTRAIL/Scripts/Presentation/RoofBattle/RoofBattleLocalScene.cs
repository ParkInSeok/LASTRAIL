using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using LastRail.Presentation.Audio;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastRail.Presentation.RoofBattle {
 // Upper composition/controller for exactly one explicitly pinned B-native LOCAL owner.
 public sealed class RoofBattleLocalScene : MonoBehaviour {
  [SerializeField]TextAsset localSource;
  [SerializeField]string independentDigest,contextId,storeDirectory;
  [SerializeField]FiveAgentVisualAssets visualAssets;
  [SerializeField]FourAgentV03Assets v03Assets;FourAgentV03FrameComposer v03Composer;
  [SerializeField]TMP_FontAsset uiFont;
  [SerializeField]int eventCapacity=10000;
  [SerializeField]bool autoAdvance=true;
  // Explicit approved mappings only. Empty bindings remain silent; no guessed shot/critical cue.
  [SerializeField]LastRailAudioCuePlayer audioPlayer;
  [SerializeField]string battleMusicCue,victoryMusicCue,failureMusicCue;
  Func<string,CombatAudioResultBinding> resultAudioMapping;
  Func<string,bool> resultAudioDisclosure;
  public void ConfigureResultAudio(Func<string,CombatAudioResultBinding> approvedMapping,Func<string,bool> originalDisclosure){
   if(started)throw new InvalidOperationException("Configure original result audio owners before Start.");
   resultAudioMapping=approvedMapping??throw new ArgumentNullException(nameof(approvedMapping));
   resultAudioDisclosure=originalDisclosure??throw new ArgumentNullException(nameof(originalDisclosure));
  }
  public string LastAudioDiagnostic {get;private set;}
  RoofBattleLocalOwner owner;RoofBattleLocalFileStore store;RoofBattleLocalSource source;
  RoofBattleLocalView view;FiveAgentVisualFeed feed;FiveAgentVisualController visualController;IDisposable subscription;
  bool started,ready,destroying;JObject lastFrame;long lastCursor;
  Coroutine simulation,rebind;
  static event Action CompositionChanged;
  readonly HashSet<string> eventIds=new HashSet<string>(StringComparer.Ordinal);
  OperatorInformationController information;OperatorInformationView informationView;
  public OperatorInformationFrame InformationFrame=>information?.Frame;
  public RoofBattleLocalView View=>view;
  public bool Ready=>ready;
  public bool LastCommandSucceeded {get;private set;}
  public Exception LastException {get;private set;}
  public string LastError=>LastException==null?null:LastException.GetType().Name+": "+LastException.Message;
  public int ExecutionCount {get;private set;}
  public int HitCount {get;private set;}
  public long PublishedEventCount {get;private set;}
  public int LastDeliveredEventCount {get;private set;}
  public JObject Snapshot=>owner?.Snapshot()??throw new InvalidOperationException("Explicit roof LOCAL source is not open.");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  bool failNextSave;
#endif
  public void Initialize(TextAsset originalSource,string trustedDigest,string explicitContext,FiveAgentVisualAssets catalog,TMP_FontAsset font,string explicitDStoreDirectory) {
   if(started||localSource!=null)throw new InvalidOperationException("Scene binding already initialized.");
   localSource=originalSource;independentDigest=trustedDigest;contextId=explicitContext;visualAssets=catalog;uiFont=font;storeDirectory=explicitDStoreDirectory;
  }
  public void ConfigureV03(FourAgentV03Assets catalog){if(started)throw new InvalidOperationException("Configure v03 before Start.");v03Assets=catalog??throw new ArgumentNullException(nameof(catalog));}
  void Start(){
   started=true;string requestedQa=null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
   requestedQa=RoofBattlePlayerSelfQa.RequestedDirectory;if(RoofBattlePlayerSelfQa.RestoreOnly&&requestedQa==null)requestedQa="";if(requestedQa!=null)autoAdvance=false;
   if(RoofBattlePlayerSelfQa.RequestedStoreDirectory!=null)storeDirectory=RoofBattlePlayerSelfQa.RequestedStoreDirectory;
#endif
   try {
    CheckExclusiveComposition();
    if(localSource==null||string.IsNullOrWhiteSpace(independentDigest)||string.IsNullOrWhiteSpace(contextId)||(visualAssets==null&&v03Assets==null)||uiFont==null||string.IsNullOrWhiteSpace(storeDirectory))throw new ArgumentException("Missing explicit LOCAL source/digest/context/catalog/font/D store. No automatic fallback.");
    view=gameObject.AddComponent<RoofBattleLocalView>();if(v03Assets!=null){view.ConfigureV03(v03Assets,uiFont);v03Composer=new FourAgentV03FrameComposer(v03Assets);}else view.Configure(visualAssets,uiFont);
    source=RoofBattleLocalSource.Read(JObject.Parse(localSource.text),independentDigest);
    if(!Path.IsPathRooted(storeDirectory))throw new ArgumentException("Explicit absolute D LOCAL store directory required.");
    store=new RoofBattleLocalFileStore(storeDirectory,contextId);
    owner=new RoofBattleLocalOwner(source.Binding,eventCapacity,contextId,store.Load,Save);
    if(v03Assets==null){feed=new FiveAgentVisualFeed();visualController=new FiveAgentVisualController(visualAssets.CreateModel());}
    owner.Open();ready=true;BindPresentation();LastCommandSucceeded=true;
   }catch(Exception error){Fail(error);}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
   if(requestedQa!=null){
    try{var probe=gameObject.AddComponent<RoofBattlePlayerSelfQa>();probe.Initialize(this,requestedQa);}
    catch(Exception error){Fail(error);Debug.LogError("Roof QA setup failed: "+error);Application.Quit(1);}
   }
#endif
  }
  void CheckExclusiveComposition(){
   foreach(var root in FindObjectsByType<BattleSceneRoot>(FindObjectsSortMode.None))if(root.isActiveAndEnabled&&root.gameObject.scene.isLoaded)throw new InvalidOperationException("Loaded active Basic/CE03 root exists. Refusing a root that could bind a second simulation.");
   foreach(var other in FindObjectsByType<RoofBattleLocalScene>(FindObjectsSortMode.None))if(other!=this&&other.isActiveAndEnabled&&other.gameObject.scene.isLoaded)throw new InvalidOperationException("Only one roof LOCAL composition may own this scene.");
  }
  void Save(JObject candidate,long revision){
#if UNITY_EDITOR || DEVELOPMENT_BUILD
   if(failNextSave){failNextSave=false;throw new IOException("Development QA injected one save failure before disk publication.");}
#endif
   store.Save(candidate,revision);
  }
  void BindPresentation(){
   if(!ready||!isActiveAndEnabled)return;
   UnbindPresentation();view.SetVisible(true);
   view.Bind(()=>AdvanceManual(10000,2),TogglePaused,ToggleSpeed,RestoreOriginal,()=>SetAutoAdvance(!autoAdvance));
   if(v03Composer==null){visualController.Bind(feed,view.PresentNeutral,view.Clear,new Dictionary<string,int>{{"op_eve",2}},contextId,0);
   // A neutral display cue is not an attack action. It never changes original next/HP or emits a projectile.
   feed.PublishPose(new CombatUnitAnimationState("op_eve",CombatUnitRole.Operator,"idle","neutral-display:"+contextId),0,0);}
   // Subscribe synchronously supplies the owner's snapshot+cursor on its owner thread.
   if(audioPlayer!=null)try{audioPlayer.BeginCombatScope(owner,0);}catch(Exception error){LastAudioDiagnostic=error.Message;}
   subscription=owner.Subscribe(OnCommittedFrame);
   if(information==null){
    informationView=view.CreateInformationView();
    information=new OperatorInformationController((target,cancellation)=>{
     var state=owner.Snapshot();if(owner.RecoveryRequired||(string)state["outcome"]!="RUNNING")throw new InvalidOperationException("Current Eve LOCAL owner unavailable.");
     return System.Threading.Tasks.Task.FromResult(new OperatorInformationSnapshot(target,(long)state["revision"],null,null,"장착 정보를 확인할 수 없습니다."));
    });
   }
   information.Bind(frame=>{if(frame.Error!=null)Debug.LogError("Eve information memory binding: "+frame.Error.GetType().Name);informationView.Present(frame);});informationView.Bind(information.OpenDetail,information.CloseDetail,information.Back);
   view.BindOperatorInformation(()=>{if(ready&&isActiveAndEnabled&&!owner.RecoveryRequired&&(string)owner.Snapshot()["outcome"]=="RUNNING")_=information.ClickActorAsync(new OperatorInformationTarget(contextId,"op_eve","op_eve",0));},information.ClickEmptyWorld);
  }
  void UnbindPresentation(){StopSimulation();information?.Unbind();informationView?.Unbind();view?.BindOperatorInformation(null,null);subscription?.Dispose();subscription=null;visualController?.Unbind();view?.Unbind();view?.Clear();}
  void ReadOriginalJournal(JObject frame){
   var checkpoint=store.Load();
   if(checkpoint==null||(long)checkpoint["revision"]!=(long)frame["revision"]||(long)checkpoint["cursor"]!=(long)frame["cursor"]||(string)checkpoint["contextId"]!=contextId)throw new InvalidOperationException("Committed snapshot and durable journal do not match.");
   eventIds.Clear();ExecutionCount=0;HitCount=0;long cursor=0;
   foreach(JObject row in (JArray)checkpoint["journal"]){if((long)row["cursor"]!=++cursor)throw new InvalidOperationException("Original journal cursor is not contiguous.");CountOriginalEvent(row);}
   lastCursor=cursor;
  }
  void CountOriginalEvent(JObject row){
   string id=(string)row["eventId"],phase=(string)row["phase"];
   if((string)row["contextId"]!=contextId||string.IsNullOrWhiteSpace(id)||!eventIds.Add(id))throw new InvalidOperationException("Original committed event identity conflict.");
   if(phase=="EXECUTED_INSTANT")ExecutionCount++;else if(phase=="HIT")HitCount++;else throw new InvalidOperationException("Unsupported original LOCAL phase.");
  }
  void OnCommittedFrame(JObject frame){
   string kind=(string)frame["kind"];var events=(JArray)frame["events"];
   if(kind=="SNAPSHOT")ReadOriginalJournal(frame);
   else foreach(JObject row in events){if((long)row["cursor"]!=lastCursor+1)throw new InvalidOperationException("Committed presentation event gap.");CountOriginalEvent(row);lastCursor++;PublishedEventCount++;}
   if((long)frame["cursor"]!=lastCursor)throw new InvalidOperationException("Snapshot cursor diverged from original events.");
   lastFrame=(JObject)frame.DeepClone();LastDeliveredEventCount=events.Count;
   if((string)frame["outcome"]!="RUNNING"||(bool?)frame["recoveryRequired"]==true)information?.Clear();
   else if(information?.Frame.Target!=null)_=information.RefreshAsync();
   if((string)frame["outcome"]!="RUNNING")autoAdvance=false;
   if(v03Composer!=null){long originalMs=checked((long)frame["tick"]*(long)frame["stepTimeSpanTicks"])/10000;view.PresentNeutral(v03Composer.Compose("op_eve",1,false,0,originalMs,new Vector2(570,1005),null,false,false));}else feed.PublishHold((bool)frame["paused"]);view.PresentState(frame,ExecutionCount,HitCount,autoAdvance);
   // Deliberately no Impact, attack pose, projectile, charge or reverse launch from HIT receipts.
   PresentCommittedAudio(frame,kind);
   RefreshSimulation();
  }
  void PresentCommittedAudio(JObject frame,string kind){
   if(audioPlayer==null)return;
   try{
    string outcome=(string)frame["outcome"];
    audioPlayer.SelectBattleMusic((long)frame["revision"],outcome=="RUNNING"?battleMusicCue:outcome=="VICTORY"?victoryMusicCue:outcome=="FAILURE"?failureMusicCue:null,outcome!="RUNNING");
    if(kind=="SNAPSHOT")return; // Rebind/reconnect never replays the historical sound journal.
    foreach(JObject row in (JArray)frame["events"]){
     if((string)row["phase"]!="HIT")continue;
     if(resultAudioMapping==null||resultAudioDisclosure==null){LastAudioDiagnostic="missing_result_mapping_or_disclosure";continue;}
     var receipt=row["nativeReceipt"] as JObject;
     foreach(string result in CombatAudioResults.ReadNative(receipt)){
      var mapping=resultAudioMapping(result);
      if(mapping==null){LastAudioDiagnostic="missing_result_mapping:"+result;continue;}
      if(mapping.Outcome!=result)throw new InvalidOperationException("Audio mapping changed the confirmed result.");
      string recipient=result=="Lifesteal"?(string)receipt["lifesteal"]["logicalActorId"]:(string)row["targetId"];
      if(string.IsNullOrWhiteSpace(recipient)){LastAudioDiagnostic="missing_original_recipient";continue;}
      // LOCAL journal explicitly has no world point/generation; only approved neutral mappings can play.
      var disclosure=resultAudioDisclosure;
      LastAudioDiagnostic=audioPlayer.TryPlayCombatResult((long)row["cursor"],(string)row["actionId"],"HIT",((long)row["sequence"]).ToString(System.Globalization.CultureInfo.InvariantCulture),
       (long)row["tick"],result,mapping,()=>!destroying&&isActiveAndEnabled&&disclosure(recipient),null,out _);
     }
    }
   }catch(Exception error){LastAudioDiagnostic=error.GetType().Name+": "+error.Message;}
  }
  string CommandId()=>"roof-local:"+Guid.NewGuid().ToString("N");
  void Execute(Action operation,bool checkComposition=true){
   LastCommandSucceeded=false;LastException=null;
   try{if(!ready||!isActiveAndEnabled)throw new InvalidOperationException("Roof LOCAL composition is not active.");if(checkComposition)CheckExclusiveComposition();operation();LastCommandSucceeded=true;view.ReportError(null);}
   catch(Exception error){Fail(error);}
  }
  void Fail(Exception error){LastCommandSucceeded=false;LastException=error;autoAdvance=false;StopSimulation();view?.ReportError(error.GetType().Name+": "+error.Message);Debug.LogError("Roof LOCAL command/source failure: "+error);}
  public void AdvanceManual(long elapsedTimeSpanTicks,int maximumSteps)=>Execute(()=>owner.Advance(CommandId(),elapsedTimeSpanTicks,maximumSteps));
  void TogglePaused()=>Execute(()=>owner.SetPaused(CommandId(),!(bool)owner.Snapshot()["nativePaused"]));
  void ToggleSpeed()=>Execute(()=>{int speed=(int)owner.Snapshot()["nativeSnapshot"]["state"]["direct"]["clock"]["speedHalfUnits"];owner.SetSpeed(CommandId(),speed==4?2:4);});
  public void RestoreOriginal()=>Execute(()=>owner.Open());
  public void SetAutoAdvance(bool value){autoAdvance=value&&((string)lastFrame?["outcome"]=="RUNNING"||lastFrame==null);if(lastFrame!=null)view?.PresentState(lastFrame,ExecutionCount,HitCount,autoAdvance);RefreshSimulation();}
  bool CanSimulate=>ready&&!destroying&&isActiveAndEnabled&&autoAdvance&&lastFrame!=null&&(string)lastFrame["outcome"]=="RUNNING"&&(bool?)lastFrame["paused"]==false&&(bool?)lastFrame["recoveryRequired"]==false;
  void StopSimulation(){if(simulation==null)return;StopCoroutine(simulation);simulation=null;}
  void RefreshSimulation(){if(!CanSimulate){StopSimulation();return;}if(simulation==null)simulation=StartCoroutine(Simulate());}
  IEnumerator Simulate(){
   // First yield prevents a command from reentering the synchronous owner notification.
   yield return null;
   while(CanSimulate){
    long elapsed=(long)Math.Round(Time.unscaledDeltaTime*TimeSpan.TicksPerSecond);
    Execute(()=>owner.Advance(CommandId(),elapsed,256),false);
    yield return null;
   }
   simulation=null;
  }
  // Parent OnEnable can run before its View is enabled. Bind after all components activate,
  // so the synchronous original snapshot is not silently discarded by the View guard.
  void OnEnable(){
   BattleSceneRoot.CompositionChanged+=ValidateComposition;CompositionChanged+=ValidateComposition;
   SceneManager.sceneLoaded+=SceneLoaded;SceneManager.sceneUnloaded+=SceneUnloaded;
   CompositionChanged?.Invoke();
   if(started&&ready&&!destroying)rebind=StartCoroutine(RebindAfterEnable());
  }
  void ValidateComposition(){if(!ready||destroying||!isActiveAndEnabled)return;try{CheckExclusiveComposition();}catch(Exception error){Fail(error);}}
  void SceneLoaded(Scene scene,LoadSceneMode mode)=>ValidateComposition();
  void SceneUnloaded(Scene scene)=>ValidateComposition();
  IEnumerator RebindAfterEnable(){yield return null;rebind=null;if(started&&ready&&!destroying&&isActiveAndEnabled)try{CheckExclusiveComposition();BindPresentation();}catch(Exception error){Fail(error);}}
  void OnDisable(){
   BattleSceneRoot.CompositionChanged-=ValidateComposition;CompositionChanged-=ValidateComposition;
   SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneUnloaded-=SceneUnloaded;
   if(rebind!=null){StopCoroutine(rebind);rebind=null;}CompositionChanged?.Invoke();
   if(!started)return;UnbindPresentation();audioPlayer?.EndCombatScope();view?.SetVisible(false);
  }
  void OnDestroy(){destroying=true;UnbindPresentation();audioPlayer?.EndCombatScope();resultAudioMapping=null;resultAudioDisclosure=null;information?.Dispose();visualController?.Dispose();owner?.Dispose();owner=null;ready=false;}
  void OnApplicationPause(bool value){if(value&&ready&&!owner.RecoveryRequired)Execute(()=>owner.SetPaused(CommandId(),true));}
  public JObject SnapshotOrNull(){try{return owner?.Snapshot();}catch(Exception error){return new JObject{["unavailable"]=error.Message};}}
  int BasicRootCount(){int count=0;foreach(var root in FindObjectsByType<BattleSceneRoot>(FindObjectsSortMode.None))if(root.isActiveAndEnabled&&root.gameObject.scene.isLoaded)count++;return count;}
  int RoofRootCount(){int count=0;foreach(var root in FindObjectsByType<RoofBattleLocalScene>(FindObjectsSortMode.None))if(root.isActiveAndEnabled&&root.gameObject.scene.isLoaded)count++;return count;}
  public JObject Metadata()=>new JObject{["classification"]="LOCAL_NOT_OPERATIONAL",["contextId"]=contextId,["sourceHash"]=source?.SourceHash,["bindingHash"]=source?.Binding.OriginalHash,["assetContractHash"]=v03Assets==null?visualAssets?.ContractHash:null,["visualContract"]=v03Assets==null?"five-agent-v02":FourAgentV03Catalog.Version,["idleClockSource"]=v03Assets==null?"LEGACY_NEUTRAL":"COMMITTED_NATIVE_TICK",["storeDirectory"]=storeDirectory,["basicCe03RootCount"]=BasicRootCount(),["roofSceneCount"]=RoofRootCount(),["ownedNativeOwnerCount"]=owner==null?0:1,["supportedActorCount"]=ready?1:0,["actorId"]="op_eve",["visualSlotIndex"]=2,["directionIndex"]=0,["nativeFacingSupplied"]=false,["bodyActionPhaseSupplied"]=false,["neutralBodyOnly"]=true,["bodyPhase"]="NEUTRAL_IDLE_NO_WORLD_ATTACK_PHASE",["spatialFxSuppressed"]=true,["spatialStatus"]="DISTANCE_ONLY_LOCAL_INPUT_NO_WORLD_CONTACT",["fullFiveRoster"]=false,["operatingStart"]=false,["backgroundComposite"]=false,["worldProjectionSupplied"]=false,["executions"]=ExecutionCount,["hits"]=HitCount,["publishedOriginalEvents"]=PublishedEventCount,["lastDeliveredEventCount"]=LastDeliveredEventCount,["lastCommandSucceeded"]=LastCommandSucceeded,["lastError"]=LastException?.ToString(),["geometry"]=view?.GeometryMetadata()};
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  public void QaFailNextSave(){if(failNextSave)throw new InvalidOperationException("One QA save failure is already armed.");failNextSave=true;}
#endif
 }
}
