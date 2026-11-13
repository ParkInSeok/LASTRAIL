using System;
using NUnit.Framework;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class ActOneTrainCarUnlockRulesTests
    {
        [Test]
        public void GetDefaultUnlockedLoadout_ReturnsTutorialTwoTrainCarsBeforeOperationUnlocks()
        {
            var loadout = ActOneTrainCarUnlockRules.GetDefaultUnlockedLoadout(Array.Empty<string>());

            Assert.That(loadout.UnlockedSlotCount, Is.EqualTo(2));
            Assert.That(loadout.TrainCarIds.Count, Is.EqualTo(2));
            Assert.That(loadout.TrainCarIds[0], Is.EqualTo("car_combat"));
            Assert.That(loadout.TrainCarIds[1], Is.EqualTo("car_gun"));
        }

        [Test]
        public void GetDefaultUnlockedLoadout_AddsCargoAfterWaterStopClear()
        {
            var loadout = ActOneTrainCarUnlockRules.GetDefaultUnlockedLoadout(new[] { "opn_03_water_stop" });

            Assert.That(loadout.UnlockedSlotCount, Is.EqualTo(3));
            Assert.That(loadout.TrainCarIds.Count, Is.EqualTo(3));
            Assert.That(loadout.TrainCarIds[0], Is.EqualTo("car_combat"));
            Assert.That(loadout.TrainCarIds[1], Is.EqualTo("car_gun"));
            Assert.That(loadout.TrainCarIds[2], Is.EqualTo("car_cargo"));
        }

        [Test]
        public void GetDefaultUnlockedLoadout_AddsDefenseAfterNightBoardersClear()
        {
            var loadout = ActOneTrainCarUnlockRules.GetDefaultUnlockedLoadout(new[]
            {
                "opn_03_water_stop",
                "opn_05_night_boarders"
            });

            Assert.That(loadout.UnlockedSlotCount, Is.EqualTo(4));
            Assert.That(loadout.TrainCarIds.Count, Is.EqualTo(4));
            Assert.That(loadout.TrainCarIds[2], Is.EqualTo("car_cargo"));
            Assert.That(loadout.TrainCarIds[3], Is.EqualTo("car_defense"));
        }

        [Test]
        public void GetDefaultUnlockedLoadout_DoesNotDefaultGrantMedicalTrainCar()
        {
            var loadout = ActOneTrainCarUnlockRules.GetDefaultUnlockedLoadout(new[]
            {
                "opn_03_water_stop",
                "opn_05_night_boarders"
            });

            Assert.That(Contains(loadout.TrainCarIds, "car_medical"), Is.EqualTo(false));
        }

        [Test]
        public void GetDefaultUnlockedLoadout_CopiesResultCollections()
        {
            var completedOperations = new[] { "opn_03_water_stop" };

            var loadout = ActOneTrainCarUnlockRules.GetDefaultUnlockedLoadout(completedOperations);
            completedOperations[0] = "opn_05_night_boarders";

            Assert.That(loadout.UnlockedSlotCount, Is.EqualTo(3));
            Assert.That(loadout.TrainCarIds[2], Is.EqualTo("car_cargo"));
        }

        [Test]
        public void GetDefaultUnlockedLoadout_RejectsNullOrEmptyOperationIds()
        {
            Assert.Throws<ArgumentNullException>(() => ActOneTrainCarUnlockRules.GetDefaultUnlockedLoadout(null));
            Assert.Throws<ArgumentException>(() => ActOneTrainCarUnlockRules.GetDefaultUnlockedLoadout(new[] { "opn_03_water_stop", " " }));
        }
        private static bool Contains(System.Collections.Generic.IReadOnlyList<string> values, string value)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

