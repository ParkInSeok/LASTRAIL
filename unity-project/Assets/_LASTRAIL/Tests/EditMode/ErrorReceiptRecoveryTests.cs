using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    // RED tests use an emitted journal interface adapter, never a native storage fallback.
    public sealed class ErrorReceiptRecoveryTests
    {
        private const string Received = "{\"accepted\":true,\"ingestStatus\":\"RECEIVED\",\"ingestId\":\"ingest_fixture\",\"acceptedEventCount\":1,\"droppedEventCount\":0,\"serverReceivedAt\":\"2026-09-06T00:00:00.000Z\"}";
        // Actual receipt API scopes its DB lookup by the authenticated account; no echo is required.
        private const string Found = "{\"accepted\":true,\"receiptStatus\":\"FOUND\",\"ingestId\":\"ingest_fixture\",\"acceptedEventCount\":1,\"serverReceivedAt\":\"2026-09-06T00:00:00.000Z\"}";

        private sealed class Wire : IRunApiHttpTransport
        {
            internal readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            internal Func<RunApiHttpRequest, Task<RunApiHttpResponse>> Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200, Received));
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request) { Requests.Add(request); return Reply(request); }
        }

        // A new adapter can share this metadata-only backing list to model a process restart.
        public sealed class JournalDouble
        {
            public readonly List<object> Records;
            public readonly List<object> Puts = new List<object>();
            public readonly List<string> Reads = new List<string>();
            public readonly List<string> Removals = new List<string>();
            public bool PutResult = true, RemoveResult = true, CorruptRead;
            public TaskCompletionSource<bool> PutWait, ReadWait;
            public JournalDouble(List<object> records = null) { Records = records ?? new List<object>(); }
            public object Dispatch(string name, object[] args)
            {
                if (name == "PutAsync") return Put(args[0]);
                if (name == "RemoveAsync")
                {
                    var account = (string)args[0]; var kind = args[1]; var key = (string)args[2];
                    Removals.Add(account + ":" + kind + ":" + key);
                    if (RemoveResult) Records.RemoveAll(r => (string)Member(r, "AccountId") == account
                        && Equals(Member(r, "Kind"), kind) && (string)Member(r, "IdempotencyKey") == key);
                    return Task.FromResult(RemoveResult);
                }
                if (name == "ReadAsync")
                {
                    Reads.Add((string)args[0]);
                    if (CorruptRead) throw new InvalidOperationException("Synthetic unavailable/corrupt journal");
                    // Intentionally do not filter: the client must reject foreign-account/kind records too.
                    var array = Array.CreateInstance(Shared("PendingReceipt"), Records.Count);
                    for (var i = 0; i < Records.Count; i++) array.SetValue(Records[i], i);
                    var listType = typeof(IReadOnlyList<>).MakeGenericType(Shared("PendingReceipt"));
                    return typeof(JournalDouble).GetMethod(nameof(ReadResult)).MakeGenericMethod(listType)
                        .Invoke(null, new object[] { ReadWait?.Task ?? Task.FromResult(true), array });
                }
                throw new InvalidOperationException(name);
            }
            private async Task<bool> Put(object record)
            {
                Puts.Add(record);
                var saved = PutWait == null ? PutResult : await PutWait.Task;
                if (saved && !Records.Any(r => (string)Member(r, "IdempotencyKey") == (string)Member(record, "IdempotencyKey")))
                    Records.Add(record);
                return saved;
            }
            public static async Task<T> ReadResult<T>(Task wait, object records) { await wait; return (T)records; }
        }

        private sealed class Fixture : IDisposable
        {
            internal readonly JournalDouble Journal;
            internal readonly Wire Wire = new Wire();
            internal GameApiAuthSessionSnapshot Session = SupportInquiryTests.Session();
            internal long Generation = 7;
            internal AppBuildSnapshot Build = new AppBuildSnapshot("1.2.3", 123);
            internal DateTimeOffset Now = DateTimeOffset.Parse("2026-09-06T00:00:00Z");
            internal readonly CurrentSessionErrorClient Client;
            internal Fixture(JournalDouble journal = null)
            {
                Journal = journal ?? new JournalDouble();
                var constructor = ClientConstructor();
                Client = (CurrentSessionErrorClient)constructor.Invoke(new object[] {
                    new GameApiAuthenticatedHttpTransport(Wire), (Func<GameApiAuthSessionSnapshot>)(() => Session),
                    (Func<long>)(() => Generation), (Func<AppBuildSnapshot>)(() => Build),
                    (Func<DateTimeOffset>)(() => Now), Adapter(Journal) });
            }
            public void Dispose() => Client.Dispose();
        }

        [Test]
        public void RecoveryApiPreservesExistingConstructorAndExposesExplicitGetOnlyEntryPoints()
        {
            Assert.That(typeof(CurrentSessionErrorClient).GetConstructor(new[] { typeof(IGameApiAuthenticatedHttpTransport),
                typeof(Func<GameApiAuthSessionSnapshot>), typeof(Func<long>), typeof(Func<AppBuildSnapshot>), typeof(Func<DateTimeOffset>) }), Is.Not.Null);
            ClientConstructor();
            foreach (var type in new[] { typeof(CurrentSessionErrorClient), typeof(CurrentSessionErrorRuntime) })
                Assert.That(RecoveryMethod(type).ReturnType, Is.EqualTo(typeof(Task)));
        }

        [Test]
        public async Task FailedJournalSaveBlocksPostAndReturnsUnknown()
        {
            using (var f = new Fixture(new JournalDouble { PutResult = false }))
            {
                var request = f.Client.Capture(SafeClientErrorKind.UnityError, 7);
                Assert.That(await f.Client.SendAsync(request), Is.EqualTo(ClientErrorDelivery.Unknown));
                Assert.That(f.Journal.Puts.Count, Is.EqualTo(1)); Assert.That(f.Wire.Requests, Is.Empty);
            }
        }

        [TestCase("generation")]
        [TestCase("account")]
        [TestCase("dispose")]
        public async Task AwaitedPutMustRecheckOwnerBeforeDispatch(string change)
        {
            using (var f = new Fixture(new JournalDouble { PutWait = new TaskCompletionSource<bool>() }))
            {
                var send = f.Client.SendAsync(f.Client.Capture(SafeClientErrorKind.UnityError, 7));
                Assert.That(f.Journal.Puts.Count, Is.EqualTo(1)); Assert.That(f.Wire.Requests, Is.Empty);
                if (change == "generation") f.Generation++;
                else if (change == "account") f.Session = SupportInquiryTests.Session("foreign_account");
                else f.Client.Dispose();
                f.Journal.PutWait.SetResult(true);
                Assert.That(await send, Is.EqualTo(ClientErrorDelivery.Stale));
                Assert.That(f.Wire.Requests, Is.Empty); Assert.That(f.Journal.Records.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task FrozenMetadataAndPostSurviveSameAccountRefreshWithoutAuthKeyReuse()
        {
            using (var f = new Fixture())
            {
                var request = f.Client.Capture(SafeClientErrorKind.UnityError, 7);
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.NetworkError("synthetic lost response"));
                await f.Client.SendAsync(request);
                var metadata = f.Journal.Records.Single();
                Assert.That(Member(metadata, "Kind").ToString(), Is.EqualTo("ClientError"));
                Assert.That(Member(metadata, "AccountId"), Is.EqualTo("account_test"));
                Assert.That(Member(metadata, "IdempotencyKey"), Is.EqualTo(request.IdempotencyKey));
                Assert.That(Member(metadata, "Category"), Is.EqualTo("UNITY_ERROR"));
                Assert.That(((AppBuildSnapshot)Member(metadata, "Build")).AppVersionCode, Is.EqualTo(123));
                Assert.That((DateTimeOffset)Member(metadata, "ComposedAt"), Is.EqualTo(f.Now));
                Assert.That(metadata.GetType().GetProperties().Select(p => p.Name), Is.EquivalentTo(new[] {
                    "Kind", "AccountId", "IdempotencyKey", "Category", "Build", "ComposedAt", "SubmittedAt" }));
                Assert.That(request.IdempotencyKey, Is.Not.EqualTo(f.Session.AccessToken.Value));
                Assert.That(request.IdempotencyKey, Is.Not.EqualTo(f.Session.RefreshToken.Value));
                f.Session = new GameApiAuthSessionSnapshot("account_test", "ACTIVE", new GameApiAccessToken("lst1.refreshed"),
                    DateTime.UtcNow.AddHours(1), new GameApiRefreshToken("refreshed_fixture"), DateTime.UtcNow.AddDays(1));
                f.Build = new AppBuildSnapshot("9.9.9", 999); f.Now = f.Now.AddHours(1);
                await f.Client.SendAsync(request);
                Assert.That(f.Wire.Requests[1].Headers.Single(h => h.Name == "Authorization").Value, Is.EqualTo("Bearer lst1.refreshed"));
                Assert.That(Key(f.Wire.Requests[1]), Is.EqualTo(Key(f.Wire.Requests[0])));
                Assert.That(f.Wire.Requests[1].BodyJson, Is.EqualTo(f.Wire.Requests[0].BodyJson));
                foreach (var put in f.Journal.Puts)
                {
                    Assert.That(Member(put, "SubmittedAt"), Is.EqualTo(Member(metadata, "SubmittedAt")));
                    Assert.That(((AppBuildSnapshot)Member(put, "Build")).AppVersionCode, Is.EqualTo(123));
                }
            }
        }

        [Test]
        public async Task RestartUsesNewClientAndJournalInstanceForReceiptGetOnly()
        {
            var records = new List<object>(); string key;
            using (var old = new Fixture(new JournalDouble(records)))
            {
                var request = old.Client.Capture(SafeClientErrorKind.UnityError, 7); key = request.IdempotencyKey;
                old.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.NetworkError("synthetic lost response"));
                await old.Client.SendAsync(request);
            }
            using (var next = new Fixture(new JournalDouble(records)))
            {
                Assert.That(next.Client.PendingCount, Is.Zero, "Restart cannot restore POST bodies.");
                next.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200, Found));
                using (var runtime = new CurrentSessionErrorRuntime(next.Client))
                {
                    await runtime.FlushAsync(); Assert.That(next.Wire.Requests, Is.Empty);
                    await Reconcile(runtime);
                    Assert.That(runtime.PendingCount, Is.Zero);
                }
                Assert.That(next.Wire.Requests.Count, Is.EqualTo(1));
                AssertGet(next.Wire.Requests.Single(), key);
                Assert.That(next.Journal.Puts, Is.Empty); Assert.That(records, Is.Empty);
            }
        }

        [Test]
        public async Task ForeignAccountAndSupportMetadataNeverBecomeErrorReceiptQueries()
        {
            using (var f = new Fixture())
            {
                f.Journal.Records.Add(Metadata("foreign_account", "foreign_fixture_key", "ClientError"));
                f.Journal.Records.Add(Metadata("account_test", "support_fixture_key", "Support"));
                f.Wire.Reply = _ => throw new Exception("No receipt request is allowed");
                await Reconcile(f.Client);
                Assert.That(f.Journal.Reads, Is.EqualTo(new[] { "account_test" }));
                Assert.That(f.Wire.Requests, Is.Empty); Assert.That(f.Journal.Removals, Is.Empty);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task StaleReadOrFoundResponseMustNotQueryOrDeleteForReplacementOwner(bool afterHttp)
        {
            using (var f = new Fixture())
            {
                f.Journal.Records.Add(Metadata());
                var response = new TaskCompletionSource<RunApiHttpResponse>();
                if (afterHttp) f.Wire.Reply = _ => response.Task;
                else f.Journal.ReadWait = new TaskCompletionSource<bool>();
                var lookup = Reconcile(f.Client);
                f.Generation++; f.Session = SupportInquiryTests.Session("foreign_account");
                if (afterHttp) response.SetResult(RunApiHttpResponse.Success(200, Found));
                else f.Journal.ReadWait.SetResult(true);
                await lookup;
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(afterHttp ? 1 : 0));
                Assert.That(f.Journal.Removals, Is.Empty); Assert.That(f.Journal.Records.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task CorruptOrUnavailableReadFailsExplicitlyWithoutHttp()
        {
            using (var f = new Fixture(new JournalDouble { CorruptRead = true }))
            {
                Assert.ThrowsAsync<InvalidOperationException>(() => Reconcile(f.Client));
                Assert.That(f.Wire.Requests, Is.Empty);
                await Task.CompletedTask;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task NewOwnerReconciliationWaitsForOldGenerationThenQueriesCurrentOwner(bool changeAccount)
        {
            using (var f = new Fixture())
            {
                const string oldKey = "old_generation_receipt_key";
                const string newKey = "new_generation_receipt_key";
                var nextAccount = changeAccount ? "account_next" : "account_test";
                f.Journal.Records.Add(Metadata("account_test", oldKey, "ClientError"));
                var delayed = new TaskCompletionSource<RunApiHttpResponse>();
                f.Wire.Reply = _ => f.Wire.Requests.Count == 1 ? delayed.Task
                    : Task.FromResult(RunApiHttpResponse.Success(200, Found));
                var old = Reconcile(f.Client);
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
                AssertGet(f.Wire.Requests[0], oldKey);
                f.Generation++;
                f.Session = new GameApiAuthSessionSnapshot(nextAccount, "ACTIVE", new GameApiAccessToken("lst1.next_owner"),
                    DateTime.UtcNow.AddHours(1), new GameApiRefreshToken("next_owner_refresh"), DateTime.UtcNow.AddDays(1));
                if (!changeAccount) f.Journal.Records.Clear();
                f.Journal.Records.Add(Metadata(nextAccount, newKey, "ClientError"));
                var next = Reconcile(f.Client);
                Assert.That(next.IsCompleted, Is.False);
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(1), "New-owner lookup must serialize behind the outstanding lookup.");
                delayed.SetResult(RunApiHttpResponse.Success(200, Found));
                await old;
                await next;
                Assert.That(f.Journal.Reads, Is.EqualTo(new[] { "account_test", nextAccount }));
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(2), "Awaiting the old operation alone is not reconciliation for the new owner.");
                AssertGet(f.Wire.Requests[1], newKey);
                Assert.That(f.Wire.Requests[1].Headers.Single(h => h.Name == "Authorization").Value,
                    Is.EqualTo("Bearer lst1.next_owner"));
                Assert.That(f.Journal.Removals, Is.EqualTo(new[] { nextAccount + ":ClientError:" + newKey }));
                Assert.That(f.Journal.Puts, Is.Empty);
                if (changeAccount) Assert.That(Member(f.Journal.Records.Single(), "IdempotencyKey"), Is.EqualTo(oldKey));
            }
        }

        [Test]
        public async Task NotFoundRetainsMetadataWithoutCreatingPostOrNewKey()
        {
            using (var f = new Fixture())
            {
                f.Journal.Records.Add(Metadata());
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(404, "{\"accepted\":false,\"receiptStatus\":\"NOT_FOUND\"}"));
                await Reconcile(f.Client); await Reconcile(f.Client);
                Assert.That(f.Journal.Records.Count, Is.EqualTo(1)); Assert.That(f.Journal.Puts, Is.Empty);
                Assert.That(f.Journal.Removals, Is.Empty);
                foreach (var request in f.Wire.Requests) AssertGet(request, "receipt_fixture_key");
            }
        }

        [Test]
        public async Task OptionalMatchingAccountEchoIsAccepted()
        {
            using (var f = new Fixture())
            {
                f.Journal.Records.Add(Metadata());
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200,
                    Found.Replace("\"receiptStatus\":\"FOUND\"", "\"receiptStatus\":\"FOUND\",\"accountId\":\"account_test\"")));
                await Reconcile(f.Client);
                Assert.That(f.Journal.Records, Is.Empty);
                AssertGet(f.Wire.Requests.Single(), "receipt_fixture_key");
            }
        }

        [TestCase("\"receiptStatus\":\"FOUND\"", "\"receiptStatus\":\"FOUND\",\"accountId\":\"foreign_account\"")]
        [TestCase("\"ingestId\":\"ingest_fixture\"", "\"ingestId\":null")]
        [TestCase("\"acceptedEventCount\":1", "\"acceptedEventCount\":-1")]
        [TestCase("\"acceptedEventCount\":1", "\"acceptedEventCount\":\"1\"")]
        [TestCase("2026-09-06T00:00:00.000Z", "invalid-time")]
        public async Task InvalidFoundCannotDeletePendingMetadata(string before, string after)
        {
            using (var f = new Fixture())
            {
                f.Journal.Records.Add(Metadata());
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200, Found.Replace(before, after)));
                await Reconcile(f.Client);
                Assert.That(f.Journal.Removals, Is.Empty); Assert.That(f.Journal.Records.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task ConfirmedPostDeleteFailureDoesNotResendAndDuplicateFoundCanFinishCleanup()
        {
            using (var f = new Fixture(new JournalDouble { RemoveResult = false }))
            using (var runtime = new CurrentSessionErrorRuntime(f.Client))
            {
                Assert.That(runtime.Record(SafeClientErrorKind.UnityError, 7), Is.True);
                await runtime.FlushAsync();
                Assert.That(runtime.PendingCount, Is.Zero); Assert.That(f.Journal.Records.Count, Is.EqualTo(1));
                var key = Key(f.Wire.Requests.Single());
                await runtime.FlushAsync(); Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200, Found));
                await Reconcile(runtime); Assert.That(f.Journal.Records.Count, Is.EqualTo(1));
                f.Journal.RemoveResult = true; await Reconcile(runtime); await Reconcile(runtime);
                Assert.That(f.Journal.Records, Is.Empty);
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(3));
                foreach (var request in f.Wire.Requests.Skip(1)) AssertGet(request, key);
            }
        }

        private static Type Shared(string name)
        {
            var type = typeof(CurrentSessionErrorClient).Assembly.GetType("LastRail.Submissions." + name);
            Assert.That(type, Is.Not.Null, "RED: missing shared receipt type " + name); return type;
        }
        private static object Member(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        private static object Metadata(string account = "account_test", string key = "receipt_fixture_key", string kind = "ClientError") =>
            Activator.CreateInstance(Shared("PendingReceipt"), Enum.Parse(Shared("PendingReceiptKind"), kind), account, key,
                kind == "ClientError" ? "UNITY_ERROR" : "BUG_OTHER", new AppBuildSnapshot("1.2.3", 123),
                DateTimeOffset.Parse("2026-09-06T00:00:00Z"), DateTimeOffset.Parse("2026-09-06T00:00:00Z"));
        private static ConstructorInfo ClientConstructor()
        {
            var constructor = typeof(CurrentSessionErrorClient).GetConstructor(new[] { typeof(IGameApiAuthenticatedHttpTransport),
                typeof(Func<GameApiAuthSessionSnapshot>), typeof(Func<long>), typeof(Func<AppBuildSnapshot>), typeof(Func<DateTimeOffset>), Shared("IPendingReceiptJournal") });
            Assert.That(constructor, Is.Not.Null, "RED: append journal in a six-argument overload; preserve the existing constructor."); return constructor;
        }
        private static MethodInfo RecoveryMethod(Type type)
        {
            var method = type.GetMethod("ReconcilePendingAsync", Type.EmptyTypes);
            Assert.That(method, Is.Not.Null, "RED: missing explicit receipt-only recovery on " + type.Name); return method;
        }
        private static Task Reconcile(object target)
        {
            try { return (Task)RecoveryMethod(target.GetType()).Invoke(target, null); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        private static string Key(RunApiHttpRequest request) => request.Headers.Single(h => h.Name == "Idempotency-Key").Value;
        private static void AssertGet(RunApiHttpRequest request, string key)
        {
            Assert.That(request.Method, Is.EqualTo("GET")); Assert.That(request.EndpointPath, Is.EqualTo("/v1/client-errors/receipt"));
            Assert.That(request.BodyJson, Is.Null); Assert.That(Key(request), Is.EqualTo(key));
        }

        private static object Adapter(JournalDouble journal)
        {
            var contract = Shared("IPendingReceiptJournal");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ErrorReceiptJournalTest_" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            var type = assembly.DefineDynamicModule("Main").DefineType("JournalAdapter", TypeAttributes.Public | TypeAttributes.Sealed);
            type.AddInterfaceImplementation(contract);
            var owner = type.DefineField("owner", typeof(JournalDouble), FieldAttributes.Private | FieldAttributes.InitOnly);
            var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(JournalDouble) });
            var il = ctor.GetILGenerator(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Stfld, owner); il.Emit(OpCodes.Ret);
            foreach (var method in contract.GetMethods())
            {
                var parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
                var implementation = type.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
                    method.ReturnType, parameters);
                il = implementation.GetILGenerator(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, owner); il.Emit(OpCodes.Ldstr, method.Name);
                il.Emit(OpCodes.Ldc_I4, parameters.Length); il.Emit(OpCodes.Newarr, typeof(object));
                for (var i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Dup); il.Emit(OpCodes.Ldc_I4, i); il.Emit(OpCodes.Ldarg, i + 1);
                    if (parameters[i].IsValueType) il.Emit(OpCodes.Box, parameters[i]); il.Emit(OpCodes.Stelem_Ref);
                }
                il.Emit(OpCodes.Callvirt, typeof(JournalDouble).GetMethod(nameof(JournalDouble.Dispatch)));
                il.Emit(OpCodes.Castclass, method.ReturnType); il.Emit(OpCodes.Ret); type.DefineMethodOverride(implementation, method);
            }
            return Activator.CreateInstance(type.CreateType(), journal);
        }
    }
}
