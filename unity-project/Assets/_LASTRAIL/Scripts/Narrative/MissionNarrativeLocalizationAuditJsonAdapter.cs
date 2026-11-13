using System;
using System.Globalization;
using System.Text;

namespace LastRail.Narrative
{
    public static class MissionNarrativeLocalizationAuditJsonAdapter
    {
        public static string ToJson(MissionNarrativeLocalizationAudit audit)
        {
            if (audit == null)
            {
                throw new ArgumentNullException(nameof(audit));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "status", audit.Status.ToString()).Append(',');
            builder.Append("\"missionCount\":").Append(audit.MissionCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"requiredStringIdCount\":").Append(audit.RequiredStringIdCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"availableStringIdCount\":").Append(audit.AvailableStringIdCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"missingStringIdCount\":").Append(audit.MissingStringIdCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"unknownAvailableStringIdCount\":").Append(audit.UnknownAvailableStringIdCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"hasLocalizedTextPayload\":false,");
            builder.Append("\"missions\":[");
            for (var i = 0; i < audit.Missions.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendMission(builder, audit.Missions[i]);
            }

            builder.Append(']');
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendMission(StringBuilder builder, MissionNarrativeLocalizationMissionAudit mission)
        {
            builder.Append('{');
            AppendString(builder, "missionId", mission.MissionId).Append(',');
            AppendString(builder, "displayName", mission.DisplayName).Append(',');
            AppendString(builder, "status", mission.Status.ToString()).Append(',');
            builder.Append("\"requiredStringIdCount\":").Append(mission.RequiredStringIdCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"availableStringIdCount\":").Append(mission.AvailableStringIdCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"missingStringIdCount\":").Append(mission.MissingStringIdCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"hasLocalizedTextPayload\":false,");
            AppendStringArray(builder, "requiredStringIds", mission.RequiredStringIds).Append(',');
            AppendStringArray(builder, "availableStringIds", mission.AvailableStringIds).Append(',');
            AppendStringArray(builder, "missingStringIds", mission.MissingStringIds);
            builder.Append('}');
        }

        private static StringBuilder AppendStringArray(StringBuilder builder, string fieldName, System.Collections.Generic.IReadOnlyList<string> values)
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
