using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public sealed class SupportReceiptReconciliationTests
    {
        private const string Preview = "synthetic-private-preview";
        private const string NotFound = "{\"accepted\":false,\"receiptStatus\":\"NOT_FOUND\",\"ticketId\":null,\"accountId\":null,\"category\":null,\"bodyPreview\":null,\"appVersion\":null,\"appVersionCode\":null,\"clientComposedAt\":null,\"clientSubmittedAt\":null,\"serverReceivedAt\":null,\"errorCode\":\"support_ticket_receipt_not_found\",\"message\":\"Support ticket receipt was not found for this account.\"}";

        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public RunApiHttpResponse Reply = RunApiHttpResponse.NetworkError("fixture response lost");
            public TaskCompletionSource<RunApiHttpResponse> Deferred;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Requests.Add(request);
                return Deferred != null ? Deferred.Task : Task.FromResult(Reply);
            }
        }

        // Exercise the existing controller's receipt handling without requiring a new controller API.
        private sealed class ClientPort : ISupportInquiryClient
        {
            public readonly SupportInquiryHttpClient Client;
            public bool Lookup;
            public ClientPort(Wire wire) { Client = new SupportInquiryHttpClient(new GameApiAuthenticatedHttpTransport(wire)); }
            public Task<InquiryReceipt> SubmitAsync(InquirySubmission original, GameApiAuthSessionSnapshot session)
                => Lookup ? InvokeReconcile(Client, original, session) : Client.SubmitAsync(original, session);
        }

        private sealed class Fixture
        {
            public readonly Wire Wire = new Wire();
            public readonly ClientPort Port;
            public readonly SupportInquiryController Model;
            public Fixture()
            {
                Port = new ClientPort(Wire);
                Model = new SupportInquiryController(Session(), Port, SupportInquiryHttpClient.Categories, 2000,
                    () => new AppBuildSnapshot("1.0", 1), minContentLength: 10);
                Assert.That(Model.Edit("BUG_OTHER", "synthetic inquiry content"), Is.True);
            }
        }

        private static GameApiAuthSessionSnapshot Session(string account = "account_test", string token = "lst1.fixture")
            => new GameApiAuthSessionSnapshot(account, "ACTIVE", new GameApiAccessToken(token),
                DateTime.UtcNow.AddHours(1), new GameApiRefreshToken("fixture"), DateTime.UtcNow.AddDays(1));

        private static MethodInfo RequireReconcile()
        {
            var method = typeof(SupportInquiryHttpClient).GetMethod("ReconcileAsync", BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(InquirySubmission), typeof(GameApiAuthSessionSnapshot) }, null);
            Assert.That(method, Is.Not.Null,
                "PHASE1 RED: missing public ReconcileAsync(InquirySubmission, GameApiAuthSessionSnapshot).");
            Assert.That(method.ReturnType, Is.EqualTo(typeof(Task<InquiryReceipt>)));
            return method;
        }

        private static async Task<InquiryReceipt> InvokeReconcile(SupportInquiryHttpClient client,
            InquirySubmission original, GameApiAuthSessionSnapshot session)
        {
            var task = RequireReconcile().Invoke(client, new object[] { original, session }) as Task<InquiryReceipt>;
            Assert.That(task, Is.Not.Null);
            var receipt = await task;
            Assert.That(receipt, Is.Not.Null);
            return receipt;
        }

        private static async Task<Fixture> Pending()
        {
            // Check outside the controller, whose exception handling deliberately swallows client failures.
            RequireReconcile();
            var fixture = new Fixture();
            await fixture.Model.SubmitAsync();
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Uncertain));
            Assert.That(fixture.Model.Pending, Is.Not.Null);
            return fixture;
        }

        private static JObject Found() => new JObject
        {
            ["accepted"] = true, ["receiptStatus"] = "FOUND", ["ticketId"] = "ticket_fixture",
            ["accountId"] = "account_test", ["category"] = "BUG_OTHER", ["bodyPreview"] = Preview,
            ["appVersion"] = "1.0", ["appVersionCode"] = "1",
            ["clientComposedAt"] = "2026-09-06T00:00:00.000Z",
            ["clientSubmittedAt"] = "2026-09-06T00:00:00.000Z",
            ["serverReceivedAt"] = "2026-09-06T00:01:00.000Z", ["errorCode"] = null, ["message"] = null
        };

        private static void AssertUnknown(InquiryReceipt receipt)
        {
            Assert.That(receipt.Delivery, Is.EqualTo(InquiryDelivery.Unknown));
            Assert.That(receipt.TicketId, Is.Null);
            Assert.That(receipt.ServerReceivedAt, Is.Null);
        }

        private static void AssertFrozen(Fixture fixture, InquirySubmission original)
        {
            Assert.That(fixture.Model.Pending, Is.SameAs(original));
            Assert.That(fixture.Model.CanEdit, Is.False);
            Assert.That(fixture.Model.StartAnother(), Is.False);
            Assert.That(fixture.Model.Edit("ACCOUNT_LOGIN", "another synthetic inquiry"), Is.False);
            Assert.That(fixture.Model.Receipt, Is.Null);
        }

        [Test]
        public async Task MatchingReceiptUsesHeadersOnlyGetAndRotatedBearer()
        {
            var fixture = await Pending();
            var original = fixture.Model.Pending;
            fixture.Wire.Reply = RunApiHttpResponse.Success(200, Found().ToString(Formatting.None));
            var receipt = await InvokeReconcile(fixture.Port.Client, original, Session(token: "lst1.rotated"));
            Assert.That(receipt.Delivery, Is.EqualTo(InquiryDelivery.Received));
            Assert.That(receipt.TicketId, Is.EqualTo("ticket_fixture"));
            Assert.That(receipt.ServerReceivedAt, Is.EqualTo(new DateTimeOffset(2026, 9, 6, 0, 1, 0, TimeSpan.Zero)));
            Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
            var request = fixture.Wire.Requests[1];
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/support/tickets/receipt"));
            Assert.That(string.IsNullOrEmpty(request.BodyJson), Is.True, "Lookup must have no request body.");
            Assert.That(request.Headers.Count(h => h.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1));
            Assert.That(request.Headers.Single(h => h.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).Value == "Bearer lst1.rotated", Is.True);
            Assert.That(request.Headers.Count(h => h.Name.Equals("Idempotency-Key", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1));
            Assert.That(request.Headers.Single(h => h.Name.Equals("Idempotency-Key", StringComparison.OrdinalIgnoreCase)).Value,
                Is.EqualTo(original.IdempotencyKey));
            Assert.That(JsonConvert.SerializeObject(receipt).Contains(Preview), Is.False, "Receipt must not retain the raw preview.");
        }

        [Test]
        public async Task NotFoundRemainsUncertainAndPostRetryKeepsFrozenBodyAndKey()
        {
            var fixture = await Pending();
            var original = fixture.Model.Pending;
            fixture.Wire.Reply = RunApiHttpResponse.Success(404, NotFound);
            AssertUnknown(await InvokeReconcile(fixture.Port.Client, original, Session()));
            fixture.Port.Lookup = true;
            await fixture.Model.SubmitAsync();
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Uncertain));
            AssertFrozen(fixture, original);
            fixture.Port.Lookup = false;
            fixture.Wire.Reply = RunApiHttpResponse.NetworkError("fixture response lost");
            await fixture.Model.SubmitAsync();
            var retry = fixture.Wire.Requests.Last();
            Assert.That(retry.Method, Is.EqualTo("POST"));
            Assert.That(retry.EndpointPath, Is.EqualTo("/v1/support/tickets"));
            Assert.That(retry.BodyJson == fixture.Wire.Requests[0].BodyJson, Is.True, "Frozen POST body must survive lookup.");
            Assert.That(retry.Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo(original.IdempotencyKey));
            AssertFrozen(fixture, original);
        }

        [Test]
        public async Task UnauthorizedIsUnknownEvenWithFoundResponseBody()
        {
            var fixture = await Pending();
            fixture.Wire.Reply = RunApiHttpResponse.Success(401, Found().ToString(Formatting.None));
            AssertUnknown(await InvokeReconcile(fixture.Port.Client, fixture.Model.Pending, Session()));
            Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
            AssertFrozen(fixture, fixture.Model.Pending);
        }

        [TestCase("invalid-json")]
        [TestCase("accepted")]
        [TestCase("receiptStatus")]
        [TestCase("ticketId")]
        [TestCase("serverReceivedAt")]
        public async Task MalformedOrIncompleteFoundIsUnknown(string defect)
        {
            var fixture = await Pending();
            var json = Found();
            if (defect == "accepted") json[defect] = "true";
            else if (defect == "receiptStatus") json[defect] = "RECEIVED";
            else if (defect == "ticketId") json[defect] = "";
            else if (defect == "serverReceivedAt") json.Remove(defect);
            fixture.Wire.Reply = RunApiHttpResponse.Success(200, defect == "invalid-json" ? "{" : json.ToString(Formatting.None));
            AssertUnknown(await InvokeReconcile(fixture.Port.Client, fixture.Model.Pending, Session()));
        }

        [TestCase("accountId", "other_account")]
        [TestCase("category", "ACCOUNT_LOGIN")]
        public async Task ReceiptMustMatchOriginalAccountAndCategory(string field, string value)
        {
            var fixture = await Pending();
            var json = Found(); json[field] = value;
            fixture.Wire.Reply = RunApiHttpResponse.Success(200, json.ToString(Formatting.None));
            AssertUnknown(await InvokeReconcile(fixture.Port.Client, fixture.Model.Pending, Session()));
        }

        [Test]
        public async Task DifferentSessionAccountCannotSendLookupForFrozenSubmission()
        {
            var fixture = await Pending();
            var original = fixture.Model.Pending;
            fixture.Wire.Reply = RunApiHttpResponse.Success(200, Found().ToString(Formatting.None));
            AssertUnknown(await InvokeReconcile(fixture.Port.Client, original, Session("other_account")));
            Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(1), "Account fence must act before sending.");
            AssertFrozen(fixture, original);
        }

        [Test]
        public async Task InFlightLookupCannotUnlockNewKeyOrAcceptReceiptAfterAccountSwitch()
        {
            var fixture = await Pending();
            var original = fixture.Model.Pending;
            fixture.Port.Lookup = true;
            fixture.Wire.Deferred = new TaskCompletionSource<RunApiHttpResponse>();
            var lookup = fixture.Model.SubmitAsync();
            try
            {
                Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Sending));
                Assert.That(fixture.Model.CanSubmit, Is.False);
                AssertFrozen(fixture, original);
                await fixture.Model.SubmitAsync();
                Assert.That(fixture.Wire.Requests.Count, Is.EqualTo(2));
                fixture.Model.SetSession(Session("other_account"));
            }
            finally
            {
                fixture.Wire.Deferred.SetResult(RunApiHttpResponse.Success(200, Found().ToString(Formatting.None)));
                await lookup;
            }
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Detached));
            AssertFrozen(fixture, original);
            fixture.Model.SetSession(Session());
            Assert.That(fixture.Model.State, Is.EqualTo(InquiryState.Uncertain));
            AssertFrozen(fixture, original);
        }
    }
}
