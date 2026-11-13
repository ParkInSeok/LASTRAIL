using System;
using System.Collections.Generic;

namespace LastRail.Gacha
{
    public sealed class GachaPullResponseDto
    {
        public GachaPullResponseDto(
            string bannerId,
            bool accepted,
            string batchId,
            GachaPullResultStatus resultStatus,
            GachaPullCount pullCount,
            string balanceVersion,
            GachaPullCostDto cost,
            GachaPostPityStateDto postPityState,
            IEnumerable<GachaPullResultDto> results,
            DateTime createdAt,
            string errorCode,
            string message)
        {
            RequireText(bannerId, nameof(bannerId));
            RequireText(balanceVersion, nameof(balanceVersion));

            BannerId = bannerId;
            Accepted = accepted;
            BatchId = OptionalText(batchId);
            ResultStatus = resultStatus;
            PullCount = GachaPullCount.FromValue(pullCount.Value);
            BalanceVersion = balanceVersion;
            Cost = cost ?? throw new ArgumentNullException(nameof(cost));
            PostPityState = postPityState ?? throw new ArgumentNullException(nameof(postPityState));
            Results = CopyResults(results);
            if (createdAt.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("CreatedAt must be UTC.", nameof(createdAt));
            }

            CreatedAt = createdAt;
            ErrorCode = OptionalText(errorCode);
            Message = OptionalText(message);
        }

        public string BannerId { get; }
        public bool Accepted { get; }
        public string BatchId { get; }
        public GachaPullResultStatus ResultStatus { get; }
        public GachaPullCount PullCount { get; }
        public string BalanceVersion { get; }
        public GachaPullCostDto Cost { get; }
        public GachaPostPityStateDto PostPityState { get; }
        public IReadOnlyList<GachaPullResultDto> Results { get; }
        public DateTime CreatedAt { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool HasRewardAmounts => false;

        public GachaPityDisplayState ToPostPityDisplayState()
        {
            return new GachaPityDisplayState(
                BannerId,
                PostPityState.TotalPullCount,
                PostPityState.ElitePityCount,
                PostPityState.PickupElitePityCount,
                PostPityState.RareOrHigherPityCount);
        }

        public GachaPullBatchPresentation ToBatchPresentation(Func<GachaPullResultDto, string> displayNameResolver)
        {
            if (displayNameResolver == null)
            {
                throw new ArgumentNullException(nameof(displayNameResolver));
            }

            RequireText(BatchId, nameof(BatchId));
            var presentations = new List<GachaPullResultPresentation>();
            foreach (var result in Results)
            {
                presentations.Add(new GachaPullResultPresentation(
                    result.ResultId,
                    result.PullSequence,
                    result.ResultType,
                    result.ResultRarity,
                    result.Reward.Id,
                    displayNameResolver(result),
                    result.WasDuplicate,
                    result.DuplicateConversion == null ? null : result.DuplicateConversion.SourceId,
                    result.DuplicateConversion == null ? null : result.DuplicateConversion.ItemId,
                    result.DuplicateConversion == null ? 0 : result.DuplicateConversion.Quantity,
                    result.GuaranteeType));
            }

            return new GachaPullBatchPresentation(BatchId, BannerId, PullCount, presentations);
        }

        private static IReadOnlyList<GachaPullResultDto> CopyResults(IEnumerable<GachaPullResultDto> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var copy = new List<GachaPullResultDto>();
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

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
