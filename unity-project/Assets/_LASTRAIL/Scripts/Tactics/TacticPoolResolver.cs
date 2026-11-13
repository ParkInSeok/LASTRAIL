using System;
using System.Collections.Generic;

namespace LastRail.Tactics
{
    public static class TacticPoolResolver
    {
        private const int MinimumCommandTacticCount = 3;
        private const int UltimateInsertIndex = 2;

        public static ResolvedTacticPool Resolve(
            IReadOnlyList<ResolvedTacticDefinition> resolvedTactics,
            bool ultimateConditionMet,
            int ultimateRollPercent)
        {
            if (resolvedTactics == null)
            {
                throw new ArgumentNullException(nameof(resolvedTactics));
            }

            var commandTacticIds = new List<string>();
            string firstUltimateTacticId = null;

            for (var i = 0; i < resolvedTactics.Count; i++)
            {
                var tactic = resolvedTactics[i];
                switch (tactic.Kind)
                {
                    case TacticKind.Command:
                        commandTacticIds.Add(tactic.TacticId);
                        break;
                    case TacticKind.Ultimate:
                        if (firstUltimateTacticId == null)
                        {
                            firstUltimateTacticId = tactic.TacticId;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(resolvedTactics), tactic.Kind, "Unsupported tactic kind.");
                }
            }

            if (commandTacticIds.Count < MinimumCommandTacticCount)
            {
                throw new InvalidOperationException("Resolved tactic pool must contain at least three command tactics.");
            }

            var shouldMixUltimate = firstUltimateTacticId != null &&
                UltimateTacticAppearanceRules.ShouldOfferUltimateTactic(ultimateConditionMet, ultimateRollPercent);

            if (shouldMixUltimate)
            {
                commandTacticIds.Insert(UltimateInsertIndex, firstUltimateTacticId);
            }

            return new ResolvedTacticPool(commandTacticIds, shouldMixUltimate);
        }
    }
}
