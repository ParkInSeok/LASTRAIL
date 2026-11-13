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

public static class KyleGuardLocalPlayerBuilder {
 public static void Build(){
  KyleGuardLocalPlayerProbe.SelfCheck();
  string output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_KYLE_LOCAL_BUILD")??throw new ArgumentException("Explicit build path required."));
  if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new ArgumentException("Fresh D QA build directory required.");
  var prior=SceneManager.GetActiveScene();if(prior.isDirty)throw new InvalidOperationException("Preserve unsaved scene.");
  Directory.CreateDirectory(output);string folder="Assets/_LASTRAIL/QA/KyleGuardLocalPlayer/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);
  var binding=OrdinaryLivePlayerBuilder.BindLocalField(folder,output);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(prior.path)?NewSceneMode.Single:NewSceneMode.Additive);
  try{
   SceneManager.SetActiveScene(scene);new GameObject("LocalInput",typeof(EventSystem),typeof(InputSystemUIInputModule));new GameObject("KyleGuardLocalProbe").AddComponent<KyleGuardLocalPlayerProbe>().Initialize(binding);
   string path=folder+"/Player.unity";EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName=Path.Combine(output,"KyleGuardLocal.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
   File.WriteAllText(Path.Combine(output,"build.json"),new JObject{["result"]=report.summary.result.ToString(),["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["scene"]=path,["source"]="EXPLICIT_LOCAL_AUTHORED_INPUT_AT_RUNTIME",["productionSettingsChanged"]=false}.ToString());
   if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Kyle local Player build failed.");
  }finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);}
 }
}
#endif
