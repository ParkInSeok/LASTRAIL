using System;

namespace LastRail.Gacha
{
    public sealed class GachaPullRequest
    {
        public const string MethodConstant = "POST";
        public const string EndpointPathTemplate = "/v1/gacha/banners/{bannerId}/pulls";
        public const string IdempotencyHeaderNameConstant = "Idempotency-Key";

        private GachaPullRequest(string bannerId, GachaPullCount pullCount, string idempotencyKey)
        {
            RequireText(bannerId, nameof(bannerId));
            RequireText(idempotencyKey, nameof(idempotencyKey));

            BannerId = bannerId;
            PullCount = GachaPullCount.FromValue(pullCount.Value);
            IdempotencyKey = idempotencyKey;
        }

        public string Method => MethodConstant;
        public string EndpointPath => "/v1/gacha/banners/" + BannerId + "/pulls";
        public string BannerId { get; }
        public GachaPullCount PullCount { get; }
        public string IdempotencyKey { get; }
        public string IdempotencyHeaderName => IdempotencyHeaderNameConstant;
        public string IdempotencyHeaderValue => IdempotencyKey;
        public string BodyJson => "{\"pullCount\":" + PullCount.Value + "}";

        public static GachaPullRequest Create(string bannerId, GachaPullCount pullCount, string idempotencyKey)
        {
            return new GachaPullRequest(bannerId, pullCount, idempotencyKey);
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
