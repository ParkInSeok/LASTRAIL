using System;
using System.Text;

namespace LastRail.Gacha
{
    public static class GachaScreenViewStateJsonAdapter
    {
        public static string ToJson(GachaScreenViewState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "title", state.Title).Append(',');
            AppendString(builder, "kind", state.Kind.ToString()).Append(',');
            AppendString(builder, "clientOutcome", state.ClientOutcome.ToString()).Append(',');
            AppendString(builder, "statusLabel", state.StatusLabel).Append(',');
            AppendOptionalString(builder, "bannerId", state.BannerId).Append(',');
            AppendString(builder, "bannerDisplayName", state.BannerDisplayName).Append(',');
            AppendString(builder, "costLabel", state.CostLabel).Append(',');
            AppendString(builder, "onePullButtonLabel", state.OnePullButtonLabel).Append(',');
            AppendBool(builder, "isOnePullEnabled", state.IsOnePullEnabled).Append(',');
            AppendString(builder, "tenPullButtonLabel", state.TenPullButtonLabel).Append(',');
            AppendBool(builder, "isTenPullEnabled", state.IsTenPullEnabled).Append(',');
            AppendStringArray(builder, "pitySummaryLines", state.PitySummaryLines).Append(',');
            AppendString(builder, "revealMode", state.RevealMode).Append(',');
            AppendOptionalString(builder, "primaryCueId", state.PrimaryCueId).Append(',');
            builder.Append("\"resultGridEntries\":[");
            for (var i = 0; i < state.ResultGridEntries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendGridEntry(builder, state.ResultGridEntries[i]);
            }

            builder.Append("],");
            AppendOptionalString(builder, "errorCode", state.ErrorCode).Append(',');
            AppendOptionalString(builder, "errorLabel", state.ErrorLabel).Append(',');
            AppendOptionalString(builder, "errorMessage", state.ErrorMessage).Append(',');
            AppendBool(builder, "canRetryWithSameIdempotencyKey", state.CanRetryWithSameIdempotencyKey).Append(',');
            AppendBool(builder, "requiresAuthRefreshOrGuestLogin", state.RequiresAuthRefreshOrGuestLogin).Append(',');
            AppendString(builder, "primaryActionLabel", state.PrimaryActionLabel).Append(',');
            AppendBool(builder, "usesPortraitSafeArea", state.UsesPortraitSafeArea).Append(',');
            builder.Append("\"minimumTouchTargetDp\":").Append(state.MinimumTouchTargetDp).Append(',');
            AppendBool(builder, "usesServerResultDisplayFallback", state.UsesServerResultDisplayFallback).Append(',');
            AppendBool(builder, "hasOperatorRosterTruth", state.HasOperatorRosterTruth).Append(',');
            AppendBool(builder, "isPresentationOnly", state.IsPresentationOnly).Append(',');
            AppendBool(builder, "hasEconomyMutation", state.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", state.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendGridEntry(StringBuilder builder, GachaScreenResultGridEntryViewState entry)
        {
            builder.Append('{');
            AppendString(builder, "resultId", entry.ResultId).Append(',');
            builder.Append("\"pullIndex\":").Append(entry.PullIndex).Append(',');
            AppendString(builder, "displayName", entry.DisplayName).Append(',');
            AppendString(builder, "rewardId", entry.RewardId).Append(',');
            AppendString(builder, "resultKindLabel", entry.ResultKindLabel).Append(',');
            AppendString(builder, "rarityLabel", entry.RarityLabel).Append(',');
            AppendOptionalString(builder, "guaranteeType", entry.GuaranteeType).Append(',');
            AppendOptionalString(builder, "guaranteeBadgeLabel", entry.GuaranteeBadgeLabel).Append(',');
            AppendOptionalString(builder, "guaranteeBadgeCueId", entry.GuaranteeBadgeCueId).Append(',');
            AppendOptionalString(builder, "duplicateConversionLabel", entry.DuplicateConversionLabel).Append(',');
            AppendOptionalString(builder, "duplicateConversionCueId", entry.DuplicateConversionCueId).Append(',');
            AppendString(builder, "resultCardCueId", entry.ResultCardCueId).Append(',');
            AppendString(builder, "rarityFlashCueId", entry.RarityFlashCueId).Append(',');
            AppendString(builder, "soundCueId", entry.SoundCueId).Append(',');
            AppendString(builder, "vibrationCueId", entry.VibrationCueId).Append(',');
            AppendBool(builder, "isPresentationOnly", entry.IsPresentationOnly).Append(',');
            AppendBool(builder, "hasEconomyMutation", entry.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", entry.HasClientSideRng);
            builder.Append('}');
        }

        private static StringBuilder AppendStringArray(StringBuilder builder, string fieldName, System.Collections.Generic.IReadOnlyList<string> values)
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
