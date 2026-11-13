using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Submissions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Support
{
    public sealed class SupportInquiryHttpClient : ISupportInquiryClient, ISupportInquiryReconciliationClient
    {
        public static IReadOnlyList<string> Categories { get; } = Array.AsReadOnly(new[]
            { "ACCOUNT_LOGIN", "RUN_REWARD", "GACHA_RECRUITMENT", "STORY_PROGRESS", "BUG_OTHER" });
        private readonly IGameApiAuthenticatedHttpTransport transport;
        private readonly Func<AppBuildSnapshot> capture;
        private readonly ConditionalWeakTable<InquirySubmission, FrozenRequest> requests = new ConditionalWeakTable<InquirySubmission, FrozenRequest>();
        private sealed class FrozenRequest
        {
            public string AccountId;
            public RunApiHttpRequest Http;
            public int Attempts;
        }
        public SupportInquiryHttpClient(IGameApiAuthenticatedHttpTransport transport) : this(transport, AppBuildSnapshot.CaptureInstalled) { }
        public SupportInquiryHttpClient(IGameApiAuthenticatedHttpTransport transport, Func<AppBuildSnapshot> capture)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.capture = capture ?? AppBuildSnapshot.CaptureInstalled;
        }
        public SupportInquiryController CreateController(GameApiAuthSessionSnapshot session) => new SupportInquiryController(
            session, this, Categories, 2000, capture, minContentLength: 10);
        public InquirySubmission RestoreReceiptLookup(PendingReceipt record)
        {
            if (record == null || record.Kind != PendingReceiptKind.Support || !Categories.Contains(record.Category)
                || string.IsNullOrWhiteSpace(record.AccountId) || string.IsNullOrWhiteSpace(record.IdempotencyKey) || record.Build == null) return null;
            try
            {
                var validated = new PendingReceipt(record.Kind, record.AccountId, record.IdempotencyKey,
                    record.Category, record.Build, record.ComposedAt, record.SubmittedAt);
                var lookup = new InquirySubmission(validated);
                // Restored entries have an owner but deliberately have no frozen POST request.
                requests.Add(lookup, new FrozenRequest { AccountId = validated.AccountId });
                return lookup;
            }
            catch (ArgumentException) { return null; }
        }
        public static string CategoryLabel(string category)
        {
            switch (category)
            {
                case "ACCOUNT_LOGIN": return "\uACC4\uC815\u00B7\uB85C\uADF8\uC778";
                case "RUN_REWARD": return "\uCD9C\uACA9\u00B7\uBCF4\uC0C1";
                case "GACHA_RECRUITMENT": return "\uBAA8\uC9D1";
                case "STORY_PROGRESS": return "\uC2A4\uD1A0\uB9AC\u00B7\uC9C4\uD589";
                case "BUG_OTHER": return "\uC624\uB958\u00B7\uAE30\uD0C0";
                default: throw new ArgumentOutOfRangeException(nameof(category));
            }
        }
        public async Task<InquiryReceipt> ReconcileAsync(InquirySubmission original, GameApiAuthSessionSnapshot session)
        {
            if (original == null || session == null || !requests.TryGetValue(original, out var frozen)
                || !string.Equals(frozen.AccountId, session.AccountId, StringComparison.Ordinal))
                return new InquiryReceipt(InquiryDelivery.Unknown);
            try
            {
                var request = new RunApiHttpRequest("GET", "/v1/support/tickets/receipt", null,
                    new[] { new RunApiHttpHeader("Idempotency-Key", original.IdempotencyKey) });
                var result = await transport.SendAsync(request, session.AccessToken);
                // A missing receipt is only a current observation; it cannot reject the frozen submission.
                if (result == null || result.Outcome != GameApiAuthenticatedHttpOutcome.Succeeded
                    || result.Response == null || result.Response.StatusCode != 200)
                    return new InquiryReceipt(InquiryDelivery.Unknown);
                var json = GameApiJson.ReadObject(result.Response.BodyJson);
                if (!GameApiJson.Boolean(json, "accepted") || GameApiJson.String(json, "receiptStatus") != "FOUND"
                    || !string.Equals(GameApiJson.String(json, "accountId"), frozen.AccountId, StringComparison.Ordinal)
                    || !string.Equals(GameApiJson.String(json, "category"), original.Category, StringComparison.Ordinal))
                    return new InquiryReceipt(InquiryDelivery.Unknown);
                var ticket = GameApiJson.String(json, "ticketId");
                if (ticket.Length > 128) return new InquiryReceipt(InquiryDelivery.Unknown);
                var receivedAt = GameApiJson.UtcTimestamp(json, "serverReceivedAt");
                // Only receipt identifiers leave this method; preview and version metadata are not retained.
                return new InquiryReceipt(InquiryDelivery.Received, ticket, receivedAt);
            }
            catch { }
            return new InquiryReceipt(InquiryDelivery.Unknown);
        }

        public async Task<InquiryReceipt> SubmitAsync(InquirySubmission submission, GameApiAuthSessionSnapshot session)
        {
            if (submission == null || session == null || submission.ReceiptOnly || submission.Content == null)
                return new InquiryReceipt(InquiryDelivery.Unknown);
            var content = submission.Content.Trim();
            if (!Categories.Contains(submission.Category) || content.Length < 10 || content.Length > 2000)
                return new InquiryReceipt(InquiryDelivery.Rejected);
            var frozen = requests.GetValue(submission, value => new FrozenRequest
            {
                AccountId = session.AccountId,
                Http = new RunApiHttpRequest("POST", "/v1/support/tickets", new JObject
                {
                    ["category"] = value.Category, ["body"] = content,
                    ["appVersion"] = value.Build.AppVersion,
                    ["appVersionCode"] = value.Build.AppVersionCode?.ToString(CultureInfo.InvariantCulture),
                    ["clientComposedAt"] = GameApiJson.UtcText(value.ComposedAt),
                    ["clientSubmittedAt"] = GameApiJson.UtcText(value.SubmittedAt)
                }.ToString(Formatting.None), new[] { new RunApiHttpHeader("Idempotency-Key", value.IdempotencyKey) })
            });
            if (!string.Equals(frozen.AccountId, session.AccountId, StringComparison.Ordinal)) return new InquiryReceipt(InquiryDelivery.Unknown);
            frozen.Attempts++;
            try
            {
                var result = await transport.SendAsync(frozen.Http, session.AccessToken);
                if (result == null || result.Outcome != GameApiAuthenticatedHttpOutcome.Succeeded || result.Response == null)
                    return new InquiryReceipt(InquiryDelivery.Unknown);
                var response = result.Response;
                var json = GameApiJson.ReadObject(response.BodyJson);
                var accepted = GameApiJson.Boolean(json, "accepted");
                var status = GameApiJson.String(json, "ticketStatus");
                if (response.StatusCode == 200 && accepted && (status == "RECEIVED" || status == "DUPLICATE_REPLAY")
                    && GameApiJson.String(json, "category") == submission.Category)
                {
                    var ticket = GameApiJson.String(json, "ticketId");
                    if (ticket.Length > 128) return new InquiryReceipt(InquiryDelivery.Unknown);
                    return new InquiryReceipt(InquiryDelivery.Received, ticket, GameApiJson.UtcTimestamp(json, "serverReceivedAt"));
                }
                // A rejection after a lost earlier response does not prove that earlier request was never accepted.
                var error = GameApiJson.String(json, "errorCode", true);
                if (response.StatusCode == 429 && !accepted && status == "REJECTED" && error == "support_ticket_rate_limited")
                    return new InquiryReceipt(InquiryDelivery.Unknown, retryAfterSeconds: GameApiJson.RetryDelaySeconds(json));
                if (frozen.Attempts == 1 && response.StatusCode == 400 && !accepted && status == "REJECTED"
                    && (error == "support_ticket_invalid" || error == "contract_violation"))
                    return new InquiryReceipt(InquiryDelivery.Rejected);
            }
            catch { }
            return new InquiryReceipt(InquiryDelivery.Unknown);
        }
    }
}
