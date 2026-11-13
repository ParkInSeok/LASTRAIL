using System;
using System.Collections.Generic;

namespace LastRail.Presentation.MissionShell
{
    public sealed class MissionShellScreenViewState
    {
        public const int LaunchMinimumTouchTargetDp = 48;
        public const int PortraitAspectWidth = 9;
        public const int PortraitAspectHeight = 16;

        public MissionShellScreenViewState(
            MissionShellSurface surface,
            string title,
            string subtitle,
            IEnumerable<string> bodyLines,
            IEnumerable<string> bodyLineStringIds,
            string primaryActionLabel,
            string primaryActionStringId,
            bool isPrimaryActionEnabled,
            string secondaryActionLabel,
            MissionShellSurface? nextBattleSurface,
            IEnumerable<MissionShellMissionRowState> missionRows,
            string errorCode,
            string errorMessage,
            bool requiresAuthRefreshOrGuestLogin,
            bool requiresStateResync,
            bool canRetryWithSameIdempotencyKey)
        {
            Surface = surface;
            Title = RequireText(title, nameof(title));
            Subtitle = OptionalText(subtitle);
            BodyLines = CopyOptionalText(bodyLines, nameof(bodyLines));
            BodyLineStringIds = CopyOptionalText(bodyLineStringIds, nameof(bodyLineStringIds));
            PrimaryActionLabel = OptionalText(primaryActionLabel);
            PrimaryActionStringId = OptionalText(primaryActionStringId);
            IsPrimaryActionEnabled = isPrimaryActionEnabled;
            SecondaryActionLabel = OptionalText(secondaryActionLabel);
            NextBattleSurface = nextBattleSurface;
            MissionRows = CopyMissionRows(missionRows);
            ErrorCode = OptionalText(errorCode);
            ErrorMessage = OptionalText(errorMessage);
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
            RequiresStateResync = requiresStateResync;
            CanRetryWithSameIdempotencyKey = canRetryWithSameIdempotencyKey;
        }

        public MissionShellSurface Surface { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public IReadOnlyList<string> BodyLines { get; }
        public IReadOnlyList<string> BodyLineStringIds { get; }
        public string PrimaryActionLabel { get; }
        public string PrimaryActionStringId { get; }
        public bool IsPrimaryActionEnabled { get; }
        public string SecondaryActionLabel { get; }
        public MissionShellSurface? NextBattleSurface { get; }
        public IReadOnlyList<MissionShellMissionRowState> MissionRows { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool RequiresStateResync { get; }
        public bool CanRetryWithSameIdempotencyKey { get; }
        public bool UsesPortraitSafeArea => true;
        public int MinimumTouchTargetDp => LaunchMinimumTouchTargetDp;
        public int PreferredAspectWidth => PortraitAspectWidth;
        public int PreferredAspectHeight => PortraitAspectHeight;
        public bool HasReadableHudLabels => true;
        public bool IsDisplayOnly => true;
        public bool HasServerTransport => false;
        public bool HasUnlockTruth => false;
        public bool HasStoryProgressMutation => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        private static IReadOnlyList<string> CopyOptionalText(IEnumerable<string> values, string paramName)
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

        private static IReadOnlyList<MissionShellMissionRowState> CopyMissionRows(IEnumerable<MissionShellMissionRowState> values)
        {
            var copied = new List<MissionShellMissionRowState>();
            if (values == null)
            {
                return copied.AsReadOnly();
            }

            foreach (var value in values)
            {
                if (value == null)
                {
                    throw new ArgumentException("Mission row cannot be null.", nameof(values));
                }

                copied.Add(value);
            }

            return copied.AsReadOnly();
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
