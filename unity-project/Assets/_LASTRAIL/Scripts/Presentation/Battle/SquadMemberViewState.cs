using System;

namespace LastRail.Presentation.Battle
{
    public sealed class SquadMemberViewState
    {
        public SquadMemberViewState(string operatorId, string displayLabel, int currentSlotIndex)
        {
            RequireText(operatorId, nameof(operatorId));
            RequireText(displayLabel, nameof(displayLabel));
            if (currentSlotIndex < 0)
            {
                throw new ArgumentException("Current slot index cannot be negative.", nameof(currentSlotIndex));
            }

            OperatorId = operatorId;
            DisplayLabel = displayLabel;
            CurrentSlotIndex = currentSlotIndex;
        }

        public string OperatorId { get; }
        public string DisplayLabel { get; }
        public int CurrentSlotIndex { get; }

        public SquadMemberViewState MoveToSlot(int targetSlotIndex)
        {
            return new SquadMemberViewState(OperatorId, DisplayLabel, targetSlotIndex);
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
