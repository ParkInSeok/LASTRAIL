#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastRail.Editor {
 public static class ApprovedFeedbackPlayerBuilder {
  public static void Build(){
   if(Application.dataPath.Replace('\\','/')!="D:/Project/LASTRAIL/unity-project/Assets")throw new InvalidOperationException("Canonical project required.");
   string requested=Environment.GetEnvironmentVariable("LASTRAIL_APPROVED_FEEDBACK_PLAYER_OUTPUT");
   if(string.IsNullOrWhiteSpace(requested))throw new ArgumentException("Explicit QA output required.");
   string output=Path.GetFullPath(requested).Replace('\\','/');if(!output.StartsWith("D:/Project/LASTRAIL/QA/",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Canonical D QA required.");
   if(File.Exists(output+"/build-authoring.json")||File.Exists(output+"/ApprovedFeedback.exe"))throw new IOException("Fresh build output required.");
   string token="approved-feedback-"+Guid.NewGuid().ToString("N"),assets="Assets/_LASTRAIL/UI/CombatAnimations/ApprovedFeedbackQa/"+token;
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LastRail.Tests.EditMode.RoofMiraLocalOwnerTests")).FirstOrDefault(t=>t!=null)??throw new InvalidOperationException("Existing Mira source authoring fixture missing.");
   var source=(JObject)type.GetMethod("SourceEnvelope").Invoke(null,null);string hash=BDirectAttackInput.Digest(source);
   // Authoring-only reuse of preserved Server modules/profile. No NUnit reference enters Player.
   var sourceType=typeof(LastRail.Presentation.RoofBattle.RoofMiraLocalSource);LastRail.Presentation.RoofBattle.RoofMiraLocalSource.Read(source,hash);
   Directory.CreateDirectory(output);Directory.CreateDirectory("D:/Project/LASTRAIL/unity-project/"+assets);
   string sourceAsset=assets+"/OriginalMiraLocalSource.json",sceneAsset=assets+"/ApprovedFeedback.unity";
   File.WriteAllText("D:/Project/LASTRAIL/unity-project/"+sourceAsset,source.ToString());AssetDatabase.ImportAsset(sourceAsset,ImportAssetOptions.ForceSynchronousImport);
   var text=AssetDatabase.LoadAssetAtPath<TextAsset>(sourceAsset);Scene old=SceneManager.GetActiveScene(),created=default;
   try{
    var mode=NewSceneMode.Additive;
    if(string.IsNullOrEmpty(old.path)){if(!Application.isBatchMode||old.isDirty||old.rootCount!=0||SceneManager.sceneCount!=1)throw new InvalidOperationException("Preserve unsaved scene content.");mode=NewSceneMode.Single;}
    created=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,mode);SceneManager.SetActiveScene(created);
    new GameObject("ExplicitApprovedMiraFeedbackQa").AddComponent<ApprovedFeedbackPlayerProbe>().Initialize(text,hash);
    if(!EditorSceneManager.SaveScene(created,sceneAsset))throw new IOException("QA scene save failed.");AssetDatabase.SaveAssets();
    string player=output+"/ApprovedFeedback.exe";var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{sceneAsset},locationPathName=player,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
    File.WriteAllText(output+"/build-authoring.json",new JObject{["status"]=report.summary.result.ToString(),["player"]=player,["sourceHash"]=hash,["sourceAsset"]=sourceAsset,["scene"]=sceneAsset,["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,["scope"]="APPROVED_FIRST_MIRA_LOCAL_FEEDBACK_NOT_ULT_OR_FULL_ROOF",["playerExecuted"]=false}.ToString());
    if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Feedback QA build failed.");
   }finally{if(old.IsValid()&&old.isLoaded)SceneManager.SetActiveScene(old);if(created.IsValid()&&created.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(created,true);}}
  }
 }
}
#endif
