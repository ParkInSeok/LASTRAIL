using System;
using NUnit.Framework;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class TacticPoolResolverTests
    {
        [Test]
        public void Resolve_ReturnsCommandTacticPoolWhenUltimateDoesNotAppear()
        {
            var pool = TacticPoolResolver.Resolve(
                Tactics(),
                ultimateConditionMet: true,
                ultimateRollPercent: 10);

            Assert.That(pool.TacticIds.Count, Is.EqualTo(5));
            Assert.That(pool.TacticIds[0], Is.EqualTo("cmd_001"));
            Assert.That(pool.TacticIds[4], Is.EqualTo("cmd_005"));
            Assert.That(pool.ContainsUltimateTactic, Is.EqualTo(false));
        }

        [Test]
        public void Resolve_MixesFirstUltimateTacticIntoPoolWhenAppearanceRollPasses()
        {
            var pool = TacticPoolResolver.Resolve(
                Tactics(),
                ultimateConditionMet: true,
                ultimateRollPercent: 9);

            Assert.That(pool.ContainsUltimateTactic, Is.EqualTo(true));
            Assert.That(pool.TacticIds.Count, Is.EqualTo(6));
            Assert.That(pool.TacticIds[0], Is.EqualTo("cmd_001"));
            Assert.That(pool.TacticIds[1], Is.EqualTo("cmd_002"));
            Assert.That(pool.TacticIds[2], Is.EqualTo("ult_001"));
            Assert.That(pool.TacticIds[3], Is.EqualTo("cmd_003"));
        }

        [Test]
        public void Resolve_DoesNotMixUltimateTacticWhenConditionIsNotMetEvenOnZeroRoll()
        {
            var pool = TacticPoolResolver.Resolve(
                Tactics(),
                ultimateConditionMet: false,
                ultimateRollPercent: 0);

            Assert.That(pool.ContainsUltimateTactic, Is.EqualTo(false));
            Assert.That(Contains(pool.TacticIds, "ult_001"), Is.EqualTo(false));
        }

        [Test]
        public void Resolve_RejectsPoolWithFewerThanThreeCommandTactics()
        {
            var tactics = new[]
            {
                new ResolvedTacticDefinition("cmd_001", TacticKind.Command),
                new ResolvedTacticDefinition("cmd_002", TacticKind.Command),
                new ResolvedTacticDefinition("ult_001", TacticKind.Ultimate)
            };

            Assert.Throws<InvalidOperationException>(() => TacticPoolResolver.Resolve(tactics, true, 0));
        }

        [Test]
        public void ResolvedTacticDefinition_RejectsEmptyId()
        {
            Assert.Throws<ArgumentException>(() => new ResolvedTacticDefinition(" ", TacticKind.Command));
        }

        private static ResolvedTacticDefinition[] Tactics()
        {
            return new[]
            {
                new ResolvedTacticDefinition("cmd_001", TacticKind.Command),
                new ResolvedTacticDefinition("cmd_002", TacticKind.Command),
                new ResolvedTacticDefinition("cmd_003", TacticKind.Command),
                new ResolvedTacticDefinition("cmd_004", TacticKind.Command),
                new ResolvedTacticDefinition("cmd_005", TacticKind.Command),
                new ResolvedTacticDefinition("ult_001", TacticKind.Ultimate),
                new ResolvedTacticDefinition("ult_002", TacticKind.Ultimate)
            };
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
