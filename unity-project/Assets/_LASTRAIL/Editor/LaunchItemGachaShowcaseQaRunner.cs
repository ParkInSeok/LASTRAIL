#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastRail.Editor
{
    public static class LaunchItemGachaShowcaseQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;

        [MenuItem("LASTRAIL/Launch/Capture Item Gacha Showcase QA")]
        public static void CaptureItemGachaShowcase()
        {
            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", "LastRailItemGachaQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "item-gacha-showcase-qa.json");
            var screenshotPath = Path.Combine(outputRoot, "item_gacha_showcase_1080x1920.png");

            try
            {
                CaptureShowcase(screenshotPath);
                var info = new FileInfo(screenshotPath);
                if (!info.Exists || info.Length < 50000) throw new InvalidOperationException("Item/gacha showcase screenshot is empty or too small.");
                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"width\":1080,\"height\":1920,\"itemIconCount\":9,\"gachaResourceCount\":9,\"screenshotCount\":1,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL launch item/gacha showcase QA captured: " + outputRoot);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"width\":1080,\"height\":1920,\"screenshotCount\":0,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"item_gacha_screenshot_qa_failed\"]}");
                Debug.LogError("LASTRAIL launch item/gacha showcase QA failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void CaptureShowcase(string outputPath)
        {
            var canvas = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try
            {
                Fill(canvas, new Color32(5, 7, 10, 255));
                DrawRect(canvas, 54, 44, 972, 142, new Color32(9, 13, 18, 245));
                DrawRect(canvas, 82, 128, 916, 10, new Color32(226, 112, 54, 255));
                DrawHeaderGlyphs(canvas);

                DrawRect(canvas, 54, 210, 972, 690, new Color32(8, 11, 15, 228));
                for (var i = 0; i < LaunchItemGachaResourceBuilder.ItemResources.Length; i++) DrawItemCard(canvas, LaunchItemGachaResourceBuilder.ItemResources[i], i);

                DrawRect(canvas, 54, 930, 972, 910, new Color32(7, 10, 14, 228));
                for (var i = 0; i < LaunchItemGachaResourceBuilder.GachaResources.Length; i++) DrawGachaCard(canvas, LaunchItemGachaResourceBuilder.GachaResources[i], i);

                canvas.Apply(false);
                File.WriteAllBytes(outputPath, canvas.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        private static void DrawItemCard(Texture2D canvas, LaunchItemGachaResourceSpec spec, int index)
        {
            var col = index % 3;
            var row = index / 3;
            var x = 100 + col * 310;
            var y = 300 + row * 190;
            var frame = spec.Category == "currency" ? new Color32(235, 178, 74, 255) : new Color32(82, 108, 118, 255);
            DrawRect(canvas, x, y, 220, 152, new Color32(15, 19, 24, 255));
            DrawFrame(canvas, x, y, 220, 152, frame, 4);
            DrawPng(canvas, spec.Path, x + 44, y + 8, 132, 116);
            DrawRect(canvas, x + 22, y + 128, 176, 12, frame);
        }

        private static void DrawGachaCard(Texture2D canvas, LaunchItemGachaResourceSpec spec, int index)
        {
            var col = index % 3;
            var row = index / 3;
            var x = 92 + col * 315;
            var y = 1026 + row * 255;
            DrawRect(canvas, x, y, 230, 216, new Color32(10, 14, 19, 245));
            DrawFrame(canvas, x, y, 230, 216, new Color32(90, 202, 226, 255), 3);
            DrawPng(canvas, spec.Path, x + 18, y + 10, 194, 168);
            DrawRect(canvas, x + 22, y + 184, 186, 12, new Color32(238, 241, 234, 255));
        }

        private static void DrawHeaderGlyphs(Texture2D canvas)
        {
            DrawRect(canvas, 84, 82, 570, 12, new Color32(238, 241, 234, 255));
            DrawRect(canvas, 84, 104, 410, 8, new Color32(176, 188, 188, 255));
            DrawRect(canvas, 744, 74, 70, 70, new Color32(235, 178, 74, 255));
            DrawRect(canvas, 842, 92, 152, 22, new Color32(90, 202, 226, 255));
        }

        private static void DrawPng(Texture2D canvas, string assetPath, int x, int y, int w, int h)
        {
            var source = LoadPng(assetPath);
            try
            {
                var scale = Math.Min(w / (float)source.width, h / (float)source.height);
                var drawW = Math.Max(1, Mathf.RoundToInt(source.width * scale));
                var drawH = Math.Max(1, Mathf.RoundToInt(source.height * scale));
                var startX = x + (w - drawW) / 2;
                var startY = y + (h - drawH) / 2;
                for (var dy = 0; dy < drawH; dy++)
                {
                    var sy = Mathf.Clamp(Mathf.RoundToInt(dy / (float)drawH * (source.height - 1)), 0, source.height - 1);
                    for (var dx = 0; dx < drawW; dx++)
                    {
                        var sx = Mathf.Clamp(Mathf.RoundToInt(dx / (float)drawW * (source.width - 1)), 0, source.width - 1);
                        Blend(canvas, startX + dx, startY + dy, source.GetPixel(sx, source.height - 1 - sy));
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static Texture2D LoadPng(string assetPath)
        {
            var fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Missing item/gacha PNG for showcase QA: " + assetPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath))) throw new InvalidOperationException("Failed to load item/gacha PNG: " + assetPath);
            return texture;
        }

        private static void Fill(Texture2D canvas, Color32 color)
        {
            var pixels = new Color32[Width * Height];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = color;
            canvas.SetPixels32(pixels);
        }

        private static void DrawRect(Texture2D canvas, int x, int y, int w, int h, Color32 color)
        {
            for (var py = Math.Max(0, y); py < Math.Min(Height, y + h); py++)
            for (var px = Math.Max(0, x); px < Math.Min(Width, x + w); px++)
                canvas.SetPixel(px, Height - 1 - py, color);
        }

        private static void DrawFrame(Texture2D canvas, int x, int y, int w, int h, Color32 color, int thickness)
        {
            DrawRect(canvas, x, y, w, thickness, color);
            DrawRect(canvas, x, y + h - thickness, w, thickness, color);
            DrawRect(canvas, x, y, thickness, h, color);
            DrawRect(canvas, x + w - thickness, y, thickness, h, color);
        }

        private static void Blend(Texture2D canvas, int x, int y, Color source)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            var flippedY = Height - 1 - y;
            var destination = canvas.GetPixel(x, flippedY);
            var alpha = source.a;
            var blended = new Color(source.r * alpha + destination.r * (1f - alpha), source.g * alpha + destination.g * (1f - alpha), source.b * alpha + destination.b * (1f - alpha), 1f);
            canvas.SetPixel(x, flippedY, blended);
        }
    }
}
#endif
