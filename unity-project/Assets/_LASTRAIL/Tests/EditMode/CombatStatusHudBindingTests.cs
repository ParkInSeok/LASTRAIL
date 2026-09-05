#if UNITY_EDITOR
using System.IO;
using LastRail.Presentation.Battle;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatStatusHudBindingTests
    {
        private const string Prefab = "Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab";

        [Test]
        public void Render_ReplacesCuesClearsStaleIconsAndKeepsUnknownDataHidden()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            try
            {
                var hud = root.GetComponent<FullyAnimatedCombatHudRootBehaviour>();
                var bindings = new SerializedObject(hud).FindProperty("statusIconSprites");
                Assert.That(bindings.arraySize, Is.EqualTo(10));
                for (var i = 0; i < 10; i++)
                {
                    Assert.That(bindings.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                    Assert.That(bindings.GetArrayElementAtIndex(i).objectReferenceValue,
                        Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>(CombatStatusIconCatalog.IconPaths[i])));
                }

                foreach (var cue in new[] { "shield", "heal", "buff", "debuff" })
                {
                    hud.Render(CombatStatusCueTests.CreateState("vfx_hit_spark", "combat_" + cue));
                    var icon = Find<Image>(root, "StatusIcon");
                    Assert.That(icon.gameObject.activeInHierarchy, Is.True);
                    Assert.That(icon.sprite, Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>(
                        CombatStatusIconCatalog.IconPaths[CombatStatusIconCatalog.CueIconIndex(cue)])));
                }

                hud.Render(CombatStatusCueTests.CreateState("vfx_hit_spark", "combat_hit"));
                Assert.That(Find<RectTransform>(root, "CombatStatusCueHud").gameObject.activeSelf, Is.False);
                Assert.That(Find<Image>(root, "StatusIcon").sprite, Is.Null);
                Assert.That(Find<TMP_Text>(root, "StatusLabel").text, Is.Empty);
                Assert.That(Find<RectTransform>(root, "UnconnectedCombatStatusLayers").gameObject.activeSelf, Is.False);
                Assert.That(Find<Image>(root, "CooldownFill").fillAmount, Is.Zero);
                Assert.That(Find<TMP_Text>(root, "CooldownValue").text, Is.Empty);
                Assert.That(root.GetComponentsInChildren<RectTransform>(true),
                    Has.Exactly(1).Property("name").EqualTo("CombatStatusCueHud"));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(1080, 1920, "combat_heal", "vfx_shield_status")]
        [TestCase(360, 640, "combat_heal", "vfx_shield_status")]
        [TestCase(1080, 1920, "combat_buff", "vfx_hit_spark")]
        [TestCase(360, 640, "combat_buff", "vfx_hit_spark")]
        [TestCase(1080, 1920, "combat_debuff", "vfx_hit_spark")]
        [TestCase(360, 640, "combat_debuff", "vfx_hit_spark")]
        public void CaptureActualHud_WithExplicitCueFixture(int width, int height, string sfx, string vfx)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var cameraObject = new GameObject("CombatStatusQaCamera", typeof(Camera));
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                root.GetComponent<FullyAnimatedCombatHudRootBehaviour>().Render(CombatStatusCueTests.CreateState(vfx, sfx, true));
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1080, 1920);
                rect.position = Vector3.zero;
                rect.localScale = Vector3.one;
                var safe = new SerializedObject(root.GetComponent<FullyAnimatedCombatHudRootBehaviour>())
                    .FindProperty("safeAreaRoot").objectReferenceValue as RectTransform;
                safe.anchorMin = Vector2.zero;
                safe.anchorMax = Vector2.one;
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 960;
                camera.aspect = width / (float)height;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.worldCamera = camera;
                }
                Canvas.ForceUpdateCanvases();
                foreach (var label in root.GetComponentsInChildren<TMP_Text>())
                {
                    label.ForceMeshUpdate();
                    if (label.name == "StatusLabel") Assert.That(label.isTextOverflowing, Is.False);
                }
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                var pixels = texture.GetPixels32();
                var varied = 0;
                var first = pixels[0];
                foreach (var pixel in pixels)
                    if (pixel.r != first.r || pixel.g != first.g || pixel.b != first.b) varied++;
                Assert.That(varied, Is.GreaterThan(pixels.Length / 10), "Uniform render; no synthetic fallback allowed.");
                var output = "QA/Act1Launch/Screenshots/CombatStatus";
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, "fixture_" + sfx + "_" + width + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(texture);
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static T Find<T>(GameObject root, string name) where T : Component
        {
            foreach (var item in root.GetComponentsInChildren<T>(true))
                if (item.name == name) return item;
            Assert.Fail("Missing " + name);
            return null;
        }
    }
}
#endif
