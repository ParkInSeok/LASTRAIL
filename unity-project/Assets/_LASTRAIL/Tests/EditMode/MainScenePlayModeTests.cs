#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Client;
using LastRail.Resources;
using LastRail.Run;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class MainScenePlayModeTests
    {
        private const string Boot = "Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity";
        private const string Root = "https://content.example.test/releases/";
        private const string Source = "https://api.lastrail.app/v1/client-config";

        [UnityTest]
        public IEnumerator DuplicateMainOpenDoesNotCancelTheOriginalLoad()
        {
            yield return new EnterPlayMode();
            var run = DuplicateOpen();
            while (!run.IsCompleted) yield return null;
            var error = run.Exception;
            yield return new ExitPlayMode();
            if (error != null) throw error;
        }

        private static async Task DuplicateOpen()
        {
            var field = typeof(Addressables).GetField("m_AddressablesInstance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = field.GetValue(null);
            var isolated = Activator.CreateInstance(previous.GetType(), new object[] { new DefaultAllocationStrategy() });
            Set(isolated, "hasStartedInitialization", true); Set(isolated, "SceneProvider", new SceneProvider());
            InitializeTracking(isolated);
            isolated.GetType().GetProperty("InstanceProvider").SetValue(isolated, new InstanceProvider());
            field.SetValue(null, isolated);
            var manager = Addressables.ResourceManager;
            manager.GetType().GetMethod("RegisterForCallbacks", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            var host = new AddressableMainSceneHost();
            try
            {
                var location = new ResourceLocationBase(AddressableContentBackend.MainSceneKey, AddressableContentBackend.MainSceneKey,
                    typeof(SceneProvider).FullName, typeof(SceneInstance));
                var first = host.OpenAsync(location, CancellationToken.None);
                var duplicate = host.OpenAsync(location, CancellationToken.None);
                Assert.That(duplicate.IsFaulted, Is.True, "Duplicate open must fail without blocking Unity's synchronization context.");
                Assert.That(duplicate.Exception.InnerException, Is.TypeOf<InvalidOperationException>());
                var root = await first;
                Assert.That(root, Is.Not.Null);
                Assert.That(root.ContentRoot.gameObject.activeInHierarchy, Is.False);
                var provider = new SourceAssetProvider { Delay = true };
                manager.ResourceProviders.Add(provider);
                var screenObject = new GameObject("PendingScreenHostFixture");
                var screens = screenObject.AddComponent<AddressableScreenHost>();
                try
                {
                    var pending = screens.OpenAsync(new ResourceLocationBase(AddressableContentBackend.LobbyKey,
                        AddressableContentBackend.LobbyKey, typeof(BundledAssetProvider).FullName, typeof(GameObject)), root.ContentRoot, CancellationToken.None);
                    await Until(() => provider.Held.HasValue);
                    var drained = screens.CloseAsync();
                    Assert.That(drained.IsCompleted, Is.False, "The Main mount must survive pending instantiation cleanup.");
                    Assert.That(root.ContentRoot.gameObject.activeInHierarchy, Is.False);
                    provider.CompleteHeld();
                    await drained;
                    try { await pending; Assert.Fail("Closed pending screen was published."); }
                    catch (OperationCanceledException) { }
                    await Task.Yield();
                    Assert.That(screens.Current, Is.Null);
                    Assert.That(root.ContentRoot.childCount, Is.Zero);
                }
                finally { await screens.CloseAsync(); UnityEngine.Object.Destroy(screenObject); }
                // Inject the state of a failed unload, retaining the real scene handle.
                // This verifies retry ownership, not an actual platform unload failure.
                var retired = Get<object>(host, "current");
                var failed = Task.FromException(new InvalidOperationException("fixture cleanup failure"));
                Set(retired, "release", failed); Set(host, "retiring", retired);
                Set(host, "current", null); Set(host, "cleanup", failed);
                await host.CloseAsync();
                Assert.That(SceneManager.GetSceneByPath(AddressableContentBackend.MainSceneKey).isLoaded, Is.False);
                var reopened = await host.OpenAsync(location, CancellationToken.None);
                Assert.That(reopened, Is.Not.Null);
                Assert.That(UnityEngine.Object.FindObjectsByType<MainSceneRoot>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            }
            finally
            {
                await host.CloseAsync();
                isolated.GetType().GetMethod("ReleaseSceneManagerOperation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(isolated, null);
                manager.Dispose(); field.SetValue(null, previous);
            }
        }

        [UnityTest]
        public IEnumerator EnabledBootstrapUsesActualMainAndLobbyWithExplicitLocalDependencies()
        {
            if (ClientEnvironmentBootstrap.CurrentEnvironment != ClientEnvironment.Unconfigured)
                Assert.Ignore("This local fixture must not start a configured remote environment.");
            yield return new EnterPlayMode();
            var run = Exercise();
            while (!run.IsCompleted) yield return null;
            var error = run.Exception;
            yield return new ExitPlayMode();
            if (error != null) throw error;
        }

        private static async Task Exercise()
        {
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(Boot, new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone) await Task.Yield();
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<ResourceBootstrapController>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Coordinator.ConfigAccepted, Is.False, "Shipping unconfigured source must fail closed.");
            Assert.That(SceneManager.GetSceneByPath(AddressableContentBackend.MainSceneKey).isLoaded, Is.False);
            var directory = Path.Combine(Path.GetTempPath(), "lastrail-main-scene-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var instanceField = typeof(Addressables).GetField("m_AddressablesInstance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instanceField.GetValue(null);
            var isolated = Activator.CreateInstance(previous.GetType(), new object[] { new DefaultAllocationStrategy() });
            Set(isolated, "hasStartedInitialization", true);
            InitializeTracking(isolated);
            Set(isolated, "SceneProvider", new SceneProvider());
            isolated.GetType().GetProperty("InstanceProvider").SetValue(isolated, new InstanceProvider());
            instanceField.SetValue(null, isolated);
            var manager = Addressables.ResourceManager;
            manager.GetType().GetMethod("RegisterForCallbacks", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            manager.ResourceProviders.Add(new SourceAssetProvider()); manager.ResourceProviders.Add(new LocalDependencyProvider());
            var host = Get<AddressableMainSceneHost>(bootstrap, "mainScene");
            try
            {
                // Only transport, catalog locations and storage are fixtures. The enabled
                // bootstrap entry callback, scene host, actual scenes, prefab and binder run unchanged.
                var gate = ClientAdmissionContract.Parse("{\"accepted\":true,\"maintenance\":{\"enabled\":false},\"clientAdmission\":{\"status\":\"ACCEPTED\",\"installedVersionCode\":\"42\",\"minimumVersionCode\":\"40\",\"updateRequired\":false},\"contentRelease\":{\"status\":\"APPROVED_COMPATIBLE\",\"releaseId\":\"fixture-main\",\"contentVersion\":\"fixture-main\",\"catalogUrl\":\"" + Root + "fixture-main/catalog.bin\",\"catalogHash\":\"sha256:" + new string('a', 64) + "\",\"compatibility\":{\"minimumVersionCode\":\"40\",\"maximumVersionCode\":null}}}", "42", Source, Root);
                var backend = Get<AddressableContentBackend>(bootstrap, "backend");
                Set(backend, "trustedCatalogRoot", Root); Set(backend, "approvedGate", gate);
                var pin = new ApprovedReleasePin(Path.Combine(directory, "pending.json"));
                pin.VerifyOrPin(Source, "42", Root, gate); Set(backend, "releasePin", pin);
                var dependency = new ResourceLocationBase("local-fixture-dependency", Path.Combine(directory, "source-only.bundle"),
                    typeof(CacheOnlyAssetBundleProvider).FullName, typeof(object)) { Data = new AssetBundleRequestOptions { BundleName = "source-fixture" } };
                var lobby = new ResourceLocationBase(AddressableContentBackend.LobbyKey, AddressableContentBackend.LobbyKey,
                    typeof(BundledAssetProvider).FullName, typeof(GameObject), dependency);
                var main = new ResourceLocationBase(AddressableContentBackend.MainSceneKey, AddressableContentBackend.MainSceneKey,
                    typeof(SceneProvider).FullName, typeof(SceneInstance), dependency);
                var locations = new ResourceLocationMap("explicit-local-source-fixture", 2);
                locations.Add(AddressableContentBackend.LobbyKey, lobby);
                Set(backend, "approvedLocator", locations);
                var content = new Content(gate); Set(bootstrap.Coordinator, "backend", content);
                bootstrap.Authentication.Dispose();
                await Task.Yield();
                var store = new Store(); var wire = new Wire();
                var runtime = new PlayerEntryRuntime(bootstrap,
                    AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"), wire, store, store);
                Set(bootstrap, "authentication", runtime);
                Invoke(bootstrap, "Retry");
                Assert.That(bootstrap.Coordinator.ConfigAccepted, Is.True);
                Assert.That(host.Current, Is.Null);
                Button(bootstrap, "Guest").onClick.Invoke();
                Assert.That(runtime.Owner.Ready, Is.False, "Nickname must still gate Main.");
                Assert.That(host.Current, Is.Null);
                bootstrap.GetComponentsInChildren<TMP_InputField>(true).Single(f => f.name == "Nickname").text = "fixture_name";
                Button(bootstrap, "SubmitNickname").onClick.Invoke();
                await Until(() => runtime.Owner.Ready);
                Assert.That(host.Current, Is.Null, "Required-content verification is still pending.");
                content.Verified.SetResult(true);
                var entry = Get<AuthenticatedScreenEntry>(bootstrap, "entry");
                await Until(() => entry.Failed);
                Assert.That(host.Current, Is.Null, "Missing approved Main must retain bootstrap retry.");
                Assert.That(Get<LastRail.Presentation.Startup.StartupScreenRootBehaviour>(bootstrap, "startup").gameObject.activeSelf, Is.True);
                locations.Add(AddressableContentBackend.MainSceneKey, main);
                Invoke(bootstrap, "Retry"); Invoke(bootstrap, "Retry");
                await Until(() => entry.Entered);
                Assert.That(content.SizeReads, Is.EqualTo(2));
                Assert.That(content.ConfigReads, Is.EqualTo(1));
                Assert.That(host.Current.ContentRoot.gameObject.activeInHierarchy, Is.True);
                var screen = Get<AddressableScreenHost>(bootstrap, "screens").Current;
                Assert.That(screen.transform.parent, Is.EqualTo(host.Current.ContentRoot));
                Assert.That(screen.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "PlayerProfileName").text, Is.EqualTo("fixture_name"));
                Assert.That(runtime.BindLobby(screen), Is.True);
                Assert.That(UnityEngine.Object.FindObjectsByType<MainSceneRoot>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
                Assert.That(UnityEngine.Object.FindObjectsByType<ResourceBootstrapController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
                Assert.That(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
                Assert.That(File.Exists(Path.Combine(directory, "pending.json")), Is.False);
                bootstrap.ClearAuthenticatedSession();
                await host.CloseAsync();
                Assert.That(SceneManager.GetSceneByPath(AddressableContentBackend.MainSceneKey).isLoaded, Is.False);
                Assert.That(entry.Entered, Is.False);
                Assert.That(runtime.Owner.Session, Is.Null);
                Assert.That(store.Value, Is.Not.Null, "Local invalidation is not durable logout.");
            }
            finally
            {
                bootstrap.ClearAuthenticatedSession();
                await host.CloseAsync();
                UnityEngine.Object.Destroy(bootstrap.gameObject);
                await Task.Yield();
                isolated.GetType().GetMethod("ReleaseSceneManagerOperation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(isolated, null);
                manager.Dispose(); instanceField.SetValue(null, previous);
                Directory.Delete(directory, true);
            }
        }

        private sealed class SourceAssetProvider : ResourceProviderBase
        {
            public bool Delay;
            public ProvideHandle? Held;
            public override string ProviderId => typeof(BundledAssetProvider).FullName;
            public override void Provide(ProvideHandle handle)
            {
                if (Delay) { Held = handle; return; }
                Complete(handle);
            }
            public void CompleteHeld() { var handle = Held.Value; Held = null; Complete(handle); }
            private static void Complete(ProvideHandle handle) => handle.Complete(AssetDatabase.LoadAssetAtPath<GameObject>(handle.Location.InternalId), true, null);
        }
        private sealed class LocalDependencyProvider : ResourceProviderBase
        {
            private sealed class SourceDependency : IAssetBundleResource { public AssetBundle GetAssetBundle() => null; }
            public override string ProviderId => typeof(CacheOnlyAssetBundleProvider).FullName;
            public override void Provide(ProvideHandle handle) => handle.Complete<IAssetBundleResource>(new SourceDependency(), true, null);
        }
        private sealed class Content : IContentStartupBackend
        {
            private readonly ClientContentGate gate;
            public int ConfigReads, SizeReads;
            public readonly TaskCompletionSource<bool> Verified = new TaskCompletionSource<bool>();
            public Content(ClientContentGate gate) { this.gate = gate; }
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken c) { ConfigReads++; return Task.FromResult(gate); }
            public Task InitializeAsync(CancellationToken c) => Task.CompletedTask;
            public Task UpdateCatalogAsync(CancellationToken c) => Task.CompletedTask;
            public Task<long> RequiredBytesAsync(CancellationToken c) { SizeReads++; return Task.FromResult(0L); }
            public Task DownloadAsync(Action<long, long> p, CancellationToken c) => throw new InvalidOperationException("Source fixture must never download.");
            public async Task VerifyRequiredAssetsAsync(CancellationToken c) { await Verified.Task; c.ThrowIfCancellationRequested(); }
        }
        private sealed class Store : IGameApiAuthTokenStore, IGameApiAuthTokenReader
        {
            public GameApiAuthSessionSnapshot Value;
            public Task<GameApiAuthTokenReadResult> ReadAsync() => Task.FromResult(new GameApiAuthTokenReadResult(Value == null ? GameApiAuthTokenReadOutcome.Missing : GameApiAuthTokenReadOutcome.Loaded, Value));
            public Task<GameApiAuthSessionSnapshot> LoadAsync() => Task.FromResult(Value);
            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot value) { Value = value; return Task.FromResult(GameApiAuthTokenStoreResult.Saved); }
        }
        private sealed class Wire : IRunApiHttpTransport
        {
            private bool named;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                string json;
                switch (request.EndpointPath)
                {
                    case "/v1/auth/guest": json = "{\"tokenType\":\"Bearer\",\"account\":{\"id\":\"account_test\",\"status\":\"ACTIVE\"},\"accessToken\":\"lst1.fixture\",\"refreshToken\":\"fixture\",\"accessTokenExpiresAt\":\"2030-01-01T00:00:00.000Z\",\"refreshTokenExpiresAt\":\"2030-02-01T00:00:00.000Z\"}"; break;
                    case "/v1/profile/display-name": named = true; json = "{}"; break;
                    case "/v1/profile": json = "{\"accepted\":true,\"account\":{\"id\":\"account_test\"},\"profile\":{\"displayName\":" + (named ? "\"fixture_name\"" : "null") + ",\"commanderXp\":0}}"; break;
                    case "/v1/story-progress": json = "{\"accepted\":true,\"actId\":\"act_01_wasteland_railway\",\"storyProgress\":{\"highestStoryProgressId\":\"none\",\"highestClearedOperationId\":null,\"order\":0},\"clearedOperationIds\":[],\"unlockedOperations\":[\"opn_01_last_departure\"],\"narrativeVersion\":\"fixture_narrative\",\"narrative\":{\"contentStatus\":\"CANDIDATE\",\"localizationStatus\":\"PENDING\",\"fixtureStatus\":\"PARTIAL\",\"missingContentStatus\":\"UNKNOWN\"}}"; break;
                    default: throw new InvalidOperationException("Unconfigured fixture endpoint.");
                }
                return Task.FromResult(RunApiHttpResponse.Success(200, json));
            }
        }
        private static async Task Until(Func<bool> ready)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (!ready()) { if (DateTime.UtcNow > deadline) Assert.Fail("Scene fixture timed out."); await Task.Yield(); }
        }
        private static void InitializeTracking(object instance)
        {
            foreach (var pair in new[] { new[] { "m_OnHandleCompleteAction", "OnHandleCompleted" },
                new[] { "m_OnSceneHandleCompleteAction", "OnSceneHandleCompleted" }, new[] { "m_OnHandleDestroyedAction", "OnHandleDestroyed" } })
            {
                var field = instance.GetType().GetField(pair[0], BindingFlags.Instance | BindingFlags.NonPublic);
                field.SetValue(instance, Delegate.CreateDelegate(field.FieldType, instance,
                    instance.GetType().GetMethod(pair[1], BindingFlags.Instance | BindingFlags.NonPublic)));
            }
        }
        private static Button Button(Component owner, string name) => owner.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
        private static void Invoke(object o, string name) => o.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, null);
        private static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
        private static void Set(object o, string name, object value) => o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(o, value);
    }
}
#endif
