using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class RunStartResponseAdapterTests
    {
        [Test]
        public void ToRunSnapshot_MapsServerResponseAndPlayerSelectionIntoSnapshot()
        {
            var response = Response(firstClearEligible: true);

            var snapshot = RunStartResponseAdapter.ToRunSnapshot(
                response,
                selectedOperatorIds: new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" },
                selectedUltimateOperatorId: "op_reina",
                selectedTrainCarSlots: new[] { CarriageType.Combat, CarriageType.Gun, CarriageType.Cargo },
                completedOperationIds: new[] { "opn_03_water_stop" },
                engineOutput: 82);

            Assert.That(snapshot.RunId, Is.EqualTo("run_001"));
            Assert.That(snapshot.BalanceVersion, Is.EqualTo("balance_v1"));
            Assert.That(snapshot.OperationId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(snapshot.ActId, Is.EqualTo("act_01_wasteland_railway"));
            Assert.That(snapshot.RegionId, Is.EqualTo("region_wasteland_railway"));
            Assert.That(snapshot.EnemyFactionId, Is.EqualTo("faction_wasteland_raiders"));
            Assert.That(snapshot.RiskLevel, Is.EqualTo(2));
            Assert.That(snapshot.WavePlanId, Is.EqualTo("waveplan_act1_opn03_seed1234"));
            Assert.That(snapshot.RewardTableId, Is.EqualTo("reward_act1_opn03_risk2"));
            Assert.That(snapshot.StartAugmentPackageRef, Is.EqualTo("augment_pkg_act1_opn03_v1"));
            Assert.That(snapshot.StartTacticPackageRef, Is.EqualTo("tactic_pkg_act1_opn03_v1"));
            Assert.That(snapshot.RngSeed, Is.EqualTo(1234u));
            Assert.That(snapshot.ServerStartedAt, Is.EqualTo(StartedAt()));
            Assert.That(snapshot.UltimateOperatorId, Is.EqualTo("op_reina"));
            Assert.That(snapshot.CarriageSlots[2].CarriageId, Is.EqualTo("car_cargo"));
        }

        [Test]
        public void ToRunSnapshot_UsesDefaultSelectionsWhenPlayerSelectionIsNull()
        {
            var snapshot = RunStartResponseAdapter.ToRunSnapshot(
                Response(firstClearEligible: false),
                selectedOperatorIds: null,
                selectedUltimateOperatorId: null,
                selectedTrainCarSlots: null,
                completedOperationIds: new[] { "opn_03_water_stop" },
                engineOutput: 82);

            Assert.That(snapshot.OperatorIds[0], Is.EqualTo("op_kyle"));
            Assert.That(snapshot.UltimateOperatorId, Is.EqualTo("op_kyle"));
            Assert.That(snapshot.CarriageSlots.Count, Is.EqualTo(3));
            Assert.That(snapshot.CarriageSlots[2].CarriageId, Is.EqualTo("car_cargo"));
        }

        [Test]
        public void ResponseDto_StoresFirstClearEligibilityWithoutChangingSnapshotBoundary()
        {
            var response = Response(firstClearEligible: true);

            Assert.That(response.FirstClearEligible, Is.EqualTo(true));
        }

        [Test]
        public void ResponseDto_RejectsEmptyRequiredFields()
        {
            Assert.Throws<ArgumentException>(() => new RunStartResponseDto(
                " ",
                "balance_v1",
                "opn_03_water_stop",
                "act_01_wasteland_railway",
                "region_wasteland_railway",
                "faction_wasteland_raiders",
                2,
                "waveplan_act1_opn03_seed1234",
                1234u,
                "reward_act1_opn03_risk2",
                StartedAt(),
                true));
        }

        [Test]
        public void ToRunSnapshot_RejectsNullResponse()
        {
            Assert.Throws<ArgumentNullException>(() => RunStartResponseAdapter.ToRunSnapshot(
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                82));
        }

        private static RunStartResponseDto Response(bool firstClearEligible)
        {
            return new RunStartResponseDto(
                runId: "run_001",
                balanceVersion: "balance_v1",
                operationId: "opn_03_water_stop",
                actId: "act_01_wasteland_railway",
                regionId: "region_wasteland_railway",
                enemyFactionId: "faction_wasteland_raiders",
                riskLevel: 2,
                wavePlanId: "waveplan_act1_opn03_seed1234",
                seed: 1234u,
                rewardTableId: "reward_act1_opn03_risk2",
                serverStartedAt: StartedAt(),
                firstClearEligible: firstClearEligible,
                startAugmentPackageRef: "augment_pkg_act1_opn03_v1",
                startTacticPackageRef: "tactic_pkg_act1_opn03_v1");
        }

        private static DateTime StartedAt()
        {
            return new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc);
        }
    }
}
