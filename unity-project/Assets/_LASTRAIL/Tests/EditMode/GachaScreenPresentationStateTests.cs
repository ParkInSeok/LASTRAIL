using System;
using System.Collections.Generic;
using NUnit.Framework;
using LastRail.Gacha;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaScreenPresentationStateTests
    {
        [Test]
        public void FromUxState_AcceptedCreatesResultRevealScreenWithButtonsPityCostAndCueEntries()
        {
            var uxState = GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(Response()));

            var screen = GachaScreenPresentationState.FromUxState(uxState, "Act 1 구조 신호");

            Assert.That(screen.Kind, Is.EqualTo(GachaPullUxStateKind.ResultReviewReady));
            Assert.That(screen.BannerId, Is.EqualTo("banner_act1_launch"));
            Assert.That(screen.BannerDisplayName, Is.EqualTo("Act 1 구조 신호"));
            Assert.That(screen.OnePullButton.PullCount.Value, Is.EqualTo(1));
            Assert.That(screen.OnePullButton.BodyJsonPreview, Is.EqualTo("{\"pullCount\":1}"));
            Assert.That(screen.TenPullButton.PullCount.Value, Is.EqualTo(10));
            Assert.That(screen.TenPullButton.BodyJsonPreview, Is.EqualTo("{\"pullCount\":10}"));
            Assert.That(screen.OnePullButton.RequiresIdempotencyKey, Is.EqualTo(true));
            Assert.That(screen.OnePullButton.HasAccountShortcut, Is.EqualTo(false));
            Assert.That(screen.Cost.ItemId, Is.EqualTo("cur_command_ticket"));
            Assert.That(screen.Cost.Quantity, Is.EqualTo(10));
            Assert.That(screen.PostPityState.PullsUntilEliteGuarantee, Is.EqualTo(18));
            Assert.That(screen.RevealEntries.Count, Is.EqualTo(10));
            Assert.That(screen.RevealEntries[0].GuaranteeType, Is.EqualTo("PICKUP_ELITE_60"));
            Assert.That(screen.RevealEntries[0].Cue.GuaranteeBadgeCueId, Is.EqualTo("gacha_badge_pickup_elite_60"));
            Assert.That(screen.RevealEntries[1].IsDuplicateConversion, Is.EqualTo(true));
            Assert.That(screen.RevealEntries[1].Cue.DuplicateConversionCueId, Is.EqualTo("gacha_duplicate_conversion"));
            Assert.That(screen.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(screen.HasClientSideRng, Is.EqualTo(false));
            Assert.That(screen.IsPresentationOnly, Is.EqualTo(true));
        }

        [Test]
        public void FromUxState_DuplicateReplayKeepsPreviousResultSyncRevealEntries()
        {
            var uxState = GachaPullUxState.FromClientResult(GachaPullClientResult.DuplicateReplay(
                Response(GachaPullResultStatus.DuplicateReplay, accepted: false, errorCode: "duplicate_replay")));

            var screen = GachaScreenPresentationState.FromUxState(uxState, null);

            Assert.That(screen.Kind, Is.EqualTo(GachaPullUxStateKind.RecognizedDuplicate));
            Assert.That(screen.BannerDisplayName, Is.EqualTo("banner_act1_launch"));
            Assert.That(screen.ErrorCode, Is.EqualTo("duplicate_replay"));
            Assert.That(screen.RevealEntries.Count, Is.EqualTo(10));
            Assert.That(screen.RevealEntries[0].ResultCardCueId, Is.EqualTo("gacha_card_operator"));
            Assert.That(screen.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(screen.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
        }

        [Test]
        public void FromUxState_RejectedAuthAndNetworkStatesDoNotCreateRevealEntries()
        {
            var recoverable = GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Rejected(Rejected("insufficient_command_ticket", "지휘권 부족"))),
                "Act 1 구조 신호");
            var authRequired = GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Rejected(Rejected("auth_required", "auth required"))),
                "Act 1 구조 신호");
            var network = GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.NetworkError("timeout")),
                "Act 1 구조 신호");

            Assert.That(recoverable.Kind, Is.EqualTo(GachaPullUxStateKind.RecoverableRejected));
            Assert.That(recoverable.RevealEntries.Count, Is.EqualTo(0));
            Assert.That(recoverable.ErrorCode, Is.EqualTo("insufficient_command_ticket"));
            Assert.That(authRequired.Kind, Is.EqualTo(GachaPullUxStateKind.AuthRequired));
            Assert.That(authRequired.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(authRequired.RevealEntries.Count, Is.EqualTo(0));
            Assert.That(network.Kind, Is.EqualTo(GachaPullUxStateKind.RetryableNetworkError));
            Assert.That(network.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(network.RevealEntries.Count, Is.EqualTo(0));
            Assert.That(network.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(network.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void Idle_CreatesBannerButtonsAndPityWithoutPullResultTruth()
        {
            var state = GachaScreenPresentationState.Idle(
                "banner_act1_launch",
                "Act 1 구조 신호",
                new GachaPullCostDto("cur_command_ticket", 1),
                new GachaPityDisplayState("banner_act1_launch", 29, 29, 59, 9));

            Assert.That(state.Kind, Is.EqualTo(GachaPullUxStateKind.ResultReviewReady));
            Assert.That(state.IsIdle, Is.EqualTo(true));
            Assert.That(state.HasBatch, Is.EqualTo(false));
            Assert.That(state.RevealEntries.Count, Is.EqualTo(0));
            Assert.That(state.OnePullButton.IsEnabled, Is.EqualTo(true));
            Assert.That(state.TenPullButton.IsEnabled, Is.EqualTo(true));
            Assert.That(state.PostPityState.PullsUntilEliteGuarantee, Is.EqualTo(1));
            Assert.That(state.PostPityState.PullsUntilPickupEliteGuarantee, Is.EqualTo(1));
            Assert.That(state.PostPityState.PullsUntilRareOrHigherGuarantee, Is.EqualTo(1));
        }

        [Test]
        public void ToJson_WritesStableDebugFieldsWithoutSensitiveOrCalculationPayload()
        {
            var screen = GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(Response())),
                "Act 1 구조 신호");

            var json = GachaScreenPresentationStateJsonAdapter.ToJson(screen);

            Assert.That(json.Contains("\"kind\":\"ResultReviewReady\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"bannerId\":\"banner_act1_launch\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"onePullButton\":{\"pullCount\":1,\"isEnabled\":true,\"bodyJsonPreview\":\"{\\\"pullCount\\\":1}\",\"requiresIdempotencyKey\":true,\"hasAccountShortcut\":false}"), Is.EqualTo(true));
            Assert.That(json.Contains("\"tenPullButton\":{\"pullCount\":10,\"isEnabled\":true,\"bodyJsonPreview\":\"{\\\"pullCount\\\":10}\",\"requiresIdempotencyKey\":true,\"hasAccountShortcut\":false}"), Is.EqualTo(true));
            Assert.That(json.Contains("\"guaranteeBadgeCueId\":\"gacha_badge_pickup_elite_60\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"duplicateConversionCueId\":\"gacha_duplicate_conversion\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasEconomyMutation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasClientSideRng\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("fullWeights"), Is.EqualTo(false));
            Assert.That(json.Contains("rngSeed"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
        }

        [Test]
        public void FactoriesRejectInvalidInputs()
        {
            var uxState = GachaPullUxState.FromClientResult(GachaPullClientResult.NetworkError("timeout"));

            Assert.Throws<ArgumentNullException>(() => GachaScreenPresentationState.FromUxState(null, "banner"));
            Assert.Throws<ArgumentException>(() => GachaScreenPresentationState.Idle("", "banner", new GachaPullCostDto("cur_command_ticket", 1), new GachaPityDisplayState("banner", 0, 0, 0, 0)));
            Assert.Throws<ArgumentNullException>(() => GachaScreenPresentationState.Idle("banner", "banner", null, new GachaPityDisplayState("banner", 0, 0, 0, 0)));
            Assert.Throws<ArgumentNullException>(() => GachaScreenPresentationState.Idle("banner", "banner", new GachaPullCostDto("cur_command_ticket", 1), null));
            Assert.Throws<ArgumentNullException>(() => GachaScreenPresentationStateJsonAdapter.ToJson(null));
            Assert.That(GachaScreenPresentationState.FromUxState(uxState, "").BannerDisplayName, Is.EqualTo("모집 배너"));
        }

        private static GachaPullResponseDto Response(
            GachaPullResultStatus resultStatus = GachaPullResultStatus.Accepted,
            bool accepted = true,
            string errorCode = null)
        {
            return new GachaPullResponseDto(
                "banner_act1_launch",
                accepted,
                "batch_001",
                resultStatus,
                GachaPullCount.FromValue(10),
                "1",
                new GachaPullCostDto("cur_command_ticket", 10),
                new GachaPostPityStateDto(42, 12, 22, 2, 4),
                TenPullResults(),
                new DateTime(2026, 8, 28, 10, 0, 0, DateTimeKind.Utc),
                errorCode,
                null);
        }

        private static GachaPullResponseDto Rejected(string errorCode, string message)
        {
            return new GachaPullResponseDto(
                "banner_act1_launch",
                false,
                null,
                GachaPullResultStatus.Rejected,
                GachaPullCount.FromValue(1),
                "1",
                new GachaPullCostDto("cur_command_ticket", 1),
                new GachaPostPityStateDto(0, 0, 0, 0, 1),
                new List<GachaPullResultDto>(),
                new DateTime(2026, 8, 28, 10, 0, 0, DateTimeKind.Utc),
                errorCode,
                message);
        }

        private static IReadOnlyList<GachaPullResultDto> TenPullResults()
        {
            return new[]
            {
                new GachaPullResultDto(
                    1,
                    GachaRarity.Elite,
                    GachaPullResultKind.Operator,
                    "result_001",
                    new GachaRewardDto("OPERATOR", "op_vex", 1),
                    true,
                    "PICKUP_ELITE_60",
                    false,
                    null),
                new GachaPullResultDto(
                    2,
                    GachaRarity.Rare,
                    GachaPullResultKind.OperatorToken,
                    "result_002",
                    new GachaRewardDto("OPERATOR_TOKEN", "cur_operator_token", 50),
                    false,
                    "RARE_OR_HIGHER_10",
                    true,
                    new GachaDuplicateConversionDto("op_vex", "cur_operator_token", 50)),
                CommonResult(3),
                CommonResult(4),
                CommonResult(5),
                CommonResult(6),
                CommonResult(7),
                CommonResult(8),
                CommonResult(9),
                CommonResult(10)
            };
        }

        private static GachaPullResultDto CommonResult(int pullSequence)
        {
            return new GachaPullResultDto(
                pullSequence,
                GachaRarity.Common,
                GachaPullResultKind.Item,
                "result_" + pullSequence.ToString("000"),
                new GachaRewardDto("ITEM", "item_scrap", 1),
                false,
                "NONE",
                false,
                null);
        }
    }
}
