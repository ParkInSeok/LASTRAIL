using System;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RunEndUxStateJsonAdapterTests
    {
        [Test]
        public void ToJson_WritesRewardReviewReadyStateWithReferenceIdsOnly()
        {
            var state = RunEndUxState.FromSubmitPolicy(Policy(
                RunApiFlowSubmitOutcome.Accepted,
                Response("ACCEPTED", true, "claim_001", "preview_001", null),
                null));

            var json = RunEndUxStateJsonAdapter.ToJson(state);

            Assert.That(json, Is.EqualTo(
                "{\"kind\":\"RewardReviewReady\"," +
                "\"submitOutcome\":\"Accepted\"," +
                "\"hasRewardReviewEntry\":true," +
                "\"rewardReviewEntry\":{\"runId\":\"run_001\",\"balanceVersion\":\"balance_v1\",\"resultStatus\":\"ACCEPTED\",\"submitOutcome\":\"Accepted\",\"rewardClaimId\":\"claim_001\",\"rewardPreviewId\":\"preview_001\",\"hasRewardClaimId\":true,\"hasRewardPreviewId\":true,\"hasRewardAmounts\":false}," +
                "\"recognizedRewardPreviewId\":null," +
                "\"hasRecognizedRewardPreviewId\":false," +
                "\"hasStaminaRefund\":false," +
                "\"staminaRefund\":null," +
                "\"errorCode\":null," +
                "\"errorMessage\":null," +
                "\"canRetryWithSameIdempotencyKey\":false," +
                "\"hasRewardAmounts\":false}"));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("creditAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesRecognizedDuplicateWithoutRewardReviewEntry()
        {
            var state = RunEndUxState.FromSubmitPolicy(Policy(
                RunApiFlowSubmitOutcome.DuplicateReplay,
                Response("DUPLICATE_REPLAY", false, null, "preview_previous_001", "duplicate_replay"),
                null));

            var json = RunEndUxStateJsonAdapter.ToJson(state);

            Assert.That(json.Contains("\"kind\":\"RecognizedDuplicate\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"submitOutcome\":\"DuplicateReplay\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRewardReviewEntry\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"rewardReviewEntry\":null"), Is.EqualTo(true));
            Assert.That(json.Contains("\"recognizedRewardPreviewId\":\"preview_previous_001\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRecognizedRewardPreviewId\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"errorCode\":\"duplicate_replay\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"canRetryWithSameIdempotencyKey\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesRejectedAndNetworkErrorFieldsForDebug()
        {
            var rejected = RunEndUxState.FromSubmitPolicy(Policy(
                RunApiFlowSubmitOutcome.Rejected,
                Response("REJECTED", false, null, null, "run_state_conflict"),
                null));
            var network = RunEndUxState.FromSubmitPolicy(Policy(
                RunApiFlowSubmitOutcome.NetworkError,
                null,
                "timeout"));

            var rejectedJson = RunEndUxStateJsonAdapter.ToJson(rejected);
            var networkJson = RunEndUxStateJsonAdapter.ToJson(network);

            Assert.That(rejectedJson.Contains("\"kind\":\"RecoverableRejected\""), Is.EqualTo(true));
            Assert.That(rejectedJson.Contains("\"errorCode\":\"run_state_conflict\""), Is.EqualTo(true));
            Assert.That(rejectedJson.Contains("\"canRetryWithSameIdempotencyKey\":false"), Is.EqualTo(true));
            Assert.That(networkJson.Contains("\"kind\":\"RetryableNetworkError\""), Is.EqualTo(true));
            Assert.That(networkJson.Contains("\"errorMessage\":\"timeout\""), Is.EqualTo(true));
            Assert.That(networkJson.Contains("\"canRetryWithSameIdempotencyKey\":true"), Is.EqualTo(true));
            Assert.That(networkJson.Contains("\"hasRewardAmounts\":false"), Is.EqualTo(true));
        }

        [Test]
        public void ToJson_WritesStaminaRefundMarkerWithoutRewardOrAnimationPayload()
        {
            var state = RunEndUxState.FromSubmitPolicy(Policy(
                RunApiFlowSubmitOutcome.Accepted,
                new RunResultSubmitResponseDto(
                    "run_raid_001",
                    true,
                    "ACCEPTED",
                    null,
                    "preview_raid_001",
                    new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc),
                    "balance_v1",
                    null,
                    null,
                    new RunStaminaRefundMarker("AVAILABLE", "Story raid failure refund available.")),
                null));

            var json = RunEndUxStateJsonAdapter.ToJson(state);

            Assert.That(state.HasStaminaRefund, Is.EqualTo(true));
            Assert.That(json.Contains("\"hasStaminaRefund\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"staminaRefund\":{\"status\":\"AVAILABLE\",\"message\":\"Story raid failure refund available.\",\"hasRewardCalculation\":false,\"hasEconomyMutation\":false}"), Is.EqualTo(true));
            Assert.That(json.Contains("staminaRefundAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(json.Contains("frameData"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_RejectsNullState()
        {
            Assert.Throws<ArgumentNullException>(() => RunEndUxStateJsonAdapter.ToJson(null));
        }

        private static RunApiFlowSubmitPolicy Policy(
            RunApiFlowSubmitOutcome outcome,
            RunResultSubmitResponseDto response,
            string errorMessage)
        {
            return RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(outcome, response, errorMessage));
        }

        private static RunResultSubmitResponseDto Response(
            string resultStatus,
            bool accepted,
            string rewardClaimId,
            string rewardPreviewId,
            string errorCode)
        {
            return new RunResultSubmitResponseDto(
                "run_001",
                accepted,
                resultStatus,
                rewardClaimId,
                rewardPreviewId,
                new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc),
                "balance_v1",
                null,
                errorCode);
        }
    }
}
