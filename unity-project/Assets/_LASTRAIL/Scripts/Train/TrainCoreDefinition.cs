namespace LastRail.Train
{
    public readonly struct TrainCoreDefinition
    {
        public TrainCoreDefinition(
            int level,
            float durability,
            float armorBonusRatio,
            int engineOutput,
            float fireControlBonusRatio)
        {
            Level = level;
            Durability = durability;
            ArmorBonusRatio = armorBonusRatio;
            EngineOutput = engineOutput;
            FireControlBonusRatio = fireControlBonusRatio;
        }

        public int Level { get; }
        public float Durability { get; }
        public float ArmorBonusRatio { get; }
        public int EngineOutput { get; }
        public float FireControlBonusRatio { get; }
    }
}
