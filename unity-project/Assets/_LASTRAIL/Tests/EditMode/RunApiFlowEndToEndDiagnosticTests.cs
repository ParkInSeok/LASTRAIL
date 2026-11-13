using System;
using System.Threading.Tasks;
using LastRail.Run;
using LastRail.Tactics;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RunApiFlowEndToEndDiagnosticTests
    {
        [Test]
        public async Task RunStartSubmitAndDiagnosticFlow_UsesPureClientBoundariesWithoutRewardCalculation()
        {
            var runtime = RunApiFlowFacade.StartActOne(ResponseJson(), StartOptions())
                .RecordTacticSelection(3, "cmd_rapid_fire", TacticKind.Command, false);

            for (var i = 0; i < 15; i++)
            {
                runtime = runtime.CompleteCurrentWave();
            }

            runtime = runtime.CompleteFinalOperation();

            var request = RunApiFlowFacade.CreateResultSubmitRequest(runtime, "submit_run_001_attempt_001");
            var client = new CapturingSubmitClient(RunResultSubmitClientResult.Accepted(Response("ACCEPTED", true)));
            var submitPolicy = await RunApiFlowFacade.SubmitResultPolicyAsync(runtime, "submit_run_001_attempt_001", client);
            var sink = new InMemoryRunApiFlowDiagnosticLogSink(maxEntries: 4);
            var diagnosticWrite = await RunApiFlowFacade.WriteDiagnosticSnapshotAsync(
                runtime,
                "submit_run_001_attempt_001",
                submitPolicy,
                sink);

            Assert.That(runtime.Snapshot.RunId, Is.EqualTo("run_001"));
            Assert.That(runtime.Snapshot.WavePlanId, Is.EqualTo("waveplan_act1_opn03_seed1234"));
            Assert.That(runtime.TacticState.Selections.Count, Is.EqualTo(1));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/runs/run_001/finish"));
            Assert.That(request.IdempotencyHeaderName, Is.EqualTo("Idempotency-Key"));
            Assert.That(request.IdempotencyHeaderValue, Is.EqualTo("submit_run_001_attempt_001"));
            Assert.That(request.IdempotencyKey, Is.EqualTo("submit_run_001_attempt_001"));
            Assert.That(request.BodyJson.Contains("\"currentStage\":\"Complete\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("\"outcome\":\"VICTORY\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("\"runtimeStateJson\":\"{\\\"runId\\\":\\\"run_001\\\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(client.LastRequest.EndpointPath, Is.EqualTo(request.EndpointPath));
            Assert.That(client.LastRequest.IdempotencyHeaderName, Is.EqualTo(request.IdempotencyHeaderName));
            Assert.That(client.LastRequest.IdempotencyHeaderValue, Is.EqualTo(request.IdempotencyHeaderValue));
            Assert.That(client.LastRequest.BodyJson.Contains("\"currentStage\":\"Complete\""), Is.EqualTo(true));
            Assert.That(client.LastRequest.BodyJson.Contains("\"outcome\":\"VICTORY\""), Is.EqualTo(true));
            Assert.That(client.LastRequest.BodyJson.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(submitPolicy.Action, Is.EqualTo(RunApiFlowSubmitAction.ShowRewardReview));
            Assert.That(submitPolicy.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(diagnosticWrite.WasSinkProvided, Is.EqualTo(true));
            Assert.That(diagnosticWrite.WasWriteAttempted, Is.EqualTo(true));
            Assert.That(diagnosticWrite.SinkResult.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Written));
            Assert.That(diagnosticWrite.IsBlockingRunSubmit, Is.EqualTo(false));
            Assert.That(diagnosticWrite.Snapshot.HasRewardAmounts, Is.EqualTo(false));
            Assert.That(diagnosticWrite.Snapshot.TacticSelectionCount, Is.EqualTo(1));
            Assert.That(sink.Snapshots.Count, Is.EqualTo(1));
            Assert.That(sink.Snapshots[0], Is.EqualTo(diagnosticWrite.Snapshot));
            Assert.That(sink.JsonEntries.Count, Is.EqualTo(0));
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
    }
}
