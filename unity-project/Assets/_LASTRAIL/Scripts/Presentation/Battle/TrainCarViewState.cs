using System;

namespace LastRail.Presentation.Battle
{
    public sealed class TrainCarViewState
    {
        public TrainCarViewState(int slotIndex, string trainCarId, string displayLabel, bool isCurrent)
        {
            if (slotIndex < 0)
            {
                throw new ArgumentException("Slot index cannot be negative.", nameof(slotIndex));
            }

            RequireText(trainCarId, nameof(trainCarId));
            RequireText(displayLabel, nameof(displayLabel));

            SlotIndex = slotIndex;
            TrainCarId = trainCarId;
            DisplayLabel = displayLabel;
            IsCurrent = isCurrent;
        }

        public int SlotIndex { get; }
        public string TrainCarId { get; }
        public string DisplayLabel { get; }
        public bool IsCurrent { get; }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
