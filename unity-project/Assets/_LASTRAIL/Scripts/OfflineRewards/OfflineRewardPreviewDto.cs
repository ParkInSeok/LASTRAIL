using System;
using System.Collections.Generic;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardPreviewDto
    {
        private readonly IReadOnlyList<OfflineRewardLineDto> rewardLines;

        public OfflineRewardPreviewDto(
            bool accepted,
            OfflineRewardPreviewStatus previewStatus,
            string storyProgressId,
            int claimableSeconds,
            double claimableHours,
            int offlineStorageCapHours,
            int minimumClaimSeconds,
            int maxClaimableHours,
            IEnumerable<OfflineRewardLineDto> rewardLines,
            OfflineRewardPreviewCalculationDto calculation,
            string narrativeVersion,
            DateTime serverCalculatedAt,
            string errorCode,
            string message,
            OfflineEconomyMetadata economyMetadata = null)
        {
            Accepted = accepted;
            PreviewStatus = previewStatus;
            StoryProgressId = Required(storyProgressId, nameof(storyProgressId));
            ClaimableSeconds = NonNegative(claimableSeconds, nameof(claimableSeconds));
            ClaimableHours = claimableHours < 0 ? throw new ArgumentException("Claimable hours cannot be negative.", nameof(claimableHours)) : claimableHours;
            OfflineStorageCapHours = NonNegative(offlineStorageCapHours, nameof(offlineStorageCapHours));
            MinimumClaimSeconds = NonNegative(minimumClaimSeconds, nameof(minimumClaimSeconds));
            MaxClaimableHours = NonNegative(maxClaimableHours, nameof(maxClaimableHours));
            this.rewardLines = CopyRewardLines(rewardLines);
            Calculation = calculation ?? throw new ArgumentNullException(nameof(calculation));
            NarrativeVersion = Required(narrativeVersion, nameof(narrativeVersion));
            if (serverCalculatedAt.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Server calculated time must be UTC.", nameof(serverCalculatedAt));
            }

            ServerCalculatedAt = serverCalculatedAt;
            ErrorCode = Optional(errorCode);
            Message = Optional(message);
            EconomyMetadata = economyMetadata;
        }

        public bool Accepted { get; }
        public OfflineEconomyMetadata EconomyMetadata { get; }
        public OfflineRewardPreviewStatus PreviewStatus { get; }
        public string StoryProgressId { get; }
        public int ClaimableSeconds { get; }
        public double ClaimableHours { get; }
        public int OfflineStorageCapHours { get; }
        public int MinimumClaimSeconds { get; }
        public int MaxClaimableHours { get; }
        public IReadOnlyList<OfflineRewardLineDto> RewardLines => rewardLines;
        public OfflineRewardPreviewCalculationDto Calculation { get; }
        public string NarrativeVersion { get; }
        public DateTime ServerCalculatedAt { get; }
        public string ErrorCode { get; }
        public string Message { get; }

        private static IReadOnlyList<OfflineRewardLineDto> CopyRewardLines(IEnumerable<OfflineRewardLineDto> values)
        {
            var copied = new List<OfflineRewardLineDto>();
            if (values == null)
            {
                return copied.AsReadOnly();
            }

            foreach (var value in values)
            {
                if (value == null)
                {
                    throw new ArgumentException("Reward line cannot be null.", nameof(values));
                }

                copied.Add(value);
            }

            return copied.AsReadOnly();
        }

        private static int NonNegative(int value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentException("Value cannot be negative.", paramName);
            }

            return value;
        }

        private static string Required(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }

        private static string Optional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
