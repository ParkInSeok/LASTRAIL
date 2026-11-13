using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class ReceiptTrackingSupportClientTests
    {
        private const string Account = "account_fixture";
        private const string Body = "synthetic private inquiry body";
        private const string Key = "11111111111111111111111111111111";
        private static readonly DateTimeOffset Origin = DateTimeOffset.Parse("2026-09-06T00:00:00.000Z");

        // The not-yet-created journal interface is implemented dynamically only in this fixture.
        public class JournalProxy : DispatchProxy
        {
            public readonly List<object> Rows = new List<object>();
            public readonly List<object> Puts = new List<object>();
            public readonly List<string> Removes = new List<string>();
            public Func<object, Task<bool>> Put = _ => Task.FromResult(true);
            public Func<Task<bool>> Remove = () => Task.FromResult(true);
            protected override object Invoke(MethodInfo method, object[] args)
            {
                if (method.Name == "PutAsync") return Save(args[0]);
                if (method.Name == "RemoveAsync") return Delete((string)args[0], args[1], (string)args[2]);
                if (method.Name == "ReadAsync")
                {
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Required("LastRail.Submissions.PendingReceipt")));
                    foreach (var row in Rows.Where(r => (string)Get(r, "AccountId") == (string)args[0])) list.Add(row);
                    return typeof(Task).GetMethods().Single(m => m.Name == "FromResult").MakeGenericMethod(method.ReturnType.GetGenericArguments()[0]).Invoke(null, new object[] { list });
                }
                throw new NotSupportedException(method.Name);
            }
            private async Task<bool> Save(object record)
            {
                Puts.Add(record);
                if (!await Put(record)) return false;
                if (!Rows.Any(r => Identity(r) == Identity(record))) Rows.Add(record);
                return true;
            }
            private async Task<bool> Delete(string account, object kind, string key)
            {
                var id = account + ":" + kind + ":" + key; Removes.Add(id);
                if (!await Remove()) return false;
                Rows.RemoveAll(r => Identity(r) == id); return true;
            }
            private static string Identity(object record) => Get(record, "AccountId") + ":" + Get(record, "Kind") + ":" + Get(record, "IdempotencyKey");
        }
        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest, Task<RunApiHttpResponse>> Reply = _ => Task.FromResult(RunApiHttpResponse.NetworkError("synthetic lost response"));
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            { Requests.Add(request); return Reply(request); }
        }
        private sealed class Fixture
        {
            public readonly Wire Wire = new Wire();
            public readonly JournalProxy Journal;
            public readonly SupportInquiryHttpClient Inner;
            public readonly ISupportInquiryClient Client;
            public readonly ISupportInquiryReconciliationClient Lookup;
            public GameApiAuthSessionSnapshot Session = Auth();
            public long Generation = 1;
            public Fixture(JournalProxy journal = null)
            {
                var wrapper = Required("LastRail.Support.ReceiptTrackingSupportClient");
                var journalType = Required("LastRail.Submissions.IPendingReceiptJournal");
                Journal = journal ?? (JournalProxy)typeof(DispatchProxy).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m => m.Name == "Create" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 2)
                    .MakeGenericMethod(journalType, typeof(JournalProxy)).Invoke(null, null);
                Inner = new SupportInquiryHttpClient(new GameApiAuthenticatedHttpTransport(Wire), () => new AppBuildSnapshot("1.0", 1));
                var instance = Activator.CreateInstance(wrapper, Inner, Journal,
                    (Func<long>)(() => Generation), (Func<GameApiAuthSessionSnapshot>)(() => Session));
                Client = (ISupportInquiryClient)instance; Lookup = (ISupportInquiryReconciliationClient)instance;
            }
            public SupportInquiryController Controller() => new SupportInquiryController(Session, Client,
                SupportInquiryHttpClient.Categories, 2000, () => new AppBuildSnapshot("1.0", 1), () => Origin, 10);
        }
        private static Type Required(string name)
        {
            var type = typeof(SupportInquiryHttpClient).Assembly.GetType(name);
            Assert.That(type, Is.Not.Null, "Missing proposed API: " + name); return type;
        }
        private static object Get(object value, string property) => value.GetType().GetProperty(property).GetValue(value);
        private static GameApiAuthSessionSnapshot Auth(string account = Account, string token = "lst1.fixture") =>
            new GameApiAuthSessionSnapshot(account, "ACTIVE", new GameApiAccessToken(token), Origin.UtcDateTime.AddYears(4),
                new GameApiRefreshToken("synthetic_refresh_token"), Origin.UtcDateTime.AddYears(5));
        private static InquirySubmission Submission(string key = Key) => (InquirySubmission)Activator.CreateInstance(typeof(InquirySubmission),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { "BUG_OTHER", Body, key, new AppBuildSnapshot("1.0", 1), Origin, Origin.AddSeconds(1) }, null);
        private static object Record(string account = Account, string key = Key, string kind = "Support") =>
            Activator.CreateInstance(Required("LastRail.Submissions.PendingReceipt"),
                Enum.Parse(Required("LastRail.Submissions.PendingReceiptKind"), kind), account, key,
                kind == "Support" ? "BUG_OTHER" : "UNITY_ERROR", new AppBuildSnapshot("1.0", 1), Origin, Origin.AddSeconds(1));
        private static InquirySubmission Restore(SupportInquiryHttpClient inner, object record)
        {
            var method = typeof(SupportInquiryHttpClient).GetMethod("RestoreReceiptLookup", new[] { Required("LastRail.Submissions.PendingReceipt") });
            Assert.That(method, Is.Not.Null, "RestoreReceiptLookup(PendingReceipt) must be public.");
            Assert.That(method.ReturnType, Is.EqualTo(typeof(InquirySubmission)));
            return (InquirySubmission)method.Invoke(inner, new[] { record });
        }
        private static bool RestoreController(SupportInquiryController model, InquirySubmission restored)
        {
            var method = typeof(SupportInquiryController).GetMethod("RestorePendingReceipt", new[] { typeof(InquirySubmission) });
            Assert.That(method, Is.Not.Null); Assert.That(method.ReturnType, Is.EqualTo(typeof(bool)));
            return (bool)method.Invoke(model, new object[] { restored });
        }
        private static bool Independent(SupportInquiryController model)
        {
            var method = typeof(SupportInquiryController).GetMethod("StartIndependentInquiry", Type.EmptyTypes);
            Assert.That(method, Is.Not.Null); Assert.That(method.ReturnType, Is.EqualTo(typeof(bool)));
            return (bool)method.Invoke(model, null);
        }
        private static RunApiHttpResponse Received(string status = "RECEIVED") => RunApiHttpResponse.Success(200,
            new JObject { ["accepted"] = true, ["ticketStatus"] = status, ["ticketId"] = "ticket_fixture",
                ["category"] = "BUG_OTHER", ["serverReceivedAt"] = "2026-09-06T00:02:00.000Z" }.ToString(Formatting.None));
        private static RunApiHttpResponse Found(string account = Account) => RunApiHttpResponse.Success(200,
            new JObject { ["accepted"] = true, ["receiptStatus"] = "FOUND", ["ticketId"] = "ticket_fixture",
                ["accountId"] = account, ["category"] = "BUG_OTHER", ["bodyPreview"] = "synthetic private preview",
                ["serverReceivedAt"] = "2026-09-06T00:02:00.000Z" }.ToString(Formatting.None));

        [Test]
        public async Task MetadataMustFinishSavingBeforePostAndNeverContainsBodyOrTokens()
        {
            var f = new Fixture(); var gate = new TaskCompletionSource<bool>(); f.Journal.Put = _ => gate.Task;
            var send = f.Client.SubmitAsync(Submission(), f.Session);
            Assert.That(f.Journal.Puts.Count, Is.EqualTo(1)); Assert.That(f.Wire.Requests, Is.Empty);
            gate.SetResult(true); await send; Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
            var record = f.Journal.Rows.Single();
            Assert.That(Get(record, "Kind").ToString(), Is.EqualTo("Support"));
            Assert.That(Get(record, "AccountId"), Is.EqualTo(Account)); Assert.That(Get(record, "IdempotencyKey"), Is.EqualTo(Key));
            Assert.That(Get(record, "ComposedAt"), Is.EqualTo(Origin)); Assert.That(Get(record, "SubmittedAt"), Is.EqualTo(Origin.AddSeconds(1)));
            var metadata = JsonConvert.SerializeObject(record);
            foreach (var forbidden in new[] { Body, "lst1.fixture", "synthetic_refresh_token", "Content", "BodyJson" })
                Assert.That(metadata, Does.Not.Contain(forbidden));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task FailedOrThrowingSaveReturnsUnknownWithoutPost(bool throws)
        {
            var f = new Fixture();
            f.Journal.Put = _ => throws ? Task.FromException<bool>(new InvalidOperationException("synthetic unavailable store")) : Task.FromResult(false);
            Assert.That((await f.Client.SubmitAsync(Submission(), f.Session)).Delivery, Is.EqualTo(InquiryDelivery.Unknown));
            Assert.That(f.Wire.Requests, Is.Empty); Assert.That(f.Journal.Rows, Is.Empty);
        }

        [TestCase("generation")]
        [TestCase("account")]
        [TestCase("same_account_refresh")]
        [TestCase("signout")]
        public async Task OwnerChangeWhileSavingFencesPostAndRetainsOriginalMetadata(string change)
        {
            var f = new Fixture(); var gate = new TaskCompletionSource<bool>(); f.Journal.Put = _ => gate.Task;
            var send = f.Client.SubmitAsync(Submission(), f.Session);
            if (change == "generation") f.Generation++;
            else if (change == "account") f.Session = Auth("account_other");
            else if (change == "same_account_refresh") f.Session = Auth(token: "lst1.rotated");
            else f.Session = null;
            gate.SetResult(true);
            Assert.That((await send).Delivery, Is.EqualTo(InquiryDelivery.Unknown)); Assert.That(f.Wire.Requests, Is.Empty);
            Assert.That(Get(f.Journal.Rows.Single(), "AccountId"), Is.EqualTo(Account));
        }

        [Test]
        public async Task InMemoryRetryPreservesOriginalMetadataBodyAndKey()
        {
            var f = new Fixture(); var original = Submission();
            await f.Client.SubmitAsync(original, f.Session); var metadata = JsonConvert.SerializeObject(f.Journal.Rows.Single());
            f.Session = Auth(token: "lst1.rotated"); await f.Client.SubmitAsync(original, f.Session);
            Assert.That(f.Journal.Rows.Count, Is.EqualTo(1)); Assert.That(JsonConvert.SerializeObject(f.Journal.Rows.Single()), Is.EqualTo(metadata));
            Assert.That(f.Wire.Requests.Count, Is.EqualTo(2)); Assert.That(f.Wire.Requests[0].BodyJson, Is.EqualTo(f.Wire.Requests[1].BodyJson));
            Assert.That(f.Wire.Requests[1].Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo(Key));
            Assert.That(f.Wire.Requests[1].Headers.Single(h => h.Name == "Authorization").Value, Is.EqualTo("Bearer lst1.rotated"));
        }

        [TestCase("RECEIVED", true)]
        [TestCase("DUPLICATE_REPLAY", true)]
        [TestCase("RECEIVED", false)]
        public async Task ReceivedIsHonestEvenIfMetadataDeletionFails(string status, bool remove)
        {
            var f = new Fixture(); f.Wire.Reply = _ => Task.FromResult(Received(status)); f.Journal.Remove = () => Task.FromResult(remove);
            var result = await f.Client.SubmitAsync(Submission(), f.Session);
            Assert.That(result.Delivery, Is.EqualTo(InquiryDelivery.Received)); Assert.That(result.TicketId, Is.EqualTo("ticket_fixture"));
            Assert.That(f.Journal.Removes.Single(), Is.EqualTo(Account + ":Support:" + Key));
            Assert.That(f.Journal.Rows.Count, Is.EqualTo(remove ? 0 : 1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task FirstExplicitRejectionUnlocksOnlyAfterConfirmedMetadataRemoval(bool remove)
        {
            var f = new Fixture(); f.Journal.Remove = () => Task.FromResult(remove);
            f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(400,
                "{\"accepted\":false,\"ticketStatus\":\"REJECTED\",\"errorCode\":\"support_ticket_invalid\"}"));
            Assert.That((await f.Client.SubmitAsync(Submission(), f.Session)).Delivery,
                Is.EqualTo(remove ? InquiryDelivery.Rejected : InquiryDelivery.Unknown));
            Assert.That(f.Journal.Rows.Count, Is.EqualTo(remove ? 0 : 1));
        }

        [Test]
        public async Task RestoredMetadataIsGetOnlyAcrossNewClientInstances()
        {
            var first = new Fixture(); await first.Client.SubmitAsync(Submission(), first.Session);
            var restarted = new Fixture(first.Journal);
            var restored = Restore(restarted.Inner, first.Journal.Rows.Single());
            Assert.That(restored.Content, Is.Null); Assert.That(Get(restored, "ReceiptOnly"), Is.EqualTo(true));
            Assert.That(restored.IdempotencyKey, Is.EqualTo(Key));
            Assert.That((await restarted.Inner.SubmitAsync(restored, restarted.Session)).Delivery, Is.EqualTo(InquiryDelivery.Unknown));
            Assert.That((await restarted.Client.SubmitAsync(restored, restarted.Session)).Delivery, Is.EqualTo(InquiryDelivery.Unknown));
            Assert.That(restarted.Wire.Requests, Is.Empty);
            restarted.Wire.Reply = _ => Task.FromResult(Found());
            var receipt = await restarted.Lookup.ReconcileAsync(restored, restarted.Session);
            Assert.That(receipt.Delivery, Is.EqualTo(InquiryDelivery.Received)); Assert.That(first.Journal.Rows, Is.Empty);
            var request = restarted.Wire.Requests.Single();
            Assert.That(request.Method, Is.EqualTo("GET")); Assert.That(request.EndpointPath, Is.EqualTo("/v1/support/tickets/receipt"));
            Assert.That(string.IsNullOrEmpty(request.BodyJson), Is.True);
            Assert.That(request.Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo(Key));
        }

        [Test]
        public async Task ForeignRestoredReceiptCannotBeQueriedOrDeleted()
        {
            var f = new Fixture(); var record = Record("account_other"); f.Journal.Rows.Add(record);
            var restored = Restore(f.Inner, record);
            Assert.That((await f.Lookup.ReconcileAsync(restored, f.Session)).Delivery, Is.EqualTo(InquiryDelivery.Unknown));
            Assert.That(f.Wire.Requests, Is.Empty); Assert.That(f.Journal.Removes, Is.Empty); Assert.That(f.Journal.Rows.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task CapturedSubmissionCannotBeSavedAgainUnderAnotherAccount()
        {
            var f = new Fixture(); var original = Submission(); await f.Client.SubmitAsync(original, f.Session);
            f.Generation++; f.Session = Auth("account_other");
            Assert.That((await f.Client.SubmitAsync(original, f.Session)).Delivery, Is.EqualTo(InquiryDelivery.Unknown));
            Assert.That(f.Wire.Requests.Count, Is.EqualTo(1)); Assert.That(f.Journal.Rows.Count, Is.EqualTo(1));
            Assert.That(f.Journal.Puts.All(r => (string)Get(r, "AccountId") == Account), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task NotFoundAndFailedFoundDeletionRetainRestartMetadata(bool found)
        {
            var f = new Fixture(); var record = Record(); f.Journal.Rows.Add(record);
            var restored = Restore(f.Inner, record); f.Journal.Remove = () => Task.FromResult(false);
            f.Wire.Reply = _ => Task.FromResult(found ? Found() : RunApiHttpResponse.Success(404, "{\"accepted\":false,\"receiptStatus\":\"NOT_FOUND\"}"));
            var receipt = await f.Lookup.ReconcileAsync(restored, f.Session);
            Assert.That(receipt.Delivery, Is.EqualTo(found ? InquiryDelivery.Received : InquiryDelivery.Unknown));
            Assert.That(f.Journal.Rows.Count, Is.EqualTo(1)); Assert.That(f.Journal.Removes.Count, Is.EqualTo(found ? 1 : 0));
        }

        [TestCase("null")]
        [TestCase("uninitialized")]
        [TestCase("client_error")]
        public void MalformedOrWrongKindStoreReferencesCannotCreateLookup(string defect)
        {
            var f = new Fixture();
            var record = defect == "null" ? null : defect == "client_error" ? Record(kind: "ClientError")
                : FormatterServices.GetUninitializedObject(Required("LastRail.Submissions.PendingReceipt"));
            Assert.That(Restore(f.Inner, record), Is.Null);
            Assert.That(f.Wire.Requests, Is.Empty); Assert.That(f.Journal.Puts, Is.Empty);
        }

        [Test]
        public async Task RestoredControllerCannotEditOrPostButCanReconcile()
        {
            var f = new Fixture(); var record = Record(); f.Journal.Rows.Add(record);
            var restored = Restore(f.Inner, record); var model = f.Controller();
            Assert.That(RestoreController(model, restored), Is.True); Assert.That(model.State, Is.EqualTo(InquiryState.Uncertain));
            Assert.That(model.CanEdit, Is.False); Assert.That(model.CanSubmit, Is.False); Assert.That(model.CanReconcile, Is.True);
            Assert.That(string.IsNullOrEmpty(model.Content), Is.True);
            Assert.That(model.Edit("BUG_OTHER", "different synthetic content"), Is.False);
            await model.SubmitAsync(); Assert.That(f.Wire.Requests, Is.Empty);
            f.Wire.Reply = _ => Task.FromResult(Found()); await model.ReconcileAsync();
            Assert.That(model.State, Is.EqualTo(InquiryState.Received)); Assert.That(f.Journal.Rows, Is.Empty);
        }

        [Test]
        public async Task IndependentInquiryClearsBodyAndKeepsOldReceiptWithoutAutomaticPost()
        {
            var f = new Fixture(); var record = Record(); f.Journal.Rows.Add(record);
            var model = f.Controller(); Assert.That(RestoreController(model, Restore(f.Inner, record)), Is.True);
            Assert.That(model.StartAnother(), Is.False);
            Assert.That(Independent(model), Is.True); Assert.That(model.State, Is.EqualTo(InquiryState.Editing));
            Assert.That(model.Content, Is.Empty); Assert.That(model.Pending, Is.Null); Assert.That(model.CanSubmit, Is.False);
            Assert.That(f.Journal.Rows.Count, Is.EqualTo(1)); Assert.That(f.Journal.Removes, Is.Empty); Assert.That(f.Wire.Requests, Is.Empty);
            Assert.That(model.Edit("BUG_OTHER", "independent synthetic inquiry"), Is.True); await model.SubmitAsync();
            Assert.That(model.Pending.IdempotencyKey, Is.Not.EqualTo(Key)); Assert.That(f.Journal.Rows.Count, Is.EqualTo(2));
            Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
            Assert.That((string)JObject.Parse(f.Wire.Requests.Single().BodyJson)["body"], Is.EqualTo("independent synthetic inquiry"));
        }
    }
}
