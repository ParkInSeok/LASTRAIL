#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CombatRailActualCapture {
 const string Key="RailActualCapture";
 static CombatRailActualCapture(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Run(){
  string output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
  if(!Application.isBatchMode||!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D batch capture output required.");
  Directory.CreateDirectory(output);CombatSecondFinalImportSettings.Apply();SessionState.SetString(Key,output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
 }
 static void Changed(PlayModeStateChange change){if(string.IsNullOrEmpty(SessionState.GetString(Key,"")))return;if(change==PlayModeStateChange.EnteredPlayMode)new GameObject("RailActualRecorder").AddComponent<CombatRailActualRecorder>();if(change==PlayModeStateChange.EnteredEditMode){SessionState.SetString(Key,"");EditorApplication.Exit(SessionState.GetInt(Key+".exit",1));}}
 public static string Output=>SessionState.GetString(Key,"");
 public static void Finish(bool success){SessionState.SetInt(Key+".exit",success?0:1);EditorApplication.ExitPlaymode();}
}
public sealed class CombatRailActualRecorder:MonoBehaviour {
 string output;CombatSceneDriver driver;ApprovedCombatView view;GameObject canvas;Camera camera;RenderTexture target;Texture2D pixels;int frame;double uiClock;StreamWriter auditTrace;string auditHash=new string('0',64);long auditSequence;
 async void Start(){output=CombatRailActualCapture.Output;try{await Capture();CombatRailActualCapture.Finish(true);}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);CombatRailActualCapture.Finish(false);}}
 void BuildView(){
  camera=new GameObject("RailCaptureCamera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;Resize(720,1280);
  canvas=new GameObject("RailActualCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));view=canvas.AddComponent<ApprovedCombatView>();view.SetCapturePresentationClock(()=>uiClock);view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
  var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;
  if(UnityEngine.Object.FindFirstObjectByType<EventSystem>()==null)new GameObject("RailCaptureEvents",typeof(EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
 }
 void Resize(int w,int h){if(target!=null){target.Release();Destroy(target);Destroy(pixels);}target=new RenderTexture(w,h,24);pixels=new Texture2D(w,h,TextureFormat.RGB24,false);camera.targetTexture=target;}
 void Render(){Canvas.ForceUpdateCanvases();view.Refresh();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();}
 void Still(string name){Render();File.WriteAllBytes(Path.Combine(output,name+".png"),pixels.EncodeToPNG());Ce03NormalUiCapture.Atomic(Path.Combine(output,name+".state.json"),driver.StateJson);}
 void Advance(){long delta=(long)Math.Floor((frame+1)*10000000d/30)-(long)Math.Floor(frame*10000000d/30);driver.Advance(delta,8);frame++;uiClock=frame/30d;}
 void AttachAudit(){driver.RailArtilleryStepCommitted+=row=>{row["auditSequence"]=++auditSequence;row["previousSha256"]=auditHash;auditHash=Ce03CommandContract.Sha(Ce03CommandContract.Canonical(row));row["recordSha256"]=auditHash;auditTrace.WriteLine(Ce03CommandContract.Canonical(row));};}
 void AdvanceBurst(){driver.Advance(1000000,8);frame+=3;uiClock=frame/30d;}
 void SupportStrategy(){var state=driver.HudState;foreach(var m in (JArray)state["manuals"]){if(!(bool)m["enabled"])continue;string id=(string)m["ability"];if(id=="emergency_repair"&&(int)state["trainHp"]>(int)state["trainMaxHp"]*.8)continue;driver.RequestSkill("qa-support-"+frame+"-"+id,(string)m["actor"],id);}}
 async Task Clip(string name,int frames,Action<int> input=null){
  string folder=Path.Combine(output,name);Directory.CreateDirectory(folder);
  var info=new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/dlstj/AppData/Roaming/Python/Python39/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe",Arguments="-v error -f rawvideo -pixel_format rgb24 -video_size 720x1280 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -crf 18 -pix_fmt yuv420p -movflags +faststart \""+Path.Combine(folder,"video.mp4")+"\"",WorkingDirectory="D:/Project/LASTRAIL/unity-project",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true};
  using(var encoder=System.Diagnostics.Process.Start(info))using(var trace=new StreamWriter(Path.Combine(folder,"frames.jsonl"))){
   for(int i=0;i<frames;i++){input?.Invoke(i);Advance();Render();byte[] raw=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(raw,0,raw.Length);trace.WriteLine(new JObject{["frame"]=i,["tick"]=driver.Tick,["phase"]=driver.HudState["railJourney"]["phase"],["distanceMm"]=driver.HudState["railJourney"]["distanceMm"],["modelSha256"]=Ce03CommandContract.Sha(driver.StateJson)}.ToString(Newtonsoft.Json.Formatting.None));if(i==0||i==frames-1)File.WriteAllBytes(Path.Combine(folder,i==0?"first.png":"last.png"),pixels.EncodeToPNG());if(i%30==0)await Task.Yield();}
   encoder.StandardInput.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new IOException("Video encoder failed.");
  }
 }
 static string HashFile(string path){using var hash=System.Security.Cryptography.SHA256.Create();return System.BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 async Task Capture(){
  var source=Ce03NormalUiCapture.Read("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/SecondFinal-20260913/actual-rail-session.json");string connection=(string)source["connectionPath"];
  driver=await Ce03RailLocalRuntime.Prepare(connection,Path.Combine(output,"session"));auditTrace=new StreamWriter(Path.Combine(output,"artillery-step-inputs.jsonl"),false,new System.Text.UTF8Encoding(false));AttachAudit();BuildView();Still("initial-http");File.WriteAllText(Path.Combine(output,"initial-factory-model.json"),driver.StateJson,new System.Text.UTF8Encoding(false));
  // Actual native UI listeners, with deterministic QA input timing recorded below.
  // Keep AUTO off until a real charged-ready state is captured; then enable through the native button.
  await Clip("opening",90,i=>{if(i==30)canvas.GetComponentsInChildren<Slider>().Single(s=>s.name=="TrainThrottle").value=1;});
  Still("high-throttle");bool artillery=false,selection=false,passed=false,destroyed=false,focused=false,readyCaptured=false,downCaptured=false;var milestones=new JArray();
  for(int i=0;i<18000&&driver.Outcome==BasicCombatOutcome.Running;i++){
   if(i%30==0)SupportStrategy();var hud=driver.HudState;
   if(!artillery&&((JArray)hud["actors"]).Any(a=>(bool?)a["artillery"]==true)){
    artillery=true;Still("artillery-live");await Clip("artillery-flight",90);Still("artillery-visible");
   }
   if(!readyCaptured&&((JArray)hud["ultimates"]).Any(u=>(int)u["charge"]==100&&(bool)u["enabled"])){Still("ultimate-ready");canvas.GetComponentsInChildren<Button>().Single(b=>b.name=="UltimateAuto").onClick.Invoke();readyCaptured=true;Still("auto-enabled");}
   if(!downCaptured&&((JArray)hud["actors"]).Any(a=>!(bool)a["enemy"]&&!(bool)a["train"]&&!(bool)a["device"]&&(int)a["hp"]==0)){downCaptured=true;Still("operator-down");}
   var artilleryRows=(JArray)hud["railArtillery"]["encounters"];
   if(!passed&&artilleryRows.Any(r=>(string)r["status"]=="Passed")){passed=true;Still("artillery-passed");}
   if(!destroyed&&artilleryRows.Any(r=>(string)r["status"]=="Destroyed")){destroyed=true;Still("artillery-destroyed");}
   if(passed&&!focused&&((JArray)hud["actors"]).Any(a=>(bool?)a["artillery"]==true)){
    view.Refresh();string id=(string)((JArray)hud["actors"]).First(a=>(bool?)a["artillery"]==true)["id"];
    canvas.GetComponentsInChildren<Button>().First(b=>b.name=="Unit"&&b.transform.parent.name==id).onClick.Invoke();canvas.GetComponentsInChildren<Slider>().Single(s=>s.name=="TrainThrottle").value=0;focused=true;Still("artillery-focused");
   }
   if((string)hud["railJourney"]["phase"]=="Selecting"){
    var offer=(JObject)hud["railJourney"]["offer"];int index=((JArray)hud["railJourney"]["confirmed"]).Count%2;
    if(!selection){
     selection=true;Still("selection-pending");long held=(long)hud["railJourney"]["distanceMm"];int wave=(int)hud["wave"];await Clip("selection-hold",60);
     if(held!=(long)driver.HudState["railJourney"]["distanceMm"]||wave!=(int)driver.HudState["wave"])throw new Exception("Junction moved while pending.");
     driver.SetPaused(true);string paused=driver.StateJson;driver.Advance(100000000,8);if(paused!=driver.StateJson)throw new Exception("Paused state changed.");Still("paused-selection");
     string before=driver.StateJson;driver=await Ce03RailLocalRuntime.Prepare(connection,Path.Combine(output,"session"));AttachAudit();if(before!=driver.StateJson)throw new Exception("Restored state differs.");DestroyImmediate(canvas);DestroyImmediate(camera.gameObject);BuildView();driver.SetPaused(false);Still("restored-selection");
     var lever=canvas.GetComponentsInChildren<CombatBranchLever>().Single();lever.value=index;Still("route-preview");lever.OnSubmit(new BaseEventData(EventSystem.current));Still("route-confirmed");await Clip("route-recovery",90);Still("route-traversed");Resize(720,1560);Still("long-aspect");Resize(720,1280);
    }else{var lever=canvas.GetComponentsInChildren<CombatBranchLever>().Single();view.Refresh();lever.value=index;lever.OnSubmit(new BaseEventData(EventSystem.current));}
    milestones.Add(new JObject{["offerId"]=offer["id"],["tick"]=driver.Tick});
   }
   AdvanceBurst();if(i%60==0)await Task.Yield();
  }
  auditTrace.Flush();auditTrace.Dispose();Still("final");File.Copy(Path.Combine(output,"session/checkpoint.json"),Path.Combine(output,"final-checkpoint.json"));string model=driver.StateJson;File.WriteAllText(Path.Combine(output,"executed-model.json"),model,new System.Text.UTF8Encoding(false));
  var identity=Ce03NormalUiCapture.Read(Path.Combine(output,"session/session.json"));var prep=Ce03NormalUiCapture.Read(Path.Combine(output,"session/preparation.json"));var state=JObject.Parse(model);var rail=(JObject)state["railJourney"];var bonuses=new JObject();foreach(string stat in new[]{"SQUAD_DAMAGE","TRAIN_DAMAGE","SHIELD_MAGNITUDE"})bonuses[stat]=((JArray)rail["confirmed"]).Where(c=>(string)c["stat"]==stat).Sum(c=>(int)c["valueBps"]);
  File.WriteAllText(Path.Combine(output,"control-report.json"),new JObject{["version"]="ce03-rail-control-report-v1",["owner"]=identity["owner"],["sessionId"]=identity["sessionId"],["configSha256"]=prep["railJourneySha256"],["modelSchema"]=state["schema"],["modelSha256"]=Ce03CommandContract.Sha(model),["finalTick"]=driver.Tick,["completedWaves"]=state["completedWaves"],["journal"]=rail["journal"],["bonusBps"]=bonuses}.ToString());
  File.WriteAllText(Path.Combine(output,"complete.json"),new JObject{["artilleryStepRows"]=auditSequence,["artilleryStepFinalChainSha256"]=auditHash,["artilleryStepFileSha256"]=HashFile(Path.Combine(output,"artillery-step-inputs.jsonl")),["scope"]="Actual UnityWebRequest rail prepare -> durable local session -> actual view; deterministic QA input timings, no START",["readyObserved"]=readyCaptured,["downObserved"]=downCaptured,["selectionObserved"]=selection,["artilleryPassedObserved"]=passed,["artilleryDestroyedObserved"]=destroyed,["artilleryObserved"]=artillery,["outcome"]=driver.Outcome.ToString(),["completedWaves"]=state["completedWaves"],["milestones"]=milestones}.ToString());  // Separate synthetic four-car view diagnostic; never included in the actual HTTP final report above.
  var four=new Ce03RailPreparation(Ce03NormalUiCapture.Read("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-RailJourney-20260914/synthetic-rail-preparation-four-cars.json"),"a",1);
  driver=new CombatSceneDriver(()=>four.CreateModel(1),()=>null,(s,r)=>r+1,()=>true);driver.Open();DestroyImmediate(canvas);DestroyImmediate(camera.gameObject);BuildView();Still("synthetic-four-car-view");
  File.WriteAllText(Path.Combine(output,"synthetic-four-car-scope.json"),new JObject{["scope"]="Actual Unity view using separately labeled Server synthetic four-car wrapper. Not an HTTP four-car execution or final combat report.",["preparationSha256"]=four.PreparationSha256,["carriages"]=driver.HudState["railJourney"]["carriages"]}.ToString());
 }
}
#endif
