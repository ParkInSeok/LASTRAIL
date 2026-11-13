using System;
using System.Collections.Generic;
using System.Text;

namespace LastRail.Narrative
{
    public sealed class MissionNarrativeStringIdCoverageMap
    {
        private readonly IReadOnlyList<MissionNarrativeStringIdCoverage> coverages;
        private readonly Dictionary<string, MissionNarrativeStringIdCoverage> byMissionId;
        private readonly IReadOnlyList<string> allStringIds;

        public MissionNarrativeStringIdCoverageMap(IEnumerable<MissionNarrativeStringIdCoverage> coverages)
        {
            if (coverages == null)
            {
                throw new ArgumentNullException(nameof(coverages));
            }

            var copied = new List<MissionNarrativeStringIdCoverage>();
            var lookup = new Dictionary<string, MissionNarrativeStringIdCoverage>(StringComparer.Ordinal);
            var ids = new List<string>();
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var coverage in coverages)
            {
                if (coverage == null)
                {
                    throw new ArgumentException("Coverage cannot be null.", nameof(coverages));
                }

                if (lookup.ContainsKey(coverage.MissionId))
                {
                    throw new ArgumentException("Duplicate mission id in narrative string id coverage map.", nameof(coverages));
                }

                foreach (var entry in coverage.Entries)
                {
                    if (!uniqueIds.Add(entry.StringId))
                    {
                        throw new ArgumentException("Duplicate narrative string id in coverage map.", nameof(coverages));
                    }

                    ids.Add(entry.StringId);
                }

                copied.Add(coverage);
                lookup.Add(coverage.MissionId, coverage);
            }

            this.coverages = copied.AsReadOnly();
            byMissionId = lookup;
            allStringIds = ids.AsReadOnly();
        }

        public int MissionCount => coverages.Count;
        public IReadOnlyList<MissionNarrativeStringIdCoverage> Coverages => coverages;
        public IReadOnlyList<string> AllStringIds => allStringIds;
        public int TotalMissingRequiredSurfaceCount
        {
            get
            {
                var count = 0;
                foreach (var coverage in coverages)
                {
                    count += coverage.MissingRequiredSurfaceCount;
                }

                return count;
            }
        }

        public MissionNarrativeStringIdCoverage FindByMissionId(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return null;
            }

            return byMissionId.TryGetValue(missionId.Trim(), out var coverage) ? coverage : null;
        }

        public string FlattenStringIdsForValidation()
        {
            var builder = new StringBuilder();
            foreach (var stringId in allStringIds)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(stringId);
            }

            return builder.ToString();
        }

        public static MissionNarrativeStringIdCoverageMap CreateActOne()
        {
            var coverages = new List<MissionNarrativeStringIdCoverage>();
            foreach (var definition in ActOneMissionNarrativeFixtures.All())
            {
                coverages.Add(MissionNarrativeStringIdCoverage.FromDefinition(definition));
            }

            return new MissionNarrativeStringIdCoverageMap(coverages);
        }
    }
}
