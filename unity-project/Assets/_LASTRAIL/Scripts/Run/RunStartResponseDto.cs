using System;

namespace LastRail.Run
{
    public sealed class RunStartResponseDto
    {
        public RunStartResponseDto(
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
            bool firstClearEligible,
            string startAugmentPackageRef = null,
            string startTacticPackageRef = null)
        {
            RequireText(runId, nameof(runId));
            RequireText(balanceVersion, nameof(balanceVersion));
            RequireText(operationId, nameof(operationId));
            RequireText(actId, nameof(actId));
            RequireText(regionId, nameof(regionId));
            RequireText(enemyFactionId, nameof(enemyFactionId));
            RequireText(wavePlanId, nameof(wavePlanId));
            RequireText(rewardTableId, nameof(rewardTableId));

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
            FirstClearEligible = firstClearEligible;
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
        public bool FirstClearEligible { get; }
        public string StartAugmentPackageRef { get; }
        public string StartTacticPackageRef { get; }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
