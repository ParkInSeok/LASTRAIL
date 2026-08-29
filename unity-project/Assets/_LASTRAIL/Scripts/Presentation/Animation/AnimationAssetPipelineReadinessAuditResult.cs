namespace LastRail.Presentation.Animation
{
    public sealed class AnimationAssetPipelineReadinessAuditResult
    {
        public AnimationAssetPipelineReadinessAuditResult(
            AnimationAssetCoverageAuditResult coverage,
            AnimationAssetPipelineSpec spec,
            bool isRequiredUnityPackageDeclared)
        {
            Coverage = coverage ?? throw new System.ArgumentNullException(nameof(coverage));
            Spec = spec ?? throw new System.ArgumentNullException(nameof(spec));
            IsRequiredUnityPackageDeclared = isRequiredUnityPackageDeclared;
        }

        public AnimationAssetCoverageAuditResult Coverage { get; }
        public AnimationAssetPipelineSpec Spec { get; }
        public string ToolchainName => Spec.ToolchainName;
        public string RequiredUnityPackageId => Spec.RequiredUnityPackageId;
        public bool IsRequiredUnityPackageDeclared { get; }
        public bool HasPackageImportBlocker => !IsRequiredUnityPackageDeclared;
        public string PackageImportBlockerMessage => HasPackageImportBlocker
            ? "Unity Editor/package import approval required for " + RequiredUnityPackageId
            : null;
        public int VisualOperatorCount => Coverage.OperatorCount;
        public int InitialOperatorCount => Coverage.InitialOperatorCount;
        public int GachaEliteOperatorCount => Coverage.GachaEliteOperatorCount;
        public int EnemyMinimumCount => Coverage.EnemyCount;
        public int RaidBossMinimumCount => Coverage.RaidBossCount;
        public bool IsCoverageComplete => Coverage.IsComplete;
        public int PrimaryAtlasMaxSize => Spec.PrimaryAtlasMaxSize;
        public int LowEndFallbackAtlasSize => Spec.LowEndFallbackAtlasSize;
        public bool UsesUnity2DAnimationPackage => Spec.UsesUnity2DAnimationPackage;
        public bool UsesAnimatorControllers => Spec.UsesAnimatorControllers;
        public bool UsesAnimationClips => Spec.UsesAnimationClips;
        public bool UsesSpriteAtlas => Spec.UsesSpriteAtlas;
        public bool UsesSpriteSheetVfx => Spec.UsesSpriteSheetVfx;
        public bool UsesSeparateUiPortraitCombatVfxAtlases => Spec.UsesSeparateUiPortraitCombatVfxAtlases;
        public string OperatorCombatAtlasKey => Spec.OperatorCombatAtlasKey;
        public string OperatorPortraitIconAtlasKey => Spec.OperatorPortraitIconAtlasKey;
        public string EnemyCombatAtlasKey => Spec.EnemyCombatAtlasKey;
        public string RaidBossCombatAtlasKey => Spec.RaidBossCombatAtlasKey;
        public string VfxAtlasKey => Spec.VfxAtlasKey;
        public int LargeVfxConcurrentLimit => Spec.VfxBudget.LargeVfxConcurrentLimit;
        public int HitSparkConcurrentMin => Spec.VfxBudget.HitSparkConcurrentMin;
        public int HitSparkConcurrentMax => Spec.VfxBudget.HitSparkConcurrentMax;
        public float FullScreenFlashMaxSeconds => Spec.VfxBudget.FullScreenFlashMaxSeconds;
        public float BossPhaseVfxMinSeconds => Spec.VfxBudget.BossPhaseVfxMinSeconds;
        public float BossPhaseVfxMaxSeconds => Spec.VfxBudget.BossPhaseVfxMaxSeconds;
        public int ScreenShakeTierCount => Spec.VfxBudget.ScreenShakeTierCount;
        public bool HudReadabilityFirst => Spec.VfxBudget.HudReadabilityFirst;
        public bool IsReadinessComplete => IsCoverageComplete && IsRequiredUnityPackageDeclared;
        public bool HasDownloadedExternalAsset => false;
        public bool HasRawArtPayload => false;
        public bool HasSpriteAssetPayload => false;
        public bool HasAnimatorControllerAsset => false;
        public bool HasPrefabAsset => false;
        public bool HasServerTruthMutation => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;
    }
}
