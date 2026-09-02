#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchCombatAnimationResourceBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string Root = UiRoot + "/CombatAnimations";
        private const string ClipsRoot = Root + "/Clips";
        private const string AnimatorsRoot = Root + "/Animators";
        private const string ShowcaseRoot = Root + "/Showcase";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-combat-animation-resources.json";
        private const string ReadmePath = Root + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchCombatAnimationShowcase.unity";
        public const string ShowcasePrefabPath = ShowcaseRoot + "/LRLaunchCombatAnimationShowcase.prefab";

        private static readonly string[] UnitStates = { "idle", "attack", "skill_or_special", "hit", "down_or_defeated" };
        private static readonly string[] BossStates = { "idle", "warning", "attack", "hit", "defeat" };

        private static readonly CombatAnimationSpec[] Specs =
        {
            Operator("op_vex"), Operator("op_nyx"), Operator("op_sable"), Operator("op_marrow"), Operator("op_iris"),
            Operator("op_rail"), Operator("op_anchor"), Operator("op_lumen"), Operator("op_cinder"), Operator("op_warden"),
            Enemy("enemy_raider_scout"), Enemy("enemy_railbike_marauder"), Enemy("enemy_scrapshield_brute"),
            Enemy("enemy_signal_jammer_drone"), Enemy("enemy_spike_cart_gunner"), Enemy("enemy_cinder_hound_mech"),
            EliteEnemy("elite_broadcast_executioner"), EliteEnemy("elite_ironclad_boarder"),
            Boss("boss_raider_king_engine")
        };

        [MenuItem("LASTRAIL/Launch/Build Combat Animation Resource Foundation")]
        public static void BuildCombatAnimationResources()
        {
            Ensure(Root); Ensure(StyleRoot); Ensure(SceneRoot); Ensure(ShowcaseRoot);
            Ensure(Root + "/Operators"); Ensure(Root + "/Enemies"); Ensure(Root + "/EliteEnemies"); Ensure(Root + "/Bosses");
            Ensure(ClipsRoot); Ensure(AnimatorsRoot);
            Ensure(ClipsRoot + "/Operators"); Ensure(ClipsRoot + "/Enemies"); Ensure(ClipsRoot + "/EliteEnemies"); Ensure(ClipsRoot + "/Bosses");
            Ensure(AnimatorsRoot + "/Operators"); Ensure(AnimatorsRoot + "/Enemies"); Ensure(AnimatorsRoot + "/EliteEnemies"); Ensure(AnimatorsRoot + "/Bosses");

            foreach (var spec in Specs)
            {
                ConfigureFrameTextures(spec);
                CreateClipsAndController(spec);
            }

            WriteManifest();
            WriteReadme();
            CreateShowcasePrefab();
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL launch combat animation resource foundation build complete.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static CombatAnimationSpec Operator(string id) => new CombatAnimationSpec(id, "operator_elite", "Operators", UiRoot + "/Operators/Portraits/" + id + "_portrait.png", UnitStates);
        private static CombatAnimationSpec Enemy(string id) => new CombatAnimationSpec(id, "enemy", "Enemies", UiRoot + "/CombatResources/Enemies/" + id + ".png", UnitStates);
        private static CombatAnimationSpec EliteEnemy(string id) => new CombatAnimationSpec(id, "elite_enemy", "EliteEnemies", UiRoot + "/CombatResources/Elites/" + id + ".png", UnitStates);
        private static CombatAnimationSpec Boss(string id) => new CombatAnimationSpec(id, "boss", "Bosses", UiRoot + "/CombatResources/Bosses/" + id + ".png", BossStates);

        private static void ConfigureFrameTextures(CombatAnimationSpec spec)
        {
            foreach (var state in spec.States)
            {
                for (var frame = 1; frame <= 4; frame++)
                {
                    var importer = AssetImporter.GetAtPath(FramePath(spec, state, frame)) as TextureImporter;
                    if (importer == null)
                    {
                        throw new FileNotFoundException("Missing combat animation frame: " + FramePath(spec, state, frame));
                    }

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.sRGBTexture = true;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.maxTextureSize = 1024;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void CreateClipsAndController(CombatAnimationSpec spec)
        {
            var clipFolder = ClipsRoot + "/" + spec.Folder + "/" + spec.Id;
            var animatorFolder = AnimatorsRoot + "/" + spec.Folder;
            Ensure(clipFolder);
            Ensure(animatorFolder);

            var clips = new List<AnimationClip>();
            foreach (var state in spec.States)
            {
                clips.Add(CreateClip(spec, state, clipFolder + "/" + spec.Id + "_" + state + ".anim"));
            }

            CreateController(AnimatorsRoot + "/" + spec.Folder + "/" + spec.Id + ".controller", spec.States, clips);
        }

        private static AnimationClip CreateClip(CombatAnimationSpec spec, string state, string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            clip.frameRate = 12f;
            var keyframes = new ObjectReferenceKeyframe[4];
            for (var frame = 1; frame <= 4; frame++)
            {
                keyframes[frame - 1] = new ObjectReferenceKeyframe
                {
                    time = (frame - 1) / 12f,
                    value = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath(spec, state, frame))
                };
            }

            AnimationUtility.SetObjectReferenceCurve(
                clip,
                EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite"),
                keyframes);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = state == "idle";
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void CreateController(string path, IReadOnlyList<string> states, IReadOnlyList<AnimationClip> clips)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            var stateMachine = controller.layers[0].stateMachine;
            for (var i = 0; i < states.Count; i++)
            {
                var state = FindState(stateMachine, states[i]) ?? stateMachine.AddState(states[i]);
                state.motion = clips[i];
                if (i == 0)
                {
                    stateMachine.defaultState = state;
                }
            }

            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
        {
            foreach (var child in stateMachine.states)
            {
                if (string.Equals(child.state.name, name, StringComparison.Ordinal))
                {
                    return child.state;
                }
            }

            return null;
        }

        private static void WriteManifest()
        {
            var b = new System.Text.StringBuilder();
            b.AppendLine("{");
            b.AppendLine("  \"name\": \"LAST RAIL Act 1 Combat Animation Resources\",");
            b.AppendLine("  \"referenceResolution\": \"1080x1920\",");
            b.AppendLine("  \"operatorEliteCount\": 10,");
            b.AppendLine("  \"enemyCount\": 6,");
            b.AppendLine("  \"eliteEnemyCount\": 2,");
            b.AppendLine("  \"bossCount\": 1,");
            b.AppendLine("  \"stateCountPerResource\": 5,");
            b.AppendLine("  \"framesPerState\": 4,");
            b.AppendLine("  \"presentationOnly\": true,");
            b.AppendLine("  \"hasCombatMath\": false,");
            b.AppendLine("  \"hasServerSubmitPayload\": false,");
            b.AppendLine("  \"hasGachaTruth\": false,");
            b.AppendLine("  \"railChoiceIncluded\": false,");
            b.AppendLine("  \"worldRaidIncluded\": false,");
            b.AppendLine("  \"resources\": [");
            for (var i = 0; i < Specs.Length; i++)
            {
                var spec = Specs[i];
                b.Append("    { \"id\": \"").Append(spec.Id)
                    .Append("\", \"kind\": \"").Append(spec.Kind)
                    .Append("\", \"states\": [");
                for (var j = 0; j < spec.States.Length; j++)
                {
                    b.Append("\"").Append(spec.States[j]).Append("\"");
                    if (j != spec.States.Length - 1) b.Append(", ");
                }

                b.Append("], \"frameCount\": ").Append((spec.States.Length * 4).ToString(CultureInfo.InvariantCulture))
                    .Append(", \"resolution\": \"512x512\", \"sourceArtPath\": \"").Append(spec.SourceArtPath)
                    .Append("\", \"qualityClass\": \"HIGH_QUALITY_2D_ILLUSTRATION\", \"showcaseVisible\": true, \"presentationOnly\": true }");
                b.AppendLine(i == Specs.Length - 1 ? string.Empty : ",");
            }

            b.AppendLine("  ]");
            b.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), b.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Act 1 Combat Animation Resources

These launch combat animation resources are visual-only derivatives of the existing LAST RAIL operator, enemy, and story raid boss art.

Coverage:
- 10 gacha elite operators.
- 6 normal Act 1 enemies.
- 2 elite Act 1 enemies.
- 1 `boss_raider_king_engine` story raid boss.
- 5 animation states per resource, 4 frames per state, 512x512 PNG frames.

Usage:
- Frame sequences live under `Assets/_LASTRAIL/UI/CombatAnimations/`.
- Animation clips and Animator Controllers are generated beside the frames for Editor-verifiable playback.
- The showcase prefab is `Assets/_LASTRAIL/UI/CombatAnimations/Showcase/LRLaunchCombatAnimationShowcase.prefab`.
- These resources are presentation-only and do not encode combat formulas, rewards, economy grants, story progress, unlocks, offline rewards, gacha odds, or ledger state.
- External paid asset dependency: none.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = CreateCanvasRoot("LRLaunchCombatAnimationShowcase");
            AddBackground(root.transform);
            AddHeader(root.transform);
            AddAnimatedGrid(root.transform);
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
            var go = UiObject("CombatAnimationBackdrop", root, typeof(Image));
            Stretch(go.GetComponent<RectTransform>());
            go.GetComponent<Image>().color = C(5, 8, 10, 255);
        }

        private static void AddHeader(Transform root)
        {
            AddText(root, "Title", "LAST RAIL COMBAT ANIMATION", 34, C(240, 238, 226, 255), new Vector2(0.07f, 0.93f), new Vector2(0.93f, 0.98f), TextAlignmentOptions.MidlineLeft);
            AddText(root, "Subtitle", "10 elite operators / 8 enemies / story raid boss / visual-only clips", 20, C(178, 196, 198, 255), new Vector2(0.07f, 0.895f), new Vector2(0.93f, 0.93f), TextAlignmentOptions.MidlineLeft);
        }

        private static void AddAnimatedGrid(Transform root)
        {
            for (var i = 0; i < Specs.Length; i++)
            {
                var spec = Specs[i];
                var col = i % 4;
                var row = i / 4;
                var xMin = 0.055f + col * 0.232f;
                var xMax = xMin + 0.205f;
                var yMax = 0.86f - row * 0.158f;
                var yMin = yMax - 0.135f;
                AddAnimatedCard(root, spec, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
            }
        }

        private static void AddAnimatedCard(Transform root, CombatAnimationSpec spec, Vector2 anchorMin, Vector2 anchorMax)
        {
            var card = UiObject(spec.Id + "Card", root, typeof(Image));
            var cardRect = card.GetComponent<RectTransform>();
            Anchor(cardRect, anchorMin, anchorMax);
            card.GetComponent<Image>().color = spec.Kind == "boss" ? C(50, 18, 18, 230) : spec.Kind == "operator_elite" ? C(18, 31, 43, 226) : C(22, 26, 27, 226);

            var image = UiObject(spec.Id + "AnimatedImage", card.transform, typeof(Image), typeof(Animator));
            Anchor(image.GetComponent<RectTransform>(), new Vector2(0.08f, 0.26f), new Vector2(0.92f, 0.94f));
            image.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath(spec, spec.States[0], 1));
            image.GetComponent<Image>().preserveAspect = true;
            image.GetComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorsRoot + "/" + spec.Folder + "/" + spec.Id + ".controller");

            AddText(card.transform, spec.Id + "Label", spec.Id, 15, C(235, 237, 228, 255), new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.23f), TextAlignmentOptions.Center);
            AddText(card.transform, spec.Id + "StateLabel", string.Join(" / ", spec.States), 9, C(159, 185, 187, 255), new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.08f), TextAlignmentOptions.Center);
        }

        private static void CreateShowcaseScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ShowcaseScenePath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void AddEventSystem(Transform root)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(root, false);
        }

        private static GameObject UiObject(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, components.Length == 0 ? new[] { typeof(RectTransform) } : WithRect(components));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Type[] WithRect(Type[] components)
        {
            var result = new Type[components.Length + 1];
            result[0] = typeof(RectTransform);
            Array.Copy(components, 0, result, 1, components.Length);
            return result;
        }

        private static TMP_Text AddText(Transform parent, string name, string text, int size, Color color, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment)
        {
            var go = UiObject(name, parent, typeof(TextMeshProUGUI));
            Anchor(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            var tmp = go.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.enableWordWrapping = false;
            tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            tmp.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            return tmp;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static string FramePath(CombatAnimationSpec spec, string state, int frame)
        {
            return Root + "/" + spec.Folder + "/" + spec.Id + "/" + state + "/" + spec.Id + "_" + state + "_" + frame.ToString("000", CultureInfo.InvariantCulture) + ".png";
        }

        private static void Ensure(string assetPath)
        {
            var fullPath = ProjectPath(assetPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        private static string ProjectPath(string assetPath) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        private static Color C(byte r, byte g, byte b, byte a) => new Color32(r, g, b, a);

        private readonly struct CombatAnimationSpec
        {
            public CombatAnimationSpec(string id, string kind, string folder, string sourceArtPath, string[] states)
            {
                Id = id;
                Kind = kind;
                Folder = folder;
                SourceArtPath = sourceArtPath;
                States = states;
            }

            public string Id { get; }
            public string Kind { get; }
            public string Folder { get; }
            public string SourceArtPath { get; }
            public string[] States { get; }
        }
    }
}
#endif
