using System;
using System.Globalization;
using System.Text;

namespace LastRail.Presentation.Battle
{
    public static class FullyAnimatedCombatPresentationStateJsonAdapter
    {
        public static string ToJson(FullyAnimatedCombatPresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "encounterType", state.EncounterType.ToString()).Append(',');
            AppendString(builder, "missionId", state.MissionId).Append(',');
            AppendInt(builder, "currentWaveNumber", state.CurrentWaveNumber).Append(',');
            AppendBool(builder, "hasBossPhase", state.HasBossPhase).Append(',');
            AppendNullableInt(builder, "bossPhase", state.BossPhase).Append(',');
            AppendOptionalString(builder, "bossId", state.BossId).Append(',');
            AppendOptionalString(builder, "startAugmentPackageRef", state.StartAugmentPackageRef).Append(',');
            AppendOptionalString(builder, "startTacticPackageRef", state.StartTacticPackageRef).Append(',');
            AppendHudReadability(builder, state.HudReadability).Append(',');
            AppendPresentationCues(builder, state.PresentationCues).Append(',');
            AppendUnits(builder, state).Append(',');
            AppendBool(builder, "isPresentationOnly", state.IsPresentationOnly).Append(',');
            AppendBool(builder, "hasServerTruthMutation", state.HasServerTruthMutation).Append(',');
            AppendBool(builder, "hasRawTimelinePayload", state.HasRawTimelinePayload).Append(',');
            AppendBool(builder, "hasFramePayload", state.HasFramePayload).Append(',');
            AppendBool(builder, "hasEconomyMutation", state.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", state.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendHudReadability(StringBuilder builder, CombatHudReadabilityState hud)
        {
            builder.Append("\"hudReadability\":{");
            AppendBool(builder, "hasSeparateHudLayer", hud.HasSeparateHudLayer).Append(',');
            AppendBool(builder, "keepsTouchControlsReadable", hud.KeepsTouchControlsReadable).Append(',');
            AppendBool(builder, "prioritizesReadableCombatText", hud.PrioritizesReadableCombatText);
            return builder.Append('}');
        }

        private static StringBuilder AppendPresentationCues(StringBuilder builder, CombatPresentationCueSet cues)
        {
            builder.Append("\"presentationCues\":{");
            AppendString(builder, "damageCueId", cues.DamageCueId).Append(',');
            AppendString(builder, "hitFlashCueId", cues.HitFlashCueId).Append(',');
            AppendString(builder, "hitStopCueId", cues.HitStopCueId).Append(',');
            AppendString(builder, "screenShakeCueId", cues.ScreenShakeCueId).Append(',');
            AppendString(builder, "vfxCueId", cues.VfxCueId).Append(',');
            AppendString(builder, "sfxCueId", cues.SfxCueId).Append(',');
            AppendString(builder, "vibrationCueId", cues.VibrationCueId).Append(',');
            AppendOptionalString(builder, "waveTransitionCueId", cues.WaveTransitionCueId).Append(',');
            AppendOptionalString(builder, "bossPhaseTransitionCueId", cues.BossPhaseTransitionCueId);
            return builder.Append('}');
        }

        private static StringBuilder AppendUnits(StringBuilder builder, FullyAnimatedCombatPresentationState state)
        {
            builder.Append("\"unitAnimationStates\":[");
            for (var i = 0; i < state.UnitAnimationStates.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var unit = state.UnitAnimationStates[i];
                builder.Append('{');
                AppendString(builder, "unitId", unit.UnitId).Append(',');
                AppendString(builder, "unitRole", unit.UnitRole.ToString()).Append(',');
                AppendString(builder, "animationKey", unit.AnimationKey).Append(',');
                AppendString(builder, "animationCueId", unit.AnimationCueId);
                builder.Append('}');
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

        private static StringBuilder AppendInt(StringBuilder builder, string fieldName, int value)
        {
            return builder.Append('"')
                .Append(fieldName)
                .Append("\":")
                .Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static StringBuilder AppendNullableInt(StringBuilder builder, string fieldName, int? value)
        {
            if (!value.HasValue)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendInt(builder, fieldName, value.Value);
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
