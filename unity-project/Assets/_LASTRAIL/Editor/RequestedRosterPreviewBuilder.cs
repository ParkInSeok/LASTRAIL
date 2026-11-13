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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
public static class RequestedRosterPreviewBuilder {
 public static void Build(){
  string output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_ROSTER_PREVIEW_BUILD")??throw new InvalidOperationException("Output required."));
  if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required.");
  var prior=SceneManager.GetActiveScene();if(prior.isDirty)throw new InvalidOperationException("Preserve dirty scene.");
  Directory.CreateDirectory(output);OrdinaryApprovedArtImport.ApplyRequestedRosterRight();
  string folder="Assets/_LASTRAIL/QA/RequestedRoster/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);
  OrdinaryLivePlayerBuilder.BindLocalField(folder,output);
  var art=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>(folder+"/Presentation.asset");
  var frames=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>("Assets/_LASTRAIL/Content/Battle/Approved821/RequestedRosterRight01.asset");
  const string source="D:/Project/LASTRAIL/QA/SideViewReuse-20261023/battle-d-field-848-v01/approved-train-views-v01/source-mask-v01/";
  var manifest=JObject.Parse(File.ReadAllText(source+"manifest.json"));var parts=(JArray)manifest["parts"];
  var textures=new Texture2D[parts.Count];var rects=new Vector4[parts.Count];
  for(int i=0;i<parts.Count;i++){
   var p=parts[i];string name=(string)p["file"],path=folder+"/"+name;using var sha=SHA256.Create();
   if(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(source+name))).Replace("-","").ToLowerInvariant()!=(string)p["sha256"])throw new InvalidDataException("Source mask changed.");
   File.Copy(source+name,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
   textures[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var r=(JArray)p["sourceRect"];rects[i]=new Vector4((float)r[0],(float)r[1],(float)r[2],(float)r[3]);
  }
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(prior.path)?NewSceneMode.Single:NewSceneMode.Additive);
  const BuildTarget target=BuildTarget.StandaloneWindows64;bool defaults=PlayerSettings.GetUseDefaultGraphicsAPIs(target);var apis=PlayerSettings.GetGraphicsAPIs(target);
  try{
   var priorSource=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/player-live-runs/approved-fixed-1280x720-slow/source.json"));
   var ids=((JArray)priorSource["originalStart"]["ordinaryRailSource"]["packet"]["physicalConsist"]["instanceIds"]).Values<string>().ToArray();
   File.WriteAllText(output+"/borrowed-physical-ids.json",new JArray(ids).ToString());
   SceneManager.SetActiveScene(scene);new GameObject("RequestedRosterDisplayOnly").AddComponent<RequestedRosterPreview>().Initialize(frames,art,textures,rects,ids);
   string scenePath=folder+"/Preview.unity";EditorSceneManager.SaveScene(scene,scenePath);AssetDatabase.SaveAssets();
   // shortcut: use the existing local DX11 workaround until the D3D12 shutdown defect is resolved.
   PlayerSettings.SetUseDefaultGraphicsAPIs(target,false);PlayerSettings.SetGraphicsAPIs(target,new[]{GraphicsDeviceType.Direct3D11});
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName=output+"/RosterPreview.exe",target=target,options=BuildOptions.Development,extraScriptingDefines=new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
   File.WriteAllText(output+"/build.json",new JObject{["result"]=report.summary.result.ToString(),["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["scope"]="DISPLAY_ONLY_NO_EXECUTION_SOURCE",["scene"]=scenePath,["sourceMask"]=manifest}.ToString());
   if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Preview build failed.");
  }finally{
   PlayerSettings.SetGraphicsAPIs(target,apis);PlayerSettings.SetUseDefaultGraphicsAPIs(target,defaults);
   bool restored=PlayerSettings.GetUseDefaultGraphicsAPIs(target)==defaults&&PlayerSettings.GetGraphicsAPIs(target).SequenceEqual(apis);
   File.WriteAllText(output+"/graphics-restoration.json",new JObject{["restored"]=restored}.ToString());
   if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);
   if(!restored)throw new InvalidOperationException("Graphics settings restoration failed.");
  }
 }
}
#endif
