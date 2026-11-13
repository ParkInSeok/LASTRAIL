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
    public static class LaunchSystemFeedbackResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string FeedbackRoot = UiRoot + "/SystemFeedbackResources";
        private const string LoadingRoot = FeedbackRoot + "/Loading";
        private const string BadgesRoot = FeedbackRoot + "/Badges";
        private const string NoticesRoot = FeedbackRoot + "/Notices";
        private const string RetryRoot = FeedbackRoot + "/Retry";
        private const string WarningRoot = FeedbackRoot + "/Warning";
        private const string DisabledRoot = FeedbackRoot + "/Disabled";
        private const string StatusRoot = FeedbackRoot + "/Status";
        private const string ShowcaseRoot = FeedbackRoot + "/Showcase";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-system-feedback-resources.json";
        private const string ReadmePath = FeedbackRoot + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchSystemFeedbackResourceShowcase.unity";
        public const string ShowcasePrefabPath = ShowcaseRoot + "/LRLaunchSystemFeedbackResourceShowcase.prefab";

        private static readonly Color32 Clear = C(0, 0, 0, 0);
        private static readonly Color32 Panel = C(7, 12, 16, 235);
        private static readonly Color32 PanelSoft = C(14, 23, 28, 228);
        private static readonly Color32 TrackDark = C(32, 40, 42, 255);
        private static readonly Color32 Steel = C(123, 145, 145, 255);
        private static readonly Color32 TextMain = C(238, 241, 234, 255);
        private static readonly Color32 TextMuted = C(172, 184, 184, 255);
        private static readonly Color32 Teal = C(58, 183, 191, 255);
        private static readonly Color32 Amber = C(232, 174, 76, 255);
        private static readonly Color32 Green = C(86, 194, 119, 255);
        private static readonly Color32 Red = C(219, 82, 72, 255);
        private static readonly Color32 Orange = C(226, 122, 54, 255);

        public static readonly LaunchSystemFeedbackResourceSpec[] Resources =
        {
            Spec("loading_rail_indicator", "loading", LoadingRoot + "/loading_rail_indicator.png", 768, 192, "loading_signal_display"),
            Spec("feedback_success_badge", "badge", BadgesRoot + "/feedback_success_badge.png", 512, 192, "success_status_display"),
            Spec("feedback_failure_badge", "badge", BadgesRoot + "/feedback_failure_badge.png", 512, 192, "failure_status_display"),
            Spec("feedback_network_error_badge", "error", BadgesRoot + "/feedback_network_error_badge.png", 512, 192, "connection_status_display"),
            Spec("maintenance_notice_panel", "notice", NoticesRoot + "/maintenance_notice_panel.png", 1024, 512, "maintenance_notice_display"),
            Spec("retry_refresh_accent", "retry", RetryRoot + "/retry_refresh_accent.png", 512, 512, "retry_refresh_display"),
            Spec("warning_strip_banner_accent", "warning", WarningRoot + "/warning_strip_banner_accent.png", 1024, 160, "warning_banner_display"),
            Spec("disabled_unavailable_overlay", "disabled", DisabledRoot + "/disabled_unavailable_overlay.png", 768, 512, "disabled_unavailable_display"),
            Spec("inline_status_dot_set", "status", StatusRoot + "/inline_status_dot_set.png", 512, 128, "inline_status_display")
        };

        [MenuItem("LASTRAIL/Launch/Build System Feedback Resource Foundation")]
        public static void BuildSystemFeedbackResources()
        {
            Ensure(FeedbackRoot);
            Ensure(LoadingRoot);
            Ensure(BadgesRoot);
            Ensure(NoticesRoot);
            Ensure(RetryRoot);
            Ensure(WarningRoot);
            Ensure(DisabledRoot);
            Ensure(StatusRoot);
            Ensure(ShowcaseRoot);
            Ensure(StyleRoot);
            Ensure(SceneRoot);

            foreach (var resource in Resources)
            {
                WriteResource(resource);
                ConfigureTexture(resource.Path);
            }

            WriteManifest();
            WriteReadme();
            AssetDatabase.Refresh();
            CreateShowcasePrefab();
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL system feedback visual resource foundation build complete.");
        }

        private static LaunchSystemFeedbackResourceSpec Spec(string id, string category, string path, int width, int height, string usage)
        {
            return new LaunchSystemFeedbackResourceSpec(id, category, path, width, height, usage);
        }

        private static void WriteResource(LaunchSystemFeedbackResourceSpec resource)
        {
            var texture = new Texture2D(resource.Width, resource.Height, TextureFormat.RGBA32, false);
            try
            {
                Fill(texture, Clear);
                switch (resource.Id)
                {
                    case "loading_rail_indicator":
                        DrawLoadingRailIndicator(texture);
                        break;
                    case "feedback_success_badge":
                        DrawBadge(texture, Green, Teal, true, false, false);
                        break;
                    case "feedback_failure_badge":
                        DrawBadge(texture, Red, Orange, false, true, false);
                        break;
                    case "feedback_network_error_badge":
                        DrawBadge(texture, Amber, Teal, false, false, true);
                        break;
                    case "maintenance_notice_panel":
                        DrawMaintenanceNoticePanel(texture);
                        break;
                    case "retry_refresh_accent":
                        DrawRetryRefreshAccent(texture);
                        break;
                    case "warning_strip_banner_accent":
                        DrawWarningStrip(texture);
                        break;
                    case "disabled_unavailable_overlay":
                        DrawDisabledOverlay(texture);
                        break;
                    case "inline_status_dot_set":
                        DrawInlineStatusDots(texture);
                        break;
                }

                texture.Apply();
                File.WriteAllBytes(ProjectPath(resource.Path), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void DrawLoadingRailIndicator(Texture2D t)
        {
            FillRect(t, 24, 40, t.width - 48, t.height - 80, Panel);
            Border(t, 24, 40, t.width - 48, t.height - 80, Steel, 3);
            FillRect(t, 82, 82, t.width - 164, 16, TrackDark);
            FillRect(t, 82, 116, t.width - 164, 16, TrackDark);
            for (var i = 0; i < 12; i++)
            {
                var x = 112 + i * 48;
                Line(t, x, 74, x + 34, 140, new Color32(99, 119, 119, 205), 5);
            }

            Circle(t, 134, 96, 26, Green);
            Circle(t, 384, 96, 26, Amber);
            Circle(t, 634, 96, 26, Teal);
            FillRect(t, 92, 152, t.width - 184, 7, Amber);
            FillRect(t, 92, 32, t.width - 184, 5, Teal);
        }

        private static void DrawBadge(Texture2D t, Color32 main, Color32 secondary, bool success, bool failure, bool connection)
        {
            FillRect(t, 40, 48, t.width - 80, t.height - 96, PanelSoft);
            Border(t, 40, 48, t.width - 80, t.height - 96, main, 5);
            FillRect(t, 64, 72, t.width - 128, 10, secondary);
            FillRect(t, 64, t.height - 86, t.width - 128, 10, main);
            Circle(t, 128, 96, 42, new Color32(main.r, main.g, main.b, 62));
            Circle(t, 128, 96, 28, main);

            if (success)
            {
                Line(t, 104, 96, 124, 76, TextMain, 8);
                Line(t, 124, 76, 160, 118, TextMain, 8);
                FillRect(t, 206, 90, 208, 12, new Color32(206, 229, 214, 215));
                FillRect(t, 206, 118, 144, 9, new Color32(124, 176, 145, 210));
            }
            else if (failure)
            {
                Line(t, 106, 74, 150, 118, TextMain, 8);
                Line(t, 150, 74, 106, 118, TextMain, 8);
                FillRect(t, 206, 90, 208, 12, new Color32(232, 202, 196, 215));
                FillRect(t, 206, 118, 122, 9, new Color32(194, 116, 102, 210));
            }
            else if (connection)
            {
                Line(t, 96, 114, 128, 72, TextMain, 7);
                Line(t, 128, 72, 160, 114, TextMain, 7);
                FillRect(t, 121, 112, 14, 22, Red);
                FillRect(t, 206, 90, 196, 12, new Color32(232, 218, 174, 215));
                FillRect(t, 206, 118, 166, 9, new Color32(93, 175, 181, 210));
            }
        }

        private static void DrawMaintenanceNoticePanel(Texture2D t)
        {
            FillRect(t, 48, 54, t.width - 96, t.height - 108, Panel);
            Border(t, 48, 54, t.width - 96, t.height - 108, Amber, 6);
            Border(t, 78, 86, t.width - 156, t.height - 172, Steel, 2);
            FillRect(t, 94, t.height - 116, t.width - 188, 18, Orange);
            for (var i = 0; i < 9; i++)
            {
                var x = 110 + i * 92;
                Line(t, x, 114, x + 62, 190, new Color32(52, 68, 70, 185), 6);
                FillRect(t, x - 16, 108, 80, 16, TrackDark);
            }

            FillRect(t, 380, 184, 264, 172, new Color32(18, 31, 34, 242));
            Border(t, 380, 184, 264, 172, Teal, 4);
            Circle(t, 512, 270, 72, new Color32(232, 174, 76, 74));
            FillRect(t, 498, 226, 28, 92, Amber);
            FillRect(t, 498, 184, 28, 28, Amber);
        }

        private static void DrawRetryRefreshAccent(Texture2D t)
        {
            for (var y = 0; y < t.height; y++)
            {
                for (var x = 0; x < t.width; x++)
                {
                    var dx = x - 256f;
                    var dy = y - 256f;
                    var d = Mathf.Sqrt(dx * dx + dy * dy) / 256f;
                    var a = Mathf.Clamp01(1f - d);
                    t.SetPixel(x, y, new Color(0.08f, 0.48f, 0.52f, a * a * 0.45f));
                }
            }

            Circle(t, 256, 256, 118, new Color32(9, 14, 18, 230));
            Border(t, 142, 142, 228, 228, Teal, 3);
            Line(t, 168, 274, 196, 204, Amber, 12);
            Line(t, 196, 204, 276, 168, Amber, 12);
            Line(t, 276, 168, 352, 214, Amber, 12);
            Line(t, 344, 188, 356, 216, Amber, 11);
            Line(t, 356, 216, 324, 222, Amber, 11);
            Line(t, 344, 238, 316, 308, Teal, 12);
            Line(t, 316, 308, 236, 344, Teal, 12);
            Line(t, 236, 344, 160, 298, Teal, 12);
            Line(t, 168, 324, 156, 296, Teal, 11);
            Line(t, 156, 296, 188, 290, Teal, 11);
        }

        private static void DrawWarningStrip(Texture2D t)
        {
            FillRect(t, 0, 0, t.width, t.height, new Color32(9, 12, 14, 235));
            Border(t, 12, 16, t.width - 24, t.height - 32, Amber, 4);
            for (var x = -120; x < t.width + 120; x += 92)
            {
                Line(t, x, 18, x + 108, t.height - 18, Orange, 20);
            }

            FillRect(t, 72, 62, t.width - 144, 36, new Color32(8, 13, 16, 205));
            FillRect(t, 92, 72, t.width - 184, 6, Teal);
            Circle(t, 96, 80, 28, Amber);
            Circle(t, t.width - 96, 80, 28, Red);
        }

        private static void DrawDisabledOverlay(Texture2D t)
        {
            FillRect(t, 32, 34, t.width - 64, t.height - 68, new Color32(7, 10, 12, 190));
            Border(t, 32, 34, t.width - 64, t.height - 68, Steel, 4);
            for (var x = -t.height; x < t.width + t.height; x += 74)
            {
                Line(t, x, 34, x + t.height, t.height - 34, new Color32(82, 96, 97, 130), 8);
            }

            FillRect(t, 226, 190, 316, 156, new Color32(14, 23, 28, 230));
            Border(t, 226, 190, 316, 156, Red, 5);
            FillRect(t, 286, 254, 196, 28, Steel);
            FillRect(t, 314, 224, 140, 24, Amber);
        }

        private static void DrawInlineStatusDots(Texture2D t)
        {
            FillRect(t, 24, 28, t.width - 48, t.height - 56, PanelSoft);
            Border(t, 24, 28, t.width - 48, t.height - 56, Steel, 2);
            var colors = new[] { Green, Amber, Red, Teal };
            for (var i = 0; i < colors.Length; i++)
            {
                var x = 88 + i * 112;
                Circle(t, x, 64, 24, new Color32(colors[i].r, colors[i].g, colors[i].b, 72));
                Circle(t, x, 64, 14, colors[i]);
                FillRect(t, x + 30, 60, 44, 8, new Color32(139, 158, 156, 170));
            }
        }

        private static void ConfigureTexture(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Missing system feedback texture importer: " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
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
            b.AppendLine("  \"name\": \"LAST RAIL Act 1 System Feedback Visual Resources\",");
            b.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            b.Append("  \"resourceCount\": ").Append(Resources.Length).AppendLine(",");
            b.AppendLine("  \"generatedBy\": \"Unity procedural texture builder\",");
            b.AppendLine("  \"presentationOnly\": true,");
            b.AppendLine("  \"serverContractChanged\": false,");
            b.AppendLine("  \"hasNetworkSourceOfTruth\": false,");
            b.AppendLine("  \"hasAuthSourceOfTruth\": false,");
            b.AppendLine("  \"hasErrorSourceOfTruth\": false,");
            b.AppendLine("  \"rewardCalculationIncluded\": false,");
            b.AppendLine("  \"economyMutationIncluded\": false,");
            b.AppendLine("  \"resources\": [");
            for (var i = 0; i < Resources.Length; i++)
            {
                var r = Resources[i];
                b.Append("    { \"id\": \"").Append(r.Id).Append("\", \"category\": \"").Append(r.Category).Append("\", \"path\": \"").Append(r.Path).Append("\", \"sourceType\": \"generated_local_launch_asset\" }");
                b.AppendLine(i == Resources.Length - 1 ? string.Empty : ",");
            }
            b.AppendLine("  ]");
            b.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), b.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Act 1 System Feedback Visual Resources

These loading, success, failure, connection, maintenance, retry, warning, disabled, and inline status files are Unity procedural texture builder outputs for the Act 1 launch UI.

Source:
- Generated locally inside the Unity Editor from `LaunchSystemFeedbackResourceBuilder`.
- No external paid asset dependency.

Scope:
- `loading_rail_indicator`
- `feedback_success_badge`
- `feedback_failure_badge`
- `feedback_network_error_badge`
- `maintenance_notice_panel`
- `retry_refresh_accent`
- `warning_strip_banner_accent`
- `disabled_unavailable_overlay`
- `inline_status_dot_set`

Usage:
- These are presentation resources only for launch UI loading, outcome, retry, maintenance, disabled, and inline status feedback.
- They do not decide backend state, authentication state, reward grants, economy changes, offline accumulation, story unlocks, gacha results, or combat results.
- The manifest at `Assets/_LASTRAIL/UI/Style/lastrail-system-feedback-resources.json` is the stable QA/readiness reference.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchSystemFeedbackResourceShowcase");
            AddBackground(root.transform);
            AddHeader(root.transform);
            AddLoadingPanel(root.transform);
            AddOutcomePanel(root.transform);
            AddNoticePanel(root.transform);
            AddEventSystem(root.transform);
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
            var image = UiObject("SystemFeedbackShowcaseBackdrop", root, typeof(Image)).GetComponent<Image>();
            Stretch(image.GetComponent<RectTransform>());
            image.color = C(5, 8, 12, 255);
        }

        private static void AddHeader(Transform root)
        {
            var panel = PanelObject("HeaderPanel", root, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.975f));
            AddText(panel.transform, "Title", "ACT 1 SYSTEM FEEDBACK UI", 31, TextMain, new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.9f), TextAlignmentOptions.MidlineLeft);
            AddText(panel.transform, "Subtitle", "loading / success / failure / retry / maintenance / disabled", 21, TextMuted, new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.42f), TextAlignmentOptions.MidlineLeft);
        }

        private static void AddLoadingPanel(Transform root)
        {
            var panel = PanelObject("LoadingSignalPanel", root, new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.885f));
            AddText(panel.transform, "PanelTitle", "로딩 / 신호 대기", 25, Amber, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.96f), TextAlignmentOptions.MidlineLeft);
            AddImage(panel.transform, "LoadingRailIndicator", Resources[0], new Vector2(0.06f, 0.2f), new Vector2(0.94f, 0.72f));
            AddText(panel.transform, "LoadingHint", "safe-area loading slot / mission start feedback", 18, TextMuted, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.18f), TextAlignmentOptions.MidlineRight);
        }

        private static void AddOutcomePanel(Transform root)
        {
            var panel = PanelObject("OutcomePanel", root, new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.635f));
            AddText(panel.transform, "PanelTitle", "결과 / 오류 / 재시도", 25, Teal, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.96f), TextAlignmentOptions.MidlineLeft);
            AddImage(panel.transform, "SuccessBadge", Resources[1], new Vector2(0.06f, 0.58f), new Vector2(0.38f, 0.78f));
            AddImage(panel.transform, "FailureBadge", Resources[2], new Vector2(0.34f, 0.58f), new Vector2(0.66f, 0.78f));
            AddImage(panel.transform, "NetworkBadge", Resources[3], new Vector2(0.62f, 0.58f), new Vector2(0.94f, 0.78f));
            AddImage(panel.transform, "RetryAccent", Resources[5], new Vector2(0.08f, 0.1f), new Vector2(0.3f, 0.48f));
            AddImage(panel.transform, "WarningStrip", Resources[6], new Vector2(0.34f, 0.24f), new Vector2(0.94f, 0.42f));
            AddImage(panel.transform, "InlineStatusDots", Resources[8], new Vector2(0.36f, 0.08f), new Vector2(0.82f, 0.2f));
            AddText(panel.transform, "OutcomeHint", "display cues only / presenter consumes existing status", 18, TextMuted, new Vector2(0.34f, 0.42f), new Vector2(0.94f, 0.52f), TextAlignmentOptions.MidlineRight);
        }

        private static void AddNoticePanel(Transform root)
        {
            var panel = PanelObject("MaintenanceDisabledPanel", root, new Vector2(0.05f, 0.045f), new Vector2(0.95f, 0.315f));
            AddText(panel.transform, "PanelTitle", "점검 / 비활성", 25, Orange, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.96f), TextAlignmentOptions.MidlineLeft);
            AddImage(panel.transform, "MaintenanceNoticePanel", Resources[4], new Vector2(0.05f, 0.1f), new Vector2(0.58f, 0.78f));
            AddImage(panel.transform, "DisabledOverlay", Resources[7], new Vector2(0.56f, 0.15f), new Vector2(0.94f, 0.74f));
            AddText(panel.transform, "NoticeHint", "maintenance / unavailable visual slots", 18, TextMuted, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.1f), TextAlignmentOptions.MidlineRight);
        }

        private static GameObject PanelObject(string name, Transform root, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = UiObject(name, root, typeof(Image));
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color32(8, 12, 16, 226);
            return panel;
        }

        private static Image AddImage(Transform parent, string name, LaunchSystemFeedbackResourceSpec spec, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = UiObject(name, parent, typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.Path);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text AddText(Transform parent, string name, string text, int size, Color color, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment)
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
            tmp.fontSizeMin = Math.Max(10, size - 8);
            tmp.fontSizeMax = size;
            tmp.enableAutoSizing = true;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) tmp.font = font;
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) tmp.fontSharedMaterial = material;
            return tmp;
        }

        private static void AddEventSystem(Transform root)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(root, false);
        }

        private static void CreateShowcaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath), scene);
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

        private static void Fill(Texture2D t, Color32 color)
        {
            for (var y = 0; y < t.height; y++)
            {
                for (var x = 0; x < t.width; x++)
                {
                    t.SetPixel(x, y, color);
                }
            }
        }

        private static void FillRect(Texture2D t, int x, int y, int width, int height, Color32 color)
        {
            var xMin = Mathf.Clamp(x, 0, t.width);
            var yMin = Mathf.Clamp(y, 0, t.height);
            var xMax = Mathf.Clamp(x + width, 0, t.width);
            var yMax = Mathf.Clamp(y + height, 0, t.height);
            for (var yy = yMin; yy < yMax; yy++)
            {
                for (var xx = xMin; xx < xMax; xx++)
                {
                    t.SetPixel(xx, yy, color);
                }
            }
        }

        private static void Border(Texture2D t, int x, int y, int width, int height, Color32 color, int thickness)
        {
            FillRect(t, x, y, width, thickness, color);
            FillRect(t, x, y + height - thickness, width, thickness, color);
            FillRect(t, x, y, thickness, height, color);
            FillRect(t, x + width - thickness, y, thickness, height, color);
        }

        private static void Circle(Texture2D t, int cx, int cy, int radius, Color32 color)
        {
            var rr = radius * radius;
            for (var y = cy - radius; y <= cy + radius; y++)
            {
                for (var x = cx - radius; x <= cx + radius; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    if (dx * dx + dy * dy <= rr && x >= 0 && y >= 0 && x < t.width && y < t.height)
                    {
                        t.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void Line(Texture2D t, int x0, int y0, int x1, int y1, Color32 color, int thickness)
        {
            var dx = Mathf.Abs(x1 - x0);
            var dy = Mathf.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;
            while (true)
            {
                FillRect(t, x0 - thickness / 2, y0 - thickness / 2, thickness, thickness, color);
                if (x0 == x1 && y0 == y1) break;
                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void Ensure(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var name = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) Ensure(parent);
            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private static Color32 C(byte r, byte g, byte b, byte a)
        {
            return new Color32(r, g, b, a);
        }
    }

    public sealed class LaunchSystemFeedbackResourceSpec
    {
        public LaunchSystemFeedbackResourceSpec(string id, string category, string path, int width, int height, string intendedUsage)
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
}
#endif
