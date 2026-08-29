using System;
using System.Collections.Generic;

namespace LastRail.Presentation.MissionShell
{
    public sealed class CodexArchiveScreenViewState
    {
        public const int LaunchMinimumTouchTargetDp = 48;
        public const int PortraitAspectWidth = 9;
        public const int PortraitAspectHeight = 16;

        public CodexArchiveScreenViewState(
            string title,
            string subtitle,
            string narrativeVersion,
            IEnumerable<CodexArchiveScreenEntryViewState> entries,
            int overflowSlotCount,
            bool canNavigateToMissionSelect,
            bool canNavigateToResult,
            bool canNavigateToRewardReview)
        {
            Title = RequireText(title, nameof(title));
            Subtitle = OptionalText(subtitle);
            NarrativeVersion = RequireText(narrativeVersion, nameof(narrativeVersion));
            Entries = CopyEntries(entries);
            OverflowSlotCount = RequireNonNegative(overflowSlotCount, nameof(overflowSlotCount));
            CanNavigateToMissionSelect = canNavigateToMissionSelect;
            CanNavigateToResult = canNavigateToResult;
            CanNavigateToRewardReview = canNavigateToRewardReview;
        }

        public MissionShellSurface Surface => MissionShellSurface.CodexArchive;
        public string Title { get; }
        public string Subtitle { get; }
        public string NarrativeVersion { get; }
        public IReadOnlyList<CodexArchiveScreenEntryViewState> Entries { get; }
        public int EntryCount => Entries.Count;
        public int OverflowSlotCount { get; }
        public bool RequiresOverflowVerification => true;
        public bool IsBlockingNavigation => false;
        public bool CanNavigateToMissionSelect { get; }
        public bool CanNavigateToResult { get; }
        public bool CanNavigateToRewardReview { get; }
        public bool UsesPortraitSafeArea => true;
        public int MinimumTouchTargetDp => LaunchMinimumTouchTargetDp;
        public int PreferredAspectWidth => PortraitAspectWidth;
        public int PreferredAspectHeight => PortraitAspectHeight;
        public bool HasReadableTextSlots => true;
        public bool IsDisplayOnly => true;
        public bool HasServerTransport => false;
        public bool HasUnlockTruth => false;
        public bool HasStoryProgressMutation => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        private static IReadOnlyList<CodexArchiveScreenEntryViewState> CopyEntries(IEnumerable<CodexArchiveScreenEntryViewState> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var copied = new List<CodexArchiveScreenEntryViewState>();
            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    throw new ArgumentException("Archive entry cannot be null.", nameof(entries));
                }

                copied.Add(entry);
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
