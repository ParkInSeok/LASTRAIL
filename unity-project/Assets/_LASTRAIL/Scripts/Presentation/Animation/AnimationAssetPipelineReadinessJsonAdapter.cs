using System;
using System.Globalization;
using System.Text;

namespace LastRail.Presentation.Animation
{
    public static class AnimationAssetPipelineReadinessJsonAdapter
    {
        public static string ToJson(AnimationAssetPipelineReadinessAuditResult audit)
        {
            if (audit == null)
            {
                throw new ArgumentNullException(nameof(audit));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "toolchainName", audit.ToolchainName).Append(',');
            AppendString(builder, "requiredUnityPackageId", audit.RequiredUnityPackageId).Append(',');
            AppendBool(builder, "isRequiredUnityPackageDeclared", audit.IsRequiredUnityPackageDeclared).Append(',');
            AppendBool(builder, "hasPackageImportBlocker", audit.HasPackageImportBlocker).Append(',');
            AppendOptionalString(builder, "packageImportBlockerMessage", audit.PackageImportBlockerMessage).Append(',');
            AppendInt(builder, "visualOperatorCount", audit.VisualOperatorCount).Append(',');
            AppendInt(builder, "initialOperatorCount", audit.InitialOperatorCount).Append(',');
            AppendInt(builder, "gachaEliteOperatorCount", audit.GachaEliteOperatorCount).Append(',');
            AppendInt(builder, "enemyMinimumCount", audit.EnemyMinimumCount).Append(',');
            AppendInt(builder, "raidBossMinimumCount", audit.RaidBossMinimumCount).Append(',');
            AppendBool(builder, "isCoverageComplete", audit.IsCoverageComplete).Append(',');
            AppendInt(builder, "primaryAtlasMaxSize", audit.PrimaryAtlasMaxSize).Append(',');
            AppendInt(builder, "lowEndFallbackAtlasSize", audit.LowEndFallbackAtlasSize).Append(',');
            AppendBool(builder, "usesUnity2DAnimationPackage", audit.UsesUnity2DAnimationPackage).Append(',');
            AppendBool(builder, "usesAnimatorControllers", audit.UsesAnimatorControllers).Append(',');
            AppendBool(builder, "usesAnimationClips", audit.UsesAnimationClips).Append(',');
            AppendBool(builder, "usesSpriteAtlas", audit.UsesSpriteAtlas).Append(',');
            AppendBool(builder, "usesSpriteSheetVfx", audit.UsesSpriteSheetVfx).Append(',');
            AppendBool(builder, "usesSeparateUiPortraitCombatVfxAtlases", audit.UsesSeparateUiPortraitCombatVfxAtlases).Append(',');
            AppendString(builder, "operatorCombatAtlasKey", audit.OperatorCombatAtlasKey).Append(',');
            AppendString(builder, "operatorPortraitIconAtlasKey", audit.OperatorPortraitIconAtlasKey).Append(',');
            AppendString(builder, "enemyCombatAtlasKey", audit.EnemyCombatAtlasKey).Append(',');
            AppendString(builder, "raidBossCombatAtlasKey", audit.RaidBossCombatAtlasKey).Append(',');
            AppendString(builder, "vfxAtlasKey", audit.VfxAtlasKey).Append(',');
            AppendInt(builder, "largeVfxConcurrentLimit", audit.LargeVfxConcurrentLimit).Append(',');
            AppendInt(builder, "hitSparkConcurrentMin", audit.HitSparkConcurrentMin).Append(',');
            AppendInt(builder, "hitSparkConcurrentMax", audit.HitSparkConcurrentMax).Append(',');
            AppendFloat(builder, "fullScreenFlashMaxSeconds", audit.FullScreenFlashMaxSeconds).Append(',');
            AppendFloat(builder, "bossPhaseVfxMinSeconds", audit.BossPhaseVfxMinSeconds).Append(',');
            AppendFloat(builder, "bossPhaseVfxMaxSeconds", audit.BossPhaseVfxMaxSeconds).Append(',');
            AppendInt(builder, "screenShakeTierCount", audit.ScreenShakeTierCount).Append(',');
            AppendBool(builder, "hudReadabilityFirst", audit.HudReadabilityFirst).Append(',');
            AppendBool(builder, "isReadinessComplete", audit.IsReadinessComplete).Append(',');
            AppendBool(builder, "hasDownloadedExternalAsset", audit.HasDownloadedExternalAsset).Append(',');
            AppendBool(builder, "hasRawArtPayload", audit.HasRawArtPayload).Append(',');
            AppendBool(builder, "hasSpriteAssetPayload", audit.HasSpriteAssetPayload).Append(',');
            AppendBool(builder, "hasAnimatorControllerAsset", audit.HasAnimatorControllerAsset).Append(',');
            AppendBool(builder, "hasPrefabAsset", audit.HasPrefabAsset).Append(',');
            AppendBool(builder, "hasServerTruthMutation", audit.HasServerTruthMutation).Append(',');
            AppendBool(builder, "hasRewardCalculation", audit.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", audit.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", audit.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
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
