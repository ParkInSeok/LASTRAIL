#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using UnityEngine.Rendering;

public static class OrdinaryLivePlayerBuilder {
 internal static CombatPlayerAssetBinding BindLocalField(string folder,string output,CombatOperatorFrames operatorFrames=null){
  const string source="D:/Project/LASTRAIL/QA/SideViewReuse-20261023/battle-d-field-848-v01";
  var manifest=JObject.Parse(File.ReadAllText(source+"/manifest.json"));
  if((string)manifest["version"]!="battle-d-field-848-v01")throw new InvalidDataException("Approved field version differs.");
  foreach(string name in new[]{"refinery.png","viaduct.png"}){
   string expected=name=="refinery.png"?"d6a143c2a7a813dbe04cbc876f6657d6c2b3a858c186628add119ca7a14cf662":"522cd4738ad2cc0304a7ebafbc96d0087e31c7d86aa65f043653740c5d77c07b";
   using var sha=SHA256.Create();
   if(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(source+"/"+name))).Replace("-","").ToLowerInvariant()!=expected)throw new InvalidDataException("Approved field bytes differ: "+name);
   File.Copy(source+"/"+name,folder+"/"+name);
  }
  AssetDatabase.Refresh();
  foreach(string name in new[]{"refinery.png","viaduct.png"}){
   var importer=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+name);
   importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.maxTextureSize=2048;
   importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.SaveAndReimport();
  }
  // This local content selection does not remap existing nodes or mutate their START snapshots.
  var config=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>("Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryPresentation.asset"));
  config.name="OrdinaryRefineryPresentation";config.MapId="refinery-viaduct-v01";
  if(operatorFrames!=null)config.OperatorFrames=operatorFrames;
  config.OrdinaryFieldBackground=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/refinery.png");
  config.OrdinaryFieldTrack=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/viaduct.png");
  AssetDatabase.CreateAsset(config,folder+"/Presentation.asset");AssetDatabase.SaveAssets();
  var binding=CombatPlayerAssetBindingBuilder.BakeConfiguration(config);
  AssetDatabase.CreateAsset(binding,folder+"/Binding.asset");AssetDatabase.SaveAssets();
  File.WriteAllText(output+"/field-binding.json",binding.ManifestJson);
  File.WriteAllText(output+"/field-selection.json",new JObject{["mapId"]=config.MapId,["scope"]="EXPLICIT_LOCAL_CONTENT_SELECTION_NOT_OPERATIONAL_NODE_MAPPING",["sourceManifest"]=manifest,["bindingHash"]=binding.ManifestHash}.ToString());
  return binding;
 }
 public static void BuildKyleRight()=>Build(true);
 public static void Build()=>Build(false);
 static void Build(bool kyleRight){
  string output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_LIVE_BUILD")??throw new ArgumentException("Explicit build path required."));
  if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new ArgumentException("Fresh D QA build path required.");
  var prior=SceneManager.GetActiveScene();if(prior.isDirty)throw new InvalidOperationException("Preserve unsaved scene.");
  OrdinaryFivePlayerAssemblyBuilder.BakeLiveEnemyResources(output);OrdinaryFivePlayerAssemblyBuilder.BakeResources();string folder="Assets/_LASTRAIL/QA/OrdinaryLivePlayer/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);
  var frames=kyleRight?AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>("Assets/_LASTRAIL/Content/Battle/Approved821/KyleRight01.asset"):null;
  if(kyleRight&&frames==null)throw new InvalidOperationException("Approved Kyle intake required.");
  var binding=BindLocalField(folder,output,frames);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(prior.path)?NewSceneMode.Single:NewSceneMode.Additive);
  try{
   SceneManager.SetActiveScene(scene);new GameObject("OrdinaryLiveInput",typeof(EventSystem),typeof(InputSystemUIInputModule));new GameObject("OrdinaryLivePlayerProbe").AddComponent<OrdinaryLivePlayerProbe>().Initialize(binding);
   string path=folder+"/Player.unity";EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();
   const BuildTarget target=BuildTarget.StandaloneWindows64;
   bool originalDefault=PlayerSettings.GetUseDefaultGraphicsAPIs(target);var originalApis=PlayerSettings.GetGraphicsAPIs(target);
   try{
    // shortcut: local Windows QA avoids the captured D3D12 cache-save crash; remove after that path is verified fixed.
    PlayerSettings.SetUseDefaultGraphicsAPIs(target,false);PlayerSettings.SetGraphicsAPIs(target,new[]{GraphicsDeviceType.Direct3D11});
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName=output+"/OrdinaryLive.exe",target=target,options=BuildOptions.Development,extraScriptingDefines=new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
    File.WriteAllText(Path.Combine(output,"build.json"),new JObject{["result"]=report.summary.result.ToString(),["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["scene"]=path,["graphicsApi"]="Direct3D11",["source"]="AUTHENTICATED_LOOPBACK_SOURCE_AT_RUNTIME"}.ToString());
    if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Live Player build failed.");
   }finally{
    PlayerSettings.SetGraphicsAPIs(target,originalApis);PlayerSettings.SetUseDefaultGraphicsAPIs(target,originalDefault);
    bool restored=PlayerSettings.GetUseDefaultGraphicsAPIs(target)==originalDefault&&PlayerSettings.GetGraphicsAPIs(target).SequenceEqual(originalApis);
    File.WriteAllText(output+"/graphics-restoration.json",new JObject{["restored"]=restored,["originalDefault"]=originalDefault,["originalApis"]=new JArray(originalApis.Select(a=>a.ToString()))}.ToString());
    if(!restored)throw new InvalidOperationException("Original project graphics settings were not restored.");
   }
  }finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);}
 }
}
#endif
