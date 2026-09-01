using System;
using System.Text;

namespace LastRail.Presentation
{
    public static class LaunchScreenScreenshotQaReportJsonAdapter
    {
        public static string ToJson(LaunchScreenScreenshotQaReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendInt(builder, "screenCount", report.ScreenCount).Append(',');
            AppendInt(builder, "codeLayoutPassCount", report.CodeLayoutPassCount).Append(',');
            AppendInt(builder, "codeLayoutFailCount", report.CodeLayoutFailCount).Append(',');
            AppendBool(builder, "screenshotCaptureAttempted", report.ScreenshotCaptureAttempted).Append(',');
            AppendInt(builder, "screenshotPassCount", report.ScreenshotPassCount).Append(',');
            AppendInt(builder, "screenshotFailCount", report.ScreenshotFailCount).Append(',');
            AppendInt(builder, "warningCount", report.WarningCount).Append(',');
            AppendInt(builder, "failCount", report.FailCount).Append(',');
            AppendStringArray(builder, "screenKeys", report.ScreenKeys).Append(',');
            AppendStringArray(builder, "fidelityReasons", report.FidelityReasons).Append(',');
            AppendStringArray(builder, "blockers", report.Blockers).Append(',');
            AppendBool(builder, "isReadyForDeviceScreenshotQa", report.IsReadyForDeviceScreenshotQa).Append(',');
            AppendBool(builder, "usesPortraitSafeArea", report.UsesPortraitSafeArea).Append(',');
            AppendInt(builder, "minimumTouchTargetDp", report.MinimumTouchTargetDp).Append(',');
            AppendBool(builder, "requiresTextClampOverflowQa", report.RequiresTextClampOverflowQa).Append(',');
            AppendBool(builder, "requiresTenPullGridOverflowQa", report.RequiresTenPullGridOverflowQa).Append(',');
            AppendBool(builder, "requiresCodexDetailScrollOrClampQa", report.RequiresCodexDetailScrollOrClampQa).Append(',');
            AppendBool(builder, "requiresCombatHudReadabilityQa", report.RequiresCombatHudReadabilityQa).Append(',');
            AppendBool(builder, "requiresRewardOfflineStatusRetryLabelQa", report.RequiresRewardOfflineStatusRetryLabelQa).Append(',');
            AppendBool(builder, "hasServerTransport", report.HasServerTransport).Append(',');
            AppendBool(builder, "hasRewardCalculation", report.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasOfflineRewardCalculation", report.HasOfflineRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", report.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", report.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
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

        private static StringBuilder AppendInt(StringBuilder builder, string fieldName, int value)
        {
            return builder.Append('"').Append(fieldName).Append("\":").Append(value);
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
