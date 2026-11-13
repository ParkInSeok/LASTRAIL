#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastRail.Editor {
 public static class DataLifecyclePlayerBuilder {
  public static void Build(){
   string output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_DATA_LIFECYCLE_BUILD")??throw new ArgumentException("Output required"));
   if(!output.StartsWith(@"D:\Project\LASTRAIL\QA\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new ArgumentException("Fresh D QA output required");
   var original=SceneManager.GetActiveScene();if(original.isDirty)throw new InvalidOperationException("Preserve unsaved scene");
   var test=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LastRail.Tests.EditMode.AccountScreenIntegrationTests")).First(t=>t!=null);
   var body=test.GetNestedType("DisplayHttpFixture",BindingFlags.NonPublic).GetMethod("Body",BindingFlags.Static|BindingFlags.NonPublic);
   var packet=new JObject();foreach(string path in new[]{"/v1/account/state","/v1/wallet/crystals","/v1/growth/config","/v1/trains/loadout","/v1/formation/ultimate"})packet[path]=(string)body.Invoke(null,new object[]{path});
   var story=test.Assembly.GetType("LastRail.Tests.EditMode.StoryArchiveReadModelTests").GetMethod("Reply",BindingFlags.Public|BindingFlags.Static);packet["/v1/story-progress"]=(string)story.Invoke(null,new object[]{new[]{"opn_01_last_departure"}});
   string folder="Assets/_LASTRAIL/QA/DataLifecycle/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);Directory.CreateDirectory(output);File.WriteAllText(folder+"/Responses.json",packet.ToString());AssetDatabase.ImportAsset(folder+"/Responses.json");
   var mode=string.IsNullOrEmpty(original.path)?NewSceneMode.Single:NewSceneMode.Additive;
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,mode);
   try{
    new GameObject("LocalDataLifecycleProbe").AddComponent<DataLifecycleLocalPlayerProbe>().Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/Responses.json"),AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),Environment.GetEnvironmentVariable("LASTRAIL_DATA_LIFECYCLE_VARIANT")??"CURRENT");
    var camera=new GameObject("LocalCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.04f,.05f);
    string path=folder+"/DataLifecycle.unity";EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName=output+"/DataLifecycle.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
    File.WriteAllText(output+"/build.json",new JObject{["result"]=report.summary.result.ToString(),["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["scene"]=path,["source"]=folder+"/Responses.json",["classification"]="EXPLICIT_LOCAL_RESPONSE_FIXTURE_NOT_GAME_SERVER"}.ToString());
    if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Data lifecycle Player build failed");
   }finally{if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);if(scene.IsValid()&&scene.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(scene,true);}}
  }
 }
}
#endif
