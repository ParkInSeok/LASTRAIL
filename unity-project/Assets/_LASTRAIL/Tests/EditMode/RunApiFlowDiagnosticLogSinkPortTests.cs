using System;
using System.Threading.Tasks;
using LastRail.Run;
using LastRail.Tactics;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RunApiFlowDiagnosticLogSinkPortTests
    {
        [Test]
        public async Task WriteAsync_PortAcceptsDiagnosticSnapshotAndReturnsWrittenResult()
        {
            var snapshot = RunApiFlowDiagnosticSnapshot.FromRuntimeState(RuntimeWithSelection());
            IRunApiFlowDiagnosticLogSink sink = new CapturingDiagnosticLogSink(RunApiFlowDiagnosticLogSinkResult.Written());

            var result = await sink.WriteAsync(snapshot);

            Assert.That(((CapturingDiagnosticLogSink)sink).LastSnapshot, Is.EqualTo(snapshot));
            Assert.That(result.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Written));
            Assert.That(result.IsBlockingRunSubmit, Is.EqualTo(false));
            Assert.That(result.CanRetryLater, Is.EqualTo(false));
        }

        [Test]
        public async Task WriteJsonAsync_PortAcceptsPreSerializedDiagnosticJson()
        {
            IRunApiFlowDiagnosticLogSink sink = new CapturingDiagnosticLogSink(RunApiFlowDiagnosticLogSinkResult.Buffered("offline queue"));

            var result = await sink.WriteJsonAsync("{\"runId\":\"run_001\"}");

            Assert.That(((CapturingDiagnosticLogSink)sink).LastJson, Is.EqualTo("{\"runId\":\"run_001\"}"));
            Assert.That(result.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Buffered));
            Assert.That(result.Message, Is.EqualTo("offline queue"));
            Assert.That(result.IsBlockingRunSubmit, Is.EqualTo(false));
            Assert.That(result.CanRetryLater, Is.EqualTo(true));
        }

        [Test]
        public void ResultFactories_RepresentFailSoftOutcomes()
        {
            var written = RunApiFlowDiagnosticLogSinkResult.Written();
            var buffered = RunApiFlowDiagnosticLogSinkResult.Buffered("local buffer full");
            var failed = RunApiFlowDiagnosticLogSinkResult.Failed("disk unavailable");

            Assert.That(written.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Written));
            Assert.That(buffered.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Buffered));
            Assert.That(failed.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Failed));
            Assert.That(written.IsBlockingRunSubmit, Is.EqualTo(false));
            Assert.That(buffered.IsBlockingRunSubmit, Is.EqualTo(false));
            Assert.That(failed.IsBlockingRunSubmit, Is.EqualTo(false));
            Assert.That(written.CanRetryLater, Is.EqualTo(false));
            Assert.That(buffered.CanRetryLater, Is.EqualTo(true));
            Assert.That(failed.CanRetryLater, Is.EqualTo(true));
            Assert.That(buffered.Message, Is.EqualTo("local buffer full"));
            Assert.That(failed.Message, Is.EqualTo("disk unavailable"));
        }

        [Test]
        public void ResultFactories_RejectEmptyMessagesForBufferedAndFailedOutcomes()
        {
            Assert.Throws<ArgumentException>(() => RunApiFlowDiagnosticLogSinkResult.Buffered(""));
            Assert.Throws<ArgumentException>(() => RunApiFlowDiagnosticLogSinkResult.Failed(""));
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

        private static RunRuntimeState RuntimeWithSelection()
        {
            return RunRuntimeState.StartActOne(Snapshot())
                .RecordTacticSelection(3, "cmd_rapid_fire", TacticKind.Command, false);
        }

        private static RunSnapshot Snapshot()
        {
            return RunStartSnapshotBuilder.BuildActOne(
                runId: "run_001",
                balanceVersion: "balance_v1",
                contentVersion: "balance_v1",
                selectedOperatorIds: null,
                selectedUltimateOperatorId: null,
                selectedTrainCarSlots: null,
                completedOperationIds: new[] { "opn_03_water_stop" },
                engineOutput: 82,
                riskLevel: 2,
                operationId: "opn_03_water_stop",
                actId: "act_01_wasteland_railway",
                regionId: "region_wasteland_railway",
                enemyFactionId: "faction_wasteland_raiders",
                wavePlanId: "waveplan_act1_opn03_seed1234",
                rewardTableId: "reward_act1_opn03_risk2",
                seed: 1234u,
                serverStartedAt: new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc));
        }
    }
}
