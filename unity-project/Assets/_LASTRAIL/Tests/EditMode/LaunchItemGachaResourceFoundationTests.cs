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
    public sealed class LaunchItemGachaResourceFoundationTests
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-item-gacha-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/GachaResources/README.md";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/GachaResources/Showcase/LRLaunchItemGachaResourceShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchItemGachaResourceShowcase.unity";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        private static readonly ExpectedResource[] ItemResources =
        {
            new ExpectedResource("cur_command_ticket", "currency", "Assets/_LASTRAIL/UI/GachaResources/Icons/cur_command_ticket.png", 512, 512),
            new ExpectedResource("cur_command_ticket_fragment", "currency", "Assets/_LASTRAIL/UI/GachaResources/Icons/cur_command_ticket_fragment.png", 512, 512),
            new ExpectedResource("cur_special_core", "currency", "Assets/_LASTRAIL/UI/GachaResources/Icons/cur_special_core.png", 512, 512),
            new ExpectedResource("ui_fuel_stamina", "item", "Assets/_LASTRAIL/UI/GachaResources/Icons/ui_fuel_stamina.png", 512, 512),
            new ExpectedResource("ui_supply_crate", "item", "Assets/_LASTRAIL/UI/GachaResources/Icons/ui_supply_crate.png", 512, 512),
            new ExpectedResource("ui_reward_chest", "item", "Assets/_LASTRAIL/UI/GachaResources/Icons/ui_reward_chest.png", 512, 512),
            new ExpectedResource("ui_operator_duplicate_token", "item", "Assets/_LASTRAIL/UI/GachaResources/Icons/ui_operator_duplicate_token.png", 512, 512),
            new ExpectedResource("ui_common_material_plate", "item", "Assets/_LASTRAIL/UI/GachaResources/Icons/ui_common_material_plate.png", 512, 512),
            new ExpectedResource("ui_common_spark_plate", "item", "Assets/_LASTRAIL/UI/GachaResources/Icons/ui_common_spark_plate.png", 512, 512)
        };

        private static readonly ExpectedResource[] GachaResources =
        {
            new ExpectedResource("gacha_single_pull_reveal_plate", "gacha_fx", "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_single_pull_reveal_plate.png", 768, 768),
            new ExpectedResource("gacha_ten_pull_result_grid_frame", "frame", "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_ten_pull_result_grid_frame.png", 1024, 768),
            new ExpectedResource("gacha_rare_glow", "gacha_fx", "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_rare_glow.png", 768, 768),
            new ExpectedResource("gacha_elite_glow", "gacha_fx", "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_elite_glow.png", 768, 768),
            new ExpectedResource("gacha_pickup_elite_glow", "gacha_fx", "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_pickup_elite_glow.png", 768, 768),
            new ExpectedResource("gacha_ticket_spend_pieces", "gacha_fx", "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_ticket_spend_pieces.png", 768, 768),
            new ExpectedResource("gacha_duplicate_conversion_visual", "gacha_fx", "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_duplicate_conversion_visual.png", 768, 768),
            new ExpectedResource("gacha_recruitment_aperture_open", "gacha_fx", "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_recruitment_aperture_open.png", 768, 768),
            new ExpectedResource("gacha_result_card_frame", "frame", "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_result_card_frame.png", 768, 1024)
        };

        [Test]
        public void ItemAndGachaResources_ExistAsImportableSpritesWithLaunchDimensions()
        {
            foreach (var resource in ItemResources) AssertSprite(resource.Path, resource.Width, resource.Height);
            foreach (var resource in GachaResources) AssertSprite(resource.Path, resource.Width, resource.Height);
        }

        [Test]
        public void ItemGachaManifest_RecordsResourceCoverageAndPresentationOnlyBoundaries()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ManifestPath);
            Assert.That(File.Exists(fullPath), Is.True, ManifestPath);
            var manifest = JsonUtility.FromJson<ItemGachaManifestFixture>(File.ReadAllText(fullPath));
            Assert.That(manifest.name, Is.EqualTo("LAST RAIL Act 1 Item And Gacha Visual Resources"));
            Assert.That(manifest.referenceResolution, Is.EqualTo("1080x1920"));
            Assert.That(manifest.itemIconCount, Is.EqualTo(ItemResources.Length));
            Assert.That(manifest.gachaResourceCount, Is.EqualTo(GachaResources.Length));
            Assert.That(manifest.generatedBy, Is.EqualTo("OpenAI image_gen built-in tool"));
            Assert.That(manifest.serverTruthFieldsAllowed, Is.False);
            Assert.That(manifest.economyTruthIncluded, Is.False);
            Assert.That(manifest.gachaRateTruthIncluded, Is.False);
            Assert.That(manifest.railsChoiceIncluded, Is.False);
            Assert.That(manifest.worldRaidIncluded, Is.False);
            Assert.That(manifest.items.Length, Is.EqualTo(ItemResources.Length));
            Assert.That(manifest.gacha.Length, Is.EqualTo(GachaResources.Length));

            foreach (var expected in ItemResources)
            {
                var match = Array.Find(manifest.items, item => item.id == expected.Id);
                Assert.That(match, Is.Not.Null, expected.Id);
                Assert.That(match.category, Is.EqualTo(expected.Category), expected.Id);
                Assert.That(match.path, Is.EqualTo(expected.Path), expected.Id);
                Assert.That(match.sourceType, Is.EqualTo("generated_local_launch_asset"), expected.Id);
            }

            foreach (var expected in GachaResources)
            {
                var match = Array.Find(manifest.gacha, item => item.id == expected.Id);
                Assert.That(match, Is.Not.Null, expected.Id);
                Assert.That(match.category, Is.EqualTo(expected.Category), expected.Id);
                Assert.That(match.path, Is.EqualTo(expected.Path), expected.Id);
            }
        }

        [Test]
        public void ItemGachaReadme_ProvidesSourceLicenseAndUsageNotes()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ReadmePath);
            Assert.That(File.Exists(fullPath), Is.True, ReadmePath);
            var readme = File.ReadAllText(fullPath);
            Assert.That(readme, Does.Contain("OpenAI image_gen built-in tool"));
            Assert.That(readme, Does.Contain("project-generated launch item and gacha visual resources"));
            Assert.That(readme, Does.Contain("No external paid asset dependency"));
            Assert.That(readme, Does.Contain("presentation resources only"));
        }

        [Test]
        public void ItemGachaShowcase_IsPortraitQaReadyWithReadableTmpOverlay()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null, ShowcasePrefabPath);
            var scaler = prefab.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(prefab.GetComponentsInChildren<Image>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(ItemResources.Length + GachaResources.Length));
            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(ItemResources.Length));
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
        public void ItemGachaResources_DoNotIntroduceTruthSecretOrServerPayloads()
        {
            var roots = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "GachaResources"),
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
                "fullWeights",
                "rewardAmount",
                "itemGrant",
                "economyGrant",
                "server payload",
                "RailChoice",
                "World Raid"
            };

            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;
                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
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

        private readonly struct ExpectedResource
        {
            public ExpectedResource(string id, string category, string path, int width, int height)
            {
                Id = id;
                Category = category;
                Path = path;
                Width = width;
                Height = height;
            }

            public string Id { get; }
            public string Category { get; }
            public string Path { get; }
            public int Width { get; }
            public int Height { get; }
        }

        [Serializable]
        private sealed class ItemGachaManifestFixture
        {
            public string name;
            public string referenceResolution;
            public int itemIconCount;
            public int gachaResourceCount;
            public string generatedBy;
            public bool serverTruthFieldsAllowed;
            public bool economyTruthIncluded;
            public bool gachaRateTruthIncluded;
            public bool railsChoiceIncluded;
            public bool worldRaidIncluded;
            public ResourceFixture[] items;
            public ResourceFixture[] gacha;
        }

        [Serializable]
        private sealed class ResourceFixture
        {
            public string id;
            public string category;
            public string path;
            public string intendedUsage;
            public string sourceType;
        }
    }
}
#endif
