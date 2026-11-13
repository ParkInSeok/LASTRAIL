using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LastRail.Narrative
{
    public static class StoryProgressReadStateJsonAdapter
    {
        public static string ToJson(StoryProgressReadState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "actId", state.ActId).Append(',');
            AppendString(builder, "highestStoryProgressId", state.HighestStoryProgressId).Append(',');
            AppendString(builder, "highestStoryProgressLabel", state.HighestStoryProgressLabel).Append(',');
            AppendOptionalString(builder, "highestClearedOperationId", state.HighestClearedOperationId).Append(',');
            builder.Append("\"order\":").Append(state.Order.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendStringArray(builder, "clearedOperationIds", state.ClearedOperationIds).Append(',');
            AppendStringArray(builder, "unlockedOperations", state.UnlockedOperations).Append(',');
            AppendString(builder, "narrativeVersion", state.NarrativeVersion).Append(',');
            AppendString(builder, "contentStatus", state.ContentStatus).Append(',');
            AppendString(builder, "localizationStatus", state.LocalizationStatus).Append(',');
            AppendString(builder, "fixtureStatus", state.FixtureStatus).Append(',');
            AppendString(builder, "missingContentStatus", state.MissingContentStatus).Append(',');
            builder.Append("\"isDisplayOnly\":true,");
            builder.Append("\"hasRewardCalculation\":false,");
            builder.Append("\"hasUnlockTruth\":false");
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendStringArray(StringBuilder builder, string fieldName, IReadOnlyList<string> values)
        {
            builder.Append('"').Append(fieldName).Append("\":[");
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendJsonString(builder, values[i]);
            }

            return builder.Append(']');
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendOptionalString(StringBuilder builder, string fieldName, string value)
        {
            if (value == null)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendString(builder, fieldName, value);
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
