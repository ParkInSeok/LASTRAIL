using System;

namespace LastRail.Auth
{
    public sealed class GoogleIdToken
    {
        public GoogleIdToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Google ID token cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
    }
}
