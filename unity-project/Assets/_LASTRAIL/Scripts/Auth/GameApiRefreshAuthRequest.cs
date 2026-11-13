using System;
using System.Collections.Generic;
using LastRail.Run;

namespace LastRail.Auth
{
    public sealed class GameApiRefreshAuthRequest
    {
        public const string MethodConstant = "POST";
        public const string EndpointPathConstant = "/v1/auth/refresh";

        private GameApiRefreshAuthRequest(GameApiRefreshToken refreshToken, string replayKey = null)
        {
            RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
            var headers = new List<RunApiHttpHeader>();
            if (replayKey != null) headers.Add(new RunApiHttpHeader("Idempotency-Key", replayKey));
            Headers = headers.AsReadOnly();
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

        public static GameApiRefreshAuthRequest CreateReplay(GameApiRefreshToken refreshToken, string replayKey)
        {
            ValidateReplayKey(replayKey);
            return new GameApiRefreshAuthRequest(refreshToken, replayKey);
        }

        internal static void ValidateReplayKey(string key)
        {
            if (key == null || key.Length < 16 || key.Length > 128) throw new ArgumentException("Invalid replay key.");
            foreach (var c in key) if (c <= 32 || c >= 127) throw new ArgumentException("Invalid replay key.");
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
