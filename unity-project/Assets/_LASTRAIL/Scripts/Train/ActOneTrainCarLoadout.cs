using System;
using System.Collections.Generic;

namespace LastRail.Train
{
    public sealed class ActOneTrainCarLoadout
    {
        public ActOneTrainCarLoadout(int unlockedSlotCount, IReadOnlyList<string> trainCarIds)
        {
            if (unlockedSlotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unlockedSlotCount), unlockedSlotCount, "Unlocked slot count must be positive.");
            }

            if (trainCarIds == null)
            {
                throw new ArgumentNullException(nameof(trainCarIds));
            }

            if (trainCarIds.Count != unlockedSlotCount)
            {
                throw new InvalidOperationException("Unlocked train car slots must all be filled.");
            }

            var copied = new string[trainCarIds.Count];
            for (var i = 0; i < trainCarIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(trainCarIds[i]))
                {
                    throw new ArgumentException("Train car ID cannot be empty.", nameof(trainCarIds));
                }

                copied[i] = trainCarIds[i];
            }

            UnlockedSlotCount = unlockedSlotCount;
            TrainCarIds = copied;
        }

        public int UnlockedSlotCount { get; }
        public IReadOnlyList<string> TrainCarIds { get; }
    }
}
