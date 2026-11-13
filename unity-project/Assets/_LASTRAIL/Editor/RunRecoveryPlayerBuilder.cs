#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastRail.Editor {
 public static class RunRecoveryPlayerBuilder {
  public static void Build(){
   string output=Environment.GetEnvironmentVariable("LASTRAIL_RECOVERY_OUTPUT");
   if(string.IsNullOrWhiteSpace(output)||!Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("D QA required");
   if(File.Exists(output+"/build.json"))throw new IOException("Fresh output required");Directory.CreateDirectory(output);
   string folder="Assets/_LASTRAIL/UI/RunRecoveryQa/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);
   Scene old=SceneManager.GetActiveScene(),created=default;
   try{var mode=NewSceneMode.Additive;if(string.IsNullOrEmpty(old.path)){if(!Application.isBatchMode||old.isDirty||old.rootCount!=0||SceneManager.sceneCount!=1)throw new InvalidOperationException("Unsaved scene");mode=NewSceneMode.Single;}
    created=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,mode);SceneManager.SetActiveScene(created);new GameObject("RunRecoveryQA").AddComponent<RunRecoveryPlayerProbe>().Initialize(AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));string scene=folder+"/Recovery.unity";if(!EditorSceneManager.SaveScene(created,scene))throw new IOException();AssetDatabase.SaveAssets();
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},target=BuildTarget.StandaloneWindows64,locationPathName=output+"/Recovery.exe",options=BuildOptions.Development});
    File.WriteAllText(output+"/build.json",new JObject{["status"]=report.summary.result.ToString(),["scene"]=scene,["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["scope"]="LOCAL_PROTOCOL_PROBE_ONLY"}.ToString());if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Build failed");
   }finally{if(old.IsValid()&&old.isLoaded)SceneManager.SetActiveScene(old);if(created.IsValid()&&created.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(created,true);}}
  }
 }
}
#endif
