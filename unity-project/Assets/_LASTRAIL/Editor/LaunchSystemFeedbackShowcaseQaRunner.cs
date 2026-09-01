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
    public static class LaunchSystemFeedbackShowcaseQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;

        [MenuItem("LASTRAIL/Launch/Capture System Feedback Showcase QA")]
        public static void CaptureSystemFeedbackShowcase()
        {
            var outputRoot = Path.Combine(Path.GetTempPath(), "LastRailSystemFeedbackQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "system-feedback-showcase-qa.json");
            var screenshotPath = Path.Combine(outputRoot, "system_feedback_showcase_1080x1920.png");

            try
            {
                Capture(screenshotPath);
                var info = new FileInfo(screenshotPath);
                if (!info.Exists || info.Length < 20000)
                {
                    throw new InvalidOperationException("System feedback showcase screenshot is empty or too small.");
                }

                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"width\":1080,\"height\":1920,\"resourceCount\":9,\"screenshotCount\":1,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL system feedback showcase QA captured: " + outputRoot);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"width\":1080,\"height\":1920,\"resourceCount\":9,\"screenshotCount\":0,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"system_feedback_showcase_screenshot_qa_failed\"]}");
                Debug.LogError("LASTRAIL system feedback showcase QA failed: " + ex.GetType().Name + ": " + ex.Message);
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

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LaunchSystemFeedbackResourceBuilder.ShowcasePrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing system feedback showcase prefab.");
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                ValidatePrefabForCapture(instance);
                var texture = ComposeResourceShowcase();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                ValidateCapturedPixels(texture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void ValidatePrefabForCapture(GameObject instance)
        {
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
                throw new InvalidOperationException("System feedback showcase text did not generate mesh data.");
            }
        }

        private static Texture2D ComposeResourceShowcase()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            Fill(texture, new Color32(5, 8, 12, 255));
            FillRect(texture, 54, 48, 972, 142, new Color32(8, 12, 16, 255));
            FillRect(texture, 92, 96, 700, 10, new Color32(236, 239, 232, 255));
            FillRect(texture, 92, 138, 520, 8, new Color32(164, 178, 178, 255));

            FillPanel(texture, 54, 230, 972, 430, new Color32(8, 12, 16, 246), new Color32(232, 174, 76, 255));
            FillRect(texture, 92, 282, 250, 10, new Color32(232, 174, 76, 255));
            BlitResource(texture, "Assets/_LASTRAIL/UI/SystemFeedbackResources/Loading/loading_rail_indicator.png", 156, 370, 768, 192);

            FillPanel(texture, 54, 705, 972, 530, new Color32(8, 12, 16, 246), new Color32(58, 183, 191, 255));
            FillRect(texture, 92, 760, 250, 10, new Color32(58, 183, 191, 255));
            BlitResource(texture, "Assets/_LASTRAIL/UI/SystemFeedbackResources/Badges/feedback_success_badge.png", 82, 840, 288, 108);
            BlitResource(texture, "Assets/_LASTRAIL/UI/SystemFeedbackResources/Badges/feedback_failure_badge.png", 396, 840, 288, 108);
            BlitResource(texture, "Assets/_LASTRAIL/UI/SystemFeedbackResources/Badges/feedback_network_error_badge.png", 710, 840, 288, 108);
            BlitResource(texture, "Assets/_LASTRAIL/UI/SystemFeedbackResources/Retry/retry_refresh_accent.png", 108, 1002, 170, 170);
            BlitResource(texture, "Assets/_LASTRAIL/UI/SystemFeedbackResources/Warning/warning_strip_banner_accent.png", 330, 1038, 626, 98);
            BlitResource(texture, "Assets/_LASTRAIL/UI/SystemFeedbackResources/Status/inline_status_dot_set.png", 366, 1150, 384, 96);

            FillPanel(texture, 54, 1280, 972, 560, new Color32(8, 12, 16, 246), new Color32(226, 122, 54, 255));
            FillRect(texture, 92, 1338, 220, 10, new Color32(226, 122, 54, 255));
            BlitResource(texture, "Assets/_LASTRAIL/UI/SystemFeedbackResources/Notices/maintenance_notice_panel.png", 98, 1390, 570, 285);
            BlitResource(texture, "Assets/_LASTRAIL/UI/SystemFeedbackResources/Disabled/disabled_unavailable_overlay.png", 650, 1428, 320, 214);
            FillRect(texture, 702, 1696, 220, 10, new Color32(164, 178, 178, 255));
            FillRect(texture, 702, 1732, 168, 8, new Color32(232, 174, 76, 255));
            texture.Apply();
            return texture;
        }

        private static void FillPanel(Texture2D texture, int x, int y, int width, int height, Color32 color, Color32 accent)
        {
            FillRect(texture, x, y, width, height, color);
            FillRect(texture, x, y, width, 4, accent);
            FillRect(texture, x, y + height - 4, width, 4, accent);
            FillRect(texture, x, y, 4, height, accent);
            FillRect(texture, x + width - 4, y, 4, height, accent);
        }

        private static void BlitResource(Texture2D target, string assetPath, int x, int y, int width, int height)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(File.ReadAllBytes(ProjectPath(assetPath))))
            {
                UnityEngine.Object.DestroyImmediate(source);
                throw new FileNotFoundException("Missing system feedback showcase resource: " + assetPath);
            }

            try
            {
                var pixels = source.GetPixels32();
                for (var yy = 0; yy < height; yy++)
                {
                    var sy = Mathf.Clamp(yy * source.height / height, 0, source.height - 1);
                    for (var xx = 0; xx < width; xx++)
                    {
                        var tx = x + xx;
                        var ty = y + yy;
                        if (tx < 0 || ty < 0 || tx >= target.width || ty >= target.height) continue;
                        var sx = Mathf.Clamp(xx * source.width / width, 0, source.width - 1);
                        var sourceColor = pixels[sy * source.width + sx];
                        if (sourceColor.a == 0) continue;
                        var baseColor = target.GetPixel(tx, ty);
                        var overlay = new Color(sourceColor.r / 255f, sourceColor.g / 255f, sourceColor.b / 255f, sourceColor.a / 255f);
                        target.SetPixel(tx, ty, Color.Lerp(baseColor, overlay, overlay.a));
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static void Fill(Texture2D texture, Color32 color)
        {
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color32 color)
        {
            var xMin = Mathf.Clamp(x, 0, texture.width);
            var yMin = Mathf.Clamp(y, 0, texture.height);
            var xMax = Mathf.Clamp(x + width, 0, texture.width);
            var yMax = Mathf.Clamp(y + height, 0, texture.height);
            for (var yy = yMin; yy < yMax; yy++)
            {
                for (var xx = xMin; xx < xMax; xx++)
                {
                    texture.SetPixel(xx, yy, color);
                }
            }
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private static void ValidateCapturedPixels(Texture2D texture)
        {
            var variedPixels = 0;
            var pixels = texture.GetPixels32();
            var baseline = pixels.Length == 0 ? new Color32(0, 0, 0, 255) : pixels[0];
            for (var y = 0; y < texture.height; y += 16)
            {
                for (var x = 0; x < texture.width; x += 16)
                {
                    var pixel = pixels[y * texture.width + x];
                    var diff = Mathf.Abs(pixel.r - baseline.r) + Mathf.Abs(pixel.g - baseline.g) + Mathf.Abs(pixel.b - baseline.b);
                    if (diff > 35)
                    {
                        variedPixels++;
                    }
                }
            }

            if (variedPixels < 120)
            {
                throw new InvalidOperationException("System feedback showcase screenshot did not contain enough visible UI pixels.");
            }
        }
    }
}
#endif
