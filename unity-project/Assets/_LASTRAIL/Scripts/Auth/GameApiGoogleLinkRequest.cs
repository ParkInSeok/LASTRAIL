using System;
using System.Collections.Generic;
using LastRail.Run;

namespace LastRail.Auth
{
    public sealed class GameApiGoogleLinkRequest
    {
        public const string MethodConstant = "POST";
        public const string EndpointPathConstant = "/v1/auth/google/link";

        private GameApiGoogleLinkRequest(GoogleIdToken idToken)
        {
            IdToken = idToken ?? throw new ArgumentNullException(nameof(idToken));
            Headers = new List<RunApiHttpHeader>().AsReadOnly();
        }

        public string Method => MethodConstant;
        public string EndpointPath => EndpointPathConstant;
        public GoogleIdToken IdToken { get; }
        public string BodyJson => "{\"idToken\":\"" + Escape(IdToken.Value) + "\"}";
        public IReadOnlyList<RunApiHttpHeader> Headers { get; }

        public static GameApiGoogleLinkRequest Create(GoogleIdToken idToken)
        {
            return new GameApiGoogleLinkRequest(idToken);
        }

        public RunApiHttpRequest ToHttpRequest()
        {
            return new RunApiHttpRequest(Method, EndpointPath, BodyJson, Headers);
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
