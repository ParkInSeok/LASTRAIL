using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class RunResultSubmitRequestTests
    {
        [Test]
        public void FromRuntimeState_CreatesPostRequestWithEndpointRunIdIdempotencyKeyAndJsonBody()
        {
            var runtime = CompletedRuntime();

            var request = RunResultSubmitRequest.FromRuntimeState(
                runtime,
                "submit_run_001_attempt_001",
                RunResultOutcome.Victory,
                new DateTime(2026, 8, 26, 12, 45, 0, 123, DateTimeKind.Utc));

            Assert.That(request.Method, Is.EqualTo(RunResultSubmitRequest.HttpMethod));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/runs/run_001/finish"));
            Assert.That(request.RunId, Is.EqualTo("run_001"));
            Assert.That(request.IdempotencyHeaderName, Is.EqualTo("Idempotency-Key"));
            Assert.That(request.IdempotencyHeaderValue, Is.EqualTo("submit_run_001_attempt_001"));
            Assert.That(request.IdempotencyKey, Is.EqualTo("submit_run_001_attempt_001"));
            Assert.That(request.BodyJson.Contains("\"runId\":\"run_001\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("\"outcome\":\"VICTORY\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("\"clientCompletedAt\":\"2026-08-26T12:45:00.1230000Z\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("\"runtimeStateJson\":\"{\\\"runId\\\":\\\"run_001\\\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("\"tacticSelections\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("idempotency"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("Idempotency"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("rewardGranted"), Is.EqualTo(false));
        }

        [Test]
        public void FromRuntimeState_UsesResultSubmitJsonAdapterAndRejectsIncompleteRuntimeState()
        {
            var runtime = RunRuntimeState.StartActOne(Snapshot());

            Assert.Throws<InvalidOperationException>(() => RunResultSubmitRequest.FromRuntimeState(runtime, "submit_run_001_attempt_001"));
        }

        [Test]
        public void FromExport_CreatesRequestFromCompletedExport()
        {
            var export = RunRuntimeStateExporter.ToResultSubmitExport(CompletedRuntime());

            var request = RunResultSubmitRequest.FromExport(
                export,
                "submit_run_001_attempt_001",
                RunResultOutcome.Victory,
                new DateTime(2026, 8, 26, 12, 45, 0, DateTimeKind.Utc));

            Assert.That(request.RunId, Is.EqualTo("run_001"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/runs/run_001/finish"));
            Assert.That(request.BodyJson.Contains("\"currentStage\":\"Complete\""), Is.EqualTo(true));
        }

        [Test]
        public void FromExport_CreatesRaidBossFinishRequestWithStableRaidMetadataOnly()
        {
            var raidResult = new RaidBossRunResult(
                "raid_raider_king_engine",
                "boss_raider_king_engine",
                true,
                3,
                184);

            var request = RunResultSubmitRequest.FromExport(
                CompletedRaidBossExport(),
                "submit_raid_001_attempt_001",
                RunResultOutcome.Victory,
                new DateTime(2026, 8, 26, 12, 45, 0, DateTimeKind.Utc),
                RunEncounterType.RaidBoss,
                raidResult);

            Assert.That(request.EndpointPath, Is.EqualTo("/v1/runs/run_raid_001/finish"));
            Assert.That(request.IdempotencyHeaderName, Is.EqualTo("Idempotency-Key"));
            Assert.That(request.BodyJson.Contains("\"encounterType\":\"RAID_BOSS\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("\"completedWaveCount\":0"), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("\"raidResult\":{\"raidId\":\"raid_raider_king_engine\""), Is.EqualTo(true));
            Assert.That(request.BodyJson.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("frameData"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("vfxEvents"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("sfxEvents"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("cameraEvents"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("hitStopEvents"), Is.EqualTo(false));
        }

        [Test]
        public void FromRuntimeState_RejectsNullRuntimeState()
        {
            Assert.Throws<ArgumentNullException>(() => RunResultSubmitRequest.FromRuntimeState(null, "submit_run_001_attempt_001"));
        }

        [Test]
        public void FromExport_RejectsNullExport()
        {
            Assert.Throws<ArgumentNullException>(() => RunResultSubmitRequest.FromExport(null, "submit_run_001_attempt_001"));
        }

        [Test]
        public void FromRuntimeState_RejectsEmptyIdempotencyKey()
        {
            Assert.Throws<ArgumentException>(() => RunResultSubmitRequest.FromRuntimeState(CompletedRuntime(), ""));
        }

        [Test]
        public void FromRuntimeState_RejectsNonUtcClientCompletedAt()
        {
            Assert.Throws<ArgumentException>(() => RunResultSubmitRequest.FromRuntimeState(
                CompletedRuntime(),
                "submit_run_001_attempt_001",
                RunResultOutcome.Victory,
                new DateTime(2026, 8, 26, 21, 45, 0, DateTimeKind.Local)));
        }

        private static RunRuntimeState CompletedRuntime()
        {
            var runtime = RunRuntimeState.StartActOne(Snapshot())
                .RecordTacticSelection(3, "cmd_rapid_fire", TacticKind.Command, false);

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
                serverStartedAt: new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc));
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
                LastRail.Sortie.SortieStage.Complete,
                0,
                0,
                true,
                new TacticSelectionExport[0]);
        }
    }
}
