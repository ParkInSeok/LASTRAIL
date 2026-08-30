using System;
using System.Globalization;
using System.Text;

namespace LastRail.Presentation
{
    public static class LaunchPlayerFacingQaReadinessGateJsonAdapter
    {
        public static string ToJson(LaunchPlayerFacingQaReadinessGate gate)
        {
            if (gate == null)
            {
                throw new ArgumentNullException(nameof(gate));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendInt(builder, "screenCount", gate.ScreenCount).Append(',');
            AppendInt(builder, "passCount", gate.PassCount).Append(',');
            AppendInt(builder, "warningCount", gate.WarningCount).Append(',');
            AppendInt(builder, "failCount", gate.FailCount).Append(',');
            AppendStringArray(builder, "screenKeys", gate.ScreenKeys).Append(',');
            AppendStringArray(builder, "blockers", gate.Blockers).Append(',');
            AppendBool(builder, "usesPortraitSafeArea", gate.UsesPortraitSafeArea).Append(',');
            AppendInt(builder, "minimumTouchTargetDp", gate.MinimumTouchTargetDp).Append(',');
            AppendBool(builder, "requiresTextOverflowVerification", gate.RequiresTextOverflowVerification).Append(',');
            AppendBool(builder, "keepsHudReadableOverAnimation", gate.KeepsHudReadableOverAnimation).Append(',');
            AppendBool(builder, "requiresTenPullGridOverflowQa", gate.RequiresTenPullGridOverflowQa).Append(',');
            AppendBool(builder, "requiresCodexDetailScrollOrClampQa", gate.RequiresCodexDetailScrollOrClampQa).Append(',');
            AppendBool(builder, "requiresRewardStatusRetryLabelQa", gate.RequiresRewardStatusRetryLabelQa).Append(',');
            AppendInt(builder, "targetFps", gate.TargetFps).Append(',');
            AppendInt(builder, "lowEndFallbackFps", gate.LowEndFallbackFps).Append(',');
            AppendInt(builder, "largeVfxConcurrentLimit", gate.LargeVfxConcurrentLimit).Append(',');
            AppendInt(builder, "hitSparkConcurrentMin", gate.HitSparkConcurrentMin).Append(',');
            AppendInt(builder, "hitSparkConcurrentMax", gate.HitSparkConcurrentMax).Append(',');
            AppendFloat(builder, "fullScreenFlashMaxSeconds", gate.FullScreenFlashMaxSeconds).Append(',');
            AppendFloat(builder, "bossPhaseVfxMinSeconds", gate.BossPhaseVfxMinSeconds).Append(',');
            AppendFloat(builder, "bossPhaseVfxMaxSeconds", gate.BossPhaseVfxMaxSeconds).Append(',');
            AppendInt(builder, "screenShakeTierCount", gate.ScreenShakeTierCount).Append(',');
            AppendBool(builder, "usesSingleBgmLoop", gate.UsesSingleBgmLoop).Append(',');
            AppendBool(builder, "preventsBgmOverlap", gate.PreventsBgmOverlap).Append(',');
            AppendBool(builder, "requiresNativeVibrationBridge", gate.RequiresNativeVibrationBridge).Append(',');
            AppendBool(builder, "isReadyForEditorBindingPass", gate.IsReadyForEditorBindingPass).Append(',');
            AppendBool(builder, "isLaunchQaComplete", gate.IsLaunchQaComplete).Append(',');
            AppendBool(builder, "hasServerTransport", gate.HasServerTransport).Append(',');
            AppendBool(builder, "hasRewardCalculation", gate.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasOfflineRewardCalculation", gate.HasOfflineRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", gate.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", gate.HasClientSideRng);
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
            return builder.Append('"').Append(fieldName).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static StringBuilder AppendFloat(StringBuilder builder, string fieldName, float value)
        {
            return builder.Append('"').Append(fieldName).Append("\":").Append(value.ToString("0.##", CultureInfo.InvariantCulture));
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
