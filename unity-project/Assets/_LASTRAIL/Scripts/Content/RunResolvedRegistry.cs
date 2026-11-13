using System;
using System.Collections.Generic;

namespace LastRail.Content
{
    public sealed class RunResolvedRegistry
    {
        private readonly Dictionary<string, IStaticDefinition> definitionsById;

        public RunResolvedRegistry(IEnumerable<IStaticDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            definitionsById = new Dictionary<string, IStaticDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Definition collection cannot contain null entries.", nameof(definitions));
                }

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    throw new ArgumentException("Definition stable ID cannot be empty.", nameof(definitions));
                }

                if (definitionsById.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException($"Duplicate static definition ID: {definition.Id}");
                }

                definitionsById.Add(definition.Id, definition);
            }
        }

        public IStaticDefinition Resolve(string id)
        {
            if (TryResolve(id, out var definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"Static definition not found: {id}");
        }

        public bool TryResolve(string id, out IStaticDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                definition = null;
                return false;
            }

            return definitionsById.TryGetValue(id, out definition);
        }
    }
}
