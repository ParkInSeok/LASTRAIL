using System;
using System.Collections.Generic;
using LastRail.Narrative;

namespace LastRail.Presentation.MissionShell
{
    public sealed class CodexArchivePresentationState
    {
        private CodexArchivePresentationState(
            string narrativeVersion,
            IReadOnlyList<CodexArchiveEntryPresentationState> entries)
        {
            NarrativeVersion = RequireText(narrativeVersion, nameof(narrativeVersion));
            Entries = entries;
        }

        public string NarrativeVersion { get; }
        public IReadOnlyList<CodexArchiveEntryPresentationState> Entries { get; }
        public int EntryCount => Entries.Count;
        public bool IsDisplayOnly => true;
        public bool HasUnlockTruth => false;
        public bool HasStoryProgressMutation => false;
        public bool HasRewardCalculation => false;

        public static CodexArchivePresentationState CreateActOne(
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

            var entries = new List<CodexArchiveEntryPresentationState>();
            foreach (var definition in narrativeCatalog.Definitions)
            {
                var stringId = MissionNarrativeStringIds.ForCodexEntry(definition.MissionId);
                var hasLocalized = localizedTextByStringId.TryGetValue(stringId, out var localizedText) &&
                    !string.IsNullOrWhiteSpace(localizedText);
                var isCleared = Contains(storyProgress.ClearedOperationIds, definition.MissionId);
                var isUnlocked = Contains(storyProgress.UnlockedOperations, definition.MissionId);
                entries.Add(new CodexArchiveEntryPresentationState(
                    definition.MissionId,
                    definition.DisplayName,
                    stringId,
                    hasLocalized ? localizedText : definition.CodexEntry,
                    !hasLocalized,
                    isCleared,
                    isUnlocked,
                    definition.HasCodexEntry && (isCleared || isUnlocked)));
            }

            return new CodexArchivePresentationState(storyProgress.NarrativeVersion, entries.AsReadOnly());
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
