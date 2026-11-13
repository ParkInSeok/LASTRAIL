using NUnit.Framework;
using LastRail.Presentation.Battle;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatOperatorSkillIconCatalogTests
    {
        [Test]
        public void CreateLaunchDefinitions_CoversInitialAndEliteOperatorsWithoutGameplayEffects()
        {
            var definitions = CombatOperatorSkillIconCatalog.CreateLaunchDefinitions();

            Assert.That(definitions.Count, Is.EqualTo(15));
            AssertDefinition(definitions[0], "op_kyle", "op_kyle_skill_or_special", "Assets/_LASTRAIL/UI/CombatResources/SkillIcons/op_kyle_skill_or_special.png");
            AssertDefinition(definitions[4], "op_mira", "op_mira_skill_or_special", "Assets/_LASTRAIL/UI/CombatResources/SkillIcons/op_mira_skill_or_special.png");
            AssertDefinition(definitions[5], "op_vex", "op_vex_skill_or_special", "Assets/_LASTRAIL/UI/CombatResources/SkillIcons/op_vex_skill_or_special.png");
            AssertDefinition(definitions[14], "op_warden", "op_warden_skill_or_special", "Assets/_LASTRAIL/UI/CombatResources/SkillIcons/op_warden_skill_or_special.png");

            foreach (var definition in definitions)
            {
                Assert.That(definition.SkillId, Does.EndWith("_skill_or_special"));
                Assert.That(definition.IconPath, Does.StartWith("Assets/_LASTRAIL/UI/CombatResources/SkillIcons/"));
                Assert.That(definition.HasGameplayEffectDefinition, Is.EqualTo(false));
                Assert.That(definition.IsPresentationOnly, Is.EqualTo(true));
            }
        }

        [Test]
        public void FromCombatState_ExposesSkillIconsForCurrentOperatorUnitsOnly()
        {
            var combat = FullyAnimatedCombatPresentationState.CreateOrdinary15Wave(
                "opn_01_last_departure",
                7,
                "aug_pkg_launch_001",
                "tactic_pkg_launch_001",
                new[]
                {
                    new CombatUnitAnimationState("op_kyle", CombatUnitRole.Operator, "idle", "operator_idle"),
                    new CombatUnitAnimationState("op_reina", CombatUnitRole.Operator, "skill_or_special", "operator_skill"),
                    new CombatUnitAnimationState("enemy_raider_scout", CombatUnitRole.Enemy, "attack", "enemy_attack")
                },
                CombatPresentationCueSet.ForOrdinaryWave(
                    "damage_number_pop",
                    "hit_flash",
                    "hit_stop_medium",
                    "screen_shake_medium",
                    "vfx_hit_spark",
                    "sfx_hit",
                    "vibration_medium",
                    "wave_transition_ordinary"));
            var state = FullyAnimatedCombatHudViewState.FromCombatState(
                combat,
                MobileCombatHudPresentationPolicy.FromCombatState(
                    combat,
                    MobileSafeAreaInsets.Portrait(1080, 1920, 72, 48)));

            Assert.That(state.OperatorSkillIconRefs.Count, Is.EqualTo(2));
            Assert.That(state.OperatorSkillIconRefs[0].OperatorId, Is.EqualTo("op_kyle"));
            Assert.That(state.OperatorSkillIconRefs[0].SkillId, Is.EqualTo("op_kyle_skill_or_special"));
            Assert.That(state.OperatorSkillIconRefs[1].OperatorId, Is.EqualTo("op_reina"));
            Assert.That(state.OperatorSkillIconRefs[1].IconPath, Is.EqualTo("Assets/_LASTRAIL/UI/CombatResources/SkillIcons/op_reina_skill_or_special.png"));
        }

        private static void AssertDefinition(CombatOperatorSkillIconDefinition definition, string operatorId, string skillId, string iconPath)
        {
            Assert.That(definition.OperatorId, Is.EqualTo(operatorId));
            Assert.That(definition.SkillId, Is.EqualTo(skillId));
            Assert.That(definition.IconPath, Is.EqualTo(iconPath));
        }
    }
}
