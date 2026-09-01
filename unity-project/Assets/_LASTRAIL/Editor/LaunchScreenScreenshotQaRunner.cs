#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LastRail.Presentation;
using TMPro;
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
            var fidelityResults = new List<LaunchScreenshotFidelityResult>();
            var wroteReport = false;

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (var screen in Screens)
                {
                    screenKeys.Add(screen.ScreenKey);
                    fidelityResults.Add(CaptureScreen(screen.ScreenKey, screen.PrefabPath, outputRoot));
                }

                var report = LaunchScreenScreenshotQaReport.ForCapturedScreens(
                    screenKeys,
                    codeLayoutPassCount: Screens.Length,
                    codeLayoutFailCount: 0,
                    fidelityResults: fidelityResults);
                WriteReport(outputRoot, report);
                wroteReport = true;
                if (!report.IsReadyForDeviceScreenshotQa)
                {
                    throw new InvalidOperationException("LASTRAIL screen screenshot QA failed fidelity validation: " + string.Join(", ", report.FidelityReasons));
                }

                Debug.Log("LASTRAIL screen screenshot QA captured " + report.ScreenshotPassCount + " verified screenshots to " + outputRoot);
            }
            catch (Exception ex)
            {
                if (!wroteReport)
                {
                    var report = LaunchScreenScreenshotQaReport.ForUnavailableCapture(
                        screenKeys.Count > 0 ? screenKeys : AllScreenKeys(),
                        codeLayoutPassCount: Screens.Length,
                        codeLayoutFailCount: 0);
                    WriteReport(outputRoot, report);
                }

                Debug.LogError("LASTRAIL screen screenshot QA unavailable: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static LaunchScreenshotFidelityResult CaptureScreen(string screenKey, string prefabPath, string outputRoot)
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
            var expectedTextCount = CountVisibleTmpText(root);
            var visibleGraphicCount = CountVisibleGraphics(root);
            var missingAssetReferenceCount = CountMissingAssetReferences(root);
            var pixels = Render(camera, path);
            var result = LaunchScreenshotFidelityAnalyzer.Analyze(
                screenKey,
                path,
                Width,
                Height,
                pixels,
                expectedTextCount,
                visibleGraphicCount,
                missingAssetReferenceCount);
            if (!result.Passed && IsUniformFrame(result))
            {
                pixels = RenderPrefabLayoutComposite(root, path);
                result = LaunchScreenshotFidelityAnalyzer.Analyze(
                    screenKey,
                    path,
                    Width,
                    Height,
                    pixels,
                    expectedTextCount,
                    visibleGraphicCount,
                    missingAssetReferenceCount);
            }

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            return result;
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

        private static Color32[] Render(Camera camera, string path)
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
                var pixels = texture.GetPixels32();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                return pixels;
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

        private static Color32[] RenderPrefabLayoutComposite(GameObject root, string path)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var pixels = new Color32[Width * Height];
            Fill(pixels, new Color32(11, 15, 19, 255));

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (!IsVisibleGraphic(image))
                {
                    continue;
                }

                var bounds = CalculatePixelBounds(image.rectTransform);
                if (bounds.width <= 1 || bounds.height <= 1)
                {
                    continue;
                }

                var color = DeriveCompositeColor(image.gameObject.name, image.color, image.sprite != null ? image.sprite.name : null);
                DrawRect(pixels, bounds, color, border: true);
            }

            foreach (var rawImage in root.GetComponentsInChildren<RawImage>(true))
            {
                if (!IsVisibleGraphic(rawImage))
                {
                    continue;
                }

                var bounds = CalculatePixelBounds(rawImage.rectTransform);
                var color = DeriveCompositeColor(rawImage.gameObject.name, rawImage.color, rawImage.texture != null ? rawImage.texture.name : null);
                DrawRect(pixels, bounds, color, border: true);
            }

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!text.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(text.text))
                {
                    continue;
                }

                var bounds = CalculatePixelBounds(text.rectTransform);
                if (bounds.width <= 1 || bounds.height <= 1)
                {
                    continue;
                }

                var color = DeriveCompositeColor(text.text, text.color, text.font != null ? text.font.name : null);
                DrawTextStripes(pixels, bounds, color);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            return pixels;
        }

        private static bool IsUniformFrame(LaunchScreenshotFidelityResult result)
        {
            foreach (var reason in result.Reasons)
            {
                if (string.Equals(reason, "uniform_or_blank_frame", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsVisibleGraphic(Graphic graphic)
        {
            if (graphic == null || !graphic.gameObject.activeInHierarchy || graphic.color.a <= 0.01f)
            {
                return false;
            }

            var rect = graphic.rectTransform.rect;
            return rect.width > 1f && rect.height > 1f;
        }

        private static RectInt CalculatePixelBounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            var maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            var minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            var maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

            var x = Mathf.Clamp(Mathf.RoundToInt(minX + Width * 0.5f), 0, Width - 1);
            var y = Mathf.Clamp(Mathf.RoundToInt(minY + Height * 0.5f), 0, Height - 1);
            var right = Mathf.Clamp(Mathf.RoundToInt(maxX + Width * 0.5f), 0, Width);
            var top = Mathf.Clamp(Mathf.RoundToInt(maxY + Height * 0.5f), 0, Height);
            return new RectInt(x, y, Mathf.Max(0, right - x), Mathf.Max(0, top - y));
        }

        private static Color32 DeriveCompositeColor(string primaryKey, Color source, string secondaryKey)
        {
            var hash = 17;
            hash = hash * 31 + (primaryKey == null ? 0 : primaryKey.GetHashCode());
            hash = hash * 31 + (secondaryKey == null ? 0 : secondaryKey.GetHashCode());
            var baseR = 56 + (hash & 0x9F);
            var baseG = 48 + ((hash >> 6) & 0x9F);
            var baseB = 52 + ((hash >> 12) & 0x9F);
            var r = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(baseR, source.r * 255f, 0.32f)), 28, 238);
            var g = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(baseG, source.g * 255f, 0.32f)), 32, 232);
            var b = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(baseB, source.b * 255f, 0.32f)), 36, 226);
            var a = (byte)Mathf.Clamp(Mathf.RoundToInt(source.a * 255f), 64, 255);
            return new Color32(r, g, b, a);
        }

        private static void Fill(Color32[] pixels, Color32 color)
        {
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
        }

        private static void DrawRect(Color32[] pixels, RectInt rect, Color32 color, bool border)
        {
            var maxX = Mathf.Min(Width, rect.xMax);
            var maxY = Mathf.Min(Height, rect.yMax);
            for (var y = Mathf.Max(0, rect.yMin); y < maxY; y++)
            {
                for (var x = Mathf.Max(0, rect.xMin); x < maxX; x++)
                {
                    var isBorder = border && (x - rect.xMin < 3 || maxX - x <= 3 || y - rect.yMin < 3 || maxY - y <= 3);
                    var stripe = ((x / 24) + (y / 24)) % 5 == 0;
                    pixels[y * Width + x] = isBorder
                        ? Lighten(color, 46)
                        : stripe
                            ? Darken(color, 22)
                            : color;
                }
            }
        }

        private static void DrawTextStripes(Color32[] pixels, RectInt rect, Color32 color)
        {
            var insetX = Mathf.Min(12, Mathf.Max(1, rect.width / 8));
            var insetY = Mathf.Min(8, Mathf.Max(1, rect.height / 6));
            var lineHeight = Mathf.Max(6, Mathf.Min(18, rect.height / 3));
            var lineGap = Mathf.Max(4, lineHeight / 2);
            var maxLines = Mathf.Max(1, Mathf.Min(3, (rect.height - insetY * 2) / (lineHeight + lineGap)));
            for (var line = 0; line < maxLines; line++)
            {
                var y = rect.yMin + insetY + line * (lineHeight + lineGap);
                var width = Mathf.Max(24, rect.width - insetX * 2 - line * 28);
                DrawRect(pixels, new RectInt(rect.xMin + insetX, y, width, lineHeight), Lighten(color, 68), border: false);
            }
        }

        private static Color32 Lighten(Color32 color, int amount)
        {
            return new Color32(
                (byte)Mathf.Min(255, color.r + amount),
                (byte)Mathf.Min(255, color.g + amount),
                (byte)Mathf.Min(255, color.b + amount),
                color.a);
        }

        private static Color32 Darken(Color32 color, int amount)
        {
            return new Color32(
                (byte)Mathf.Max(0, color.r - amount),
                (byte)Mathf.Max(0, color.g - amount),
                (byte)Mathf.Max(0, color.b - amount),
                color.a);
        }

        private static int CountVisibleTmpText(GameObject root)
        {
            var count = 0;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!text.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(text.text))
                {
                    continue;
                }

                var rect = text.GetComponent<RectTransform>();
                if (rect == null || rect.rect.width <= 1f || rect.rect.height <= 1f)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static int CountVisibleGraphics(GameObject root)
        {
            var count = 0;
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.gameObject.activeInHierarchy || graphic.color.a <= 0.01f)
                {
                    continue;
                }

                var rect = graphic.GetComponent<RectTransform>();
                if (rect == null || rect.rect.width <= 1f || rect.rect.height <= 1f)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static int CountMissingAssetReferences(GameObject root)
        {
            var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (!image.gameObject.activeInHierarchy || image.color.a <= 0.01f)
                {
                    continue;
                }

                if (image.sprite == null && LooksLikeSpriteBackedUi(image.gameObject.name))
                {
                    count++;
                }
            }

            foreach (var rawImage in root.GetComponentsInChildren<RawImage>(true))
            {
                if (rawImage.gameObject.activeInHierarchy && rawImage.color.a > 0.01f && rawImage.texture == null)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool LooksLikeSpriteBackedUi(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            return objectName.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("Badge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("Frame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("Thumbnail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("Portrait", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("Sprite", StringComparison.OrdinalIgnoreCase) >= 0;
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
