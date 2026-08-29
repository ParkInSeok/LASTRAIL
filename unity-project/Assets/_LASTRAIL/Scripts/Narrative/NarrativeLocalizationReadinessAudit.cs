using System;
using System.Collections.Generic;

namespace LastRail.Narrative
{
    public sealed class NarrativeLocalizationReadinessAudit
    {
        private const int OperatorDisplayNameMaxCharacters = 24;
        private const int RewardFramingMaxCharacters = 64;
        private const int RaidFailureRefundToastMaxCharacters = 72;
        private const int CodexEntryMaxCharacters = 180;

        private readonly Dictionary<string, NarrativeLocalizationReadinessSlot> byStringId;

        private NarrativeLocalizationReadinessAudit(
            IReadOnlyList<NarrativeLocalizationReadinessSlot> slots,
            int operatorDisplayNameStringIdCount,
            string raidFailureRefundToastStringId)
        {
            Slots = slots ?? throw new ArgumentNullException(nameof(slots));
            OperatorDisplayNameStringIdCount = operatorDisplayNameStringIdCount;
            RaidFailureRefundToastStringId = RequireText(raidFailureRefundToastStringId, nameof(raidFailureRefundToastStringId));
            byStringId = new Dictionary<string, NarrativeLocalizationReadinessSlot>(StringComparer.Ordinal);

            var missingCount = 0;
            var overflowCount = 0;
            foreach (var slot in slots)
            {
                byStringId[slot.StringId] = slot;
                if (!slot.IsAvailable)
                {
                    missingCount++;
                }

                if (slot.ExceedsRecommendedLength)
                {
                    overflowCount++;
                }
            }

            MissingStringIdCount = missingCount;
            OverflowSlotCount = overflowCount;
        }

        public IReadOnlyList<NarrativeLocalizationReadinessSlot> Slots { get; }
        public int RequiredStringIdCount => Slots.Count;
        public int OperatorDisplayNameStringIdCount { get; }
        public int MissingStringIdCount { get; }
        public int OverflowSlotCount { get; }
        public string RaidFailureRefundToastStringId { get; }
        public bool HasLocalizedFullTextPayload => false;
        public bool HasServerTruthPayload => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public bool HasStringId(string stringId)
        {
            return !string.IsNullOrWhiteSpace(stringId) && byStringId.ContainsKey(stringId.Trim());
        }

        public NarrativeLocalizationReadinessSlot FindSlot(string stringId)
        {
            if (string.IsNullOrWhiteSpace(stringId))
            {
                return null;
            }

            return byStringId.TryGetValue(stringId.Trim(), out var slot) ? slot : null;
        }

        public static NarrativeLocalizationReadinessAudit FromActOneLaunchScope(
            IEnumerable<MissionNarrativeDefinition> definitions,
            IEnumerable<string> launchVisualOperatorIds,
            IReadOnlyDictionary<string, string> localizedTextByStringId)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (launchVisualOperatorIds == null)
            {
                throw new ArgumentNullException(nameof(launchVisualOperatorIds));
            }

            if (localizedTextByStringId == null)
            {
                throw new ArgumentNullException(nameof(localizedTextByStringId));
            }

            var slots = new List<NarrativeLocalizationReadinessSlot>();
            var operatorCount = 0;
            foreach (var operatorId in launchVisualOperatorIds)
            {
                AddSlot(
                    slots,
                    "operator_display_name",
                    LaunchNarrativeLocalizationStringIds.ForOperatorDisplayName(operatorId),
                    localizedTextByStringId,
                    OperatorDisplayNameMaxCharacters);
                operatorCount++;
            }

            foreach (var definition in definitions)
            {
                var coverage = MissionNarrativeStringIdCoverage.FromDefinition(definition);
                foreach (var entry in coverage.Entries)
                {
                    AddSlot(
                        slots,
                        ResolveSlotKind(entry.Surface, entry.Slot),
                        entry.StringId,
                        localizedTextByStringId,
                        ResolveMaxCharacters(entry.Surface, entry.Slot));
                }
            }

            var raidFailureRefundToastStringId = LaunchNarrativeLocalizationStringIds.ForRaidFailureRefundToast();
            AddSlot(
                slots,
                "raid_failure_refund_toast",
                raidFailureRefundToastStringId,
                localizedTextByStringId,
                RaidFailureRefundToastMaxCharacters);

            return new NarrativeLocalizationReadinessAudit(
                slots.AsReadOnly(),
                operatorCount,
                raidFailureRefundToastStringId);
        }

        private static void AddSlot(
            ICollection<NarrativeLocalizationReadinessSlot> slots,
            string slotKind,
            string stringId,
            IReadOnlyDictionary<string, string> localizedTextByStringId,
            int maxCharacterCount)
        {
            var hasText = localizedTextByStringId.TryGetValue(stringId, out var text) &&
                !string.IsNullOrWhiteSpace(text);
            slots.Add(new NarrativeLocalizationReadinessSlot(
                slotKind,
                stringId,
                hasText,
                hasText ? text.Trim().Length : 0,
                maxCharacterCount));
        }

        private static string ResolveSlotKind(string surface, string slot)
        {
            if (string.Equals(surface, "codex", StringComparison.Ordinal))
            {
                return "codex_entry";
            }

            if (string.Equals(surface, "reward_framing", StringComparison.Ordinal))
            {
                return "reward_framing";
            }

            if (string.Equals(surface, "mission_start", StringComparison.Ordinal))
            {
                return "mission_start";
            }

            return surface + "_" + slot;
        }

        private static int ResolveMaxCharacters(string surface, string slot)
        {
            if (string.Equals(surface, "codex", StringComparison.Ordinal))
            {
                return CodexEntryMaxCharacters;
            }

            if (string.Equals(surface, "reward_framing", StringComparison.Ordinal))
            {
                return RewardFramingMaxCharacters;
            }

            if (string.Equals(surface, "mission_start", StringComparison.Ordinal))
            {
                return 24;
            }

            if (string.Equals(surface, "loading", StringComparison.Ordinal))
            {
                return 80;
            }

            if (string.Equals(surface, "result", StringComparison.Ordinal))
            {
                return 96;
            }

            return 72;
        }

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
