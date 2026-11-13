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
    public static class LaunchRewardOfflineShowcaseQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;

        [MenuItem("LASTRAIL/Launch/Capture Reward Offline Showcase QA")]
        public static void CaptureRewardOfflineShowcase()
        {
            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", "LastRailRewardOfflineQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "reward-offline-showcase-qa.json");
            var screenshotPath = Path.Combine(outputRoot, "reward_offline_showcase_1080x1920.png");

            try
            {
                Capture(screenshotPath);
                var info = new FileInfo(screenshotPath);
                if (!info.Exists || info.Length < 50000)
                {
                    throw new InvalidOperationException("Reward/offline showcase screenshot is empty or too small.");
                }

                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"width\":1080,\"height\":1920,\"resourceCount\":9,\"screenshotCount\":1,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL reward/offline showcase QA captured: " + outputRoot);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"width\":1080,\"height\":1920,\"resourceCount\":9,\"screenshotCount\":0,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"reward_offline_showcase_screenshot_qa_failed\"]}");
                Debug.LogError("LASTRAIL reward/offline showcase QA failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void Capture(string outputPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("RewardOfflineQaCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(5, 8, 12, 255);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LaunchRewardOfflineResourceBuilder.ShowcasePrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing reward/offline showcase prefab.");
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            var canvas = instance.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;

            foreach (var image in instance.GetComponentsInChildren<Image>(true))
            {
                image.raycastTarget = false;
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
                throw new InvalidOperationException("Reward/offline showcase text did not generate mesh data.");
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
                UnityEngine.Object.DestroyImmediate(instance);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
#endif
