using System;

namespace LastRail.Tactics
{
    public readonly struct ResolvedTacticDefinition
    {
        public ResolvedTacticDefinition(string tacticId, TacticKind kind)
        {
            if (string.IsNullOrWhiteSpace(tacticId))
            {
                throw new ArgumentException("Tactic ID cannot be empty.", nameof(tacticId));
            }

            if (kind != TacticKind.Command && kind != TacticKind.Ultimate)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported tactic kind.");
            }

            TacticId = tacticId;
            Kind = kind;
        }

        public string TacticId { get; }
        public TacticKind Kind { get; }
    }
}
