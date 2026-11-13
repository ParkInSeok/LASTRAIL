using System;

namespace LastRail.Run
{
    public sealed class RunApiHttpHeader
    {
        public RunApiHttpHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Header name cannot be empty.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Header value cannot be empty.", nameof(value));
            }

            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }
    }
}
