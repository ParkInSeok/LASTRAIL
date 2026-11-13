using System;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class PlayerAuthOwnerTests
    {
        private sealed class Store : IGameApiAuthTokenReader, IGameApiAuthTokenStore
        {
            public GameApiAuthTokenReadResult Read = new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Missing);
            public TaskCompletionSource<GameApiAuthTokenStoreResult> Save;
            public TaskCompletionSource<GameApiAuthTokenReadResult> ReadGate;
            public int Saves, Reads;
            public Task<GameApiAuthTokenReadResult> ReadAsync() { Reads++; return ReadGate?.Task ?? Task.FromResult(Read); }
            public Task<GameApiAuthSessionSnapshot> LoadAsync() => throw new Exception("Legacy read is forbidden");
            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot value)
            { Saves++; return Save?.Task ?? Task.FromResult(GameApiAuthTokenStoreResult.Saved); }
        }

        private static object Owner(Store store, Func<GameApiAuthSessionSnapshot, Task<GameApiAuthSessionSnapshot>> refresh = null,
            Func<GameApiAuthSessionSnapshot, Task<bool>> prepare = null)
        {
            var type = typeof(GameApiAuthSessionSnapshot).Assembly.GetType("LastRail.Auth.PlayerAuthOwner");
            Assert.That(type, Is.Not.Null, "The single runtime auth owner is missing");
            return Activator.CreateInstance(type, store, store,
                refresh ?? (session => Task.FromResult(session)), prepare ?? (session => Task.FromResult(true)));
        }
        private static Task Call(object owner, string method, params object[] args) =>
            (Task)owner.GetType().GetMethod(method).Invoke(owner, args);
        private static T Get<T>(object owner, string property) => (T)owner.GetType().GetProperty(property).GetValue(owner);
        private static void Cancel(object owner) => owner.GetType().GetMethod("Cancel").Invoke(owner, null);
        private static Func<Task<GameApiAuthSessionSnapshot>> Login(string id = "account_test") => () => Task.FromResult(SupportInquiryTests.Session(id));

        [TestCase(false)]
        [TestCase(true)]
        public async Task CancelledStorageRecheckCannotPublishGuestOrResumeEligibility(bool loaded)
        {
            var store = new Store { ReadGate = new TaskCompletionSource<GameApiAuthTokenReadResult>() };
            var owner = (PlayerAuthOwner)Owner(store);
            var pending = owner.RecheckStorageAsync();
            await owner.RecheckStorageAsync();
            Assert.That(store.Reads, Is.EqualTo(1), "Secure reads must remain singleflight");
            owner.SignOutLocally();
            var generation = owner.Generation;
            store.ReadGate.SetResult(loaded
                ? new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Loaded, SupportInquiryTests.Session())
                : new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Missing));
            await pending;
            Assert.That(owner.Generation, Is.EqualTo(generation));
            Assert.That(owner.CanCreateGuest, Is.False);
            Assert.That(owner.CanResumeStoredSession, Is.False);
            Assert.That(owner.Session, Is.Null);
            Assert.That(store.Saves, Is.Zero);
        }

        [Test]
        public async Task StorageRecheckWaitsForCancelledSecureWriteToSettle()
        {
            var store = new Store { Save = new TaskCompletionSource<GameApiAuthTokenStoreResult>() };
            int refreshes = 0;
            var owner = (PlayerAuthOwner)Owner(store, s => { refreshes++; return Task.FromResult(s); });
            var session = SupportInquiryTests.Session();
            var pending = owner.LoginAsync(() => Task.FromResult(session));
            owner.SignOutLocally();
            await owner.RecheckStorageAsync();
            Assert.That(store.Reads, Is.Zero, "A cancelled write can still be committing");
            Assert.That(owner.Busy, Is.True);
            store.Read = new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Loaded, session);
            store.Save.SetResult(GameApiAuthTokenStoreResult.Saved);
            await pending;
            var generation = owner.Generation;
            await owner.RecheckStorageAsync();
            Assert.That(owner.CanResumeStoredSession, Is.True);
            Assert.That(owner.CanCreateGuest, Is.False);
            Assert.That(owner.Session, Is.Null);
            Assert.That(owner.Generation, Is.EqualTo(generation), "Storage inspection cannot adopt an account");
            Assert.That(refreshes, Is.Zero, "Rechecking is not an automatic login");
        }

        [Test]
        public async Task CancelledResumeCannotSaveLateRefreshOrPrepareOldGeneration()
        {
            var session = SupportInquiryTests.Session();
            var store = new Store { Read = new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Loaded, session) };
            var refresh = new TaskCompletionSource<GameApiAuthSessionSnapshot>();
            int calls = 0, preparations = 0;
            var owner = (PlayerAuthOwner)Owner(store, s => { calls++; return refresh.Task; },
                s => { preparations++; return Task.FromResult(true); });
            await owner.RecheckStorageAsync();
            var pending = owner.ResumeStoredSessionAsync();
            await owner.ResumeStoredSessionAsync();
            Assert.That(calls, Is.EqualTo(1));
            owner.SignOutLocally();
            var generation = owner.Generation;
            refresh.SetResult(session);
            await pending;
            Assert.That(store.Saves, Is.Zero);
            Assert.That(preparations, Is.Zero);
            Assert.That(owner.Session, Is.Null);
            Assert.That(owner.Generation, Is.EqualTo(generation));
            Assert.That(owner.CanResumeStoredSession, Is.False);
            await owner.RecheckStorageAsync();
            await owner.ResumeStoredSessionAsync();
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(store.Saves, Is.EqualTo(1));
            Assert.That(preparations, Is.EqualTo(1));
            Assert.That(owner.Generation, Is.GreaterThan(generation));
            Assert.That(owner.Ready, Is.True);
        }

        [TestCase(GameApiAuthTokenReadOutcome.Corrupt)]
        [TestCase(GameApiAuthTokenReadOutcome.Unavailable)]
        [TestCase(GameApiAuthTokenReadOutcome.ReadError)]
        public async Task StorageRecheckRevokesPreviouslyMissingEvidenceOnFailure(GameApiAuthTokenReadOutcome outcome)
        {
            var store = new Store();
            var owner = (PlayerAuthOwner)Owner(store);
            await owner.RestoreAsync();
            Assert.That(owner.CanCreateGuest, Is.True);
            store.Read = new GameApiAuthTokenReadResult(outcome);
            await owner.RecheckStorageAsync();
            Assert.That(owner.CanCreateGuest, Is.False);
            Assert.That(owner.CanResumeStoredSession, Is.False);
            Assert.That(owner.Stage, Is.EqualTo(PlayerAuthStage.Recovery));
        }

        [Test]
        public async Task StorageRecheckCannotResetConsumedGuestAttempt()
        {
            var owner = (PlayerAuthOwner)Owner(new Store());
            await owner.RestoreAsync();
            await owner.GuestAsync(() => Task.FromResult<GameApiAuthSessionSnapshot>(null));
            owner.SignOutLocally();
            await owner.RecheckStorageAsync();
            Assert.That(owner.CanCreateGuest, Is.False, "Missing storage cannot prove a lost guest request was rejected");
        }

        [Test]
        public async Task SecureCommitAndPreparationBothPrecedeReady()
        {
            var store = new Store { Save = new TaskCompletionSource<GameApiAuthTokenStoreResult>() };
            var prepare = new TaskCompletionSource<bool>();
            var owner = Owner(store, prepare: s => prepare.Task);
            await Call(owner, "RestoreAsync");
            var operation = Call(owner, "LoginAsync", Login());
            Assert.That(Get<GameApiAuthSessionSnapshot>(owner, "Session"), Is.Null);
            Assert.That(Get<bool>(owner, "Ready"), Is.False);
            store.Save.SetResult(GameApiAuthTokenStoreResult.Saved);
            Assert.That(Get<bool>(owner, "Ready"), Is.False);
            prepare.SetResult(true); await operation;
            Assert.That(Get<bool>(owner, "Ready"), Is.True);
            Assert.That(Get<GameApiAuthSessionSnapshot>(owner, "Session").AccountId, Is.EqualTo("account_test"));
        }

        [TestCase(GameApiAuthTokenReadOutcome.Corrupt)]
        [TestCase(GameApiAuthTokenReadOutcome.Unavailable)]
        [TestCase(GameApiAuthTokenReadOutcome.ReadError)]
        public async Task ReadFailureCannotBecomeNewGuest(GameApiAuthTokenReadOutcome outcome)
        {
            var store = new Store { Read = new GameApiAuthTokenReadResult(outcome) };
            var owner = Owner(store); int calls = 0;
            await Call(owner, "RestoreAsync");
            await Call(owner, "GuestAsync", (Func<Task<GameApiAuthSessionSnapshot>>)(() => { calls++; return Login()(); }));
            Assert.That(calls, Is.Zero); Assert.That(store.Saves, Is.Zero);
            Assert.That(Get<bool>(owner, "Ready"), Is.False);
        }

        [Test]
        public async Task LostGuestResponseCannotCreateAnotherGuest()
        {
            var owner = Owner(new Store()); int calls = 0;
            Func<Task<GameApiAuthSessionSnapshot>> unknown = () => { calls++; return Task.FromResult<GameApiAuthSessionSnapshot>(null); };
            await Call(owner, "RestoreAsync");
            await Call(owner, "GuestAsync", unknown); await Call(owner, "GuestAsync", unknown);
            Assert.That(calls, Is.EqualTo(1)); Assert.That(Get<bool>(owner, "Ready"), Is.False);
        }

        [Test]
        public async Task DuplicateLoginAndCancelledLateResponseCannotAdopt()
        {
            var store = new Store(); var owner = Owner(store);
            var pending = new TaskCompletionSource<GameApiAuthSessionSnapshot>(); int calls = 0;
            Func<Task<GameApiAuthSessionSnapshot>> login = () => { calls++; return pending.Task; };
            var first = Call(owner, "LoginAsync", login); await Call(owner, "LoginAsync", login);
            Cancel(owner); pending.SetResult(SupportInquiryTests.Session()); await first;
            Assert.That(calls, Is.EqualTo(1)); Assert.That(store.Saves, Is.Zero);
            Assert.That(Get<GameApiAuthSessionSnapshot>(owner, "Session"), Is.Null);
        }

        [Test]
        public async Task CancelDuringSaveBlocksNewWriteUntilPreviousWriteSettles()
        {
            var store = new Store { Save = new TaskCompletionSource<GameApiAuthTokenStoreResult>() };
            var owner = Owner(store); var first = Call(owner, "LoginAsync", Login()); Cancel(owner);
            await Call(owner, "LoginAsync", Login("account_second"));
            Assert.That(store.Saves, Is.EqualTo(1));
            store.Save.SetResult(GameApiAuthTokenStoreResult.Saved); await first;
            Assert.That(Get<GameApiAuthSessionSnapshot>(owner, "Session"), Is.Null);
            Assert.That(Get<bool>(owner, "Ready"), Is.False);
        }

        [Test]
        public async Task FailedSaveCannotReplaceOwnerOrPublishReady()
        {
            var store = new Store(); var owner = Owner(store);
            await Call(owner, "LoginAsync", Login());
            var generation = Get<long>(owner, "Generation");
            store.Save = new TaskCompletionSource<GameApiAuthTokenStoreResult>();
            store.Save.SetResult(GameApiAuthTokenStoreResult.Failed);
            await Call(owner, "LoginAsync", Login("account_second"));
            Assert.That(Get<GameApiAuthSessionSnapshot>(owner, "Session").AccountId, Is.EqualTo("account_test"));
            Assert.That(Get<long>(owner, "Generation"), Is.EqualTo(generation));
            Assert.That(Get<bool>(owner, "Ready"), Is.False);
        }

        [Test]
        public async Task RefreshMustStayOnStoredAccountAndNeverAutoRetry()
        {
            var store = new Store { Read = new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Loaded, SupportInquiryTests.Session()) };
            int calls = 0;
            var owner = Owner(store, s => { calls++; return Login("different_account")(); });
            await Call(owner, "RestoreAsync"); await Call(owner, "RestoreAsync");
            Assert.That(calls, Is.EqualTo(1)); Assert.That(store.Saves, Is.Zero);
            Assert.That(Get<bool>(owner, "Ready"), Is.False);
        }

        [Test]
        public async Task CancelledSaveRevokesMissingEvidence()
        {
            var store = new Store { Save = new TaskCompletionSource<GameApiAuthTokenStoreResult>() };
            var owner = Owner(store); await Call(owner, "RestoreAsync");
            var login = Call(owner, "LoginAsync", Login()); Cancel(owner);
            store.Save.SetResult(GameApiAuthTokenStoreResult.Saved); await login;
            Assert.That(Get<bool>(owner, "CanCreateGuest"), Is.False);
        }

        [TestCase("Authenticating")]
        [TestCase("Saving")]
        [TestCase("Preparing")]
        public async Task ReentrantSignoutFencesFollowingDependency(string stage)
        {
            var store = new Store(); int calls = 0, preparations = 0;
            var owner = Owner(store, prepare: s => { preparations++; return Task.FromResult(true); });
            Action changed = () =>
            {
                if (Get<object>(owner, "Stage").ToString() == stage)
                    owner.GetType().GetMethod("SignOutLocally").Invoke(owner, null);
            };
            owner.GetType().GetEvent("Changed").AddEventHandler(owner, changed);
            await Call(owner, "LoginAsync", (Func<Task<GameApiAuthSessionSnapshot>>)(() => { calls++; return Login()(); }));
            if (stage == "Authenticating") Assert.That(calls, Is.Zero);
            if (stage == "Saving") Assert.That(store.Saves, Is.Zero);
            if (stage == "Preparing") Assert.That(preparations, Is.Zero);
            Assert.That(Get<bool>(owner, "Ready"), Is.False);
        }

        [Test]
        public async Task FailedPreparationRetainsSessionButCannotEnterLobby()
        {
            var owner = Owner(new Store(), prepare: s => Task.FromResult(false));
            await Call(owner, "LoginAsync", Login());
            Assert.That(Get<GameApiAuthSessionSnapshot>(owner, "Session"), Is.Not.Null);
            Assert.That(Get<bool>(owner, "Ready"), Is.False);
        }
    }
}
