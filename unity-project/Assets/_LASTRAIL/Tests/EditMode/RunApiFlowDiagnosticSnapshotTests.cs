using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class RunApiFlowDiagnosticSnapshotTests
    {
        [Test]
        public void FromRuntimeState_CapturesStartAndRuntimeFieldsForQaWithoutSubmitResult()
        {
            var runtime = RuntimeWithSelection();

            var snapshot = RunApiFlowDiagnosticSnapshot.FromRuntimeState(runtime);

            Assert.That(snapshot.RunId, Is.EqualTo("run_001"));
            Assert.That(snapshot.OperationId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(snapshot.ActId, Is.EqualTo("act_01_wasteland_railway"));
            Assert.That(snapshot.WavePlanId, Is.EqualTo("waveplan_act1_opn03_seed1234"));
            Assert.That(snapshot.BalanceVersion, Is.EqualTo("balance_v1"));
            Assert.That(snapshot.RiskLevel, Is.EqualTo(2));
            Assert.That(snapshot.Seed, Is.EqualTo(1234u));
            Assert.That(snapshot.SelectedOperatorIds.Count, Is.EqualTo(5));
            Assert.That(snapshot.SelectedOperatorIds[0], Is.EqualTo("op_kyle"));
            Assert.That(snapshot.SelectedUltimateOperatorId, Is.EqualTo("op_kyle"));
            Assert.That(snapshot.TrainCarSlots.Count, Is.EqualTo(3));
            Assert.That(snapshot.TrainCarSlots[2].TrainCarId, Is.EqualTo("car_cargo"));
            Assert.That(snapshot.TacticSelectionCount, Is.EqualTo(1));
            Assert.That(snapshot.TacticSelections[0].TacticId, Is.EqualTo("cmd_rapid_fire"));
            Assert.That(snapshot.HasSubmitResult, Is.EqualTo(false));
            Assert.That(snapshot.HasIdempotencyKey, Is.EqualTo(false));
        }

        [Test]
        public void FromSubmitPolicy_CapturesOutcomeActionAndIdempotencyPresence()
        {
            var policy = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.NetworkError,
                null,
                "timeout"));

            var snapshot = RunApiFlowDiagnosticSnapshot.FromSubmitPolicy(
                RuntimeWithSelection(),
                "submit_run_001_attempt_001",
                policy);

            Assert.That(snapshot.HasSubmitResult, Is.EqualTo(true));
            Assert.That(snapshot.RiskLevel, Is.EqualTo(2));
            Assert.That(snapshot.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.NetworkError));
            Assert.That(snapshot.SubmitAction, Is.EqualTo(RunApiFlowSubmitAction.RetryWithSameIdempotencyKey));
            Assert.That(snapshot.HasIdempotencyKey, Is.EqualTo(true));
            Assert.That(snapshot.SubmitErrorCode, Is.EqualTo(null));
            Assert.That(snapshot.SubmitErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public void FromSubmitPolicy_CapturesRewardReferenceIdsWithoutRewardValues()
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

            Assert.That(snapshot.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(snapshot.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(snapshot.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public void FromRuntimeState_CopiesCollections()
        {
            var snapshot = RunApiFlowDiagnosticSnapshot.FromRuntimeState(RuntimeWithSelection());

            var operators = snapshot.SelectedOperatorIds;
            var tactics = snapshot.TacticSelections;

            Assert.That(operators.Count, Is.EqualTo(5));
            Assert.That(tactics.Count, Is.EqualTo(1));
        }

        [Test]
        public void Factories_RejectNullInputs()
        {
            Assert.Throws<ArgumentNullException>(() => RunApiFlowDiagnosticSnapshot.FromRuntimeState(null));
            Assert.Throws<ArgumentNullException>(() => RunApiFlowDiagnosticSnapshot.FromSubmitPolicy(RuntimeWithSelection(), "submit_run_001_attempt_001", null));
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
