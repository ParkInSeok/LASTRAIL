#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Submissions;
using LastRail.Support;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class ReceiptRecoveryLocalHttpTests
    {
        private const string Origin = "http://127.0.0.1:4174";
        private const long Code = 42062;

        [Test]
        public async Task AcceptedLostResponsesRecoverFromNewFileJournalClientsWithoutPostReplay()
        {
            if (Environment.GetEnvironmentVariable("LASTRAIL_SUPPORT_HTTP_QA") != "1")
                Assert.Ignore("Requires explicit parent-owned --support-errors local HTTP harness on 127.0.0.1:4174.");
            var path = Path.Combine(Path.GetTempPath(), "lastrail-receipt-http-" + Guid.NewGuid().ToString("N") + ".json");
            var version = "1.0.0-receipt-" + Guid.NewGuid().ToString("N");
            var build = new AppBuildSnapshot(version, Code);
            var body = "LOCAL restart receipt body " + Guid.NewGuid().ToString("N");
            try
            {
                var wire = new ObservedWire();
                var owner = await new GameApiPlayerEntryClient(wire).GuestAsync();
                var foreign = await new GameApiPlayerEntryClient(wire).GuestAsync();
                Assert.That(owner, Is.Not.Null); Assert.That(foreign, Is.Not.Null);
                Assert.That(foreign.AccountId, Is.Not.EqualTo(owner.AccountId));
                var records = await SubmitAndDropClients(path, wire, owner, build, body);
                Assert.That(wire.Posts, Is.EqualTo(2));
                Assert.That(wire.AcceptedLosses, Is.EqualTo(2));
                Assert.That(File.Exists(path), Is.True);

                // A new stateless fake crypto port reads the actual file, not an in-memory payload cache.
                var freshPort = Port(path);
                var metadata = freshPort.Decrypt(freshPort.Load());
                Assert.That(metadata, Does.Not.Contain(body));
                Assert.That(metadata, Does.Not.Contain("\"events\""));
                Assert.That(metadata, Does.Not.Contain(owner.AccessToken.Value));
                Assert.That(File.ReadAllText(path), Does.Not.Contain(owner.AccountId));
                var journal = new EncryptedPendingReceiptJournal(freshPort);
                var restored = await journal.ReadAsync(owner.AccountId);
                Assert.That(restored.Count, Is.EqualTo(2));
                foreach (var record in restored)
                {
                    var original = records.Single(r => r.Kind == record.Kind);
                    Assert.That(record.IdempotencyKey, Is.EqualTo(original.IdempotencyKey));
                    Assert.That(record.AccountId, Is.EqualTo(owner.AccountId));
                    Assert.That(record.Category, Is.EqualTo(original.Category));
                    Assert.That(record.Build.AppVersion, Is.EqualTo(version));
                    Assert.That(record.Build.AppVersionCode, Is.EqualTo(Code));
                    Assert.That(record.ComposedAt, Is.EqualTo(original.ComposedAt));
                    Assert.That(record.SubmittedAt, Is.EqualTo(original.SubmittedAt));
                }
                Assert.That(await journal.ReadAsync(foreign.AccountId), Is.Empty);
                var auth = new GameApiAuthenticatedHttpTransport(wire);
                var support = restored.Single(r => r.Kind == PendingReceiptKind.Support);
                var error = restored.Single(r => r.Kind == PendingReceiptKind.ClientError);
                var recaptures = 0;
                Func<AppBuildSnapshot> changedBuild = () => { recaptures++; return new AppBuildSnapshot(version + "-new", Code + 1); };
                var inner = new SupportInquiryHttpClient(auth, changedBuild);
                var lookup = inner.RestoreReceiptLookup(support);
                Assert.That(lookup, Is.Not.Null); Assert.That(lookup.ReceiptOnly, Is.True);
                Assert.That(lookup.Content, Is.Null);
                Assert.That(lookup.Build.AppVersion, Is.EqualTo(version));
                Assert.That((await inner.SubmitAsync(lookup, owner)).Delivery, Is.EqualTo(InquiryDelivery.Unknown));
                Assert.That((await inner.ReconcileAsync(lookup, foreign)).Delivery, Is.EqualTo(InquiryDelivery.Unknown));
                using (var foreignClient = new CurrentSessionErrorClient(auth, () => foreign, () => 2, changedBuild, null,
                    new EncryptedPendingReceiptJournal(Port(path))))
                    await foreignClient.ReconcilePendingAsync();
                Assert.That(wire.Gets.Count, Is.Zero, "Restoration must not query another account's records.");

                // Deliberate raw security probes, not the recovery client's account-selection behavior.
                await ForeignProbe(auth, foreign, "/v1/support/tickets/receipt", support.IdempotencyKey);
                await ForeignProbe(auth, foreign, "/v1/client-errors/receipt", error.IdempotencyKey);
                Assert.That((await journal.ReadAsync(owner.AccountId)).Count, Is.EqualTo(2));
                var tracked = new ReceiptTrackingSupportClient(inner, journal, () => 3, () => owner);
                var receipt = await tracked.ReconcileAsync(lookup, owner);
                Assert.That(receipt.Delivery, Is.EqualTo(InquiryDelivery.Received));
                Assert.That(receipt.TicketId, Is.EqualTo(wire.Ticket));
                using (var client = new CurrentSessionErrorClient(auth, () => owner, () => 3, changedBuild, null, journal))
                {
                    Assert.That(client.PendingCount, Is.Zero);
                    await client.ReconcilePendingAsync();
                    await client.ReconcilePendingAsync();
                    Assert.That(client.PendingCount, Is.Zero);
                }
                Assert.That(await new EncryptedPendingReceiptJournal(Port(path)).ReadAsync(owner.AccountId), Is.Empty);
                Assert.That(recaptures, Is.Zero, "Receipt restoration must never capture a new build.");
                Assert.That(wire.Posts, Is.EqualTo(2), "All restart actions are GET-only, with no replay POST.");
                Assert.That(wire.Gets.Count, Is.EqualTo(4));
                Assert.That(wire.Gets.Count(g => g.Key == support.IdempotencyKey && g.Status == 200), Is.EqualTo(1));
                Assert.That(wire.Gets.Count(g => g.Key == error.IdempotencyKey && g.Status == 200), Is.EqualTo(1));
                await AssertAdmin(owner.AccountId, foreign.AccountId, wire.Ticket, version, body);
                TestContext.WriteLine("LOCAL_HTTP_RECEIPT_RESTART account=" + owner.AccountId + " ticket=" + wire.Ticket
                    + " appVersion=" + version + " appVersionCode=" + Code
                    + " acceptedLostPOST=2 restoredFOUND=2 foreign404=2 replayPOST=0 storage=real-file/fake-crypto-not-keystore");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        private static async Task<IReadOnlyList<PendingReceipt>> SubmitAndDropClients(string path, ObservedWire wire,
            GameApiAuthSessionSnapshot owner, AppBuildSnapshot build, string body)
        {
            var journal = new EncryptedPendingReceiptJournal(Port(path));
            var auth = new GameApiAuthenticatedHttpTransport(wire);
            var inner = new SupportInquiryHttpClient(auth, () => build);
            var tracked = new ReceiptTrackingSupportClient(inner, journal, () => 1, () => owner);
            var controller = new SupportInquiryController(owner, tracked, SupportInquiryHttpClient.Categories, 2000, () => build,
                minContentLength: 10);
            Assert.That(controller.Edit("BUG_OTHER", body), Is.True);
            await controller.SubmitAsync();
            Assert.That(controller.State, Is.EqualTo(InquiryState.Uncertain));
            using (var client = new CurrentSessionErrorClient(auth, () => owner, () => 1, () => build, null, journal))
            {
                var request = client.Capture(SafeClientErrorKind.UnityError, 1);
                Assert.That(request, Is.Not.Null);
                Assert.That(await client.SendAsync(request), Is.EqualTo(ClientErrorDelivery.Unknown));
                Assert.That(client.PendingCount, Is.EqualTo(1));
                var rows = await journal.ReadAsync(owner.AccountId);
                Assert.That(rows.Single(r => r.Kind == PendingReceiptKind.Support).IdempotencyKey, Is.EqualTo(controller.Pending.IdempotencyKey));
                Assert.That(rows.Single(r => r.Kind == PendingReceiptKind.ClientError).IdempotencyKey, Is.EqualTo(request.IdempotencyKey));
                controller.SetSession(null);
                return rows;
            }
        }

        private static async Task ForeignProbe(IGameApiAuthenticatedHttpTransport auth, GameApiAuthSessionSnapshot foreign,
            string endpoint, string key)
        {
            var result = await auth.SendAsync(new RunApiHttpRequest("GET", endpoint, null,
                new[] { new RunApiHttpHeader("Idempotency-Key", key) }), foreign.AccessToken);
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response.IsNetworkError, Is.False);
            Assert.That(result.Response.StatusCode, Is.EqualTo(404));
            Assert.That((string)JObject.Parse(result.Response.BodyJson)["receiptStatus"], Is.EqualTo("NOT_FOUND"));
        }

        private static async Task AssertAdmin(string account, string foreign, string ticket, string version, string body)
        {
            var tickets = (JArray)(await Admin("/api/support/tickets?accountId=" + Uri.EscapeDataString(account)))["items"];
            Assert.That(tickets.Count, Is.EqualTo(1));
            Assert.That((string)tickets[0]["ticketId"], Is.EqualTo(ticket));
            Assert.That(((JArray)(await Admin("/api/support/tickets?accountId=" + Uri.EscapeDataString(foreign)))["items"]).Count, Is.Zero);
            var detail = (await Admin("/api/support/tickets/" + Uri.EscapeDataString(ticket)))["ticket"];
            Assert.That((string)detail["accountId"], Is.EqualTo(account));
            Assert.That((string)detail["body"], Is.EqualTo(body));
            Assert.That((string)detail["appVersion"], Is.EqualTo(version));
            Assert.That((string)detail["appVersionCode"], Is.EqualTo(Code.ToString()));
            var query = "/api/ops/error-logs?category=frontend&appVersion=" + Uri.EscapeDataString(version) + "&appVersionCode=";
            var logs = (JArray)(await Admin(query + Code))["items"];
            Assert.That(logs.Count, Is.EqualTo(1), "Unique version excludes all harness seeds and detects replay duplicates.");
            Assert.That((string)logs[0]["accountId"], Is.EqualTo(account));
            Assert.That((string)logs[0]["errorCode"], Is.EqualTo("frontend_unity_error"));
            Assert.That((string)logs[0]["appVersion"], Is.EqualTo(version));
            Assert.That((string)logs[0]["appVersionCode"], Is.EqualTo(Code.ToString()));
            Assert.That(((JArray)(await Admin(query + (Code + 1)))["items"]).Count, Is.Zero);
            Assert.That(((JArray)(await Admin("/api/ops/error-logs?category=frontend&appVersion="
                + Uri.EscapeDataString(version + "-new") + "&appVersionCode=" + Code))["items"]).Count, Is.Zero);
        }

        private sealed class ReceiptGet { internal string Key; internal long Status; }
        private sealed class ObservedWire : IRunApiHttpTransport
        {
            private readonly IRunApiHttpTransport actual = NewWire();
            internal int Posts, AcceptedLosses;
            internal string Ticket;
            internal readonly List<ReceiptGet> Gets = new List<ReceiptGet>();
            public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                var path = request.EndpointPath;
                if (path != "/v1/auth/guest" && path != "/v1/support/tickets" && path != "/v1/client-errors"
                    && path != "/v1/support/tickets/receipt" && path != "/v1/client-errors/receipt")
                    throw new InvalidOperationException("Unexpected local receipt QA route.");
                var response = await actual.SendAsync(request);
                if (path.EndsWith("/receipt", StringComparison.Ordinal))
                {
                    Assert.That(request.Method, Is.EqualTo("GET")); Assert.That(request.BodyJson, Is.Null);
                    Gets.Add(new ReceiptGet { Key = request.Headers.Single(h => h.Name == "Idempotency-Key").Value, Status = response.StatusCode });
                }
                if (request.Method == "POST" && path != "/v1/auth/guest")
                {
                    Posts++;
                    Assert.That(response.IsNetworkError, Is.False); Assert.That(response.StatusCode, Is.EqualTo(200));
                    var json = JObject.Parse(response.BodyJson);
                    Assert.That((bool)json["accepted"], Is.True);
                    Assert.That((string)json[path == "/v1/support/tickets" ? "ticketStatus" : "ingestStatus"], Is.EqualTo("RECEIVED"));
                    if (path == "/v1/support/tickets") Ticket = (string)json["ticketId"];
                    AcceptedLosses++;
                    return RunApiHttpResponse.NetworkError("Explicit synthetic loss AFTER actual accepted HTTP response.");
                }
                return response;
            }
        }

        // Test-only reversible obfuscation, NOT Android Keystore or cryptographic security evidence.
        // It has no payload dictionary: brand-new instances must decode bytes from the real temp file.
        private sealed class ExplicitFakeCrypto : IAndroidKeystoreNativeStoragePort
        {
            public bool IsAndroidRuntime => true;
            public bool IsKeystoreAvailable => true;
            public bool UsedPlayerPrefsFallback => false;
            public string ProviderName => "explicit_editor_fake_crypto";
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string plaintext) =>
                new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(Transform(Encoding.UTF8.GetBytes(plaintext))), "fixture_iv");
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload encrypted) =>
                Encoding.UTF8.GetString(Transform(Convert.FromBase64String(encrypted.CipherTextBase64)));
            private static byte[] Transform(byte[] bytes) { for (var i = 0; i < bytes.Length; i++) bytes[i] ^= 0xA7; return bytes; }
            public AndroidKeystoreEncryptedTokenPayload Load() => throw new InvalidOperationException("Auth storage must not be used.");
            public bool Save(AndroidKeystoreEncryptedTokenPayload value) => throw new InvalidOperationException("Auth storage must not be used.");
            public bool Clear() => throw new InvalidOperationException("Auth storage must not be used.");
        }
        private static AndroidEncryptedReceiptFilePort Port(string path) => new AndroidEncryptedReceiptFilePort(new ExplicitFakeCrypto(), path);
        private static IRunApiHttpTransport NewWire() => new UnityWebRequestRunApiHttpTransport(new UnityWebRequestRunApiHttpAdapter(Origin, 10));
        private static async Task<JObject> Admin(string path)
        {
            Assert.That(path.StartsWith("/api/", StringComparison.Ordinal), Is.True);
            var response = await NewWire().SendAsync(new RunApiHttpRequest("GET", path, null, null));
            Assert.That(response.IsNetworkError, Is.False); Assert.That(response.StatusCode, Is.EqualTo(200));
            return JObject.Parse(response.BodyJson);
        }
    }
}
#endif
