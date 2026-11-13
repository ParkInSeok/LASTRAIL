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
    public static class LaunchCombatArtResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string CombatRoot = UiRoot + "/CombatResources";
        private const string EnemiesRoot = CombatRoot + "/Enemies";
        private const string ElitesRoot = CombatRoot + "/Elites";
        private const string BossesRoot = CombatRoot + "/Bosses";
        private const string VfxRoot = CombatRoot + "/VFX";
        private const string AccentsRoot = CombatRoot + "/Accents";
        private const string SourcesRoot = CombatRoot + "/Sources";
        private const string ShowcaseRoot = CombatRoot + "/Showcase";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-combat-art-resources.json";
        private const string ReadmePath = CombatRoot + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchCombatArtResourceShowcase.unity";
        public const string ShowcasePrefabPath = ShowcaseRoot + "/LRLaunchCombatArtResourceShowcase.prefab";

        private static readonly Color TextMain = C(238, 241, 234, 255);
        private static readonly Color TextMuted = C(176, 188, 188, 255);
        private static readonly Color Panel = C(5, 8, 12, 218);
        private static readonly Color Stroke = C(226, 112, 54, 210);
        private static readonly Color Elite = C(235, 178, 74, 255);
        private static readonly Color Boss = C(242, 72, 52, 255);
        private static readonly Color VfxColor = C(90, 202, 226, 255);

        public static readonly LaunchCombatEnemyResourceSpec[] Enemies =
        {
            Enemy("enemy_raider_scout", "normal", EnemiesRoot + "/enemy_raider_scout.png", "frontline_small"),
            Enemy("enemy_railbike_marauder", "normal", EnemiesRoot + "/enemy_railbike_marauder.png", "fast_flanker"),
            Enemy("enemy_scrapshield_brute", "normal", EnemiesRoot + "/enemy_scrapshield_brute.png", "shield_pressure"),
            Enemy("enemy_signal_jammer_drone", "normal", EnemiesRoot + "/enemy_signal_jammer_drone.png", "drone_signal"),
            Enemy("enemy_spike_cart_gunner", "normal", EnemiesRoot + "/enemy_spike_cart_gunner.png", "rail_cart_ranged"),
            Enemy("enemy_cinder_hound_mech", "normal", EnemiesRoot + "/enemy_cinder_hound_mech.png", "mechanical_pursuit"),
            Enemy("elite_broadcast_executioner", "elite", ElitesRoot + "/elite_broadcast_executioner.png", "elite_pressure"),
            Enemy("elite_ironclad_boarder", "elite", ElitesRoot + "/elite_ironclad_boarder.png", "elite_boarding"),
            Enemy("boss_raider_king_engine", "boss", BossesRoot + "/boss_raider_king_engine.png", "story_raid_boss")
        };

        public static readonly LaunchCombatVfxResourceSpec[] VfxResources =
        {
            VfxSpec("vfx_basic_hit_spark", "hit", VfxRoot + "/vfx_basic_hit_spark.png", "short_overlay"),
            VfxSpec("vfx_skill_activation", "skill", VfxRoot + "/vfx_skill_activation.png", "short_overlay"),
            VfxSpec("vfx_ultimate_activation", "ultimate", VfxRoot + "/vfx_ultimate_activation.png", "short_overlay"),
            VfxSpec("vfx_shield_status", "shield", VfxRoot + "/vfx_shield_status.png", "status_overlay"),
            VfxSpec("vfx_heal_status", "heal", VfxRoot + "/vfx_heal_status.png", "status_overlay"),
            VfxSpec("vfx_buff_debuff_status", "buff_debuff", VfxRoot + "/vfx_buff_debuff_status.png", "status_overlay"),
            VfxSpec("vfx_story_raid_danger", "raid_danger", VfxRoot + "/vfx_story_raid_danger.png", "boss_warning_overlay"),
            VfxSpec("vfx_boss_defeat_burst", "boss_defeat", VfxRoot + "/vfx_boss_defeat_burst.png", "short_overlay")
        };

        public static readonly LaunchCombatAccentResourceSpec[] Accents =
        {
            new LaunchCombatAccentResourceSpec("boss_danger_frame", AccentsRoot + "/boss_danger_frame.png", 1536, 1024, "boss_frame_overlay"),
            new LaunchCombatAccentResourceSpec("boss_hp_bar_accent", AccentsRoot + "/boss_hp_bar_accent.png", 1024, 128, "boss_hud_accent")
        };

        [MenuItem("LASTRAIL/Launch/Build Combat Art Resource Foundation")]
        public static void BuildCombatArtResources()
        {
            Ensure(CombatRoot); Ensure(EnemiesRoot); Ensure(ElitesRoot); Ensure(BossesRoot); Ensure(VfxRoot); Ensure(AccentsRoot); Ensure(SourcesRoot); Ensure(ShowcaseRoot); Ensure(StyleRoot); Ensure(SceneRoot);
            foreach (var enemy in Enemies) ConfigureTexture(enemy.Path, false);
            foreach (var vfx in VfxResources) ConfigureTexture(vfx.Path, true);
            foreach (var accent in Accents) ConfigureTexture(accent.Path, true);
            ConfigureTexture(SourcesRoot + "/act1_enemy_boss_contact_sheet_generated.png", false, false);
            ConfigureTexture(SourcesRoot + "/act1_combat_vfx_contact_sheet_generated.png", false, false);
            WriteManifest(); WriteReadme(); CreateShowcasePrefab(); CreateShowcaseScene();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("LASTRAIL launch combat art resource foundation build complete.");
        }

        private static LaunchCombatEnemyResourceSpec Enemy(string id, string type, string path, string usage)
        {
            var width = type == "boss" ? 1536 : (type == "elite" ? 896 : 768);
            var height = type == "boss" ? 1024 : (type == "elite" ? 896 : 768);
            return new LaunchCombatEnemyResourceSpec(id, type, path, width, height, usage);
        }

        private static LaunchCombatVfxResourceSpec VfxSpec(string id, string category, string path, string usage) => new LaunchCombatVfxResourceSpec(id, category, path, 512, 512, usage);

        private static void ConfigureTexture(string assetPath, bool alpha, bool sprite = true)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing combat art texture importer: " + assetPath);
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
            b.AppendLine("  \"name\": \"LAST RAIL Act 1 Combat Art Resources\",");
            b.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            b.AppendLine("  \"normalEnemyCount\": 6,");
            b.AppendLine("  \"eliteEnemyCount\": 2,");
            b.AppendLine("  \"bossCount\": 1,");
            b.AppendLine("  \"vfxCount\": 8,");
            b.AppendLine("  \"accentCount\": 2,");
            b.AppendLine("  \"generatedBy\": \"OpenAI image_gen built-in tool\",");
            b.AppendLine("  \"sourceType\": \"project_generated_launch_asset\",");
            b.AppendLine("  \"serverTruthFieldsAllowed\": false,");
            b.AppendLine("  \"combatMathIncluded\": false,");
            b.AppendLine("  \"railsChoiceIncluded\": false,");
            b.AppendLine("  \"worldRaidIncluded\": false,");
            b.AppendLine("  \"vfxBudget\": { \"largeConcurrentMax\": 2, \"hitSparkConcurrentRange\": \"8-12\", \"fullScreenFlashSecondsMax\": 0.15, \"bossPhaseSecondsRange\": \"1-2\", \"hudReadabilityFirst\": true },");
            b.AppendLine("  \"enemies\": [");
            for (var i = 0; i < Enemies.Length; i++)
            {
                var e = Enemies[i];
                b.Append("    { \"id\": \"").Append(e.Id).Append("\", \"type\": \"").Append(e.Type).Append("\", \"path\": \"").Append(e.Path).Append("\", \"width\": ").Append(e.Width).Append(", \"height\": ").Append(e.Height).Append(", \"intendedUsage\": \"").Append(e.IntendedUsage).Append("\", \"sourceType\": \"generated_local_launch_asset\" }");
                b.AppendLine(i == Enemies.Length - 1 ? string.Empty : ",");
            }
            b.AppendLine("  ],");
            b.AppendLine("  \"vfx\": [");
            for (var i = 0; i < VfxResources.Length; i++)
            {
                var v = VfxResources[i];
                b.Append("    { \"id\": \"").Append(v.Id).Append("\", \"category\": \"").Append(v.Category).Append("\", \"path\": \"").Append(v.Path).Append("\", \"width\": ").Append(v.Width).Append(", \"height\": ").Append(v.Height).Append(", \"usage\": \"").Append(v.Usage).Append("\", \"sourceType\": \"generated_local_launch_asset\" }");
                b.AppendLine(i == VfxResources.Length - 1 ? string.Empty : ",");
            }
            b.AppendLine("  ],");
            b.AppendLine("  \"accents\": [");
            for (var i = 0; i < Accents.Length; i++)
            {
                var a = Accents[i];
                b.Append("    { \"id\": \"").Append(a.Id).Append("\", \"path\": \"").Append(a.Path).Append("\", \"width\": ").Append(a.Width).Append(", \"height\": ").Append(a.Height).Append(", \"usage\": \"").Append(a.Usage).Append("\" }");
                b.AppendLine(i == Accents.Length - 1 ? string.Empty : ",");
            }
            b.AppendLine("  ]");
            b.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), b.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Act 1 Combat Art Resources

These enemy, boss, accent, and VFX files are project-generated launch combat resources created with the OpenAI image_gen built-in tool for LAST RAIL Act 1.

Source sheets:
- `Assets/_LASTRAIL/UI/CombatResources/Sources/act1_enemy_boss_contact_sheet_generated.png`
- `Assets/_LASTRAIL/UI/CombatResources/Sources/act1_combat_vfx_contact_sheet_generated.png`

Scope:
- 6 normal enemy display resources.
- 2 elite enemy display resources.
- 1 `raid_raider_king_engine` boss visual resource.
- 8 combat VFX overlay resources plus 2 boss HUD accent resources.

Usage:
- Use the manifest at `Assets/_LASTRAIL/UI/Style/lastrail-combat-art-resources.json` for QA and resource readiness.
- These are presentation resources only. They do not encode combat formulas, rewards, economy grants, story progress, unlocks, offline rewards, gacha odds, or ledger state.
- External paid asset dependency: none.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchCombatArtResourceShowcase");
            AddBackground(root.transform); AddHeader(root.transform); AddEnemyLineup(root.transform); AddBossPreview(root.transform); AddVfxGrid(root.transform); AddEventSystem(root.transform);
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
            var go = UiObject("CombatArtShowcaseBackdrop", root, typeof(Image)); Stretch(go.GetComponent<RectTransform>()); go.GetComponent<Image>().color = C(5, 7, 10, 255);
        }

        private static void AddHeader(Transform root)
        {
            var panel = PanelObject("HeaderPanel", root, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.975f));
            AddText(panel.transform, "Title", "ACT 1 COMBAT ART FOUNDATION", 36, TextMain, new Vector2(0.04f, 0.44f), new Vector2(0.96f, 0.9f), TextAlignmentOptions.MidlineLeft);
            AddText(panel.transform, "Subtitle", "6 normal enemies / 2 elites / Raider King Engine / 8 VFX cues", 22, TextMuted, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.42f), TextAlignmentOptions.MidlineLeft);
        }

        private static void AddEnemyLineup(Transform root)
        {
            var panel = PanelObject("EnemyLineupPanel", root, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.885f));
            AddText(panel.transform, "EnemyLineupLabel", "ENEMY LINEUP", 24, Elite, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.98f), TextAlignmentOptions.MidlineLeft);
            for (var i = 0; i < 8; i++) AddEnemyCard(panel.transform, Enemies[i], i);
        }

        private static void AddEnemyCard(Transform parent, LaunchCombatEnemyResourceSpec enemy, int index)
        {
            var col = index % 4; var row = index / 4;
            var card = UiObject("EnemyCard_" + enemy.Id, parent, typeof(Image));
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.035f + col * 0.235f, row == 0 ? 0.47f : 0.05f);
            rect.anchorMax = new Vector2(0.225f + col * 0.235f, row == 0 ? 0.85f : 0.43f);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = C(10, 13, 17, 215);
            var img = UiObject("Sprite", card.transform, typeof(Image));
            var imgRect = img.GetComponent<RectTransform>();
            imgRect.anchorMin = new Vector2(0.04f, 0.2f); imgRect.anchorMax = new Vector2(0.96f, 0.96f); imgRect.offsetMin = Vector2.zero; imgRect.offsetMax = Vector2.zero;
            var image = img.GetComponent<Image>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(enemy.Path); image.preserveAspect = true; image.raycastTarget = false;
            AddText(card.transform, "Id", enemy.Id, 16, enemy.Type == "elite" ? Elite : TextMain, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.18f), TextAlignmentOptions.Center);
        }

        private static void AddBossPreview(Transform root)
        {
            var panel = PanelObject("BossPreviewPanel", root, new Vector2(0.05f, 0.365f), new Vector2(0.95f, 0.605f));
            AddText(panel.transform, "BossLabel", "STORY RAID BOSS", 24, Boss, new Vector2(0.03f, 0.84f), new Vector2(0.97f, 0.98f), TextAlignmentOptions.MidlineLeft);
            var bossImage = UiObject("Boss_RaiderKingEngine", panel.transform, typeof(Image));
            var rect = bossImage.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0.03f, 0.06f); rect.anchorMax = new Vector2(0.73f, 0.84f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var image = bossImage.GetComponent<Image>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossesRoot + "/boss_raider_king_engine.png"); image.preserveAspect = true; image.raycastTarget = false;
            var frame = UiObject("BossDangerFrame", bossImage.transform, typeof(Image)); Stretch(frame.GetComponent<RectTransform>()); frame.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AccentsRoot + "/boss_danger_frame.png"); frame.GetComponent<Image>().preserveAspect = true; frame.GetComponent<Image>().raycastTarget = false;
            var hp = UiObject("BossHpAccent", panel.transform, typeof(Image)); var hpRect = hp.GetComponent<RectTransform>(); hpRect.anchorMin = new Vector2(0.68f, 0.36f); hpRect.anchorMax = new Vector2(0.97f, 0.52f); hpRect.offsetMin = Vector2.zero; hpRect.offsetMax = Vector2.zero; hp.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AccentsRoot + "/boss_hp_bar_accent.png"); hp.GetComponent<Image>().preserveAspect = true; hp.GetComponent<Image>().raycastTarget = false;
            AddText(panel.transform, "BossId", "boss_raider_king_engine", 21, TextMain, new Vector2(0.68f, 0.14f), new Vector2(0.97f, 0.32f), TextAlignmentOptions.Center);
        }

        private static void AddVfxGrid(Transform root)
        {
            var panel = PanelObject("VfxPreviewPanel", root, new Vector2(0.05f, 0.045f), new Vector2(0.95f, 0.345f));
            AddText(panel.transform, "VfxLabel", "COMBAT VFX CUES", 24, VfxColor, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.98f), TextAlignmentOptions.MidlineLeft);
            for (var i = 0; i < VfxResources.Length; i++) AddVfxCard(panel.transform, VfxResources[i], i);
        }

        private static void AddVfxCard(Transform parent, LaunchCombatVfxResourceSpec vfx, int index)
        {
            var col = index % 4; var row = index / 4;
            var card = UiObject("VfxCard_" + vfx.Id, parent, typeof(Image));
            var rect = card.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0.04f + col * 0.235f, row == 0 ? 0.48f : 0.08f); rect.anchorMax = new Vector2(0.22f + col * 0.235f, row == 0 ? 0.86f : 0.46f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = C(9, 12, 16, 190);
            var img = UiObject("Sprite", card.transform, typeof(Image)); var imgRect = img.GetComponent<RectTransform>(); imgRect.anchorMin = new Vector2(0.06f, 0.23f); imgRect.anchorMax = new Vector2(0.94f, 0.96f); imgRect.offsetMin = Vector2.zero; imgRect.offsetMax = Vector2.zero;
            var image = img.GetComponent<Image>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(vfx.Path); image.preserveAspect = true; image.raycastTarget = false;
            AddText(card.transform, "Id", vfx.Id.Replace("vfx_", string.Empty), 15, TextMain, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.2f), TextAlignmentOptions.Center);
        }

        private static GameObject PanelObject(string name, Transform root, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = UiObject(name, root, typeof(Image)); var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; panel.GetComponent<Image>().color = Panel;
            var rail = UiObject("AccentRail", panel.transform, typeof(Image)); var railRect = rail.GetComponent<RectTransform>(); railRect.anchorMin = new Vector2(0.03f, 0.82f); railRect.anchorMax = new Vector2(0.97f, 0.835f); railRect.offsetMin = Vector2.zero; railRect.offsetMax = Vector2.zero; rail.GetComponent<Image>().color = Stroke; rail.GetComponent<Image>().raycastTarget = false;
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

    public sealed class LaunchCombatEnemyResourceSpec
    {
        public LaunchCombatEnemyResourceSpec(string id, string type, string path, int width, int height, string intendedUsage) { Id = id; Type = type; Path = path; Width = width; Height = height; IntendedUsage = intendedUsage; }
        public string Id { get; } public string Type { get; } public string Path { get; } public int Width { get; } public int Height { get; } public string IntendedUsage { get; }
    }

    public sealed class LaunchCombatVfxResourceSpec
    {
        public LaunchCombatVfxResourceSpec(string id, string category, string path, int width, int height, string usage) { Id = id; Category = category; Path = path; Width = width; Height = height; Usage = usage; }
        public string Id { get; } public string Category { get; } public string Path { get; } public int Width { get; } public int Height { get; } public string Usage { get; }
    }

    public sealed class LaunchCombatAccentResourceSpec
    {
        public LaunchCombatAccentResourceSpec(string id, string path, int width, int height, string usage) { Id = id; Path = path; Width = width; Height = height; Usage = usage; }
        public string Id { get; } public string Path { get; } public int Width { get; } public int Height { get; } public string Usage { get; }
    }
}
#endif
