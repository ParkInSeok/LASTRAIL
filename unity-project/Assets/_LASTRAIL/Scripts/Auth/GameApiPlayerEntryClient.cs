using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Narrative;
using LastRail.Run;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Auth
{
    public sealed class PlayerDataRead<T> where T : class
    {
        public T Value { get; }
        public bool RequiresAuthentication { get; }
        public string ErrorCode { get; }
        public PlayerDataRead(T value, bool requiresAuthentication = false, string errorCode = null)
        { Value = value; RequiresAuthentication = requiresAuthentication; ErrorCode = errorCode; }
    }

    public sealed class PlayerEntryProfile
    {
        public string AccountId { get; }
        public string DisplayName { get; }
        public long CommanderXp { get; }
        public PlayerEntryProfile(string accountId, string displayName, long commanderXp)
        { AccountId = accountId; DisplayName = displayName; CommanderXp = commanderXp; }
    }

    public sealed class GameApiPlayerEntryClient
    {
        private readonly IRunApiHttpTransport wire;
        private readonly IGameApiAuthenticatedHttpTransport authenticated;
        public string ReplayFailureCode { get; private set; }
        public GameApiPlayerEntryClient(IRunApiHttpTransport wire)
        {
            this.wire = wire ?? throw new ArgumentNullException(nameof(wire));
            authenticated = new GameApiAuthenticatedHttpTransport(wire);
        }

        public Task<GameApiAuthSessionSnapshot> GuestAsync() => AuthenticateAsync("/v1/auth/guest", null, null);

        public Task<GameApiAuthSessionSnapshot> GuestReplayAsync(string replayKey)
        {
            GameApiRefreshAuthRequest.ValidateReplayKey(replayKey);
            ReplayFailureCode = null;
            return AuthenticateAsync("/v1/auth/guest", null, null, replayKey);
        }

        public Task<GameApiAuthSessionSnapshot> PasswordAsync(string loginId, string password)
        {
            if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrEmpty(password))
                return Task.FromResult<GameApiAuthSessionSnapshot>(null);
            return AuthenticateAsync("/v1/auth/password/login",
                new JObject { ["loginId"] = loginId, ["password"] = password }.ToString(Formatting.None), "SIGNED_IN");
        }

        private async Task<GameApiAuthSessionSnapshot> AuthenticateAsync(string path, string body, string signInStatus, string replayKey = null)
        {
            try
            {
                var headers = replayKey == null ? null : new[] { new RunApiHttpHeader("Idempotency-Key", replayKey) };
                var response = await wire.SendAsync(new RunApiHttpRequest("POST", path, body, headers));
                if (response == null || response.IsNetworkError) return null;
                if (response.StatusCode != 200)
                {
                    if (replayKey != null)
                    {
                        var code = GameApiJson.String(GameApiJson.ReadObject(response.BodyJson), "errorCode", true);
                        if (code == "auth_replay_expired" || code == "account_not_active") ReplayFailureCode = code;
                    }
                    return null;
                }
                if (signInStatus != null && GameApiJson.String(GameApiJson.ReadObject(response.BodyJson), "signInStatus") != signInStatus) return null;
                var candidate = GameApiAuthSessionSnapshot.FromGuestResponse(GameApiGuestAuthResponseJsonAdapter.FromJson(response.BodyJson));
                return candidate.AccountStatus == "ACTIVE" ? candidate : null;
            }
            catch { return null; }
        }

        public async Task<PlayerEntryProfile> ProfileAsync(GameApiAuthSessionSnapshot session) => (await ReadProfileAsync(session)).Value;

        public Task<PlayerDataRead<PlayerEntryProfile>> ReadProfileAsync(GameApiAuthSessionSnapshot session) =>
            ReadAsync("/v1/profile", session, json =>
            {
                var id = GameApiJson.String(GameApiJson.Object(json, "account"), "id");
                if (!string.Equals(id, session.AccountId, StringComparison.Ordinal)) return null;
                var profile = GameApiJson.Object(json, "profile");
                if (!profile.ContainsKey("displayName")) return null;
                var name = GameApiJson.String(profile, "displayName", true);
                if (name != null && string.IsNullOrWhiteSpace(name)) return null;
                var xp = profile["commanderXp"];
                if (xp?.Type != JTokenType.Integer || (long)xp < 0) return null;
                return new PlayerEntryProfile(id, name, (long)xp);
            });

        public Task<PlayerDataRead<StoryProgressReadState>> ReadStoryProgressAsync(GameApiAuthSessionSnapshot session) =>
            ReadAsync("/v1/story-progress", session, json =>
            {
                var progress = GameApiJson.Object(json, "storyProgress");
                var narrative = GameApiJson.Object(json, "narrative");
                var order = progress["order"];
                if (order?.Type != JTokenType.Integer || (long)order < 0 || (long)order > int.MaxValue
                    || !progress.ContainsKey("highestClearedOperationId")) return null;
                return StoryProgressReadState.FromResponse(new StoryProgressReadResponseDto(
                    GameApiJson.String(json, "actId"), GameApiJson.String(progress, "highestStoryProgressId"),
                    GameApiJson.String(progress, "highestClearedOperationId", true), (int)order,
                    ReadIds(json, "clearedOperationIds"), ReadIds(json, "unlockedOperations"),
                    GameApiJson.String(json, "narrativeVersion"), GameApiJson.String(narrative, "contentStatus"),
                    GameApiJson.String(narrative, "localizationStatus"), GameApiJson.String(narrative, "fixtureStatus"),
                    GameApiJson.String(narrative, "missingContentStatus")));
            });

        private static string[] ReadIds(JObject json, string key)
        {
            if (!(json[key] is JArray values) || values.Count > 1024
                || values.Any(v => v.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)v) || ((string)v).Length > 128))
                throw new FormatException("Invalid operation list.");
            var ids = values.Select(v => (string)v).ToArray();
            if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length) throw new FormatException("Duplicate operation.");
            return ids;
        }

        private async Task<PlayerDataRead<T>> ReadAsync<T>(string path, GameApiAuthSessionSnapshot session, Func<JObject, T> parse) where T : class
        {
            if (session == null) return new PlayerDataRead<T>(null, true);
            try
            {
                var result = await authenticated.SendAsync(new RunApiHttpRequest("GET", path, null, null), session.AccessToken);
                var response = result?.Response;
                if (response == null || response.IsNetworkError) return new PlayerDataRead<T>(null);
                var auth = response.StatusCode == 401 || response.StatusCode == 403;
                if (response.StatusCode != 200)
                {
                    string code = null;
                    try { code = GameApiJson.String(GameApiJson.ReadObject(response.BodyJson), "errorCode", true); } catch { }
                    return new PlayerDataRead<T>(null, auth, code);
                }
                var json = GameApiJson.ReadObject(response.BodyJson);
                return new PlayerDataRead<T>(GameApiJson.Boolean(json, "accepted") ? parse(json) : null);
            }
            catch { return new PlayerDataRead<T>(null); }
        }

        public async Task<bool> SetDisplayNameAsync(GameApiAuthSessionSnapshot session, string displayName)
        {
            if (session == null || string.IsNullOrWhiteSpace(displayName)) return false;
            try
            {
                var body = new JObject { ["displayName"] = displayName }.ToString(Formatting.None);
                var result = await authenticated.SendAsync(new RunApiHttpRequest("POST", "/v1/profile/display-name", body, null), session.AccessToken);
                if (result?.Outcome != GameApiAuthenticatedHttpOutcome.Succeeded || result.Response.StatusCode != 200) return false;
                var json = GameApiJson.ReadObject(result.Response.BodyJson);
                var change = GameApiJson.String(json, "changeStatus");
                return GameApiJson.Boolean(json, "accepted") && !string.IsNullOrWhiteSpace(GameApiJson.String(json, "displayName"))
                    && (change == "UPDATED" || change == "UNCHANGED");
            }
            catch { return false; }
        }
    }
}
