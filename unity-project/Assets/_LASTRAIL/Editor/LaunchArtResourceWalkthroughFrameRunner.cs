#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
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
    public static class LaunchArtResourceWalkthroughFrameRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const string QaRootRelativePath = "QA/Act1Launch";
        private const string FramesRelativePath = "Frames/ArtResourceWalkthrough";
        private const string ReportsRelativePath = "Reports";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";

        private static readonly string[] BackgroundPaths =
        {
            "Assets/_LASTRAIL/UI/Backgrounds/bg_main_lobby_train_interior.png",
            "Assets/_LASTRAIL/UI/Backgrounds/bg_mission_select_rail_map.png",
            "Assets/_LASTRAIL/UI/Backgrounds/bg_combat_wasteland_rail.png",
            "Assets/_LASTRAIL/UI/KeyVisuals/kv_gacha_recruitment_chamber.png",
            "Assets/_LASTRAIL/UI/Backgrounds/bg_codex_archive_records_room.png",
            "Assets/_LASTRAIL/UI/Backgrounds/bg_reward_result_supply_bay.png",
            "Assets/_LASTRAIL/UI/Backgrounds/bg_offline_reward_settlement.png",
            "Assets/_LASTRAIL/UI/KeyVisuals/kv_story_raid_boss_entrance.png"
        };

        private static readonly string[] OperatorCardPaths =
        {
            "Assets/_LASTRAIL/UI/Operators/Cards/op_vex_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_nyx_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_iris_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_anchor_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_cinder_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_eve_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_kyle_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_lumen_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_marrow_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_mira_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_rail_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_reina_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_sable_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_sera_card.png",
            "Assets/_LASTRAIL/UI/Operators/Cards/op_warden_card.png"
        };

        private static readonly string[] CombatArtPaths =
        {
            "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_cinder_hound_mech.png",
            "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_raider_scout.png",
            "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_railbike_marauder.png",
            "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_scrapshield_brute.png",
            "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_signal_jammer_drone.png",
            "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_spike_cart_gunner.png",
            "Assets/_LASTRAIL/UI/CombatResources/Elites/elite_broadcast_executioner.png",
            "Assets/_LASTRAIL/UI/CombatResources/Elites/elite_ironclad_boarder.png",
            "Assets/_LASTRAIL/UI/CombatResources/Bosses/boss_raider_king_engine.png",
            "Assets/_LASTRAIL/UI/CombatResources/Lanes/combat_train_exterior_advance_lane.png",
            "Assets/_LASTRAIL/UI/CombatResources/Lanes/combat_train_interior_boarding_lane.png"
        };

        private static readonly string[] GachaArtPaths =
        {
            "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_recruitment_aperture_open.png",
            "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_single_pull_reveal_plate.png",
            "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_ten_pull_result_grid_frame.png",
            "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_pickup_elite_glow.png",
            "Assets/_LASTRAIL/UI/GachaResources/Icons/cur_command_ticket.png",
            "Assets/_LASTRAIL/UI/GachaResources/Icons/ui_operator_duplicate_token.png"
        };

        private static readonly string[] MissionThumbnailPaths =
        {
            "Assets/_LASTRAIL/UI/MissionStoryThumbnails/Thumbnails/opn_01_last_departure_thumbnail.png",
            "Assets/_LASTRAIL/UI/MissionStoryThumbnails/Thumbnails/opn_02_first_signal_thumbnail.png",
            "Assets/_LASTRAIL/UI/MissionStoryThumbnails/Thumbnails/opn_03_water_stop_thumbnail.png",
            "Assets/_LASTRAIL/UI/MissionStoryThumbnails/Thumbnails/opn_04_scrapstar_show_thumbnail.png",
            "Assets/_LASTRAIL/UI/MissionStoryThumbnails/Thumbnails/opn_05_night_boarders_thumbnail.png",
            "Assets/_LASTRAIL/UI/MissionStoryThumbnails/Thumbnails/opn_06_broken_line_thumbnail.png",
            "Assets/_LASTRAIL/UI/MissionStoryThumbnails/Thumbnails/raid_raider_king_engine_thumbnail.png"
        };

        private static readonly string[] RewardOfflinePaths =
        {
            "Assets/_LASTRAIL/UI/Backgrounds/bg_reward_result_supply_bay.png",
            "Assets/_LASTRAIL/UI/Backgrounds/bg_offline_reward_settlement.png",
            "Assets/_LASTRAIL/UI/RewardOfflineResources/Frames/reward_result_plate.png",
            "Assets/_LASTRAIL/UI/RewardOfflineResources/Frames/reward_line_item_frame.png",
            "Assets/_LASTRAIL/UI/RewardOfflineResources/Badges/claim_success_badge.png",
            "Assets/_LASTRAIL/UI/RewardOfflineResources/Badges/claim_pending_claimable_accent.png",
            "Assets/_LASTRAIL/UI/RewardOfflineResources/Meters/offline_settlement_meter.png",
            "Assets/_LASTRAIL/UI/RewardOfflineResources/Meters/offline_duration_accumulation_strip.png",
            "Assets/_LASTRAIL/UI/RewardOfflineResources/Empty/offline_empty_state_visual.png",
            "Assets/_LASTRAIL/UI/RewardOfflineResources/Accents/reward_chest_supply_crate_accent.png",
            "Assets/_LASTRAIL/UI/RewardOfflineResources/Accents/reward_material_glow.png"
        };

        [MenuItem("LASTRAIL/Launch/Capture Art Resource Walkthrough Frames")]
        public static void CaptureArtResourceWalkthroughFrames()
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

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CaptureBackgroundFrame("background_key_visuals", BackgroundPaths, framesRoot, checkedSurfaces, framePaths);
                CaptureGridFrame("operator_portrait_cards", OperatorCardPaths, framesRoot, checkedSurfaces, framePaths, 3, 5, 250, 332);
                CaptureGridFrame("combat_enemy_boss_vfx", CombatArtPaths, framesRoot, checkedSurfaces, framePaths, 3, 4, 286, 250);
                CaptureGridFrame("gacha_recruitment_visuals", GachaArtPaths, framesRoot, checkedSurfaces, framePaths, 2, 3, 380, 380);
                CaptureGridFrame("mission_story_thumbnails", MissionThumbnailPaths, framesRoot, checkedSurfaces, framePaths, 1, 7, 910, 512);
                CaptureGridFrame("reward_offline_visuals", RewardOfflinePaths, framesRoot, checkedSurfaces, framePaths, 2, 6, 440, 270);
            }
            catch (Exception ex)
            {
                failCount = 1;
                blockers.Add("art_resource_walkthrough_capture_failed");
                Debug.LogError("LASTRAIL art resource walkthrough failed: " + ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            File.WriteAllLines(Path.Combine(reportsRoot, "art-resource-walkthrough-frame-sequence.txt"), framePaths);
            var report = LaunchEditorFlowVideoQaReport.ForPngFrameSequence(
                outputRoot,
                checkedSurfaces,
                framePaths.Count,
                warningCount: 0,
                failCount: failCount,
                blockers: blockers,
                captureMode: "resource_showcase_render",
                usesFallbackComposite: false,
                fallbackCompositeFrameCount: 0,
                hasHighQualityArtResourceCoverage: true);
            File.WriteAllText(
                Path.Combine(reportsRoot, "art-resource-walkthrough-render-report.json"),
                LaunchEditorFlowVideoQaReportJsonAdapter.ToJson(report));

            if (failCount > 0)
            {
                throw new InvalidOperationException("LASTRAIL art resource walkthrough failed. See " + outputRoot);
            }

            Debug.Log("LASTRAIL art resource walkthrough captured " + framePaths.Count.ToString(CultureInfo.InvariantCulture) + " Unity-rendered frames to " + outputRoot);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void CaptureBackgroundFrame(
            string surfaceKey,
            IReadOnlyList<string> assetPaths,
            string framesRoot,
            ICollection<string> checkedSurfaces,
            ICollection<string> framePaths)
        {
            var root = CreateCanvasRoot(surfaceKey);
            try
            {
                AddFullscreenImage(root.transform, LoadTexture(assetPaths[0]));
                for (var i = 1; i < assetPaths.Count; i++)
                {
                    var x = -350 + (i - 1) % 2 * 700;
                    var y = 570 - (i - 1) / 2 * 300;
                    AddImage(root.transform, LoadTexture(assetPaths[i]), x, y, 310, 552, 0.92f);
                }

                AddHeader(root.transform, "LAST RAIL launch key visuals", surfaceKey);
                CaptureCanvasRoot(root, surfaceKey, framesRoot, checkedSurfaces, framePaths);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CaptureGridFrame(
            string surfaceKey,
            IReadOnlyList<string> assetPaths,
            string framesRoot,
            ICollection<string> checkedSurfaces,
            ICollection<string> framePaths,
            int columns,
            int rows,
            int cellWidth,
            int cellHeight)
        {
            var root = CreateCanvasRoot(surfaceKey);
            try
            {
                AddPanel(root.transform, 0, 0, 1000, 1770, new Color32(15, 22, 27, 235));
                AddHeader(root.transform, "LAST RAIL launch art resources", surfaceKey);

                var startX = -((columns - 1) * cellWidth) * 0.5f;
                var startY = 560f;
                for (var i = 0; i < assetPaths.Count; i++)
                {
                    var column = i % columns;
                    var row = i / columns;
                    var x = startX + column * cellWidth;
                    var y = startY - row * cellHeight;
                    AddPanel(root.transform, x, y, cellWidth - 18, cellHeight - 18, new Color32(30, 38, 44, 210));
                    AddImage(root.transform, LoadTexture(assetPaths[i]), x, y + 8, cellWidth - 34, cellHeight - 54, 1f);
                    AddSmallLabel(root.transform, ShortName(assetPaths[i]), x, y - cellHeight * 0.5f + 22, cellWidth - 40, 32);
                }

                CaptureCanvasRoot(root, surfaceKey, framesRoot, checkedSurfaces, framePaths);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateCanvasRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Width, Height);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.pixelPerfect = false;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.matchWidthOrHeight = 1f;
            return root;
        }

        private static void AddFullscreenImage(Transform parent, Texture2D texture)
        {
            AddImage(parent, texture, 0, 0, Width, Height, 1f);
            AddPanel(parent, 0, -620, 1000, 520, new Color32(6, 8, 10, 135));
        }

        private static void AddImage(Transform parent, Texture2D texture, float x, float y, float width, float height, float alpha)
        {
            var go = new GameObject(texture.name + "_Image", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            var rawImage = go.GetComponent<RawImage>();
            rawImage.texture = texture;
            rawImage.color = new Color(1f, 1f, 1f, alpha);
        }

        private static void AddPanel(Transform parent, float x, float y, float width, float height, Color32 color)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            go.GetComponent<Image>().color = color;
        }

        private static void AddHeader(Transform parent, string title, string subtitle)
        {
            AddText(parent, title, 0, 820, 920, 58, 42, FontStyles.Bold, new Color32(237, 242, 235, 255));
            AddText(parent, subtitle, 0, 766, 920, 42, 27, FontStyles.Normal, new Color32(174, 205, 214, 255));
        }

        private static void AddSmallLabel(Transform parent, string text, float x, float y, float width, float height)
        {
            AddText(parent, text, x, y, width, height, 20, FontStyles.Normal, new Color32(220, 226, 220, 255));
        }

        private static void AddText(Transform parent, string value, float x, float y, float width, float height, float fontSize, FontStyles style, Color32 color)
        {
            var go = new GameObject("TmpLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(12f, fontSize * 0.55f);
            text.fontSizeMax = fontSize;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static void CaptureCanvasRoot(
            GameObject root,
            string surfaceKey,
            string framesRoot,
            ICollection<string> checkedSurfaces,
            ICollection<string> framePaths)
        {
            var path = Path.Combine(framesRoot, "frame_" + (framePaths.Count + 1).ToString("000", CultureInfo.InvariantCulture) + "_" + surfaceKey + "_1080x1920.png");
            var cameraObject = new GameObject(surfaceKey + "_ResourceQaCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.04f, 0.045f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = Height * 0.5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            try
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    canvas.worldCamera = camera;
                }

                Canvas.ForceUpdateCanvases();
                var pixels = Render(camera, path);
                ValidateHighQualityResourceFrame(pixels, surfaceKey);
                checkedSurfaces.Add(surfaceKey);
                framePaths.Add(path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
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

        private static void ValidateHighQualityResourceFrame(Color32[] pixels, string surfaceKey)
        {
            if (pixels == null || pixels.Length != Width * Height)
            {
                throw new InvalidOperationException("Missing rendered pixels for art resource frame: " + surfaceKey);
            }

            var first = pixels[0];
            var distinct = 0;
            var nonDark = 0;
            var nonGray = 0;
            for (var i = 0; i < pixels.Length; i += 67)
            {
                var p = pixels[i];
                if (Math.Abs(p.r - first.r) > 8 || Math.Abs(p.g - first.g) > 8 || Math.Abs(p.b - first.b) > 8)
                {
                    distinct++;
                }

                if (p.r + p.g + p.b > 150)
                {
                    nonDark++;
                }

                var max = Math.Max(p.r, Math.Max(p.g, p.b));
                var min = Math.Min(p.r, Math.Min(p.g, p.b));
                if (max - min > 10)
                {
                    nonGray++;
                }
            }

            if (distinct < 80 || nonDark < 30 || nonGray < 60)
            {
                throw new InvalidOperationException("Art resource frame appears blank, gray, or low-fidelity: " + surfaceKey);
            }
        }

        private static Texture2D LoadTexture(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Invalid Unity asset path: " + assetPath, nameof(assetPath));
            }

            var absolutePath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Missing art resource texture: " + assetPath);
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Could not load art resource texture: " + assetPath);
            }

            texture.name = Path.GetFileNameWithoutExtension(assetPath);
            return texture;
        }

        private static string ShortName(string assetPath)
        {
            return Path.GetFileNameWithoutExtension(assetPath).Replace("_", " ");
        }
    }
}
#endif
