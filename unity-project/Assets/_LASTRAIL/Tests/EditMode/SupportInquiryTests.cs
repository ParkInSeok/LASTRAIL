using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Support;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class SupportInquiryAvailabilityTests
    {
        [Test]
        public void LocalInquiryControllerExists()
        {
            Assert.That(typeof(LastRail.Diagnostics.AppBuildSnapshot).Assembly.GetType("LastRail.Support.SupportInquiryController"), Is.Not.Null);
        }

        [Test]
        public void LocalInquiryPanelExists()
        {
            Assert.That(typeof(AppBuildSnapshot).Assembly.GetType("LastRail.Support.SupportInquiryPanel"), Is.Not.Null);
        }
    }

    public sealed class SupportInquiryTests
    {
        private sealed class Client : ISupportInquiryClient
        {
            public readonly List<InquirySubmission> Requests = new List<InquirySubmission>();
            public TaskCompletionSource<InquiryReceipt> Completion = new TaskCompletionSource<InquiryReceipt>();
            public Task<InquiryReceipt> SubmitAsync(InquirySubmission request, GameApiAuthSessionSnapshot session)
            { Requests.Add(request); return Completion.Task; }
        }
        internal static GameApiAuthSessionSnapshot Session(string id = "account_test") => new GameApiAuthSessionSnapshot(
            id, "ACTIVE", new GameApiAccessToken("lst1.fixture"), DateTime.UtcNow.AddHours(1),
            new GameApiRefreshToken("fixture"), DateTime.UtcNow.AddDays(1));
        private static SupportInquiryController Controller(Client client) => new SupportInquiryController(
            Session(), client, new[] { "fixture_category" }, 100, () => new AppBuildSnapshot("1.2.3", 123));

        [Test]
        public async Task DoubleClickAndUncertainRetryPreserveEntireSnapshot()
        {
            var client = new Client(); var model = Controller(client);
            model.Edit("fixture_category", "fixture content");
            var first = model.SubmitAsync(); await model.SubmitAsync();
            Assert.That(client.Requests.Count, Is.EqualTo(1));
            Assert.That(model.Edit("fixture_category", "changed"), Is.False);
            client.Completion.SetResult(new InquiryReceipt(InquiryDelivery.Unknown)); await first;
            var frozen = model.Pending;
            Assert.That(model.State, Is.EqualTo(InquiryState.Uncertain));
            Assert.That(model.StartAnother(), Is.False);
            Assert.That(model.Edit("fixture_category", "changed"), Is.False);
            client.Completion = new TaskCompletionSource<InquiryReceipt>();
            var retry = model.SubmitAsync();
            Assert.That(client.Requests[1], Is.SameAs(frozen));
            client.Completion.SetResult(new InquiryReceipt(InquiryDelivery.Received, "ticket_fixture", DateTimeOffset.UtcNow));
            await retry; Assert.That(model.State, Is.EqualTo(InquiryState.Received));
            Assert.That(model.CanSubmit, Is.False);
        }

        [Test]
        public async Task ChangedAccountCannotSubmitOrAcceptLateReceipt()
        {
            var client = new Client(); var model = Controller(client);
            model.Edit("fixture_category", "fixture content"); var send = model.SubmitAsync();
            model.SetSession(Session("different_account"));
            client.Completion.SetResult(new InquiryReceipt(InquiryDelivery.Received, "ticket_fixture", DateTimeOffset.UtcNow));
            await send; await model.SubmitAsync();
            Assert.That(model.State, Is.EqualTo(InquiryState.Detached)); Assert.That(model.Receipt, Is.Null);
            Assert.That(client.Requests.Count, Is.EqualTo(1));
            model.SetSession(Session()); Assert.That(model.State, Is.EqualTo(InquiryState.Uncertain));
            Assert.That(model.Pending, Is.SameAs(client.Requests[0]));
        }

        [Test]
        public async Task MissingTicketOrTimestampIsNotSuccess()
        {
            var client = new Client(); var model = Controller(client);
            model.Edit("fixture_category", "fixture content"); var send = model.SubmitAsync();
            client.Completion.SetResult(new InquiryReceipt(InquiryDelivery.Received, "ticket_fixture")); await send;
            Assert.That(model.State, Is.EqualTo(InquiryState.Uncertain)); Assert.That(model.Receipt, Is.Null);
        }

        [Test]
        public async Task SessionRefreshDoesNotOverlapOutstandingSubmission()
        {
            var client = new Client(); var model = Controller(client);
            model.Edit("fixture_category", "fixture content"); var first = model.SubmitAsync();
            model.SetSession(Session()); var second = model.SubmitAsync();
            client.Completion.SetResult(new InquiryReceipt(InquiryDelivery.Unknown)); await first; await second;
            Assert.That(client.Requests.Count, Is.EqualTo(1));
            Assert.That(model.CanSubmit, Is.True);
        }

        [Test]
        public async Task DefinitiveRejectionKeepsDraftAndAllowsCorrection()
        {
            var client = new Client(); var model = Controller(client);
            model.Edit("fixture_category", "fixture content"); var send = model.SubmitAsync();
            client.Completion.SetResult(new InquiryReceipt(InquiryDelivery.Rejected)); await send;
            Assert.That(model.State, Is.EqualTo(InquiryState.Rejected)); Assert.That(model.Content, Is.EqualTo("fixture content"));
            Assert.That(model.Pending, Is.Null); Assert.That(model.Edit("fixture_category", "correction"), Is.True);
        }

        [Test]
        public async Task ExceptionIsUnknownAndInputValidationDoesNotSend()
        {
            var client = new Client(); var model = Controller(client);
            Assert.That(model.Edit("invalid", "content"), Is.False);
            Assert.That(model.Edit("fixture_category", new string('x', 101)), Is.False);
            await model.SubmitAsync(); Assert.That(client.Requests, Is.Empty);
            model.Edit("fixture_category", "fixture content"); var send = model.SubmitAsync();
            client.Completion.SetException(new InvalidOperationException("sensitive fixture")); await send;
            Assert.That(model.State, Is.EqualTo(InquiryState.Uncertain)); Assert.That(model.Content, Is.EqualTo("fixture content"));
        }

        [TestCase(360, 640)]
        [TestCase(1080, 1920)]
        [TestCase(360, 480)]
        public void PanelRendersAndCloseReopenKeepsMemoryDraft(int width, int height)
        {
            var root = new GameObject("SupportFixture", typeof(RectTransform));
            var cameraObject = new GameObject("SupportCamera", typeof(Camera));
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                var model = Controller(new Client());
                model.Edit("fixture_category", "\uBB38\uC758 \uD14C\uC2A4\uD2B8 \uB0B4\uC6A9\uC785\uB2C8\uB2E4.");
                var panel = root.AddComponent<SupportInquiryPanel>();
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
                panel.Initialize(model, font, _ => "\uD14C\uC2A4\uD2B8 \uCE74\uD14C\uACE0\uB9AC");
                panel.Hide(); panel.Show();
                Assert.That(root.GetComponentInChildren<TMP_InputField>().text, Is.EqualTo(model.Content));
                var camera = cameraObject.GetComponent<Camera>();
                var canvasHeight = 1080f * height / width;
                camera.orthographic = true; camera.orthographicSize = canvasHeight / 2; camera.aspect = width / (float)height;
                camera.transform.position = new Vector3(0, 0, -10);
                root.GetComponent<CanvasScaler>().enabled = false;
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
                var rect = root.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1080, canvasHeight); rect.position = Vector3.zero;
                Canvas.ForceUpdateCanvases();
                panel.Show(); Canvas.ForceUpdateCanvases();
                if (height == 480)
                {
                    var scroll = root.GetComponentInChildren<ScrollRect>();
                    Assert.That(scroll, Is.Not.Null, "Short viewports need a scrollable form.");
                    scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
                    var closeCorners = new Vector3[4];
                    root.GetComponentsInChildren<Button>().Single(b => b.name == "Close").GetComponent<RectTransform>().GetWorldCorners(closeCorners);
                    Assert.That(closeCorners[0].y, Is.GreaterThanOrEqualTo(-canvasHeight / 2 - 1));
                }
                foreach (var text in root.GetComponentsInChildren<TMP_Text>())
                {
                    text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name);
                    var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                    Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(-541), text.name);
                    Assert.That(corners[2].x, Is.LessThanOrEqualTo(541), text.name);
                    if (height != 480) Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(-canvasHeight / 2 - 1), text.name);
                }
                camera.targetTexture = target; RenderTexture.active = target; camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32();
                Assert.That(pixels.Count(p => p.r != pixels[0].r || p.g != pixels[0].g), Is.GreaterThan(pixels.Length / 10));
                Directory.CreateDirectory("QA/Act1Launch/Screenshots/SupportInquiry");
                File.WriteAllBytes("QA/Act1Launch/Screenshots/SupportInquiry/draft_" + width + "_" + height + ".png", texture.EncodeToPNG());
                model.SetSession(Session("other"));
                Assert.That(root.GetComponentInChildren<TMP_InputField>().text, Is.Empty);
            }
            finally
            {
                RenderTexture.active = previous; cameraObject.GetComponent<Camera>().targetTexture = null;
                target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [UnityTest]
        public IEnumerator ExpandedCategoryItemsFitAndSelectionUpdatesModel()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("CategoryFixture", typeof(RectTransform));
            var events = new GameObject("EventFixture", typeof(EventSystem));
            try
            {
                var model = new SupportInquiryController(Session(), new Client(), new[] { "first", "second" }, 100,
                    () => new AppBuildSnapshot(null, null));
                var panel = root.AddComponent<SupportInquiryPanel>();
                panel.Initialize(model, AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset"), key => key);
                var dropdown = root.GetComponentInChildren<TMP_Dropdown>();
                yield return null;
                dropdown.Show(); Canvas.ForceUpdateCanvases();
                var list = dropdown.transform.Find("Dropdown List"); Assert.That(list, Is.Not.Null);
                var toggles = list.GetComponentsInChildren<Toggle>();
                Assert.That(toggles.Length, Is.EqualTo(2));
                var listContent = list.GetComponent<ScrollRect>().content;
                Assert.That(listContent.rect.height, Is.GreaterThanOrEqualTo(220));
                toggles[1].isOn = true;
                Assert.That(model.Category, Is.EqualTo("second"));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(events); }
            yield return new ExitPlayMode();
        }
    }
}
