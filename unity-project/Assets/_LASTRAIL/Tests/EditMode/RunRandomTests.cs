using NUnit.Framework;
using LastRail.Core.Simulation;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRandomTests
    {
        [Test]
        public void NextUInt_ProducesRepeatableSequenceForSameSeed()
        {
            var first = new RunRandom(12345u);
            var second = new RunRandom(12345u);

            Assert.That(first.NextUInt(), Is.EqualTo(second.NextUInt()));
            Assert.That(first.NextUInt(), Is.EqualTo(second.NextUInt()));
            Assert.That(first.NextUInt(), Is.EqualTo(second.NextUInt()));
        }

        [Test]
        public void Range_ReturnsInclusiveMinimumAndExclusiveMaximum()
        {
            var random = new RunRandom(1u);

            for (var i = 0; i < 128; i++)
            {
                var value = random.Range(4, 9);
                Assert.That(value, Is.GreaterThanOrEqualTo(4));
                Assert.That(value, Is.LessThan(9));
            }
        }
    }
}
