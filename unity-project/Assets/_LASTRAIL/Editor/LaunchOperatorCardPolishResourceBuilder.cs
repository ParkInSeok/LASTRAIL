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
    public static class LaunchOperatorCardPolishResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string OperatorRoot = UiRoot + "/Operators";
        private const string PolishRoot = OperatorRoot + "/CardPolish";
        private const string FramesRoot = PolishRoot + "/Frames";
        private const string BadgesRoot = PolishRoot + "/Badges";
        private const string RarityRoot = PolishRoot + "/Rarity";
        private const string RoleIconsRoot = PolishRoot + "/RoleIcons";
        private const string OverlaysRoot = PolishRoot + "/Overlays";
        private const string AccentsRoot = PolishRoot + "/Accents";
        private const string ShowcaseRoot = PolishRoot + "/Showcase";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-operator-card-polish-resources.json";
        private const string ReadmePath = PolishRoot + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchOperatorCardPolishShowcase.unity";

        public const string ShowcasePrefabPath = ShowcaseRoot + "/LRLaunchOperatorCardPolishShowcase.prefab";

        private static readonly Color32 TextMain = C(238, 241, 234, 255);
        private static readonly Color32 TextMuted = C(169, 181, 179, 255);
        private static readonly Color32 Panel = C(7, 10, 14, 224);
        private static readonly Color32 Copper = C(221, 126, 58, 255);
        private static readonly Color32 RailBlue = C(77, 180, 211, 255);
        private static readonly Color32 SignalGreen = C(122, 196, 120, 255);
        private static readonly Color32 WarningAmber = C(235, 185, 78, 255);
        private static readonly Color32 DangerRed = C(204, 77, 66, 255);
        private static readonly Color32 Steel = C(86, 101, 112, 255);

        public static readonly OperatorCardPolishResourceSpec[] Resources =
        {
            Resource("operator_card_slot_background", "frame", FramesRoot + "/operator_card_slot_background.png", 512, 720, "base_card_slot"),
            Resource("operator_card_selected_frame", "frame", FramesRoot + "/operator_card_selected_frame.png", 512, 720, "selected_state_frame"),
            Resource("operator_card_locked_overlay", "overlay", FramesRoot + "/operator_card_locked_overlay.png", 512, 720, "locked_state_overlay"),
            Resource("operator_card_detail_panel_accent", "accent", AccentsRoot + "/operator_card_detail_panel_accent.png", 1024, 256, "detail_panel_accent"),
            Resource("operator_badge_new", "badge", BadgesRoot + "/operator_badge_new.png", 256, 96, "new_badge"),
            Resource("operator_badge_owned", "badge", BadgesRoot + "/operator_badge_owned.png", 256, 96, "owned_badge"),
            Resource("operator_badge_level", "badge", BadgesRoot + "/operator_badge_level.png", 256, 96, "level_badge"),
            Resource("operator_badge_upgrade", "badge", BadgesRoot + "/operator_badge_upgrade.png", 256, 96, "upgrade_badge"),
            Resource("operator_rarity_initial_free", "rarity", RarityRoot + "/operator_rarity_initial_free.png", 256, 96, "initial_free_rarity_accent"),
            Resource("operator_rarity_rare", "rarity", RarityRoot + "/operator_rarity_rare.png", 256, 96, "rare_rarity_accent"),
            Resource("operator_rarity_elite", "rarity", RarityRoot + "/operator_rarity_elite.png", 256, 96, "elite_rarity_accent"),
            Resource("operator_rarity_pickup_elite", "rarity", RarityRoot + "/operator_rarity_pickup_elite.png", 256, 96, "pickup_elite_rarity_accent"),
            Resource("operator_role_striker", "role_icon", RoleIconsRoot + "/operator_role_striker.png", 128, 128, "striker_role_icon"),
            Resource("operator_role_guard", "role_icon", RoleIconsRoot + "/operator_role_guard.png", 128, 128, "guard_role_icon"),
            Resource("operator_role_medic", "role_icon", RoleIconsRoot + "/operator_role_medic.png", 128, 128, "medic_role_icon"),
            Resource("operator_role_engineer", "role_icon", RoleIconsRoot + "/operator_role_engineer.png", 128, 128, "engineer_role_icon"),
            Resource("operator_role_support", "role_icon", RoleIconsRoot + "/operator_role_support.png", 128, 128, "support_role_icon"),
            Resource("operator_state_locked_label_plate", "overlay", OverlaysRoot + "/operator_state_locked_label_plate.png", 256, 96, "locked_state_label_plate"),
            Resource("operator_state_available_label_plate", "overlay", OverlaysRoot + "/operator_state_available_label_plate.png", 256, 96, "available_state_label_plate"),
            Resource("operator_grid_section_rail", "accent", AccentsRoot + "/operator_grid_section_rail.png", 1024, 96, "grid_section_rail"),
            Resource("operator_grid_safe_mask", "overlay", OverlaysRoot + "/operator_grid_safe_mask.png", 512, 720, "safe_readability_mask")
        };

        private static readonly OperatorCardPolishShowcaseSpec[] ShowcaseOperators =
        {
            Showcase("op_kyle", "Kyle", "initial_free", "striker", false, true, false, false),
            Showcase("op_reina", "Reina", "initial_free", "guard", true, true, false, false),
            Showcase("op_eve", "Eve", "initial_free", "engineer", false, true, false, true),
            Showcase("op_sera", "Sera", "initial_free", "medic", false, true, false, false),
            Showcase("op_mira", "Mira", "initial_free", "support", false, true, false, false),
            Showcase("op_vex", "Vex", "pickup_elite", "striker", true, false, true, false),
            Showcase("op_nyx", "Nyx", "elite", "support", false, true, false, false),
            Showcase("op_sable", "Sable", "elite", "guard", false, false, false, false),
            Showcase("op_marrow", "Marrow", "elite", "medic", false, true, false, true),
            Showcase("op_iris", "Iris", "elite", "engineer", false, false, false, false),
            Showcase("op_rail", "Rail", "elite", "striker", false, true, false, false),
            Showcase("op_anchor", "Anchor", "elite", "guard", false, false, false, false),
            Showcase("op_lumen", "Lumen", "elite", "support", false, true, false, true),
            Showcase("op_cinder", "Cinder", "elite", "engineer", false, false, false, false),
            Showcase("op_warden", "Warden", "elite", "guard", false, true, false, false)
        };

        [MenuItem("LASTRAIL/Launch/Build Operator Card Polish Resources")]
        public static void BuildOperatorCardPolishResources()
        {
            Ensure(PolishRoot);
            Ensure(FramesRoot);
            Ensure(BadgesRoot);
            Ensure(RarityRoot);
            Ensure(RoleIconsRoot);
            Ensure(OverlaysRoot);
            Ensure(AccentsRoot);
            Ensure(ShowcaseRoot);
            Ensure(StyleRoot);
            Ensure(SceneRoot);

            foreach (var resource in Resources)
            {
                GenerateResource(resource);
                ConfigureTexture(resource.Path);
            }

            WriteManifest();
            WriteReadme();
            CreateShowcasePrefab();
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL operator card polish resources build complete.");
        }

        private static OperatorCardPolishResourceSpec Resource(string id, string category, string path, int width, int height, string usage)
        {
            return new OperatorCardPolishResourceSpec(id, category, path, width, height, usage);
        }

        private static OperatorCardPolishShowcaseSpec Showcase(string operatorId, string label, string rarity, string role, bool selected, bool owned, bool isNew, bool locked)
        {
            return new OperatorCardPolishShowcaseSpec(operatorId, label, rarity, role, selected, owned, isNew, locked);
        }

        private static void GenerateResource(OperatorCardPolishResourceSpec spec)
        {
            var texture = new Texture2D(spec.Width, spec.Height, TextureFormat.RGBA32, false);
            try
            {
                Fill(texture, C(0, 0, 0, 0));
                switch (spec.Id)
                {
                    case "operator_card_slot_background":
                        DrawCardSlot(texture);
                        break;
                    case "operator_card_selected_frame":
                        DrawSelectionFrame(texture);
                        DrawCornerTicks(texture, WarningAmber);
                        break;
                    case "operator_card_locked_overlay":
                        DrawLockOverlay(texture);
                        break;
                    case "operator_card_detail_panel_accent":
                        DrawDetailAccent(texture);
                        break;
                    case "operator_badge_new":
                        DrawBadge(texture, RailBlue, C(6, 24, 31, 238), "N");
                        break;
                    case "operator_badge_owned":
                        DrawBadge(texture, SignalGreen, C(8, 28, 20, 238), "O");
                        break;
                    case "operator_badge_level":
                        DrawBadge(texture, Steel, C(14, 18, 22, 238), "L");
                        break;
                    case "operator_badge_upgrade":
                        DrawBadge(texture, WarningAmber, C(36, 24, 8, 238), "U");
                        break;
                    case "operator_rarity_initial_free":
                        DrawRarityAccent(texture, Copper, C(84, 47, 25, 235), "I");
                        break;
                    case "operator_rarity_rare":
                        DrawRarityAccent(texture, RailBlue, C(16, 38, 49, 235), "R");
                        break;
                    case "operator_rarity_elite":
                        DrawRarityAccent(texture, WarningAmber, C(58, 43, 18, 235), "E");
                        break;
                    case "operator_rarity_pickup_elite":
                        DrawRarityAccent(texture, C(186, 232, 133, 255), C(34, 55, 22, 235), "P");
                        break;
                    case "operator_role_striker":
                        DrawRoleIcon(texture, DangerRed, "striker");
                        break;
                    case "operator_role_guard":
                        DrawRoleIcon(texture, Steel, "guard");
                        break;
                    case "operator_role_medic":
                        DrawRoleIcon(texture, SignalGreen, "medic");
                        break;
                    case "operator_role_engineer":
                        DrawRoleIcon(texture, Copper, "engineer");
                        break;
                    case "operator_role_support":
                        DrawRoleIcon(texture, RailBlue, "support");
                        break;
                    case "operator_state_locked_label_plate":
                        DrawStatePlate(texture, C(52, 58, 64, 238), DangerRed);
                        break;
                    case "operator_state_available_label_plate":
                        DrawStatePlate(texture, C(12, 31, 36, 238), SignalGreen);
                        break;
                    case "operator_grid_section_rail":
                        DrawSectionRail(texture);
                        break;
                    case "operator_grid_safe_mask":
                        DrawSafeMask(texture);
                        break;
                }

                texture.Apply(false);
                File.WriteAllBytes(ProjectPath(spec.Path), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureTexture(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Missing operator card polish texture importer: " + assetPath);
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
            b.AppendLine("  \"name\": \"LAST RAIL Act 1 Operator Card Grid Polish Resources\",");
            b.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            b.AppendLine("  \"resourceCount\": 21,");
            b.AppendLine("  \"operatorVisualScopeCount\": 15,");
            b.AppendLine("  \"initialFreeCount\": 5,");
            b.AppendLine("  \"gachaEliteCount\": 10,");
            b.AppendLine("  \"generatedBy\": \"Unity procedural texture builder\",");
            b.AppendLine("  \"sourceType\": \"procedurally generated local launch UI assets\",");
            b.AppendLine("  \"presentationOnly\": true,");
            b.AppendLine("  \"operatorTruthIncluded\": false,");
            b.AppendLine("  \"levelTruthIncluded\": false,");
            b.AppendLine("  \"upgradeTruthIncluded\": false,");
            b.AppendLine("  \"resources\": [");
            for (var i = 0; i < Resources.Length; i++)
            {
                var r = Resources[i];
                b.Append("    { \"id\": \"").Append(r.Id).Append("\", ");
                b.Append("\"category\": \"").Append(r.Category).Append("\", ");
                b.Append("\"path\": \"").Append(r.Path).Append("\", ");
                b.Append("\"width\": ").Append(r.Width).Append(", ");
                b.Append("\"height\": ").Append(r.Height).Append(", ");
                b.Append("\"intendedUsage\": \"").Append(r.IntendedUsage).Append("\", ");
                b.Append("\"sourceType\": \"generated_local_launch_asset\" }");
                b.AppendLine(i == Resources.Length - 1 ? string.Empty : ",");
            }
            b.AppendLine("  ]");
            b.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), b.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Operator Card Grid Polish Resources

These are procedurally generated local launch UI assets for operator card and grid presentation in LAST RAIL Act 1.

Scope:
- Card slot background, selected frame, locked overlay, state badges, rarity accents, role icons, level/upgrade badges, detail accent, grid rail, and readability mask.
- 15 visual operators are represented as a showcase layout: 5 initial/free and 10 gacha elite display candidates.
- These resources are presentation-only and do not encode ownership, level, upgrade, unlock, acquisition, item grant, or economy decisions.

Usage:
- Use `Assets/_LASTRAIL/UI/Style/lastrail-operator-card-polish-resources.json` for QA/resource readiness.
- Existing operator portrait/card/icon resources remain the portrait source. This package adds reusable UI skin layers around those cards.
- External paid asset dependency: none.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchOperatorCardPolishShowcase");
            AddBackground(root.transform);
            AddHeader(root.transform);
            AddOperatorGrid(root.transform);
            AddDetailPanel(root.transform);
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
            scaler.matchWidthOrHeight = 1f;
            Stretch(root.GetComponent<RectTransform>());
            return root;
        }

        private static void AddBackground(Transform root)
        {
            var backdrop = UiObject("OperatorCardPolishBackdrop", root, typeof(Image));
            Stretch(backdrop.GetComponent<RectTransform>());
            backdrop.GetComponent<Image>().color = C(5, 7, 10, 255);
            AddImage(root, "GridSectionRail", AccentsRoot + "/operator_grid_section_rail.png", new Vector2(0f, 738f), new Vector2(972f, 92f), C(255, 255, 255, 205));
        }

        private static void AddHeader(Transform root)
        {
            var panel = PanelObject("HeaderPanel", root, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.975f));
            AddText(panel.transform, "Title", "ACT 1 OPERATOR CARD GRID", 34, TextMain, new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.MidlineLeft);
            AddText(panel.transform, "Subtitle", "15 visual operators / state frames / role icons / badges", 22, TextMuted, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.44f), TextAlignmentOptions.MidlineLeft);
        }

        private static void AddOperatorGrid(Transform root)
        {
            var gridRoot = UiObject("OperatorPolishGrid", root, typeof(GridLayoutGroup));
            var rect = gridRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.055f, 0.265f);
            rect.anchorMax = new Vector2(0.945f, 0.855f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var grid = gridRoot.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.cellSize = new Vector2(178, 252);
            grid.spacing = new Vector2(16, 24);
            grid.childAlignment = TextAnchor.MiddleCenter;

            foreach (var op in ShowcaseOperators)
            {
                AddOperatorCard(gridRoot.transform, op);
            }
        }

        private static void AddOperatorCard(Transform parent, OperatorCardPolishShowcaseSpec op)
        {
            var card = UiObject("OperatorCard_" + op.OperatorId, parent, typeof(Image));
            card.GetComponent<Image>().sprite = LoadSprite(FramesRoot + "/operator_card_slot_background.png");
            card.GetComponent<Image>().color = C(255, 255, 255, 255);
            card.GetComponent<Image>().raycastTarget = false;

            AddImage(card.transform, "Portrait", OperatorRoot + "/Cards/" + op.OperatorId + "_card.png", new Vector2(0f, 20f), new Vector2(146f, 194f), C(255, 255, 255, op.Locked ? (byte)88 : (byte)235));
            AddImage(card.transform, "RarityAccent", RarityPath(op.Rarity), new Vector2(0f, -92f), new Vector2(132f, 44f), C(255, 255, 255, 240));
            AddImage(card.transform, "RoleIcon", RolePath(op.Role), new Vector2(-58f, 92f), new Vector2(42f, 42f), C(255, 255, 255, 245));

            if (op.Selected)
            {
                AddImage(card.transform, "SelectedFrame", FramesRoot + "/operator_card_selected_frame.png", Vector2.zero, new Vector2(178f, 252f), C(255, 255, 255, 255));
            }

            if (op.Locked)
            {
                AddImage(card.transform, "LockedOverlay", FramesRoot + "/operator_card_locked_overlay.png", Vector2.zero, new Vector2(178f, 252f), C(255, 255, 255, 245));
                AddImage(card.transform, "LockedPlate", OverlaysRoot + "/operator_state_locked_label_plate.png", new Vector2(0f, -16f), new Vector2(104f, 38f), C(255, 255, 255, 245));
            }
            else if (op.Owned)
            {
                AddImage(card.transform, "OwnedBadge", BadgesRoot + "/operator_badge_owned.png", new Vector2(48f, 94f), new Vector2(78f, 30f), C(255, 255, 255, 245));
            }

            if (op.IsNew)
            {
                AddImage(card.transform, "NewBadge", BadgesRoot + "/operator_badge_new.png", new Vector2(48f, 62f), new Vector2(78f, 30f), C(255, 255, 255, 245));
            }

            AddImage(card.transform, "LevelBadge", BadgesRoot + "/operator_badge_level.png", new Vector2(-48f, -96f), new Vector2(76f, 28f), C(255, 255, 255, 235));
            AddText(card.transform, "Label", op.OperatorId, 15, TextMain, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.13f), TextAlignmentOptions.Center);
        }

        private static void AddDetailPanel(Transform root)
        {
            var panel = PanelObject("OperatorDetailPanel", root, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.235f));
            AddImage(panel.transform, "DetailAccent", AccentsRoot + "/operator_card_detail_panel_accent.png", new Vector2(180f, 0f), new Vector2(560f, 82f), C(255, 255, 255, 115));
            AddText(panel.transform, "DetailTitle", "VISUAL-ONLY CARD STATES", 24, WarningAmber, new Vector2(0.04f, 0.56f), new Vector2(0.96f, 0.86f), TextAlignmentOptions.MidlineLeft);
            AddText(panel.transform, "DetailBody", "selected / locked / new / owned / rarity / role / level badge readiness", 20, TextMain, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.52f), TextAlignmentOptions.MidlineLeft);
        }

        private static void CreateShowcaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath), scene);
            EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        }

        private static GameObject PanelObject(string name, Transform root, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = UiObject(name, root, typeof(Image));
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = Panel;
            return panel;
        }

        private static Image AddImage(Transform parent, string name, string spritePath, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var go = UiObject(name, parent, typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            var image = go.GetComponent<Image>();
            image.sprite = LoadSprite(spritePath);
            image.preserveAspect = true;
            image.color = color;
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
            tmp.fontSizeMin = Math.Max(10, size - 7);
            tmp.fontSizeMax = size;
            tmp.enableAutoSizing = true;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
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
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(root, false);
        }

        private static string RarityPath(string rarity)
        {
            return RarityRoot + "/operator_rarity_" + rarity + ".png";
        }

        private static string RolePath(string role)
        {
            return RoleIconsRoot + "/operator_role_" + role + ".png";
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new FileNotFoundException("Missing operator card polish sprite: " + path);
            }

            return sprite;
        }

        private static void DrawCardSlot(Texture2D t)
        {
            FillGradient(t, C(8, 12, 17, 235), C(25, 30, 35, 245));
            DrawFrame(t, C(98, 112, 118, 210), C(32, 40, 46, 235), 8);
            DrawRect(t, 34, 40, t.width - 68, 8, Copper);
            DrawRect(t, 46, t.height - 88, t.width - 92, 5, RailBlue);
            DrawRailTicks(t, 44, 90, t.width - 88, C(166, 119, 68, 170));
        }

        private static void DrawSelectionFrame(Texture2D t)
        {
            Fill(t, C(0, 0, 0, 0));
            DrawFrameRect(t, 14, 14, t.width - 28, t.height - 28, RailBlue, 14);
            DrawFrameRect(t, 34, 34, t.width - 68, t.height - 68, C(238, 214, 132, 220), 5);
            DrawRect(t, 58, 54, t.width - 116, 7, C(238, 214, 132, 220));
            DrawRect(t, 58, t.height - 62, t.width - 116, 7, RailBlue);
        }
        private static void DrawFrame(Texture2D t, Color32 outer, Color32 inner, int thickness)
        {
            DrawRect(t, 0, 0, t.width, t.height, C(0, 0, 0, 0));
            DrawRect(t, 10, 10, t.width - 20, t.height - 20, inner);
            for (var i = 0; i < thickness; i++)
            {
                DrawRect(t, 12 + i, 12 + i, t.width - 24 - i * 2, 2, outer);
                DrawRect(t, 12 + i, t.height - 14 - i, t.width - 24 - i * 2, 2, outer);
                DrawRect(t, 12 + i, 12 + i, 2, t.height - 24 - i * 2, outer);
                DrawRect(t, t.width - 14 - i, 12 + i, 2, t.height - 24 - i * 2, outer);
            }
        }

        private static void DrawCornerTicks(Texture2D t, Color32 color)
        {
            DrawRect(t, 28, 28, 72, 8, color);
            DrawRect(t, 28, 28, 8, 72, color);
            DrawRect(t, t.width - 100, 28, 72, 8, color);
            DrawRect(t, t.width - 36, 28, 8, 72, color);
            DrawRect(t, 28, t.height - 36, 72, 8, color);
            DrawRect(t, 28, t.height - 100, 8, 72, color);
            DrawRect(t, t.width - 100, t.height - 36, 72, 8, color);
            DrawRect(t, t.width - 36, t.height - 100, 8, 72, color);
        }

        private static void DrawLockOverlay(Texture2D t)
        {
            FillGradient(t, C(3, 5, 8, 172), C(8, 10, 14, 210));
            DrawFrame(t, C(88, 95, 104, 190), C(0, 0, 0, 0), 6);
            DrawRect(t, 174, 318, 164, 112, C(18, 22, 27, 230));
            DrawFrameRect(t, 174, 318, 164, 112, DangerRed, 5);
            DrawRect(t, 206, 278, 100, 52, C(0, 0, 0, 0));
            DrawFrameRect(t, 206, 278, 100, 70, C(144, 152, 160, 210), 8);
        }

        private static void DrawDetailAccent(Texture2D t)
        {
            FillGradient(t, C(7, 11, 15, 226), C(16, 22, 27, 236));
            DrawRect(t, 32, 42, t.width - 64, 8, Copper);
            DrawRect(t, 32, 72, t.width - 230, 4, RailBlue);
            DrawRailTicks(t, 80, 116, t.width - 160, C(216, 155, 84, 180));
            DrawFrameRect(t, 0, 0, t.width, t.height, C(76, 91, 101, 220), 6);
        }

        private static void DrawBadge(Texture2D t, Color32 accent, Color32 background, string mark)
        {
            FillGradient(t, background, C((byte)Math.Min(255, background.r + 18), (byte)Math.Min(255, background.g + 18), (byte)Math.Min(255, background.b + 18), background.a));
            DrawFrameRect(t, 4, 4, t.width - 8, t.height - 8, accent, 5);
            DrawRect(t, 22, 22, t.width - 44, 7, accent);
            DrawLetterMark(t, mark, accent);
        }

        private static void DrawRarityAccent(Texture2D t, Color32 accent, Color32 background, string mark)
        {
            FillGradient(t, background, C((byte)Math.Min(255, background.r + 12), (byte)Math.Min(255, background.g + 12), (byte)Math.Min(255, background.b + 12), background.a));
            DrawFrameRect(t, 8, 8, t.width - 16, t.height - 16, accent, 5);
            DrawRect(t, 24, 44, t.width - 48, 8, accent);
            DrawLetterMark(t, mark, accent);
        }

        private static void DrawRoleIcon(Texture2D t, Color32 accent, string role)
        {
            Fill(t, C(0, 0, 0, 0));
            DrawCircle(t, 64, 64, 58, C(9, 13, 18, 238));
            DrawCircleFrame(t, 64, 64, 58, accent, 5);
            if (role == "striker")
            {
                DrawRect(t, 60, 26, 8, 78, accent);
                DrawRect(t, 34, 66, 60, 8, accent);
                DrawRect(t, 84, 42, 8, 24, accent);
            }
            else if (role == "guard")
            {
                DrawFrameRect(t, 38, 30, 52, 70, accent, 7);
                DrawRect(t, 46, 88, 36, 8, accent);
            }
            else if (role == "medic")
            {
                DrawRect(t, 58, 34, 12, 60, accent);
                DrawRect(t, 34, 58, 60, 12, accent);
            }
            else if (role == "engineer")
            {
                DrawCircleFrame(t, 64, 64, 28, accent, 6);
                DrawRect(t, 60, 28, 8, 18, accent);
                DrawRect(t, 60, 82, 8, 18, accent);
                DrawRect(t, 28, 60, 18, 8, accent);
                DrawRect(t, 82, 60, 18, 8, accent);
            }
            else
            {
                DrawRect(t, 30, 42, 68, 8, accent);
                DrawRect(t, 30, 62, 68, 8, accent);
                DrawRect(t, 30, 82, 68, 8, accent);
                DrawCircle(t, 42, 46, 5, TextMain);
                DrawCircle(t, 74, 66, 5, TextMain);
                DrawCircle(t, 56, 86, 5, TextMain);
            }
        }

        private static void DrawStatePlate(Texture2D t, Color32 background, Color32 accent)
        {
            FillGradient(t, background, C((byte)Math.Min(255, background.r + 12), (byte)Math.Min(255, background.g + 12), (byte)Math.Min(255, background.b + 12), background.a));
            DrawFrameRect(t, 6, 6, t.width - 12, t.height - 12, accent, 4);
            DrawRect(t, 28, 44, t.width - 56, 8, accent);
        }

        private static void DrawSectionRail(Texture2D t)
        {
            Fill(t, C(0, 0, 0, 0));
            DrawRect(t, 12, 38, t.width - 24, 10, Steel);
            DrawRect(t, 12, 58, t.width - 24, 10, Copper);
            DrawRailTicks(t, 36, 26, t.width - 72, C(220, 166, 88, 190));
        }

        private static void DrawSafeMask(Texture2D t)
        {
            FillGradient(t, C(4, 5, 7, 70), C(4, 5, 7, 170));
            DrawRect(t, 32, t.height - 112, t.width - 64, 72, C(4, 5, 7, 190));
        }

        private static void Fill(Texture2D t, Color32 color)
        {
            var pixels = new Color32[t.width * t.height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            t.SetPixels32(pixels);
        }

        private static void FillGradient(Texture2D t, Color32 top, Color32 bottom)
        {
            for (var y = 0; y < t.height; y++)
            {
                var k = y / (float)Math.Max(1, t.height - 1);
                var c = Lerp(bottom, top, k);
                for (var x = 0; x < t.width; x++)
                {
                    t.SetPixel(x, y, c);
                }
            }
        }

        private static void DrawRect(Texture2D t, int x, int y, int w, int h, Color32 color)
        {
            for (var py = Math.Max(0, y); py < Math.Min(t.height, y + h); py++)
            {
                for (var px = Math.Max(0, x); px < Math.Min(t.width, x + w); px++)
                {
                    Blend(t, px, py, color);
                }
            }
        }

        private static void DrawFrameRect(Texture2D t, int x, int y, int w, int h, Color32 color, int thickness)
        {
            DrawRect(t, x, y, w, thickness, color);
            DrawRect(t, x, y + h - thickness, w, thickness, color);
            DrawRect(t, x, y, thickness, h, color);
            DrawRect(t, x + w - thickness, y, thickness, h, color);
        }

        private static void DrawRailTicks(Texture2D t, int x, int y, int width, Color32 color)
        {
            for (var i = 0; i < 12; i++)
            {
                var tickX = x + i * width / 12;
                DrawRect(t, tickX, y, 8, 64, color);
            }
        }

        private static void DrawCircle(Texture2D t, int cx, int cy, int radius, Color32 color)
        {
            var r2 = radius * radius;
            for (var y = cy - radius; y <= cy + radius; y++)
            {
                for (var x = cx - radius; x <= cx + radius; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    if (dx * dx + dy * dy <= r2)
                    {
                        Blend(t, x, y, color);
                    }
                }
            }
        }

        private static void DrawCircleFrame(Texture2D t, int cx, int cy, int radius, Color32 color, int thickness)
        {
            var outer = radius * radius;
            var innerRadius = radius - thickness;
            var inner = innerRadius * innerRadius;
            for (var y = cy - radius; y <= cy + radius; y++)
            {
                for (var x = cx - radius; x <= cx + radius; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var d = dx * dx + dy * dy;
                    if (d <= outer && d >= inner)
                    {
                        Blend(t, x, y, color);
                    }
                }
            }
        }

        private static void DrawLetterMark(Texture2D t, string mark, Color32 color)
        {
            var x = mark == "P" ? 108 : 112;
            DrawRect(t, x, 26, 12, 44, color);
            if (mark != "I")
            {
                DrawRect(t, x, 26, 34, 10, color);
                DrawRect(t, x, 48, 34, 9, color);
            }

            if (mark == "N")
            {
                DrawRect(t, x + 34, 26, 12, 44, color);
                DrawRect(t, x + 14, 38, 22, 8, color);
            }
            else if (mark == "O")
            {
                DrawFrameRect(t, x + 6, 28, 34, 38, color, 8);
            }
            else if (mark == "U")
            {
                DrawRect(t, x + 34, 26, 12, 44, color);
                DrawRect(t, x + 12, 60, 34, 10, color);
            }
            else if (mark == "R")
            {
                DrawRect(t, x + 32, 56, 12, 16, color);
            }
            else if (mark == "E")
            {
                DrawRect(t, x, 62, 42, 8, color);
            }
            else if (mark == "P")
            {
                DrawRect(t, x + 40, 26, 12, 28, color);
            }
        }

        private static void Blend(Texture2D t, int x, int y, Color source)
        {
            if (x < 0 || y < 0 || x >= t.width || y >= t.height)
            {
                return;
            }

            var destination = t.GetPixel(x, y);
            var a = source.a;
            var blended = new Color(
                source.r * a + destination.r * (1f - a),
                source.g * a + destination.g * (1f - a),
                source.b * a + destination.b * (1f - a),
                Math.Max(source.a, destination.a));
            t.SetPixel(x, y, blended);
        }

        private static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
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
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var name = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                Ensure(parent);
            }

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

    public sealed class OperatorCardPolishResourceSpec
    {
        public OperatorCardPolishResourceSpec(string id, string category, string path, int width, int height, string intendedUsage)
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

    public sealed class OperatorCardPolishShowcaseSpec
    {
        public OperatorCardPolishShowcaseSpec(string operatorId, string displayLabel, string rarity, string role, bool selected, bool owned, bool isNew, bool locked)
        {
            OperatorId = operatorId;
            DisplayLabel = displayLabel;
            Rarity = rarity;
            Role = role;
            Selected = selected;
            Owned = owned;
            IsNew = isNew;
            Locked = locked;
        }

        public string OperatorId { get; }
        public string DisplayLabel { get; }
        public string Rarity { get; }
        public string Role { get; }
        public bool Selected { get; }
        public bool Owned { get; }
        public bool IsNew { get; }
        public bool Locked { get; }
    }
}
#endif



