using System;

namespace LastRail.Auth
{
    public sealed class GameApiRefreshToken
    {
        public GameApiRefreshToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Refresh token cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
    }
}
