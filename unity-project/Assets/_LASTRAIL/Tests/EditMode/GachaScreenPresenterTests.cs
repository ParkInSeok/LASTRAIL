using System;
using System.Collections.Generic;
using NUnit.Framework;
using LastRail.Gacha;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaScreenPresenterTests
    {
        [Test]
        public void RenderIdle_BuildsPickupBannerButtonsAndPityViewWithoutTruthCalculation()
        {
            var view = new CapturingGachaScreenView();
            var presenter = new GachaScreenPresenter(view);

            presenter.Render(GachaScreenPresentationState.Idle(
                "banner_act1_launch",
                "Act 1 구조 신호",
                new GachaPullCostDto("cur_command_ticket", 1),
                new GachaPityDisplayState("banner_act1_launch", 29, 29, 59, 9)));

            Assert.That(view.LastState.Title, Is.EqualTo("요원 모집"));
            Assert.That(view.LastState.BannerDisplayName, Is.EqualTo("Act 1 구조 신호"));
            Assert.That(view.LastState.CostLabel, Is.EqualTo("cur_command_ticket x1"));
            Assert.That(view.LastState.OnePullButtonLabel, Is.EqualTo("1회 모집"));
            Assert.That(view.LastState.TenPullButtonLabel, Is.EqualTo("10회 모집"));
            Assert.That(view.LastState.IsOnePullEnabled, Is.EqualTo(true));
            Assert.That(view.LastState.IsTenPullEnabled, Is.EqualTo(true));
            Assert.That(view.LastState.PitySummaryLines[0], Is.EqualTo("희귀 이상 보장까지 1회"));
            Assert.That(view.LastState.PitySummaryLines[1], Is.EqualTo("정예 확정까지 1회"));
            Assert.That(view.LastState.PitySummaryLines[2], Is.EqualTo("픽업 정예 확정까지 1회"));
            Assert.That(view.LastState.RevealMode, Is.EqualTo("Idle"));
            Assert.That(view.LastState.ResultGridEntries.Count, Is.EqualTo(0));
            Assert.That(view.LastState.UsesPortraitSafeArea, Is.EqualTo(true));
            Assert.That(view.LastState.MinimumTouchTargetDp, Is.EqualTo(48));
            Assert.That(view.LastState.IsPresentationOnly, Is.EqualTo(true));
            Assert.That(view.LastState.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(view.LastState.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void RenderAcceptedTenPull_BuildsQuickRevealAndResultGridFromServerResultEntries()
        {
            var view = new CapturingGachaScreenView();
            var presenter = new GachaScreenPresenter(view);

            presenter.Render(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(Response())),
                "Act 1 구조 신호"));

            Assert.That(view.LastState.Kind, Is.EqualTo(GachaPullUxStateKind.ResultReviewReady));
            Assert.That(view.LastState.RevealMode, Is.EqualTo("QuickTenPullGrid"));
            Assert.That(view.LastState.PrimaryCueId, Is.EqualTo("gacha_reveal_start"));
            Assert.That(view.LastState.ResultGridEntries.Count, Is.EqualTo(10));
            Assert.That(view.LastState.ResultGridEntries[0].ResultId, Is.EqualTo("result_001"));
            Assert.That(view.LastState.ResultGridEntries[0].DisplayName, Is.EqualTo("op_vex"));
            Assert.That(view.LastState.ResultGridEntries[0].RarityLabel, Is.EqualTo("정예"));
            Assert.That(view.LastState.ResultGridEntries[0].ResultKindLabel, Is.EqualTo("요원"));
            Assert.That(view.LastState.ResultGridEntries[0].GuaranteeBadgeLabel, Is.EqualTo("60뽑 픽업 정예 확정"));
            Assert.That(view.LastState.ResultGridEntries[0].GuaranteeBadgeCueId, Is.EqualTo("gacha_badge_pickup_elite_60"));
            Assert.That(view.LastState.ResultGridEntries[1].DuplicateConversionLabel, Is.EqualTo("중복 전환: cur_operator_token x50"));
            Assert.That(view.LastState.ResultGridEntries[1].DuplicateConversionCueId, Is.EqualTo("gacha_duplicate_conversion"));
            Assert.That(view.LastState.UsesServerResultDisplayFallback, Is.EqualTo(true));
            Assert.That(view.LastState.HasOperatorRosterTruth, Is.EqualTo(false));
            Assert.That(view.LastState.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(view.LastState.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void RenderDuplicateRejectedAuthAndNetwork_SeparatesUxFlagsWithoutRevealGridTruth()
        {
            var view = new CapturingGachaScreenView();
            var presenter = new GachaScreenPresenter(view);

            presenter.Render(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.DuplicateReplay(
                    Response(GachaPullResultStatus.DuplicateReplay, false, "duplicate_replay"))),
                "Act 1 구조 신호"));
            Assert.That(view.LastState.Kind, Is.EqualTo(GachaPullUxStateKind.RecognizedDuplicate));
            Assert.That(view.LastState.StatusLabel, Is.EqualTo("이전 모집 결과 동기화"));
            Assert.That(view.LastState.RevealMode, Is.EqualTo("QuickTenPullGrid"));
            Assert.That(view.LastState.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));

            presenter.Render(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Rejected(
                    Rejected("insufficient_command_ticket", "지휘권이 부족합니다."))),
                "Act 1 구조 신호"));
            Assert.That(view.LastState.Kind, Is.EqualTo(GachaPullUxStateKind.RecoverableRejected));
            Assert.That(view.LastState.StatusLabel, Is.EqualTo("복구 가능 모집 오류"));
            Assert.That(view.LastState.ErrorLabel, Is.EqualTo("지휘권 부족"));
            Assert.That(view.LastState.ResultGridEntries.Count, Is.EqualTo(0));

            presenter.Render(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Rejected(
                    Rejected("auth_required", "auth required"))),
                "Act 1 구조 신호"));
            Assert.That(view.LastState.Kind, Is.EqualTo(GachaPullUxStateKind.AuthRequired));
            Assert.That(view.LastState.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));

            presenter.Render(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.NetworkError("timeout")),
                "Act 1 구조 신호"));
            Assert.That(view.LastState.Kind, Is.EqualTo(GachaPullUxStateKind.RetryableNetworkError));
            Assert.That(view.LastState.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(view.LastState.PrimaryActionLabel, Is.EqualTo("같은 요청으로 재시도"));
            Assert.That(view.LastState.ResultGridEntries.Count, Is.EqualTo(0));
        }

        [Test]
        public void ToDebugJson_WritesStableDisplayFieldsWithoutSensitiveTruthPayload()
        {
            var view = new CapturingGachaScreenView();
            var presenter = new GachaScreenPresenter(view);
            var state = presenter.Render(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(Response())),
                "Act 1 구조 신호"));

            var json = GachaScreenViewStateJsonAdapter.ToJson(state);

            Assert.That(json.Contains("\"title\":\"요원 모집\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"revealMode\":\"QuickTenPullGrid\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"minimumTouchTargetDp\":48"), Is.EqualTo(true));
            Assert.That(json.Contains("\"guaranteeBadgeLabel\":\"60뽑 픽업 정예 확정\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasOperatorRosterTruth\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasEconomyMutation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasClientSideRng\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("fullWeights"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
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
                new DateTime(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc),
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
                Array.Empty<GachaPullResultDto>(),
                new DateTime(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc),
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

        private sealed class CapturingGachaScreenView : IGachaScreenView
        {
            public GachaScreenViewState LastState { get; private set; }

            public void Render(GachaScreenViewState state)
            {
                LastState = state;
            }
        }
    }
}
