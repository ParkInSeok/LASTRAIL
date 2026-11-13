using System;
using System.Collections.Generic;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    // Explicit presentation fixtures. No native ultimate source or final cutscene assets.
    public sealed class UltimatePresentationSequenceTests
    {
        sealed class Lease : IDisposable
        {
            Action release;
            public Lease(Action release) { this.release = release; }
            public void Dispose() { var r = release; release = null; r?.Invoke(); }
        }
        sealed class Fixture
        {
            public readonly List<string> Played = new List<string>();
            public readonly List<string> Stops = new List<string>();
            public readonly Dictionary<string, Action> Ends = new Dictionary<string, Action>();
            public readonly Dictionary<string, Action> Frames = new Dictionary<string, Action>();
            public readonly Dictionary<string, Action<Exception>> Fails = new Dictionary<string, Action<Exception>>();
            public readonly HashSet<string> Cancelled = new HashSet<string>();
            public readonly List<Exception> Errors = new List<Exception>();
            public bool Current = true, Seen, Ready = true, Synchronous;
            public int Holds, Releases;
            public Action OnBegin, OnStop;
            public object Sequence;
            readonly Type entryType;
            public Fixture()
            {
                var assembly = typeof(UltimatePresentationSession).Assembly;
                var type = assembly.GetType("LastRail.Presentation.Battle.UltimatePresentationSequence");
                entryType = assembly.GetType("LastRail.Presentation.Battle.UltimatePresentationEntry");
                Assert.That(type, Is.Not.Null, "Ordered presentation owner missing.");
                Sequence = Activator.CreateInstance(type, (Func<bool>)(() => Current),
                    (Func<IDisposable>)(() => { Holds++; return new Lease(() => Releases++); }),
                    (Action<Exception>)(e => Errors.Add(e)));
            }
            public object Entry(long order, string id, CombatPresentationMode mode = CombatPresentationMode.Full)
            {
                return Activator.CreateInstance(entryType, order, id,
                    (Func<Func<IDisposable>, UltimatePresentationSession>)(hold =>
                        new UltimatePresentationSession(id, 1, mode, () => Current && !Cancelled.Contains(id), () => Ready, () => true,
                            () => !Seen, () => Seen = true, hold,
                            (kind, visible, end, skip, fail) =>
                            {
                                Played.Add(id + ":" + kind); Ends[id] = end; Frames[id] = visible; Fails[id] = fail;
                                OnBegin?.Invoke();
                                if (Synchronous) { visible(); end(); }
                                return new Lease(() => { OnStop?.Invoke(); Stops.Add(id); });
                            }, valid => { }, Errors.Add)));
            }
            public void Add(params object[] entries)
            {
                var array = Array.CreateInstance(entryType, entries.Length);
                for (int i = 0; i < entries.Length; i++) array.SetValue(entries[i], i);
                Call("AcceptBatch", array);
            }
            public object Call(string method, params object[] args) => Sequence.GetType().GetMethod(method).Invoke(Sequence, args);
            public long Display => (long)Sequence.GetType().GetProperty("DisplayGeneration").GetValue(Sequence);
            public string Next => (string)Sequence.GetType().GetProperty("NextActor").GetValue(Sequence);
        }
        [Test] public void OrderedFullBatchKeepsOneHoldAcrossEndAndSkip()
        {
            var f = new Fixture(); f.Add(f.Entry(10, "nyx"), f.Entry(11, "eve"));
            Assert.That(f.Played, Is.EqualTo(new[] { "nyx:Standard" }));
            Assert.That(f.Next, Is.EqualTo("eve"));
            f.Ends["nyx"]();
            Assert.That(f.Played, Is.EqualTo(new[] { "nyx:Standard", "eve:Standard" }));
            Assert.That(f.Holds, Is.EqualTo(1)); Assert.That(f.Releases, Is.Zero);
            f.Call("Skip", f.Display, 1L); Assert.That(f.Releases, Is.EqualTo(1));
        }
        [Test] public void OldDisplayOrRepeatedInputEdgeCannotSkipNextFilm()
        {
            var f = new Fixture(); f.Add(f.Entry(1, "eve"), f.Entry(2, "nyx"));
            var old = f.Display; f.Call("Skip", old, 7L);
            f.Call("Skip", old, 8L); f.Call("Skip", f.Display, 7L);
            Assert.That(f.Releases, Is.Zero);
            f.Call("Skip", f.Display, 9L); Assert.That(f.Releases, Is.EqualTo(1));
        }
        [Test] public void DailyRechecksAfterFirstVisibleAndDoesNotHoldSummary()
        {
            var f = new Fixture(); f.Add(f.Entry(1, "a", CombatPresentationMode.FirstUseFull), f.Entry(2, "b", CombatPresentationMode.FirstUseFull));
            f.Frames["a"](); f.Call("Refresh");
            Assert.That(f.Next, Is.Null);
            f.Ends["a"]();
            Assert.That(f.Played, Is.EqualTo(new[] { "a:Standard", "b:Summary" }));
            Assert.That(f.Releases, Is.EqualTo(1));
        }
        [Test] public void DuplicateOriginalEventNeverReplaysAndLateCallbackCannotFinishSuccessor()
        {
            var f = new Fixture(); f.Add(f.Entry(1, "a"), f.Entry(2, "b"));
            f.Add(f.Entry(1, "a")); f.Ends["a"](); f.Ends["a"]();
            Assert.That(f.Played.Count, Is.EqualTo(2)); Assert.That(f.Releases, Is.Zero);
            f.Call("Dispose"); f.Ends["b"](); Assert.That(f.Releases, Is.EqualTo(1));
        }
        [Test] public void SynchronousCompletionCleansAllLeasesWithoutRecursiveQueueGrowth()
        {
            var f = new Fixture { Synchronous = true };
            var rows = new object[300]; for (int i = 0; i < rows.Length; i++) rows[i] = f.Entry(i, "e" + i);
            f.Add(rows);
            Assert.That(f.Played.Count, Is.EqualTo(300)); Assert.That(f.Stops.Count, Is.EqualTo(300));
            Assert.That(f.Holds, Is.EqualTo(1)); Assert.That(f.Releases, Is.EqualTo(1));
        }
        [Test] public void PartialSummariesMergeWithoutDelayedExpandedReplay()
        {
            var f = new Fixture(); f.Add(f.Entry(1, "a", CombatPresentationMode.Simplified), f.Entry(2, "b", CombatPresentationMode.Simplified));
            Assert.That(f.Played, Is.EqualTo(new[] { "a:Summary" }));
            var actors = (IReadOnlyList<string>)f.Sequence.GetType().GetProperty("SummaryActors").GetValue(f.Sequence);
            Assert.That(actors, Is.EqualTo(new[] { "a", "b" }));
            f.Ends["a"](); Assert.That(f.Played.Count, Is.EqualTo(1)); Assert.That(f.Holds, Is.Zero);
        }
        [Test] public void ScopeInvalidationDisposesCurrentAndPendingWithoutOldReplay()
        {
            var f = new Fixture(); f.Add(f.Entry(1, "a"), f.Entry(2, "b"));
            f.Current = false; f.Call("Refresh"); f.Frames["a"](); f.Ends["a"]();
            Assert.That(f.Seen, Is.False); Assert.That(f.Played.Count, Is.EqualTo(1));
            Assert.That(f.Releases, Is.EqualTo(1)); Assert.That(f.Next, Is.Null);
        }
        [Test] public void CancelledEventIsNotMergedIntoExistingSummary()
        {
            var f = new Fixture(); f.Add(f.Entry(1, "a", CombatPresentationMode.Simplified));
            f.Cancelled.Add("b"); f.Add(f.Entry(2, "b", CombatPresentationMode.Simplified));
            var actors = (IReadOnlyList<string>)f.Sequence.GetType().GetProperty("SummaryActors").GetValue(f.Sequence);
            Assert.That(actors, Is.EqualTo(new[] { "a" }));
            f.Call("Dispose");
        }
        [Test] public void FailureBeforeVisibilityLeavesNextDailyEligibleAndKeepsSegment()
        {
            var f = new Fixture(); f.Add(f.Entry(1, "a", CombatPresentationMode.FirstUseFull), f.Entry(2, "b", CombatPresentationMode.FirstUseFull));
            f.Fails["a"](new InvalidOperationException("decode fixture"));
            Assert.That(f.Played, Is.EqualTo(new[] { "a:Standard", "b:Standard" }));
            Assert.That(f.Seen, Is.False); Assert.That(f.Releases, Is.Zero);
            f.Call("Dispose");
        }
        [Test] public void RefreshDoesNotEmitUnchangedViewNotifications()
        {
            var f = new Fixture(); int changes = 0;
            f.Sequence.GetType().GetEvent("Changed").AddEventHandler(f.Sequence, (Action)(() => changes++));
            f.Add(f.Entry(1, "a")); var before = changes;
            f.Call("Refresh"); f.Call("Refresh");
            Assert.That(changes, Is.EqualTo(before)); f.Call("Dispose");
        }
        [Test] public void CapturedSkipIntentCannotCrossFilmOrRebindBoundary()
        {
            var f = new Fixture(); f.Add(f.Entry(1, "a"), f.Entry(2, "b"));
            var method = f.Sequence.GetType().GetMethod("CaptureSkipInput");
            Assert.That(method, Is.Not.Null);
            var oldPress = (Func<bool>)method.Invoke(f.Sequence, null);
            Assert.That(oldPress(), Is.True); Assert.That(oldPress(), Is.False);
            var nextPress = (Func<bool>)method.Invoke(f.Sequence, null);
            Assert.That(nextPress(), Is.True); Assert.That(f.Releases, Is.EqualTo(1));
        }
        [Test] public void DisposeInsideBeginRetainsSegmentUntilReturnedPlayerIsStopped()
        {
            var f = new Fixture(); int releasesAtStop = -1;
            f.OnBegin = () => f.Call("Dispose"); f.OnStop = () => releasesAtStop = f.Releases;
            f.Add(f.Entry(1, "a"));
            Assert.That(releasesAtStop, Is.Zero); Assert.That(f.Releases, Is.EqualTo(1));
            Assert.That(f.Errors, Is.Empty);
        }
        [Test] public void OriginalActionCancellationEndsCurrentWithoutWaitingForPlayerCallback()
        {
            var f = new Fixture(); f.Add(f.Entry(1, "a"), f.Entry(2, "b"));
            f.Cancelled.Add("a"); f.Call("Refresh");
            Assert.That(f.Played, Is.EqualTo(new[] { "a:Standard", "b:Standard" }));
            Assert.That(f.Stops, Is.EqualTo(new[] { "a" }));
            Assert.That(f.Releases, Is.Zero); f.Call("Dispose");
        }
        [Test] public void ViewUnbindIgnoresHeldPointerAndRebindDoesNotDuplicateListeners()
        {
            var f = new Fixture(); f.Add(f.Entry(1, "a"), f.Entry(2, "b"));
            var type = typeof(UltimatePresentationSession).Assembly.GetType("LastRail.Presentation.Battle.UltimateSequenceView");
            Assert.That(type, Is.Not.Null);
            var go = new UnityEngine.GameObject("explicit-sequence-view-fixture");
            var eventGo = new UnityEngine.GameObject("explicit-pointer-fixture");
            try
            {
                var events = eventGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var view = go.AddComponent(type);
                var bind = type.GetMethod("Bind"); bind.Invoke(view, new[] { f.Sequence, null, null });
                var data = new UnityEngine.EventSystems.PointerEventData(events) { pointerId = 5 };
                type.GetMethod("OnPointerDown").Invoke(view, new object[] { data });
                type.GetMethod("Unbind").Invoke(view, null);
                type.GetMethod("OnPointerUp").Invoke(view, new object[] { data });
                Assert.That(f.Played.Count, Is.EqualTo(1));
                bind.Invoke(view, new[] { f.Sequence, null, null }); bind.Invoke(view, new[] { f.Sequence, null, null });
                type.GetMethod("OnPointerDown").Invoke(view, new object[] { data });
                type.GetMethod("OnPointerUp").Invoke(view, new object[] { data });
                type.GetMethod("OnPointerUp").Invoke(view, new object[] { data });
                Assert.That(f.Played.Count, Is.EqualTo(2)); Assert.That(f.Releases, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(eventGo); f.Call("Dispose"); }
        }
    }
}
