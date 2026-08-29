using System;
using System.Collections.Generic;
using System.Text;

namespace LastRail.Presentation.Audio
{
    public static class LaunchAudioCueReadinessJsonAdapter
    {
        public static string ToJson(LaunchAudioCueReadinessReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "bgmLoopCueId", report.BgmLoopCueId);
            AppendCommaBool(builder, "usesSingleBgmLoop", report.UsesSingleBgmLoop);
            AppendCommaBool(builder, "preventsBgmOverlap", report.PreventsBgmOverlap);
            AppendCommaBool(builder, "requiresNativeVibrationBridge", report.RequiresNativeVibrationBridge);
            AppendCommaString(builder, "nativeVibrationBlockerMessage", report.NativeVibrationBlockerMessage);
            builder.Append(",\"mission\":");
            AppendState(builder, report.Mission);
            builder.Append(",\"gacha\":");
            AppendState(builder, report.Gacha);
            builder.Append(",\"combat\":");
            AppendState(builder, report.Combat);
            AppendCommaBool(builder, "hasNativeVibrationImplementation", report.HasNativeVibrationImplementation);
            AppendCommaBool(builder, "hasExternalAudioAsset", report.HasExternalAudioAsset);
            AppendCommaBool(builder, "hasServerTransport", report.HasServerTransport);
            AppendCommaBool(builder, "hasRewardCalculation", report.HasRewardCalculation);
            AppendCommaBool(builder, "hasEconomyMutation", report.HasEconomyMutation);
            AppendCommaBool(builder, "hasClientSideRng", report.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendState(StringBuilder builder, LaunchAudioCueReadinessState state)
        {
            builder.Append('{');
            AppendString(builder, "surfaceKey", state.SurfaceKey);
            builder.Append(',');
            AppendStringArray(builder, "soundCueIds", state.SoundCueIds);
            builder.Append(',');
            AppendStringArray(builder, "vibrationCueIds", state.VibrationCueIds);
            builder.Append(',');
            AppendStringArray(builder, "presentationCueIds", state.PresentationCueIds);
            AppendCommaString(builder, "bgmLoopCueId", state.BgmLoopCueId);
            AppendCommaBool(builder, "usesSingleBgmLoop", state.UsesSingleBgmLoop);
            AppendCommaBool(builder, "preventsBgmOverlap", state.PreventsBgmOverlap);
            AppendCommaBool(builder, "requiresNativeVibrationBridge", state.RequiresNativeVibrationBridge);
            AppendCommaBool(builder, "hasNativeVibrationImplementation", state.HasNativeVibrationImplementation);
            AppendCommaBool(builder, "hasExternalAudioAsset", state.HasExternalAudioAsset);
            AppendCommaBool(builder, "hasServerTransport", state.HasServerTransport);
            AppendCommaBool(builder, "hasRewardCalculation", state.HasRewardCalculation);
            AppendCommaBool(builder, "hasEconomyMutation", state.HasEconomyMutation);
            AppendCommaBool(builder, "hasClientSideRng", state.HasClientSideRng);
            builder.Append('}');
        }

        private static void AppendStringArray(StringBuilder builder, string name, IReadOnlyList<string> values)
        {
            builder.Append('"').Append(name).Append("\":[");
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendJsonString(builder, values[i]);
            }

            builder.Append(']');
        }

        private static void AppendString(StringBuilder builder, string name, string value)
        {
            builder.Append('"').Append(name).Append("\":");
            AppendJsonString(builder, value);
        }

        private static void AppendCommaString(StringBuilder builder, string name, string value)
        {
            builder.Append(',');
            AppendString(builder, name, value);
        }

        private static void AppendCommaBool(StringBuilder builder, string name, bool value)
        {
            builder.Append(',').Append('"').Append(name).Append("\":").Append(value ? "true" : "false");
        }

        private static void AppendJsonString(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            foreach (var character in value)
            {
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (char.IsControl(character))
                        {
                            builder.Append("\\u").Append(((int)character).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            builder.Append('"');
        }
    }
}
