using System;

namespace LastRail.Gacha
{
    public sealed class GachaDuplicateConversionDto
    {
        public GachaDuplicateConversionDto(string sourceId, string itemId, int quantity)
        {
            RequireText(sourceId, nameof(sourceId));
            RequireText(itemId, nameof(itemId));
            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));
            }

            SourceId = sourceId;
            ItemId = itemId;
            Quantity = quantity;
        }

        public string SourceId { get; }
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
