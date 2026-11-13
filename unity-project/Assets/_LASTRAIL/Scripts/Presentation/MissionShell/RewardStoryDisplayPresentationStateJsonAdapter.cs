using System;
using System.Text;

namespace LastRail.Presentation.MissionShell
{
    public static class RewardStoryDisplayPresentationStateJsonAdapter
    {
        public static string ToJson(RewardStoryDisplayPresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "surface", state.Surface.ToString()).Append(',');
            AppendRunReward(builder, state.RunReward).Append(',');
            AppendOfflineReward(builder, state.OfflineReward).Append(',');
            AppendStoryProgress(builder, state.StoryProgress).Append(',');
            builder.Append("\"isBlockingDisplay\":").Append(Bool(state.IsBlockingDisplay)).Append(',');
            builder.Append("\"isPresentationOnly\":").Append(Bool(state.IsPresentationOnly)).Append(',');
            builder.Append("\"hasRewardAmountCalculation\":").Append(Bool(state.HasRewardAmountCalculation)).Append(',');
            builder.Append("\"hasMultiplierCalculation\":").Append(Bool(state.HasMultiplierCalculation)).Append(',');
            builder.Append("\"hasUnlockTruthCalculation\":").Append(Bool(state.HasUnlockTruthCalculation)).Append(',');
            builder.Append("\"hasEconomyMutation\":").Append(Bool(state.HasEconomyMutation));
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendRunReward(StringBuilder builder, RunRewardDisplaySection runReward)
        {
            builder.Append("\"runReward\":{");
            AppendOptionalString(builder, "kind", runReward.Kind.HasValue ? runReward.Kind.Value.ToString() : null).Append(',');
            AppendOptionalString(builder, "submitOutcome", runReward.SubmitOutcome.HasValue ? runReward.SubmitOutcome.Value.ToString() : null).Append(',');
            AppendOptionalString(builder, "recognizedRewardPreviewId", runReward.RecognizedRewardPreviewId).Append(',');
            builder.Append("\"hasRecognizedRewardPreviewId\":").Append(Bool(runReward.HasRecognizedRewardPreviewId)).Append(',');
            builder.Append("\"hasStaminaRefund\":").Append(Bool(runReward.HasStaminaRefund)).Append(',');
            AppendOptionalString(builder, "staminaRefundStatus", runReward.StaminaRefundStatus).Append(',');
            AppendOptionalString(builder, "staminaRefundMessage", runReward.StaminaRefundMessage).Append(',');
            AppendOptionalString(builder, "previewOutcome", runReward.PreviewOutcome.HasValue ? runReward.PreviewOutcome.Value.ToString() : null).Append(',');
            AppendOptionalString(builder, "previewStatus", runReward.PreviewStatus).Append(',');
            AppendOptionalString(builder, "claimability", runReward.Claimability).Append(',');
            AppendOptionalString(builder, "claimOutcome", runReward.ClaimOutcome.HasValue ? runReward.ClaimOutcome.Value.ToString() : null).Append(',');
            AppendOptionalString(builder, "claimStatus", runReward.ClaimStatus).Append(',');
            builder.Append("\"rewardLineCount\":").Append(runReward.RewardLines.Count).Append(',');
            AppendRewardLines(builder, "rewardLines", runReward.RewardLines).Append(',');
            builder.Append("\"hasBalanceAfterDiagnostic\":").Append(Bool(runReward.HasBalanceAfterDiagnostic)).Append(',');
            AppendOptionalString(builder, "errorCode", runReward.ErrorCode).Append(',');
            AppendOptionalString(builder, "errorMessage", runReward.ErrorMessage).Append(',');
            builder.Append("\"canRetryWithSameIdempotencyKey\":").Append(Bool(runReward.CanRetryWithSameIdempotencyKey)).Append(',');
            builder.Append("\"canRetryNetwork\":").Append(Bool(runReward.CanRetryNetwork)).Append(',');
            builder.Append("\"requiresAuthRefreshOrGuestLogin\":").Append(Bool(runReward.RequiresAuthRefreshOrGuestLogin)).Append(',');
            builder.Append("\"hasRewardAmounts\":").Append(Bool(runReward.HasRewardAmounts));
            return builder.Append('}');
        }

        private static StringBuilder AppendOfflineReward(StringBuilder builder, OfflineRewardDisplaySection offlineReward)
        {
            builder.Append("\"offlineReward\":{");
            AppendOptionalString(builder, "previewOutcome", offlineReward.PreviewOutcome.HasValue ? offlineReward.PreviewOutcome.Value.ToString() : null).Append(',');
            AppendOptionalString(builder, "previewStatus", offlineReward.PreviewStatus).Append(',');
            AppendOptionalString(builder, "claimOutcome", offlineReward.ClaimOutcome.HasValue ? offlineReward.ClaimOutcome.Value.ToString() : null).Append(',');
            AppendOptionalString(builder, "claimStatus", offlineReward.ClaimStatus).Append(',');
            AppendOptionalString(builder, "storyProgressId", offlineReward.StoryProgressId).Append(',');
            AppendOptionalString(builder, "storyProgressLabel", offlineReward.StoryProgressLabel).Append(',');
            builder.Append("\"claimableSeconds\":").Append(offlineReward.ClaimableSeconds).Append(',');
            builder.Append("\"claimableHours\":").Append(offlineReward.ClaimableHours.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"offlineStorageCapHours\":").Append(offlineReward.OfflineStorageCapHours).Append(',');
            AppendOptionalString(builder, "offlineStorageCapLabel", offlineReward.OfflineStorageCapLabel).Append(',');
            builder.Append("\"minimumClaimSeconds\":").Append(offlineReward.MinimumClaimSeconds).Append(',');
            builder.Append("\"maxClaimableHours\":").Append(offlineReward.MaxClaimableHours).Append(',');
            builder.Append("\"rewardLineCount\":").Append(offlineReward.RewardLines.Count).Append(',');
            AppendRewardLines(builder, "rewardLines", offlineReward.RewardLines).Append(',');
            builder.Append("\"hasBalanceAfterDiagnostic\":").Append(Bool(offlineReward.HasBalanceAfterDiagnostic)).Append(',');
            AppendOptionalString(builder, "errorCode", offlineReward.ErrorCode).Append(',');
            AppendOptionalString(builder, "errorMessage", offlineReward.ErrorMessage).Append(',');
            builder.Append("\"canRetryNetwork\":").Append(Bool(offlineReward.CanRetryNetwork)).Append(',');
            builder.Append("\"requiresAuthRefreshOrGuestLogin\":").Append(Bool(offlineReward.RequiresAuthRefreshOrGuestLogin)).Append(',');
            builder.Append("\"isNoClaimAvailable\":").Append(Bool(offlineReward.IsNoClaimAvailable)).Append(',');
            builder.Append("\"hasClientCalculation\":").Append(Bool(offlineReward.HasClientCalculation)).Append(',');
            builder.Append("\"hasEconomyGrant\":").Append(Bool(offlineReward.HasEconomyGrant));
            return builder.Append('}');
        }

        private static StringBuilder AppendStoryProgress(StringBuilder builder, StoryProgressDisplaySection storyProgress)
        {
            builder.Append("\"storyProgress\":{");
            AppendOptionalString(builder, "actId", storyProgress.ActId).Append(',');
            AppendOptionalString(builder, "highestStoryProgressId", storyProgress.HighestStoryProgressId).Append(',');
            AppendOptionalString(builder, "highestStoryProgressLabel", storyProgress.HighestStoryProgressLabel).Append(',');
            AppendOptionalString(builder, "highestClearedOperationId", storyProgress.HighestClearedOperationId).Append(',');
            builder.Append("\"order\":").Append(storyProgress.Order).Append(',');
            AppendStringArray(builder, "clearedOperationIds", storyProgress.ClearedOperationIds).Append(',');
            builder.Append("\"unlockedOperationCount\":").Append(storyProgress.UnlockedOperationCount).Append(',');
            AppendStringArray(builder, "unlockedOperations", storyProgress.UnlockedOperations).Append(',');
            AppendOptionalString(builder, "narrativeVersion", storyProgress.NarrativeVersion).Append(',');
            AppendOptionalString(builder, "contentStatus", storyProgress.ContentStatus).Append(',');
            AppendOptionalString(builder, "localizationStatus", storyProgress.LocalizationStatus).Append(',');
            AppendOptionalString(builder, "fixtureStatus", storyProgress.FixtureStatus).Append(',');
            AppendOptionalString(builder, "missingContentStatus", storyProgress.MissingContentStatus).Append(',');
            builder.Append("\"hasUnlockTruthCalculation\":").Append(Bool(storyProgress.HasUnlockTruthCalculation));
            return builder.Append('}');
        }

        private static StringBuilder AppendRewardLines(
            StringBuilder builder,
            string fieldName,
            System.Collections.Generic.IReadOnlyList<RewardStoryRewardLinePresentation> rewardLines)
        {
            builder.Append('"').Append(fieldName).Append("\":[");
            for (var i = 0; i < rewardLines.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var line = rewardLines[i];
                builder.Append('{');
                AppendOptionalString(builder, "rewardLineId", line.RewardLineId).Append(',');
                AppendOptionalString(builder, "rewardCode", line.RewardCode).Append(',');
                AppendOptionalString(builder, "displayName", line.DisplayName).Append(',');
                builder.Append("\"quantity\":").Append(line.Quantity).Append(',');
                AppendOptionalString(builder, "unitLabel", line.UnitLabel).Append(',');
                AppendOptionalString(builder, "source", line.Source).Append(',');
                AppendNullableInt(builder, "balanceAfter", line.BalanceAfter).Append(',');
                builder.Append("\"hasBalanceAfter\":").Append(Bool(line.HasBalanceAfter));
                builder.Append('}');
            }

            return builder.Append(']');
        }

        private static StringBuilder AppendStringArray(
            StringBuilder builder,
            string fieldName,
            System.Collections.Generic.IReadOnlyList<string> values)
        {
            builder.Append('"').Append(fieldName).Append("\":[");
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendJsonString(builder, values[i]);
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

        private static StringBuilder AppendNullableInt(StringBuilder builder, string fieldName, int? value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return value.HasValue ? builder.Append(value.Value) : builder.Append("null");
        }

        private static StringBuilder AppendJsonString(StringBuilder builder, string value)
        {
            return builder.Append('"').Append(Escape(value)).Append('"');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
