using System;
using System.Globalization;
using System.Text;

namespace LastRail.Narrative
{
    public static class MissionNarrativeStringIdCoverageJsonAdapter
    {
        public static string ToJson(MissionNarrativeStringIdCoverageMap map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            builder.Append("\"missionCount\":").Append(map.MissionCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"totalStringIdCount\":").Append(map.AllStringIds.Count.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"totalMissingRequiredSurfaceCount\":").Append(map.TotalMissingRequiredSurfaceCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"hasLocalizedTextPayload\":false,");
            builder.Append("\"missions\":[");
            for (var i = 0; i < map.Coverages.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendCoverageObject(builder, map.Coverages[i]);
            }

            builder.Append(']');
            builder.Append('}');
            return builder.ToString();
        }

        public static string ToJson(MissionNarrativeStringIdCoverage coverage)
        {
            if (coverage == null)
            {
                throw new ArgumentNullException(nameof(coverage));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendCoverageObject(builder, coverage);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendCoverageObject(StringBuilder builder, MissionNarrativeStringIdCoverage coverage)
        {
            builder.Append('{');
            AppendString(builder, "missionId", coverage.MissionId).Append(',');
            AppendString(builder, "displayName", coverage.DisplayName).Append(',');
            builder.Append("\"entryCount\":").Append(coverage.EntryCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"missingRequiredSurfaceCount\":").Append(coverage.MissingRequiredSurfaceCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"hasLocalizedTextPayload\":false,");
            builder.Append("\"entries\":[");
            for (var i = 0; i < coverage.Entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendEntryObject(builder, coverage.Entries[i]);
            }

            builder.Append(']');
            builder.Append('}');
        }

        private static void AppendEntryObject(StringBuilder builder, MissionNarrativeStringIdEntry entry)
        {
            builder.Append('{');
            AppendString(builder, "surface", entry.Surface).Append(',');
            AppendString(builder, "slot", entry.Slot).Append(',');
            AppendString(builder, "stringId", entry.StringId).Append(',');
            builder.Append("\"hasLocalizedTextPayload\":false");
            builder.Append('}');
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
