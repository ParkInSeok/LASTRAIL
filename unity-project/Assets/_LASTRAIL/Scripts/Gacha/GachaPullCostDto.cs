using System;

namespace LastRail.Gacha
{
    public sealed class GachaPullCostDto
    {
        public GachaPullCostDto(string itemId, int quantity)
        {
            RequireText(itemId, nameof(itemId));
            if (quantity < 0)
            {
                throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
            }

            ItemId = itemId;
            Quantity = quantity;
        }

        public string ItemId { get; }
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
