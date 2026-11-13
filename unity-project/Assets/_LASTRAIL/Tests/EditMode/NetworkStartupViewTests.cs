using System.Collections;
using System.IO;
using System.Linq;
using LastRail.Presentation.AuthGate;
using LastRail.Presentation.Startup;
using LastRail.Resources;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class NetworkStartupViewTests
    {
        static readonly string CaptureDirectory="QA/Act1Launch/Screenshots/NetworkStartup/regression-"+System.Guid.NewGuid().ToString("N");
        [UnityTest]
        public IEnumerator ProgressDoesNotResetLoginActionsAndOverlayOwnsConsent()
        {
            yield return new EnterPlayMode();
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
            var ui = new GameObject("OverlayFixture", typeof(RectTransform));
            try
            {
                var view = root.GetComponent<StartupScreenRootBehaviour>(); var calls = 0;
                view.PrimaryActionRequested += () => calls++;
                var button = root.GetComponentsInChildren<Button>(true).Single(b => b.name == "StartupPrimary");
                view.Render(StartupScreenViewState.FromAuth(AuthGateUxState.FirstLaunch()).WithContent("40 / 100 MiB", 0.4f));
                button.onClick.Invoke();
                view.Render(StartupScreenViewState.FromAuth(AuthGateUxState.FirstLaunch()).WithContent("50 / 100 MiB", 0.5f));
                button.onClick.Invoke(); Assert.That(calls, Is.EqualTo(1), "Progress must not restart a pending login");
                var policy = new DownloadAccessPolicy(); policy.SetNetwork(ContentNetwork.Wifi); policy.SetPlan("a", 100);
                var overlay = ui.AddComponent<StartupDownloadOverlay>();
                overlay.Initialize(root.GetComponentInChildren<TMP_Text>(true).font, policy.Approve, policy.Decline, policy.Retry);
                policy.SetNetwork(ContentNetwork.OtherOnline); overlay.Show(policy.NeedsConsent, policy.RemainingBytes, false);
                Assert.That(overlay.ConsentVisible, Is.False);
                policy.SetAuthenticated(true); overlay.Show(policy.NeedsConsent, policy.RemainingBytes, false);
                Assert.That(overlay.ConsentVisible, Is.True);
                ui.GetComponentsInChildren<Button>().Single(b => b.name == "DeferDownload").onClick.Invoke();
                overlay.Show(policy.NeedsConsent, policy.RemainingBytes, true); Assert.That(overlay.ConsentVisible, Is.False);
                Assert.That(policy.Allowed, Is.False);
                using var network = new ContentNetworkService(); var changes = 0;
                network.Start(); network.Start(); network.NetworkStateChanged += _ => changes++;
                network.InjectForTests(ContentNetwork.Wifi); Assert.That(changes, Is.EqualTo(1));
                network.Dispose(); network.InjectForTests(ContentNetwork.OtherOnline); Assert.That(changes, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(ui); Object.DestroyImmediate(root); }
            yield return new ExitPlayMode();
        }

        [TestCase(640, 360, false)][TestCase(840, 360, false)][TestCase(1024, 768, false)]
        [TestCase(640, 360, true)][TestCase(840, 360, true)][TestCase(1024, 768, true)]
        public void LocalLoginProgressAndConsentAreReadable(int width, int height, bool consent)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
            var ui = new GameObject("OverlayFixture", typeof(RectTransform));
            var cameraObject = new GameObject("NetworkUiCamera", typeof(Camera));
            var target = new RenderTexture(width, height, 24); var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                root.GetComponent<StartupScreenRootBehaviour>().Render(StartupScreenViewState.FromAuth(AuthGateUxState.FirstLaunch()).WithContent("40.0 / 100.0 MiB", 0.4f));
                var overlay = ui.AddComponent<StartupDownloadOverlay>();
                overlay.Initialize(root.GetComponentInChildren<TMP_Text>(true).font, () => { }, () => { }, () => { });
                overlay.Show(consent, 100 * 1048576L, false);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 540; camera.aspect = width / (float)height;
                camera.transform.position = new Vector3(0, 0, -10); camera.backgroundColor = Color.black;
                foreach (var item in new[] { root, ui })
                {
                    foreach (var scaler in item.GetComponentsInChildren<CanvasScaler>()) scaler.enabled = false;
                    foreach (var canvas in item.GetComponentsInChildren<Canvas>()) { canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera; }
                    var rect = item.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1080f * width / height, 1080); rect.position = Vector3.zero; rect.localScale = Vector3.one;
                }
                Canvas.ForceUpdateCanvases();
                // World-space fixtures model the landscape canvas independently of the editor Game view.
                var safe = ui.transform.Find("DownloadSafeArea").GetComponent<RectTransform>();
                safe.anchorMin = new Vector2(.025f, .025f); safe.anchorMax = new Vector2(.975f, .975f);
                typeof(StartupDownloadOverlay).GetMethod("LayoutConsent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(overlay, null);
                Canvas.ForceUpdateCanvases();
                foreach (var text in root.GetComponentsInChildren<TMP_Text>().Concat(ui.GetComponentsInChildren<TMP_Text>()))
                {
                    text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name);
                    var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                    Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(-540f * width / height)); Assert.That(corners[2].x, Is.LessThanOrEqualTo(540f * width / height));
                }
                foreach (var button in ui.GetComponentsInChildren<Button>())
                {
                    var bounds = new Vector3[4]; button.GetComponent<RectTransform>().GetWorldCorners(bounds);
                    Assert.That((bounds[2].y - bounds[0].y) * height / 1080f, Is.GreaterThanOrEqualTo(48), button.name + " physical pixel height");
                    Assert.That(bounds[0].y, Is.GreaterThanOrEqualTo(-540 * .95f), button.name);
                    Assert.That(bounds[2].y, Is.LessThanOrEqualTo(540 * .95f), button.name);
                }
                if (consent)
                {
                    var approve = ui.GetComponentsInChildren<Button>().Single(b => b.name == "ApproveDownload").GetComponent<RectTransform>();
                    var defer = ui.GetComponentsInChildren<Button>().Single(b => b.name == "DeferDownload").GetComponent<RectTransform>();
                    var left = new Vector3[4]; var right = new Vector3[4]; approve.GetWorldCorners(left); defer.GetWorldCorners(right);
                    Assert.That(left[2].x, Is.LessThan(right[0].x), "Consent actions must remain separate side-by-side targets");
                }
                camera.targetTexture = target; RenderTexture.active = target; camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32(); Assert.That(pixels.Count(p => p.r != pixels[0].r || p.g != pixels[0].g), Is.GreaterThan(pixels.Length / 10));
                Directory.CreateDirectory(CaptureDirectory);
                var capturePath=Path.Combine(CaptureDirectory,(consent ? "consent_" : "login_progress_")+width+".png");File.WriteAllBytes(capturePath, texture.EncodeToPNG());Debug.Log("Startup regression capture: "+Path.GetFullPath(capturePath));
            }
            finally
            {
                RenderTexture.active = previous; cameraObject.GetComponent<Camera>().targetTexture = null;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(texture);
                Object.DestroyImmediate(ui); Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
