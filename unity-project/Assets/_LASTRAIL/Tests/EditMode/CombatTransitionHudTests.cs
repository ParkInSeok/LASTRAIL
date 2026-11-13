#if UNITY_EDITOR
using System.IO;
using System.Collections;
using LastRail.Presentation.Battle;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatTransitionHudTests
    {
        private const string Prefab = "Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab";

        [UnityTest]
        public IEnumerator ActualHud_DeduplicatesCancelsExpiresAndDoesNotReplayOnReentry()
        {
            yield return new EnterPlayMode();
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            try
            {
                var hud = root.GetComponent<FullyAnimatedCombatHudRootBehaviour>();
                hud.Render(CombatTransitionTrackerTests.State(1));
                Assert.That(root.GetComponentInChildren<CombatHudTransitionView>(true), Is.Null);
                hud.Render(CombatTransitionTrackerTests.State(2));
                var view = root.GetComponentInChildren<CombatHudTransitionView>(true);
                Assert.That(view, Is.Not.Null);
                Assert.That(view.PresentationCount, Is.EqualTo(1));
                Assert.That(view.AudioDispatchCount, Is.EqualTo(1));
                view.Tick(0.2f);
                var opacity = view.GetComponent<CanvasGroup>().alpha;
                hud.Render(CombatTransitionTrackerTests.State(2));
                Assert.That(view.PresentationCount, Is.EqualTo(1));
                Assert.That(view.AudioDispatchCount, Is.EqualTo(1));
                Assert.That(view.GetComponent<CanvasGroup>().alpha, Is.EqualTo(opacity));
                Assert.That(view.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                hud.CancelCombatTransition();
                AssertCleared(view);
                hud.Render(CombatTransitionTrackerTests.State(2));
                AssertCleared(view);
                hud.Render(CombatTransitionTrackerTests.State(3));
                view.Tick(2);
                AssertCleared(view);
                hud.Render(CombatTransitionTrackerTests.State(4));
                root.SetActive(false);
                AssertCleared(view);
                root.SetActive(true);
                hud.Render(CombatTransitionTrackerTests.State(4));
                AssertCleared(view);
                Assert.That(view.AudioDispatchCount, Is.EqualTo(3));
                hud.Render(CombatTransitionTrackerTests.State(5));
                hud.Render(CombatTransitionTrackerTests.State(7));
                AssertCleared(view);
                hud.Render(CombatTransitionTrackerTests.State(1, true));
                AssertCleared(view);
                Assert.That(root.GetComponentsInChildren<CombatHudTransitionView>(true).Length, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(root); }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator RestoreEditorMode()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [Test]
        public void Prefab_HasAllPreparedArtAndExistingAudioReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var serialized = new SerializedObject(prefab.GetComponent<FullyAnimatedCombatHudRootBehaviour>());
            var sprites = serialized.FindProperty("transitionSprites");
            Assert.That(sprites.arraySize, Is.EqualTo(7));
            for (var i = 0; i < sprites.arraySize; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Sprite>(CombatTransitionResourceCatalog.SpritePaths[i]);
                Assert.That(asset, Is.Not.Null);
                Assert.That(sprites.GetArrayElementAtIndex(i).objectReferenceValue, Is.EqualTo(asset));
            }
            var clips = serialized.FindProperty("transitionClips");
            Assert.That(clips.arraySize, Is.EqualTo(2));
            for (var i = 0; i < clips.arraySize; i++)
                Assert.That(clips.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
        }

        [TestCase(1080, 1920, false)]
        [TestCase(360, 640, false)]
        [TestCase(1080, 1920, true)]
        [TestCase(360, 640, true)]
        public void CaptureActualTransitionHud_WithExplicitAdjacentSnapshotFixture(int width, int height, bool boss)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var cameraObject = new GameObject("TransitionFixtureCamera", typeof(Camera));
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                var hud = root.GetComponent<FullyAnimatedCombatHudRootBehaviour>();
                hud.Render(CombatTransitionTrackerTests.State(1, boss));
                hud.Render(CombatTransitionTrackerTests.State(2, boss));
                var view = root.GetComponentInChildren<CombatHudTransitionView>(true);
                view.Tick(0.2f);
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1080, 1920);
                rect.position = Vector3.zero;
                rect.localScale = Vector3.one;
                var safe = new SerializedObject(hud).FindProperty("safeAreaRoot").objectReferenceValue as RectTransform;
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
                var label = view.GetComponentInChildren<TMP_Text>();
                label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing, Is.False);
                Assert.That(view.Current.Number, Is.EqualTo(2));
                var bannerBounds = Bounds(view.GetComponent<RectTransform>());
                var serializedHud = new SerializedObject(hud);
                var hint = serializedHud.FindProperty("refundHintText").objectReferenceValue as TMP_Text;
                var objective = serializedHud.FindProperty("objectiveText").objectReferenceValue as TMP_Text;
                hint.ForceMeshUpdate();
                Assert.That(hint.isTextOverflowing, Is.False);
                Assert.That(Bounds(hint.rectTransform).Overlaps(Bounds(objective.rectTransform)), Is.False);
                Assert.That(bannerBounds.Overlaps(Bounds(hint.rectTransform)), Is.False);
                foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>())
                {
                    if (image.name.StartsWith("OperatorAnimated_") || image.name.StartsWith("EnemyAnimated_")
                        || image.name.StartsWith("SkillIconSlot_") || image.name == "InteriorTrainCarBackgroundImage")
                        Assert.That(bannerBounds.Overlaps(Bounds(image.rectTransform)), Is.False, image.name);
                    if (image.name.StartsWith("SkillIconSlot_"))
                        Assert.That(Bounds(hint.rectTransform).Overlaps(Bounds(image.rectTransform)), Is.False, image.name);
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
                Assert.That(varied, Is.GreaterThan(pixels.Length / 10), "Uniform actual render; no composite fallback.");
                var output = "QA/Act1Launch/Screenshots/CombatTransitions";
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, "fixture_" + (boss ? "boss_phase" : "wave_start") + "_" + width + ".png"), texture.EncodeToPNG());
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

        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }

        private static void AssertCleared(CombatHudTransitionView view)
        {
            Assert.That(view.Current, Is.Null);
            Assert.That(view.gameObject.activeSelf, Is.False);
            Assert.That(view.GetComponent<AudioSource>().clip, Is.Null);
            Assert.That(view.GetComponent<AudioSource>().isPlaying, Is.False);
        }
    }
}
#endif
