using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Auth
{
    public enum AuthReplayKind { Guest, Refresh }

    public sealed class AuthReplayOperation
    {
        public AuthReplayKind Kind { get; }
        public string IdempotencyKey { get; }
        public string AccountId { get; }
        public DateTimeOffset CreatedAt { get; }
        internal AuthReplayOperation(AuthReplayKind kind, string key, string accountId, DateTimeOffset createdAt)
        { Kind = kind; IdempotencyKey = key; AccountId = accountId; CreatedAt = createdAt; }
    }

    // Main-thread, single encrypted value: token commit and removal of the replay secret cannot split.
    public sealed class KeystoreAuthReplayJournal : IGameApiAuthTokenReader, IGameApiAuthTokenStore
    {
        private readonly IAndroidKeystoreNativeStoragePort native;
        private GameApiAuthSessionSnapshot session;
        public AuthReplayOperation Pending { get; private set; }
        public KeystoreAuthReplayJournal(IAndroidKeystoreNativeStoragePort native)
        { this.native = native ?? throw new ArgumentNullException(nameof(native)); }

        public Task<GameApiAuthTokenReadResult> ReadAsync()
        {
            session = null; Pending = null;
            try
            {
                if (!native.IsAndroidRuntime || !native.IsKeystoreAvailable || native.UsedPlayerPrefsFallback)
                    return ReadResult(GameApiAuthTokenReadOutcome.Unavailable);
            }
            catch { return ReadResult(GameApiAuthTokenReadOutcome.Unavailable); }
            AndroidKeystoreEncryptedTokenPayload encrypted;
            try { encrypted = native.Load(); }
            catch (FormatException) { return ReadResult(GameApiAuthTokenReadOutcome.Corrupt); }
            catch (ArgumentException) { return ReadResult(GameApiAuthTokenReadOutcome.Corrupt); }
            catch { return ReadResult(GameApiAuthTokenReadOutcome.ReadError); }
            if (encrypted == null) return ReadResult(GameApiAuthTokenReadOutcome.Missing);
            try
            {
                var text = native.Decrypt(encrypted);
                if (text == null || text.Length > 32768) throw new FormatException();
                var root = GameApiJson.ReadObject(text);
                if (!root.ContainsKey("schema"))
                    session = AndroidKeystoreAuthTokenPayloadJsonAdapter.FromJson(text).ToSnapshot();
                else
                {
                    if (root["schema"].Type != JTokenType.Integer || (int)root["schema"] != 2
                        || !root.ContainsKey("session") || !root.ContainsKey("pending")) throw new FormatException();
                    if (root["session"].Type != JTokenType.Null)
                        session = AndroidKeystoreAuthTokenPayloadJsonAdapter.FromJson(GameApiJson.Object(root, "session").ToString(Formatting.None)).ToSnapshot();
                    if (root["pending"].Type != JTokenType.Null)
                    {
                        var pending = GameApiJson.Object(root, "pending");
                        var kind = GameApiJson.String(pending, "kind");
                        if (kind != "GUEST" && kind != "REFRESH") throw new FormatException();
                        var key = GameApiJson.String(pending, "key");
                        if (!ValidKey(key)) throw new FormatException();
                        var account = GameApiJson.String(pending, "accountId", true);
                        if ((kind == "GUEST" && (session != null || account != null))
                            || (kind == "REFRESH" && (session == null || account != session.AccountId))) throw new FormatException();
                        Pending = new AuthReplayOperation(kind == "GUEST" ? AuthReplayKind.Guest : AuthReplayKind.Refresh,
                            key, account, GameApiJson.UtcTimestamp(pending, "createdAt"));
                    }
                }
                return Task.FromResult(new GameApiAuthTokenReadResult(session == null ? GameApiAuthTokenReadOutcome.Missing : GameApiAuthTokenReadOutcome.Loaded, session));
            }
            catch { session = null; Pending = null; return ReadResult(GameApiAuthTokenReadOutcome.Corrupt); }
        }

        public async Task<GameApiAuthSessionSnapshot> LoadAsync() => (await ReadAsync()).Session;

        public async Task<AuthReplayOperation> BeginGuestAsync()
        {
            var read = await ReadAsync();
            if (read.Outcome != GameApiAuthTokenReadOutcome.Missing) return null;
            // Reads may see a cached value from a failed write. Re-encrypt and confirm persistence before dispatch.
            if (Pending != null) return Pending.Kind == AuthReplayKind.Guest && Persist(null, Pending) ? Pending : null;
            var candidate = new AuthReplayOperation(AuthReplayKind.Guest, NewKey(), null, DateTimeOffset.UtcNow);
            if (!Persist(null, candidate)) return null;
            Pending = candidate; return candidate;
        }

        public async Task<AuthReplayOperation> BeginRefreshAsync(GameApiAuthSessionSnapshot original)
        {
            if (original == null) return null;
            var read = await ReadAsync();
            if (read.Outcome != GameApiAuthTokenReadOutcome.Loaded || original.AccountId != session.AccountId
                || original.RefreshToken.Value != session.RefreshToken.Value) return null;
            if (Pending != null) return Pending.Kind == AuthReplayKind.Refresh && Persist(session, Pending) ? Pending : null;
            var candidate = new AuthReplayOperation(AuthReplayKind.Refresh, NewKey(), session.AccountId, DateTimeOffset.UtcNow);
            if (!Persist(session, candidate)) return null;
            Pending = candidate; return candidate;
        }

        public async Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot candidate)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            var read = await ReadAsync();
            if ((read.Outcome != GameApiAuthTokenReadOutcome.Missing && read.Outcome != GameApiAuthTokenReadOutcome.Loaded)
                || (session != null && session.AccountId != candidate.AccountId) || candidate.AccountStatus != "ACTIVE")
                return GameApiAuthTokenStoreResult.Failed;
            if (!Persist(candidate, null)) return GameApiAuthTokenStoreResult.Failed;
            session = candidate; Pending = null; return GameApiAuthTokenStoreResult.Saved;
        }

        private bool Persist(GameApiAuthSessionSnapshot value, AuthReplayOperation pending)
        {
            try
            {
                if (!native.IsAndroidRuntime || !native.IsKeystoreAvailable || native.UsedPlayerPrefsFallback) return false;
                var root = new JObject
                {
                    ["schema"] = 2,
                    ["session"] = value == null ? JValue.CreateNull() : (JToken)GameApiJson.ReadObject(AndroidKeystoreAuthTokenPayloadJsonAdapter.ToJson(AndroidKeystoreAuthTokenPayload.FromSnapshot(value))),
                    ["pending"] = pending == null ? JValue.CreateNull() : (JToken)new JObject
                    {
                        ["kind"] = pending.Kind == AuthReplayKind.Guest ? "GUEST" : "REFRESH", ["key"] = pending.IdempotencyKey,
                        ["accountId"] = pending.AccountId, ["createdAt"] = GameApiJson.UtcText(pending.CreatedAt)
                    }
                };
                var text = root.ToString(Formatting.None);
                return text.Length <= 32768 && native.Save(native.Encrypt(text));
            }
            catch { return false; }
        }
        private static Task<GameApiAuthTokenReadResult> ReadResult(GameApiAuthTokenReadOutcome outcome) => Task.FromResult(new GameApiAuthTokenReadResult(outcome));
        private static string NewKey()
        {
            var bytes = new byte[32]; using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        private static bool ValidKey(string key)
        {
            if (key == null || key.Length < 32 || key.Length > 128) return false;
            foreach (var c in key) if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-')) return false;
            return true;
        }
    }
}
