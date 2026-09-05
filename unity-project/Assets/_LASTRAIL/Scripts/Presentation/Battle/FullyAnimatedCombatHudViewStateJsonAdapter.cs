using System;
using System.Text;

namespace LastRail.Presentation.Battle
{
    public static class FullyAnimatedCombatHudViewStateJsonAdapter
    {
        public static string ToJson(FullyAnimatedCombatHudViewState state)
        {
            if (state == null)
            {
                return "null";
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "surface", state.Surface.ToString());
            AppendCommaString(builder, "encounterType", state.EncounterType.ToString());
            AppendCommaString(builder, "missionId", state.MissionId);
            AppendCommaString(builder, "encounterTitle", state.EncounterTitle);
            AppendCommaString(builder, "waveCounterLabel", state.WaveCounterLabel);
            AppendCommaString(builder, "bossId", state.BossId);
            AppendCommaString(builder, "bossPhaseLabel", state.BossPhaseLabel);
            AppendCommaString(builder, "bossStatusPlaceholder", state.BossStatusPlaceholder);
            AppendCommaString(builder, "bossObjectiveLabel", state.BossObjectiveLabel);
            AppendCommaString(builder, "failureStaminaRefundHint", state.FailureStaminaRefundHint);
            AppendCommaNumber(builder, "operatorSlotCount", state.OperatorSlotCount);
            AppendCommaNumber(builder, "enemyUnitCount", state.EnemyUnitCount);
            AppendCommaNumber(builder, "raidBossUnitCount", state.RaidBossUnitCount);
            AppendCommaSkillIcons(builder, state.OperatorSkillIconRefs);
            AppendCommaString(builder, "skillCueId", state.SkillCueId);
            AppendCommaString(builder, "ultimateCueId", state.UltimateCueId);
            AppendCommaString(builder, "damageCueId", state.DamageCueId);
            AppendCommaString(builder, "hitFlashCueId", state.HitFlashCueId);
            AppendCommaString(builder, "hitStopCueId", state.HitStopCueId);
            AppendCommaString(builder, "screenShakeCueId", state.ScreenShakeCueId);
            AppendCommaString(builder, "vfxCueId", state.VfxCueId);
            AppendCommaString(builder, "sfxCueId", state.SfxCueId);
            builder.Append(",\"statusCueIds\":[");
            for (var i = 0; i < state.StatusCueIds.Count; i++)
            {
                if (i > 0) builder.Append(',');
                AppendJsonString(builder, state.StatusCueIds[i]);
            }
            builder.Append(']');
            AppendCommaString(builder, "vibrationCueId", state.VibrationCueId);
            AppendCommaString(builder, "waveTransitionCueId", state.WaveTransitionCueId);
            AppendCommaString(builder, "bossPhaseTransitionCueId", state.BossPhaseTransitionCueId);
            AppendCommaString(builder, "battlefieldRegionKey", state.BattlefieldRegionKey);
            AppendCommaString(builder, "tacticalRegionKey", state.TacticalRegionKey);
            AppendCommaString(builder, "hudLayerKey", state.HudLayerKey);
            AppendCommaBool(builder, "usesPortraitSafeArea", state.UsesPortraitSafeArea);
            AppendCommaNumber(builder, "minimumTouchTargetDp", state.MinimumTouchTargetDp);
            AppendCommaBool(builder, "keepsHudAboveAnimationLayer", state.KeepsHudAboveAnimationLayer);
            AppendCommaBool(builder, "preventsTextOverflow", state.PreventsTextOverflow);
            AppendCommaBool(builder, "preservesBattlefieldVisibility", state.PreservesBattlefieldVisibility);
            AppendCommaBool(builder, "preservesCurrentCarTacticalVisibility", state.PreservesCurrentCarTacticalVisibility);
            AppendCommaBool(builder, "hasAnimationTimelinePayload", state.HasAnimationTimelinePayload);
            AppendCommaBool(builder, "hasFramePayload", state.HasFramePayload);
            AppendCommaBool(builder, "hasServerTruthMutation", state.HasServerTruthMutation);
            AppendCommaBool(builder, "hasRewardCalculation", state.HasRewardCalculation);
            AppendCommaBool(builder, "hasEconomyMutation", state.HasEconomyMutation);
            AppendCommaBool(builder, "hasClientSideRng", state.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
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

        private static void AppendCommaNumber(StringBuilder builder, string name, int value)
        {
            builder.Append(',').Append('"').Append(name).Append("\":").Append(value);
        }

        private static void AppendCommaBool(StringBuilder builder, string name, bool value)
        {
            builder.Append(',').Append('"').Append(name).Append("\":").Append(value ? "true" : "false");
        }

        private static void AppendCommaSkillIcons(StringBuilder builder, System.Collections.Generic.IReadOnlyList<CombatOperatorSkillIconDefinition> icons)
        {
            builder.Append(",\"operatorSkillIconRefs\":[");
            for (var i = 0; i < icons.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var icon = icons[i];
                builder.Append('{');
                AppendString(builder, "operatorId", icon.OperatorId);
                AppendCommaString(builder, "skillId", icon.SkillId);
                AppendCommaString(builder, "iconPath", icon.IconPath);
                AppendCommaBool(builder, "hasGameplayEffectDefinition", icon.HasGameplayEffectDefinition);
                builder.Append('}');
            }

            builder.Append(']');
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
