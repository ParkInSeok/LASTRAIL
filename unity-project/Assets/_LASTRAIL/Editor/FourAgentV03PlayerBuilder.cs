#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastRail.Editor {
 public static class FourAgentV03PlayerBuilder {
  public static void Build(){
   string output=Environment.GetEnvironmentVariable("LASTRAIL_V03_OUTPUT");if(string.IsNullOrWhiteSpace(output)||!Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Explicit D QA directory required.");
   if(File.Exists(output+"/build.json"))throw new IOException("Fresh build output required.");
   var assets=AssetDatabase.LoadAssetAtPath<FourAgentV03Assets>(FourAgentV03Builder.CatalogPath)??throw new InvalidOperationException("Import v03 first.");
   FiveAgentVisualAssets legacy=null; // V03 owns its texture references; no legacy catalog is needed in this QA Player.
   const string sourcePath="Assets/_LASTRAIL/UI/CombatAnimations/RoofBattleLocalV01/20261017-043603-391fafb019b24c199c64430aed0d5adc/OriginalLocalSource.json";
   var source=AssetDatabase.LoadAssetAtPath<TextAsset>(sourcePath);string hash=BDirectAttackInput.Digest(JObject.Parse(source.text));LastRail.Presentation.RoofBattle.RoofBattleLocalSource.Read(JObject.Parse(source.text),hash);
   var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
   string folder=FourAgentV03Builder.Destination+"/PlayerQa/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);Directory.CreateDirectory(output);Scene old=SceneManager.GetActiveScene(),created=default;
   try{
    var mode=NewSceneMode.Additive;if(string.IsNullOrEmpty(old.path)){if(!Application.isBatchMode||old.isDirty||old.rootCount!=0||SceneManager.sceneCount!=1)throw new InvalidOperationException("Preserve unsaved scene.");mode=NewSceneMode.Single;}
    created=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,mode);SceneManager.SetActiveScene(created);new GameObject("FourAgentV03ExplicitPlayerQa").AddComponent<FourAgentV03PlayerProbe>().Initialize(assets,legacy,source,hash,font);string scene=folder+"/FourAgentV03Qa.unity";if(!EditorSceneManager.SaveScene(created,scene))throw new IOException("Scene save failed.");AssetDatabase.SaveAssets();
    string player=output+"/FourAgentV03.exe";var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},locationPathName=player,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});File.WriteAllText(output+"/build.json",new JObject{["status"]=report.summary.result.ToString(),["player"]=player,["scene"]=scene,["source"]=sourcePath,["sourceHash"]=hash,["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["playerExecuted"]=false}.ToString());if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Player build failed.");
   }finally{if(old.IsValid()&&old.isLoaded)SceneManager.SetActiveScene(old);if(created.IsValid()&&created.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(created,true);}}
  }
 }
}
#endif
