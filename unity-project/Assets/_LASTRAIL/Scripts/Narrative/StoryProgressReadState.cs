using System;
using System.Collections.Generic;
using LastRail.Localization;

namespace LastRail.Narrative
{
    public sealed class StoryProgressReadState
    {
        private const string NoneProgressId = "none";

        private StoryProgressReadState(
            string actId,
            string highestStoryProgressId,
            string highestClearedOperationId,
            int order,
            IReadOnlyList<string> clearedOperationIds,
            IReadOnlyList<string> unlockedOperations,
            string narrativeVersion,
            string contentStatus,
            string localizationStatus,
            string fixtureStatus,
            string missingContentStatus)
        {
            ActId = actId;
            HighestStoryProgressId = highestStoryProgressId;
            HighestStoryProgressLabel = UnityDisplayLabels.ForActOneStoryProgress(highestStoryProgressId);
            HighestClearedOperationId = highestClearedOperationId;
            Order = order;
            ClearedOperationIds = clearedOperationIds;
            UnlockedOperations = unlockedOperations;
            NarrativeVersion = narrativeVersion;
            ContentStatus = contentStatus;
            LocalizationStatus = localizationStatus;
            FixtureStatus = fixtureStatus;
            MissingContentStatus = missingContentStatus;
        }

        public string ActId { get; }
        public string HighestStoryProgressId { get; }
        public string HighestStoryProgressLabel { get; }
        public string HighestClearedOperationId { get; }
        public int Order { get; }
        public IReadOnlyList<string> ClearedOperationIds { get; }
        public IReadOnlyList<string> UnlockedOperations { get; }
        public string NarrativeVersion { get; }
        public string ContentStatus { get; }
        public string LocalizationStatus { get; }
        public string FixtureStatus { get; }
        public string MissingContentStatus { get; }

        public static StoryProgressReadState FromResponse(StoryProgressReadResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            var progressId = string.IsNullOrWhiteSpace(response.HighestStoryProgressId)
                ? NoneProgressId
                : response.HighestStoryProgressId.Trim();

            return new StoryProgressReadState(
                response.ActId,
                progressId,
                response.HighestClearedOperationId,
                response.Order,
                Copy(response.ClearedOperationIds),
                Copy(response.UnlockedOperations),
                response.NarrativeVersion,
                response.ContentStatus,
                response.LocalizationStatus,
                response.FixtureStatus,
                response.MissingContentStatus);
        }

        private static IReadOnlyList<string> Copy(IReadOnlyList<string> values)
        {
            return new List<string>(values).AsReadOnly();
        }
    }
}
