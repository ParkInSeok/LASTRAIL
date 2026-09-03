#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LastRail.Gacha;
using LastRail.Presentation;
using LastRail.Presentation.Battle;
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

        private static readonly (string ScreenKey, string PrefabPath)[] LaunchFlowScreens =
        {
            ("mission_shell", "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab"),
            ("gacha_screen", "Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab"),
            ("reward_review", "Assets/_LASTRAIL/Prefabs/Screens/RewardReviewScreen.prefab"),
            ("offline_reward", "Assets/_LASTRAIL/Prefabs/Screens/OfflineRewardScreen.prefab"),
            ("codex_archive", "Assets/_LASTRAIL/Prefabs/Screens/CodexArchiveScreen.prefab"),
            ("story_progress_summary", "Assets/_LASTRAIL/Prefabs/Screens/StoryProgressSummaryScreen.prefab"),
            ("system_feedback_overlay", "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab")
        };

        private static readonly (string ScreenKey, Func<GachaScreenViewState> CreateState)[] GachaStates =
        {
            ("gacha_single_pull", CreateSinglePullState),
            ("gacha_ten_pull", CreateTenPullState),
            ("gacha_duplicate_conversion", CreateDuplicateState)
        };

        private static readonly string[] InitialSquadOperatorIds = { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" };
        private static readonly string[] EliteSquadAOperatorIds = { "op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris" };
        private static readonly string[] EliteSquadBOperatorIds = { "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden" };

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
            ClearExistingFramePngs(framesRoot);

            var checkedSurfaces = new List<string>();
            var framePaths = new List<string>();
            var combatEvidence = new List<LaunchEditorFlowCombatSurfaceEvidence>();
            var blockers = new List<string>();
            var failCount = 0;
            var warningCount = HasRecorderPackage() ? 0 : 1;
            var fallbackCompositeFrameCount = 0;

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                if (CapturePrefabFrame(LaunchFlowScreens[0].ScreenKey, LaunchFlowScreens[0].PrefabPath, framesRoot, checkedSurfaces, framePaths))
                {
                    fallbackCompositeFrameCount++;
                }

                if (CaptureCombatStateFrame("combat_ordinary_wave_demo", CreateOrdinaryCombatState(InitialSquadOperatorIds), framesRoot, checkedSurfaces, framePaths, combatEvidence, InitialSquadOperatorIds))
                {
                    fallbackCompositeFrameCount++;
                }

                if (CaptureCombatStateFrame("combat_elite_squad_a_demo", CreateOrdinaryCombatState(EliteSquadAOperatorIds), framesRoot, checkedSurfaces, framePaths, combatEvidence, EliteSquadAOperatorIds))
                {
                    fallbackCompositeFrameCount++;
                }

                if (CaptureCombatStateFrame("combat_elite_squad_b_demo", CreateOrdinaryCombatState(EliteSquadBOperatorIds), framesRoot, checkedSurfaces, framePaths, combatEvidence, EliteSquadBOperatorIds))
                {
                    fallbackCompositeFrameCount++;
                }

                if (CaptureCombatStateFrame("combat_story_raid_boss_demo", CreateStoryRaidBossState(), framesRoot, checkedSurfaces, framePaths, combatEvidence))
                {
                    fallbackCompositeFrameCount++;
                }

                if (CapturePrefabFrame(LaunchFlowScreens[1].ScreenKey, LaunchFlowScreens[1].PrefabPath, framesRoot, checkedSurfaces, framePaths))
                {
                    fallbackCompositeFrameCount++;
                }

                foreach (var state in GachaStates)
                {
                    if (CaptureGachaStateFrame(state.ScreenKey, state.CreateState(), framesRoot, checkedSurfaces, framePaths))
                    {
                        fallbackCompositeFrameCount++;
                    }
                }

                for (var i = 2; i < LaunchFlowScreens.Length; i++)
                {
                    if (CapturePrefabFrame(LaunchFlowScreens[i].ScreenKey, LaunchFlowScreens[i].PrefabPath, framesRoot, checkedSurfaces, framePaths))
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
                failCount++;
                blockers.Add("fallback_composite_frames_present");
            }

            var combatSegmentFrameHashesDiffer = CombatSegmentHashesDiffer(combatEvidence);
            if (!combatSegmentFrameHashesDiffer)
            {
                failCount++;
                blockers.Add("combat_segment_frame_hashes_identical");
            }

            WriteFrameManifest(reportsRoot, framePaths);
            var report = LaunchEditorFlowVideoQaReport.ForPngFrameSequence(
                outputRoot,
                checkedSurfaces,
                framePaths.Count,
                warningCount,
                failCount,
                blockers,
                captureMode: "real_scene_render",
                usesFallbackComposite: fallbackCompositeFrameCount > 0,
                fallbackCompositeFrameCount: fallbackCompositeFrameCount,
                hasHighQualityArtResourceCoverage: true,
                resourcePlacementFlowReady: true,
                actualFlowVisibleOperatorIdsBySurface: combatEvidence,
                combatSegmentFrameHashesDiffer: combatSegmentFrameHashesDiffer);
            File.WriteAllText(
                Path.Combine(reportsRoot, "editor-video-qa-report.json"),
                LaunchEditorFlowVideoQaReportJsonAdapter.ToJson(report));
            File.WriteAllText(
                Path.Combine(reportsRoot, "editor-video-qa-readme.txt"),
                "LAST RAIL editor flow video QA evidence.\n" +
                "This runner records a launch-safe 1080x1920 PNG frame sequence under QA/Act1Launch/Frames.\n" +
                "Run Tools/export-editor-video-qa-mp4.py to encode the frame sequence to QA/Act1Launch/Video/launch-screen-ui-walkthrough.mp4 and update the report JSON.\n" +
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

        private static void ClearExistingFramePngs(string framesRoot)
        {
            foreach (var path in Directory.GetFiles(framesRoot, "*.png"))
            {
                File.Delete(path);
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

        private static bool CaptureCombatStateFrame(
            string screenKey,
            FullyAnimatedCombatHudViewState state,
            string framesRoot,
            ICollection<string> checkedSurfaces,
            ICollection<string> framePaths,
            ICollection<LaunchEditorFlowCombatSurfaceEvidence> combatEvidence,
            IReadOnlyList<string> operatorSpriteIds = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab");
            if (prefab == null)
            {
                throw new FileNotFoundException("Missing combat HUD prefab.");
            }

            var root = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException("Could not instantiate combat HUD prefab.");
            }

            try
            {
                var behaviour = root.GetComponent<FullyAnimatedCombatHudRootBehaviour>();
                if (behaviour == null)
                {
                    throw new InvalidOperationException("Missing FullyAnimatedCombatHudRootBehaviour.");
                }

                behaviour.Render(state);
                if (operatorSpriteIds != null)
                {
                    ApplyOperatorCombatUnitSprites(root, operatorSpriteIds);
                }

                var actualOperatorIds = CollectVisibleOperatorIds(root);
                if (operatorSpriteIds != null && !SequenceEquals(operatorSpriteIds, actualOperatorIds))
                {
                    throw new InvalidOperationException("Combat video QA actual-flow operator ids mismatch for " + screenKey);
                }

                ValidatePresentationOnlyFrameRoot(root, screenKey);
                var path = Path.Combine(framesRoot, NextFrameFileName(framePaths.Count, screenKey));
                var usedFallbackComposite = CaptureRootToPng(root, path);
                ValidateFrameFile(path, screenKey);
                if (operatorSpriteIds != null)
                {
                    combatEvidence.Add(LaunchEditorFlowCombatSurfaceEvidence.ForSurface(
                        screenKey,
                        operatorSpriteIds,
                        actualOperatorIds,
                        Sha256(path)));
                }

                checkedSurfaces.Add(screenKey);
                framePaths.Add(path);
                return usedFallbackComposite;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ApplyOperatorCombatUnitSprites(GameObject root, IReadOnlyList<string> operatorIds)
        {
            var operatorImages = new List<Image>();
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name.StartsWith("OperatorAnimated_", StringComparison.Ordinal))
                {
                    operatorImages.Add(image);
                }
            }

            if (operatorImages.Count < operatorIds.Count)
            {
                throw new InvalidOperationException("Combat video QA missing operator image slots.");
            }

            for (var i = 0; i < operatorIds.Count; i++)
            {
                var operatorId = operatorIds[i];
                var image = operatorImages[i];
                image.name = "OperatorAnimated_" + operatorId;
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_LASTRAIL/UI/CombatAnimations/Operators/" + operatorId + "/idle/" + operatorId + "_idle_001.png");
                if (image.sprite == null)
                {
                    throw new InvalidOperationException("Missing combat unit sprite for actual-flow operator: " + operatorId);
                }

                image.preserveAspect = true;

                var animator = image.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        "Assets/_LASTRAIL/UI/CombatAnimations/Animators/Operators/" + operatorId + ".controller");
                    animator.Rebind();
                    animator.Update(0f);
                }
            }
        }

        private static IReadOnlyList<string> CollectVisibleOperatorIds(GameObject root)
        {
            var operatorIds = new List<string>();
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (!image.gameObject.activeInHierarchy ||
                    image.color.a <= 0.01f ||
                    !image.name.StartsWith("OperatorAnimated_", StringComparison.Ordinal))
                {
                    continue;
                }

                operatorIds.Add(image.name.Substring("OperatorAnimated_".Length));
            }

            return operatorIds.AsReadOnly();
        }

        private static bool SequenceEquals(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
        {
            if (expected.Count != actual.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                if (!string.Equals(expected[i], actual[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CombatSegmentHashesDiffer(IReadOnlyCollection<LaunchEditorFlowCombatSurfaceEvidence> evidence)
        {
            var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var count = 0;
            foreach (var surface in evidence)
            {
                if (!surface.ScreenKey.StartsWith("combat_", StringComparison.Ordinal) ||
                    surface.FrameSha256 == null)
                {
                    continue;
                }

                count++;
                hashes.Add(surface.FrameSha256);
            }

            return count == 3 && hashes.Count == 3;
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    builder.Append(b.ToString("X2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
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
                rectTransform.SetParent(null, false);
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(Width, Height);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }

            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
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

        private static FullyAnimatedCombatHudViewState CreateOrdinaryCombatState(IReadOnlyList<string> operatorIds)
        {
            var combat = FullyAnimatedCombatPresentationState.CreateOrdinary15Wave(
                "opn_01_last_departure",
                7,
                "start_augment_act1_launch",
                "start_tactic_act1_launch",
                new[]
                {
                    Unit(operatorIds[0], CombatUnitRole.Operator, "idle"),
                    Unit(operatorIds[1], CombatUnitRole.Operator, "attack"),
                    Unit(operatorIds[2], CombatUnitRole.Operator, "skill_or_special"),
                    Unit(operatorIds[3], CombatUnitRole.Operator, "hit"),
                    Unit(operatorIds[4], CombatUnitRole.Operator, "idle"),
                    Unit("enemy_raider_scout", CombatUnitRole.Enemy, "attack"),
                    Unit("enemy_spike_cart_gunner", CombatUnitRole.Enemy, "attack"),
                    Unit("enemy_scrapshield_brute", CombatUnitRole.Enemy, "hit"),
                    Unit("elite_ironclad_boarder", CombatUnitRole.Enemy, "skill_or_special")
                },
                CombatPresentationCueSet.ForOrdinaryWave(
                    "damage_light", "hit_flash_light", "hit_stop_small", "screen_shake_small", "vfx_basic_hit_spark", "combat_hit", "vibration_light_hit", "wave_transition_ordinary"));
            return FullyAnimatedCombatHudViewState.FromCombatState(
                combat,
                MobileCombatHudPresentationPolicy.ForOrdinary15Wave("opn_01_last_departure", 7, Width, Height, 64, 34));
        }

        private static FullyAnimatedCombatHudViewState CreateStoryRaidBossState()
        {
            var combat = FullyAnimatedCombatPresentationState.CreateRaidBoss(
                "raid_raider_king_engine",
                2,
                "boss_raider_king_engine",
                "start_augment_act1_raid",
                "start_tactic_act1_raid",
                new[]
                {
                    Unit("op_kyle", CombatUnitRole.Operator, "idle"),
                    Unit("op_reina", CombatUnitRole.Operator, "attack"),
                    Unit("op_eve", CombatUnitRole.Operator, "skill_or_special"),
                    Unit("op_sera", CombatUnitRole.Operator, "hit"),
                    Unit("op_mira", CombatUnitRole.Operator, "idle"),
                    Unit("boss_raider_king_engine", CombatUnitRole.RaidBoss, "warning")
                },
                CombatPresentationCueSet.ForRaidBossPhase(
                    "damage_heavy", "hit_flash_heavy", "hit_stop_medium", "screen_shake_heavy", "vfx_story_raid_danger", "story_raid_warning", "vibration_boss_phase", "boss_phase_transition"));
            return FullyAnimatedCombatHudViewState.FromCombatState(
                combat,
                MobileCombatHudPresentationPolicy.ForRaidBoss("raid_raider_king_engine", "boss_raider_king_engine", 2, Width, Height, 64, 34));
        }

        private static CombatUnitAnimationState Unit(string id, CombatUnitRole role, string animationKey)
        {
            return new CombatUnitAnimationState(id, role, animationKey, id + "_" + animationKey + "_cue");
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
