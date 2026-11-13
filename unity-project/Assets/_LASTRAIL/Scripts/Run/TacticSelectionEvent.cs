using System;
using LastRail.Tactics;

namespace LastRail.Run
{
    public readonly struct TacticSelectionEvent
    {
        public TacticSelectionEvent(
            int waveNumber,
            string tacticId,
            TacticKind tacticKind,
            bool draftContainedUltimateTactic)
        {
            if (string.IsNullOrWhiteSpace(tacticId))
            {
                throw new ArgumentException("Tactic ID cannot be empty.", nameof(tacticId));
            }

            if (tacticKind != TacticKind.Command && tacticKind != TacticKind.Ultimate)
            {
                throw new ArgumentOutOfRangeException(nameof(tacticKind), tacticKind, "Unsupported tactic kind.");
            }

            WaveNumber = waveNumber;
            TacticId = tacticId;
            TacticKind = tacticKind;
            DraftContainedUltimateTactic = draftContainedUltimateTactic;
        }

        public int WaveNumber { get; }
        public string TacticId { get; }
        public TacticKind TacticKind { get; }
        public bool DraftContainedUltimateTactic { get; }
    }
}
