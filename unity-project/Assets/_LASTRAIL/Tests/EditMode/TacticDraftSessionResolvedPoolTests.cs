using NUnit.Framework;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class TacticDraftSessionResolvedPoolTests
    {
        [Test]
        public void StartFromResolvedPool_StartsSessionFromResolverResultAndKeepsUltimateFlag()
        {
            var resolvedPool = new ResolvedTacticPool(
                new[] { "cmd_001", "cmd_002", "ult_001", "cmd_003" },
                containsUltimateTactic: true);

            var session = TacticDraftSession.StartFromResolvedPool(3, resolvedPool);

            Assert.That(session.Options.Count, Is.EqualTo(3));
            Assert.That(session.Options[0].TacticId, Is.EqualTo("cmd_001"));
            Assert.That(session.Options[1].TacticId, Is.EqualTo("cmd_002"));
            Assert.That(session.Options[2].TacticId, Is.EqualTo("ult_001"));
            Assert.That(session.ContainsUltimateTactic, Is.EqualTo(true));
        }

        [Test]
        public void StartFromResolvedPool_KeepsFalseUltimateFlagForCommandOnlyPool()
        {
            var resolvedPool = new ResolvedTacticPool(
                new[] { "cmd_001", "cmd_002", "cmd_003" },
                containsUltimateTactic: false);

            var session = TacticDraftSession.StartFromResolvedPool(3, resolvedPool);

            Assert.That(session.ContainsUltimateTactic, Is.EqualTo(false));
        }
    }
}
