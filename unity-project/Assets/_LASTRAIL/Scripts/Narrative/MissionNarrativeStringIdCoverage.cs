using System;
using System.Collections.Generic;
using System.Text;

namespace LastRail.Narrative
{
    public sealed class MissionNarrativeStringIdCoverage
    {
        private readonly HashSet<string> stringIds;

        private MissionNarrativeStringIdCoverage(
            string missionId,
            string displayName,
            IReadOnlyList<MissionNarrativeStringIdEntry> entries,
            int missingRequiredSurfaceCount)
        {
            MissionId = missionId;
            DisplayName = displayName;
            Entries = entries;
            MissingRequiredSurfaceCount = missingRequiredSurfaceCount;
            stringIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                stringIds.Add(entry.StringId);
            }
        }

        public string MissionId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<MissionNarrativeStringIdEntry> Entries { get; }
        public int EntryCount => Entries.Count;
        public int MissingRequiredSurfaceCount { get; }

        public bool HasStringId(string stringId)
        {
            return !string.IsNullOrWhiteSpace(stringId) && stringIds.Contains(stringId.Trim());
        }

        public static MissionNarrativeStringIdCoverage FromDefinition(MissionNarrativeDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var entries = new List<MissionNarrativeStringIdEntry>();
            Add(entries, definition.MissionId, "mission_start", "copy");
            Add(entries, definition.MissionId, "loading", "hint");
            AddNumbered(entries, definition.MissionId, "briefing", "line", definition.BriefingLines.Count);
            AddNumbered(entries, definition.MissionId, "commander_response", "line", definition.CommanderResponseCandidates.Count);
            AddCallouts(entries, definition.MissionId, "opening", definition.OpeningCallouts.Count);
            AddCallouts(entries, definition.MissionId, "tutorial", definition.TutorialAdjacentLines.Count);
            AddCallouts(entries, definition.MissionId, "escalation", definition.EscalationCallouts.Count);
            AddCallouts(entries, definition.MissionId, "optional_objective", definition.OptionalObjectiveCallouts.Count);
            AddCallouts(entries, definition.MissionId, "final_wave", definition.FinalWaveCallouts.Count);
            AddResultLines(entries, definition.MissionId, "victory", definition.VictoryLines.Count);
            AddResultLines(entries, definition.MissionId, "failure", definition.FailureLines.Count);
            Add(entries, definition.MissionId, "result", "title");
            Add(entries, definition.MissionId, "result", "summary");
            Add(entries, definition.MissionId, "reward_framing", "first_clear");
            Add(entries, definition.MissionId, "reward_framing", "repeat_clear");
            if (definition.HasCodexEntry)
            {
                Add(entries, definition.MissionId, "codex", "entry");
            }

            return new MissionNarrativeStringIdCoverage(
                definition.MissionId,
                definition.DisplayName,
                entries.AsReadOnly(),
                missingRequiredSurfaceCount: 0);
        }

        public string FlattenStringIdsForValidation()
        {
            var builder = new StringBuilder();
            foreach (var entry in Entries)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(entry.StringId);
            }

            return builder.ToString();
        }

        private static void AddNumbered(List<MissionNarrativeStringIdEntry> entries, string missionId, string surface, string prefix, int count)
        {
            for (var i = 1; i <= count; i++)
            {
                Add(entries, missionId, surface, prefix + "_" + i.ToString("000", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        private static void AddCallouts(List<MissionNarrativeStringIdEntry> entries, string missionId, string group, int count)
        {
            AddNumbered(entries, missionId, "callout", group, count);
        }

        private static void AddResultLines(List<MissionNarrativeStringIdEntry> entries, string missionId, string group, int count)
        {
            AddNumbered(entries, missionId, "result", group, count);
        }

        private static void Add(List<MissionNarrativeStringIdEntry> entries, string missionId, string surface, string slot)
        {
            entries.Add(new MissionNarrativeStringIdEntry(missionId, surface, slot));
        }
    }
}
