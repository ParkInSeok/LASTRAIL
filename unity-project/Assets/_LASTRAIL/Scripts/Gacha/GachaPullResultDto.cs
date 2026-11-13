using System;

namespace LastRail.Gacha
{
    public sealed class GachaPullResultDto
    {
        public GachaPullResultDto(
            int pullSequence,
            GachaRarity resultRarity,
            GachaPullResultKind resultType,
            string resultId,
            GachaRewardDto reward,
            bool isPickup,
            string guaranteeType,
            bool wasDuplicate,
            GachaDuplicateConversionDto duplicateConversion)
        {
            if (pullSequence <= 0)
            {
                throw new ArgumentException("Pull sequence must be positive.", nameof(pullSequence));
            }

            RequireText(resultId, nameof(resultId));
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
            if (wasDuplicate && duplicateConversion == null)
            {
                throw new ArgumentException("Duplicate conversion is required for duplicate results.", nameof(duplicateConversion));
            }

            PullSequence = pullSequence;
            ResultRarity = resultRarity;
            ResultType = resultType;
            ResultId = resultId;
            IsPickup = isPickup;
            GuaranteeType = OptionalText(guaranteeType);
            WasDuplicate = wasDuplicate;
            DuplicateConversion = duplicateConversion;
        }

        public int PullSequence { get; }
        public GachaRarity ResultRarity { get; }
        public GachaPullResultKind ResultType { get; }
        public string ResultId { get; }
        public GachaRewardDto Reward { get; }
        public bool IsPickup { get; }
        public string GuaranteeType { get; }
        public bool WasDuplicate { get; }
        public GachaDuplicateConversionDto DuplicateConversion { get; }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
