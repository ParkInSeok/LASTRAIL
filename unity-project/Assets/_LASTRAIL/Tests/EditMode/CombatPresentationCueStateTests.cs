using System;
using LastRail.Presentation.Battle;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatPresentationCueStateTests
    {
        [Test]
        public void Ordinary15Wave_KeepsWaveCueHudReadabilityAndStartPackageRefsAsPresentationData()
        {
            var state = FullyAnimatedCombatPresentationState.CreateOrdinary15Wave(
                missionId: "opn_03_water_stop",
                currentWaveNumber: 7,
                startAugmentPackageRef: "augment_pkg_act1_opn03_v1",
                startTacticPackageRef: "tactic_pkg_act1_opn03_v1",
                unitAnimationStates: new[]
                {
                    new CombatUnitAnimationState("op_reina", CombatUnitRole.Operator, "attack_or_cast", "cue_unit_op_reina_attack"),
                    new CombatUnitAnimationState("enemy_raider_grunt", CombatUnitRole.Enemy, "hit", "cue_unit_enemy_raider_grunt_hit")
                },
                presentationCues: CombatPresentationCueSet.ForOrdinaryWave(
                    damageCueId: "cue_damage_number_pop",
                    hitFlashCueId: "cue_hit_flash_red",
                    hitStopCueId: "cue_hit_stop_short",
                    screenShakeCueId: "cue_screen_shake_light",
                    vfxCueId: "cue_vfx_muzzle_flash",
                    sfxCueId: "cue_sfx_railgun_shot",
                    vibrationCueId: "cue_vibration_light",
                    waveTransitionCueId: "cue_wave_07_to_08"));

            Assert.That(state.EncounterType, Is.EqualTo(RunEncounterType.Ordinary15Wave));
            Assert.That(state.MissionId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(state.CurrentWaveNumber, Is.EqualTo(7));
            Assert.That(state.HasBossPhase, Is.EqualTo(false));
            Assert.That(state.StartAugmentPackageRef, Is.EqualTo("augment_pkg_act1_opn03_v1"));
            Assert.That(state.StartTacticPackageRef, Is.EqualTo("tactic_pkg_act1_opn03_v1"));
            Assert.That(state.UnitAnimationStates.Count, Is.EqualTo(2));
            Assert.That(state.PresentationCues.WaveTransitionCueId, Is.EqualTo("cue_wave_07_to_08"));
            Assert.That(state.PresentationCues.BossPhaseTransitionCueId, Is.EqualTo(null));
            Assert.That(state.HudReadability.HasSeparateHudLayer, Is.EqualTo(true));
            Assert.That(state.HudReadability.KeepsTouchControlsReadable, Is.EqualTo(true));
            Assert.That(state.IsPresentationOnly, Is.EqualTo(true));
            Assert.That(state.HasServerTruthMutation, Is.EqualTo(false));
            Assert.That(state.HasRawTimelinePayload, Is.EqualTo(false));
            Assert.That(state.HasFramePayload, Is.EqualTo(false));
            Assert.That(state.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(state.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void RaidBoss_KeepsBossPhaseCueSeparateFromOrdinaryWaveTransition()
        {
            var state = FullyAnimatedCombatPresentationState.CreateRaidBoss(
                missionId: "raid_raider_king_engine",
                bossPhase: 2,
                bossId: "boss_raider_king_engine",
                startAugmentPackageRef: "augment_pkg_act1_raid_v1",
                startTacticPackageRef: "tactic_pkg_act1_raid_v1",
                unitAnimationStates: new[]
                {
                    new CombatUnitAnimationState("op_vex", CombatUnitRole.Operator, "ultimate_cue", "cue_unit_op_vex_ultimate"),
                    new CombatUnitAnimationState("boss_raider_king_engine", CombatUnitRole.RaidBoss, "phase_shift", "cue_unit_boss_engine_phase_shift")
                },
                presentationCues: CombatPresentationCueSet.ForRaidBossPhase(
                    damageCueId: "cue_boss_damage_number_pop",
                    hitFlashCueId: "cue_boss_hit_flash",
                    hitStopCueId: "cue_hit_stop_heavy",
                    screenShakeCueId: "cue_screen_shake_heavy",
                    vfxCueId: "cue_vfx_engine_core_spark",
                    sfxCueId: "cue_sfx_engine_roar",
                    vibrationCueId: "cue_vibration_heavy",
                    bossPhaseTransitionCueId: "cue_boss_phase_02_to_03"));

            Assert.That(state.EncounterType, Is.EqualTo(RunEncounterType.RaidBoss));
            Assert.That(state.MissionId, Is.EqualTo("raid_raider_king_engine"));
            Assert.That(state.CurrentWaveNumber, Is.EqualTo(0));
            Assert.That(state.HasBossPhase, Is.EqualTo(true));
            Assert.That(state.BossPhase, Is.EqualTo(2));
            Assert.That(state.BossId, Is.EqualTo("boss_raider_king_engine"));
            Assert.That(state.PresentationCues.WaveTransitionCueId, Is.EqualTo(null));
            Assert.That(state.PresentationCues.BossPhaseTransitionCueId, Is.EqualTo("cue_boss_phase_02_to_03"));
        }

        [Test]
        public void JsonAdapter_WritesStableDebugFieldsWithoutRawPayloadOrClientTruth()
        {
            var state = FullyAnimatedCombatPresentationState.CreateRaidBoss(
                missionId: "raid_raider_king_engine",
                bossPhase: 3,
                bossId: "boss_raider_king_engine",
                startAugmentPackageRef: "augment_pkg_act1_raid_v1",
                startTacticPackageRef: "tactic_pkg_act1_raid_v1",
                unitAnimationStates: new[]
                {
                    new CombatUnitAnimationState("boss_raider_king_engine", CombatUnitRole.RaidBoss, "ultimate_cue", "cue_unit_boss_engine_final")
                },
                presentationCues: CombatPresentationCueSet.ForRaidBossPhase(
                    "cue_damage_boss",
                    "cue_hit_flash_boss",
                    "cue_hit_stop_boss",
                    "cue_shake_boss",
                    "cue_vfx_boss_core",
                    "cue_sfx_boss_core",
                    "cue_vibration_boss",
                    "cue_boss_phase_final"));

            var json = FullyAnimatedCombatPresentationStateJsonAdapter.ToJson(state);

            const string expected =
                "{\"encounterType\":\"RaidBoss\"," +
                "\"missionId\":\"raid_raider_king_engine\"," +
                "\"currentWaveNumber\":0," +
                "\"hasBossPhase\":true," +
                "\"bossPhase\":3," +
                "\"bossId\":\"boss_raider_king_engine\"," +
                "\"startAugmentPackageRef\":\"augment_pkg_act1_raid_v1\"," +
                "\"startTacticPackageRef\":\"tactic_pkg_act1_raid_v1\"," +
                "\"hudReadability\":{\"hasSeparateHudLayer\":true,\"keepsTouchControlsReadable\":true,\"prioritizesReadableCombatText\":true}," +
                "\"presentationCues\":{\"damageCueId\":\"cue_damage_boss\",\"hitFlashCueId\":\"cue_hit_flash_boss\",\"hitStopCueId\":\"cue_hit_stop_boss\",\"screenShakeCueId\":\"cue_shake_boss\",\"vfxCueId\":\"cue_vfx_boss_core\",\"sfxCueId\":\"cue_sfx_boss_core\",\"vibrationCueId\":\"cue_vibration_boss\",\"waveTransitionCueId\":null,\"bossPhaseTransitionCueId\":\"cue_boss_phase_final\"}," +
                "\"unitAnimationStates\":[{\"unitId\":\"boss_raider_king_engine\",\"unitRole\":\"RaidBoss\",\"animationKey\":\"ultimate_cue\",\"animationCueId\":\"cue_unit_boss_engine_final\"}]," +
                "\"isPresentationOnly\":true," +
                "\"hasServerTruthMutation\":false," +
                "\"hasRawTimelinePayload\":false," +
                "\"hasFramePayload\":false," +
                "\"hasEconomyMutation\":false," +
                "\"hasClientSideRng\":false}";

            Assert.That(json, Is.EqualTo(expected));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(json.Contains("frameData"), Is.EqualTo(false));
            Assert.That(json.Contains("vfxEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("sfxEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("cameraEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("hitStopEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
        }

        [Test]
        public void Constructors_DefendAgainstInvalidCueInputs()
        {
            Assert.Throws<ArgumentException>(() => new CombatUnitAnimationState(" ", CombatUnitRole.Operator, "idle_loop", "cue_idle"));
            Assert.Throws<ArgumentException>(() => new CombatUnitAnimationState("op_kyle", CombatUnitRole.Operator, " ", "cue_idle"));
            Assert.Throws<ArgumentException>(() => new CombatUnitAnimationState("op_kyle", CombatUnitRole.Operator, "idle_loop", " "));
            Assert.Throws<ArgumentNullException>(() => FullyAnimatedCombatPresentationState.CreateOrdinary15Wave(
                "opn_01_last_departure",
                1,
                null,
                null,
                null,
                CombatPresentationCueSet.ForOrdinaryWave("d", "h", "s", "sh", "vfx", "sfx", "vib", "w")));
            Assert.Throws<ArgumentException>(() => FullyAnimatedCombatPresentationState.CreateOrdinary15Wave(
                "opn_01_last_departure",
                0,
                null,
                null,
                new[] { new CombatUnitAnimationState("op_kyle", CombatUnitRole.Operator, "idle_loop", "cue_idle") },
                CombatPresentationCueSet.ForOrdinaryWave("d", "h", "s", "sh", "vfx", "sfx", "vib", "w")));
            Assert.Throws<ArgumentException>(() => FullyAnimatedCombatPresentationState.CreateRaidBoss(
                "raid_raider_king_engine",
                0,
                "boss_raider_king_engine",
                null,
                null,
                new[] { new CombatUnitAnimationState("boss_raider_king_engine", CombatUnitRole.RaidBoss, "idle_loop", "cue_idle") },
                CombatPresentationCueSet.ForRaidBossPhase("d", "h", "s", "sh", "vfx", "sfx", "vib", "phase")));
        }
    }
}
