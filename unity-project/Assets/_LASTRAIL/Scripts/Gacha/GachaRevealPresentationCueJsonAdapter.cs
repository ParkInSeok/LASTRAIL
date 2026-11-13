using System;
using System.Text;

namespace LastRail.Gacha
{
    public static class GachaRevealPresentationCueJsonAdapter
    {
        public static string ToJson(GachaRevealPresentationCue cue)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "revealStartCueId", cue.RevealStartCueId);
            builder.Append(',');
            AppendString(builder, "rarityFlashCueId", cue.RarityFlashCueId);
            builder.Append(',');
            AppendString(builder, "guaranteeBadgeCueId", cue.GuaranteeBadgeCueId);
            builder.Append(',');
            AppendString(builder, "duplicateConversionCueId", cue.DuplicateConversionCueId);
            builder.Append(',');
            AppendString(builder, "resultCardCueId", cue.ResultCardCueId);
            builder.Append(',');
            AppendString(builder, "soundCueId", cue.SoundCueId);
            builder.Append(',');
            AppendString(builder, "vibrationCueId", cue.VibrationCueId);
            builder.Append(',');
            AppendBoolean(builder, "hasGuaranteeBadge", cue.HasGuaranteeBadge);
            builder.Append(',');
            AppendBoolean(builder, "hasDuplicateConversionCue", cue.HasDuplicateConversionCue);
            builder.Append(',');
            AppendBoolean(builder, "hasEconomyMutation", cue.HasEconomyMutation);
            builder.Append(',');
            AppendBoolean(builder, "hasClientSideRng", cue.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendString(StringBuilder builder, string propertyName, string value)
        {
            builder.Append('"');
            builder.Append(propertyName);
            builder.Append("\":");
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            builder.Append(Escape(value));
            builder.Append('"');
        }

        private static void AppendBoolean(StringBuilder builder, string propertyName, bool value)
        {
            builder.Append('"');
            builder.Append(propertyName);
            builder.Append("\":");
            builder.Append(value ? "true" : "false");
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
