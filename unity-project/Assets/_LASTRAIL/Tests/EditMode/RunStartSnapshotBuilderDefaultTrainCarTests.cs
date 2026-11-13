using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class RunStartSnapshotBuilderDefaultTrainCarTests
    {
        [Test]
        public void BuildActOne_UsesDefaultOperatorSquadAndKyleUltimateWhenPlayerSelectionIsNull()
        {
            var snapshot = RunStartSnapshotBuilder.BuildActOne(
                runId: "run_001",
                balanceVersion: "balance_v1",
                contentVersion: "content_v1",
                selectedOperatorIds: null,
                selectedUltimateOperatorId: null,
                selectedTrainCarSlots: null,
                completedOperationIds: new[] { "opn_03_water_stop" },
                engineOutput: 82,
                riskLevel: 2,
                operationId: "opn_04_scrapstar_show",
                actId: "act_01_wasteland_railway",
                regionId: "region_wasteland_railway",
                enemyFactionId: "faction_scrapstar_troupe",
                wavePlanId: "waveplan_act1_opn04_seed2222",
                rewardTableId: "reward_act1_opn04_risk2",
                seed: 2222u,
                serverStartedAt: StartedAt());

            Assert.That(snapshot.OperatorIds.Count, Is.EqualTo(5));
            Assert.That(snapshot.OperatorIds[0], Is.EqualTo("op_kyle"));
            Assert.That(snapshot.OperatorIds[1], Is.EqualTo("op_reina"));
            Assert.That(snapshot.OperatorIds[2], Is.EqualTo("op_eve"));
            Assert.That(snapshot.OperatorIds[3], Is.EqualTo("op_sera"));
            Assert.That(snapshot.OperatorIds[4], Is.EqualTo("op_mira"));
            Assert.That(snapshot.UltimateOperatorId, Is.EqualTo("op_kyle"));
        }

        [Test]
        public void BuildActOne_UsesDefaultTrainCarLoadoutWhenPlayerSelectionIsNull()
        {
            var snapshot = RunStartSnapshotBuilder.BuildActOne(
                runId: "run_001",
                balanceVersion: "balance_v1",
                contentVersion: "content_v1",
                selectedOperatorIds: Operators(),
                selectedUltimateOperatorId: "op_reina",
                selectedTrainCarSlots: null,
                completedOperationIds: new[] { "opn_03_water_stop" },
                engineOutput: 82,
                riskLevel: 2,
                operationId: "opn_04_scrapstar_show",
                actId: "act_01_wasteland_railway",
                regionId: "region_wasteland_railway",
                enemyFactionId: "faction_scrapstar_troupe",
                wavePlanId: "waveplan_act1_opn04_seed2222",
                rewardTableId: "reward_act1_opn04_risk2",
                seed: 2222u,
                serverStartedAt: StartedAt());

            Assert.That(snapshot.CarriageSlots.Count, Is.EqualTo(3));
            Assert.That(snapshot.CarriageSlots[0].CarriageId, Is.EqualTo("car_combat"));
            Assert.That(snapshot.CarriageSlots[1].CarriageId, Is.EqualTo("car_gun"));
            Assert.That(snapshot.CarriageSlots[2].CarriageId, Is.EqualTo("car_cargo"));
            Assert.That(snapshot.TotalWeight, Is.EqualTo(74));
            Assert.That(snapshot.BaseSpeedIndex, Is.EqualTo(1.08f).Within(0.0001f));
        }

        [Test]
        public void BuildActOne_UsesPlayerSelectedOperatorSquadUltimateAndTrainCarsWhenProvided()
        {
            var snapshot = RunStartSnapshotBuilder.BuildActOne(
                runId: "run_001",
                balanceVersion: "balance_v1",
                contentVersion: "content_v1",
                selectedOperatorIds: Operators(),
                selectedUltimateOperatorId: "op_reina",
                selectedTrainCarSlots: new[] { CarriageType.Combat, CarriageType.Gun, CarriageType.Medical },
                completedOperationIds: new[] { "opn_03_water_stop" },
                engineOutput: 82,
                riskLevel: 2,
                operationId: "opn_04_scrapstar_show",
                actId: "act_01_wasteland_railway",
                regionId: "region_wasteland_railway",
                enemyFactionId: "faction_scrapstar_troupe",
                wavePlanId: "waveplan_act1_opn04_seed2222",
                rewardTableId: "reward_act1_opn04_risk2",
                seed: 2222u,
                serverStartedAt: StartedAt());

            Assert.That(snapshot.OperatorIds[1], Is.EqualTo("op_reina"));
            Assert.That(snapshot.UltimateOperatorId, Is.EqualTo("op_reina"));
            Assert.That(snapshot.CarriageSlots.Count, Is.EqualTo(3));
            Assert.That(snapshot.CarriageSlots[2].CarriageId, Is.EqualTo("car_medical"));
            Assert.That(snapshot.TotalWeight, Is.EqualTo(70));
        }

        private static string[] Operators()
        {
            return new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" };
        }

        private static DateTime StartedAt()
        {
            return new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc);
        }
    }
}
