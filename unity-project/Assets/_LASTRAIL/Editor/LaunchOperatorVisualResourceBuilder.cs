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
    public static class LaunchOperatorVisualResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string OperatorRoot = UiRoot + "/Operators";
        private const string CardsRoot = OperatorRoot + "/Cards";
        private const string PortraitsRoot = OperatorRoot + "/Portraits";
        private const string IconsRoot = OperatorRoot + "/Icons";
        private const string FramesRoot = OperatorRoot + "/Frames";
        private const string SourcesRoot = OperatorRoot + "/Sources";
        private const string ShowcaseRoot = OperatorRoot + "/Showcase";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-operator-visual-resources.json";
        private const string ReadmePath = OperatorRoot + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchOperatorVisualShowcase.unity";

        public const string ShowcasePrefabPath = ShowcaseRoot + "/LRLaunchOperatorVisualShowcase.prefab";

        private static readonly Color TextMain = C(238, 241, 234, 255);
        private static readonly Color TextMuted = C(176, 188, 188, 255);
        private static readonly Color Panel = C(5, 8, 12, 216);
        private static readonly Color Stroke = C(218, 146, 70, 190);
        private static readonly Color Elite = C(226, 194, 94, 255);
        private static readonly Color Pickup = C(255, 214, 92, 255);
        private static readonly Color Initial = C(210, 128, 58, 255);

        public static readonly LaunchOperatorVisualResourceSpec[] Operators =
        {
            Operator("op_kyle", "Kyle", "initial_free", "operator_card_initial_free", false),
            Operator("op_reina", "Reina", "initial_free", "operator_card_initial_free", false),
            Operator("op_eve", "Eve", "initial_free", "operator_card_initial_free", false),
            Operator("op_sera", "Sera", "initial_free", "operator_card_initial_free", false),
            Operator("op_mira", "Mira", "initial_free", "operator_card_initial_free", false),
            Operator("op_vex", "Vex", "gacha_elite", "operator_card_pickup_elite", true),
            Operator("op_nyx", "Nyx", "gacha_elite", "operator_card_elite", false),
            Operator("op_sable", "Sable", "gacha_elite", "operator_card_elite", false),
            Operator("op_marrow", "Marrow", "gacha_elite", "operator_card_elite", false),
            Operator("op_iris", "Iris", "gacha_elite", "operator_card_elite", false),
            Operator("op_rail", "Rail", "gacha_elite", "operator_card_elite", false),
            Operator("op_anchor", "Anchor", "gacha_elite", "operator_card_elite", false),
            Operator("op_lumen", "Lumen", "gacha_elite", "operator_card_elite", false),
            Operator("op_cinder", "Cinder", "gacha_elite", "operator_card_elite", false),
            Operator("op_warden", "Warden", "gacha_elite", "operator_card_elite", false)
        };

        [MenuItem("LASTRAIL/Launch/Build Operator Visual Resource Foundation")]
        public static void BuildOperatorVisualResources()
        {
            Ensure(OperatorRoot);
            Ensure(CardsRoot);
            Ensure(PortraitsRoot);
            Ensure(IconsRoot);
            Ensure(FramesRoot);
            Ensure(SourcesRoot);
            Ensure(ShowcaseRoot);
            Ensure(StyleRoot);
            Ensure(SceneRoot);

            foreach (var op in Operators)
            {
                ConfigureTexture(op.CardPath, true);
                ConfigureTexture(op.PortraitPath, true);
                ConfigureTexture(op.IconPath, true);
                ConfigureTexture(op.FramePath, true);
            }

            ConfigureTexture(SourcesRoot + "/act1_operator_contact_sheet_generated.png", false);
            WriteManifest();
            WriteReadme();
            CreateShowcasePrefab();
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL launch operator visual resource foundation build complete.");
        }

        private static LaunchOperatorVisualResourceSpec Operator(
            string operatorId,
            string displayLabel,
            string acquisition,
            string frameId,
            bool isPickup)
        {
            return new LaunchOperatorVisualResourceSpec(
                operatorId,
                displayLabel,
                acquisition,
                frameId,
                isPickup,
                PortraitsRoot + "/" + operatorId + "_portrait.png",
                CardsRoot + "/" + operatorId + "_card.png",
                IconsRoot + "/" + operatorId + "_icon.png",
                FramesRoot + "/" + frameId + ".png");
        }

        private static void ConfigureTexture(string assetPath, bool sprite)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Missing operator visual texture importer: " + assetPath);
            }

            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = assetPath.Contains("/Frames/");
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
            builder.AppendLine("  \"name\": \"LAST RAIL Act 1 Operator Visual Resources\",");
            builder.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            builder.AppendLine("  \"operatorCount\": 15,");
            builder.AppendLine("  \"initialFreeCount\": 5,");
            builder.AppendLine("  \"gachaEliteCount\": 10,");
            builder.AppendLine("  \"generatedBy\": \"OpenAI image_gen built-in tool\",");
            builder.AppendLine("  \"sourceType\": \"project_generated_launch_asset\",");
            builder.AppendLine("  \"serverTruthFieldsAllowed\": false,");
            builder.AppendLine("  \"railsChoiceIncluded\": false,");
            builder.AppendLine("  \"worldRaidIncluded\": false,");
            builder.AppendLine("  \"frames\": [");
            builder.AppendLine("    { \"id\": \"operator_card_initial_free\", \"displayScope\": \"initial_free\", \"path\": \"Assets/_LASTRAIL/UI/Operators/Frames/operator_card_initial_free.png\" },");
            builder.AppendLine("    { \"id\": \"operator_card_elite\", \"displayScope\": \"gacha_elite\", \"path\": \"Assets/_LASTRAIL/UI/Operators/Frames/operator_card_elite.png\" },");
            builder.AppendLine("    { \"id\": \"operator_card_pickup_elite\", \"displayScope\": \"pickup_elite\", \"path\": \"Assets/_LASTRAIL/UI/Operators/Frames/operator_card_pickup_elite.png\" }");
            builder.AppendLine("  ],");
            builder.AppendLine("  \"operators\": [");
            for (var i = 0; i < Operators.Length; i++)
            {
                var op = Operators[i];
                builder.Append("    { \"operatorId\": \"").Append(op.OperatorId).Append("\", ");
                builder.Append("\"displayLabel\": \"").Append(op.DisplayLabel).Append("\", ");
                builder.Append("\"acquisition\": \"").Append(op.Acquisition).Append("\", ");
                builder.Append("\"isPickup\": ").Append(op.IsPickup ? "true" : "false").Append(", ");
                builder.Append("\"portraitPath\": \"").Append(op.PortraitPath).Append("\", ");
                builder.Append("\"cardPath\": \"").Append(op.CardPath).Append("\", ");
                builder.Append("\"iconPath\": \"").Append(op.IconPath).Append("\", ");
                builder.Append("\"frameId\": \"").Append(op.FrameId).Append("\" }");
                builder.AppendLine(i == Operators.Length - 1 ? string.Empty : ",");
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), builder.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Act 1 Operator Visual Resources

These operator card, portrait, icon, and frame resources are project-generated launch UI assets created with the OpenAI image_gen built-in tool for LAST RAIL Act 1.

The source sheet is preserved at `Assets/_LASTRAIL/UI/Operators/Sources/act1_operator_contact_sheet_generated.png`.

Scope:
- 15 visual operators for Act 1 launch: 5 initial/free operators and 10 gacha elite display candidates.
- `op_vex` uses the pickup-elite display frame candidate.
- Card, portrait, icon, and frame files are presentation resources only.
- These files are not gameplay roster truth, reward truth, story progress truth, economy truth, unlock truth, or gacha odds truth.

Usage:
- Use the manifest at `Assets/_LASTRAIL/UI/Style/lastrail-operator-visual-resources.json` for QA and showcase readiness.
- Localized operator names should continue to use string-id lookup; manifest display labels are non-authoritative fallback labels.
- External paid asset dependency: none.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchOperatorVisualShowcase");
            AddBackground(root.transform);
            AddHeader(root.transform);
            AddOperatorGrid(root.transform);
            AddEventSystem(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, ShowcasePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static GameObject CreateCanvasRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return root;
        }

        private static void AddBackground(Transform root)
        {
            var go = UiObject("OperatorShowcaseBackdrop", root, typeof(Image));
            Stretch(go.GetComponent<RectTransform>());
            var image = go.GetComponent<Image>();
            image.color = C(7, 9, 12, 255);
        }

        private static void AddHeader(Transform root)
        {
            var panel = UiObject("HeaderPanel", root, typeof(Image));
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.88f);
            rect.anchorMax = new Vector2(0.95f, 0.975f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = Panel;

            AddText(panel.transform, "Title", "ACT 1 OPERATOR VISUAL FOUNDATION", 38, TextMain, new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.92f));
            AddText(panel.transform, "Subtitle", "15 operators / cards / portraits / icons / rarity display frames", 24, TextMuted, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.44f));
        }

        private static void AddOperatorGrid(Transform root)
        {
            var gridRoot = UiObject("OperatorCardGrid", root, typeof(GridLayoutGroup));
            var rect = gridRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.045f);
            rect.anchorMax = new Vector2(0.95f, 0.865f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var grid = gridRoot.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.cellSize = new Vector2(186, 248);
            grid.spacing = new Vector2(16, 28);
            grid.childAlignment = TextAnchor.MiddleCenter;

            foreach (var op in Operators)
            {
                AddOperatorCard(gridRoot.transform, op);
            }
        }

        private static void AddOperatorCard(Transform parent, LaunchOperatorVisualResourceSpec op)
        {
            var card = UiObject("OperatorCard_" + op.OperatorId, parent, typeof(Image));
            card.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(op.CardPath);
            card.GetComponent<Image>().preserveAspect = true;

            var stroke = UiObject("Frame_" + op.OperatorId, card.transform, typeof(Image));
            Stretch(stroke.GetComponent<RectTransform>());
            stroke.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(op.FramePath);
            stroke.GetComponent<Image>().preserveAspect = true;
            stroke.GetComponent<Image>().raycastTarget = false;

            var tag = op.IsPickup ? "PICKUP ELITE" : (op.Acquisition == "initial_free" ? "INITIAL" : "ELITE");
            var tagColor = op.IsPickup ? Pickup : (op.Acquisition == "initial_free" ? Initial : Elite);
            AddText(card.transform, "OperatorId", op.OperatorId, 22, TextMain, new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.18f));
            AddText(card.transform, "DisplayLabel", op.DisplayLabel, 25, TextMain, new Vector2(0.08f, 0.02f), new Vector2(0.5f, 0.1f));
            AddText(card.transform, "Acquisition", tag, 18, tagColor, new Vector2(0.5f, 0.02f), new Vector2(0.92f, 0.1f));
        }

        private static TMP_Text AddText(Transform parent, string name, string text, int size, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = UiObject(name, parent, typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Math.Max(12, size - 8);
            tmp.fontSizeMax = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null)
            {
                tmp.font = font;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                tmp.fontSharedMaterial = material;
            }

            return tmp;
        }

        private static void AddEventSystem(Transform root)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(root, false);
        }

        private static void CreateShowcaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        }

        private static GameObject UiObject(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Ensure(string assetPath)
        {
            var current = string.Empty;
            foreach (var segment in assetPath.Split('/'))
            {
                var next = string.IsNullOrEmpty(current) ? segment : current + "/" + segment;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segment);
                }

                current = next;
            }
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private static Color C(byte r, byte g, byte b, byte a)
        {
            return new Color32(r, g, b, a);
        }
    }

    public sealed class LaunchOperatorVisualResourceSpec
    {
        public LaunchOperatorVisualResourceSpec(
            string operatorId,
            string displayLabel,
            string acquisition,
            string frameId,
            bool isPickup,
            string portraitPath,
            string cardPath,
            string iconPath,
            string framePath)
        {
            OperatorId = operatorId;
            DisplayLabel = displayLabel;
            Acquisition = acquisition;
            FrameId = frameId;
            IsPickup = isPickup;
            PortraitPath = portraitPath;
            CardPath = cardPath;
            IconPath = iconPath;
            FramePath = framePath;
        }

        public string OperatorId { get; }
        public string DisplayLabel { get; }
        public string Acquisition { get; }
        public string FrameId { get; }
        public bool IsPickup { get; }
        public string PortraitPath { get; }
        public string CardPath { get; }
        public string IconPath { get; }
        public string FramePath { get; }
    }
}
#endif
