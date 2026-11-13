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
    public static class LaunchCodexArchivePolishResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string Root = UiRoot + "/CodexArchiveResources";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-codex-archive-polish-resources.json";
        private const string ReadmePath = Root + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchCodexArchivePolishShowcase.unity";

        public const string ShowcasePrefabPath = Root + "/Showcase/LRLaunchCodexArchivePolishShowcase.prefab";
        public const string SourceAtlasPath = Root + "/Sources/codex_archive_polish_hifi_v1_source.png";

        public static readonly CodexArchivePolishResourceSpec[] Resources =
        {
            Resource("codex_archive_main_panel", "Panels", 1024, 1024),
            Resource("codex_archive_entry_card_frame", "Frames", 512, 384),
            Resource("codex_archive_entry_selected_frame", "Frames", 512, 384),
            Resource("codex_archive_record_detail_panel", "Panels", 1024, 512),
            Resource("codex_archive_unlock_seal", "Badges", 256, 256),
            Resource("codex_archive_locked_seal", "Badges", 256, 256),
            Resource("codex_archive_lore_tag", "Badges", 512, 160),
            Resource("codex_archive_scroll_edge", "Frames", 1024, 128),
            Resource("codex_archive_bottom_nav_accent", "Buttons", 512, 160)
        };

        [MenuItem("LASTRAIL/Launch/Build CodexArchive Polish Resources")]
        public static void BuildCodexArchivePolishResources()
        {
            Ensure(Root + "/Sources");
            Ensure(Root + "/Panels");
            Ensure(Root + "/Frames");
            Ensure(Root + "/Badges");
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
            Debug.Log("LASTRAIL CodexArchive polish resources build complete.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static CodexArchivePolishResourceSpec Resource(string id, string folder, int width, int height)
        {
            return new CodexArchivePolishResourceSpec(id, folder, Root + "/" + folder + "/" + id + ".png", width, height);
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
            b.AppendLine("  \"name\": \"LAST RAIL Act 1 CodexArchive Polish Resources\",");
            b.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            b.AppendLine("  \"sourceType\": \"OpenAI image generation high-quality 2D local launch UI atlas\",");
            b.AppendLine("  \"sourcePath\": \"").Append(SourceAtlasPath).AppendLine("\",");
            b.AppendLine("  \"resourceCount\": 9,");
            b.AppendLine("  \"presentationOnly\": true,");
            b.AppendLine("  \"serverContractChanged\": false,");
            b.AppendLine("  \"archiveUnlockIncluded\": false,");
            b.AppendLine("  \"storyProgressIncluded\": false,");
            b.AppendLine("  \"resources\": [");
            for (var i = 0; i < Resources.Length; i++)
            {
                var resource = Resources[i];
                b.Append("    { \"id\": \"").Append(resource.Id).Append("\", ");
                b.Append("\"path\": \"").Append(resource.Path).Append("\", ");
                b.Append("\"width\": ").Append(resource.Width).Append(", ");
                b.Append("\"height\": ").Append(resource.Height).Append(", ");
                b.Append("\"intendedUsage\": \"codex_archive_visual_skin\" }");
                b.AppendLine(i == Resources.Length - 1 ? string.Empty : ",");
            }

            b.AppendLine("  ]");
            b.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), b.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Act 1 CodexArchive Polish Resources

These files are high-quality 2D local launch UI assets for the CodexArchive records surface.

## Source And Usage

Source: OpenAI image generation atlas copied into `Sources/codex_archive_polish_hifi_v1_source.png`, then cropped into Unity-ready Sprite PNG files.

License note: no external paid or third-party art asset was introduced by this slice.

Use: visual-only archive cabinet panels, entry frames, seals, lore tags, scroll dividers, and bottom navigation accents for the 1080x1920 portrait CodexArchive flow.

Boundary: this resource set does not change server contracts, story progress, archive unlock truth, rewards, economy, offline rewards, gacha odds, or ledger behavior.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchCodexArchivePolishShowcase");
            AddBackground(root.transform);
            AddText(root.transform, "Title", "CODEX ARCHIVE POLISH", 40, new Color32(238, 241, 234, 255), new Vector2(900, 72), new Vector2(0, 820));
            AddText(root.transform, "Subtitle", "records cabinet / entry cards / lore seals / navigation accent", 22, new Color32(170, 184, 184, 255), new Vector2(900, 52), new Vector2(0, 760));

            AddSprite(root.transform, "MainPanel", "codex_archive_main_panel", new Vector2(-210, 330), new Vector2(520, 520));
            AddSprite(root.transform, "DetailPanel", "codex_archive_record_detail_panel", new Vector2(190, 310), new Vector2(520, 260));
            AddSprite(root.transform, "EntryFrame", "codex_archive_entry_card_frame", new Vector2(-190, -110), new Vector2(270, 202));
            AddSprite(root.transform, "SelectedEntryFrame", "codex_archive_entry_selected_frame", new Vector2(170, -110), new Vector2(270, 202));
            AddText(root.transform, "EntryFrameLabel", "archive entry frame states", 18, new Color32(243, 179, 72, 255), new Vector2(620, 38), new Vector2(0, -255));
            AddSprite(root.transform, "UnlockSeal", "codex_archive_unlock_seal", new Vector2(-180, -360), new Vector2(120, 120));
            AddSprite(root.transform, "LockedSeal", "codex_archive_locked_seal", new Vector2(0, -360), new Vector2(120, 120));
            AddSprite(root.transform, "LoreTag", "codex_archive_lore_tag", new Vector2(220, -360), new Vector2(300, 94));
            AddText(root.transform, "SealLabel", "unlock seal / locked seal / lore tag", 18, new Color32(69, 197, 213, 255), new Vector2(620, 38), new Vector2(0, -455));
            AddSprite(root.transform, "ScrollEdge", "codex_archive_scroll_edge", new Vector2(0, -535), new Vector2(720, 86));
            AddSprite(root.transform, "BottomNav", "codex_archive_bottom_nav_accent", new Vector2(0, -690), new Vector2(430, 134));
            AddText(root.transform, "Footer", "visual-only / TMP Pretendard / Input System / no archive state embedded", 20, new Color32(170, 184, 184, 255), new Vector2(900, 44), new Vector2(0, -840));
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

            throw new FileNotFoundException("Unknown CodexArchive polish resource: " + resourceId);
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

    public readonly struct CodexArchivePolishResourceSpec
    {
        public CodexArchivePolishResourceSpec(string id, string folder, string path, int width, int height)
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
