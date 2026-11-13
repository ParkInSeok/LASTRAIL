using System;
using System.Collections.Generic;
using System.Linq;

namespace LastRail.Run
{
    public sealed class RunApiHttpResponse
    {
        private RunApiHttpResponse(int statusCode, string bodyJson, string networkErrorMessage)
        {
            StatusCode = statusCode;
            BodyJson = bodyJson;
            NetworkErrorMessage = networkErrorMessage;
        }

        public int StatusCode { get; }
        public string BodyJson { get; }
        public string NetworkErrorMessage { get; }
        public bool IsNetworkError => !string.IsNullOrWhiteSpace(NetworkErrorMessage);
        private IReadOnlyDictionary<string,string> headers = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public string Header(string name) => headers.TryGetValue(name,out var value)?value:null;

        public static RunApiHttpResponse Success(int statusCode,string bodyJson,IEnumerable<RunApiHttpHeader> responseHeaders)
        {
            var response=Success(statusCode,bodyJson);
            response.headers=(responseHeaders??Array.Empty<RunApiHttpHeader>()).ToDictionary(h=>h.Name,h=>h.Value,StringComparer.OrdinalIgnoreCase);
            return response;
        }

        public static RunApiHttpResponse Success(int statusCode, string bodyJson)
        {
            if (statusCode <= 0)
            {
                throw new ArgumentException("Status code must be positive.", nameof(statusCode));
            }

            if (string.IsNullOrWhiteSpace(bodyJson))
            {
                throw new ArgumentException("Body JSON cannot be empty.", nameof(bodyJson));
            }

            return new RunApiHttpResponse(statusCode, bodyJson, null);
        }

        public static RunApiHttpResponse NetworkError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Network error message cannot be empty.", nameof(message));
            }

            return new RunApiHttpResponse(0, null, message);
        }
    }
}
