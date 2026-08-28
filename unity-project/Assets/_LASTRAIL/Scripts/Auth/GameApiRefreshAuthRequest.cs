using System;
using System.Collections.Generic;
using LastRail.Run;

namespace LastRail.Auth
{
    public sealed class GameApiRefreshAuthRequest
    {
        public const string MethodConstant = "POST";
        public const string EndpointPathConstant = "/v1/auth/refresh";

        private GameApiRefreshAuthRequest(GameApiRefreshToken refreshToken)
        {
            RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
            Headers = new List<RunApiHttpHeader>().AsReadOnly();
        }

        public string Method => MethodConstant;
        public string EndpointPath => EndpointPathConstant;
        public GameApiRefreshToken RefreshToken { get; }
        public string BodyJson => "{\"refreshToken\":\"" + Escape(RefreshToken.Value) + "\"}";
        public IReadOnlyList<RunApiHttpHeader> Headers { get; }

        public static GameApiRefreshAuthRequest Create(GameApiRefreshToken refreshToken)
        {
            return new GameApiRefreshAuthRequest(refreshToken);
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
