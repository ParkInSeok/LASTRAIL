using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class PasswordEmailHttpTests
    {
        private const string Challenge = "email_challenge_fixture";
        private const string Pending = "{\"accepted\":true,\"verificationStatus\":\"PENDING\",\"challengeId\":\"email_challenge_fixture\",\"expiresAt\":\"2030-01-01T00:00:00.000Z\",\"retryAfterSeconds\":60,\"errorCode\":null}";
        private const string Verified = "{\"accepted\":true,\"verificationStatus\":\"VERIFIED\",\"challengeId\":\"email_challenge_fixture\",\"expiresAt\":\"2030-01-01T00:00:00.000Z\",\"retryAfterSeconds\":null,\"errorCode\":null}";
        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public RunApiHttpResponse Reply = RunApiHttpResponse.Success(200, Pending);
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request) { Requests.Add(request); return Task.FromResult(Reply); }
        }
        private static async Task<object> Send(Wire wire, string method, params object[] args)
        {
            var type = typeof(PlayerAuthOwner).Assembly.GetType("LastRail.Auth.PasswordEmailClient");
            Assert.That(type, Is.Not.Null, "Email credential HTTP client is missing");
            var member = type.GetMethod(method); Assert.That(member, Is.Not.Null, method);
            var client = Activator.CreateInstance(type, wire);
            var task = (Task)member.Invoke(client, args); await task;
            return task.GetType().GetProperty("Result").GetValue(task);
        }
        private static object Get(object value, string property)
        { var member = value.GetType().GetProperty(property); Assert.That(member, Is.Not.Null, property); return member.GetValue(value); }
        private static string Bundle(string status, string account = "account_test") =>
            "{\"accepted\":true,\"" + status + "\":\"" + (status == "registerStatus" ? "REGISTERED" : "LINKED") + "\",\"tokenType\":\"Bearer\",\"account\":{\"id\":\"" + account + "\",\"status\":\"ACTIVE\"},\"accessToken\":\"lst1.fixture\",\"refreshToken\":\"fixture_refresh\",\"accessTokenExpiresAt\":\"2030-01-01T00:00:00.000Z\",\"refreshTokenExpiresAt\":\"2030-02-01T00:00:00.000Z\"}";

        [Test]
        public async Task RegisterVerificationUsesOnlyPurposeAndEmailWithoutBearer()
        {
            var wire = new Wire(); var result = await Send(wire, "StartVerificationAsync", "PASSWORD_REGISTER", "synthetic@example.invalid", null);
            var request = wire.Requests.Single(); var body = JObject.Parse(request.BodyJson);
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/auth/password/recovery-email/verify/start"));
            Assert.That(body.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "purpose", "recoveryEmail" }));
            Assert.That((string)body["purpose"], Is.EqualTo("PASSWORD_REGISTER")); Assert.That(request.Headers.Any(h => h.Name == "Authorization"), Is.False);
            Assert.That(Get(result, "Accepted"), Is.True); Assert.That(Get(result, "ChallengeId"), Is.EqualTo(Challenge));
            Assert.That(Get(result, "RetryAfterSeconds"), Is.EqualTo(60)); Assert.That(Get(result, "ExpiresAt"), Is.Not.Null);
        }
        [Test]
        public async Task LinkVerificationUsesCurrentBearerWithoutAccountShortcut()
        {
            var wire = new Wire(); await Send(wire, "StartVerificationAsync", "PASSWORD_LINK", "synthetic@example.invalid", SupportInquiryTests.Session("account_test"));
            Assert.That(wire.Requests.Single().Headers.Count(h => h.Name == "Authorization"), Is.EqualTo(1));
            Assert.That(JObject.Parse(wire.Requests.Single().BodyJson)["accountId"], Is.Null);
        }
        [Test]
        public async Task ConfirmationPreservesLeadingZeroCodeAndRequiresMatchingChallenge()
        {
            var wire = new Wire { Reply = RunApiHttpResponse.Success(200, Verified) };
            var result = await Send(wire, "ConfirmVerificationAsync", Challenge, "00123456", null);
            var body = JObject.Parse(wire.Requests.Single().BodyJson);
            Assert.That(body.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "challengeId", "verificationCode" }));
            Assert.That(body["verificationCode"].Type, Is.EqualTo(JTokenType.String)); Assert.That((string)body["verificationCode"], Is.EqualTo("00123456"));
            Assert.That(Get(result, "Accepted"), Is.True);
            wire.Reply = RunApiHttpResponse.Success(200, Verified.Replace(Challenge, "email_challenge_other"));
            Assert.That(Get(await Send(wire, "ConfirmVerificationAsync", Challenge, "00123456", null), "Accepted"), Is.False);
        }
        [TestCase("1234567")]
        [TestCase("123456789")]
        [TestCase("abcdefgh")]
        public async Task InvalidCodeNeverDispatches(string code)
        {
            var wire = new Wire(); var result = await Send(wire, "ConfirmVerificationAsync", Challenge, code, null);
            Assert.That(Get(result, "Accepted"), Is.False); Assert.That(wire.Requests, Is.Empty);
        }
        [Test]
        public async Task RegisterUsesExactFourFieldsAndReturnsValidatedTokenBundle()
        {
            var wire = new Wire { Reply = RunApiHttpResponse.Success(200, Bundle("registerStatus")) };
            var result = await Send(wire, "RegisterAsync", "fixture_id", "fixture_password", "synthetic@example.invalid", Challenge);
            Assert.That(JObject.Parse(wire.Requests.Single().BodyJson).Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "loginId", "password", "recoveryEmail", "verificationChallengeId" }));
            Assert.That(Get(result, "Accepted"), Is.True); Assert.That(((GameApiAuthSessionSnapshot)Get(result, "Session")).AccountId, Is.EqualTo("account_test"));
        }
        [Test]
        public async Task LinkCannotAdoptAnotherAccountOrDispatchWithoutOwner()
        {
            var wire = new Wire { Reply = RunApiHttpResponse.Success(200, Bundle("linkStatus", "account_other")) };
            var result = await Send(wire, "LinkAsync", "fixture_id", "fixture_password", "synthetic@example.invalid", Challenge, SupportInquiryTests.Session("account_test"));
            Assert.That(Get(result, "Accepted"), Is.False); Assert.That(Get(result, "Session"), Is.Null);
            wire.Requests.Clear(); result = await Send(wire, "LinkAsync", "fixture_id", "fixture_password", "synthetic@example.invalid", Challenge, null);
            Assert.That(Get(result, "Accepted"), Is.False); Assert.That(wire.Requests, Is.Empty);
        }
        [Test]
        public async Task GenericResetStartDoesNotInventChallengeOrAuthenticate()
        {
            var wire = new Wire { Reply = RunApiHttpResponse.Success(200, "{\"accepted\":true,\"resetStatus\":\"EMAIL_SENT_IF_AVAILABLE\",\"errorCode\":null}") };
            var result = await Send(wire, "StartResetAsync", "fixture_id", "synthetic@example.invalid");
            Assert.That(Get(result, "Accepted"), Is.True); Assert.That(Get(result, "Status"), Is.EqualTo("EMAIL_SENT_IF_AVAILABLE"));
            Assert.That(Get(result, "ChallengeId"), Is.Null); Assert.That(Get(result, "Session"), Is.Null);
        }
        [Test]
        public async Task ResetConfirmationUsesFourFieldsAndNeverReturnsSession()
        {
            var wire = new Wire { Reply = RunApiHttpResponse.Success(200, "{\"accepted\":true,\"resetStatus\":\"RESET_CONFIRMED\",\"errorCode\":null}") };
            var result = await Send(wire, "ConfirmResetAsync", "fixture_id", "synthetic@example.invalid", "00123456", "fixture_new_password");
            var body = JObject.Parse(wire.Requests.Single().BodyJson);
            Assert.That(body.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "loginId", "recoveryEmail", "verificationCode", "newPassword" }));
            Assert.That((string)body["verificationCode"], Is.EqualTo("00123456")); Assert.That(Get(result, "Accepted"), Is.True); Assert.That(Get(result, "Session"), Is.Null);
        }
        [Test]
        public async Task ServerRetryDelayIsPreservedWithoutFakePendingStatus()
        {
            var wire = new Wire { Reply = RunApiHttpResponse.Success(429, "{\"accepted\":false,\"verificationStatus\":\"REJECTED\",\"retryAfterSeconds\":120,\"errorCode\":\"recovery_email_resend_limited\"}") };
            var result = await Send(wire, "StartVerificationAsync", "PASSWORD_REGISTER", "synthetic@example.invalid", null);
            Assert.That(Get(result, "Accepted"), Is.False); Assert.That(Get(result, "RetryAfterSeconds"), Is.EqualTo(120));
            Assert.That(Get(result, "ErrorCode"), Is.EqualTo("recovery_email_resend_limited"));
        }
        [TestCase("{}")]
        [TestCase("{\"accepted\":true}")]
        public async Task MalformedSuccessCannotConfirmCredential(string body)
        {
            var wire = new Wire { Reply = RunApiHttpResponse.Success(200, body) };
            var result = await Send(wire, "RegisterAsync", "fixture_id", "fixture_password", "synthetic@example.invalid", Challenge);
            Assert.That(Get(result, "Accepted"), Is.False); Assert.That(Get(result, "Uncertain"), Is.True);
        }
        [Test]
        public async Task LostResponseStaysUncertainWithoutAutomaticRetry()
        {
            var wire = new Wire { Reply = RunApiHttpResponse.NetworkError("synthetic timeout") };
            var result = await Send(wire, "RegisterAsync", "fixture_id", "fixture_password", "synthetic@example.invalid", Challenge);
            Assert.That(Get(result, "Accepted"), Is.False); Assert.That(Get(result, "Uncertain"), Is.True); Assert.That(wire.Requests.Count, Is.EqualTo(1));
        }
    }
}
