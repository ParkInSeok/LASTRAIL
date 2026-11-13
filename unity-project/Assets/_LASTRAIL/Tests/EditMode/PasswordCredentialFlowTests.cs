using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class PasswordCredentialFlowTests
    {
        private const string Email = "synthetic@example.invalid";
        private const string Challenge = "email_challenge_fixture";
        private static readonly DateTimeOffset Origin = DateTimeOffset.Parse("2026-09-06T00:00:00Z");
        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest, Task<RunApiHttpResponse>> Override;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            { Requests.Add(request); return Override?.Invoke(request) ?? Task.FromResult(Reply(request)); }
        }
        private sealed class Fixture
        {
            public readonly Wire Wire = new Wire();
            public GameApiAuthSessionSnapshot Session;
            public long Generation;
            public DateTimeOffset Now = Origin;
            public int Commits;
            public bool AllowCommit = true;
            public readonly object Flow;
            public Fixture(string mode, bool withGuest = false)
            {
                Session = withGuest ? SupportInquiryTests.Session("account_test") : null;
                var type = typeof(PlayerAuthOwner).Assembly.GetType("LastRail.Auth.PasswordCredentialFlow");
                Assert.That(type, Is.Not.Null, "Account-fenced email UI flow is missing");
                Flow = Activator.CreateInstance(type, new PasswordEmailClient(Wire), mode,
                    (Func<GameApiAuthSessionSnapshot>)(() => Session), (Func<long>)(() => Generation),
                    (Func<GameApiAuthSessionSnapshot, Task<bool>>)(candidate =>
                    {
                        Commits++; if (!AllowCommit) return Task.FromResult(false);
                        if (Session?.AccountId != candidate.AccountId) Generation++;
                        Session = candidate; return Task.FromResult(true);
                    }), (Func<DateTimeOffset>)(() => Now));
            }
            public Task Call(string name, params object[] args) => (Task)Flow.GetType().GetMethod(name).Invoke(Flow, args);
            public object Get(string name) => Flow.GetType().GetProperty(name).GetValue(Flow);
            public string Stage => Get("Stage").ToString();
            public void Cancel() => Flow.GetType().GetMethod("Cancel").Invoke(Flow, null);
            public Task Start() => Call("StartAsync", "fixture_id", Email);
            public Task Confirm() => Call("ConfirmAsync", "00123456");
            public Task Complete(string code = null) => Call("CompleteAsync", "fixture_password", code);
        }
        private static string Bundle(string field, string status, string account = "account_test") =>
            "{\"accepted\":true,\"" + field + "\":\"" + status + "\",\"tokenType\":\"Bearer\",\"account\":{\"id\":\"" + account + "\",\"status\":\"ACTIVE\"},\"accessToken\":\"lst1.fixture\",\"refreshToken\":\"fixture_refresh\",\"accessTokenExpiresAt\":\"2030-01-01T00:00:00.000Z\",\"refreshTokenExpiresAt\":\"2030-02-01T00:00:00.000Z\"}";
        private static RunApiHttpResponse Reply(RunApiHttpRequest request)
        {
            switch (request.EndpointPath)
            {
                case "/v1/auth/password/recovery-email/verify/start": return Verification("PENDING", 60);
                case "/v1/auth/password/recovery-email/verify/confirm": return Verification("VERIFIED", null);
                case "/v1/auth/password/register": return RunApiHttpResponse.Success(200, Bundle("registerStatus", "REGISTERED"));
                case "/v1/auth/password/link": return RunApiHttpResponse.Success(200, Bundle("linkStatus", "LINKED"));
                case "/v1/auth/password/login": return RunApiHttpResponse.Success(200, Bundle("signInStatus", "SIGNED_IN"));
                case "/v1/auth/password/reset/start": return RunApiHttpResponse.Success(200, "{\"accepted\":true,\"resetStatus\":\"EMAIL_SENT_IF_AVAILABLE\"}");
                case "/v1/auth/password/reset/confirm": return RunApiHttpResponse.Success(200, "{\"accepted\":true,\"resetStatus\":\"RESET_CONFIRMED\"}");
                default: throw new Exception("Unexpected fixture route " + request.EndpointPath);
            }
        }
        private static RunApiHttpResponse Verification(string status, int? delay) => RunApiHttpResponse.Success(200,
            new JObject { ["accepted"] = true, ["verificationStatus"] = status, ["challengeId"] = Challenge,
                ["expiresAt"] = "2026-09-06T00:15:00.000Z", ["retryAfterSeconds"] = delay }.ToString());

        [Test]
        public async Task CancellingPendingLinkNeverReplacesOrSignsOutGuest()
        {
            var fixture = new Fixture("Link", true); var guest = fixture.Session;
            await fixture.Start(); fixture.Cancel();
            Assert.That(fixture.Session, Is.SameAs(guest)); Assert.That(fixture.Commits, Is.Zero);
            Assert.That(fixture.Stage, Is.EqualTo("Cancelled")); Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(1));
        }
        [Test]
        public async Task ResendCountdownUsesServerDelayAndFrozenInputs()
        {
            var fixture = new Fixture("Register"); fixture.Wire.Override = _ => Task.FromResult(Verification("PENDING", 120));
            await fixture.Start(); Assert.That(fixture.Get("RetryWaitSeconds"), Is.EqualTo(120));
            await fixture.Start(); Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(1));
            fixture.Now = fixture.Now.AddSeconds(119); Assert.That(fixture.Get("RetryWaitSeconds"), Is.EqualTo(1));
            fixture.Now = fixture.Now.AddSeconds(1);
            await fixture.Call("StartAsync", "different_id", "other@example.invalid"); Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(1));
            await fixture.Start(); Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
        }
        [Test]
        public async Task RegistrationCommitsOnlyAfterMatchingVerifiedCredentialResponse()
        {
            var fixture = new Fixture("Register"); await fixture.Start(); await fixture.Confirm();
            Assert.That(fixture.Commits, Is.Zero); Assert.That(fixture.Stage, Is.EqualTo("Verified"));
            await fixture.Complete(); Assert.That(fixture.Stage, Is.EqualTo("Completed")); Assert.That(fixture.Commits, Is.EqualTo(1));
            Assert.That(fixture.Session.AccountId, Is.EqualTo("account_test")); Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(3));
        }
        [Test]
        public async Task ResetCompletesWithoutVerificationEndpointOrSessionAdoption()
        {
            var fixture = new Fixture("Reset", true); var guest = fixture.Session;
            await fixture.Start(); await fixture.Complete("00123456");
            Assert.That(fixture.Stage, Is.EqualTo("Completed")); Assert.That(fixture.Commits, Is.Zero); Assert.That(fixture.Session, Is.SameAs(guest));
            Assert.That(fixture.Wire.Requests.Select(r => r.EndpointPath), Is.EqualTo(new[] { "/v1/auth/password/reset/start", "/v1/auth/password/reset/confirm" }));
        }
        [Test]
        public async Task LostRegistrationResponseUsesLoginProofNotAnotherRegistration()
        {
            var fixture = new Fixture("Register"); await fixture.Start(); await fixture.Confirm();
            fixture.Wire.Override = request => Task.FromResult(request.EndpointPath.EndsWith("/register") ? RunApiHttpResponse.NetworkError("synthetic timeout") : Reply(request));
            await fixture.Complete(); Assert.That(fixture.Stage, Is.EqualTo("Uncertain")); Assert.That(fixture.Commits, Is.Zero);
            await fixture.Complete(); Assert.That(fixture.Wire.Requests.Count(r => r.EndpointPath.EndsWith("/register")), Is.EqualTo(1));
            await fixture.Call("ReconcileAsync", "fixture_password");
            Assert.That(fixture.Stage, Is.EqualTo("Completed")); Assert.That(fixture.Wire.Requests.Count(r => r.EndpointPath.EndsWith("/register")), Is.EqualTo(1));
        }
        [Test]
        public async Task LinkLoginProofForAnotherAccountCannotReplaceGuest()
        {
            var fixture = new Fixture("Link", true); var guest = fixture.Session;
            await fixture.Start(); await fixture.Confirm();
            fixture.Wire.Override = request => Task.FromResult(request.EndpointPath.EndsWith("/link") ? RunApiHttpResponse.NetworkError("synthetic timeout")
                : RunApiHttpResponse.Success(200, Bundle("signInStatus", "SIGNED_IN", "account_other")));
            await fixture.Complete(); await fixture.Call("ReconcileAsync", "fixture_password");
            Assert.That(fixture.Session, Is.SameAs(guest)); Assert.That(fixture.Commits, Is.Zero); Assert.That(fixture.Stage, Is.EqualTo("Uncertain"));
        }
        [Test]
        public async Task LateStartAfterCancelCannotPublishChallenge()
        {
            var fixture = new Fixture("Register"); var pending = new TaskCompletionSource<RunApiHttpResponse>(); fixture.Wire.Override = _ => pending.Task;
            var request = fixture.Start(); fixture.Cancel(); pending.SetResult(Verification("PENDING", 60)); await request;
            Assert.That(fixture.Stage, Is.EqualTo("Cancelled")); Assert.That(fixture.Get("ChallengeId"), Is.Null); Assert.That(fixture.Get("Busy"), Is.False);
        }
        [Test]
        public async Task LateConfirmationAfterAccountChangeCannotVerify()
        {
            var fixture = new Fixture("Link", true); await fixture.Start();
            var pending = new TaskCompletionSource<RunApiHttpResponse>(); fixture.Wire.Override = _ => pending.Task;
            var request = fixture.Confirm(); fixture.Generation++; fixture.Session = SupportInquiryTests.Session("account_other");
            pending.SetResult(Verification("VERIFIED", null)); await request;
            Assert.That(fixture.Stage, Is.EqualTo("Cancelled")); Assert.That(fixture.Commits, Is.Zero);
        }
        [Test]
        public async Task DoubleSubmitAndCancellationNeverCreateSecondCredentialRequest()
        {
            var fixture = new Fixture("Register"); await fixture.Start(); await fixture.Confirm();
            var pending = new TaskCompletionSource<RunApiHttpResponse>(); fixture.Wire.Override = _ => pending.Task;
            var request = fixture.Complete(); await fixture.Complete(); fixture.Cancel();
            pending.SetResult(RunApiHttpResponse.Success(200, Bundle("registerStatus", "REGISTERED"))); await request;
            Assert.That(fixture.Wire.Requests.Count(r => r.EndpointPath.EndsWith("/register")), Is.EqualTo(1)); Assert.That(fixture.Commits, Is.Zero);
        }
        [Test]
        public async Task SynchronousCancellationBeforeDispatchSendsNothing()
        {
            var fixture = new Fixture("Register");
            fixture.Flow.GetType().GetEvent("Changed").AddEventHandler(fixture.Flow, (Action)(() => { if (fixture.Stage == "Sending") fixture.Cancel(); }));
            await fixture.Start(); Assert.That(fixture.Wire.Requests, Is.Empty); Assert.That(fixture.Get("Busy"), Is.False);
        }
        [Test]
        public async Task ExpiredProofCannotCreateCredential()
        {
            var fixture = new Fixture("Register"); await fixture.Start(); await fixture.Confirm(); fixture.Now = fixture.Now.AddMinutes(16);
            await fixture.Complete(); Assert.That(fixture.Commits, Is.Zero); Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
            Assert.That(fixture.Get("CanStart"), Is.True);
        }
        [Test]
        public async Task FailedSecureAdoptionKeepsGuestAndRequiresReconciliation()
        {
            var fixture = new Fixture("Link", true) { AllowCommit = false }; var guest = fixture.Session;
            await fixture.Start(); await fixture.Confirm(); await fixture.Complete();
            Assert.That(fixture.Session, Is.SameAs(guest)); Assert.That(fixture.Stage, Is.EqualTo("Uncertain"));
        }
    }
}
