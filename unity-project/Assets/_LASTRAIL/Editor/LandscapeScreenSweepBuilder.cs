#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class LandscapeScreenSweepBuilder {
 public static void Build(){
  string output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_LANDSCAPE_BUILD")??throw new ArgumentException("Output required"));if(!output.StartsWith(@"D:\Project\LASTRAIL\QA\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");
  var prior=SceneManager.GetActiveScene();if(prior.isDirty)throw new InvalidOperationException("Unsaved scene");Directory.CreateDirectory(output);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(prior.path)?NewSceneMode.Single:NewSceneMode.Additive);
  try{
   var names=new[]{"StartupScreen","MissionShellScreen","GachaScreen","CodexArchiveScreen","RewardReviewScreen","OfflineRewardScreen","StoryProgressSummaryScreen"};
   var prefabs=names.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/"+n+".prefab")??throw new InvalidOperationException("Missing "+n)).ToArray();
   new GameObject("LandscapeSweep").AddComponent<LandscapeScreenSweepProbe>().Initialize(prefabs,AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));
   var camera=new GameObject("SweepCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
   string folder="Assets/_LASTRAIL/QA/LandscapeSweep/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);string path=folder+"/Sweep.unity";EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();
   var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName=output+"/LandscapeSweep.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
   File.WriteAllText(output+"/build.json",new JObject{["result"]=result.summary.result.ToString(),["errors"]=result.summary.totalErrors,["warnings"]=result.summary.totalWarnings,["scene"]=path,["scope"]="SCREEN_COMPONENT_SWEEP"}.ToString());if(result.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Build failed");
  }finally{if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);if(scene.IsValid()&&scene.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(scene,true);}}
 }
}
#endif
