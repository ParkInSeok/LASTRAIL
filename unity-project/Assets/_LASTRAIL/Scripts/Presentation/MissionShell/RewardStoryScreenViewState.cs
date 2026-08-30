using System;
using System.Collections.Generic;

namespace LastRail.Presentation.MissionShell
{
    public sealed class RewardStoryScreenViewState
    {
        public const int LaunchMinimumTouchTargetDp = 48;
        public const int PortraitAspectWidth = 9;
        public const int PortraitAspectHeight = 16;

        public RewardStoryScreenViewState(
            MissionShellSurface surface,
            string title,
            string subtitle,
            IEnumerable<string> bodyLines,
            IEnumerable<string> bodyLineStringIds,
            string primaryActionLabel,
            string secondaryActionLabel,
            IEnumerable<RewardStoryScreenRewardLineViewState> rewardLines,
            string storyProgressId,
            string storyProgressLabel,
            string offlineStorageCapLabel,
            int unlockedOperationCount,
            string codexUnlockStatusLabel,
            bool canNavigateToCodexArchive,
            bool hasCommittedStaminaRefund,
            string staminaRefundStatus,
            string staminaRefundStringId,
            string errorCode,
            string errorMessage,
            bool isNoClaimAvailable,
            bool canRetryWithSameIdempotencyKey,
            bool canRetryNetwork,
            bool requiresAuthRefreshOrGuestLogin)
        {
            Surface = surface;
            Title = RequireText(title, nameof(title));
            Subtitle = OptionalText(subtitle);
            BodyLines = CopyText(bodyLines, nameof(bodyLines));
            BodyLineStringIds = CopyText(bodyLineStringIds, nameof(bodyLineStringIds));
            PrimaryActionLabel = OptionalText(primaryActionLabel);
            SecondaryActionLabel = OptionalText(secondaryActionLabel);
            RewardLines = CopyRewardLines(rewardLines);
            StoryProgressId = OptionalText(storyProgressId);
            StoryProgressLabel = OptionalText(storyProgressLabel);
            OfflineStorageCapLabel = OptionalText(offlineStorageCapLabel);
            UnlockedOperationCount = RequireNonNegative(unlockedOperationCount, nameof(unlockedOperationCount));
            CodexUnlockStatusLabel = OptionalText(codexUnlockStatusLabel);
            CanNavigateToCodexArchive = canNavigateToCodexArchive;
            HasCommittedStaminaRefund = hasCommittedStaminaRefund;
            StaminaRefundStatus = OptionalText(staminaRefundStatus);
            StaminaRefundStringId = OptionalText(staminaRefundStringId);
            ErrorCode = OptionalText(errorCode);
            ErrorMessage = OptionalText(errorMessage);
            IsNoClaimAvailable = isNoClaimAvailable;
            CanRetryWithSameIdempotencyKey = canRetryWithSameIdempotencyKey;
            CanRetryNetwork = canRetryNetwork;
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
        }

        public MissionShellSurface Surface { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public IReadOnlyList<string> BodyLines { get; }
        public IReadOnlyList<string> BodyLineStringIds { get; }
        public string PrimaryActionLabel { get; }
        public string SecondaryActionLabel { get; }
        public IReadOnlyList<RewardStoryScreenRewardLineViewState> RewardLines { get; }
        public int RewardLineCount => RewardLines.Count;
        public string StoryProgressId { get; }
        public string StoryProgressLabel { get; }
        public string OfflineStorageCapLabel { get; }
        public int UnlockedOperationCount { get; }
        public string CodexUnlockStatusLabel { get; }
        public bool CanNavigateToCodexArchive { get; }
        public bool HasCommittedStaminaRefund { get; }
        public string StaminaRefundStatus { get; }
        public string StaminaRefundStringId { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public bool IsNoClaimAvailable { get; }
        public bool CanRetryWithSameIdempotencyKey { get; }
        public bool CanRetryNetwork { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool UsesPortraitSafeArea => true;
        public int MinimumTouchTargetDp => LaunchMinimumTouchTargetDp;
        public int PreferredAspectWidth => PortraitAspectWidth;
        public int PreferredAspectHeight => PortraitAspectHeight;
        public bool HasReadableTextSlots => true;
        public bool RequiresTextClamp => true;
        public bool IsDisplayOnly => true;
        public bool HasServerTransport => false;
        public bool HasUnlockTruth => false;
        public bool HasStoryProgressMutation => false;
        public bool HasRewardCalculation => false;
        public bool HasOfflineRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        private static IReadOnlyList<string> CopyText(IEnumerable<string> values, string paramName)
        {
            var copied = new List<string>();
            if (values == null)
            {
                return copied.AsReadOnly();
            }

            foreach (var value in values)
            {
                copied.Add(RequireText(value, paramName));
            }

            return copied.AsReadOnly();
        }

        private static IReadOnlyList<RewardStoryScreenRewardLineViewState> CopyRewardLines(IEnumerable<RewardStoryScreenRewardLineViewState> values)
        {
            var copied = new List<RewardStoryScreenRewardLineViewState>();
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

        private static int RequireNonNegative(int value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
            }

            return value;
        }

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
