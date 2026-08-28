using System;

namespace LastRail.Run
{
    public enum RunEncounterType
    {
        Ordinary15Wave,
        RaidBoss
    }

    public static class RunEncounterTypeFormatter
    {
        public static string ToServerValue(RunEncounterType encounterType)
        {
            switch (encounterType)
            {
                case RunEncounterType.Ordinary15Wave:
                    return "ORDINARY_15_WAVE";
                case RunEncounterType.RaidBoss:
                    return "RAID_BOSS";
                default:
                    throw new ArgumentOutOfRangeException(nameof(encounterType), encounterType, "Unsupported run encounter type.");
            }
        }
    }
}
