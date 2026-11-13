using System;
using System.Collections.Generic;

namespace LastRail.Presentation.Battle
{
    public static class BattleScreenRootPresenter
    {
        public static BattleScreenPresentationState CreateDefault(
            IEnumerable<TrainCarViewState> trainCars,
            IEnumerable<SquadMemberViewState> squadMembers)
        {
            var copiedTrainCars = CopyTrainCars(trainCars);
            var currentSlotIndex = FindSingleCurrentSlotIndex(copiedTrainCars);

            return new BattleScreenPresentationState(
                BattleScreenLayoutModel.CreatePortraitDefault(),
                copiedTrainCars,
                squadMembers,
                currentSlotIndex);
        }

        private static IReadOnlyList<TrainCarViewState> CopyTrainCars(IEnumerable<TrainCarViewState> trainCars)
        {
            if (trainCars == null)
            {
                throw new ArgumentNullException(nameof(trainCars));
            }

            var copied = new List<TrainCarViewState>();
            foreach (var trainCar in trainCars)
            {
                if (trainCar == null)
                {
                    throw new ArgumentException("Train car cannot be null.", nameof(trainCars));
                }

                copied.Add(trainCar);
            }

            return copied.AsReadOnly();
        }

        private static int FindSingleCurrentSlotIndex(IReadOnlyList<TrainCarViewState> trainCars)
        {
            var found = false;
            var currentSlotIndex = -1;
            foreach (var trainCar in trainCars)
            {
                if (!trainCar.IsCurrent)
                {
                    continue;
                }

                if (found)
                {
                    throw new ArgumentException("Battle screen requires exactly one current train car.", nameof(trainCars));
                }

                found = true;
                currentSlotIndex = trainCar.SlotIndex;
            }

            if (!found)
            {
                throw new ArgumentException("Battle screen requires exactly one current train car.", nameof(trainCars));
            }

            return currentSlotIndex;
        }
    }
}
