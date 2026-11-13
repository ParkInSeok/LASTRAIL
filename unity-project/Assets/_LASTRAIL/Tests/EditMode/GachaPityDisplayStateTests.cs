using NUnit.Framework;
using LastRail.Gacha;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaPityDisplayStateTests
    {
        [Test]
        public void Create_StoresServerPityCountersAndCalculatesRemainingGuaranteeCounts()
        {
            var state = new GachaPityDisplayState(
                "banner_act1_elite_001",
                totalPullCount: 47,
                elitePityCount: 12,
                pickupElitePityCount: 44,
                rareOrHigherPityCount: 7);

            Assert.That(state.BannerId, Is.EqualTo("banner_act1_elite_001"));
            Assert.That(state.TotalPullCount, Is.EqualTo(47));
            Assert.That(state.ElitePityCount, Is.EqualTo(12));
            Assert.That(state.PickupElitePityCount, Is.EqualTo(44));
            Assert.That(state.RareOrHigherPityCount, Is.EqualTo(7));
            Assert.That(state.EliteGuaranteeThreshold, Is.EqualTo(30));
            Assert.That(state.PickupEliteGuaranteeThreshold, Is.EqualTo(60));
            Assert.That(state.RareOrHigherGuaranteeThreshold, Is.EqualTo(10));
            Assert.That(state.PullsUntilEliteGuarantee, Is.EqualTo(18));
            Assert.That(state.PullsUntilPickupEliteGuarantee, Is.EqualTo(16));
            Assert.That(state.PullsUntilRareOrHigherGuarantee, Is.EqualTo(3));
        }

        [Test]
        public void Create_ClampsRemainingGuaranteeCountsAtZeroForDisplayOnly()
        {
            var state = new GachaPityDisplayState(
                "banner_act1_elite_001",
                totalPullCount: 90,
                elitePityCount: 31,
                pickupElitePityCount: 61,
                rareOrHigherPityCount: 11);

            Assert.That(state.PullsUntilEliteGuarantee, Is.EqualTo(0));
            Assert.That(state.PullsUntilPickupEliteGuarantee, Is.EqualTo(0));
            Assert.That(state.PullsUntilRareOrHigherGuarantee, Is.EqualTo(0));
        }

        [Test]
        public void Create_RejectsEmptyBannerIdOrNegativeCounters()
        {
            Assert.Throws<System.ArgumentException>(() => new GachaPityDisplayState("", 0, 0, 0, 0));
            Assert.Throws<System.ArgumentException>(() => new GachaPityDisplayState("banner", -1, 0, 0, 0));
            Assert.Throws<System.ArgumentException>(() => new GachaPityDisplayState("banner", 0, -1, 0, 0));
            Assert.Throws<System.ArgumentException>(() => new GachaPityDisplayState("banner", 0, 0, -1, 0));
            Assert.Throws<System.ArgumentException>(() => new GachaPityDisplayState("banner", 0, 0, 0, -1));
        }
    }
}
