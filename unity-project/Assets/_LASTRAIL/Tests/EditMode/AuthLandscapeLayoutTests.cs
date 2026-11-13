using System.Linq;
using System.Threading.Tasks;
using LastRail.Run;
using LastRail.Auth;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class AuthLandscapeLayoutTests
    {
        private static void UpdatePanelLayout(GameObject root)
        {
            // Exercise each panel's own layout callback without broadcasting to child behaviours.
            Component panel = root.GetComponent<PlayerEntryPanel>();
            if (panel == null) panel = root.GetComponent<PasswordCredentialPanel>();
            Assert.That(panel, Is.Not.Null);
            var update = panel.GetType().GetMethod("OnRectTransformDimensionsChange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(update, Is.Not.Null);
            update.Invoke(panel, null);
        }
        [Test]
        public void EntryUsesLandscapeCanvasAndSeparatesIdentityFromInput()
        {
            var root = new GameObject("EntryLandscapeTest", typeof(RectTransform));
            try
            {
                root.AddComponent<PlayerEntryPanel>().Initialize(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset"));
                Canvas.ForceUpdateCanvases();
                UpdatePanelLayout(root);
                Assert.That(root.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
                var methods = root.transform.Find("SafeArea/Viewport/Form/LoginMethods");
                var brand = (RectTransform)methods.Find("Brand");
                var actions = (RectTransform)methods.Find("MethodActions");
                Assert.That(actions.anchoredPosition.x, Is.GreaterThan(brand.anchoredPosition.x + brand.rect.width));
                Assert.That(root.transform.Find("SafeArea").GetComponent<ScrollRect>().horizontal, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(1920, 1080)]
        [TestCase(2520, 1080)]
        [TestCase(1440, 1080)]
        public void EntryPagesKeepActionsInsideSafeWidthAndScrollableAtLandscapeRatios(int width, int height)
        {
            var root = new GameObject("EntryRatioTest", typeof(RectTransform));
            try
            {
                var panel = root.AddComponent<PlayerEntryPanel>();
                panel.Initialize(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset"));
                root.GetComponent<CanvasScaler>().enabled = false;
                root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                float canvasWidth = 1920 * Mathf.Sqrt((width / (float)height) / (16f / 9));
                ((RectTransform)root.transform).sizeDelta = new Vector2(canvasWidth, canvasWidth * height / width);
                var scroll = root.GetComponentInChildren<ScrollRect>();
                var safe = (RectTransform)scroll.transform;
                var pages = new System.Action[]
                {
                    () => panel.ShowLogin(true, true),
                    () => root.GetComponentsInChildren<Button>(true).Single(b => b.name == "ChooseIdLogin").onClick.Invoke(),
                    () => panel.ShowNickname(),
                    () => panel.ShowError("연결 상태를 확인하고 다시 시도해 주세요.", true),
                    () => panel.ShowBusy("연결 중입니다.")
                };
                foreach (var show in pages)
                {
                    show(); Canvas.ForceUpdateCanvases(); UpdatePanelLayout(root); Canvas.ForceUpdateCanvases();
                    Assert.That(safe.anchorMin.x, Is.EqualTo(Screen.safeArea.xMin / Screen.width).Within(.001f));
                    Assert.That(safe.anchorMax.x, Is.EqualTo(Screen.safeArea.xMax / Screen.width).Within(.001f));
                    var activeButtons = root.GetComponentsInChildren<Button>();
                    Assert.That(activeButtons, Is.Not.Empty);
                    foreach (var button in activeButtons)
                    {
                        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content, (RectTransform)button.transform);
                        Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(scroll.content.rect.xMin - 1), button.name);
                        Assert.That(bounds.max.x, Is.LessThanOrEqualTo(scroll.content.rect.xMax + 1), button.name);
                        Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(scroll.content.rect.yMin - 1), button.name);
                        Assert.That(bounds.max.y, Is.LessThanOrEqualTo(scroll.content.rect.yMax + 1), button.name);
                    }
                    scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
                    var bottomAction = activeButtons.OrderBy(button =>
                        RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content, (RectTransform)button.transform).min.y).First();
                    var bottomBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, (RectTransform)bottomAction.transform);
                    Assert.That(bottomBounds.min.y, Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin - 1), bottomAction.name);
                    Assert.That(bottomBounds.max.y, Is.LessThanOrEqualTo(scroll.viewport.rect.yMax + 1), bottomAction.name);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
        private sealed class NoNetwork : IRunApiHttpTransport
        {
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            { throw new System.InvalidOperationException("Layout must not dispatch requests."); }
        }

        [Test]
        public void RegistrationUsesTwoColumnsAndReflowPreservesSecretUntilHidden()
        {
            var root = new GameObject("CredentialLandscapeTest", typeof(RectTransform));
            try
            {
                var flow = new PasswordCredentialFlow(new PasswordEmailClient(new NoNetwork()), "Register",
                    () => null, () => 0, _ => Task.FromResult(true), () => System.DateTimeOffset.UtcNow);
                var panel = root.AddComponent<PasswordCredentialPanel>();
                panel.Initialize(flow, AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset"));
                Canvas.ForceUpdateCanvases();
                UpdatePanelLayout(root);
                var form = root.transform.Find("SafeArea/Viewport/Form");
                var id = (RectTransform)form.Find("LoginIdField");
                var passwordRow = (RectTransform)form.Find("PasswordField");
                Assert.That(passwordRow.anchoredPosition.x, Is.GreaterThan(id.anchoredPosition.x + id.rect.width));
                Assert.That(passwordRow.anchoredPosition.y, Is.EqualTo(id.anchoredPosition.y));
                var password = passwordRow.GetComponentInChildren<TMP_InputField>();
                password.SetTextWithoutNotify("synthetic_layout_secret");
                UpdatePanelLayout(root);
                Assert.That(password.text, Is.EqualTo("synthetic_layout_secret"));
                var scroll = root.GetComponentInChildren<ScrollRect>();
                Assert.That(scroll.viewport.offsetMin.y, Is.EqualTo(24));
                root.GetComponent<AuthKeyboardAvoidance>().EvaluateEditorKeyboard(Screen.height * .45f, password);
                Assert.That(scroll.viewport.offsetMin.y, Is.GreaterThanOrEqualTo(24));
                panel.Hide();
                Assert.That(password.text, Is.Empty);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [TestCase(1460)]
        [TestCase(1920)]
        [TestCase(2560)]
        public void EntryColumnsKeepControlsInsideAvailableWidth(float width)
        {
            var root = new GameObject("EntryGeometry", typeof(RectTransform));
            try
            {
                var page = AuthFormVisuals.Rect("Methods", root.transform);
                AuthFormVisuals.Rect("Brand", page);
                AuthFormVisuals.Rect("Welcome", page);
                var actions = AuthFormVisuals.Rect("MethodActions", page);
                foreach(var name in new[]{"ChooseMethodPrompt","Guest","ChooseIdLogin","Google","GuestStorageNotice","Terms","Privacy","Support"})
                    AuthFormVisuals.Rect(name, actions);
                AuthFormVisuals.Rect("FooterNotice", page);
                AuthLandscapeLayout.EntryMethods(page, width);
                foreach(RectTransform child in actions)
                {
                    Assert.That(child.anchoredPosition.x, Is.GreaterThanOrEqualTo(0));
                    Assert.That(child.anchoredPosition.x + child.rect.width, Is.LessThanOrEqualTo(actions.rect.width + .1f));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
