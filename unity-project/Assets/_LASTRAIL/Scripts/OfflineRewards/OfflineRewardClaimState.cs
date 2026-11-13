using System;
using System.Collections.Generic;
using LastRail.Localization;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardClaimState
    {
        private OfflineRewardClaimState(OfflineRewardClaimResponseDto dto)
        {
            Accepted = dto.Accepted;
            ClaimStatus = dto.ClaimStatus;
            OfflineRewardClaimId = dto.OfflineRewardClaimId;
            RewardTransactionId = dto.RewardTransactionId;
            EconomyTransactionId = dto.EconomyTransactionId;
            AccountMutationId = dto.AccountMutationId;
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
            ServerClaimedAt = dto.ServerClaimedAt;
            NarrativeVersion = dto.NarrativeVersion;
            ErrorCode = dto.ErrorCode;
            Message = dto.Message;
        }

        public bool Accepted { get; }
        public OfflineRewardClaimStatus ClaimStatus { get; }
        public string OfflineRewardClaimId { get; }
        public string RewardTransactionId { get; }
        public string EconomyTransactionId { get; }
        public string AccountMutationId { get; }
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
        public DateTime ServerClaimedAt { get; }
        public string NarrativeVersion { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool HasClientCalculation => false;
        public bool HasEconomyGrant => false;

        public static OfflineRewardClaimState FromResponse(OfflineRewardClaimResponseDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            return new OfflineRewardClaimState(dto);
        }
    }
}
