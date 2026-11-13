using System;

namespace LastRail.Operators
{
    public readonly struct BlockTarget
    {
        public BlockTarget(string enemyId, int blockWeight)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Enemy ID cannot be empty.", nameof(enemyId));
            }

            if (blockWeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockWeight), blockWeight, "Block weight must be positive.");
            }

            EnemyId = enemyId;
            BlockWeight = blockWeight;
        }

        public string EnemyId { get; }
        public int BlockWeight { get; }
    }
}
