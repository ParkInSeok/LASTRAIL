using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using LastRail.Resources;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class ApprovedReleasePinTests
    {
        private string directory, path;
        private const string Source = "https://api.lastrail.app/v1/client-config";
        private const string Root = "https://content.lastrail.test/releases/";
        [SetUp] public void SetUp() { directory = Path.Combine(Path.GetTempPath(), "lastrail-pin-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); path = Path.Combine(directory, "pending.json"); }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private object Store()
        {
            var type = typeof(ClientContentGate).Assembly.GetType("LastRail.Resources.ApprovedReleasePin");
            Assert.That(type, Is.Not.Null, "Incomplete startup release must survive process restart");
            return Activator.CreateInstance(type, path);
        }
        private static ClientContentGate Gate(string release = "r42")
        {
            var json = "{\"accepted\":true,\"maintenance\":{\"enabled\":false},\"clientAdmission\":{\"status\":\"ACCEPTED\",\"installedVersionCode\":\"42\",\"minimumVersionCode\":\"40\",\"updateRequired\":false},\"contentRelease\":{\"status\":\"APPROVED_COMPATIBLE\",\"releaseId\":\"" + release + "\",\"contentVersion\":\"c42\",\"catalogUrl\":\"" + Root + release + "/catalog.bin\",\"catalogHash\":\"sha256:" + new string('a', 64) + "\",\"compatibility\":{\"minimumVersionCode\":\"40\",\"maximumVersionCode\":null}}}";
            return ClientAdmissionContract.Parse(json, "42", Source, Root);
        }
        private static void Call(object store, string method, params object[] args)
        {
            var member = store.GetType().GetMethod(method); Assert.That(member, Is.Not.Null);
            try { member.Invoke(store, args); } catch (TargetInvocationException error) { throw error.InnerException; }
        }
        private void Pin(object store, ClientContentGate gate, string code = "42") => Call(store, "VerifyOrPin", Source, code, Root, gate);
        [Test] public void RestartRetainsTheSamePendingReleaseAndRejectsSilentReplacement()
        {
            Pin(Store(), Gate()); var before = File.ReadAllText(path); Pin(Store(), Gate());
            Assert.Throws<ApprovedReleasePinException>(() => Pin(Store(), Gate("r43")));
            Assert.That(File.ReadAllText(path), Is.EqualTo(before));
        }
        [Test] public void AChangedInstalledBuildCannotReuseThePreviousPendingRelease()
        {
            Pin(Store(), Gate()); Assert.Throws<ApprovedReleasePinException>(() => Pin(Store(), Gate(), "43"));
        }
        [Test] public void CorruptPinNeverBecomesAnApprovedFallback()
        {
            File.WriteAllText(path, "{broken"); Assert.Throws<ApprovedReleasePinException>(() => Pin(Store(), Gate()));
            Assert.That(File.ReadAllText(path), Is.EqualTo("{broken"));
        }
        [Test] public void ExplicitResetOnlyRemovesPendingMetadata()
        {
            Pin(Store(), Gate()); var unrelated = Path.Combine(directory, "bundle.bin"); File.WriteAllText(unrelated, "fixture");
            Call(Store(), "Clear"); Pin(Store(), Gate("r43")); Assert.That(File.ReadAllText(unrelated), Is.EqualTo("fixture"));
        }
        [Test] public void SuccessfulLobbyBindingCompletesOnlyTheMatchingPin()
        {
            var store = Store(); Pin(store, Gate()); Assert.Throws<ApprovedReleasePinException>(() => Call(store, "Complete", Gate("r43")));
            Assert.That(File.Exists(path), Is.True); Call(store, "Complete", Gate()); Assert.That(File.Exists(path), Is.False);
        }

        [Test] public void CompletionRejectsAReplacedBuildIdentityEvenWhenReleaseMatches()
        {
            var store = Store(); Pin(store, Gate());
            var changed = JObject.Parse(File.ReadAllText(path)); changed["installedCode"] = "43"; File.WriteAllText(path, changed.ToString());
            Assert.Throws<ApprovedReleasePinException>(() => Call(store, "Complete", Gate()));
            Assert.That(File.Exists(path), Is.True);
        }
        [Test] public void CompletionRequiresFreshVerificationInThisProcess()
        {
            Pin(Store(), Gate()); Assert.Throws<ApprovedReleasePinException>(() => Call(Store(), "Complete", Gate()));
        }
        [Test] public void CompletedReleaseAllowsReentryOnlyWithinTheSameVerifiedInstance()
        {
            var store = Store(); Pin(store, Gate()); Call(store, "Complete", Gate());
            Assert.DoesNotThrow(() => Call(store, "Complete", Gate()));
            Assert.Throws<ApprovedReleasePinException>(() => Call(store, "Complete", Gate("r43")));
            Assert.That(File.Exists(path), Is.False);
        }
        [Test] public async Task ExplicitReleaseRetryRechecksAdmissionAfterPreviouslyReadyState()
        {
            var backend = new Backend(); var access = new DownloadAccessPolicy(); access.SetAuthenticated(true);
            var coordinator = new DownloadCoordinator(backend, access);
            await coordinator.StartAsync("1.0.0", CancellationToken.None); Assert.That(coordinator.CanEnterGame, Is.True);
            Call(coordinator, "InvalidateAdmission"); Assert.That(coordinator.CanEnterGame, Is.False);
            await coordinator.StartAsync("1.0.0", CancellationToken.None);
            Assert.That(backend.Reads, Is.EqualTo(2)); Assert.That(coordinator.CanEnterGame, Is.True);
        }
        private sealed class Backend : IContentStartupBackend
        {
            public int Reads;
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken token) { Reads++; return Task.FromResult(new ClientContentGate { MinimumVersion = "1.0.0" }); }
            public Task InitializeAsync(CancellationToken token) => Task.CompletedTask;
            public Task UpdateCatalogAsync(CancellationToken token) => Task.CompletedTask;
            public Task<long> RequiredBytesAsync(CancellationToken token) => Task.FromResult(0L);
            public Task DownloadAsync(Action<long, long> progress, CancellationToken token) => Task.CompletedTask;
            public Task VerifyRequiredAssetsAsync(CancellationToken token) => Task.CompletedTask;
        }
    }
}
