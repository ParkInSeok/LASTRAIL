#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrdinaryFiveSeraPlayerBuilder {
 public static void Build(){
  string output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_SERA_BUILD")??throw new ArgumentException("Explicit output required"));
  string input=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_SERA_INPUT")??throw new ArgumentException("Explicit source required"));
  if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||!input.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new ArgumentException("Fresh D QA output required");
  var prior=SceneManager.GetActiveScene();if(prior.isDirty)throw new InvalidOperationException("Preserve unsaved scene");
  string folder="Assets/_LASTRAIL/QA/OrdinaryFiveSera/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);Directory.CreateDirectory(output);File.Copy(input,folder+"/input.json");AssetDatabase.Refresh();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(prior.path)?NewSceneMode.Single:NewSceneMode.Additive);
  try{
   new GameObject("OrdinaryFiveSeraComponentQA").AddComponent<OrdinaryFiveSeraPlayerProbe>().Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/input.json"));
   string path=folder+"/Player.unity";EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName=output+"/OrdinarySera.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
   File.WriteAllText(output+"/build.json",new JObject{["result"]=report.summary.result.ToString(),["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["scope"]="NATIVE_SERA_COMPONENT_ONLY_SYNTHETIC_START_NO_ADMISSION",["source"]=input,["scene"]=path}.ToString());if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Player build failed");
  }finally{if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);if(scene.IsValid()&&scene.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(scene,true);}}
 }
}
#endif
