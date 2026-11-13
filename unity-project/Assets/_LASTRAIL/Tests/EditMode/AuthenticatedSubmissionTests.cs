using NUnit.Framework;
using LastRail.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Support;
using LastRail.Diagnostics;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Tests.EditMode
{
    public sealed class AuthenticatedSubmissionAvailabilityTests
    {
        [TestCase("LastRail.Support.SupportInquiryHttpClient")]
        [TestCase("LastRail.Diagnostics.CurrentSessionErrorClient")]
        public void ConfirmedContractAdapterExists(string name)
        {
            Assert.That(typeof(GameApiJson).Assembly.GetType(name), Is.Not.Null);
        }
    }

    public sealed class AuthenticatedSubmissionTests
    {
        private sealed class Transport : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public Func<RunApiHttpResponse> Reply;
            public TaskCompletionSource<RunApiHttpResponse> Deferred;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            { Requests.Add(request); return Deferred != null ? Deferred.Task : Task.FromResult(Reply()); }
        }
        private const string Received = "{\"accepted\":true,\"ticketStatus\":\"RECEIVED\",\"ticketId\":\"ticket_fixture\",\"category\":\"BUG_OTHER\",\"serverReceivedAt\":\"2026-09-06T00:00:00.000Z\"}";
        private const string Ingested = "{\"accepted\":true,\"ingestStatus\":\"RECEIVED\",\"ingestId\":\"ingest_fixture\",\"acceptedEventCount\":1,\"droppedEventCount\":0,\"serverReceivedAt\":\"2026-09-06T00:00:00.000Z\"}";
        private static GameApiAuthenticatedHttpTransport Auth(Transport transport) => new GameApiAuthenticatedHttpTransport(transport);
        [Test]
        public async Task SupportWireUsesConfirmedFieldsAndFrozenBodyOnReplay()
        {
            var wire = new Transport { Reply = () => RunApiHttpResponse.NetworkError("fixture timeout") };
            var client = new SupportInquiryHttpClient(Auth(wire));
            var build = new AppBuildSnapshot("1.0", long.MaxValue);
            var model = new SupportInquiryController(SupportInquiryTests.Session(), client, SupportInquiryHttpClient.Categories, 2000, () => build, minContentLength: 10);
            model.Edit("BUG_OTHER", "  1234567890  "); await model.SubmitAsync();
            build = new AppBuildSnapshot("2.0", 2);
            wire.Reply = () => RunApiHttpResponse.Success(200, Received.Replace("RECEIVED", "DUPLICATE_REPLAY"));
            await model.SubmitAsync();
            Assert.That(model.State, Is.EqualTo(InquiryState.Received));
            Assert.That(wire.Requests[0].BodyJson, Is.EqualTo(wire.Requests[1].BodyJson));
            Assert.That(wire.Requests[0].EndpointPath, Is.EqualTo("/v1/support/tickets"));
            var json = JObject.Parse(wire.Requests[0].BodyJson);
            Assert.That(json.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "category", "body", "appVersion", "appVersionCode", "clientComposedAt", "clientSubmittedAt" }));
            Assert.That((string)json["body"], Is.EqualTo("1234567890")); Assert.That((string)json["appVersion"], Is.EqualTo("1.0"));
            Assert.That(json["appVersionCode"].Type, Is.EqualTo(Newtonsoft.Json.Linq.JTokenType.String));
            Assert.That((string)json["appVersionCode"], Is.EqualTo(long.MaxValue.ToString()));
            Assert.That(wire.Requests[0].Headers.Count(h => h.Name == "Authorization"), Is.EqualTo(1));
            Assert.That(wire.Requests[0].Headers.Single(h => h.Name == "Idempotency-Key").Value,
                Is.EqualTo(wire.Requests[1].Headers.Single(h => h.Name == "Idempotency-Key").Value));
        }
        [TestCase(409, "{\"accepted\":false,\"ticketStatus\":\"REJECTED\",\"errorCode\":\"idempotency_conflict\"}")]
        [TestCase(200, "{\"accepted\":true,\"ticketStatus\":\"RECEIVED\",\"category\":\"BUG_OTHER\",\"ticketId\":\"fixture\"}")]
        [TestCase(401, "{\"errorCode\":\"auth_required\"}")]
        public async Task AmbiguousSupportResponseKeepsFrozenPending(int status, string json)
        {
            var wire = new Transport { Reply = () => RunApiHttpResponse.Success(status, json) };
            var client = new SupportInquiryHttpClient(Auth(wire)); var model = client.CreateController(SupportInquiryTests.Session());
            model.Edit("BUG_OTHER", "1234567890"); await model.SubmitAsync();
            Assert.That(model.State, Is.EqualTo(InquiryState.Uncertain)); Assert.That(model.CanEdit, Is.False);
            await client.SubmitAsync(model.Pending, SupportInquiryTests.Session("other"));
            Assert.That(wire.Requests.Count, Is.EqualTo(1));
        }
        [Test]
        public async Task EarlierLostResponseCannotBeUnlockedByLaterValidationError()
        {
            var wire = new Transport { Reply = () => RunApiHttpResponse.NetworkError("fixture") };
            var model = new SupportInquiryHttpClient(Auth(wire)).CreateController(SupportInquiryTests.Session());
            model.Edit("BUG_OTHER", "1234567890"); await model.SubmitAsync();
            wire.Reply = () => RunApiHttpResponse.Success(400, "{\"accepted\":false,\"ticketStatus\":\"REJECTED\",\"errorCode\":\"support_ticket_invalid\"}");
            await model.SubmitAsync(); Assert.That(model.State, Is.EqualTo(InquiryState.Uncertain));
        }
        [Test]
        public async Task SupportRateLimitWaitsWithoutDiscardingDraft()
        {
            var wire = new Transport { Reply = () => RunApiHttpResponse.Success(429, "{\"accepted\":false,\"ticketStatus\":\"REJECTED\",\"errorCode\":\"support_ticket_rate_limited\",\"retryAfterSeconds\":600}") };
            var client = new SupportInquiryHttpClient(Auth(wire)); var clock = DateTimeOffset.UtcNow;
            var model = new SupportInquiryController(SupportInquiryTests.Session(), client, SupportInquiryHttpClient.Categories, 2000,
                () => new AppBuildSnapshot(null, null), () => clock, 10);
            model.Edit("BUG_OTHER", "123456789"); Assert.That(model.CanSubmit, Is.False);
            model.Edit("BUG_OTHER", "1234567890"); await model.SubmitAsync();
            Assert.That(model.CanSubmit, Is.False); await model.SubmitAsync(); Assert.That(wire.Requests.Count, Is.EqualTo(1));
            Assert.That(model.Pending, Is.Not.Null); Assert.That(model.Content, Is.EqualTo("1234567890"));
            clock = clock.AddSeconds(601); Assert.That(model.CanSubmit, Is.True);
        }
        [Test]
        public async Task SameOwnerRefreshRetainsInflightRateLimit()
        {
            var wire = new Transport { Deferred = new TaskCompletionSource<RunApiHttpResponse>() };
            var client = new SupportInquiryHttpClient(Auth(wire)); var clock = DateTimeOffset.UtcNow;
            var model = new SupportInquiryController(SupportInquiryTests.Session(), client, SupportInquiryHttpClient.Categories, 2000,
                () => new AppBuildSnapshot(null, null), () => clock, 10);
            model.Edit("BUG_OTHER", "1234567890"); var send = model.SubmitAsync();
            model.SetSession(SupportInquiryTests.Session());
            wire.Deferred.SetResult(RunApiHttpResponse.Success(429, "{\"accepted\":false,\"ticketStatus\":\"REJECTED\",\"errorCode\":\"support_ticket_rate_limited\",\"retryAfterSeconds\":600}"));
            await send;
            Assert.That(model.RetryWaitSeconds, Is.EqualTo(600));
            Assert.That(model.CanSubmit, Is.False);
            await model.SubmitAsync(); Assert.That(wire.Requests.Count, Is.EqualTo(1));
            clock = clock.AddSeconds(601); Assert.That(model.CanSubmit, Is.True);
        }
        [Test]
        public async Task ErrorCaptureRequiresCurrentOwnerAndRejectsStaleQueuedEvents()
        {
            var wire = new Transport { Reply = () => RunApiHttpResponse.Success(200, Ingested) };
            GameApiAuthSessionSnapshot owner = null; long generation = 1;
            var client = new CurrentSessionErrorClient(Auth(wire), () => owner, () => generation);
            Assert.That(client.Capture(SafeClientErrorKind.UnityError, 1), Is.Null);
            owner = SupportInquiryTests.Session();
            Assert.That(client.Capture(SafeClientErrorKind.UnityError, 0), Is.Null);
            var request = client.Capture(SafeClientErrorKind.UnityException, 1);
            generation++; Assert.That(await client.SendAsync(request), Is.EqualTo(ClientErrorDelivery.Stale));
            Assert.That(wire.Requests, Is.Empty); Assert.That(client.PendingCount, Is.Zero);
        }
        [Test]
        public async Task CurrentErrorWireIsAllowlistedAndCanUseRefreshedSameOwner()
        {
            var wire = new Transport { Reply = () => RunApiHttpResponse.NetworkError("fixture") };
            var owner = SupportInquiryTests.Session();
            var client = new CurrentSessionErrorClient(Auth(wire), () => owner, () => 7, () => new AppBuildSnapshot("1.0", long.MaxValue));
            var request = client.Capture(SafeClientErrorKind.UnityAssert, 7);
            Assert.That(await client.SendAsync(request), Is.EqualTo(ClientErrorDelivery.Unknown));
            owner = SupportInquiryTests.Session();
            wire.Reply = () => RunApiHttpResponse.Success(200, Ingested.Replace("RECEIVED", "DUPLICATE_REPLAY").Replace("\"acceptedEventCount\":1", "\"acceptedEventCount\":0").Replace("\"droppedEventCount\":0", "\"droppedEventCount\":1"));
            Assert.That(await client.SendAsync(request), Is.EqualTo(ClientErrorDelivery.Received));
            Assert.That(wire.Requests[0].BodyJson, Is.EqualTo(wire.Requests[1].BodyJson));
            var json = JObject.Parse(wire.Requests[0].BodyJson); Assert.That(json.Properties().Count(), Is.EqualTo(1));
            var item = (JObject)json["events"][0];
            Assert.That(item.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "kind", "occurredAt", "occurrenceCount", "appVersion", "appVersionCode", "scope" }));
            Assert.That((string)item["kind"], Is.EqualTo("UNITY_ASSERT"));
            Assert.That((string)item["scope"], Is.EqualTo("AUTHENTICATED_CURRENT_SESSION"));
            Assert.That(client.PendingCount, Is.Zero);
        }
        [Test]
        public async Task ErrorRateLimitDoesNotSendAgainDuringWait()
        {
            var wire = new Transport { Reply = () => RunApiHttpResponse.Success(429, "{\"accepted\":false,\"ingestStatus\":\"REJECTED\",\"errorCode\":\"client_error_ingest_rate_limited\",\"retryAfterSeconds\":600}") };
            var client = new CurrentSessionErrorClient(Auth(wire), () => SupportInquiryTests.Session(), () => 1);
            var request = client.Capture(SafeClientErrorKind.UnityError, 1);
            await client.SendAsync(request); await client.SendAsync(request);
            Assert.That(wire.Requests.Count, Is.EqualTo(1)); Assert.That(client.PendingCount, Is.EqualTo(1));
        }
        [Test]
        public async Task LateErrorReceiptAndBoundedPendingCannotLeakAcrossAccountSwitch()
        {
            var wire = new Transport { Deferred = new TaskCompletionSource<RunApiHttpResponse>() };
            var owner = SupportInquiryTests.Session(); long generation = 1;
            var client = new CurrentSessionErrorClient(Auth(wire), () => owner, () => generation);
            var request = client.Capture(SafeClientErrorKind.UnityError, 1);
            for (var i = 0; i < 24; i++) Assert.That(client.Capture(SafeClientErrorKind.UnityError, 1), Is.Not.Null);
            Assert.That(client.Capture(SafeClientErrorKind.UnityError, 1), Is.Null);
            var send = client.SendAsync(request); generation++; owner = SupportInquiryTests.Session("other");
            wire.Deferred.SetResult(RunApiHttpResponse.Success(200, Ingested));
            Assert.That(await send, Is.EqualTo(ClientErrorDelivery.Stale));
            client.Dispose(); Assert.That(client.PendingCount, Is.Zero);
        }
    }
}
