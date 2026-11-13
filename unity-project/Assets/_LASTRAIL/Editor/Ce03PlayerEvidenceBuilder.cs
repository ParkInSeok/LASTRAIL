#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LastRail.Battle;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class Ce03PlayerEvidenceBuilder {
 static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
 static string C(JToken token)=>Ce03CommandContract.Canonical(token);
 public static void Build(){
  string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
  if(root.Replace('\\','/')!="D:/Project/LASTRAIL/unity-project")throw new InvalidOperationException("Canonical Unity project required");
  string output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_PLAYER_EVIDENCE_BUILD")??throw new ArgumentException("Explicit build output required"));
  if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D build required");
  var prior=SceneManager.GetActiveScene();if(prior.isDirty)throw new InvalidOperationException("Preserve dirty scene");
  var binding=AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>("Assets/_LASTRAIL/QA/CombatAssetBinding/89e63936213b4ce090e6a03fbb98bbe7/Binding.asset");if(binding==null)throw new InvalidOperationException("Pinned binding missing");
  string id=Guid.NewGuid().ToString("N"),folder="Assets/_LASTRAIL/QA/CombatAssetBinding/"+id,manifestPath="QA/Ce03PlayerBuild/"+id+"/player-build.json";
  Directory.CreateDirectory(folder);Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));Directory.CreateDirectory(output);
  var names=Directory.GetFiles("Assets/_LASTRAIL/Scripts","*.cs",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).Concat(new[]{"Assets/_LASTRAIL/Editor/Ce03PlayerEvidenceBuilder.cs","Assets/_LASTRAIL/Editor/Ce03ConsumerEvidenceRunner.cs","Assets/_LASTRAIL/Editor/Ce03ConsumerEffectChecks.cs","Assets/_LASTRAIL/Editor/Ce03AcceptedRunConsumer.cs","Assets/_LASTRAIL/Scripts/LastRail.Runtime.asmdef","QA/Ce03CommandIntegration/synthetic-r6-transport.json","Packages/manifest.json","Packages/packages-lock.json","ProjectSettings/ProjectVersion.txt"}).Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToArray();
  const string embeddedAnimation="Packages/com.unity.2d.animation";
  if(Directory.Exists(embeddedAnimation))names=names.Concat(Directory.GetFiles(embeddedAnimation,"*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".cs",StringComparison.Ordinal)||p.EndsWith(".asmdef",StringComparison.Ordinal)||p.Replace('\\','/')==embeddedAnimation+"/package.json").Select(p=>p.Replace('\\','/'))).Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToArray();
  var fingerprints=new JArray(names.Select(p=>new JObject{["path"]=p,["sha256"]=Ce03CommandContract.Sha(File.ReadAllText(p,Utf8))}));
  File.WriteAllText(folder+"/compiled-sources.json",C(new JObject{["version"]="ce03.compiled-sources.v1",["buildManifestPath"]=manifestPath,["files"]=fingerprints}),Utf8);
  File.Copy("QA/Ce03CommandIntegration/synthetic-r6-transport.json",folder+"/recovery-fixture.json");AssetDatabase.Refresh();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(prior.path)?NewSceneMode.Single:NewSceneMode.Additive);
  try{
   new GameObject("ExplicitCe03PlayerEvidence").AddComponent<Ce03PlayerEvidenceRunner>().Initialize(binding,AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/compiled-sources.json"),AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/recovery-fixture.json"));
   string scenePath=folder+"/Evidence.unity";EditorSceneManager.SaveScene(scene,scenePath);AssetDatabase.SaveAssets();
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName=output+"/Ce03Evidence.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
   if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Player evidence build failed");
   var rows=new JArray();foreach(var path in Directory.GetFiles(output,"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal)){using var hash=SHA256.Create();using var stream=File.OpenRead(path);rows.Add(new JObject{["path"]=path.Substring(output.Length+1).Replace('\\','/'),["sha256"]=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()});}
   File.WriteAllText(manifestPath,C(new JObject{["version"]="ce03.player-build.v1.20261022",["files"]=rows}),Utf8);
   File.WriteAllText(output+"-inputs.json",new JObject{["result"]=report.summary.result.ToString(),["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["buildRoot"]=output,["executable"]=output+"/Ce03Evidence.exe",["sourceRoot"]=root,["buildManifestPath"]=manifestPath,["sourceFiles"]=new JArray(names.Concat(new[]{manifestPath})),["scene"]=scenePath,["manifestHash"]=binding.ManifestHash}.ToString(),Utf8);
  }finally{if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);if(scene.IsValid()&&scene.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(scene,true);}}
 }
}
#endif
