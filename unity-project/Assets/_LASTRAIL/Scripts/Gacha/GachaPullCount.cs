using System;

namespace LastRail.Gacha
{
    public readonly struct GachaPullCount
    {
        private GachaPullCount(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public static GachaPullCount Single => new GachaPullCount(1);
        public static GachaPullCount Ten => new GachaPullCount(10);

        public static GachaPullCount FromValue(int value)
        {
            if (value == 1)
            {
                return Single;
            }

            if (value == 10)
            {
                return Ten;
            }

            throw new ArgumentException("Gacha pull count must be 1 or 10.", nameof(value));
        }
    }
}
