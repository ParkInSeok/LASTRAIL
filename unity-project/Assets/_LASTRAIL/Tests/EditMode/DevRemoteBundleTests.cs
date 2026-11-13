#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class DevRemoteBundleTests
    {
        const string Root = "https://dev-content.lastrail.app/windowsqa/releases/pc-resources-20261111-v02/";
        const string Output = "D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/dev-bundle-20261111/v02";
        static object Call(Type type, object target, string name, params object[] args)
        {
            try { return type.GetMethod(name, BindingFlags.NonPublic | (target == null ? BindingFlags.Static : BindingFlags.Instance)).Invoke(target, args); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        static Type Fixture => typeof(AddressableContentBackend).Assembly.GetType("LastRail.Resources.EditorLoopbackContentFixture", true);
        sealed class NoRequestWire : LastRail.Run.IRunApiHttpTransport
        {
            public int Calls;
            public Task<LastRail.Run.RunApiHttpResponse> SendAsync(LastRail.Run.RunApiHttpRequest request){Calls++;throw new Exception("Unexpected request");}
        }
        [TestCase("/v1/runs/start")]
        [TestCase("/v1/recurring/bulk-claims")]
        [TestCase("/v1/wallet/crystals/tickets/exchange")]
        public void RealLoginProbeRejectsEconomicCommandsBeforeSending(string path)
        {
            var type=typeof(DevServerLoginProbe);var wire=new NoRequestWire();
            var report=Activator.CreateInstance(type.GetNestedType("Report",BindingFlags.NonPublic),true);
            var recording=(LastRail.Run.IRunApiHttpTransport)Activator.CreateInstance(type.GetNestedType("RecordedWire",BindingFlags.NonPublic),wire,report,Output);
            Assert.ThrowsAsync<InvalidOperationException>(async()=>await recording.SendAsync(new LastRail.Run.RunApiHttpRequest("POST",path,"{}",null)));
            Assert.That(wire.Calls,Is.Zero);
        }
        [TestCase("/v1/recurring/login")]
        [TestCase("/v1/recurring/sync")]
        public void RealLoginProbePermitsExistingLoginBookkeeping(string path)
        {
            var type=typeof(DevServerLoginProbe);var wire=new NoRequestWire();
            var report=Activator.CreateInstance(type.GetNestedType("Report",BindingFlags.NonPublic),true);
            var recording=(LastRail.Run.IRunApiHttpTransport)Activator.CreateInstance(type.GetNestedType("RecordedWire",BindingFlags.NonPublic),wire,report,Output);
            var error=Assert.ThrowsAsync<Exception>(async()=>await recording.SendAsync(new LastRail.Run.RunApiHttpRequest("POST",path,null,null)));
            Assert.That(error.Message,Is.EqualTo("Unexpected request"));Assert.That(wire.Calls,Is.EqualTo(1));
        }
        [Test] public void EditorLoginJournalPreservesPendingOwnerAndRejectsEconomicActions()
        {
            var type=typeof(DevServerLoginProbe).GetNestedType("LoginJournal",BindingFlags.NonPublic);
            var journal=(IAccountActionJournal)Activator.CreateInstance(type,true);
            var action=new PendingAccountAction("qa_account","recurring-login","login",0,"qa-login-key");
            Assert.That(journal.Save(new PendingAccountAction("qa_account","offline","",0,"qa_offline")),Is.False);
            Assert.That(journal.Save(action),Is.True);Assert.That(journal.Save(action),Is.False);
            Assert.That(journal.Read("other_account"),Is.Null);Assert.That(journal.Read("qa_account"),Is.SameAs(action));
            Assert.That(journal.Remove(new PendingAccountAction("qa_account","recurring-login","login",0,"qa-login-key")),Is.False);
            Assert.That(journal.Remove(action),Is.True);Assert.That(journal.Read("qa_account"),Is.Null);
        }
        [Test] public void RealDevProbeUsesRealEndpointAndRejectsPrdContent()
        {
            var context=Call(Fixture,null,"CreateDevServerProbe",Path.Combine(Output,"built/settings.json"),Path.Combine(Output,"real-server-state"));
            var url=Fixture.GetProperty("ConfigUrl",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(context);
            Assert.That(url,Is.EqualTo("https://dev-api.lastrail.app/v1/client-config"));
            Assert.That(Fixture.GetProperty("InstalledVersionCode",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(context),Is.EqualTo("45"),"Admission and authentication must use the same new DEV QA version.");
            Assert.DoesNotThrow(()=>Call(Fixture,context,"ValidateContentUrl","https://dev-content.lastrail.app/windowsqa/releases/pc-formation-20261112-v02/catalog_0.1.0.bin"));
            Assert.Throws<InvalidOperationException>(()=>Call(Fixture,context,"ValidateContentUrl",Root+"catalog_0.1.0.bin"));
            Assert.Throws<InvalidOperationException>(()=>Call(Fixture,context,"ValidateContentUrl","https://content.lastrail.app/catalog.bin"));
        }
        [Test] public void ContentHeaderPinIsClearedWithAdmissionOwner()
        {
            using(var backend=new AddressableContentBackend("https://dev-api.lastrail.app/v1/client-config",true)){
                var gate=typeof(ClientContentGate).GetMethod("Approved",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"approved-release","version",Root+"catalog.bin","sha256:"+new string('a',64),false});
                typeof(AddressableContentBackend).GetField("approvedGate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(backend,gate);
                Assert.That(backend.ApprovedContentReleaseId,Is.EqualTo("approved-release"));
                backend.InvalidateSessionAuthorization();Assert.That(backend.ApprovedContentReleaseId,Is.Null);
                typeof(AddressableContentBackend).GetField("approvedGate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(backend,gate);
                typeof(AddressableContentBackend).GetField("sessionBoundGate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(backend,true);
                Assert.That(backend.ApprovedContentReleaseId,Is.Null,"A stale session must not supply a pin.");
                backend.Dispose();Assert.That(backend.ApprovedContentReleaseId,Is.Null);
            }
        }
        static object Context() => Call(Fixture, null, "CreateDevProbe", "http://127.0.0.1:18285/client-config.json",
            Path.Combine(Output, "built/settings.json"), Path.Combine(Output, "state/" + Guid.NewGuid().ToString("N")), "42");

        [TestCase("https://content.lastrail.app/windowsqa/releases/pc-resources-20261111-v02/a.bundle")]
        [TestCase(Root + "../a.bundle")]
        [TestCase(Root + "%2e%2e/a.bundle")]
        [TestCase(Root + "a.bundle?token=x")]
        [TestCase(Root + "a.bundle#fragment")]
        [TestCase(Root + "a//b.bundle")]
        [TestCase(Root)]
        public void ExplicitDevProbeRejectsUnapprovedUrls(string url) =>
            Assert.Throws<InvalidOperationException>(() => Call(Fixture, Context(), "ValidateContentUrl", url));

        [TestCase("ui_assets_0123.bundle")]
        [TestCase("remote_resources_assets_assets_textmeshpro_resources_fonts&materials_liberationsanssdf-outline.mat_34f7cd71787632bc8450fe3838982287.bundle")]
        public void ExplicitDevProbeAcceptsOnlyFixedRelease(string file)
        {
            var context = Context();
            Assert.DoesNotThrow(() => Call(Fixture, context, "ValidateContentUrl", Root + file));
            Assert.Throws<InvalidOperationException>(() => Call(Fixture, null, "CreateDevProbe", "https://api.lastrail.app/v1/client-config",
                Path.Combine(Output, "built/settings.json"), Path.Combine(Output, "state"), "42"));
        }

        [UnityTest, Timeout(1800000)] public IEnumerator ActualDevDownloadCacheAndResourceLifetimes()
        {
            if (Environment.GetEnvironmentVariable("LASTRAIL_DEV_BUNDLE_QA") != "1")
                Assert.Ignore("Explicit DEV Windows QA only; requires built settings and TEST_ONLY admission server.");
            yield return new EnterPlayMode();
            var task = Run();
            while (!task.IsCompleted) yield return null;
            var error = task.Exception;
            yield return new ExitPlayMode();
            if (error != null) throw error;
        }

        [Serializable] sealed class Report
        {
            public string source = "Real DEV HTTPS Windows bundles; TEST_ONLY loopback admission, no DEV login or Android registry";
            public string release, hash, cachePath, state;
            public long plannedDownloadBytes, progressBytes, cachedRequiredBytes;
            public int cachedRunProgressEvents, preparedTextures, texturesRetainedByExternalOwners;
            public bool publicArtworkCancelledReenteredReleased;
            public bool sharedScopePreservedTexture, equipmentSharedLease, equipmentDestroyedAfterLastLease;
        }
        static async Task Run()
        {
            Directory.CreateDirectory(Output);
            var run = Guid.NewGuid().ToString("N");
            var report = new Report { cachePath = Path.Combine(Output, "cache", run), state = Path.Combine(Output, "state", run) };
            Directory.CreateDirectory(report.cachePath);
            var priorCache = Caching.currentCacheForWriting;
            var cache = Caching.AddCache(report.cachePath);
            Assert.That(cache.valid, Is.True);
            Caching.currentCacheForWriting = cache;
            var editorAdapter = PreparedContent.EditorAsset;
            PreparedContent.EditorAsset = null;
            var hostObject = new GameObject("DEV bundle QA screen host");
            var host = hostObject.AddComponent<AddressableScreenHost>();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(30));
            try
            {
                var settings = Environment.GetEnvironmentVariable("LASTRAIL_DEV_FIXTURE_SETTINGS");
                Assert.That(File.Exists(settings), Is.True, "Set LASTRAIL_DEV_FIXTURE_SETTINGS to packed Windows settings.json");
                var context = Call(Fixture, null, "CreateDevProbe", "http://127.0.0.1:18285/client-config.json", settings, report.state, "42");
                var policy = new DownloadAccessPolicy(); policy.SetNetwork(ContentNetwork.Wifi);
                using var backend = (AddressableContentBackend)Call(typeof(AddressableContentBackend), null,
                    "CreateForEditorLoopbackFixture", context, policy);
                using var session = PreparedContent.Install(backend);
                Assert.That(await PublicEntryTexture.IsCachedAsync(timeout.Token),Is.False,"First login must keep minimal UI until the normal content download.");
                var gate = await backend.FetchConfigAsync(timeout.Token);
                report.release = gate.ApprovedReleaseId; report.hash = gate.ApprovedCatalogHash;
                var flow = new ContentStartupFlow();
                flow.Progress += (received, total) => report.progressBytes = received;
                await flow.RunAsync(backend, "0.1.0", (bytes, token) => { report.plannedDownloadBytes = bytes; return Task.FromResult(true); }, timeout.Token);
                Assert.That(flow.Stage, Is.EqualTo(ContentStage.Ready), flow.Failure?.ToString());
                Assert.That(report.plannedDownloadBytes, Is.GreaterThan(0), "A cold run needs unique bundle hashes or a fresh cache environment; never clear user caches.");
                backend.CompletePendingRelease();
                report.cachedRequiredBytes = await backend.RequiredBytesAsync(timeout.Token);
                Assert.That(report.cachedRequiredBytes, Is.Zero);
                flow.Progress += (received, total) => report.cachedRunProgressEvents++;
                await flow.RunAsync(backend, "0.1.0", (bytes, token) => throw new Exception("Cached run requested consent"), timeout.Token);
                Assert.That(flow.Stage, Is.EqualTo(ContentStage.Ready), flow.Failure?.ToString());
                Assert.That(report.cachedRunProgressEvents, Is.Zero);
                backend.CompletePendingRelease();
                Assert.That(await PublicEntryTexture.IsCachedAsync(timeout.Token),Is.True);
                var publicView=new GameObject("Public entry real remote lifecycle");Texture2D displayed=null,loaded=null;
                try
                {
                    publicView.AddComponent<PublicEntryTexture>().Bind("login-background",value=>displayed=value);
                    publicView.SetActive(false);publicView.SetActive(true);
                    while(displayed==null)await Task.Delay(20,timeout.Token);
                    loaded=displayed;Assert.That(loaded.width,Is.GreaterThan(loaded.height));
                    publicView.SetActive(false);Assert.That(displayed,Is.Null);
                    await Task.Delay(300,timeout.Token);Assert.That(loaded==null,Is.True,"Closing the last public artwork owner must unload its texture.");
                    report.publicArtworkCancelledReenteredReleased=true;
                }
                finally{UnityEngine.Object.Destroy(publicView);}
                var selectors = new[] { "CombatSecondFinal/", "ApprovedUi/", "LobbyDeck/", "OperatorIdentity/", "CommanderSupportIcons/", "NumberedFormation/", "DestinationPages/", "NoncombatCodex/", "StartupMotion/", "CombatPresentation/" };
                using (var scope = await PreparedContent.PrepareAsync(selectors, timeout.Token))
                {
                    var textureKeys = RemoteContentCatalog.Select(selectors).Where(e => !e.Local && e.ResourceType == typeof(Texture2D)).Select(e => e.Key).ToArray();
                    Assert.That(textureKeys, Is.Not.Empty);
                    var textures = textureKeys.Select(PreparedContent.Load<Texture2D>).ToArray();
                    report.preparedTextures = textures.Length;
                    Assert.That(textures.All(t => t != null), Is.True);
                    using (var shared = await PreparedContent.PrepareAsync(new[] { textureKeys[0] }, timeout.Token))
                    {
                        Assert.That(PreparedContent.Load<Texture2D>(textureKeys[0]), Is.SameAs(textures[0]));
                        await host.OpenAsync(backend.ResolveApprovedScreen(AddressableContentBackend.LobbyKey), timeout.Token);
                        Assert.That(host.Current, Is.Not.Null);
                        PreparedContent.Attach(host.Current, await PreparedContent.PrepareAsync(new[] { textureKeys[0] }, timeout.Token));
                        host.enabled = false;
                        Assert.That(host.Current, Is.Null);
                        await host.CloseAsync();
                        await Task.Delay(100);
                        host.enabled = true;
                        await host.OpenAsync(backend.ResolveApprovedScreen(AddressableContentBackend.LobbyKey), timeout.Token);
                        Assert.That(host.Current, Is.Not.Null);
                        await host.CloseAsync();
                        await Task.Delay(100);
                        report.sharedScopePreservedTexture = textures[0] != null;
                    }
                    using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
                    Assert.CatchAsync<OperationCanceledException>(async () => await PreparedContent.PrepareAsync(selectors, cancelled.Token));
                    scope.Dispose();
                    await Task.Delay(100);
                    report.texturesRetainedByExternalOwners = textures.Count(t => t != null);
                }
                using (var reentry = await PreparedContent.PrepareAsync(new[] { "CombatSecondFinal/" }, timeout.Token))
                    Assert.That(PreparedContent.Load<Texture2D>("CombatSecondFinal/DConsole/barrier-face"), Is.Not.Null);
                var equipment = new EquipmentIconResources(backend);
                var first = await equipment.AcquireAsync(); var second = await equipment.AcquireAsync();
                var atlas = first.Texture;
                report.equipmentSharedLease = ReferenceEquals(atlas, second.Texture);
                Assert.That(report.equipmentSharedLease, Is.True);
                first.Dispose(); Assert.That(atlas != null, Is.True);
                second.Dispose(); await Task.Delay(100);
                report.equipmentDestroyedAfterLastLease = atlas == null;
                // Other real bundle owners can retain native assets; record that observation rather than inventing memory savings.
                Assert.That(report.sharedScopePreservedTexture, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostObject);
                PreparedContent.EditorAsset = editorAdapter;
                Caching.currentCacheForWriting = priorCache;
                Caching.RemoveCache(cache);
                File.WriteAllText(Path.Combine(Output, "lifecycle-report.json"), JsonUtility.ToJson(report, true));
            }
        }
    }
}
#endif
