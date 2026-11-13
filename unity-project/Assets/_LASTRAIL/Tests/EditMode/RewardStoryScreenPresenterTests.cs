using System;
using NUnit.Framework;
using LastRail.Narrative;
using LastRail.OfflineRewards;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using OfflineLine = LastRail.OfflineRewards.OfflineRewardLineDto;
using RunLine = LastRail.Run.RunRewardLineDto;

namespace LastRail.Tests.EditMode
{
    public sealed class RewardStoryScreenPresenterTests
    {
        [Test]
        public void RenderRewardReview_BuildsMobileDisplayOnlyStateWithCommittedStaminaRefundHint()
        {
            var view = new CapturingRewardStoryScreenView();
            var presenter = new RewardStoryScreenPresenter(view);
            var presentation = BuildPresentation(
                RunEndUxState.FromSubmitPolicy(RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                    RunApiFlowSubmitOutcome.Accepted,
                    RunFinishResponse(new RunStaminaRefundMarker("COMMITTED", "Story raid failure stamina refund committed.")),
                    null))),
                OfflineRewardClaimClientResult.Claimed(OfflineClaim()));

            var state = presenter.RenderRewardReview(presentation);

            Assert.That(view.LastState, Is.SameAs(state));
            Assert.That(state.Surface, Is.EqualTo(MissionShellSurface.RewardReview));
            Assert.That(state.Title, Is.EqualTo("작전 보상"));
            Assert.That(state.RewardLineCount, Is.EqualTo(1));
            Assert.That(state.HasCommittedStaminaRefund, Is.EqualTo(true));
            Assert.That(state.StaminaRefundStatus, Is.EqualTo("COMMITTED"));
            Assert.That(state.StaminaRefundStringId, Is.EqualTo("narrative.raid_raider_king_engine.failure_refund.toast"));
            Assert.That(state.BodyLineStringIds[0], Is.EqualTo("narrative.raid_raider_king_engine.failure_refund.toast"));
            Assert.That(state.UsesPortraitSafeArea, Is.EqualTo(true));
            Assert.That(state.MinimumTouchTargetDp, Is.EqualTo(48));
            Assert.That(state.PreferredAspectWidth, Is.EqualTo(9));
            Assert.That(state.PreferredAspectHeight, Is.EqualTo(16));
            Assert.That(state.HasReadableTextSlots, Is.EqualTo(true));
            Assert.That(state.RequiresTextClamp, Is.EqualTo(true));
            Assert.That(state.IsDisplayOnly, Is.EqualTo(true));
            Assert.That(state.HasRewardCalculation, Is.EqualTo(false));
            Assert.That(state.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(state.HasServerTransport, Is.EqualTo(false));
        }

        [Test]
        public void RenderOfflineReward_SeparatesClaimableNoClaimAuthAndNetworkFlagsForPlayerDisplay()
        {
            var view = new CapturingRewardStoryScreenView();
            var presenter = new RewardStoryScreenPresenter(view);
            var presentation = BuildPresentation(
                null,
                OfflineRewardClaimClientResult.Rejected("offline_reward_not_claimable", "아직 수령할 보상이 없습니다."));

            var state = presenter.RenderOfflineReward(presentation);

            Assert.That(state.Surface, Is.EqualTo(MissionShellSurface.OfflineReward));
            Assert.That(state.BodyLines, Does.Contain("오프라인 반복 보상에서는 프리미엄 재화를 지급하지 않습니다."));
            Assert.That(state.Title, Is.EqualTo("오프라인 보상"));
            Assert.That(state.StoryProgressId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(state.StoryProgressLabel, Is.EqualTo("급수 정차"));
            Assert.That(state.OfflineStorageCapLabel, Is.EqualTo("최대 7시간"));
            Assert.That(state.RewardLineCount, Is.EqualTo(1));
            Assert.That(state.IsNoClaimAvailable, Is.EqualTo(true));
            Assert.That(state.ErrorCode, Is.EqualTo("offline_reward_not_claimable"));
            Assert.That(state.PrimaryActionLabel, Is.EqualTo("보상 다시 확인"));
            Assert.That(state.CanRetryNetwork, Is.EqualTo(false));
            Assert.That(state.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
        }

        [Test]
        public void RenderStoryProgress_BuildsUnlockSummaryWithoutCalculatingUnlockTruth()
        {
            var view = new CapturingRewardStoryScreenView();
            var presenter = new RewardStoryScreenPresenter(view);
            var presentation = BuildPresentation(null, null);

            var state = presenter.RenderStoryProgress(presentation);

            Assert.That(state.Surface, Is.EqualTo(MissionShellSurface.StoryProgressSummary));
            Assert.That(state.Title, Is.EqualTo("스토리 진행"));
            Assert.That(state.StoryProgressId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(state.StoryProgressLabel, Is.EqualTo("급수 정차"));
            Assert.That(state.UnlockedOperationCount, Is.EqualTo(4));
            Assert.That(state.CodexUnlockStatusLabel, Is.EqualTo("기록 4개 열람 가능"));
            Assert.That(state.PrimaryActionLabel, Is.EqualTo("기록 보관소"));
            Assert.That(state.CanNavigateToCodexArchive, Is.EqualTo(true));
            Assert.That(state.HasUnlockTruth, Is.EqualTo(false));
            Assert.That(state.HasStoryProgressMutation, Is.EqualTo(false));
        }

        [Test]
        public void ToDebugJson_WritesIdsStatusesAndCountsWithoutAmountsTokensTruthOrServerPayload()
        {
            var presenter = new RewardStoryScreenPresenter(new CapturingRewardStoryScreenView());
            var state = presenter.RenderRewardReview(BuildPresentation(
                RunEndUxState.FromSubmitPolicy(RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                    RunApiFlowSubmitOutcome.Accepted,
                    RunFinishResponse(new RunStaminaRefundMarker("COMMITTED", "Story raid failure stamina refund committed.")),
                    null))),
                OfflineRewardClaimClientResult.Claimed(OfflineClaim())));

            var json = RewardStoryScreenViewStateJsonAdapter.ToJson(state);

            Assert.That(json.Contains("\"surface\":\"RewardReview\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"rewardLineCount\":1"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasCommittedStaminaRefund\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"staminaRefundStatus\":\"COMMITTED\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"staminaRefundStringId\":\"narrative.raid_raider_king_engine.failure_refund.toast\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"usesPortraitSafeArea\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"minimumTouchTargetDp\":48"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRewardCalculation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasEconomyMutation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("Story raid failure stamina refund committed."), Is.EqualTo(false));
            Assert.That(json.Contains("작전 보상"), Is.EqualTo(false));
            Assert.That(json.Contains("크레딧"), Is.EqualTo(false));
            Assert.That(json.Contains("최대 7시간"), Is.EqualTo(false));
            Assert.That(json.Contains("quantity"), Is.EqualTo(false));
            Assert.That(json.Contains("balanceAfter"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }

        [Test]
        public void PresenterAndJsonRejectNullInputs()
        {
            var presenter = new RewardStoryScreenPresenter(new CapturingRewardStoryScreenView());

            Assert.Throws<ArgumentNullException>(() => new RewardStoryScreenPresenter(null));
            Assert.Throws<ArgumentNullException>(() => presenter.RenderRewardReview(null));
            Assert.Throws<ArgumentNullException>(() => presenter.RenderOfflineReward(null));
            Assert.Throws<ArgumentNullException>(() => presenter.RenderStoryProgress(null));
            Assert.Throws<ArgumentNullException>(() => RewardStoryScreenViewStateJsonAdapter.ToJson(null));
        }

        private static RewardStoryDisplayPresentationState BuildPresentation(
            RunEndUxState runEndUxState,
            OfflineRewardClaimClientResult offlineClaimResult)
        {
            return RewardStoryDisplayPresentationState.Create(
                runEndUxState,
                RunRewardPreviewClientResult.Found(RunRewardPreview()),
                null,
                OfflineRewardPreviewFlowResult.ForClientResult(OfflineRewardPreviewClientResult.Available(OfflinePreview())),
                offlineClaimResult == null
                    ? null
                    : OfflineRewardClaimFlowResult.ForClientResult(offlineClaimResult),
                StoryProgress());
        }

        private static RunResultSubmitResponseDto RunFinishResponse(RunStaminaRefundMarker staminaRefund)
        {
            return new RunResultSubmitResponseDto(
                "run_raid_001",
                true,
                "ACCEPTED",
                null,
                "preview_raid_001",
                new DateTime(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc),
                "1",
                null,
                null,
                staminaRefund);
        }

        private static RunRewardPreviewDto RunRewardPreview()
        {
            return new RunRewardPreviewDto(
                "run_raid_001",
                "preview_raid_001",
                RunRewardPreviewStatus.Active,
                new DateTime(2026, 8, 31, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc),
                "1",
                RunRewardClaimability.Claimable,
                new[] { new RunLine("line_credits", "cur_credits", "크레딧", 120, "개", null) });
        }

        private static OfflineRewardPreviewDto OfflinePreview()
        {
            return new OfflineRewardPreviewDto(
                true,
                OfflineRewardPreviewStatus.Available,
                "opn_03_water_stop",
                14400,
                4.0,
                7,
                600,
                24,
                new[] { new OfflineLine("offline_line_credits", "cur_credits", "크레딧", 90, "개", "OFFLINE_REWARD", null) },
                new OfflineRewardPreviewCalculationDto("opn_03_water_stop", "1.52x", 14400, "story_progress_cap"),
                "act1.2026-08-30.001",
                new DateTime(2026, 8, 30, 2, 0, 0, DateTimeKind.Utc),
                null,
                null);
        }

        private static OfflineRewardClaimResponseDto OfflineClaim()
        {
            return new OfflineRewardClaimResponseDto(
                true,
                OfflineRewardClaimStatus.Claimed,
                "offline_claim_001",
                "reward_tx_001",
                "economy_tx_001",
                "mutation_001",
                "opn_03_water_stop",
                14400,
                4.0,
                7,
                600,
                24,
                new[] { new OfflineLine("offline_claim_credits", "cur_credits", "크레딧", 90, "개", "OFFLINE_REWARD", 3300) },
                new OfflineRewardPreviewCalculationDto("opn_03_water_stop", "1.52x", 14400, "story_progress_cap"),
                new DateTime(2026, 8, 30, 2, 5, 0, DateTimeKind.Utc),
                "act1.2026-08-30.001",
                null,
                null);
        }

        private static StoryProgressReadState StoryProgress()
        {
            return StoryProgressReadState.FromResponse(new StoryProgressReadResponseDto(
                "act_01_wasteland_railway",
                "opn_03_water_stop",
                "opn_03_water_stop",
                3,
                new[] { "opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop" },
                new[] { "opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop", "opn_04_scrapstar_show" },
                "act1.2026-08-30.001",
                "READY",
                "LOCALIZED",
                "LOCAL_FIXTURE_READY",
                "NONE"));
        }

        private sealed class CapturingRewardStoryScreenView : IRewardStoryScreenView
        {
            public RewardStoryScreenViewState LastState { get; private set; }

            public void Render(RewardStoryScreenViewState state)
            {
                LastState = state;
            }
        }
    }
}

