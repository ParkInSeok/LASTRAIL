using System;
using System.Threading.Tasks;

namespace LastRail.Auth
{
    public enum PasswordCredentialStage
    {
        Editing, Sending, AwaitingCode, Confirming, Verified, VerificationUncertain,
        Completing, Uncertain, Completed, Cancelled
    }

    public sealed class PasswordCredentialFlow
    {
        private readonly PasswordEmailClient client;
        private readonly Func<GameApiAuthSessionSnapshot> session;
        private readonly Func<long> generation;
        private readonly Func<GameApiAuthSessionSnapshot, Task<bool>> commit;
        private readonly Func<DateTimeOffset> now;
        private readonly string originalAccount;
        private readonly long originalGeneration;
        private long epoch;
        private DateTimeOffset retryAt;
        private DateTimeOffset? expiresAt;
        private bool frozen, credentialAttempted;
        public string Mode { get; }
        public PasswordCredentialStage Stage { get; private set; }
        public bool Busy { get; private set; }
        public string LoginId { get; private set; }
        public string RecoveryEmail { get; private set; }
        public string ChallengeId { get; private set; }
        public string ErrorCode { get; private set; }
        public int RetryWaitSeconds => (int)Math.Max(0, Math.Ceiling((retryAt - now()).TotalSeconds));
        public TimeSpan? NextPresentationChangeDelay
        {
            get
            {
                if (!Active) return null;
                var timestamp = now();
                var remaining = (retryAt - timestamp).TotalSeconds;
                double? seconds = remaining > 0 ? remaining - (Math.Ceiling(remaining) - 1) : (double?)null;
                if (expiresAt.HasValue && expiresAt.Value > timestamp)
                {
                    var expiry = (expiresAt.Value - timestamp).TotalSeconds;
                    seconds = seconds.HasValue ? Math.Min(seconds.Value, expiry) : expiry;
                }
                return seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
            }
        }
        public bool ProofExpired => expiresAt.HasValue && expiresAt.Value <= now();
        private bool Active => Stage != PasswordCredentialStage.Cancelled && Stage != PasswordCredentialStage.Completed;
        private bool SameOwner => generation() == originalGeneration && session()?.AccountId == originalAccount;
        public bool CanEdit => Active && !Busy && !frozen && SameOwner;
        public bool CanStart => Active && !Busy && !credentialAttempted && RetryWaitSeconds == 0 && SameOwner;
        public bool CanConfirm => Active && !Busy && Mode != "Reset" && !ProofExpired && SameOwner
            && Stage == PasswordCredentialStage.AwaitingCode && ChallengeId != null;
        public bool CanComplete => Active && !Busy && !credentialAttempted && SameOwner && !ProofExpired
            && (Mode == "Reset" ? Stage == PasswordCredentialStage.AwaitingCode
                : Stage == PasswordCredentialStage.Verified || Stage == PasswordCredentialStage.VerificationUncertain);
        public bool CanReconcile => Active && !Busy && Mode != "Reset" && SameOwner && Stage == PasswordCredentialStage.Uncertain;
        public event Action Changed;

        public PasswordCredentialFlow(PasswordEmailClient client, string mode,
            Func<GameApiAuthSessionSnapshot> session, Func<long> generation,
            Func<GameApiAuthSessionSnapshot, Task<bool>> commit, Func<DateTimeOffset> now)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (mode != "Register" && mode != "Link" && mode != "Reset") throw new ArgumentException(nameof(mode));
            Mode = mode; this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.generation = generation ?? throw new ArgumentNullException(nameof(generation));
            this.commit = commit ?? throw new ArgumentNullException(nameof(commit));
            this.now = now ?? throw new ArgumentNullException(nameof(now));
            originalAccount = session()?.AccountId; originalGeneration = generation();
            if ((mode == "Link" && originalAccount == null) || (mode == "Register" && originalAccount != null))
                throw new InvalidOperationException("Credential mode does not match the current account.");
        }

        public async Task StartAsync(string id, string email)
        {
            if (!CanStart) return;
            if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || string.IsNullOrWhiteSpace(email) || email.Length > 254)
            { ErrorCode = "invalid_request"; Changed?.Invoke(); return; }
            if (frozen && (id != LoginId || email != RecoveryEmail)) return;
            LoginId = id; RecoveryEmail = email; frozen = true;
            var stamp = Begin(PasswordCredentialStage.Sending);
            try
            {
                if (!Current(stamp)) return;
                var result = Mode == "Reset" ? await client.StartResetAsync(id, email)
                    : await client.StartVerificationAsync(Mode == "Link" ? "PASSWORD_LINK" : "PASSWORD_REGISTER", email, LinkSession());
                if (!Current(stamp)) return;
                ErrorCode = result.ErrorCode;
                retryAt = now().AddSeconds(result.RetryAfterSeconds ?? (result.Accepted || result.Uncertain ? 60 : 0));
                if (result.Accepted)
                {
                    ChallengeId = result.ChallengeId; expiresAt = result.ExpiresAt;
                    Stage = PasswordCredentialStage.AwaitingCode;
                }
                else Stage = ChallengeId == null ? PasswordCredentialStage.Editing : PasswordCredentialStage.AwaitingCode;
            }
            finally { End(); }
        }

        public async Task ConfirmAsync(string code)
        {
            if (!CanConfirm) return;
            var stamp = Begin(PasswordCredentialStage.Confirming);
            try
            {
                if (!Current(stamp)) return;
                var result = await client.ConfirmVerificationAsync(ChallengeId, code, LinkSession());
                if (!Current(stamp)) return;
                ErrorCode = result.ErrorCode;
                Stage = result.Accepted ? PasswordCredentialStage.Verified
                    : result.Uncertain ? PasswordCredentialStage.VerificationUncertain : PasswordCredentialStage.AwaitingCode;
            }
            finally { End(); }
        }

        public async Task CompleteAsync(string password, string code)
        {
            if (!CanComplete) return;
            if (password == null || password.Length < 8 || password.Length > 128)
            { ErrorCode = "invalid_request"; Changed?.Invoke(); return; }
            var stamp = Begin(PasswordCredentialStage.Completing);
            try
            {
                if (!Current(stamp)) return;
                credentialAttempted = Mode != "Reset";
                var result = Mode == "Reset" ? await client.ConfirmResetAsync(LoginId, RecoveryEmail, code, password)
                    : Mode == "Link" ? await client.LinkAsync(LoginId, password, RecoveryEmail, ChallengeId, LinkSession())
                    : await client.RegisterAsync(LoginId, password, RecoveryEmail, ChallengeId);
                if (!Current(stamp)) return;
                ErrorCode = result.ErrorCode;
                if (Mode == "Reset")
                    Stage = result.Accepted ? PasswordCredentialStage.Completed : PasswordCredentialStage.AwaitingCode;
                else await AdoptAsync(result, stamp);
            }
            finally { End(); }
        }

        public async Task ReconcileAsync(string password)
        {
            if (!CanReconcile) return;
            var stamp = Begin(PasswordCredentialStage.Completing);
            try
            {
                if (!Current(stamp)) return;
                await AdoptAsync(await client.LoginForReconciliationAsync(LoginId, password), stamp);
            }
            finally { End(); }
        }

        private async Task AdoptAsync(EmailActionResult result, long stamp)
        {
            if (!Current(stamp)) return;
            ErrorCode = result.ErrorCode;
            Stage = PasswordCredentialStage.Uncertain;
            if (!result.Accepted || result.Session == null || (Mode == "Link" && result.Session.AccountId != originalAccount)) return;
            bool saved;
            try { saved = await commit(result.Session); }
            catch { saved = false; }
            // Successful adoption may intentionally advance the owner's account generation.
            if (epoch != stamp || Stage == PasswordCredentialStage.Cancelled) return;
            Stage = saved ? PasswordCredentialStage.Completed : PasswordCredentialStage.Uncertain;
        }

        private GameApiAuthSessionSnapshot LinkSession() => Mode == "Link" ? session() : null;
        private long Begin(PasswordCredentialStage stage)
        {
            Busy = true; ErrorCode = null; Stage = stage;
            var stamp = ++epoch; Changed?.Invoke(); return stamp;
        }
        private bool Current(long stamp)
        {
            if (epoch != stamp || !Active) return false;
            if (SameOwner) return true;
            Cancel(); return false;
        }
        private void End() { Busy = false; Changed?.Invoke(); }
        public void Cancel()
        {
            epoch++; Stage = PasswordCredentialStage.Cancelled;
            ChallengeId = null; LoginId = null; RecoveryEmail = null; ErrorCode = null;
            Changed?.Invoke();
        }
    }
}
