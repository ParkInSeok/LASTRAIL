using System;

namespace LastRail.Gacha
{
    public sealed class GachaRewardDto
    {
        public GachaRewardDto(string kind, string id, int quantity)
        {
            RequireText(kind, nameof(kind));
            RequireText(id, nameof(id));
            if (quantity < 0)
            {
                throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
            }

            Kind = kind;
            Id = id;
            Quantity = quantity;
        }

        public string Kind { get; }
        public string Id { get; }
        public int Quantity { get; }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
