using System;
using NUnit.Framework;
using LastRail.Presentation.Battle;

namespace LastRail.Tests.EditMode
{
    public sealed class FullyAnimatedCombatHudPresenterTests
    {
        [Test]
        public void RenderOrdinary15Wave_BuildsWaveHudUnitAnimationAndCueRefs()
        {
            var view = new CapturingCombatHudView();
            var presenter = new FullyAnimatedCombatHudPresenter(view);
            var combat = FullyAnimatedCombatPresentationState.CreateOrdinary15Wave(
                "opn_01_last_departure",
                7,
                "aug_pkg_launch_001",
                "tactic_pkg_launch_001",
                Units(),
                OrdinaryCues());
            var policy = MobileCombatHudPresentationPolicy.FromCombatState(
                combat,
                MobileSafeAreaInsets.Portrait(1080, 1920, 72, 48));

            presenter.Render(combat, policy);

            Assert.That(view.LastState.Surface, Is.EqualTo(MobileCombatHudSurface.Ordinary15Wave));
            Assert.That(view.LastState.EncounterTitle, Is.EqualTo("15공세 작전"));
            Assert.That(view.LastState.WaveCounterLabel, Is.EqualTo("웨이브 7/15"));
            Assert.That(view.LastState.BossObjectiveLabel, Is.EqualTo(null));
            Assert.That(view.LastState.OperatorSlotCount, Is.EqualTo(2));
            Assert.That(view.LastState.EnemyUnitCount, Is.EqualTo(1));
            Assert.That(view.LastState.RaidBossUnitCount, Is.EqualTo(0));
            Assert.That(view.LastState.UnitAnimationRefs[0].UnitId, Is.EqualTo("op_vex"));
            Assert.That(view.LastState.UnitAnimationRefs[0].AnimationKey, Is.EqualTo("attack_or_cast"));
            Assert.That(view.LastState.SkillCueId, Is.EqualTo("skill_cue_operator"));
            Assert.That(view.LastState.UltimateCueId, Is.EqualTo("ultimate_cue_operator"));
            Assert.That(view.LastState.DamageCueId, Is.EqualTo("damage_number_pop"));
            Assert.That(view.LastState.HitStopCueId, Is.EqualTo("hit_stop_medium"));
            Assert.That(view.LastState.WaveTransitionCueId, Is.EqualTo("wave_transition_ordinary"));
            Assert.That(view.LastState.UsesPortraitSafeArea, Is.EqualTo(true));
            Assert.That(view.LastState.MinimumTouchTargetDp, Is.EqualTo(48));
            Assert.That(view.LastState.KeepsHudAboveAnimationLayer, Is.EqualTo(true));
            Assert.That(view.LastState.HasAnimationTimelinePayload, Is.EqualTo(false));
            Assert.That(view.LastState.HasFramePayload, Is.EqualTo(false));
            Assert.That(view.LastState.HasServerTruthMutation, Is.EqualTo(false));
        }

        [Test]
        public void RenderStoryRaidBoss_BuildsBossHudObjectiveAndRefundHintWithoutTruthMutation()
        {
            var view = new CapturingCombatHudView();
            var presenter = new FullyAnimatedCombatHudPresenter(view);
            var combat = FullyAnimatedCombatPresentationState.CreateRaidBoss(
                "raid_raider_king_engine",
                2,
                "boss_crown_engine",
                "aug_pkg_raid_001",
                "tactic_pkg_raid_001",
                RaidUnits(),
                RaidCues());
            var policy = MobileCombatHudPresentationPolicy.FromCombatState(
                combat,
                MobileSafeAreaInsets.Portrait(1080, 1920, 72, 48));

            presenter.Render(combat, policy);

            Assert.That(view.LastState.Surface, Is.EqualTo(MobileCombatHudSurface.RaidBoss));
            Assert.That(view.LastState.EncounterTitle, Is.EqualTo("스토리 레이드 보스전"));
            Assert.That(view.LastState.WaveCounterLabel, Is.EqualTo(null));
            Assert.That(view.LastState.BossId, Is.EqualTo("boss_crown_engine"));
            Assert.That(view.LastState.BossPhaseLabel, Is.EqualTo("Phase 2"));
            Assert.That(view.LastState.BossStatusPlaceholder, Is.EqualTo("보스: Phase 2"));
            Assert.That(view.LastState.BossObjectiveLabel, Is.EqualTo("목표: 보스 격파"));
            Assert.That(view.LastState.FailureStaminaRefundHint, Is.EqualTo("철수 시 연료 보호"));
            Assert.That(view.LastState.OperatorSlotCount, Is.EqualTo(5));
            Assert.That(view.LastState.RaidBossUnitCount, Is.EqualTo(1));
            Assert.That(view.LastState.BossPhaseTransitionCueId, Is.EqualTo("boss_phase_transition"));
            Assert.That(view.LastState.HasRewardCalculation, Is.EqualTo(false));
            Assert.That(view.LastState.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(view.LastState.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void ToDebugJson_WritesStableFieldsWithoutRawTimelineServerPayloadOrSensitiveTruth()
        {
            var view = new CapturingCombatHudView();
            var presenter = new FullyAnimatedCombatHudPresenter(view);
            var combat = FullyAnimatedCombatPresentationState.CreateRaidBoss(
                "raid_raider_king_engine",
                3,
                "boss_crown_engine",
                "aug_pkg_raid_001",
                "tactic_pkg_raid_001",
                RaidUnits(),
                RaidCues());
            var state = presenter.Render(
                combat,
                MobileCombatHudPresentationPolicy.FromCombatState(
                    combat,
                    MobileSafeAreaInsets.Portrait(1080, 1920, 72, 48)));

            var json = FullyAnimatedCombatHudViewStateJsonAdapter.ToJson(state);

            Assert.That(json.Contains("\"surface\":\"RaidBoss\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"bossObjectiveLabel\":\"목표: 보스 격파\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"minimumTouchTargetDp\":48"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasAnimationTimelinePayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasFramePayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasServerTruthMutation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(json.Contains("frameData"), Is.EqualTo(false));
            Assert.That(json.Contains("cameraEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
        }

        private static CombatUnitAnimationState[] Units()
        {
            return new[]
            {
                new CombatUnitAnimationState("op_vex", CombatUnitRole.Operator, "attack_or_cast", "skill_cue_operator"),
                new CombatUnitAnimationState("op_rin", CombatUnitRole.Operator, "ultimate_cue", "ultimate_cue_operator"),
                new CombatUnitAnimationState("enemy_raider", CombatUnitRole.Enemy, "hit", "enemy_hit_flash")
            };
        }

        private static CombatUnitAnimationState[] RaidUnits()
        {
            return new[]
            {
                new CombatUnitAnimationState("op_vex", CombatUnitRole.Operator, "idle_loop", "operator_idle"),
                new CombatUnitAnimationState("op_rin", CombatUnitRole.Operator, "idle_loop", "operator_idle"),
                new CombatUnitAnimationState("op_nyx", CombatUnitRole.Operator, "idle_loop", "operator_idle"),
                new CombatUnitAnimationState("op_cipher", CombatUnitRole.Operator, "idle_loop", "operator_idle"),
                new CombatUnitAnimationState("op_iris", CombatUnitRole.Operator, "idle_loop", "operator_idle"),
                new CombatUnitAnimationState("boss_crown_engine", CombatUnitRole.RaidBoss, "boss_phase_attack", "boss_attack_cue")
            };
        }

        private static CombatPresentationCueSet OrdinaryCues()
        {
            return CombatPresentationCueSet.ForOrdinaryWave(
                "damage_number_pop",
                "hit_flash",
                "hit_stop_medium",
                "screen_shake_medium",
                "vfx_hit_spark",
                "sfx_hit",
                "vibration_medium",
                "wave_transition_ordinary");
        }

        private static CombatPresentationCueSet RaidCues()
        {
            return CombatPresentationCueSet.ForRaidBossPhase(
                "damage_number_pop",
                "hit_flash",
                "hit_stop_heavy",
                "screen_shake_heavy",
                "vfx_boss_phase",
                "sfx_boss_phase",
                "vibration_heavy",
                "boss_phase_transition");
        }

        private sealed class CapturingCombatHudView : IFullyAnimatedCombatHudView
        {
            public FullyAnimatedCombatHudViewState LastState { get; private set; }

            public void Render(FullyAnimatedCombatHudViewState state)
            {
                LastState = state;
            }
        }
    }
}
