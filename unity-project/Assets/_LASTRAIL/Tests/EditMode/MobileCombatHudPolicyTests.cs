using LastRail.Presentation.Battle;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class MobileCombatHudPolicyTests
    {
        [Test]
        public void ForOrdinary15Wave_UsesPortraitSafeAreaAndWaveHudOverAnimatedCombat()
        {
            var policy = MobileCombatHudPresentationPolicy.ForOrdinary15Wave(
                missionId: "opn_03_water_stop",
                currentWaveNumber: 7,
                viewportWidth: 1080,
                viewportHeight: 1920,
                safeAreaTop: 48,
                safeAreaBottom: 36);

            Assert.That(policy.Surface, Is.EqualTo(MobileCombatHudSurface.Ordinary15Wave));
            Assert.That(policy.EncounterType, Is.EqualTo(RunEncounterType.Ordinary15Wave));
            Assert.That(policy.MissionId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(policy.IsPortraitNineBySixteen, Is.EqualTo(true));
            Assert.That(policy.SafeArea.Top, Is.EqualTo(48));
            Assert.That(policy.SafeArea.Bottom, Is.EqualTo(36));
            Assert.That(policy.BattlefieldRegionKey, Is.EqualTo("top_battlefield"));
            Assert.That(policy.TacticalRegionKey, Is.EqualTo("bottom_current_car_tactical"));
            Assert.That(policy.HudLayerKey, Is.EqualTo("hud_layer_over_fully_animated_combat"));
            Assert.That(policy.WaveHud.CurrentWaveNumber, Is.EqualTo(7));
            Assert.That(policy.WaveHud.TotalWaveCount, Is.EqualTo(15));
            Assert.That(policy.WaveHud.HasWaveCounter, Is.EqualTo(true));
            Assert.That(policy.BossHud.HasBossPhase, Is.EqualTo(false));
            Assert.That(policy.Touch.MinimumTouchTargetDp, Is.EqualTo(48));
            Assert.That(policy.Touch.RequiresSafeAreaPadding, Is.EqualTo(true));
            Assert.That(policy.Readability.PreventsTextOverflow, Is.EqualTo(true));
            Assert.That(policy.Readability.KeepsHudAboveAnimationLayer, Is.EqualTo(true));
            Assert.That(policy.Vibration.LightHitCueId, Is.EqualTo("vibration_light_hit"));
            Assert.That(policy.Vibration.SkillCueId, Is.EqualTo("vibration_skill_ready"));
            Assert.That(policy.IsPresentationOnly, Is.EqualTo(true));
            Assert.That(policy.HasServerTruthMutation, Is.EqualTo(false));
            Assert.That(policy.HasAnimationTimelinePayload, Is.EqualTo(false));
            Assert.That(policy.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(policy.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void ForRaidBoss_UsesBossHudWithoutOrdinaryWaveCounter()
        {
            var policy = MobileCombatHudPresentationPolicy.ForRaidBoss(
                missionId: "raid_raider_king_engine",
                bossId: "boss_raider_king_engine",
                bossPhase: 2,
                viewportWidth: 1080,
                viewportHeight: 1920,
                safeAreaTop: 44,
                safeAreaBottom: 28);

            Assert.That(policy.Surface, Is.EqualTo(MobileCombatHudSurface.RaidBoss));
            Assert.That(policy.EncounterType, Is.EqualTo(RunEncounterType.RaidBoss));
            Assert.That(policy.MissionId, Is.EqualTo("raid_raider_king_engine"));
            Assert.That(policy.WaveHud.HasWaveCounter, Is.EqualTo(false));
            Assert.That(policy.BossHud.BossId, Is.EqualTo("boss_raider_king_engine"));
            Assert.That(policy.BossHud.BossPhase, Is.EqualTo(2));
            Assert.That(policy.BossHud.HasBossPhase, Is.EqualTo(true));
            Assert.That(policy.BossHud.BossHudKey, Is.EqualTo("boss_only_raid_hud"));
            Assert.That(policy.Vibration.BossPhaseCueId, Is.EqualTo("vibration_boss_phase"));
        }

        [Test]
        public void FromCombatState_RoutesOrdinaryAndRaidIntoTheSameMobilePolicyBoundary()
        {
            var ordinary = MobileCombatHudPresentationPolicy.FromCombatState(
                FullyAnimatedCombatPresentationState.CreateOrdinary15Wave(
                    "opn_01_last_departure",
                    3,
                    "augment_pkg",
                    "tactic_pkg",
                    new[] { new CombatUnitAnimationState("op_reina", CombatUnitRole.Operator, "idle_loop", "cue_idle") },
                    CombatPresentationCueSet.ForOrdinaryWave("d", "h", "s", "sh", "vfx", "sfx", "vib", "wave")),
                MobileSafeAreaInsets.Portrait(1080, 1920, 50, 30));
            var raid = MobileCombatHudPresentationPolicy.FromCombatState(
                FullyAnimatedCombatPresentationState.CreateRaidBoss(
                    "raid_raider_king_engine",
                    3,
                    "boss_raider_king_engine",
                    "augment_pkg",
                    "tactic_pkg",
                    new[] { new CombatUnitAnimationState("boss_raider_king_engine", CombatUnitRole.RaidBoss, "idle_loop", "cue_idle") },
                    CombatPresentationCueSet.ForRaidBossPhase("d", "h", "s", "sh", "vfx", "sfx", "vib", "phase")),
                MobileSafeAreaInsets.Portrait(1080, 1920, 50, 30));

            Assert.That(ordinary.Surface, Is.EqualTo(MobileCombatHudSurface.Ordinary15Wave));
            Assert.That(ordinary.WaveHud.CurrentWaveNumber, Is.EqualTo(3));
            Assert.That(raid.Surface, Is.EqualTo(MobileCombatHudSurface.RaidBoss));
            Assert.That(raid.BossHud.BossPhase, Is.EqualTo(3));
        }

        [Test]
        public void ToJson_WritesStablePolicyFieldsWithoutTransportTruthOrRawAnimationPayload()
        {
            var policy = MobileCombatHudPresentationPolicy.ForRaidBoss(
                missionId: "raid_raider_king_engine",
                bossId: "boss_raider_king_engine",
                bossPhase: 3,
                viewportWidth: 1080,
                viewportHeight: 1920,
                safeAreaTop: 52,
                safeAreaBottom: 34);

            var json = MobileCombatHudPresentationPolicyJsonAdapter.ToJson(policy);

            Assert.That(json.Contains("\"surface\":\"RaidBoss\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"safeArea\":{\"viewportWidth\":1080,\"viewportHeight\":1920,\"top\":52,\"bottom\":34"), Is.EqualTo(true));
            Assert.That(json.Contains("\"layout\":{\"battlefieldRegionKey\":\"top_battlefield\",\"tacticalRegionKey\":\"bottom_current_car_tactical\",\"hudLayerKey\":\"hud_layer_over_fully_animated_combat\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"bossHud\":{\"bossHudKey\":\"boss_only_raid_hud\",\"bossId\":\"boss_raider_king_engine\",\"hasBossPhase\":true,\"bossPhase\":3"), Is.EqualTo(true));
            Assert.That(json.Contains("\"touch\":{\"minimumTouchTargetDp\":48,\"requiresSafeAreaPadding\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"vibration\":{\"isEnabledByDefault\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasAnimationTimelinePayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("UnityWebRequest"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmitPayload"), Is.EqualTo(false));
        }

        [Test]
        public void Constructors_RejectInvalidViewportSafeAreaAndHudInputs()
        {
            Assert.Throws<System.ArgumentException>(() => MobileSafeAreaInsets.Portrait(1000, 1000, 0, 0));
            Assert.Throws<System.ArgumentException>(() => MobileSafeAreaInsets.Portrait(1080, 1920, -1, 0));
            Assert.Throws<System.ArgumentException>(() => MobileCombatHudPresentationPolicy.ForOrdinary15Wave("opn_01", 0, 1080, 1920, 0, 0));
            Assert.Throws<System.ArgumentException>(() => MobileCombatHudPresentationPolicy.ForRaidBoss("raid", "boss", 0, 1080, 1920, 0, 0));
            Assert.Throws<System.ArgumentNullException>(() => MobileCombatHudPresentationPolicy.FromCombatState(null, MobileSafeAreaInsets.Portrait(1080, 1920, 0, 0)));
            Assert.Throws<System.ArgumentNullException>(() => MobileCombatHudPresentationPolicyJsonAdapter.ToJson(null));
        }
    }
}
