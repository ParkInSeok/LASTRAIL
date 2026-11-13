using LastRail.Presentation.Battle;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatStatusCueTests
    {
        [TestCase("combat_shield", "shield")]
        [TestCase("sfx_combat_heal", "heal")]
        [TestCase("combat_buff", "buff")]
        [TestCase("sfx_combat_debuff", "debuff")]
        public void ExplicitAudioCue_MapsOnlyToPresentationEvent(string cue, string expected)
        {
            var state = CreateState("vfx_hit_spark", cue);
            Assert.That(state.StatusCueIds, Is.EqualTo(new[] { expected }));
        }

        [Test]
        public void AmbiguousAndUnknownCues_DoNotInventStatus()
        {
            Assert.That(CreateState("vfx_buff_debuff_status", "combat_hit").StatusCueIds, Is.Empty);
            Assert.That(CreateState("unknown_shield", "not_combat_debuff").StatusCueIds, Is.Empty);
        }

        [Test]
        public void ExplicitVisualAndAudioCues_DeduplicateAndPreserveDistinctEvents()
        {
            Assert.That(CreateState("vfx_shield_status", "combat_shield").StatusCueIds,
                Is.EqualTo(new[] { "shield" }));
            Assert.That(CreateState("vfx_shield_status", "combat_heal").StatusCueIds,
                Is.EqualTo(new[] { "shield", "heal" }));
        }

        public static FullyAnimatedCombatHudViewState CreateState(string vfx, string sfx, bool fullSquad = false)
        {
            var units = new System.Collections.Generic.List<CombatUnitAnimationState>();
            var operatorIds = fullSquad
                ? new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" }
                : new[] { "op_kyle" };
            foreach (var id in operatorIds)
                units.Add(new CombatUnitAnimationState(id, CombatUnitRole.Operator, "idle", "operator_idle"));
            if (fullSquad)
                foreach (var id in new[] { "enemy_raider_scout", "enemy_scrapshield_brute", "enemy_spike_cart_gunner", "enemy_signal_jammer_drone" })
                    units.Add(new CombatUnitAnimationState(id, CombatUnitRole.Enemy, "idle", "enemy_idle"));
            var combat = FullyAnimatedCombatPresentationState.CreateOrdinary15Wave(
                "opn_01_last_departure", 1, null, null,
                units,
                CombatPresentationCueSet.ForOrdinaryWave("damage", "flash", "stop", "shake", vfx, sfx, "vibration", "wave"));
            return FullyAnimatedCombatHudViewState.FromCombatState(combat,
                MobileCombatHudPresentationPolicy.FromCombatState(combat,
                    MobileSafeAreaInsets.Portrait(1080, 1920, 72, 48)));
        }
    }
}
