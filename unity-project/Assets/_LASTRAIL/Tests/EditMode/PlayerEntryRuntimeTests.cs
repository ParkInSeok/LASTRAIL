using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Resources;
using LastRail.Run;
using LastRail.Presentation.Startup;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class PlayerEntryRuntimeTests
    {
        private sealed class Store : IGameApiAuthTokenStore, IGameApiAuthTokenReader
        {
            public GameApiAuthTokenReadResult Read = new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Missing);
            public TaskCompletionSource<GameApiAuthTokenReadResult> Deferred;
            public int Saves;
            public Task<GameApiAuthTokenReadResult> ReadAsync() => Deferred?.Task ?? Task.FromResult(Read);
            public Task<GameApiAuthSessionSnapshot> LoadAsync() => throw new Exception("Legacy read forbidden");
            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot session)
            { Saves++; Read = new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Loaded, session); return Task.FromResult(GameApiAuthTokenStoreResult.Saved); }
        }
        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public bool Named;
            public string ProfileAccount = "account_test";
            public bool EmailEnabled;
            public bool FailProfile;
            public TaskCompletionSource<RunApiHttpResponse> DeferredProfile;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Requests.Add(request);
                if (request.EndpointPath == "/v1/profile" && DeferredProfile != null) return DeferredProfile.Task;
                if (request.EndpointPath == "/v1/profile" && FailProfile)
                    return Task.FromResult(RunApiHttpResponse.Success(500, "{\"errorCode\":\"synthetic_profile_failure\"}"));
                if (EmailEnabled && request.EndpointPath.Contains("/recovery-email/verify/"))
                    return Task.FromResult(RunApiHttpResponse.Success(200, "{\"accepted\":true,\"verificationStatus\":\"" + (request.EndpointPath.EndsWith("/start") ? "PENDING" : "VERIFIED") + "\",\"challengeId\":\"fixture_challenge\",\"expiresAt\":\"2030-01-01T00:00:00.000Z\",\"retryAfterSeconds\":60}"));
                if (EmailEnabled && request.EndpointPath == "/v1/auth/password/register")
                    return Task.FromResult(RunApiHttpResponse.Success(200, "{\"accepted\":true,\"registerStatus\":\"REGISTERED\",\"tokenType\":\"Bearer\",\"account\":{\"id\":\"account_test\",\"status\":\"ACTIVE\"},\"accessToken\":\"lst1.fixture\",\"refreshToken\":\"fixture\",\"accessTokenExpiresAt\":\"2030-01-01T00:00:00.000Z\",\"refreshTokenExpiresAt\":\"2030-02-01T00:00:00.000Z\"}"));
                if (EmailEnabled && request.EndpointPath.Contains("/password/reset/"))
                    return Task.FromResult(RunApiHttpResponse.Success(200, "{\"accepted\":true,\"resetStatus\":\"" + (request.EndpointPath.EndsWith("/start") ? "EMAIL_SENT_IF_AVAILABLE" : "RESET_CONFIRMED") + "\"}"));
                if (EmailEnabled && request.EndpointPath == "/v1/story-progress")
                    return Task.FromResult(RunApiHttpResponse.Success(200, "{\"accepted\":true,\"actId\":\"act_01_wasteland_railway\",\"storyProgress\":{\"highestStoryProgressId\":\"none\",\"highestClearedOperationId\":null,\"order\":0},\"clearedOperationIds\":[],\"unlockedOperations\":[\"opn_01_last_departure\"],\"narrativeVersion\":\"fixture_narrative\",\"narrative\":{\"contentStatus\":\"CANDIDATE\",\"localizationStatus\":\"PENDING\",\"fixtureStatus\":\"PARTIAL\",\"missingContentStatus\":\"UNKNOWN\"}}"));
                if (request.EndpointPath == "/v1/auth/guest") return Task.FromResult(RunApiHttpResponse.Success(200,
                    "{\"tokenType\":\"Bearer\",\"account\":{\"id\":\"account_test\",\"status\":\"ACTIVE\"},\"accessToken\":\"lst1.fixture\",\"refreshToken\":\"fixture\",\"accessTokenExpiresAt\":\"2030-01-01T00:00:00.000Z\",\"refreshTokenExpiresAt\":\"2030-02-01T00:00:00.000Z\"}"));
                if (request.EndpointPath == "/v1/profile/display-name")
                { Named = true; return Task.FromResult(RunApiHttpResponse.NetworkError("Fixture response loss")); }
                if (request.EndpointPath == "/v1/profile") return Task.FromResult(RunApiHttpResponse.Success(200,
                    "{\"accepted\":true,\"account\":{\"id\":\"" + ProfileAccount + "\"},\"profile\":{\"displayName\":" + (Named ? "\"fixture_name\"" : "null") + ",\"commanderXp\":0}}"));
                return Task.FromResult(RunApiHttpResponse.Success(401, "{}"));
            }
        }
        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root, Startup;
            public readonly Store Store = new Store();
            public readonly Wire Wire = new Wire();
            public readonly PlayerEntryRuntime Runtime;
            public Fixture(IRunOperationJournal journal = null)
            {
                Root = new GameObject("InactiveRuntimeFixture"); Root.SetActive(false);
                var bootstrap = Root.AddComponent<ResourceBootstrapController>();
                Startup = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
                Set(bootstrap, "startup", Startup.GetComponent<StartupScreenRootBehaviour>());
                Set(bootstrap, "access", new DownloadAccessPolicy());
                Set(bootstrap, "entry", new AuthenticatedScreenEntry(_ => Task.CompletedTask, () => { }));
                Runtime = new PlayerEntryRuntime(bootstrap, Font(), Wire, Store, Store, LastRail.Diagnostics.AppBuildSnapshot.CaptureInstalled, null, journal);
            }
            public void Dispose()
            { UnityEngine.Object.DestroyImmediate(Root); Runtime.Dispose(); UnityEngine.Object.DestroyImmediate(Startup); }
        }
        private static TMP_FontAsset Font() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class EmptyRunJournal : IRunOperationJournal
        {
            public int Reads;
            public TaskCompletionSource<IReadOnlyList<PendingRunOperation>> Deferred;
            public Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account)
            { Reads++; return Deferred?.Task ?? Task.FromResult<IReadOnlyList<PendingRunOperation>>(Array.Empty<PendingRunOperation>()); }
            public Task<bool> PutAsync(PendingRunOperation operation) => throw new InvalidOperationException("No write expected.");
            public Task<bool> ConfirmAsync(PendingRunOperation operation, string responseJson) => throw new InvalidOperationException("No confirmation expected.");
            public Task<bool> RemoveConfirmedAsync(string account, PendingRunKind kind, string key) => throw new InvalidOperationException("No removal expected.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task SignOutReleasesPriorReconciliationOwnersWithoutReReadingJournal(bool deferred)
        {
            var journal = new EmptyRunJournal();
            if (deferred) journal.Deferred = new TaskCompletionSource<IReadOnlyList<PendingRunOperation>>();
            using (var fixture = new Fixture(journal))
            {
                fixture.Wire.Named = true;
                fixture.Runtime.LobbyPreparation = _ => Task.FromResult(true);
                await fixture.Runtime.Owner.RestoreAsync();
                Find<Button>(fixture.Root, "Guest").onClick.Invoke();
                await fixture.Runtime.ResumePreparedContentAsync();
                var owner = fixture.Runtime.Owner.Session;
                Assert.That(owner, Is.Not.Null);
                object Cached(string field) => typeof(PlayerEntryRuntime).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fixture.Runtime);
                Assert.That(Cached("reconciledErrorSession"), Is.SameAs(owner));
                Assert.That(Cached("restoredRunSession"), Is.SameAs(owner));
                if (!deferred) Assert.That(Cached("loadedRunSession"), Is.SameAs(owner));
                var reads = journal.Reads;
                fixture.Runtime.Owner.SignOutLocally();
                foreach (var field in new[] { "reconciledErrorSession", "restoredRunSession", "loadedRunSession" })
                    Assert.That(Cached(field), Is.Null, field + " retained the previous session.");
                journal.Deferred?.SetResult(Array.Empty<PendingRunOperation>());
                await Task.Yield();
                Assert.That(Cached("loadedRunSession"), Is.Null, "Late restore must not republish a signed-out owner.");
                Assert.That(journal.Reads, Is.EqualTo(reads));
            }
        }

        [Test]
        public async Task CurrentProfileFailureProducesOneSafeAuthenticatedErrorRequest()
        {
            using (var fixture = new Fixture())
            {
                fixture.Wire.FailProfile = true;
                await fixture.Runtime.Owner.RestoreAsync();
                Find<Button>(fixture.Root, "Guest").onClick.Invoke();
                var errors = fixture.Wire.Requests.Where(r => r.EndpointPath == "/v1/client-errors").ToArray();
                Assert.That(errors.Length, Is.EqualTo(1), "Actual failed profile preparation must reach the safe error runtime");
                Assert.That(errors[0].BodyJson, Does.Contain("AUTHENTICATED_CURRENT_SESSION"));
                Assert.That(errors[0].BodyJson, Does.Not.Contain("synthetic_profile_failure"));
                Assert.That(errors[0].BodyJson, Does.Not.Contain("fixture_refresh"));
            }
        }

        [Test]
        public async Task ProfileFailureAfterAccountCancellationCannotBeReattributed()
        {
            using (var fixture = new Fixture())
            {
                fixture.Wire.DeferredProfile = new TaskCompletionSource<RunApiHttpResponse>();
                await fixture.Runtime.Owner.RestoreAsync();
                Find<Button>(fixture.Root, "Guest").onClick.Invoke();
                Find<Button>(fixture.Root, "Cancel").onClick.Invoke();
                fixture.Wire.DeferredProfile.SetResult(RunApiHttpResponse.Success(500, "{}"));
                for (var i = 0; i < 5; i++) await Task.Yield();
                Assert.That(fixture.Wire.Requests.Count(r => r.EndpointPath == "/v1/client-errors"), Is.Zero);
            }
        }

        [Test]
        public async Task CancelDuringCredentialAdoptionReadMustNotSaveOrAdopt()
        {
            using (var fixture = new Fixture())
            {
                fixture.Wire.EmailEnabled = true;
                await fixture.Runtime.Owner.RestoreAsync();
                Find<Button>(fixture.Root, "ChooseIdLogin").onClick.Invoke();
                Find<Button>(fixture.Root, "Register").onClick.Invoke();
                var modal = fixture.Root.GetComponentInChildren<PasswordCredentialPanel>(true).gameObject;
                Find<TMP_InputField>(modal, "LoginId").text = "fixture_id";
                Find<TMP_InputField>(modal, "RecoveryEmail").text = "synthetic@example.invalid";
                Find<TMP_InputField>(modal, "Password").text = "fixture_password";
                Find<TMP_InputField>(modal, "ConfirmPassword").text = "fixture_password";
                foreach(var toggle in modal.GetComponentsInChildren<Toggle>(true)) toggle.isOn=true;
                Find<Button>(modal, "SendCode").onClick.Invoke();
                Find<TMP_InputField>(modal, "VerificationCode").text = "00123456";
                Find<Button>(modal, "ConfirmCode").onClick.Invoke();
                fixture.Store.Deferred = new TaskCompletionSource<GameApiAuthTokenReadResult>();
                Find<TMP_InputField>(modal, "Password").text = "fixture_password";
                Find<TMP_InputField>(modal, "ConfirmPassword").text = "fixture_password";
                foreach(var toggle in modal.GetComponentsInChildren<Toggle>(true)) toggle.isOn=true;
                Find<Button>(modal, "CompleteCredential").onClick.Invoke();
                Find<Button>(modal, "CancelCredential").onClick.Invoke();
                fixture.Store.Deferred.SetResult(new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Missing));
                for (var i = 0; i < 5; i++) await Task.Yield();
                Assert.That(fixture.Store.Saves, Is.Zero);
                Assert.That(fixture.Runtime.Owner.Session, Is.Null);
            }
        }

        [Test]
        public async Task RegisterPublicActionsSaveThenExposeNicknameAndPrepareLobbyData()
        {
            using (var fixture = new Fixture())
            {
                fixture.Wire.EmailEnabled = true;
                await fixture.Runtime.Owner.RestoreAsync();
                Find<Button>(fixture.Root, "ChooseIdLogin").onClick.Invoke();
                Find<Button>(fixture.Root, "Register").onClick.Invoke();
                var modal = fixture.Root.GetComponentInChildren<PasswordCredentialPanel>(true).gameObject;
                Find<TMP_InputField>(modal, "LoginId").text = "fixture_id";
                Find<TMP_InputField>(modal, "RecoveryEmail").text = "synthetic@example.invalid";
                Find<TMP_InputField>(modal, "Password").text = "fixture_password";
                Find<TMP_InputField>(modal, "ConfirmPassword").text = "fixture_password";
                foreach(var toggle in modal.GetComponentsInChildren<Toggle>(true)) toggle.isOn=true;
                Find<Button>(modal, "SendCode").onClick.Invoke();
                Find<TMP_InputField>(modal, "VerificationCode").text = "00123456";
                Find<Button>(modal, "ConfirmCode").onClick.Invoke();
                Find<TMP_InputField>(modal, "Password").text = "fixture_password";
                Find<TMP_InputField>(modal, "ConfirmPassword").text = "fixture_password";
                foreach(var toggle in modal.GetComponentsInChildren<Toggle>(true)) toggle.isOn=true;
                Find<Button>(modal, "CompleteCredential").onClick.Invoke();
                Assert.That(fixture.Store.Saves, Is.EqualTo(1));
                Assert.That(modal.activeSelf, Is.False, "Email modal must not cover the nickname form");
                Assert.That(fixture.Runtime.Owner.Stage, Is.EqualTo(PlayerAuthStage.Preparing));
                Find<TMP_InputField>(fixture.Root, "Nickname").text = "fixture_name";
                Find<Button>(fixture.Root, "SubmitNickname").onClick.Invoke();
                for (var i = 0; i < 20 && fixture.Runtime.Owner.Busy; i++) await Task.Yield();
                Assert.That(fixture.Runtime.Owner.Ready, Is.True);
                Assert.That(fixture.Runtime.LobbySnapshot.Profile.DisplayName, Is.EqualTo("fixture_name"));
                Assert.That(fixture.Wire.Requests.Count(r => r.EndpointPath == "/v1/auth/guest"), Is.Zero);
            }
        }

        [Test]
        public async Task ResetPublicCompletionReturnsLoginWithoutAuthenticating()
        {
            using (var fixture = new Fixture())
            {
                fixture.Wire.EmailEnabled = true;
                await fixture.Runtime.Owner.RestoreAsync();
                Find<Button>(fixture.Root, "ChooseIdLogin").onClick.Invoke();
                Find<Button>(fixture.Root, "ResetPassword").onClick.Invoke();
                var modal = fixture.Root.GetComponentInChildren<PasswordCredentialPanel>(true).gameObject;
                Find<TMP_InputField>(modal, "LoginId").text = "fixture_id";
                Find<TMP_InputField>(modal, "RecoveryEmail").text = "synthetic@example.invalid";
                Find<Button>(modal, "SendCode").onClick.Invoke();
                Find<TMP_InputField>(modal, "VerificationCode").text = "00123456";
                Find<TMP_InputField>(modal, "Password").text = "fixture_password";
                Find<TMP_InputField>(modal, "ConfirmPassword").text = "fixture_password";
                foreach(var toggle in modal.GetComponentsInChildren<Toggle>(true)) toggle.isOn=true;
                Find<Button>(modal, "CompleteCredential").onClick.Invoke();
                Assert.That(modal.activeSelf, Is.False);
                Assert.That(Find<Button>(fixture.Root, "ChooseIdLogin").gameObject.activeSelf, Is.True);
                Assert.That(fixture.Store.Saves, Is.Zero);
                Assert.That(fixture.Runtime.Owner.Session, Is.Null);
            }
        }

        [Test]
        public async Task RegistrationMenuOpensOnlyAfterMissingStoreAndCancelReturnsLogin()
        {
            using (var fixture = new Fixture())
            {
                await fixture.Runtime.Owner.RestoreAsync();
                Find<Button>(fixture.Root, "ChooseIdLogin").onClick.Invoke();
                var register = fixture.Root.GetComponentsInChildren<Button>(true).SingleOrDefault(b => b.name == "Register");
                Assert.That(register, Is.Not.Null, "Main login has no registration action");
                Assert.That(register.interactable, Is.True);
                register.onClick.Invoke();
                var credential = fixture.Root.GetComponentsInChildren<MonoBehaviour>(true).SingleOrDefault(c => c.GetType().Name == "PasswordCredentialPanel");
                Assert.That(credential, Is.Not.Null, "Registration must open the actual email panel");
                Find<Button>(credential.gameObject, "CancelCredential").onClick.Invoke();
                Assert.That(fixture.Runtime.Owner.Session, Is.Null);
                Assert.That(fixture.Store.Saves, Is.Zero);
                Assert.That(Find<Button>(fixture.Root, "ChooseIdLogin").gameObject.activeSelf, Is.True);
            }
        }

        [Test]
        public async Task ResetMenuOpensWithoutCreatingGuestOrSavingTokens()
        {
            using (var fixture = new Fixture())
            {
                await fixture.Runtime.Owner.RestoreAsync();
                Find<Button>(fixture.Root, "ChooseIdLogin").onClick.Invoke();
                var reset = fixture.Root.GetComponentsInChildren<Button>(true).SingleOrDefault(b => b.name == "ResetPassword");
                Assert.That(reset, Is.Not.Null, "Main login has no reset action");
                reset.onClick.Invoke();
                var credential = fixture.Root.GetComponentsInChildren<MonoBehaviour>(true).SingleOrDefault(c => c.GetType().Name == "PasswordCredentialPanel");
                Assert.That(credential, Is.Not.Null);
                Assert.That(Find<Button>(credential.gameObject, "ConfirmCode").gameObject.activeInHierarchy, Is.False);
                Assert.That(fixture.Wire.Requests, Is.Empty);
                Assert.That(fixture.Store.Saves, Is.Zero);
            }
        }

        [Test]
        public async Task PublicGuestAndNicknameActionsReconcileLostWriteButDoNotBypassHydration()
        {
            using (var fixture = new Fixture())
            {
                fixture.Runtime.LobbyPreparation = null;
                await fixture.Runtime.Owner.RestoreAsync();
                Find<Button>(fixture.Root, "Guest").onClick.Invoke();
                Assert.That(fixture.Store.Saves, Is.EqualTo(1));
                Assert.That(fixture.Runtime.Owner.Stage, Is.EqualTo(PlayerAuthStage.Preparing));
                Assert.That(Find<TMP_InputField>(fixture.Root, "Nickname").gameObject.activeSelf, Is.True);
                Find<TMP_InputField>(fixture.Root, "Nickname").text = "fixture_name";
                Find<Button>(fixture.Root, "SubmitNickname").onClick.Invoke();
                Assert.That(fixture.Runtime.Profile.DisplayName, Is.EqualTo("fixture_name"));
                Assert.That(fixture.Runtime.Owner.Ready, Is.False, "No lobby binder was configured");
                Assert.That(fixture.Wire.Requests.Count(r => r.EndpointPath == "/v1/profile/display-name"), Is.EqualTo(1));
                Find<Button>(fixture.Root, "Cancel").onClick.Invoke();
                Assert.That(fixture.Runtime.Owner.Session, Is.Null);
                Assert.That(Find<Button>(fixture.Root, "ChooseIdLogin").gameObject.activeSelf, Is.True);
            }
        }

        [Test]
        public async Task CancelDuringSecureReadMustNotStartPasswordHttp()
        {
            using (var fixture = new Fixture())
            {
                await fixture.Runtime.Owner.RestoreAsync();
                fixture.Store.Deferred = new TaskCompletionSource<GameApiAuthTokenReadResult>();
                Find<Button>(fixture.Root, "ChooseIdLogin").onClick.Invoke();
                Find<TMP_InputField>(fixture.Root, "LoginId").text = "fixture_id";
                Find<TMP_InputField>(fixture.Root, "Password").text = "fixture_password";
                Find<Button>(fixture.Root, "Login").onClick.Invoke();
                Find<Button>(fixture.Root, "Cancel").onClick.Invoke();
                Assert.That(Find<Button>(fixture.Root, "Login").gameObject.activeSelf, Is.False, "Pending cancellation must not offer a silently ignored login");
                fixture.Store.Deferred.SetResult(new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Missing));
                Assert.That(fixture.Wire.Requests, Is.Empty);
                Assert.That(fixture.Runtime.Owner.Session, Is.Null);
                Assert.That(Find<Button>(fixture.Root, "ChooseIdLogin").gameObject.activeSelf, Is.True);
            }
        }

        [Test]
        public void DownloadControlsRemainAboveLoginAndRetryUsesReservedFooter()
        {
            var root = new GameObject("LoginLayerFixture", typeof(RectTransform));
            var download = new GameObject("DownloadLayerFixture", typeof(RectTransform));
            try
            {
                root.AddComponent<PlayerEntryPanel>().Initialize(Font());
                var overlay = download.AddComponent<StartupDownloadOverlay>(); overlay.Initialize(Font(), () => { }, () => { }, () => { });
                overlay.Show(true, 1000, false);
                Assert.That(download.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(root.GetComponent<Canvas>().sortingOrder));
                overlay.Show(false, 1000, true);
                Assert.That(Find<Button>(download, "RetryContent").GetComponent<RectTransform>().anchorMin.y, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(download); }
        }

        [Test]
        public void PlayerTransportCanBoundAStalledRequest()
        {
            var ctor = typeof(UnityWebRequestRunApiHttpAdapter).GetConstructor(new[] { typeof(string), typeof(int) });
            Assert.That(ctor, Is.Not.Null, "Player auth needs a bounded request timeout");
            var adapter = ctor.Invoke(new object[] { "http://127.0.0.1:1", 30 });
            var request = UnityWebRequestTransportRequest.FromRunApiRequest(new RunApiHttpRequest("GET", "/fixture", null, null));
            using (var native = (UnityEngine.Networking.UnityWebRequest)typeof(UnityWebRequestRunApiHttpAdapter)
                .GetMethod("BuildRequest", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(adapter, new object[] { request }))
                Assert.That(native.timeout, Is.EqualTo(30));
        }

        [TestCase(1600, 900)]
        [TestCase(2100, 900)]
        [TestCase(1200, 900)]
        public void LoginPanelFitsAndRenders(int width, int height)
        {
            var root = new GameObject("LoginRenderFixture", typeof(RectTransform));
            var cameraObject = new GameObject("LoginCamera", typeof(Camera));
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                var panel = root.AddComponent<PlayerEntryPanel>(); panel.Initialize(Font()); panel.ShowLogin(true, false);
                var camera = cameraObject.GetComponent<Camera>(); var canvasHeight = 1920f * height / width;
                camera.orthographic = true; camera.orthographicSize = canvasHeight / 2; camera.aspect = width / (float)height;
                camera.transform.position = new Vector3(0, 0, -10);
                root.GetComponent<CanvasScaler>().enabled = false;
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, canvasHeight);
                root.GetComponent<RectTransform>().position = Vector3.zero;
                Canvas.ForceUpdateCanvases(); panel.ShowLogin(true, false); Canvas.ForceUpdateCanvases();
                if (height == 480) { root.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases(); }
                foreach (var text in root.GetComponentsInChildren<TMP_Text>())
                {
                    text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name);
                    var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                    Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(-961), text.name);
                    Assert.That(corners[2].x, Is.LessThanOrEqualTo(961), text.name);
                }
                camera.targetTexture = target; RenderTexture.active = target; camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32();
                Assert.That(pixels.Count(p => p.r != pixels[0].r || p.g != pixels[0].g), Is.GreaterThan(pixels.Length / 10));
                Directory.CreateDirectory("QA/Act1Launch/Screenshots/PlayerEntry");
                File.WriteAllBytes("QA/Act1Launch/Screenshots/PlayerEntry/login_" + width + "_" + height + ".png", texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; cameraObject.GetComponent<Camera>().targetTexture = null;
                target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void LoginPublicActionClearsPasswordAndNicknameCancelReturnsToOwner()
        {
            var type = typeof(PlayerAuthOwner).Assembly.GetType("LastRail.Auth.PlayerEntryPanel");
            Assert.That(type, Is.Not.Null, "Actual login panel is missing");
            var root = new GameObject("EntryPanelFixture", typeof(RectTransform));
            try
            {
                var panel = root.AddComponent(type);
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                type.GetMethod("Initialize").Invoke(panel, new object[] { font });
                type.GetMethod("ShowLogin").Invoke(panel, new object[] { true, false });
                string receivedId = null, receivedPassword = null; int cancels = 0;
                type.GetEvent("LoginSubmitted").AddEventHandler(panel, (Action<string, string>)((id, password) => { receivedId = id; receivedPassword = password; }));
                type.GetEvent("CancelRequested").AddEventHandler(panel, (Action)(() => cancels++));
                Find<Button>(root, "ChooseIdLogin").onClick.Invoke();
                Find<TMP_InputField>(root, "LoginId").text = "fixture_id";
                var passwordInput = Find<TMP_InputField>(root, "Password");
                Assert.That(passwordInput.contentType, Is.EqualTo(TMP_InputField.ContentType.Password));
                passwordInput.text = "fixture_secret";
                Find<Button>(root, "Login").onClick.Invoke();
                Assert.That(receivedId, Is.EqualTo("fixture_id")); Assert.That(receivedPassword, Is.EqualTo("fixture_secret"));
                Assert.That(passwordInput.text, Is.Empty);
                Assert.That(Find<Button>(root, "Google").interactable, Is.False);
                type.GetMethod("ShowNickname").Invoke(panel, null);
                Find<Button>(root, "Cancel").onClick.Invoke(); Assert.That(cancels, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void BootstrapOwnsRuntimeBindingInLifecycle()
        {
            var type = typeof(PlayerAuthOwner).Assembly.GetType("LastRail.Auth.PlayerEntryRuntime");
            Assert.That(type, Is.Not.Null, "Actual bootstrap auth binding is missing");
            var field = typeof(LastRail.Resources.ResourceBootstrapController).GetField("authentication", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null); Assert.That(field.FieldType, Is.EqualTo(type));
        }

        private static T Find<T>(GameObject root, string name) where T : Component
        {
            foreach (var value in root.GetComponentsInChildren<T>(true)) if (value.name == name) return value;
            throw new Exception("Missing control " + name);
        }
    }
}
