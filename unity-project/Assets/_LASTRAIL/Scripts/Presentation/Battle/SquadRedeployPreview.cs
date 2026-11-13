using System;
using System.Collections.Generic;

namespace LastRail.Presentation.Battle
{
    public sealed class SquadRedeployPreview
    {
        private const int RequiredSquadMemberCount = 5;

        private SquadRedeployPreview(
            int currentSlotIndex,
            int targetSlotIndex,
            string destinationTrainCarId,
            IReadOnlyList<string> affectedOperatorIds,
            IReadOnlyList<SquadMemberViewState> previewMembers)
        {
            CurrentSlotIndex = currentSlotIndex;
            TargetSlotIndex = targetSlotIndex;
            DestinationTrainCarId = destinationTrainCarId;
            AffectedOperatorIds = affectedOperatorIds;
            PreviewMembers = previewMembers;
        }

        public int CurrentSlotIndex { get; }
        public int TargetSlotIndex { get; }
        public string DestinationTrainCarId { get; }
        public bool IsNoOp => CurrentSlotIndex == TargetSlotIndex;
        public IReadOnlyList<string> AffectedOperatorIds { get; }
        public IReadOnlyList<SquadMemberViewState> PreviewMembers { get; }

        public static SquadRedeployPreview Create(
            int currentSlotIndex,
            int targetSlotIndex,
            IEnumerable<TrainCarViewState> trainCars,
            IEnumerable<SquadMemberViewState> squadMembers)
        {
            if (currentSlotIndex < 0)
            {
                throw new ArgumentException("Current slot index cannot be negative.", nameof(currentSlotIndex));
            }

            if (targetSlotIndex < 0)
            {
                throw new ArgumentException("Target slot index cannot be negative.", nameof(targetSlotIndex));
            }

            var destination = FindDestination(trainCars, targetSlotIndex);
            var members = CopyMembers(squadMembers, currentSlotIndex);
            var affected = new List<string>(RequiredSquadMemberCount);
            var preview = new List<SquadMemberViewState>(RequiredSquadMemberCount);

            foreach (var member in members)
            {
                affected.Add(member.OperatorId);
                preview.Add(member.MoveToSlot(targetSlotIndex));
            }

            return new SquadRedeployPreview(
                currentSlotIndex,
                targetSlotIndex,
                destination.TrainCarId,
                affected.AsReadOnly(),
                preview.AsReadOnly());
        }

        private static TrainCarViewState FindDestination(IEnumerable<TrainCarViewState> trainCars, int targetSlotIndex)
        {
            if (trainCars == null)
            {
                throw new ArgumentNullException(nameof(trainCars));
            }

            foreach (var trainCar in trainCars)
            {
                if (trainCar == null)
                {
                    throw new ArgumentException("Train car cannot be null.", nameof(trainCars));
                }

                if (trainCar.SlotIndex == targetSlotIndex)
                {
                    return trainCar;
                }
            }

            throw new ArgumentException("Target train car slot does not exist.", nameof(targetSlotIndex));
        }

        private static IReadOnlyList<SquadMemberViewState> CopyMembers(
            IEnumerable<SquadMemberViewState> squadMembers,
            int currentSlotIndex)
        {
            if (squadMembers == null)
            {
                throw new ArgumentNullException(nameof(squadMembers));
            }

            var members = new List<SquadMemberViewState>(RequiredSquadMemberCount);
            foreach (var member in squadMembers)
            {
                if (member == null)
                {
                    throw new ArgumentException("Squad member cannot be null.", nameof(squadMembers));
                }

                if (member.CurrentSlotIndex != currentSlotIndex)
                {
                    throw new ArgumentException("All squad members must start in the current train car slot.", nameof(squadMembers));
                }

                members.Add(member);
            }

            if (members.Count != RequiredSquadMemberCount)
            {
                throw new ArgumentException("Redeploy preview requires the whole five-person squad.", nameof(squadMembers));
            }

            return members.AsReadOnly();
        }
    }
}
