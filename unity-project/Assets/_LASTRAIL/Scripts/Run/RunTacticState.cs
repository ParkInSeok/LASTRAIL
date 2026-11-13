using System;
using System.Collections.Generic;
using LastRail.Tactics;

namespace LastRail.Run
{
    public sealed class RunTacticState
    {
        private RunTacticState(IReadOnlyList<TacticSelectionEvent> selections)
        {
            Selections = selections;
        }

        public IReadOnlyList<TacticSelectionEvent> Selections { get; }

        public static RunTacticState Empty()
        {
            return new RunTacticState(new TacticSelectionEvent[0]);
        }

        public RunTacticState RecordSelection(
            int waveNumber,
            string tacticId,
            TacticKind tacticKind,
            bool draftContainedUltimateTactic)
        {
            if (!TacticDraftRules.IsCommandTacticDraftWave(waveNumber))
            {
                throw new InvalidOperationException("Tactic selection can only be recorded at command tactic draft waves.");
            }

            if (HasSelectionAtWave(waveNumber))
            {
                throw new InvalidOperationException("Tactic selection has already been recorded for this wave.");
            }

            var copied = new TacticSelectionEvent[Selections.Count + 1];
            for (var i = 0; i < Selections.Count; i++)
            {
                copied[i] = Selections[i];
            }

            copied[copied.Length - 1] = new TacticSelectionEvent(
                waveNumber,
                tacticId,
                tacticKind,
                draftContainedUltimateTactic);

            return new RunTacticState(copied);
        }

        public bool HasSelectionAtWave(int waveNumber)
        {
            for (var i = 0; i < Selections.Count; i++)
            {
                if (Selections[i].WaveNumber == waveNumber)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
