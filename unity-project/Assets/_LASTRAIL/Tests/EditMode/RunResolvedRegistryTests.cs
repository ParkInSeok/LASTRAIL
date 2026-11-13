using System;
using NUnit.Framework;
using LastRail.Content;

namespace LastRail.Tests.EditMode
{
    public sealed class RunResolvedRegistryTests
    {
        [Test]
        public void Resolve_ReturnsDefinitionByStableId()
        {
            var registry = new RunResolvedRegistry(new IStaticDefinition[]
            {
                new TestDefinition("operator_reina"),
                new TestDefinition("carriage_combat")
            });

            var definition = registry.Resolve("operator_reina");

            Assert.That(definition.Id, Is.EqualTo("operator_reina"));
            Assert.That(registry.TryResolve("missing", out _), Is.EqualTo(false));
        }

        [Test]
        public void Constructor_RejectsDuplicateStableIds()
        {
            Assert.Throws<InvalidOperationException>(() => new RunResolvedRegistry(new IStaticDefinition[]
            {
                new TestDefinition("operator_reina"),
                new TestDefinition("operator_reina")
            }));
        }

        private sealed class TestDefinition : IStaticDefinition
        {
            public TestDefinition(string id)
            {
                Id = id;
            }

            public string Id { get; }
        }
    }
}
