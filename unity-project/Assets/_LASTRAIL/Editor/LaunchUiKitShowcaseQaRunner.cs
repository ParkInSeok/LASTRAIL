#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastRail.Editor
{
    public static class LaunchUiKitShowcaseQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/Common/Showcase/LRLaunchUiKitShowcase.prefab";

        [MenuItem("LASTRAIL/Launch/Capture UI Kit Showcase QA")]
        public static void CaptureShowcase()
        {
            var outputRoot = Path.Combine(Path.GetTempPath(), "LastRailUiKitQa");
            Directory.CreateDirectory(outputRoot);
            var screenshotPath = Path.Combine(outputRoot, "ui-kit-showcase.png");
            var reportPath = Path.Combine(outputRoot, "ui-kit-showcase-qa.json");
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Capture(ShowcasePrefabPath, screenshotPath);
                var info = new FileInfo(screenshotPath);
                if (!info.Exists || info.Length < 4096)
                {
                    throw new InvalidOperationException("UI kit showcase screenshot is empty or too small.");
                }

                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"width\":1080,\"height\":1920,\"screenshotCount\":1,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL UI kit showcase QA captured: " + screenshotPath);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"BLOCKED\",\"width\":1080,\"height\":1920,\"screenshotCount\":0,\"warningCount\":1,\"failCount\":0,\"blockers\":[\"ui_kit_showcase_screenshot_qa_unavailable\"]}");
                Debug.LogError("LASTRAIL UI kit showcase QA unavailable: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void Capture(string prefabPath, string outputPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing UI kit showcase prefab: " + prefabPath);
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            var cameraObject = new GameObject("UiKitShowcaseCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
                canvas.pixelPerfect = false;
            }

            var rect = root.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(Width, Height);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            Canvas.ForceUpdateCanvases();
            var tmpCount = 0;
            var tmpCharacters = 0;
            var rawTextCharacters = 0;
            var tmpVertices = 0;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.SetText(text.text);
                text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
                tmpCount++;
                tmpCharacters += text.textInfo.characterCount;
                rawTextCharacters += string.IsNullOrEmpty(text.text) ? 0 : text.text.Length;
                tmpVertices += text.mesh == null ? 0 : text.mesh.vertexCount;
            }

            if (tmpCount == 0 || rawTextCharacters == 0 || tmpCharacters == 0 || tmpVertices == 0)
            {
                throw new InvalidOperationException("UI kit showcase TMP text did not generate visible mesh data.");
            }

            Debug.Log("LASTRAIL UI kit TMP mesh ready: count=" + tmpCount + ", characters=" + tmpCharacters + ", vertices=" + tmpVertices);
            var previous = RenderTexture.active;
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
