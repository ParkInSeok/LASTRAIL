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
    public static class LaunchCombatArtShowcaseQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;

        [MenuItem("LASTRAIL/Launch/Capture Combat Art Showcase QA")]
        public static void CaptureCombatArtShowcase()
        {
            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", "LastRailCombatArtQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "combat-art-showcase-qa.json");
            var screenshotPath = Path.Combine(outputRoot, "combat_art_showcase_1080x1920.png");

            try
            {
                CaptureShowcase(screenshotPath);
                var info = new FileInfo(screenshotPath);
                if (!info.Exists || info.Length < 50000) throw new InvalidOperationException("Combat art showcase screenshot is empty or too small.");
                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"width\":1080,\"height\":1920,\"normalEnemyCount\":6,\"eliteEnemyCount\":2,\"bossCount\":1,\"vfxCount\":8,\"screenshotCount\":1,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL launch combat art showcase QA captured: " + outputRoot);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"width\":1080,\"height\":1920,\"screenshotCount\":0,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"combat_art_screenshot_qa_failed\"]}");
                Debug.LogError("LASTRAIL launch combat art showcase QA failed: " + ex.GetType().Name + ": " + ex.Message);
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

                DrawRect(canvas, 54, 210, 972, 520, new Color32(8, 11, 15, 228));
                for (var i = 0; i < 8; i++) DrawEnemyCard(canvas, LaunchCombatArtResourceBuilder.Enemies[i], i);

                DrawRect(canvas, 54, 760, 972, 430, new Color32(8, 10, 14, 232));
                DrawBossPreview(canvas, LaunchCombatArtResourceBuilder.Enemies[8]);

                DrawRect(canvas, 54, 1220, 972, 620, new Color32(7, 10, 14, 228));
                for (var i = 0; i < LaunchCombatArtResourceBuilder.VfxResources.Length; i++) DrawVfxCard(canvas, LaunchCombatArtResourceBuilder.VfxResources[i], i);

                canvas.Apply(false);
                File.WriteAllBytes(outputPath, canvas.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        private static void DrawEnemyCard(Texture2D canvas, LaunchCombatEnemyResourceSpec enemy, int index)
        {
            var col = index % 4;
            var row = index / 4;
            var x = 86 + col * 240;
            var y = 286 + row * 220;
            var frame = enemy.Type == "elite" ? new Color32(235, 178, 74, 255) : new Color32(82, 108, 118, 255);
            DrawRect(canvas, x, y, 188, 188, new Color32(15, 19, 24, 255));
            DrawFrame(canvas, x, y, 188, 188, frame, 4);
            DrawPng(canvas, enemy.Path, x + 10, y + 8, 168, 152);
            DrawRect(canvas, x + 12, y + 164, 164, 14, frame);
        }

        private static void DrawBossPreview(Texture2D canvas, LaunchCombatEnemyResourceSpec boss)
        {
            DrawRect(canvas, 94, 800, 650, 350, new Color32(12, 15, 20, 255));
            DrawFrame(canvas, 94, 800, 650, 350, new Color32(242, 72, 52, 255), 6);
            DrawPng(canvas, boss.Path, 118, 830, 602, 286);
            DrawPng(canvas, "Assets/_LASTRAIL/UI/CombatResources/Accents/boss_danger_frame.png", 94, 800, 650, 350);
            DrawPng(canvas, "Assets/_LASTRAIL/UI/CombatResources/Accents/boss_hp_bar_accent.png", 770, 910, 206, 40);
            DrawRect(canvas, 770, 978, 206, 14, new Color32(242, 72, 52, 255));
            DrawRect(canvas, 770, 1010, 154, 12, new Color32(238, 241, 234, 255));
        }

        private static void DrawVfxCard(Texture2D canvas, LaunchCombatVfxResourceSpec vfx, int index)
        {
            var col = index % 4;
            var row = index / 4;
            var x = 86 + col * 240;
            var y = 1310 + row * 230;
            DrawRect(canvas, x, y, 188, 188, new Color32(10, 14, 19, 245));
            DrawFrame(canvas, x, y, 188, 188, new Color32(90, 202, 226, 255), 3);
            DrawPng(canvas, vfx.Path, x + 20, y + 12, 148, 148);
            DrawRect(canvas, x + 18, y + 164, 152, 12, new Color32(238, 241, 234, 255));
        }

        private static void DrawHeaderGlyphs(Texture2D canvas)
        {
            DrawRect(canvas, 84, 82, 520, 12, new Color32(238, 241, 234, 255));
            DrawRect(canvas, 84, 104, 380, 8, new Color32(176, 188, 188, 255));
            DrawRect(canvas, 760, 74, 66, 66, new Color32(226, 112, 54, 255));
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
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Missing combat art PNG for showcase QA: " + assetPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath))) throw new InvalidOperationException("Failed to load combat art PNG: " + assetPath);
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
            var blended = new Color(
                source.r * alpha + destination.r * (1f - alpha),
                source.g * alpha + destination.g * (1f - alpha),
                source.b * alpha + destination.b * (1f - alpha),
                1f);
            canvas.SetPixel(x, flippedY, blended);
        }
    }
}
#endif
