using System;
using System.Collections.Generic;

namespace LastRail.Run
{
    public sealed class RunRewardPreviewDto
    {
        public RunRewardPreviewDto(
            string runId,
            string rewardPreviewId,
            RunRewardPreviewStatus previewStatus,
            DateTime? expiresAt,
            DateTime serverCalculatedAt,
            string balanceVersion,
            RunRewardClaimability claimability,
            IEnumerable<RunRewardLineDto> rewardLines)
        {
            RequireText(runId, nameof(runId));
            RequireText(rewardPreviewId, nameof(rewardPreviewId));
            RequireUtc(expiresAt, nameof(expiresAt));
            RequireUtc(serverCalculatedAt, nameof(serverCalculatedAt));
            RequireText(balanceVersion, nameof(balanceVersion));

            RunId = runId;
            RewardPreviewId = rewardPreviewId;
            PreviewStatus = previewStatus;
            ExpiresAt = expiresAt;
            ServerCalculatedAt = serverCalculatedAt;
            BalanceVersion = balanceVersion;
            Claimability = claimability;
            RewardLines = CopyRewardLines(rewardLines);
        }

        public string RunId { get; }
        public string RewardPreviewId { get; }
        public RunRewardPreviewStatus PreviewStatus { get; }
        public DateTime? ExpiresAt { get; }
        public DateTime ServerCalculatedAt { get; }
        public string BalanceVersion { get; }
        public RunRewardClaimability Claimability { get; }
        public IReadOnlyList<RunRewardLineDto> RewardLines { get; }

        private static IReadOnlyList<RunRewardLineDto> CopyRewardLines(IEnumerable<RunRewardLineDto> rewardLines)
        {
            if (rewardLines == null)
            {
                throw new ArgumentNullException(nameof(rewardLines));
            }

            var copy = new List<RunRewardLineDto>();
            foreach (var line in rewardLines)
            {
                if (line == null)
                {
                    throw new ArgumentException("Reward line cannot be null.", nameof(rewardLines));
                }

                copy.Add(line);
            }

            return copy.AsReadOnly();
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }

        private static void RequireUtc(DateTime? value, string name)
        {
            if (value.HasValue && value.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("DateTime must be UTC.", name);
            }
        }
    }
}
