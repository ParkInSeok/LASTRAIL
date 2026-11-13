using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class CurrentSessionErrorRuntimeTests
    {
        private const string Receipt = "{\"accepted\":true,\"ingestStatus\":\"RECEIVED\",\"ingestId\":\"ingest_fixture\",\"acceptedEventCount\":1,\"droppedEventCount\":0,\"serverReceivedAt\":\"2026-09-06T00:00:00.000Z\"}";
        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest, Task<RunApiHttpResponse>> Reply = _ => Task.FromResult(Received());
            public int Active, MaxActive;
            public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Requests.Add(request); Active++; MaxActive = Math.Max(MaxActive, Active);
                try { return await Reply(request); }
                finally { Active--; }
            }
        }
        private sealed class Fixture : IDisposable
        {
            public readonly Wire Wire = new Wire();
            public GameApiAuthSessionSnapshot Session = SupportInquiryTests.Session("account_test");
            public long Generation = 7;
            public AppBuildSnapshot Build = new AppBuildSnapshot("1.0", 11);
            public DateTimeOffset Now = DateTimeOffset.Parse("2026-09-06T00:00:00Z");
            public readonly CurrentSessionErrorClient Client;
            public readonly object Runtime;
            public Fixture()
            {
                var type = RuntimeType();
                Client = new CurrentSessionErrorClient(new GameApiAuthenticatedHttpTransport(Wire),
                    () => Session, () => Generation, () => Build, () => Now);
                Runtime = Activator.CreateInstance(type, Client);
            }
            public bool Record(SafeClientErrorKind kind = SafeClientErrorKind.UnityError, long origin = 7) =>
                (bool)Runtime.GetType().GetMethod("Record").Invoke(Runtime, new object[] { kind, origin });
            public Task Flush() => (Task)Runtime.GetType().GetMethod("FlushAsync").Invoke(Runtime, null);
            public int Count => (int)Runtime.GetType().GetProperty("PendingCount").GetValue(Runtime);
            public void Dispose() { ((IDisposable)Runtime).Dispose(); Client.Dispose(); }
        }
        private static Type RuntimeType()
        {
            var type = typeof(CurrentSessionErrorClient).Assembly.GetType("LastRail.Diagnostics.CurrentSessionErrorRuntime");
            Assert.That(type, Is.Not.Null, "Missing explicit current-session error runtime.");
            return type;
        }
        private static RunApiHttpResponse Received() => RunApiHttpResponse.Success(200, Receipt);
        private static string Key(RunApiHttpRequest request) => request.Headers.Single(h => h.Name == "Idempotency-Key").Value;

        [Test]
        public void PublicApiAcceptsOnlyTypedCurrentGenerationEvents()
        {
            var type = RuntimeType();
            Assert.That(type.IsPublic, Is.True); Assert.That(typeof(IDisposable).IsAssignableFrom(type), Is.True);
            Assert.That(type.GetConstructors().Length, Is.EqualTo(1));
            Assert.That(type.GetConstructors()[0].GetParameters().Select(p => p.ParameterType), Is.EqualTo(new[] { typeof(CurrentSessionErrorClient) }));
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName).ToArray();
            Assert.That(methods.Select(m => m.Name), Is.EquivalentTo(new[] { "Record", "FlushAsync", "ReconcilePendingAsync", "Dispose" }));
            var record = methods.Single(m => m.Name == "Record");
            Assert.That(record.ReturnType, Is.EqualTo(typeof(bool)));
            Assert.That(record.GetParameters().Select(p => p.ParameterType), Is.EqualTo(new[] { typeof(SafeClientErrorKind), typeof(long) }));
            Assert.That(type.GetMethod("FlushAsync", Type.EmptyTypes).ReturnType, Is.EqualTo(typeof(Task)));
            Assert.That(type.GetProperty("PendingCount").PropertyType, Is.EqualTo(typeof(int)));
            Assert.That(type.GetProperty("PendingCount").GetSetMethod(), Is.Null);
        }

        [Test]
        public async Task CaptureIsBoundedAt25AndDoesNotPublishUntilExplicitFlush()
        {
            using (var f = new Fixture())
            {
                for (var i = 0; i < 25; i++) Assert.That(f.Record(), Is.True, "Capture " + i);
                Assert.That(f.Record(), Is.False); Assert.That(f.Count, Is.EqualTo(25));
                Assert.That(f.Client.PendingCount, Is.EqualTo(25)); Assert.That(f.Wire.Requests, Is.Empty);
                await f.Flush();
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(25)); Assert.That(f.Count, Is.Zero);
                Assert.That(f.Wire.Requests.Select(Key).Distinct().Count(), Is.EqualTo(25));
                Assert.That(f.Record(), Is.True, "Received items release queue capacity.");
            }
        }

        [Test]
        public async Task MissingOwnerStaleOriginAndInvalidKindsAreRejectedWithoutHttp()
        {
            using (var f = new Fixture())
            {
                Assert.That(f.Record(origin: 6), Is.False);
                Assert.That(f.Record((SafeClientErrorKind)999), Is.False);
                f.Session = null; Assert.That(f.Record(), Is.False);
                await f.Flush(); Assert.That(f.Count, Is.Zero); Assert.That(f.Wire.Requests, Is.Empty);
            }
        }

        [Test]
        public async Task RetryKeepsCaptureTimeBuildBodyAndKeyAcrossSameAccountRefresh()
        {
            using (var f = new Fixture())
            {
                Assert.That(f.Record(SafeClientErrorKind.UnityAssert), Is.True);
                f.Now = f.Now.AddMinutes(1); f.Build = new AppBuildSnapshot("2.0", 22);
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.NetworkError("synthetic transport failure"));
                await f.Flush(); Assert.That(f.Count, Is.EqualTo(1)); Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
                f.Session = SupportInquiryTests.Session("account_test");
                f.Wire.Reply = _ => Task.FromResult(Received()); await f.Flush();
                Assert.That(f.Count, Is.Zero); Assert.That(f.Client.PendingCount, Is.Zero);
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(2));
                Assert.That(f.Wire.Requests[1].BodyJson, Is.EqualTo(f.Wire.Requests[0].BodyJson));
                Assert.That(Key(f.Wire.Requests[1]), Is.EqualTo(Key(f.Wire.Requests[0])));
                var json = JObject.Parse(f.Wire.Requests[0].BodyJson);
                Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] { "events" }));
                var item = (JObject)json["events"][0];
                Assert.That(item.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "kind", "occurredAt", "occurrenceCount", "appVersion", "appVersionCode", "scope" }));
                Assert.That((string)item["kind"], Is.EqualTo("UNITY_ASSERT"));
                Assert.That((string)item["appVersion"], Is.EqualTo("1.0"));
                Assert.That((string)item["appVersionCode"], Is.EqualTo("11"));
                // JObject's default date parsing is deliberately avoided for the timestamp assertion.
                Assert.That(f.Wire.Requests[0].BodyJson, Does.Contain("2026-09-06T00:00:00.000Z"));
                Assert.That((string)item["scope"], Is.EqualTo("AUTHENTICATED_CURRENT_SESSION"));
            }
        }

        [Test]
        public async Task ConfirmedMalformedEventIsNotResentOrReportedAsReceived()
        {
            using(var f=new Fixture()){
                Assert.IsTrue(f.Record());
                f.Wire.Reply=_=>Task.FromResult(RunApiHttpResponse.Success(400,"{\"accepted\":false,\"errorCode\":\"contract_violation\"}"));
                await f.Flush();await f.Flush();
                Assert.That(f.Wire.Requests.Count,Is.EqualTo(1));Assert.That(f.Count,Is.Zero);
                var counter=f.Runtime.GetType().GetProperty("RejectedDeliveryCount");Assert.IsNotNull(counter);
                Assert.That(counter.GetValue(f.Runtime),Is.EqualTo(1L));
            }
        }
        [Test]
        public async Task UnknownItemsAreAttemptedOnceEachPerFlushWithoutEndlessRetry()
        {
            using (var f = new Fixture())
            {
                for (var i = 0; i < 3; i++) Assert.That(f.Record((SafeClientErrorKind)i), Is.True);
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.NetworkError("synthetic transport failure"));
                await f.Flush(); Assert.That(f.Count, Is.EqualTo(3)); Assert.That(f.Wire.Requests.Count, Is.EqualTo(3));
                var keys = f.Wire.Requests.Select(Key).ToArray();
                for (var i = 0; i < 3; i++) await Task.Yield();
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(3), "No autonomous retry after a flush.");
                await f.Flush(); Assert.That(f.Wire.Requests.Count, Is.EqualTo(6));
                Assert.That(f.Wire.Requests.Skip(3).Select(Key), Is.EqualTo(keys));
            }
        }

        [Test]
        public async Task RateLimitedItemRetainsOriginalRequestUntilExplicitRetryAfterServerWait()
        {
            using (var f = new Fixture())
            {
                Assert.That(f.Record(), Is.True);
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(429,
                    "{\"accepted\":false,\"errorCode\":\"client_error_ingest_rate_limited\",\"retryAfterSeconds\":120}"));
                await f.Flush(); await f.Flush(); Assert.That(f.Count, Is.EqualTo(1)); Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
                f.Now = f.Now.AddSeconds(119); await f.Flush(); Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
                f.Now = f.Now.AddSeconds(1); f.Wire.Reply = _ => Task.FromResult(Received());
                await f.Flush(); Assert.That(f.Count, Is.Zero); Assert.That(f.Wire.Requests.Count, Is.EqualTo(2));
                Assert.That(Key(f.Wire.Requests[1]), Is.EqualTo(Key(f.Wire.Requests[0])));
                Assert.That(f.Wire.Requests[1].BodyJson, Is.EqualTo(f.Wire.Requests[0].BodyJson));
            }
        }

        [TestCase("generation")]
        [TestCase("account")]
        [TestCase("signed_out")]
        public async Task StalePendingItemsAreRemovedWithoutReattributingOrSending(string change)
        {
            using (var f = new Fixture())
            {
                Assert.That(f.Record(), Is.True);
                if (change == "generation") f.Generation++;
                else if (change == "account") f.Session = SupportInquiryTests.Session("account_other");
                else f.Session = null;
                await f.Flush(); Assert.That(f.Count, Is.Zero); Assert.That(f.Client.PendingCount, Is.Zero);
                Assert.That(f.Wire.Requests, Is.Empty);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task FullOldOwnerQueueReleasesCapacityWithoutSendingOrAccumulatingStaleEntries(bool changeAccount)
        {
            using (var f = new Fixture())
            {
                for (var i = 0; i < 25; i++) Assert.That(f.Record(), Is.True);
                Assert.That(f.Count, Is.EqualTo(25));
                f.Generation++;
                if (changeAccount) f.Session = SupportInquiryTests.Session("account_other");
                var accepted = f.Record(SafeClientErrorKind.UnityAssert, f.Generation);
                Assert.That(f.Count, Is.LessThanOrEqualTo(25), "A replacement capture must not append beyond the wrapper bound.");
                if (!accepted)
                {
                    // Both eager pruning and an explicit stale-only flush may release the old queue.
                    await f.Flush();
                    Assert.That(f.Count, Is.Zero); Assert.That(f.Wire.Requests, Is.Empty);
                    accepted = f.Record(SafeClientErrorKind.UnityAssert, f.Generation);
                }
                Assert.That(accepted, Is.True, "Old-owner capacity must not permanently block the new owner.");
                Assert.That(f.Count, Is.EqualTo(1), "The wrapper must discard the same stale requests that Capture prunes.");
                Assert.That(f.Client.PendingCount, Is.EqualTo(1)); Assert.That(f.Wire.Requests, Is.Empty);
                await f.Flush();
                Assert.That(f.Count, Is.Zero); Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
                Assert.That((string)JObject.Parse(f.Wire.Requests.Single().BodyJson)["events"][0]["kind"], Is.EqualTo("UNITY_ASSERT"));
            }
        }

        [Test]
        public async Task ConcurrentFlushesNeverOverlapAndNewCapturesWaitForNextExplicitFlush()
        {
            using (var f = new Fixture())
            {
                var pending = new TaskCompletionSource<RunApiHttpResponse>();
                f.Wire.Reply = _ => pending.Task;
                Assert.That(f.Record(), Is.True); Assert.That(f.Record(SafeClientErrorKind.UnityAssert), Is.True);
                var first = f.Flush(); var second = f.Flush();
                try
                {
                    Assert.That(f.Wire.Requests.Count, Is.EqualTo(1)); Assert.That(f.Wire.MaxActive, Is.EqualTo(1));
                    Assert.That(f.Record(SafeClientErrorKind.UnityException), Is.True);
                    f.Wire.Reply = _ => Task.FromResult(Received());
                }
                finally { pending.TrySetResult(Received()); }
                await Task.WhenAll(first, second);
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(2)); Assert.That(f.Wire.MaxActive, Is.EqualTo(1));
                Assert.That(f.Count, Is.EqualTo(1)); await f.Flush(); Assert.That(f.Count, Is.Zero);
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(3));
            }
        }

        [Test]
        public async Task AccountChangeDuringSendDiscardsRemainingOldRequests()
        {
            using (var f = new Fixture())
            {
                var pending = new TaskCompletionSource<RunApiHttpResponse>();
                f.Wire.Reply = _ => pending.Task;
                Assert.That(f.Record(), Is.True); Assert.That(f.Record(), Is.True);
                var flush = f.Flush();
                f.Generation++; f.Session = SupportInquiryTests.Session("account_other");
                pending.SetResult(Received()); await flush;
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(1)); Assert.That(f.Count, Is.Zero);
                Assert.That(f.Record(origin: f.Generation), Is.True);
                f.Wire.Reply = _ => Task.FromResult(Received()); await f.Flush();
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(2));
                Assert.That(Key(f.Wire.Requests[1]), Is.Not.EqualTo(Key(f.Wire.Requests[0])));
            }
        }

        [Test]
        public async Task DisposeClearsQueueAndFencesLateCompletionAndFutureFlushes()
        {
            using (var f = new Fixture())
            {
                var pending = new TaskCompletionSource<RunApiHttpResponse>();
                f.Wire.Reply = _ => pending.Task;
                Assert.That(f.Record(), Is.True); Assert.That(f.Record(), Is.True);
                var flush = f.Flush();
                ((IDisposable)f.Runtime).Dispose();
                Assert.That(f.Count, Is.Zero); Assert.That(f.Record(), Is.False);
                pending.SetResult(Received()); await flush; await f.Flush();
                Assert.That(f.Count, Is.Zero); Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task DisposedQueueDoesNotTransferIntoAnotherRuntime()
        {
            using (var old = new Fixture())
            {
                Assert.That(old.Record(), Is.True); ((IDisposable)old.Runtime).Dispose();
                using (var current = new Fixture())
                {
                    await current.Flush(); Assert.That(current.Count, Is.Zero); Assert.That(current.Wire.Requests, Is.Empty);
                }
                Assert.That(old.Wire.Requests, Is.Empty);
            }
        }

        [Test]
        public void RuntimeHasNoDirectLegacyQueueFileOrGlobalLogDependencies()
        {
            // Inspect compiled references, including async state machines, without opening any legacy files.
            foreach (var type in ImplementationTypes(RuntimeType()))
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    AllowedType(field.FieldType);
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Cast<MethodBase>()
                    .Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
                foreach (var method in methods)
                    foreach (var member in ReferencedMembers(method))
                    {
                        AllowedType(member as Type ?? member.DeclaringType);
                        Assert.That(member.DeclaringType == typeof(UnityEngine.Application) && member.Name.StartsWith("add_logMessageReceived", StringComparison.Ordinal), Is.False,
                            "Only explicit typed producers may record errors.");
                    }
            }
        }
        private static IEnumerable<Type> ImplementationTypes(Type type)
        {
            yield return type;
            foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                foreach (var child in ImplementationTypes(nested)) yield return child;
        }
        private static void AllowedType(Type type)
        {
            if (type == null) return;
            Assert.That(type.FullName ?? "", Does.Not.Contain("SafeClientErrorQueue"));
            Assert.That(type.FullName ?? "", Does.Not.Contain("LocalClientErrorCapture"));
            Assert.That(type.Namespace == "System.IO" || (type.Namespace ?? "").StartsWith("System.IO.", StringComparison.Ordinal), Is.False,
                "This runtime may not read or persist process-wide error files.");
        }
        private static IEnumerable<MemberInfo> ReferencedMembers(MethodBase method)
        {
            var bytes = method.GetMethodBody()?.GetILAsByteArray();
            if (bytes == null) yield break;
            var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)).ToDictionary(o => unchecked((ushort)o.Value));
            for (var offset = 0; offset < bytes.Length;)
            {
                var code = (ushort)bytes[offset++];
                if (code == 0xfe) code = (ushort)(0xfe00 | bytes[offset++]);
                var operand = opcodes[code].OperandType;
                if (operand == OperandType.InlineMethod || operand == OperandType.InlineField || operand == OperandType.InlineType || operand == OperandType.InlineTok)
                    yield return method.Module.ResolveMember(BitConverter.ToInt32(bytes, offset), method.DeclaringType.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
                switch (operand)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar: offset++; break;
                    case OperandType.InlineVar: offset += 2; break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR: offset += 8; break;
                    case OperandType.InlineSwitch: offset += 4 + 4 * BitConverter.ToInt32(bytes, offset); break;
                    default: offset += 4; break;
                }
            }
        }
    }
}
