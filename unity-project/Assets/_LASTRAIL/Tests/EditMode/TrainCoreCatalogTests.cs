using NUnit.Framework;
using LastRail.Train;

namespace LastRail.Tests.EditMode
{
    public sealed class TrainCoreCatalogTests
    {
        [Test]
        public void All_ReturnsLvOneToTenReferenceTable()
        {
            var entries = TrainCoreCatalog.All;

            Assert.That(entries.Length, Is.EqualTo(10));
            AssertEntry(entries[0], 1, 3000.0f, 0.00f, 65, 0.00f);
            AssertEntry(entries[1], 2, 3120.0f, 0.01f, 68, 0.02f);
            AssertEntry(entries[2], 3, 3240.0f, 0.02f, 72, 0.04f);
            AssertEntry(entries[3], 4, 3360.0f, 0.03f, 75, 0.06f);
            AssertEntry(entries[4], 5, 3480.0f, 0.04f, 78, 0.08f);
            AssertEntry(entries[5], 6, 3600.0f, 0.05f, 82, 0.10f);
            AssertEntry(entries[6], 7, 3720.0f, 0.06f, 86, 0.12f);
            AssertEntry(entries[7], 8, 3840.0f, 0.07f, 89, 0.14f);
            AssertEntry(entries[8], 9, 3960.0f, 0.08f, 92, 0.16f);
            AssertEntry(entries[9], 10, 4080.0f, 0.10f, 95, 0.18f);
        }

        [Test]
        public void ForLevel_ReturnsRequestedLevel()
        {
            var levelEight = TrainCoreCatalog.ForLevel(8);

            AssertEntry(levelEight, 8, 3840.0f, 0.07f, 89, 0.14f);
        }

        [Test]
        public void ForLevel_RejectsOutOfRangeLevel()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TrainCoreCatalog.ForLevel(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TrainCoreCatalog.ForLevel(11));
        }

        private static void AssertEntry(
            TrainCoreDefinition entry,
            int level,
            float durability,
            float armorBonusRatio,
            int engineOutput,
            float fireControlBonusRatio)
        {
            Assert.That(entry.Level, Is.EqualTo(level));
            Assert.That(entry.Durability, Is.EqualTo(durability).Within(0.0001f));
            Assert.That(entry.ArmorBonusRatio, Is.EqualTo(armorBonusRatio).Within(0.0001f));
            Assert.That(entry.EngineOutput, Is.EqualTo(engineOutput));
            Assert.That(entry.FireControlBonusRatio, Is.EqualTo(fireControlBonusRatio).Within(0.0001f));
        }
    }
}
