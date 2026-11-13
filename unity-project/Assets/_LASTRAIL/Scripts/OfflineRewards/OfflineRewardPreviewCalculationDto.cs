using System;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardPreviewCalculationDto
    {
        public OfflineRewardPreviewCalculationDto(
            string storyProgressId,
            string storyProgressMultiplierLabel,
            int cappedOfflineSeconds,
            string capSource)
        {
            StoryProgressId = Required(storyProgressId, nameof(storyProgressId));
            StoryProgressMultiplierLabel = Required(storyProgressMultiplierLabel, nameof(storyProgressMultiplierLabel));
            if (cappedOfflineSeconds < 0)
            {
                throw new ArgumentException("Capped offline seconds cannot be negative.", nameof(cappedOfflineSeconds));
            }

            CappedOfflineSeconds = cappedOfflineSeconds;
            CapSource = Required(capSource, nameof(capSource));
        }

        public string StoryProgressId { get; }
        public string StoryProgressMultiplierLabel { get; }
        public int CappedOfflineSeconds { get; }
        public string CapSource { get; }

        private static string Required(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
