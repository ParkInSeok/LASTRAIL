using System;
using System.Collections.Generic;

namespace LastRail.Presentation.Battle
{
    /// <summary>An original accepted stream position and existing display name, not a skill command.</summary>
    public sealed class UltimatePresentationEntry
    {
        public long Order { get; }
        public string ActorName { get; }
        internal Func<Func<IDisposable>, UltimatePresentationSession> Create { get; }
        public UltimatePresentationEntry(long order, string actorName,
            Func<Func<IDisposable>, UltimatePresentationSession> create)
        {
            if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));
            if (string.IsNullOrWhiteSpace(actorName)) throw new ArgumentException("Original actor display name required.", nameof(actorName));
            Order = order; ActorName = actorName; Create = create ?? throw new ArgumentNullException(nameof(create));
        }
    }

    /// <summary>
    /// One account/run/node display scope, owned/disposed by its controller. AcceptBatch consumes
    /// an already ordered accepted stream at its safe boundary. It cannot recover missing stream
    /// positions or reorder out-of-order callbacks. No gameplay, clock, DAILY or resource authority.
    /// All calls are on the presentation thread.
    /// </summary>
    public sealed class UltimatePresentationSequence : IDisposable
    {
        sealed class Item
        {
            public UltimatePresentationEntry Entry;
            public UltimatePresentationSession Session;
            public Action Completed;
        }
        sealed class Borrow : IDisposable
        {
            Action release;
            public Borrow(Action release) { this.release = release; }
            public void Dispose() { var old = release; release = null; old?.Invoke(); }
        }
        readonly Func<bool> scopeCurrent;
        readonly Func<IDisposable> acquireSegment;
        readonly Action<Exception> report;
        readonly Queue<Item> pending = new Queue<Item>();
        readonly Dictionary<string, long> seen = new Dictionary<string, long>(StringComparer.Ordinal);
        readonly List<string> summaries = new List<string>();
        readonly System.Collections.ObjectModel.ReadOnlyCollection<string> summaryView;
        Item current, summary;
        IDisposable segment;
        int segmentBorrowers;
        bool disposed, pumping;
        long lastOrder = -1, lastSkipEdge = -1, nextInputEdge;
        long notifiedDisplay = -1;
        string notifiedNext;
        int notifiedSummaryCount = -1;
        bool notifiedActive;
        public long DisplayGeneration { get; private set; }
        public string NextActor { get; private set; }
        public IReadOnlyList<string> SummaryActors => summaryView;
        // View owns one subscription and removes it at unbind. Refresh never emits input commands.
        public event Action Changed;

        /// <param name="acquireSegment">Existing safe-tick owner returns this segment's time AND
        /// gameplay-input lease. Per-film restoration must not release this shared segment lease.
        /// Null means unsafe: sessions use fallback. Dispose must preserve other owners' holds.</param>
        public UltimatePresentationSequence(Func<bool> scopeCurrent, Func<IDisposable> acquireSegment,
            Action<Exception> report)
        {
            this.scopeCurrent = scopeCurrent ?? throw new ArgumentNullException(nameof(scopeCurrent));
            this.acquireSegment = acquireSegment ?? throw new ArgumentNullException(nameof(acquireSegment));
            this.report = report ?? throw new ArgumentNullException(nameof(report));
            summaryView = summaries.AsReadOnly();
        }

        public void AcceptBatch(IReadOnlyList<UltimatePresentationEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (!Valid()) return;
            // Check ordering before invoking any factory or publishing part of a malformed batch.
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] == null || i > 0 && entries[i].Order <= entries[i - 1].Order)
                    throw new ArgumentException("Use the original accepted ordered stream.", nameof(entries));
            foreach (var entry in entries)
            {
                UltimatePresentationSession session = null;
                try
                {
                    session = entry.Create(BorrowSegment);
                    if (session == null) throw new InvalidOperationException("Accepted presentation factory returned no session.");
                    if (session.State != "Waiting") throw new InvalidOperationException("Session was already started.");
                    // Action IDs are scoped by this account/run/node owner. Generation must agree.
                    if (seen.TryGetValue(session.ActionId, out var generation))
                    {
                        if (generation != session.ActionGeneration)
                            Report(new InvalidOperationException("Original action identity reused with another generation."));
                        session.Dispose(); continue;
                    }
                    if (entry.Order <= lastOrder)
                        throw new InvalidOperationException("Accepted stream arrived out of order; resynchronize its original owner.");
                    if (!Valid()) { session.Dispose(); return; }
                    seen.Add(session.ActionId, session.ActionGeneration); lastOrder = entry.Order;
                    pending.Enqueue(new Item { Entry = entry, Session = session });
                }
                catch (Exception error) { session?.Dispose(); Report(error); }
            }
            Pump();
        }

        IDisposable BorrowSegment()
        {
            if (!Valid()) return null;
            if (segment == null)
            {
                var acquired = acquireSegment();
                if (disposed) { Release(acquired); return null; }
                segment = acquired;
            }
            if (segment == null) return null;
            segmentBorrowers++;
            return new Borrow(() =>
            {
                segmentBorrowers--;
                if (disposed) ReleaseSegment();
            });
        }
        void Pump()
        {
            if (pumping || disposed) return;
            pumping = true;
            try
            {
                while (current == null && pending.Count != 0 && Valid())
                {
                    var item = pending.Dequeue();
                    if (!item.Session.HasCurrentSource) { item.Session.Dispose(); continue; }
                    if (summary != null && !item.Session.CanPresentStandard)
                    {
                        // One expanded cut-in; subsequent already resolved events are name-only.
                        // Their original field effects have their own owner and never wait here.
                        summaries.Add(item.Entry.ActorName);
                        item.Session.Dispose();
                        continue;
                    }
                    EndSummary();
                    if (!Valid()) { item.Session.Dispose(); break; }
                    current = item; DisplayGeneration++;
                    item.Completed = () => OnCompleted(item);
                    item.Session.Completed += item.Completed;
                    item.Session.Start();
                    if (current == item && item.Session.Kind != UltimatePresentationSession.Standard)
                    {
                        current = null;
                        if (item.Session.Kind == UltimatePresentationSession.Summary)
                        {
                            summary = item; summaries.Add(item.Entry.ActorName);
                        }
                        else
                        {
                            // Minimal decoration is the existing battlefield. No delayed replay.
                            Unsubscribe(item); item.Session.Dispose();
                        }
                    }
                }
            }
            finally
            {
                pumping = false;
                if (current == null) ReleaseSegment();
                UpdateView();
            }
        }
        void OnCompleted(Item item)
        {
            Unsubscribe(item);
            if (ReferenceEquals(current, item)) current = null;
            if (ReferenceEquals(summary, item)) { summary = null; summaries.Clear(); }
            Pump(); // Synchronous Begin completion stays in the current iterative pump.
        }
        void EndSummary()
        {
            var old = summary; summary = null; summaries.Clear();
            if (old == null) return;
            Unsubscribe(old); old.Session.Dispose();
        }
        static void Unsubscribe(Item item)
        {
            if (item.Completed != null) item.Session.Completed -= item.Completed;
            item.Completed = null;
        }
        public bool Skip(long displayGeneration, long inputEdge)
        {
            if (!Valid() || current == null || displayGeneration != DisplayGeneration || inputEdge <= lastSkipEdge) return false;
            lastSkipEdge = inputEdge;
            current.Session.Skip();
            return true;
        }
        // View captures this on a NEW pointer/key down; holding/releasing an old press can never
        // skip a successor. Edge ownership survives view replacement within this sequence.
        public Func<bool> CaptureSkipInput()
        {
            long display = DisplayGeneration;
            long edge = nextInputEdge = checked(Math.Max(nextInputEdge, lastSkipEdge) + 1);
            return () => Skip(display, edge);
        }
        // Call on readiness/DAILY invalidation or controller update; this never loads or uses APIs.
        public void Refresh()
        {
            if (!Valid()) return;
            var active = current;
            if (active != null && !active.Session.HasCurrentSource) active.Session.Cancel();
            var partial = summary;
            if (partial != null && !partial.Session.HasCurrentSource) partial.Session.Cancel();
            Pump();
        }
        bool Valid()
        {
            if (disposed) return false;
            try { if (scopeCurrent() && !disposed) return true; }
            catch (Exception error) { Report(error); }
            Dispose(); return false;
        }
        void UpdateView()
        {
            NextActor = null;
            if (!disposed && current != null)
                foreach (var item in pending.ToArray())
                    if (item.Session.CanPresentStandard) { NextActor = item.Entry.ActorName; break; }
            if (notifiedDisplay == DisplayGeneration && notifiedNext == NextActor &&
                notifiedSummaryCount == summaries.Count && notifiedActive == (current != null)) return;
            notifiedDisplay = DisplayGeneration; notifiedNext = NextActor;
            notifiedSummaryCount = summaries.Count; notifiedActive = current != null;
            var subscribers = Changed;
            if (subscribers != null)
                foreach (Action subscriber in subscribers.GetInvocationList())
                    try { subscriber(); } catch (Exception error) { Report(error); }
        }
        void ReleaseSegment()
        {
            if (segmentBorrowers != 0) return;
            var old = segment; segment = null; Release(old);
        }
        void Release(IDisposable owned)
        {
            try { owned?.Dispose(); } catch (Exception error) { Report(error); }
        }
        void Report(Exception error)
        {
            try { report(error); }
            catch (Exception sinkError) { LastDiagnosticFailure = new AggregateException(error, sinkError); }
        }
        public Exception LastDiagnosticFailure { get; private set; }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; DisplayGeneration++;
            var old = current; current = null;
            if (old != null) { Unsubscribe(old); old.Session.Dispose(); }
            EndSummary();
            while (pending.Count != 0) pending.Dequeue().Session.Dispose();
            seen.Clear(); ReleaseSegment(); UpdateView();
        }
    }
}
