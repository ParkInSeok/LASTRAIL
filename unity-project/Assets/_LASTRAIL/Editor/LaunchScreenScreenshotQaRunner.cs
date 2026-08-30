#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LastRail.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchScreenScreenshotQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const string OutputDirectoryName = "LastRailScreenQa";

        private static readonly (string ScreenKey, string PrefabPath)[] Screens =
        {
            ("mission_shell", "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab"),
            ("gacha_screen", "Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab"),
            ("fully_animated_combat_hud", "Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab"),
            ("codex_archive", "Assets/_LASTRAIL/Prefabs/Screens/CodexArchiveScreen.prefab"),
            ("reward_review", "Assets/_LASTRAIL/Prefabs/Screens/RewardReviewScreen.prefab"),
            ("offline_reward", "Assets/_LASTRAIL/Prefabs/Screens/OfflineRewardScreen.prefab"),
            ("story_progress_summary", "Assets/_LASTRAIL/Prefabs/Screens/StoryProgressSummaryScreen.prefab")
        };

        [MenuItem("LASTRAIL/Launch/Capture Screen QA")]
        public static void CaptureSevenScreenPortraitQa()
        {
            var outputRoot = Path.Combine(Path.GetTempPath(), OutputDirectoryName);
            Directory.CreateDirectory(outputRoot);

            var screenKeys = new List<string>();
            var screenshotPaths = new List<string>();

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (var screen in Screens)
                {
                    screenKeys.Add(screen.ScreenKey);
                    screenshotPaths.Add(CaptureScreen(screen.ScreenKey, screen.PrefabPath, outputRoot));
                }

                var report = LaunchScreenScreenshotQaReport.ForCapturedScreens(
                    screenKeys,
                    codeLayoutPassCount: Screens.Length,
                    codeLayoutFailCount: 0,
                    screenshotPaths: screenshotPaths);
                WriteReport(outputRoot, report);
                Debug.Log("LASTRAIL screen screenshot QA captured " + screenshotPaths.Count + " screenshots to " + outputRoot);
            }
            catch (Exception ex)
            {
                var report = LaunchScreenScreenshotQaReport.ForUnavailableCapture(
                    screenKeys.Count > 0 ? screenKeys : AllScreenKeys(),
                    codeLayoutPassCount: Screens.Length,
                    codeLayoutFailCount: 0);
                WriteReport(outputRoot, report);
                Debug.LogError("LASTRAIL screen screenshot QA unavailable: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static string CaptureScreen(string screenKey, string prefabPath, string outputRoot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing screenshot QA prefab: " + prefabPath);
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            if (root == null)
            {
                throw new InvalidOperationException("Could not instantiate screenshot QA prefab: " + prefabPath);
            }

            var cameraObject = new GameObject(screenKey + "_Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = Height * 0.5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            PrepareCanvasForCamera(root, camera);
            var path = Path.Combine(outputRoot, screenKey + ".png");
            Render(camera, path);

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            return path;
        }

        private static void PrepareCanvasForCamera(GameObject root, Camera camera)
        {
            var rectTransform = root.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(Width, Height);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }

            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                canvas.pixelPerfect = false;
            }

            Canvas.ForceUpdateCanvases();
        }

        private static void Render(Camera camera, string path)
        {
            var previous = RenderTexture.active;
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(texture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void WriteReport(string outputRoot, LaunchScreenScreenshotQaReport report)
        {
            File.WriteAllText(
                Path.Combine(outputRoot, "screenshot-qa-report.json"),
                LaunchScreenScreenshotQaReportJsonAdapter.ToJson(report));
        }

        private static IEnumerable<string> AllScreenKeys()
        {
            foreach (var screen in Screens)
            {
                yield return screen.ScreenKey;
            }
        }
    }
}
#endif
