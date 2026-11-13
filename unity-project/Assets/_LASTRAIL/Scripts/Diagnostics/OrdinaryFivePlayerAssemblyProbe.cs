#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastRail.Diagnostics {
 // Explicit local Player evidence; captured START is never substituted for product admission.
 public sealed class OrdinaryFivePlayerAssemblyProbe:MonoBehaviour {
  [SerializeField] CombatPlayerAssetBinding binding;
  [SerializeField] TextAsset source;
  public void Initialize(CombatPlayerAssetBinding assets,TextAsset original){binding=assets;source=original;}
  static void Check(bool valid,string reason){if(!valid)throw new InvalidOperationException(reason);}
  static string QaPath(string value){string path=Path.GetFullPath(value??throw new ArgumentException("Explicit D QA path required."));Check(path.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase),"Output must remain inside D project QA.");return path;}
  static string Hash(string value)=>Ce03CommandContract.Sha(value);
  async void Start(){
   string output=null;bool ownsOutput=false;SceneMemoryRecorder memory=null;
   try{
    Application.runInBackground=true;Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
    if(Environment.GetEnvironmentVariable("LASTRAIL_OBSTACLE_TRIAL")=="1"){
     await ObstacleTrialPlayer.Run(QaPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_FIVE_QA")));Application.Quit(0);return;
    }
#if LASTRAIL_BATTLE_D_COMPARISON_BUILD
    if(Environment.GetEnvironmentVariable("LASTRAIL_BATTLE_D_PLAYER")!="1"){
     await BattleDPlayerCheck.InspectComparison();return;
    }
#endif
    if(Environment.GetEnvironmentVariable("LASTRAIL_BATTLE_D_PLAYER")=="1"){
     output=QaPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_FIVE_QA"));
     Check(!Directory.Exists(output),"Fresh battle D comparison output required.");
     Directory.CreateDirectory(output);ownsOutput=true;
     await BattleDPlayerCheck.Run(binding,output);Application.Quit(0);return;
    }
    string mode=Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_FIVE_MODE");Check(mode=="WRITE"||mode=="READ","Explicit WRITE or READ required.");
    output=QaPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_FIVE_QA"));Check(!Directory.Exists(output)&&!File.Exists(output),"Fresh output folder required for each process.");
    string restore=mode=="READ"?QaPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_FIVE_RESTORE_FROM")):null;
    var prior=restore==null?null:JObject.Parse(File.ReadAllText(Path.Combine(restore,"result.json")));
    using var process=System.Diagnostics.Process.GetCurrentProcess();string processStart=process.StartTime.ToUniversalTime().ToString("O");
    string sourceHash=Hash(source.text);
    if(prior!=null)Check((string)prior["mode"]=="WRITE"&&(string)prior["status"]=="ASSEMBLY_CHECKS_PASSED"&&(string)prior["sourceSha256"]==sourceHash&&(string)prior["manifestHash"]==binding.ManifestHash&&((int)prior["pid"]!=process.Id||(string)prior["processStartUtc"]!=processStart),"Matching writer evidence from a different OS process required.");
    var fixture=JObject.Parse(source.text);Check((string)fixture["classification"]=="SYNTHETIC_ACCOUNT_INTERNAL_START_PRODUCTION_INITIALIZATION_HTTP_NOT_PRODUCT_ADMISSION","Unexpected source classification.");
    var start=(JObject)fixture["originalStart"];var original=OrdinaryFiveStartInput.Read(start,(string)fixture["accountId"],(string)fixture["runId"]);
    var factory=OrdinaryFiveCombatFactory.FromInitialization(original,fixture["initializationResponse"].ToString(),500000L);
    // The captured source lacks a durable account journal. This QA-only envelope retains its exact START.
    var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=original.RunId,["runStatus"]="ACTIVE",["startResponse"]=start.DeepClone(),["errorCode"]=null};
    var journal=new PendingAccountAction(original.AccountId,"departure-start",(string)start["operationPreviewId"],0,"ordinary-five-assembly-local-source",new JObject{["operationPreviewId"]=start["operationPreviewId"].DeepClone()}.ToString(),response.ToString(Newtonsoft.Json.Formatting.None));
    var input=FindFirstObjectByType<EventSystem>();Check(input!=null&&input.isActiveAndEnabled,"Generated scene must own an active EventSystem.");
    Directory.CreateDirectory(output);ownsOutput=true;memory=SceneMemoryRecorder.CreateExternal(Path.Combine(output,"memory"),true);
    if(mode=="WRITE")await CaptureReadyUi(original,output);
    using var artLease=binding.Acquire(binding.ManifestHash,original.Actors.OrderBy(a=>a.Slot).Select(a=>a.OperatorId).ToArray());
    var approvedFrames=artLease.ReadAssets().OperatorFrames;
    var cycles=new JArray();
    for(int cycle=0;cycle<3;cycle++){
     memory.BeginWindow();await Task.Delay(100);memory.Checkpoint(cycle+"-before");
     string prefix=Path.Combine(output,"cycle-"+cycle),checkpointPath=prefix+"-checkpoint.json";
     JObject expected=prior==null?null:(JObject)prior["cycles"][cycle];
     if(restore!=null){string savedPath=Path.Combine(restore,"cycle-"+cycle+"-checkpoint.json");Check(Hash(File.ReadAllText(savedPath))==(string)expected["checkpointFileSha256"],"Writer checkpoint bytes changed.");File.Copy(savedPath,checkpointPath);}
     var checkpoint=new OrdinaryFiveCombatCheckpoint(CombatCheckpointStore.CreateExplicitLocalQaFile(checkpointPath),journal,factory,new AppBuildSnapshot("ordinary-five-assembly-20261028",1));
     if(mode=="WRITE")checkpoint.Save(factory.Create(),0);
     var driver=CombatSceneDriver.ForOrdinaryCombatStart(checkpoint,()=>true);driver.Open();string before=driver.StateJson;
     if(expected!=null)Check(Hash(before)==(string)expected["savedStateSha256"],"Restored first frame differs from saved authoritative state.");
     var scene=new GameObject("OrdinaryFiveActualPlayerScene").AddComponent<CombatPlayerScene>();
     bool foregroundReady=false;
     using var controller=new OrdinaryPlayerBattleController(original,driver,binding,()=>true,()=>foregroundReady);Exception failure=null;CombatSceneResult terminal=null;int lobbyReturns=0;
     try{
      memory.BeginWindow();controller.Present(scene,input,()=>lobbyReturns++,result=>terminal=result,error=>failure=error);
      Check(before==driver.StateJson,"Opening presentation mutated combat.");
      CombatScenePlayerTrial.Capture(scene.SceneCamera,prefix+"-foreground-held.png");
      Check(!scene.IsRunning&&before==driver.StateJson,"Injected background readiness must not begin combat.");
      scene.View.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Speed").onClick.Invoke();
      Check(before==driver.StateJson,"Preparation must not execute or queue speed input.");
      foregroundReady=true;typeof(CombatPlayerScene).GetMethod("AdvanceFrame",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(scene,new object[]{0d});
      Check(!scene.IsRunning&&before==driver.StateJson,"Foreground still requires the first committed render.");
      CombatScenePlayerTrial.Capture(scene.SceneCamera,prefix+"-first-frame.png");
      var readiness=new JObject{["isRunning"]=scene.IsRunning,["stateUnchanged"]=before==driver.StateJson,["failure"]=failure?.ToString()};
      foreach(string field in new[]{"phase","suspended","applicationPaused","renderFailure"})readiness[field]=typeof(CombatPlayerScene).GetField(field,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(scene)?.ToString();
      File.WriteAllText(prefix+"-readiness.json",readiness.ToString());
      Check(failure==null&&scene.IsOpen&&!scene.IsRunning&&before==driver.StateJson,"First actual render remains preparation until native execution is ready.");
      var selected=new JArray();var actors=((JArray)driver.HudState["actors"]).Where(a=>!(bool)a["enemy"]&&!(bool)a["train"]&&!(bool)a["device"]).ToArray();Check(actors.Length==5,"Exactly five original operators required.");
      foreach(var actor in actors){
       string id=(string)actor["id"];Check(original.Actors.Any(a=>a.OperatorId==id),"HUD actor is outside original START.");
       var card=scene.View.GetComponentsInChildren<Button>(true).Single(b=>b.name=="SelectOperator_"+id);var world=scene.View.GetComponentsInChildren<Button>(true).Single(b=>b.name==id);
       var supplied=approvedFrames?.Find(id);
       Check(card.transform.parent.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t=>t.name=="ChargePercent").text=="미확인","Missing node actions must not be displayed as confirmed zero charge.");
       if(supplied?.Portrait!=null)Check(card.transform.parent.GetComponentsInChildren<RawImage>().Any(p=>p.name=="ApprovedSourcePortraitCrop"&&p.texture==supplied.Portrait),"The approved portrait was overwritten by legacy artwork.");
       if(supplied?.HasIdle==true)Check(world.GetComponentsInChildren<RawImage>().Any(p=>supplied.IdleFrames.Contains(p.texture)),"The approved body is absent from its actual actor view.");
       card.onClick.Invoke();var frame=scene.View.InformationFrame;
       Check(frame?.Target?.ActorId==id&&frame.Target.OperatorId==(string)actor["visual"]&&frame.Snapshot?.Health?.CurrentHp==(int)actor["hp"]&&frame.Snapshot.Health.Shield==(int)actor["shield"],"Card information target/HP/shield differs from original actor.");
       scene.View.Refresh();var meters=scene.View.GetComponentsInChildren<CombatMeter>(true);var cardHp=meters.Single(m=>m.name=="OperatorHp"&&m.transform.parent.name=="OperatorCard_"+id);var worldHp=meters.Single(m=>m.name=="Health"&&m.transform.parent.name==id);
       double denominator=Math.Max((int)actor["maxHp"],(long)(int)actor["hp"]+(int)actor["shield"]);float health=(float)((int)actor["hp"]/denominator),shield=(float)((int)actor["shield"]/denominator);
       Check(cardHp.Value==health&&worldHp.Value==health&&cardHp.ShieldValue==shield&&worldHp.ShieldValue==shield,"Card and world health bars disagree with the selected original actor.");
       CombatScenePlayerTrial.Capture(scene.SceneCamera,prefix+"-selected-"+selected.Count+".png");
       world.onClick.Invoke();Check(scene.View.InformationFrame.Target==null,"Same world actor must toggle selection exactly once.");
       world.onClick.Invoke();Check(scene.View.InformationFrame.Target?.ActorId==id,"World actor selection differs from card.");card.onClick.Invoke();Check(scene.View.InformationFrame.Target==null,"Same card must toggle once.");
       selected.Add(new JObject{["actorId"]=id,["visualId"]=actor["visual"].DeepClone(),["hp"]=actor["hp"].DeepClone(),["shield"]=actor["shield"].DeepClone()});
      }
      Check(before==driver.StateJson,"Selection or screenshot mutated the combat owner.");
      memory.Checkpoint(cycle+"-rendered-selected",continueWindow:true);
      if(mode=="WRITE"){
       var speed=scene.View.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Speed");speed.onClick.Invoke();speed.onClick.Invoke();
       Check(((JArray)driver.CaptureOrdinaryReplayInputs().Inputs["inputs"]).Count==2,"Two installed speed callbacks must create two semantic inputs.");
       double until=Time.realtimeSinceStartupAsDouble+.4;while(Time.realtimeSinceStartupAsDouble<until){await Task.Yield();if(failure!=null)throw failure;Check(terminal==null&&scene.IsOpen,"Unexpected combat completion in source-only assembly scope.");}
      }
      if(failure!=null)throw failure;var hud=driver.HudState;
      var camera=scene.SceneCamera;var view=scene.View;
      if(mode=="WRITE"&&Environment.GetEnvironmentVariable("LASTRAIL_LEAVE_INTENT_QA")=="1"){
       var pause=view.GetComponent<CombatPauseController>();pause.Open();
       Button Menu(string name)=>view.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
       CombatScenePlayerTrial.Capture(camera,prefix+"-pause-menu.png");
       Check(!Menu("PauseAbandon").interactable,"No authoritative abandon producer is bound.");
       string paused=driver.StateJson;Menu("PauseLobby").onClick.Invoke();
       Check(lobbyReturns==0&&scene.IsOpen&&driver.StateJson==paused,"Confirmation must preserve the current battle.");
       CombatScenePlayerTrial.Capture(camera,prefix+"-leave-confirmation.png");
       Menu("PauseContinue").onClick.Invoke();Check(!pause.IsOpen&&!(bool)driver.HudState["paused"]&&lobbyReturns==0,"Cancel must resume the same memory without leaving.");
       pause.Open();Menu("PauseLobby").onClick.Invoke();var confirm=Menu("PauseLobby");confirm.onClick.Invoke();confirm.onClick.Invoke();
       Check(lobbyReturns==1&&!scene.IsOpen&&!controller.IsCurrent,"Confirmed leave must retire and close exactly once.");
      }
      controller.ClosePresentation();
      string saved=driver.StateJson;File.WriteAllText(prefix+"-state.json",saved);
      var replayInputs=driver.CaptureOrdinaryReplayInputs();
      Check(replayInputs.StateJson==saved,"Input export differs from committed checkpoint.");
      if(expected!=null)Check(JToken.DeepEquals(replayInputs.Inputs,expected["replayInputs"]),"Restored semantic input sequence differs from writer.");
      string continuation;
      if(mode=="WRITE"){var baseline=BasicCombatSimulation.RestoreLocalState(saved,factory.Create);baseline.Advance(5000000,20);continuation=baseline.CaptureLocalState();File.WriteAllText(prefix+"-expected-continuation.json",continuation);}
      else {driver.Advance(5000000,20);continuation=driver.StateJson;Check(continuation==File.ReadAllText(Path.Combine(restore,"cycle-"+cycle+"-expected-continuation.json")),"Restored execution differs from uninterrupted execution.");}
      cycles.Add(new JObject{["replayInputs"]=replayInputs.Inputs,["cycle"]=cycle,["firstFrameStateSha256"]=Hash(before),["savedStateSha256"]=Hash(saved),["checkpointFileSha256"]=Hash(File.ReadAllText(checkpointPath)),["continuationSha256"]=Hash(continuation),["tick"]=hud["tick"].DeepClone(),["executionPhase"]=hud["executionPhase"]?.DeepClone(),["fullCombatReady"]=hud["fullCombatReady"]?.DeepClone(),["manualsAvailability"]=hud["manualsAvailability"]?.DeepClone(),["nativeSkills"]=hud["skills"].DeepClone(),["selected"]=selected});
      memory.Checkpoint(cycle+"-closed");await Task.Delay(100);Check(camera==null&&view==null&&scene.transform.childCount==0,"Owned presentation survived destruction.");
      Check(!UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>().Any(g=>g.name=="OwnedOrdinaryRoofRenderStage")&&!UnityEngine.Resources.FindObjectsOfTypeAll<RenderTexture>().Any(t=>t.name=="OwnedOrdinaryRoofTexture"),"Owned roof render resources survived closure.");
      memory.Checkpoint(cycle+"-after-destruction");
     }finally{controller.ClosePresentation();Destroy(scene.gameObject);}
     await Task.Yield();
    }
    File.WriteAllText(Path.Combine(output,"result.json"),new JObject{["status"]="ASSEMBLY_CHECKS_PASSED",["mode"]=mode,["pid"]=process.Id,["processStartUtc"]=processStart,["sourceSha256"]=sourceHash,["manifestHash"]=binding.ManifestHash,["scope"]="ACTUAL_ORDINARY_PLAYER_HOST_FROM_CAPTURED_SOURCE",["render"]="ACTUAL_CAMERA_OFFSCREEN_NOT_PHYSICAL_WINDOW_OR_TOUCH_QA",["art"]="AUTHORED_PRIMITIVE_3_PLUS_2_ROOF_NOT_FINAL_ART_OR_COMBAT_GEOMETRY",["selectionInput"]="INSTALLED_BUTTON_CALLBACKS_NOT_PHYSICAL_RAYCAST",["checkpointStorage"]="EXPLICIT_LOCAL_QA_PLAINTEXT_PRODUCT_CHECKPOINT_VALIDATOR",["qaJournalEnvelope"]=true,["realAdmissionTested"]=false,["secureEncryptionTested"]=false,["networkRequestsPerformed"]=0,["threeApiRunsTested"]=false,["fullCombatTested"]=false,["foregroundCheck"]="INJECTED_VISIBILITY_PREDICATE_NOT_OS_FOCUS",["readyUiCheck"]="ACTUAL_UI_CAMERA_WITH_SYNTHETIC_DELAY_NO_API",["forcedGc"]=false,["cycles"]=cycles}.ToString());
    Application.Quit(0);
   }catch(Exception error){if(ownsOutput)File.WriteAllText(Path.Combine(output,"failure.json"),new JObject{["status"]="FAILED",["error"]=error.ToString()}.ToString());Debug.LogException(error);Application.Quit(1);}
   finally{if(memory!=null)Destroy(memory.gameObject);}
  }
  async Task CaptureReadyUi(OrdinaryFiveStartInput original,string output){
   using var assets=binding.Acquire(binding.ManifestHash,original.Actors.OrderBy(a=>a.Slot).Select(a=>a.OperatorId).ToArray());
   var root=new GameObject("ReadyUiPlayerCheck",typeof(RectTransform),typeof(Canvas));var cameraObject=new GameObject("ReadyUiCamera",typeof(Camera));
   try{
    var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
    var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
    ((RectTransform)root.transform).sizeDelta=new Vector2(1280,720);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
    var safeRect=(RectTransform)safe.transform;safeRect.anchorMin=Vector2.zero;safeRect.anchorMax=Vector2.one;safeRect.offsetMin=safeRect.offsetMax=Vector2.zero;
    var deck=LobbyCommandDeck.Attach(root,assets.ReadAssets().Font,"LOCAL UI QA",0,Array.Empty<MissionShellMissionRowState>(),null,null);
    int calls=0;var pending=new TaskCompletionSource<bool>();deck.BindOrdinaryPreparation(id=>{calls++;return pending.Task;});deck.OpenDepartureRecovery(original.RunId);
    Button Prepare()=>root.GetComponentsInChildren<Button>().Single(b=>b.name=="PrepareOrdinaryCombat");
    CombatScenePlayerTrial.Capture(camera,Path.Combine(output,"ready-ui-idle.png"));Prepare().onClick.Invoke();Prepare().onClick.Invoke();Check(calls==1&&!Prepare().interactable,"Ready UI duplicated a pending request.");
    CombatScenePlayerTrial.Capture(camera,Path.Combine(output,"ready-ui-pending.png"));deck.CloseDialog();pending.SetResult(true);await Task.Yield();Check(!deck.IsDialogOpen,"Canceled preparation reopened its view.");
    deck.BindOrdinaryPreparation(id=>Task.FromException(new OrdinaryNodeStartUnavailableException()));deck.OpenDepartureRecovery(original.RunId);Prepare().onClick.Invoke();await Task.Yield();Check(!Prepare().interactable,"Missing origin must not offer endless retries.");
    CombatScenePlayerTrial.Capture(camera,Path.Combine(output,"ready-ui-unavailable.png"));
   }finally{root.SetActive(false);Destroy(root);Destroy(cameraObject);}
  }
 }
}
#endif
