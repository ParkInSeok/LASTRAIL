#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Sortie;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class DurableRunLocalHttpTests
    {
        private const string Origin = "http://127.0.0.1:4174";
        private static readonly AppBuildSnapshot B = new AppBuildSnapshot("1.0.0-durable-B", 100);
        private static readonly AppBuildSnapshot C = new AppBuildSnapshot("1.0.1-durable-C", 101);

        [TestCase(true)]
        [TestCase(false)]
        public async Task RestartRetainsLogicalBytesButSnapshotUsesFirstAcceptedDispatch(bool acceptedOnB)
        {
            if (Environment.GetEnvironmentVariable("LASTRAIL_RUN_HTTP_QA") != "1")
                Assert.Ignore("Requires parent-owned default ENFORCED harness on 4174 and POST /__fixtures/run-preview helper; never --support-errors.");
            var path = Path.Combine(Path.GetTempPath(), "lastrail-durable-http-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var native = NewWire();
                var session = await new GameApiPlayerEntryClient(new InstalledAdmissionTransport(native, C, "android", "internal")).GuestAsync();
                Assert.That(session, Is.Not.Null);
                var denied = await native.SendAsync(new RunApiHttpRequest("GET", "/v1/profile", null,
                    new[] { new RunApiHttpHeader("Authorization", "Bearer " + session.AccessToken.Value) }));
                Assert.That(denied.IsNetworkError, Is.False);
                Assert.That(denied.StatusCode, Is.Not.EqualTo(200), "This evidence requires admission ENFORCED, not the support-only harness.");

                // Local Admin-owned fixture route only seeds a preview for this authenticated account.
                var previewResponse = await new InstalledAdmissionTransport(native, C, "android", "internal").SendAsync(
                    new RunApiHttpRequest("POST", "/__fixtures/run-preview", "{}",
                        new[] { new RunApiHttpHeader("Authorization", "Bearer " + session.AccessToken.Value) }));
                RequireHttp(previewResponse);
                var preview = (string)JObject.Parse(previewResponse.BodyJson)["operationPreviewId"];
                Assert.That(preview, Is.Not.Null.And.Not.Empty, "Parent/Admin fixture helper must return an account-owned operationPreviewId.");
                var composedAt = DateTimeOffset.UtcNow;
                var start = RunStartRequest.Create(preview, "durable_start_" + Guid.NewGuid().ToString("N"));
                var startOp = Operation("Start", session, start, composedAt);
                var started = await RestartOperation(path, session, startOp, acceptedOnB);
                var runId = (string)started["runId"];
                Assert.That(runId, Is.Not.Null.And.Not.Empty);

                var export = new RunRuntimeStateExport(runId, (string)started["balanceVersion"],
                    (string)started["operationId"], (string)started["actId"], (string)started["regionId"],
                    "faction_wasteland_raiders", 3, "local-wave", 1u, "local-reward",
                    DateTime.Parse((string)started["serverStartedAt"], null, System.Globalization.DateTimeStyles.RoundtripKind),
                    SortieStage.Complete, 0, 15, true, new TacticSelectionExport[0]);
                var finish = RunResultSubmitRequest.FromExport(export, "durable_finish_" + Guid.NewGuid().ToString("N"),
                    RunResultOutcome.Victory, composedAt.UtcDateTime);
                Assert.That(JObject.Parse(finish.BodyJson).ContainsKey("raidResult"), Is.False,
                    "Ordinary finish omits raidResult rather than sending null.");
                var finishOp = Operation("Finish", session, finish, composedAt);
                var finished = await RestartOperation(path, session, finishOp, acceptedOnB);
                Assert.That((string)finished["runId"], Is.EqualTo(runId));
                Assert.That((string)finished["resultStatus"], Is.EqualTo(acceptedOnB ? "DUPLICATE_REPLAY" : "ACCEPTED"));
                Assert.That((string)finished["rewardPreviewId"], Is.Not.Null.And.Not.Empty);

                var detailResponse = await native.SendAsync(new RunApiHttpRequest("GET", "/api/runs/" + Uri.EscapeDataString(runId), null, null));
                RequireHttp(detailResponse);
                var detail = JObject.Parse(detailResponse.BodyJson);
                var expected = acceptedOnB ? B : C;
                AssertSnapshot(detail["runStartVersionSnapshot"], expected);
                AssertSnapshot(detail["runFinishVersionSnapshot"], expected);
                Assert.That(JToken.DeepEquals(detail["runStartVersionSnapshot"], started["runVersionSnapshot"]), Is.True);
                Assert.That(JToken.DeepEquals(detail["runFinishVersionSnapshot"], finished["runFinishVersionSnapshot"]), Is.True);
                var stored = await Read(Journal(path), session.AccountId);
                Assert.That(stored.Length, Is.EqualTo(2));
                Assert.That(stored.All(r => Get(r, "AcceptedResponseJson") != null), Is.True);
                TestContext.WriteLine("LOCAL_HTTP_DURABLE_RUN account=" + session.AccountId + " run=" + runId
                    + " composed=B installedRetry=C firstAccepted=" + (acceptedOnB ? "B" : "C")
                    + " start+finish=confirmed storage=real-file/fake-crypto-not-keystore");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        private static async Task<JObject> RestartOperation(string path, GameApiAuthSessionSnapshot session, object operation, bool acceptedOnB)
        {
            Assert.That(await Call(Journal(path), "PutAsync", operation), Is.True);
            var wireB = new ObservedWire { LoseAccepted = true };
            if (acceptedOnB)
            {
                var clientB = Client(Journal(path), session, B, wireB);
                try { await Call(clientB, "RestoreAsync"); await Call(clientB, "RetryAsync", operation); }
                finally { ((IDisposable)clientB).Dispose(); }
                Assert.That(wireB.Requests.Count, Is.EqualTo(1));
                Assert.That((bool)wireB.Responses.Single()["accepted"], Is.True);
            }
            else Assert.That(wireB.Requests, Is.Empty, "Composition must not dispatch on B.");
            var freshJournal = Journal(path);
            var restored = (await Read(freshJournal, session.AccountId)).Single(r => Equals(Get(r, "IdempotencyKey"), Get(operation, "IdempotencyKey")));
            Assert.That(Get(restored, "AcceptedResponseJson"), Is.Null);
            foreach (var name in new[] { "BodyJson", "IdempotencyKey", "ComposedAt" })
                Assert.That(Get(restored, name), Is.EqualTo(Get(operation, name)));
            Assert.That(((AppBuildSnapshot)Get(restored, "Build")).AppVersion, Is.EqualTo(B.AppVersion));
            var wireC = new ObservedWire();
            var clientC = Client(freshJournal, session, C, wireC);
            try
            {
                await Call(clientC, "RestoreAsync"); Assert.That(wireC.Requests, Is.Empty);
                await Call(clientC, "RetryAsync", restored);
                Assert.That(wireC.Requests.Count, Is.EqualTo(1));
                var sent = wireC.Requests.Single();
                Assert.That(sent.BodyJson, Is.EqualTo(Get(operation, "BodyJson")));
                Assert.That(Header(sent, "Idempotency-Key"), Is.EqualTo(Get(operation, "IdempotencyKey")));
                Assert.That(Header(sent, "X-LastRail-App-Version"), Is.EqualTo(C.AppVersion));
                Assert.That(Header(sent, "X-LastRail-App-Version-Code"), Is.EqualTo("101"));
                if (acceptedOnB)
                {
                    Assert.That(sent.BodyJson, Is.EqualTo(wireB.Requests.Single().BodyJson));
                    Assert.That(Header(wireB.Requests.Single(), "X-LastRail-App-Version"), Is.EqualTo(B.AppVersion));
                    var snapshot = Get(operation, "Kind").ToString() == "Start" ? "runVersionSnapshot" : "runFinishVersionSnapshot";
                    Assert.That(JToken.DeepEquals(wireB.Responses.Single()[snapshot], wireC.Responses.Single()[snapshot]), Is.True);
                }
                var confirmed = (await Read(Journal(path), session.AccountId)).Single(r => Equals(Get(r, "IdempotencyKey"), Get(operation, "IdempotencyKey")));
                Assert.That(Get(confirmed, "AcceptedResponseJson"), Is.Not.Null);
                await Call(clientC, "RetryAsync", confirmed);
                Assert.That(wireC.Requests.Count, Is.EqualTo(1), "Durably confirmed work is not another pending POST.");
                return JObject.Parse((string)Get(confirmed, "AcceptedResponseJson"));
            }
            finally { ((IDisposable)clientC).Dispose(); }
        }

        private sealed class ObservedWire : IRunApiHttpTransport
        {
            public bool LoseAccepted;
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public readonly List<JObject> Responses = new List<JObject>();
            public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Assert.That(request.Method, Is.EqualTo("POST"));
                Assert.That(request.EndpointPath == "/v1/runs/start" || request.EndpointPath.StartsWith("/v1/runs/", StringComparison.Ordinal)
                    && request.EndpointPath.EndsWith("/finish", StringComparison.Ordinal), Is.True);
                Requests.Add(request);
                var response = await NewWire().SendAsync(request);
                if (response.StatusCode != 200) TestContext.WriteLine("LOCAL_RUN_HTTP_FAILURE status=" + response.StatusCode + " body=" + response.BodyJson);
                RequireHttp(response);
                Responses.Add(JObject.Parse(response.BodyJson));
                return LoseAccepted ? RunApiHttpResponse.NetworkError("Synthetic response loss after actual HTTP acceptance.") : response;
            }
        }

        // Reversible file codec only: exercises fresh file reads, not Android encryption guarantees.
        private sealed class FileCrypto : IAndroidKeystoreNativeStoragePort
        {
            private readonly string path;
            public FileCrypto(string path) { this.path = path; }
            public bool IsAndroidRuntime => true;
            public bool IsKeystoreAvailable => true;
            public bool UsedPlayerPrefsFallback => false;
            public string ProviderName => "explicit_durable_http_fake_crypto";
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string text) => new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(Encoding.UTF8.GetBytes(text)), "fixture_iv");
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload value) => Encoding.UTF8.GetString(Convert.FromBase64String(value.CipherTextBase64));
            public AndroidKeystoreEncryptedTokenPayload Load() => File.Exists(path) ? new AndroidKeystoreEncryptedTokenPayload(File.ReadAllText(path), "fixture_iv") : null;
            public bool Save(AndroidKeystoreEncryptedTokenPayload value) { File.WriteAllText(path, value.CipherTextBase64); return true; }
            public bool Clear() { if (File.Exists(path)) File.Delete(path); return true; }
        }
        private static object Journal(string path) => Activator.CreateInstance(Required("EncryptedRunOperationJournal"), new FileCrypto(path));
        private static object Operation(string kind, GameApiAuthSessionSnapshot owner, object request, DateTimeOffset time) =>
            Required("PendingRunOperation").GetMethod(kind).Invoke(null, new object[] { owner.AccountId, request, B, time });
        private static object Client(object journal, GameApiAuthSessionSnapshot owner, AppBuildSnapshot installed, IRunApiHttpTransport wire) =>
            Activator.CreateInstance(Required("DurableRunClient"), new object[] {
                new GameApiAuthenticatedHttpTransport(new InstalledAdmissionTransport(wire, installed, "android", "internal")), journal,
                (Func<GameApiAuthSessionSnapshot>)(() => owner), (Func<long>)(() => 1),
                (Func<AppBuildSnapshot>)(() => { throw new InvalidOperationException("Restoration must not recapture composed build."); }),
                (Func<DateTimeOffset>)(() => DateTimeOffset.UtcNow) });
        private static Type Required(string name)
        { var type = typeof(RunStartRequest).Assembly.GetType("LastRail.Run." + name); Assert.That(type, Is.Not.Null, "Missing " + name); return type; }
        private static object Get(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        private static async Task<object> Call(object target, string name, params object[] args)
        { var task = (Task)target.GetType().GetMethod(name).Invoke(target, args); await task; return task.GetType().GetProperty("Result")?.GetValue(task); }
        private static async Task<object[]> Read(object journal, string account) => ((IEnumerable)await Call(journal, "ReadAsync", account)).Cast<object>().ToArray();
        private static string Header(RunApiHttpRequest request, string name) => request.Headers.Single(h => h.Name == name).Value;
        private static IRunApiHttpTransport NewWire() => new UnityWebRequestRunApiHttpTransport(new UnityWebRequestRunApiHttpAdapter(Origin, 10));
        private static void RequireHttp(RunApiHttpResponse response)
        { Assert.That(response.IsNetworkError, Is.False); Assert.That(response.StatusCode, Is.EqualTo(200), response.BodyJson); }
        private static void AssertSnapshot(JToken snapshot, AppBuildSnapshot expected)
        {
            Assert.That(snapshot, Is.Not.Null);
            Assert.That((string)snapshot["clientInstalled"]["appVersion"], Is.EqualTo(expected.AppVersion));
            Assert.That((string)snapshot["clientInstalled"]["versionCode"], Is.EqualTo(expected.AppVersionCode.ToString()));
        }
    }
}
#endif
