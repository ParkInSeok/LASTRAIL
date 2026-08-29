using System;

namespace LastRail.Narrative
{
    public sealed class NarrativeLocalizationReadinessSlot
    {
        public NarrativeLocalizationReadinessSlot(
            string slotKind,
            string stringId,
            bool isAvailable,
            int characterCount,
            int maxCharacterCount)
        {
            SlotKind = RequireText(slotKind, nameof(slotKind));
            StringId = RequireText(stringId, nameof(stringId));
            IsAvailable = isAvailable;
            if (characterCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(characterCount), characterCount, "Character count cannot be negative.");
            }

            if (maxCharacterCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCharacterCount), maxCharacterCount, "Max character count must be positive.");
            }

            CharacterCount = characterCount;
            MaxCharacterCount = maxCharacterCount;
        }

        public string SlotKind { get; }
        public string StringId { get; }
        public bool IsAvailable { get; }
        public int CharacterCount { get; }
        public int MaxCharacterCount { get; }
        public bool ExceedsRecommendedLength => CharacterCount > MaxCharacterCount;
        public bool HasLocalizedFullTextPayload => false;

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
