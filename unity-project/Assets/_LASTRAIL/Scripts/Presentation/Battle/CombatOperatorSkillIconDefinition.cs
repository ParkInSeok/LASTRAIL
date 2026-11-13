using System;

namespace LastRail.Presentation.Battle
{
    public sealed class CombatOperatorSkillIconDefinition
    {
        public CombatOperatorSkillIconDefinition(
            string operatorId,
            string displayLabel,
            string skillId,
            string sourceMotif,
            string iconPath)
        {
            OperatorId = RequireText(operatorId, nameof(operatorId));
            DisplayLabel = RequireText(displayLabel, nameof(displayLabel));
            SkillId = RequireText(skillId, nameof(skillId));
            SourceMotif = RequireText(sourceMotif, nameof(sourceMotif));
            IconPath = RequireText(iconPath, nameof(iconPath));
        }

        public string OperatorId { get; }
        public string DisplayLabel { get; }
        public string SkillId { get; }
        public string SourceMotif { get; }
        public string IconPath { get; }
        public bool HasGameplayEffectDefinition => false;
        public bool IsPresentationOnly => true;

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value must not be blank.", name);
            }

            return value;
        }
    }
}
