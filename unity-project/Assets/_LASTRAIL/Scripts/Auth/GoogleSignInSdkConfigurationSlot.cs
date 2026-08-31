using System;

namespace LastRail.Auth
{
    public sealed class GoogleSignInSdkConfigurationSlot
    {
        private GoogleSignInSdkConfigurationSlot(
            string providerName,
            string integrationMode,
            string sdkPackageStatus,
            string webClientIdStatus,
            string audienceStatus,
            string androidPackageNameStatus,
            string shaFingerprintStatus,
            string tokenStoreStrategy,
            string realOAuthEnvironmentPolicy,
            bool requiresGoogleSignInSdkImport,
            bool requiresOAuthAppRegistration,
            bool hasRealOAuthValues)
        {
            ProviderName = RequireText(providerName, nameof(providerName));
            IntegrationMode = RequireText(integrationMode, nameof(integrationMode));
            SdkPackageStatus = RequireText(sdkPackageStatus, nameof(sdkPackageStatus));
            WebClientIdStatus = RequireText(webClientIdStatus, nameof(webClientIdStatus));
            AudienceStatus = RequireText(audienceStatus, nameof(audienceStatus));
            AndroidPackageNameStatus = RequireText(androidPackageNameStatus, nameof(androidPackageNameStatus));
            ShaFingerprintStatus = RequireText(shaFingerprintStatus, nameof(shaFingerprintStatus));
            TokenStoreStrategy = RequireText(tokenStoreStrategy, nameof(tokenStoreStrategy));
            RealOAuthEnvironmentPolicy = RequireText(realOAuthEnvironmentPolicy, nameof(realOAuthEnvironmentPolicy));
            RequiresGoogleSignInSdkImport = requiresGoogleSignInSdkImport;
            RequiresOAuthAppRegistration = requiresOAuthAppRegistration;
            HasRealOAuthValues = hasRealOAuthValues;
        }

        public string ProviderName { get; }
        public string IntegrationMode { get; }
        public string SdkPackageStatus { get; }
        public string WebClientIdStatus { get; }
        public string AudienceStatus { get; }
        public string AndroidPackageNameStatus { get; }
        public string ShaFingerprintStatus { get; }
        public string TokenStoreStrategy { get; }
        public string RealOAuthEnvironmentPolicy { get; }
        public bool RequiresGoogleSignInSdkImport { get; }
        public bool RequiresOAuthAppRegistration { get; }
        public bool HasRealOAuthValues { get; }
        public bool AllowsRealGoogleInDev => false;
        public bool HasProductionSecret => false;
        public bool UsesPlayerPrefsTokenStorage => false;
        public bool HasRawTokenDebugPayload => false;
        public bool HasAccountShortcut => false;

        public static GoogleSignInSdkConfigurationSlot DeferredLaunchConfig()
        {
            return new GoogleSignInSdkConfigurationSlot(
                "GoogleSignInUnitySdk",
                "PlaceholderConfigSlots",
                "NOT_IMPORTED",
                "DEFERRED_APP_REGISTRATION",
                "DEFERRED_APP_REGISTRATION",
                "DEFERRED_APP_REGISTRATION",
                "DEFERRED_APP_REGISTRATION",
                "AndroidKeystoreFirstPartyBridge",
                "PRD_ONLY_AFTER_APP_REGISTRATION",
                true,
                true,
                false);
        }

        public static GoogleSignInSdkConfigurationSlot ConfiguredForLocalHarness()
        {
            return new GoogleSignInSdkConfigurationSlot(
                "GoogleSignInUnitySdk",
                "CompileSafeProviderBridge",
                "FAKE_BRIDGE",
                "CONFIGURED_FOR_LOCAL_HARNESS",
                "CONFIGURED_FOR_LOCAL_HARNESS",
                "CONFIGURED_FOR_LOCAL_HARNESS",
                "CONFIGURED_FOR_LOCAL_HARNESS",
                "AndroidKeystoreFirstPartyBridge",
                "LOCAL_HARNESS_ONLY",
                true,
                false,
                true);
        }

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
