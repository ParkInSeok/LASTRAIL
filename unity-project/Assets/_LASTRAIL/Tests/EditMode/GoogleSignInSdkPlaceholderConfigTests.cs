using System.Threading.Tasks;
using NUnit.Framework;
using LastRail.Auth;

namespace LastRail.Tests.EditMode
{
    public sealed class GoogleSignInSdkPlaceholderConfigTests
    {
        [Test]
        public void DeferredLaunchConfig_ExposesSlotsWithoutRealOAuthValues()
        {
            var config = GoogleSignInSdkConfigurationSlot.DeferredLaunchConfig();

            Assert.That(config.ProviderName, Is.EqualTo("GoogleSignInUnitySdk"));
            Assert.That(config.IntegrationMode, Is.EqualTo("PlaceholderConfigSlots"));
            Assert.That(config.SdkPackageStatus, Is.EqualTo("NOT_IMPORTED"));
            Assert.That(config.WebClientIdStatus, Is.EqualTo("DEFERRED_APP_REGISTRATION"));
            Assert.That(config.AudienceStatus, Is.EqualTo("DEFERRED_APP_REGISTRATION"));
            Assert.That(config.AndroidPackageNameStatus, Is.EqualTo("DEFERRED_APP_REGISTRATION"));
            Assert.That(config.ShaFingerprintStatus, Is.EqualTo("DEFERRED_APP_REGISTRATION"));
            Assert.That(config.TokenStoreStrategy, Is.EqualTo("AndroidKeystoreFirstPartyBridge"));
            Assert.That(config.RequiresGoogleSignInSdkImport, Is.EqualTo(true));
            Assert.That(config.RequiresOAuthAppRegistration, Is.EqualTo(true));
            Assert.That(config.HasRealOAuthValues, Is.EqualTo(false));
            Assert.That(config.RealOAuthEnvironmentPolicy, Is.EqualTo("PRD_ONLY_AFTER_APP_REGISTRATION"));
            Assert.That(config.AllowsRealGoogleInDev, Is.EqualTo(false));
            Assert.That(config.HasProductionSecret, Is.EqualTo(false));
            Assert.That(config.UsesPlayerPrefsTokenStorage, Is.EqualTo(false));
            Assert.That(config.HasRawTokenDebugPayload, Is.EqualTo(false));
            Assert.That(config.HasAccountShortcut, Is.EqualTo(false));
        }

        [Test]
        public void DeferredConfigJson_WritesStableStatusesWithoutRawTokenOrSecretValues()
        {
            var json = GoogleSignInSdkConfigurationSlotJsonAdapter.ToJson(
                GoogleSignInSdkConfigurationSlot.DeferredLaunchConfig());

            Assert.That(json.Contains("\"providerName\":\"GoogleSignInUnitySdk\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"integrationMode\":\"PlaceholderConfigSlots\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"sdkPackageStatus\":\"NOT_IMPORTED\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"webClientIdStatus\":\"DEFERRED_APP_REGISTRATION\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"audienceStatus\":\"DEFERRED_APP_REGISTRATION\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"androidPackageNameStatus\":\"DEFERRED_APP_REGISTRATION\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"shaFingerprintStatus\":\"DEFERRED_APP_REGISTRATION\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresGoogleSignInSdkImport\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresOAuthAppRegistration\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRealOAuthValues\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"realOAuthEnvironmentPolicy\":\"PRD_ONLY_AFTER_APP_REGISTRATION\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"allowsRealGoogleInDev\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"tokenStoreStrategy\":\"AndroidKeystoreFirstPartyBridge\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"usesPlayerPrefsTokenStorage\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRawTokenDebugPayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("idToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("Authorization"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("client_secret"), Is.EqualTo(false));
            Assert.That(json.Contains("production_secret"), Is.EqualTo(false));
        }

        [Test]
        public async Task SdkIdTokenProvider_WhenOAuthConfigDeferred_ReturnsProviderErrorWithoutCallingBridge()
        {
            var bridge = new RecordingGoogleSignInSdkBridge(GoogleSignInSdkBridgeResult.Cancelled("cancelled"));
            var provider = new GoogleSignInSdkIdTokenProvider(
                GoogleSignInSdkConfigurationSlot.DeferredLaunchConfig(),
                bridge);

            var result = await provider.GetIdTokenAsync();

            Assert.That(result.Outcome, Is.EqualTo(GoogleIdTokenProviderOutcome.ProviderError));
            Assert.That(result.ErrorCode, Is.EqualTo("google_oauth_config_missing"));
            Assert.That(result.Message, Does.Contain("deferred"));
            Assert.That(bridge.CallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task SdkIdTokenProvider_MapsCancelledProviderAndNetworkOutcomes()
        {
            var cancelled = await new GoogleSignInSdkIdTokenProvider(
                GoogleSignInSdkConfigurationSlot.ConfiguredForLocalHarness(),
                new RecordingGoogleSignInSdkBridge(GoogleSignInSdkBridgeResult.Cancelled("User cancelled Google sign-in.")))
                .GetIdTokenAsync();

            var providerError = await new GoogleSignInSdkIdTokenProvider(
                GoogleSignInSdkConfigurationSlot.ConfiguredForLocalHarness(),
                new RecordingGoogleSignInSdkBridge(GoogleSignInSdkBridgeResult.ProviderError("google_sdk_unavailable", "SDK unavailable.")))
                .GetIdTokenAsync();

            var networkError = await new GoogleSignInSdkIdTokenProvider(
                GoogleSignInSdkConfigurationSlot.ConfiguredForLocalHarness(),
                new RecordingGoogleSignInSdkBridge(GoogleSignInSdkBridgeResult.NetworkError("Network unavailable.")))
                .GetIdTokenAsync();

            Assert.That(cancelled.Outcome, Is.EqualTo(GoogleIdTokenProviderOutcome.Cancelled));
            Assert.That(providerError.Outcome, Is.EqualTo(GoogleIdTokenProviderOutcome.ProviderError));
            Assert.That(providerError.ErrorCode, Is.EqualTo("google_sdk_unavailable"));
            Assert.That(networkError.Outcome, Is.EqualTo(GoogleIdTokenProviderOutcome.NetworkError));
            Assert.That(networkError.CanRetryNetwork, Is.EqualTo(true));
        }

        [Test]
        public async Task DeferredSdkProvider_FailsBeforeGameApiGoogleAuthOrLinkClientCalls()
        {
            var provider = new GoogleSignInSdkIdTokenProvider(
                GoogleSignInSdkConfigurationSlot.DeferredLaunchConfig(),
                new RecordingGoogleSignInSdkBridge(GoogleSignInSdkBridgeResult.ProviderError("should_not_call", "Should not call.")));

            var signIn = await GameApiGoogleAuthFlow.SignInAsync(provider, new ThrowingGoogleAuthClient());
            var link = await GameApiGoogleLinkFlow.LinkAsync(provider, new ThrowingGoogleLinkClient(), CreateGuestSession());

            Assert.That(signIn.Outcome, Is.EqualTo(GameApiGoogleAuthFlowOutcome.ProviderError));
            Assert.That(signIn.ErrorCode, Is.EqualTo("google_oauth_config_missing"));
            Assert.That(link.Outcome, Is.EqualTo(GameApiGoogleLinkFlowOutcome.ProviderError));
            Assert.That(link.ErrorCode, Is.EqualTo("google_oauth_config_missing"));
        }

        private sealed class RecordingGoogleSignInSdkBridge : IGoogleSignInSdkBridge
        {
            private readonly GoogleSignInSdkBridgeResult result;

            public RecordingGoogleSignInSdkBridge(GoogleSignInSdkBridgeResult result)
            {
                this.result = result;
            }

            public int CallCount { get; private set; }

            public Task<GoogleSignInSdkBridgeResult> RequestIdTokenAsync(GoogleSignInSdkConfigurationSlot configuration)
            {
                CallCount++;
                return Task.FromResult(result);
            }
        }

        private sealed class ThrowingGoogleAuthClient : IGameApiGoogleAuthClient
        {
            public Task<GameApiGoogleAuthClientResult> SignInAsync(GameApiGoogleAuthRequest request)
            {
                throw new AssertionException("Game API Google auth client should not be called while OAuth config is deferred.");
            }
        }

        private sealed class ThrowingGoogleLinkClient : IGameApiGoogleLinkClient
        {
            public Task<GameApiGoogleLinkClientResult> LinkAsync(GameApiGoogleLinkRequest request, GameApiAccessToken accessToken)
            {
                throw new AssertionException("Game API Google link client should not be called while OAuth config is deferred.");
            }
        }

        private static GameApiAuthSessionSnapshot CreateGuestSession()
        {
            return new GameApiAuthSessionSnapshot(
                "acct_guest_001",
                "ACTIVE",
                new GameApiAccessToken("lst1.access.payload"),
                new System.DateTime(2026, 8, 30, 1, 0, 0, System.DateTimeKind.Utc),
                new GameApiRefreshToken("refresh_once"),
                new System.DateTime(2026, 9, 30, 1, 0, 0, System.DateTimeKind.Utc));
        }
    }
}
