using System;
using System.Globalization;
using System.Text;

namespace LastRail.Presentation.MissionShell
{
    public static class CodexArchivePresentationStateJsonAdapter
    {
        public static string ToJson(CodexArchivePresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "narrativeVersion", state.NarrativeVersion).Append(',');
            AppendInt(builder, "entryCount", state.EntryCount).Append(',');
            builder.Append("\"entries\":[");
            for (var i = 0; i < state.Entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendEntry(builder, state.Entries[i]);
            }

            builder.Append("],");
            AppendBool(builder, "isDisplayOnly", state.IsDisplayOnly).Append(',');
            AppendBool(builder, "hasUnlockTruth", state.HasUnlockTruth).Append(',');
            AppendBool(builder, "hasStoryProgressMutation", state.HasStoryProgressMutation).Append(',');
            AppendBool(builder, "hasRewardCalculation", state.HasRewardCalculation);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendEntry(StringBuilder builder, CodexArchiveEntryPresentationState entry)
        {
            builder.Append('{');
            AppendString(builder, "missionId", entry.MissionId).Append(',');
            AppendString(builder, "displayName", entry.DisplayName).Append(',');
            AppendString(builder, "codexStringId", entry.CodexStringId).Append(',');
            AppendBool(builder, "isFallbackText", entry.IsFallbackText).Append(',');
            AppendBool(builder, "isServerListedClearedForDisplay", entry.IsServerListedClearedForDisplay).Append(',');
            AppendBool(builder, "isServerListedUnlockedForDisplay", entry.IsServerListedUnlockedForDisplay).Append(',');
            AppendBool(builder, "canOpenForDisplay", entry.CanOpenForDisplay);
            builder.Append('}');
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
