using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LastRail.Resources
{
    /// <summary>
    /// Optional presentation assets for one real formation/content scope. The supplied loader
    /// must use the existing approved content mapping/backend and its CombatAssetLease, not
    /// arbitrary file paths. Validation includes version/identity AND first displayable frame,
    /// supported playback and teardown. A loaded file alone is not ready.
    /// Main-thread owner: synchronize on account/run/formation generation change; dispose on exit.
    /// </summary>
    public sealed class UltimateAssetPreparation<T> : IDisposable where T : UnityEngine.Object
    {
        sealed class Slot
        {
            public string Key, Revision;
            public CombatAssetLease<T> Loaded;
            public Task Loading;
            public int Borrowers;
            public bool Retained = true, Failed;
        }
        readonly Func<string, string, CancellationToken, Task<CombatAssetLease<T>>> load;
        readonly Func<T, string, string, bool> validateReady;
        readonly Action<Exception> report;
        readonly Dictionary<string, Slot> slots = new Dictionary<string, Slot>(StringComparer.Ordinal);
        HashSet<string> formation = new HashSet<string>(StringComparer.Ordinal);
        CancellationTokenSource lifetime = new CancellationTokenSource();
        object owner;
        long generation, epoch;
        string revision;
        bool disposed;
        public int CachedCount => slots.Count;
        public Exception LastFailure { get; private set; }
        // Read-only DEV diagnostics; no automatic PRD logs or fabricated byte-memory estimate.
        public string ContentRevision => revision;
        public long PreparationGeneration => epoch;
        public long LoadRequests { get; private set; }
        public long CacheReuses { get; private set; }
        public long ReturnedSourceLeases { get; private set; }
        public int ActiveBorrowCount { get; private set; }
        public double LastLoadMilliseconds { get; private set; }

        public UltimateAssetPreparation(
            Func<string, string, CancellationToken, Task<CombatAssetLease<T>>> load,
            Func<T, string, string, bool> validateReady, Action<Exception> report)
        {
            this.load = load ?? throw new ArgumentNullException(nameof(load));
            this.validateReady = validateReady ?? throw new ArgumentNullException(nameof(validateReady));
            this.report = report ?? throw new ArgumentNullException(nameof(report));
        }
        public void SynchronizeOwner(object session, long sessionGeneration)
        {
            if (disposed) throw new ObjectDisposedException(nameof(UltimateAssetPreparation<T>));
            if (sessionGeneration < 0) throw new ArgumentOutOfRangeException(nameof(sessionGeneration));
            if (ReferenceEquals(session, owner) && generation == sessionGeneration) return;
            Reset(); owner = session; generation = sessionGeneration;
        }
        /// <summary>
        /// Call with actual formation references after essential gameplay preparation. Stable
        /// repeated inputs reuse valid assets; only changed references are retired. A standard
        /// ID is supplied only if currently usable by the original skill owner. canWarm consumes
        /// measured device budget; null denies optional warmup. No invented MB/count/time limits.
        /// </summary>
        public async Task PrepareAsync(IReadOnlyList<string> summaries, IReadOnlyList<string> standards,
            string contentRevision, Func<string, bool> canWarm, CancellationToken cancel)
        {
            if (disposed) throw new ObjectDisposedException(nameof(UltimateAssetPreparation<T>));
            if (owner == null) throw new InvalidOperationException("A current account/formation scope is required.");
            if (string.IsNullOrWhiteSpace(contentRevision)) throw new ArgumentException("Verified content revision required.", nameof(contentRevision));
            var required = Keys(summaries); var optional = Keys(standards);
            if (revision != contentRevision) { Reset(); revision = contentRevision; }
            var desired = new HashSet<string>(required, StringComparer.Ordinal);
            desired.UnionWith(optional);
            if (!formation.SetEquals(desired))
            {
                CancelPreparation();
                formation = desired;
                // Preserve ready shared assets, but do not share an old cancelled load.
                foreach (var key in new List<string>(slots.Keys))
                    if (slots[key].Loaded == null && !slots[key].Failed) Retire(key);
            }
            foreach (var key in new List<string>(slots.Keys))
                if (!desired.Contains(key)) Retire(key);
            var stamp = epoch;
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancel))
            {
                foreach (var key in required)
                {
                    if (!Current(stamp, linked.Token)) return;
                    await Ensure(key, contentRevision, stamp, linked.Token);
                }
                foreach (var key in optional)
                {
                    if (!Current(stamp, linked.Token)) return;
                    bool allowed = false;
                    try { allowed = canWarm != null && canWarm(key); }
                    catch (Exception error) { Report(error); }
                    if (allowed && Current(stamp, linked.Token)) await Ensure(key, contentRevision, stamp, linked.Token);
                }
            }
        }
        static List<string> Keys(IReadOnlyList<string> input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var keys = new List<string>(); var distinct = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in input)
            {
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Original manifest reference required.", nameof(input));
                if (distinct.Add(key)) keys.Add(key);
            }
            return keys;
        }
        Task Ensure(string key, string contentRevision, long stamp, CancellationToken token)
        {
            if (slots.TryGetValue(key, out var existing))
            {
                CacheReuses++;
                return existing.Loading ?? Task.CompletedTask;
            }
            var slot = new Slot { Key = key, Revision = contentRevision };
            slots.Add(key, slot);
            var loading = Load(slot, stamp, token);
            slot.Loading = loading.IsCompleted ? null : loading;
            return loading;
        }
        async Task Load(Slot slot, long stamp, CancellationToken token)
        {
            CombatAssetLease<T> acquired = null;
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                LoadRequests++;
                acquired = await load(slot.Key, slot.Revision, token);
                if (!Current(stamp, token) || !slot.Retained) { RetireIfCurrent(slot); return; }
                if (acquired?.Asset == null || !validateReady(acquired.Asset, slot.Key, slot.Revision))
                    throw new InvalidOperationException("Optional cutscene identity/version/first-frame validation failed.");
                if (!Current(stamp, token) || !slot.Retained) { RetireIfCurrent(slot); return; }
                slot.Loaded = acquired; acquired = null;
            }
            catch (OperationCanceledException)
            {
                // Cancelled work is not a validated content failure. An explicit later prepare
                // may retry, but a completed failed asset does not spin within this generation.
                RetireIfCurrent(slot);
            }
            catch (Exception error) { slot.Failed = true; Report(error); }
            finally { slot.Loading = null; ReturnSource(acquired); LastLoadMilliseconds = elapsed.Elapsed.TotalMilliseconds; }
        }
        void RetireIfCurrent(Slot slot)
        {
            if (slots.TryGetValue(slot.Key, out var active) && ReferenceEquals(active, slot)) Retire(slot.Key);
        }
        bool Current(long stamp, CancellationToken token) => !disposed && stamp == epoch && !token.IsCancellationRequested;
        public bool IsReady(string key, string contentRevision) =>
            !disposed && owner != null && contentRevision == revision && key != null &&
            slots.TryGetValue(key, out var slot) && slot.Retained && !slot.Failed && slot.Loaded?.Asset != null;
        public CombatAssetLease<T> Borrow(string key, string contentRevision)
        {
            if (!IsReady(key, contentRevision)) return null;
            var slot = slots[key]; slot.Borrowers++; ActiveBorrowCount++;
            return new CombatAssetLease<T>(slot.Loaded.Asset, () =>
            {
                slot.Borrowers--; ActiveBorrowCount--;
                if ((!slot.Retained || slot.Failed) && slot.Borrowers == 0) ReleaseSlot(slot);
            });
        }
        /// <summary>
        /// Pin the verified visual/cue/stem references BEFORE queuing a presentation. References
        /// are approved mapping IDs (including independent BGM/SFX roles), never guessed paths.
        /// The validation port checks exact timeline, lengths, hashes, branch/profile and consumer
        /// support. A missing required component rejects the combination; caller selects fallback.
        /// </summary>
        public UltimateAvSelection<T> SelectCombination(string combinationId,string timelineRevision,string cueRevision,
            bool reduced,IReadOnlyList<string> references,string contentRevision,Func<IReadOnlyList<T>,bool> validateCombination)
        {
            if(string.IsNullOrWhiteSpace(combinationId)||string.IsNullOrWhiteSpace(timelineRevision)||string.IsNullOrWhiteSpace(cueRevision)||validateCombination==null)
                throw new ArgumentException("Verified immutable combination/timeline/cue revisions and validator required.");
            var keys=Keys(references);
            if(keys.Count==0||keys.Count!=references.Count)throw new ArgumentException("Exact distinct component references required.");
            var borrowed=new List<CombatAssetLease<T>>();
            var failures=new List<Action<Exception>>();
            long selectedEpoch=epoch;
            bool retained=false;
            try
            {
                var values=new List<T>();
                foreach(var key in keys)
                {
                    var lease=Borrow(key,contentRevision);
                    if(lease==null)return null;
                    borrowed.Add(lease);values.Add(lease.Asset);
                    failures.Add(CapturePlaybackFailure(key,contentRevision));
                }
                if(!validateCombination(values.AsReadOnly())||!Current(selectedEpoch,CancellationToken.None))return null;
                var selection=new UltimateAvSelection<T>(combinationId,timelineRevision,cueRevision,reduced,keys,borrowed,failures,Report);
                retained=true;return selection;
            }
            finally{if(!retained)foreach(var lease in borrowed)Release(lease);}
        }
        /// <summary>Capture at playback creation and bind to its decode/timeout failure callback,
        /// followed by the session's fail callback. A late failure cannot invalidate a replacement
        /// scope even when its asset key/revision are identical. Never call for normal skip/end.</summary>
        public Action<Exception> CapturePlaybackFailure(string key, string contentRevision)
        {
            slots.TryGetValue(key, out var captured);
            if (captured != null && captured.Revision != contentRevision) captured = null;
            return error =>
            {
                if (captured != null && captured.Retained && slots.TryGetValue(key, out var active) &&
                    ReferenceEquals(active, captured))
                {
                    captured.Failed = true;
                    if (captured.Borrowers == 0) ReleaseSlot(captured);
                }
                Report(error ?? new InvalidOperationException("Optional cutscene playback failed."));
            };
        }
        sealed class Playback : IDisposable
        {
            IDisposable view, asset;
            readonly Action<IDisposable> release;
            public Playback(IDisposable view, IDisposable asset, Action<IDisposable> release)
            { this.view = view; this.asset = asset; this.release = release; }
            public void Dispose()
            {
                var oldView = view; var oldAsset = asset; view = null; asset = null;
                release(oldView); release(oldAsset);
            }
        }
        // Bind this to the existing session's play callback. The returned lifetime always stops
        // the player's callbacks/render targets BEFORE returning the borrowed shared asset.
        public IDisposable PlayBorrowed(string key, string contentRevision, Func<T, IDisposable> begin)
        {
            if (begin == null) throw new ArgumentNullException(nameof(begin));
            var asset = Borrow(key, contentRevision);
            if (asset == null) throw new InvalidOperationException("Prepared cutscene is no longer ready.");
            var failed = CapturePlaybackFailure(key, contentRevision);
            try
            {
                var view = begin(asset.Asset);
                if (view == null) throw new InvalidOperationException("Cutscene player did not supply its lifetime.");
                return new Playback(view, asset, Release);
            }
            catch (Exception error) { failed(error); asset.Dispose(); throw; }
        }
        // Memory pressure reclaims only unused prepared assets; live playback remains borrowed.
        public void TrimUnused()
        {
            foreach (var key in new List<string>(slots.Keys))
                if (slots[key].Borrowers == 0 && !slots[key].Failed) Retire(key);
        }
        void Retire(string key)
        {
            var slot = slots[key]; slots.Remove(key); slot.Retained = false;
            if (slot.Borrowers == 0) ReleaseSlot(slot);
        }
        void ReleaseSlot(Slot slot) { var old = slot.Loaded; slot.Loaded = null; ReturnSource(old); }
        void ReturnSource(CombatAssetLease<T> lease)
        {
            if (lease == null) return;
            ReturnedSourceLeases++; Release(lease);
        }
        void CancelPreparation()
        {
            epoch++;
            var old = lifetime; lifetime = new CancellationTokenSource();
            try { old.Cancel(); } catch (Exception error) { Report(error); }
            old.Dispose();
        }
        void Reset()
        {
            CancelPreparation();
            foreach (var key in new List<string>(slots.Keys)) Retire(key);
            revision = null; formation.Clear();
        }
        void Release(IDisposable lease)
        {
            try { lease?.Dispose(); } catch (Exception error) { Report(error); }
        }
        void Report(Exception error)
        {
            LastFailure = error;
            try { report(error); } catch (Exception sink) { LastFailure = new AggregateException(error, sink); }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; Reset(); lifetime.Dispose(); owner = null;
        }
    }
}
