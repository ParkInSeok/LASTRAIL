using System;

namespace LastRail.Run
{
    public sealed class RunRewardLineDto
    {
        public RunRewardLineDto(string rewardLineId, string rewardCode, string displayName, int quantity, string unitLabel, int? balanceAfter = null)
        {
            RequireText(rewardLineId, nameof(rewardLineId));
            RequireText(rewardCode, nameof(rewardCode));
            RequireText(displayName, nameof(displayName));
            RequireText(unitLabel, nameof(unitLabel));
            if (quantity < 0)
            {
                throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
            }

            RewardLineId = rewardLineId;
            RewardCode = rewardCode;
            DisplayName = displayName;
            Quantity = quantity;
            UnitLabel = unitLabel;
            BalanceAfter = balanceAfter;
        }

        public string RewardLineId { get; }
        public string RewardCode { get; }
        public string DisplayName { get; }
        public int Quantity { get; }
        public string UnitLabel { get; }
        public int? BalanceAfter { get; }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
