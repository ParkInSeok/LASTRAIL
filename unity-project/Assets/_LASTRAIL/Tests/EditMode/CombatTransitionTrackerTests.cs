using System.Collections.Generic;
using LastRail.Presentation.Battle;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatTransitionTrackerTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void FirstLoadAndRepeatedRender_DoNotReplay(bool boss)
        {
            var tracker = new CombatHudTransitionTracker();
            Assert.That(tracker.Observe(State(2, boss)), Is.Null);
            Assert.That(tracker.Observe(State(2, boss)), Is.Null);
            var transition = tracker.Observe(State(3, boss));
            Assert.That(transition, Is.Not.Null);
            Assert.That(transition.Number, Is.EqualTo(3));
            Assert.That(transition.Kind, Is.EqualTo(boss ? CombatHudTransitionKind.BossPhaseChanged : CombatHudTransitionKind.WaveStarted));
            Assert.That(tracker.Observe(State(3, boss)), Is.Null);
        }

        [Test]
        public void SkippedAndRolledBackSnapshots_AreNotNewEvents()
        {
            var tracker = new CombatHudTransitionTracker();
            tracker.Observe(State(1));
            Assert.That(tracker.Observe(State(5)), Is.Null);
            Assert.That(tracker.RequiresClear, Is.True);
            Assert.That(tracker.Observe(State(4)), Is.Null);
            Assert.That(tracker.Observe(State(5)), Is.Null);
            Assert.That(tracker.Observe(State(6)), Is.Not.Null);
        }

        [Test]
        public void ReentryAndMissionOrBossChanges_EstablishBaselineOnly()
        {
            var tracker = new CombatHudTransitionTracker();
            tracker.Observe(State(1));
            tracker.Observe(State(2));
            tracker.Reset();
            Assert.That(tracker.Observe(State(2)), Is.Null);
            Assert.That(tracker.Observe(State(3, false, "other_mission")), Is.Null);
            Assert.That(tracker.RequiresClear, Is.True);
            Assert.That(tracker.Observe(State(1, true)), Is.Null);
            Assert.That(tracker.Observe(State(2, true, null, "another_boss")), Is.Null);
        }

        [Test]
        public void LastWaveAndCueChange_DoNotInventCompletionOrOutcome()
        {
            var tracker = new CombatHudTransitionTracker();
            tracker.Observe(State(14));
            Assert.That(tracker.Observe(State(15)).Kind, Is.EqualTo(CombatHudTransitionKind.WaveStarted));
            Assert.That(tracker.Observe(State(15, false, null, null, "different_preset")), Is.Null);
        }

        public static FullyAnimatedCombatHudViewState State(int number, bool boss = false,
            string mission = null, string bossId = null, string transitionCue = null)
        {
            mission = mission ?? (boss ? "raid_raider_king_engine" : "opn_01_last_departure");
            bossId = bossId ?? "boss_raider_king_engine";
            var units = new List<CombatUnitAnimationState>();
            foreach (var id in new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" })
                units.Add(new CombatUnitAnimationState(id, CombatUnitRole.Operator, "idle", "operator_idle"));
            units.Add(new CombatUnitAnimationState(boss ? bossId : "enemy_raider_scout",
                boss ? CombatUnitRole.RaidBoss : CombatUnitRole.Enemy, "idle", "idle_cue"));
            var combat = boss
                ? FullyAnimatedCombatPresentationState.CreateRaidBoss(mission, number, bossId, null, null, units,
                    CombatPresentationCueSet.ForRaidBossPhase("damage", "flash", "stop", "shake", "vfx_story_raid_danger",
                        "combat_hit", "vibration", transitionCue ?? "boss_phase_transition"))
                : FullyAnimatedCombatPresentationState.CreateOrdinary15Wave(mission, number, null, null, units,
                    CombatPresentationCueSet.ForOrdinaryWave("damage", "flash", "stop", "shake", "vfx_basic_hit_spark",
                        "combat_hit", "vibration", transitionCue ?? "wave_transition_ordinary"));
            return FullyAnimatedCombatHudViewState.FromCombatState(combat,
                MobileCombatHudPresentationPolicy.FromCombatState(combat, MobileSafeAreaInsets.Portrait(1080, 1920, 64, 34)));
        }
    }
}
