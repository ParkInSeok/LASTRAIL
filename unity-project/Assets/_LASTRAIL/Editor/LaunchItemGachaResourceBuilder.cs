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
    public static class LaunchItemGachaResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string GachaRoot = UiRoot + "/GachaResources";
        private const string IconsRoot = GachaRoot + "/Icons";
        private const string GachaFxRoot = GachaRoot + "/GachaFx";
        private const string SourcesRoot = GachaRoot + "/Sources";
        private const string ShowcaseRoot = GachaRoot + "/Showcase";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-item-gacha-resources.json";
        private const string ReadmePath = GachaRoot + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchItemGachaResourceShowcase.unity";
        public const string ShowcasePrefabPath = ShowcaseRoot + "/LRLaunchItemGachaResourceShowcase.prefab";

        private static readonly Color TextMain = C(238, 241, 234, 255);
        private static readonly Color TextMuted = C(176, 188, 188, 255);
        private static readonly Color Panel = C(5, 8, 12, 218);
        private static readonly Color Stroke = C(226, 112, 54, 210);
        private static readonly Color Currency = C(235, 178, 74, 255);
        private static readonly Color Gacha = C(90, 202, 226, 255);

        public static readonly LaunchItemGachaResourceSpec[] ItemResources =
        {
            Item("cur_command_ticket", "currency", IconsRoot + "/cur_command_ticket.png", "pull_cost_icon"),
            Item("cur_command_ticket_fragment", "currency", IconsRoot + "/cur_command_ticket_fragment.png", "ticket_fragment_display"),
            Item("cur_special_core", "currency", IconsRoot + "/cur_special_core.png", "premium_core_display"),
            Item("ui_fuel_stamina", "item", IconsRoot + "/ui_fuel_stamina.png", "stamina_display"),
            Item("ui_supply_crate", "item", IconsRoot + "/ui_supply_crate.png", "reward_source_display"),
            Item("ui_reward_chest", "item", IconsRoot + "/ui_reward_chest.png", "reward_claim_display"),
            Item("ui_operator_duplicate_token", "item", IconsRoot + "/ui_operator_duplicate_token.png", "duplicate_conversion_display"),
            Item("ui_common_material_plate", "item", IconsRoot + "/ui_common_material_plate.png", "common_backing_plate"),
            Item("ui_common_spark_plate", "item", IconsRoot + "/ui_common_spark_plate.png", "small_glow_plate")
        };

        public static readonly LaunchItemGachaResourceSpec[] GachaResources =
        {
            GachaSpec("gacha_single_pull_reveal_plate", "gacha_fx", GachaFxRoot + "/gacha_single_pull_reveal_plate.png", 768, 768, "single_pull_reveal"),
            GachaSpec("gacha_ten_pull_result_grid_frame", "frame", GachaFxRoot + "/gacha_ten_pull_result_grid_frame.png", 1024, 768, "ten_pull_grid"),
            GachaSpec("gacha_rare_glow", "gacha_fx", GachaFxRoot + "/gacha_rare_glow.png", 768, 768, "rare_presentation_glow"),
            GachaSpec("gacha_elite_glow", "gacha_fx", GachaFxRoot + "/gacha_elite_glow.png", 768, 768, "elite_presentation_glow"),
            GachaSpec("gacha_pickup_elite_glow", "gacha_fx", GachaFxRoot + "/gacha_pickup_elite_glow.png", 768, 768, "pickup_elite_presentation_glow"),
            GachaSpec("gacha_ticket_spend_pieces", "gacha_fx", GachaFxRoot + "/gacha_ticket_spend_pieces.png", 768, 768, "ticket_spend_cue"),
            GachaSpec("gacha_duplicate_conversion_visual", "gacha_fx", GachaFxRoot + "/gacha_duplicate_conversion_visual.png", 768, 768, "duplicate_conversion_cue"),
            GachaSpec("gacha_recruitment_aperture_open", "gacha_fx", GachaFxRoot + "/gacha_recruitment_aperture_open.png", 768, 768, "capsule_open_cue"),
            GachaSpec("gacha_result_card_frame", "frame", GachaFxRoot + "/gacha_result_card_frame.png", 768, 1024, "result_card_back")
        };

        [MenuItem("LASTRAIL/Launch/Build Item Gacha Resource Foundation")]
        public static void BuildItemGachaResources()
        {
            Ensure(GachaRoot); Ensure(IconsRoot); Ensure(GachaFxRoot); Ensure(SourcesRoot); Ensure(ShowcaseRoot); Ensure(StyleRoot); Ensure(SceneRoot);
            foreach (var item in ItemResources) ConfigureTexture(item.Path, false);
            foreach (var gacha in GachaResources) ConfigureTexture(gacha.Path, true);
            ConfigureTexture(SourcesRoot + "/act1_item_currency_contact_sheet_generated.png", false, false);
            ConfigureTexture(SourcesRoot + "/act1_gacha_reveal_contact_sheet_generated.png", false, false);
            WriteManifest(); WriteReadme(); CreateShowcasePrefab(); CreateShowcaseScene();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("LASTRAIL launch item/gacha visual resource foundation build complete.");
        }

        private static LaunchItemGachaResourceSpec Item(string id, string category, string path, string usage) => new LaunchItemGachaResourceSpec(id, category, path, 512, 512, usage);
        private static LaunchItemGachaResourceSpec GachaSpec(string id, string category, string path, int width, int height, string usage) => new LaunchItemGachaResourceSpec(id, category, path, width, height, usage);

        private static void ConfigureTexture(string assetPath, bool alpha, bool sprite = true)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing item/gacha texture importer: " + assetPath);
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
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
            b.AppendLine("  \"name\": \"LAST RAIL Act 1 Item And Gacha Visual Resources\",");
            b.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            b.AppendLine("  \"itemIconCount\": 9,");
            b.AppendLine("  \"gachaResourceCount\": 9,");
            b.AppendLine("  \"generatedBy\": \"OpenAI image_gen built-in tool\",");
            b.AppendLine("  \"sourceType\": \"project_generated_launch_asset\",");
            b.AppendLine("  \"serverTruthFieldsAllowed\": false,");
            b.AppendLine("  \"economyTruthIncluded\": false,");
            b.AppendLine("  \"gachaRateTruthIncluded\": false,");
            b.AppendLine("  \"railsChoiceIncluded\": false,");
            b.AppendLine("  \"worldRaidIncluded\": false,");
            b.AppendLine("  \"items\": [");
            AppendSpecs(b, ItemResources);
            b.AppendLine("  ],");
            b.AppendLine("  \"gacha\": [");
            AppendSpecs(b, GachaResources);
            b.AppendLine("  ]");
            b.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), b.ToString());
        }

        private static void AppendSpecs(System.Text.StringBuilder b, LaunchItemGachaResourceSpec[] specs)
        {
            for (var i = 0; i < specs.Length; i++)
            {
                var s = specs[i];
                b.Append("    { \"id\": \"").Append(s.Id).Append("\", \"category\": \"").Append(s.Category).Append("\", \"path\": \"").Append(s.Path).Append("\", \"width\": ").Append(s.Width).Append(", \"height\": ").Append(s.Height).Append(", \"intendedUsage\": \"").Append(s.IntendedUsage).Append("\", \"sourceType\": \"generated_local_launch_asset\" }");
                b.AppendLine(i == specs.Length - 1 ? string.Empty : ",");
            }
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Act 1 Item And Gacha Visual Resources

These item, currency, and gacha reveal files are project-generated launch item and gacha visual resources created with the OpenAI image_gen built-in tool for LAST RAIL Act 1.

Source sheets:
- `Assets/_LASTRAIL/UI/GachaResources/Sources/act1_item_currency_contact_sheet_generated.png`
- `Assets/_LASTRAIL/UI/GachaResources/Sources/act1_gacha_reveal_contact_sheet_generated.png`

Scope:
- 9 item/currency icon display resources.
- 9 gacha reveal/frame/glow display resources.
- Includes command ticket, ticket fragment, special core, fuel/stamina, supply/reward containers, duplicate token, one-pull reveal plate, ten-pull grid frame, rarity presentation glows, ticket spend pieces, duplicate conversion visual, aperture open visual, and result card frame.

Usage:
- Use the manifest at `Assets/_LASTRAIL/UI/Style/lastrail-item-gacha-resources.json` for QA and resource readiness.
- These are presentation resources only. They do not encode item grants, rewards, economy grants, offline rewards, gacha odds, rates, weights, pity state, or ledger state.
- External paid asset dependency: none.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchItemGachaResourceShowcase");
            AddBackground(root.transform); AddHeader(root.transform); AddItemGrid(root.transform); AddGachaPreview(root.transform); AddEventSystem(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, ShowcasePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static GameObject CreateCanvasRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            Stretch(root.GetComponent<RectTransform>());
            return root;
        }

        private static void AddBackground(Transform root)
        {
            var go = UiObject("ItemGachaShowcaseBackdrop", root, typeof(Image)); Stretch(go.GetComponent<RectTransform>()); go.GetComponent<Image>().color = C(5, 7, 10, 255);
        }

        private static void AddHeader(Transform root)
        {
            var panel = PanelObject("HeaderPanel", root, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.975f));
            AddText(panel.transform, "Title", "ACT 1 ITEM / GACHA VISUAL FOUNDATION", 34, TextMain, new Vector2(0.04f, 0.44f), new Vector2(0.96f, 0.9f), TextAlignmentOptions.MidlineLeft);
            AddText(panel.transform, "Subtitle", "currency icons / reveal plates / frame and glow cues", 22, TextMuted, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.42f), TextAlignmentOptions.MidlineLeft);
        }

        private static void AddItemGrid(Transform root)
        {
            var panel = PanelObject("ItemIconPanel", root, new Vector2(0.05f, 0.515f), new Vector2(0.95f, 0.885f));
            AddText(panel.transform, "ItemIconLabel", "ITEM AND CURRENCY ICONS", 24, Currency, new Vector2(0.03f, 0.89f), new Vector2(0.97f, 0.98f), TextAlignmentOptions.MidlineLeft);
            for (var i = 0; i < ItemResources.Length; i++) AddIconCard(panel.transform, ItemResources[i], i);
        }

        private static void AddIconCard(Transform parent, LaunchItemGachaResourceSpec spec, int index)
        {
            var col = index % 3; var row = index / 3;
            var card = UiObject("ItemCard_" + spec.Id, parent, typeof(Image));
            var rect = card.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0.045f + col * 0.315f, 0.07f + (2 - row) * 0.27f); rect.anchorMax = new Vector2(0.295f + col * 0.315f, 0.31f + (2 - row) * 0.27f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = C(10, 13, 17, 215);
            var img = UiObject("Sprite", card.transform, typeof(Image)); var imgRect = img.GetComponent<RectTransform>(); imgRect.anchorMin = new Vector2(0.08f, 0.22f); imgRect.anchorMax = new Vector2(0.92f, 0.96f); imgRect.offsetMin = Vector2.zero; imgRect.offsetMax = Vector2.zero;
            var image = img.GetComponent<Image>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.Path); image.preserveAspect = true; image.raycastTarget = false;
            AddText(card.transform, "Id", spec.Id, 14, TextMain, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.19f), TextAlignmentOptions.Center);
        }

        private static void AddGachaPreview(Transform root)
        {
            var panel = PanelObject("GachaVisualPanel", root, new Vector2(0.05f, 0.045f), new Vector2(0.95f, 0.495f));
            AddText(panel.transform, "GachaVisualLabel", "GACHA REVEAL STATES", 24, Gacha, new Vector2(0.03f, 0.91f), new Vector2(0.97f, 0.98f), TextAlignmentOptions.MidlineLeft);
            for (var i = 0; i < GachaResources.Length; i++) AddGachaCard(panel.transform, GachaResources[i], i);
        }

        private static void AddGachaCard(Transform parent, LaunchItemGachaResourceSpec spec, int index)
        {
            var col = index % 3; var row = index / 3;
            var card = UiObject("GachaCard_" + spec.Id, parent, typeof(Image));
            var rect = card.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0.045f + col * 0.315f, 0.06f + (2 - row) * 0.285f); rect.anchorMax = new Vector2(0.295f + col * 0.315f, 0.31f + (2 - row) * 0.285f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = C(9, 12, 16, 190);
            var img = UiObject("Sprite", card.transform, typeof(Image)); var imgRect = img.GetComponent<RectTransform>(); imgRect.anchorMin = new Vector2(0.05f, 0.2f); imgRect.anchorMax = new Vector2(0.95f, 0.97f); imgRect.offsetMin = Vector2.zero; imgRect.offsetMax = Vector2.zero;
            var image = img.GetComponent<Image>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.Path); image.preserveAspect = true; image.raycastTarget = false;
            AddText(card.transform, "Id", spec.Id.Replace("gacha_", string.Empty), 13, TextMain, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.19f), TextAlignmentOptions.Center);
        }

        private static GameObject PanelObject(string name, Transform root, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = UiObject(name, root, typeof(Image)); var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; panel.GetComponent<Image>().color = Panel;
            var rail = UiObject("AccentRail", panel.transform, typeof(Image)); var railRect = rail.GetComponent<RectTransform>(); railRect.anchorMin = new Vector2(0.03f, 0.84f); railRect.anchorMax = new Vector2(0.97f, 0.855f); railRect.offsetMin = Vector2.zero; railRect.offsetMax = Vector2.zero; rail.GetComponent<Image>().color = Stroke; rail.GetComponent<Image>().raycastTarget = false;
            return panel;
        }

        private static TMP_Text AddText(Transform parent, string name, string text, int size, Color color, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment)
        {
            var go = UiObject(name, parent, typeof(TextMeshProUGUI)); var rect = go.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var tmp = go.GetComponent<TMP_Text>(); tmp.text = text; tmp.fontSize = size; tmp.fontSizeMin = Math.Max(10, size - 8); tmp.fontSizeMax = size; tmp.enableAutoSizing = true; tmp.color = color; tmp.alignment = alignment; tmp.overflowMode = TextOverflowModes.Ellipsis; tmp.raycastTarget = false;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath); if (font != null) tmp.font = font;
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath); if (material != null) tmp.fontSharedMaterial = material;
            return tmp;
        }

        private static void AddEventSystem(Transform root) => new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(root, false);

        private static void CreateShowcaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath), scene);
            EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        }

        private static GameObject UiObject(string name, Transform parent, params Type[] components) { var go = new GameObject(name, components); go.transform.SetParent(parent, false); return go; }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
        private static void Ensure(string assetPath) { if (AssetDatabase.IsValidFolder(assetPath)) return; var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/'); var name = Path.GetFileName(assetPath); if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) Ensure(parent); AssetDatabase.CreateFolder(parent ?? "Assets", name); }
        private static string ProjectPath(string assetPath) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        private static Color C(byte r, byte g, byte b, byte a) => new Color32(r, g, b, a);
    }

    public sealed class LaunchItemGachaResourceSpec
    {
        public LaunchItemGachaResourceSpec(string id, string category, string path, int width, int height, string intendedUsage) { Id = id; Category = category; Path = path; Width = width; Height = height; IntendedUsage = intendedUsage; }
        public string Id { get; } public string Category { get; } public string Path { get; } public int Width { get; } public int Height { get; } public string IntendedUsage { get; }
    }
}
#endif
