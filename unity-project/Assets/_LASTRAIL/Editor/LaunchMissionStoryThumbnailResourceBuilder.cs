#if UNITY_EDITOR
using System;
using System.IO;
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
    public static class LaunchMissionStoryThumbnailResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string Root = UiRoot + "/MissionStoryThumbnails";
        private const string ThumbnailRoot = Root + "/Thumbnails";
        private const string OverlayRoot = Root + "/Overlays";
        private const string ShowcaseRoot = Root + "/Showcase";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-mission-story-thumbnails.json";
        private const string ReadmePath = Root + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchMissionStoryThumbnailShowcase.unity";

        public const string ShowcasePrefabPath = ShowcaseRoot + "/LRLaunchMissionStoryThumbnailShowcase.prefab";

        private static readonly Color32 TextMain = C(238, 241, 234, 255);
        private static readonly Color32 TextMuted = C(173, 184, 181, 255);
        private static readonly Color32 Panel = C(6, 10, 14, 224);
        private static readonly Color32 Track = C(210, 146, 76, 255);
        private static readonly Color32 Steel = C(80, 96, 108, 255);
        private static readonly Color32 Signal = C(88, 192, 176, 255);
        private static readonly Color32 Warning = C(233, 180, 78, 255);
        private static readonly Color32 Danger = C(205, 70, 60, 255);

        public static readonly MissionStoryThumbnailSpec[] Missions =
        {
            Mission("opn_01_last_departure", "ordinary", "마지막 출발", "station_departure"),
            Mission("opn_02_first_signal", "ordinary", "첫 신호", "signal_tower"),
            Mission("opn_03_water_stop", "ordinary", "급수 정차", "water_stop"),
            Mission("opn_04_scrapstar_show", "ordinary", "스크랩스타 쇼", "scrap_arena"),
            Mission("opn_05_night_boarders", "ordinary", "야간 승선전", "night_boarding"),
            Mission("opn_06_broken_line", "ordinary", "끊어진 선로", "broken_line"),
            Mission("raid_raider_king_engine", "story_raid", "약탈왕 기관차", "raider_king_engine")
        };

        public static readonly MissionStoryOverlaySpec[] Overlays =
        {
            Overlay("mission_state_locked_overlay", "overlay", 1024, 576, "locked_visual_state"),
            Overlay("mission_state_available_accent", "accent", 1024, 576, "available_visual_accent"),
            Overlay("mission_state_cleared_stamp", "badge", 256, 96, "cleared_visual_stamp"),
            Overlay("mission_state_first_clear_badge", "badge", 256, 96, "first_clear_visual_badge"),
            Overlay("mission_state_risk_accent", "badge", 256, 96, "difficulty_risk_visual_accent"),
            Overlay("mission_state_boss_only_badge", "badge", 256, 96, "story_raid_boss_only_badge")
        };

        [MenuItem("LASTRAIL/Launch/Build Mission Story Thumbnail Resources")]
        public static void BuildMissionStoryThumbnailResources()
        {
            Ensure(Root);
            Ensure(ThumbnailRoot);
            Ensure(OverlayRoot);
            Ensure(ShowcaseRoot);
            Ensure(StyleRoot);
            Ensure(SceneRoot);

            foreach (var mission in Missions)
            {
                GenerateThumbnail(mission);
                ConfigureTexture(mission.ThumbnailPath, false);
            }

            foreach (var overlay in Overlays)
            {
                GenerateOverlay(overlay);
                ConfigureTexture(overlay.Path, true);
            }

            WriteManifest();
            WriteReadme();
            CreateShowcasePrefab();
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL mission/story thumbnail resources build complete.");
        }

        private static MissionStoryThumbnailSpec Mission(string missionId, string category, string label, string motif)
        {
            return new MissionStoryThumbnailSpec(
                missionId,
                category,
                label,
                motif,
                ThumbnailRoot + "/" + missionId + "_thumbnail.png");
        }

        private static MissionStoryOverlaySpec Overlay(string id, string category, int width, int height, string usage)
        {
            return new MissionStoryOverlaySpec(id, category, OverlayRoot + "/" + id + ".png", width, height, usage);
        }

        private static void GenerateThumbnail(MissionStoryThumbnailSpec mission)
        {
            var texture = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
            try
            {
                var palette = PaletteFor(mission.Motif);
                DrawGradient(texture, palette.Back, palette.Horizon);
                DrawNoiseRails(texture, palette.Glow);
                DrawTrackPerspective(texture, palette.Track);
                DrawSceneMotif(texture, mission.Motif, palette);
                DrawVignette(texture);
                texture.Apply(false);
                File.WriteAllBytes(ProjectPath(mission.ThumbnailPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void GenerateOverlay(MissionStoryOverlaySpec overlay)
        {
            var texture = new Texture2D(overlay.Width, overlay.Height, TextureFormat.RGBA32, false);
            try
            {
                Fill(texture, C(0, 0, 0, 0));
                switch (overlay.Id)
                {
                    case "mission_state_locked_overlay":
                        DrawLockedOverlay(texture);
                        break;
                    case "mission_state_available_accent":
                        DrawBorder(texture, Signal, 14);
                        DrawCornerTicks(texture, Warning);
                        break;
                    case "mission_state_cleared_stamp":
                        DrawBadge(texture, C(76, 164, 120, 246), C(7, 32, 24, 238), "C");
                        break;
                    case "mission_state_first_clear_badge":
                        DrawBadge(texture, Warning, C(56, 38, 12, 238), "F");
                        break;
                    case "mission_state_risk_accent":
                        DrawBadge(texture, C(96, 168, 220, 246), C(9, 30, 42, 238), "R");
                        break;
                    case "mission_state_boss_only_badge":
                        DrawBadge(texture, C(224, 92, 74, 246), C(52, 12, 12, 238), "B");
                        break;
                }

                texture.Apply(false);
                File.WriteAllBytes(ProjectPath(overlay.Path), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureTexture(string assetPath, bool alpha)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Missing mission/story thumbnail texture importer: " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = alpha;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void WriteManifest()
        {
            var b = new System.Text.StringBuilder();
            b.AppendLine("{");
            b.AppendLine("  \"name\": \"LAST RAIL Act 1 Mission Story Thumbnail Resources\",");
            b.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            b.AppendLine("  \"thumbnailResolution\": \"1024x576\",");
            b.AppendLine("  \"missionCount\": 7,");
            b.AppendLine("  \"ordinaryCount\": 6,");
            b.AppendLine("  \"storyRaidCount\": 1,");
            b.AppendLine("  \"overlayCount\": 6,");
            b.AppendLine("  \"generatedBy\": \"Unity procedural texture builder\",");
            b.AppendLine("  \"sourceType\": \"procedurally generated local launch UI assets\",");
            b.AppendLine("  \"presentationOnly\": true,");
            b.AppendLine("  \"serverContractChanged\": false,");
            b.AppendLine("  \"durableProgressionIncluded\": false,");
            b.AppendLine("  \"operationAccessIncluded\": false,");
            b.AppendLine("  \"rewardCalculationIncluded\": false,");
            b.AppendLine("  \"economyGrantIncluded\": false,");
            b.AppendLine("  \"missions\": [");
            for (var i = 0; i < Missions.Length; i++)
            {
                var mission = Missions[i];
                b.Append("    { \"missionId\": \"").Append(mission.MissionId).Append("\", ");
                b.Append("\"category\": \"").Append(mission.Category).Append("\", ");
                b.Append("\"displayLabel\": \"").Append(mission.DisplayLabel).Append("\", ");
                b.Append("\"thumbnailPath\": \"").Append(mission.ThumbnailPath).Append("\", ");
                b.Append("\"intendedUsage\": \"mission_select_story_progress_codex_archive\" }");
                b.AppendLine(i == Missions.Length - 1 ? string.Empty : ",");
            }

            b.AppendLine("  ],");
            b.AppendLine("  \"overlays\": [");
            for (var i = 0; i < Overlays.Length; i++)
            {
                var overlay = Overlays[i];
                b.Append("    { \"id\": \"").Append(overlay.Id).Append("\", ");
                b.Append("\"category\": \"").Append(overlay.Category).Append("\", ");
                b.Append("\"path\": \"").Append(overlay.Path).Append("\", ");
                b.Append("\"width\": ").Append(overlay.Width).Append(", ");
                b.Append("\"height\": ").Append(overlay.Height).Append(", ");
                b.Append("\"intendedUsage\": \"").Append(overlay.IntendedUsage).Append("\" }");
                b.AppendLine(i == Overlays.Length - 1 ? string.Empty : ",");
            }

            b.AppendLine("  ]");
            b.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), b.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Act 1 Mission Story Thumbnails

These mission/story thumbnail and overlay files are procedurally generated local launch UI assets for LAST RAIL Act 1.

## Resources

- 6 ordinary operation thumbnails: `opn_01_last_departure` through `opn_06_broken_line`
- 1 story raid thumbnail: `raid_raider_king_engine`
- Visual state overlays: locked, available, cleared, first-clear badge, risk accent, and boss-only badge

## Source And Usage

Source: Unity procedural texture builder in `LaunchMissionStoryThumbnailResourceBuilder`.

License note: no external paid or third-party art asset was introduced by this slice.

Use: composable 1024x576 thumbnails for 1080x1920 portrait MissionShell, StoryProgressSummary, and CodexArchive presentation surfaces. Mission access and progression are supplied by existing game state/server responses; these images are presentation-only.

Boundary: this resource set does not change server contracts, gameplay progression, operation access, first-clear policy, rewards, economy, offline rewards, gacha odds, or ledger behavior.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchMissionStoryThumbnailShowcase");
            AddBackground(root.transform);
            AddText(root.transform, "Title", "ACT 1 MISSION THUMBNAILS", 40, TextMain, new Vector2(900, 72), new Vector2(0, 820));
            AddText(root.transform, "Subtitle", "6 operations + story raid / visual state overlays / presentation-only", 23, TextMuted, new Vector2(900, 52), new Vector2(0, 765));

            for (var i = 0; i < 6; i++)
            {
                var x = i % 2 == 0 ? -252f : 252f;
                var y = 575f - (i / 2) * 348f;
                CreateMissionCard(root.transform, Missions[i], i, new Vector2(x, y), new Vector2(454, 282));
            }

            CreateMissionCard(root.transform, Missions[6], 6, new Vector2(0, -552), new Vector2(932, 326));
            AddText(root.transform, "Footer", "1080x1920 / safe area / TMP Pretendard / no server-side state embedded", 19, TextMuted, new Vector2(900, 44), new Vector2(0, -862));
            AddEventSystem(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, ShowcasePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateMissionCard(Transform parent, MissionStoryThumbnailSpec mission, int index, Vector2 position, Vector2 size)
        {
            var card = Image(parent, "Card_" + mission.MissionId, size, Panel);
            card.rectTransform.anchoredPosition = position;

            var thumbnail = Image(card.transform, "Thumbnail", new Vector2(size.x - 26, size.y - 58), Color.white);
            thumbnail.sprite = LoadSprite(mission.ThumbnailPath);
            thumbnail.preserveAspect = true;
            thumbnail.rectTransform.anchoredPosition = new Vector2(0, 19);

            if (index == 0)
            {
                AddOverlay(card.transform, "ClearedStamp", "mission_state_cleared_stamp", new Vector2(126, 47), new Vector2(size.x * 0.34f, size.y * 0.32f));
            }
            else if (index == 2)
            {
                AddOverlay(card.transform, "FirstClearBadge", "mission_state_first_clear_badge", new Vector2(126, 47), new Vector2(size.x * 0.29f, size.y * 0.32f));
                AddOverlay(card.transform, "RiskAccent", "mission_state_risk_accent", new Vector2(88, 34), new Vector2(-size.x * 0.36f, size.y * 0.32f));
            }
            else if (index == 3 || index == 5)
            {
                AddOverlay(card.transform, "LockedOverlay", "mission_state_locked_overlay", new Vector2(size.x - 26, size.y - 58), new Vector2(0, 19));
            }
            else if (index == 6)
            {
                AddOverlay(card.transform, "BossOnlyBadge", "mission_state_boss_only_badge", new Vector2(132, 49), new Vector2(size.x * 0.38f, size.y * 0.32f));
            }
            else
            {
                AddOverlay(card.transform, "AvailableAccent", "mission_state_available_accent", new Vector2(size.x - 26, size.y - 58), new Vector2(0, 19));
            }

            var labelPanel = Image(card.transform, "LabelPanel", new Vector2(size.x - 30, 44), C(3, 6, 9, 218));
            labelPanel.rectTransform.anchoredPosition = new Vector2(0, -size.y * 0.39f);
            AddText(card.transform, "Label", mission.DisplayLabel + " / " + mission.MissionId, 19, TextMain, new Vector2(size.x - 50, 38), new Vector2(0, -size.y * 0.39f));
        }

        private static void CreateShowcaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        }

        private static GameObject CreateCanvasRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1080, 1920);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            return root;
        }

        private static void AddBackground(Transform root)
        {
            var backdrop = Image(root, "Backdrop", new Vector2(1080, 1920), C(5, 8, 12, 255));
            backdrop.rectTransform.anchoredPosition = Vector2.zero;
            var rail = Image(root, "HeaderRail", new Vector2(924, 5), Track);
            rail.rectTransform.anchoredPosition = new Vector2(0, 740);
        }

        private static void AddOverlay(Transform parent, string name, string overlayId, Vector2 size, Vector2 position)
        {
            var image = Image(parent, name, size, Color.white);
            image.sprite = LoadSprite(OverlayRoot + "/" + overlayId + ".png");
            image.preserveAspect = true;
            image.rectTransform.anchoredPosition = position;
        }

        private static void AddEventSystem(Transform parent)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(parent, false);
            eventSystem.GetComponent<InputSystemUIInputModule>().enabled = true;
        }

        private static Image Image(Transform parent, string name, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        private static TMP_Text AddText(Transform parent, string name, string value, int size, Color color, Vector2 dimensions, Vector2 position)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            text.fontSize = size;
            text.fontSizeMax = size;
            text.fontSizeMin = 12;
            text.enableAutoSizing = true;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Truncate;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = dimensions;
            text.rectTransform.anchoredPosition = position;
            return text;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException("Missing mission/story thumbnail sprite: " + assetPath);
            }

            return sprite;
        }

        private static MissionPalette PaletteFor(string motif)
        {
            switch (motif)
            {
                case "signal_tower":
                    return new MissionPalette(C(7, 18, 28, 255), C(24, 86, 103, 255), C(64, 190, 170, 255), C(195, 145, 80, 255));
                case "water_stop":
                    return new MissionPalette(C(10, 23, 22, 255), C(34, 89, 85, 255), C(92, 186, 164, 255), C(198, 154, 96, 255));
                case "scrap_arena":
                    return new MissionPalette(C(24, 10, 10, 255), C(102, 48, 34, 255), C(222, 96, 70, 255), C(224, 174, 88, 255));
                case "night_boarding":
                    return new MissionPalette(C(7, 10, 24, 255), C(34, 42, 78, 255), C(124, 146, 210, 255), C(188, 132, 82, 255));
                case "broken_line":
                    return new MissionPalette(C(15, 18, 20, 255), C(70, 76, 72, 255), C(232, 186, 86, 255), C(126, 150, 150, 255));
                case "raider_king_engine":
                    return new MissionPalette(C(22, 7, 8, 255), C(96, 26, 22, 255), C(240, 176, 72, 255), C(210, 62, 50, 255));
                default:
                    return new MissionPalette(C(18, 16, 14, 255), C(92, 68, 42, 255), C(230, 145, 70, 255), C(96, 112, 116, 255));
            }
        }

        private static void DrawSceneMotif(Texture2D texture, string motif, MissionPalette palette)
        {
            switch (motif)
            {
                case "station_departure":
                    DrawRect(texture, 80, 274, 320, 94, C(38, 38, 34, 255));
                    DrawRect(texture, 112, 228, 540, 72, C(88, 62, 38, 255));
                    DrawRect(texture, 610, 216, 170, 88, palette.Glow);
                    DrawLine(texture, 112, 210, 620, 290, palette.Glow, 8);
                    break;
                case "signal_tower":
                    DrawLine(texture, 720, 106, 760, 410, palette.Glow, 8);
                    DrawLine(texture, 800, 106, 760, 410, palette.Glow, 8);
                    DrawCircle(texture, 760, 430, 46, palette.Glow);
                    DrawLine(texture, 610, 398, 910, 398, palette.Track, 7);
                    break;
                case "water_stop":
                    DrawRect(texture, 624, 210, 174, 172, C(54, 88, 83, 255));
                    DrawCircle(texture, 710, 400, 94, palette.Glow);
                    DrawRect(texture, 190, 285, 260, 42, palette.Track);
                    DrawLine(texture, 320, 286, 610, 196, palette.Glow, 8);
                    break;
                case "scrap_arena":
                    DrawCircle(texture, 512, 278, 196, C(142, 66, 42, 255));
                    DrawCircle(texture, 512, 278, 150, C(28, 22, 20, 255));
                    DrawRect(texture, 360, 386, 304, 56, palette.Glow);
                    DrawLine(texture, 226, 120, 820, 430, palette.Track, 7);
                    break;
                case "night_boarding":
                    DrawRect(texture, 96, 244, 832, 112, C(22, 28, 42, 255));
                    for (var i = 0; i < 7; i++)
                    {
                        DrawRect(texture, 150 + i * 108, 285, 54, 36, palette.Glow);
                    }
                    DrawLine(texture, 160, 390, 920, 250, palette.Track, 6);
                    break;
                case "broken_line":
                    DrawLine(texture, 80, 216, 438, 298, palette.Track, 12);
                    DrawLine(texture, 582, 310, 944, 232, palette.Track, 12);
                    DrawLine(texture, 450, 296, 566, 350, palette.Glow, 7);
                    DrawRect(texture, 400, 120, 224, 52, C(42, 44, 42, 255));
                    break;
                case "raider_king_engine":
                    DrawRect(texture, 192, 214, 576, 132, C(38, 34, 34, 255));
                    DrawRect(texture, 680, 246, 168, 118, C(48, 24, 20, 255));
                    DrawCircle(texture, 300, 196, 48, C(20, 20, 20, 255));
                    DrawCircle(texture, 612, 196, 58, C(20, 20, 20, 255));
                    DrawLine(texture, 360, 382, 512, 466, palette.Glow, 12);
                    DrawLine(texture, 664, 382, 512, 466, palette.Glow, 12);
                    break;
            }
        }

        private static void DrawGradient(Texture2D texture, Color32 bottom, Color32 top)
        {
            for (var y = 0; y < texture.height; y++)
            {
                var t = y / (float)(texture.height - 1);
                var color = Lerp(bottom, top, t);
                for (var x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void DrawNoiseRails(Texture2D texture, Color32 color)
        {
            for (var i = 0; i < 42; i++)
            {
                var y = 80 + i * 11;
                var alpha = (byte)Mathf.Clamp(18 + i * 3, 18, 90);
                DrawLine(texture, 0, y, texture.width, y + (i % 3 - 1) * 18, C(color.r, color.g, color.b, alpha), 2);
            }
        }

        private static void DrawTrackPerspective(Texture2D texture, Color32 color)
        {
            DrawLine(texture, 118, 16, 464, 315, color, 7);
            DrawLine(texture, 906, 16, 560, 315, color, 7);
            for (var i = 0; i < 9; i++)
            {
                var y = 24 + i * 34;
                var inset = i * 39;
                DrawLine(texture, 132 + inset, y, 892 - inset, y + 18, C(color.r, color.g, color.b, 168), 5);
            }
        }

        private static void DrawVignette(Texture2D texture)
        {
            var center = new Vector2(texture.width * 0.5f, texture.height * 0.52f);
            var maxDistance = center.magnitude;
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                    if (d > 0.52f)
                    {
                        var a = (byte)Mathf.Clamp((d - 0.52f) * 240f, 0, 124);
                        BlendPixel(texture, x, y, C(0, 0, 0, a));
                    }
                }
            }
        }

        private static void DrawLockedOverlay(Texture2D texture)
        {
            Fill(texture, C(0, 0, 0, 116));
            DrawBorder(texture, C(128, 132, 132, 190), 10);
            DrawLine(texture, 180, 130, 844, 446, C(214, 70, 64, 190), 16);
            DrawLine(texture, 844, 130, 180, 446, C(214, 70, 64, 190), 16);
            DrawRect(texture, 430, 238, 164, 116, C(18, 22, 26, 232));
            DrawCircle(texture, 512, 365, 64, C(210, 210, 195, 210));
            DrawRect(texture, 468, 300, 88, 72, C(18, 22, 26, 232));
        }

        private static void DrawBadge(Texture2D texture, Color32 stroke, Color32 fill, string marker)
        {
            DrawRect(texture, 0, 0, texture.width, texture.height, C(0, 0, 0, 0));
            DrawRect(texture, 6, 8, texture.width - 12, texture.height - 16, fill);
            DrawBorder(texture, stroke, 7);
            var cx = texture.width / 2;
            var cy = texture.height / 2;
            if (marker == "C")
            {
                DrawCircle(texture, cx, cy, 24, stroke);
                DrawRect(texture, cx + 4, cy - 20, 48, 42, fill);
            }
            else if (marker == "F")
            {
                DrawRect(texture, cx - 18, cy - 28, 12, 56, stroke);
                DrawRect(texture, cx - 18, cy + 18, 52, 10, stroke);
                DrawRect(texture, cx - 18, cy - 1, 42, 9, stroke);
            }
            else if (marker == "R")
            {
                DrawRect(texture, cx - 22, cy - 28, 12, 56, stroke);
                DrawCircle(texture, cx + 2, cy + 12, 24, stroke);
                DrawLine(texture, cx + 8, cy - 4, cx + 34, cy - 30, stroke, 9);
            }
            else
            {
                DrawRect(texture, cx - 30, cy - 28, 48, 56, stroke);
                DrawCircle(texture, cx + 16, cy + 8, 22, fill);
                DrawRect(texture, cx - 30, cy - 4, 46, 11, fill);
            }
        }

        private static void DrawBorder(Texture2D texture, Color32 color, int width)
        {
            DrawRect(texture, 0, 0, texture.width, width, color);
            DrawRect(texture, 0, texture.height - width, texture.width, width, color);
            DrawRect(texture, 0, 0, width, texture.height, color);
            DrawRect(texture, texture.width - width, 0, width, texture.height, color);
        }

        private static void DrawCornerTicks(Texture2D texture, Color32 color)
        {
            var w = texture.width;
            var h = texture.height;
            DrawRect(texture, 26, h - 58, 160, 12, color);
            DrawRect(texture, 26, h - 156, 12, 110, color);
            DrawRect(texture, w - 186, 46, 160, 12, color);
            DrawRect(texture, w - 38, 46, 12, 110, color);
        }

        private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color32 color)
        {
            var radiusSq = radius * radius;
            for (var y = cy - radius; y <= cy + radius; y++)
            {
                for (var x = cx - radius; x <= cx + radius; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    if (dx * dx + dy * dy <= radiusSq)
                    {
                        BlendPixel(texture, x, y, color);
                    }
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color, int width)
        {
            var dx = Math.Abs(x1 - x0);
            var sx = x0 < x1 ? 1 : -1;
            var dy = -Math.Abs(y1 - y0);
            var sy = y0 < y1 ? 1 : -1;
            var err = dx + dy;
            while (true)
            {
                DrawRect(texture, x0 - width / 2, y0 - width / 2, width, width, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                var e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color32 color)
        {
            var x0 = Mathf.Clamp(x, 0, texture.width);
            var y0 = Mathf.Clamp(y, 0, texture.height);
            var x1 = Mathf.Clamp(x + width, 0, texture.width);
            var y1 = Mathf.Clamp(y + height, 0, texture.height);
            for (var yy = y0; yy < y1; yy++)
            {
                for (var xx = x0; xx < x1; xx++)
                {
                    BlendPixel(texture, xx, yy, color);
                }
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

        private static void BlendPixel(Texture2D texture, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            var dst = (Color32)texture.GetPixel(x, y);
            var alpha = color.a / 255f;
            var inv = 1f - alpha;
            texture.SetPixel(x, y, new Color32(
                (byte)(color.r * alpha + dst.r * inv),
                (byte)(color.g * alpha + dst.g * inv),
                (byte)(color.b * alpha + dst.b * inv),
                (byte)Mathf.Max(color.a, dst.a)));
        }

        private static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)Mathf.Lerp(a.r, b.r, t),
                (byte)Mathf.Lerp(a.g, b.g, t),
                (byte)Mathf.Lerp(a.b, b.b, t),
                255);
        }

        private static Color32 C(byte r, byte g, byte b, byte a)
        {
            return new Color32(r, g, b, a);
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var folder = Path.GetFileName(path);
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        public readonly struct MissionStoryThumbnailSpec
        {
            public MissionStoryThumbnailSpec(string missionId, string category, string displayLabel, string motif, string thumbnailPath)
            {
                MissionId = missionId;
                Category = category;
                DisplayLabel = displayLabel;
                Motif = motif;
                ThumbnailPath = thumbnailPath;
            }

            public string MissionId { get; }
            public string Category { get; }
            public string DisplayLabel { get; }
            public string Motif { get; }
            public string ThumbnailPath { get; }
        }

        public readonly struct MissionStoryOverlaySpec
        {
            public MissionStoryOverlaySpec(string id, string category, string path, int width, int height, string intendedUsage)
            {
                Id = id;
                Category = category;
                Path = path;
                Width = width;
                Height = height;
                IntendedUsage = intendedUsage;
            }

            public string Id { get; }
            public string Category { get; }
            public string Path { get; }
            public int Width { get; }
            public int Height { get; }
            public string IntendedUsage { get; }
        }

        private readonly struct MissionPalette
        {
            public MissionPalette(Color32 back, Color32 horizon, Color32 glow, Color32 track)
            {
                Back = back;
                Horizon = horizon;
                Glow = glow;
                Track = track;
            }

            public Color32 Back { get; }
            public Color32 Horizon { get; }
            public Color32 Glow { get; }
            public Color32 Track { get; }
        }
    }
}
#endif
