using System;
using System.Collections.Generic;

namespace LastRail.Tactics
{
    public sealed class ResolvedTacticPool
    {
        public ResolvedTacticPool(IReadOnlyList<string> tacticIds, bool containsUltimateTactic)
        {
            if (tacticIds == null)
            {
                throw new ArgumentNullException(nameof(tacticIds));
            }

            var copied = new string[tacticIds.Count];
            for (var i = 0; i < tacticIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(tacticIds[i]))
                {
                    throw new ArgumentException("Tactic ID cannot be empty.", nameof(tacticIds));
                }

                copied[i] = tacticIds[i];
            }

            TacticIds = copied;
            ContainsUltimateTactic = containsUltimateTactic;
        }

        public IReadOnlyList<string> TacticIds { get; }
        public bool ContainsUltimateTactic { get; }
    }
}
