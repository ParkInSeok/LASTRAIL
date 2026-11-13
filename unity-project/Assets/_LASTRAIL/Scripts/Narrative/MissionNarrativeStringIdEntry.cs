using System;

namespace LastRail.Narrative
{
    public sealed class MissionNarrativeStringIdEntry
    {
        public MissionNarrativeStringIdEntry(string missionId, string surface, string slot)
        {
            MissionId = Require(missionId, nameof(missionId));
            Surface = Require(surface, nameof(surface));
            Slot = Require(slot, nameof(slot));
            StringId = MissionNarrativeStringIds.Build(MissionId, Surface, Slot);
        }

        public string MissionId { get; }
        public string Surface { get; }
        public string Slot { get; }
        public string StringId { get; }
        public bool HasLocalizedTextPayload => false;

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }
    }
}
