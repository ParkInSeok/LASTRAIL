using System;

namespace LastRail.Run
{
    public static class RunRuntimeStateExporter
    {
        public static RunRuntimeStateExport ToDebugExport(RunRuntimeState runtimeState)
        {
            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            return BuildExport(runtimeState);
        }

        public static RunRuntimeStateExport ToResultSubmitExport(RunRuntimeState runtimeState)
        {
            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            if (!runtimeState.Progression.IsComplete)
            {
                throw new InvalidOperationException("Run result submit export requires a completed runtime state.");
            }

            return BuildExport(runtimeState);
        }

        private static RunRuntimeStateExport BuildExport(RunRuntimeState runtimeState)
        {
            var snapshot = runtimeState.Snapshot;
            var progression = runtimeState.Progression;
            var selections = new TacticSelectionExport[runtimeState.TacticState.Selections.Count];

            for (var i = 0; i < runtimeState.TacticState.Selections.Count; i++)
            {
                var selection = runtimeState.TacticState.Selections[i];
                selections[i] = new TacticSelectionExport(
                    selection.WaveNumber,
                    selection.TacticId,
                    selection.TacticKind,
                    selection.DraftContainedUltimateTactic);
            }

            return new RunRuntimeStateExport(
                snapshot.RunId,
                snapshot.BalanceVersion,
                snapshot.OperationId,
                snapshot.ActId,
                snapshot.RegionId,
                snapshot.EnemyFactionId,
                snapshot.RiskLevel,
                snapshot.WavePlanId,
                snapshot.RngSeed,
                snapshot.RewardTableId,
                snapshot.ServerStartedAt,
                progression.Stage,
                progression.CurrentWaveNumber,
                progression.CompletedWaveCount,
                progression.IsComplete,
                selections,
                snapshot.StartAugmentPackageRef,
                snapshot.StartTacticPackageRef);
        }
    }
}
