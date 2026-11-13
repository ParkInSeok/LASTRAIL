using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine;

namespace LastRail.Tests.EditMode
{
    public sealed class UltimateAssetPreparationTests
    {
        sealed class Fixture : IDisposable
        {
            public readonly List<string> Loads = new List<string>();
            public int Released;
            public bool Verified = true;
            public TaskCompletionSource<CombatAssetLease<GameObject>> Pending;
            public readonly GameObject Asset = new GameObject("explicit-cutscene-asset-fixture");
            public object Owner;
            public Fixture()
            {
                var type = typeof(CombatAssetLease<>).Assembly.GetType("LastRail.Resources.UltimateAssetPreparation`1");
                Assert.That(type, Is.Not.Null, "Scoped cutscene preparation owner missing.");
                Owner = Activator.CreateInstance(type.MakeGenericType(typeof(GameObject)),
                    (Func<string, string, CancellationToken, Task<CombatAssetLease<GameObject>>>)(async (key, revision, token) =>
                    {
                        Loads.Add(key + ":" + revision);
                        return Pending == null ? Lease() : await Pending.Task;
                    }),
                    (Func<GameObject, string, string, bool>)((asset, key, revision) => Verified),
                    (Action<Exception>)(e => { }));
                Call("SynchronizeOwner", this, 1L);
            }
            public CombatAssetLease<GameObject> Lease() => CombatAssetLease<GameObject>.CreateFixture(Asset, () => Released++);
            public object Call(string method, params object[] args) => Owner.GetType().GetMethod(method).Invoke(Owner, args);
            public Task Prepare(string[] summaries, string[] standards, string revision = "r1", bool budget = true) =>
                (Task)Call("PrepareAsync", summaries, standards, revision, (Func<string, bool>)(_ => budget), CancellationToken.None);
            public CombatAssetLease<GameObject> Borrow(string key, string revision = "r1") =>
                (CombatAssetLease<GameObject>)Call("Borrow", key, revision);
            public void Dispose() { if (Owner != null) Call("Dispose"); UnityEngine.Object.DestroyImmediate(Asset); }
        }
        [Test] public async Task SummariesPrecedeBudgetAllowedStandardsAndWarmReuseDoesNotLoadAgain()
        {
            using (var f = new Fixture())
            {
                await f.Prepare(new[] { "s1", "s2" }, new[] { "f1", "f2" });
                await f.Prepare(new[] { "s1", "s2" }, new[] { "f1", "f2" });
                Assert.That(f.Loads, Is.EqualTo(new[] { "s1:r1", "s2:r1", "f1:r1", "f2:r1" }));
                using (var lease = f.Borrow("f1")) Assert.That(lease.Asset, Is.SameAs(f.Asset));
            }
        }
        [Test] public async Task NoMeasuredBudgetMeansSummaryOnly()
        {
            using (var f = new Fixture())
            {
                await f.Prepare(new[] { "s" }, new[] { "f" }, budget: false);
                Assert.That(f.Loads, Is.EqualTo(new[] { "s:r1" })); Assert.That(f.Borrow("f"), Is.Null);
            }
        }
        [Test] public async Task UnverifiedOrNoFirstFrameNeverBecomesReadyOrRetriesInSameGeneration()
        {
            using (var f = new Fixture { Verified = false })
            {
                await f.Prepare(new[] { "s" }, Array.Empty<string>());
                await f.Prepare(new[] { "s" }, Array.Empty<string>());
                Assert.That(f.Borrow("s"), Is.Null); Assert.That(f.Loads.Count, Is.EqualTo(1)); Assert.That(f.Released, Is.EqualTo(1));
            }
        }
        [Test] public async Task MemoryTrimAndScopeChangeDoNotDestroyPlayingBorrow()
        {
            using (var f = new Fixture())
            {
                await f.Prepare(new[] { "s" }, new[] { "f" });
                var playing = f.Borrow("f");
                f.Call("TrimUnused");
                Assert.That(f.Released, Is.EqualTo(1)); Assert.That(playing.Asset, Is.Not.Null);
                f.Call("SynchronizeOwner", new object(), 2L);
                Assert.That(f.Released, Is.EqualTo(1));
                playing.Dispose(); playing.Dispose(); Assert.That(f.Released, Is.EqualTo(2));
            }
        }
        [Test] public async Task LateLoadAfterScopeReplacementIsReturnedAndNeverPublished()
        {
            using (var f = new Fixture())
            {
                f.Pending = new TaskCompletionSource<CombatAssetLease<GameObject>>();
                var pending = f.Prepare(new[] { "s" }, new[] { "f" });
                f.Call("SynchronizeOwner", new object(), 2L);
                f.Pending.SetResult(f.Lease()); await pending;
                Assert.That(f.Borrow("s"), Is.Null); Assert.That(f.Released, Is.EqualTo(1));
                Assert.That(f.Loads.Count, Is.EqualTo(1));
            }
        }
        [Test] public async Task RevisionChangeReusesOnlyMatchingContentAndReturnsOldLease()
        {
            using (var f = new Fixture())
            {
                await f.Prepare(new[] { "s" }, Array.Empty<string>());
                await f.Prepare(new[] { "s" }, Array.Empty<string>(), "r2");
                Assert.That(f.Borrow("s", "r1"), Is.Null);
                Assert.That(f.Released, Is.EqualTo(1));
                using (var lease = f.Borrow("s", "r2")) Assert.That(lease.Asset, Is.Not.Null);
            }
        }
        [Test] public async Task FormationReplacementStopsOldOptionalWorkEvenIfLoaderIgnoresCancellation()
        {
            using (var f = new Fixture())
            {
                f.Pending = new TaskCompletionSource<CombatAssetLease<GameObject>>();
                var old = f.Prepare(new[] { "old-summary" }, new[] { "old-full" });
                var oldSource = f.Pending; f.Pending = null;
                await f.Prepare(new[] { "new-summary" }, Array.Empty<string>());
                oldSource.SetResult(f.Lease()); await old;
                Assert.That(f.Loads, Is.EqualTo(new[] { "old-summary:r1", "new-summary:r1" }));
                Assert.That(f.Borrow("old-full"), Is.Null);
            }
        }
        [Test] public async Task CallerCancellationWithIgnoringLoaderCanBeExplicitlyRetried()
        {
            using (var f = new Fixture())
            using (var cancel = new CancellationTokenSource())
            {
                f.Pending = new TaskCompletionSource<CombatAssetLease<GameObject>>();
                var old = (Task)f.Call("PrepareAsync", new[] { "s" }, Array.Empty<string>(), "r1", (Func<string, bool>)(_ => true), cancel.Token);
                cancel.Cancel(); var oldSource = f.Pending; f.Pending = null;
                oldSource.SetResult(f.Lease()); await old;
                await f.Prepare(new[] { "s" }, Array.Empty<string>());
                using (var lease = f.Borrow("s")) Assert.That(lease, Is.Not.Null);
                Assert.That(f.Loads.Count, Is.EqualTo(2));
            }
        }
        [Test] public async Task PlaybackBorrowStopsViewBeforeReturningAssetAfterScopeExit()
        {
            using (var f = new Fixture())
            {
                await f.Prepare(new[] { "s" }, Array.Empty<string>());
                bool stopped = false;
                var method = f.Owner.GetType().GetMethod("PlayBorrowed");
                Assert.That(method, Is.Not.Null, "Playback must own its borrowed asset until its view stops.");
                var playback = (IDisposable)method.Invoke(f.Owner, new object[] { "s", "r1",
                    (Func<GameObject, IDisposable>)(asset => new TestLease(() => { Assert.That(f.Released, Is.Zero); stopped = true; })) });
                f.Call("SynchronizeOwner", new object(), 2L);
                playback.Dispose(); playback.Dispose();
                Assert.That(stopped, Is.True); Assert.That(f.Released, Is.EqualTo(1));
            }
        }
        sealed class TestLease : IDisposable
        {
            Action release;
            public TestLease(Action release) { this.release = release; }
            public void Dispose() { var old = release; release = null; old?.Invoke(); }
        }
        [Test] public async Task PreparedAssetsStayBorrowedThroughActualSessionAndSequenceCleanup()
        {
            using (var f = new Fixture())
            {
                await f.Prepare(new[] { "summary" }, new[] { "full" });
                Action end = null; int restored = 0, holds = 0;
                var sequence = new LastRail.Presentation.Battle.UltimatePresentationSequence(() => true,
                    () => { holds++; return new TestLease(() => holds--); }, e => Assert.Fail(e.ToString()));
                var play = f.Owner.GetType().GetMethod("PlayBorrowed");
                sequence.AcceptBatch(new[] { new LastRail.Presentation.Battle.UltimatePresentationEntry(1, "Fixture actor", hold =>
                    new LastRail.Presentation.Battle.UltimatePresentationSession("fixture-original-action", 1,
                        LastRail.Battle.CombatPresentationMode.Full, () => true,
                        () => (bool)f.Call("IsReady", "full", "r1"), () => (bool)f.Call("IsReady", "summary", "r1"),
                        () => true, () => { }, hold,
                        (kind, visible, ended, skip, fail) =>
                        {
                            end = ended;
                            return (IDisposable)play.Invoke(f.Owner, new object[] { "full", "r1",
                                (Func<GameObject, IDisposable>)(asset => new TestLease(() => restored++)) });
                        }, valid => { }, e => Assert.Fail(e.ToString()))) });
                f.Call("SynchronizeOwner", new object(), 2L);
                Assert.That(f.Released, Is.EqualTo(1)); Assert.That(holds, Is.EqualTo(1));
                end(); end(); sequence.Dispose();
                Assert.That(restored, Is.EqualTo(1)); Assert.That(f.Released, Is.EqualTo(2)); Assert.That(holds, Is.Zero);
            }
        }
        [Test] public async Task FailedPlaybackIsQuarantinedWithoutDestroyingItsActiveBorrow()
        {
            using (var f = new Fixture())
            {
                await f.Prepare(new[] { "s" }, new[] { "f" });
                var active = f.Borrow("f");
                var failure = f.Owner.GetType().GetMethod("CapturePlaybackFailure");
                Assert.That(failure, Is.Not.Null, "A ready asset that failed playback needs same-generation quarantine.");
                var onFailure = (Action<Exception>)failure.Invoke(f.Owner, new object[] { "f", "r1" });
                onFailure(new InvalidOperationException("decode fixture"));
                Assert.That(f.Borrow("f"), Is.Null); Assert.That(active.Asset, Is.Not.Null);
                Assert.That(f.Released, Is.Zero);
                active.Dispose(); Assert.That(f.Released, Is.EqualTo(1));
                await f.Prepare(new[] { "s" }, new[] { "f" });
                Assert.That(f.Loads.Count, Is.EqualTo(2)); // summary + standard; no failure retry.
                var lateFailure = (Action<Exception>)failure.Invoke(f.Owner, new object[] { "s", "r1" });
                f.Call("SynchronizeOwner", new object(), 2L);
                await f.Prepare(new[] { "s" }, Array.Empty<string>());
                lateFailure(new InvalidOperationException("late failure"));
                using (var summary = f.Borrow("s")) Assert.That(summary, Is.Not.Null);
            }
        }
    }
}
