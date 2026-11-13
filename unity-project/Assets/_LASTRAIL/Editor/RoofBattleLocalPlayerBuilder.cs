#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using LastRail.Presentation.RoofBattle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace LastRail.Editor {
 // Authoring-only test-source reflection. Built Player has no NUnit/test-assembly reference.
 public static class RoofBattleLocalPlayerBuilder {
  const string Catalog="Assets/_LASTRAIL/UI/CombatAnimations/FiveAgentReferenceV02/ApprovedVisualAssets.asset";
  const string Font="Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset";
  const string AssetsRoot="Assets/_LASTRAIL/UI/CombatAnimations/RoofBattleLocalV01";
  static string ProjectPath(string value){string full=Path.GetFullPath(value).Replace('\\','/');if(!full.StartsWith("D:/Project/LASTRAIL/",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Canonical D project output required.");return full;}
  static void WriteNew(string path,string value){using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream,new UTF8Encoding(false)))writer.Write(value);}
  [MenuItem("LASTRAIL/Visual QA/Build Explicit Roof B LOCAL Development Player")]
  public static void Build(){
   if(!string.Equals(ProjectPath(Application.dataPath).TrimEnd('/'),"D:/Project/LASTRAIL/unity-project/Assets",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use canonical D Unity project, not stale worktree.");
   string requested=Environment.GetEnvironmentVariable("LASTRAIL_ROOF_PLAYER_OUTPUT");
   if(string.IsNullOrWhiteSpace(requested)||!Path.IsPathRooted(requested))throw new ArgumentException("Set LASTRAIL_ROOF_PLAYER_OUTPUT to an explicit absolute D project QA directory.");
   string outputRoot=ProjectPath(requested).TrimEnd('/');
   if(!outputRoot.StartsWith("D:/Project/LASTRAIL/QA/",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Player output must remain inside D project QA.");
   var catalog=AssetDatabase.LoadAssetAtPath<FiveAgentVisualAssets>(Catalog)??throw new InvalidOperationException("Existing approved catalog is missing.");
   var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Font)??throw new InvalidOperationException("Existing BootstrapFont is missing.");
   var sourceType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LastRail.Tests.EditMode.RoofBattleLocalStorageTests",false)).FirstOrDefault(t=>t!=null)??throw new InvalidOperationException("Authoring source factory test assembly not loaded.");
   var factory=sourceType.GetMethod("SourceEnvelope",BindingFlags.Public|BindingFlags.Static)??throw new MissingMethodException(sourceType.FullName,"SourceEnvelope");
   var envelope=factory.Invoke(null,null) as JObject??throw new InvalidOperationException("Original source factory did not supply JObject.");
   string digest=BDirectAttackInput.Digest(envelope);
   var verified=RoofBattleLocalSource.Read(envelope,digest);
   if(verified.Binding.Composition.Attack.OperationallyExecutable)throw new InvalidOperationException("This builder is only for non-operational LOCAL input.");
   string token=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N");
   string context="LOCAL-roof-eve-"+Guid.NewGuid().ToString("N");
   string assetDirectory=AssetsRoot+"/"+token;
   string output=outputRoot+"/roof-local-"+token;
   string checkpoints=output+"/checkpoint";
   string sourceAsset=assetDirectory+"/OriginalLocalSource.json";
   string sceneAsset=assetDirectory+"/RoofBattleLocal.unity";
   string executable=output+"/RoofBattleLocal.exe";
   string unityRoot=Path.GetDirectoryName(Application.dataPath);
   Directory.CreateDirectory(ProjectPath(Path.Combine(unityRoot,assetDirectory)));Directory.CreateDirectory(output);
   WriteNew(ProjectPath(Path.Combine(unityRoot,sourceAsset)),envelope.ToString(Formatting.None));
   AssetDatabase.ImportAsset(sourceAsset,ImportAssetOptions.ForceSynchronousImport);
   var text=AssetDatabase.LoadAssetAtPath<TextAsset>(sourceAsset)??throw new InvalidOperationException("Original source TextAsset import failed.");
   AssetDatabase.SetLabels(text,new[]{"LOCAL_NOT_OPERATIONAL","EXISTING_EVE_BASIC","NEUTRAL_BODY_ONLY","SPATIAL_FX_SUPPRESSED"});
   Scene previous=SceneManager.GetActiveScene();Scene created=default;BuildReport report=null;
   try {
    var mode=NewSceneMode.Additive;
    if(string.IsNullOrEmpty(previous.path)){
     Debug.Log("Roof authoring existing untitled scene: dirty="+previous.isDirty+" roots="+previous.rootCount+" loaded="+SceneManager.sceneCount);
     if(!Application.isBatchMode||previous.isDirty||previous.rootCount!=0||SceneManager.sceneCount!=1)throw new InvalidOperationException("Preserve unsaved scene content: save or close it explicitly before this scoped build.");
     // A clean, empty batch bootstrap has no user content to overwrite and cannot coexist additively in Unity.
     mode=NewSceneMode.Single;
    }
    created=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,mode);SceneManager.SetActiveScene(created);
    var input=new GameObject("RoofLocalInput",typeof(EventSystem),typeof(InputSystemUIInputModule));
    input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    var root=new GameObject("RoofBattleLocalComposition");
    var scene=root.AddComponent<RoofBattleLocalScene>();
    scene.Initialize(text,digest,context,catalog,font,checkpoints);
    if(!EditorSceneManager.SaveScene(created,sceneAsset))throw new IOException("Failed to save fresh LOCAL composition scene.");
    AssetDatabase.ImportAsset(sceneAsset,ImportAssetOptions.ForceSynchronousImport);
    var sceneObject=AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneAsset);
    AssetDatabase.SetLabels(sceneObject,new[]{"LOCAL_NOT_OPERATIONAL","NEUTRAL_BODY_ONLY","NO_WORLD_MAPPING","ONE_SUPPORTED_ACTOR"});
    AssetDatabase.SaveAssets();
    report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{sceneAsset},locationPathName=executable,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
    var audit=new JObject{["version"]="unity.roof-local-development-build.v1.20261017",["status"]=report.summary.result.ToString(),["classification"]="LOCAL_NOT_OPERATIONAL",["contextId"]=context,["sourceFactory"]="LastRail.Tests.EditMode.RoofBattleLocalStorageTests.SourceEnvelope (Editor authoring only)",["sourceAsset"]=sourceAsset,["sourceDigest"]=digest,["bindingHash"]=verified.Binding.OriginalHash,["scene"]=sceneAsset,["player"]=executable,["checkpointDirectory"]=checkpoints,["suggestedQaDirectory"]=output+"/player-qa-"+Guid.NewGuid().ToString("N"),["catalog"]=Catalog,["font"]=Font,["supportedActorCount"]=1,["actorId"]="op_eve",["visualSlotIndex"]=2,["neutralBodyOnly"]=true,["nativeFacingSupplied"]=false,["bodyActionPhaseSupplied"]=false,["spatialFxSuppressed"]=true,["worldProjectionSupplied"]=false,["backgroundComposite"]=false,["development"]=true,["buildErrors"]=report.summary.totalErrors,["buildWarnings"]=report.summary.totalWarnings,["playerExecuted"]=false};
    WriteNew(output+"/build-authoring.json",audit.ToString());
    if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Roof LOCAL Player build failed: "+report.summary.result+"; errors="+report.summary.totalErrors);
    Debug.Log("Roof LOCAL Development Player built (not executed): "+executable+"; source "+digest);
   }finally {
    if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
    if(created.IsValid()&&created.isLoaded){
     if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
     else EditorSceneManager.CloseScene(created,true);
    }
   }
  }
 }
}
#endif