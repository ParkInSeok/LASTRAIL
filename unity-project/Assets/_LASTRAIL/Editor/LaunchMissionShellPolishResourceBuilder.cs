#if UNITY_EDITOR
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
    public static class LaunchMissionShellPolishResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string Root = UiRoot + "/MissionShellResources";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-mission-shell-polish-resources.json";
        private const string ReadmePath = Root + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchMissionShellPolishShowcase.unity";

        public const string ShowcasePrefabPath = Root + "/Showcase/LRLaunchMissionShellPolishShowcase.prefab";
        public const string SourceAtlasPath = Root + "/Sources/mission_shell_polish_hifi_v1_source.png";

        public static readonly MissionShellPolishResourceSpec[] Resources =
        {
            Resource("mission_operation_card_frame", "Frames", 512, 384),
            Resource("mission_operation_card_selected_frame", "Frames", 512, 384),
            Resource("mission_briefing_panel", "Panels", 1024, 512),
            Resource("mission_objective_panel", "Panels", 1024, 384),
            Resource("mission_objective_plate", "Badges", 512, 160),
            Resource("mission_wave_badge", "Badges", 256, 256),
            Resource("mission_risk_badge", "Badges", 256, 256),
            Resource("mission_first_clear_badge", "Badges", 256, 256),
            Resource("mission_progress_rail_strip", "Progress", 1024, 192),
            Resource("mission_start_button_accent", "Buttons", 512, 160),
            Resource("mission_back_button_accent", "Buttons", 384, 192)
        };

        [MenuItem("LASTRAIL/Launch/Build MissionShell Polish Resources")]
        public static void BuildMissionShellPolishResources()
        {
            Ensure(Root + "/Sources");
            Ensure(Root + "/Frames");
            Ensure(Root + "/Panels");
            Ensure(Root + "/Badges");
            Ensure(Root + "/Progress");
            Ensure(Root + "/Buttons");
            Ensure(Root + "/Showcase");
            Ensure(StyleRoot);
            Ensure(SceneRoot);

            ConfigureTexture(SourceAtlasPath, false);
            foreach (var resource in Resources)
            {
                ConfigureTexture(resource.Path, true);
            }

            WriteManifest();
            WriteReadme();
            CreateShowcasePrefab();
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL MissionShell polish resources build complete.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static MissionShellPolishResourceSpec Resource(string id, string folder, int width, int height)
        {
            return new MissionShellPolishResourceSpec(id, folder, Root + "/" + folder + "/" + id + ".png", width, height);
        }

        private static void ConfigureTexture(string assetPath, bool alpha)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Missing texture importer: " + assetPath);
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
            b.AppendLine("  \"name\": \"LAST RAIL Act 1 MissionShell Polish Resources\",");
            b.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            b.AppendLine("  \"sourceType\": \"OpenAI image generation high-quality 2D local launch UI atlas\",");
            b.AppendLine("  \"sourcePath\": \"").Append(SourceAtlasPath).AppendLine("\",");
            b.AppendLine("  \"resourceCount\": 11,");
            b.AppendLine("  \"presentationOnly\": true,");
            b.AppendLine("  \"serverContractChanged\": false,");
            b.AppendLine("  \"progressionIncluded\": false,");
            b.AppendLine("  \"operationAccessIncluded\": false,");
            b.AppendLine("  \"rewardCalculationIncluded\": false,");
            b.AppendLine("  \"resources\": [");
            for (var i = 0; i < Resources.Length; i++)
            {
                var resource = Resources[i];
                b.Append("    { \"id\": \"").Append(resource.Id).Append("\", ");
                b.Append("\"path\": \"").Append(resource.Path).Append("\", ");
                b.Append("\"width\": ").Append(resource.Width).Append(", ");
                b.Append("\"height\": ").Append(resource.Height).Append(", ");
                b.Append("\"intendedUsage\": \"mission_shell_operation_selection_visual_skin\" }");
                b.AppendLine(i == Resources.Length - 1 ? string.Empty : ",");
            }

            b.AppendLine("  ]");
            b.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), b.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Act 1 MissionShell Polish Resources

These files are high-quality 2D local launch UI assets for the MissionShell operation selection surface.

## Source And Usage

Source: OpenAI image generation atlas copied into `Sources/mission_shell_polish_hifi_v1_source.png`, then cropped into Unity-ready Sprite PNG files.

License note: no external paid or third-party art asset was introduced by this slice.

Use: visual-only frames, panels, badges, progress rail, and button accents for the 1080x1920 portrait MissionShell operation selection flow.

Boundary: this resource set does not change server contracts, gameplay progression, operation access, first-clear policy, rewards, economy, offline rewards, gacha odds, or ledger behavior.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchMissionShellPolishShowcase");
            AddBackground(root.transform);
            AddText(root.transform, "Title", "MISSION SHELL POLISH", 40, new Color32(238, 241, 234, 255), new Vector2(900, 72), new Vector2(0, 820));
            AddText(root.transform, "Subtitle", "operation cards / briefing panels / status badges / button accents", 22, new Color32(170, 184, 184, 255), new Vector2(900, 52), new Vector2(0, 760));

            AddSprite(root.transform, "BriefingPanel", "mission_briefing_panel", new Vector2(0, 540), new Vector2(900, 300));
            AddSprite(root.transform, "ObjectivePanel", "mission_objective_panel", new Vector2(0, 250), new Vector2(900, 220));
            AddText(root.transform, "PanelLabel", "briefing + objective panels", 18, new Color32(238, 190, 92, 255), new Vector2(900, 36), new Vector2(0, 690));
            AddSprite(root.transform, "ObjectivePlate", "mission_objective_plate", new Vector2(-250, 390), new Vector2(300, 94));
            AddSprite(root.transform, "WaveBadge", "mission_wave_badge", new Vector2(260, 388), new Vector2(92, 92));
            AddSprite(root.transform, "RiskBadge", "mission_risk_badge", new Vector2(365, 388), new Vector2(92, 92));
            AddSprite(root.transform, "FirstClearBadge", "mission_first_clear_badge", new Vector2(365, 160), new Vector2(96, 96));

            AddSprite(root.transform, "ProgressRail", "mission_progress_rail_strip", new Vector2(0, -15), new Vector2(860, 140));
            AddText(root.transform, "RailLabel", "progress rail + status badges", 18, new Color32(88, 205, 210, 255), new Vector2(900, 36), new Vector2(0, 95));
            AddSprite(root.transform, "CardFrame", "mission_operation_card_frame", new Vector2(-180, -260), new Vector2(260, 194));
            AddSprite(root.transform, "SelectedCardFrame", "mission_operation_card_selected_frame", new Vector2(180, -260), new Vector2(260, 194));
            AddText(root.transform, "CardLabel", "operation card frames", 18, new Color32(238, 190, 92, 255), new Vector2(900, 36), new Vector2(0, -125));
            AddSprite(root.transform, "StartButton", "mission_start_button_accent", new Vector2(-190, -540), new Vector2(380, 118));
            AddSprite(root.transform, "BackButton", "mission_back_button_accent", new Vector2(210, -540), new Vector2(270, 136));
            AddText(root.transform, "ButtonLabel", "start + back accents", 18, new Color32(88, 205, 210, 255), new Vector2(900, 36), new Vector2(0, -415));
            AddText(root.transform, "Footer", "visual-only / TMP Pretendard / Input System / no gameplay state embedded", 20, new Color32(170, 184, 184, 255), new Vector2(900, 44), new Vector2(0, -835));
            AddEventSystem(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, ShowcasePrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateShowcaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        }

        private static void AddSprite(Transform parent, string name, string resourceId, Vector2 position, Vector2 size)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = LoadSprite(ResourcePath(resourceId));
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static string ResourcePath(string resourceId)
        {
            foreach (var resource in Resources)
            {
                if (resource.Id == resourceId)
                {
                    return resource.Path;
                }
            }

            throw new FileNotFoundException("Unknown MissionShell polish resource: " + resourceId);
        }

        private static void AddBackground(Transform parent)
        {
            var image = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = new Color32(2, 12, 13, 255);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddText(Transform parent, string name, string value, float size, Color32 color, Vector2 dimensions, Vector2 position)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null)
            {
                text.font = font;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                text.fontSharedMaterial = material;
            }

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
        }

        private static GameObject CreateCanvasRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            return root;
        }

        private static void AddEventSystem(Transform parent)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(parent, false);
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new FileNotFoundException("Missing sprite: " + path);
            }

            return sprite;
        }

        private static void Ensure(string assetPath)
        {
            Directory.CreateDirectory(ProjectPath(assetPath));
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }
    }

    public readonly struct MissionShellPolishResourceSpec
    {
        public MissionShellPolishResourceSpec(string id, string folder, string path, int width, int height)
        {
            Id = id;
            Folder = folder;
            Path = path;
            Width = width;
            Height = height;
        }

        public string Id { get; }
        public string Folder { get; }
        public string Path { get; }
        public int Width { get; }
        public int Height { get; }
    }
}
#endif
