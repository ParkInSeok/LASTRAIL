#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class BackendContentDeltaHttpTests
    {
        private const string Origin = "http://127.0.0.1:18286";
        [UnityTest]
        public IEnumerator FreshProcessUsesExistingApprovalPinDownloadAndLobbyGates()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LASTRAIL_BACKEND_DELTA_ROOT")))
                Assert.Ignore("Requires explicit fresh-process backend delta fixture phase.");
            yield return new EnterPlayMode();
            var run = RunAsync();
            while (!run.IsCompleted) yield return null;
            var error = run.Exception;
            yield return new ExitPlayMode();
            if (error != null) throw error;
        }
        private static async Task RunAsync()
        {
            var root = Environment.GetEnvironmentVariable("LASTRAIL_BACKEND_DELTA_ROOT");
            var phase = Environment.GetEnvironmentVariable("LASTRAIL_BACKEND_DELTA_PHASE");
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(phase)) Assert.Ignore("Requires explicit fresh-process backend delta fixture phase.");
            Assert.That(new[] { "pendingA", "switchB", "supportedA", "failureB" }, Does.Contain(phase));
            var fixture = JObject.Parse(File.ReadAllText(Path.Combine(root, "fixture.json")));
            Assert.That((string)fixture["platform"], Is.EqualTo("StandaloneWindows64"));
            var state = Path.Combine(root, "session-state");
            var pin = Path.Combine(state, "pending-content-release.json");
            var selected = phase == "pendingA" || phase == "supportedA" ? "A" : phase == "failureB" ? "badB" : "B";
            await Select(selected);
            var startStats = await Stats();
            var hadPreference = PlayerPrefs.HasKey(UnityEngine.AddressableAssets.Addressables.kAddressablesRuntimeDataPath);
            var preference = PlayerPrefs.GetString(UnityEngine.AddressableAssets.Addressables.kAddressablesRuntimeDataPath, "");
            var access = new DownloadAccessPolicy(); access.SetNetwork(ContentNetwork.OtherOnline);
            var type = typeof(AddressableContentBackend).Assembly.GetType("LastRail.Resources.EditorLoopbackContentFixture", true);
            var context = type.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] {
                Origin + "/client-config.json", Origin + "/LOCAL_TEST/StandaloneWindows64/",
                Path.Combine(root, "installed/settings.json"), state, "42" });
            using (var backend = (AddressableContentBackend)typeof(AddressableContentBackend)
                .GetMethod("CreateForEditorLoopbackFixture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { context, access }))
            using (var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(90)))
            {
                var coordinator = new DownloadCoordinator(backend, access);
                Assert.That(coordinator.CanEnterGame, Is.False);
                if (phase == "switchB" || phase == "failureB")
                {
                    await coordinator.StartAsync(null, cancel.Token);
                    Assert.That(coordinator.Stage, Is.EqualTo(ContentStage.Failed));
                    Assert.That(coordinator.CanEnterGame, Is.False);
                    Assert.That(File.Exists(pin), Is.True);
                    if (phase == "switchB")
                    {
                        Assert.That(backend.PinConfirmationRequired, Is.True);
                        Assert.That(coordinator.ConfigAccepted, Is.False);
                        Assert.That(File.ReadAllText(pin), Does.Contain("synthetic_A"));
                    }
                    else
                    {
                        Assert.That(coordinator.Failure.Message, Does.Contain("hash"));
                        Assert.Throws<InvalidOperationException>(() => backend.ResolveApprovedScreen(AddressableContentBackend.LobbyKey));
                    }
                    Assert.That((long)(await Stats())["bundleRequests"], Is.EqualTo((long)startStats["bundleRequests"]));
                    await Select("B");
                    backend.ResetPendingRelease(); coordinator.InvalidateAdmission();
                }
                var operation = coordinator.StartAsync(null, cancel.Token);
                await Until(() => access.NeedsConsent || coordinator.Stage == ContentStage.AwaitingAuthentication || operation.IsCompleted, cancel.Token);
                Assert.That(coordinator.Failure, Is.Null, coordinator.Failure?.ToString());
                Assert.That(coordinator.CanEnterGame, Is.False);
                var remaining = coordinator.TotalBytes;
                if (phase == "pendingA" || phase == "switchB")
                {
                    Assert.That(remaining, Is.GreaterThan(0));
                    Assert.That(access.NeedsConsent, Is.True);
                    Assert.That((long)(await Stats())["bundleRequests"], Is.EqualTo((long)startStats["bundleRequests"]));
                    Assert.Throws<InvalidOperationException>(() => backend.ResolveApprovedScreen(AddressableContentBackend.LobbyKey));
                    access.Approve();
                }
                else Assert.That(remaining, Is.Zero, "A supported cached release must not download again.");
                await Until(() => coordinator.Stage == ContentStage.AwaitingAuthentication || operation.IsCompleted, cancel.Token);
                Assert.That(coordinator.Stage, Is.EqualTo(ContentStage.AwaitingAuthentication), coordinator.Failure?.ToString());
                Assert.That(coordinator.CanEnterGame, Is.False);
                if (phase == "pendingA")
                {
                    cancel.Cancel(); await operation;
                    Assert.That(coordinator.Stage, Is.EqualTo(ContentStage.Cancelled));
                    Assert.That(File.ReadAllText(pin), Does.Contain("synthetic_A"));
                }
                else
                {
                    access.SetAuthenticated(true); await operation;
                    Assert.That(coordinator.Stage, Is.EqualTo(ContentStage.Ready), coordinator.Failure?.ToString());
                    Assert.That(coordinator.CanEnterGame, Is.True);
                    var hostObject = new GameObject("SyntheticGateHost");
                    var host = hostObject.AddComponent<AddressableScreenHost>();
                    try
                    {
                        var entry = new AuthenticatedScreenEntry(async token => {
                            if (!coordinator.CanEnterGame) throw new InvalidOperationException("Content/authentication gate is closed.");
                            await host.OpenAsync(backend.ResolveApprovedScreen(AddressableContentBackend.LobbyKey), token);
                            backend.CompletePendingRelease();
                        }, host.Close);
                        Assert.That(await entry.EnterAsync(cancel.Token), Is.True);
                        Assert.That(await entry.EnterAsync(cancel.Token), Is.False);
                        Assert.That(host.Current.transform.GetChild(0).name, Is.EqualTo(phase == "supportedA" ? "synthetic-release-A" : "synthetic-release-B-changed"));
                        Assert.That(File.Exists(pin), Is.False);
                        if (phase == "switchB")
                        {
                            var approvedRevision = backend.ContentRevision;
                            var configCount = (long)(await Stats())["configRequests"];
                            await Select("A"); await coordinator.StartAsync(null, cancel.Token);
                            Assert.That(backend.ContentRevision, Is.EqualTo(approvedRevision));
                            Assert.That((long)(await Stats())["configRequests"], Is.EqualTo(configCount));
                            Assert.That(host.Current.transform.GetChild(0).name, Is.EqualTo("synthetic-release-B-changed"));
                        }
                        entry.Reset();
                    }
                    finally { UnityEngine.Object.DestroyImmediate(hostObject); }
                }
                var endStats = await Stats();
                var transfers = (long)endStats["bundleRequests"] - (long)startStats["bundleRequests"];
                var bytes = (long)endStats["bundleBytes"] - (long)startStats["bundleBytes"];
                Assert.That(transfers, Is.EqualTo(phase == "pendingA" || phase == "switchB" ? 1 : 0));
                Assert.That(bytes, Is.EqualTo(remaining));
                Assert.That(PlayerPrefs.HasKey(UnityEngine.AddressableAssets.Addressables.kAddressablesRuntimeDataPath), Is.EqualTo(hadPreference));
                Assert.That(PlayerPrefs.GetString(UnityEngine.AddressableAssets.Addressables.kAddressablesRuntimeDataPath, ""), Is.EqualTo(preference));
                Assert.That(SyntheticContentDeltaQa.Hash(Path.Combine(root, "archives/full_A/manifest.json")), Is.EqualTo((string)fixture["baselineManifestSha256"]));
                TestContext.WriteLine("BACKEND_DELTA_HTTP_OK phase=" + phase + " platform=StandaloneWindows64 remaining=" + remaining
                    + " bundleRequests=" + transfers + " bundleBytes=" + bytes + " stage=" + coordinator.Stage + " pin=" + File.Exists(pin));
            }
        }
        private static async Task Until(Func<bool> condition, CancellationToken cancel)
        { while (!condition()) { cancel.ThrowIfCancellationRequested(); await Task.Yield(); } }
        private static async Task Select(string value)
        { using (var request = new UnityWebRequest(Origin + "/__qa/select/" + value, "POST") { downloadHandler = new DownloadHandlerBuffer() }) await Send(request); }
        private static async Task<JObject> Stats()
        { using (var request = UnityWebRequest.Get(Origin + "/stats")) { await Send(request); return JObject.Parse(request.downloadHandler.text); } }
        private static async Task Send(UnityWebRequest request)
        {
            request.timeout = 15; request.redirectLimit = 0; var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
            Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), request.error);
        }
    }
}
#endif
