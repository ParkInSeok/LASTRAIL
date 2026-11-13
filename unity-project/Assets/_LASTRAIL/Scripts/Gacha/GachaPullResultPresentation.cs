using System;

namespace LastRail.Gacha
{
    public sealed class GachaPullResultPresentation
    {
        public GachaPullResultPresentation(
            string resultId,
            int pullIndex,
            GachaPullResultKind resultKind,
            GachaRarity rarity,
            string rewardId,
            string displayName,
            bool isDuplicateConversion,
            string duplicateSourceId,
            string conversionItemId,
            int conversionQuantity,
            string guaranteeType = null)
        {
            RequireText(resultId, nameof(resultId));
            if (pullIndex <= 0)
            {
                throw new ArgumentException("Pull index must be positive.", nameof(pullIndex));
            }

            RequireText(rewardId, nameof(rewardId));
            RequireText(displayName, nameof(displayName));
            if (conversionQuantity < 0)
            {
                throw new ArgumentException("Conversion quantity cannot be negative.", nameof(conversionQuantity));
            }

            if (isDuplicateConversion)
            {
                RequireText(duplicateSourceId, nameof(duplicateSourceId));
                RequireText(conversionItemId, nameof(conversionItemId));
                if (conversionQuantity <= 0)
                {
                    throw new ArgumentException("Duplicate conversion quantity must be positive.", nameof(conversionQuantity));
                }
            }

            ResultId = resultId;
            PullIndex = pullIndex;
            ResultKind = resultKind;
            Rarity = rarity;
            RewardId = rewardId;
            DisplayName = displayName;
            IsDuplicateConversion = isDuplicateConversion;
            DuplicateSourceId = OptionalText(duplicateSourceId);
            ConversionItemId = OptionalText(conversionItemId);
            ConversionQuantity = conversionQuantity;
            GuaranteeType = OptionalText(guaranteeType);
        }

        public string ResultId { get; }
        public int PullIndex { get; }
        public GachaPullResultKind ResultKind { get; }
        public GachaRarity Rarity { get; }
        public string RewardId { get; }
        public string DisplayName { get; }
        public bool IsDuplicateConversion { get; }
        public string DuplicateSourceId { get; }
        public string ConversionItemId { get; }
        public int ConversionQuantity { get; }
        public string GuaranteeType { get; }
        public bool HasConversionReward => IsDuplicateConversion && ConversionQuantity > 0;

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
