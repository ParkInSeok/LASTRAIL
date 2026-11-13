using System;
using System.Globalization;
using System.Text;

namespace LastRail.OfflineRewards
{
    public static class OfflineRewardClaimStateJsonAdapter
    {
        private const string UtcRoundTripFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

        public static string ToJson(OfflineRewardClaimState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            builder.Append("\"accepted\":").Append(state.Accepted ? "true" : "false").Append(',');
            AppendString(builder, "claimStatus", OfflineRewardClaimResponseJsonAdapter.StatusToServerValue(state.ClaimStatus)).Append(',');
            AppendOptionalString(builder, "offlineRewardClaimId", state.OfflineRewardClaimId).Append(',');
            AppendOptionalString(builder, "rewardTransactionId", state.RewardTransactionId).Append(',');
            AppendOptionalString(builder, "economyTransactionId", state.EconomyTransactionId).Append(',');
            AppendOptionalString(builder, "accountMutationId", state.AccountMutationId).Append(',');
            AppendString(builder, "storyProgressId", state.StoryProgressId).Append(',');
            AppendString(builder, "storyProgressLabel", state.StoryProgressLabel).Append(',');
            builder.Append("\"claimableSeconds\":").Append(state.ClaimableSeconds.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"claimableHours\":").Append(state.ClaimableHours.ToString("0.###", CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"offlineStorageCapHours\":").Append(state.OfflineStorageCapHours.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "offlineStorageCapLabel", state.OfflineStorageCapLabel).Append(',');
            builder.Append("\"minimumClaimSeconds\":").Append(state.MinimumClaimSeconds.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"maxClaimableHours\":").Append(state.MaxClaimableHours.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendRewardLines(builder, state).Append(',');
            builder.Append("\"calculation\":{");
            AppendString(builder, "storyProgressId", state.Calculation.StoryProgressId).Append(',');
            AppendString(builder, "storyProgressMultiplierLabel", state.Calculation.StoryProgressMultiplierLabel).Append(',');
            builder.Append("\"cappedOfflineSeconds\":").Append(state.Calculation.CappedOfflineSeconds.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "capSource", state.Calculation.CapSource);
            builder.Append("},");
            AppendString(builder, "serverClaimedAt", state.ServerClaimedAt.ToString(UtcRoundTripFormat, CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "narrativeVersion", state.NarrativeVersion).Append(',');
            AppendOptionalString(builder, "errorCode", state.ErrorCode).Append(',');
            AppendOptionalString(builder, "message", state.Message).Append(',');
            builder.Append("\"hasClientCalculation\":false,");
            builder.Append("\"hasEconomyGrant\":false");
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendRewardLines(StringBuilder builder, OfflineRewardClaimState state)
        {
            builder.Append("\"rewardLines\":[");
            for (var i = 0; i < state.RewardLines.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var line = state.RewardLines[i];
                builder.Append('{');
                AppendString(builder, "rewardLineId", line.RewardLineId).Append(',');
                AppendString(builder, "rewardCode", line.RewardCode).Append(',');
                AppendString(builder, "displayName", line.DisplayName).Append(',');
                builder.Append("\"quantity\":").Append(line.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',');
                AppendString(builder, "unitLabel", line.UnitLabel).Append(',');
                AppendString(builder, "source", line.Source).Append(',');
                if (line.BalanceAfter.HasValue)
                {
                    builder.Append("\"balanceAfter\":").Append(line.BalanceAfter.Value.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append("\"balanceAfter\":null");
                }

                builder.Append('}');
            }

            return builder.Append(']');
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendOptionalString(StringBuilder builder, string fieldName, string value)
        {
            if (value == null)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendString(builder, fieldName, value);
        }

        private static StringBuilder AppendJsonString(StringBuilder builder, string value)
        {
            return builder.Append('"').Append(Escape(value)).Append('"');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
