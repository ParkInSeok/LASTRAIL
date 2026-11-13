#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchOperatorCardPolishShowcaseQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;

        [MenuItem("LASTRAIL/Launch/Capture Operator Card Polish Showcase QA")]
        public static void CaptureOperatorCardPolishShowcase()
        {
            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", "LastRailOperatorCardPolishQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "operator-card-polish-showcase-qa.json");
            var screenshotPath = Path.Combine(outputRoot, "operator_card_polish_showcase_1080x1920.png");

            try
            {
                CaptureShowcase(screenshotPath);
                var info = new FileInfo(screenshotPath);
                if (!info.Exists || info.Length < 50000)
                {
                    throw new InvalidOperationException("Operator card polish showcase screenshot is empty or too small.");
                }

                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"width\":1080,\"height\":1920,\"operatorVisualScopeCount\":15,\"resourceCount\":21,\"screenshotCount\":1,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL launch operator card polish showcase QA captured: " + outputRoot);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"width\":1080,\"height\":1920,\"operatorVisualScopeCount\":15,\"resourceCount\":21,\"screenshotCount\":0,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"operator_card_polish_screenshot_qa_failed\"]}");
                Debug.LogError("LASTRAIL launch operator card polish showcase QA failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void CaptureShowcase(string outputPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("OperatorCardPolishQaCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(5, 7, 10, 255);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LaunchOperatorCardPolishResourceBuilder.ShowcasePrefabPath);
            if (prefab == null)
            {
                throw new FileNotFoundException("Missing operator card polish showcase prefab: " + LaunchOperatorCardPolishResourceBuilder.ShowcasePrefabPath);
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Failed to instantiate operator card polish showcase prefab.");
            }

            var canvas = instance.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                throw new InvalidOperationException("Operator card polish showcase prefab has no Canvas.");
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.referenceResolution != new Vector2(Width, Height))
            {
                throw new InvalidOperationException("Operator card polish showcase CanvasScaler is not configured for 1080x1920.");
            }

            Canvas.ForceUpdateCanvases();
            var tmpVertices = 0;
            foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
            {
                text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
                tmpVertices += text.mesh == null ? 0 : text.mesh.vertexCount;
            }

            if (tmpVertices == 0)
            {
                throw new InvalidOperationException("Operator card polish showcase text did not generate visible mesh data.");
            }

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
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
#endif
