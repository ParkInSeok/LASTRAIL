using System;
using System.Collections.Generic;
using LastRail.Sortie;
using LastRail.Tactics;

namespace LastRail.Run
{
    public sealed class RunRuntimeState
    {
        private RunRuntimeState(
            RunSnapshot snapshot,
            SortieProgression progression,
            RunTacticState tacticState)
        {
            Snapshot = snapshot;
            Progression = progression;
            TacticState = tacticState;
        }

        public RunSnapshot Snapshot { get; }
        public SortieProgression Progression { get; }
        public RunTacticState TacticState { get; }

        public static RunRuntimeState StartActOne(RunSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new RunRuntimeState(
                snapshot,
                SortieProgression.StartActOne(snapshot.WavePlanId),
                RunTacticState.Empty());
        }

        public RunRuntimeState CompleteCurrentWave()
        {
            return new RunRuntimeState(
                Snapshot,
                Progression.CompleteCurrentWave(),
                TacticState);
        }

        public RunRuntimeState CompleteFinalOperation()
        {
            return new RunRuntimeState(
                Snapshot,
                Progression.CompleteFinalOperation(),
                TacticState);
        }

        public RunRuntimeState RecordTacticSelection(
            int waveNumber,
            string tacticId,
            TacticKind tacticKind,
            bool draftContainedUltimateTactic)
        {
            return new RunRuntimeState(
                Snapshot,
                Progression,
                TacticState.RecordSelection(waveNumber, tacticId, tacticKind, draftContainedUltimateTactic));
        }

        public RunRuntimeState RecordConfirmedTacticSelection(
            TacticDraftSession session,
            IReadOnlyList<ResolvedTacticDefinition> resolvedTactics)
        {
            return new RunRuntimeState(
                Snapshot,
                Progression,
                RunTacticStateRecorder.RecordConfirmedSelection(TacticState, session, resolvedTactics));
        }
    }
}
