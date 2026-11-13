using System;
using System.Collections.Generic;
using LastRail.Narrative;

namespace LastRail.Presentation.MissionShell
{
    public sealed class StoryProgressDisplaySection
    {
        private StoryProgressDisplaySection(
            string actId,
            string highestStoryProgressId,
            string highestStoryProgressLabel,
            string highestClearedOperationId,
            int order,
            IEnumerable<string> clearedOperationIds,
            IEnumerable<string> unlockedOperations,
            string narrativeVersion,
            string contentStatus,
            string localizationStatus,
            string fixtureStatus,
            string missingContentStatus)
        {
            ActId = OptionalText(actId);
            HighestStoryProgressId = OptionalText(highestStoryProgressId);
            HighestStoryProgressLabel = OptionalText(highestStoryProgressLabel);
            HighestClearedOperationId = OptionalText(highestClearedOperationId);
            Order = order;
            ClearedOperationIds = CopyStrings(clearedOperationIds, nameof(clearedOperationIds));
            UnlockedOperations = CopyStrings(unlockedOperations, nameof(unlockedOperations));
            NarrativeVersion = OptionalText(narrativeVersion);
            ContentStatus = OptionalText(contentStatus);
            LocalizationStatus = OptionalText(localizationStatus);
            FixtureStatus = OptionalText(fixtureStatus);
            MissingContentStatus = OptionalText(missingContentStatus);
        }

        public string ActId { get; }
        public string HighestStoryProgressId { get; }
        public string HighestStoryProgressLabel { get; }
        public string HighestClearedOperationId { get; }
        public int Order { get; }
        public IReadOnlyList<string> ClearedOperationIds { get; }
        public IReadOnlyList<string> UnlockedOperations { get; }
        public int UnlockedOperationCount => UnlockedOperations.Count;
        public string NarrativeVersion { get; }
        public string ContentStatus { get; }
        public string LocalizationStatus { get; }
        public string FixtureStatus { get; }
        public string MissingContentStatus { get; }
        public bool HasUnlockTruthCalculation => false;

        public static StoryProgressDisplaySection FromState(StoryProgressReadState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new StoryProgressDisplaySection(
                state.ActId,
                state.HighestStoryProgressId,
                state.HighestStoryProgressLabel,
                state.HighestClearedOperationId,
                state.Order,
                state.ClearedOperationIds,
                state.UnlockedOperations,
                state.NarrativeVersion,
                state.ContentStatus,
                state.LocalizationStatus,
                state.FixtureStatus,
                state.MissingContentStatus);
        }

        private static IReadOnlyList<string> CopyStrings(IEnumerable<string> values, string name)
        {
            if (values == null)
            {
                throw new ArgumentNullException(name);
            }

            var copy = new List<string>();
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Value cannot be empty.", name);
                }

                copy.Add(value.Trim());
            }

            return copy.AsReadOnly();
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
