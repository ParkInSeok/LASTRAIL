#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Battle;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class OrdinaryFivePlayerAssemblyBuilder {
 const string BindingPath="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryBinding.asset";
 const string SourcePath="D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json";
 static CombatPlayerAssetBinding EnsureBinding(){
  if(Path.GetFullPath(Application.dataPath).Replace('\\','/')!="D:/Project/LASTRAIL/unity-project/Assets")throw new InvalidOperationException("Canonical D Unity project required.");
  EnsureRoof();
  var presentation=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>("Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryPresentation.asset");
  if(presentation==null)throw new InvalidOperationException("Approved ordinary presentation must be imported before building.");
  var baked=CombatPlayerAssetBindingBuilder.BakeConfiguration(presentation);
  var existing=AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>(BindingPath);
  if(existing!=null){
   try{if(existing.ManifestHash!=baked.ManifestHash)throw new InvalidOperationException("Existing ordinary binding differs; preserve it and review source changes.");using var lease=existing.Acquire(existing.ManifestHash,new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"});return existing;}
   finally{UnityEngine.Object.DestroyImmediate(baked);}
  }
  if(File.Exists(BindingPath)){UnityEngine.Object.DestroyImmediate(baked);throw new InvalidOperationException("Existing binding file cannot be replaced.");}
  Directory.CreateDirectory(Path.GetDirectoryName(BindingPath));AssetDatabase.Refresh();AssetDatabase.CreateAsset(baked,BindingPath);AssetDatabase.SaveAssets();return baked;
 }
 static void EnsureRoof(){
  const string path="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryRoof.asset";
  const string original="Assets/_LASTRAIL/Content/Battle/OrdinaryRoofSpawnV01/";
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(original+"OrdinaryRoofSpawnV01.prefab");
  var definition=AssetDatabase.LoadAssetAtPath<TextAsset>(original+"definition.json");
  if(prefab==null||definition==null)throw new InvalidOperationException("Original roof authoring assets missing.");
  var enemy=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/OrdinaryEnemyTrainV01.prefab");
  var live=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LASTRAIL/Content/Battle/OrdinaryLiveGeometryV02/definition.json");
  if(enemy==null||live==null)throw new InvalidOperationException("Original enemy and live judgement definition required.");
  var existing=AssetDatabase.LoadAssetAtPath<OrdinaryRoofPresentationAssets>(path);
  if(existing!=null){if(existing.Prefab!=prefab||existing.Definition!=definition)throw new InvalidOperationException("Existing roof binding differs; preserve and review it.");
   if(existing.EnemyPrefab!=null&&existing.EnemyPrefab!=enemy||existing.LiveDefinition!=null&&existing.LiveDefinition!=live)throw new InvalidOperationException("Existing enemy binding differs.");
   existing.EnemyPrefab=enemy;existing.LiveDefinition=live;EditorUtility.SetDirty(existing);AssetDatabase.SaveAssets();return;}
  if(File.Exists(path))throw new InvalidOperationException("Existing roof binding cannot be overwritten.");
  var asset=ScriptableObject.CreateInstance<OrdinaryRoofPresentationAssets>();asset.Prefab=prefab;asset.Definition=definition;asset.EnemyPrefab=enemy;asset.LiveDefinition=live;
  Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();AssetDatabase.CreateAsset(asset,path);AssetDatabase.SaveAssets();
 }
 public static void BakeResources(){EnsureBinding();}
 public static void BakeLiveEnemyResources(string evidenceDirectory){
  Directory.CreateDirectory(evidenceDirectory);
  foreach(string path in new[]{BindingPath,"Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryRoof.asset"})
   if(File.Exists(path))File.Copy(path,Path.Combine(evidenceDirectory,"previous-"+Path.GetFileName(path)),false);
  EnsureRoof();
  var presentation=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>("Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryPresentation.asset");
  var baked=CombatPlayerAssetBindingBuilder.BakeConfiguration(presentation);
  try{
   File.WriteAllText(Path.Combine(evidenceDirectory,"live-enemy-binding.json"),baked.ManifestJson);
   var existing=AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>(BindingPath);
   if(existing==null)throw new InvalidOperationException("Preserved ordinary binding required.");
   EditorUtility.CopySerialized(baked,existing);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssets();
  }finally{UnityEngine.Object.DestroyImmediate(baked);}
 }
 public static void Build(){
  string output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_FIVE_BUILD")??throw new ArgumentException("Explicit build output required."));
  if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output)||File.Exists(output))throw new ArgumentException("Fresh D QA build folder required.");
  var prior=SceneManager.GetActiveScene();if(prior.isDirty)throw new InvalidOperationException("Preserve unsaved scene.");
  var binding=EnsureBinding();string folder="Assets/_LASTRAIL/QA/OrdinaryFiveAssembly/"+Guid.NewGuid().ToString("N");
  Directory.CreateDirectory(folder);Directory.CreateDirectory(output);File.Copy(SourcePath,folder+"/source.json");AssetDatabase.Refresh();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(prior.path)?NewSceneMode.Single:NewSceneMode.Additive);
  try{
   SceneManager.SetActiveScene(scene);
   new GameObject("OrdinaryQaInput",typeof(EventSystem),typeof(InputSystemUIInputModule));
   new GameObject("OrdinaryFivePlayerAssemblyProbe").AddComponent<OrdinaryFivePlayerAssemblyProbe>().Initialize(binding,AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/source.json"));
   string path=folder+"/Player.unity";EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName=output+"/OrdinaryFive.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=Environment.GetEnvironmentVariable("LASTRAIL_BATTLE_D_COMPARISON_BUILD")=="1"?new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER","LASTRAIL_BATTLE_D_COMPARISON_BUILD"}:new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
   File.WriteAllText(output+"/build.json",new JObject{["result"]=report.summary.result.ToString(),["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["source"]=SourcePath,["sourceSha256"]=Ce03CommandContract.Sha(File.ReadAllText(SourcePath)),["manifestHash"]=binding.ManifestHash,["scene"]=path,["scope"]="ORDINARY_FIVE_ACTUAL_PLAYER_HOST_LOCAL_CAPTURED_SOURCE_NOT_PRODUCT_ADMISSION"}.ToString());
   if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Ordinary Player build failed.");
   if(Environment.GetEnvironmentVariable("LASTRAIL_BATTLE_D_COMPARISON_BUILD")=="1"){
    const string source="D:/Project/LASTRAIL/QA/SideViewReuse-20261023/battle-d-field-848-v01";
    string target=Path.Combine(output,"OrdinaryFive_Data/StreamingAssets/BattleDField");
    Directory.CreateDirectory(target);
    File.Copy(Path.Combine(source,"enemy-placement-v01/enemy-engine.png"),Path.Combine(target,"enemy-engine.png"));
    foreach(string file in new[]{"refinery.png","viaduct.png","manifest.json"})File.Copy(Path.Combine(source,file),Path.Combine(target,file));
   }
  }finally{if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);if(scene.IsValid()&&scene.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(scene,true);}}
 }
}
#endif
