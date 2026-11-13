using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class RunApiFlowFacadeTests
    {
        [Test]
        public async Task FrozenFinishRequestIsReusedWithoutRecapturingCompletionTime()
        {
            var method = typeof(RunApiFlowFacade).GetMethod("SubmitResultAsync", new[] { typeof(RunResultSubmitRequest), typeof(IRunResultSubmitClient) });
            Assert.That(method, Is.Not.Null, "A frozen finish request must have a facade entry point.");
            var request = RunApiFlowFacade.CreateResultSubmitRequest(CompletedRuntime(), "submit_frozen_run_001",
                RunResultOutcome.Victory, new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc));
            var client = new CapturingSubmitClient(RunResultSubmitClientResult.NetworkError("timeout"));
            for (var i = 0; i < 2; i++)
            {
                await (Task<RunApiFlowSubmitResult>)method.Invoke(null, new object[] { request, client });
                Assert.That(client.LastRequest, Is.SameAs(request));
            }
        }
        [Test]
        public void StartActOneFromResponse_CreatesRuntimeStateFromServerDtoAndStartOptions()
        {
            var runtime = RunApiFlowFacade.StartActOne(ResponseDto(), StartOptions());

            Assert.That(runtime.Snapshot.RunId, Is.EqualTo("run_001"));
            Assert.That(runtime.Snapshot.BalanceVersion, Is.EqualTo("balance_v1"));
            Assert.That(runtime.Snapshot.WavePlanId, Is.EqualTo("waveplan_act1_opn03_seed1234"));
            Assert.That(runtime.Progression.CurrentWaveNumber, Is.EqualTo(1));
            Assert.That(runtime.TacticState.Selections.Count, Is.EqualTo(0));
        }

        [Test]
        public void StartActOneFromJson_UsesResponseJsonAdapterAndCreatesRuntimeState()
        {
            var runtime = RunApiFlowFacade.StartActOne(ResponseJson(), StartOptions());

            Assert.That(runtime.Snapshot.RunId, Is.EqualTo("run_001"));
            Assert.That(runtime.Snapshot.ServerStartedAt, Is.EqualTo(new DateTime(2026, 8, 26, 12, 30, 0, 123, DateTimeKind.Utc)));
        }

        [Test]
        public void CreateResultSubmitRequest_UsesCompletedRuntimeStateAndIdempotencyKey()
        {
            var request = RunApiFlowFacade.CreateResultSubmitRequest(CompletedRuntime(), "submit_run_001_attempt_001");

            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.RunId, Is.EqualTo("run_001"));
            Assert.That(request.IdempotencyKey, Is.EqualTo("submit_run_001_attempt_001"));
            Assert.That(request.BodyJson.Contains("\"currentStage\":\"Complete\""), Is.EqualTo(true));
        }

        [Test]
        public void CreateResultSubmitRequest_AllowsExplicitOutcomeAndClientCompletedAt()
        {
            var request = RunApiFlowFacade.CreateResultSubmitRequest(
                CompletedRuntime(),
                "submit_run_001_attempt_001",
                RunResultOutcome.Failure,
                new DateTime(2026, 8, 26, 13, 5, 0, 456, DateTimeKind.Utc));

            Assert.That(request.EndpointPath, Is.EqualTo("/v1/runs/run_001/finish"));
            Assert.That(request.BodyJson.Contains("\"outcome\":\"FAILURE\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("\"clientCompletedAt\":\"2026-08-26T13:05:00.4560000Z\""), Is.EqualTo(true));
        }

        [Test]
        public void CreateResultSubmitRequest_RejectsIncompleteRuntimeState()
        {
            var runtime = RunApiFlowFacade.StartActOne(ResponseDto(), StartOptions());

            Assert.Throws<InvalidOperationException>(() => RunApiFlowFacade.CreateResultSubmitRequest(runtime, "submit_run_001_attempt_001"));
        }

        [Test]
        public async Task SubmitResultAsync_PassesRequestToClientAndMapsAcceptedOutcome()
        {
            var client = new CapturingSubmitClient(RunResultSubmitClientResult.Accepted(Response("ACCEPTED", true)));

            var result = await RunApiFlowFacade.SubmitResultAsync(CompletedRuntime(), "submit_run_001_attempt_001", client);

            Assert.That(client.LastRequest.IdempotencyKey, Is.EqualTo("submit_run_001_attempt_001"));
            Assert.That(result.Outcome, Is.EqualTo(RunApiFlowSubmitOutcome.Accepted));
            Assert.That(result.Response.ResultStatus, Is.EqualTo("ACCEPTED"));
            Assert.That(result.ErrorMessage, Is.EqualTo(null));
        }

        [Test]
        public async Task SubmitResultAsync_MapsRejectedDuplicateAndNetworkErrorOutcomes()
        {
            var rejected = await RunApiFlowFacade.SubmitResultAsync(
                CompletedRuntime(),
                "submit_run_001_attempt_001",
                new CapturingSubmitClient(RunResultSubmitClientResult.Rejected(Response("REJECTED", false))));
            var duplicate = await RunApiFlowFacade.SubmitResultAsync(
                CompletedRuntime(),
                "submit_run_001_attempt_001",
                new CapturingSubmitClient(RunResultSubmitClientResult.DuplicateReplay(Response("DUPLICATE_REPLAY", false))));
            var network = await RunApiFlowFacade.SubmitResultAsync(
                CompletedRuntime(),
                "submit_run_001_attempt_001",
                new CapturingSubmitClient(RunResultSubmitClientResult.NetworkError("timeout")));

            Assert.That(rejected.Outcome, Is.EqualTo(RunApiFlowSubmitOutcome.Rejected));
            Assert.That(duplicate.Outcome, Is.EqualTo(RunApiFlowSubmitOutcome.DuplicateReplay));
            Assert.That(network.Outcome, Is.EqualTo(RunApiFlowSubmitOutcome.NetworkError));
            Assert.That(network.Response, Is.EqualTo(null));
            Assert.That(network.ErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public async Task SubmitResultPolicyAsync_ReturnsPolicyMappedFromClientOutcome()
        {
            var accepted = await RunApiFlowFacade.SubmitResultPolicyAsync(
                CompletedRuntime(),
                "submit_run_001_attempt_001",
                new CapturingSubmitClient(RunResultSubmitClientResult.Accepted(Response("ACCEPTED", true))));
            var network = await RunApiFlowFacade.SubmitResultPolicyAsync(
                CompletedRuntime(),
                "submit_run_001_attempt_001",
                new CapturingSubmitClient(RunResultSubmitClientResult.NetworkError("timeout")));

            Assert.That(accepted.Action, Is.EqualTo(RunApiFlowSubmitAction.ShowRewardReview));
            Assert.That(accepted.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(network.Action, Is.EqualTo(RunApiFlowSubmitAction.RetryWithSameIdempotencyKey));
            Assert.That(network.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(network.ErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public async Task SubmitResultEndUxStateAsync_ReturnsRewardReviewReadyStateForAcceptedSubmit()
        {
            var state = await RunApiFlowFacade.SubmitResultEndUxStateAsync(
                CompletedRuntime(),
                "submit_run_001_attempt_001",
                new CapturingSubmitClient(RunResultSubmitClientResult.Accepted(Response("ACCEPTED", true))));

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.RewardReviewReady));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.Accepted));
            Assert.That(state.RewardReviewEntry.RunId, Is.EqualTo("run_001"));
            Assert.That(state.RewardReviewEntry.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(state.RewardReviewEntry.HasRewardAmounts, Is.EqualTo(false));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public async Task SubmitResultEndUxStateAsync_ReturnsRetryableNetworkStateForNetworkError()
        {
            var state = await RunApiFlowFacade.SubmitResultEndUxStateAsync(
                CompletedRuntime(),
                "submit_run_001_attempt_001",
                new CapturingSubmitClient(RunResultSubmitClientResult.NetworkError("timeout")));

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.RetryableNetworkError));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.NetworkError));
            Assert.That(state.RewardReviewEntry, Is.EqualTo(null));
            Assert.That(state.ErrorMessage, Is.EqualTo("timeout"));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public void CreateEndUxState_ConvertsSubmitPolicyWithoutCallingNetwork()
        {
            var policy = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.Rejected,
                Response("REJECTED", false),
                null));

            var state = RunApiFlowFacade.CreateEndUxState(policy);

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.FatalRejected));
            Assert.That(state.ErrorCode, Is.EqualTo(null));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public async Task WriteDiagnosticSnapshotAsync_WritesSubmitPolicySnapshotWhenSinkIsProvided()
        {
            var runtime = CompletedRuntime();
            var policy = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.Accepted,
                Response("ACCEPTED", true),
                null));
            var sink = new CapturingDiagnosticLogSink(RunApiFlowDiagnosticLogSinkResult.Buffered("queued"));

            var result = await RunApiFlowFacade.WriteDiagnosticSnapshotAsync(
                runtime,
                "submit_run_001_attempt_001",
                policy,
                sink);

            Assert.That(result.Snapshot.RunId, Is.EqualTo("run_001"));
            Assert.That(result.Snapshot.HasSubmitResult, Is.EqualTo(true));
            Assert.That(result.Snapshot.SubmitAction, Is.EqualTo(RunApiFlowSubmitAction.ShowRewardReview));
            Assert.That(result.Snapshot.HasIdempotencyKey, Is.EqualTo(true));
            Assert.That(result.WasSinkProvided, Is.EqualTo(true));
            Assert.That(result.WasWriteAttempted, Is.EqualTo(true));
            Assert.That(result.SinkResult.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Buffered));
            Assert.That(result.SinkResult.IsBlockingRunSubmit, Is.EqualTo(false));
            Assert.That(sink.LastSnapshot, Is.EqualTo(result.Snapshot));
        }

        [Test]
        public async Task WriteDiagnosticSnapshotAsync_ReturnsNoOpResultWhenSinkIsMissing()
        {
            var runtime = CompletedRuntime();
            var policy = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.NetworkError,
                null,
                "timeout"));

            var result = await RunApiFlowFacade.WriteDiagnosticSnapshotAsync(
                runtime,
                "submit_run_001_attempt_001",
                policy,
                null);

            Assert.That(result.Snapshot.RunId, Is.EqualTo("run_001"));
            Assert.That(result.Snapshot.SubmitAction, Is.EqualTo(RunApiFlowSubmitAction.RetryWithSameIdempotencyKey));
            Assert.That(result.WasSinkProvided, Is.EqualTo(false));
            Assert.That(result.WasWriteAttempted, Is.EqualTo(false));
            Assert.That(result.SinkResult, Is.EqualTo(null));
            Assert.That(result.IsBlockingRunSubmit, Is.EqualTo(false));
        }

        [Test]
        public async Task WriteDiagnosticSnapshotAsync_CanBuildPolicyFromSubmitResultBeforeWriting()
        {
            var runtime = CompletedRuntime();
            var submitResult = new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.Accepted,
                Response("ACCEPTED", true),
                null);
            var sink = new CapturingDiagnosticLogSink(RunApiFlowDiagnosticLogSinkResult.Written());

            var result = await RunApiFlowFacade.WriteDiagnosticSnapshotAsync(
                runtime,
                "submit_run_001_attempt_001",
                submitResult,
                sink);

            Assert.That(result.Snapshot.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(result.SinkResult.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Written));
        }

        [Test]
        public void StartOptions_RejectsNullCompletedOperationIds()
        {
            Assert.Throws<ArgumentNullException>(() => new RunApiFlowStartOptions(
                selectedOperatorIds: null,
                selectedUltimateOperatorId: null,
                selectedTrainCarSlots: null,
                completedOperationIds: null,
                engineOutput: 82));
        }

        private static RunRuntimeState CompletedRuntime()
        {
            var runtime = RunApiFlowFacade.StartActOne(ResponseDto(), StartOptions())
                .RecordTacticSelection(3, "cmd_rapid_fire", TacticKind.Command, false);

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

        private static RunStartResponseDto ResponseDto()
        {
            return new RunStartResponseDto(
                "run_001",
                "balance_v1",
                "opn_03_water_stop",
                "act_01_wasteland_railway",
                "region_wasteland_railway",
                "faction_wasteland_raiders",
                2,
                "waveplan_act1_opn03_seed1234",
                1234u,
                "reward_act1_opn03_risk2",
                new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc),
                true);
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

        private static RunResultSubmitResponseDto Response(string resultStatus, bool accepted)
        {
            return new RunResultSubmitResponseDto(
                "run_001",
                accepted,
                resultStatus,
                accepted ? "claim_001" : null,
                "preview_001",
                new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc),
                "balance_v1",
                null,
                null);
        }

        private sealed class CapturingSubmitClient : IRunResultSubmitClient
        {
            private readonly RunResultSubmitClientResult result;

            public CapturingSubmitClient(RunResultSubmitClientResult result)
            {
                this.result = result;
            }

            public RunResultSubmitRequest LastRequest { get; private set; }

            public Task<RunResultSubmitClientResult> SubmitAsync(RunResultSubmitRequest request)
            {
                LastRequest = request;
                return Task.FromResult(result);
            }
        }

        private sealed class CapturingDiagnosticLogSink : IRunApiFlowDiagnosticLogSink
        {
            private readonly RunApiFlowDiagnosticLogSinkResult result;

            public CapturingDiagnosticLogSink(RunApiFlowDiagnosticLogSinkResult result)
            {
                this.result = result;
            }

            public RunApiFlowDiagnosticSnapshot LastSnapshot { get; private set; }
            public string LastJson { get; private set; }

            public Task<RunApiFlowDiagnosticLogSinkResult> WriteAsync(RunApiFlowDiagnosticSnapshot snapshot)
            {
                LastSnapshot = snapshot;
                return Task.FromResult(result);
            }

            public Task<RunApiFlowDiagnosticLogSinkResult> WriteJsonAsync(string diagnosticJson)
            {
                LastJson = diagnosticJson;
                return Task.FromResult(result);
            }
        }
    }
}
