using System;
using System.Globalization;
using System.Text;

namespace LastRail.Presentation.Battle
{
    public static class MobileCombatHudPresentationPolicyJsonAdapter
    {
        public static string ToJson(MobileCombatHudPresentationPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "surface", policy.Surface.ToString()).Append(',');
            AppendString(builder, "encounterType", policy.EncounterType.ToString()).Append(',');
            AppendString(builder, "missionId", policy.MissionId).Append(',');
            AppendSafeArea(builder, policy.SafeArea).Append(',');
            AppendLayout(builder, policy).Append(',');
            AppendWaveHud(builder, policy.WaveHud).Append(',');
            AppendBossHud(builder, policy.BossHud).Append(',');
            AppendTouch(builder, policy.Touch).Append(',');
            AppendReadability(builder, policy.Readability).Append(',');
            AppendVibration(builder, policy.Vibration).Append(',');
            AppendBool(builder, "isPresentationOnly", policy.IsPresentationOnly).Append(',');
            AppendBool(builder, "hasServerTruthMutation", policy.HasServerTruthMutation).Append(',');
            AppendBool(builder, "hasAnimationTimelinePayload", policy.HasAnimationTimelinePayload).Append(',');
            AppendBool(builder, "hasFramePayload", policy.HasFramePayload).Append(',');
            AppendBool(builder, "hasTransportPayload", policy.HasTransportPayload).Append(',');
            AppendBool(builder, "hasEconomyMutation", policy.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", policy.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendSafeArea(StringBuilder builder, MobileSafeAreaInsets safeArea)
        {
            builder.Append("\"safeArea\":{");
            AppendInt(builder, "viewportWidth", safeArea.ViewportWidth).Append(',');
            AppendInt(builder, "viewportHeight", safeArea.ViewportHeight).Append(',');
            AppendInt(builder, "top", safeArea.Top).Append(',');
            AppendInt(builder, "bottom", safeArea.Bottom).Append(',');
            AppendInt(builder, "left", safeArea.Left).Append(',');
            AppendInt(builder, "right", safeArea.Right).Append(',');
            AppendBool(builder, "isPortraitNineBySixteen", safeArea.IsPortraitNineBySixteen);
            return builder.Append('}');
        }

        private static StringBuilder AppendLayout(StringBuilder builder, MobileCombatHudPresentationPolicy policy)
        {
            builder.Append("\"layout\":{");
            AppendString(builder, "battlefieldRegionKey", policy.BattlefieldRegionKey).Append(',');
            AppendString(builder, "tacticalRegionKey", policy.TacticalRegionKey).Append(',');
            AppendString(builder, "hudLayerKey", policy.HudLayerKey);
            return builder.Append('}');
        }

        private static StringBuilder AppendWaveHud(StringBuilder builder, MobileCombatHudWaveState waveHud)
        {
            builder.Append("\"waveHud\":{");
            AppendString(builder, "waveHudKey", waveHud.WaveHudKey).Append(',');
            AppendBool(builder, "hasWaveCounter", waveHud.HasWaveCounter).Append(',');
            AppendInt(builder, "currentWaveNumber", waveHud.CurrentWaveNumber).Append(',');
            AppendInt(builder, "totalWaveCount", waveHud.TotalWaveCount);
            return builder.Append('}');
        }

        private static StringBuilder AppendBossHud(StringBuilder builder, MobileCombatHudBossState bossHud)
        {
            builder.Append("\"bossHud\":{");
            AppendString(builder, "bossHudKey", bossHud.BossHudKey).Append(',');
            AppendOptionalString(builder, "bossId", bossHud.BossId).Append(',');
            AppendBool(builder, "hasBossPhase", bossHud.HasBossPhase).Append(',');
            AppendNullableInt(builder, "bossPhase", bossHud.BossPhase);
            return builder.Append('}');
        }

        private static StringBuilder AppendTouch(StringBuilder builder, MobileCombatTouchPolicy touch)
        {
            builder.Append("\"touch\":{");
            AppendInt(builder, "minimumTouchTargetDp", touch.MinimumTouchTargetDp).Append(',');
            AppendBool(builder, "requiresSafeAreaPadding", touch.RequiresSafeAreaPadding).Append(',');
            AppendBool(builder, "supportsBottomTacticalGestures", touch.SupportsBottomTacticalGestures).Append(',');
            AppendBool(builder, "keepsCriticalActionsAwayFromInsets", touch.KeepsCriticalActionsAwayFromInsets);
            return builder.Append('}');
        }

        private static StringBuilder AppendReadability(StringBuilder builder, MobileCombatHudReadabilityPolicy readability)
        {
            builder.Append("\"readability\":{");
            AppendBool(builder, "preventsTextOverflow", readability.PreventsTextOverflow).Append(',');
            AppendBool(builder, "keepsHudAboveAnimationLayer", readability.KeepsHudAboveAnimationLayer).Append(',');
            AppendBool(builder, "preservesBattlefieldVisibility", readability.PreservesBattlefieldVisibility).Append(',');
            AppendBool(builder, "preservesCurrentCarTacticalVisibility", readability.PreservesCurrentCarTacticalVisibility);
            return builder.Append('}');
        }

        private static StringBuilder AppendVibration(StringBuilder builder, MobileCombatVibrationCuePolicy vibration)
        {
            builder.Append("\"vibration\":{");
            AppendBool(builder, "isEnabledByDefault", vibration.IsEnabledByDefault).Append(',');
            AppendString(builder, "lightHitCueId", vibration.LightHitCueId).Append(',');
            AppendString(builder, "heavyHitCueId", vibration.HeavyHitCueId).Append(',');
            AppendString(builder, "skillCueId", vibration.SkillCueId).Append(',');
            AppendString(builder, "ultimateCueId", vibration.UltimateCueId).Append(',');
            AppendString(builder, "bossPhaseCueId", vibration.BossPhaseCueId);
            return builder.Append('}');
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
