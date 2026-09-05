#if UNITY_EDITOR
using System.Collections;
using System.IO;
using LastRail.Auth;
using LastRail.Presentation.AuthGate;
using LastRail.Presentation.Startup;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class StartupScreenViewTests
    {
        private const string Prefab = "Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab";

        [Test]
        public void PrefabReferences_AllSerializedObjectsResolve()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            Assert.That(root, Is.Not.Null);
            var serialized = new SerializedObject(root.GetComponent<StartupScreenRootBehaviour>());
            foreach (var field in new[] { "safeAreaRoot", "statusText", "detailsText", "progressText", "progressTrack",
                "progressFill", "statusIcon", "primaryButton", "secondaryButton", "primaryText", "secondaryText" })
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var sprites = serialized.FindProperty("statusSprites");
            Assert.That(sprites.arraySize, Is.EqualTo(5));
            for (var i = 0; i < sprites.arraySize; i++)
                Assert.That(sprites.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null, "status sprite " + i);
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                if (image.name != "TitleScrim" && image.name != "StartupActionBand")
                    Assert.That(image.sprite, Is.Not.Null, image.name);
        }

        [UnityTest]
        public IEnumerator Callbacks_AreSingleBoundAndBusyErrorsReentryClearStaleState()
        {
            yield return new EnterPlayMode();
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            try
            {
                var view = root.GetComponent<StartupScreenRootBehaviour>();
                var primary = Find<Button>(root, "StartupPrimary");
                var secondary = Find<Button>(root, "StartupSecondary");
                Assert.That(primary.interactable, Is.False, "No callback owner is installed.");
                var calls = 0;
                view.PrimaryActionRequested += () => calls++;
                view.SecondaryActionRequested += () => calls++;
                for (var i = 0; i < 4; i++) view.RenderAuth(AuthGateUxState.FirstLaunch());
                primary.onClick.Invoke();
                primary.onClick.Invoke();
                Assert.That(calls, Is.EqualTo(1));
                view.Render(StartupScreenViewState.Waiting());
                primary.onClick.Invoke();
                Assert.That(calls, Is.EqualTo(1));
                view.Render(StartupScreenViewState.Downloading(25, 100));
                Assert.That(Find<Image>(root, "StartupProgressFill").fillAmount, Is.EqualTo(0.25f));
                view.Render(StartupScreenViewState.DownloadError("fixture network error"));
                Assert.That(Find<Image>(root, "StartupProgressTrack").gameObject.activeSelf, Is.False);
                Assert.That(Find<Image>(root, "StartupProgressFill").fillAmount, Is.Zero);
                Assert.That(Find<TMP_Text>(root, "StartupProgressText").text, Is.Empty);
                primary.onClick.Invoke();
                Assert.That(calls, Is.EqualTo(2));
                root.SetActive(false);
                primary.onClick.Invoke();
                Assert.That(calls, Is.EqualTo(2));
                root.SetActive(true);
                view.RenderAuth(AuthGateUxState.FirstLaunch());
                secondary.onClick.Invoke();
                Assert.That(calls, Is.EqualTo(3));
            }
            finally { Object.DestroyImmediate(root); }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor() { if (Application.isPlaying) yield return new ExitPlayMode(); }

        [TestCase(1080, 1920)]
        [TestCase(360, 640)]
        public void CaptureReusableView_WithExplicitStateFixtures(int width, int height)
        {
            foreach (var scenario in new[] { "auth_start", "loading", "download", "download_unknown", "download_error", "update", "auth_error" })
                Capture(width, height, scenario);
        }

        private static void Capture(int width, int height, string scenario)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var cameraObject = new GameObject("StartupFixtureCamera", typeof(Camera));
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                var view = root.GetComponent<StartupScreenRootBehaviour>();
                view.PrimaryActionRequested += () => { };
                view.SecondaryActionRequested += () => { };
                view.Render(Fixture(scenario));
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1080, 1920);
                rect.position = Vector3.zero;
                rect.localScale = Vector3.one;
                var safe = Find<RectTransform>(root, "SafeArea");
                safe.anchorMin = new Vector2(0, 48f / 1920);
                safe.anchorMax = new Vector2(1, 1 - 72f / 1920);
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
                var safeBounds = Bounds(safe);
                foreach (var text in root.GetComponentsInChildren<TMP_Text>())
                {
                    text.ForceMeshUpdate();
                    Assert.That(text.isTextOverflowing, Is.False, scenario + ":" + text.name);
                    AssertContained(safeBounds, Bounds(text.rectTransform), text.name);
                }
                foreach (var button in root.GetComponentsInChildren<Button>())
                {
                    AssertContained(safeBounds, Bounds(button.GetComponent<RectTransform>()), button.name);
                    Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(144), "48px touch target at 360px width");
                }
                var status = Bounds(Find<TMP_Text>(root, "StartupStatus").rectTransform);
                var details = Bounds(Find<TMP_Text>(root, "StartupDetails").rectTransform);
                Assert.That(status.Overlaps(details), Is.False);
                Assert.That(details.Overlaps(Bounds(Find<TMP_Text>(root, "StartupProgressText").rectTransform)), Is.False);
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                var pixels = texture.GetPixels32();
                var varied = 0;
                foreach (var pixel in pixels)
                    if (pixel.r != pixels[0].r || pixel.g != pixels[0].g || pixel.b != pixels[0].b) varied++;
                Assert.That(varied, Is.GreaterThan(pixels.Length / 10), "Uniform actual render; no composite fallback.");
                var output = "QA/Act1Launch/Screenshots/Startup";
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, "fixture_" + scenario + "_" + width + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(texture);
                target.Release(); Object.DestroyImmediate(target);
                Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject);
            }
        }

        private static StartupScreenViewState Fixture(string scenario)
        {
            switch (scenario)
            {
                case "loading": return StartupScreenViewState.Waiting();
                case "download": return StartupScreenViewState.Downloading(67108864, 268435456);
                case "download_unknown": return StartupScreenViewState.Downloading(67108864, null);
                case "download_error": return StartupScreenViewState.DownloadError("\uC5F0\uACB0\uC744 \uD655\uC778\uD55C \uD6C4 \uB2E4\uC2DC \uC2DC\uB3C4\uD574 \uC8FC\uC138\uC694.");
                case "update": return StartupScreenViewState.UpdateNotice("2.0.0", "\uC0C8 \uBC84\uC804 \uC548\uB0B4\uB97C \uD655\uC778\uD574 \uC8FC\uC138\uC694.");
                case "auth_error": return StartupScreenViewState.FromAuth(AuthGateUxState.FromRefreshResult(GameApiRefreshAuthClientResult.NetworkError("fixture timeout")));
                default: return StartupScreenViewState.FromAuth(AuthGateUxState.FirstLaunch());
            }
        }

        private static T Find<T>(GameObject root, string name) where T : Component
        {
            foreach (var item in root.GetComponentsInChildren<T>(true)) if (item.name == name) return item;
            Assert.Fail("Missing " + name); return null;
        }
        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }
        private static void AssertContained(Rect parent, Rect child, string name)
        {
            Assert.That(child.xMin, Is.GreaterThanOrEqualTo(parent.xMin - 1), name);
            Assert.That(child.xMax, Is.LessThanOrEqualTo(parent.xMax + 1), name);
            Assert.That(child.yMin, Is.GreaterThanOrEqualTo(parent.yMin - 1), name);
            Assert.That(child.yMax, Is.LessThanOrEqualTo(parent.yMax + 1), name);
        }
    }
}
#endif
