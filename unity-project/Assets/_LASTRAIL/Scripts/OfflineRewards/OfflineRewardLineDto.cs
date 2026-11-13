using System;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardLineDto
    {
        public OfflineRewardLineDto(string rewardLineId, string rewardCode, string displayName, int quantity, string unitLabel, string source, int? balanceAfter = null, string kind = null)
        {
            RewardLineId = Required(rewardLineId, nameof(rewardLineId));
            RewardCode = Required(rewardCode, nameof(rewardCode));
            DisplayName = Required(displayName, nameof(displayName));
            if (quantity < 0)
            {
                throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
            }

            Quantity = quantity;
            UnitLabel = Required(unitLabel, nameof(unitLabel));
            Source = Required(source, nameof(source));
            BalanceAfter = balanceAfter;
            Kind = string.IsNullOrWhiteSpace(kind) ? null : kind.Trim();
        }

        public string RewardLineId { get; }
        public string Kind { get; }
        public string RewardCode { get; }
        public string DisplayName { get; }
        public int Quantity { get; }
        public string UnitLabel { get; }
        public string Source { get; }
        public int? BalanceAfter { get; }

        private static string Required(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
