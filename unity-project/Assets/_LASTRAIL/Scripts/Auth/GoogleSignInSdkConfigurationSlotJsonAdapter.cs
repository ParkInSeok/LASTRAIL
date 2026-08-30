using System;

namespace LastRail.Auth
{
    public static class GoogleSignInSdkConfigurationSlotJsonAdapter
    {
        public static string ToJson(GoogleSignInSdkConfigurationSlot config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return "{"
                + "\"providerName\":\"" + Escape(config.ProviderName) + "\","
                + "\"integrationMode\":\"" + Escape(config.IntegrationMode) + "\","
                + "\"sdkPackageStatus\":\"" + Escape(config.SdkPackageStatus) + "\","
                + "\"webClientIdStatus\":\"" + Escape(config.WebClientIdStatus) + "\","
                + "\"audienceStatus\":\"" + Escape(config.AudienceStatus) + "\","
                + "\"androidPackageNameStatus\":\"" + Escape(config.AndroidPackageNameStatus) + "\","
                + "\"shaFingerprintStatus\":\"" + Escape(config.ShaFingerprintStatus) + "\","
                + "\"requiresGoogleSignInSdkImport\":" + Bool(config.RequiresGoogleSignInSdkImport) + ","
                + "\"requiresOAuthAppRegistration\":" + Bool(config.RequiresOAuthAppRegistration) + ","
                + "\"hasRealOAuthValues\":" + Bool(config.HasRealOAuthValues) + ","
                + "\"hasProductionSecret\":" + Bool(config.HasProductionSecret) + ","
                + "\"tokenStoreStrategy\":\"" + Escape(config.TokenStoreStrategy) + "\","
                + "\"usesPlayerPrefsTokenStorage\":" + Bool(config.UsesPlayerPrefsTokenStorage) + ","
                + "\"hasRawTokenDebugPayload\":" + Bool(config.HasRawTokenDebugPayload) + ","
                + "\"hasAccountShortcut\":" + Bool(config.HasAccountShortcut)
                + "}";
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
