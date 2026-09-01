#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LastRail.Gacha;
using LastRail.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchEditorFlowVideoQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const string QaRootRelativePath = "QA/Act1Launch";
        private const string FramesRelativePath = "Frames/EditorFlowVideoQa";
        private const string ReportsRelativePath = "Reports";

        private static readonly (string ScreenKey, string PrefabPath)[] LaunchScreens =
        {
            ("mission_shell", "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab"),
            ("gacha_screen", "Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab"),
            ("fully_animated_combat_hud", "Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab"),
            ("codex_archive", "Assets/_LASTRAIL/Prefabs/Screens/CodexArchiveScreen.prefab"),
            ("reward_review", "Assets/_LASTRAIL/Prefabs/Screens/RewardReviewScreen.prefab"),
            ("offline_reward", "Assets/_LASTRAIL/Prefabs/Screens/OfflineRewardScreen.prefab"),
            ("story_progress_summary", "Assets/_LASTRAIL/Prefabs/Screens/StoryProgressSummaryScreen.prefab")
        };

        private static readonly (string ScreenKey, Func<GachaScreenViewState> CreateState)[] GachaStates =
        {
            ("gacha_single_pull", CreateSinglePullState),
            ("gacha_ten_pull", CreateTenPullState),
            ("gacha_duplicate_conversion", CreateDuplicateState)
        };

        private static readonly (string ScreenKey, string PrefabPath)[] ShowcaseScreens =
        {
            ("system_feedback_showcase", "Assets/_LASTRAIL/UI/SystemFeedbackResources/Showcase/LRLaunchSystemFeedbackResourceShowcase.prefab"),
            ("operator_card_polish_showcase", "Assets/_LASTRAIL/UI/Operators/CardPolish/Showcase/LRLaunchOperatorCardPolishShowcase.prefab"),
            ("audio_showcase", "Assets/_LASTRAIL/Audio/Showcase/LRLaunchAudioResourceShowcase.prefab")
        };

        [MenuItem("LASTRAIL/Launch/Capture Editor Flow Video QA")]
        public static void CaptureEditorFlowVideoQa()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var outputRoot = Path.Combine(projectRoot, QaRootRelativePath);
            var framesRoot = Path.Combine(outputRoot, FramesRelativePath);
            var reportsRoot = Path.Combine(outputRoot, ReportsRelativePath);
            Directory.CreateDirectory(outputRoot);
            Directory.CreateDirectory(framesRoot);
            Directory.CreateDirectory(reportsRoot);

            var checkedSurfaces = new List<string>();
            var framePaths = new List<string>();
            var blockers = new List<string>();
            var failCount = 0;
            var warningCount = HasRecorderPackage() ? 0 : 1;
            var fallbackCompositeFrameCount = 0;

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                foreach (var screen in LaunchScreens)
                {
                    if (CapturePrefabFrame(screen.ScreenKey, screen.PrefabPath, framesRoot, checkedSurfaces, framePaths))
                    {
                        fallbackCompositeFrameCount++;
                    }
                }

                foreach (var state in GachaStates)
                {
                    if (CaptureGachaStateFrame(state.ScreenKey, state.CreateState(), framesRoot, checkedSurfaces, framePaths))
                    {
                        fallbackCompositeFrameCount++;
                    }
                }

                foreach (var showcase in ShowcaseScreens)
                {
                    if (CapturePrefabFrame(showcase.ScreenKey, showcase.PrefabPath, framesRoot, checkedSurfaces, framePaths))
                    {
                        fallbackCompositeFrameCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                failCount = 1;
                blockers.Add("editor_flow_video_qa_capture_failed");
                Debug.LogError("LASTRAIL editor flow video QA failed: " + ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            if (fallbackCompositeFrameCount > 0)
            {
                warningCount++;
                blockers.Add("fallback_composite_frames_present");
            }

            WriteFrameManifest(reportsRoot, framePaths);
            var report = LaunchEditorFlowVideoQaReport.ForPngFrameSequence(
                outputRoot,
                checkedSurfaces,
                framePaths.Count,
                warningCount,
                failCount,
                blockers,
                captureMode: "launch_screen_walkthrough",
                usesFallbackComposite: fallbackCompositeFrameCount > 0,
                fallbackCompositeFrameCount: fallbackCompositeFrameCount,
                hasHighQualityArtResourceCoverage: false);
            File.WriteAllText(
                Path.Combine(reportsRoot, "editor-video-qa-report.json"),
                LaunchEditorFlowVideoQaReportJsonAdapter.ToJson(report));
            File.WriteAllText(
                Path.Combine(reportsRoot, "editor-video-qa-readme.txt"),
                "LAST RAIL editor flow video QA evidence.\n" +
                "This runner records a launch-safe 1080x1920 PNG frame sequence under QA/Act1Launch/Frames.\n" +
                "Run Tools/export-editor-video-qa-mp4.py to encode the frame sequence to QA/Act1Launch/Video/editor-flow-video-qa.mp4 and update the report JSON.\n" +
                "No sensitive auth, server, gameplay, or economy payload is written.\n");

            if (failCount > 0)
            {
                throw new InvalidOperationException("LASTRAIL editor flow video QA failed. See " + outputRoot);
            }

            Debug.Log("LASTRAIL editor flow video QA captured " + framePaths.Count.ToString(CultureInfo.InvariantCulture) + " frames to " + outputRoot);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static bool CapturePrefabFrame(
            string screenKey,
            string prefabPath,
            string framesRoot,
            ICollection<string> checkedSurfaces,
            ICollection<string> framePaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new FileNotFoundException("Missing editor video QA prefab: " + prefabPath);
            }

            var root = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException("Could not instantiate editor video QA prefab: " + prefabPath);
            }

            try
            {
                ValidatePresentationOnlyFrameRoot(root, screenKey);
                var path = Path.Combine(framesRoot, NextFrameFileName(framePaths.Count, screenKey));
                var usedFallbackComposite = CaptureRootToPng(root, path);
                ValidateFrameFile(path, screenKey);
                checkedSurfaces.Add(screenKey);
                framePaths.Add(path);
                return usedFallbackComposite;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static bool CaptureGachaStateFrame(
            string screenKey,
            GachaScreenViewState state,
            string framesRoot,
            ICollection<string> checkedSurfaces,
            ICollection<string> framePaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab");
            if (prefab == null)
            {
                throw new FileNotFoundException("Missing gacha screen prefab.");
            }

            var root = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException("Could not instantiate gacha screen prefab.");
            }

            try
            {
                var behaviour = root.GetComponent<GachaScreenRootBehaviour>();
                if (behaviour == null)
                {
                    throw new InvalidOperationException("Missing GachaScreenRootBehaviour.");
                }

                behaviour.Render(state);
                ValidatePresentationOnlyFrameRoot(root, screenKey);
                var path = Path.Combine(framesRoot, NextFrameFileName(framePaths.Count, screenKey));
                var usedFallbackComposite = CaptureRootToPng(root, path);
                ValidateFrameFile(path, screenKey);
                checkedSurfaces.Add(screenKey);
                framePaths.Add(path);
                return usedFallbackComposite;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static bool CaptureRootToPng(GameObject root, string outputPath)
        {
            var cameraObject = new GameObject(root.name + "_EditorVideoQaCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.06f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = Height * 0.5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            try
            {
                PrepareCanvasForCamera(root, camera);
                var pixels = Render(camera, outputPath);
                var usedFallbackComposite = false;
                if (!PixelsLookNonBlank(pixels))
                {
                    pixels = RenderPrefabLayoutComposite(root, outputPath);
                    usedFallbackComposite = true;
                }

                if (!PixelsLookNonBlank(pixels))
                {
                    throw new InvalidOperationException("Editor video QA frame appears blank or uniformly dark.");
                }

                return usedFallbackComposite;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
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
                target.Release();
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Color32[] RenderPrefabLayoutComposite(GameObject root, string path)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var pixels = new Color32[Width * Height];
            Fill(pixels, new Color32(10, 14, 18, 255));

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

                DrawRect(pixels, bounds, DeriveCompositeColor(image.gameObject.name, image.color, image.sprite != null ? image.sprite.name : null), true);
            }

            foreach (var rawImage in root.GetComponentsInChildren<RawImage>(true))
            {
                if (!IsVisibleGraphic(rawImage))
                {
                    continue;
                }

                var bounds = CalculatePixelBounds(rawImage.rectTransform);
                if (bounds.width <= 1 || bounds.height <= 1)
                {
                    continue;
                }

                DrawRect(pixels, bounds, DeriveCompositeColor(rawImage.gameObject.name, rawImage.color, rawImage.texture != null ? rawImage.texture.name : null), true);
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

                DrawTextStripes(pixels, bounds, DeriveCompositeColor(text.text, text.color, text.font != null ? text.font.name : null));
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            return pixels;
        }

        private static void ValidatePresentationOnlyFrameRoot(GameObject root, string screenKey)
        {
            if (root.GetComponentsInChildren<TMP_Text>(true).Length == 0)
            {
                throw new InvalidOperationException("Editor video QA frame has no TMP text: " + screenKey);
            }

            if (root.GetComponentsInChildren<Text>(true).Length > 0)
            {
                throw new InvalidOperationException("Editor video QA frame contains legacy Text: " + screenKey);
            }

            if (root.GetComponentsInChildren<StandaloneInputModule>(true).Length > 0)
            {
                throw new InvalidOperationException("Editor video QA frame contains StandaloneInputModule: " + screenKey);
            }

            foreach (var inputSystemModule in root.GetComponentsInChildren<InputSystemUIInputModule>(true))
            {
                if (!inputSystemModule.gameObject.activeInHierarchy)
                {
                    throw new InvalidOperationException("Inactive InputSystemUIInputModule in frame: " + screenKey);
                }
            }

            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root) > 0)
            {
                throw new InvalidOperationException("Editor video QA frame has missing script references: " + screenKey);
            }

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (!image.gameObject.activeInHierarchy || image.color.a <= 0.01f)
                {
                    continue;
                }

                if (LooksLikeSpriteBackedUi(image.gameObject.name) && image.sprite == null)
                {
                    throw new InvalidOperationException("Editor video QA frame has missing sprite reference: " + screenKey + "/" + image.gameObject.name);
                }
            }
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

        private static bool PixelsLookNonBlank(Color32[] pixels)
        {
            if (pixels == null || pixels.Length != Width * Height)
            {
                return false;
            }

            var first = pixels[0];
            var distinct = 0;
            var nonDark = 0;
            for (var i = 0; i < pixels.Length; i += 97)
            {
                var p = pixels[i];
                if (Math.Abs(p.r - first.r) > 6 || Math.Abs(p.g - first.g) > 6 || Math.Abs(p.b - first.b) > 6)
                {
                    distinct++;
                }

                if (p.r + p.g + p.b > 120)
                {
                    nonDark++;
                }
            }

            return distinct >= 16 && nonDark >= 8;
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
            var baseR = 48 + (hash & 0x9F);
            var baseG = 50 + ((hash >> 6) & 0x9F);
            var baseB = 54 + ((hash >> 12) & 0x9F);
            var r = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(baseR, source.r * 255f, 0.35f)), 28, 238);
            var g = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(baseG, source.g * 255f, 0.35f)), 32, 232);
            var b = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(baseB, source.b * 255f, 0.35f)), 36, 226);
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
                    pixels[y * Width + x] = isBorder ? Lighten(color, 48) : stripe ? Darken(color, 20) : color;
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
                DrawRect(pixels, new RectInt(rect.xMin + insetX, y, width, lineHeight), Lighten(color, 70), false);
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

        private static void ValidateFrameFile(string path, string screenKey)
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < 20000)
            {
                throw new InvalidOperationException("Editor video QA frame is empty or too small: " + screenKey);
            }
        }

        private static string NextFrameFileName(int index, string screenKey)
        {
            return "frame_" + (index + 1).ToString("000", CultureInfo.InvariantCulture) + "_" + screenKey + "_1080x1920.png";
        }

        private static void WriteFrameManifest(string outputRoot, IEnumerable<string> framePaths)
        {
            File.WriteAllLines(Path.Combine(outputRoot, "frame-sequence.txt"), framePaths);
        }

        private static bool HasRecorderPackage()
        {
            var manifestPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Packages", "manifest.json");
            return File.Exists(manifestPath) &&
                File.ReadAllText(manifestPath).IndexOf("com.unity.recorder", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static GachaScreenViewState CreateSinglePullState()
        {
            return new GachaScreenViewState(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(Response(1, false))),
                "Act 1 구조 신호"));
        }

        private static GachaScreenViewState CreateTenPullState()
        {
            return new GachaScreenViewState(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(Response(10, true))),
                "Act 1 구조 신호"));
        }

        private static GachaScreenViewState CreateDuplicateState()
        {
            return new GachaScreenViewState(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.DuplicateReplay(Response(10, true, GachaPullResultStatus.DuplicateReplay, false, "duplicate_replay"))),
                "Act 1 구조 신호"));
        }

        private static GachaPullResponseDto Response(
            int pullCount,
            bool includeDuplicate,
            GachaPullResultStatus status = GachaPullResultStatus.Accepted,
            bool accepted = true,
            string errorCode = null)
        {
            return new GachaPullResponseDto(
                "banner_act1_launch",
                accepted,
                "batch_video_qa_" + pullCount.ToString(CultureInfo.InvariantCulture),
                status,
                GachaPullCount.FromValue(pullCount),
                "1",
                new GachaPullCostDto("cur_command_ticket", pullCount),
                new GachaPostPityStateDto(42, 12, 22, 2, 4),
                Results(pullCount, includeDuplicate),
                new DateTime(2026, 9, 1, 1, 0, 0, DateTimeKind.Utc),
                errorCode,
                null);
        }

        private static IReadOnlyList<GachaPullResultDto> Results(int pullCount, bool includeDuplicate)
        {
            var results = new List<GachaPullResultDto>
            {
                new GachaPullResultDto(
                    1,
                    GachaRarity.Elite,
                    GachaPullResultKind.Operator,
                    "result_001",
                    new GachaRewardDto("OPERATOR", "op_vex", 1),
                    true,
                    "PICKUP_ELITE_60",
                    false,
                    null)
            };

            for (var i = 2; i <= pullCount; i++)
            {
                if (includeDuplicate && i == 2)
                {
                    results.Add(new GachaPullResultDto(
                        i,
                        GachaRarity.Elite,
                        GachaPullResultKind.Operator,
                        "result_" + i.ToString("000", CultureInfo.InvariantCulture),
                        new GachaRewardDto("OPERATOR", "op_nyx", 1),
                        false,
                        "ELITE_30",
                        true,
                        new GachaDuplicateConversionDto("duplicate_conversion_002", "cur_command_ticket_fragment", 6)));
                    continue;
                }

                results.Add(new GachaPullResultDto(
                    i,
                    GachaRarity.Rare,
                    GachaPullResultKind.Currency,
                    "result_" + i.ToString("000", CultureInfo.InvariantCulture),
                    new GachaRewardDto("RESOURCE", "cur_tactical_data", 1),
                    false,
                    i == 10 ? "RARE_OR_HIGHER_10" : "NONE",
                    false,
                    null));
            }

            return results.AsReadOnly();
        }
    }
}
#endif
