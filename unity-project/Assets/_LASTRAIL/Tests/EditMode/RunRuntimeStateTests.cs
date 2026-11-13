using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Sortie;
using LastRail.Tactics;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRuntimeStateTests
    {
        [Test]
        public void StartActOne_HoldsSnapshotProgressionAndEmptyTacticState()
        {
            var snapshot = Snapshot();

            var runtime = RunRuntimeState.StartActOne(snapshot);

            Assert.That(runtime.Snapshot, Is.EqualTo(snapshot));
            Assert.That(runtime.Progression.WavePlanId, Is.EqualTo(snapshot.WavePlanId));
            Assert.That(runtime.Progression.Stage, Is.EqualTo(SortieStage.Wave));
            Assert.That(runtime.Progression.CurrentWaveNumber, Is.EqualTo(1));
            Assert.That(runtime.TacticState.Selections.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompleteCurrentWave_ReturnsUpdatedRuntimeStateWithoutChangingSnapshotOrTactics()
        {
            var runtime = RunRuntimeState.StartActOne(Snapshot());

            var next = runtime.CompleteCurrentWave();

            Assert.That(next.Snapshot, Is.EqualTo(runtime.Snapshot));
            Assert.That(next.Progression.CurrentWaveNumber, Is.EqualTo(2));
            Assert.That(next.Progression.CompletedWaveCount, Is.EqualTo(1));
            Assert.That(next.TacticState.Selections.Count, Is.EqualTo(0));
            Assert.That(runtime.Progression.CurrentWaveNumber, Is.EqualTo(1));
        }

        [Test]
        public void RecordTacticSelection_ReturnsUpdatedRuntimeStateWithoutChangingProgression()
        {
            var runtime = RunRuntimeState.StartActOne(Snapshot());

            var next = runtime.RecordTacticSelection(3, "cmd_001", TacticKind.Command, false);

            Assert.That(next.Snapshot, Is.EqualTo(runtime.Snapshot));
            Assert.That(next.Progression.CurrentWaveNumber, Is.EqualTo(runtime.Progression.CurrentWaveNumber));
            Assert.That(next.TacticState.Selections.Count, Is.EqualTo(1));
            Assert.That(next.TacticState.Selections[0].WaveNumber, Is.EqualTo(3));
        }

        [Test]
        public void RecordConfirmedTacticSelection_UsesDraftSessionRecorder()
        {
            var session = TacticDraftSession.StartFromResolvedPool(
                3,
                new ResolvedTacticPool(new[] { "cmd_001", "cmd_002", "ult_001" }, true))
                .ConfirmSelection("ult_001");

            var runtime = RunRuntimeState.StartActOne(Snapshot()).RecordConfirmedTacticSelection(
                session,
                new[]
                {
                    new ResolvedTacticDefinition("cmd_001", TacticKind.Command),
                    new ResolvedTacticDefinition("cmd_002", TacticKind.Command),
                    new ResolvedTacticDefinition("ult_001", TacticKind.Ultimate)
                });

            Assert.That(runtime.TacticState.Selections[0].TacticId, Is.EqualTo("ult_001"));
            Assert.That(runtime.TacticState.Selections[0].TacticKind, Is.EqualTo(TacticKind.Ultimate));
            Assert.That(runtime.TacticState.Selections[0].DraftContainedUltimateTactic, Is.EqualTo(true));
        }

        [Test]
        public void CompleteFinalOperation_ReturnsCompletedRuntimeState()
        {
            var runtime = RunRuntimeState.StartActOne(Snapshot());
            for (var i = 0; i < 15; i++)
            {
                runtime = runtime.CompleteCurrentWave();
            }

            runtime = runtime.CompleteFinalOperation();

            Assert.That(runtime.Progression.Stage, Is.EqualTo(SortieStage.Complete));
            Assert.That(runtime.Progression.IsComplete, Is.EqualTo(true));
        }

        [Test]
        public void StartActOne_RejectsNullSnapshot()
        {
            Assert.Throws<ArgumentNullException>(() => RunRuntimeState.StartActOne(null));
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
