using System;

namespace LastRail.Train
{
    public static class TrainCoreCatalog
    {
        private static readonly TrainCoreDefinition[] entries =
        {
            new TrainCoreDefinition(1, 3000f, 0.00f, 65, 0.00f),
            new TrainCoreDefinition(2, 3120f, 0.01f, 68, 0.02f),
            new TrainCoreDefinition(3, 3240f, 0.02f, 72, 0.04f),
            new TrainCoreDefinition(4, 3360f, 0.03f, 75, 0.06f),
            new TrainCoreDefinition(5, 3480f, 0.04f, 78, 0.08f),
            new TrainCoreDefinition(6, 3600f, 0.05f, 82, 0.10f),
            new TrainCoreDefinition(7, 3720f, 0.06f, 86, 0.12f),
            new TrainCoreDefinition(8, 3840f, 0.07f, 89, 0.14f),
            new TrainCoreDefinition(9, 3960f, 0.08f, 92, 0.16f),
            new TrainCoreDefinition(10, 4080f, 0.10f, 95, 0.18f)
        };

        public static TrainCoreDefinition[] All => (TrainCoreDefinition[])entries.Clone();

        public static TrainCoreDefinition ForLevel(int level)
        {
            if (level < 1 || level > entries.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "Train core level must be between 1 and 10.");
            }

            return entries[level - 1];
        }
    }
}
