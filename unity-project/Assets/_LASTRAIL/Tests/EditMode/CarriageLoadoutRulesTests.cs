using NUnit.Framework;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class CarriageLoadoutRulesTests
    {
        [Test]
        public void Validate_AcceptsLoadoutWithExactlyOneCombatAndAllSlotsFilled()
        {
            var loadout = new[]
            {
                CarriageType.Combat,
                CarriageType.Gun,
                CarriageType.Defense,
                CarriageType.Medical
            };

            var result = CarriageLoadoutRules.Validate(loadout, unlockedSlotCount: 4);

            Assert.That(result.IsValid, Is.EqualTo(true));
            Assert.That(result.Error, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Validate_RejectsMissingOrDuplicateCombatCar()
        {
            var missingCombat = new[] { CarriageType.Gun, CarriageType.Defense };
            var duplicateCombat = new[] { CarriageType.Combat, CarriageType.Combat };

            Assert.That(CarriageLoadoutRules.Validate(missingCombat, 2).IsValid, Is.EqualTo(false));
            Assert.That(CarriageLoadoutRules.Validate(duplicateCombat, 2).IsValid, Is.EqualTo(false));
        }

        [Test]
        public void Validate_RejectsTypeCountsAboveMaximums()
        {
            Assert.That(CarriageLoadoutRules.Validate(new[] { CarriageType.Combat, CarriageType.Gun, CarriageType.Gun, CarriageType.Gun }, 4).IsValid, Is.EqualTo(false));
            Assert.That(CarriageLoadoutRules.Validate(new[] { CarriageType.Combat, CarriageType.Medical, CarriageType.Medical, CarriageType.Medical }, 4).IsValid, Is.EqualTo(false));
            Assert.That(CarriageLoadoutRules.Validate(new[] { CarriageType.Combat, CarriageType.Defense, CarriageType.Defense, CarriageType.Defense }, 4).IsValid, Is.EqualTo(false));
            Assert.That(CarriageLoadoutRules.Validate(new[] { CarriageType.Combat, CarriageType.Cargo, CarriageType.Cargo }, 3).IsValid, Is.EqualTo(false));
        }

        [Test]
        public void Validate_RejectsUnfilledOrOverfilledUnlockedSlots()
        {
            Assert.That(CarriageLoadoutRules.Validate(new[] { CarriageType.Combat }, 2).IsValid, Is.EqualTo(false));
            Assert.That(CarriageLoadoutRules.Validate(new[] { CarriageType.Combat, CarriageType.Gun, CarriageType.Medical }, 2).IsValid, Is.EqualTo(false));
        }

        [Test]
        public void MaxCountFor_ReturnsReferenceMaximums()
        {
            Assert.That(CarriageLoadoutRules.MaxCountFor(CarriageType.Combat), Is.EqualTo(1));
            Assert.That(CarriageLoadoutRules.MaxCountFor(CarriageType.Gun), Is.EqualTo(2));
            Assert.That(CarriageLoadoutRules.MaxCountFor(CarriageType.Medical), Is.EqualTo(2));
            Assert.That(CarriageLoadoutRules.MaxCountFor(CarriageType.Defense), Is.EqualTo(2));
            Assert.That(CarriageLoadoutRules.MaxCountFor(CarriageType.Cargo), Is.EqualTo(1));
        }
    }
}
