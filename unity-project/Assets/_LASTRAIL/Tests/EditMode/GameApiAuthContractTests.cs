using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class GameApiAuthContractTests
    {
        [Test]
        public void GuestRequest_CreatesPostRequestWithoutAccountBodyOrShortcutHeaders()
        {
            var request = GameApiGuestAuthRequest.Create();

            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/auth/guest"));
            Assert.That(request.BodyJson, Is.EqualTo(null));
            Assert.That(request.Headers.Count, Is.EqualTo(0));
        }

        [Test]
        public void GuestResponseJson_MapsBearerAccountAndTokens()
        {
            var response = GameApiGuestAuthResponseJsonAdapter.FromJson(Json());

            Assert.That(response.TokenType, Is.EqualTo(GameApiTokenType.Bearer));
            Assert.That(response.Account.Id, Is.EqualTo("acct_guest_001"));
            Assert.That(response.Account.Status, Is.EqualTo("ACTIVE"));
            Assert.That(response.AccessToken.Value, Is.EqualTo("lst1.payload.signature"));
            Assert.That(response.AccessTokenExpiresAt, Is.EqualTo(new DateTime(2026, 8, 26, 15, 0, 0, 123, DateTimeKind.Utc)));
            Assert.That(response.RefreshToken.Value, Is.EqualTo("refresh_plain_once"));
            Assert.That(response.RefreshTokenExpiresAt, Is.EqualTo(new DateTime(2026, 9, 25, 15, 0, 0, DateTimeKind.Utc)));
            Assert.That(response.ContainsSecretStorageMaterial, Is.EqualTo(false));
        }

        [Test]
        public void GuestResponseJson_RejectsInvalidTokenTypeTokenFormatOrNonUtcTime()
        {
            Assert.Throws<ArgumentException>(() => GameApiGuestAuthResponseJsonAdapter.FromJson(Json().Replace("\"tokenType\":\"Bearer\"", "\"tokenType\":\"Basic\"")));
            Assert.Throws<ArgumentException>(() => GameApiGuestAuthResponseJsonAdapter.FromJson(Json().Replace("\"accessToken\":\"lst1.payload.signature\"", "\"accessToken\":\"bad\"")));
            Assert.Throws<FormatException>(() => GameApiGuestAuthResponseJsonAdapter.FromJson(Json().Replace("\"accessTokenExpiresAt\":\"2026-08-26T15:00:00.123Z\"", "\"accessTokenExpiresAt\":\"2026-08-27T00:00:00.123+09:00\"")));
        }

        [Test]
        public void TokenStorePort_StoresSessionSnapshotWithoutConcreteSecureStorage()
        {
            IGameApiAuthTokenStore store = new CapturingAuthTokenStore();
            var snapshot = GameApiAuthSessionSnapshot.FromGuestResponse(GameApiGuestAuthResponseJsonAdapter.FromJson(Json()));

            var saved = store.SaveAsync(snapshot).Result;
            var loaded = store.LoadAsync().Result;

            Assert.That(saved, Is.EqualTo(GameApiAuthTokenStoreResult.Saved));
            Assert.That(loaded.AccountId, Is.EqualTo("acct_guest_001"));
            Assert.That(loaded.AccessToken.Value, Is.EqualTo("lst1.payload.signature"));
            Assert.That(loaded.RefreshToken.Value, Is.EqualTo("refresh_plain_once"));
        }

        [Test]
        public void RefreshRequest_CreatesPostRequestWithRefreshTokenBodyOnly()
        {
            var request = GameApiRefreshAuthRequest.Create(new GameApiRefreshToken("refresh_plain_once"));

            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/auth/refresh"));
            Assert.That(request.Headers.Count, Is.EqualTo(0));
            Assert.That(request.BodyJson, Is.EqualTo("{\"refreshToken\":\"refresh_plain_once\"}"));
            Assert.That(request.BodyJson.Contains("accountId"), Is.EqualTo(false));
        }

        [Test]
        public void RefreshResponseJson_MapsRotatedBearerTokens()
        {
            var response = GameApiRefreshAuthResponseJsonAdapter.FromJson(RefreshJson());

            Assert.That(response.TokenType, Is.EqualTo(GameApiTokenType.Bearer));
            Assert.That(response.Account.Id, Is.EqualTo("acct_guest_001"));
            Assert.That(response.AccessToken.Value, Is.EqualTo("lst1.newpayload.newsignature"));
            Assert.That(response.RefreshToken.Value, Is.EqualTo("refresh_plain_rotated"));
            Assert.That(response.AccessTokenExpiresAt, Is.EqualTo(new DateTime(2026, 8, 26, 15, 15, 0, DateTimeKind.Utc)));
            Assert.That(response.RefreshTokenExpiresAt, Is.EqualTo(new DateTime(2026, 9, 25, 15, 15, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void RefreshClientResult_DistinguishesRefreshReuseFromNetworkRetry()
        {
            var refreshed = GameApiRefreshAuthClientResult.Refreshed(GameApiRefreshAuthResponseJsonAdapter.FromJson(RefreshJson()));
            var reused = GameApiRefreshAuthClientResult.RefreshTokenReused("refresh_token_reused", "Refresh token reused.");
            var network = GameApiRefreshAuthClientResult.NetworkError("timeout");

            Assert.That(refreshed.Outcome, Is.EqualTo(GameApiRefreshAuthClientOutcome.Refreshed));
            Assert.That(reused.Outcome, Is.EqualTo(GameApiRefreshAuthClientOutcome.RefreshTokenReused));
            Assert.That(reused.RequiresGuestReauth, Is.EqualTo(true));
            Assert.That(reused.CanRetryNetwork, Is.EqualTo(false));
            Assert.That(network.Outcome, Is.EqualTo(GameApiRefreshAuthClientOutcome.NetworkError));
            Assert.That(network.RequiresGuestReauth, Is.EqualTo(false));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
        }

        [Test]
        public void TokenRotationPolicy_ReplacesStoredRefreshTokenOnSuccess()
        {
            var existing = GameApiAuthSessionSnapshot.FromGuestResponse(GameApiGuestAuthResponseJsonAdapter.FromJson(Json()));
            var refreshResponse = GameApiRefreshAuthResponseJsonAdapter.FromJson(RefreshJson());

            var rotated = GameApiAuthTokenRotationPolicy.RotateFromRefreshResponse(existing, refreshResponse);

            Assert.That(rotated.AccountId, Is.EqualTo("acct_guest_001"));
            Assert.That(rotated.AccessToken.Value, Is.EqualTo("lst1.newpayload.newsignature"));
            Assert.That(rotated.RefreshToken.Value, Is.EqualTo("refresh_plain_rotated"));
            Assert.That(rotated.RefreshToken.Value == existing.RefreshToken.Value, Is.EqualTo(false));
        }

        [Test]
        public void BearerAuthRequestFactory_AddsAuthorizationHeaderAndPreservesExistingHeaders()
        {
            var request = new RunApiHttpRequest(
                "POST",
                "/v1/gacha/banners/banner_act1_launch/pulls",
                "{\"pullCount\":10}",
                new[] { new RunApiHttpHeader("Idempotency-Key", "gacha_attempt_001") });
            var token = new GameApiAccessToken("lst1.payload.signature");

            var authenticated = GameApiBearerAuthRequestFactory.WithBearerToken(request, token);

            Assert.That(authenticated.Headers.Count, Is.EqualTo(2));
            Assert.That(authenticated.Headers[0].Name, Is.EqualTo("Idempotency-Key"));
            Assert.That(authenticated.Headers[1].Name, Is.EqualTo("Authorization"));
            Assert.That(authenticated.Headers[1].Value, Is.EqualTo("Bearer lst1.payload.signature"));
            Assert.That(authenticated.BodyJson.Contains("accountId"), Is.EqualTo(false));
        }

        [Test]
        public void BearerAuthRequestFactory_RejectsClientSuppliedAccountShortcutHeaders()
        {
            var token = new GameApiAccessToken("lst1.payload.signature");
            Assert.Throws<ArgumentException>(() => GameApiBearerAuthRequestFactory.WithBearerToken(
                new RunApiHttpRequest("POST", "/v1/gacha/banners/banner/pulls", "{}", new[] { new RunApiHttpHeader("X-Account-Id", "acct") }),
                token));
            Assert.Throws<ArgumentException>(() => GameApiBearerAuthRequestFactory.WithBearerToken(
                new RunApiHttpRequest("POST", "/v1/gacha/banners/banner/pulls", "{}", new[] { new RunApiHttpHeader("X-Lastrail-Account-Id", "acct") }),
                token));
        }

        private static string Json()
        {
            return "{\"tokenType\":\"Bearer\"," +
                "\"account\":{\"id\":\"acct_guest_001\",\"status\":\"ACTIVE\"}," +
                "\"accessToken\":\"lst1.payload.signature\"," +
                "\"accessTokenExpiresAt\":\"2026-08-26T15:00:00.123Z\"," +
                "\"refreshToken\":\"refresh_plain_once\"," +
                "\"refreshTokenExpiresAt\":\"2026-09-25T15:00:00.0000000Z\"}";
        }

        private static string RefreshJson()
        {
            return "{\"tokenType\":\"Bearer\"," +
                "\"account\":{\"id\":\"acct_guest_001\",\"status\":\"ACTIVE\"}," +
                "\"accessToken\":\"lst1.newpayload.newsignature\"," +
                "\"accessTokenExpiresAt\":\"2026-08-26T15:15:00.000Z\"," +
                "\"refreshToken\":\"refresh_plain_rotated\"," +
                "\"refreshTokenExpiresAt\":\"2026-09-25T15:15:00.0000000Z\"}";
        }

        private sealed class CapturingAuthTokenStore : IGameApiAuthTokenStore
        {
            private GameApiAuthSessionSnapshot snapshot;

            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot value)
            {
                snapshot = value;
                return Task.FromResult(GameApiAuthTokenStoreResult.Saved);
            }

            public Task<GameApiAuthSessionSnapshot> LoadAsync()
            {
                return Task.FromResult(snapshot);
            }
        }
    }
}
