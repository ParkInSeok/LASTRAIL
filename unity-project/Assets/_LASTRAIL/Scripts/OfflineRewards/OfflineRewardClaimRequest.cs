using System;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardClaimRequest
    {
        private OfflineRewardClaimRequest(string idempotencyKey)
        {
            IdempotencyHeaderValue = Required(idempotencyKey, nameof(idempotencyKey));
        }

        public string Method => "POST";
        public string EndpointPath => "/v1/offline-rewards/claim";
        public string BodyJson => null;
        public bool HasQueryParameters => false;
        public string IdempotencyHeaderName => "Idempotency-Key";
        public string IdempotencyHeaderValue { get; }
        public bool HasAccountShortcut => false;

        public static OfflineRewardClaimRequest Create(string idempotencyKey)
        {
            return new OfflineRewardClaimRequest(idempotencyKey);
        }

        private static string Required(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
