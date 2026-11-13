#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class BuiltBundleResourceTests
    {
        [UnityTest]
        public IEnumerator RealHttpBundlesPrepareCacheAndInstantiateExistingScreens()
        {
            if (Environment.GetEnvironmentVariable("LASTRAIL_BUNDLE_QA") != "1")
                Assert.Ignore("Explicit built-content HTTP QA requires local server; not an AssetDatabase test.");
            yield return new EnterPlayMode();
            var run = RunBundlesAsync();
            while (!run.IsCompleted) yield return null;
            var failure = run.Exception;
            yield return new ExitPlayMode();
            if (failure != null) throw failure;
        }

        private static async Task RunBundlesAsync()
        {
            var oldLogPolicy = LogAssert.ignoreFailingMessages;
            var root = new GameObject("BuiltBundleScreenHost");
            var host = root.AddComponent<AddressableScreenHost>();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            try
            {
                var policy = new DownloadAccessPolicy(); policy.SetNetwork(ContentNetwork.Wifi);
                using var fixture = new LoopbackBundleQa(policy);
                var backend = fixture.Backend;
                await fixture.PrepareColdAsync(timeout.Token);
                var flow = new ContentStartupFlow();
                var confirmedBytes = 0L;
                await flow.RunAsync(backend, "0.1.0", (bytes, c) => { confirmedBytes = bytes; return Task.FromResult(true); }, timeout.Token);
                Assert.That(flow.Stage, Is.EqualTo(ContentStage.Ready), flow.Failure?.ToString());
                Assert.That(confirmedBytes, Is.GreaterThan(0));
                backend.CompletePendingRelease();
                var cached = await backend.RequiredBytesAsync(timeout.Token);
                Assert.That(cached, Is.EqualTo(0));
                await flow.RunAsync(backend, "0.1.0", (b, c) => throw new Exception("Cached run must not request data"), timeout.Token);
                Assert.That(flow.Stage, Is.EqualTo(ContentStage.Ready), flow.Failure?.ToString());
                backend.CompletePendingRelease();
                var paths = Directory.GetFiles("Assets/_LASTRAIL/Prefabs/Screens", "*.prefab")
                    .Select(p => p.Replace('\\', '/')).Where(p => !p.EndsWith("StartupScreen.prefab")).OrderBy(p => p).ToArray();
                foreach (var path in paths)
                {
                    var instance = await host.OpenAsync(backend.ResolveApprovedScreen(path), timeout.Token);
                    Assert.That(instance, Is.Not.Null, path);
                    Assert.That(host.Current, Is.SameAs(instance));
                    foreach (var image in instance.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                        if (image.sprite != null) Assert.That(image.sprite.texture, Is.Not.Null, image.name);
                }
                using (var cancelled = new CancellationTokenSource())
                {
                    cancelled.Cancel();
                    Assert.CatchAsync<OperationCanceledException>(async () => await host.OpenAsync(backend.ResolveApprovedScreen(AddressableContentBackend.LobbyKey), cancelled.Token));
                }
                LogAssert.ignoreFailingMessages = true;
                Assert.Throws<InvalidOperationException>(() => backend.ResolveApprovedScreen("missing-resource-qa-key"));
                host.enabled = false;
                Assert.That(host.Current, Is.Null);
                using var missing = new LoopbackBundleQa(policy, "/missing-config.json");
                await flow.RunAsync(missing.Backend, "0.1.0", (b, c) => Task.FromResult(true), timeout.Token);
                Assert.That(flow.Stage, Is.EqualTo(ContentStage.Failed));
                Assert.That(flow.CanEnterGame, Is.False);
                using var rejected = new LoopbackBundleQa(policy, "/rejected-config.json");
                await flow.RunAsync(rejected.Backend, "0.1.0", (b, c) => Task.FromResult(true), timeout.Token);
                Assert.That(flow.Stage, Is.EqualTo(ContentStage.Failed), "Actual server rejection must fail closed");
                Directory.CreateDirectory("QA/Act1Launch/Addressables");
                File.WriteAllText("QA/Act1Launch/Addressables/built-bundle-qa.json", JsonUtility.ToJson(new Report
                    { source = "built catalog + strict server admission + real loopback HTTP", release = fixture.Gate.ApprovedReleaseId,
                        hash = fixture.Gate.ApprovedCatalogHash, state = fixture.StateDirectory,
                        confirmedBytes = confirmedBytes, cachedRequiredBytes = cached, loadedScreens = paths }, true));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                LogAssert.ignoreFailingMessages = oldLogPolicy;
            }
        }


        [Serializable] private sealed class Report { public string source, release, hash, state; public long confirmedBytes, cachedRequiredBytes; public string[] loadedScreens; }
    }
}
#endif
