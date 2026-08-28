using System;
using System.Collections.Generic;
using LastRail.Run;

namespace LastRail.Auth
{
    public static class GameApiBearerAuthRequestFactory
    {
        public const string AuthorizationHeaderName = "Authorization";

        public static RunApiHttpRequest WithBearerToken(RunApiHttpRequest request, GameApiAccessToken accessToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (accessToken == null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            var headers = new List<RunApiHttpHeader>();
            foreach (var header in request.Headers)
            {
                if (IsForbiddenAccountHeader(header.Name))
                {
                    throw new ArgumentException("Client-supplied account headers are not allowed.", nameof(request));
                }

                if (!string.Equals(header.Name, AuthorizationHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    headers.Add(header);
                }
            }

            headers.Add(new RunApiHttpHeader(AuthorizationHeaderName, "Bearer " + accessToken.Value));
            return new RunApiHttpRequest(request.Method, request.EndpointPath, request.BodyJson, headers);
        }

        private static bool IsForbiddenAccountHeader(string name)
        {
            return string.Equals(name, "X-Account-Id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "X-Lastrail-Account-Id", StringComparison.OrdinalIgnoreCase);
        }
    }
}
