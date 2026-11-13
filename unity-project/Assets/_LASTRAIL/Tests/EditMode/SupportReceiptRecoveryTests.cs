using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Submissions;
using LastRail.Support;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class SupportReceiptRecoveryTests
    {
        private sealed class Journal : IPendingReceiptJournal
        {
            public readonly List<PendingReceipt> Rows = new List<PendingReceipt>();
            public bool FailRead, FailDelete;
            public int Deletes;
            public TaskCompletionSource<IReadOnlyList<PendingReceipt>> Deferred;
            public Task<bool> PutAsync(PendingReceipt r) { Rows.Add(r); return Task.FromResult(true); }
            public Task<IReadOnlyList<PendingReceipt>> ReadAsync(string account)
            {
                if (FailRead) throw new InvalidOperationException("synthetic corruption");
                return Deferred?.Task ?? Task.FromResult<IReadOnlyList<PendingReceipt>>(Rows.ToArray());
            }
            public Task<bool> RemoveAsync(string account, PendingReceiptKind kind, string key)
            {
                Deletes++;
                if (FailDelete) return Task.FromResult(false);
                Rows.RemoveAll(r => r.AccountId == account && r.Kind == kind && r.IdempotencyKey == key);
                return Task.FromResult(true);
            }
        }
        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public RunApiHttpResponse Reply = RunApiHttpResponse.Success(404, "{}");
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            { Requests.Add(request); return Task.FromResult(Reply); }
        }
        private sealed class ReceiptWithCooldown : ISupportInquiryClient, ISupportInquiryReconciliationClient
        {
            public Task<InquiryReceipt> SubmitAsync(InquirySubmission submission, GameApiAuthSessionSnapshot session) =>
                Task.FromResult(new InquiryReceipt(InquiryDelivery.Unknown));
            public Task<InquiryReceipt> ReconcileAsync(InquirySubmission submission, GameApiAuthSessionSnapshot session) =>
                Task.FromResult(new InquiryReceipt(InquiryDelivery.Received, "ticket_fixture", DateTimeOffset.UtcNow, 120));
        }
        private sealed class Fixture
        {
            public readonly Journal Journal = new Journal();
            public readonly Wire Wire = new Wire();
            public GameApiAuthSessionSnapshot Session = new GameApiAuthSessionSnapshot("account_test", "ACTIVE",
                new GameApiAccessToken("lst1.fixture"), DateTime.UtcNow.AddDays(1),
                new GameApiRefreshToken("fixture_refresh"), DateTime.UtcNow.AddDays(2));
            public long Generation = 1;
            public DateTimeOffset Now = DateTimeOffset.UtcNow;
            public readonly SupportInquiryController Controller;
            public readonly object Model;
            public Fixture()
            {
                var inner = new SupportInquiryHttpClient(new GameApiAuthenticatedHttpTransport(Wire));
                var tracked = new ReceiptTrackingSupportClient(inner, Journal, () => Generation, () => Session);
                Controller = new SupportInquiryController(Session, tracked, SupportInquiryHttpClient.Categories,
                    2000, () => new AppBuildSnapshot("1.0", 1), () => Now, minContentLength: 10);
                var type = typeof(SupportInquiryController).Assembly.GetType("LastRail.Support.SupportReceiptRecovery");
                Assert.That(type, Is.Not.Null, "Missing receipt recovery UX model");
                Model = Activator.CreateInstance(type, Controller, inner, Journal,
                    (Func<GameApiAuthSessionSnapshot>)(() => Session), (Func<long>)(() => Generation));
            }
            public object Call(string name, params object[] args) => Model.GetType().GetMethod(name).Invoke(Model, args);
            public T Get<T>(string name) => (T)Model.GetType().GetProperty(name).GetValue(Model);
            public Task Refresh() => (Task)Call("RefreshAsync");
        }
        private static PendingReceipt Record(string account = "account_test", PendingReceiptKind kind = PendingReceiptKind.Support) =>
            new PendingReceipt(kind, account, Guid.NewGuid().ToString("N"),
                kind == PendingReceiptKind.Support ? "BUG_OTHER" : "UNITY_ERROR", new AppBuildSnapshot("1.0", 1),
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        [Test] public async Task FiltersForeignMetadataAndRestoresOnlyGetLookup()
        {
            var f = new Fixture(); var row = Record(); f.Journal.Rows.Add(row); f.Journal.Rows.Add(Record("foreign"));
            await f.Refresh();
            Assert.That(f.Get<IReadOnlyList<PendingReceipt>>("Records").Count, Is.EqualTo(1));
            Assert.That(f.Controller.Pending.IdempotencyKey, Is.EqualTo(row.IdempotencyKey));
            Assert.That(f.Controller.CanSubmit, Is.False); Assert.That(f.Wire.Requests, Is.Empty);
            await f.Controller.ReconcileAsync();
            Assert.That(f.Wire.Requests.Single().Method, Is.EqualTo("GET"));
            Assert.That(f.Controller.State, Is.EqualTo(InquiryState.Uncertain));
            Assert.That(f.Journal.Rows, Has.Count.EqualTo(2));
        }
        [Test] public async Task ExplicitIndependentInquiryPreservesOldRecordAndStartsEmpty()
        {
            var f = new Fixture(); f.Journal.Rows.Add(Record()); await f.Refresh();
            Assert.That((bool)f.Call("StartIndependentInquiry"), Is.True);
            Assert.That(f.Controller.Content, Is.Empty); Assert.That(f.Controller.Pending, Is.Null);
            Assert.That(f.Controller.CanEdit, Is.True); Assert.That(f.Journal.Rows, Has.Count.EqualTo(1));
            await f.Refresh(); Assert.That(f.Controller.Pending, Is.Null, "Refresh must not hijack independent draft");
        }
        [Test] public async Task DeleteNeedsConfirmationAndNeverMakesNetworkRequest()
        {
            var f = new Fixture(); var row = Record(); f.Journal.Rows.Add(row); await f.Refresh();
            Assert.That(await (Task<bool>)f.Call("ForgetSelectedAsync", false), Is.False);
            Assert.That(f.Journal.Deletes, Is.Zero);
            Assert.That(await (Task<bool>)f.Call("ForgetSelectedAsync", true), Is.True);
            Assert.That(f.Journal.Rows, Is.Empty); Assert.That(f.Wire.Requests, Is.Empty);
            Assert.That(f.Controller.State, Is.EqualTo(InquiryState.Editing));
        }
        [Test] public async Task DeleteFailureRetainsSelectionAndOriginalKey()
        {
            var f = new Fixture(); var row = Record(); f.Journal.Rows.Add(row); await f.Refresh(); f.Journal.FailDelete = true;
            Assert.That(await (Task<bool>)f.Call("ForgetSelectedAsync", true), Is.False);
            Assert.That(f.Get<PendingReceipt>("Selected"), Is.SameAs(row));
            Assert.That(f.Controller.Pending.IdempotencyKey, Is.EqualTo(row.IdempotencyKey));
        }
        [Test] public async Task CorruptionIsVisibleAndNotSilentlyCleared()
        {
            var f = new Fixture(); f.Journal.Rows.Add(Record()); f.Journal.FailRead = true; await f.Refresh();
            Assert.That(f.Get<bool>("StorageFailed"), Is.True); Assert.That(f.Get<string>("Notice"), Is.Not.Empty);
            Assert.That(f.Journal.Deletes, Is.Zero); Assert.That(f.Wire.Requests, Is.Empty);
            f.Journal.FailRead = false; await f.Refresh(); Assert.That(f.Get<bool>("StorageFailed"), Is.False);
        }
        [Test] public async Task LateReadAfterOwnerChangeCannotPublishOrQuery()
        {
            var f = new Fixture(); f.Journal.Deferred = new TaskCompletionSource<IReadOnlyList<PendingReceipt>>();
            var read = f.Refresh(); f.Generation++; f.Session = null;
            f.Journal.Deferred.SetResult(new[] { Record() }); await read;
            Assert.That(f.Get<IReadOnlyList<PendingReceipt>>("Records"), Is.Empty);
            Assert.That(f.Controller.Pending, Is.Null); Assert.That(f.Wire.Requests, Is.Empty);
        }
        [TestCase(PendingReceiptKind.Support)]
        [TestCase(PendingReceiptKind.ClientError)]
        public async Task FullJournalManagementDeletionPreservesIndependentDraft(PendingReceiptKind kind)
        {
            var f = new Fixture(); var selected = Record(kind: kind); f.Journal.Rows.Add(selected);
            for (var i = 1; i < 64; i++) f.Journal.Rows.Add(Record());
            await f.Refresh(); Assert.That((bool)f.Call("StartIndependentInquiry"), Is.True);
            Assert.That(f.Controller.Edit("BUG_OTHER", "synthetic independent draft to preserve"), Is.True);
            Assert.That((bool)f.Call("Select", selected), Is.True);
            Assert.That(f.Get<bool>("ManagingDraft"), Is.True);
            Assert.That(f.Controller.Content, Is.EqualTo("synthetic independent draft to preserve"));
            Assert.That(f.Controller.Pending, Is.Null); Assert.That(f.Controller.CanEdit, Is.True);
            Assert.That(f.Get<string>("Notice"), Does.Contain("\uC720\uC9C0"));
            Assert.That(await (Task<bool>)f.Call("ForgetSelectedAsync", true), Is.True);
            Assert.That(f.Journal.Rows.Count, Is.EqualTo(63)); Assert.That(f.Journal.Rows.Contains(selected), Is.False);
            Assert.That(f.Controller.Content, Is.EqualTo("synthetic independent draft to preserve"));
            Assert.That(f.Controller.Pending, Is.Null); Assert.That(f.Wire.Requests, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ManagementSelectionCanBeClearedWithoutResettingDraft(bool independentButton)
        {
            var f = new Fixture(); var selected = Record(); f.Journal.Rows.Add(selected); await f.Refresh();
            Assert.That((bool)f.Call("StartIndependentInquiry"), Is.True);
            Assert.That(f.Controller.Edit("BUG_OTHER", "synthetic draft remains unchanged"), Is.True);
            Assert.That((bool)f.Call("Select", selected), Is.True);
            Assert.That(independentButton ? (bool)f.Call("StartIndependentInquiry") : (bool)f.Call("Select", new object[] { null }), Is.True);
            Assert.That(f.Get<PendingReceipt>("Selected"), Is.Null); Assert.That(f.Get<bool>("ManagingDraft"), Is.False);
            Assert.That(f.Controller.Content, Is.EqualTo("synthetic draft remains unchanged"));
            Assert.That(f.Journal.Deletes, Is.Zero); Assert.That(f.Wire.Requests, Is.Empty);
        }

        [Test]
        public async Task SelectingOtherSupportAndReturningToDraftCannotClearServerCooldown()
        {
            var f = new Fixture(); var old = Record(); f.Journal.Rows.Add(old); await f.Refresh();
            Assert.That((bool)f.Call("StartIndependentInquiry"), Is.True);
            Assert.That(f.Controller.Edit("BUG_OTHER", "synthetic rate limited inquiry"), Is.True);
            f.Wire.Reply = RunApiHttpResponse.Success(429,
                "{\"accepted\":false,\"ticketStatus\":\"REJECTED\",\"errorCode\":\"support_ticket_rate_limited\",\"retryAfterSeconds\":120}");
            await f.Controller.SubmitAsync(); Assert.That(f.Controller.RetryWaitSeconds, Is.EqualTo(120));
            Assert.That((bool)f.Call("Select", old), Is.True);
            Assert.That(f.Controller.RetryWaitSeconds, Is.EqualTo(120));
            Assert.That((bool)f.Call("StartIndependentInquiry"), Is.True);
            Assert.That(f.Controller.Edit("BUG_OTHER", "another independent synthetic inquiry"), Is.True);
            Assert.That(f.Controller.CanSubmit, Is.False);
            f.Now = f.Now.AddSeconds(119); Assert.That(f.Controller.RetryWaitSeconds, Is.EqualTo(1));
            await f.Controller.SubmitAsync(); Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
            f.Now = f.Now.AddSeconds(1); Assert.That(f.Controller.CanSubmit, Is.True);
        }

        [Test]
        public async Task StartAnotherPreservesCooldownCarriedByConfirmedReceipt()
        {
            // Exercise the controller's supported receipt-plus-cooldown contract at its client boundary.
            var now = DateTimeOffset.UtcNow;
            var controller = new SupportInquiryController(SupportInquiryTests.Session(), new ReceiptWithCooldown(),
                SupportInquiryHttpClient.Categories, 2000, () => new AppBuildSnapshot("1.0", 1), () => now, 10);
            Assert.That(controller.Edit("BUG_OTHER", "synthetic inquiry awaiting receipt"), Is.True);
            await controller.SubmitAsync(); await controller.ReconcileAsync();
            Assert.That(controller.State, Is.EqualTo(InquiryState.Received));
            Assert.That(controller.RetryWaitSeconds, Is.EqualTo(120));
            Assert.That(controller.StartAnother(), Is.True);
            Assert.That(controller.RetryWaitSeconds, Is.EqualTo(120));
            Assert.That(controller.Edit("BUG_OTHER", "synthetic next independent inquiry"), Is.True);
            Assert.That(controller.CanSubmit, Is.False);
            now = now.AddSeconds(120); Assert.That(controller.CanSubmit, Is.True);
        }

        [Test] public async Task ErrorMetadataCanBeRemovedWithoutCreatingSupportOrErrorPost()
        {
            var f = new Fixture(); var row = Record(kind: PendingReceiptKind.ClientError); f.Journal.Rows.Add(row);
            await f.Refresh(); Assert.That((bool)f.Call("Select", row), Is.True);
            Assert.That(f.Controller.Pending, Is.Null);
            Assert.That(await (Task<bool>)f.Call("ForgetSelectedAsync", true), Is.True);
            Assert.That(f.Wire.Requests, Is.Empty);
        }
    }
}
