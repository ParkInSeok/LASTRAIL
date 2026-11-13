using System;
using System.Collections.Generic;
using System.Text;

namespace LastRail.Narrative
{
    public sealed class MissionNarrativeLocalizationMissionAudit
    {
        public MissionNarrativeLocalizationMissionAudit(
            string missionId,
            string displayName,
            IEnumerable<string> requiredStringIds,
            IEnumerable<string> availableStringIds,
            IEnumerable<string> missingStringIds)
        {
            MissionId = Require(missionId, nameof(missionId));
            DisplayName = Require(displayName, nameof(displayName));
            RequiredStringIds = Copy(requiredStringIds, nameof(requiredStringIds));
            AvailableStringIds = Copy(availableStringIds, nameof(availableStringIds));
            MissingStringIds = Copy(missingStringIds, nameof(missingStringIds));
        }

        public string MissionId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> RequiredStringIds { get; }
        public IReadOnlyList<string> AvailableStringIds { get; }
        public IReadOnlyList<string> MissingStringIds { get; }
        public int RequiredStringIdCount => RequiredStringIds.Count;
        public int AvailableStringIdCount => AvailableStringIds.Count;
        public int MissingStringIdCount => MissingStringIds.Count;
        public MissionNarrativeLocalizationStatus Status => MissingStringIdCount == 0
            ? MissionNarrativeLocalizationStatus.Complete
            : MissionNarrativeLocalizationStatus.Missing;
        public bool HasLocalizedTextPayload => false;

        public string FlattenForValidation()
        {
            var builder = new StringBuilder();
            Append(builder, MissionId);
            Append(builder, DisplayName);
            Append(builder, RequiredStringIds);
            Append(builder, AvailableStringIds);
            Append(builder, MissingStringIds);
            return builder.ToString();
        }

        private static IReadOnlyList<string> Copy(IEnumerable<string> values, string name)
        {
            if (values == null)
            {
                throw new ArgumentNullException(name);
            }

            var copied = new List<string>();
            foreach (var value in values)
            {
                copied.Add(Require(value, name));
            }

            return copied.AsReadOnly();
        }

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }

        private static void Append(StringBuilder builder, IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                Append(builder, value);
            }
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
