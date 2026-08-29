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
    public sealed class RewardStoryDisplayPresentationStateTests
    {
        [Test]
        public void Create_CombinesRunRewardOfflineRewardAndStoryProgressForDisplayOnly()
        {
            var state = RewardStoryDisplayPresentationState.Create(
                RunEndUxState.FromSubmitPolicy(RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(RunApiFlowSubmitOutcome.Accepted, RunFinishResponse(), null))),
                RunRewardPreviewClientResult.Found(RunRewardPreview()),
                RunRewardClaimClientResult.Claimed(RunRewardClaim()),
                OfflineRewardPreviewFlowResult.ForClientResult(OfflineRewardPreviewClientResult.Available(OfflinePreview())),
                OfflineRewardClaimFlowResult.ForClientResult(OfflineRewardClaimClientResult.Claimed(OfflineClaim())),
                StoryProgress());

            Assert.That(state.Surface, Is.EqualTo(MissionShellSurface.RewardReview));
            Assert.That(state.RunReward.Kind, Is.EqualTo(RunEndUxStateKind.RewardReviewReady));
            Assert.That(state.RunReward.PreviewStatus, Is.EqualTo(RunRewardPreviewStatus.Active.ToString()));
            Assert.That(state.RunReward.Claimability, Is.EqualTo(RunRewardClaimability.Claimable.ToString()));
            Assert.That(state.RunReward.ClaimStatus, Is.EqualTo(RunRewardClaimResultStatus.Claimed.ToString()));
            Assert.That(state.RunReward.RewardLines.Count, Is.EqualTo(1));
            Assert.That(state.RunReward.RewardLines[0].DisplayName, Is.EqualTo("크레딧"));
            Assert.That(state.RunReward.RewardLines[0].BalanceAfter, Is.EqualTo(3120));
            Assert.That(state.RunReward.HasBalanceAfterDiagnostic, Is.EqualTo(true));
            Assert.That(state.OfflineReward.PreviewStatus, Is.EqualTo(OfflineRewardPreviewStatus.Available.ToString()));
            Assert.That(state.OfflineReward.ClaimStatus, Is.EqualTo(OfflineRewardClaimStatus.Claimed.ToString()));
            Assert.That(state.OfflineReward.StoryProgressLabel, Is.EqualTo("급수 정차"));
            Assert.That(state.OfflineReward.OfflineStorageCapLabel, Is.EqualTo("최대 7시간"));
            Assert.That(state.OfflineReward.RewardLines[0].Source, Is.EqualTo("OFFLINE_REWARD"));
            Assert.That(state.StoryProgress.HighestStoryProgressLabel, Is.EqualTo("급수 정차"));
            Assert.That(state.StoryProgress.UnlockedOperationCount, Is.EqualTo(4));
            Assert.That(state.StoryProgress.UnlockedOperations[3], Is.EqualTo("opn_04_scrapstar_show"));
            Assert.That(state.HasRewardAmountCalculation, Is.EqualTo(false));
            Assert.That(state.HasMultiplierCalculation, Is.EqualTo(false));
            Assert.That(state.HasUnlockTruthCalculation, Is.EqualTo(false));
            Assert.That(state.HasEconomyMutation, Is.EqualTo(false));
        }

        [Test]
        public void Create_MapsDuplicateRejectedAuthAndNetworkStatesWithoutBlockingDisplay()
        {
            var duplicate = RewardStoryDisplayPresentationState.Create(
                RunEndUxState.FromSubmitPolicy(RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(RunApiFlowSubmitOutcome.DuplicateReplay, RunFinishResponse(
                    accepted: false,
                    status: "DUPLICATE_REPLAY",
                    rewardPreviewId: "preview_001",
                    errorCode: "duplicate_replay"), null))),
                null,
                RunRewardClaimClientResult.DuplicateReplay(RunRewardClaim(RunRewardClaimResultStatus.DuplicateReplay, "duplicate_replay")),
                OfflineRewardPreviewFlowResult.ForClientResult(OfflineRewardPreviewClientResult.Empty(OfflinePreview(OfflineRewardPreviewStatus.Empty, accepted: false))),
                OfflineRewardClaimFlowResult.ForClientResult(OfflineRewardClaimClientResult.Rejected("offline_reward_not_claimable", "no claim")),
                StoryProgress());
            var auth = RewardStoryDisplayPresentationState.Create(
                null,
                RunRewardPreviewClientResult.Rejected("auth_required", "auth"),
                null,
                OfflineRewardPreviewFlowResult.ForClientResult(OfflineRewardPreviewClientResult.Rejected("auth_required", "auth")),
                null,
                StoryProgress());
            var network = RewardStoryDisplayPresentationState.Create(
                null,
                RunRewardPreviewClientResult.NetworkError("timeout"),
                null,
                OfflineRewardPreviewFlowResult.ForClientResult(OfflineRewardPreviewClientResult.NetworkError("timeout")),
                OfflineRewardClaimFlowResult.ForClientResult(OfflineRewardClaimClientResult.NetworkError("claim timeout")),
                StoryProgress());

            Assert.That(duplicate.RunReward.Kind, Is.EqualTo(RunEndUxStateKind.RecognizedDuplicate));
            Assert.That(duplicate.RunReward.RecognizedRewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(duplicate.RunReward.ClaimStatus, Is.EqualTo(RunRewardClaimResultStatus.DuplicateReplay.ToString()));
            Assert.That(duplicate.OfflineReward.PreviewStatus, Is.EqualTo(OfflineRewardPreviewStatus.Empty.ToString()));
            Assert.That(duplicate.OfflineReward.IsNoClaimAvailable, Is.EqualTo(true));
            Assert.That(auth.RunReward.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(auth.OfflineReward.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(network.RunReward.CanRetryNetwork, Is.EqualTo(true));
            Assert.That(network.OfflineReward.CanRetryNetwork, Is.EqualTo(true));
            Assert.That(network.IsBlockingDisplay, Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesStableDisplayFieldsWithoutTruthCalculationTransportOrSensitivePayload()
        {
            var state = RewardStoryDisplayPresentationState.Create(
                RunEndUxState.FromSubmitPolicy(RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(RunApiFlowSubmitOutcome.Accepted, RunFinishResponse(), null))),
                RunRewardPreviewClientResult.Found(RunRewardPreview()),
                RunRewardClaimClientResult.Claimed(RunRewardClaim()),
                OfflineRewardPreviewFlowResult.ForClientResult(OfflineRewardPreviewClientResult.Available(OfflinePreview())),
                OfflineRewardClaimFlowResult.ForClientResult(OfflineRewardClaimClientResult.Claimed(OfflineClaim())),
                StoryProgress());

            var json = RewardStoryDisplayPresentationStateJsonAdapter.ToJson(state);

            Assert.That(json.Contains("\"surface\":\"RewardReview\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"runReward\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"offlineReward\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"storyProgress\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"balanceAfter\":3120"), Is.EqualTo(true));
            Assert.That(json.Contains("\"unlockedOperations\":[\"opn_01_last_departure\",\"opn_02_first_signal\",\"opn_03_water_stop\",\"opn_04_scrapstar_show\"]"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRewardAmountCalculation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasMultiplierCalculation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasUnlockTruthCalculation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasEconomyMutation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardMultiplierBps"), Is.EqualTo(false));
            Assert.That(json.Contains("storyProgressMultiplierBps"), Is.EqualTo(false));
        }

        [Test]
        public void FactoriesRejectNullStoryProgressAndJsonRejectsNullState()
        {
            Assert.Throws<ArgumentNullException>(() => RewardStoryDisplayPresentationState.Create(null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => RewardStoryDisplayPresentationStateJsonAdapter.ToJson(null));
        }

        private static RunResultSubmitResponseDto RunFinishResponse(
            bool accepted = true,
            string status = "ACCEPTED",
            string rewardPreviewId = "preview_001",
            string errorCode = null)
        {
            return new RunResultSubmitResponseDto(
                "run_001",
                accepted,
                status,
                null,
                rewardPreviewId,
                new DateTime(2026, 8, 29, 1, 0, 0, DateTimeKind.Utc),
                "1",
                null,
                errorCode);
        }

        private static RunRewardPreviewDto RunRewardPreview()
        {
            return new RunRewardPreviewDto(
                "run_001",
                "preview_001",
                RunRewardPreviewStatus.Active,
                new DateTime(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 8, 29, 1, 0, 0, DateTimeKind.Utc),
                "1",
                RunRewardClaimability.Claimable,
                new[] { new RunLine("run_line_credits", "cur_credits", "크레딧", 120, "개", 3000) });
        }

        private static RunRewardClaimResponseDto RunRewardClaim(
            RunRewardClaimResultStatus status = RunRewardClaimResultStatus.Claimed,
            string errorCode = null)
        {
            return new RunRewardClaimResponseDto(
                "run_001",
                "preview_001",
                "claim_001",
                status,
                new DateTime(2026, 8, 29, 1, 5, 0, DateTimeKind.Utc),
                "1",
                status == RunRewardClaimResultStatus.Claimed
                    ? new[] { new RunLine("run_claim_credits", "cur_credits", "크레딧", 120, "개", 3120) }
                    : Array.Empty<RunLine>(),
                errorCode,
                null);
        }

        private static OfflineRewardPreviewDto OfflinePreview(
            OfflineRewardPreviewStatus status = OfflineRewardPreviewStatus.Available,
            bool accepted = true)
        {
            return new OfflineRewardPreviewDto(
                accepted,
                status,
                "opn_03_water_stop",
                14400,
                4.0,
                7,
                600,
                24,
                accepted ? new[] { new OfflineLine("offline_line_credits", "cur_credits", "크레딧", 90, "개", "OFFLINE_REWARD", 3210) } : Array.Empty<OfflineLine>(),
                new OfflineRewardPreviewCalculationDto("opn_03_water_stop", "1.52x", 14400, "story_progress_cap"),
                "act1.2026-08-27.001",
                new DateTime(2026, 8, 29, 2, 0, 0, DateTimeKind.Utc),
                accepted ? null : "offline_reward_empty",
                accepted ? null : "empty");
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
                new DateTime(2026, 8, 29, 2, 5, 0, DateTimeKind.Utc),
                "act1.2026-08-27.001",
                null,
                null);
        }

        private static StoryProgressReadState StoryProgress()
        {
            return StoryProgressReadState.FromResponse(new StoryProgressReadResponseDto(
                "act_1",
                "opn_03_water_stop",
                "opn_03_water_stop",
                3,
                new[] { "opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop" },
                new[] { "opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop", "opn_04_scrapstar_show" },
                "act1.2026-08-27.001",
                "READY",
                "READY",
                "READY",
                "NONE"));
        }
    }
}
