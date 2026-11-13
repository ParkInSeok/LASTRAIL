#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace LastRail.Editor
{
    public static class AndroidBrandingBuilder
    {
        private const string Root = "Assets/_LASTRAIL/UI/Branding";
        private const string Qa = "QA/Act1Launch/AndroidBranding";
        private static readonly Color Background = new Color32(33, 39, 42, 255);

        [MenuItem("LAST RAIL/Resources/Build Android Branding Candidate")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Android");
            Directory.CreateDirectory(Qa);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(File.ReadAllBytes(Root + "/Sources/rail_symbol_candidate_generated.png")))
                throw new InvalidOperationException("Cannot decode branding source.");
            source.wrapMode = TextureWrapMode.Clamp;
            var pixels = source.GetPixels32();
            var min = new Vector2(source.width, source.height);
            var max = Vector2.zero;
            for (var y = 0; y < source.height; y++)
            for (var x = 0; x < source.width; x++)
            {
                if (pixels[y * source.width + x].a == 0) continue;
                min = Vector2.Min(min, new Vector2(x, y));
                max = Vector2.Max(max, new Vector2(x, y));
            }
            if (min.x >= max.x || min.y >= max.y) throw new InvalidOperationException("Empty symbol.");
            var center = (min + max) * 0.5f;
            var radius = 0f;
            for (var y = 0; y < source.height; y++)
            for (var x = 0; x < source.width; x++)
                if (pixels[y * source.width + x].a != 0)
                    radius = Mathf.Max(radius, Vector2.Distance(center, new Vector2(x, y)) + 2f);

            var exports = new List<string>();
            foreach (var kind in new[] { AndroidPlatformIconKind.Legacy, AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Adaptive })
            {
                var adaptive = kind == AndroidPlatformIconKind.Adaptive;
                var round = kind == AndroidPlatformIconKind.Round;
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                if (icons.Length == 0) throw new InvalidOperationException("Android icon slots unavailable.");
                foreach (var icon in icons)
                {
                    var size = icon.width;
                    var stem = Root + "/Android/";
                    if (adaptive)
                    {
                        var bg = stem + "adaptive_background_" + size + ".png";
                        var fg = stem + "adaptive_foreground_" + size + ".png";
                        Export(bg, Render(source, center, radius, size, 0.27f, true, false, false));
                        Export(fg, Render(source, center, radius, size, 0.27f, false, true, false));
                        // Unity 6000.3 Android customLayerLabels: Background, Foreground.
                        icon.SetTextures(new[] { Import(bg), Import(fg) });
                        exports.Add(bg); exports.Add(fg);
                    }
                    else
                    {
                        var path = stem + (round ? "round_" : "legacy_") + size + ".png";
                        Export(path, Render(source, center, radius, size, 0.43f, true, true, round));
                        icon.SetTexture(Import(path));
                        exports.Add(path);
                    }
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
            }
            var splash = Root + "/splash_logo_candidate.png";
            Export(splash, Render(source, center, radius, 512, 0.43f, false, true, false));
            Import(splash, true);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(splash);
            var logos = new List<PlayerSettings.SplashScreenLogo>();
            foreach (var existing in PlayerSettings.SplashScreen.logos)
                if (existing.logo != logo) logos.Add(existing);
            logos.Add(PlayerSettings.SplashScreenLogo.Create(2f, logo));
            PlayerSettings.SplashScreen.logos = logos.ToArray();
            AssetDatabase.SaveAssets();
            // These are deterministic asset-mask previews, not OS launcher screenshots.
            foreach (var size in new[] { 48, 72, 96, 256 })
            {
                Export(Qa + "/circle_" + size + ".png", Render(source, center, radius, size, 0.405f, true, true, true));
                Export(Qa + "/square_" + size + ".png", Render(source, center, radius, size, 0.405f, true, true, false, true));
            }
            File.WriteAllText(Qa + "/exports.json", JsonUtility.ToJson(new ExportReport { unityVersion = Application.unityVersion, textures = exports.ToArray(), splash = splash }, true));
            UnityEngine.Object.DestroyImmediate(source);
            Debug.Log("Android branding generated and bound: " + exports.Count + " icon textures; one splash logo. Unity branding flags preserved.");
        }

        private static Texture2D Render(Texture2D source, Vector2 center, float radius, int size, float fit, bool background, bool symbol, bool circle, bool rounded = false)
        {
            var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var colors = new Color[size * size];
            var scale = size * fit / radius;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
                var c = background ? Background : Color.clear;
                if (symbol)
                {
                    var sample = center + p * size / scale;
                    var fg = sample.x < 0 || sample.y < 0 || sample.x >= source.width || sample.y >= source.height
                        ? Color.clear : source.GetPixelBilinear((sample.x + 0.5f) / source.width, (sample.y + 0.5f) / source.height);
                    c = background ? new Color(fg.r * fg.a + c.r * (1 - fg.a), fg.g * fg.a + c.g * (1 - fg.a), fg.b * fg.a + c.b * (1 - fg.a), 1) : fg;
                }
                if (circle && p.magnitude > 0.5f) c = Color.clear;
                if (rounded && new Vector2(Mathf.Max(Mathf.Abs(p.x) - 0.34f, 0), Mathf.Max(Mathf.Abs(p.y) - 0.34f, 0)).magnitude > 0.16f) c = Color.clear;
                colors[y * size + x] = c;
            }
            output.SetPixels(colors);
            output.Apply();
            return output;
        }

        private static void Export(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static Texture2D Import(string path, bool sprite = false)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        [Serializable] private sealed class ExportReport
        {
            public string unityVersion;
            public string[] textures;
            public string splash;
        }
    }
}
#endif
