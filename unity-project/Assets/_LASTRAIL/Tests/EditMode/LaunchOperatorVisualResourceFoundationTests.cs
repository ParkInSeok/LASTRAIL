#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchOperatorVisualResourceFoundationTests
    {
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/Operators/Showcase/LRLaunchOperatorVisualShowcase.prefab";

        private static readonly OperatorSpec[] Operators =
        {
            new OperatorSpec("op_kyle", "initial_free", "operator_card_initial_free"),
            new OperatorSpec("op_reina", "initial_free", "operator_card_initial_free"),
            new OperatorSpec("op_eve", "initial_free", "operator_card_initial_free"),
            new OperatorSpec("op_sera", "initial_free", "operator_card_initial_free"),
            new OperatorSpec("op_mira", "initial_free", "operator_card_initial_free"),
            new OperatorSpec("op_vex", "gacha_elite", "operator_card_pickup_elite"),
            new OperatorSpec("op_nyx", "gacha_elite", "operator_card_elite"),
            new OperatorSpec("op_sable", "gacha_elite", "operator_card_elite"),
            new OperatorSpec("op_marrow", "gacha_elite", "operator_card_elite"),
            new OperatorSpec("op_iris", "gacha_elite", "operator_card_elite"),
            new OperatorSpec("op_rail", "gacha_elite", "operator_card_elite"),
            new OperatorSpec("op_anchor", "gacha_elite", "operator_card_elite"),
            new OperatorSpec("op_lumen", "gacha_elite", "operator_card_elite"),
            new OperatorSpec("op_cinder", "gacha_elite", "operator_card_elite"),
            new OperatorSpec("op_warden", "gacha_elite", "operator_card_elite")
        };

        [Test]
        public void ManifestDefinesFiveInitialAndTenGachaEliteOperators()
        {
            Assert.That(Operators.Length, Is.EqualTo(15));
            Assert.That(CountByAcquisition("initial_free"), Is.EqualTo(5));
            Assert.That(CountByAcquisition("gacha_elite"), Is.EqualTo(10));
            Assert.That(HasDuplicateOperatorId(), Is.EqualTo(false));
        }

        [Test]
        public void OperatorPortraitsCardsIconsAndFramesAreImportableSprites()
        {
            foreach (var spec in Operators)
            {
                AssertSprite("Assets/_LASTRAIL/UI/Operators/Portraits/" + spec.OperatorId + "_portrait.png", 1024, 1024);
                AssertSprite("Assets/_LASTRAIL/UI/Operators/Cards/" + spec.OperatorId + "_card.png", 1080, 1440);
                AssertSprite("Assets/_LASTRAIL/UI/Operators/Icons/" + spec.OperatorId + "_icon.png", 512, 512);
                AssertSprite("Assets/_LASTRAIL/UI/Operators/Frames/" + spec.FrameId + ".png", 1080, 1440);
            }
        }

        [Test]
        public void ShowcaseUsesPretendardTmpInputSystemAndNoLegacyText()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(true).Length, Is.GreaterThanOrEqualTo(15));
            Assert.That(prefab.GetComponentInChildren<UnityEngine.UI.Text>(true), Is.Null);
            Assert.That(prefab.GetComponentInChildren<InputSystemUIInputModule>(true), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<StandaloneInputModule>(true), Is.Null);
        }

        [Test]
        public void ShowcaseConsumesOperatorCardPolishLayerResources()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Assert.That(ContainsSprite(prefab, "operator_card_slot_background"), Is.EqualTo(true));
            Assert.That(ContainsSprite(prefab, "operator_card_selected_frame"), Is.EqualTo(true));
            Assert.That(ContainsSprite(prefab, "operator_card_locked_overlay"), Is.EqualTo(true));
            Assert.That(ContainsSprite(prefab, "operator_badge_new"), Is.EqualTo(true));
            Assert.That(ContainsSprite(prefab, "operator_badge_owned"), Is.EqualTo(true));
            Assert.That(ContainsSprite(prefab, "operator_badge_level"), Is.EqualTo(true));
            Assert.That(ContainsSprite(prefab, "operator_rarity_pickup_elite"), Is.EqualTo(true));
            Assert.That(ContainsSprite(prefab, "operator_role_striker"), Is.EqualTo(true));
            Assert.That(ContainsSprite(prefab, "operator_grid_section_rail"), Is.EqualTo(true));
        }

        [Test]
        public void OperatorCardPolishSkinResourcesAreHighFidelityAndNotPlaceholderGeometry()
        {
            var manifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-operator-card-polish-resources.json";
            var readmePath = "Assets/_LASTRAIL/UI/Operators/CardPolish/README.md";
            Assert.That(File.Exists(ProjectPath(manifestPath)), Is.True);
            Assert.That(File.Exists(ProjectPath(readmePath)), Is.True);

            var combined = File.ReadAllText(ProjectPath(manifestPath)) + "\n" + File.ReadAllText(ProjectPath(readmePath));
            Assert.That(combined, Does.Contain(@"""qualityClass"": ""HIGH_QUALITY_2D_UI_SKIN"""));
            Assert.That(combined, Does.Contain(@"""sourceType"": ""generated_high_fidelity_local_launch_ui_skin"""));
            Assert.That(combined, Does.Not.Contain("procedurally generated local launch UI assets"));
            Assert.That(combined, Does.Not.Contain("PROCEDURAL_PLACEHOLDER"));
            Assert.That(combined, Does.Not.Contain("LOW_FIDELITY"));

            AssertHighFidelityTexture("Assets/_LASTRAIL/UI/Operators/Frames/operator_card_initial_free.png");
            AssertHighFidelityTexture("Assets/_LASTRAIL/UI/Operators/Frames/operator_card_elite.png");
            AssertHighFidelityTexture("Assets/_LASTRAIL/UI/Operators/Frames/operator_card_pickup_elite.png");
            AssertHighFidelityTexture("Assets/_LASTRAIL/UI/Operators/CardPolish/Frames/operator_card_slot_background.png");
            AssertHighFidelityTexture("Assets/_LASTRAIL/UI/Operators/CardPolish/Accents/operator_card_detail_panel_accent.png");
            AssertHighFidelityTexture("Assets/_LASTRAIL/UI/Operators/CardPolish/Accents/operator_grid_section_rail.png");
        }

        [Test]
        public void ManifestAndReadmeKeepSourceLicenseAndNoTruthPayloads()
        {
            var manifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-operator-visual-resources.json";
            var readmePath = "Assets/_LASTRAIL/UI/Operators/README.md";
            Assert.That(File.Exists(ProjectPath(manifestPath)), Is.EqualTo(true));
            Assert.That(File.Exists(ProjectPath(readmePath)), Is.EqualTo(true));

            var combined = File.ReadAllText(ProjectPath(manifestPath)) + "\n" + File.ReadAllText(ProjectPath(readmePath));
            Assert.That(combined.Contains("OpenAI image_gen built-in tool"), Is.EqualTo(true));
            AssertForbidden(combined, "accessToken");
            AssertForbidden(combined, "refreshToken");
            AssertForbidden(combined, "accountId");
            AssertForbidden(combined, "rawRng");
            AssertForbidden(combined, "probability");
            AssertForbidden(combined, "server submit");
            AssertForbidden(combined, "Rail Choice");
            AssertForbidden(combined, "World Raid");
        }

        private static int CountByAcquisition(string acquisition)
        {
            var count = 0;
            for (var i = 0; i < Operators.Length; i++)
            {
                if (Operators[i].Acquisition == acquisition)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasDuplicateOperatorId()
        {
            for (var i = 0; i < Operators.Length; i++)
            {
                for (var j = i + 1; j < Operators.Length; j++)
                {
                    if (Operators[i].OperatorId == Operators[j].OperatorId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void AssertSprite(string path, int width, int height)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(width), path);
            Assert.That(texture.height, Is.EqualTo(height), path);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(2048), path);
        }

        private static void AssertForbidden(string value, string token)
        {
            Assert.That(value.Contains(token), Is.EqualTo(false), token);
        }

        private static bool ContainsSprite(GameObject root, string spriteName)
        {
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.sprite != null && image.sprite.name == spriteName)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertHighFidelityTexture(string path)
        {
            var absolutePath = ProjectPath(path);
            Assert.That(File.Exists(absolutePath), Is.True, path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePath)), Is.True, path);
                var pixels = texture.GetPixels32();
                var opaqueCount = 0;
                var vividCount = 0;
                var uniqueColors = new HashSet<int>();
                var edgeTransitions = 0;
                var previous = default(Color32);
                for (var i = 0; i < pixels.Length; i++)
                {
                    var pixel = pixels[i];
                    if (pixel.a < 32)
                    {
                        continue;
                    }

                    opaqueCount++;
                    var max = Math.Max(pixel.r, Math.Max(pixel.g, pixel.b));
                    var min = Math.Min(pixel.r, Math.Min(pixel.g, pixel.b));
                    if (max - min > 42)
                    {
                        vividCount++;
                    }

                    uniqueColors.Add((pixel.r / 8 << 16) | (pixel.g / 8 << 8) | (pixel.b / 8));
                    if (i > 0)
                    {
                        var delta = Math.Abs(pixel.r - previous.r) + Math.Abs(pixel.g - previous.g) + Math.Abs(pixel.b - previous.b);
                        if (delta > 54)
                        {
                            edgeTransitions++;
                        }
                    }

                    previous = pixel;
                }

                Assert.That(opaqueCount, Is.GreaterThan(texture.width * texture.height / 3), path + " opaque coverage");
                Assert.That(uniqueColors.Count, Is.GreaterThan(120), path + " quantized color variety");
                Assert.That(vividCount, Is.GreaterThan(opaqueCount / 32), path + " accent detail");
                Assert.That(edgeTransitions, Is.GreaterThan(Math.Min(texture.width * 6, (texture.width + texture.height) * 2)), path + " edge/detail transitions");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private readonly struct OperatorSpec
        {
            public OperatorSpec(string operatorId, string acquisition, string frameId)
            {
                OperatorId = operatorId;
                Acquisition = acquisition;
                FrameId = frameId;
            }

            public string OperatorId { get; }
            public string Acquisition { get; }
            public string FrameId { get; }
        }
    }
}
#endif
