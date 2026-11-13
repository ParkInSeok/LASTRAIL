using System;
using System.Collections.Generic;

namespace LastRail.Train
{
    public static class ActOneTrainCarUnlockRules
    {
        public const string WaterStopOperationId = "opn_03_water_stop";
        public const string NightBoardersOperationId = "opn_05_night_boarders";

        public const string CombatTrainCarId = "car_combat";
        public const string GunTrainCarId = "car_gun";
        public const string CargoTrainCarId = "car_cargo";
        public const string DefenseTrainCarId = "car_defense";

        public static ActOneTrainCarLoadout GetDefaultUnlockedLoadout(IReadOnlyList<string> completedOperationIds)
        {
            if (completedOperationIds == null)
            {
                throw new ArgumentNullException(nameof(completedOperationIds));
            }

            var hasClearedWaterStop = false;
            var hasClearedNightBoarders = false;
            for (var i = 0; i < completedOperationIds.Count; i++)
            {
                var operationId = completedOperationIds[i];
                if (string.IsNullOrWhiteSpace(operationId))
                {
                    throw new ArgumentException("Completed operation ID cannot be empty.", nameof(completedOperationIds));
                }

                hasClearedWaterStop |= string.Equals(operationId, WaterStopOperationId, StringComparison.Ordinal);
                hasClearedNightBoarders |= string.Equals(operationId, NightBoardersOperationId, StringComparison.Ordinal);
            }

            var trainCarIds = hasClearedNightBoarders
                ? new[] { CombatTrainCarId, GunTrainCarId, CargoTrainCarId, DefenseTrainCarId }
                : hasClearedWaterStop
                    ? new[] { CombatTrainCarId, GunTrainCarId, CargoTrainCarId }
                    : new[] { CombatTrainCarId, GunTrainCarId };

            return new ActOneTrainCarLoadout(trainCarIds.Length, trainCarIds);
        }
    }
}
