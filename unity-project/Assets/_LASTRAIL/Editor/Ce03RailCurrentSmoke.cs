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
using UnityEngine.UI;
[InitializeOnLoad]
public static class Ce03RailCurrentSmoke {
 const string Key="RailCurrentSmoke";
 static Ce03RailCurrentSmoke(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Run(){string output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!Application.isBatchMode||!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D output required");Directory.CreateDirectory(output);SessionState.SetString(Key,output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange state){if(string.IsNullOrEmpty(SessionState.GetString(Key,"")))return;if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("CurrentRailRecorder").AddComponent<Ce03RailCurrentRecorder>();if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetString(Key,"");EditorApplication.Exit(SessionState.GetInt(Key+".exit",1));}}
 public static string Output=>SessionState.GetString(Key,"");
 public static void Finish(bool success){SessionState.SetInt(Key+".exit",success?0:1);EditorApplication.ExitPlaymode();}
}
public sealed class Ce03RailCurrentRecorder:MonoBehaviour {
 string output;GameObject canvas;ApprovedCombatView view;Camera camera;RenderTexture target;Texture2D pixels;CombatSceneDriver driver;
 async void Start(){output=Ce03RailCurrentSmoke.Output;try{await Capture();Ce03RailCurrentSmoke.Finish(true);}catch(Exception e){File.WriteAllText(output+"/failure.txt",e.ToString());Debug.LogException(e);Ce03RailCurrentSmoke.Finish(false);}}
 void View(){canvas=new GameObject("CurrentRailCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));view=canvas.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}
 void Still(string name){Canvas.ForceUpdateCanvases();view.Refresh();Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();File.WriteAllBytes(output+"/"+name+".png",pixels.EncodeToPNG());RenderTexture.active=previous;}
 async Task Capture(){
  string connection=(string)Ce03NormalUiCapture.Read("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/SecondFinal-20260913/current-profile-session.json")["connectionPath"];
  driver=await Ce03RailLocalRuntime.Prepare(connection,output+"/session");var prep=new Ce03RailPreparation(Ce03NormalUiCapture.Read(output+"/session/preparation.json"),"a",1);if(prep.Presentation==null)throw new Exception("Current profile required");
  camera=new GameObject("CurrentRailCamera",typeof(Camera)).GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;target=new RenderTexture(720,1280,24);camera.targetTexture=target;pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);View();Still("initial-current-profile");
  string initial=driver.StateJson;File.WriteAllText(output+"/initial-model.json",initial,new System.Text.UTF8Encoding(false));
  canvas.GetComponentsInChildren<Slider>().Single(s=>s.name=="TrainThrottle").value=1;canvas.GetComponentsInChildren<Button>().Single(b=>b.name=="Speed").onClick.Invoke();canvas.GetComponentsInChildren<Button>().Single(b=>b.name=="Pause").onClick.Invoke();
  string paused=driver.StateJson;driver.Advance(100000000,8);if(paused!=driver.StateJson)throw new Exception("Pause mutated state");Still("paused-current-profile");
  driver=await Ce03RailLocalRuntime.Prepare(connection,output+"/session");if(paused!=driver.StateJson||driver.RecoveryRequired)throw new Exception("Restore mismatch");DestroyImmediate(canvas);View();Still("restored-current-profile");canvas.GetComponentsInChildren<Button>().Single(b=>b.name=="Pause").onClick.Invoke();
  if((int)driver.HudState["railJourney"]["throttleBps"]!=13000||(int)driver.HudState["speedHalfUnits"]!=3)throw new Exception("Controls not retained");
  var reference=BasicCombatSimulation.RestoreLocalState(driver.StateJson,()=>prep.CreateModel(1));using(var trace=new StreamWriter(output+"/frames.jsonl")){for(int i=0;i<30;i++){driver.Advance(1000000,8);reference.Advance(1000000,8);if(driver.StateJson!=reference.CaptureLocalState()||driver.RecoveryRequired)throw new Exception("Reference mismatch");view.Refresh();trace.WriteLine(new JObject{["frame"]=i,["tick"]=driver.Tick,["referenceExact"]=true,["modelSha256"]=Ce03CommandContract.Sha(driver.StateJson)}.ToString(Newtonsoft.Json.Formatting.None));if(i%10==0)await Task.Yield();}}
  Still("running-current-profile");File.WriteAllText(output+"/final-model.json",driver.StateJson,new System.Text.UTF8Encoding(false));var cp=Ce03NormalUiCapture.Read(output+"/session/checkpoint.json");if((string)cp["state"]!=driver.StateJson)throw new Exception("Final durable mismatch");
  File.WriteAllText(output+"/complete.json",new JObject{["scope"]="Actual UnityWebRequest current rail profile prepare + physical current binding + native throttle/speed/pause controls + durable restore + bounded reference steps. No START or full15wave rerun.",["presentationSha256"]=prep.Presentation.Hash,["preparationSha256"]=prep.PreparationSha256,["initialConfigurationSha256"]=JObject.Parse(initial)["configurationSha256"],["referenceExactFrames"]=30,["finalTick"]=driver.Tick,["checkpointRevision"]=cp["revision"],["pauseUnchanged"]=true,["restoreExact"]=true,["finalCheckpointExact"]=true,["recoveryRequired"]=false,["ready"]=false}.ToString());
 }
}
#endif
