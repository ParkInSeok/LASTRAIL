using System;
using System.Text;

namespace LastRail.Gacha
{
    public static class GachaScreenPresentationStateJsonAdapter
    {
        public static string ToJson(GachaScreenPresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "kind", state.Kind.ToString()).Append(',');
            AppendString(builder, "clientOutcome", state.ClientOutcome.ToString()).Append(',');
            AppendOptionalString(builder, "bannerId", state.BannerId).Append(',');
            AppendString(builder, "bannerDisplayName", state.BannerDisplayName).Append(',');
            builder.Append("\"isIdle\":").Append(Bool(state.IsIdle)).Append(',');
            AppendButton(builder, "onePullButton", state.OnePullButton).Append(',');
            AppendButton(builder, "tenPullButton", state.TenPullButton).Append(',');
            AppendCost(builder, state.Cost).Append(',');
            AppendPostPityState(builder, state.PostPityState).Append(',');
            builder.Append("\"hasBatch\":").Append(Bool(state.HasBatch)).Append(',');
            AppendOptionalString(builder, "batchId", state.Batch == null ? null : state.Batch.BatchId).Append(',');
            builder.Append("\"revealEntryCount\":").Append(state.RevealEntries.Count).Append(',');
            AppendRevealEntries(builder, state).Append(',');
            AppendString(builder, "rejectionRecovery", state.RejectionRecovery.ToString()).Append(',');
            AppendOptionalString(builder, "errorCode", state.ErrorCode).Append(',');
            AppendOptionalString(builder, "errorMessage", state.ErrorMessage).Append(',');
            builder.Append("\"canRetryWithSameIdempotencyKey\":").Append(Bool(state.CanRetryWithSameIdempotencyKey)).Append(',');
            builder.Append("\"requiresAuthRefreshOrGuestLogin\":").Append(Bool(state.RequiresAuthRefreshOrGuestLogin)).Append(',');
            builder.Append("\"isPresentationOnly\":").Append(Bool(state.IsPresentationOnly)).Append(',');
            builder.Append("\"hasEconomyMutation\":").Append(Bool(state.HasEconomyMutation)).Append(',');
            builder.Append("\"hasClientSideRng\":").Append(Bool(state.HasClientSideRng));
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendButton(
            StringBuilder builder,
            string fieldName,
            GachaScreenPullButtonState button)
        {
            builder.Append('"').Append(fieldName).Append("\":{");
            builder.Append("\"pullCount\":").Append(button.PullCount.Value).Append(',');
            builder.Append("\"isEnabled\":").Append(Bool(button.IsEnabled)).Append(',');
            AppendString(builder, "bodyJsonPreview", button.BodyJsonPreview).Append(',');
            builder.Append("\"requiresIdempotencyKey\":").Append(Bool(button.RequiresIdempotencyKey)).Append(',');
            builder.Append("\"hasAccountShortcut\":").Append(Bool(button.HasAccountShortcut));
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

        private static StringBuilder AppendRevealEntries(StringBuilder builder, GachaScreenPresentationState state)
        {
            builder.Append("\"revealEntries\":[");
            for (var i = 0; i < state.RevealEntries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendRevealEntry(builder, state.RevealEntries[i]);
            }

            return builder.Append(']');
        }

        private static StringBuilder AppendRevealEntry(StringBuilder builder, GachaScreenRevealEntryPresentation entry)
        {
            builder.Append('{');
            AppendString(builder, "resultId", entry.ResultId).Append(',');
            builder.Append("\"pullIndex\":").Append(entry.PullIndex).Append(',');
            AppendString(builder, "resultKind", entry.ResultKind.ToString()).Append(',');
            AppendString(builder, "rarity", entry.Rarity.ToString()).Append(',');
            AppendString(builder, "rewardId", entry.RewardId).Append(',');
            AppendString(builder, "displayName", entry.DisplayName).Append(',');
            builder.Append("\"isDuplicateConversion\":").Append(Bool(entry.IsDuplicateConversion)).Append(',');
            AppendOptionalString(builder, "duplicateSourceId", entry.DuplicateSourceId).Append(',');
            AppendOptionalString(builder, "conversionItemId", entry.ConversionItemId).Append(',');
            builder.Append("\"conversionQuantity\":").Append(entry.ConversionQuantity).Append(',');
            AppendOptionalString(builder, "guaranteeType", entry.GuaranteeType).Append(',');
            AppendString(builder, "revealStartCueId", entry.RevealStartCueId).Append(',');
            AppendString(builder, "rarityFlashCueId", entry.RarityFlashCueId).Append(',');
            AppendOptionalString(builder, "guaranteeBadgeCueId", entry.GuaranteeBadgeCueId).Append(',');
            AppendOptionalString(builder, "duplicateConversionCueId", entry.DuplicateConversionCueId).Append(',');
            AppendString(builder, "resultCardCueId", entry.ResultCardCueId).Append(',');
            AppendString(builder, "soundCueId", entry.SoundCueId).Append(',');
            AppendString(builder, "vibrationCueId", entry.VibrationCueId).Append(',');
            builder.Append("\"hasGuaranteeBadge\":").Append(Bool(entry.HasGuaranteeBadge)).Append(',');
            builder.Append("\"hasDuplicateConversionCue\":").Append(Bool(entry.HasDuplicateConversionCue)).Append(',');
            builder.Append("\"hasEconomyMutation\":").Append(Bool(entry.HasEconomyMutation)).Append(',');
            builder.Append("\"hasClientSideRng\":").Append(Bool(entry.HasClientSideRng));
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

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
