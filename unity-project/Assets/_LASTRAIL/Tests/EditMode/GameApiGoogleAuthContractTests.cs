using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class GameApiGoogleAuthContractTests
    {
        [Test]
        public void GoogleIdTokenProviderResult_DistinguishesUserCancelProviderErrorAndNetworkRetry()
        {
            var success = GoogleIdTokenProviderResult.Success(new GoogleIdToken("google.id.token"));
            var cancelled = GoogleIdTokenProviderResult.Cancelled("User cancelled.");
            var providerError = GoogleIdTokenProviderResult.ProviderError("google_sign_in_failed", "Provider failed.");
            var network = GoogleIdTokenProviderResult.NetworkError("Provider network timeout.");

            Assert.That(success.Outcome, Is.EqualTo(GoogleIdTokenProviderOutcome.Success));
            Assert.That(success.IdToken.Value, Is.EqualTo("google.id.token"));
            Assert.That(cancelled.Outcome, Is.EqualTo(GoogleIdTokenProviderOutcome.Cancelled));
            Assert.That(cancelled.CanRetryNetwork, Is.EqualTo(false));
            Assert.That(providerError.Outcome, Is.EqualTo(GoogleIdTokenProviderOutcome.ProviderError));
            Assert.That(providerError.ErrorCode, Is.EqualTo("google_sign_in_failed"));
            Assert.That(network.Outcome, Is.EqualTo(GoogleIdTokenProviderOutcome.NetworkError));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
        }

        [Test]
        public void GoogleAuthRequest_CreatesPostRequestWithIdTokenBodyOnly()
        {
            var request = GameApiGoogleAuthRequest.Create(new GoogleIdToken("google.id.token"));

            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/auth/google"));
            Assert.That(request.Headers.Count, Is.EqualTo(0));
            Assert.That(request.BodyJson, Is.EqualTo("{\"idToken\":\"google.id.token\"}"));
            Assert.That(request.BodyJson.Contains("accountId"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("refreshToken"), Is.EqualTo(false));
        }

        [Test]
        public void GoogleAuthResponseJson_MapsToSessionSnapshot()
        {
            var response = GameApiGoogleAuthResponseJsonAdapter.FromJson(TokenBundleJson("acct_google_001"));
            var snapshot = GameApiAuthSessionSnapshot.FromGoogleAuthResponse(response);

            Assert.That(response.TokenType, Is.EqualTo(GameApiTokenType.Bearer));
            Assert.That(response.Account.Id, Is.EqualTo("acct_google_001"));
            Assert.That(snapshot.AccountId, Is.EqualTo("acct_google_001"));
            Assert.That(snapshot.AccessToken.Value, Is.EqualTo("lst1.google.payload"));
            Assert.That(snapshot.RefreshToken.Value, Is.EqualTo("google_refresh_plain_once"));
            Assert.That(response.ContainsSecretStorageMaterial, Is.EqualTo(false));
        }

        [Test]
        public void GoogleLinkRequest_CreatesBearerAuthenticatedPostWithoutTokenBody()
        {
            var request = GameApiGoogleLinkRequest.Create(new GoogleIdToken("google.id.token"));
            var authenticated = GameApiBearerAuthRequestFactory.WithBearerToken(
                request.ToHttpRequest(),
                new GameApiAccessToken("lst1.current.payload"));

            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/auth/google/link"));
            Assert.That(request.BodyJson, Is.EqualTo("{\"idToken\":\"google.id.token\"}"));
            Assert.That(authenticated.Headers.Count, Is.EqualTo(1));
            Assert.That(authenticated.Headers[0].Name, Is.EqualTo("Authorization"));
            Assert.That(authenticated.BodyJson.Contains("accountId"), Is.EqualTo(false));
            Assert.That(authenticated.BodyJson.Contains("lst1.current.payload"), Is.EqualTo(false));
        }

        [Test]
        public void GoogleLinkResponseJson_MapsTokenBundleAndLinkStatus()
        {
            var linked = GameApiGoogleLinkResponseJsonAdapter.FromJson(LinkJson("LINKED"));
            var alreadyLinked = GameApiGoogleLinkResponseJsonAdapter.FromJson(LinkJson("ALREADY_LINKED"));

            Assert.That(linked.LinkStatus, Is.EqualTo(GameApiGoogleLinkStatus.Linked));
            Assert.That(alreadyLinked.LinkStatus, Is.EqualTo(GameApiGoogleLinkStatus.AlreadyLinked));
            Assert.That(GameApiAuthSessionSnapshot.FromGoogleLinkResponse(linked).AccountId, Is.EqualTo("acct_guest_001"));
            Assert.Throws<ArgumentException>(() => GameApiGoogleLinkResponseJsonAdapter.FromJson(LinkJson("UNKNOWN")));
        }

        [Test]
        public void GoogleAuthClientResult_SeparatesConflictAuthRequiredAndNetworkError()
        {
            var signedIn = GameApiGoogleAuthClientResult.SignedIn(GameApiGoogleAuthResponseJsonAdapter.FromJson(TokenBundleJson("acct_google_001")));
            var conflict = GameApiGoogleAuthClientResult.Conflict("google_identity_already_linked", "Google account is already linked.");
            var network = GameApiGoogleAuthClientResult.NetworkError("timeout");

            Assert.That(signedIn.Outcome, Is.EqualTo(GameApiGoogleAuthClientOutcome.SignedIn));
            Assert.That(conflict.RequiresAccountRecovery, Is.EqualTo(true));
            Assert.That(conflict.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
        }

        [Test]
        public void GoogleLinkClientResult_SeparatesLinkedAlreadyLinkedConflictAuthRequiredAndNetworkError()
        {
            var linked = GameApiGoogleLinkClientResult.Linked(GameApiGoogleLinkResponseJsonAdapter.FromJson(LinkJson("LINKED")));
            var already = GameApiGoogleLinkClientResult.AlreadyLinked(GameApiGoogleLinkResponseJsonAdapter.FromJson(LinkJson("ALREADY_LINKED")));
            var conflict = GameApiGoogleLinkClientResult.Conflict("google_identity_already_linked", "Google account already linked.");
            var authRequired = GameApiGoogleLinkClientResult.AuthRequired("auth_required", "Auth required.");
            var network = GameApiGoogleLinkClientResult.NetworkError("timeout");

            Assert.That(linked.Outcome, Is.EqualTo(GameApiGoogleLinkClientOutcome.Linked));
            Assert.That(already.Outcome, Is.EqualTo(GameApiGoogleLinkClientOutcome.AlreadyLinked));
            Assert.That(conflict.RequiresAccountRecovery, Is.EqualTo(true));
            Assert.That(authRequired.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
        }

        [Test]
        public void GoogleAuthFlow_UsesProviderTokenThenClientAndConvergesToSessionSnapshot()
        {
            IGoogleIdTokenProvider provider = new StaticGoogleIdTokenProvider(GoogleIdTokenProviderResult.Success(new GoogleIdToken("google.id.token")));
            IGameApiGoogleAuthClient client = new StaticGoogleAuthClient(GameApiGoogleAuthClientResult.SignedIn(GameApiGoogleAuthResponseJsonAdapter.FromJson(TokenBundleJson("acct_google_001"))));

            var result = GameApiGoogleAuthFlow.SignInAsync(provider, client).Result;

            Assert.That(result.Outcome, Is.EqualTo(GameApiGoogleAuthFlowOutcome.SignedIn));
            Assert.That(result.SessionSnapshot.AccountId, Is.EqualTo("acct_google_001"));
            Assert.That(result.ProviderOutcome, Is.EqualTo(GoogleIdTokenProviderOutcome.Success));
        }

        [Test]
        public void GoogleLinkFlow_UsesProviderTokenAndExistingBearerSession()
        {
            IGoogleIdTokenProvider provider = new StaticGoogleIdTokenProvider(GoogleIdTokenProviderResult.Success(new GoogleIdToken("google.id.token")));
            IGameApiGoogleLinkClient client = new StaticGoogleLinkClient(GameApiGoogleLinkClientResult.Linked(GameApiGoogleLinkResponseJsonAdapter.FromJson(LinkJson("LINKED"))));
            var existing = GameApiAuthSessionSnapshot.FromGuestResponse(GameApiGuestAuthResponseJsonAdapter.FromJson(GuestJson()));

            var result = GameApiGoogleLinkFlow.LinkAsync(provider, client, existing).Result;

            Assert.That(result.Outcome, Is.EqualTo(GameApiGoogleLinkFlowOutcome.Linked));
            Assert.That(result.SessionSnapshot.AccountId, Is.EqualTo("acct_guest_001"));
            Assert.That(result.LinkStatus, Is.EqualTo(GameApiGoogleLinkStatus.Linked));
        }

        [Test]
        public void GoogleAuthDebugJson_ContainsStatusAndErrorButNoRawTokens()
        {
            var result = GameApiGoogleLinkClientResult.Conflict("google_identity_already_linked", "Google account already linked.");

            var json = GameApiGoogleAuthDebugJsonAdapter.ToJson(result);

            Assert.That(json, Is.EqualTo("{\"flow\":\"googleLink\",\"outcome\":\"Conflict\",\"linkStatus\":null,\"errorCode\":\"google_identity_already_linked\",\"message\":\"Google account already linked.\",\"canRetryNetwork\":false,\"requiresAuthRefreshOrGuestLogin\":false,\"requiresAccountRecovery\":true}"));
            Assert.That(json.Contains("idToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("secret"), Is.EqualTo(false));
        }

        private static string TokenBundleJson(string accountId)
        {
            return "{\"tokenType\":\"Bearer\"," +
                "\"account\":{\"id\":\"" + accountId + "\",\"status\":\"ACTIVE\"}," +
                "\"accessToken\":\"lst1.google.payload\"," +
                "\"accessTokenExpiresAt\":\"2026-08-26T15:00:00.123Z\"," +
                "\"refreshToken\":\"google_refresh_plain_once\"," +
                "\"refreshTokenExpiresAt\":\"2026-09-25T15:00:00.0000000Z\"}";
        }

        private static string LinkJson(string status)
        {
            return "{\"tokenType\":\"Bearer\"," +
                "\"account\":{\"id\":\"acct_guest_001\",\"status\":\"ACTIVE\"}," +
                "\"accessToken\":\"lst1.linked.payload\"," +
                "\"accessTokenExpiresAt\":\"2026-08-26T15:00:00.123Z\"," +
                "\"refreshToken\":\"linked_refresh_plain_once\"," +
                "\"refreshTokenExpiresAt\":\"2026-09-25T15:00:00.0000000Z\"," +
                "\"linkStatus\":\"" + status + "\"}";
        }

        private static string GuestJson()
        {
            return "{\"tokenType\":\"Bearer\"," +
                "\"account\":{\"id\":\"acct_guest_001\",\"status\":\"ACTIVE\"}," +
                "\"accessToken\":\"lst1.payload.signature\"," +
                "\"accessTokenExpiresAt\":\"2026-08-26T15:00:00.123Z\"," +
                "\"refreshToken\":\"refresh_plain_once\"," +
                "\"refreshTokenExpiresAt\":\"2026-09-25T15:00:00.0000000Z\"}";
        }

        private sealed class StaticGoogleIdTokenProvider : IGoogleIdTokenProvider
        {
            private readonly GoogleIdTokenProviderResult result;

            public StaticGoogleIdTokenProvider(GoogleIdTokenProviderResult result)
            {
                this.result = result;
            }

            public Task<GoogleIdTokenProviderResult> GetIdTokenAsync()
            {
                return Task.FromResult(result);
            }
        }

        private sealed class StaticGoogleAuthClient : IGameApiGoogleAuthClient
        {
            private readonly GameApiGoogleAuthClientResult result;

            public StaticGoogleAuthClient(GameApiGoogleAuthClientResult result)
            {
                this.result = result;
            }

            public Task<GameApiGoogleAuthClientResult> SignInAsync(GameApiGoogleAuthRequest request)
            {
                return Task.FromResult(result);
            }
        }

        private sealed class StaticGoogleLinkClient : IGameApiGoogleLinkClient
        {
            private readonly GameApiGoogleLinkClientResult result;

            public StaticGoogleLinkClient(GameApiGoogleLinkClientResult result)
            {
                this.result = result;
            }

            public Task<GameApiGoogleLinkClientResult> LinkAsync(GameApiGoogleLinkRequest request, GameApiAccessToken accessToken)
            {
                return Task.FromResult(result);
            }
        }
    }
}
