using System;

namespace LastRail.Presentation.MissionShell
{
    public sealed class CodexArchiveScreenEntryViewState
    {
        public CodexArchiveScreenEntryViewState(
            string missionId,
            string displayName,
            string codexStringId,
            string displayText,
            bool isFallbackText,
            bool isServerListedClearedForDisplay,
            bool isServerListedUnlockedForDisplay,
            bool canOpenForDisplay,
            int characterCount,
            int maxCharacterCount,
            bool exceedsRecommendedLength,
            string readinessStatus)
        {
            MissionId = RequireText(missionId, nameof(missionId));
            DisplayName = RequireText(displayName, nameof(displayName));
            CodexStringId = RequireText(codexStringId, nameof(codexStringId));
            DisplayText = RequireText(displayText, nameof(displayText));
            IsFallbackText = isFallbackText;
            IsServerListedClearedForDisplay = isServerListedClearedForDisplay;
            IsServerListedUnlockedForDisplay = isServerListedUnlockedForDisplay;
            CanOpenForDisplay = canOpenForDisplay;
            CharacterCount = RequireNonNegative(characterCount, nameof(characterCount));
            MaxCharacterCount = maxCharacterCount <= 0
                ? throw new ArgumentOutOfRangeException(nameof(maxCharacterCount), maxCharacterCount, "Max character count must be positive.")
                : maxCharacterCount;
            ExceedsRecommendedLength = exceedsRecommendedLength;
            ReadinessStatus = RequireText(readinessStatus, nameof(readinessStatus));
        }

        public string MissionId { get; }
        public string DisplayName { get; }
        public string CodexStringId { get; }
        public string DisplayText { get; }
        public bool IsFallbackText { get; }
        public bool IsServerListedClearedForDisplay { get; }
        public bool IsServerListedUnlockedForDisplay { get; }
        public bool CanOpenForDisplay { get; }
        public int CharacterCount { get; }
        public int MaxCharacterCount { get; }
        public bool ExceedsRecommendedLength { get; }
        public string ReadinessStatus { get; }

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
    }
}
