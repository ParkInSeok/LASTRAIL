using System;

namespace LastRail.Run
{
    public sealed class RunRewardPreviewLookupRequest
    {
        public const string HttpMethod = "GET";
        public const string EndpointPathTemplate = "/v1/runs/{runId}/reward-preview/{rewardPreviewId}";

        private RunRewardPreviewLookupRequest(string runId, string rewardPreviewId)
        {
            RequireText(runId, nameof(runId));
            RequireText(rewardPreviewId, nameof(rewardPreviewId));

            RunId = runId;
            RewardPreviewId = rewardPreviewId;
        }

        public string Method => HttpMethod;
        public string EndpointPath => "/v1/runs/" + RunId + "/reward-preview/" + RewardPreviewId;
        public string RunId { get; }
        public string RewardPreviewId { get; }
        public string BodyJson => null;
        public bool RequiresIdempotencyKey => false;

        public static RunRewardPreviewLookupRequest Create(string runId, string rewardPreviewId)
        {
            return new RunRewardPreviewLookupRequest(runId, rewardPreviewId);
        }

        public static RunRewardPreviewLookupRequest FromRewardReviewEntry(RunRewardReviewEntryState entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (!entry.HasRewardPreviewId)
            {
                throw new InvalidOperationException("Reward preview lookup requires a reward preview id.");
            }

            return Create(entry.RunId, entry.RewardPreviewId);
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
