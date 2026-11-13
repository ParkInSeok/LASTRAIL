using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Sortie;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRuntimeStateExportTests
    {
        [Test]
        public void ToDebugExport_ReturnsSnapshotProgressionAndTacticSelectionLogWhileInProgress()
        {
            var runtime = RuntimeWithOneSelection();

            var export = RunRuntimeStateExporter.ToDebugExport(runtime);

            Assert.That(export.RunId, Is.EqualTo("run_001"));
            Assert.That(export.BalanceVersion, Is.EqualTo("balance_v1"));
            Assert.That(export.OperationId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(export.ActId, Is.EqualTo("act_01_wasteland_railway"));
            Assert.That(export.RegionId, Is.EqualTo("region_wasteland_railway"));
            Assert.That(export.EnemyFactionId, Is.EqualTo("faction_wasteland_raiders"));
            Assert.That(export.RiskLevel, Is.EqualTo(2));
            Assert.That(export.WavePlanId, Is.EqualTo("waveplan_act1_opn03_seed1234"));
            Assert.That(export.Seed, Is.EqualTo(1234u));
            Assert.That(export.RewardTableId, Is.EqualTo("reward_act1_opn03_risk2"));
            Assert.That(export.ServerStartedAt, Is.EqualTo(StartedAt()));
            Assert.That(export.CurrentStage, Is.EqualTo(SortieStage.Wave));
            Assert.That(export.CurrentWaveNumber, Is.EqualTo(1));
            Assert.That(export.CompletedWaveCount, Is.EqualTo(0));
            Assert.That(export.FinalOperationCompleted, Is.EqualTo(false));
            Assert.That(export.TacticSelections.Count, Is.EqualTo(1));
            Assert.That(export.TacticSelections[0].WaveNumber, Is.EqualTo(3));
            Assert.That(export.TacticSelections[0].TacticId, Is.EqualTo("ult_001"));
            Assert.That(export.TacticSelections[0].TacticKind, Is.EqualTo(TacticKind.Ultimate));
            Assert.That(export.TacticSelections[0].DraftContainedUltimateTactic, Is.EqualTo(true));
        }

        [Test]
        public void ToResultSubmitExport_RequiresCompletedRuntimeState()
        {
            Assert.Throws<InvalidOperationException>(() => RunRuntimeStateExporter.ToResultSubmitExport(RuntimeWithOneSelection()));
        }

        [Test]
        public void ToResultSubmitExport_AllowsCompletedRuntimeState()
        {
            var runtime = RuntimeWithOneSelection();
            for (var i = 0; i < 15; i++)
            {
                runtime = runtime.CompleteCurrentWave();
            }

            runtime = runtime.CompleteFinalOperation();

            var export = RunRuntimeStateExporter.ToResultSubmitExport(runtime);

            Assert.That(export.CurrentStage, Is.EqualTo(SortieStage.Complete));
            Assert.That(export.FinalOperationCompleted, Is.EqualTo(true));
            Assert.That(export.CompletedWaveCount, Is.EqualTo(15));
            Assert.That(export.TacticSelections.Count, Is.EqualTo(1));
        }

        [Test]
        public void ToDebugExport_RejectsNullRuntimeState()
        {
            Assert.Throws<ArgumentNullException>(() => RunRuntimeStateExporter.ToDebugExport(null));
        }

        private static RunRuntimeState RuntimeWithOneSelection()
        {
            return RunRuntimeState.StartActOne(Snapshot())
                .RecordTacticSelection(3, "ult_001", TacticKind.Ultimate, true);
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
                serverStartedAt: StartedAt());
        }

        private static DateTime StartedAt()
        {
            return new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc);
        }
    }
}
