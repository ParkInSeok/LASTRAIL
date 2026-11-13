using System;
using System.Collections.Generic;
using LastRail.Localization;
using LastRail.Narrative;

namespace LastRail.Presentation.MissionShell
{
    public sealed class MissionNarrativePresentationState
    {
        private const string Localized = "LOCALIZED";
        private const string MissingFallback = "MISSING_FALLBACK";

        private MissionNarrativePresentationState(
            MissionNarrativeSurfaceKind surface,
            string missionId,
            string displayName,
            string narrativeVersion,
            bool isServerListedUnlockedForDisplay,
            bool isServerListedClearedForDisplay,
            bool hasNarrativeDefinition,
            IReadOnlyList<MissionNarrativeLinePresentation> lines,
            string localizationStatus)
        {
            Surface = surface;
            MissionId = RequireText(missionId, nameof(missionId));
            DisplayName = RequireText(displayName, nameof(displayName));
            NarrativeVersion = RequireText(narrativeVersion, nameof(narrativeVersion));
            IsServerListedUnlockedForDisplay = isServerListedUnlockedForDisplay;
            IsServerListedClearedForDisplay = isServerListedClearedForDisplay;
            HasNarrativeDefinition = hasNarrativeDefinition;
            Lines = lines;
            LocalizationStatus = RequireText(localizationStatus, nameof(localizationStatus));
        }

        public MissionNarrativeSurfaceKind Surface { get; }
        public string MissionId { get; }
        public string DisplayName { get; }
        public string NarrativeVersion { get; }
        public bool IsServerListedUnlockedForDisplay { get; }
        public bool IsServerListedClearedForDisplay { get; }
        public bool HasNarrativeDefinition { get; }
        public IReadOnlyList<MissionNarrativeLinePresentation> Lines { get; }
        public string LocalizationStatus { get; }
        public int LineCount => Lines.Count;
        public bool IsDisplayOnly => true;
        public bool HasUnlockTruth => false;
        public bool HasStoryProgressMutation => false;
        public bool HasRewardCalculation => false;

        public static MissionNarrativePresentationState Create(
            MissionNarrativeSurfaceKind surface,
            string missionId,
            StoryProgressReadState storyProgress,
            MissionNarrativeCatalog narrativeCatalog,
            IReadOnlyDictionary<string, string> localizedTextByStringId)
        {
            if (storyProgress == null)
            {
                throw new ArgumentNullException(nameof(storyProgress));
            }

            if (narrativeCatalog == null)
            {
                throw new ArgumentNullException(nameof(narrativeCatalog));
            }

            if (localizedTextByStringId == null)
            {
                throw new ArgumentNullException(nameof(localizedTextByStringId));
            }

            var normalizedMissionId = RequireText(missionId, nameof(missionId));
            var definition = narrativeCatalog.FindByMissionId(normalizedMissionId);
            var lines = BuildLines(surface, normalizedMissionId, definition, localizedTextByStringId);
            return new MissionNarrativePresentationState(
                surface,
                normalizedMissionId,
                definition == null ? UnityDisplayLabels.ForActOneStoryProgress(normalizedMissionId) : definition.DisplayName,
                storyProgress.NarrativeVersion,
                Contains(storyProgress.UnlockedOperations, normalizedMissionId),
                Contains(storyProgress.ClearedOperationIds, normalizedMissionId),
                definition != null,
                lines,
                HasFallback(lines) ? MissingFallback : Localized);
        }

        private static IReadOnlyList<MissionNarrativeLinePresentation> BuildLines(
            MissionNarrativeSurfaceKind surface,
            string missionId,
            MissionNarrativeDefinition definition,
            IReadOnlyDictionary<string, string> localizedTextByStringId)
        {
            var lines = new List<MissionNarrativeLinePresentation>();
            switch (surface)
            {
                case MissionNarrativeSurfaceKind.MissionSelect:
                    Add(lines, MissionNarrativeStringIds.ForMissionStart(missionId), definition == null ? missionId : definition.MissionStartButtonCopy, localizedTextByStringId);
                    break;
                case MissionNarrativeSurfaceKind.Briefing:
                    if (definition == null)
                    {
                        Add(lines, MissionNarrativeStringIds.ForBriefingLine(missionId, 1), missionId, localizedTextByStringId);
                    }
                    else
                    {
                        for (var i = 0; i < definition.BriefingLines.Count; i++)
                        {
                            Add(lines, MissionNarrativeStringIds.ForBriefingLine(missionId, i + 1), definition.BriefingLines[i], localizedTextByStringId);
                        }
                    }

                    Add(lines, MissionNarrativeStringIds.ForMissionStart(missionId), definition == null ? missionId : definition.MissionStartButtonCopy, localizedTextByStringId);
                    break;
                case MissionNarrativeSurfaceKind.LoadingHint:
                    Add(lines, MissionNarrativeStringIds.ForLoadingHint(missionId), definition == null ? missionId : definition.LoadingHint, localizedTextByStringId);
                    break;
                case MissionNarrativeSurfaceKind.Result:
                    Add(lines, MissionNarrativeStringIds.Build(missionId, "result", "title"), definition == null ? missionId : definition.ResultTitle, localizedTextByStringId);
                    Add(lines, MissionNarrativeStringIds.Build(missionId, "result", "summary"), definition == null ? missionId : definition.ResultSummary, localizedTextByStringId);
                    break;
                case MissionNarrativeSurfaceKind.CodexArchive:
                    Add(lines, MissionNarrativeStringIds.ForCodexEntry(missionId), definition == null || !definition.HasCodexEntry ? missionId : definition.CodexEntry, localizedTextByStringId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unsupported narrative surface.");
            }

            return lines.AsReadOnly();
        }

        private static void Add(
            List<MissionNarrativeLinePresentation> lines,
            string stringId,
            string fallbackText,
            IReadOnlyDictionary<string, string> localizedTextByStringId)
        {
            if (localizedTextByStringId.TryGetValue(stringId, out var localizedText) &&
                !string.IsNullOrWhiteSpace(localizedText))
            {
                lines.Add(new MissionNarrativeLinePresentation(stringId, localizedText, false));
                return;
            }

            lines.Add(new MissionNarrativeLinePresentation(stringId, fallbackText, true));
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFallback(IReadOnlyList<MissionNarrativeLinePresentation> lines)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].IsFallbackText)
                {
                    return true;
                }
            }

            return false;
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
