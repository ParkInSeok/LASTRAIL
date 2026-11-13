using System;
using System.Collections.Generic;
using LastRail.Sortie;

namespace LastRail.Run
{
    public sealed class RunRuntimeStateExport
    {
        public RunRuntimeStateExport(
            string runId,
            string balanceVersion,
            string operationId,
            string actId,
            string regionId,
            string enemyFactionId,
            int riskLevel,
            string wavePlanId,
            uint seed,
            string rewardTableId,
            DateTime serverStartedAt,
            SortieStage currentStage,
            int currentWaveNumber,
            int completedWaveCount,
            bool finalOperationCompleted,
            IReadOnlyList<TacticSelectionExport> tacticSelections,
            string startAugmentPackageRef = null,
            string startTacticPackageRef = null)
        {
            RunId = runId;
            BalanceVersion = balanceVersion;
            OperationId = operationId;
            ActId = actId;
            RegionId = regionId;
            EnemyFactionId = enemyFactionId;
            RiskLevel = riskLevel;
            WavePlanId = wavePlanId;
            Seed = seed;
            RewardTableId = rewardTableId;
            ServerStartedAt = serverStartedAt;
            CurrentStage = currentStage;
            CurrentWaveNumber = currentWaveNumber;
            CompletedWaveCount = completedWaveCount;
            FinalOperationCompleted = finalOperationCompleted;
            TacticSelections = CopySelections(tacticSelections);
            StartAugmentPackageRef = NormalizeOptional(startAugmentPackageRef);
            StartTacticPackageRef = NormalizeOptional(startTacticPackageRef);
        }

        public string RunId { get; }
        public string BalanceVersion { get; }
        public string OperationId { get; }
        public string ActId { get; }
        public string RegionId { get; }
        public string EnemyFactionId { get; }
        public int RiskLevel { get; }
        public string WavePlanId { get; }
        public uint Seed { get; }
        public string RewardTableId { get; }
        public DateTime ServerStartedAt { get; }
        public SortieStage CurrentStage { get; }
        public int CurrentWaveNumber { get; }
        public int CompletedWaveCount { get; }
        public bool FinalOperationCompleted { get; }
        public IReadOnlyList<TacticSelectionExport> TacticSelections { get; }
        public string StartAugmentPackageRef { get; }
        public string StartTacticPackageRef { get; }

        private static IReadOnlyList<TacticSelectionExport> CopySelections(IReadOnlyList<TacticSelectionExport> tacticSelections)
        {
            if (tacticSelections == null)
            {
                throw new ArgumentNullException(nameof(tacticSelections));
            }

            var copied = new TacticSelectionExport[tacticSelections.Count];
            for (var i = 0; i < tacticSelections.Count; i++)
            {
                copied[i] = tacticSelections[i];
            }

            return copied;
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
