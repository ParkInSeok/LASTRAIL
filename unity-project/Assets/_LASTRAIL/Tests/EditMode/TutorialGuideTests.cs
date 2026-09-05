#if UNITY_EDITOR
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LastRail.Presentation.Tutorial;
using LastRail.Presentation.Startup;
using LastRail.Presentation.AuthGate;
using LastRail.Presentation.Battle;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class TutorialGuideTests
    {
        private const string Prefab = "Assets/_LASTRAIL/Prefabs/Screens/TutorialGuide.prefab";
        [Test]
        public void ReusablePrefab_ExistsWithoutReplacingScreen()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Canvas>(), Is.Null, "Uses the owner's existing canvas");
        }

        [UnityTest]
        public IEnumerator RealTarget_CallbacksRaycastsTrackingAndInvalidation()
        {
            yield return new EnterPlayMode();
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
            var eventObject = new GameObject("GuideEventSystem", typeof(EventSystem));
            try
            {
                var owner = root.GetComponent<StartupScreenRootBehaviour>();
                var guide = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), root.transform).GetComponent<TutorialGuideView>();
                var safe = Find<RectTransform>(root, "SafeArea");
                var target = Find<Button>(root, "StartupPrimary");
                var targetCalls = 0; owner.PrimaryActionRequested += () => targetCalls++;
                owner.RenderAuth(AuthGateUxState.FirstLaunch());
                Canvas.ForceUpdateCanvases();
                var next = Find<Button>(guide.gameObject, "GuideNext");
                var close = Find<Button>(guide.gameObject, "GuideClose");
                Assert.That(guide.Show(owner, safe, (RectTransform)target.transform, "fixture", "\uBC84\uD2BC \uC548\uB0B4"), Is.True, guide.LastClearReason);
                Assert.That(next.interactable, Is.False);
                Assert.That(close.interactable, Is.False);
                next.onClick.Invoke(); Assert.That(guide.IsVisible, Is.True, "No fake progress");
                var calls = 0;
                for (var i = 0; i < 4; i++) Assert.That(guide.Show(owner, safe, (RectTransform)target.transform, "fixture", "\uBC84\uD2BC \uC548\uB0B4", () => calls++, () => calls++), Is.True);
                yield return null;
                Canvas.ForceUpdateCanvases();
                var raycaster = root.GetComponent<GraphicRaycaster>();
                var eventData = new PointerEventData(eventObject.GetComponent<EventSystem>());
                var hits = new List<RaycastResult>();
                eventData.position = RectTransformUtility.WorldToScreenPoint(null, target.transform.position);
                raycaster.Raycast(eventData, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(hits[0].gameObject.transform.IsChildOf(guide.transform), Is.False, "Target must remain accessible");
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, eventData, ExecuteEvents.pointerClickHandler);
                Assert.That(targetCalls, Is.EqualTo(1), "Existing auth display callback reached, not a login");
                hits.Clear();
                eventData.position = RectTransformUtility.WorldToScreenPoint(null, Find<RectTransform>(guide.gameObject, "GuidePanel").position);
                raycaster.Raycast(eventData, hits);
                Assert.That(hits[0].gameObject.name, Is.EqualTo("GuidePanel"), "Panel background blocks click-through");
                hits.Clear();
                eventData.position = RectTransformUtility.WorldToScreenPoint(null, next.transform.position);
                raycaster.Raycast(eventData, hits);
                Assert.That(hits[0].gameObject.transform.IsChildOf(guide.transform), Is.True, "Guide control does not click through");
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, eventData, ExecuteEvents.pointerClickHandler);
                next.onClick.Invoke(); Assert.That(calls, Is.EqualTo(1)); Assert.That(guide.IsVisible, Is.False);
                guide.Show(owner, safe, (RectTransform)target.transform, "move", "\uBC84\uD2BC \uC548\uB0B4", () => calls++, () => calls++);
                var old = guide.TargetBounds;
                ((RectTransform)target.transform).anchoredPosition += new Vector2(0, 20);
                yield return null;
                Assert.That(guide.TargetBounds.y, Is.EqualTo(old.y + 20).Within(1));
                close.onClick.Invoke(); Assert.That(calls, Is.EqualTo(2));
                guide.Show(owner, safe, (RectTransform)target.transform, "inactive", "fixture", () => calls++);
                target.gameObject.SetActive(false); yield return null;
                Assert.That(guide.IsVisible, Is.False); next.onClick.Invoke(); Assert.That(calls, Is.EqualTo(2));
                target.gameObject.SetActive(true);
                guide.Show(owner, safe, (RectTransform)target.transform, "hidden_graphic", "fixture", () => calls++);
                target.GetComponent<Image>().enabled = false; yield return null;
                Assert.That(guide.IsVisible, Is.False); target.GetComponent<Image>().enabled = true;
                guide.Show(owner, safe, (RectTransform)target.transform, "owner_exit", "fixture", () => calls++);
                owner.enabled = false; yield return null;
                Assert.That(guide.IsVisible, Is.False);
                owner.enabled = true;
                guide.Show(owner, safe, (RectTransform)target.transform, "destroyed", "fixture", () => calls++);
                Object.Destroy(target.gameObject); yield return null;
                Assert.That(guide.IsVisible, Is.False); next.onClick.Invoke(); Assert.That(calls, Is.EqualTo(2));
                root.SetActive(false); root.SetActive(true);
                Assert.That(guide.IsVisible, Is.False, "No automatic tutorial on reentry");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(eventObject); }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor() { if (Application.isPlaying) yield return new ExitPlayMode(); }

        [TestCase(1080, 1920, "button")]
        [TestCase(360, 640, "button")]
        [TestCase(1080, 1920, "operator")]
        [TestCase(360, 640, "operator")]
        [TestCase(1080, 1920, "skill")]
        [TestCase(360, 640, "skill")]
        public void CaptureActualScreensWithExternalGuideFixture(int width, int height, string kind)
        {
            var screenPath = "Assets/_LASTRAIL/Prefabs/Screens/" + (kind == "button" ? "StartupScreen" : "FullyAnimatedCombatHud") + ".prefab";
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(screenPath));
            var cameraObject = new GameObject("GuideFixtureCamera", typeof(Camera));
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var render = new RenderTexture(width, height, 24);
            var previous = RenderTexture.active;
            try
            {
                Behaviour owner;
                if (kind == "button")
                {
                    var startup = root.GetComponent<StartupScreenRootBehaviour>();
                    startup.RenderAuth(AuthGateUxState.FirstLaunch()); owner = startup;
                }
                else
                {
                    var combat = root.GetComponent<FullyAnimatedCombatHudRootBehaviour>();
                    combat.Render(CombatTransitionTrackerTests.State(1, false)); owner = combat;
                }
                var guide = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), root.transform).GetComponent<TutorialGuideView>();
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = new Vector2(1080, 1920); rect.position = Vector3.zero; rect.localScale = Vector3.one;
                var safe = Find<RectTransform>(root, "SafeArea");
                safe.anchorMin = new Vector2(0, 48f / 1920); safe.anchorMax = new Vector2(1, 1 - 72f / 1920);
                // Existing SafeArea includes an extra 24-unit design margin; guide input is the device safe rectangle.
                var deviceSafe = new GameObject("FixtureDeviceSafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
                deviceSafe.SetParent(root.transform, false);
                deviceSafe.anchorMin = safe.anchorMin; deviceSafe.anchorMax = safe.anchorMax;
                deviceSafe.offsetMin = deviceSafe.offsetMax = Vector2.zero;
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 960; camera.aspect = width / (float)height;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                camera.transform.position = new Vector3(0, 0, -10);
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) { canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera; }
                Canvas.ForceUpdateCanvases();
                var targetName = kind == "button" ? "StartupPrimary" : kind == "operator" ? "OperatorAnimated_op_kyle" : "SkillIconSlot_0";
                var target = Find<RectTransform>(root, targetName);
                var avoid = new List<RectTransform>();
                foreach (var image in root.GetComponentsInChildren<Image>())
                    if (image.name.StartsWith("OperatorAnimated_") || image.name.StartsWith("EnemyAnimated_") || image.name.StartsWith("SkillIconSlot_")) avoid.Add(image.rectTransform);
                var label = kind == "button" ? "\uBC84\uD2BC \uC548\uB0B4" : kind == "operator" ? "\uC694\uC6D0 \uC601\uC5ED \uC548\uB0B4" : "\uC2A4\uD0AC \uC544\uC774\uCF58 \uC548\uB0B4";
                var shown = guide.Show(owner, deviceSafe, target, "fixture." + kind, label, () => { }, () => { }, avoid.ToArray());
                if (!shown)
                {
                    var tc = new Vector3[4]; var sc = new Vector3[4]; target.GetWorldCorners(tc); safe.GetWorldCorners(sc);
                    Debug.Log("Guide " + kind + " target " + tc[0] + ".." + tc[2] + " safe " + sc[0] + ".." + sc[2]);
                }
                Assert.That(shown, Is.True, guide.LastClearReason);
                Assert.That(guide.PanelBounds.Overlaps(guide.TargetBounds), Is.False);
                var panelBounds = WorldBounds(Find<RectTransform>(guide.gameObject, "GuidePanel"));
                var safeBounds = WorldBounds(deviceSafe);
                Assert.That(safeBounds.Contains(panelBounds.min) && safeBounds.Contains(panelBounds.max), Is.True);
                foreach (var text in owner.GetComponentsInChildren<TMP_Text>())
                    if (!text.transform.IsChildOf(guide.transform) && !string.IsNullOrEmpty(text.text))
                        Assert.That(panelBounds.Overlaps(WorldBounds(text.rectTransform)), Is.False, text.name);
                foreach (var protectedRect in avoid) Assert.That(panelBounds.Overlaps(WorldBounds(protectedRect)), Is.False, protectedRect.name);
                if (kind != "skill") Assert.That(Find<Image>(guide.gameObject, "GuidePointer").gameObject.activeSelf, Is.True, "Pointer has safe space");
                foreach (var text in guide.GetComponentsInChildren<TMP_Text>()) { text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False); }
                foreach (var border in guide.GetComponentsInChildren<Image>())
                    if (border.name.StartsWith("GuideBorder") || border.name == "GuidePointer") Assert.That(border.raycastTarget, Is.False);
                camera.targetTexture = render; RenderTexture.active = render; camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32(); var varied = 0;
                foreach (var pixel in pixels) if (pixel.r != pixels[0].r || pixel.g != pixels[0].g || pixel.b != pixels[0].b) varied++;
                Assert.That(varied, Is.GreaterThan(pixels.Length / 10), "Actual nonblank render, no composite fallback");
                Directory.CreateDirectory("QA/Act1Launch/Screenshots/Tutorial");
                File.WriteAllBytes("QA/Act1Launch/Screenshots/Tutorial/fixture_" + kind + "_" + width + ".png", texture.EncodeToPNG());
                // Boundary and crowding invalidate the step and clear callbacks, not clamp onto unrelated UI.
                Assert.That(guide.Show(owner, safe, target, "blocked", label, () => Assert.Fail("stale callback"), null, new[] { safe }), Is.False);
                Find<Button>(guide.gameObject, "GuideNext").onClick.Invoke();
                target.anchoredPosition += new Vector2(0, 5000);
                Assert.That(guide.Show(owner, safe, target, "offscreen", label), Is.False);
            }
            finally
            {
                RenderTexture.active = previous; cameraObject.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(texture); render.Release(); Object.DestroyImmediate(render);
                Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject);
            }
        }

        private static T Find<T>(GameObject root, string name) where T : Component
        {
            foreach (var item in root.GetComponentsInChildren<T>(true)) if (item.name == name) return item;
            Assert.Fail("Missing " + name); return null;
        }
        private static Rect WorldBounds(RectTransform rect)
        {
            var points = new Vector3[4]; rect.GetWorldCorners(points);
            return new Rect(points[0].x, points[0].y, points[2].x - points[0].x, points[2].y - points[0].y);
        }
    }
}
#endif
