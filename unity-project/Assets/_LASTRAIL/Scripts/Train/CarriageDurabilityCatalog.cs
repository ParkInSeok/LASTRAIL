using System;

namespace LastRail.Train
{
    public static class CarriageDurabilityCatalog
    {
        public static CarriageDurabilityDefinition ForType(CarriageType carriageType)
        {
            switch (carriageType)
            {
                case CarriageType.Combat:
                    return new CarriageDurabilityDefinition(carriageType, 1100f, 0.10f);
                case CarriageType.Gun:
                    return new CarriageDurabilityDefinition(carriageType, 1000f, 0.08f);
                case CarriageType.Cargo:
                    return new CarriageDurabilityDefinition(carriageType, 900f, 0.05f);
                case CarriageType.Medical:
                    return new CarriageDurabilityDefinition(carriageType, 950f, 0.05f);
                case CarriageType.Defense:
                    return new CarriageDurabilityDefinition(carriageType, 1350f, 0.20f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(carriageType), carriageType, "Unsupported carriage type.");
            }
        }
    }
}
