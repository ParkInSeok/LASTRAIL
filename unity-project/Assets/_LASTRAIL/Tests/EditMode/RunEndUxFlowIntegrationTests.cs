using System;
using System.Threading.Tasks;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RunEndUxFlowIntegrationTests
    {
        [Test]
        public async Task SubmitResultEndUxStateAsync_MapsAcceptedFlowToRewardReviewReady()
        {
            var state = await SubmitFlow(RunResultSubmitClientResult.Accepted(
                Response("ACCEPTED", true, "claim_001", "preview_001", null)));

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.RewardReviewReady));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.Accepted));
            Assert.That(state.RewardReviewEntry.RunId, Is.EqualTo("run_001"));
            Assert.That(state.RewardReviewEntry.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(state.RewardReviewEntry.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(state.RewardReviewEntry.HasRewardAmounts, Is.EqualTo(false));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public async Task SubmitResultEndUxStateAsync_MapsDuplicateReplayFlowToRecognizedDuplicate()
        {
            var state = await SubmitFlow(RunResultSubmitClientResult.DuplicateReplay(
                Response("DUPLICATE_REPLAY", false, null, null, "duplicate_replay")));

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.RecognizedDuplicate));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.DuplicateReplay));
            Assert.That(state.RewardReviewEntry, Is.EqualTo(null));
            Assert.That(state.ErrorCode, Is.EqualTo("duplicate_replay"));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public async Task SubmitResultEndUxStateAsync_MapsRejectedFlowToRecoverableOrFatalUxState()
        {
            var recoverable = await SubmitFlow(RunResultSubmitClientResult.Rejected(
                Response("REJECTED", false, null, null, "run_state_conflict")));
            var fatal = await SubmitFlow(RunResultSubmitClientResult.Rejected(
                Response("REJECTED", false, null, null, "contract_violation")));

            Assert.That(recoverable.Kind, Is.EqualTo(RunEndUxStateKind.RecoverableRejected));
            Assert.That(recoverable.ErrorCode, Is.EqualTo("run_state_conflict"));
            Assert.That(recoverable.RewardReviewEntry, Is.EqualTo(null));
            Assert.That(recoverable.HasRewardAmounts, Is.EqualTo(false));
            Assert.That(fatal.Kind, Is.EqualTo(RunEndUxStateKind.FatalRejected));
            Assert.That(fatal.ErrorCode, Is.EqualTo("contract_violation"));
            Assert.That(fatal.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
        }

        [Test]
        public async Task SubmitResultEndUxStateAsync_MapsNetworkErrorFlowToRetryableUxState()
        {
            var state = await SubmitFlow(RunResultSubmitClientResult.NetworkError("timeout"));

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.RetryableNetworkError));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.NetworkError));
            Assert.That(state.RewardReviewEntry, Is.EqualTo(null));
            Assert.That(state.ErrorMessage, Is.EqualTo("timeout"));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        private static async Task<RunEndUxState> SubmitFlow(RunResultSubmitClientResult clientResult)
        {
            return await RunApiFlowFacade.SubmitResultEndUxStateAsync(
                CompletedRuntimeFromStartJson(),
                "submit_run_001_attempt_001",
                new CapturingSubmitClient(clientResult));
        }

        private static RunRuntimeState CompletedRuntimeFromStartJson()
        {
            var runtime = RunApiFlowFacade.StartActOne(ResponseJson(), StartOptions());

            for (var i = 0; i < 15; i++)
            {
                runtime = runtime.CompleteCurrentWave();
            }

            return runtime.CompleteFinalOperation();
        }

        private static RunApiFlowStartOptions StartOptions()
        {
            return new RunApiFlowStartOptions(
                selectedOperatorIds: null,
                selectedUltimateOperatorId: null,
                selectedTrainCarSlots: null,
                completedOperationIds: new[] { "opn_03_water_stop" },
                engineOutput: 82);
        }

        private static string ResponseJson()
        {
            return "{"
                + "\"runId\":\"run_001\","
                + "\"balanceVersion\":\"balance_v1\","
                + "\"operationId\":\"opn_03_water_stop\","
                + "\"actId\":\"act_01_wasteland_railway\","
                + "\"regionId\":\"region_wasteland_railway\","
                + "\"enemyFactionId\":\"faction_wasteland_raiders\","
                + "\"riskLevel\":2,"
                + "\"wavePlanId\":\"waveplan_act1_opn03_seed1234\","
                + "\"seed\":1234,"
                + "\"rewardTableId\":\"reward_act1_opn03_risk2\","
                + "\"serverStartedAt\":\"2026-08-26T12:30:00.123Z\","
                + "\"firstClearEligible\":true"
                + "}";
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

        private sealed class CapturingSubmitClient : IRunResultSubmitClient
        {
            private readonly RunResultSubmitClientResult result;

            public CapturingSubmitClient(RunResultSubmitClientResult result)
            {
                this.result = result;
            }

            public Task<RunResultSubmitClientResult> SubmitAsync(RunResultSubmitRequest request)
            {
                Assert.That(request.EndpointPath, Is.EqualTo("/v1/runs/run_001/finish"));
                Assert.That(request.IdempotencyHeaderName, Is.EqualTo("Idempotency-Key"));
                Assert.That(request.IdempotencyHeaderValue, Is.EqualTo("submit_run_001_attempt_001"));
                Assert.That(request.IdempotencyKey, Is.EqualTo("submit_run_001_attempt_001"));
                Assert.That(request.BodyJson.Contains("\"currentStage\":\"Complete\""), Is.EqualTo(true));
                Assert.That(request.BodyJson.Contains("\"outcome\":\"VICTORY\""), Is.EqualTo(true));
                Assert.That(request.BodyJson.Contains("\"runtimeStateJson\":\"{\\\"runId\\\":\\\"run_001\\\""), Is.EqualTo(true));
                Assert.That(request.BodyJson.Contains("rewardAmount"), Is.EqualTo(false));
                return Task.FromResult(result);
            }
        }
    }
}
