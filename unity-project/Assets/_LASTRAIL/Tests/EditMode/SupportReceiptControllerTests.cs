using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class SupportReceiptControllerTests
    {
        private const string Preview = "synthetic-private-receipt-preview";
        private const string Found = "{\"accepted\":true,\"receiptStatus\":\"FOUND\",\"ticketId\":\"ticket_fixture\",\"accountId\":\"account_test\",\"category\":\"BUG_OTHER\",\"serverReceivedAt\":\"2026-09-06T00:01:00.000Z\",\"bodyPreview\":\"synthetic-private-receipt-preview\",\"appVersion\":\"1.0\",\"appVersionCode\":\"1\"}";
        private const string Missing = "{\"accepted\":false,\"receiptStatus\":\"NOT_FOUND\",\"errorCode\":\"support_ticket_receipt_not_found\"}";
        private const string Limited = "{\"accepted\":false,\"ticketStatus\":\"REJECTED\",\"errorCode\":\"support_ticket_rate_limited\",\"retryAfterSeconds\":60}";

        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public RunApiHttpResponse Reply = RunApiHttpResponse.NetworkError("fixture response lost");
            public TaskCompletionSource<RunApiHttpResponse> Deferred;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Requests.Add(request);
                return Deferred == null ? Task.FromResult(Reply) : Deferred.Task;
            }
        }

        private sealed class Fixture
        {
            public readonly Wire Wire = new Wire();
            public readonly SupportInquiryController Model;
            public DateTimeOffset Now = new DateTimeOffset(2026, 9, 6, 0, 0, 0, TimeSpan.Zero);
            public Fixture()
            {
                RequireReconcile();
                var client = new SupportInquiryHttpClient(new GameApiAuthenticatedHttpTransport(Wire));
                Model = new SupportInquiryController(Session(), client, SupportInquiryHttpClient.Categories, 2000,
                    () => new AppBuildSnapshot("1.0", 1), () => Now, minContentLength: 10);
                Assert.That(Model.Edit("BUG_OTHER", "synthetic inquiry content"), Is.True);
            }
        }

        private static GameApiAuthSessionSnapshot Session(string account = "account_test", string token = "lst1.fixture")
            => new GameApiAuthSessionSnapshot(account, "ACTIVE", new GameApiAccessToken(token),
                DateTime.UtcNow.AddHours(1), new GameApiRefreshToken("fixture"), DateTime.UtcNow.AddDays(1));

        private static MethodInfo RequireReconcile()
        {
            var method = typeof(SupportInquiryController).GetMethod("ReconcileAsync", BindingFlags.Public | BindingFlags.Instance,
                null, Type.EmptyTypes, null);
            Assert.That(method, Is.Not.Null, "PHASE1 RED: missing public SupportInquiryController.ReconcileAsync().");
            Assert.That(method.ReturnType, Is.EqualTo(typeof(Task)));
            return method;
        }

        private static Task Reconcile(SupportInquiryController model)
        {
            var task = RequireReconcile().Invoke(model, null) as Task;
            Assert.That(task, Is.Not.Null);
            return task;
        }

        private static async Task<Fixture> Pending()
        {
            var fixture = new Fixture();
            await fixture.Model.SubmitAsync();
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Uncertain));
            return fixture;
        }

        private static void AssertFrozen(Fixture fixture, InquirySubmission original)
        {
            Assert.That(fixture.Model.Pending, Is.SameAs(original));
            Assert.That(fixture.Model.Receipt, Is.Null);
            Assert.That(fixture.Model.CanEdit, Is.False);
            Assert.That(fixture.Model.StartAnother(), Is.False);
            Assert.That(fixture.Model.Edit("ACCOUNT_LOGIN", "another synthetic inquiry"), Is.False);
        }

        private static void AssertLookup(RunApiHttpRequest request, InquirySubmission original)
        {
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/support/tickets/receipt"));
            Assert.That(string.IsNullOrEmpty(request.BodyJson), Is.True, "Lookup must have no body.");
            Assert.That(request.Headers.Count(h => h.Name == "Authorization"), Is.EqualTo(1));
            Assert.That(request.Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo(original.IdempotencyKey));
        }

        [Test]
        public async Task UnknownSendThenPublicLookupReceivesTicketWithoutAnotherPost()
        {
            var fixture = await Pending();
            var original = fixture.Model.Pending;
            fixture.Wire.Reply = RunApiHttpResponse.Success(200, Found);
            var changes = 0;
            fixture.Model.Changed += () => changes++;
            await Reconcile(fixture.Model);
            Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
            AssertLookup(fixture.Wire.Requests[1], original);
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Received));
            Assert.That(fixture.Model.Receipt.TicketId, Is.EqualTo("ticket_fixture"));
            Assert.That(fixture.Model.Receipt.ServerReceivedAt, Is.EqualTo(fixture.Now.AddMinutes(1)));
            Assert.That(changes, Is.GreaterThanOrEqualTo(1), "Panel must be notified of the result.");
            Assert.That(fixture.Model.CanSubmit, Is.False);
            await Reconcile(fixture.Model);
            Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
            Assert.That(fixture.Model.StartAnother(), Is.True);
        }

        [Test]
        public async Task NotFoundKeepsUnknownAndOriginalPostBodyAndKeyForRetry()
        {
            var fixture = await Pending();
            var original = fixture.Model.Pending;
            fixture.Wire.Reply = RunApiHttpResponse.Success(404, Missing);
            await Reconcile(fixture.Model);
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Uncertain));
            AssertLookup(fixture.Wire.Requests[1], original);
            AssertFrozen(fixture, original);
            fixture.Wire.Reply = RunApiHttpResponse.NetworkError("fixture response lost");
            await fixture.Model.SubmitAsync();
            Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(3));
            var retry = fixture.Wire.Requests[2];
            Assert.That(retry.Method, Is.EqualTo("POST"));
            Assert.That(retry.BodyJson == fixture.Wire.Requests[0].BodyJson, Is.True, "Retry must preserve the frozen body.");
            Assert.That(retry.Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo(original.IdempotencyKey));
            AssertFrozen(fixture, original);
        }

        [Test]
        public async Task EditingWithoutPendingCannotLookupOrCreateKey()
        {
            var fixture = new Fixture();
            await Reconcile(fixture.Model);
            Assert.That(fixture.Wire.Requests, Is.Empty);
            Assert.That(fixture.Model.Pending, Is.Null);
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Editing));
            Assert.That(fixture.Model.CanEdit, Is.True);
        }

        [Test]
        public async Task SubmitLogoutFromSendingCallbackPreventsPostDispatchAndClearsBusy()
        {
            var fixture = new Fixture();
            var loggedOut = false;
            fixture.Model.Changed += () =>
            {
                if (fixture.Model.State != InquiryState.Sending) return;
                loggedOut = true;
                fixture.Model.SetSession(null);
            };
            await fixture.Model.SubmitAsync();
            Assert.That(loggedOut, Is.True);
            Assert.That(fixture.Wire.Requests.Count, Is.Zero, "Logout during Changed must prevent POST dispatch.");
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Detached));
            Assert.That(fixture.Model.Receipt, Is.Null);
            Assert.That(fixture.Model.CanSubmit, Is.False);
            Assert.That(fixture.Model.CanReconcile, Is.False);
            // Reattaching exposes a stuck inFlight flag through the public submission guard.
            fixture.Model.SetSession(Session());
            Assert.That(fixture.Model.CanSubmit, Is.True, "Aborted dispatch must release the busy guard.");
            fixture.Model.SetSession(null);
        }

        [Test]
        public async Task ReconcileLogoutFromSendingCallbackPreventsGetDispatchAndClearsBusy()
        {
            var fixture = await Pending();
            var original = fixture.Model.Pending;
            var loggedOut = false;
            fixture.Model.Changed += () =>
            {
                if (fixture.Model.State != InquiryState.Sending) return;
                loggedOut = true;
                fixture.Model.SetSession(null);
            };
            await Reconcile(fixture.Model);
            Assert.That(loggedOut, Is.True);
            Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(1), "Logout during Changed must prevent GET dispatch.");
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Detached));
            AssertFrozen(fixture, original);
            Assert.That(fixture.Model.CanSubmit, Is.False);
            Assert.That(fixture.Model.CanReconcile, Is.False);
            fixture.Model.SetSession(Session());
            Assert.That(fixture.Model.CanSubmit, Is.True, "Aborted lookup must release the busy guard.");
            Assert.That(fixture.Model.CanReconcile, Is.True);
            fixture.Model.SetSession(null);
        }

        [TestCase("account_test")]
        [TestCase("other_account")]
        public async Task SessionChangeRejectsStaleLookupAndPreventsOverlappingRequests(string nextAccount)
        {
            var fixture = await Pending();
            var original = fixture.Model.Pending;
            fixture.Wire.Deferred = new TaskCompletionSource<RunApiHttpResponse>();
            var lookup = Reconcile(fixture.Model);
            try
            {
                Assert.That(fixture.Model.CanSubmit, Is.False);
                AssertFrozen(fixture, original);
                await Reconcile(fixture.Model);
                await fixture.Model.SubmitAsync();
                fixture.Model.SetSession(Session(nextAccount, "lst1.rotated"));
                await Reconcile(fixture.Model);
                await fixture.Model.SubmitAsync();
                Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
            }
            finally
            {
                fixture.Wire.Deferred.SetResult(RunApiHttpResponse.Success(200, Found));
                await lookup;
                fixture.Wire.Deferred = null;
            }
            Assert.That(fixture.Model.State, Is.EqualTo(nextAccount == "account_test" ? InquiryState.Uncertain : InquiryState.Detached));
            AssertFrozen(fixture, original);
            if (nextAccount != "account_test")
            {
                await Reconcile(fixture.Model);
                Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
                fixture.Model.SetSession(Session(token: "lst1.rotated"));
            }
            fixture.Wire.Reply = RunApiHttpResponse.Success(200, Found);
            await Reconcile(fixture.Model);
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Received));
            AssertLookup(fixture.Wire.Requests.Last(), original);
            Assert.That(fixture.Wire.Requests.Last().Headers.Single(h => h.Name == "Authorization").Value == "Bearer lst1.rotated", Is.True);
        }

        [Test]
        public async Task SameAccountRefreshAndLookupRespectExistingCooldown()
        {
            var fixture = new Fixture();
            fixture.Wire.Reply = RunApiHttpResponse.Success(429, Limited);
            await fixture.Model.SubmitAsync();
            var original = fixture.Model.Pending;
            fixture.Model.SetSession(Session(token: "lst1.rotated"));
            await Reconcile(fixture.Model);
            await fixture.Model.SubmitAsync();
            Assert.That(fixture.Model.RetryWaitSeconds, Is.EqualTo(60));
            Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(1));
            AssertFrozen(fixture, original);
            fixture.Now = fixture.Now.AddSeconds(60);
            fixture.Wire.Reply = RunApiHttpResponse.Success(404, Missing);
            await Reconcile(fixture.Model);
            Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
            AssertLookup(fixture.Wire.Requests[1], original);
            AssertFrozen(fixture, original);
        }

        [TestCase(200)]
        [TestCase(404)]
        public async Task PanelHasExplicitLookupButtonThatUsesControllerAndNeverShowsPreview(int status)
        {
            var fixture = await Pending();
            var original = fixture.Model.Pending;
            var root = new GameObject("ReceiptPanelFixture", typeof(RectTransform));
            try
            {
                var panel = root.AddComponent<SupportInquiryPanel>();
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
                Assert.That(font, Is.Not.Null);
                panel.Initialize(fixture.Model, font, SupportInquiryHttpClient.CategoryLabel);
                var button = root.GetComponentsInChildren<Button>(true).SingleOrDefault(b => b.name == "Reconcile");
                Assert.That(button, Is.Not.Null, "PHASE1 RED: panel needs a dedicated Reconcile receipt lookup button.");
                Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.True);
                Assert.That(button.GetComponentInChildren<TMP_Text>().text, Is.Not.Empty);
                fixture.Wire.Reply = RunApiHttpResponse.Success(status, status == 200 ? Found : Missing);
                // The fake wire completes synchronously, including the panel's async click handler.
                button.onClick.Invoke();
                Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
                AssertLookup(fixture.Wire.Requests[1], original);
                Assert.That(fixture.Model.State, Is.EqualTo(status == 200 ? InquiryState.Received : InquiryState.Uncertain));
                Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.Contains(Preview)), Is.False);
                if (status == 404) AssertFrozen(fixture, original);
                else Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.Contains("ticket_fixture")), Is.True);
                fixture.Model.SetSession(Session("other_account"));
                Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
