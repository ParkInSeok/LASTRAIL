#if UNITY_5_3_OR_NEWER
using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchCombatAnimationResourceFoundationTests
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-combat-animation-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/CombatAnimations/README.md";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/CombatAnimations/Showcase/LRLaunchCombatAnimationShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchCombatAnimationShowcase.unity";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        private static readonly string[] UnitStates = { "idle", "attack", "skill_or_special", "hit", "down_or_defeated" };
        private static readonly string[] BossStates = { "idle", "warning", "attack", "hit", "defeat" };

        private static readonly ExpectedAnimatedResource[] Resources =
        {
            Resource("op_kyle", "operator_initial_free"),
            Resource("op_reina", "operator_initial_free"),
            Resource("op_eve", "operator_initial_free"),
            Resource("op_sera", "operator_initial_free"),
            Resource("op_mira", "operator_initial_free"),
            Resource("op_vex", "operator_elite"),
            Resource("op_nyx", "operator_elite"),
            Resource("op_sable", "operator_elite"),
            Resource("op_marrow", "operator_elite"),
            Resource("op_iris", "operator_elite"),
            Resource("op_rail", "operator_elite"),
            Resource("op_anchor", "operator_elite"),
            Resource("op_lumen", "operator_elite"),
            Resource("op_cinder", "operator_elite"),
            Resource("op_warden", "operator_elite"),
            Resource("enemy_raider_scout", "enemy"),
            Resource("enemy_railbike_marauder", "enemy"),
            Resource("enemy_scrapshield_brute", "enemy"),
            Resource("enemy_signal_jammer_drone", "enemy"),
            Resource("enemy_spike_cart_gunner", "enemy"),
            Resource("enemy_cinder_hound_mech", "enemy"),
            Resource("elite_broadcast_executioner", "elite_enemy"),
            Resource("elite_ironclad_boarder", "elite_enemy"),
            Resource("boss_raider_king_engine", "boss")
        };

        [Test]
        public void CombatAnimationManifest_CoversLaunchOperatorsEnemiesAndBossAsPresentationOnly()
        {
            var manifestFile = ProjectPath(ManifestPath);
            Assert.That(File.Exists(manifestFile), Is.True, ManifestPath);
            var manifest = JsonUtility.FromJson<CombatAnimationManifestFixture>(File.ReadAllText(manifestFile));

            Assert.That(manifest.name, Is.EqualTo("LAST RAIL Act 1 Combat Animation Resources"));
            Assert.That(manifest.referenceResolution, Is.EqualTo("1080x1920"));
            Assert.That(manifest.operatorInitialFreeCount, Is.EqualTo(5));
            Assert.That(manifest.operatorEliteCount, Is.EqualTo(10));
            Assert.That(manifest.operatorTotalCount, Is.EqualTo(15));
            Assert.That(manifest.enemyCount, Is.EqualTo(6));
            Assert.That(manifest.eliteEnemyCount, Is.EqualTo(2));
            Assert.That(manifest.bossCount, Is.EqualTo(1));
            Assert.That(manifest.stateCountPerResource, Is.EqualTo(5));
            Assert.That(manifest.framesPerState, Is.EqualTo(4));
            Assert.That(manifest.presentationOnly, Is.True);
            Assert.That(manifest.hasCombatMath, Is.False);
            Assert.That(manifest.hasServerSubmitPayload, Is.False);
            Assert.That(manifest.hasGachaTruth, Is.False);
            Assert.That(manifest.railChoiceIncluded, Is.False);
            Assert.That(manifest.worldRaidIncluded, Is.False);
            Assert.That(manifest.resources.Length, Is.EqualTo(Resources.Length));

            foreach (var expected in Resources)
            {
                var entry = Array.Find(manifest.resources, item => item.id == expected.Id);
                Assert.That(entry, Is.Not.Null, expected.Id);
                Assert.That(entry.kind, Is.EqualTo(expected.Kind), expected.Id);
                Assert.That(entry.qualityClass, Is.EqualTo("HIGH_QUALITY_2D_ILLUSTRATION"), expected.Id);
                Assert.That(entry.showcaseVisible, Is.True, expected.Id);
                Assert.That(entry.presentationOnly, Is.True, expected.Id);
                var expectedStates = StatesFor(expected);
                Assert.That(entry.states.Length, Is.EqualTo(expectedStates.Length), expected.Id);
                Assert.That(entry.frameCount, Is.EqualTo(expectedStates.Length * 4), expected.Id);
                Assert.That(entry.resolution, Is.EqualTo("512x512"), expected.Id);
                Assert.That(entry.sourceArtPath, Does.StartWith("Assets/_LASTRAIL/UI/"), expected.Id);
                foreach (var state in expectedStates)
                {
                    Assert.That(Array.Exists(entry.states, value => value == state), Is.True, expected.Id + ":" + state);
                }
            }
        }

        [Test]
        public void CombatAnimationFrameSequences_AreImportableSpritesWithFourFramesPerState()
        {
            foreach (var resource in Resources)
            {
                foreach (var state in StatesFor(resource))
                {
                    for (var i = 1; i <= 4; i++)
                    {
                        var path = FramePath(resource, state, i);
                        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        Assert.That(texture, Is.Not.Null, path);
                        Assert.That(texture.width, Is.EqualTo(512), path);
                        Assert.That(texture.height, Is.EqualTo(512), path);

                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        Assert.That(importer, Is.Not.Null, path);
                        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                        Assert.That(importer.mipmapEnabled, Is.False, path);
                        Assert.That(importer.alphaIsTransparency, Is.True, path);
                        Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(1024), path);
                    }
                }
            }
        }

        [Test]
        public void CombatAnimationResources_AreCombatUnitSpritesNotPortraitOrCardDerived()
        {
            var manifestFile = ProjectPath(ManifestPath);
            Assert.That(File.Exists(manifestFile), Is.True, ManifestPath);
            var manifest = JsonUtility.FromJson<CombatAnimationManifestFixture>(File.ReadAllText(manifestFile));

            foreach (var entry in manifest.resources)
            {
                Assert.That(entry.spriteType, Is.EqualTo("combat_unit"), entry.id);
                Assert.That(entry.portraitDerived, Is.False, entry.id);
                Assert.That(entry.hasCardFrame, Is.False, entry.id);
                Assert.That(entry.transparentOrSceneCompatible, Is.True, entry.id);
                Assert.That(entry.sourceArtPath, Does.Contain("/CombatAnimations/Sources/"), entry.id);
                Assert.That(entry.sourceArtPath, Does.Not.Contain("/Operators/Portraits/"), entry.id);
                Assert.That(entry.sourceArtPath, Does.Not.Contain("/Operators/Cards/"), entry.id);
                Assert.That(entry.sourceArtPath, Does.Not.Contain("/Operators/Icons/"), entry.id);
                Assert.That(File.Exists(ProjectPath(entry.sourceArtPath)), Is.True, entry.id + " source cutout missing");

                foreach (var state in entry.states)
                {
                    for (var frame = 1; frame <= 4; frame++)
                    {
                        var framePath = "Assets/_LASTRAIL/UI/CombatAnimations/" + FolderFor(entry.kind) + "/" + entry.id + "/" + state + "/" + entry.id + "_" + state + "_" + frame.ToString("000") + ".png";
                        AssertFrameHasTransparentUnitPadding(framePath);
                    }
                }
            }
        }

        [Test]
        public void CombatAnimationClipsAndControllers_ExistForEveryAnimatedResource()
        {
            foreach (var resource in Resources)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(resource));
                Assert.That(controller, Is.Not.Null, resource.Id);
                foreach (var state in StatesFor(resource))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(resource, state));
                    Assert.That(clip, Is.Not.Null, resource.Id + ":" + state);
                    Assert.That(clip.frameRate, Is.EqualTo(12f), resource.Id + ":" + state);
                    Assert.That(clip.length, Is.GreaterThan(0f), resource.Id + ":" + state);
                }
            }
        }

        [Test]
        public void CombatAnimationShowcase_IsPortraitQaReadyAndUsesTmpInputSystem()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null, ShowcasePrefabPath);
            var scaler = prefab.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(prefab.GetComponentsInChildren<Animator>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(Resources.Length));
            Assert.That(prefab.GetComponentsInChildren<Image>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(Resources.Length));
            Assert.That(prefab.GetComponentsInChildren<Text>(includeInactive: true), Is.Empty);
            Assert.That(prefab.GetComponentInChildren<StandaloneInputModule>(includeInactive: true), Is.Null);
            Assert.That(prefab.GetComponentInChildren<InputSystemUIInputModule>(includeInactive: true), Is.Not.Null);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.That(font, Is.Not.Null);
            Assert.That(material, Is.Not.Null);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                Assert.That(text.font, Is.EqualTo(font), text.name);
                Assert.That(text.fontSharedMaterial, Is.EqualTo(material), text.name);
                Assert.That(text.overflowMode, Is.EqualTo(TextOverflowModes.Ellipsis).Or.EqualTo(TextOverflowModes.Truncate), text.name);
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ShowcaseScenePath);
            Assert.That(sceneAsset, Is.Not.Null, ShowcaseScenePath);
            var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
            try
            {
                Assert.That(scene.IsValid(), Is.True);
                var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
                Assert.That(eventSystems.Length, Is.EqualTo(1));
                Assert.That(eventSystems[0].GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
                Assert.That(eventSystems[0].GetComponent<StandaloneInputModule>(), Is.Null);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void CombatAnimationReadmeAndManifest_DoNotIntroduceTruthSecretsOrServerPayloads()
        {
            Assert.That(File.Exists(ProjectPath(ReadmePath)), Is.True, ReadmePath);
            var roots = new[]
            {
                ProjectPath("Assets/_LASTRAIL/UI/CombatAnimations"),
                ProjectPath(ManifestPath)
            };
            var forbidden = new[]
            {
                "accessToken",
                "refreshToken",
                "idToken",
                "client_secret",
                "accountId",
                "Authorization",
                "rawRng",
                "probability",
                "fullWeights",
                "rewardAmount",
                "damageFormula",
                "combatDamage",
                "server submit",
                "RailChoice",
                "World Raid",
                "PlayerPrefs"
            };

            foreach (var root in roots)
            {
                if (File.Exists(root))
                {
                    AssertForbiddenTokens(root, forbidden);
                    continue;
                }

                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".controller", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".anim", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AssertForbiddenTokens(file, forbidden);
                }
            }
        }

        private static void AssertForbiddenTokens(string file, string[] forbidden)
        {
            var text = File.ReadAllText(file);
            foreach (var value in forbidden)
            {
                Assert.That(text, Does.Not.Contain(value), file + " leaked forbidden launch boundary token: " + value);
            }
        }

        private static string FramePath(ExpectedAnimatedResource resource, string state, int frame)
        {
            return "Assets/_LASTRAIL/UI/CombatAnimations/" + FolderFor(resource.Kind) + "/" + resource.Id + "/" + state + "/" + resource.Id + "_" + state + "_" + frame.ToString("000") + ".png";
        }

        private static string ClipPath(ExpectedAnimatedResource resource, string state)
        {
            return "Assets/_LASTRAIL/UI/CombatAnimations/Clips/" + FolderFor(resource.Kind) + "/" + resource.Id + "/" + resource.Id + "_" + state + ".anim";
        }

        private static string ControllerPath(ExpectedAnimatedResource resource)
        {
            return "Assets/_LASTRAIL/UI/CombatAnimations/Animators/" + FolderFor(resource.Kind) + "/" + resource.Id + ".controller";
        }

        private static string FolderFor(string kind)
        {
            switch (kind)
            {
                case "operator_initial_free": return "Operators";
                case "operator_elite": return "Operators";
                case "enemy": return "Enemies";
                case "elite_enemy": return "EliteEnemies";
                case "boss": return "Bosses";
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }


        private static void AssertFrameHasTransparentUnitPadding(string assetPath)
        {
            var fullPath = ProjectPath(assetPath);
            Assert.That(File.Exists(fullPath), Is.True, assetPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(texture.LoadImage(File.ReadAllBytes(fullPath)), Is.True, assetPath);
            var pixels = texture.GetPixels32();
            var transparentBorderPixels = 0;
            var sampledBorderPixels = 0;
            for (var y = 0; y < texture.height; y += Math.Max(1, texture.height / 16))
            {
                for (var x = 0; x < texture.width; x += Math.Max(1, texture.width / 16))
                {
                    var isBorder = x < texture.width / 10 || x > texture.width - texture.width / 10 || y < texture.height / 10 || y > texture.height - texture.height / 10;
                    if (!isBorder)
                    {
                        continue;
                    }

                    sampledBorderPixels++;
                    if (pixels[y * texture.width + x].a < 32)
                    {
                        transparentBorderPixels++;
                    }
                }
            }

            Assert.That(transparentBorderPixels, Is.GreaterThan(sampledBorderPixels / 3), assetPath + " should have transparent/scene-compatible unit padding, not an opaque portrait/card background.");
        }
        private static ExpectedAnimatedResource Resource(string id, string kind) => new ExpectedAnimatedResource(id, kind);
        private static string[] StatesFor(ExpectedAnimatedResource resource) => resource.Kind == "boss" ? BossStates : UnitStates;
        private static string ProjectPath(string assetPath) => Path.Combine(Directory.GetCurrentDirectory(), assetPath);

        private readonly struct ExpectedAnimatedResource
        {
            public ExpectedAnimatedResource(string id, string kind)
            {
                Id = id;
                Kind = kind;
            }

            public string Id { get; }
            public string Kind { get; }
        }

        [Serializable]
        private sealed class CombatAnimationManifestFixture
        {
            public string name;
            public string referenceResolution;
            public int operatorInitialFreeCount;
            public int operatorEliteCount;
            public int operatorTotalCount;
            public int enemyCount;
            public int eliteEnemyCount;
            public int bossCount;
            public int stateCountPerResource;
            public int framesPerState;
            public bool presentationOnly;
            public string spriteType;
            public bool portraitDerived;
            public bool hasCardFrame;
            public bool transparentOrSceneCompatible;
            public bool hasCombatMath;
            public bool hasServerSubmitPayload;
            public bool hasGachaTruth;
            public bool railChoiceIncluded;
            public bool worldRaidIncluded;
            public CombatAnimationResourceFixture[] resources;
        }

        [Serializable]
        private sealed class CombatAnimationResourceFixture
        {
            public string id;
            public string kind;
            public string[] states;
            public int frameCount;
            public string resolution;
            public string sourceArtPath;
            public string qualityClass;
            public bool showcaseVisible;
            public bool presentationOnly;
            public string spriteType;
            public bool portraitDerived;
            public bool hasCardFrame;
            public bool transparentOrSceneCompatible;
        }
    }
}
#endif
