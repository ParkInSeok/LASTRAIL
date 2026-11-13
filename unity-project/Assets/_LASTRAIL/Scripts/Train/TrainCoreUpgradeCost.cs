namespace LastRail.Train
{
    public readonly struct TrainCoreUpgradeCost
    {
        public TrainCoreUpgradeCost(int fromLevel, int toLevel, int credits, int machineParts, int specialCores)
        {
            FromLevel = fromLevel;
            ToLevel = toLevel;
            Credits = credits;
            MachineParts = machineParts;
            SpecialCores = specialCores;
        }

        public int FromLevel { get; }
        public int ToLevel { get; }
        public int Credits { get; }
        public int MachineParts { get; }
        public int SpecialCores { get; }
    }
}
