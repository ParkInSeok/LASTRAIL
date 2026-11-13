using System;

namespace LastRail.Run
{
    public sealed class RunStartRequest
    {
        private RunStartRequest(string operationPreviewId, string idempotencyKey)
        {
            OperationPreviewId = Required(operationPreviewId, nameof(operationPreviewId));
            if (!RunRequestKey.IsSupported(idempotencyKey, 1))
                throw new ArgumentException("Unsupported original request key; normalization is not allowed.", nameof(idempotencyKey));
            IdempotencyKey = idempotencyKey;
        }

        public string Method => "POST";
        public string EndpointPath => "/v1/runs/start";
        public string OperationPreviewId { get; }
        public string IdempotencyKey { get; }
        public string IdempotencyHeaderName => "Idempotency-Key";
        public string IdempotencyHeaderValue => IdempotencyKey;
        public string BodyJson => "{\"operationPreviewId\":\"" + Escape(OperationPreviewId) + "\"}";

        public static RunStartRequest Create(string operationPreviewId, string idempotencyKey)
        {
            return new RunStartRequest(operationPreviewId, idempotencyKey);
        }

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
