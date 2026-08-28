using System;

namespace LastRail.Auth
{
    public sealed class GameApiAccountDto
    {
        public GameApiAccountDto(string id, string status)
        {
            RequireText(id, nameof(id));
            RequireText(status, nameof(status));

            Id = id;
            Status = status;
        }

        public string Id { get; }
        public string Status { get; }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
