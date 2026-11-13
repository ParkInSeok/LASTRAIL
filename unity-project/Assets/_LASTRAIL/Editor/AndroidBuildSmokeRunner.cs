#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace LastRail.Editor
{
    public static class AndroidBuildSmokeRunner
    {
        private const string SceneDirectory = "Assets/_LASTRAIL/Scenes";
        private const string SmokeScenePath = SceneDirectory + "/AndroidBuildSmoke.unity";
        private const string DefaultOutputPath = "Builds/AndroidSmoke/LASTRAIL-AndroidSmoke.apk";

        private static readonly string[] ScreenPrefabPaths =
        {
            "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/CodexArchiveScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/RewardReviewScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/OfflineRewardScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/StoryProgressSummaryScreen.prefab"
        };

        [MenuItem("LASTRAIL/Launch/Build Android Smoke APK")]
        public static void BuildAndroidSmokeApk()
        {
            var outputPath = GetArgumentValue("-lastRailAndroidSmokeOutput");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = DefaultOutputPath;
            }

            var scenePath = CreateOrUpdateSmokeScene();
            BuildAndroidSmoke(outputPath, scenePath, exportGradleProject: false);
        }

        [MenuItem("LASTRAIL/Launch/Export Android Smoke Gradle Project")]
        public static void ExportAndroidSmokeGradleProject()
        {
            var outputPath = GetArgumentValue("-lastRailAndroidSmokeOutput");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = "Builds/AndroidSmoke/GradleProject";
            }

            var scenePath = CreateOrUpdateSmokeScene();
            BuildAndroidSmoke(outputPath, scenePath, exportGradleProject: true);
        }

        public static string CreateOrUpdateSmokeScene()
        {
            EnsureDirectory(SceneDirectory);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "AndroidBuildSmoke";

            var root = new GameObject("AndroidBuildSmokeRoot");
            root.SetActive(true);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(root.transform, false);

            for (var i = 0; i < ScreenPrefabPaths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabPaths[i]);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Missing launch screen prefab for Android smoke: " + ScreenPrefabPaths[i]);
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = Path.GetFileNameWithoutExtension(ScreenPrefabPaths[i]);
                instance.transform.SetParent(root.transform, false);
                instance.SetActive(i == 0);
            }

            EditorSceneManager.SaveScene(scene, SmokeScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return SmokeScenePath;
        }

        private static void BuildAndroidSmoke(string outputPath, string scenePath, bool exportGradleProject)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Android smoke output path is required.", nameof(outputPath));
            }

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException("Android smoke scene path is required.", nameof(scenePath));
            }

            var normalizedOutputPath = outputPath.Replace('\\', '/');
            var directory = Path.GetDirectoryName(normalizedOutputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = exportGradleProject;

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = normalizedOutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.StrictMode
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Android smoke build failed: " + summary.result + ", errors=" + summary.totalErrors + ", warnings=" + summary.totalWarnings);
            }

            Debug.Log("LASTRAIL Android smoke build succeeded: " + normalizedOutputPath + " size=" + summary.totalSize);
        }

        private static string GetArgumentValue(string argumentName)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], argumentName, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folder))
            {
                throw new InvalidOperationException("Invalid AssetDatabase folder path: " + path);
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureDirectory(parent);
            }

            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
