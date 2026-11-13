#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
public static class OrdinaryTravelPanelPlayerBuilder {
 public static void Build(){
  var output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_TRAVEL_UI_BUILD")??throw new ArgumentException("Output required"));
  if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new ArgumentException("Fresh D QA folder required");
  var prior=SceneManager.GetActiveScene();if(prior.isDirty)throw new InvalidOperationException("Preserve unsaved scene");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(prior.path)?NewSceneMode.Single:NewSceneMode.Additive);
  try{
   SceneManager.SetActiveScene(scene);new GameObject("Input",typeof(EventSystem),typeof(InputSystemUIInputModule));
   new GameObject("CaptureCamera",typeof(Camera));
   new GameObject("TravelUiProbe").AddComponent<OrdinaryTravelPanelPlayerProbe>().Initialize(AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));
   string folder="Assets/_LASTRAIL/QA/TravelUi829/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);Directory.CreateDirectory(output);
   string path=folder+"/Player.unity";EditorSceneManager.SaveScene(scene,path);
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName=output+"/TravelUi.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
   File.WriteAllText(output+"/build.json",new JObject{["result"]=report.summary.result.ToString(),["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["scope"]="SYNTHETIC_PLAYER_PROBES_NOT_SERVER_COMBAT"}.ToString());
   if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Player build failed");
  }finally{if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);if(scene.IsValid()&&scene.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(scene,true);}}
 }
}
#endif
