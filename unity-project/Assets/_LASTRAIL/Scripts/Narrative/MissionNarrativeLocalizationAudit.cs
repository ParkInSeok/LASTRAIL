using System;
using System.Collections.Generic;
using System.Text;

namespace LastRail.Narrative
{
    public sealed class MissionNarrativeLocalizationAudit
    {
        private readonly Dictionary<string, MissionNarrativeLocalizationMissionAudit> byMissionId;

        private MissionNarrativeLocalizationAudit(
            IReadOnlyList<MissionNarrativeLocalizationMissionAudit> missions,
            int requiredStringIdCount,
            int availableStringIdCount,
            int missingStringIdCount,
            int unknownAvailableStringIdCount)
        {
            Missions = missions;
            RequiredStringIdCount = requiredStringIdCount;
            AvailableStringIdCount = availableStringIdCount;
            MissingStringIdCount = missingStringIdCount;
            UnknownAvailableStringIdCount = unknownAvailableStringIdCount;
            byMissionId = new Dictionary<string, MissionNarrativeLocalizationMissionAudit>(StringComparer.Ordinal);
            foreach (var mission in missions)
            {
                byMissionId.Add(mission.MissionId, mission);
            }
        }

        public IReadOnlyList<MissionNarrativeLocalizationMissionAudit> Missions { get; }
        public int MissionCount => Missions.Count;
        public int RequiredStringIdCount { get; }
        public int AvailableStringIdCount { get; }
        public int MissingStringIdCount { get; }
        public int UnknownAvailableStringIdCount { get; }
        public MissionNarrativeLocalizationStatus Status => MissingStringIdCount == 0
            ? MissionNarrativeLocalizationStatus.Complete
            : MissionNarrativeLocalizationStatus.Missing;
        public bool HasLocalizedTextPayload => false;

        public MissionNarrativeLocalizationMissionAudit FindMission(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return null;
            }

            return byMissionId.TryGetValue(missionId.Trim(), out var mission) ? mission : null;
        }

        public string FlattenForValidation()
        {
            var builder = new StringBuilder();
            foreach (var mission in Missions)
            {
                Append(builder, mission.FlattenForValidation());
            }

            return builder.ToString();
        }

        public static MissionNarrativeLocalizationAudit FromCoverage(
            MissionNarrativeStringIdCoverage coverage,
            IEnumerable<string> availableStringIds)
        {
            if (coverage == null)
            {
                throw new ArgumentNullException(nameof(coverage));
            }

            return FromCoverages(new[] { coverage }, availableStringIds);
        }

        public static MissionNarrativeLocalizationAudit FromCoverageMap(
            MissionNarrativeStringIdCoverageMap coverageMap,
            IEnumerable<string> availableStringIds)
        {
            if (coverageMap == null)
            {
                throw new ArgumentNullException(nameof(coverageMap));
            }

            return FromCoverages(coverageMap.Coverages, availableStringIds);
        }

        private static MissionNarrativeLocalizationAudit FromCoverages(
            IEnumerable<MissionNarrativeStringIdCoverage> coverages,
            IEnumerable<string> availableStringIds)
        {
            var available = NormalizeAvailable(availableStringIds);
            var knownRequired = new HashSet<string>(StringComparer.Ordinal);
            var missionAudits = new List<MissionNarrativeLocalizationMissionAudit>();
            var requiredCount = 0;
            var availableCount = 0;
            var missingCount = 0;

            foreach (var coverage in coverages)
            {
                var requiredForMission = new List<string>();
                var availableForMission = new List<string>();
                var missingForMission = new List<string>();
                foreach (var entry in coverage.Entries)
                {
                    requiredForMission.Add(entry.StringId);
                    knownRequired.Add(entry.StringId);
                    requiredCount++;

                    if (available.Contains(entry.StringId))
                    {
                        availableForMission.Add(entry.StringId);
                        availableCount++;
                    }
                    else
                    {
                        missingForMission.Add(entry.StringId);
                        missingCount++;
                    }
                }

                missionAudits.Add(new MissionNarrativeLocalizationMissionAudit(
                    coverage.MissionId,
                    coverage.DisplayName,
                    requiredForMission,
                    availableForMission,
                    missingForMission));
            }

            var unknownCount = 0;
            foreach (var stringId in available)
            {
                if (!knownRequired.Contains(stringId))
                {
                    unknownCount++;
                }
            }

            return new MissionNarrativeLocalizationAudit(
                missionAudits.AsReadOnly(),
                requiredCount,
                availableCount,
                missingCount,
                unknownCount);
        }

        private static HashSet<string> NormalizeAvailable(IEnumerable<string> availableStringIds)
        {
            if (availableStringIds == null)
            {
                throw new ArgumentNullException(nameof(availableStringIds));
            }

            var normalized = new HashSet<string>(StringComparer.Ordinal);
            foreach (var stringId in availableStringIds)
            {
                if (string.IsNullOrWhiteSpace(stringId))
                {
                    continue;
                }

                normalized.Add(stringId.Trim());
            }

            return normalized;
        }

        private static void Append(StringBuilder builder, string value)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(value);
        }
    }
}
