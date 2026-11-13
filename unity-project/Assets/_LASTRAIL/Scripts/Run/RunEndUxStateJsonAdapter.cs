using System;
using System.Text;

namespace LastRail.Run
{
    public static class RunEndUxStateJsonAdapter
    {
        public static string ToJson(RunEndUxState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "kind", state.Kind.ToString()).Append(',');
            AppendString(builder, "submitOutcome", state.SubmitOutcome.ToString()).Append(',');
            builder.Append("\"hasRewardReviewEntry\":").Append(state.RewardReviewEntry == null ? "false" : "true").Append(',');
            AppendRewardReviewEntry(builder, state.RewardReviewEntry).Append(',');
            AppendOptionalString(builder, "recognizedRewardPreviewId", state.RecognizedRewardPreviewId).Append(',');
            builder.Append("\"hasRecognizedRewardPreviewId\":").Append(state.HasRecognizedRewardPreviewId ? "true" : "false").Append(',');
            builder.Append("\"hasStaminaRefund\":").Append(state.HasStaminaRefund ? "true" : "false").Append(',');
            AppendStaminaRefund(builder, state.StaminaRefund).Append(',');
            AppendOptionalString(builder, "errorCode", state.ErrorCode).Append(',');
            AppendOptionalString(builder, "errorMessage", state.ErrorMessage).Append(',');
            builder.Append("\"canRetryWithSameIdempotencyKey\":").Append(state.CanRetryWithSameIdempotencyKey ? "true" : "false").Append(',');
            builder.Append("\"hasRewardAmounts\":").Append(state.HasRewardAmounts ? "true" : "false");
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendRewardReviewEntry(StringBuilder builder, RunRewardReviewEntryState entry)
        {
            builder.Append("\"rewardReviewEntry\":");
            if (entry == null)
            {
                return builder.Append("null");
            }

            builder.Append('{');
            AppendString(builder, "runId", entry.RunId).Append(',');
            AppendString(builder, "balanceVersion", entry.BalanceVersion).Append(',');
            AppendString(builder, "resultStatus", entry.ResultStatus).Append(',');
            AppendString(builder, "submitOutcome", entry.SubmitOutcome.ToString()).Append(',');
            AppendOptionalString(builder, "rewardClaimId", entry.RewardClaimId).Append(',');
            AppendOptionalString(builder, "rewardPreviewId", entry.RewardPreviewId).Append(',');
            builder.Append("\"hasRewardClaimId\":").Append(entry.HasRewardClaimId ? "true" : "false").Append(',');
            builder.Append("\"hasRewardPreviewId\":").Append(entry.HasRewardPreviewId ? "true" : "false").Append(',');
            builder.Append("\"hasRewardAmounts\":").Append(entry.HasRewardAmounts ? "true" : "false");
            return builder.Append('}');
        }

        private static StringBuilder AppendStaminaRefund(StringBuilder builder, RunStaminaRefundMarker marker)
        {
            builder.Append("\"staminaRefund\":");
            if (marker == null)
            {
                return builder.Append("null");
            }

            builder.Append('{');
            AppendOptionalString(builder, "status", marker.Status).Append(',');
            AppendOptionalString(builder, "message", marker.Message).Append(',');
            builder.Append("\"hasRewardCalculation\":").Append(marker.HasRewardCalculation ? "true" : "false").Append(',');
            builder.Append("\"hasEconomyMutation\":").Append(marker.HasEconomyMutation ? "true" : "false");
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
