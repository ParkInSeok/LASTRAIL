using System;

namespace LastRail.Presentation.MissionShell
{
    public sealed class RewardStoryScreenRewardLineViewState
    {
        public RewardStoryScreenRewardLineViewState(
            string rewardLineId,
            string rewardCode,
            string displayName,
            string quantityText,
            string source,
            bool hasBalanceAfterDiagnostic)
        {
            RewardLineId = OptionalText(rewardLineId);
            RewardCode = OptionalText(rewardCode);
            DisplayName = OptionalText(displayName) ?? "보상";
            QuantityText = OptionalText(quantityText);
            Source = OptionalText(source);
            HasBalanceAfterDiagnostic = hasBalanceAfterDiagnostic;
        }

        public string RewardLineId { get; }
        public string RewardCode { get; }
        public string DisplayName { get; }
        public string QuantityText { get; }
        public string Source { get; }
        public bool HasBalanceAfterDiagnostic { get; }
        public bool IsDisplayOnly => true;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;

        public static RewardStoryScreenRewardLineViewState FromPresentation(RewardStoryRewardLinePresentation line)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            var quantityText = line.UnitLabel == null
                ? line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) + line.UnitLabel;
            return new RewardStoryScreenRewardLineViewState(
                line.RewardLineId,
                line.RewardCode,
                line.DisplayName,
                quantityText,
                line.Source,
                line.HasBalanceAfter);
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
