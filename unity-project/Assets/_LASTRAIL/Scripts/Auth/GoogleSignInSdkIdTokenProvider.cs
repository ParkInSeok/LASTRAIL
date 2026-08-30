using System;
using System.Threading.Tasks;

namespace LastRail.Auth
{
    public sealed class GoogleSignInSdkIdTokenProvider : IGoogleIdTokenProvider
    {
        private readonly GoogleSignInSdkConfigurationSlot configuration;
        private readonly IGoogleSignInSdkBridge bridge;

        public GoogleSignInSdkIdTokenProvider(
            GoogleSignInSdkConfigurationSlot configuration,
            IGoogleSignInSdkBridge bridge)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        public async Task<GoogleIdTokenProviderResult> GetIdTokenAsync()
        {
            if (!configuration.HasRealOAuthValues)
            {
                return GoogleIdTokenProviderResult.ProviderError(
                    "google_oauth_config_missing",
                    "Google OAuth configuration is deferred until app registration.");
            }

            var result = await bridge.RequestIdTokenAsync(configuration);
            if (result == null)
            {
                return GoogleIdTokenProviderResult.ProviderError(
                    "google_sdk_result_missing",
                    "Google Sign-In SDK did not return a provider result.");
            }

            switch (result.Outcome)
            {
                case GoogleSignInSdkBridgeOutcome.Success:
                    return GoogleIdTokenProviderResult.Success(result.IdToken);
                case GoogleSignInSdkBridgeOutcome.Cancelled:
                    return GoogleIdTokenProviderResult.Cancelled(result.Message);
                case GoogleSignInSdkBridgeOutcome.NetworkError:
                    return GoogleIdTokenProviderResult.NetworkError(result.Message);
                case GoogleSignInSdkBridgeOutcome.ProviderError:
                    return GoogleIdTokenProviderResult.ProviderError(result.ErrorCode, result.Message);
                default:
                    return GoogleIdTokenProviderResult.ProviderError(
                        "google_sdk_unknown_outcome",
                        "Google Sign-In SDK returned an unknown provider outcome.");
            }
        }
    }
}
