using NUnit.Framework;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class CarriageDurabilityCatalogTests
    {
        [Test]
        public void ForType_ReturnsReferenceHpAndArmorByCarriageType()
        {
            AssertDurability(CarriageType.Combat, 1100.0f, 0.10f);
            AssertDurability(CarriageType.Gun, 1000.0f, 0.08f);
            AssertDurability(CarriageType.Cargo, 900.0f, 0.05f);
            AssertDurability(CarriageType.Medical, 950.0f, 0.05f);
            AssertDurability(CarriageType.Defense, 1350.0f, 0.20f);
        }

        [Test]
        public void ForType_RejectsUnsupportedCarriageType()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                CarriageDurabilityCatalog.ForType((CarriageType)999));
        }

        private static void AssertDurability(CarriageType type, float expectedMaxHp, float expectedArmorRatio)
        {
            var durability = CarriageDurabilityCatalog.ForType(type);

            Assert.That(durability.CarriageType, Is.EqualTo(type));
            Assert.That(durability.MaxHp, Is.EqualTo(expectedMaxHp).Within(0.0001f));
            Assert.That(durability.ArmorDamageReductionRatio, Is.EqualTo(expectedArmorRatio).Within(0.0001f));
        }
    }
}
