using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class RunApiFlowDiagnosticSnapshotJsonAdapterTests
    {
        [Test]
        public void ToJson_WritesStableQaDiagnosticFieldsWithoutSubmitResult()
        {
            var snapshot = RunApiFlowDiagnosticSnapshot.FromRuntimeState(RuntimeWithSelection());

            var json = RunApiFlowDiagnosticSnapshotJsonAdapter.ToJson(snapshot);

            Assert.That(json, Is.EqualTo(
                "{\"runId\":\"run_001\"," +
                "\"operationId\":\"opn_03_water_stop\"," +
                "\"actId\":\"act_01_wasteland_railway\"," +
                "\"wavePlanId\":\"waveplan_act1_opn03_seed1234\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"riskLevel\":2," +
                "\"startAugmentPackageRef\":\"augment_pkg_act1_opn03_v1\"," +
                "\"startTacticPackageRef\":\"tactic_pkg_act1_opn03_v1\"," +
                "\"seed\":1234," +
                "\"selectedOperatorIds\":[\"op_kyle\",\"op_reina\",\"op_eve\",\"op_sera\",\"op_mira\"]," +
                "\"selectedUltimateOperatorId\":\"op_kyle\"," +
                "\"trainCarSlots\":[{\"slotIndex\":0,\"trainCarId\":\"car_combat\"},{\"slotIndex\":1,\"trainCarId\":\"car_gun\"},{\"slotIndex\":2,\"trainCarId\":\"car_cargo\"}]," +
                "\"tacticSelectionCount\":1," +
                "\"tacticSelections\":[{\"waveNumber\":3,\"tacticId\":\"cmd_rapid_fire\",\"tacticKind\":\"Command\",\"draftContainedUltimateTactic\":false}]," +
                "\"hasSubmitResult\":false," +
                "\"submitOutcome\":null," +
                "\"submitAction\":null," +
                "\"hasIdempotencyKey\":false," +
                "\"rewardClaimId\":null," +
                "\"rewardPreviewId\":null," +
                "\"submitErrorCode\":null," +
                "\"submitErrorMessage\":null}"));
        }

        [Test]
        public void ToJson_WritesSubmitPolicyReferencesWithoutRewardAmounts()
        {
            var response = new RunResultSubmitResponseDto(
                "run_001",
                true,
                "ACCEPTED",
                "claim_001",
                "preview_001",
                new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc),
                "balance_v1",
                null,
                null);
            var policy = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.Accepted,
                response,
                null));
            var snapshot = RunApiFlowDiagnosticSnapshot.FromSubmitPolicy(
                RuntimeWithSelection(),
                "submit_run_001_attempt_001",
                policy);

            var json = RunApiFlowDiagnosticSnapshotJsonAdapter.ToJson(snapshot);

            Assert.That(json.Contains("\"hasSubmitResult\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"submitOutcome\":\"Accepted\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"submitAction\":\"ShowRewardReview\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasIdempotencyKey\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"riskLevel\":2"), Is.EqualTo(true));
            Assert.That(json.Contains("\"rewardClaimId\":\"claim_001\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"rewardPreviewId\":\"preview_001\""), Is.EqualTo(true));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("creditAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("multiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("riskMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("difficultyMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("outcomeMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("waveCompletionMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardMultiplierLabel"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("internalWeights"), Is.EqualTo(false));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(json.Contains("frameData"), Is.EqualTo(false));
            Assert.That(json.Contains("vfxEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("sfxEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("cameraEvents"), Is.EqualTo(false));
            Assert.That(json.Contains("hitStop"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesNetworkErrorForRetryDiagnostics()
        {
            var policy = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.NetworkError,
                null,
                "timeout"));
            var snapshot = RunApiFlowDiagnosticSnapshot.FromSubmitPolicy(
                RuntimeWithSelection(),
                "submit_run_001_attempt_001",
                policy);

            var json = RunApiFlowDiagnosticSnapshotJsonAdapter.ToJson(snapshot);

            Assert.That(json.Contains("\"submitOutcome\":\"NetworkError\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"submitAction\":\"RetryWithSameIdempotencyKey\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"submitErrorMessage\":\"timeout\""), Is.EqualTo(true));
        }

        [Test]
        public void ToJson_RejectsNullSnapshot()
        {
            Assert.Throws<ArgumentNullException>(() => RunApiFlowDiagnosticSnapshotJsonAdapter.ToJson(null));
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
                serverStartedAt: new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc),
                startAugmentPackageRef: "augment_pkg_act1_opn03_v1",
                startTacticPackageRef: "tactic_pkg_act1_opn03_v1");
        }
    }
}
