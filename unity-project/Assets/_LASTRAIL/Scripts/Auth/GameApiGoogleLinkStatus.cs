using System;

namespace LastRail.Auth
{
    public enum GameApiGoogleLinkStatus
    {
        Linked,
        AlreadyLinked
    }

    public static class GameApiGoogleLinkStatusParser
    {
        public static GameApiGoogleLinkStatus Parse(string value)
        {
            if (value == "LINKED")
            {
                return GameApiGoogleLinkStatus.Linked;
            }

            if (value == "ALREADY_LINKED")
            {
                return GameApiGoogleLinkStatus.AlreadyLinked;
            }

            throw new ArgumentException("Unsupported Google link status.", nameof(value));
        }

        public static string ToServerString(GameApiGoogleLinkStatus status)
        {
            if (status == GameApiGoogleLinkStatus.Linked)
            {
                return "LINKED";
            }

            if (status == GameApiGoogleLinkStatus.AlreadyLinked)
            {
                return "ALREADY_LINKED";
            }

            throw new ArgumentException("Unsupported Google link status.", nameof(status));
        }
    }
}
