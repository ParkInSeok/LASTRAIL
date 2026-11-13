using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class ContentStartupFlowTests
    {
        private sealed class Backend : IContentStartupBackend
        {
            public long Bytes = 42;
            public string FailAt;
            public ClientContentGate Gate = new ClientContentGate { MinimumVersion = "0.1.0" };
            public readonly List<string> Calls = new List<string>();
            public TaskCompletionSource<bool> DownloadHold;
            private Task Step(string step) { Calls.Add(step); if (FailAt == step) throw new InvalidOperationException(step); return Task.CompletedTask; }
            public async Task<ClientContentGate> FetchConfigAsync(CancellationToken c) { await Step("config"); return Gate; }
            public Task InitializeAsync(CancellationToken c) => Step("init");
            public Task UpdateCatalogAsync(CancellationToken c) => Step("catalog");
            public async Task<long> RequiredBytesAsync(CancellationToken c) { await Step("size"); return Bytes; }
            public async Task DownloadAsync(Action<long, long> progress, CancellationToken c)
            {
                progress(Bytes, Bytes);
                if (DownloadHold != null) await DownloadHold.Task;
                await Step("download");
            }
            public Task VerifyRequiredAssetsAsync(CancellationToken c) => Step("load");
        }

        [Test]
        public async Task ColdStartRequiresConsentThenSuccessfulAssetLoad()
        {
            var backend = new Backend(); var flow = new ContentStartupFlow(); var asked = false;
            flow.Progress += (r, t) => Assert.That(flow.CanEnterGame, Is.False, "100% bytes is not success");
            await flow.RunAsync(backend, "0.1.0", (b, c) => { asked = true; return Task.FromResult(true); }, CancellationToken.None);
            Assert.That(asked, Is.True); Assert.That(flow.CanEnterGame, Is.True);
            Assert.That(backend.Calls, Is.EqualTo(new[] { "config", "init", "catalog", "size", "download", "load" }));
        }

        [TestCase("config")][TestCase("init")][TestCase("catalog")][TestCase("size")][TestCase("download")][TestCase("load")]
        public async Task FailureAtEveryStageBlocksEntryAndCanRetry(string stage)
        {
            var backend = new Backend { FailAt = stage }; var flow = new ContentStartupFlow();
            await flow.RunAsync(backend, "0.1.0", (b, c) => Task.FromResult(true), CancellationToken.None);
            Assert.That(flow.Stage, Is.EqualTo(ContentStage.Failed)); Assert.That(flow.CanEnterGame, Is.False);
            backend.FailAt = null;
            await flow.RunAsync(backend, "0.1.0", (b, c) => Task.FromResult(true), CancellationToken.None);
            Assert.That(flow.Stage, Is.EqualTo(ContentStage.Ready));
        }

        [Test]
        public async Task CachedStartStillChecksCatalogAndLoadsAssets()
        {
            var backend = new Backend { Bytes = 0 }; var flow = new ContentStartupFlow();
            await flow.RunAsync(backend, "0.1.0", (b, c) => throw new Exception("No consent for zero bytes"), CancellationToken.None);
            Assert.That(flow.CanEnterGame, Is.True); Assert.That(backend.Calls, Does.Not.Contain("download"));
            Assert.That(backend.Calls, Does.Contain("catalog")); Assert.That(backend.Calls, Does.Contain("load"));
        }

        [Test]
        public async Task DeclinedDownloadDoesNotLoadOrEnter()
        {
            var backend = new Backend(); var flow = new ContentStartupFlow();
            await flow.RunAsync(backend, "0.1.0", (b, c) => Task.FromResult(false), CancellationToken.None);
            Assert.That(flow.Stage, Is.EqualTo(ContentStage.Cancelled)); Assert.That(backend.Calls, Does.Not.Contain("load"));
        }

        [Test]
        public async Task CancellationAndDuplicateRequestCannotEnterAfterLateCompletion()
        {
            var backend = new Backend { DownloadHold = new TaskCompletionSource<bool>() }; var flow = new ContentStartupFlow();
            using (var cancel = new CancellationTokenSource())
            {
                var run = flow.RunAsync(backend, "0.1.0", (b, c) => Task.FromResult(true), cancel.Token);
                Assert.ThrowsAsync<InvalidOperationException>(async () => await flow.RunAsync(backend, "0.1.0", (b, c) => Task.FromResult(true), cancel.Token));
                cancel.Cancel(); backend.DownloadHold.SetResult(true); await run;
                Assert.That(flow.Stage, Is.EqualTo(ContentStage.Cancelled)); Assert.That(backend.Calls, Does.Not.Contain("load"));
            }
        }

        [TestCase(true, "0.1.0", ContentStage.Maintenance)]
        [TestCase(false, "9.0.0", ContentStage.UpdateRequired)]
        public async Task ConfigGateStopsBeforeAddressables(bool maintenance, string minimum, ContentStage expected)
        {
            var backend = new Backend { Gate = new ClientContentGate { Maintenance = maintenance, MinimumVersion = minimum } };
            var flow = new ContentStartupFlow();
            await flow.RunAsync(backend, "0.1.0", (b, c) => Task.FromResult(true), CancellationToken.None);
            Assert.That(flow.Stage, Is.EqualTo(expected)); Assert.That(backend.Calls, Is.EqualTo(new[] { "config" }));
        }
    }
}
