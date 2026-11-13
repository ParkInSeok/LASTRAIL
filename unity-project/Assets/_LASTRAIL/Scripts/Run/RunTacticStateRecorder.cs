using System;
using System.Collections.Generic;
using LastRail.Tactics;

namespace LastRail.Run
{
    public static class RunTacticStateRecorder
    {
        public static RunTacticState RecordConfirmedSelection(
            RunTacticState state,
            TacticDraftSession session,
            IReadOnlyList<ResolvedTacticDefinition> resolvedTactics)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (resolvedTactics == null)
            {
                throw new ArgumentNullException(nameof(resolvedTactics));
            }

            if (!session.IsSelectionConfirmed)
            {
                throw new InvalidOperationException("Tactic draft session selection is not confirmed.");
            }

            return state.RecordSelection(
                session.WaveNumber,
                session.SelectedTacticId,
                KindFor(session.SelectedTacticId, resolvedTactics),
                session.ContainsUltimateTactic);
        }

        private static TacticKind KindFor(string tacticId, IReadOnlyList<ResolvedTacticDefinition> resolvedTactics)
        {
            for (var i = 0; i < resolvedTactics.Count; i++)
            {
                if (string.Equals(resolvedTactics[i].TacticId, tacticId, StringComparison.Ordinal))
                {
                    return resolvedTactics[i].Kind;
                }
            }

            throw new InvalidOperationException("Selected tactic ID was not found in resolved tactic definitions.");
        }
    }
}
