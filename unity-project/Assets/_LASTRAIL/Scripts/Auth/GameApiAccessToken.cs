using System;

namespace LastRail.Auth
{
    public sealed class GameApiAccessToken
    {
        public GameApiAccessToken(string value)
        {
            RequireToken(value, nameof(value));
            Value = value;
        }

        public string Value { get; }

        private static void RequireToken(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("lst1.", StringComparison.Ordinal))
            {
                throw new ArgumentException("Access token must use lst1 format.", name);
            }
        }
    }
}
