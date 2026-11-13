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
        public static RunEncounterType FromServerValue(string value)
        {
            switch (value)
            {
                case null:
                case "":
                case "ORDINARY_15_WAVE":
                    return RunEncounterType.Ordinary15Wave;
                case "RAID_BOSS":
                    return RunEncounterType.RaidBoss;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported run encounter type.");
            }
        }

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
