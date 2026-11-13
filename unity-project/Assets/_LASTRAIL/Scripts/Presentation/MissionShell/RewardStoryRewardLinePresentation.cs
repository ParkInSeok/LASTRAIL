using LastRail.OfflineRewards;
using LastRail.Run;

namespace LastRail.Presentation.MissionShell
{
    public sealed class RewardStoryRewardLinePresentation
    {
        public RewardStoryRewardLinePresentation(
            string rewardLineId,
            string rewardCode,
            string displayName,
            int quantity,
            string unitLabel,
            string source,
            int? balanceAfter)
        {
            RewardLineId = OptionalText(rewardLineId);
            RewardCode = OptionalText(rewardCode);
            DisplayName = OptionalText(displayName);
            Quantity = quantity;
            UnitLabel = OptionalText(unitLabel);
            Source = OptionalText(source);
            BalanceAfter = balanceAfter;
        }

        public string RewardLineId { get; }
        public string RewardCode { get; }
        public string DisplayName { get; }
        public int Quantity { get; }
        public string UnitLabel { get; }
        public string Source { get; }
        public int? BalanceAfter { get; }
        public bool HasBalanceAfter => BalanceAfter.HasValue;

        public static RewardStoryRewardLinePresentation FromRunRewardLine(RunRewardLineDto line)
        {
            return new RewardStoryRewardLinePresentation(
                line.RewardLineId,
                line.RewardCode,
                line.DisplayName,
                line.Quantity,
                line.UnitLabel,
                null,
                line.BalanceAfter);
        }

        public static RewardStoryRewardLinePresentation FromOfflineRewardLine(OfflineRewardLineDto line)
        {
            return new RewardStoryRewardLinePresentation(
                line.RewardLineId,
                line.RewardCode,
                line.DisplayName,
                line.Quantity,
                line.UnitLabel,
                line.Source,
                line.BalanceAfter);
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
