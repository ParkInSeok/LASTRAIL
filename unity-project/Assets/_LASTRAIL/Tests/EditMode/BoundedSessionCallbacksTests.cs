using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Resources;
using LastRail.Run;
using LastRail.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class BoundedSessionCallbacksTests
    {
        const string Source = "https://api.lastrail.app/v1/client-config", Root = "https://example.test/";
        static readonly DateTimeOffset Origin = DateTimeOffset.Parse("2026-09-06T00:00:00Z");
        static TimeSpan? Delay(object owner)
        {
            var property = owner.GetType().GetProperty("NextPresentationChangeDelay");
            Assert.That(property, Is.Not.Null, "Views need the owner's actual clock boundary, not a frame poll.");
            return (TimeSpan?)property.GetValue(owner);
        }
        sealed class Wire : IRunApiHttpTransport
        {
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request) => Task.FromResult(RunApiHttpResponse.Success(200,
                "{\"accepted\":true,\"verificationStatus\":\"PENDING\",\"challengeId\":\"email_challenge_fixture\",\"expiresAt\":\"2026-09-06T00:00:05.000Z\",\"retryAfterSeconds\":2}"));
        }
        sealed class InquiryClient : ISupportInquiryClient
        {
            public Task<InquiryReceipt> SubmitAsync(InquirySubmission submission, GameApiAuthSessionSnapshot session) =>
                Task.FromResult(new InquiryReceipt(InquiryDelivery.Rejected, retryAfterSeconds: 2));
        }
        [Test]
        public async Task CredentialScheduleStopsWhenIdleAndUsesRetryBoundaryThenProofDeadline()
        {
            var now = Origin;
            var flow = new PasswordCredentialFlow(new PasswordEmailClient(new Wire()), "Register", () => null, () => 1,
                _ => Task.FromResult(true), () => now);
            Assert.That(Delay(flow), Is.Null);
            await flow.StartAsync("fixture_id", "synthetic@example.invalid");
            now = Origin.AddMilliseconds(250);
            Assert.That(Delay(flow)?.TotalSeconds, Is.EqualTo(.75).Within(.0001));
            now = Origin.AddSeconds(2);
            Assert.That(Delay(flow)?.TotalSeconds, Is.EqualTo(3));
            now = Origin.AddSeconds(5);
            Assert.That(flow.ProofExpired, Is.True); Assert.That(Delay(flow), Is.Null);
            flow.Cancel(); Assert.That(Delay(flow), Is.Null);
        }
        [Test]
        public async Task InquiryScheduleStopsAtRetryDeadlineWithoutChangingDraft()
        {
            var now = Origin;
            var flow = new SupportInquiryController(SupportInquiryTests.Session(), new InquiryClient(), new[] { "fixture" }, 100,
                () => new AppBuildSnapshot("1.2.3", 123), () => now);
            Assert.That(Delay(flow), Is.Null);
            flow.Edit("fixture", "original content"); await flow.SubmitAsync();
            now = Origin.AddMilliseconds(250);
            Assert.That(Delay(flow)?.TotalSeconds, Is.EqualTo(.75).Within(.0001));
            now = Origin.AddSeconds(2);
            Assert.That(Delay(flow), Is.Null); Assert.That(flow.CanSubmit, Is.True);
            Assert.That(flow.Content, Is.EqualTo("original content"));
        }
        [UnityTest]
        public IEnumerator PanelHideCancelsDeadlineAndShowReusesDraftWithoutIdleScheduling()
        {
            // Support is a runtime-only MonoBehaviour; EditMode does not deliver its disable callback.
            yield return new EnterPlayMode();
            var credentials = CheckPanelLifecycle(false);
            while (!credentials.IsCompleted) yield return null;
            credentials.GetAwaiter().GetResult();
            var support = CheckPanelLifecycle(true);
            while (!support.IsCompleted) yield return null;
            support.GetAwaiter().GetResult();
            yield return new ExitPlayMode();
        }
        private async Task CheckPanelLifecycle(bool support)
        {
            var now = Origin;
            var root = new GameObject("BoundedPanelFixture", typeof(RectTransform));
            try
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
                MonoBehaviour panel;
                Action hide, show;
                TMP_InputField draft;
                if (support)
                {
                    var flow = new SupportInquiryController(SupportInquiryTests.Session(), new InquiryClient(), new[] { "fixture" }, 100,
                        () => new AppBuildSnapshot("1.2.3", 123), () => now);
                    flow.Edit("fixture", "original content"); await flow.SubmitAsync();
                    now = Origin.AddMilliseconds(1990);
                    var view = root.AddComponent<SupportInquiryPanel>(); view.Initialize(flow, font, x => x);
                    panel = view; hide = view.Hide; show = view.Show;
                    draft = root.GetComponentsInChildren<TMP_InputField>(true).Single(x => x.name == "Content");
                    draft.text = "draft survives hide";
                }
                else
                {
                    var flow = new PasswordCredentialFlow(new PasswordEmailClient(new Wire()), "Register", () => null, () => 1,
                        _ => Task.FromResult(true), () => now);
                    await flow.StartAsync("fixture_id", "synthetic@example.invalid"); now = Origin.AddMilliseconds(1990);
                    var view = root.AddComponent<PasswordCredentialPanel>(); view.Initialize(flow, font);
                    panel = view; hide = view.Hide; show = view.Show;
                    draft = root.GetComponentsInChildren<TMP_InputField>(true).Single(x => x.name == "LoginId");
                    draft.SetTextWithoutNotify("draft survives hide");
                }
                var field = panel.GetType().GetField("presentationDelay", BindingFlags.Instance | BindingFlags.NonPublic);
                var scheduled = (CancellationTokenSource)field.GetValue(panel);
                Assert.That(scheduled, Is.Not.Null);
                var token = scheduled.Token;
                var status = root.GetComponentsInChildren<TMP_Text>(true).Single(x => x.name == "Status");
                var hiddenStatus = status.text;
                hide(); Assert.That(token.IsCancellationRequested, Is.True);
                now = Origin.AddMilliseconds(1995); await Task.Delay(100);
                Assert.That(status.text, Is.EqualTo(hiddenStatus), "A cancelled hidden-view callback must not render.");
                show();
                Assert.That(field.GetValue(panel), Is.Not.Null, "Reopening a pending deadline must re-arm it.");
                Assert.That(field.GetValue(panel), Is.Not.SameAs(scheduled));
                now = Origin.AddSeconds(6); await Task.Delay(100);
                Assert.That(status.text, Is.Not.EqualTo(hiddenStatus), "The visible deadline must update without another UI event.");
                Assert.That(draft.text, Is.EqualTo("draft survives hide"));
                Assert.That(field.GetValue(panel), Is.Null, "Past deadlines must not leave an idle timer running.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public async Task ReplacingAuthorizationCancelsOldDeadlineAndDisposeSuppressesExpiryNotification()
        {
            var now = DateTime.UtcNow;
            GameApiAuthSessionSnapshot Owner(int milliseconds) => new GameApiAuthSessionSnapshot("fixture", "ACTIVE",
                new GameApiAccessToken("lst1.fixture"), now.AddMilliseconds(milliseconds), new GameApiRefreshToken("fixture"), now.AddHours(1));
            var owner = Owner(40); var expirations = 0;
            using var client = new SessionContentAuthorization(Source, "2", Root, () => owner, () => 1,
                (u,t,c,k) => Task.FromResult(RunApiHttpResponse.Success(200,
                    File.ReadAllText("QA/Unity1Artifacts/Reports/Server2-AdmissionSamples-20260914/authorized.json"))), () => now);
            client.SessionExpired += () => expirations++;
            await client.FetchAsync(CancellationToken.None);
            owner = Owner(200); await client.FetchAsync(CancellationToken.None);
            now = now.AddMilliseconds(100); await Task.Delay(80);
            Assert.That(client.HasAuthorization, Is.True, "The previous owner's deadline must not revoke the replacement.");
            client.Dispose(); now = now.AddSeconds(1); await Task.Delay(250);
            Assert.That(expirations, Is.Zero, "Disposed authorization must not notify its former consumer.");
        }
        [Test]
        public async Task TokenDeadlineCancelsPendingRequestWithoutOwnerNotificationOrReply()
        {
            var now = DateTime.UtcNow;
            var owner = new GameApiAuthSessionSnapshot("fixture", "ACTIVE", new GameApiAccessToken("lst1.fixture"), now.AddMilliseconds(40),
                new GameApiRefreshToken("fixture"), now.AddHours(1));
            var reply = new TaskCompletionSource<RunApiHttpResponse>();
            CancellationToken requestToken = default;
            using var client = new SessionContentAuthorization(Source, "2", Root, () => owner, () => 1,
                (u,t,c,k) => { requestToken = k; return reply.Task; }, () => now);
            var pending = client.FetchAsync(CancellationToken.None);
            now = now.AddSeconds(1);
            await Task.Delay(150);
            var cancelledAtDeadline = requestToken.IsCancellationRequested;
            reply.SetResult(RunApiHttpResponse.Success(500, "{}"));
            try { await pending; } catch (InvalidOperationException) { } catch (OperationCanceledException) { }
            Assert.That(cancelledAtDeadline, Is.True, "Expiry must cancel in-flight content even without a model event or response.");
        }
        [Test]
        public async Task TokenDeadlineRevokesApprovedContentWithoutAnotherRequest()
        {
            var now = DateTime.UtcNow;
            var owner = new GameApiAuthSessionSnapshot("fixture", "ACTIVE", new GameApiAccessToken("lst1.fixture"), now.AddMilliseconds(40),
                new GameApiRefreshToken("fixture"), now.AddHours(1));
            using var client = new SessionContentAuthorization(Source, "2", Root, () => owner, () => 1,
                (u,t,c,k) => Task.FromResult(RunApiHttpResponse.Success(200,
                    File.ReadAllText("QA/Unity1Artifacts/Reports/Server2-AdmissionSamples-20260914/authorized.json"))), () => now);
            await client.FetchAsync(CancellationToken.None);
            Assert.That(client.HasAuthorization, Is.True);
            now = now.AddSeconds(1); await Task.Delay(150);
            Assert.That(client.HasAuthorization, Is.False, "The approved gate must be revoked at the token deadline.");
        }
    }
}
