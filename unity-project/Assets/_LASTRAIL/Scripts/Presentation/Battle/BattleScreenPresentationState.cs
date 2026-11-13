using System;
using System.Collections.Generic;

namespace LastRail.Presentation.Battle
{
    public sealed class BattleScreenPresentationState
    {
        private const int RequiredSquadMemberCount = 5;

        public BattleScreenPresentationState(
            BattleScreenLayoutModel layout,
            IEnumerable<TrainCarViewState> trainCars,
            IEnumerable<SquadMemberViewState> squadMembers,
            int currentSlotIndex,
            SquadRedeployPreview redeployPreview = null)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (currentSlotIndex < 0)
            {
                throw new ArgumentException("Current slot index cannot be negative.", nameof(currentSlotIndex));
            }

            var copiedTrainCars = CopyTrainCars(trainCars);
            var copiedSquadMembers = CopySquadMembers(squadMembers);
            if (!ContainsSlot(copiedTrainCars, currentSlotIndex))
            {
                throw new ArgumentException("Current train car slot does not exist.", nameof(currentSlotIndex));
            }

            Layout = layout;
            TrainCars = copiedTrainCars;
            SquadMembers = copiedSquadMembers;
            CurrentSlotIndex = currentSlotIndex;
            RedeployPreview = redeployPreview;
        }

        public BattleScreenLayoutModel Layout { get; }
        public IReadOnlyList<TrainCarViewState> TrainCars { get; }
        public IReadOnlyList<SquadMemberViewState> SquadMembers { get; }
        public int CurrentSlotIndex { get; }
        public SquadRedeployPreview RedeployPreview { get; }
        public bool HasRedeployPreview => RedeployPreview != null;

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

            if (copied.Count == 0)
            {
                throw new ArgumentException("At least one train car is required.", nameof(trainCars));
            }

            return copied.AsReadOnly();
        }

        private static IReadOnlyList<SquadMemberViewState> CopySquadMembers(IEnumerable<SquadMemberViewState> squadMembers)
        {
            if (squadMembers == null)
            {
                throw new ArgumentNullException(nameof(squadMembers));
            }

            var copied = new List<SquadMemberViewState>(RequiredSquadMemberCount);
            foreach (var member in squadMembers)
            {
                if (member == null)
                {
                    throw new ArgumentException("Squad member cannot be null.", nameof(squadMembers));
                }

                copied.Add(member);
            }

            if (copied.Count != RequiredSquadMemberCount)
            {
                throw new ArgumentException("Battle screen state requires the whole five-person squad.", nameof(squadMembers));
            }

            return copied.AsReadOnly();
        }

        private static bool ContainsSlot(IReadOnlyList<TrainCarViewState> trainCars, int slotIndex)
        {
            foreach (var trainCar in trainCars)
            {
                if (trainCar.SlotIndex == slotIndex)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
