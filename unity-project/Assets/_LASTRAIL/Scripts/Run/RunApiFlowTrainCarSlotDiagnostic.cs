namespace LastRail.Run
{
    public readonly struct RunApiFlowTrainCarSlotDiagnostic
    {
        public RunApiFlowTrainCarSlotDiagnostic(int slotIndex, string trainCarId)
        {
            SlotIndex = slotIndex;
            TrainCarId = trainCarId;
        }

        public int SlotIndex { get; }
        public string TrainCarId { get; }
    }
}
