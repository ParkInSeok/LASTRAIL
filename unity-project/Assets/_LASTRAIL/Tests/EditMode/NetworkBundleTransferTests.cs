using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class NetworkBundleTransferTests
    {
        [Serializable] public sealed class Request { public string path; public long bytes; public bool aborted; }
        [Serializable] public sealed class Stats { public Request[] requests; }
        [Serializable] public sealed class Evidence
        {
            public string source = "Real loopback built bundles; auth/network injected fixtures, not Android transport or real authentication";
            public string release, hash, state;
            public long coldBytes, remainingAfterAbort, cachedBytes;
            public int beforeConsentRequests, afterAbortRequests, afterResumeRequests, afterLoadRequests;
            public bool invalidCachedCrcRejected;
            public int cacheFaultExtraRequests;
            public Stats transport;
        }

        [UnityTest]
        public IEnumerator ConsentAbortResumeAndCacheOnlyLoadsUseActualTransport()
        {
            if (Environment.GetEnvironmentVariable("LASTRAIL_NETWORK_BUNDLE_QA") != "1") Assert.Ignore("Requires explicit throttled loopback server and Windows content build.");
            yield return new EnterPlayMode();
            var task = RunAsync(); while (!task.IsCompleted) yield return null;
            var failure = task.Exception;
            yield return new ExitPlayMode();
            if (failure != null) throw failure;
        }

        private static async Task<Stats> StatsAsync()
        {
            using var request = UnityWebRequest.Get("http://127.0.0.1:18284/__stats");
            request.timeout = 10; request.redirectLimit = 0; var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success));
            return JsonUtility.FromJson<Stats>(request.downloadHandler.text);
        }

        private static async Task RunAsync()
        {
            var logs = LogAssert.ignoreFailingMessages;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(150));
            GameObject root = null;
            try
            {
                var policy = new DownloadAccessPolicy(); policy.SetNetwork(ContentNetwork.OtherOnline);
                using var fixture = new LoopbackBundleQa(policy);
                var backend = fixture.Backend;
                await fixture.PrepareColdAsync(timeout.Token);
                var evidence = new Evidence { release = fixture.Gate.ApprovedReleaseId, hash = fixture.Gate.ApprovedCatalogHash, state = fixture.StateDirectory };
                var before = (await StatsAsync()).requests.Length;
                evidence.coldBytes = await backend.RequiredBytesAsync(timeout.Token);
                Assert.That(evidence.coldBytes, Is.GreaterThan(0));
                LogAssert.ignoreFailingMessages = true;
                var missingRejected = false;
                try { await backend.VerifyRequiredAssetsAsync(timeout.Token); }
                catch (InvalidOperationException) { missingRejected = true; }
                Assert.That(missingRejected, Is.True);
                LogAssert.ignoreFailingMessages = logs;
                evidence.beforeConsentRequests = (await StatsAsync()).requests.Length - before;
                Assert.That(evidence.beforeConsentRequests, Is.Zero, "Metadata and uncached Load must not fetch bundles");
                var owner = new DownloadCoordinator(backend, policy);
                var switched = false;
                owner.Changed += () =>
                {
                    if (!switched && owner.ReceivedBytes > 65536)
                    {
                        switched = true; policy.SetNetwork(ContentNetwork.OtherOnline);
                    }
                };
                var run = owner.StartAsync("0.1.0", timeout.Token);
                while (owner.Stage != ContentStage.Downloading && !run.IsCompleted) { timeout.Token.ThrowIfCancellationRequested(); await Task.Yield(); }
                Assert.That(policy.NeedsConsent, Is.True);
                Assert.That((await StatsAsync()).requests.Length - before, Is.Zero);
                policy.Decline(); Assert.That(owner.CanEnterGame, Is.False); policy.Retry();
                policy.SetNetwork(ContentNetwork.Wifi);
                while (!switched && !run.IsCompleted) { timeout.Token.ThrowIfCancellationRequested(); await Task.Yield(); }
                await Task.Delay(500, timeout.Token);
                evidence.afterAbortRequests = (await StatsAsync()).requests.Length - before;
                evidence.remainingAfterAbort = policy.RemainingBytes;
                Assert.That(evidence.afterAbortRequests, Is.EqualTo(1), "Abort must prevent subsequent bundle requests");
                Assert.That(policy.NeedsConsent, Is.False, "No transition modal during login");
                Assert.That(policy.AwaitingNonWifiConsent, Is.True);
                policy.SetAuthenticated(false); Assert.That(policy.NeedsConsent, Is.False);
                policy.SetAuthenticated(true); Assert.That(policy.NeedsConsent, Is.True);
                Assert.That(owner.StartAsync("0.1.0", timeout.Token), Is.SameAs(run));
                policy.Approve(); await run;
                Assert.That(owner.Stage, Is.EqualTo(ContentStage.Ready), owner.Failure?.ToString());
                backend.CompletePendingRelease();
                evidence.cachedBytes = await backend.RequiredBytesAsync(timeout.Token);
                Assert.That(evidence.cachedBytes, Is.Zero);
                evidence.afterResumeRequests = (await StatsAsync()).requests.Length - before;
                root = new GameObject("NetworkBundleHost"); var host = root.AddComponent<AddressableScreenHost>();
                foreach (var path in Directory.GetFiles("Assets/_LASTRAIL/Prefabs/Screens", "*.prefab").Where(p => !p.EndsWith("StartupScreen.prefab")))
                    Assert.That(await host.OpenAsync(backend.ResolveApprovedScreen(path.Replace('\\', '/')), timeout.Token), Is.Not.Null);
                host.enabled = false; Assert.That(host.Current, Is.Null);
                evidence.transport = await StatsAsync(); evidence.afterLoadRequests = evidence.transport.requests.Length - before;
                Assert.That(evidence.afterLoadRequests, Is.EqualTo(evidence.afterResumeRequests), "Cache-only screen consumption must not fetch");
                Assert.That(evidence.transport.requests.Skip(before).Any(r => r.aborted), Is.True);
                await Task.Delay(100, timeout.Token);
                {
                    var options = BundleOptions(backend.ResolveApprovedScreen(AddressableContentBackend.LobbyKey));
                    Assert.That(options, Is.Not.Null);
                    var crc = options.Crc; var useCrc = options.UseCrcForCachedBundle;
                    LogAssert.ignoreFailingMessages = true;
                    try
                    {
                        options.Crc ^= uint.MaxValue; options.UseCrcForCachedBundle = true;
                        try { await backend.VerifyRequiredAssetsAsync(timeout.Token); }
                        catch (InvalidOperationException) { evidence.invalidCachedCrcRejected = true; }
                    }
                    finally { options.Crc = crc; options.UseCrcForCachedBundle = useCrc; LogAssert.ignoreFailingMessages = logs; }
                    evidence.cacheFaultExtraRequests = (await StatsAsync()).requests.Length - before - evidence.afterLoadRequests;
                    Assert.That(evidence.invalidCachedCrcRejected, Is.True);
                    Assert.That(evidence.cacheFaultExtraRequests, Is.Zero, "Cache validation failure cannot silently redownload");
                }
                Directory.CreateDirectory("QA/Act1Launch/Addressables");
                File.WriteAllText("QA/Act1Launch/Addressables/network-transfer-qa.json", JsonUtility.ToJson(evidence, true));
            }
            finally
            {
                timeout.Cancel(); if (root != null) UnityEngine.Object.DestroyImmediate(root);
                LogAssert.ignoreFailingMessages = logs;
            }
        }

        private static AssetBundleRequestOptions BundleOptions(IResourceLocation location)
        {
            if (location.Data is AssetBundleRequestOptions options) return options;
            if (location.HasDependencies)
                foreach (var dependency in location.Dependencies)
                {
                    var result = BundleOptions(dependency); if (result != null) return result;
                }
            return null;
        }
    }
}
