#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Newtonsoft.Json.Linq;
using LastRail.Battle;
[InitializeOnLoad]
public static class CombatPresentationCapture {
 const string Key="LastRail.PresentationCapture.20260912";
 public static string Output=>SessionState.GetString(Key+".output","");
 static CombatPresentationCapture(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Run(){
  var args=Environment.GetCommandLineArgs();int n=Array.IndexOf(args,"-presentationOutput");
  if(!Application.isBatchMode||n<0||n+1>=args.Length||Directory.Exists(args[n+1]))throw new InvalidOperationException("Fresh explicit batch output required");
  SessionState.SetString(Key+".output",args[n+1]);Directory.CreateDirectory(Output);
  ConfigureAvAssets();
  // Do not regenerate the approved art or prefab.
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
 }
 static void ConfigureAvAssets(){
  var asset=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");
  if(asset==null||asset.ExteriorMap==null)throw new InvalidOperationException("Approved map missing");
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-distantBattle")>=0 && (asset.MapId!="canyon"||asset.DistantFar==null||asset.DistantTerrainLayout==null))throw new InvalidOperationException("Installed canyon required; capture must not replace map");
  foreach(var id in CombatAbilityAudioSet.InitialAbilityIds){var path="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/Audio/"+id+".wav";var audio=AssetImporter.GetAtPath(path) as AudioImporter;if(audio==null)continue;var sample=audio.defaultSampleSettings;sample.compressionFormat=AudioCompressionFormat.PCM;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;audio.defaultSampleSettings=sample;audio.SaveAndReimport();}
 }
 static void Changed(PlayModeStateChange state){
  if(!SessionState.GetBool(Key,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode){try{new GameObject("PresentationCapture").AddComponent<CombatPresentationRecorder>().Begin();}catch(Exception e){File.WriteAllText(Output+"/failed.txt",e.ToString());EditorApplication.ExitPlaymode();}}
  if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);Time.captureFramerate=0;EditorApplication.Exit(File.Exists(Output+"/complete.json")&&!File.Exists(Output+"/failed.txt")?0:1);}
 }
}
public sealed class CombatPresentationRecorder:MonoBehaviour {
 const int Fps=60;
 int Length=>PolishStill?60:RigCastDown?240:RigDown?840:SkillRig?240:FigureComparison?180:TravelPreview?360:TravelCap?300:TravelRecovery?1800:DistantBattle?1440:ChargeCycle?360:WorldLoop && Array.IndexOf(Environment.GetCommandLineArgs(),"-meterComparison")<0?1560:132;
 static bool PolishStill=>Array.IndexOf(Environment.GetCommandLineArgs(),"-polishStill")>=0;
 static bool PolishFinal=>Array.IndexOf(Environment.GetCommandLineArgs(),"-polishFinal")>=0;
 static bool ChargeCycle=>Array.IndexOf(Environment.GetCommandLineArgs(),"-chargeCycle")>=0;
 static bool HudPolish=>Array.IndexOf(Environment.GetCommandLineArgs(),"-hudPolish")>=0;
 static bool AllAudio=>Array.IndexOf(Environment.GetCommandLineArgs(),"-allAbilityAudio")>=0;
 static bool TravelCap=>Array.IndexOf(Environment.GetCommandLineArgs(),"-travelCap")>=0;
 static bool TravelPreview=>Array.IndexOf(Environment.GetCommandLineArgs(),"-travelPreview")>=0;
 static bool RigCastDown=>Array.IndexOf(Environment.GetCommandLineArgs(),"-rigCastDown")>=0;
 static bool RigDown=>RigCastDown||Array.IndexOf(Environment.GetCommandLineArgs(),"-rigDownRecovery")>=0;
 static bool AllOperatorSkills=>Array.IndexOf(Environment.GetCommandLineArgs(),"-allOperatorSkills")>=0;
 static bool AllOperatorRigs=>RigDown||AllOperatorSkills||Array.IndexOf(Environment.GetCommandLineArgs(),"-allOperatorRigs")>=0;
 static bool CompareRig=>DistantBattle||SkillRig;
 static bool ConnectedRig=>Array.IndexOf(Environment.GetCommandLineArgs(),"-kyleConnected")>=0;
 static bool SkillRig=>AllOperatorSkills||Array.IndexOf(Environment.GetCommandLineArgs(),"-kyleRigSkill")>=0;
 static bool OptimizedRig=>Array.IndexOf(Environment.GetCommandLineArgs(),"-kyleRigOptimized")>=0;
 static bool RigMotion=>AllOperatorRigs||SkillRig||Array.IndexOf(Environment.GetCommandLineArgs(),"-kyleRigMotion")>=0;
 static bool RigDiagnostic=>RigMotion||Array.IndexOf(Environment.GetCommandLineArgs(),"-kyleRigDiagnostic")>=0;
 static bool FigureComparison=>!RigMotion&&RigDiagnostic||Array.IndexOf(Environment.GetCommandLineArgs(),"-figureComparison")>=0;
 static bool TravelRecovery=>!SkillRig&&(PolishStill||PolishFinal||RigMotion||FigureComparison||TravelPreview||TravelCap||Array.IndexOf(Environment.GetCommandLineArgs(),"-travelRecovery")>=0);
 static bool DistantBattle=>TravelRecovery||Array.IndexOf(Environment.GetCommandLineArgs(),"-distantBattle")>=0;
 static bool WorldLoop=>DistantBattle||Array.IndexOf(Environment.GetCommandLineArgs(),"-worldLoop")>=0;
 readonly string[] groups=RigCastDown?new[]{"skills"}:AllOperatorSkills?new[]{"skills","skills","skills","skills","skills","ultimate","ultimate","ultimate","ultimate","ultimate"}:SkillRig?new[]{"skills","ultimate"}:FigureComparison?new[]{"ultimate","ultimate","ultimate"}:WorldLoop||ChargeCycle?new[]{"ultimate"}:AllAudio?new[]{"skills","skills","skills","skills","skills","manuals","manuals","manuals","ultimate","ultimate","ultimate","ultimate","ultimate"}:HudPolish?new[]{"manuals","manuals","manuals","ultimate"}:new[]{"skills","skills","skills","skills","skills","manuals","manuals","manuals","ultimate","ultimate","ultimate"};
 readonly int[] indices=RigCastDown?new[]{1}:AllOperatorSkills?new[]{0,1,2,3,4,0,0,0,0,0}:SkillRig?new[]{0,0}:FigureComparison?new[]{0,0,0}:WorldLoop||ChargeCycle?new[]{0}:AllAudio?new[]{0,1,2,3,4,0,1,2,0,0,0,0,0}:HudPolish?new[]{0,1,2,0}:new[]{0,1,2,3,4,0,1,2,0,0,0};
 EventSystem inputEvents;string actualCommandId="presentation-capture-command";
 Camera cam;RenderTexture target;Texture2D pixels;GameObject hud;CombatSceneDriver driver;ApprovedCombatView view;
 BasicCombatSimulation reference; int referenceFrames; JArray nativeInputs=new JArray();
 int segment,frame,total,castFrame=-1;bool submitted;string ability;JObject draft;System.Diagnostics.Process encoder;
 StreamWriter trace;BinaryWriter audio;bool audioRecording;double audioPeak;long audioSamples;JArray results=new JArray();string ultimateBaseline;
 readonly List<string> resolvedVisuals=new List<string>();readonly List<string> cues=new List<string>();readonly List<string> effectCues=new List<string>();
 int readyFrame=-1,restoreFrame=-1;bool saw96,saw100,sawSpent,sawLit,restoreEqual;
 public void Begin(){
  Time.captureFramerate=Fps;
  inputEvents=new GameObject("CaptureEvents").AddComponent<EventSystem>();
  var config=AudioSettings.GetConfiguration();config.speakerMode=AudioSpeakerMode.Stereo;config.sampleRate=48000;AudioSettings.Reset(config);
  cam=new GameObject("CaptureCamera").AddComponent<Camera>();cam.gameObject.AddComponent<AudioListener>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
  target=new RenderTexture(720,1280,24);cam.targetTexture=target;pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);
  draft=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));draft["ownedSelection"]["accountId"]="presentation-local-qa";
  var start=new System.Diagnostics.ProcessStartInfo{
   FileName="C:/Users/dlstj/AppData/Roaming/Python/Python39/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe",
   Arguments="-hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 720x1280 -framerate 60 -i pipe:0 -vf vflip -an -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p -movflags +faststart \""+CombatPresentationCapture.Output+"/video.mp4\"",
   UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden,RedirectStandardInput=true};
  encoder=System.Diagnostics.Process.Start(start);trace=new StreamWriter(CombatPresentationCapture.Output+"/frames.jsonl");audio=new BinaryWriter(File.Create(CombatPresentationCapture.Output+"/audio.f32"));
  audioRecording=AudioRenderer.Start();if(!audioRecording)throw new InvalidOperationException("AudioRenderer not available");
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-v03ShieldOnly")>=0)segment=8;
  StartSegment();
 }
 BasicCombatSimulation UnmodifiedFresh(){var s=BasicCombatSimulation.FromPreparedProfileForLocalTest(draft,(JObject)draft["ownedSelection"],"presentation-local-qa",1);s.ConfigureLocalLeaderCharge(4);return s;}
 BasicCombatSimulation Fresh(){var model=UnmodifiedFresh();if(!RigDown)return model;
  // Explicit offline starting-condition coverage. All following impacts/recovery use the unchanged model.
  var raw=JObject.Parse(model.CaptureLocalState());foreach(var actor in (JArray)raw["actors"]){string id=(string)actor["id"];if(id.StartsWith("op_"))actor["vitals"]["hp"]=1;if(id.StartsWith("op_")||id=="train_a"||id.StartsWith("device"))actor["ready"]=20000;}
  return BasicCombatSimulation.RestoreLocalState(raw.ToString(),UnmodifiedFresh);
 }
 JToken Row(JObject state)=>groups[segment]=="ultimate"?state["ultimate"]:state[groups[segment]][indices[segment]];
 void StartSegment(){
  if(hud!=null){hud.SetActive(false);Destroy(hud);}frame=0;castFrame=-1;submitted=false;cues.Clear();effectCues.Clear();resolvedVisuals.Clear();
  if(AllAudio&&segment>=8){var selected=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"}[segment-8];draft["draft"]["template"]["selectedUltimateOperatorId"]=selected;draft["ownedSelection"]["ultimateSelection"]["operatorId"]=selected;}
  if(SkillRig){var selected=AllOperatorSkills&&segment>=5?new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"}[segment-5]:"op_kyle";draft["draft"]["template"]["selectedUltimateOperatorId"]=selected;draft["ownedSelection"]["ultimateSelection"]["operatorId"]=selected;}
  var warm=Fresh();if(RigCastDown)while(warm.Tick<70)warm.Advance(500000,1);if(TravelCap){while(warm.Tick<1680&&warm.Outcome==BasicCombatOutcome.Running)warm.Advance(500000,1);if(warm.Tick!=1680)throw new InvalidOperationException("Cap sample could not reach natural84s");}
  // Skip unrecorded waiting using the real simulation. Never inject charge/HP/cast success.
  for(int i=0;!DistantBattle&&i<5000;i++){
   var state=warm.ReadHudState();var row=Row(state);
   if(ChargeCycle){if((int)row["charge"]==96)break;if((int)row["charge"]>96)throw new InvalidOperationException("No natural 96% checkpoint");warm.Advance(500000,1);continue;}
   bool warning=((JArray)state["actors"]).Any(a=>(bool)a["enemy"]&&(bool)a["attacking"]&&(long)a["attackDue"]<=(long)state["tick"]+4);
   if((bool)row["enabled"]&&(groups[segment]!="ultimate"||!warning)){
    // Readiness alone does not guarantee a target. Probe an isolated copy at the
    // exact recorded input time, then record the original unmodified checkpoint.
    var probe=BasicCombatSimulation.RestoreLocalState(warm.CaptureLocalState(),Fresh);
    probe.Advance(2000000,4);var input=Row(probe.ReadHudState());
    if(HudPolish&&!(bool)input["enabled"]){warm.Advance(500000,1);continue;}
    probe.RequestSkill("capture-probe",(string)input["actor"],(string)input["ability"]);
    for(int j=0;j<8&&probe.CommandStatus("capture-probe")=="QUEUED";j++)probe.Advance(500000,1);
    var projected=probe.ReadHudState();bool blocked=((JArray)projected["actors"]).Any(a=>(bool)a["enemy"]&&(bool)a["attacking"]&&(long)a["attackDue"]<=(long)projected["tick"]+4);
    if(probe.CommandStatus("capture-probe")=="CASTING"&&(groups[segment]!="ultimate"||!blocked))break;
   }
   warm.Advance(500000,1);
  }
  if(!DistantBattle&&!ChargeCycle&&!(bool)Row(warm.ReadHudState())["enabled"])throw new InvalidOperationException("Case never became ready");
  string checkpoint=warm.CaptureLocalState();
  driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  // The driver's restore factory must remain the original tick-zero configuration.
  // Fast-forward the unrecorded wait through the same published transition port.
  while(driver.Tick<warm.Tick)driver.Advance(500000,1);
  if(driver.StateJson!=checkpoint)throw new InvalidOperationException("Warmup differs from original driver stream");
  if(WorldLoop)driver.SetSpeed(TravelRecovery?2:4);
  if(CompareRig){reference=BasicCombatSimulation.RestoreLocalState(driver.StateJson,Fresh);File.WriteAllText(CombatPresentationCapture.Output+"/initial-"+segment+".json",driver.StateJson);File.WriteAllText(CombatPresentationCapture.Output+"/initial-state.json",driver.StateJson);}
  hud=new GameObject("ActualPresentation",typeof(RectTransform),typeof(Canvas));view=hud.AddComponent<ApprovedCombatView>();
  view.SetCapturePresentationClock(()=>total/(double)Fps);
  bool used=false;var policy=new CombatPresentationPolicy(()=>used,()=>{used=true;return true;});policy.Mode=!AllAudio&&!SkillRig&&segment==9?CombatPresentationMode.Simplified:CombatPresentationMode.Full;
  view.ConfigurePresentation(policy);view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
  if(!WorldLoop||DistantBattle)view.ConfigurePresentationAudio(CombatAbilityAudioSet.LoadResources(),.65f);
  view.PresentationCue+=(kind,id)=>{cues.Add(kind+":"+id);if(castFrame<0)castFrame=frame;};view.EffectPresentationCue+=(kind,id)=>effectCues.Add(kind+":"+id);view.ResolvedVisualCue+=(shape,id,targetId)=>resolvedVisuals.Add(shape+":"+id+":"+targetId);
  if(FigureComparison)view.SetCaptureFigureScale(RigDiagnostic?.86f:new[]{1f,.92f,.86f}[segment]);if(RigDiagnostic&&!RigDown)view.SetCaptureKyleRig(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_LASTRAIL/UI/Diagnostics/KyleParts/parts-source-v01.png"),JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/UI/Diagnostics/KyleParts/"+(ConnectedRig?"rig-connected-union-v01.json":OptimizedRig?"rig-exact-union-v01.json":"rig-diagnostic-v01.json"))),(RigMotion?0:new[]{0f,.5f,1f}[segment]));if(AllOperatorRigs)foreach(var id in RigDown?new[]{"kyle","reina","eve","sera","mira"}:new[]{"reina","eve","sera","mira"})view.SetCaptureOperatorRig("op_"+id,AssetDatabase.LoadAssetAtPath<Texture2D>((id=="kyle"?"Assets/_LASTRAIL/UI/Diagnostics/KyleParts/parts-source-v01.png":"Assets/_LASTRAIL/UI/Diagnostics/OperatorParts/"+id+"-source-v01.png")),JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/UI/Diagnostics/OperatorParts/"+id+"-rig-exact-v04.json")));if(RigMotion)view.EnableCaptureRigMotion();if(Array.IndexOf(Environment.GetCommandLineArgs(),"-cabinSpread")>=0)view.SetCaptureCabinSpread();var captureArgs=Environment.GetCommandLineArgs();int figureArg=Array.IndexOf(captureArgs,"-figureScale");if(figureArg>=0)view.SetCaptureFigureScale(float.Parse(captureArgs[figureArg+1],System.Globalization.CultureInfo.InvariantCulture));var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
  ability=(string)Row(driver.HudState)["ability"];
  var labelGo=new GameObject("QAContext",typeof(RectTransform));labelGo.transform.SetParent(hud.transform,false);var r=labelGo.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-66);r.sizeDelta=new Vector2(710,23);
  var label=labelGo.AddComponent<TextMeshProUGUI>();label.font=hud.GetComponentInChildren<TMP_Text>().font;label.fontSize=13;label.color=new Color(1,.82f,.42f);label.alignment=TextAlignmentOptions.Center;label.text=RigMotion?"LOCAL QA · 분리 파트 실제 공격 진단 · 제품 미적용":RigDiagnostic?"LOCAL QA · 분리 파트 조립 "+new[]{"대기","중간","확장"}[segment]+" · 제품 미적용":FigureComparison?"LOCAL QA · 캐릭터 크기 "+(view.CabinFigureScale*100).ToString("0")+"% · 동일 전투 상태":DistantBattle?"LOCAL QA · 협곡 원거리 / 실제 전투 판정":ChargeCycle?"LOCAL QA · 실제 96→100→사용→소모 / 같은 프로세스 복원":WorldLoop?"LOCAL QA · 단일 태양 / 연속 주행": "LOCAL QA · "+CombatSkillNames.Get(ability)+(AllAudio?" · 개별 시전음":segment==9?" · 간소화":segment==10?" · 건너뛰기":" · 시전 연출");label.raycastTarget=false;
 }
 void LateUpdate(){
  if(driver==null)return;
  try{
   var buttonRoot=ChargeCycle?hud.transform.Find("ApprovedBattle/ultimate0"):HudPolish&&groups[segment]=="manuals"?hud.transform.Find("ApprovedBattle/Command"+indices[segment]):null;
   if(ChargeCycle&&(int)driver.HudState["ultimate"]["charge"]==100&&readyFrame<0)readyFrame=frame;
   var pointer=new PointerEventData(inputEvents){button=PointerEventData.InputButton.Left,pointerId=1};
   if(buttonRoot!=null&&(!ChargeCycle&&frame==4||ChargeCycle&&readyFrame>=0&&frame==readyFrame+30))ExecuteEvents.Execute(buttonRoot.gameObject,pointer,ExecuteEvents.pointerDownHandler);
   if((ChargeCycle?readyFrame>=0&&frame>=readyFrame+36:frame>=12)&&!submitted&&!WorldLoop){
    if(buttonRoot!=null){
     ExecuteEvents.Execute(buttonRoot.gameObject,pointer,ExecuteEvents.pointerUpHandler);
     ExecuteEvents.Execute(buttonRoot.gameObject,pointer,ExecuteEvents.pointerClickHandler);
     var commands=(JArray)JObject.Parse(driver.StateJson)["commands"];
     if(commands.Count!=1)throw new InvalidOperationException("Native button did not submit exactly once");actualCommandId=(string)commands[0]["id"];
    }else{var row=Row(driver.HudState);actualCommandId="presentation-capture-command";driver.RequestSkill(actualCommandId,(string)row["actor"],ability);if(SkillRig)reference.RequestSkill(actualCommandId,(string)row["actor"],ability);}
    submitted=true;
   }
   if(RigCastDown&&frame==12){actualCommandId="diagnostic-cast-down";driver.RequestSkill(actualCommandId,"op_reina","op_reina.active");reference.RequestSkill("diagnostic-cast-down","op_reina","op_reina.active");nativeInputs.Add(new JObject{["frame"]=frame,["method"]="Local actual RequestSkill during diagnostic low HP",["ability"]="op_reina.active"});}
   if(DistantBattle&&!RigDown){
    if(frame==(TravelRecovery?480:400)){driver.SetPaused(true);reference.SetPaused(true);}
    if(frame==(TravelRecovery?510:430)){driver.SetPaused(false);reference.SetPaused(false);}
    if(frame==600){driver.SetSpeed(TravelRecovery?3:2);reference.SetSpeed(TravelRecovery?3:2);}
    if(TravelRecovery&&frame==1200){driver.SetSpeed(4);reference.SetSpeed(4);}
    if(!PolishFinal&&(frame==120||frame==720)){
     var row=driver.HudState["manuals"][0];var node=hud.transform.Find("ApprovedBattle/Command0");
     var before=((JArray)JObject.Parse(driver.StateJson)["commands"]).Select(c=>(string)c["id"]).ToArray();
     ExecuteEvents.Execute(node.gameObject,pointer,ExecuteEvents.pointerDownHandler);
     ExecuteEvents.Execute(node.gameObject,pointer,ExecuteEvents.pointerUpHandler);
     ExecuteEvents.Execute(node.gameObject,pointer,ExecuteEvents.pointerClickHandler);
     var added=((JArray)JObject.Parse(driver.StateJson)["commands"]).Where(c=>!before.Contains((string)c["id"])).ToArray();
     foreach(var c in added)reference.RequestSkill((string)c["id"],(string)row["actor"],(string)row["ability"]);
     nativeInputs.Add(new JObject{["frame"]=frame,["method"]="ExecuteEvents pointer down/up/click",["button"]="Command0",["newCommands"]=added.Length});
    }
   }
   if(PolishFinal)CapturePolishInputs(pointer);
   long delta=(frame+1)*10000000L/Fps-frame*10000000L/Fps;
   driver.Advance(delta,1);if(CompareRig){reference.Advance(delta,1);if(reference.CaptureLocalState()!=driver.StateJson)throw new InvalidOperationException("Distant reference mismatch at "+frame);referenceFrames++;}view.Refresh();
   if(DistantBattle&&!RigDown&&frame==(TravelRecovery?1000:900))RestoreCycle();
   if(ChargeCycle&&castFrame>=0&&frame==castFrame+45)RestoreCycle();
   if(!AllAudio&&segment==10&&castFrame>=0&&frame>=castFrame+6)view.SkipCurrentPresentation();
   var cost=System.Diagnostics.Stopwatch.StartNew();Canvas.ForceUpdateCanvases();double canvasCPUms=cost.Elapsed.TotalMilliseconds;cost.Restart();cam.Render();double cameraCPUms=cost.Elapsed.TotalMilliseconds;cost.Restart();var previous=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();RenderTexture.active=previous;double readbackCPUms=cost.Elapsed.TotalMilliseconds;
   var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
   int count=AudioRenderer.GetSampleCountForCaptureFrame();
   using(var buffer=new NativeArray<float>(count*2,Allocator.Temp)){
    if(!AudioRenderer.Render(buffer))throw new InvalidOperationException("Audio render failed");
    foreach(float sample in buffer){audio.Write(sample);audioPeak=Math.Max(audioPeak,Math.Abs(sample));audioSamples++;}
   }
   var state=driver.HudState;
   float rim=ChargeCycle||PolishFinal?hud.transform.Find("ApprovedBattle/ultimate0/LeaderReadyFrame").GetComponent<CombatReadyFrame>().Progress:0;
   int charge=(int)state["ultimate"]["charge"];
   if(ChargeCycle){saw96|=charge==96;saw100|=charge==100;sawSpent|=submitted&&charge==0;sawLit|=charge==100&&rim>.99f;if(charge<100&&(rim!=0||(bool)state["ultimate"]["enabled"]))throw new InvalidOperationException("Uncharged leader still enabled/lit");}
   var command=((JArray)state["casts"]).FirstOrDefault(c=>(string)c["id"]==actualCommandId);
   trace.WriteLine(new JObject{["canvasCPUms"]=canvasCPUms,["cameraSubmitCPUms"]=cameraCPUms,["synchronousReadbackCPUms"]=readbackCPUms,["cardStates"]=new JArray(((JArray)state["skills"]).Select(r=>r.DeepClone()).Concat(((JArray)state["manuals"]).Select(r=>r.DeepClone())).Concat(new[]{state["ultimate"].DeepClone()})),["rigBuildMilliseconds"]=view.CaptureRigBuildMs,["rigMeanPoseMicroseconds"]=view.CaptureRigPoseUs,["operatorStates"]=new JArray(((JArray)state["actors"]).Where(a=>((string)a["id"]).StartsWith("op_")).Select(a=>a.DeepClone())),["rigStates"]=view.CaptureRigStates(),["rigShots"]=view.CaptureRigShotCount,["rigGuard"]=view.CaptureRigGuard,["rigExtension"]=view.CaptureRigExtension,["rigDiagnosticTriangles"]=view.CaptureRigTriangles,["cabinFigureScale"]=view.CabinFigureScale,["pursuit"]=view.CapturePursuitProjection(),["motionSeconds"]=state["motionSeconds"],["speedHalfUnits"]=state["speedHalfUnits"],["paused"]=state["paused"],["beltPhases"]=new JArray(hud.GetComponentsInChildren<CombatTravelGround>().Select(b=>new JObject{["foreground"]=b.Foreground,["phase"]=b.Phase})),["traceGeometry"]=DistantBattle?TraceGeometry():new JArray(),["referenceExact"]=CompareRig?new JValue(true):JValue.CreateNull(),["exteriorActors"]=new JArray(((JArray)state["actors"]).Where(a=>(bool)a["exterior"]).Select(a=>a.DeepClone())),["hits"]=state["hits"].DeepClone(),["frame"]=total,["segment"]=segment,["localFrame"]=frame,["tick"]=driver.Tick,["terrainUv"]=view.TravelOffset,["sunWorldX"]=CombatSunView.WorldCenter(driver.Tick*.05).x,["ability"]=ability,["status"]=command?["status"],["fullCutin"]=view.FullCutinVisible,["charge"]=state["ultimate"]["charge"],["castFirstFrame"]=castFrame,["leaderRim"]=rim,["leaderEnabled"]=state["ultimate"]["enabled"],["restored"]=restoreFrame>=0,["effectCueCount"]=effectCues.Count,["resolvedVisualCount"]=resolvedVisuals.Count,["typedResultCount"]=((JArray)state["effectResults"]).Count}.ToString(Newtonsoft.Json.Formatting.None));
   if(RigDown&&frame%12==0||SkillRig&&frame%6==0||PolishFinal&&nativeInputs.Any(n=>(int)n["frame"]>=frame-8)||ChargeCycle&&(charge==96||charge==100&&frame==readyFrame+30||charge==0||frame==restoreFrame)||TravelCap&&frame>=96&&frame<=112||TravelRecovery&&((frame>=136&&frame<=148)||(frame>=170&&frame<=184)||frame==479||frame==480||frame==509||frame==510||frame==599||frame==600||frame==1199||frame==1200||frame==999||frame==1000||frame==1001)||frame==0||DistantBattle&&frame%30==0||WorldLoop&&frame%120==0||HudPolish&&frame==8||castFrame>=0&&(frame==castFrame+3||frame==castFrame+10||frame==castFrame+27))File.WriteAllBytes(CombatPresentationCapture.Output+"/s"+segment+"-f"+frame+".png",pixels.EncodeToPNG());
   total++;if(++frame<Length)return;
   if(!WorldLoop&&(castFrame<0||command==null))throw new InvalidOperationException("No actual cast in segment "+segment);
   File.WriteAllText(CombatPresentationCapture.Output+"/state-"+segment+".json",driver.StateJson);if(CompareRig){File.WriteAllText(CombatPresentationCapture.Output+"/reference-final.json",reference.CaptureLocalState());File.WriteAllText(CombatPresentationCapture.Output+"/reference-"+segment+".json",reference.CaptureLocalState());}
   if(!AllAudio&&!SkillRig&&!WorldLoop&&!ChargeCycle){if(segment==8)ultimateBaseline=driver.StateJson;if(segment>=9&&driver.StateJson!=ultimateBaseline)throw new InvalidOperationException("Presentation mode changed gameplay");}
   results.Add(new JObject{["segment"]=segment,["scope"]=PolishFinal?"integrated multi-ability; see nativeCommands":"single ability or movement diagnostic",["ability"]=PolishFinal?null:ability,["castFrame"]=castFrame,["finalStatus"]=command?["status"],["cues"]=new JArray(cues),["effectCues"]=new JArray(effectCues),["resolvedVisuals"]=new JArray(resolvedVisuals),["effectResults"]=state["effectResults"].DeepClone(),["mode"]=AllAudio||SkillRig?"full":segment==9?"simplified":segment==10?"full+skip":"full"});
   segment++;if(Array.IndexOf(Environment.GetCommandLineArgs(),"-v03ShieldOnly")>=0)segment=groups.Length;if(AllAudio&&Array.IndexOf(Environment.GetCommandLineArgs(),"-v03Comparison")>=0)while(segment<groups.Length&&!new[]{0,3,6,8,10,12}.Contains(segment))segment++;if(segment<groups.Length){StartSegment();return;}
   if(ChargeCycle){if(!saw96||!saw100||!sawSpent||!sawLit||!restoreEqual||cues.Count!=1||(string)command?["status"]!="APPLIED")throw new InvalidOperationException("Charge/consume/restore evidence incomplete");File.WriteAllText(CombatPresentationCapture.Output+"/charge-cycle.json",new JObject{["naturalChargePerHit"]=4,["saw96"]=saw96,["saw100"]=saw100,["sawZeroAfterSubmit"]=sawSpent,["sawFullyLit"]=sawLit,["restoreStateIdentical"]=restoreEqual,["restoreFrame"]=restoreFrame,["castCueCount"]=cues.Count,["nativeButton"]=true,["processRestart"]=false}.ToString());}
   var completedNativeCommands=PolishFinal?JObject.Parse(driver.StateJson)["commands"].DeepClone():new JArray();File.WriteAllText(CombatPresentationCapture.Output+"/capture-inputs.json",new JObject{["nativeInputEvents"]=nativeInputs,["commands"]=completedNativeCommands}.ToString());
   driver=null;encoder.StandardInput.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new InvalidOperationException("Video encoder failed");
   trace.Dispose();trace=null;audio.Dispose();audio=null;AudioRenderer.Stop();audioRecording=false;
   if(WorldLoop&&!DistantBattle){CombatNeonComparison.SaveValues(cam,CombatPresentationCapture.Output);CombatNeonComparison.Save(cam,hud,CombatPresentationCapture.Output+"/s0-f0.png",CombatPresentationCapture.Output+"/neon-reference-vs-runtime.png");}
   File.WriteAllText(CombatPresentationCapture.Output+"/complete.json",new JObject{["polishFinal"]=PolishFinal,["nativeCommands"]=completedNativeCommands,["rigCastDown"]=RigCastDown,["rigDownDiagnosticInitialHpOne"]=RigDown,["travelCapNaturalWarmup84Seconds"]=TravelCap,["travelRecovery"]=TravelRecovery,["distantBattle"]=DistantBattle,["referenceFrames"]=referenceFrames,["nativeInputEvents"]=nativeInputs,["restoreStateIdentical"]=restoreEqual,["pauseAndSpeed"]="diagnostic driver ports, not native clicks",["playMode"]=true,["serverRun"]=false,["localSeed"]=1,["fps"]=Fps,["frames"]=total,["audioRate"]=AudioSettings.outputSampleRate,["audioChannels"]=2,["audioSamples"]=audioSamples,["audioPeak"]=audioPeak,["audioOrigin"]="Unity AudioRenderer, independent per-ability source clips; see audio delivery provenance",["modeGameplayIdentical"]=HudPolish||AllAudio||WorldLoop||ChargeCycle||SkillRig?JValue.CreateNull():new JValue(true),["chargeCycle"]=ChargeCycle,["nativeButtons"]=DistantBattle||HudPolish||ChargeCycle,["allAbilityAudio"]=AllAudio,["worldLoop"]=WorldLoop,["segments"]=results}.ToString());
   if(DistantBattle||Array.IndexOf(Environment.GetCommandLineArgs(),"-v03Comparison")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-v03ShieldOnly")>=0)Ce03PresentationClosureExport.Run();EditorApplication.ExitPlaymode();
  }catch(Exception e){File.WriteAllText(CombatPresentationCapture.Output+"/failed.txt",e.ToString());driver=null;EditorApplication.ExitPlaymode();}
 }
 readonly HashSet<string> polishSubmitted=new HashSet<string>();
 readonly Dictionary<string,string> polishPending=new Dictionary<string,string>();
 readonly Dictionary<string,int> polishLastAttempt=new Dictionary<string,int>();
 void CapturePolishInputs(PointerEventData pointer){
  var state=driver.HudState;if((int)state["ultimate"]["charge"]==100&&readyFrame<0)readyFrame=frame;
  var names=new[]{"skills0","skills1","skills2","skills3","skills4","Command0","Command1","Command2","ultimate0"};
  for(int i=0;i<names.Length;i++){
   string name=names[i];if(polishSubmitted.Contains(name)||frame<60+i*90||i==8&&(readyFrame<0||frame<readyFrame+30))continue;
   if(polishPending.TryGetValue(name,out var pending)){var prior=((JArray)JObject.Parse(driver.StateJson)["commands"]).Single(c=>(string)c["id"]==pending);string status=(string)prior["status"];if(status=="APPLIED"){polishSubmitted.Add(name);continue;}if(status=="QUEUED"||status=="CASTING")continue;polishPending.Remove(name);}
   if(polishLastAttempt.TryGetValue(name,out int lastAttempt)&&frame-lastAttempt<60)continue;
   var row=i<5?state["skills"][i]:i<8?state["manuals"][i-5]:state["ultimate"];if(!(bool)row["enabled"])continue;
   var node=hud.transform.Find("ApprovedBattle/"+name);var before=((JArray)JObject.Parse(driver.StateJson)["commands"]).Select(c=>(string)c["id"]).ToArray();
   ExecuteEvents.Execute(node.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(node.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(node.gameObject,pointer,ExecuteEvents.pointerClickHandler);
   var added=((JArray)JObject.Parse(driver.StateJson)["commands"]).Where(c=>!before.Contains((string)c["id"])).ToArray();
   foreach(var c in added)reference.RequestSkill((string)c["id"],(string)row["actor"],(string)row["ability"]);
   nativeInputs.Add(new JObject{["frame"]=frame,["method"]="ExecuteEvents pointer down/up/click on actually enabled control",["button"]=name,["ability"]=row["ability"],["newCommands"]=added.Length});
   if(added.Length>0){polishPending[name]=(string)added[0]["id"];polishLastAttempt[name]=frame;}break;
  }
 }
 JArray TraceGeometry(){
  var result=new JArray();var root=hud.transform.Find("ApprovedBattle") as RectTransform;
  Vector2 Point(Vector3 world){Vector2 v=root.InverseTransformPoint(world);return v-new Vector2(root.rect.xMin,root.rect.yMax);}
  foreach(var line in hud.GetComponentsInChildren<CombatSourceTrace>())if(line.Cannon){float p=(float)typeof(CombatSourceTrace).GetField("progress",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(line);var start=Vector2.Lerp(line.From,line.To,.12f+p*.32f);var end=Vector2.Lerp(line.From,line.To,.72f+p*.28f);result.Add(new JObject{["kind"]="movingCannonSegment",["shotMuzzle"]=new JArray(line.From.x,line.From.y),["target"]=new JArray(line.To.x,line.To.y),["progress"]=p,["drawStart"]=new JArray(start.x,start.y),["drawEnd"]=new JArray(end.x,end.y)});}
  foreach(var image in hud.GetComponentsInChildren<RawImage>())if(image.name=="EventMuzzle"){var point=Point(image.rectTransform.TransformPoint(image.rectTransform.rect.center));result.Add(new JObject{["kind"]="currentVisibleMuzzle",["mount"]=image.transform.parent.parent.name,["point"]=new JArray(point.x,point.y)});}
  return result;
 }
 void RestoreCycle(){
  var saved=driver.StateJson;File.WriteAllText(CombatPresentationCapture.Output+"/restore-checkpoint.json",saved);
  var restored=BasicCombatSimulation.RestoreLocalState(File.ReadAllText(CombatPresentationCapture.Output+"/restore-checkpoint.json"),Fresh);
  // Editor-only loader fixture packages the validated model; no trusted START/admission assertion.
  var checkpoint=(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{restored,1L},null);
  driver=new CombatSceneDriver(Fresh,()=>checkpoint,(model,revision)=>revision+1,()=>true);driver.Open();restoreEqual=driver.StateJson==saved;if(!restoreEqual)throw new InvalidOperationException("Restore state mismatch");
  var qaLabel=hud.transform.Find("QAContext");if(qaLabel!=null)qaLabel.SetParent(null,false);hud.SetActive(false);Destroy(hud);hud=new GameObject("RestoredPresentation",typeof(RectTransform),typeof(Canvas));view=hud.AddComponent<ApprovedCombatView>();view.SetCapturePresentationClock(()=>total/(double)Fps);view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));view.ConfigurePresentationAudio(CombatAbilityAudioSet.LoadResources(),.65f);view.PresentationCue+=(kind,id)=>cues.Add(kind+":"+id);view.EffectPresentationCue+=(kind,id)=>effectCues.Add(kind+":"+id);view.ResolvedVisualCue+=(shape,id,targetId)=>resolvedVisuals.Add(shape+":"+id+":"+targetId);
  if(qaLabel!=null)qaLabel.SetParent(hud.transform,false);var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;view.Refresh();if(view.FullCutinVisible)throw new InvalidOperationException("Restored cast replayed cutin");restoreFrame=frame;
 }
 void OnDestroy(){trace?.Dispose();audio?.Dispose();if(audioRecording)AudioRenderer.Stop();if(encoder!=null&&!encoder.HasExited){encoder.StandardInput.Close();encoder.WaitForExit(1000);}if(target!=null)target.Release();if(pixels!=null)Destroy(pixels);}
}
#endif
