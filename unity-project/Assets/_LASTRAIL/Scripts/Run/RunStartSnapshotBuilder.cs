using System;
using System.Collections.Generic;
using LastRail.Operators;
using LastRail.Train;

namespace LastRail.Run
{
    public static class RunStartSnapshotBuilder
    {
        public static RunSnapshot BuildActOne(
            string runId,
            string balanceVersion,
            string contentVersion,
            IReadOnlyList<string> selectedOperatorIds,
            string selectedUltimateOperatorId,
            IReadOnlyList<CarriageType> selectedTrainCarSlots,
            IReadOnlyList<string> completedOperationIds,
            int engineOutput,
            int riskLevel,
            string operationId,
            string actId,
            string regionId,
            string enemyFactionId,
            string wavePlanId,
            string rewardTableId,
            uint seed,
            DateTime serverStartedAt,
            string startAugmentPackageRef = null,
            string startTacticPackageRef = null,
            string operationPreviewId = null,
            RunEncounterType encounterType = RunEncounterType.Ordinary15Wave,
            int? ordinaryTotalWaves = null,
            string raidId = null,
            string bossId = null,
            IReadOnlyList<string> bossPhaseRefs = null,
            IReadOnlyList<string> allowedOperatorSlots = null)
        {
            var defaultRoster = selectedOperatorIds == null || string.IsNullOrWhiteSpace(selectedUltimateOperatorId)
                ? ActOneOperatorRosterRules.CreateInitialRoster()
                : null;

            var operatorIds = selectedOperatorIds ?? defaultRoster.OwnedOperatorIds;
            var ultimateOperatorId = string.IsNullOrWhiteSpace(selectedUltimateOperatorId)
                ? ActOneOperatorRosterRules.KyleOperatorId
                : selectedUltimateOperatorId;
            var trainCarSlots = selectedTrainCarSlots ?? DefaultTrainCarSlotsFor(completedOperationIds);

            return Build(
                runId,
                balanceVersion,
                contentVersion,
                operatorIds,
                ultimateOperatorId,
                trainCarSlots,
                engineOutput,
                riskLevel,
                operationId,
                actId,
                regionId,
                enemyFactionId,
                wavePlanId,
                rewardTableId,
                seed,
                serverStartedAt,
                startAugmentPackageRef,
                startTacticPackageRef,
                operationPreviewId,
                encounterType,
                ordinaryTotalWaves,
                raidId,
                bossId,
                bossPhaseRefs,
                allowedOperatorSlots);
        }

        public static RunSnapshot Build(
            string runId,
            string balanceVersion,
            string contentVersion,
            IReadOnlyList<string> operatorIds,
            string ultimateOperatorId,
            IReadOnlyList<CarriageType> trainCarSlots,
            int engineOutput,
            int riskLevel,
            string operationId,
            string actId,
            string regionId,
            string enemyFactionId,
            string wavePlanId,
            string rewardTableId,
            uint seed,
            DateTime serverStartedAt,
            string startAugmentPackageRef = null,
            string startTacticPackageRef = null,
            string operationPreviewId = null,
            RunEncounterType encounterType = RunEncounterType.Ordinary15Wave,
            int? ordinaryTotalWaves = null,
            string raidId = null,
            string bossId = null,
            IReadOnlyList<string> bossPhaseRefs = null,
            IReadOnlyList<string> allowedOperatorSlots = null)
        {
            if (trainCarSlots == null)
            {
                throw new ArgumentNullException(nameof(trainCarSlots));
            }

            var validation = CarriageLoadoutRules.Validate(trainCarSlots, trainCarSlots.Count);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.Error);
            }

            var trainCarSlotSnapshots = new CarriageSlotSnapshot[trainCarSlots.Count];
            for (var i = 0; i < trainCarSlots.Count; i++)
            {
                trainCarSlotSnapshots[i] = new CarriageSlotSnapshot(i, StableIdFor(trainCarSlots[i]));
            }

            var totalWeight = TrainWeightCalculator.TotalWeight(trainCarSlots);
            var baseSpeedIndex = TrainSpeedCalculator.CalculateBaseSpeedIndex(engineOutput, totalWeight);

            return RunSnapshot.Create(
                runId,
                balanceVersion,
                contentVersion,
                operatorIds,
                ultimateOperatorId,
                trainCarSlotSnapshots,
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
                seed,
                serverStartedAt,
                startAugmentPackageRef,
                startTacticPackageRef,
                operationPreviewId,
                encounterType,
                ordinaryTotalWaves,
                raidId,
                bossId,
                bossPhaseRefs,
                allowedOperatorSlots);
        }

        private static IReadOnlyList<CarriageType> DefaultTrainCarSlotsFor(IReadOnlyList<string> completedOperationIds)
        {
            var defaultLoadout = ActOneTrainCarUnlockRules.GetDefaultUnlockedLoadout(completedOperationIds);
            var trainCarSlots = new CarriageType[defaultLoadout.TrainCarIds.Count];
            for (var i = 0; i < defaultLoadout.TrainCarIds.Count; i++)
            {
                trainCarSlots[i] = TrainCarTypeFor(defaultLoadout.TrainCarIds[i]);
            }

            return trainCarSlots;
        }

        private static CarriageType TrainCarTypeFor(string trainCarId)
        {
            switch (trainCarId)
            {
                case ActOneTrainCarUnlockRules.CombatTrainCarId:
                    return CarriageType.Combat;
                case ActOneTrainCarUnlockRules.GunTrainCarId:
                    return CarriageType.Gun;
                case ActOneTrainCarUnlockRules.CargoTrainCarId:
                    return CarriageType.Cargo;
                case ActOneTrainCarUnlockRules.DefenseTrainCarId:
                    return CarriageType.Defense;
                case "car_medical":
                    return CarriageType.Medical;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trainCarId), trainCarId, "Unsupported train car ID.");
            }
        }

        private static string StableIdFor(CarriageType trainCarType)
        {
            switch (trainCarType)
            {
                case CarriageType.Combat:
                    return "car_combat";
                case CarriageType.Gun:
                    return "car_gun";
                case CarriageType.Medical:
                    return "car_medical";
                case CarriageType.Cargo:
                    return "car_cargo";
                case CarriageType.Defense:
                    return "car_defense";
                default:
                    throw new ArgumentOutOfRangeException(nameof(trainCarType), trainCarType, "Unsupported train car type.");
            }
        }
    }
}
