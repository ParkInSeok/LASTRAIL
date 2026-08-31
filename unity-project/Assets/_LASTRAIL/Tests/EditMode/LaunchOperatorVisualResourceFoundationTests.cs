#if UNITY_EDITOR
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
