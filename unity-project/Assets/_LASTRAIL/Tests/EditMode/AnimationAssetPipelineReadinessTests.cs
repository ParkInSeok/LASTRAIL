using NUnit.Framework;
using LastRail.Presentation.Animation;

namespace LastRail.Tests.EditMode
{
    public sealed class AnimationAssetPipelineReadinessTests
    {
        [Test]
        public void AuditLaunchPipeline_MapsFifteenOperatorsEnemiesBossAtlasGroupsAndVfxBudget()
        {
            var manifest = ActOneAnimationAssetManifestFixtures.CreateMinimumLaunchCandidate();

            var audit = AnimationAssetPipelineReadinessAudit.AuditLaunchPipeline(
                manifest,
                AnimationAssetPipelineSpec.ActOneLaunchRecommended(),
                PackageManifestWithout2DAnimation());

            Assert.That(audit.VisualOperatorCount, Is.EqualTo(15));
            Assert.That(audit.InitialOperatorCount, Is.EqualTo(5));
            Assert.That(audit.GachaEliteOperatorCount, Is.EqualTo(10));
            Assert.That(audit.EnemyMinimumCount, Is.EqualTo(1));
            Assert.That(audit.RaidBossMinimumCount, Is.EqualTo(1));
            Assert.That(audit.IsCoverageComplete, Is.EqualTo(true));
            Assert.That(audit.ToolchainName, Is.EqualTo("Unity 2D Animation package + Animator/Animation Clip + Sprite Atlas + sprite-sheet VFX"));
            Assert.That(audit.RequiredUnityPackageId, Is.EqualTo("com.unity.2d.animation"));
            Assert.That(audit.IsRequiredUnityPackageDeclared, Is.EqualTo(false));
            Assert.That(audit.HasPackageImportBlocker, Is.EqualTo(true));
            Assert.That(audit.PackageImportBlockerMessage, Is.EqualTo("Unity Editor/package import approval required for com.unity.2d.animation"));
            Assert.That(audit.PrimaryAtlasMaxSize, Is.EqualTo(2048));
            Assert.That(audit.LowEndFallbackAtlasSize, Is.EqualTo(1024));
            Assert.That(audit.UsesSeparateUiPortraitCombatVfxAtlases, Is.EqualTo(true));
            Assert.That(audit.OperatorCombatAtlasKey, Is.EqualTo("atlas_operator_combat"));
            Assert.That(audit.OperatorPortraitIconAtlasKey, Is.EqualTo("atlas_operator_portrait_icon"));
            Assert.That(audit.EnemyCombatAtlasKey, Is.EqualTo("atlas_enemy_combat"));
            Assert.That(audit.RaidBossCombatAtlasKey, Is.EqualTo("atlas_raid_boss_combat"));
            Assert.That(audit.VfxAtlasKey, Is.EqualTo("atlas_sprite_sheet_vfx"));
            Assert.That(audit.LargeVfxConcurrentLimit, Is.EqualTo(2));
            Assert.That(audit.HitSparkConcurrentMin, Is.EqualTo(8));
            Assert.That(audit.HitSparkConcurrentMax, Is.EqualTo(12));
            Assert.That(audit.FullScreenFlashMaxSeconds, Is.EqualTo(0.15f));
            Assert.That(audit.BossPhaseVfxMinSeconds, Is.EqualTo(1.0f));
            Assert.That(audit.BossPhaseVfxMaxSeconds, Is.EqualTo(2.0f));
            Assert.That(audit.ScreenShakeTierCount, Is.EqualTo(3));
            Assert.That(audit.HudReadabilityFirst, Is.EqualTo(true));
        }

        [Test]
        public void AuditLaunchPipeline_WhenPackageDeclared_RemovesPackageImportBlockerWithoutImportingAssets()
        {
            var audit = AnimationAssetPipelineReadinessAudit.AuditLaunchPipeline(
                ActOneAnimationAssetManifestFixtures.CreateMinimumLaunchCandidate(),
                AnimationAssetPipelineSpec.ActOneLaunchRecommended(),
                PackageManifestWith2DAnimation());

            Assert.That(audit.IsRequiredUnityPackageDeclared, Is.EqualTo(true));
            Assert.That(audit.HasPackageImportBlocker, Is.EqualTo(false));
            Assert.That(audit.PackageImportBlockerMessage, Is.EqualTo(null));
            Assert.That(audit.HasDownloadedExternalAsset, Is.EqualTo(false));
            Assert.That(audit.HasSpriteAssetPayload, Is.EqualTo(false));
            Assert.That(audit.HasAnimatorControllerAsset, Is.EqualTo(false));
            Assert.That(audit.HasPrefabAsset, Is.EqualTo(false));
        }

        [Test]
        public void DebugJson_WritesStablePipelineFieldsWithoutRawArtTruthServerOrSensitivePayload()
        {
            var audit = AnimationAssetPipelineReadinessAudit.AuditLaunchPipeline(
                ActOneAnimationAssetManifestFixtures.CreateMinimumLaunchCandidate(),
                AnimationAssetPipelineSpec.ActOneLaunchRecommended(),
                PackageManifestWithout2DAnimation());

            var json = AnimationAssetPipelineReadinessJsonAdapter.ToJson(audit);

            Assert.That(json.Contains("\"toolchainName\":\"Unity 2D Animation package + Animator/Animation Clip + Sprite Atlas + sprite-sheet VFX\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"visualOperatorCount\":15"), Is.EqualTo(true));
            Assert.That(json.Contains("\"gachaEliteOperatorCount\":10"), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiredUnityPackageId\":\"com.unity.2d.animation\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"isRequiredUnityPackageDeclared\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"primaryAtlasMaxSize\":2048"), Is.EqualTo(true));
            Assert.That(json.Contains("\"lowEndFallbackAtlasSize\":1024"), Is.EqualTo(true));
            Assert.That(json.Contains("\"largeVfxConcurrentLimit\":2"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hitSparkConcurrentMin\":8"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hitSparkConcurrentMax\":12"), Is.EqualTo(true));
            Assert.That(json.Contains("\"fullScreenFlashMaxSeconds\":0.15"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hudReadabilityFirst\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasDownloadedExternalAsset\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasSpriteAssetPayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasServerTruthMutation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("rawImage"), Is.EqualTo(false));
            Assert.That(json.Contains("rawTexture"), Is.EqualTo(false));
            Assert.That(json.Contains("texturePath"), Is.EqualTo(false));
            Assert.That(json.Contains("assetPath"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(json.Contains("frameData"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }

        private static string PackageManifestWithout2DAnimation()
        {
            return "{\"dependencies\":{\"com.unity.2d.sprite\":\"1.0.0\",\"com.unity.ugui\":\"2.0.0\"}}";
        }

        private static string PackageManifestWith2DAnimation()
        {
            return "{\"dependencies\":{\"com.unity.2d.animation\":\"10.0.0\",\"com.unity.2d.sprite\":\"1.0.0\"}}";
        }
    }
}
