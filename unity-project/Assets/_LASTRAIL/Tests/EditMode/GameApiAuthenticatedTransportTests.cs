using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class GameApiAuthenticatedTransportTests
    {
        [Test]
        public async Task SendAsync_AddsBearerTokenAndPreservesExistingHeadersWithoutAccountShortcut()
        {
            var inner = new CapturingRunApiHttpTransport(RunApiHttpResponse.Success(200, "{\"accepted\":true}"));
            IGameApiAuthenticatedHttpTransport transport = new GameApiAuthenticatedHttpTransport(inner);
            var request = new RunApiHttpRequest(
                "POST",
                "/v1/gacha/banners/banner_act1_launch/pulls",
                "{\"pullCount\":10}",
                new[] { new RunApiHttpHeader("Idempotency-Key", "gacha_attempt_001") });

            var result = await transport.SendAsync(request, new GameApiAccessToken("lst1.payload.signature"));

            Assert.That(result.Outcome, Is.EqualTo(GameApiAuthenticatedHttpOutcome.Succeeded));
            Assert.That(inner.LastRequest.Method, Is.EqualTo("POST"));
            Assert.That(inner.LastRequest.EndpointPath, Is.EqualTo("/v1/gacha/banners/banner_act1_launch/pulls"));
            Assert.That(inner.LastRequest.BodyJson, Is.EqualTo("{\"pullCount\":10}"));
            Assert.That(inner.LastRequest.BodyJson.Contains("accountId"), Is.EqualTo(false));
            Assert.That(inner.LastRequest.Headers.Count, Is.EqualTo(2));
            Assert.That(inner.LastRequest.Headers[0].Name, Is.EqualTo("Idempotency-Key"));
            Assert.That(inner.LastRequest.Headers[1].Name, Is.EqualTo("Authorization"));
            Assert.That(inner.LastRequest.Headers[1].Value, Is.EqualTo("Bearer lst1.payload.signature"));
            Assert.That(result.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
            Assert.That(result.CanRetryNetwork, Is.EqualTo(false));
        }

        [Test]
        public async Task SendAsync_MapsAuthRequiredAndNetworkErrorToSeparatePolicies()
        {
            var authRequiredTransport = new GameApiAuthenticatedHttpTransport(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.Success(401, "{\"accepted\":false,\"resultStatus\":\"REJECTED\",\"errorCode\":\"auth_required\",\"message\":\"Auth required.\"}")));
            var networkTransport = new GameApiAuthenticatedHttpTransport(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.NetworkError("timeout")));
            var request = new RunApiHttpRequest("GET", "/v1/story-progress", null, null);
            var token = new GameApiAccessToken("lst1.payload.signature");

            var authRequired = await authRequiredTransport.SendAsync(request, token);
            var network = await networkTransport.SendAsync(request, token);

            Assert.That(authRequired.Outcome, Is.EqualTo(GameApiAuthenticatedHttpOutcome.AuthRequired));
            Assert.That(authRequired.ErrorCode, Is.EqualTo("auth_required"));
            Assert.That(authRequired.ErrorMessage, Is.EqualTo("Auth required."));
            Assert.That(authRequired.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(authRequired.CanRetryNetwork, Is.EqualTo(false));
            Assert.That(authRequired.Response.StatusCode, Is.EqualTo(401));
            Assert.That(network.Outcome, Is.EqualTo(GameApiAuthenticatedHttpOutcome.NetworkError));
            Assert.That(network.ErrorMessage, Is.EqualTo("timeout"));
            Assert.That(network.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
            Assert.That(network.Response, Is.EqualTo(null));
        }

        [Test]
        public void AuthenticatedTransport_RejectsNullsAndForbiddenAccountShortcutHeaders()
        {
            Assert.Throws<ArgumentNullException>(() => new GameApiAuthenticatedHttpTransport(null));
            var transport = new GameApiAuthenticatedHttpTransport(new CapturingRunApiHttpTransport(RunApiHttpResponse.Success(200, "{\"ok\":true}")));
            var token = new GameApiAccessToken("lst1.payload.signature");

            Assert.ThrowsAsync<ArgumentNullException>(() => transport.SendAsync(null, token));
            Assert.ThrowsAsync<ArgumentNullException>(() => transport.SendAsync(new RunApiHttpRequest("GET", "/v1/story-progress", null, null), null));
            Assert.ThrowsAsync<ArgumentException>(() => transport.SendAsync(
                new RunApiHttpRequest("GET", "/v1/story-progress", null, new[] { new RunApiHttpHeader("X-Account-Id", "acct_001") }),
                token));
            Assert.ThrowsAsync<ArgumentException>(() => transport.SendAsync(
                new RunApiHttpRequest("GET", "/v1/story-progress", null, new[] { new RunApiHttpHeader("X-Lastrail-Account-Id", "acct_001") }),
                token));
        }

        [Test]
        public void DebugJson_ContainsStatusOnlyAndNeverRawTokenOrAccountShortcut()
        {
            var result = GameApiAuthenticatedHttpResult.AuthRequired(
                RunApiHttpResponse.Success(401, "{\"errorCode\":\"auth_required\",\"message\":\"Auth required.\"}"),
                "auth_required",
                "Auth required.");

            var json = GameApiAuthenticatedHttpResultJsonAdapter.ToJson(result);

            Assert.That(json, Is.EqualTo("{\"outcome\":\"AuthRequired\",\"statusCode\":401,\"errorCode\":\"auth_required\",\"errorMessage\":\"Auth required.\",\"requiresAuthRefreshOrGuestLogin\":true,\"canRetryNetwork\":false,\"hasRawTokenPayload\":false,\"hasAccountShortcut\":false,\"hasRewardCalculation\":false,\"hasEconomyMutation\":false,\"hasClientSideRng\":false}"));
            Assert.That(json.Contains("lst1.payload.signature"), Is.EqualTo(false));
            Assert.That(json.Contains("refresh_plain_once"), Is.EqualTo(false));
            Assert.That(json.Contains("google_id_token"), Is.EqualTo(false));
            Assert.That(json.Contains("Authorization"), Is.EqualTo(false));
            Assert.That(json.Contains("X-Account-Id"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
        }

        [Test]
        public async Task UnityWebRequestTransport_DelegatesStableRequestShapeThroughTestableAdapter()
        {
            var adapter = new CapturingUnityWebRequestAdapter(RunApiHttpResponse.Success(200, "{\"ok\":true}"));
            IRunApiHttpTransport transport = new UnityWebRequestRunApiHttpTransport(adapter);
            var request = new RunApiHttpRequest(
                "POST",
                "/v1/offline-rewards/claim",
                null,
                new[] { new RunApiHttpHeader("Idempotency-Key", "offline_claim_001") });

            var result = await transport.SendAsync(request);

            Assert.That(adapter.LastRequest.Method, Is.EqualTo("POST"));
            Assert.That(adapter.LastRequest.EndpointPath, Is.EqualTo("/v1/offline-rewards/claim"));
            Assert.That(adapter.LastRequest.BodyJson, Is.EqualTo(null));
            Assert.That(adapter.LastRequest.Headers.Count, Is.EqualTo(1));
            Assert.That(adapter.LastRequest.Headers[0].Name, Is.EqualTo("Idempotency-Key"));
            Assert.That(adapter.LastRequest.Headers[0].Value, Is.EqualTo("offline_claim_001"));
            Assert.That(adapter.LastRequest.HasAccountShortcut, Is.EqualTo(false));
            Assert.That(result.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task RefreshAuthHttpClient_UsesSharedTransportAndSeparatesTokenReuseFromNetworkError()
        {
            var refreshedTransport = new CapturingRunApiHttpTransport(RunApiHttpResponse.Success(200, TokenBundleJson()));
            IGameApiRefreshAuthClient refreshedClient = new GameApiRefreshAuthHttpClient(refreshedTransport);
            var reusedClient = new GameApiRefreshAuthHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.Success(401, "{\"errorCode\":\"refresh_token_reused\",\"message\":\"Refresh token reused.\"}")));
            var networkClient = new GameApiRefreshAuthHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.NetworkError("timeout")));
            var request = GameApiRefreshAuthRequest.Create(new GameApiRefreshToken("refresh_plain_once"));

            var refreshed = await refreshedClient.RefreshAsync(request);
            var reused = await reusedClient.RefreshAsync(request);
            var network = await networkClient.RefreshAsync(request);

            Assert.That(refreshedTransport.LastRequest.Method, Is.EqualTo("POST"));
            Assert.That(refreshedTransport.LastRequest.EndpointPath, Is.EqualTo("/v1/auth/refresh"));
            Assert.That(refreshedTransport.LastRequest.BodyJson, Is.EqualTo("{\"refreshToken\":\"refresh_plain_once\"}"));
            Assert.That(refreshedTransport.LastRequest.BodyJson.Contains("accountId"), Is.EqualTo(false));
            Assert.That(refreshedTransport.LastRequest.Headers.Count, Is.EqualTo(0));
            Assert.That(refreshed.Outcome, Is.EqualTo(GameApiRefreshAuthClientOutcome.Refreshed));
            Assert.That(reused.Outcome, Is.EqualTo(GameApiRefreshAuthClientOutcome.RefreshTokenReused));
            Assert.That(reused.RequiresGuestReauth, Is.EqualTo(true));
            Assert.That(reused.CanRetryNetwork, Is.EqualTo(false));
            Assert.That(network.Outcome, Is.EqualTo(GameApiRefreshAuthClientOutcome.NetworkError));
            Assert.That(network.RequiresGuestReauth, Is.EqualTo(false));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
        }

        private sealed class CapturingRunApiHttpTransport : IRunApiHttpTransport
        {
            private readonly RunApiHttpResponse response;

            public CapturingRunApiHttpTransport(RunApiHttpResponse response)
            {
                this.response = response;
            }

            public RunApiHttpRequest LastRequest { get; private set; }

            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                LastRequest = request;
                return Task.FromResult(response);
            }
        }

        private sealed class CapturingUnityWebRequestAdapter : IUnityWebRequestAdapter
        {
            private readonly RunApiHttpResponse response;

            public CapturingUnityWebRequestAdapter(RunApiHttpResponse response)
            {
                this.response = response;
            }

            public UnityWebRequestTransportRequest LastRequest { get; private set; }

            public Task<RunApiHttpResponse> SendAsync(UnityWebRequestTransportRequest request)
            {
                LastRequest = request;
                return Task.FromResult(response);
            }
        }

        private static string TokenBundleJson()
        {
            return "{\"tokenType\":\"Bearer\"," +
                "\"account\":{\"id\":\"acct_guest_001\",\"status\":\"ACTIVE\"}," +
                "\"accessToken\":\"lst1.newpayload.newsignature\"," +
                "\"accessTokenExpiresAt\":\"2026-08-26T15:15:00.000Z\"," +
                "\"refreshToken\":\"refresh_plain_rotated\"," +
                "\"refreshTokenExpiresAt\":\"2026-09-25T15:15:00.0000000Z\"}";
        }
    }
}
