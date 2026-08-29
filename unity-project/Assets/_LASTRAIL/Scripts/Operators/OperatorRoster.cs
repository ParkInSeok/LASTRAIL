using System;
using System.Collections.Generic;

namespace LastRail.Operators
{
    public sealed class OperatorRoster
    {
        public OperatorRoster(IReadOnlyList<string> ownedOperatorIds, IReadOnlyList<string> recruitableOperatorIds)
        {
            OwnedOperatorIds = CopyAndValidate(ownedOperatorIds, nameof(ownedOperatorIds));
            RecruitableOperatorIds = CopyAndValidate(recruitableOperatorIds, nameof(recruitableOperatorIds));
            EnsureNoOverlap(OwnedOperatorIds, RecruitableOperatorIds);
        }

        public IReadOnlyList<string> OwnedOperatorIds { get; }
        public IReadOnlyList<string> RecruitableOperatorIds { get; }

        public bool IsOwned(string operatorId)
        {
            return Contains(OwnedOperatorIds, operatorId);
        }

        public bool IsRecruitable(string operatorId)
        {
            return Contains(RecruitableOperatorIds, operatorId);
        }

        private static IReadOnlyList<string> CopyAndValidate(IReadOnlyList<string> operatorIds, string name)
        {
            if (operatorIds == null)
            {
                throw new ArgumentNullException(name);
            }

            var copied = new string[operatorIds.Count];
            for (var i = 0; i < operatorIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(operatorIds[i]))
                {
                    throw new ArgumentException("Operator ID cannot be empty.", name);
                }

                copied[i] = operatorIds[i];
            }

            return copied;
        }

        private static void EnsureNoOverlap(IReadOnlyList<string> ownedOperatorIds, IReadOnlyList<string> recruitableOperatorIds)
        {
            for (var i = 0; i < ownedOperatorIds.Count; i++)
            {
                if (Contains(recruitableOperatorIds, ownedOperatorIds[i]))
                {
                    throw new InvalidOperationException("Owned operators cannot also be recruitable.");
                }
            }
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
