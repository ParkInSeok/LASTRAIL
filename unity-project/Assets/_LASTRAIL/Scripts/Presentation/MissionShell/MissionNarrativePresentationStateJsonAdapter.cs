using System;
using System.Globalization;
using System.Text;

namespace LastRail.Presentation.MissionShell
{
    public static class MissionNarrativePresentationStateJsonAdapter
    {
        public static string ToJson(MissionNarrativePresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "surface", state.Surface.ToString()).Append(',');
            AppendString(builder, "missionId", state.MissionId).Append(',');
            AppendString(builder, "displayName", state.DisplayName).Append(',');
            AppendString(builder, "narrativeVersion", state.NarrativeVersion).Append(',');
            AppendBool(builder, "isServerListedUnlockedForDisplay", state.IsServerListedUnlockedForDisplay).Append(',');
            AppendBool(builder, "isServerListedClearedForDisplay", state.IsServerListedClearedForDisplay).Append(',');
            AppendBool(builder, "hasNarrativeDefinition", state.HasNarrativeDefinition).Append(',');
            AppendString(builder, "localizationStatus", state.LocalizationStatus).Append(',');
            AppendInt(builder, "lineCount", state.LineCount).Append(',');
            AppendLines(builder, state).Append(',');
            AppendBool(builder, "isDisplayOnly", state.IsDisplayOnly).Append(',');
            AppendBool(builder, "hasUnlockTruth", state.HasUnlockTruth).Append(',');
            AppendBool(builder, "hasStoryProgressMutation", state.HasStoryProgressMutation).Append(',');
            AppendBool(builder, "hasRewardCalculation", state.HasRewardCalculation);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendLines(StringBuilder builder, MissionNarrativePresentationState state)
        {
            builder.Append("\"lines\":[");
            for (var i = 0; i < state.Lines.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append('{');
                AppendString(builder, "stringId", state.Lines[i].StringId).Append(',');
                AppendBool(builder, "isFallbackText", state.Lines[i].IsFallbackText);
                builder.Append('}');
            }

            return builder.Append(']');
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendInt(StringBuilder builder, string fieldName, int value)
        {
            return builder.Append('"')
                .Append(fieldName)
                .Append("\":")
                .Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static StringBuilder AppendBool(StringBuilder builder, string fieldName, bool value)
        {
            return builder.Append('"').Append(fieldName).Append("\":").Append(value ? "true" : "false");
        }

        private static StringBuilder AppendJsonString(StringBuilder builder, string value)
        {
            return builder.Append('"').Append(Escape(value)).Append('"');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
