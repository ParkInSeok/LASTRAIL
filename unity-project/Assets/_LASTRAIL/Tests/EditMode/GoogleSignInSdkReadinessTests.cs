using System;
using NUnit.Framework;
using LastRail.Auth;
using LastRail.Presentation.AuthGate;

namespace LastRail.Tests.EditMode
{
    public sealed class GoogleSignInSdkReadinessTests
    {
        [Test]
        public void FromFirstLaunch_ExposesProviderBoundaryWithoutRealOAuthValues()
        {
            var readiness = GoogleSignInSdkIntegrationReadiness.FromAuthGate(AuthGateUxState.FirstLaunch());

            Assert.That(readiness.ProviderPortName, Is.EqualTo("IGoogleIdTokenProvider"));
            Assert.That(readiness.SignInFlowName, Is.EqualTo("GameApiGoogleAuthFlow"));
            Assert.That(readiness.LinkFlowName, Is.EqualTo("GameApiGoogleLinkFlow"));
            Assert.That(readiness.TokenStorePortName, Is.EqualTo("IGameApiAuthTokenStore"));
            Assert.That(readiness.SecureStorageStrategy, Is.EqualTo("AndroidKeystoreFirstPartyBridge"));
            Assert.That(readiness.CanStartGoogleSignIn, Is.EqualTo(true));
            Assert.That(readiness.CanContinueAsGuest, Is.EqualTo(true));
            Assert.That(readiness.CanLinkGuestToGoogle, Is.EqualTo(false));
            Assert.That(readiness.RequiresGoogleSignInSdkImport, Is.EqualTo(true));
            Assert.That(readiness.RequiresOAuthClientConfiguration, Is.EqualTo(true));
            Assert.That(readiness.HasRealOAuthClientId, Is.EqualTo(false));
            Assert.That(readiness.HasRawTokenDebugPayload, Is.EqualTo(false));
            Assert.That(readiness.UsesInsecureTokenStorage, Is.EqualTo(false));
            Assert.That(readiness.HasAccountShortcut, Is.EqualTo(false));
            Assert.That(readiness.ProviderOutcomeNames, Does.Contain("Success"));
            Assert.That(readiness.ProviderOutcomeNames, Does.Contain("Cancelled"));
            Assert.That(readiness.ProviderOutcomeNames, Does.Contain("ProviderError"));
            Assert.That(readiness.ProviderOutcomeNames, Does.Contain("NetworkError"));
        }

        [Test]
        public void FromGuestSession_ExposesGuestToGoogleLinkReadinessAndKeystoreStorePortOnly()
        {
            var session = new GameApiAuthSessionSnapshot(
                "acct_guest_001",
                "ACTIVE",
                new GameApiAccessToken("lst1.access.payload"),
                new DateTime(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc),
                new GameApiRefreshToken("refresh_once"),
                new DateTime(2026, 9, 30, 1, 0, 0, DateTimeKind.Utc));

            var readiness = GoogleSignInSdkIntegrationReadiness.FromAuthGate(AuthGateUxState.FromGuestSession(session));

            Assert.That(readiness.CanStartGoogleSignIn, Is.EqualTo(false));
            Assert.That(readiness.CanContinueAsGuest, Is.EqualTo(true));
            Assert.That(readiness.CanLinkGuestToGoogle, Is.EqualTo(true));
            Assert.That(readiness.HasSession, Is.EqualTo(true));
            Assert.That(readiness.AuthGateKind, Is.EqualTo("GuestReady"));
            Assert.That(readiness.TokenStorePortName, Is.EqualTo("IGameApiAuthTokenStore"));
            Assert.That(readiness.SecureStorageStrategy, Is.EqualTo("AndroidKeystoreFirstPartyBridge"));
            Assert.That(readiness.HasSecureStorageImplementation, Is.EqualTo(true));
            Assert.That(readiness.UsesInsecureTokenStorage, Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesStableReadinessFieldsWithoutTokensClientIdsOrTruthPayload()
        {
            var readiness = GoogleSignInSdkIntegrationReadiness.FromAuthGate(AuthGateUxState.FirstLaunch());

            var json = GoogleSignInSdkIntegrationReadinessJsonAdapter.ToJson(readiness);

            Assert.That(json.Contains("\"authGateKind\":\"StartOptions\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"providerPortName\":\"IGoogleIdTokenProvider\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresProviderSdkImport\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresOAuthClientConfiguration\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"secureStorageStrategy\":\"AndroidKeystoreFirstPartyBridge\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRealOAuthClientId\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasSecureStorageImplementation\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"usesInsecureTokenStorage\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRawTokenDebugPayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasAccountShortcut\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("idToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("clientId"), Is.EqualTo(false));
            Assert.That(json.Contains("audience"), Is.EqualTo(false));
            Assert.That(json.Contains("secret"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("PlayerPrefs"), Is.EqualTo(false));
            Assert.That(json.Contains("UnityWebRequest"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("economyGrant"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }
    }
}
