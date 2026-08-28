using System;
using System.Text;

namespace LastRail.Presentation.MissionShell
{
    public static class ActOneMissionShellStateJsonAdapter
    {
        public static string ToJson(ActOneMissionShellState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "surface", state.Surface.ToString()).Append(',');
            AppendOptionalString(builder, "missionId", state.MissionId).Append(',');
            AppendOptionalString(builder, "encounterType", state.EncounterType.HasValue ? state.EncounterType.Value.ToString() : null).Append(',');
            AppendString(builder, "displayLabel", state.DisplayLabel).Append(',');
            AppendBool(builder, "isServerListedUnlockedForDisplay", state.IsServerListedUnlockedForDisplay).Append(',');
            AppendString(builder, "narrativeVersion", state.NarrativeVersion).Append(',');
            AppendBool(builder, "hasNarrativeDefinition", state.HasNarrativeDefinition).Append(',');
            AppendOptionalString(builder, "missionStartButtonCopy", state.MissionStartButtonCopy).Append(',');
            AppendOptionalString(builder, "missionStartStringId", state.MissionStartStringId).Append(',');
            AppendOptionalString(builder, "loadingHintStringId", state.LoadingHintStringId).Append(',');
            AppendBool(builder, "hasCodexEntry", state.HasCodexEntry).Append(',');
            AppendOptionalString(builder, "codexEntryStringId", state.CodexEntryStringId).Append(',');
            AppendString(builder, "localizationStatus", state.LocalizationStatus).Append(',');
            AppendString(builder, "missingLocalizationState", state.MissingLocalizationState).Append(',');
            AppendBool(builder, "isDisplayOnly", state.IsDisplayOnly).Append(',');
            AppendBool(builder, "hasUnlockTruth", state.HasUnlockTruth).Append(',');
            AppendBool(builder, "hasRewardCalculation", state.HasRewardCalculation);
            builder.Append('}');
            return builder.ToString();
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
