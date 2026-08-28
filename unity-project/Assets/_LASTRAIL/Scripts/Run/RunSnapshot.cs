using System;
using System.Collections.Generic;

namespace LastRail.Run
{
    public sealed class RunSnapshot
    {
        private const int RequiredOperatorCount = 5;

        private RunSnapshot(
            string runId,
            string balanceVersion,
            string contentVersion,
            IReadOnlyList<string> operatorIds,
            string ultimateOperatorId,
            IReadOnlyList<CarriageSlotSnapshot> carriageSlots,
            int engineOutput,
            int totalWeight,
            float baseSpeedIndex,
            int riskLevel,
            string operationId,
            string actId,
            string regionId,
            string enemyFactionId,
            string wavePlanId,
            string rewardTableId,
            uint rngSeed,
            DateTime serverStartedAt,
            string startAugmentPackageRef,
            string startTacticPackageRef)
        {
            RunId = runId;
            BalanceVersion = balanceVersion;
            ContentVersion = contentVersion;
            OperatorIds = operatorIds;
            UltimateOperatorId = ultimateOperatorId;
            CarriageSlots = carriageSlots;
            EngineOutput = engineOutput;
            TotalWeight = totalWeight;
            BaseSpeedIndex = baseSpeedIndex;
            RiskLevel = riskLevel;
            OperationId = operationId;
            ActId = actId;
            RegionId = regionId;
            EnemyFactionId = enemyFactionId;
            WavePlanId = wavePlanId;
            RewardTableId = rewardTableId;
            RngSeed = rngSeed;
            ServerStartedAt = serverStartedAt;
            StartAugmentPackageRef = NormalizeOptional(startAugmentPackageRef);
            StartTacticPackageRef = NormalizeOptional(startTacticPackageRef);
        }

        public string RunId { get; }
        public string BalanceVersion { get; }
        public string ContentVersion { get; }
        public IReadOnlyList<string> OperatorIds { get; }
        public string UltimateOperatorId { get; }
        public IReadOnlyList<CarriageSlotSnapshot> CarriageSlots { get; }
        public int EngineOutput { get; }
        public int TotalWeight { get; }
        public float BaseSpeedIndex { get; }
        public int RiskLevel { get; }
        public int Risk { get { return RiskLevel; } }
        public string OperationId { get; }
        public string ActId { get; }
        public string RegionId { get; }
        public string EnemyFactionId { get; }
        public string FactionId { get { return EnemyFactionId; } }
        public string WavePlanId { get; }
        public string RewardTableId { get; }
        public uint RngSeed { get; }
        public DateTime ServerStartedAt { get; }
        public string StartAugmentPackageRef { get; }
        public string StartTacticPackageRef { get; }

        public static RunSnapshot Create(
            string runId,
            string balanceVersion,
            string contentVersion,
            IReadOnlyList<string> operatorIds,
            string ultimateOperatorId,
            IReadOnlyList<CarriageSlotSnapshot> carriageSlots,
            int engineOutput,
            int totalWeight,
            float baseSpeedIndex,
            int risk,
            string operationId,
            string regionId,
            string factionId,
            uint rngSeed)
        {
            return Create(
                runId,
                balanceVersion,
                contentVersion,
                operatorIds,
                ultimateOperatorId,
                carriageSlots,
                engineOutput,
                totalWeight,
                baseSpeedIndex,
                risk,
                operationId,
                "act_unset",
                regionId,
                factionId,
                "waveplan_unset",
                "reward_unset",
                rngSeed,
                DateTime.MinValue,
                null,
                null);
        }

        public static RunSnapshot Create(
            string runId,
            string balanceVersion,
            string contentVersion,
            IReadOnlyList<string> operatorIds,
            string ultimateOperatorId,
            IReadOnlyList<CarriageSlotSnapshot> carriageSlots,
            int engineOutput,
            int totalWeight,
            float baseSpeedIndex,
            int riskLevel,
            string operationId,
            string actId,
            string regionId,
            string enemyFactionId,
            string wavePlanId,
            string rewardTableId,
            uint rngSeed,
            DateTime serverStartedAt,
            string startAugmentPackageRef = null,
            string startTacticPackageRef = null)
        {
            RequireText(runId, nameof(runId));
            RequireText(balanceVersion, nameof(balanceVersion));
            RequireText(contentVersion, nameof(contentVersion));
            RequireText(ultimateOperatorId, nameof(ultimateOperatorId));
            RequireText(operationId, nameof(operationId));
            RequireText(actId, nameof(actId));
            RequireText(regionId, nameof(regionId));
            RequireText(enemyFactionId, nameof(enemyFactionId));
            RequireText(wavePlanId, nameof(wavePlanId));
            RequireText(rewardTableId, nameof(rewardTableId));

            if (operatorIds == null)
            {
                throw new ArgumentNullException(nameof(operatorIds));
            }

            if (operatorIds.Count != RequiredOperatorCount)
            {
                throw new InvalidOperationException("RunSnapshot requires exactly five operator IDs.");
            }

            var copiedOperatorIds = new string[operatorIds.Count];
            var hasUltimateOperator = false;
            for (var i = 0; i < operatorIds.Count; i++)
            {
                RequireText(operatorIds[i], nameof(operatorIds));
                copiedOperatorIds[i] = operatorIds[i];
                hasUltimateOperator |= string.Equals(operatorIds[i], ultimateOperatorId, StringComparison.Ordinal);
            }

            if (!hasUltimateOperator)
            {
                throw new InvalidOperationException("Ultimate operator must be included in the five-person squad.");
            }

            if (carriageSlots == null)
            {
                throw new ArgumentNullException(nameof(carriageSlots));
            }

            var copiedCarriageSlots = new CarriageSlotSnapshot[carriageSlots.Count];
            for (var i = 0; i < carriageSlots.Count; i++)
            {
                copiedCarriageSlots[i] = carriageSlots[i];
            }

            return new RunSnapshot(
                runId,
                balanceVersion,
                contentVersion,
                copiedOperatorIds,
                ultimateOperatorId,
                copiedCarriageSlots,
                engineOutput,
                totalWeight,
                baseSpeedIndex,
                riskLevel,
                operationId,
                actId,
                regionId,
                enemyFactionId,
                wavePlanId,
                rewardTableId,
                rngSeed,
                serverStartedAt,
                startAugmentPackageRef,
                startTacticPackageRef);
        }

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
