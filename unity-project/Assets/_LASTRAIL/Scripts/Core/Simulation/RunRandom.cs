using System;

namespace LastRail.Core.Simulation
{
    public struct RunRandom
    {
        private uint state;

        public RunRandom(uint seed)
        {
            state = seed == 0u ? 0x6D2B79F5u : seed;
        }

        public uint ExportState()
        {
            if (state == 0u) throw new InvalidOperationException("Uninitialized random state cannot be checkpointed.");
            return state;
        }

        public static RunRandom FromState(uint checkpointState)
        {
            if (checkpointState == 0u) throw new ArgumentOutOfRangeException(nameof(checkpointState));
            return new RunRandom(checkpointState);
        }

        public uint NextUInt()
        {
            var x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Maximum must be greater than minimum.");
            }

            var span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % span);
        }
    }
}
