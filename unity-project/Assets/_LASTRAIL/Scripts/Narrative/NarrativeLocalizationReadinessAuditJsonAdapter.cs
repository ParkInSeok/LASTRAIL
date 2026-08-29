using System;
using System.Globalization;
using System.Text;

namespace LastRail.Narrative
{
    public static class NarrativeLocalizationReadinessAuditJsonAdapter
    {
        public static string ToJson(NarrativeLocalizationReadinessAudit audit)
        {
            if (audit == null)
            {
                throw new ArgumentNullException(nameof(audit));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendInt(builder, "requiredStringIdCount", audit.RequiredStringIdCount).Append(',');
            AppendInt(builder, "operatorDisplayNameStringIdCount", audit.OperatorDisplayNameStringIdCount).Append(',');
            AppendInt(builder, "missingStringIdCount", audit.MissingStringIdCount).Append(',');
            AppendInt(builder, "overflowSlotCount", audit.OverflowSlotCount).Append(',');
            AppendString(builder, "raidFailureRefundToastStringId", audit.RaidFailureRefundToastStringId).Append(',');
            builder.Append("\"slots\":[");
            for (var i = 0; i < audit.Slots.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendSlot(builder, audit.Slots[i]);
            }

            builder.Append("],");
            AppendBool(builder, "hasLocalizedFullTextPayload", audit.HasLocalizedFullTextPayload).Append(',');
            AppendBool(builder, "hasServerTruthPayload", audit.HasServerTruthPayload).Append(',');
            AppendBool(builder, "hasRewardCalculation", audit.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", audit.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", audit.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendSlot(StringBuilder builder, NarrativeLocalizationReadinessSlot slot)
        {
            builder.Append('{');
            AppendString(builder, "slotKind", slot.SlotKind).Append(',');
            AppendString(builder, "stringId", slot.StringId).Append(',');
            AppendBool(builder, "isAvailable", slot.IsAvailable).Append(',');
            AppendInt(builder, "characterCount", slot.CharacterCount).Append(',');
            AppendInt(builder, "maxCharacterCount", slot.MaxCharacterCount).Append(',');
            AppendBool(builder, "exceedsRecommendedLength", slot.ExceedsRecommendedLength).Append(',');
            AppendBool(builder, "hasLocalizedFullTextPayload", slot.HasLocalizedFullTextPayload);
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
