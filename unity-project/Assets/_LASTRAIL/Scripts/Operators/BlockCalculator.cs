using System;
using System.Collections.Generic;

namespace LastRail.Operators
{
    public static class BlockCalculator
    {
        public const int KyleBlockCapacity = 3;
        public const int DefaultOperatorBlockCapacity = 1;
        public const string KyleOperatorId = "operator_kyle";

        public static int BlockCapacityFor(string operatorId)
        {
            if (string.IsNullOrWhiteSpace(operatorId))
            {
                throw new ArgumentException("Operator ID cannot be empty.", nameof(operatorId));
            }

            return string.Equals(operatorId, KyleOperatorId, StringComparison.Ordinal)
                ? KyleBlockCapacity
                : DefaultOperatorBlockCapacity;
        }

        public static int TotalBlockWeight(IReadOnlyList<BlockTarget> targets)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            var total = 0;
            for (var i = 0; i < targets.Count; i++)
            {
                total += targets[i].BlockWeight;
            }

            return total;
        }

        public static bool CanBlock(string operatorId, IReadOnlyList<BlockTarget> targets)
        {
            return TotalBlockWeight(targets) <= BlockCapacityFor(operatorId);
        }
    }
}
