using System;
using System.Globalization;

namespace LastRail.Narrative
{
    public static class MissionNarrativeStringIds
    {
        public static string Build(string missionId, string surface, string slot)
        {
            return "narrative." + RequireToken(missionId, nameof(missionId)) + "." +
                RequireToken(surface, nameof(surface)) + "." +
                RequireToken(slot, nameof(slot));
        }

        public static string ForMissionStart(string missionId)
        {
            return Build(missionId, "mission_start", "copy");
        }

        public static string ForLoadingHint(string missionId)
        {
            return Build(missionId, "loading", "hint");
        }

        public static string ForBriefingLine(string missionId, int lineNumber)
        {
            return Build(missionId, "briefing", NumberedSlot("line", lineNumber));
        }

        public static string ForCommanderResponse(string missionId, int lineNumber)
        {
            return Build(missionId, "commander_response", NumberedSlot("line", lineNumber));
        }

        public static string ForCallout(string missionId, string calloutGroup, int lineNumber)
        {
            return Build(missionId, "callout", RequireToken(calloutGroup, nameof(calloutGroup)) + "_" + FormatNumber(lineNumber));
        }

        public static string ForResultLine(string missionId, string resultGroup, int lineNumber)
        {
            return Build(missionId, "result", RequireToken(resultGroup, nameof(resultGroup)) + "_" + FormatNumber(lineNumber));
        }

        public static string ForRewardFraming(string missionId, string framingKind)
        {
            return Build(missionId, "reward_framing", RequireToken(framingKind, nameof(framingKind)));
        }

        public static string ForCodexEntry(string missionId)
        {
            return Build(missionId, "codex", "entry");
        }

        private static string NumberedSlot(string prefix, int lineNumber)
        {
            return RequireToken(prefix, nameof(prefix)) + "_" + FormatNumber(lineNumber);
        }

        private static string FormatNumber(int lineNumber)
        {
            if (lineNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, "Line number must be positive.");
            }

            return lineNumber.ToString("000", CultureInfo.InvariantCulture);
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
                throw new ArgumentException("Narrative string id parts cannot contain dot separators.", paramName);
            }

            return normalized;
        }
    }
}
