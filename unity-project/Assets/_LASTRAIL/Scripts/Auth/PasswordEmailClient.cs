using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Run;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Auth
{
    public sealed class EmailActionResult
    {
        public bool Accepted { get; }
        public bool Uncertain { get; }
        public string Status { get; }
        public string ErrorCode { get; }
        public string ChallengeId { get; }
        public DateTimeOffset? ExpiresAt { get; }
        public int? RetryAfterSeconds { get; }
        public GameApiAuthSessionSnapshot Session { get; }
        public EmailActionResult(bool accepted, bool uncertain = false, string status = null, string errorCode = null,
            string challengeId = null, DateTimeOffset? expiresAt = null, int? retryAfterSeconds = null, GameApiAuthSessionSnapshot session = null)
        {
            Accepted = accepted; Uncertain = uncertain; Status = status; ErrorCode = errorCode;
            ChallengeId = challengeId; ExpiresAt = expiresAt; RetryAfterSeconds = retryAfterSeconds; Session = session;
        }
    }

    public sealed class PasswordEmailClient
    {
        private readonly IRunApiHttpTransport wire;
        private readonly IGameApiAuthenticatedHttpTransport authenticated;
        private readonly GameApiPlayerEntryClient login;
        public PasswordEmailClient(IRunApiHttpTransport wire)
        {
            this.wire = wire ?? throw new ArgumentNullException(nameof(wire));
            authenticated = new GameApiAuthenticatedHttpTransport(wire); login = new GameApiPlayerEntryClient(wire);
        }
        public Task<EmailActionResult> StartVerificationAsync(string purpose, string email, GameApiAuthSessionSnapshot session)
        {
            if (!Text(email, 254) || (purpose != "PASSWORD_REGISTER" && purpose != "PASSWORD_LINK")
                || (purpose == "PASSWORD_LINK" ? session == null : session != null)) return Invalid();
            return SendAsync("/v1/auth/password/recovery-email/verify/start",
                new JObject { ["purpose"] = purpose, ["recoveryEmail"] = email }, session, "verificationStatus", "PENDING", verification: true);
        }
        public Task<EmailActionResult> ConfirmVerificationAsync(string challenge, string code, GameApiAuthSessionSnapshot session)
        {
            if (!Text(challenge, 128) || !Code(code)) return Invalid();
            return SendAsync("/v1/auth/password/recovery-email/verify/confirm",
                new JObject { ["challengeId"] = challenge, ["verificationCode"] = code }, session,
                "verificationStatus", "VERIFIED", verification: true, challenge: challenge);
        }
        public Task<EmailActionResult> RegisterAsync(string id, string password, string email, string challenge) =>
            CredentialAsync("register", id, password, email, challenge, null);
        public Task<EmailActionResult> LinkAsync(string id, string password, string email, string challenge, GameApiAuthSessionSnapshot session) =>
            session == null ? Invalid() : CredentialAsync("link", id, password, email, challenge, session);
        private Task<EmailActionResult> CredentialAsync(string mode, string id, string password, string email, string challenge, GameApiAuthSessionSnapshot session)
        {
            if (!Text(id, 128) || !Password(password) || !Text(email, 254) || !Text(challenge, 128)) return Invalid();
            return SendAsync("/v1/auth/password/" + mode,
                new JObject { ["loginId"] = id, ["password"] = password, ["recoveryEmail"] = email, ["verificationChallengeId"] = challenge },
                session, mode + "Status", mode == "register" ? "REGISTERED" : "LINKED", tokenBundle: true);
        }
        public Task<EmailActionResult> StartResetAsync(string id, string email)
        {
            if (!Text(id, 128) || !Text(email, 254)) return Invalid();
            return SendAsync("/v1/auth/password/reset/start", new JObject { ["loginId"] = id, ["recoveryEmail"] = email },
                null, "resetStatus", "EMAIL_SENT_IF_AVAILABLE");
        }
        public Task<EmailActionResult> ConfirmResetAsync(string id, string email, string code, string password)
        {
            if (!Text(id, 128) || !Text(email, 254) || !Code(code) || !Password(password)) return Invalid();
            return SendAsync("/v1/auth/password/reset/confirm", new JObject { ["loginId"] = id, ["recoveryEmail"] = email,
                ["verificationCode"] = code, ["newPassword"] = password }, null, "resetStatus", "RESET_CONFIRMED");
        }
        public async Task<EmailActionResult> LoginForReconciliationAsync(string id, string password)
        {
            var session = await login.PasswordAsync(id, password);
            return session == null ? new EmailActionResult(false, true) : new EmailActionResult(true, status: "SIGNED_IN", session: session);
        }

        private async Task<EmailActionResult> SendAsync(string path, JObject body, GameApiAuthSessionSnapshot session,
            string statusField, string expectedStatus, bool verification = false, string challenge = null, bool tokenBundle = false)
        {
            try
            {
                var request = new RunApiHttpRequest("POST", path, body.ToString(Formatting.None), null);
                var response = session == null ? await wire.SendAsync(request) : (await authenticated.SendAsync(request, session.AccessToken))?.Response;
                if (response == null || response.IsNetworkError) return new EmailActionResult(false, true);
                var json = GameApiJson.ReadObject(response.BodyJson);
                var error = GameApiJson.String(json, "errorCode", true);
                var delay = Delay(json);
                if (response.StatusCode != 200)
                    return new EmailActionResult(false, response.StatusCode >= 500, errorCode: error, retryAfterSeconds: delay);
                if (!GameApiJson.Boolean(json, "accepted") || GameApiJson.String(json, statusField) != expectedStatus || error != null)
                    return new EmailActionResult(false, true, errorCode: error, retryAfterSeconds: delay);
                if (verification)
                {
                    var id = GameApiJson.String(json, "challengeId");
                    if (!Text(id, 128) || (challenge != null && id != challenge)) return new EmailActionResult(false, true);
                    return new EmailActionResult(true, status: expectedStatus, challengeId: id,
                        expiresAt: GameApiJson.UtcTimestamp(json, "expiresAt"), retryAfterSeconds: delay);
                }
                if (tokenBundle)
                {
                    var candidate = GameApiAuthSessionSnapshot.FromGuestResponse(GameApiGuestAuthResponseJsonAdapter.FromJson(response.BodyJson));
                    if (candidate.AccountStatus != "ACTIVE" || (session != null && candidate.AccountId != session.AccountId))
                        return new EmailActionResult(false, true);
                    return new EmailActionResult(true, status: expectedStatus, session: candidate);
                }
                // Recovery never turns a code or unexpected token field into authentication.
                if (json.ContainsKey("accessToken") || json.ContainsKey("refreshToken")) return new EmailActionResult(false, true);
                return new EmailActionResult(true, status: expectedStatus, retryAfterSeconds: delay);
            }
            catch { return new EmailActionResult(false, true); }
        }
        private static int? Delay(JObject json)
        {
            var value = json["retryAfterSeconds"];
            if (value == null || value.Type == JTokenType.Null) return null;
            if (value.Type != JTokenType.Integer || (long)value < 0 || (long)value > 86400) throw new FormatException("Invalid retry delay.");
            return (int)value;
        }
        private static bool Text(string value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max;
        private static bool Password(string value) => value != null && value.Length >= 8 && value.Length <= 128;
        private static bool Code(string value) => value != null && value.Length == 8 && value.All(c => c >= '0' && c <= '9');
        private static Task<EmailActionResult> Invalid() => Task.FromResult(new EmailActionResult(false, errorCode: "client_validation_failed"));
    }
}
