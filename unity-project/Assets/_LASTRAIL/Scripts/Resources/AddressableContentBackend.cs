using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LastRail.Resources
{
    public sealed partial class AddressableContentBackend : IContentStartupBackend, IContentRevision, IDisposable
    {
        public const string RequiredLabel = "launch_required";
        public const string LobbyKey = "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab";
        public const string MainSceneKey = "Assets/_LASTRAIL/Scenes/Main.unity";
        private readonly string configUrl;
        private readonly bool contentConfigured;
        private readonly string installedVersionCode;
        private readonly string trustedCatalogRoot;
        private readonly ApprovedReleasePin releasePin;
        public bool PinConfirmationRequired { get; private set; }
        private ClientContentGate approvedGate;
        private IResourceLocator approvedLocator;
        private bool disposed;
        private int admissionGeneration;
        private readonly DownloadAccessPolicy access;
        private readonly List<IResourceLocation> bundles = new List<IResourceLocation>();
        private static readonly SemaphoreSlim DownloadLease = new SemaphoreSlim(1, 1);
        public string ContentRevision { get; private set; }
        public string ApprovedContentReleaseId => !disposed && !PinConfirmationRequired && approvedGate != null &&
            approvedGate.Evaluate(null) == ContentStage.Initializing && (!sessionBoundGate || SessionAuthorizationCurrent)
            ? approvedGate.ApprovedReleaseId : null;
        public ContentResourceSource Source { get; }
#if UNITY_EDITOR
        private IEditorProjectContent localContent;
        private readonly EditorLoopbackContentFixture fixture;
        private object fixtureRuntimeInstance;
        private string fixtureRuntimeSettings;
        private bool fixtureRuntimeReady;

        internal static AddressableContentBackend CreateForEditorLoopbackFixture(
            EditorLoopbackContentFixture fixture, DownloadAccessPolicy access) =>
            new AddressableContentBackend(fixture ?? throw new ArgumentNullException(nameof(fixture)), access);

        private AddressableContentBackend(EditorLoopbackContentFixture fixture, DownloadAccessPolicy access)
        {
            this.fixture = fixture;
            configUrl = fixture.ConfigUrl; contentConfigured = true;
            trustedCatalogRoot = fixture.TrustedCatalogRoot; installedVersionCode = fixture.InstalledVersionCode;
            this.access = access ?? new DownloadAccessPolicy();
            fixture.ValidateStateDirectory();
            releasePin = new ApprovedReleasePin(Path.Combine(fixture.StateDirectory, "pending-content-release.json"));
        }
#endif

        // Construct on Unity's main thread; all retries reuse this installed-build snapshot.
        public AddressableContentBackend(string configUrl, bool contentConfigured, DownloadAccessPolicy access = null,
            string trustedCatalogRoot = null, AppBuildSnapshot installedBuild = null,
            ContentResourceSource source = ContentResourceSource.RemoteApproved)
        {
            if (!Enum.IsDefined(typeof(ContentResourceSource), source)) throw new ArgumentOutOfRangeException(nameof(source));
#if !UNITY_EDITOR
            if (source != ContentResourceSource.RemoteApproved) throw new PlatformNotSupportedException("Local project resources are Editor-only.");
#endif
            Source = source;
            this.configUrl = configUrl; this.contentConfigured = contentConfigured;
            this.access = access ?? new DownloadAccessPolicy();
            this.trustedCatalogRoot = trustedCatalogRoot;
            releasePin = new ApprovedReleasePin(Path.Combine(Application.persistentDataPath, "pending-content-release.json"));
            var build = installedBuild ?? AppBuildSnapshot.CaptureInstalled();
            installedVersionCode = build.AppVersionCode?.ToString(CultureInfo.InvariantCulture);
        }

        public async Task<ClientContentGate> FetchConfigAsync(CancellationToken cancel)
        {
            if (disposed) throw new ObjectDisposedException(nameof(AddressableContentBackend));
            var generation = ++admissionGeneration;
            authorization?.Invalidate(); sessionBoundGate = false;
            approvedGate = null;
            ReleaseOwnedLocator();
            bundles.Clear();
            ContentRevision = null;
            cancel.ThrowIfCancellationRequested();
            using (var request = CreateAdmissionRequest())
            {
                request.timeout = 20;
                var operation = request.SendWebRequest();
                using (cancel.Register(request.Abort))
                    while (!operation.isDone) { cancel.ThrowIfCancellationRequested(); await Task.Yield(); }
                cancel.ThrowIfCancellationRequested();
                if (disposed || generation != admissionGeneration) throw new OperationCanceledException();
                if (request.result != UnityWebRequest.Result.Success) throw new InvalidOperationException("Client config request failed.");
                var gate = ParseAdmission(request.downloadHandler.text, request.url);
                if (gate.Evaluate(null) == ContentStage.Initializing)
                {
                    try
                    {
                        ValidateFixtureState();
                        if (Source == ContentResourceSource.RemoteApproved)
                            releasePin.VerifyOrPin(configUrl, installedVersionCode, trustedCatalogRoot, gate);
                    }
                    catch (ApprovedReleasePinException) { PinConfirmationRequired = true; throw; }
                    approvedGate = gate;
                }
                return gate;
            }
        }

        private Task installedRuntimeInitialization;
        public async Task InitializeAsync(CancellationToken cancel)
        {
            RequireApproved();
#if UNITY_EDITOR
            if(Source==ContentResourceSource.EditorLocal){await InitializeInstalledRuntimeCoreAsync(cancel);return;}
#endif
            await InitializePublicEntryAsync(cancel);
        }
        internal async Task InitializePublicEntryAsync(CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
#if UNITY_EDITOR
            if(Source==ContentResourceSource.EditorLocal)return;
#endif
            if(disposed)throw new ObjectDisposedException(nameof(AddressableContentBackend));
            // Installation initialization is shared; cancellation belongs to each waiting screen.
            if(installedRuntimeInitialization==null)installedRuntimeInitialization=InitializeInstalledRuntimeCoreAsync(CancellationToken.None);
            await installedRuntimeInitialization;
            cancel.ThrowIfCancellationRequested();
            if(disposed)throw new ObjectDisposedException(nameof(AddressableContentBackend));
        }

        private async Task InitializeInstalledRuntimeCoreAsync(CancellationToken cancel)
        {
            var generation = admissionGeneration;
#if UNITY_EDITOR
            if (Source == ContentResourceSource.EditorLocal)
            {
                if (localContent == null) localContent = EditorProjectContentBridge.Factory?.Invoke()
                    ?? throw new InvalidOperationException("Editor project loader is unavailable.");
                var locator = await localContent.InitializeAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                if (disposed) throw new OperationCanceledException();
                approvedLocator = locator;
                return;
            }
#endif
            if (!contentConfigured) throw new InvalidOperationException("Remote content hosting is unconfigured.");
            // Validate the actual built runtime setting before Addressables can contact any catalog.
            var runtimePath = SelectRuntimeSettingsPath(PlayerPrefs.HasKey(Addressables.kAddressablesRuntimeDataPath));
            string settings;
            if (runtimePath.Contains("://"))
            {
                if (!runtimePath.StartsWith("jar:file:", StringComparison.Ordinal) && !runtimePath.StartsWith("file:", StringComparison.Ordinal))
                    throw new InvalidOperationException("Addressables settings must be installed locally.");
                using (var request = UnityWebRequest.Get(runtimePath))
                {
                    await Send(request, cancel);
                    settings = request.downloadHandler.text;
                }
            }
            else settings = File.ReadAllText(runtimePath);
            var runtimeData = JsonUtility.FromJson<ResourceManagerRuntimeData>(settings);
            ApprovedCatalogSafety.ValidateInstalledRuntimeData(runtimeData,
                id => ApprovedCatalogSafety.NormalizeInstalledPath(Addressables.ResolveInternalId(id)));
            cancel.ThrowIfCancellationRequested();
#if UNITY_EDITOR
            if (fixture != null)
            {
                if (disposed) throw new OperationCanceledException();
                if (fixtureRuntimeInstance != null)
                {
                    fixture.RequireSameRuntime(fixtureRuntimeInstance);
                    if (!fixtureRuntimeReady || fixtureRuntimeSettings != settings)
                        throw new InvalidOperationException("Fixture runtime initialization is incomplete or its settings changed.");
                    return;
                }
                var fixtureHandle = fixture.InitializeRuntime(out fixtureRuntimeInstance);
                fixtureRuntimeSettings = settings;
                try
                {
                    await Complete(fixtureHandle, cancel);
                    if (disposed) throw new OperationCanceledException();
                    fixture.RequireSameRuntime(fixtureRuntimeInstance);
                    fixtureRuntimeReady = true;
                }
                finally { ReleaseAfterCompletion(fixtureHandle); }
                return;
            }
#endif
            var handle = Addressables.InitializeAsync(false);
            try { await Complete(handle, cancel); }
            finally { ReleaseAfterCompletion(handle); }
        }

        public async Task UpdateCatalogAsync(CancellationToken cancel)
        {
            RequireApproved();
#if UNITY_EDITOR
            if (Source == ContentResourceSource.EditorLocal)
            {
                cancel.ThrowIfCancellationRequested();
                if (approvedLocator == null) throw new InvalidOperationException("Local project resources are not initialized.");
                ContentRevision = "editor-local-project";
                return;
            }
#endif
            RequireFixtureRuntime();
            var generation = admissionGeneration;
            var gate = approvedGate;
            ReleaseOwnedLocator();
            var directory = Path.Combine(Application.persistentDataPath, "approved-catalogs");
#if UNITY_EDITOR
            if (fixture != null) directory = Path.Combine(fixture.StateDirectory, "approved-catalogs");
#endif
#if ENABLE_JSON_CATALOG
            const bool jsonCatalogEnabled = true;
#else
            const bool jsonCatalogEnabled = false;
#endif
            var path = VerifiedCatalogPath(directory, gate, jsonCatalogEnabled);
            byte[] bytes;
            using (var request = UnityWebRequest.Get(gate.ApprovedCatalogUrl))
            {
                await Send(request, cancel);
                bytes = request.downloadHandler.data;
            }
            string digest;
            using (var sha = SHA256.Create()) digest = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            if ("sha256:" + digest != gate.ApprovedCatalogHash)
                throw new InvalidOperationException("Approved catalog bytes do not match the server hash.");
            cancel.ThrowIfCancellationRequested();
            if (disposed || generation != admissionGeneration) throw new OperationCanceledException();
            ValidateFixtureState();
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, bytes);
            var handle = Addressables.LoadContentCatalogAsync(path, false);
            try
            {
                // A catalog operation can register a global locator after cancellation. Observe completion
                // before exposing the locator or cleaning it up, rather than accepting a late result.
                while (!handle.IsDone) await Task.Yield();
                if ((cancel.IsCancellationRequested || disposed || generation != admissionGeneration) && handle.Status == AsyncOperationStatus.Succeeded)
                    Addressables.RemoveResourceLocator(handle.Result);
                cancel.ThrowIfCancellationRequested();
                if (disposed || generation != admissionGeneration) throw new OperationCanceledException();
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                    throw new InvalidOperationException("Approved catalog could not be loaded.", handle.OperationException);
                approvedLocator = handle.Result;
                ContentRevision = gate.ApprovedReleaseId + ":" + gate.ApprovedCatalogHash;
            }
            finally { ReleaseAfterCompletion(handle); }
        }

        public Task<long> RequiredBytesAsync(CancellationToken cancel)
        {
            RequireApproved();
            RequireFixtureRuntime();
            cancel.ThrowIfCancellationRequested();
            if (approvedLocator == null || !approvedLocator.Locate(RequiredLabel, null, out var locations) || locations == null)
                throw new InvalidOperationException("Approved required content locations are missing.");
            bundles.Clear();
#if UNITY_EDITOR
            if (Source == ContentResourceSource.EditorLocal) return Task.FromResult(0L);
#endif
            bundles.AddRange(BuildRemoteBundlePlan(locations.ToArray()));
            return Task.FromResult(Remaining());
        }

        private static bool Cached(IResourceLocation location)
        {
            var options = (AssetBundleRequestOptions)location.Data;
            return Caching.IsVersionCached(new CachedAssetBundle(options.BundleName, Hash128.Parse(options.Hash)));
        }

        private long Remaining() => bundles.Where(b => !Cached(b)).Sum(b => ((AssetBundleRequestOptions)b.Data).BundleSize);

        public async Task DownloadAsync(Action<long, long> progress, CancellationToken cancel)
        {
            RequireApproved();
#if UNITY_EDITOR
            if (Source == ContentResourceSource.EditorLocal)
            {
                await RequiredBytesAsync(cancel);
                progress?.Invoke(0, 0);
                return;
            }
#endif
            RequireFixtureRuntime();
            await DownloadLease.WaitAsync(cancel);
            try { await PrefetchAsync(progress, cancel); }
            finally { DownloadLease.Release(); }
        }

        private async Task PrefetchAsync(Action<long, long> progress, CancellationToken cancel)
        {
            if (bundles.Count == 0) throw new InvalidOperationException("Resolve the content plan before downloading.");
            var total = Remaining();
            foreach (var location in bundles)
            {
                while (!Cached(location))
                {
                    access.SetRemaining(Remaining());
                    await access.WaitForAccessAsync(cancel);
                    cancel.ThrowIfCancellationRequested();
                    var options = (AssetBundleRequestOptions)location.Data;
                    var url = Addressables.ResourceManager.TransformInternalId(location);
                    ValidateContentUrl(url);
                    var interrupted = false;
                    using (var request = UnityWebRequestAssetBundle.GetAssetBundle(url,
                        new CachedAssetBundle(options.BundleName, Hash128.Parse(options.Hash)), options.Crc))
                    {
                        request.timeout = 30;
                        request.redirectLimit = 0;
                        ((DownloadHandlerAssetBundle)request.downloadHandler).autoLoadAssetBundle = false;
                        Action policyChanged = () => { if (!access.Allowed) { interrupted = true; request.Abort(); } };
                        access.Changed += policyChanged;
                        try
                        {
                            if (!access.Allowed) continue;
                            var operation = request.SendWebRequest();
                            using (cancel.Register(request.Abort))
                            {
                                while (!operation.isDone)
                                {
                                    var remaining = Remaining();
                                    var current = Math.Min((long)request.downloadedBytes, options.BundleSize);
                                    progress(Math.Max(0, Math.Min(total, total - remaining + current)), total);
                                    await Task.Yield();
                                }
                            }
                            cancel.ThrowIfCancellationRequested();
                            if (interrupted)
                            {
                                // Incomplete bundles are not range-resumed; report their full remaining size again.
                                progress(Math.Max(0, total - Remaining()), total);
                                continue;
                            }
                            if (request.result != UnityWebRequest.Result.Success)
                                throw new InvalidOperationException("Required bundle download failed.");
                            if (!Cached(location)) throw new InvalidOperationException("Downloaded bundle was not committed to cache.");
                        }
                        finally { access.Changed -= policyChanged; }
                    }
                }
                access.SetRemaining(Remaining());
                progress(Math.Max(0, total - Remaining()), total);
            }
        }

        public IResourceLocation ResolveApprovedAsset(string key, Type type) => ResolveApprovedLocation(key, type);
        public void ValidatePreparedContent(string revision) { RequireApproved(); if (revision != ContentRevision || approvedLocator == null) throw new OperationCanceledException("Prepared content revision changed."); }

        public IResourceLocation ResolveApprovedTexture(string key) => ResolveApprovedLocation(key, typeof(Texture2D));

        public IResourceLocation ResolveApprovedScreen(string key) => ResolveApprovedLocation(key, typeof(GameObject));

        public IResourceLocation ResolveApprovedMainScene() => ResolveApprovedLocation(MainSceneKey, typeof(SceneInstance));

        private IResourceLocation ResolveApprovedLocation(string key, Type resourceType)
        {
            RequireApproved();
            RequireFixtureRuntime();
            if (string.IsNullOrWhiteSpace(key) || approvedLocator == null ||
                !approvedLocator.Locate(key, resourceType, out var locations) ||
                locations == null || locations.Count != 1 || locations[0] == null)
                throw new InvalidOperationException("Approved screen location is missing or ambiguous.");
#if UNITY_EDITOR
            if (Source == ContentResourceSource.EditorLocal) return locations[0];
#endif
            foreach (var bundle in BuildRemoteBundlePlan(new[] { locations[0] }))
                if (!Cached(bundle)) throw new InvalidOperationException("Approved remote dependencies must be cached before loading.");
            return locations[0];
        }

        public async Task VerifyRequiredAssetsAsync(CancellationToken cancel)
        {
            var location = ResolveApprovedScreen(LobbyKey);
            var handle = Addressables.LoadAssetAsync<GameObject>(location);
            try
            {
                await Complete(handle, cancel);
                if (handle.Result == null) throw new InvalidOperationException("Required screen is missing.");
            }
            finally { ReleaseAfterCompletion(handle); }
        }

        public static async Task Complete<T>(AsyncOperationHandle<T> handle, CancellationToken cancel)
        {
            while (!handle.IsDone) { cancel.ThrowIfCancellationRequested(); await Task.Yield(); }
            cancel.ThrowIfCancellationRequested();
            if (handle.Status != AsyncOperationStatus.Succeeded) throw new InvalidOperationException("Content operation failed.", handle.OperationException);
        }

        // Shared Addressables operations cannot be aborted safely by a single consumer.
        public static void ReleaseAfterCompletion<T>(AsyncOperationHandle<T> handle)
        {
            if (!handle.IsValid()) return;
            if (handle.IsDone) Addressables.Release(handle);
            else handle.Completed += completed => Addressables.Release(completed);
        }

        public static void ReleaseAfterCompletion(AsyncOperationHandle handle)
        {
            if (!handle.IsValid()) return;
            if (handle.IsDone) Addressables.Release(handle);
            else handle.Completed += completed => Addressables.Release(completed);
        }

        private void RequireApproved()
        {
            if (disposed) throw new ObjectDisposedException(nameof(AddressableContentBackend));
            if (sessionBoundGate && !SessionAuthorizationCurrent)
                throw new InvalidOperationException("Content authorization no longer belongs to the current session.");
            if (approvedGate == null || approvedGate.Evaluate(null) != ContentStage.Initializing)
                throw new InvalidOperationException("A current approved admission is required before content operations.");
        }

        public void ResetPendingRelease()
        {
            if (disposed) throw new ObjectDisposedException(nameof(AddressableContentBackend));
            if (Source == ContentResourceSource.EditorLocal) return;
            ValidateFixtureState();
            releasePin.Clear(); PinConfirmationRequired = false;
        }

        public void CompletePendingRelease()
        {
            RequireApproved();
            if (Source == ContentResourceSource.EditorLocal) return;
            try { ValidateFixtureState(); releasePin.Complete(approvedGate); }
            catch (ApprovedReleasePinException) { PinConfirmationRequired = true; throw; }
        }

        private void ReleaseOwnedLocator()
        {
            if (approvedLocator != null && Source == ContentResourceSource.RemoteApproved) Addressables.RemoveResourceLocator(approvedLocator);
            approvedLocator = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            authorization?.Dispose();
            admissionGeneration++;
            approvedGate = null;
            ReleaseOwnedLocator();
            bundles.Clear();
            ContentRevision = null;
        }

        private string SelectRuntimeSettingsPath(bool hasRuntimePathOverride)
        {
#if UNITY_EDITOR
            // Only the fixture path adapter uses this path directly on the actual instance.
            // The default Addressables initializer below still requires no preference override.
            if (fixture != null) return fixture.RuntimeSettingsPath;
#endif
            if (hasRuntimePathOverride)
                throw new InvalidOperationException("An overridden Addressables runtime path is not approved.");
            return Addressables.RuntimePath + "/settings.json";
        }

        private UnityWebRequest CreateAdmissionRequest()
        {
#if UNITY_EDITOR
            if (fixture != null) return fixture.CreateAdmissionRequest();
#endif
            ClientAdmissionContract.ValidateCatalogRoot(trustedCatalogRoot);
            return ClientAdmissionContract.CreateRequest(configUrl, installedVersionCode);
        }

        private ClientContentGate ParseAdmission(string json, string source)
        {
#if UNITY_EDITOR
            if (fixture != null)
            {
                if (source != fixture.ConfigUrl) throw new InvalidOperationException("Fixture admission endpoint changed.");
                return fixture.ParseAdmission(json);
            }
#endif
            return ClientAdmissionContract.Parse(json, installedVersionCode, source, trustedCatalogRoot);
        }

        private string VerifiedCatalogPath(string directory, ClientContentGate gate, bool json)
        {
#if UNITY_EDITOR
            if (fixture != null) return fixture.VerifiedCatalogPath(gate.ApprovedCatalogUrl, gate.ApprovedCatalogHash, json);
#endif
            return ApprovedCatalogSafety.VerifiedCatalogPath(directory, gate.ApprovedCatalogUrl, gate.ApprovedCatalogHash, json);
        }

        private IResourceLocation[] BuildRemoteBundlePlan(IResourceLocation[] roots)
        {
#if UNITY_EDITOR
            if (fixture != null) return fixture.BuildRemoteBundlePlan(roots, Addressables.ResourceManager.TransformInternalId);
#endif
            return ApprovedCatalogSafety.BuildRemoteBundlePlan(roots, trustedCatalogRoot, Addressables.ResourceManager.TransformInternalId);
        }

        private void ValidateContentUrl(string url)
        {
#if UNITY_EDITOR
            if (fixture != null) { fixture.ValidateContentUrl(url); return; }
#endif
            ClientAdmissionContract.ValidateCatalogUrl(url, trustedCatalogRoot);
        }

        private void ValidateFixtureState()
        {
#if UNITY_EDITOR
            fixture?.ValidateStateDirectory();
#endif
        }

        private void RequireFixtureRuntime()
        {
#if UNITY_EDITOR
            if (fixture != null)
            {
                if (!fixtureRuntimeReady) throw new InvalidOperationException("Initialize the approved fixture runtime first.");
                fixture.RequireSameRuntime(fixtureRuntimeInstance);
            }
#endif
        }

        private static async Task Send(UnityWebRequest request, CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
            request.timeout = 20;
            request.redirectLimit = 0;
            var operation = request.SendWebRequest();
            using (cancel.Register(request.Abort))
                while (!operation.isDone) { cancel.ThrowIfCancellationRequested(); await Task.Yield(); }
            cancel.ThrowIfCancellationRequested();
            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException("Approved content request failed.");
        }
    }
}
