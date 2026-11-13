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
    public static class LaunchStoryProgressPolishResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string Root = UiRoot + "/StoryProgressResources";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-story-progress-polish-resources.json";
        private const string ReadmePath = Root + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchStoryProgressPolishShowcase.unity";

        public const string ShowcasePrefabPath = Root + "/Showcase/LRLaunchStoryProgressPolishShowcase.prefab";
        public const string SourceAtlasPath = Root + "/Sources/story_progress_polish_hifi_v1_source.png";

        public static readonly StoryProgressPolishResourceSpec[] Resources =
        {
            Resource("story_progress_timeline_panel", "Panels", 1024, 512),
            Resource("story_progress_route_spine", "Frames", 1024, 160),
            Resource("story_progress_current_node_glow", "Badges", 256, 256),
            Resource("story_progress_cleared_node_seal", "Badges", 256, 256),
            Resource("story_progress_locked_node_mist", "Overlays", 512, 256),
            Resource("story_progress_raid_gate_badge", "Badges", 512, 256),
            Resource("story_progress_narrative_status_ribbon", "Badges", 512, 160),
            Resource("story_progress_codex_unlock_hint_panel", "Panels", 1024, 384),
            Resource("story_progress_bottom_nav_accent", "Buttons", 512, 160)
        };

        [MenuItem("LASTRAIL/Launch/Build StoryProgress Polish Resources")]
        public static void BuildStoryProgressPolishResources()
        {
            Ensure(Root + "/Sources");
            Ensure(Root + "/Panels");
            Ensure(Root + "/Frames");
            Ensure(Root + "/Badges");
            Ensure(Root + "/Overlays");
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
            Debug.Log("LASTRAIL StoryProgress polish resources build complete.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static StoryProgressPolishResourceSpec Resource(string id, string folder, int width, int height)
        {
            return new StoryProgressPolishResourceSpec(id, folder, Root + "/" + folder + "/" + id + ".png", width, height);
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
            b.AppendLine("  \"name\": \"LAST RAIL Act 1 StoryProgress Polish Resources\",");
            b.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            b.AppendLine("  \"sourceType\": \"OpenAI image generation high-quality 2D local launch UI atlas\",");
            b.Append("  \"sourcePath\": \"").Append(SourceAtlasPath).AppendLine("\",");
            b.AppendLine("  \"resourceCount\": 9,");
            b.AppendLine("  \"presentationOnly\": true,");
            b.AppendLine("  \"serverContractChanged\": false,");
            b.AppendLine("  \"storyProgressTruthIncluded\": false,");
            b.AppendLine("  \"archiveUnlockTruthIncluded\": false,");
            b.AppendLine("  \"resources\": [");
            for (var i = 0; i < Resources.Length; i++)
            {
                var resource = Resources[i];
                b.Append("    { \"id\": \"").Append(resource.Id).Append("\", ");
                b.Append("\"path\": \"").Append(resource.Path).Append("\", ");
                b.Append("\"width\": ").Append(resource.Width).Append(", ");
                b.Append("\"height\": ").Append(resource.Height).Append(", ");
                b.Append("\"intendedUsage\": \"story_progress_visual_skin\" }");
                b.AppendLine(i == Resources.Length - 1 ? string.Empty : ",");
            }

            b.AppendLine("  ]");
            b.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), b.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Act 1 StoryProgress Polish Resources

These files are high-quality 2D local launch UI assets for the StoryProgressSummary surface.

## Source And Usage

Source: OpenAI image generation atlas copied into `Sources/story_progress_polish_hifi_v1_source.png`, then cropped into Unity-ready Sprite PNG files.

License note: no external paid or third-party art asset was introduced by this slice.

Use: visual-only story route panel, progress spine, node states, raid gate badge, narrative ribbon, codex hint panel, and bottom navigation accent for the 1080x1920 portrait StoryProgressSummary flow.

Boundary: this resource set does not change server contracts, story progress truth, archive unlock truth, rewards, economy, offline rewards, gacha odds, or ledger behavior.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchStoryProgressPolishShowcase");
            AddBackground(root.transform);
            AddText(root.transform, "Title", "STORY PROGRESS POLISH", 38, new Color32(238, 241, 234, 255), new Vector2(900, 72), new Vector2(0, 820));
            AddText(root.transform, "Subtitle", "route panel / story nodes / raid gate / codex hint", 22, new Color32(170, 184, 184, 255), new Vector2(900, 52), new Vector2(0, 760));

            AddSprite(root.transform, "TimelinePanel", "story_progress_timeline_panel", new Vector2(0, 510), new Vector2(820, 410));
            AddSprite(root.transform, "RouteSpine", "story_progress_route_spine", new Vector2(0, 205), new Vector2(820, 128));
            AddText(root.transform, "RouteLabel", "seven-node Act 1 route frame", 18, new Color32(243, 179, 72, 255), new Vector2(720, 38), new Vector2(0, 112));
            AddSprite(root.transform, "CurrentNodeGlow", "story_progress_current_node_glow", new Vector2(-290, -40), new Vector2(150, 150));
            AddSprite(root.transform, "ClearedNodeSeal", "story_progress_cleared_node_seal", new Vector2(-100, -40), new Vector2(138, 138));
            AddSprite(root.transform, "LockedNodeMist", "story_progress_locked_node_mist", new Vector2(210, -40), new Vector2(360, 180));
            AddText(root.transform, "NodeLabel", "current / cleared / locked state visuals", 18, new Color32(69, 197, 213, 255), new Vector2(720, 38), new Vector2(0, -175));
            AddSprite(root.transform, "RaidGateBadge", "story_progress_raid_gate_badge", new Vector2(-240, -405), new Vector2(370, 185));
            AddSprite(root.transform, "NarrativeStatusRibbon", "story_progress_narrative_status_ribbon", new Vector2(230, -340), new Vector2(380, 118));
            AddSprite(root.transform, "CodexHintPanel", "story_progress_codex_unlock_hint_panel", new Vector2(230, -535), new Vector2(400, 150));
            AddSprite(root.transform, "BottomNav", "story_progress_bottom_nav_accent", new Vector2(0, -735), new Vector2(430, 134));
            AddText(root.transform, "Footer", "visual-only / server progress remains source-owned", 20, new Color32(170, 184, 184, 255), new Vector2(900, 44), new Vector2(0, -850));
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

            throw new FileNotFoundException("Unknown StoryProgress polish resource: " + resourceId);
        }

        private static void AddBackground(Transform parent)
        {
            var image = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = new Color32(2, 11, 11, 255);
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
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = 12f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
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
                throw new FileNotFoundException("Missing StoryProgress polish sprite: " + path);
            }

            return sprite;
        }

        private static void Ensure(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
                var name = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                {
                    Ensure(parent);
                }

                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }
    }

    public readonly struct StoryProgressPolishResourceSpec
    {
        public StoryProgressPolishResourceSpec(string id, string folder, string path, int width, int height)
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
