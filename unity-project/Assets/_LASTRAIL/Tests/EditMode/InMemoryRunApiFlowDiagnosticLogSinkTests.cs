using System;
using System.Threading.Tasks;
using LastRail.Run;
using LastRail.Tactics;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class InMemoryRunApiFlowDiagnosticLogSinkTests
    {
        [Test]
        public async Task WriteAsync_StoresRecentSnapshotsAndReturnsWrittenWithinCapacity()
        {
            var sink = new InMemoryRunApiFlowDiagnosticLogSink(maxEntries: 2);
            var first = Snapshot("run_001");
            var second = Snapshot("run_002");

            var firstResult = await sink.WriteAsync(first);
            var secondResult = await sink.WriteAsync(second);

            Assert.That(firstResult.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Written));
            Assert.That(secondResult.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Written));
            Assert.That(sink.Snapshots.Count, Is.EqualTo(2));
            Assert.That(sink.Snapshots[0].RunId, Is.EqualTo("run_001"));
            Assert.That(sink.Snapshots[1].RunId, Is.EqualTo("run_002"));
            Assert.That(firstResult.IsBlockingRunSubmit, Is.EqualTo(false));
        }

        [Test]
        public async Task WriteAsync_DropsOldestSnapshotAndReturnsBufferedWhenCapacityIsExceeded()
        {
            var sink = new InMemoryRunApiFlowDiagnosticLogSink(maxEntries: 2);

            await sink.WriteAsync(Snapshot("run_001"));
            await sink.WriteAsync(Snapshot("run_002"));
            var result = await sink.WriteAsync(Snapshot("run_003"));

            Assert.That(result.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Buffered));
            Assert.That(result.CanRetryLater, Is.EqualTo(true));
            Assert.That(result.IsBlockingRunSubmit, Is.EqualTo(false));
            Assert.That(sink.Snapshots.Count, Is.EqualTo(2));
            Assert.That(sink.Snapshots[0].RunId, Is.EqualTo("run_002"));
            Assert.That(sink.Snapshots[1].RunId, Is.EqualTo("run_003"));
        }

        [Test]
        public async Task WriteJsonAsync_StoresRecentJsonEntriesAndReturnsBufferedWhenRotated()
        {
            var sink = new InMemoryRunApiFlowDiagnosticLogSink(maxEntries: 2);

            await sink.WriteJsonAsync("{\"runId\":\"run_001\"}");
            await sink.WriteJsonAsync("{\"runId\":\"run_002\"}");
            var result = await sink.WriteJsonAsync("{\"runId\":\"run_003\"}");

            Assert.That(result.Outcome, Is.EqualTo(RunApiFlowDiagnosticLogSinkOutcome.Buffered));
            Assert.That(sink.JsonEntries.Count, Is.EqualTo(2));
            Assert.That(sink.JsonEntries[0], Is.EqualTo("{\"runId\":\"run_002\"}"));
            Assert.That(sink.JsonEntries[1], Is.EqualTo("{\"runId\":\"run_003\"}"));
            Assert.That(result.IsBlockingRunSubmit, Is.EqualTo(false));
        }

        [Test]
        public void ConstructorAndWrites_RejectInvalidInputs()
        {
            var sink = new InMemoryRunApiFlowDiagnosticLogSink(maxEntries: 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryRunApiFlowDiagnosticLogSink(0));
            Assert.Throws<ArgumentNullException>(() => sink.WriteAsync(null).GetAwaiter().GetResult());
            Assert.Throws<ArgumentException>(() => sink.WriteJsonAsync("").GetAwaiter().GetResult());
        }

        private static RunApiFlowDiagnosticSnapshot Snapshot(string runId)
        {
            return RunApiFlowDiagnosticSnapshot.FromRuntimeState(
                RunRuntimeState.StartActOne(RunSnapshotFor(runId))
                    .RecordTacticSelection(3, "cmd_rapid_fire", TacticKind.Command, false));
        }

        private static RunSnapshot RunSnapshotFor(string runId)
        {
            return RunStartSnapshotBuilder.BuildActOne(
                runId: runId,
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
