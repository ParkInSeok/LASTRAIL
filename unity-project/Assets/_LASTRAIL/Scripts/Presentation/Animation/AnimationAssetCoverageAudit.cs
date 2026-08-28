using System.Collections.Generic;

namespace LastRail.Presentation.Animation
{
    public static class AnimationAssetCoverageAudit
    {
        private static readonly string[] ActOneOperatorIds =
        {
            "op_kyle",
            "op_reina",
            "op_eve",
            "op_sera",
            "op_mira",
            "op_vex",
            "op_rin",
            "op_nyx",
            "op_cipher",
            "op_iris"
        };

        private static readonly string[] OperatorRequiredKeys =
        {
            "idle_loop",
            "move_or_slot_shift",
            "attack_or_cast",
            "hit",
            "down_or_retreat",
            "skill_cue",
            "ultimate_cue"
        };

        private static readonly string[] EnemyRequiredKeys =
        {
            "idle_loop",
            "move_or_advance",
            "attack_or_cast",
            "hit",
            "down_or_destroyed"
        };

        private static readonly string[] RaidBossRequiredKeys =
        {
            "idle_loop",
            "attack_or_cast",
            "hit",
            "phase_transition",
            "ultimate_cue",
            "down_or_destroyed"
        };

        public static AnimationAssetCoverageAuditResult AuditActOneLaunch(AnimationAssetManifest manifest)
        {
            var missing = new List<AnimationAssetMissingRequirement>();
            var operatorCount = 0;
            var enemyCount = 0;
            var raidBossCount = 0;

            if (manifest == null)
            {
                throw new System.ArgumentNullException(nameof(manifest));
            }

            for (var i = 0; i < manifest.Entries.Count; i++)
            {
                switch (manifest.Entries[i].Role)
                {
                    case AnimationAssetRole.Operator:
                        operatorCount++;
                        break;
                    case AnimationAssetRole.Enemy:
                        enemyCount++;
                        break;
                    case AnimationAssetRole.RaidBoss:
                        raidBossCount++;
                        break;
                }
            }

            for (var i = 0; i < ActOneOperatorIds.Length; i++)
            {
                AddMissingKeys(manifest.Find(ActOneOperatorIds[i], AnimationAssetRole.Operator), ActOneOperatorIds[i], AnimationAssetRole.Operator, OperatorRequiredKeys, missing);
            }

            AddMissingKeys(manifest.Find("enemy_raider_grunt", AnimationAssetRole.Enemy), "enemy_raider_grunt", AnimationAssetRole.Enemy, EnemyRequiredKeys, missing);
            AddMissingKeys(manifest.Find("boss_raider_king_engine", AnimationAssetRole.RaidBoss), "boss_raider_king_engine", AnimationAssetRole.RaidBoss, RaidBossRequiredKeys, missing);

            return new AnimationAssetCoverageAuditResult(operatorCount, enemyCount, raidBossCount, missing);
        }

        private static void AddMissingKeys(
            AnimationAssetManifestEntry entry,
            string assetId,
            AnimationAssetRole role,
            IReadOnlyList<string> requiredKeys,
            List<AnimationAssetMissingRequirement> missing)
        {
            for (var i = 0; i < requiredKeys.Count; i++)
            {
                if (entry == null || !entry.HasAnimationKey(requiredKeys[i]))
                {
                    missing.Add(new AnimationAssetMissingRequirement(assetId, role, requiredKeys[i]));
                }
            }
        }
    }
}
