using System;

namespace LastRail.Presentation.Battle
{
    public sealed class BattleScreenLayoutRegion
    {
        public BattleScreenLayoutRegion(string name, int order, float heightRatio)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(name));
            }

            if (order < 0)
            {
                throw new ArgumentException("Order cannot be negative.", nameof(order));
            }

            if (heightRatio <= 0f || heightRatio >= 1f)
            {
                throw new ArgumentException("Height ratio must be between 0 and 1.", nameof(heightRatio));
            }

            Name = name;
            Order = order;
            HeightRatio = heightRatio;
        }

        public string Name { get; }
        public int Order { get; }
        public float HeightRatio { get; }
    }
}
