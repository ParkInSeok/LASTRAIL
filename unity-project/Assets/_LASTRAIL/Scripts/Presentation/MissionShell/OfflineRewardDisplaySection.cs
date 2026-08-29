using System;
using System.Collections.Generic;
using LastRail.OfflineRewards;

namespace LastRail.Presentation.MissionShell
{
    public sealed class OfflineRewardDisplaySection
    {
        private OfflineRewardDisplaySection(
            OfflineRewardPreviewClientOutcome? previewOutcome,
            string previewStatus,
            OfflineRewardClaimClientOutcome? claimOutcome,
            string claimStatus,
            string storyProgressId,
            string storyProgressLabel,
            long claimableSeconds,
            double claimableHours,
            int offlineStorageCapHours,
            string offlineStorageCapLabel,
            long minimumClaimSeconds,
            int maxClaimableHours,
            IEnumerable<RewardStoryRewardLinePresentation> rewardLines,
            string errorCode,
            string errorMessage,
            bool canRetryNetwork,
            bool requiresAuthRefreshOrGuestLogin,
            bool isNoClaimAvailable)
        {
            PreviewOutcome = previewOutcome;
            PreviewStatus = OptionalText(previewStatus);
            ClaimOutcome = claimOutcome;
            ClaimStatus = OptionalText(claimStatus);
            StoryProgressId = OptionalText(storyProgressId);
            StoryProgressLabel = OptionalText(storyProgressLabel);
            ClaimableSeconds = claimableSeconds;
            ClaimableHours = claimableHours;
            OfflineStorageCapHours = offlineStorageCapHours;
            OfflineStorageCapLabel = OptionalText(offlineStorageCapLabel);
            MinimumClaimSeconds = minimumClaimSeconds;
            MaxClaimableHours = maxClaimableHours;
            RewardLines = CopyRewardLines(rewardLines);
            ErrorCode = OptionalText(errorCode);
            ErrorMessage = OptionalText(errorMessage);
            CanRetryNetwork = canRetryNetwork;
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
            IsNoClaimAvailable = isNoClaimAvailable;
        }

        public OfflineRewardPreviewClientOutcome? PreviewOutcome { get; }
        public string PreviewStatus { get; }
        public OfflineRewardClaimClientOutcome? ClaimOutcome { get; }
        public string ClaimStatus { get; }
        public string StoryProgressId { get; }
        public string StoryProgressLabel { get; }
        public long ClaimableSeconds { get; }
        public double ClaimableHours { get; }
        public int OfflineStorageCapHours { get; }
        public string OfflineStorageCapLabel { get; }
        public long MinimumClaimSeconds { get; }
        public int MaxClaimableHours { get; }
        public IReadOnlyList<RewardStoryRewardLinePresentation> RewardLines { get; }
        public bool HasBalanceAfterDiagnostic => HasBalanceAfter(RewardLines);
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public bool CanRetryNetwork { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool IsNoClaimAvailable { get; }
        public bool HasClientCalculation => false;
        public bool HasEconomyGrant => false;

        public static OfflineRewardDisplaySection Create(
            OfflineRewardPreviewFlowResult previewFlow,
            OfflineRewardClaimFlowResult claimFlow)
        {
            var previewState = previewFlow == null ? null : previewFlow.State;
            var claimState = claimFlow == null ? null : claimFlow.State;
            var rewardLines = new List<RewardStoryRewardLinePresentation>();

            if (claimState != null && claimState.RewardLines.Count > 0)
            {
                foreach (var line in claimState.RewardLines)
                {
                    rewardLines.Add(RewardStoryRewardLinePresentation.FromOfflineRewardLine(line));
                }
            }
            else if (previewState != null)
            {
                foreach (var line in previewState.RewardLines)
                {
                    rewardLines.Add(RewardStoryRewardLinePresentation.FromOfflineRewardLine(line));
                }
            }

            var previewClient = previewFlow == null ? null : previewFlow.ClientResult;
            var claimClient = claimFlow == null ? null : claimFlow.ClientResult;
            var storyProgressId = claimState != null ? claimState.StoryProgressId : previewState == null ? null : previewState.StoryProgressId;
            var storyProgressLabel = claimState != null ? claimState.StoryProgressLabel : previewState == null ? null : previewState.StoryProgressLabel;
            var claimableSeconds = claimState != null ? claimState.ClaimableSeconds : previewState == null ? 0 : previewState.ClaimableSeconds;
            var claimableHours = claimState != null ? claimState.ClaimableHours : previewState == null ? 0 : previewState.ClaimableHours;
            var offlineStorageCapHours = claimState != null ? claimState.OfflineStorageCapHours : previewState == null ? 0 : previewState.OfflineStorageCapHours;
            var offlineStorageCapLabel = claimState != null ? claimState.OfflineStorageCapLabel : previewState == null ? null : previewState.OfflineStorageCapLabel;
            var minimumClaimSeconds = claimState != null ? claimState.MinimumClaimSeconds : previewState == null ? 0 : previewState.MinimumClaimSeconds;
            var maxClaimableHours = claimState != null ? claimState.MaxClaimableHours : previewState == null ? 0 : previewState.MaxClaimableHours;

            return new OfflineRewardDisplaySection(
                previewClient == null ? (OfflineRewardPreviewClientOutcome?)null : previewClient.Outcome,
                previewState == null ? null : previewState.PreviewStatus.ToString(),
                claimClient == null ? (OfflineRewardClaimClientOutcome?)null : claimClient.Outcome,
                claimState == null ? null : claimState.ClaimStatus.ToString(),
                storyProgressId,
                storyProgressLabel,
                claimableSeconds,
                claimableHours,
                offlineStorageCapHours,
                offlineStorageCapLabel,
                minimumClaimSeconds,
                maxClaimableHours,
                rewardLines,
                ResolveErrorCode(previewClient, claimClient),
                ResolveErrorMessage(previewClient, claimClient),
                (previewFlow != null && previewFlow.CanRetryNetwork) || (claimFlow != null && claimFlow.CanRetryNetwork),
                (previewFlow != null && previewFlow.RequiresAuthRefreshOrGuestLogin) || (claimFlow != null && claimFlow.RequiresAuthRefreshOrGuestLogin),
                claimFlow != null && claimFlow.IsNoClaimAvailable);
        }

        private static string ResolveErrorCode(
            OfflineRewardPreviewClientResult previewClient,
            OfflineRewardClaimClientResult claimClient)
        {
            if (claimClient != null && claimClient.ErrorCode != null)
            {
                return claimClient.ErrorCode;
            }

            return previewClient == null ? null : previewClient.ErrorCode;
        }

        private static string ResolveErrorMessage(
            OfflineRewardPreviewClientResult previewClient,
            OfflineRewardClaimClientResult claimClient)
        {
            if (claimClient != null && claimClient.ErrorMessage != null)
            {
                return claimClient.ErrorMessage;
            }

            return previewClient == null ? null : previewClient.ErrorMessage;
        }

        private static IReadOnlyList<RewardStoryRewardLinePresentation> CopyRewardLines(IEnumerable<RewardStoryRewardLinePresentation> rewardLines)
        {
            var copy = new List<RewardStoryRewardLinePresentation>();
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

        private static bool HasBalanceAfter(IEnumerable<RewardStoryRewardLinePresentation> rewardLines)
        {
            foreach (var line in rewardLines)
            {
                if (line.HasBalanceAfter)
                {
                    return true;
                }
            }

            return false;
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
