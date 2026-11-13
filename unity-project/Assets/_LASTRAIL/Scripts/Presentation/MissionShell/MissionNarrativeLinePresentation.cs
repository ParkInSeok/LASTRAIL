using System;

namespace LastRail.Presentation.MissionShell
{
    public sealed class MissionNarrativeLinePresentation
    {
        public MissionNarrativeLinePresentation(string stringId, string text, bool isFallbackText)
        {
            StringId = RequireText(stringId, nameof(stringId));
            Text = RequireText(text, nameof(text));
            IsFallbackText = isFallbackText;
        }

        public string StringId { get; }
        public string Text { get; }
        public bool IsFallbackText { get; }

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
