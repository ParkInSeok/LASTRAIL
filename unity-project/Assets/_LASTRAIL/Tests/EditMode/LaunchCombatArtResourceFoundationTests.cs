#if UNITY_5_3_OR_NEWER
using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchCombatArtResourceFoundationTests
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-combat-art-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/CombatResources/README.md";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/CombatResources/Showcase/LRLaunchCombatArtResourceShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchCombatArtResourceShowcase.unity";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        private static readonly ExpectedCombatResource[] EnemyResources =
        {
            new ExpectedCombatResource("enemy_raider_scout", "normal", "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_raider_scout.png", 768, 768),
            new ExpectedCombatResource("enemy_railbike_marauder", "normal", "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_railbike_marauder.png", 768, 768),
            new ExpectedCombatResource("enemy_scrapshield_brute", "normal", "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_scrapshield_brute.png", 768, 768),
            new ExpectedCombatResource("enemy_signal_jammer_drone", "normal", "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_signal_jammer_drone.png", 768, 768),
            new ExpectedCombatResource("enemy_spike_cart_gunner", "normal", "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_spike_cart_gunner.png", 768, 768),
            new ExpectedCombatResource("enemy_cinder_hound_mech", "normal", "Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_cinder_hound_mech.png", 768, 768),
            new ExpectedCombatResource("elite_broadcast_executioner", "elite", "Assets/_LASTRAIL/UI/CombatResources/Elites/elite_broadcast_executioner.png", 896, 896),
            new ExpectedCombatResource("elite_ironclad_boarder", "elite", "Assets/_LASTRAIL/UI/CombatResources/Elites/elite_ironclad_boarder.png", 896, 896),
            new ExpectedCombatResource("boss_raider_king_engine", "boss", "Assets/_LASTRAIL/UI/CombatResources/Bosses/boss_raider_king_engine.png", 1536, 1024)
        };

        private static readonly ExpectedCombatResource[] VfxResources =
        {
            new ExpectedCombatResource("vfx_basic_hit_spark", "hit", "Assets/_LASTRAIL/UI/CombatResources/VFX/vfx_basic_hit_spark.png", 512, 512),
            new ExpectedCombatResource("vfx_skill_activation", "skill", "Assets/_LASTRAIL/UI/CombatResources/VFX/vfx_skill_activation.png", 512, 512),
            new ExpectedCombatResource("vfx_ultimate_activation", "ultimate", "Assets/_LASTRAIL/UI/CombatResources/VFX/vfx_ultimate_activation.png", 512, 512),
            new ExpectedCombatResource("vfx_shield_status", "shield", "Assets/_LASTRAIL/UI/CombatResources/VFX/vfx_shield_status.png", 512, 512),
            new ExpectedCombatResource("vfx_heal_status", "heal", "Assets/_LASTRAIL/UI/CombatResources/VFX/vfx_heal_status.png", 512, 512),
            new ExpectedCombatResource("vfx_buff_debuff_status", "buff_debuff", "Assets/_LASTRAIL/UI/CombatResources/VFX/vfx_buff_debuff_status.png", 512, 512),
            new ExpectedCombatResource("vfx_story_raid_danger", "raid_danger", "Assets/_LASTRAIL/UI/CombatResources/VFX/vfx_story_raid_danger.png", 512, 512),
            new ExpectedCombatResource("vfx_boss_defeat_burst", "boss_defeat", "Assets/_LASTRAIL/UI/CombatResources/VFX/vfx_boss_defeat_burst.png", 512, 512)
        };

        [Test]
        public void CombatArtResources_ExistAsImportableSpritesWithLaunchDimensions()
        {
            foreach (var resource in EnemyResources)
            {
                AssertSprite(resource.Path, resource.Width, resource.Height);
            }

            foreach (var resource in VfxResources)
            {
                AssertSprite(resource.Path, resource.Width, resource.Height);
            }
        }

        [Test]
        public void CombatArtManifest_RecordsEnemyBossVfxCoverageAndBoundaries()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ManifestPath);
            Assert.That(File.Exists(fullPath), Is.True, ManifestPath);
            var manifest = JsonUtility.FromJson<CombatArtManifestFixture>(File.ReadAllText(fullPath));
            Assert.That(manifest.name, Is.EqualTo("LAST RAIL Act 1 Combat Art Resources"));
            Assert.That(manifest.referenceResolution, Is.EqualTo("1080x1920"));
            Assert.That(manifest.normalEnemyCount, Is.EqualTo(6));
            Assert.That(manifest.eliteEnemyCount, Is.EqualTo(2));
            Assert.That(manifest.bossCount, Is.EqualTo(1));
            Assert.That(manifest.vfxCount, Is.EqualTo(8));
            Assert.That(manifest.generatedBy, Is.EqualTo("OpenAI image_gen built-in tool"));
            Assert.That(manifest.serverTruthFieldsAllowed, Is.False);
            Assert.That(manifest.combatMathIncluded, Is.False);
            Assert.That(manifest.railsChoiceIncluded, Is.False);
            Assert.That(manifest.worldRaidIncluded, Is.False);
            Assert.That(manifest.enemies.Length, Is.EqualTo(EnemyResources.Length));
            Assert.That(manifest.vfx.Length, Is.EqualTo(VfxResources.Length));

            foreach (var expected in EnemyResources)
            {
                var match = Array.Find(manifest.enemies, item => item.id == expected.Id);
                Assert.That(match, Is.Not.Null, expected.Id);
                Assert.That(match.type, Is.EqualTo(expected.Type), expected.Id);
                Assert.That(match.path, Is.EqualTo(expected.Path), expected.Id);
                Assert.That(match.sourceType, Is.EqualTo("generated_local_launch_asset"), expected.Id);
            }

            foreach (var expected in VfxResources)
            {
                var match = Array.Find(manifest.vfx, item => item.id == expected.Id);
                Assert.That(match, Is.Not.Null, expected.Id);
                Assert.That(match.category, Is.EqualTo(expected.Type), expected.Id);
                Assert.That(match.path, Is.EqualTo(expected.Path), expected.Id);
            }
        }

        [Test]
        public void CombatArtReadme_ProvidesSourceLicenseAndUsageNotes()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ReadmePath);
            Assert.That(File.Exists(fullPath), Is.True, ReadmePath);
            var readme = File.ReadAllText(fullPath);
            Assert.That(readme, Does.Contain("OpenAI image_gen built-in tool"));
            Assert.That(readme, Does.Contain("project-generated launch combat resource"));
            Assert.That(readme, Does.Contain("No external paid asset dependency"));
            Assert.That(readme, Does.Contain("presentation resources only"));
        }

        [Test]
        public void CombatArtShowcase_IsPortraitQaReadyWithReadableTmpOverlay()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null, ShowcasePrefabPath);
            var scaler = prefab.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(prefab.GetComponentsInChildren<Image>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(EnemyResources.Length + VfxResources.Length));
            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(EnemyResources.Length));
            Assert.That(prefab.GetComponentsInChildren<Text>(includeInactive: true), Is.Empty);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.That(font, Is.Not.Null);
            Assert.That(material, Is.Not.Null);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                Assert.That(text.font, Is.EqualTo(font), text.name);
                Assert.That(text.fontSharedMaterial, Is.EqualTo(material), text.name);
                Assert.That(text.overflowMode, Is.EqualTo(TextOverflowModes.Ellipsis).Or.EqualTo(TextOverflowModes.Truncate), text.name);
                Assert.That(text.color.a, Is.GreaterThan(0.8f), text.name);
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
        public void CombatArtResources_DoNotIntroduceTruthSecretOrServerPayloads()
        {
            var roots = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "CombatResources"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Style")
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
                "rewardAmount",
                "damageFormula",
                "combatDamage",
                "server submit",
                "RailChoice",
                "World Raid"
            };

            foreach (var root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var text = File.ReadAllText(file);
                    foreach (var value in forbidden)
                    {
                        Assert.That(text, Does.Not.Contain(value), file + " leaked forbidden launch boundary token: " + value);
                    }
                }
            }
        }

        private static void AssertSprite(string path, int width, int height)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(width), path + " width");
            Assert.That(texture.height, Is.EqualTo(height), path + " height");

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.mipmapEnabled, Is.False, path + " mipmaps");
            Assert.That(importer.sRGBTexture, Is.True, path + " sRGB");
            Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(2048), path + " max texture size");
        }

        private readonly struct ExpectedCombatResource
        {
            public ExpectedCombatResource(string id, string type, string path, int width, int height)
            {
                Id = id;
                Type = type;
                Path = path;
                Width = width;
                Height = height;
            }

            public string Id { get; }
            public string Type { get; }
            public string Path { get; }
            public int Width { get; }
            public int Height { get; }
        }

        [Serializable]
        private sealed class CombatArtManifestFixture
        {
            public string name;
            public string referenceResolution;
            public int normalEnemyCount;
            public int eliteEnemyCount;
            public int bossCount;
            public int vfxCount;
            public string generatedBy;
            public bool serverTruthFieldsAllowed;
            public bool combatMathIncluded;
            public bool railsChoiceIncluded;
            public bool worldRaidIncluded;
            public EnemyFixture[] enemies;
            public VfxFixture[] vfx;
        }

        [Serializable]
        private sealed class EnemyFixture
        {
            public string id;
            public string type;
            public string path;
            public string intendedUsage;
            public string sourceType;
        }

        [Serializable]
        private sealed class VfxFixture
        {
            public string id;
            public string category;
            public string path;
            public string usage;
            public string sourceType;
        }
    }
}
#endif
