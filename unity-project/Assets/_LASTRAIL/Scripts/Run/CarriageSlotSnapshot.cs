using System;

namespace LastRail.Run
{
    public readonly struct CarriageSlotSnapshot
    {
        public CarriageSlotSnapshot(int slotIndex, string carriageId)
        {
            if (slotIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(carriageId))
            {
                throw new ArgumentException("Carriage ID cannot be empty.", nameof(carriageId));
            }

            SlotIndex = slotIndex;
            CarriageId = carriageId;
        }

        public int SlotIndex { get; }
        public string CarriageId { get; }
    }
}
