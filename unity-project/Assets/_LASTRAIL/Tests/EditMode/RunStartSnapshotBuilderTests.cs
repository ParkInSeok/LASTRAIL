using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class RunStartSnapshotBuilderTests
    {
        [Test]
        public void Build_CreatesRunSnapshotFromServerStateAndPlayerSelection()
        {
            var operators = new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" };
            var trainCars = new[]
            {
                CarriageType.Combat,
                CarriageType.Gun,
                CarriageType.Cargo,
                CarriageType.Defense
            };
            var serverStartedAt = new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc);

            var snapshot = RunStartSnapshotBuilder.Build(
                runId: "run_001",
                balanceVersion: "balance_v1",
                contentVersion: "content_v1",
                operatorIds: operators,
                ultimateOperatorId: "op_reina",
                trainCarSlots: trainCars,
                engineOutput: 82,
                riskLevel: 2,
                operationId: "opn_03_water_stop",
                actId: "act_01_wasteland_railway",
                regionId: "region_wasteland_railway",
                enemyFactionId: "faction_wasteland_raiders",
                wavePlanId: "waveplan_act1_opn03_seed9876",
                rewardTableId: "reward_act1_opn03_risk2",
                seed: 9876u,
                serverStartedAt: serverStartedAt);

            Assert.That(snapshot.OperatorIds[0], Is.EqualTo("op_kyle"));
            Assert.That(snapshot.UltimateOperatorId, Is.EqualTo("op_reina"));
            Assert.That(snapshot.OperationId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(snapshot.ActId, Is.EqualTo("act_01_wasteland_railway"));
            Assert.That(snapshot.RegionId, Is.EqualTo("region_wasteland_railway"));
            Assert.That(snapshot.EnemyFactionId, Is.EqualTo("faction_wasteland_raiders"));
            Assert.That(snapshot.RiskLevel, Is.EqualTo(2));
            Assert.That(snapshot.WavePlanId, Is.EqualTo("waveplan_act1_opn03_seed9876"));
            Assert.That(snapshot.RewardTableId, Is.EqualTo("reward_act1_opn03_risk2"));
            Assert.That(snapshot.ServerStartedAt, Is.EqualTo(serverStartedAt));
            Assert.That(snapshot.TotalWeight, Is.EqualTo(96));
            Assert.That(snapshot.BaseSpeedIndex, Is.EqualTo(0.86f).Within(0.0001f));
            Assert.That(snapshot.CarriageSlots.Count, Is.EqualTo(4));
            Assert.That(snapshot.CarriageSlots[0].SlotIndex, Is.EqualTo(0));
            Assert.That(snapshot.CarriageSlots[0].CarriageId, Is.EqualTo("car_combat"));
            Assert.That(snapshot.CarriageSlots[3].SlotIndex, Is.EqualTo(3));
            Assert.That(snapshot.CarriageSlots[3].CarriageId, Is.EqualTo("car_defense"));
            Assert.That(snapshot.RngSeed, Is.EqualTo(9876u));
        }

        [Test]
        public void Build_RejectsInvalidTrainCarSlotsBeforeCreatingSnapshot()
        {
            var operators = new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" };
            var trainCars = new[] { CarriageType.Gun, CarriageType.Medical };

            Assert.Throws<InvalidOperationException>(() => RunStartSnapshotBuilder.Build(
                "run_001",
                "balance_v1",
                "content_v1",
                operators,
                "op_reina",
                trainCars,
                82,
                2,
                "opn_03_water_stop",
                "act_01_wasteland_railway",
                "region_wasteland_railway",
                "faction_wasteland_raiders",
                "waveplan_act1_opn03_seed9876",
                "reward_act1_opn03_risk2",
                9876u,
                new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc)));
        }
    }
}
