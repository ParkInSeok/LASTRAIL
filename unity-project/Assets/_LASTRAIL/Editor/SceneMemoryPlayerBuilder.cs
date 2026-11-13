#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Diagnostics;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastRail.Editor {
 public static class SceneMemoryPlayerBuilder {
  public static void Build(){
   string output=Environment.GetEnvironmentVariable("LASTRAIL_MEMORY_OUTPUT");if(string.IsNullOrWhiteSpace(output)||!Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||File.Exists(output+"/build.json"))throw new ArgumentException("Fresh D QA output required.");
   var original=SceneManager.GetActiveScene();if(original.isDirty)throw new InvalidOperationException("Preserve unsaved scene.");
   string folder="Assets/_LASTRAIL/UI/CombatAnimations/MemoryQa/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);Directory.CreateDirectory(output);
   var mode=NewSceneMode.Additive;if(string.IsNullOrEmpty(original.path)){if(!Application.isBatchMode||original.rootCount!=0||SceneManager.sceneCount!=1)throw new InvalidOperationException("Preserve unsaved scene.");mode=NewSceneMode.Single;}
   var created=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,mode);try{SceneManager.SetActiveScene(created);
    new GameObject("MemoryLifecycle").AddComponent<SceneMemoryLifecycleProbe>().Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LASTRAIL/UI/CombatAnimations/RoofBattleLocalV01/20261017-043603-391fafb019b24c199c64430aed0d5adc/OriginalLocalSource.json"),AssetDatabase.LoadAssetAtPath<FourAgentV03Assets>(FourAgentV03Builder.CatalogPath),AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));
    string scene=folder+"/Memory.unity";if(!EditorSceneManager.SaveScene(created,scene))throw new IOException("Scene save failed.");AssetDatabase.SaveAssets();
    var textures=new JArray(AssetDatabase.GetDependencies(new[]{"Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity",scene},true).Distinct().Select(p=>new{path=p,texture=AssetDatabase.LoadAssetAtPath<Texture2D>(p)}).Where(x=>x.texture!=null).Select(x=>new JObject{["path"]=x.path,["guid"]=AssetDatabase.AssetPathToGUID(x.path),["name"]=x.texture.name,["width"]=x.texture.width,["height"]=x.texture.height,["readable"]=x.texture.isReadable,["format"]=x.texture.graphicsFormat.ToString(),["mips"]=x.texture.mipmapCount,["editorRuntimeSizeNotPlayerSize"]=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(x.texture)}));File.WriteAllText(output+"/texture-dependencies.json",textures.ToString());
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity",scene},locationPathName=output+"/SceneMemory.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
    File.WriteAllText(output+"/build.json",new JObject{["result"]=report.summary.result.ToString(),["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["scene"]=scene,["preloadedScenarioReferences"]="Second scene serializes Eve source, four-agent catalog and font; retained across all diagnostic cycles; not engine-only baseline."}.ToString());if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Memory Player build failed.");
   }finally{if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);if(created.IsValid()&&created.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(created,true);}}
  }
 }
}
#endif
