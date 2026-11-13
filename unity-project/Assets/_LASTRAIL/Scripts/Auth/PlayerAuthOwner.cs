using System;
using System.Threading.Tasks;

namespace LastRail.Auth
{
    public enum PlayerAuthStage { Login, Reading, Authenticating, Saving, Preparing, Ready, Recovery }

    // Main-thread owner. Network cancellation fences publication; secure writes stay serialized until settled.
    public sealed class PlayerAuthOwner : IDisposable
    {
        private readonly IGameApiAuthTokenReader reader;
        private readonly IGameApiAuthTokenStore store;
        private readonly Func<GameApiAuthSessionSnapshot, Task<GameApiAuthSessionSnapshot>> refresh;
        private readonly Func<GameApiAuthSessionSnapshot, Task<bool>> prepare;
        private bool busy, disposed, restoreAttempted, guestAttempted, missing, storedSession;
        private long operationGeneration;
        public GameApiAuthSessionSnapshot Session { get; private set; }
        public long Generation { get; private set; }
        public PlayerAuthStage Stage { get; private set; } = PlayerAuthStage.Login;
        public bool Ready => Stage == PlayerAuthStage.Ready;
        public bool Busy => busy;
        public bool CanCreateGuest => !disposed && !busy && missing && !guestAttempted && Session == null;
        public bool CanResumeStoredSession => !disposed && !busy && storedSession && Session == null;
        public event Action Changed;

        public PlayerAuthOwner(IGameApiAuthTokenReader reader, IGameApiAuthTokenStore store,
            Func<GameApiAuthSessionSnapshot, Task<GameApiAuthSessionSnapshot>> refresh,
            Func<GameApiAuthSessionSnapshot, Task<bool>> prepare)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
            this.prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
        }

        public Task RestoreAsync()
        {
            if (disposed || busy || restoreAttempted || Session != null) return Task.CompletedTask;
            restoreAttempted = true;
            return ReadStoredAsync(true);
        }

        public Task RecheckStorageAsync() => disposed || busy || Session != null
            ? Task.CompletedTask : ReadStoredAsync(false);

        public Task ResumeStoredSessionAsync() => CanResumeStoredSession
            ? ReadStoredAsync(true) : Task.CompletedTask;

        private async Task ReadStoredAsync(bool refreshStored)
        {
            busy = true; missing = false; storedSession = false;
            var operation = ++operationGeneration;
            try
            {
                Set(PlayerAuthStage.Reading);
                if (!Current(operation)) return;
                var read = await reader.ReadAsync();
                if (!Current(operation)) return;
                // A pending guest envelope also reads as Missing, but is not permission to create an account.
                missing = read?.Outcome == GameApiAuthTokenReadOutcome.Missing
                    && !(reader is KeystoreAuthReplayJournal journal && journal.Pending != null);
                if (read?.Outcome == GameApiAuthTokenReadOutcome.Missing) { Set(PlayerAuthStage.Login); return; }
                if (read?.Outcome != GameApiAuthTokenReadOutcome.Loaded || read.Session == null) { Set(PlayerAuthStage.Recovery); return; }
                storedSession = true;
                if (!refreshStored) { Set(PlayerAuthStage.Login); return; }
                Set(PlayerAuthStage.Authenticating);
                if (!Current(operation)) return;
                var candidate = await refresh(read.Session);
                if (!Current(operation)) return;
                if (candidate == null || !string.Equals(candidate.AccountId, read.Session.AccountId, StringComparison.Ordinal))
                { Set(PlayerAuthStage.Recovery); return; }
                await CommitAsync(candidate, operation);
            }
            catch { if (Current(operation)) Set(PlayerAuthStage.Recovery); }
            finally { busy = false; if (!disposed) Changed?.Invoke(); }
        }

        public Task GuestAsync(Func<Task<GameApiAuthSessionSnapshot>> authenticate)
        {
            if (!CanCreateGuest) return Task.CompletedTask;
            // Until the server confirms replay/reconciliation, even a lost or cancelled response consumes this attempt.
            guestAttempted = true;
            return LoginAsync(authenticate);
        }

        public async Task LoginAsync(Func<Task<GameApiAuthSessionSnapshot>> authenticate)
        {
            if (authenticate == null) throw new ArgumentNullException(nameof(authenticate));
            if (disposed || busy) return;
            busy = true; var operation = ++operationGeneration;
            try
            {
                Set(PlayerAuthStage.Authenticating);
                if (!Current(operation)) return;
                var candidate = await authenticate();
                if (!Current(operation)) return;
                if (candidate == null) { Set(PlayerAuthStage.Recovery); return; }
                await CommitAsync(candidate, operation);
            }
            catch { if (Current(operation)) Set(PlayerAuthStage.Recovery); }
            finally { busy = false; if (!disposed) Changed?.Invoke(); }
        }

        private async Task CommitAsync(GameApiAuthSessionSnapshot candidate, long operation)
        {
            Set(PlayerAuthStage.Saving);
            if (!Current(operation)) return;
            missing = false; storedSession = false;
            var result = await store.SaveAsync(candidate);
            if (!Current(operation)) return;
            if (result != GameApiAuthTokenStoreResult.Saved) { Set(PlayerAuthStage.Recovery); return; }
            if (!string.Equals(Session?.AccountId, candidate.AccountId, StringComparison.Ordinal)) Generation++;
            Session = candidate; missing = false;
            Set(PlayerAuthStage.Preparing);
            if (!Current(operation)) return;
            var prepared = await prepare(candidate);
            if (Current(operation)) Set(prepared ? PlayerAuthStage.Ready : PlayerAuthStage.Recovery);
        }

        public async Task PrepareAgainAsync()
        {
            if (disposed || busy || Session == null || Ready) return;
            busy = true; var operation = ++operationGeneration;
            try
            {
                Set(PlayerAuthStage.Preparing);
                if (!Current(operation)) return;
                var prepared = await prepare(Session);
                if (Current(operation)) Set(prepared ? PlayerAuthStage.Ready : PlayerAuthStage.Recovery);
            }
            catch { if (Current(operation)) Set(PlayerAuthStage.Recovery); }
            finally { busy = false; if (!disposed) Changed?.Invoke(); }
        }

        public void Cancel()
        {
            if (disposed) return;
            operationGeneration++; missing = false; storedSession = false;
            Set(busy ? PlayerAuthStage.Recovery : PlayerAuthStage.Login);
        }

        public void SignOutLocally()
        {
            if (disposed) return;
            operationGeneration++; Generation++; Session = null; missing = false; storedSession = false;
            Set(PlayerAuthStage.Login);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; operationGeneration++; Generation++; Session = null; Changed = null;
            Stage = PlayerAuthStage.Login;
        }

        private bool Current(long operation) => !disposed && operation == operationGeneration;
        private void Set(PlayerAuthStage stage) { Stage = stage; Changed?.Invoke(); }
    }
}
