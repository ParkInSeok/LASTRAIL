using System;
using System.Text;

namespace LastRail.Presentation.AuthGate
{
    public static class AuthGateUxStateJsonAdapter
    {
        public static string ToJson(AuthGateUxState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "kind", state.Kind.ToString()).Append(',');
            AppendString(builder, "title", state.Title).Append(',');
            AppendString(builder, "primaryActionLabel", state.PrimaryActionLabel).Append(',');
            AppendOptionalString(builder, "secondaryActionLabel", state.SecondaryActionLabel).Append(',');
            AppendOptionalString(builder, "providerOutcome", state.ProviderOutcome).Append(',');
            AppendOptionalString(builder, "googleAuthOutcome", state.GoogleAuthOutcome).Append(',');
            AppendOptionalString(builder, "googleLinkOutcome", state.GoogleLinkOutcome).Append(',');
            AppendOptionalString(builder, "refreshOutcome", state.RefreshOutcome).Append(',');
            AppendOptionalString(builder, "linkStatus", state.LinkStatus).Append(',');
            AppendOptionalString(builder, "errorCode", state.ErrorCode).Append(',');
            AppendOptionalString(builder, "message", state.Message).Append(',');
            AppendBool(builder, "canRetryNetwork", state.CanRetryNetwork).Append(',');
            AppendBool(builder, "requiresAuthRefreshOrGuestLogin", state.RequiresAuthRefreshOrGuestLogin).Append(',');
            AppendBool(builder, "requiresGuestReauth", state.RequiresGuestReauth).Append(',');
            AppendBool(builder, "requiresAccountRecovery", state.RequiresAccountRecovery).Append(',');
            AppendBool(builder, "canStartGoogleSignIn", state.CanStartGoogleSignIn).Append(',');
            AppendBool(builder, "canContinueAsGuest", state.CanContinueAsGuest).Append(',');
            AppendBool(builder, "canLinkGoogle", state.CanLinkGoogle).Append(',');
            AppendBool(builder, "hasSession", state.HasSession).Append(',');
            AppendOptionalString(builder, "accountStatus", state.AccountStatus).Append(',');
            AppendBool(builder, "isDisplayOnly", state.IsDisplayOnly).Append(',');
            AppendBool(builder, "hasRawTokenPayload", state.HasRawTokenPayload).Append(',');
            AppendBool(builder, "hasAccountShortcut", state.HasAccountShortcut).Append(',');
            AppendBool(builder, "hasRewardCalculation", state.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", state.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", state.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendOptionalString(StringBuilder builder, string fieldName, string value)
        {
            if (value == null)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendString(builder, fieldName, value);
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
