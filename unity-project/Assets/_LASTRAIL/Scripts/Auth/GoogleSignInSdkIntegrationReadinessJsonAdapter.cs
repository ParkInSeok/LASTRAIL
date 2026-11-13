using System;
using System.Text;

namespace LastRail.Auth
{
    public static class GoogleSignInSdkIntegrationReadinessJsonAdapter
    {
        public static string ToJson(GoogleSignInSdkIntegrationReadiness readiness)
        {
            if (readiness == null)
            {
                throw new ArgumentNullException(nameof(readiness));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "authGateKind", readiness.AuthGateKind).Append(',');
            AppendString(builder, "providerPortName", readiness.ProviderPortName).Append(',');
            AppendString(builder, "configurationSlotName", readiness.ConfigurationSlotName).Append(',');
            AppendString(builder, "sdkBridgePortName", readiness.SdkBridgePortName).Append(',');
            AppendString(builder, "sdkIdTokenProviderName", readiness.SdkIdTokenProviderName).Append(',');
            AppendString(builder, "signInFlowName", readiness.SignInFlowName).Append(',');
            AppendString(builder, "linkFlowName", readiness.LinkFlowName).Append(',');
            AppendString(builder, "tokenStorePortName", readiness.TokenStorePortName).Append(',');
            AppendString(builder, "secureStorageStrategy", readiness.SecureStorageStrategy).Append(',');
            AppendStringArray(builder, "providerOutcomeNames", readiness.ProviderOutcomeNames).Append(',');
            AppendBool(builder, "canStartProviderSignIn", readiness.CanStartGoogleSignIn).Append(',');
            AppendBool(builder, "canContinueAsGuest", readiness.CanContinueAsGuest).Append(',');
            AppendBool(builder, "canLinkGuestToGoogle", readiness.CanLinkGuestToGoogle).Append(',');
            AppendBool(builder, "hasSession", readiness.HasSession).Append(',');
            AppendBool(builder, "requiresProviderSdkImport", readiness.RequiresGoogleSignInSdkImport).Append(',');
            AppendBool(builder, "requiresOAuthClientConfiguration", readiness.RequiresOAuthClientConfiguration).Append(',');
            AppendBool(builder, "hasPlaceholderConfigSlots", readiness.HasPlaceholderConfigSlots).Append(',');
            AppendBool(builder, "hasRealOAuthClientId", readiness.HasRealOAuthClientId).Append(',');
            AppendBool(builder, "hasSecureStorageImplementation", readiness.HasSecureStorageImplementation).Append(',');
            AppendBool(builder, "usesInsecureTokenStorage", readiness.UsesInsecureTokenStorage).Append(',');
            AppendBool(builder, "hasRawTokenDebugPayload", readiness.HasRawTokenDebugPayload).Append(',');
            AppendBool(builder, "hasAccountShortcut", readiness.HasAccountShortcut).Append(',');
            AppendBool(builder, "hasRewardCalculation", readiness.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", readiness.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", readiness.HasClientSideRng).Append(',');
            AppendBool(builder, "hasBacklogChoiceEvents", readiness.HasBacklogChoiceEvents).Append(',');
            AppendBool(builder, "hasBacklogRaidModeUi", readiness.HasBacklogRaidModeUi);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendStringArray(
            StringBuilder builder,
            string fieldName,
            System.Collections.Generic.IReadOnlyList<string> values)
        {
            builder.Append('"').Append(fieldName).Append("\":[");
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendJsonString(builder, values[i]);
            }

            return builder.Append(']');
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendBool(StringBuilder builder, string fieldName, bool value)
        {
            return builder.Append('"').Append(fieldName).Append("\":").Append(value ? "true" : "false");
        }

        private static StringBuilder AppendJsonString(StringBuilder builder, string value)
        {
            return builder.Append('"').Append(Escape(value)).Append('"');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
