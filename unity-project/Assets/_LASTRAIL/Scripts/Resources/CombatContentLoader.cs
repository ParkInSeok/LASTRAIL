using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LastRail.Resources
{
    public enum CombatContentStatus { Missing, Unsupported, Invalid, LoadFailed, Cancelled, Loaded }

    // Carries the server document unchanged. This is not a new manifest schema.
    public sealed class CombatContentRequest
    {
        readonly JObject content;
        public JObject RequiredContent => (JObject)content.DeepClone();
        public string BindingVersion => Read("bindingVersion");
        public string CatalogHash => Read("catalogHash");
        public string ContentVersion => Read("contentVersion");
        public CombatContentRequest(JObject requiredContent)
        { content = (JObject)(requiredContent ?? throw new ArgumentNullException(nameof(requiredContent))).DeepClone(); }
        string Read(string field) => content[field]?.Type == JTokenType.String ? (string)content[field] : null;
        internal string InvalidField()
        {
            foreach (var field in new[] { "operationId", "contentVersion", "startAugmentPackageRef", "startTacticPackageRef", "encounterType", "raidId", "bossId", "bindingVersion", "catalogHash" })
                if (content[field] == null || (content[field].Type != JTokenType.Null &&
                    (content[field].Type != JTokenType.String || string.IsNullOrWhiteSpace((string)content[field])))) return field;
            if (!(content["bossPhaseRefs"] is JArray phases)) return "bossPhaseRefs";
            foreach (var phase in phases)
                if (phase.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)phase)) return "bossPhaseRefs";
            if (CatalogHash != null && !Regex.IsMatch(CatalogHash, "\\Asha256:[a-f0-9]{64}\\z")) return "catalogHash";
            return null;
        }
    }

    public sealed class CombatAssetLease<T> : IDisposable where T : UnityEngine.Object
    {
        Action release;
        public T Asset { get; private set; }
        internal CombatAssetLease(T asset, Action release) { Asset = asset; this.release = release; }
        public void Dispose() { var action = release; release = null; Asset = null; action?.Invoke(); }
#if UNITY_EDITOR
        public static CombatAssetLease<T> CreateFixture(T asset, Action release) => new CombatAssetLease<T>(asset, release);
#endif
    }

    public sealed class CombatContentResult
    {
        public CombatContentStatus Status { get; }
        public string Diagnostic { get; }
        public bool Ready => false; // CE02 load success alone never authorizes combat entry.
        public string Notice => Status == CombatContentStatus.Cancelled ? "" : "전투 준비가 완료되면 출발할 수 있습니다.";
        internal CombatContentResult(CombatContentStatus status, string diagnostic) { Status = status; Diagnostic = diagnostic; }
    }

    internal sealed class CombatContentException : Exception
    {
        internal readonly CombatContentStatus Status;
        internal CombatContentException(CombatContentStatus status, string diagnostic) : base(diagnostic) { Status = status; }
    }

    // Unity main-thread owner. The shipping binding registry is deliberately empty until
    // an approved version/type/asset mapping exists. Editor fixtures cannot register it.
    public sealed class CombatContentLoader<T> : IDisposable where T : UnityEngine.Object
    {
        readonly Func<string, CombatContentRequest, CancellationToken, Task<CombatAssetLease<T>>> source;
        readonly string supportedVersion;
        readonly Func<CombatContentRequest, string> resolve;
        readonly Func<T, CombatContentRequest, bool> validate;
        object owner;
        long generation, request;
        bool disposed;
        CancellationTokenSource pending;
        CombatAssetLease<T> current;
        public CombatContentResult Result { get; private set; } = new CombatContentResult(CombatContentStatus.Missing, "binding.unapproved");
        public T Asset => current?.Asset;
        public CombatContentLoader(AddressableContentBackend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            source = (key, input, token) => backend.LoadApprovedCombatAssetAsync<T>(key, input.ContentVersion, input.CatalogHash, token);
        }
#if UNITY_EDITOR
        public static CombatContentLoader<T> CreateFixture(string version, Func<CombatContentRequest, string> resolve,
            Func<T, CombatContentRequest, bool> validate,
            Func<string, CombatContentRequest, CancellationToken, Task<CombatAssetLease<T>>> source)
            => new CombatContentLoader<T>(version, resolve, validate, source);
        CombatContentLoader(string version, Func<CombatContentRequest, string> resolve,
            Func<T, CombatContentRequest, bool> validate,
            Func<string, CombatContentRequest, CancellationToken, Task<CombatAssetLease<T>>> source)
        { supportedVersion = version; this.resolve = resolve; this.validate = validate; this.source = source; }
#endif
        public void SynchronizeOwner(object session, long sessionGeneration)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CombatContentLoader<T>));
            if (ReferenceEquals(owner, session) && generation == sessionGeneration) return;
            Cancel(); owner = session; generation = sessionGeneration;
            Result = new CombatContentResult(CombatContentStatus.Cancelled, "owner.changed");
        }
        void Cancel()
        {
            request++; pending?.Cancel(); pending?.Dispose(); pending = null;
            current?.Dispose(); current = null;
        }
        public async Task<CombatContentResult> LoadAsync(CombatContentRequest input, CancellationToken cancel = default)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CombatContentLoader<T>));
            Cancel(); var stamp = request;
            var operation = CancellationTokenSource.CreateLinkedTokenSource(cancel); pending = operation;
            var token = operation.Token;
            CombatAssetLease<T> loaded = null;
            CombatContentResult outcome;
            try
            {
                token.ThrowIfCancellationRequested();
                if (owner == null) throw new OperationCanceledException();
                var invalid = input?.InvalidField() ?? (input == null ? "requiredContent" : null);
                if (invalid != null) throw new CombatContentException(CombatContentStatus.Invalid, "requiredContent." + invalid);
                if (input.BindingVersion == null || input.CatalogHash == null)
                    throw new CombatContentException(CombatContentStatus.Missing, "binding.unapproved");
                if (supportedVersion == null || input.BindingVersion != supportedVersion)
                    throw new CombatContentException(CombatContentStatus.Unsupported, "binding.version_or_type");
                if (input.ContentVersion == null) throw new CombatContentException(CombatContentStatus.Invalid, "requiredContent.contentVersion");
                var key = resolve(input);
                if (string.IsNullOrWhiteSpace(key)) throw new CombatContentException(CombatContentStatus.Missing, "binding.asset_mapping");
                loaded = await source(key, input, token);
                token.ThrowIfCancellationRequested();
                if (disposed || stamp != request) throw new OperationCanceledException();
                if (loaded?.Asset == null) throw new CombatContentException(CombatContentStatus.LoadFailed, "asset.empty");
                if (!validate(loaded.Asset, input)) throw new CombatContentException(CombatContentStatus.Invalid, "asset.required_content_mismatch");
                current = loaded; loaded = null;
                outcome = new CombatContentResult(CombatContentStatus.Loaded, "asset.loaded_combat_still_blocked");
            }
            catch (OperationCanceledException) { outcome = new CombatContentResult(CombatContentStatus.Cancelled, "request.cancelled"); }
            catch (CombatContentException error) { outcome = new CombatContentResult(error.Status, error.Message); }
            catch { outcome = new CombatContentResult(CombatContentStatus.LoadFailed, "asset.load_failed"); }
            finally { loaded?.Dispose(); if (ReferenceEquals(pending, operation)) pending = null; operation.Dispose(); }
            if (stamp != request || disposed) return new CombatContentResult(CombatContentStatus.Cancelled, "request.superseded");
            Result = outcome; return outcome;
        }
        public void Dispose() { if (disposed) return; disposed = true; Cancel(); Result = new CombatContentResult(CombatContentStatus.Cancelled, "loader.disposed"); }
    }

    public sealed partial class AddressableContentBackend
    {
        public async Task<CombatAssetLease<T>> LoadApprovedCombatAssetAsync<T>(string key, string contentVersion,
            string catalogHash, CancellationToken cancel) where T : UnityEngine.Object
        {
            cancel.ThrowIfCancellationRequested();
            RequireApproved(); RequireFixtureRuntime();
            if (Source == ContentResourceSource.EditorLocal || approvedLocator == null || string.IsNullOrEmpty(ContentRevision))
                throw new CombatContentException(CombatContentStatus.Missing, "catalog.approved_locator");
            if (string.IsNullOrEmpty(contentVersion) || string.IsNullOrEmpty(catalogHash) ||
                approvedGate.ApprovedContentVersion != contentVersion || approvedGate.ApprovedCatalogHash != catalogHash ||
                ContentRevision != approvedGate.ApprovedReleaseId + ":" + catalogHash)
                throw new CombatContentException(CombatContentStatus.Invalid, "catalog.admission_mismatch");
            if (string.IsNullOrWhiteSpace(key) || !approvedLocator.Locate(key, typeof(T), out var locations) || locations == null || locations.Count == 0)
                throw new CombatContentException(CombatContentStatus.Missing, "asset.approved_location");
            if (locations.Count != 1 || locations[0] == null || locations[0].ResourceType == null || !typeof(T).IsAssignableFrom(locations[0].ResourceType))
                throw new CombatContentException(CombatContentStatus.Invalid, "asset.ambiguous_or_wrong_type");
            // Reuses approved dependency URL/hash/cache policy, never a global key lookup.
            var location = ResolveApprovedLocation(key, typeof(T));
            var stamp = admissionGeneration; var gate = approvedGate; var locator = approvedLocator; var revision = ContentRevision;
            var handle = Addressables.LoadAssetAsync<T>(location);
            var transferred = false;
            try
            {
                await Complete(handle, cancel);
                if (disposed || stamp != admissionGeneration || !ReferenceEquals(gate, approvedGate) || !ReferenceEquals(locator, approvedLocator) || revision != ContentRevision)
                    throw new OperationCanceledException();
                if (handle.Result == null) throw new CombatContentException(CombatContentStatus.LoadFailed, "asset.empty");
                var lease = new CombatAssetLease<T>(handle.Result, () => ReleaseAfterCompletion(handle));
                transferred = true; return lease;
            }
            finally { if (!transferred) ReleaseAfterCompletion(handle); }
        }
    }
}
