namespace LastRail.Train
{
    public readonly struct CarriageDurabilityDefinition
    {
        public CarriageDurabilityDefinition(
            CarriageType carriageType,
            float maxHp,
            float armorDamageReductionRatio)
        {
            CarriageType = carriageType;
            MaxHp = maxHp;
            ArmorDamageReductionRatio = armorDamageReductionRatio;
        }

        public CarriageType CarriageType { get; }
        public float MaxHp { get; }
        public float ArmorDamageReductionRatio { get; }
    }
}
