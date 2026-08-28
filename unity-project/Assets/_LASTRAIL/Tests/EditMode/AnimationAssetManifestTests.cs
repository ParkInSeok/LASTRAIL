using System;
using LastRail.Presentation.Animation;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class AnimationAssetManifestTests
    {
        [Test]
        public void AuditActOneLaunchManifest_RequiresTenOperatorsAndMinimumCombatAnimationKeys()
        {
            var manifest = ActOneAnimationAssetManifestFixtures.CreateMinimumLaunchCandidate();

            var audit = AnimationAssetCoverageAudit.AuditActOneLaunch(manifest);

            Assert.That(audit.IsComplete, Is.EqualTo(true));
            Assert.That(audit.OperatorCount, Is.EqualTo(10));
            Assert.That(audit.EnemyCount, Is.EqualTo(1));
            Assert.That(audit.RaidBossCount, Is.EqualTo(1));
            Assert.That(audit.MissingRequirements.Count, Is.EqualTo(0));
        }

        [Test]
        public void AuditActOneLaunchManifest_ReportsMissingOperatorRequiredAnimationKey()
        {
            var manifest = ActOneAnimationAssetManifestFixtures.CreateMinimumLaunchCandidate()
                .WithAnimationKeyRemoved("op_vex", "ultimate_cue");

            var audit = AnimationAssetCoverageAudit.AuditActOneLaunch(manifest);

            Assert.That(audit.IsComplete, Is.EqualTo(false));
            Assert.That(audit.MissingRequirements.Count, Is.EqualTo(1));
            Assert.That(audit.MissingRequirements[0].AssetId, Is.EqualTo("op_vex"));
            Assert.That(audit.MissingRequirements[0].Role, Is.EqualTo(AnimationAssetRole.Operator));
            Assert.That(audit.MissingRequirements[0].MissingKey, Is.EqualTo("ultimate_cue"));
        }

        [Test]
        public void AuditActOneLaunchManifest_ReportsMissingEnemyAndRaidBossRequiredAnimationKeys()
        {
            var manifest = ActOneAnimationAssetManifestFixtures.CreateMinimumLaunchCandidate()
                .WithAnimationKeyRemoved("enemy_raider_grunt", "down_or_destroyed")
                .WithAnimationKeyRemoved("boss_raider_king_engine", "phase_transition");

            var audit = AnimationAssetCoverageAudit.AuditActOneLaunch(manifest);

            Assert.That(audit.IsComplete, Is.EqualTo(false));
            Assert.That(audit.MissingRequirements.Count, Is.EqualTo(2));
            Assert.That(audit.MissingRequirements[0].AssetId, Is.EqualTo("enemy_raider_grunt"));
            Assert.That(audit.MissingRequirements[0].MissingKey, Is.EqualTo("down_or_destroyed"));
            Assert.That(audit.MissingRequirements[1].AssetId, Is.EqualTo("boss_raider_king_engine"));
            Assert.That(audit.MissingRequirements[1].MissingKey, Is.EqualTo("phase_transition"));
        }

        [Test]
        public void ManifestEntry_RejectsEmptyIdsAndDefensivelyCopiesAnimationKeys()
        {
            var keys = new[] { "idle_loop", "hit" };
            var entry = new AnimationAssetManifestEntry(
                "op_kyle",
                AnimationAssetRole.Operator,
                "portrait_op_kyle",
                "icon_op_kyle",
                "combat_op_kyle",
                keys);

            keys[0] = "mutated";

            Assert.That(entry.AnimationKeys[0], Is.EqualTo("idle_loop"));
            Assert.Throws<ArgumentException>(() => new AnimationAssetManifestEntry(
                " ",
                AnimationAssetRole.Operator,
                "portrait_op_kyle",
                "icon_op_kyle",
                "combat_op_kyle",
                keys));
            Assert.Throws<ArgumentException>(() => new AnimationAssetManifestEntry(
                "op_kyle",
                AnimationAssetRole.Operator,
                " ",
                "icon_op_kyle",
                "combat_op_kyle",
                keys));
        }

        [Test]
        public void DebugJson_WritesStableCountsAndMissingKeysWithoutRawAssetPayload()
        {
            var manifest = ActOneAnimationAssetManifestFixtures.CreateMinimumLaunchCandidate()
                .WithAnimationKeyRemoved("op_vex", "ultimate_cue");
            var audit = AnimationAssetCoverageAudit.AuditActOneLaunch(manifest);

            var json = AnimationAssetCoverageAuditJsonAdapter.ToJson(audit);

            Assert.That(json, Is.EqualTo(
                "{\"isComplete\":false," +
                "\"operatorCount\":10," +
                "\"enemyCount\":1," +
                "\"raidBossCount\":1," +
                "\"missingRequirementCount\":1," +
                "\"missingRequirements\":[{\"assetId\":\"op_vex\",\"role\":\"Operator\",\"missingKey\":\"ultimate_cue\"}]," +
                "\"hasRawArtPayload\":false}"));
            Assert.That(json.Contains("rawImage"), Is.EqualTo(false));
            Assert.That(json.Contains("rawTexture"), Is.EqualTo(false));
            Assert.That(json.Contains("texturePath"), Is.EqualTo(false));
            Assert.That(json.Contains("assetPath"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("secret"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
        }
    }

    internal static class AnimationAssetManifestTestExtensions
    {
        public static AnimationAssetManifest WithAnimationKeyRemoved(
            this AnimationAssetManifest manifest,
            string assetId,
            string animationKey)
        {
            var entries = new AnimationAssetManifestEntry[manifest.Entries.Count];
            for (var i = 0; i < manifest.Entries.Count; i++)
            {
                var entry = manifest.Entries[i];
                entries[i] = string.Equals(entry.AssetId, assetId, StringComparison.Ordinal)
                    ? new AnimationAssetManifestEntry(
                        entry.AssetId,
                        entry.Role,
                        entry.PortraitAssetId,
                        entry.IconAssetId,
                        entry.CombatSpriteOrRigKey,
                        RemoveKey(entry.AnimationKeys, animationKey))
                    : entry;
            }

            return new AnimationAssetManifest(entries);
        }

        private static string[] RemoveKey(System.Collections.Generic.IReadOnlyList<string> keys, string animationKey)
        {
            var kept = new System.Collections.Generic.List<string>();
            for (var i = 0; i < keys.Count; i++)
            {
                if (!string.Equals(keys[i], animationKey, StringComparison.Ordinal))
                {
                    kept.Add(keys[i]);
                }
            }

            return kept.ToArray();
        }
    }
}
