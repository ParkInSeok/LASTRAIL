using System;
using System.Collections.Generic;

namespace LastRail.Run
{
    public sealed class RunApiHttpRequest
    {
        public RunApiHttpRequest(string method, string endpointPath, string bodyJson, IEnumerable<RunApiHttpHeader> headers)
        {
            RequireText(method, nameof(method));
            RequireText(endpointPath, nameof(endpointPath));

            Method = method;
            EndpointPath = endpointPath;
            BodyJson = string.IsNullOrWhiteSpace(bodyJson) ? null : bodyJson;
            Headers = CopyHeaders(headers);
        }

        public string Method { get; }
        public string EndpointPath { get; }
        public string BodyJson { get; }
        public IReadOnlyList<RunApiHttpHeader> Headers { get; }

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

                copy.Add(header);
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
