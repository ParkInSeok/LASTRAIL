using System;
using System.Text;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardPreviewFlowResult
    {
        private OfflineRewardPreviewFlowResult(
            OfflineRewardPreviewClientResult clientResult,
            OfflineRewardPreviewState state,
            string diagnosticJson,
            bool requiresAuthRefreshOrGuestLogin,
            bool canRetryNetwork)
        {
            ClientResult = clientResult;
            State = state;
            DiagnosticJson = diagnosticJson;
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
            CanRetryNetwork = canRetryNetwork;
        }

        public OfflineRewardPreviewClientResult ClientResult { get; }
        public OfflineRewardPreviewState State { get; }
        public string DiagnosticJson { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool CanRetryNetwork { get; }
        public bool IsBlockingOfflineRewardPreview => false;

        public static OfflineRewardPreviewFlowResult ForClientResult(OfflineRewardPreviewClientResult clientResult)
        {
            if (clientResult == null)
            {
                throw new ArgumentNullException(nameof(clientResult));
            }

            if (clientResult.Outcome == OfflineRewardPreviewClientOutcome.Available ||
                clientResult.Outcome == OfflineRewardPreviewClientOutcome.Empty)
            {
                var state = OfflineRewardPreviewState.FromResponse(clientResult.Response);
                return new OfflineRewardPreviewFlowResult(
                    clientResult,
                    state,
                    OfflineRewardPreviewStateJsonAdapter.ToJson(state),
                    false,
                    false);
            }

            var requiresAuth = string.Equals(clientResult.ErrorCode, "auth_required", StringComparison.Ordinal);
            var canRetryNetwork = clientResult.Outcome == OfflineRewardPreviewClientOutcome.NetworkError;
            return new OfflineRewardPreviewFlowResult(
                clientResult,
                null,
                ErrorJson(clientResult, requiresAuth, canRetryNetwork),
                requiresAuth,
                canRetryNetwork);
        }

        private static string ErrorJson(OfflineRewardPreviewClientResult clientResult, bool requiresAuth, bool canRetryNetwork)
        {
            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "clientOutcome", clientResult.Outcome.ToString()).Append(',');
            builder.Append("\"hasState\":false,");
            AppendOptionalString(builder, "errorCode", clientResult.ErrorCode).Append(',');
            AppendOptionalString(builder, "errorMessage", clientResult.ErrorMessage).Append(',');
            builder.Append("\"requiresAuthRefreshOrGuestLogin\":").Append(requiresAuth ? "true" : "false").Append(',');
            builder.Append("\"canRetryNetwork\":").Append(canRetryNetwork ? "true" : "false").Append(',');
            builder.Append("\"isBlockingOfflineRewardPreview\":false,");
            builder.Append("\"hasClientCalculation\":false,");
            builder.Append("\"hasEconomyGrant\":false");
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
