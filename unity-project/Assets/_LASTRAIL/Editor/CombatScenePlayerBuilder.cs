#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CombatScenePlayerBuilder
{
    public static void Build()
    {
        string output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_COMBAT_SCENE_BUILD")??throw new ArgumentException("Explicit build output required"));
        if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");
        if(Path.GetFullPath(Application.dataPath).Replace('\\','/')!="D:/Project/LASTRAIL/unity-project/Assets")throw new InvalidOperationException("Canonical Unity project required");
        var prior=SceneManager.GetActiveScene();if(prior.isDirty)throw new InvalidOperationException("Preserve unsaved scene");
        var binding=AssetDatabase.LoadAssetAtPath<LastRail.Battle.CombatPlayerAssetBinding>("Assets/_LASTRAIL/QA/CombatAssetBinding/89e63936213b4ce090e6a03fbb98bbe7/Binding.asset");if(binding==null)throw new InvalidOperationException("Pinned binding missing");
        string bindingPath=AssetDatabase.GetAssetPath(binding);
        string folder="Assets/_LASTRAIL/QA/CombatAssetBinding/"+Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(folder);Directory.CreateDirectory(output);
        File.Copy("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/BattlePlayer435-prepare/prepare.json",folder+"/prepare.json");AssetDatabase.Refresh();var prepared=AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/prepare.json");
        File.WriteAllText(output+"/manifest.json",binding.ManifestJson);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(prior.path)?NewSceneMode.Single:NewSceneMode.Additive);
        try
        {
            new GameObject("CombatSceneProbe").AddComponent<CombatScenePlayerProbe>().Initialize(binding,prepared);
            
            string path=folder+"/AssetBinding.unity";EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName=output+"/CombatScene.exe",target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.Development,extraScriptingDefines=new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
            File.WriteAllText(output+"/build.json",new JObject{["result"]=report.summary.result.ToString(),["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings,
                ["manifestHash"]=binding.ManifestHash,["scene"]=path,["bindingAsset"]=bindingPath,["scope"]="PRE_ADMISSION_SCENE_NO_START"}.ToString());
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Asset Player build failed");
        }
        finally
        {
            if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);
            if(scene.IsValid()&&scene.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
#endif


