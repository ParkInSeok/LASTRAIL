using System;
using System.Text;

namespace LastRail.Narrative
{
    public sealed class StoryProgressReadFlowResult
    {
        private StoryProgressReadFlowResult(
            StoryProgressReadClientResult clientResult,
            StoryProgressReadState state,
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

        public StoryProgressReadClientResult ClientResult { get; }
        public StoryProgressReadState State { get; }
        public string DiagnosticJson { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool CanRetryNetwork { get; }
        public bool IsBlockingStoryProgressRead => false;

        public static StoryProgressReadFlowResult ForClientResult(StoryProgressReadClientResult clientResult)
        {
            if (clientResult == null)
            {
                throw new ArgumentNullException(nameof(clientResult));
            }

            if (clientResult.Outcome == StoryProgressReadClientOutcome.Found)
            {
                var state = StoryProgressReadState.FromResponse(clientResult.Response);
                return new StoryProgressReadFlowResult(
                    clientResult,
                    state,
                    StoryProgressReadStateJsonAdapter.ToJson(state),
                    false,
                    false);
            }

            var requiresAuth = string.Equals(clientResult.ErrorCode, "auth_required", StringComparison.Ordinal);
            var canRetryNetwork = clientResult.Outcome == StoryProgressReadClientOutcome.NetworkError;
            return new StoryProgressReadFlowResult(
                clientResult,
                null,
                ErrorJson(clientResult, requiresAuth, canRetryNetwork),
                requiresAuth,
                canRetryNetwork);
        }

        private static string ErrorJson(
            StoryProgressReadClientResult clientResult,
            bool requiresAuthRefreshOrGuestLogin,
            bool canRetryNetwork)
        {
            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "clientOutcome", clientResult.Outcome.ToString()).Append(',');
            builder.Append("\"hasState\":false,");
            AppendOptionalString(builder, "errorCode", clientResult.ErrorCode).Append(',');
            AppendOptionalString(builder, "errorMessage", clientResult.ErrorMessage).Append(',');
            builder.Append("\"requiresAuthRefreshOrGuestLogin\":").Append(requiresAuthRefreshOrGuestLogin ? "true" : "false").Append(',');
            builder.Append("\"canRetryNetwork\":").Append(canRetryNetwork ? "true" : "false").Append(',');
            builder.Append("\"isBlockingStoryProgressRead\":false,");
            builder.Append("\"hasRewardCalculation\":false,");
            builder.Append("\"hasUnlockTruth\":false");
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
