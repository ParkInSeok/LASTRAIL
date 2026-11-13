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
    public static class LaunchArtResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string BackgroundRoot = UiRoot + "/Backgrounds";
        private const string KeyVisualRoot = UiRoot + "/KeyVisuals";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string ShowcaseRoot = BackgroundRoot + "/Showcase";
        private const string ShowcasePrefabPath = ShowcaseRoot + "/LRLaunchArtResourceShowcase.prefab";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchArtResourceShowcase.unity";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        private static readonly Color TextMain = C(236, 239, 235, 255);
        private static readonly Color TextMuted = C(174, 184, 184, 255);
        private static readonly Color Panel = C(8, 12, 17, 220);
        private static readonly Color Stroke = C(204, 125, 60, 190);

        public static readonly LaunchArtResourceSpec[] Resources =
        {
            new LaunchArtResourceSpec("main_lobby_train_interior", "MainLobby", BackgroundRoot + "/bg_main_lobby_train_interior.png", "메인 로비 / 열차 내부"),
            new LaunchArtResourceSpec("mission_select_rail_map", "MissionSelect", BackgroundRoot + "/bg_mission_select_rail_map.png", "작전 선택 / 철도 지도"),
            new LaunchArtResourceSpec("combat_wasteland_rail", "CombatHud", BackgroundRoot + "/bg_combat_wasteland_rail.png", "전투 HUD / 황무지 철로"),
            new LaunchArtResourceSpec("gacha_recruitment_chamber", "Gacha", KeyVisualRoot + "/kv_gacha_recruitment_chamber.png", "모집 / 신호 장치"),
            new LaunchArtResourceSpec("codex_archive_records_room", "CodexArchive", BackgroundRoot + "/bg_codex_archive_records_room.png", "Codex / 기록실"),
            new LaunchArtResourceSpec("reward_result_supply_bay", "RewardReview", BackgroundRoot + "/bg_reward_result_supply_bay.png", "보상 결과 / 보급칸"),
            new LaunchArtResourceSpec("offline_reward_settlement", "OfflineReward", BackgroundRoot + "/bg_offline_reward_settlement.png", "오프라인 정산 / 정비칸"),
            new LaunchArtResourceSpec("story_raid_boss_entrance", "StoryRaidBoss", KeyVisualRoot + "/kv_story_raid_boss_entrance.png", "Act 1 보스 진입")
        };

        [MenuItem("LASTRAIL/Launch/Build Art Resource Foundation")]
        public static void BuildArtResources()
        {
            Ensure(BackgroundRoot);
            Ensure(KeyVisualRoot);
            Ensure(StyleRoot);
            Ensure(SceneRoot);
            Ensure(ShowcaseRoot);

            foreach (var resource in Resources)
            {
                ConfigureTexture(resource.Path);
            }

            WriteManifest();
            WriteReadme();
            CreateShowcasePrefab();
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL launch art resource foundation build complete.");
        }

        private static void ConfigureTexture(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Missing launch art texture importer: " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void WriteManifest()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"name\": \"LAST RAIL Act 1 Launch Art Resources\",");
            builder.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            builder.AppendLine("  \"resourceCount\": 8,");
            builder.AppendLine("  \"generatedBy\": \"OpenAI image_gen built-in tool\",");
            builder.AppendLine("  \"uiOverlayReadable\": true,");
            builder.AppendLine("  \"serverTruthFieldsAllowed\": false,");
            builder.AppendLine("  \"railsChoiceIncluded\": false,");
            builder.AppendLine("  \"worldRaidIncluded\": false,");
            builder.AppendLine("  \"resources\": [");
            for (var i = 0; i < Resources.Length; i++)
            {
                var resource = Resources[i];
                builder.Append("    { \"id\": \"").Append(resource.Id).Append("\", ");
                builder.Append("\"surface\": \"").Append(resource.Surface).Append("\", ");
                builder.Append("\"path\": \"").Append(resource.Path).Append("\", ");
                builder.Append("\"sourceType\": \"generated_local_launch_asset\", ");
                builder.Append("\"licenseNote\": \"Project-generated image resource; no external paid asset dependency.\" }");
                builder.AppendLine(i == Resources.Length - 1 ? string.Empty : ",");
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(ProjectPath(StyleRoot + "/lastrail-launch-art-resources.json"), builder.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(BackgroundRoot + "/README.md"), @"# LAST RAIL Launch Background And Key Visual Resources

These files are project-generated launch UI resources created with the OpenAI image_gen built-in tool for LAST RAIL Act 1 screen composition.

## Imported Resources

- `bg_main_lobby_train_interior.png` - main lobby / train interior
- `bg_mission_select_rail_map.png` - mission select / railway map
- `bg_combat_wasteland_rail.png` - combat HUD / wasteland railway
- `../KeyVisuals/kv_gacha_recruitment_chamber.png` - gacha / recruitment reveal
- `bg_codex_archive_records_room.png` - codex / archive records room
- `bg_reward_result_supply_bay.png` - reward result / supply bay
- `bg_offline_reward_settlement.png` - offline reward settlement
- `../KeyVisuals/kv_story_raid_boss_entrance.png` - Act 1 story raid boss entrance

## Source And License Note

Source: OpenAI image_gen built-in tool, generated for this local project. No external paid asset dependency was introduced by this slice.

Use: intended as composable 1080x1920 portrait launch UI backgrounds/key visuals. Keep UI text in safe area overlays with dark readability scrims when needed.

Boundary: these assets are presentation resources only. They do not encode server, reward, economy, story progress, unlock, offline reward, gacha odds, or ledger truth.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = new GameObject("LRLaunchArtResourceShowcase", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1080f, 1920f);

            var title = Text(root.transform, "Title", "LAST RAIL Launch Art Resources", 34, TextMain, new Vector2(920f, 64f));
            title.rectTransform.anchoredPosition = new Vector2(0f, 870f);

            for (var i = 0; i < Resources.Length; i++)
            {
                var x = i % 2 == 0 ? -265f : 265f;
                var y = 650f - (i / 2) * 410f;
                CreateCard(root.transform, Resources[i], new Vector2(x, y));
            }

            var footer = Text(root.transform, "Footer", "1080x1920 / safe area overlay / Pretendard TMP / presentation only", 20, TextMuted, new Vector2(940f, 44f));
            footer.rectTransform.anchoredPosition = new Vector2(0f, -880f);

            PrefabUtility.SaveAsPrefabAsset(root, ShowcasePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateCard(Transform parent, LaunchArtResourceSpec resource, Vector2 position)
        {
            var card = new GameObject(resource.Id, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(490f, 360f);
            cardRect.anchoredPosition = position;
            card.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            var imageObject = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(card.transform, false);
            var image = imageObject.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(resource.Path);
            image.preserveAspect = true;
            image.color = Color.white;
            var imageRect = image.GetComponent<RectTransform>();
            imageRect.sizeDelta = new Vector2(450f, 280f);
            imageRect.anchoredPosition = new Vector2(0f, 30f);

            var overlay = new GameObject("ReadabilityScrim", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(card.transform, false);
            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = Panel;
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.sizeDelta = new Vector2(450f, 56f);
            overlayRect.anchoredPosition = new Vector2(0f, -118f);

            var label = Text(card.transform, "Label", resource.Label, 22, TextMain, new Vector2(420f, 42f));
            label.rectTransform.anchoredPosition = new Vector2(0f, -118f);

            var stroke = new GameObject("AccentRail", typeof(RectTransform), typeof(Image));
            stroke.transform.SetParent(card.transform, false);
            var strokeImage = stroke.GetComponent<Image>();
            strokeImage.color = Stroke;
            var strokeRect = stroke.GetComponent<RectTransform>();
            strokeRect.sizeDelta = new Vector2(450f, 4f);
            strokeRect.anchoredPosition = new Vector2(0f, -86f);
        }

        private static void CreateShowcaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().enabled = true;
            EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        }

        private static TMP_Text Text(Transform parent, string name, string value, int size, Color color, Vector2 dimensions)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            text.fontSize = size;
            text.fontSizeMin = 16;
            text.fontSizeMax = size;
            text.enableAutoSizing = true;
            text.overflowMode = TextOverflowModes.Truncate;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = dimensions;
            return text;
        }

        private static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                Ensure(parent);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static Color C(byte r, byte g, byte b, byte a)
        {
            return new Color32(r, g, b, a);
        }

        public readonly struct LaunchArtResourceSpec
        {
            public LaunchArtResourceSpec(string id, string surface, string path, string label)
            {
                Id = id;
                Surface = surface;
                Path = path;
                Label = label;
            }

            public string Id { get; }
            public string Surface { get; }
            public string Path { get; }
            public string Label { get; }
        }
    }
}
#endif
