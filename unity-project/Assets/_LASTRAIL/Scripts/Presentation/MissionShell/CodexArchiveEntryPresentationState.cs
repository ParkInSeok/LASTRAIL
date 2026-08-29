using System;

namespace LastRail.Presentation.MissionShell
{
    public sealed class CodexArchiveEntryPresentationState
    {
        public CodexArchiveEntryPresentationState(
            string missionId,
            string displayName,
            string codexStringId,
            string codexEntryText,
            bool isFallbackText,
            bool isServerListedClearedForDisplay,
            bool isServerListedUnlockedForDisplay,
            bool canOpenForDisplay)
        {
            MissionId = RequireText(missionId, nameof(missionId));
            DisplayName = RequireText(displayName, nameof(displayName));
            CodexStringId = RequireText(codexStringId, nameof(codexStringId));
            CodexEntryText = RequireText(codexEntryText, nameof(codexEntryText));
            IsFallbackText = isFallbackText;
            IsServerListedClearedForDisplay = isServerListedClearedForDisplay;
            IsServerListedUnlockedForDisplay = isServerListedUnlockedForDisplay;
            CanOpenForDisplay = canOpenForDisplay;
        }

        public string MissionId { get; }
        public string DisplayName { get; }
        public string CodexStringId { get; }
        public string CodexEntryText { get; }
        public bool IsFallbackText { get; }
        public bool IsServerListedClearedForDisplay { get; }
        public bool IsServerListedUnlockedForDisplay { get; }
        public bool CanOpenForDisplay { get; }

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
