#if UNITY_EDITOR
using System.Collections.Generic;
using LastRail.Presentation.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchAnimationAssetPipelineImplementationTests
    {
        private const string PipelineRoot = "Assets/_LASTRAIL/Art/Animation";
        private static readonly string[] AtlasNames =
        {
            "operator_combat",
            "operator_portrait_icon",
            "enemy_combat",
            "raid_boss_combat",
            "sprite_sheet_vfx"
        };

        [Test]
        public void LaunchPipeline_CreatesSplitAtlasAssetsForPrimaryAndLowEndBudgets()
        {
            foreach (var atlasName in AtlasNames)
            {
                AssertAtlas(atlasName, 2048);
                AssertAtlas(atlasName, 1024);
            }
        }

        [Test]
        public void LaunchPipeline_CreatesAnimatorControllersAndMinimumClipsForOperatorsEnemiesAndRaidBoss()
        {
            var manifest = ActOneAnimationAssetManifestFixtures.CreateMinimumLaunchCandidate();
            var operatorCount = 0;
            var enemyCount = 0;
            var raidBossCount = 0;

            foreach (var entry in manifest.Entries)
            {
                switch (entry.Role)
                {
                    case AnimationAssetRole.Operator:
                        operatorCount++;
                        AssertController("Operators", entry.AssetId);
                        AssertClips("Operators", entry);
                        break;
                    case AnimationAssetRole.Enemy:
                        enemyCount++;
                        AssertController("Enemies", entry.AssetId);
                        AssertClips("Enemies", entry);
                        break;
                    case AnimationAssetRole.RaidBoss:
                        raidBossCount++;
                        AssertController("RaidBosses", entry.AssetId);
                        AssertClips("RaidBosses", entry);
                        break;
                }
            }

            Assert.That(operatorCount, Is.EqualTo(15));
            Assert.That(enemyCount, Is.EqualTo(1));
            Assert.That(raidBossCount, Is.EqualTo(1));
        }

        [Test]
        public void LaunchPipeline_CreatesSpriteSheetVfxPlaceholderClipsForApprovedBudgetCues()
        {
            var requiredVfxClips = new[]
            {
                "vfx_large_impact",
                "vfx_hit_spark",
                "vfx_full_screen_flash",
                "vfx_screen_shake_small",
                "vfx_screen_shake_medium",
                "vfx_screen_shake_heavy",
                "vfx_boss_phase"
            };

            foreach (var clipId in requiredVfxClips)
            {
                var path = PipelineRoot + "/Clips/VFX/" + clipId + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                Assert.That(clip, Is.Not.Null, path);
            }
        }

        [Test]
        public void LaunchPipeline_AuditJsonUsesIdsAndCountsWithoutRawArtServerOrSensitivePayload()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(PipelineRoot + "/launch_animation_pipeline_readiness.json");

            Assert.That(json, Is.Not.Null);
            Assert.That(json.text.Contains("\"operatorControllerCount\":15"), Is.EqualTo(true));
            Assert.That(json.text.Contains("\"primaryAtlasMaxSize\":2048"), Is.EqualTo(true));
            Assert.That(json.text.Contains("\"lowEndFallbackAtlasSize\":1024"), Is.EqualTo(true));
            Assert.That(json.text.Contains("\"largeVfxConcurrentLimit\":2"), Is.EqualTo(true));
            Assert.That(json.text.Contains("\"hitSparkConcurrentMin\":8"), Is.EqualTo(true));
            Assert.That(json.text.Contains("\"hitSparkConcurrentMax\":12"), Is.EqualTo(true));
            Assert.That(json.text.Contains("\"fullScreenFlashMaxSeconds\":0.15"), Is.EqualTo(true));
            Assert.That(json.text.Contains("\"bossPhaseVfxMaxSeconds\":2"), Is.EqualTo(true));
            AssertForbidden(json.text, new[]
            {
                "rawImage",
                "rawTexture",
                "texturePath",
                "rewardAmount",
                "accountId",
                "accessToken",
                "refreshToken",
                "rawRng",
                "probability",
                "animationTimeline",
                "frameData",
                "serverSubmit",
                "RailChoice",
                "WorldRaid"
            });
        }

        private static void AssertAtlas(string atlasName, int maxSize)
        {
            var path = PipelineRoot + "/Atlases/" + atlasName + "_" + maxSize + ".spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            Assert.That(atlas, Is.Not.Null, path);
        }

        private static void AssertController(string roleFolder, string assetId)
        {
            var path = PipelineRoot + "/Animators/" + roleFolder + "/" + assetId + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            Assert.That(controller, Is.Not.Null, path);
        }

        private static void AssertClips(string roleFolder, AnimationAssetManifestEntry entry)
        {
            for (var i = 0; i < entry.AnimationKeys.Count; i++)
            {
                var path = PipelineRoot + "/Clips/" + roleFolder + "/" + entry.AssetId + "/" + entry.AssetId + "_" + entry.AnimationKeys[i] + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                Assert.That(clip, Is.Not.Null, path);
            }
        }

        private static void AssertForbidden(string value, IReadOnlyList<string> forbidden)
        {
            for (var i = 0; i < forbidden.Count; i++)
            {
                Assert.That(value.Contains(forbidden[i]), Is.EqualTo(false), forbidden[i]);
            }
        }
    }
}
#endif
