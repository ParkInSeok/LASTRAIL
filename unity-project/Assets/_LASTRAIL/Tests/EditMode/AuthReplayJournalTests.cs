using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Resources;
using LastRail.Presentation.Startup;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class AuthReplayJournalTests
    {
        private sealed class Native : IAndroidKeystoreNativeStoragePort
        {
            private readonly Dictionary<string, string> encrypted = new Dictionary<string, string>();
            public AndroidKeystoreEncryptedTokenPayload Stored;
            public AndroidKeystoreEncryptedTokenPayload DiskStored;
            public bool FailSave, CacheOnFailedSave;
            public int Loads, Clears;
            public bool IsAndroidRuntime => true;
            public bool IsKeystoreAvailable => true;
            public string ProviderName => "fixture";
            public bool UsedPlayerPrefsFallback => false;
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string text)
            { var id = Guid.NewGuid().ToString("N"); encrypted[id] = text; return new AndroidKeystoreEncryptedTokenPayload(id, "fixture_iv"); }
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload value) => encrypted[value.CipherTextBase64];
            public bool Save(AndroidKeystoreEncryptedTokenPayload value)
            {
                if (FailSave) { if (CacheOnFailedSave) Stored = value; return false; }
                Stored = DiskStored = value; return true;
            }
            public AndroidKeystoreEncryptedTokenPayload Load() { Loads++; return Stored; }
            public bool Clear() { Clears++; Stored = null; return true; }
        }
        private static object Journal(Native native)
        {
            var type = typeof(PlayerAuthOwner).Assembly.GetType("LastRail.Auth.KeystoreAuthReplayJournal");
            Assert.That(type, Is.Not.Null, "Durable auth replay journal is missing");
            return Activator.CreateInstance(type, native);
        }
        private static async Task<object> Call(object journal, string method, params object[] args)
        {
            var task = (Task)journal.GetType().GetMethod(method).Invoke(journal, args); await task;
            return task.GetType().GetProperty("Result").GetValue(task);
        }
        private static object Get(object target, string field) => target.GetType().GetProperty(field).GetValue(target);

        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest, Task<RunApiHttpResponse>> Respond;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            { Requests.Add(request); return Respond != null ? Respond(request) : Task.FromResult(RunApiHttpResponse.NetworkError("Fixture response loss")); }
        }

        private sealed class RuntimeFixture : IDisposable
        {
            public readonly GameObject Root, Startup;
            public readonly object Journal;
            public readonly Wire Wire = new Wire();
            public readonly PlayerEntryRuntime Runtime;
            private readonly ResourceBootstrapController bootstrap;
            public RuntimeFixture(Native native)
            {
                Journal = AuthReplayJournalTests.Journal(native);
                Root = new GameObject("InactiveReplayFixture"); Root.SetActive(false);
                Startup = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
                bootstrap = Root.AddComponent<ResourceBootstrapController>();
                Set("startup", Startup.GetComponent<StartupScreenRootBehaviour>()); Set("access", new DownloadAccessPolicy());
                Set("entry", new AuthenticatedScreenEntry(_ => Task.CompletedTask, () => { }));
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                Runtime = new PlayerEntryRuntime(bootstrap, font, Wire, (IGameApiAuthTokenStore)Journal, (IGameApiAuthTokenReader)Journal);
            }
            private void Set(string name, object value) => typeof(ResourceBootstrapController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bootstrap, value);
            public void Restore() => ((Action)typeof(ResourceBootstrapController).GetField("SessionRestoreRequested", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap))();
            public Button Button(string name)
            {
                foreach (var button in Root.GetComponentsInChildren<Button>(true)) if (button.name == name) return button;
                throw new Exception("Missing button " + name);
            }
            public void Dispose() { UnityEngine.Object.DestroyImmediate(Root); Runtime.Dispose(); UnityEngine.Object.DestroyImmediate(Startup); }
        }

        [Test]
        public async Task SavedGuestNicknameCancelOffersSecureResumeWithoutCreatingAnotherGuest()
        {
            const string bundle = "{\"tokenType\":\"Bearer\",\"account\":{\"id\":\"account_test\",\"status\":\"ACTIVE\"},\"accessToken\":\"lst1.fixture\",\"refreshToken\":\"fixture_refresh\",\"accessTokenExpiresAt\":\"2030-01-01T00:00:00.000Z\",\"refreshTokenExpiresAt\":\"2030-02-01T00:00:00.000Z\"}";
            var native = new Native();
            var refresh = new TaskCompletionSource<RunApiHttpResponse>();
            using (var fixture = new RuntimeFixture(native))
            {
                fixture.Wire.Respond = request =>
                {
                    if (request.EndpointPath == "/v1/auth/guest") return Task.FromResult(RunApiHttpResponse.Success(200, bundle));
                    if (request.EndpointPath == "/v1/auth/refresh") return refresh.Task;
                    if (request.EndpointPath == "/v1/profile") return Task.FromResult(RunApiHttpResponse.Success(200,
                        "{\"accepted\":true,\"account\":{\"id\":\"account_test\"},\"profile\":{\"displayName\":null,\"commanderXp\":0}}"));
                    return Task.FromResult(RunApiHttpResponse.Success(500, "{}"));
                };
                fixture.Restore();
                fixture.Button("Guest").onClick.Invoke();
                Assert.That(fixture.Button("SubmitNickname").gameObject.activeSelf, Is.True);
                Assert.That(Get(fixture.Journal, "Pending"), Is.Null, "Guest tokens must already be committed before cancellation");
                var saved = native.DiskStored;
                var generation = fixture.Runtime.Owner.Generation;

                fixture.Button("Cancel").onClick.Invoke();
                for (var i = 0; i < 10; i++) await Task.Yield();
                Assert.That(fixture.Runtime.Owner.Session, Is.Null);
                Assert.That(fixture.Runtime.Owner.Generation, Is.GreaterThan(generation));
                Assert.That(native.DiskStored, Is.SameAs(saved), "Returning to login must preserve the saved guest");
                Assert.That(native.Clears, Is.Zero);
                Assert.That(fixture.Button("Guest").interactable, Is.False);
                Assert.That(fixture.Button("Register").interactable, Is.False);
                Assert.That(fixture.Button("Retry").gameObject.activeSelf, Is.True, "Saved guest needs a visible resume action after nickname cancellation");
                Assert.That(fixture.Button("Retry").interactable, Is.True);

                var loads = native.Loads;
                fixture.Button("Retry").onClick.Invoke();
                fixture.Button("Retry").onClick.Invoke();
                for (var i = 0; i < 10; i++) await Task.Yield();
                Assert.That(native.Loads, Is.GreaterThan(loads), "Resume must reread secure storage");
                var request = fixture.Wire.Requests.Single(r => r.EndpointPath == "/v1/auth/refresh");
                Assert.That(request.BodyJson, Is.EqualTo("{\"refreshToken\":\"fixture_refresh\"}"));
                var durable = new KeystoreAuthReplayJournal(native);
                await durable.ReadAsync();
                Assert.That(durable.Pending, Is.Not.Null);
                Assert.That(durable.Pending.Kind, Is.EqualTo(AuthReplayKind.Refresh));
                Assert.That(request.Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo(durable.Pending.IdempotencyKey));
                Assert.That(fixture.Runtime.Owner.Session, Is.Null, "A pending refresh cannot publish the saved session");

                refresh.SetResult(RunApiHttpResponse.Success(200, bundle.Replace("fixture_refresh", "fixture_rotated")));
                for (var i = 0; i < 10; i++) await Task.Yield();
                Assert.That(fixture.Runtime.Owner.Session?.AccountId, Is.EqualTo("account_test"));
                Assert.That(fixture.Runtime.Owner.Session?.RefreshToken.Value, Is.EqualTo("fixture_rotated"));
                Assert.That(fixture.Button("SubmitNickname").gameObject.activeSelf, Is.True);
                Assert.That(fixture.Runtime.Owner.Ready, Is.False, "Resume must still pass nickname hydration");
                Assert.That(Get(fixture.Journal, "Pending"), Is.Null);
                Assert.That(fixture.Wire.Requests.Count(r => r.EndpointPath == "/v1/auth/guest"), Is.EqualTo(1));
                Assert.That(native.Clears, Is.Zero);
            }
        }

        [Test]
        public async Task FailedPasswordCancelRechecksMissingStoreAndRestoresGuestAndRegisterEligibility()
        {
            var native = new Native();
            using (var fixture = new RuntimeFixture(native))
            {
                fixture.Wire.Respond = _ => Task.FromResult(RunApiHttpResponse.Success(401, "{\"errorCode\":\"password_login_invalid\"}"));
                fixture.Restore();
                Assert.That(fixture.Runtime.Owner.CanCreateGuest, Is.True);
                fixture.Root.GetComponentsInChildren<TMP_InputField>(true).Single(f => f.name == "LoginId").text = "unknown_id";
                fixture.Root.GetComponentsInChildren<TMP_InputField>(true).Single(f => f.name == "Password").text = "wrong_password";
                fixture.Button("Login").onClick.Invoke();
                Assert.That(fixture.Runtime.Owner.Stage, Is.EqualTo(PlayerAuthStage.Recovery));
                var loads = native.Loads;
                fixture.Button("Cancel").onClick.Invoke();
                for (var i = 0; i < 10; i++) await Task.Yield();

                Assert.That(fixture.Runtime.Owner.CanCreateGuest, Is.True, "Failed password cancellation must not consume fresh-account eligibility");
                Assert.That(native.Loads, Is.GreaterThan(loads), "Eligibility must come from a fresh secure read");
                Assert.That(fixture.Button("Guest").gameObject.activeSelf && fixture.Button("Guest").interactable, Is.True);
                Assert.That(fixture.Button("Register").gameObject.activeSelf && fixture.Button("Register").interactable, Is.True);
                Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(1));
                Assert.That(fixture.Wire.Requests[0].EndpointPath, Is.EqualTo("/v1/auth/password/login"));
                Assert.That(native.Stored, Is.Null);
                Assert.That(native.DiskStored, Is.Null);
                Assert.That(native.Clears, Is.Zero);
                Assert.That(Get(fixture.Journal, "Pending"), Is.Null);
            }
        }

        [Test]
        public async Task CachedFailedWriteMustBeRecommittedBeforeKeyCanBeUsed()
        {
            var native = new Native(); var session = SupportInquiryTests.Session();
            native.Save(native.Encrypt(AndroidKeystoreAuthTokenPayloadJsonAdapter.ToJson(AndroidKeystoreAuthTokenPayload.FromSnapshot(session))));
            native.FailSave = native.CacheOnFailedSave = true;
            Assert.That(await Call(Journal(native), "BeginRefreshAsync", session), Is.Null);
            var restartedRuntime = Journal(native);
            Assert.That(await Call(restartedRuntime, "BeginRefreshAsync", session), Is.Null, "A cached failed write is not proof of durability");
            native.FailSave = false;
            var committed = await Call(restartedRuntime, "BeginRefreshAsync", session);
            native.Stored = native.DiskStored;
            var restartedProcess = Journal(native); await ((IGameApiAuthTokenReader)restartedProcess).ReadAsync();
            Assert.That((string)Get(Get(restartedProcess, "Pending"), "IdempotencyKey"), Is.EqualTo((string)Get(committed, "IdempotencyKey")));
        }

        [Test]
        public void FailedGuestKeyCommitExposesRecoveryWithoutASecondGuestGesture()
        {
            var native = new Native { FailSave = true };
            using (var fixture = new RuntimeFixture(native))
            {
                fixture.Restore(); fixture.Button("Guest").onClick.Invoke();
                Assert.That(fixture.Wire.Requests, Is.Empty);
                Assert.That(fixture.Button("Retry").gameObject.activeSelf, Is.True);
                native.FailSave = false; fixture.Button("Retry").onClick.Invoke();
                Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(1));
                Assert.That(Get(fixture.Journal, "Pending"), Is.Not.Null);
            }
        }

        [Test]
        public async Task CancelledPendingReplayKeepsRecoveryActionOnLogin()
        {
            var native = new Native(); await Call(Journal(native), "BeginGuestAsync");
            using (var fixture = new RuntimeFixture(native))
            {
                fixture.Restore(); fixture.Button("Cancel").onClick.Invoke();
                Assert.That(fixture.Runtime.Owner.CanCreateGuest, Is.False, "A pending guest envelope is not fresh-account eligibility");
                Assert.That(fixture.Button("Retry").gameObject.activeSelf, Is.True);
                fixture.Button("Retry").onClick.Invoke();
                Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
                Assert.That(fixture.Wire.Requests[0].Headers[0].Value, Is.EqualTo(fixture.Wire.Requests[1].Headers[0].Value));
            }
        }

        [Test]
        public async Task BootstrapRestoreResumesCommittedGuestKeyInsteadOfOfferingAnotherGuest()
        {
            var native = new Native(); var first = Journal(native);
            var pending = await Call(first, "BeginGuestAsync"); var key = (string)Get(pending, "IdempotencyKey");
            var journal = Journal(native); var wire = new Wire();
            var root = new GameObject("InactiveReplayRuntime"); root.SetActive(false);
            var startup = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
            PlayerEntryRuntime runtime = null;
            try
            {
                var bootstrap = root.AddComponent<ResourceBootstrapController>();
                typeof(ResourceBootstrapController).GetField("startup", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bootstrap, startup.GetComponent<StartupScreenRootBehaviour>());
                typeof(ResourceBootstrapController).GetField("access", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bootstrap, new DownloadAccessPolicy());
                typeof(ResourceBootstrapController).GetField("entry", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bootstrap, new AuthenticatedScreenEntry(_ => Task.CompletedTask, () => { }));
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                runtime = new PlayerEntryRuntime(bootstrap, font, wire, (IGameApiAuthTokenStore)journal, (IGameApiAuthTokenReader)journal);
                var restore = (Action)typeof(ResourceBootstrapController).GetField("SessionRestoreRequested", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
                restore();
                Assert.That(wire.Requests.Count, Is.EqualTo(1));
                Assert.That(wire.Requests[0].EndpointPath, Is.EqualTo("/v1/auth/guest"));
                Assert.That(wire.Requests[0].Headers[0].Value, Is.EqualTo(key));
                Assert.That(runtime.Owner.CanCreateGuest, Is.False);
                Assert.That((string)Get(Get(journal, "Pending"), "IdempotencyKey"), Is.EqualTo(key));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); runtime?.Dispose(); UnityEngine.Object.DestroyImmediate(startup); }
        }

        [Test]
        public async Task GuestKeyIsCommittedBeforeUseAndRestoredWithoutReplacement()
        {
            var native = new Native(); var journal = Journal(native);
            var pending = await Call(journal, "BeginGuestAsync");
            Assert.That(pending, Is.Not.Null); Assert.That(native.Stored, Is.Not.Null);
            var key = (string)Get(pending, "IdempotencyKey");
            Assert.That(key.Length, Is.InRange(32, 128));
            Assert.That(native.Stored.CipherTextBase64, Does.Not.Contain(key));
            var restarted = Journal(native); await ((IGameApiAuthTokenReader)restarted).ReadAsync();
            Assert.That((string)Get(Get(restarted, "Pending"), "IdempotencyKey"), Is.EqualTo(key));
            Assert.That((string)Get(await Call(restarted, "BeginGuestAsync"), "IdempotencyKey"), Is.EqualTo(key));
        }
        [Test]
        public async Task FailedPendingCommitCannotAuthorizeDispatch()
        {
            var native = new Native { FailSave = true }; var journal = Journal(native);
            Assert.That(await Call(journal, "BeginGuestAsync"), Is.Null);
            Assert.That(Get(journal, "Pending"), Is.Null); Assert.That(native.Stored, Is.Null);
        }
        [Test]
        public async Task TokenCommitClearsPendingAtomicallyAndFailedCommitPreservesIt()
        {
            var native = new Native(); var journal = Journal(native);
            var pending = await Call(journal, "BeginGuestAsync"); var key = (string)Get(pending, "IdempotencyKey");
            native.FailSave = true;
            Assert.That(await ((IGameApiAuthTokenStore)journal).SaveAsync(SupportInquiryTests.Session()), Is.EqualTo(GameApiAuthTokenStoreResult.Failed));
            var restarted = Journal(native); await ((IGameApiAuthTokenReader)restarted).ReadAsync();
            Assert.That((string)Get(Get(restarted, "Pending"), "IdempotencyKey"), Is.EqualTo(key));
            native.FailSave = false;
            Assert.That(await ((IGameApiAuthTokenStore)restarted).SaveAsync(SupportInquiryTests.Session()), Is.EqualTo(GameApiAuthTokenStoreResult.Saved));
            var committed = Journal(native); var read = await ((IGameApiAuthTokenReader)committed).ReadAsync();
            Assert.That(read.Outcome, Is.EqualTo(GameApiAuthTokenReadOutcome.Loaded));
            Assert.That(Get(committed, "Pending"), Is.Null);
            Assert.That(await Call(committed, "BeginGuestAsync"), Is.Null);
        }
        [Test]
        public async Task LegacySessionMigratesAndRefreshKeepsOriginalOwnerAndKey()
        {
            var native = new Native(); var session = SupportInquiryTests.Session();
            native.Save(native.Encrypt(AndroidKeystoreAuthTokenPayloadJsonAdapter.ToJson(AndroidKeystoreAuthTokenPayload.FromSnapshot(session))));
            var journal = Journal(native);
            var read = await ((IGameApiAuthTokenReader)journal).ReadAsync(); Assert.That(read.Session.AccountId, Is.EqualTo(session.AccountId));
            Assert.That(await Call(journal, "BeginRefreshAsync", SupportInquiryTests.Session("wrong_account")), Is.Null);
            var pending = await Call(journal, "BeginRefreshAsync", session);
            Assert.That(pending, Is.Not.Null);
            var restarted = Journal(native); var restored = await ((IGameApiAuthTokenReader)restarted).ReadAsync();
            Assert.That(restored.Session.RefreshToken.Value, Is.EqualTo(session.RefreshToken.Value));
            Assert.That((string)Get(await Call(restarted, "BeginRefreshAsync", restored.Session), "IdempotencyKey"), Is.EqualTo((string)Get(pending, "IdempotencyKey")));
        }
        [Test]
        public async Task CorruptEnvelopeNeverBecomesNewGuestPermission()
        {
            var native = new Native(); native.Save(native.Encrypt("{\"schema\":2,\"session\":null,\"pending\":{\"kind\":\"GUEST\",\"key\":\"bad\"}}"));
            var journal = Journal(native); var read = await ((IGameApiAuthTokenReader)journal).ReadAsync();
            Assert.That(read.Outcome, Is.EqualTo(GameApiAuthTokenReadOutcome.Corrupt));
            Assert.That(await Call(journal, "BeginGuestAsync"), Is.Null);
        }
    }
}
