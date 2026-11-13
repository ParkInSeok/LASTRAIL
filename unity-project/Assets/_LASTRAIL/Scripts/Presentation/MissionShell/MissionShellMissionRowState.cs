using System;
using LastRail.Run;

namespace LastRail.Presentation.MissionShell
{
    public sealed class MissionShellMissionRowState
    {
        public MissionShellMissionRowState(
            string missionId,
            string displayLabel,
            RunEncounterType encounterType,
            bool isServerListedUnlockedForDisplay,
            string missionStartStringId,
            string loadingHintStringId,
            bool hasCodexEntry)
        {
            MissionId = RequireText(missionId, nameof(missionId));
            DisplayLabel = RequireText(displayLabel, nameof(displayLabel));
            EncounterType = encounterType;
            IsServerListedUnlockedForDisplay = isServerListedUnlockedForDisplay;
            MissionStartStringId = RequireText(missionStartStringId, nameof(missionStartStringId));
            LoadingHintStringId = RequireText(loadingHintStringId, nameof(loadingHintStringId));
            HasCodexEntry = hasCodexEntry;
            MinimumTouchTargetDp = MissionShellScreenViewState.LaunchMinimumTouchTargetDp;
        }

        public string MissionId { get; }
        public string DisplayLabel { get; }
        public RunEncounterType EncounterType { get; }
        public bool IsServerListedUnlockedForDisplay { get; }
        public string MissionStartStringId { get; }
        public string LoadingHintStringId { get; }
        public bool HasCodexEntry { get; }
        public int MinimumTouchTargetDp { get; }
        public bool IsDisplayOnly => true;
        public bool HasUnlockTruth => false;

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
