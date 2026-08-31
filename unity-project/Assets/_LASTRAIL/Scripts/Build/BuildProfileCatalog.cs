using System;
using LastRail.Client;

namespace LastRail.Build
{
    public static class BuildProfileCatalog
    {
        private static readonly BuildProfileDefinition[] profiles =
        {
            new BuildProfileDefinition("Android_DEV", ClientEnvironment.Dev, "LASTRAIL_ENV_DEV", true, "apk"),
            new BuildProfileDefinition("Android_PRD", ClientEnvironment.Prd, "LASTRAIL_ENV_PRD", false, "aab")
        };

        public static BuildProfileDefinition[] All => (BuildProfileDefinition[])profiles.Clone();

        public static BuildProfileDefinition ForName(string name)
        {
            for (var i = 0; i < profiles.Length; i++)
            {
                if (string.Equals(profiles[i].Name, name, StringComparison.Ordinal))
                {
                    return profiles[i];
                }
            }

            throw new InvalidOperationException($"Unknown LAST RAIL build profile: {name}");
        }
    }
}
