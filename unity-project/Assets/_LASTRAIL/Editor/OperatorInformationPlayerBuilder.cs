#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastRail.Editor {
 public static class OperatorInformationPlayerBuilder {
  public static void Build(){
   string output=Environment.GetEnvironmentVariable("LASTRAIL_INFORMATION_OUTPUT");if(string.IsNullOrWhiteSpace(output)||!Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Explicit D QA directory required.");if(File.Exists(output+"/build.json"))throw new IOException("Fresh build required.");
   var art=AssetDatabase.LoadAssetAtPath<FourAgentV03Assets>(FourAgentV03Builder.CatalogPath);var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
   var eve=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LASTRAIL/UI/CombatAnimations/RoofBattleLocalV01/20261017-043603-391fafb019b24c199c64430aed0d5adc/OriginalLocalSource.json");
   string folder="Assets/_LASTRAIL/UI/CombatAnimations/OperatorInformationQa/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);Directory.CreateDirectory(output);
   string mira=folder+"/OriginalMira.json";File.Copy("D:/Project/LASTRAIL/QA/CommanderS1-20260922/en17-formal-local-trial-packet-20261015.json",mira);AssetDatabase.ImportAsset(mira,ImportAssetOptions.ForceSynchronousImport);
   Scene old=SceneManager.GetActiveScene(),created=default;
   try{var mode=NewSceneMode.Additive;if(string.IsNullOrEmpty(old.path)){if(!Application.isBatchMode||old.isDirty||old.rootCount!=0||SceneManager.sceneCount!=1)throw new InvalidOperationException("Preserve unsaved scene.");mode=NewSceneMode.Single;}
    created=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,mode);SceneManager.SetActiveScene(created);new GameObject("OperatorInformationPlayerQa").AddComponent<OperatorInformationPlayerProbe>().Initialize(eve,AssetDatabase.LoadAssetAtPath<TextAsset>(mira),art,font);string scene=folder+"/Information.unity";if(!EditorSceneManager.SaveScene(created,scene))throw new IOException("Scene save failed.");AssetDatabase.SaveAssets();
    string player=output+"/OperatorInformation.exe";var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},locationPathName=player,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});File.WriteAllText(output+"/build.json",new JObject{["status"]=report.summary.result.ToString(),["player"]=player,["scene"]=scene,["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["playerExecuted"]=false}.ToString());if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Player build failed.");
   }finally{if(old.IsValid()&&old.isLoaded)SceneManager.SetActiveScene(old);if(created.IsValid()&&created.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(created,true);}}
  }
 }
}
#endif
