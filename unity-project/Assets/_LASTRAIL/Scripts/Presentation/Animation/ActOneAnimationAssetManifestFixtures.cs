namespace LastRail.Presentation.Animation
{
    public static class ActOneAnimationAssetManifestFixtures
    {
        private static readonly string[] OperatorAnimationKeys =
        {
            "idle_loop",
            "move_or_slot_shift",
            "attack_or_cast",
            "hit",
            "down_or_retreat",
            "skill_cue",
            "ultimate_cue"
        };

        private static readonly string[] EnemyAnimationKeys =
        {
            "idle_loop",
            "move_or_advance",
            "attack_or_cast",
            "hit",
            "down_or_destroyed"
        };

        private static readonly string[] RaidBossAnimationKeys =
        {
            "idle_loop",
            "attack_or_cast",
            "hit",
            "phase_transition",
            "ultimate_cue",
            "down_or_destroyed"
        };

        public static AnimationAssetManifest CreateMinimumLaunchCandidate()
        {
            return new AnimationAssetManifest(new[]
            {
                Operator("op_kyle"),
                Operator("op_reina"),
                Operator("op_eve"),
                Operator("op_sera"),
                Operator("op_mira"),
                Operator("op_vex"),
                Operator("op_nyx"),
                Operator("op_sable"),
                Operator("op_marrow"),
                Operator("op_iris"),
                Operator("op_rail"),
                Operator("op_anchor"),
                Operator("op_lumen"),
                Operator("op_cinder"),
                Operator("op_warden"),
                new AnimationAssetManifestEntry(
                    "enemy_raider_grunt",
                    AnimationAssetRole.Enemy,
                    "portrait_enemy_raider_grunt",
                    "icon_enemy_raider_grunt",
                    "combat_enemy_raider_grunt",
                    EnemyAnimationKeys),
                new AnimationAssetManifestEntry(
                    "boss_raider_king_engine",
                    AnimationAssetRole.RaidBoss,
                    "portrait_boss_raider_king_engine",
                    "icon_boss_raider_king_engine",
                    "combat_boss_raider_king_engine",
                    RaidBossAnimationKeys)
            });
        }

        private static AnimationAssetManifestEntry Operator(string operatorId)
        {
            return new AnimationAssetManifestEntry(
                operatorId,
                AnimationAssetRole.Operator,
                "portrait_" + operatorId,
                "icon_" + operatorId,
                "combat_" + operatorId,
                OperatorAnimationKeys);
        }
    }
}
