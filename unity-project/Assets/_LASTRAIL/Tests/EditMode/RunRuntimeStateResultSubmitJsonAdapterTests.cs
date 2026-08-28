using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Sortie;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRuntimeStateResultSubmitJsonAdapterTests
    {
        [Test]
        public void ToJson_WritesCompletedRunResultSubmitFieldsWithTacticSelections()
        {
            var runtime = CompletedRuntimeWithSelections();

            var json = RunRuntimeStateResultSubmitJsonAdapter.ToJson(
                runtime,
                RunResultOutcome.Victory,
                new DateTime(2026, 8, 26, 12, 45, 0, 123, DateTimeKind.Utc));

            Assert.That(json, Is.EqualTo(
                "{\"runId\":\"run_001\"," +
                "\"encounterType\":\"ORDINARY_15_WAVE\"," +
                "\"currentStage\":\"Complete\"," +
                "\"currentWaveNumber\":0," +
                "\"completedWaveCount\":15," +
                "\"outcome\":\"VICTORY\"," +
                "\"raidResult\":null," +
                "\"startAugmentPackageRef\":\"augment_pkg_act1_opn03_v1\"," +
                "\"startTacticPackageRef\":\"tactic_pkg_act1_opn03_v1\"," +
                "\"runtimeStateJson\":\"{\\\"runId\\\":\\\"run_001\\\",\\\"balanceVersion\\\":\\\"balance_v1\\\",\\\"operationId\\\":\\\"opn_03_water_stop\\\",\\\"actId\\\":\\\"act_01_wasteland_railway\\\",\\\"regionId\\\":\\\"region_wasteland_railway\\\",\\\"enemyFactionId\\\":\\\"faction_wasteland_raiders\\\",\\\"riskLevel\\\":2,\\\"wavePlanId\\\":\\\"waveplan_act1_opn03_seed1234\\\",\\\"seed\\\":1234,\\\"rewardTableId\\\":\\\"reward_act1_opn03_risk2\\\",\\\"startAugmentPackageRef\\\":\\\"augment_pkg_act1_opn03_v1\\\",\\\"startTacticPackageRef\\\":\\\"tactic_pkg_act1_opn03_v1\\\",\\\"serverStartedAt\\\":\\\"2026-08-26T12:30:00.0000000Z\\\",\\\"currentStage\\\":\\\"Complete\\\",\\\"currentWaveNumber\\\":0,\\\"completedWaveCount\\\":15,\\\"finalOperationCompleted\\\":true,\\\"tacticSelections\\\":[{\\\"waveNumber\\\":3,\\\"tacticId\\\":\\\"cmd_rapid_fire\\\",\\\"tacticKind\\\":\\\"Command\\\",\\\"draftContainedUltimateTactic\\\":false,\\\"packageRef\\\":\\\"tactic_pkg_act1_opn03_v1\\\"},{\\\"waveNumber\\\":6,\\\"tacticId\\\":\\\"ult_last_stand\\\",\\\"tacticKind\\\":\\\"Ultimate\\\",\\\"draftContainedUltimateTactic\\\":true,\\\"packageRef\\\":\\\"tactic_pkg_act1_opn03_v1\\\"}]}\"," +
                "\"tacticSelections\":[" +
                "{\"waveNumber\":3,\"tacticId\":\"cmd_rapid_fire\",\"tacticKind\":\"Command\",\"draftContainedUltimateTactic\":false,\"packageRef\":\"tactic_pkg_act1_opn03_v1\"}," +
                "{\"waveNumber\":6,\"tacticId\":\"ult_last_stand\",\"tacticKind\":\"Ultimate\",\"draftContainedUltimateTactic\":true,\"packageRef\":\"tactic_pkg_act1_opn03_v1\"}" +
                "]," +
                "\"clientCompletedAt\":\"2026-08-26T12:45:00.1230000Z\"}"));
        }

        [Test]
        public void ToJson_UsesResultSubmitExportAndRejectsIncompleteRuntimeState()
        {
            var runtime = RunRuntimeState.StartActOne(Snapshot());

            Assert.Throws<InvalidOperationException>(() => RunRuntimeStateResultSubmitJsonAdapter.ToJson(runtime));
        }

        [Test]
        public void ToJson_DoesNotIncludeRewardCalculationFields()
        {
            var json = RunRuntimeStateResultSubmitJsonAdapter.ToJson(CompletedRuntimeWithSelections());

            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardGranted"), Is.EqualTo(false));
            Assert.That(json.Contains("currencyRewards"), Is.EqualTo(false));
            Assert.That(json.Contains("currencyGrant"), Is.EqualTo(false));
            Assert.That(json.Contains("finalReward"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesOrdinaryEncounterTypeForCompletedFifteenWaveRun()
        {
            var json = RunRuntimeStateResultSubmitJsonAdapter.ToJson(
                CompletedRuntimeWithSelections(),
                RunResultOutcome.Victory,
                new DateTime(2026, 8, 26, 12, 45, 0, DateTimeKind.Utc));

            Assert.That(json.Contains("\"encounterType\":\"ORDINARY_15_WAVE\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"completedWaveCount\":15"), Is.EqualTo(true));
            Assert.That(json.Contains("\"startAugmentPackageRef\":\"augment_pkg_act1_opn03_v1\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"startTacticPackageRef\":\"tactic_pkg_act1_opn03_v1\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"packageRef\":\"tactic_pkg_act1_opn03_v1\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"raidResult\":null"), Is.EqualTo(true));
        }

        [Test]
        public void ToJson_WritesRaidBossEncounterResultWithoutAnimationPayload()
        {
            var raidResult = new RaidBossRunResult(
                "raid_raider_king_engine",
                "boss_raider_king_engine",
                true,
                3,
                184);
            var json = RunRuntimeStateResultSubmitJsonAdapter.ToJson(
                CompletedRaidBossExport(),
                RunResultOutcome.Victory,
                new DateTime(2026, 8, 26, 12, 45, 0, DateTimeKind.Utc),
                RunEncounterType.RaidBoss,
                raidResult);

            Assert.That(json.Contains("\"encounterType\":\"RAID_BOSS\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"completedWaveCount\":0"), Is.EqualTo(true));
            Assert.That(json.Contains("\"raidResult\":{\"raidId\":\"raid_raider_king_engine\",\"bossId\":\"boss_raider_king_engine\",\"bossDefeated\":true,\"completedPhaseCount\":3,\"encounterDurationSeconds\":184}"), Is.EqualTo(true));
            Assert.That(json.Contains("\"startAugmentPackageRef\":\"augment_pkg_act1_raid_v1\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"startTacticPackageRef\":\"tactic_pkg_act1_raid_v1\""), Is.EqualTo(true));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(json.Contains("frameData"), Is.EqualTo(false));
            Assert.That(json.Contains("vfxEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("sfxEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("cameraEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("hitStopEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_RejectsNonUtcClientCompletedAt()
        {
            Assert.Throws<ArgumentException>(() => RunRuntimeStateResultSubmitJsonAdapter.ToJson(
                CompletedRuntimeWithSelections(),
                RunResultOutcome.Victory,
                new DateTime(2026, 8, 26, 21, 45, 0, DateTimeKind.Local)));
        }

        [Test]
        public void ToJson_RejectsNullRuntimeState()
        {
            Assert.Throws<ArgumentNullException>(() => RunRuntimeStateResultSubmitJsonAdapter.ToJson((RunRuntimeState)null));
        }

        [Test]
        public void ToJson_RejectsNullExport()
        {
            Assert.Throws<ArgumentNullException>(() => RunRuntimeStateResultSubmitJsonAdapter.ToJson((RunRuntimeStateExport)null));
        }

        private static RunRuntimeState CompletedRuntimeWithSelections()
        {
            var runtime = RunRuntimeState.StartActOne(Snapshot())
                .RecordTacticSelection(3, "cmd_rapid_fire", TacticKind.Command, false)
                .RecordTacticSelection(6, "ult_last_stand", TacticKind.Ultimate, true);

            for (var i = 0; i < 15; i++)
            {
                runtime = runtime.CompleteCurrentWave();
            }

            return runtime.CompleteFinalOperation();
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
                serverStartedAt: new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc),
                startAugmentPackageRef: "augment_pkg_act1_opn03_v1",
                startTacticPackageRef: "tactic_pkg_act1_opn03_v1");
        }

        private static RunRuntimeStateExport CompletedRaidBossExport()
        {
            return new RunRuntimeStateExport(
                "run_raid_001",
                "balance_v1",
                "raid_raider_king_engine",
                "act_01_wasteland_railway",
                "region_wasteland_railway",
                "faction_wasteland_raiders",
                5,
                "raidplan_raider_king_engine_seed999",
                999u,
                "reward_act1_raid_risk5",
                new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc),
                SortieStage.Complete,
                0,
                0,
                true,
                new TacticSelectionExport[0],
                "augment_pkg_act1_raid_v1",
                "tactic_pkg_act1_raid_v1");
        }
    }
}
