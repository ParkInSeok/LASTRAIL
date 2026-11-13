using System;
using System.Collections.Generic;

namespace LastRail.Gacha
{
    public sealed class GachaPullBatchPresentation
    {
        public GachaPullBatchPresentation(
            string batchId,
            string bannerId,
            GachaPullCount pullCount,
            IEnumerable<GachaPullResultPresentation> results)
        {
            RequireText(batchId, nameof(batchId));
            RequireText(bannerId, nameof(bannerId));

            BatchId = batchId;
            BannerId = bannerId;
            PullCount = GachaPullCount.FromValue(pullCount.Value);
            Results = CopyResults(results);
            if (Results.Count != PullCount.Value)
            {
                throw new ArgumentException("Result count must match pull count.", nameof(results));
            }
        }

        public string BatchId { get; }
        public string BannerId { get; }
        public GachaPullCount PullCount { get; }
        public IReadOnlyList<GachaPullResultPresentation> Results { get; }

        private static IReadOnlyList<GachaPullResultPresentation> CopyResults(IEnumerable<GachaPullResultPresentation> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var copy = new List<GachaPullResultPresentation>();
            foreach (var result in results)
            {
                if (result == null)
                {
                    throw new ArgumentException("Pull result cannot be null.", nameof(results));
                }

                copy.Add(result);
            }

            return copy.AsReadOnly();
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
