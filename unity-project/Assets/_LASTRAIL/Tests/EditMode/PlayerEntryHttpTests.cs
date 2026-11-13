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
    public sealed class PlayerEntryHttpTests
    {
        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public RunApiHttpResponse Response;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            { Requests.Add(request); return Task.FromResult(Response); }
        }
        private const string Bundle = "{\"tokenType\":\"Bearer\",\"account\":{\"id\":\"account_test\",\"status\":\"ACTIVE\"},\"accessToken\":\"lst1.fixture_access\",\"refreshToken\":\"fixture_refresh\",\"accessTokenExpiresAt\":\"2030-01-01T00:00:00.000Z\",\"refreshTokenExpiresAt\":\"2030-02-01T00:00:00.000Z\",\"signInStatus\":\"SIGNED_IN\"}";
        private static object Client(Wire wire)
        {
            var type = typeof(GameApiJson).Assembly.GetType("LastRail.Auth.GameApiPlayerEntryClient");
            Assert.That(type, Is.Not.Null, "Player entry HTTP client is missing");
            return Activator.CreateInstance(type, wire);
        }
        private static async Task<object> Call(object client, string method, params object[] args)
        {
            var task = (Task)client.GetType().GetMethod(method).Invoke(client, args); await task;
            return task.GetType().GetProperty("Result").GetValue(task);
        }
        [Test]
        public async Task PasswordUsesConfirmedBodyWithoutBearerOrAccountShortcut()
        {
            var wire = new Wire { Response = RunApiHttpResponse.Success(200, Bundle) };
            var session = (GameApiAuthSessionSnapshot)await Call(Client(wire), "PasswordAsync", "test_id", "fixture_password");
            Assert.That(session, Is.Not.Null);
            Assert.That(session.AccountId, Is.EqualTo("account_test"));
            var request = wire.Requests.Single(); Assert.That(request.EndpointPath, Is.EqualTo("/v1/auth/password/login"));
            var body = JObject.Parse(request.BodyJson);
            Assert.That(body.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "loginId", "password" }));
            Assert.That(request.Headers.Any(h => h.Name == "Authorization"), Is.False);
        }
        [TestCase(503, "{}")]
        [TestCase(200, "{}")]
        [TestCase(401, "{\"errorCode\":\"password_login_invalid\"}")]
        public async Task AuthFailuresNeverInventSessionOrRetry(int status, string json)
        {
            var wire = new Wire { Response = RunApiHttpResponse.Success(status, json) };
            Assert.That(await Call(Client(wire), "PasswordAsync", "test_id", "fixture_password"), Is.Null);
            Assert.That(wire.Requests.Count, Is.EqualTo(1));
        }
        [Test]
        public async Task GuestReplayCarriesDurableKeyAndPreservesExpiryCode()
        {
            var wire = new Wire { Response = RunApiHttpResponse.Success(401, "{\"errorCode\":\"auth_replay_expired\"}") };
            var client = Client(wire);
            Assert.That(client.GetType().GetMethod("GuestReplayAsync"), Is.Not.Null);
            Assert.That(await Call(client, "GuestReplayAsync", "fixture_key_0123456789"), Is.Null);
            Assert.That(wire.Requests.Single().Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo("fixture_key_0123456789"));
            Assert.That(client.GetType().GetProperty("ReplayFailureCode").GetValue(client), Is.EqualTo("auth_replay_expired"));
        }
        [Test]
        public async Task RefreshReplayPreservesBodyAndTerminalRecoveryCode()
        {
            var create = typeof(GameApiRefreshAuthRequest).GetMethod("CreateReplay");
            Assert.That(create, Is.Not.Null);
            var token = new GameApiRefreshToken("fixture_refresh");
            var request = (GameApiRefreshAuthRequest)create.Invoke(null, new object[] { token, "fixture_key_0123456789" });
            Assert.That(request.BodyJson, Is.EqualTo(GameApiRefreshAuthRequest.Create(token).BodyJson));
            Assert.That(request.Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo("fixture_key_0123456789"));
            var wire = new Wire { Response = RunApiHttpResponse.Success(401, "{\"errorCode\":\"auth_replay_expired\"}") };
            var result = await new GameApiRefreshAuthHttpClient(wire).RefreshAsync(request);
            Assert.That(result.ErrorCode, Is.EqualTo("auth_replay_expired"));
            Assert.That(result.RequiresUserReauthentication, Is.True);
        }
        [Test]
        public async Task NicknamePublicWriteUsesOnlyDisplayNameAndBearer()
        {
            var wire = new Wire { Response = RunApiHttpResponse.Success(200, "{\"accepted\":true,\"displayName\":\"fixture_name\",\"changeStatus\":\"UPDATED\"}") };
            var client = Client(wire);
            Assert.That(client.GetType().GetMethod("SetDisplayNameAsync"), Is.Not.Null);
            Assert.That(await Call(client, "SetDisplayNameAsync", SupportInquiryTests.Session("account_test"), "fixture_name"), Is.EqualTo(true));
            var request = wire.Requests.Single(); Assert.That(request.EndpointPath, Is.EqualTo("/v1/profile/display-name"));
            Assert.That(JObject.Parse(request.BodyJson).Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "displayName" }));
            Assert.That(request.Headers.Count(h => h.Name == "Authorization"), Is.EqualTo(1));
        }
        [Test]
        public async Task ExplicitNullProfileNameIsNotMissingProfile()
        {
            var wire = new Wire { Response = RunApiHttpResponse.Success(200, "{\"accepted\":true,\"account\":{\"id\":\"account_test\"},\"profile\":{\"displayName\":null,\"commanderXp\":0}}") };
            var value = await Call(Client(wire), "ProfileAsync", SupportInquiryTests.Session("account_test"));
            Assert.That(value, Is.Not.Null);
            Assert.That(value.GetType().GetProperty("DisplayName").GetValue(value), Is.Null);
            Assert.That(wire.Requests.Single().BodyJson, Is.Null);
            Assert.That(wire.Requests.Single().Headers.Count(h => h.Name == "Authorization"), Is.EqualTo(1));
        }
        [TestCase("{\"accepted\":true,\"account\":{\"id\":\"other\"},\"profile\":{\"displayName\":null,\"commanderXp\":0}}")]
        [TestCase("{\"accepted\":true,\"account\":{\"id\":\"account_test\"},\"profile\":{\"commanderXp\":0}}")]
        [TestCase("{\"accepted\":true,\"account\":{\"id\":\"account_test\"},\"profile\":{\"displayName\":\"\",\"commanderXp\":0}}")]
        public async Task InvalidProfileCannotBecomeNicknameSetup(string json)
        {
            var wire = new Wire { Response = RunApiHttpResponse.Success(200, json) };
            Assert.That(await Call(Client(wire), "ProfileAsync", SupportInquiryTests.Session("account_test")), Is.Null);
        }
    }
}
