using System;
using LastRail.Localization;
using LastRail.Narrative;
using LastRail.Run;

namespace LastRail.Presentation.MissionShell
{
    public sealed class ActOneMissionShellState
    {
        private const string MissingLocalFixture = "LOCAL_FIXTURE_MISSING";
        private const string NoMissingLocalization = "NONE";

        private ActOneMissionShellState(
            MissionShellSurface surface,
            string missionId,
            RunEncounterType? encounterType,
            string displayLabel,
            bool isServerListedUnlockedForDisplay,
            string narrativeVersion,
            bool hasNarrativeDefinition,
            string missionStartButtonCopy,
            string missionStartStringId,
            string loadingHintStringId,
            bool hasCodexEntry,
            string codexEntryStringId,
            string localizationStatus,
            string missingLocalizationState)
        {
            Surface = surface;
            MissionId = missionId;
            EncounterType = encounterType;
            DisplayLabel = RequireText(displayLabel, nameof(displayLabel));
            IsServerListedUnlockedForDisplay = isServerListedUnlockedForDisplay;
            NarrativeVersion = RequireText(narrativeVersion, nameof(narrativeVersion));
            HasNarrativeDefinition = hasNarrativeDefinition;
            MissionStartButtonCopy = missionStartButtonCopy;
            MissionStartStringId = missionStartStringId;
            LoadingHintStringId = loadingHintStringId;
            HasCodexEntry = hasCodexEntry;
            CodexEntryStringId = codexEntryStringId;
            LocalizationStatus = RequireText(localizationStatus, nameof(localizationStatus));
            MissingLocalizationState = RequireText(missingLocalizationState, nameof(missingLocalizationState));
        }

        public MissionShellSurface Surface { get; }
        public string MissionId { get; }
        public RunEncounterType? EncounterType { get; }
        public string DisplayLabel { get; }
        public bool IsServerListedUnlockedForDisplay { get; }
        public string NarrativeVersion { get; }
        public bool HasNarrativeDefinition { get; }
        public string MissionStartButtonCopy { get; }
        public string MissionStartStringId { get; }
        public string LoadingHintStringId { get; }
        public bool HasCodexEntry { get; }
        public string CodexEntryStringId { get; }
        public string LocalizationStatus { get; }
        public string MissingLocalizationState { get; }
        public bool IsDisplayOnly => true;
        public bool HasUnlockTruth => false;
        public bool HasRewardCalculation => false;

        public static ActOneMissionShellState CreateGlobalSurface(
            MissionShellSurface surface,
            StoryProgressReadState storyProgress)
        {
            if (storyProgress == null)
            {
                throw new ArgumentNullException(nameof(storyProgress));
            }

            if (!IsGlobalSurface(surface))
            {
                throw new ArgumentException("Surface is not a global mission shell surface.", nameof(surface));
            }

            return new ActOneMissionShellState(
                surface,
                null,
                null,
                GlobalDisplayLabel(surface),
                false,
                storyProgress.NarrativeVersion,
                false,
                null,
                null,
                null,
                false,
                null,
                storyProgress.LocalizationStatus,
                NoMissingLocalization);
        }

        public static ActOneMissionShellState CreateMissionSurface(
            MissionShellSurface surface,
            string missionId,
            RunEncounterType encounterType,
            StoryProgressReadState storyProgress,
            MissionNarrativeCatalog narrativeCatalog)
        {
            if (!IsMissionSurface(surface))
            {
                throw new ArgumentException("Surface is not a mission-specific shell surface.", nameof(surface));
            }

            return CreateMissionState(surface, missionId, encounterType, storyProgress, narrativeCatalog);
        }

        public static ActOneMissionShellState CreateBattleSurface(
            string missionId,
            RunEncounterType encounterType,
            StoryProgressReadState storyProgress,
            MissionNarrativeCatalog narrativeCatalog)
        {
            var surface = encounterType == RunEncounterType.RaidBoss
                ? MissionShellSurface.BattleRaidBoss
                : MissionShellSurface.BattleOrdinary15Wave;

            return CreateMissionState(surface, missionId, encounterType, storyProgress, narrativeCatalog);
        }

        private static ActOneMissionShellState CreateMissionState(
            MissionShellSurface surface,
            string missionId,
            RunEncounterType encounterType,
            StoryProgressReadState storyProgress,
            MissionNarrativeCatalog narrativeCatalog)
        {
            if (storyProgress == null)
            {
                throw new ArgumentNullException(nameof(storyProgress));
            }

            if (narrativeCatalog == null)
            {
                throw new ArgumentNullException(nameof(narrativeCatalog));
            }

            var normalizedMissionId = RequireText(missionId, nameof(missionId));
            var definition = narrativeCatalog.FindByMissionId(normalizedMissionId);

            return new ActOneMissionShellState(
                surface,
                normalizedMissionId,
                encounterType,
                definition == null ? UnityDisplayLabels.ForActOneStoryProgress(normalizedMissionId) : definition.DisplayName,
                IsListedByServer(storyProgress, normalizedMissionId),
                storyProgress.NarrativeVersion,
                definition != null,
                definition == null ? null : definition.MissionStartButtonCopy,
                MissionNarrativeStringIds.ForMissionStart(normalizedMissionId),
                MissionNarrativeStringIds.ForLoadingHint(normalizedMissionId),
                definition != null && definition.HasCodexEntry,
                MissionNarrativeStringIds.ForCodexEntry(normalizedMissionId),
                storyProgress.LocalizationStatus,
                definition == null ? MissingLocalFixture : storyProgress.MissingContentStatus);
        }

        private static bool IsListedByServer(StoryProgressReadState storyProgress, string missionId)
        {
            foreach (var unlockedOperation in storyProgress.UnlockedOperations)
            {
                if (string.Equals(unlockedOperation, missionId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsGlobalSurface(MissionShellSurface surface)
        {
            return surface == MissionShellSurface.AuthGate ||
                surface == MissionShellSurface.MissionSelect ||
                surface == MissionShellSurface.OfflineReward ||
                surface == MissionShellSurface.StoryProgressSummary ||
                surface == MissionShellSurface.Gacha ||
                surface == MissionShellSurface.CodexArchive;
        }

        private static bool IsMissionSurface(MissionShellSurface surface)
        {
            return surface == MissionShellSurface.Briefing ||
                surface == MissionShellSurface.LoadingHint ||
                surface == MissionShellSurface.Result ||
                surface == MissionShellSurface.RewardReview;
        }

        private static string GlobalDisplayLabel(MissionShellSurface surface)
        {
            switch (surface)
            {
                case MissionShellSurface.AuthGate:
                    return "로그인";
                case MissionShellSurface.MissionSelect:
                    return "작전 선택";
                case MissionShellSurface.OfflineReward:
                    return "오프라인 보상";
                case MissionShellSurface.StoryProgressSummary:
                    return "스토리 진행도";
                case MissionShellSurface.Gacha:
                    return "요원 모집";
                case MissionShellSurface.CodexArchive:
                    return "기록 보관소";
                default:
                    throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unsupported global surface.");
            }
        }

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
