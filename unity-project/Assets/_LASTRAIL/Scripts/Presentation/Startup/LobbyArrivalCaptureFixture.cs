#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Client;
using LastRail.Resources;
using LastRail.Run;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
namespace LastRail.Presentation.Startup
{
    // Local recording dependencies only. Actual Main/host/prefab/auth preparation run unchanged.
    public static class LobbyArrivalCaptureFixture
    {
        const string Root="https://content.example.test/releases/", Source="https://api.lastrail.app/v1/client-config";
        public static void Configure(ResourceBootstrapController bootstrap,string directory)
        {
            if(Get<AddressableContentBackend>(bootstrap,"backend").Source!=ContentResourceSource.EditorLocal) throw new InvalidOperationException("Local capture requires editor source content.");
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
                locations.Add(AddressableContentBackend.MainSceneKey, main);
                Set(backend, "approvedLocator", locations);
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
        public sealed class Store : IGameApiAuthTokenStore, IGameApiAuthTokenReader
        {
            public GameApiAuthSessionSnapshot Value;
            public Task<GameApiAuthTokenReadResult> ReadAsync() => Task.FromResult(new GameApiAuthTokenReadResult(Value == null ? GameApiAuthTokenReadOutcome.Missing : GameApiAuthTokenReadOutcome.Loaded, Value));
            public Task<GameApiAuthSessionSnapshot> LoadAsync() => Task.FromResult(Value);
            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot value) { Value = value; return Task.FromResult(GameApiAuthTokenStoreResult.Saved); }
        }
        public sealed class Wire : IRunApiHttpTransport
        {
            private bool named;
            public string Output;
            public int NameWrites;
            public readonly TaskCompletionSource<bool> NameAccepted=new TaskCompletionSource<bool>();
            public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                File.AppendAllText(Path.Combine(Output,"arrival-local-requests.txt"),request.EndpointPath+" LOCAL ONLY\n");
                string json;
                switch (request.EndpointPath)
                {
                    case "/v1/auth/guest": json = "{\"tokenType\":\"Bearer\",\"account\":{\"id\":\"account_test\",\"status\":\"ACTIVE\"},\"accessToken\":\"lst1.fixture\",\"refreshToken\":\"fixture\",\"accessTokenExpiresAt\":\"2030-01-01T00:00:00.000Z\",\"refreshTokenExpiresAt\":\"2030-02-01T00:00:00.000Z\"}"; break;
                    case "/v1/profile/display-name": NameWrites++; await NameAccepted.Task; named = true; json = "{\"accepted\":true,\"displayName\":\"철도지휘관\",\"changeStatus\":\"UPDATED\"}"; break;
                    case "/v1/profile": json = "{\"accepted\":true,\"account\":{\"id\":\"account_test\"},\"profile\":{\"displayName\":" + (named ? "\"철도지휘관\"" : "null") + ",\"commanderXp\":0}}"; break;
                    case "/v1/story-progress": json = "{\"accepted\":true,\"actId\":\"act_01_wasteland_railway\",\"storyProgress\":{\"highestStoryProgressId\":\"none\",\"highestClearedOperationId\":null,\"order\":0},\"clearedOperationIds\":[],\"unlockedOperations\":[\"opn_01_last_departure\"],\"narrativeVersion\":\"fixture_narrative\",\"narrative\":{\"contentStatus\":\"CANDIDATE\",\"localizationStatus\":\"PENDING\",\"fixtureStatus\":\"PARTIAL\",\"missingContentStatus\":\"UNKNOWN\"}}"; break;
                    default: throw new InvalidOperationException("Unconfigured fixture endpoint.");
                }
                return RunApiHttpResponse.Success(200, json);
            }
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
        private static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        private static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(o,value);
    }
}
#endif

