using System;
using System.Text;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardClaimFlowResult
    {
        private OfflineRewardClaimFlowResult(
            OfflineRewardClaimClientResult clientResult,
            OfflineRewardClaimState state,
            string diagnosticJson,
            bool requiresAuthRefreshOrGuestLogin,
            bool canRetryNetwork,
            bool isNoClaimAvailable)
        {
            ClientResult = clientResult;
            State = state;
            DiagnosticJson = diagnosticJson;
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
            CanRetryNetwork = canRetryNetwork;
            IsNoClaimAvailable = isNoClaimAvailable;
        }

        public OfflineRewardClaimClientResult ClientResult { get; }
        public OfflineRewardClaimState State { get; }
        public string DiagnosticJson { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool CanRetryNetwork { get; }
        public bool IsNoClaimAvailable { get; }
        public bool IsBlockingOfflineRewardClaim => false;

        public static OfflineRewardClaimFlowResult ForClientResult(OfflineRewardClaimClientResult clientResult)
        {
            return FromClientResult(clientResult);
        }

        public static OfflineRewardClaimFlowResult FromClientResult(OfflineRewardClaimClientResult clientResult)
        {
            if (clientResult == null)
            {
                throw new ArgumentNullException(nameof(clientResult));
            }

            if (clientResult.Outcome == OfflineRewardClaimClientOutcome.Claimed ||
                clientResult.Outcome == OfflineRewardClaimClientOutcome.DuplicateReplay)
            {
                var state = OfflineRewardClaimState.FromResponse(clientResult.Response);
                return new OfflineRewardClaimFlowResult(
                    clientResult,
                    state,
                    OfflineRewardClaimStateJsonAdapter.ToJson(state),
                    requiresAuthRefreshOrGuestLogin: false,
                    canRetryNetwork: false,
                    isNoClaimAvailable: false);
            }

            var requiresAuth = string.Equals(clientResult.ErrorCode, "auth_required", StringComparison.Ordinal);
            var canRetryNetwork = clientResult.Outcome == OfflineRewardClaimClientOutcome.NetworkError;
            var isNoClaimAvailable = string.Equals(clientResult.ErrorCode, "offline_reward_not_claimable", StringComparison.Ordinal);
            return new OfflineRewardClaimFlowResult(
                clientResult,
                null,
                ErrorDiagnosticJson(clientResult, requiresAuth, canRetryNetwork, isNoClaimAvailable),
                requiresAuth,
                canRetryNetwork,
                isNoClaimAvailable);
        }

        private static string ErrorDiagnosticJson(
            OfflineRewardClaimClientResult clientResult,
            bool requiresAuthRefreshOrGuestLogin,
            bool canRetryNetwork,
            bool isNoClaimAvailable)
        {
            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "clientOutcome", clientResult.Outcome.ToString()).Append(',');
            builder.Append("\"hasState\":false,");
            AppendOptionalString(builder, "errorCode", clientResult.ErrorCode).Append(',');
            AppendOptionalString(builder, "errorMessage", clientResult.ErrorMessage).Append(',');
            builder.Append("\"requiresAuthRefreshOrGuestLogin\":").Append(requiresAuthRefreshOrGuestLogin ? "true" : "false").Append(',');
            builder.Append("\"canRetryNetwork\":").Append(canRetryNetwork ? "true" : "false").Append(',');
            builder.Append("\"isNoClaimAvailable\":").Append(isNoClaimAvailable ? "true" : "false").Append(',');
            builder.Append("\"isBlockingOfflineRewardClaim\":false,");
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
