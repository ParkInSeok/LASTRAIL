using System;
using System.Collections.Generic;

namespace LastRail.Run
{
    public sealed class RunRewardClaimResponseDto
    {
        public RunRewardClaimResponseDto(
            string runId,
            string rewardPreviewId,
            string rewardClaimId,
            RunRewardClaimResultStatus claimResultStatus,
            DateTime serverClaimedAt,
            string balanceVersion,
            IEnumerable<RunRewardLineDto> rewardLines,
            string errorCode,
            string message)
        {
            RequireText(runId, nameof(runId));
            RequireText(rewardPreviewId, nameof(rewardPreviewId));
            RequireUtc(serverClaimedAt, nameof(serverClaimedAt));
            RequireText(balanceVersion, nameof(balanceVersion));
            if ((claimResultStatus == RunRewardClaimResultStatus.Claimed
                    || claimResultStatus == RunRewardClaimResultStatus.DuplicateReplay)
                && string.IsNullOrWhiteSpace(rewardClaimId))
            {
                throw new ArgumentException("Claimed responses require a reward claim id.", nameof(rewardClaimId));
            }

            RunId = runId;
            RewardPreviewId = rewardPreviewId;
            RewardClaimId = OptionalText(rewardClaimId);
            ClaimResultStatus = claimResultStatus;
            ServerClaimedAt = serverClaimedAt;
            BalanceVersion = balanceVersion;
            RewardLines = CopyRewardLines(rewardLines);
            ErrorCode = OptionalText(errorCode);
            Message = OptionalText(message);
        }

        public string RunId { get; }
        public string RewardPreviewId { get; }
        public string RewardClaimId { get; }
        public RunRewardClaimResultStatus ClaimResultStatus { get; }
        public DateTime ServerClaimedAt { get; }
        public string BalanceVersion { get; }
        public IReadOnlyList<RunRewardLineDto> RewardLines { get; }
        public string ErrorCode { get; }
        public string Message { get; }

        private static IReadOnlyList<RunRewardLineDto> CopyRewardLines(IEnumerable<RunRewardLineDto> rewardLines)
        {
            if (rewardLines == null)
            {
                return new List<RunRewardLineDto>().AsReadOnly();
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

        private static void RequireUtc(DateTime value, string name)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("DateTime must be UTC.", name);
            }
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
