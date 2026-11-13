using System;
using NUnit.Framework;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class RunSnapshotTests
    {
        [Test]
        public void Create_StoresRunStartStateAndCopiesCollections()
        {
            var operators = new[] { "operator_kyle", "operator_reina", "operator_eve", "operator_sera", "operator_mira" };
            var carriages = new[]
            {
                new CarriageSlotSnapshot(0, "carriage_combat"),
                new CarriageSlotSnapshot(1, "carriage_gun")
            };

            var snapshot = RunSnapshot.Create(
                "run_001",
                "balance_v1",
                "content_v1",
                operators,
                "operator_reina",
                carriages,
                engineOutput: 65,
                totalWeight: 60,
                baseSpeedIndex: 1.05f,
                risk: 1,
                operationId: "operation_annihilation",
                regionId: "region_wasteland_railway",
                factionId: "faction_wasteland_raiders",
                rngSeed: 1234u);

            operators[1] = "operator_vex";

            Assert.That(snapshot.OperatorIds.Count, Is.EqualTo(5));
            Assert.That(snapshot.OperatorIds[1], Is.EqualTo("operator_reina"));
            Assert.That(snapshot.UltimateOperatorId, Is.EqualTo("operator_reina"));
            Assert.That(snapshot.CarriageSlots.Count, Is.EqualTo(2));
            Assert.That(snapshot.BaseSpeedIndex, Is.EqualTo(1.05f).Within(0.0001f));
            Assert.That(snapshot.RngSeed, Is.EqualTo(1234u));
        }

        [Test]
        public void Create_RejectsNonFiveOperatorSquad()
        {
            var operators = new[] { "operator_kyle", "operator_reina", "operator_eve", "operator_sera" };
            var carriages = new[] { new CarriageSlotSnapshot(0, "carriage_combat") };

            Assert.Throws<InvalidOperationException>(() => RunSnapshot.Create(
                "run_001",
                "balance_v1",
                "content_v1",
                operators,
                "operator_reina",
                carriages,
                65,
                60,
                1.05f,
                1,
                "operation_annihilation",
                "region_wasteland_railway",
                "faction_wasteland_raiders",
                1234u));
        }

        [Test]
        public void Create_RejectsUltimateOutsideSquad()
        {
            var operators = new[] { "operator_kyle", "operator_reina", "operator_eve", "operator_sera", "operator_mira" };
            var carriages = new[] { new CarriageSlotSnapshot(0, "carriage_combat") };

            Assert.Throws<InvalidOperationException>(() => RunSnapshot.Create(
                "run_001",
                "balance_v1",
                "content_v1",
                operators,
                "operator_vex",
                carriages,
                65,
                60,
                1.05f,
                1,
                "operation_annihilation",
                "region_wasteland_railway",
                "faction_wasteland_raiders",
                1234u));
        }
    }
}
