using System;
using System.Text;

namespace LastRail.Gacha
{
    public static class GachaPullUxStateJsonAdapter
    {
        public static string ToJson(GachaPullUxState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "kind", state.Kind.ToString()).Append(',');
            AppendString(builder, "clientOutcome", state.ClientOutcome.ToString()).Append(',');
            AppendString(builder, "rejectionRecovery", state.RejectionRecovery.ToString()).Append(',');
            builder.Append("\"hasBatch\":").Append(state.Batch == null ? "false" : "true").Append(',');
            AppendBatch(builder, state.Batch).Append(',');
            AppendPostPityState(builder, state.PostPityState).Append(',');
            AppendCost(builder, state.Cost).Append(',');
            AppendOptionalString(builder, "errorCode", state.ErrorCode).Append(',');
            AppendOptionalString(builder, "errorMessage", state.ErrorMessage).Append(',');
            builder.Append("\"canRetryWithSameIdempotencyKey\":").Append(state.CanRetryWithSameIdempotencyKey ? "true" : "false").Append(',');
            builder.Append("\"requiresAuthRefreshOrGuestLogin\":").Append(state.RequiresAuthRefreshOrGuestLogin ? "true" : "false").Append(',');
            builder.Append("\"hasEconomyMutation\":").Append(state.HasEconomyMutation ? "true" : "false").Append(',');
            builder.Append("\"hasClientSideRng\":").Append(state.HasClientSideRng ? "true" : "false");
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendBatch(StringBuilder builder, GachaPullBatchPresentation batch)
        {
            builder.Append("\"batch\":");
            if (batch == null)
            {
                return builder.Append("null");
            }

            builder.Append('{');
            AppendString(builder, "batchId", batch.BatchId).Append(',');
            AppendString(builder, "bannerId", batch.BannerId).Append(',');
            builder.Append("\"pullCount\":").Append(batch.PullCount.Value).Append(',');
            builder.Append("\"results\":[");
            for (var i = 0; i < batch.Results.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendResult(builder, batch.Results[i]);
            }

            builder.Append(']');
            return builder.Append('}');
        }

        private static StringBuilder AppendResult(StringBuilder builder, GachaPullResultPresentation result)
        {
            builder.Append('{');
            AppendString(builder, "resultId", result.ResultId).Append(',');
            builder.Append("\"pullIndex\":").Append(result.PullIndex).Append(',');
            AppendString(builder, "resultKind", result.ResultKind.ToString()).Append(',');
            AppendString(builder, "rarity", result.Rarity.ToString()).Append(',');
            AppendString(builder, "rewardId", result.RewardId).Append(',');
            AppendString(builder, "displayName", result.DisplayName).Append(',');
            builder.Append("\"isDuplicateConversion\":").Append(result.IsDuplicateConversion ? "true" : "false").Append(',');
            AppendOptionalString(builder, "duplicateSourceId", result.DuplicateSourceId).Append(',');
            AppendOptionalString(builder, "conversionItemId", result.ConversionItemId).Append(',');
            builder.Append("\"conversionQuantity\":").Append(result.ConversionQuantity).Append(',');
            AppendOptionalString(builder, "guaranteeType", result.GuaranteeType);
            return builder.Append('}');
        }

        private static StringBuilder AppendPostPityState(StringBuilder builder, GachaPityDisplayState postPityState)
        {
            builder.Append("\"postPityState\":");
            if (postPityState == null)
            {
                return builder.Append("null");
            }

            builder.Append('{');
            AppendString(builder, "bannerId", postPityState.BannerId).Append(',');
            builder.Append("\"totalPullCount\":").Append(postPityState.TotalPullCount).Append(',');
            builder.Append("\"elitePityCount\":").Append(postPityState.ElitePityCount).Append(',');
            builder.Append("\"pickupElitePityCount\":").Append(postPityState.PickupElitePityCount).Append(',');
            builder.Append("\"rareOrHigherPityCount\":").Append(postPityState.RareOrHigherPityCount).Append(',');
            builder.Append("\"remainingEliteGuaranteeCount\":").Append(postPityState.PullsUntilEliteGuarantee).Append(',');
            builder.Append("\"remainingPickupEliteGuaranteeCount\":").Append(postPityState.PullsUntilPickupEliteGuarantee).Append(',');
            builder.Append("\"remainingRareOrHigherGuaranteeCount\":").Append(postPityState.PullsUntilRareOrHigherGuarantee);
            return builder.Append('}');
        }

        private static StringBuilder AppendCost(StringBuilder builder, GachaPullCostDto cost)
        {
            builder.Append("\"cost\":");
            if (cost == null)
            {
                return builder.Append("null");
            }

            builder.Append('{');
            AppendString(builder, "itemId", cost.ItemId).Append(',');
            builder.Append("\"quantity\":").Append(cost.Quantity);
            return builder.Append('}');
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
