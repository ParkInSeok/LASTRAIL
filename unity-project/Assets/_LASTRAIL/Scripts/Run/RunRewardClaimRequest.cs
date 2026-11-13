using System;
using System.Text;

namespace LastRail.Run
{
    public sealed class RunRewardClaimRequest
    {
        public const string HttpMethod = "POST";
        public const string EndpointPathTemplate = "/v1/runs/{runId}/reward-preview/{rewardPreviewId}/claim";
        public const string IdempotencyHeaderNameConstant = "Idempotency-Key";

        private RunRewardClaimRequest(string runId, string rewardPreviewId, string idempotencyKey)
        {
            RequireText(runId, nameof(runId));
            RequireText(rewardPreviewId, nameof(rewardPreviewId));
            RequireText(idempotencyKey, nameof(idempotencyKey));

            RunId = runId;
            RewardPreviewId = rewardPreviewId;
            IdempotencyKey = idempotencyKey;
            BodyJson = BuildBodyJson(runId, rewardPreviewId);
        }

        public string Method => HttpMethod;
        public string EndpointPath => "/v1/runs/" + RunId + "/reward-preview/" + RewardPreviewId + "/claim";
        public string RunId { get; }
        public string RewardPreviewId { get; }
        public string IdempotencyKey { get; }
        public string IdempotencyHeaderName => IdempotencyHeaderNameConstant;
        public string IdempotencyHeaderValue => IdempotencyKey;
        public string BodyJson { get; }

        public static RunRewardClaimRequest Create(string runId, string rewardPreviewId, string idempotencyKey)
        {
            return new RunRewardClaimRequest(runId, rewardPreviewId, idempotencyKey);
        }

        private static string BuildBodyJson(string runId, string rewardPreviewId)
        {
            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "runId", runId).Append(',');
            AppendString(builder, "rewardPreviewId", rewardPreviewId);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            return builder.Append('"')
                .Append(fieldName)
                .Append("\":\"")
                .Append(value.Replace("\\", "\\\\").Replace("\"", "\\\""))
                .Append('"');
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
