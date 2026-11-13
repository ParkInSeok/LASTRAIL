using System;
using System.Collections.Generic;

namespace LastRail.Narrative
{
    public sealed class MissionNarrativeCatalog
    {
        private readonly IReadOnlyList<MissionNarrativeDefinition> definitions;
        private readonly Dictionary<string, MissionNarrativeDefinition> byMissionId;

        public MissionNarrativeCatalog(IEnumerable<MissionNarrativeDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var copied = new List<MissionNarrativeDefinition>();
            var lookup = new Dictionary<string, MissionNarrativeDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Narrative definition cannot be null.", nameof(definitions));
                }

                if (lookup.ContainsKey(definition.MissionId))
                {
                    throw new ArgumentException("Duplicate mission narrative id.", nameof(definitions));
                }

                copied.Add(definition);
                lookup.Add(definition.MissionId, definition);
            }

            this.definitions = copied.AsReadOnly();
            byMissionId = lookup;
        }

        public int Count => definitions.Count;
        public IReadOnlyList<MissionNarrativeDefinition> Definitions => definitions;

        public MissionNarrativeDefinition FindByMissionId(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return null;
            }

            return byMissionId.TryGetValue(missionId.Trim(), out var definition) ? definition : null;
        }

        public static MissionNarrativeCatalog CreateActOne()
        {
            return new MissionNarrativeCatalog(ActOneMissionNarrativeFixtures.All());
        }
    }
}
