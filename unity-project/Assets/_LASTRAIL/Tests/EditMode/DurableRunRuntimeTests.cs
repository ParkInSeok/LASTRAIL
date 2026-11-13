using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Presentation.Startup;
using LastRail.Resources;
using LastRail.Run;
using LastRail.Submissions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace LastRail.Tests.EditMode
{
    public sealed class DurableRunRuntimeTests
    {
        private sealed class Store : IGameApiAuthTokenStore, IGameApiAuthTokenReader
        {
            public Task<GameApiAuthSessionSnapshot> LoadAsync() => Task.FromResult<GameApiAuthSessionSnapshot>(null);
            public Task<GameApiAuthTokenReadResult> ReadAsync() => Task.FromResult(new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Missing));
            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot s) => Task.FromResult(GameApiAuthTokenStoreResult.Saved);
        }
        private sealed class Journal : IRunOperationJournal
        {
            public readonly List<PendingRunOperation> Rows = new List<PendingRunOperation>();
            public readonly List<string> Reads = new List<string>();
            public TaskCompletionSource<bool> Delay;
            public async Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account)
            {
                Reads.Add(account); var pending = Delay; Delay = null;
                if (pending != null) await pending.Task;
                return Rows.Where(r => r.AccountId == account).ToArray();
            }
            public Task<bool> PutAsync(PendingRunOperation op) => Task.FromResult(false);
            public Task<bool> ConfirmAsync(PendingRunOperation op, string json) => Task.FromResult(false);
            public Task<bool> RemoveConfirmedAsync(string account, PendingRunKind kind, string key) => Task.FromResult(false);
        }
        private sealed class Wire : IRunApiHttpTransport
        {
            public string Account = "account_test";
            public int RunPosts;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
            {
                if (r.EndpointPath.StartsWith("/v1/runs/")) RunPosts++;
                if (r.EndpointPath == "/v1/profile") return Task.FromResult(RunApiHttpResponse.Success(200,
                    "{\"accepted\":true,\"account\":{\"id\":\"" + Account + "\"},\"profile\":{\"displayName\":\"fixture\",\"commanderXp\":0}}"));
                if (r.EndpointPath == "/v1/story-progress") return Task.FromResult(RunApiHttpResponse.Success(200,
                    "{\"accepted\":true,\"actId\":\"act_01_wasteland_railway\",\"storyProgress\":{\"highestStoryProgressId\":\"none\",\"highestClearedOperationId\":null,\"order\":0},\"clearedOperationIds\":[],\"unlockedOperations\":[\"opn_01_last_departure\"],\"narrativeVersion\":\"fixture\",\"narrative\":{\"contentStatus\":\"CANDIDATE\",\"localizationStatus\":\"PENDING\",\"fixtureStatus\":\"PARTIAL\",\"missingContentStatus\":\"UNKNOWN\"}}"));
                return Task.FromResult(RunApiHttpResponse.NetworkError("Unexpected fixture request"));
            }
        }
        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root, Startup;
            public readonly Journal Journal = new Journal();
            public readonly Wire Wire = new Wire();
            public readonly PlayerEntryRuntime Runtime;
            public Fixture(bool productionDefault = false)
            {
                var ctor = typeof(PlayerEntryRuntime).GetConstructor(new[] { typeof(ResourceBootstrapController), typeof(TMP_FontAsset),
                    typeof(IRunApiHttpTransport), typeof(IGameApiAuthTokenStore), typeof(IGameApiAuthTokenReader), typeof(Func<AppBuildSnapshot>),
                    typeof(IPendingReceiptJournal), typeof(IRunOperationJournal) });
                Assert.That(ctor, Is.Not.Null, "Missing production run journal composition");
                Root = new GameObject("InactiveRunRuntimeFixture"); Root.SetActive(false);
                var bootstrap = Root.AddComponent<ResourceBootstrapController>();
                Startup = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
                Set(bootstrap, "startup", Startup.GetComponent<StartupScreenRootBehaviour>());
                Set(bootstrap, "access", new DownloadAccessPolicy());
                Set(bootstrap, "entry", new AuthenticatedScreenEntry(_ => Task.CompletedTask, () => { }));
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var store = new Store();
                Runtime = productionDefault ? new PlayerEntryRuntime(bootstrap, font, null)
                    : (PlayerEntryRuntime)ctor.Invoke(new object[] { bootstrap, font, Wire, store, store,
                        (Func<AppBuildSnapshot>)(() => new AppBuildSnapshot("current_C", 30)), null, Journal });
            }
            public object Runs => typeof(PlayerEntryRuntime).GetProperty("Runs").GetValue(Runtime);
            public IEnumerable<PendingRunOperation> Operations => (IEnumerable<PendingRunOperation>)Runs.GetType().GetProperty("Operations").GetValue(Runs);
            public async Task Login(string account)
            { Wire.Account = account; await Runtime.Owner.LoginAsync(() => Task.FromResult(SupportInquiryTests.Session(account))); Assert.That(Runtime.Owner.Ready, Is.True); }
            public void Dispose() { Runtime.Dispose(); UnityEngine.Object.DestroyImmediate(Startup); UnityEngine.Object.DestroyImmediate(Root); }
        }
        private static PendingRunOperation Record(string account) => PendingRunOperation.Start(account,
            RunStartRequest.Create("preview_fixture", "original_runtime_run_key"), new AppBuildSnapshot("original_B", 20), DateTimeOffset.UtcNow);
        [Test] public void DefaultCompositionOwnsSecureRunClientWithoutSending()
        {
            using (var f = new Fixture(true)) Assert.That(f.Runs, Is.Not.Null);
        }
        [Test] public async Task ReadyRestoresCurrentAccountOriginalOperationsWithoutPosting()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record("account_test")); f.Journal.Rows.Add(Record("account_other"));
                await f.Login("account_test");
                for (var i = 0; i < 100 && !f.Operations.Any(); i++) await Task.Yield();
                Assert.That(f.Operations.Count(), Is.EqualTo(1));
                Assert.That(f.Operations.Single().Build.AppVersion, Is.EqualTo("original_B"));
                Assert.That(f.Wire.RunPosts, Is.Zero);
            }
        }
        [Test] public async Task NewReadyWaitsForOldReadAndRestoresReplacementAccountOnly()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record("account_test")); f.Journal.Rows.Add(Record("account_other"));
                var delay = new TaskCompletionSource<bool>(); f.Journal.Delay = delay;
                await f.Login("account_test"); await f.Login("account_other");
                delay.SetResult(true);
                for (var i = 0; i < 100 && (!f.Operations.Any() || f.Operations.Any(r => r.AccountId != "account_other")); i++) await Task.Yield();
                Assert.That(f.Journal.Reads, Does.Contain("account_other"));
                Assert.That(f.Operations.Select(r => r.AccountId), Is.EqualTo(new[] { "account_other" }));
                Assert.That(f.Wire.RunPosts, Is.Zero);
            }
        }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
