using System;
using System.Globalization;
using System.Text;

namespace LastRail.Presentation.MissionShell
{
    public static class RewardStoryScreenViewStateJsonAdapter
    {
        public static string ToJson(RewardStoryScreenViewState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "surface", state.Surface.ToString()).Append(',');
            AppendOptionalString(builder, "storyProgressId", state.StoryProgressId).Append(',');
            AppendInt(builder, "rewardLineCount", state.RewardLineCount).Append(',');
            AppendInt(builder, "unlockedOperationCount", state.UnlockedOperationCount).Append(',');
            AppendBool(builder, "canNavigateToCodexArchive", state.CanNavigateToCodexArchive).Append(',');
            AppendBool(builder, "hasCommittedStaminaRefund", state.HasCommittedStaminaRefund).Append(',');
            AppendOptionalString(builder, "staminaRefundStatus", state.StaminaRefundStatus).Append(',');
            AppendOptionalString(builder, "staminaRefundStringId", state.StaminaRefundStringId).Append(',');
            AppendOptionalString(builder, "errorCode", state.ErrorCode).Append(',');
            AppendBool(builder, "isNoClaimAvailable", state.IsNoClaimAvailable).Append(',');
            AppendBool(builder, "canRetryWithSameIdempotencyKey", state.CanRetryWithSameIdempotencyKey).Append(',');
            AppendBool(builder, "canRetryNetwork", state.CanRetryNetwork).Append(',');
            AppendBool(builder, "requiresAuthRefreshOrGuestLogin", state.RequiresAuthRefreshOrGuestLogin).Append(',');
            AppendBool(builder, "usesPortraitSafeArea", state.UsesPortraitSafeArea).Append(',');
            AppendInt(builder, "minimumTouchTargetDp", state.MinimumTouchTargetDp).Append(',');
            AppendString(builder, "preferredAspectRatio", state.PreferredAspectWidth + ":" + state.PreferredAspectHeight).Append(',');
            AppendBool(builder, "hasReadableTextSlots", state.HasReadableTextSlots).Append(',');
            AppendBool(builder, "requiresTextClamp", state.RequiresTextClamp).Append(',');
            AppendBool(builder, "isDisplayOnly", state.IsDisplayOnly).Append(',');
            AppendBool(builder, "hasServerTransport", state.HasServerTransport).Append(',');
            AppendBool(builder, "hasUnlockTruth", state.HasUnlockTruth).Append(',');
            AppendBool(builder, "hasStoryProgressMutation", state.HasStoryProgressMutation).Append(',');
            AppendBool(builder, "hasRewardCalculation", state.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasOfflineRewardCalculation", state.HasOfflineRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", state.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", state.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
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

        private static StringBuilder AppendInt(StringBuilder builder, string fieldName, int value)
        {
            return builder.Append('"')
                .Append(fieldName)
                .Append("\":")
                .Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static StringBuilder AppendBool(StringBuilder builder, string fieldName, bool value)
        {
            return builder.Append('"').Append(fieldName).Append("\":").Append(value ? "true" : "false");
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


