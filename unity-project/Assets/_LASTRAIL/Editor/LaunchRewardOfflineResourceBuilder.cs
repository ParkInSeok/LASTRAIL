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
    public static class LaunchRewardOfflineResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string RewardOfflineRoot = UiRoot + "/RewardOfflineResources";
        private const string FramesRoot = RewardOfflineRoot + "/Frames";
        private const string BadgesRoot = RewardOfflineRoot + "/Badges";
        private const string MetersRoot = RewardOfflineRoot + "/Meters";
        private const string EmptyRoot = RewardOfflineRoot + "/Empty";
        private const string AccentsRoot = RewardOfflineRoot + "/Accents";
        private const string ShowcaseRoot = RewardOfflineRoot + "/Showcase";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-reward-offline-resources.json";
        private const string ReadmePath = RewardOfflineRoot + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchRewardOfflineResourceShowcase.unity";
        public const string ShowcasePrefabPath = ShowcaseRoot + "/LRLaunchRewardOfflineResourceShowcase.prefab";

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Panel = C(8, 12, 16, 238);
        private static readonly Color32 PanelSoft = C(15, 23, 28, 225);
        private static readonly Color32 Amber = C(232, 174, 76, 255);
        private static readonly Color32 Copper = C(188, 83, 48, 255);
        private static readonly Color32 Teal = C(64, 183, 190, 255);
        private static readonly Color32 Green = C(90, 192, 116, 255);
        private static readonly Color32 Steel = C(126, 148, 151, 255);
        private static readonly Color32 TextMain = C(238, 241, 234, 255);
        private static readonly Color32 TextMuted = C(172, 184, 184, 255);

        public static readonly LaunchRewardOfflineResourceSpec[] Resources =
        {
            Spec("reward_result_plate", "frame", FramesRoot + "/reward_result_plate.png", 1024, 512, "reward_result_backing"),
            Spec("reward_line_item_frame", "frame", FramesRoot + "/reward_line_item_frame.png", 1024, 160, "reward_line_display"),
            Spec("claim_success_badge", "badge", BadgesRoot + "/claim_success_badge.png", 512, 192, "claim_success_display"),
            Spec("claim_pending_claimable_accent", "accent", BadgesRoot + "/claim_pending_claimable_accent.png", 512, 192, "claimable_status_display"),
            Spec("offline_settlement_meter", "meter", MetersRoot + "/offline_settlement_meter.png", 1024, 256, "offline_settlement_display"),
            Spec("offline_duration_accumulation_strip", "meter", MetersRoot + "/offline_duration_accumulation_strip.png", 1024, 192, "offline_duration_display"),
            Spec("offline_empty_state_visual", "empty", EmptyRoot + "/offline_empty_state_visual.png", 768, 512, "offline_empty_state_display"),
            Spec("reward_chest_supply_crate_accent", "accent", AccentsRoot + "/reward_chest_supply_crate_accent.png", 512, 512, "reward_container_accent"),
            Spec("reward_material_glow", "accent", AccentsRoot + "/reward_material_glow.png", 512, 512, "material_highlight_accent")
        };

        [MenuItem("LASTRAIL/Launch/Build Reward Offline Resource Foundation")]
        public static void BuildRewardOfflineResources()
        {
            Ensure(RewardOfflineRoot);
            Ensure(FramesRoot);
            Ensure(BadgesRoot);
            Ensure(MetersRoot);
            Ensure(EmptyRoot);
            Ensure(AccentsRoot);
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
            Debug.Log("LASTRAIL reward/offline visual resource foundation build complete.");
        }

        private static LaunchRewardOfflineResourceSpec Spec(string id, string category, string path, int width, int height, string usage)
        {
            return new LaunchRewardOfflineResourceSpec(id, category, path, width, height, usage);
        }

        private static void WriteResource(LaunchRewardOfflineResourceSpec resource)
        {
            var texture = new Texture2D(resource.Width, resource.Height, TextureFormat.RGBA32, false);
            try
            {
                Fill(texture, Clear);
                switch (resource.Id)
                {
                    case "reward_result_plate":
                        DrawRewardResultPlate(texture);
                        break;
                    case "reward_line_item_frame":
                        DrawRewardLineItemFrame(texture);
                        break;
                    case "claim_success_badge":
                        DrawClaimBadge(texture, Green, Teal, true);
                        break;
                    case "claim_pending_claimable_accent":
                        DrawClaimBadge(texture, Amber, Copper, false);
                        break;
                    case "offline_settlement_meter":
                        DrawOfflineSettlementMeter(texture);
                        break;
                    case "offline_duration_accumulation_strip":
                        DrawOfflineDurationStrip(texture);
                        break;
                    case "offline_empty_state_visual":
                        DrawOfflineEmptyState(texture);
                        break;
                    case "reward_chest_supply_crate_accent":
                        DrawRewardChestAccent(texture);
                        break;
                    case "reward_material_glow":
                        DrawMaterialGlow(texture);
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

        private static void DrawRewardResultPlate(Texture2D t)
        {
            FillRect(t, 40, 46, t.width - 80, t.height - 92, Panel);
            Border(t, 40, 46, t.width - 80, t.height - 92, Amber, 7);
            Border(t, 62, 70, t.width - 124, t.height - 140, Steel, 2);
            FillRect(t, 70, t.height - 118, t.width - 140, 8, Copper);
            FillRect(t, 70, 96, t.width - 140, 6, Teal);
            for (var i = 0; i < 7; i++)
            {
                var x = 105 + i * 118;
                FillRect(t, x, 132, 52, 16, new Color32(74, 91, 90, 180));
                Line(t, x - 18, 166, x + 76, 222, new Color32(39, 59, 62, 160), 3);
            }
        }

        private static void DrawRewardLineItemFrame(Texture2D t)
        {
            FillRect(t, 18, 20, t.width - 36, t.height - 40, PanelSoft);
            Border(t, 18, 20, t.width - 36, t.height - 40, Steel, 3);
            FillRect(t, 48, 38, 94, 84, new Color32(18, 31, 34, 245));
            Border(t, 48, 38, 94, 84, Teal, 2);
            FillRect(t, 172, 62, 560, 12, new Color32(93, 112, 110, 160));
            FillRect(t, 172, 92, 390, 10, new Color32(55, 72, 72, 180));
            FillRect(t, t.width - 194, 54, 132, 48, new Color32(201, 145, 58, 210));
            Border(t, t.width - 194, 54, 132, 48, Amber, 2);
        }

        private static void DrawClaimBadge(Texture2D t, Color32 main, Color32 secondary, bool success)
        {
            FillRect(t, 38, 46, t.width - 76, t.height - 92, new Color32(8, 12, 16, 235));
            Border(t, 38, 46, t.width - 76, t.height - 92, main, 5);
            FillRect(t, 58, 68, t.width - 116, 12, secondary);
            FillRect(t, 58, t.height - 84, t.width - 116, 12, main);
            if (success)
            {
                Line(t, 160, 105, 225, 64, Green, 14);
                Line(t, 225, 64, 352, 142, Green, 14);
            }
            else
            {
                FillRect(t, 124, 82, 264, 36, Amber);
                FillRect(t, 158, 125, 196, 18, Copper);
            }
        }

        private static void DrawOfflineSettlementMeter(Texture2D t)
        {
            FillRect(t, 36, 44, t.width - 72, t.height - 88, Panel);
            Border(t, 36, 44, t.width - 72, t.height - 88, Teal, 5);
            FillRect(t, 88, 104, t.width - 176, 52, new Color32(19, 34, 38, 245));
            FillRect(t, 88, 104, (t.width - 176) * 7 / 10, 52, new Color32(80, 178, 186, 230));
            Border(t, 88, 104, t.width - 176, 52, Steel, 3);
            for (var i = 0; i <= 10; i++)
            {
                var x = 88 + (t.width - 176) * i / 10;
                FillRect(t, x, 92, 4, 76, new Color32(230, 236, 226, 150));
            }

            FillRect(t, 88, t.height - 82, 260, 12, Amber);
            FillRect(t, t.width - 348, t.height - 82, 260, 12, Copper);
        }

        private static void DrawOfflineDurationStrip(Texture2D t)
        {
            FillRect(t, 22, 42, t.width - 44, t.height - 84, new Color32(10, 16, 21, 235));
            Border(t, 22, 42, t.width - 44, t.height - 84, Steel, 3);
            for (var i = 0; i < 12; i++)
            {
                var x = 60 + i * 76;
                var height = 24 + (i % 4) * 18;
                FillRect(t, x, 72, 42, height, i < 8 ? Teal : new Color32(62, 78, 78, 210));
            }

            Line(t, 60, 136, t.width - 60, 136, Amber, 4);
            FillRect(t, t.width - 144, 58, 80, 80, new Color32(226, 112, 54, 215));
        }

        private static void DrawOfflineEmptyState(Texture2D t)
        {
            FillRect(t, 58, 58, t.width - 116, t.height - 116, new Color32(9, 14, 18, 225));
            Border(t, 58, 58, t.width - 116, t.height - 116, Steel, 4);
            FillRect(t, 162, 186, t.width - 324, 96, new Color32(29, 43, 47, 240));
            Border(t, 162, 186, t.width - 324, 96, Teal, 3);
            FillRect(t, 216, 292, t.width - 432, 24, Amber);
            FillRect(t, 250, 144, t.width - 500, 20, Copper);
            for (var i = 0; i < 4; i++)
            {
                Line(t, 180 + i * 90, 356, 230 + i * 90, 400, new Color32(62, 86, 90, 170), 5);
            }
        }

        private static void DrawRewardChestAccent(Texture2D t)
        {
            FillRect(t, 118, 178, 276, 172, new Color32(82, 55, 35, 245));
            Border(t, 118, 178, 276, 172, Amber, 7);
            FillRect(t, 102, 300, 308, 80, new Color32(106, 70, 40, 245));
            Border(t, 102, 300, 308, 80, Copper, 5);
            FillRect(t, 238, 170, 38, 212, new Color32(229, 172, 70, 245));
            FillRect(t, 218, 244, 78, 58, new Color32(18, 28, 29, 245));
            Border(t, 218, 244, 78, 58, Teal, 3);
            Circle(t, 256, 264, 118, new Color32(241, 186, 82, 50));
        }

        private static void DrawMaterialGlow(Texture2D t)
        {
            for (var y = 0; y < t.height; y++)
            {
                for (var x = 0; x < t.width; x++)
                {
                    var dx = x - t.width * 0.5f;
                    var dy = y - t.height * 0.5f;
                    var d = Mathf.Sqrt(dx * dx + dy * dy) / (t.width * 0.5f);
                    var a = Mathf.Clamp01(1f - d);
                    var color = new Color(0.9f, 0.62f + a * 0.2f, 0.22f, a * a * 0.85f);
                    t.SetPixel(x, y, color);
                }
            }

            Circle(t, 256, 256, 54, new Color32(245, 223, 141, 220));
            Border(t, 205, 205, 102, 102, Amber, 4);
            Line(t, 175, 256, 337, 256, Copper, 4);
            Line(t, 256, 176, 256, 336, Teal, 4);
        }

        private static void ConfigureTexture(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Missing reward/offline texture importer: " + assetPath);
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
            b.AppendLine("  \"name\": \"LAST RAIL Act 1 Reward Offline Visual Resources\",");
            b.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            b.AppendLine("  \"resourceCount\": ").Append(Resources.Length).AppendLine(",");
            b.AppendLine("  \"generatedBy\": \"Unity procedural texture builder\",");
            b.AppendLine("  \"presentationOnly\": true,");
            b.AppendLine("  \"serverContractChanged\": false,");
            b.AppendLine("  \"rewardGrantIncluded\": false,");
            b.AppendLine("  \"claimTruthIncluded\": false,");
            b.AppendLine("  \"offlineAccumulationTruthIncluded\": false,");
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
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Act 1 Reward / Offline Reward Visual Resources

These reward and offline settlement files are Unity procedural texture builder outputs for the Act 1 launch UI.

Source:
- Generated locally inside the Unity Editor from `LaunchRewardOfflineResourceBuilder`.
- No external paid asset dependency.

Scope:
- `reward_result_plate`
- `reward_line_item_frame`
- `claim_success_badge`
- `claim_pending_claimable_accent`
- `offline_settlement_meter`
- `offline_duration_accumulation_strip`
- `offline_empty_state_visual`
- `reward_chest_supply_crate_accent`
- `reward_material_glow`

Usage:
- These are presentation resources only for RewardReview and OfflineReward screen polish.
- They are not authoritative gameplay, account, claim, balance, grant, or offline accumulation data.
- The manifest at `Assets/_LASTRAIL/UI/Style/lastrail-reward-offline-resources.json` is the stable QA/readiness reference.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchRewardOfflineResourceShowcase");
            AddBackground(root.transform);
            AddHeader(root.transform);
            AddRewardPanel(root.transform);
            AddOfflinePanel(root.transform);
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
            var image = UiObject("RewardOfflineShowcaseBackdrop", root, typeof(Image)).GetComponent<Image>();
            Stretch(image.GetComponent<RectTransform>());
            image.color = C(5, 8, 12, 255);
        }

        private static void AddHeader(Transform root)
        {
            var panel = PanelObject("HeaderPanel", root, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.975f));
            AddText(panel.transform, "Title", "ACT 1 REWARD / OFFLINE VISUAL FOUNDATION", 31, TextMain, new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.9f), TextAlignmentOptions.MidlineLeft);
            AddText(panel.transform, "Subtitle", "result plates / settlement meter / badges / line frames", 21, TextMuted, new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.42f), TextAlignmentOptions.MidlineLeft);
        }

        private static void AddRewardPanel(Transform root)
        {
            var panel = PanelObject("RewardResultPanel", root, new Vector2(0.05f, 0.49f), new Vector2(0.95f, 0.885f));
            AddText(panel.transform, "PanelTitle", "REWARD REVIEW", 25, Amber, new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.98f), TextAlignmentOptions.MidlineLeft);
            AddImage(panel.transform, "ResultPlate", Resources[0], new Vector2(0.04f, 0.33f), new Vector2(0.96f, 0.84f));
            AddImage(panel.transform, "ChestAccent", Resources[7], new Vector2(0.07f, 0.42f), new Vector2(0.26f, 0.72f));
            AddImage(panel.transform, "MaterialGlow", Resources[8], new Vector2(0.76f, 0.42f), new Vector2(0.94f, 0.72f));
            AddImage(panel.transform, "SuccessBadge", Resources[2], new Vector2(0.66f, 0.74f), new Vector2(0.92f, 0.86f));
            AddText(panel.transform, "BadgeLabel", "success badge / chest accent", 17, TextMuted, new Vector2(0.62f, 0.84f), new Vector2(0.95f, 0.89f), TextAlignmentOptions.MidlineRight);
            for (var i = 0; i < 3; i++)
            {
                AddImage(panel.transform, "LineFrame_" + i, Resources[1], new Vector2(0.28f, 0.65f - i * 0.14f), new Vector2(0.88f, 0.76f - i * 0.14f));
            }
            AddText(panel.transform, "RewardHint", "server response display only", 19, TextMuted, new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.2f), TextAlignmentOptions.MidlineRight);
        }

        private static void AddOfflinePanel(Transform root)
        {
            var panel = PanelObject("OfflineSettlementPanel", root, new Vector2(0.05f, 0.045f), new Vector2(0.95f, 0.47f));
            AddText(panel.transform, "PanelTitle", "OFFLINE SETTLEMENT", 25, Teal, new Vector2(0.04f, 0.9f), new Vector2(0.96f, 0.98f), TextAlignmentOptions.MidlineLeft);
            AddImage(panel.transform, "SettlementMeter", Resources[4], new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.84f));
            AddImage(panel.transform, "DurationStrip", Resources[5], new Vector2(0.05f, 0.43f), new Vector2(0.95f, 0.58f));
            AddImage(panel.transform, "EmptyStateVisual", Resources[6], new Vector2(0.06f, 0.11f), new Vector2(0.38f, 0.39f));
            AddImage(panel.transform, "PendingAccent", Resources[3], new Vector2(0.64f, 0.29f), new Vector2(0.93f, 0.43f));
            AddImage(panel.transform, "RewardLineFrame", Resources[1], new Vector2(0.41f, 0.15f), new Vector2(0.93f, 0.27f));
            AddText(panel.transform, "MeterLabel", "storage cap and duration display slots", 17, TextMuted, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.62f), TextAlignmentOptions.MidlineRight);
            AddText(panel.transform, "OfflineHint", "claimable / empty / retry status display", 19, TextMuted, new Vector2(0.4f, 0.04f), new Vector2(0.95f, 0.12f), TextAlignmentOptions.MidlineRight);
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

        private static Image AddImage(Transform parent, string name, LaunchRewardOfflineResourceSpec spec, Vector2 anchorMin, Vector2 anchorMax)
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

    public sealed class LaunchRewardOfflineResourceSpec
    {
        public LaunchRewardOfflineResourceSpec(string id, string category, string path, int width, int height, string intendedUsage)
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
