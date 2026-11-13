using System;

#if LASTRAIL_ENV_DEV && LASTRAIL_ENV_PRD
#error Exactly one LASTRAIL_ENV_* define must be active; LASTRAIL_ENV_DEV and LASTRAIL_ENV_PRD cannot both be set.
#endif

namespace LastRail.Client
{
    public static class ClientEnvironmentBootstrap
    {
        public const string DevClientConfigUrl = "https://dev-api.lastrail.app/v1/client-config";
        public const string PrdClientConfigUrl = "https://api.lastrail.app/v1/client-config";

        public static ClientEnvironment CurrentEnvironment
        {
            get
            {
#if LASTRAIL_ENV_DEV
                return ClientEnvironment.Dev;
#elif LASTRAIL_ENV_PRD
                return ClientEnvironment.Prd;
#else
                return ClientEnvironment.Unconfigured;
#endif
            }
        }

        public static string CurrentClientConfigUrl => UrlFor(CurrentEnvironment);

        public static string UrlFor(ClientEnvironment environment)
        {
            switch (environment)
            {
                case ClientEnvironment.Dev:
                    return DevClientConfigUrl;
                case ClientEnvironment.Prd:
                    return PrdClientConfigUrl;
                default:
                    throw new InvalidOperationException("Exactly one LASTRAIL_ENV_* define must be active before requesting client config.");
            }
        }
    }
}
