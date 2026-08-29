using System.Collections.Generic;

namespace LastRail.Presentation.Animation
{
    public static class AnimationAssetCoverageAudit
    {
        private static readonly string[] ActOneInitialOperatorIds =
        {
            "op_kyle",
            "op_reina",
            "op_eve",
            "op_sera",
            "op_mira"
        };

        private static readonly string[] ActOneGachaEliteOperatorIds =
        {
            "op_vex",
            "op_nyx",
            "op_sable",
            "op_marrow",
            "op_iris",
            "op_rail",
            "op_anchor",
            "op_lumen",
            "op_cinder",
            "op_warden"
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

            for (var i = 0; i < ActOneInitialOperatorIds.Length; i++)
            {
                AddMissingKeys(manifest.Find(ActOneInitialOperatorIds[i], AnimationAssetRole.Operator), ActOneInitialOperatorIds[i], AnimationAssetRole.Operator, OperatorRequiredKeys, missing);
            }

            for (var i = 0; i < ActOneGachaEliteOperatorIds.Length; i++)
            {
                AddMissingKeys(manifest.Find(ActOneGachaEliteOperatorIds[i], AnimationAssetRole.Operator), ActOneGachaEliteOperatorIds[i], AnimationAssetRole.Operator, OperatorRequiredKeys, missing);
            }

            AddMissingKeys(manifest.Find("enemy_raider_grunt", AnimationAssetRole.Enemy), "enemy_raider_grunt", AnimationAssetRole.Enemy, EnemyRequiredKeys, missing);
            AddMissingKeys(manifest.Find("boss_raider_king_engine", AnimationAssetRole.RaidBoss), "boss_raider_king_engine", AnimationAssetRole.RaidBoss, RaidBossRequiredKeys, missing);

            return new AnimationAssetCoverageAuditResult(
                operatorCount,
                CountPresentOperators(manifest, ActOneInitialOperatorIds),
                CountPresentOperators(manifest, ActOneGachaEliteOperatorIds),
                enemyCount,
                raidBossCount,
                missing);
        }

        private static int CountPresentOperators(AnimationAssetManifest manifest, IReadOnlyList<string> operatorIds)
        {
            var count = 0;
            for (var i = 0; i < operatorIds.Count; i++)
            {
                if (manifest.Find(operatorIds[i], AnimationAssetRole.Operator) != null)
                {
                    count++;
                }
            }

            return count;
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
