using System;

namespace LastRail.Narrative
{
    public static class LaunchNarrativeLocalizationStringIds
    {
        public static string ForOperatorDisplayName(string operatorId)
        {
            return "narrative.operator." + RequireToken(operatorId, nameof(operatorId)) + ".display.name";
        }

        public static string ForRaidFailureRefundToast()
        {
            return "narrative.raid_raider_king_engine.failure_refund.toast";
        }

        private static string RequireToken(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            var normalized = value.Trim();
            if (normalized.Contains("."))
            {
                throw new ArgumentException("Narrative localization string id parts cannot contain dot separators.", paramName);
            }

            return normalized;
        }
    }
}
