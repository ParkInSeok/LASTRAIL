#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchAudioShowcaseQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;

        [MenuItem("LASTRAIL/Launch/Capture Audio Resource Showcase QA")]
        public static void CaptureAudioShowcase()
        {
            var outputRoot = Path.Combine(Path.GetTempPath(), "LastRailAudioQa");
            Directory.CreateDirectory(outputRoot);
            var screenshotPath = Path.Combine(outputRoot, "audio_resource_showcase_1080x1920.png");
            var reportPath = Path.Combine(outputRoot, "audio-resource-showcase-qa.json");

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CapturePrefab(screenshotPath);
                var info = new FileInfo(screenshotPath);
                if (!info.Exists || info.Length < 20000) throw new InvalidOperationException("Audio showcase screenshot is empty or too small.");
                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"width\":1080,\"height\":1920,\"clipCount\":" + LaunchAudioResourceBuilder.Clips.Length + ",\"screenshotCount\":1,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL audio showcase QA captured: " + outputRoot);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"width\":1080,\"height\":1920,\"clipCount\":" + LaunchAudioResourceBuilder.Clips.Length + ",\"screenshotCount\":0,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"audio_showcase_screenshot_qa_failed\"]}");
                Debug.LogError("LASTRAIL audio showcase QA failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }


        private static void CapturePrefab(string outputPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LaunchAudioResourceBuilder.ShowcasePrefabPath);
            if (prefab == null) throw new InvalidOperationException("Missing audio showcase prefab.");

            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            var cameraObject = new GameObject("AudioShowcaseCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = Height * 0.5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            try
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.worldCamera = camera;
                    canvas.pixelPerfect = false;
                }

                var rect = root.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(Width, Height);
                    rect.anchoredPosition = Vector2.zero;
                    rect.localScale = Vector3.one;
                }

                var tmpCount = 0;
                var thumbnailCount = 0;
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
                    if (!string.IsNullOrWhiteSpace(text.text)) tmpCount++;
                }

                foreach (var image in root.GetComponentsInChildren<Image>(true))
                {
                    if (image.name == "SourceArtThumbnail" && image.sprite != null) thumbnailCount++;
                }

                if (tmpCount == 0 || thumbnailCount < LaunchAudioResourceBuilder.Clips.Length)
                {
                    throw new InvalidOperationException("Audio showcase prefab is missing visible TMP text or launch art thumbnails.");
                }

                Canvas.ForceUpdateCanvases();
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
                    ValidateCapturedPixels(texture);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previous;
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(texture);
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Texture2D ComposeShowcase()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            Fill(texture, C(5, 8, 12, 255));
            FillPanel(texture, 54, 48, 972, 146, C(8, 12, 16, 255), C(232, 174, 76, 255));
            FillRect(texture, 92, 98, 620, 10, C(238, 241, 234, 255));
            FillRect(texture, 92, 140, 470, 8, C(172, 184, 184, 255));

            var columns = new[]
            {
                new CategoryColumn("ui_sfx", 76, 252, C(58, 183, 191, 255)),
                new CategoryColumn("gacha_sfx", 568, 252, C(232, 174, 76, 255)),
                new CategoryColumn("reward_sfx", 76, 740, C(86, 194, 119, 255)),
                new CategoryColumn("combat_sfx", 568, 740, C(219, 82, 72, 255)),
                new CategoryColumn("story_raid_sfx", 76, 1320, C(226, 122, 54, 255)),
                new CategoryColumn("bgm", 568, 1320, C(126, 190, 205, 255))
            };

            foreach (var column in columns)
            {
                DrawCategory(texture, column);
            }

            texture.Apply();
            return texture;
        }

        private static void DrawCategory(Texture2D texture, CategoryColumn column)
        {
            FillPanel(texture, column.X, column.Y, 436, column.Category == "combat_sfx" ? 520 : 420, C(10, 16, 20, 246), column.Accent);
            FillRect(texture, column.X + 32, column.Y + 40, 190, 8, column.Accent);
            var itemY = column.Y + 92;
            foreach (var clip in LaunchAudioResourceBuilder.Clips)
            {
                if (!string.Equals(clip.Category, column.Category, StringComparison.Ordinal)) continue;
                FillRect(texture, column.X + 32, itemY, 372, 46, C(20, 31, 35, 255));
                FillRect(texture, column.X + 42, itemY + 12, Mathf.Clamp(Mathf.RoundToInt(clip.DurationSeconds * 70f), 20, 270), 8, column.Accent);
                FillRect(texture, column.X + 42, itemY + 28, clip.Loop ? 110 : 64, 6, clip.Loop ? C(232, 174, 76, 255) : C(172, 184, 184, 255));
                itemY += 58;
            }
        }

        private static void FillPanel(Texture2D texture, int x, int y, int width, int height, Color32 color, Color32 accent)
        {
            FillRect(texture, x, y, width, height, color);
            FillRect(texture, x, y, width, 4, accent);
            FillRect(texture, x, y + height - 4, width, 4, accent);
            FillRect(texture, x, y, 4, height, accent);
            FillRect(texture, x + width - 4, y, 4, height, accent);
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

        private static void ValidateCapturedPixels(Texture2D texture)
        {
            var variedPixels = 0;
            var pixels = texture.GetPixels32();
            var baseline = pixels.Length == 0 ? C(0, 0, 0, 255) : pixels[0];
            for (var y = 0; y < texture.height; y += 16)
            {
                for (var x = 0; x < texture.width; x += 16)
                {
                    var pixel = pixels[y * texture.width + x];
                    var diff = Mathf.Abs(pixel.r - baseline.r) + Mathf.Abs(pixel.g - baseline.g) + Mathf.Abs(pixel.b - baseline.b);
                    if (diff > 35) variedPixels++;
                }
            }

            if (variedPixels < 140) throw new InvalidOperationException("Audio showcase screenshot did not contain enough visible UI pixels.");
        }

        private static Color32 C(byte r, byte g, byte b, byte a)
        {
            return new Color32(r, g, b, a);
        }

        private readonly struct CategoryColumn
        {
            public CategoryColumn(string category, int x, int y, Color32 accent)
            {
                Category = category;
                X = x;
                Y = y;
                Accent = accent;
            }

            public readonly string Category;
            public readonly int X;
            public readonly int Y;
            public readonly Color32 Accent;
        }
    }
}
#endif
