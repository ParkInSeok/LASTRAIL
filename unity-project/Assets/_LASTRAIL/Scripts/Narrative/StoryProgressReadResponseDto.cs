using System;
using System.Collections.Generic;

namespace LastRail.Narrative
{
    public sealed class StoryProgressReadResponseDto
    {
        private readonly IReadOnlyList<string> clearedOperationIds;
        private readonly IReadOnlyList<string> unlockedOperations;

        public StoryProgressReadResponseDto(
            string actId,
            string highestStoryProgressId,
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
            ActId = Required(actId, nameof(actId));
            HighestStoryProgressId = OptionalTrimmed(highestStoryProgressId);
            HighestClearedOperationId = OptionalTrimmed(highestClearedOperationId);
            Order = order;
            this.clearedOperationIds = CopyOptionalIds(clearedOperationIds, nameof(clearedOperationIds));
            this.unlockedOperations = CopyOptionalIds(unlockedOperations, nameof(unlockedOperations));
            NarrativeVersion = Required(narrativeVersion, nameof(narrativeVersion));
            ContentStatus = Required(contentStatus, nameof(contentStatus));
            LocalizationStatus = Required(localizationStatus, nameof(localizationStatus));
            FixtureStatus = Required(fixtureStatus, nameof(fixtureStatus));
            MissingContentStatus = Required(missingContentStatus, nameof(missingContentStatus));
        }

        public string ActId { get; }
        public string HighestStoryProgressId { get; }
        public string HighestClearedOperationId { get; }
        public int Order { get; }
        public IReadOnlyList<string> ClearedOperationIds => clearedOperationIds;
        public IReadOnlyList<string> UnlockedOperations => unlockedOperations;
        public string NarrativeVersion { get; }
        public string ContentStatus { get; }
        public string LocalizationStatus { get; }
        public string FixtureStatus { get; }
        public string MissingContentStatus { get; }

        private static IReadOnlyList<string> CopyOptionalIds(IEnumerable<string> values, string paramName)
        {
            var copied = new List<string>();
            if (values == null)
            {
                return copied.AsReadOnly();
            }

            foreach (var value in values)
            {
                copied.Add(Required(value, paramName));
            }

            return copied.AsReadOnly();
        }

        private static string Required(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }

        private static string OptionalTrimmed(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
