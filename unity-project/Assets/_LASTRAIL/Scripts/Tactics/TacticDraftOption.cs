using System;

namespace LastRail.Tactics
{
    public readonly struct TacticDraftOption
    {
        public TacticDraftOption(string tacticId)
        {
            if (string.IsNullOrWhiteSpace(tacticId))
            {
                throw new ArgumentException("Tactic ID cannot be empty.", nameof(tacticId));
            }

            TacticId = tacticId;
        }

        public string TacticId { get; }
    }
}
