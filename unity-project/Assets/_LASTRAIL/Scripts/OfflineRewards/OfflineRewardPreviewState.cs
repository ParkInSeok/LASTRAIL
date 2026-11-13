using System;
using System.Collections.Generic;
using LastRail.Localization;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardPreviewState
    {
        private OfflineRewardPreviewState(OfflineRewardPreviewDto dto)
        {
            Accepted = dto.Accepted;
            PreviewStatus = dto.PreviewStatus;
            StoryProgressId = dto.StoryProgressId;
            StoryProgressLabel = UnityDisplayLabels.ForActOneStoryProgress(dto.StoryProgressId);
            ClaimableSeconds = dto.ClaimableSeconds;
            ClaimableHours = dto.ClaimableHours;
            OfflineStorageCapHours = dto.OfflineStorageCapHours;
            OfflineStorageCapLabel = UnityDisplayLabels.ForOfflineRewardCapHours(dto.OfflineStorageCapHours);
            MinimumClaimSeconds = dto.MinimumClaimSeconds;
            MaxClaimableHours = dto.MaxClaimableHours;
            RewardLines = new List<OfflineRewardLineDto>(dto.RewardLines).AsReadOnly();
            Calculation = dto.Calculation;
            NarrativeVersion = dto.NarrativeVersion;
            ServerCalculatedAt = dto.ServerCalculatedAt;
            ErrorCode = dto.ErrorCode;
            Message = dto.Message;
        }

        public bool Accepted { get; }
        public OfflineRewardPreviewStatus PreviewStatus { get; }
        public string StoryProgressId { get; }
        public string StoryProgressLabel { get; }
        public int ClaimableSeconds { get; }
        public double ClaimableHours { get; }
        public int OfflineStorageCapHours { get; }
        public string OfflineStorageCapLabel { get; }
        public int MinimumClaimSeconds { get; }
        public int MaxClaimableHours { get; }
        public IReadOnlyList<OfflineRewardLineDto> RewardLines { get; }
        public OfflineRewardPreviewCalculationDto Calculation { get; }
        public string NarrativeVersion { get; }
        public DateTime ServerCalculatedAt { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool HasClientCalculation => false;
        public bool HasEconomyGrant => false;

        public static OfflineRewardPreviewState FromResponse(OfflineRewardPreviewDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            return new OfflineRewardPreviewState(dto);
        }
    }
}
