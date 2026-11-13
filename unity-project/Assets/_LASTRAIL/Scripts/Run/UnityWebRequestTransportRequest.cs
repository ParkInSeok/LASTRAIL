using System;
using System.Collections.Generic;

namespace LastRail.Run
{
    public sealed class UnityWebRequestTransportRequest
    {
        public UnityWebRequestTransportRequest(string method, string endpointPath, string bodyJson, IEnumerable<RunApiHttpHeader> headers)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                throw new ArgumentException("Method cannot be empty.", nameof(method));
            }

            if (string.IsNullOrWhiteSpace(endpointPath))
            {
                throw new ArgumentException("Endpoint path cannot be empty.", nameof(endpointPath));
            }

            Method = method.Trim();
            EndpointPath = endpointPath.Trim();
            BodyJson = string.IsNullOrWhiteSpace(bodyJson) ? null : bodyJson;
            Headers = CopyHeaders(headers);
        }

        public string Method { get; }
        public string EndpointPath { get; }
        public string BodyJson { get; }
        public IReadOnlyList<RunApiHttpHeader> Headers { get; }
        public bool HasAccountShortcut => false;

        public static UnityWebRequestTransportRequest FromRunApiRequest(RunApiHttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return new UnityWebRequestTransportRequest(request.Method, request.EndpointPath, request.BodyJson, request.Headers);
        }

        private static IReadOnlyList<RunApiHttpHeader> CopyHeaders(IEnumerable<RunApiHttpHeader> headers)
        {
            var copy = new List<RunApiHttpHeader>();
            if (headers == null)
            {
                return copy.AsReadOnly();
            }

            foreach (var header in headers)
            {
                if (header == null)
                {
                    throw new ArgumentException("Header cannot be null.", nameof(headers));
                }

                if (string.Equals(header.Name, "X-Account-Id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(header.Name, "X-Lastrail-Account-Id", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Client-supplied account headers are not allowed.", nameof(headers));
                }

                copy.Add(header);
            }

            return copy.AsReadOnly();
        }
    }
}
