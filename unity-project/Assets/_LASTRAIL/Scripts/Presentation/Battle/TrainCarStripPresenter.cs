using System;
using System.Collections.Generic;

namespace LastRail.Presentation.Battle
{
    public static class TrainCarStripPresenter
    {
        public static IReadOnlyList<TrainCarViewState> MarkCurrent(
            IEnumerable<TrainCarViewState> trainCars,
            int currentSlotIndex)
        {
            if (trainCars == null)
            {
                throw new ArgumentNullException(nameof(trainCars));
            }

            if (currentSlotIndex < 0)
            {
                throw new ArgumentException("Current slot index cannot be negative.", nameof(currentSlotIndex));
            }

            var found = false;
            var result = new List<TrainCarViewState>();
            foreach (var trainCar in trainCars)
            {
                if (trainCar == null)
                {
                    throw new ArgumentException("Train car cannot be null.", nameof(trainCars));
                }

                var isCurrent = trainCar.SlotIndex == currentSlotIndex;
                found = found || isCurrent;
                result.Add(new TrainCarViewState(
                    trainCar.SlotIndex,
                    trainCar.TrainCarId,
                    trainCar.DisplayLabel,
                    isCurrent));
            }

            if (!found)
            {
                throw new ArgumentException("Current train car slot does not exist.", nameof(currentSlotIndex));
            }

            return result.AsReadOnly();
        }
    }
}
