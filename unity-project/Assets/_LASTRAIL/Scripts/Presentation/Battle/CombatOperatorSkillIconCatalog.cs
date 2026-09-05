using System;
using System.Collections.Generic;
using LastRail.Operators;

namespace LastRail.Presentation.Battle
{
    public static class CombatOperatorSkillIconCatalog
    {
        private const string Root = "Assets/_LASTRAIL/UI/CombatResources/SkillIcons/";

        private static readonly CombatOperatorSkillIconDefinition[] LaunchDefinitions =
        {
            Definition(ActOneOperatorRosterRules.KyleOperatorId, "Kyle", "striker rail flare"),
            Definition(ActOneOperatorRosterRules.ReinaOperatorId, "Reina", "guard shield brake"),
            Definition(ActOneOperatorRosterRules.EveOperatorId, "Eve", "engineer spark wrench"),
            Definition(ActOneOperatorRosterRules.SeraOperatorId, "Sera", "medic pulse vial"),
            Definition(ActOneOperatorRosterRules.MiraOperatorId, "Mira", "support signal prism"),
            Definition(ActOneOperatorRosterRules.VexOperatorId, "Vex", "elite voltage blade"),
            Definition(ActOneOperatorRosterRules.NyxOperatorId, "Nyx", "shadow rail mark"),
            Definition(ActOneOperatorRosterRules.SableOperatorId, "Sable", "black steel counter"),
            Definition(ActOneOperatorRosterRules.MarrowOperatorId, "Marrow", "bone rivet breaker"),
            Definition(ActOneOperatorRosterRules.IrisOperatorId, "Iris", "optic targeting lens"),
            Definition(ActOneOperatorRosterRules.RailOperatorId, "Rail", "rail spike surge"),
            Definition(ActOneOperatorRosterRules.AnchorOperatorId, "Anchor", "anchor lock field"),
            Definition(ActOneOperatorRosterRules.LumenOperatorId, "Lumen", "lumen beacon burst"),
            Definition(ActOneOperatorRosterRules.CinderOperatorId, "Cinder", "cinder thermal charge"),
            Definition(ActOneOperatorRosterRules.WardenOperatorId, "Warden", "warden barricade seal")
        };

        public static IReadOnlyList<CombatOperatorSkillIconDefinition> CreateLaunchDefinitions()
        {
            var copy = new CombatOperatorSkillIconDefinition[LaunchDefinitions.Length];
            Array.Copy(LaunchDefinitions, copy, LaunchDefinitions.Length);
            return copy;
        }

        public static bool TryGetDefinition(string operatorId, out CombatOperatorSkillIconDefinition definition)
        {
            foreach (var candidate in LaunchDefinitions)
            {
                if (string.Equals(candidate.OperatorId, operatorId, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        private static CombatOperatorSkillIconDefinition Definition(string operatorId, string displayLabel, string motif)
        {
            var skillId = operatorId + "_skill_or_special";
            return new CombatOperatorSkillIconDefinition(
                operatorId,
                displayLabel,
                skillId,
                motif,
                Root + skillId + ".png");
        }
    }
}
