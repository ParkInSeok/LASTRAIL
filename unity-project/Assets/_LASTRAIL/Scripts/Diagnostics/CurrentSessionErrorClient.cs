using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Submissions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Diagnostics
{
    public enum ClientErrorDelivery { Received, Unknown, Stale, Busy, RateLimited, Rejected }

    // Explicit main-thread error source. Never imports the unscoped persistent crash/error queue.
    public sealed class CurrentSessionErrorClient : IDisposable
    {
        public sealed class Request
        {
            internal string AccountId;
            internal long Generation;
            internal RunApiHttpRequest Http;
            internal bool Sending;
            internal PendingReceipt Receipt;
            public string IdempotencyKey { get; internal set; }
            internal Request() { }
        }
        private readonly IGameApiAuthenticatedHttpTransport transport;
        private readonly Func<GameApiAuthSessionSnapshot> session;
        private readonly Func<long> generation;
        private readonly Func<AppBuildSnapshot> build;
        private readonly Func<DateTimeOffset> now;
        private readonly IPendingReceiptJournal journal;
        private Task reconciliation;
        private string reconciliationAccount;
        private long reconciliationGeneration;
        private readonly HashSet<Request> pending = new HashSet<Request>();
        private DateTimeOffset window;
        private int count;
        private bool disposed;
        private DateTimeOffset nextSendAt;
        private string backoffAccount;
        public int PendingCount => pending.Count;

        public CurrentSessionErrorClient(IGameApiAuthenticatedHttpTransport transport,
            Func<GameApiAuthSessionSnapshot> currentSession, Func<long> ownerGeneration,
            Func<AppBuildSnapshot> build = null, Func<DateTimeOffset> now = null)
            : this(transport, currentSession, ownerGeneration, build, now, null) { }

        public CurrentSessionErrorClient(IGameApiAuthenticatedHttpTransport transport,
            Func<GameApiAuthSessionSnapshot> currentSession, Func<long> ownerGeneration,
            Func<AppBuildSnapshot> build, Func<DateTimeOffset> now, IPendingReceiptJournal journal)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            session = currentSession ?? throw new ArgumentNullException(nameof(currentSession));
            generation = ownerGeneration ?? throw new ArgumentNullException(nameof(ownerGeneration));
            this.build = build ?? AppBuildSnapshot.CaptureInstalled;
            this.now = now ?? (() => DateTimeOffset.UtcNow);
            this.journal = journal;
        }

        public Request Capture(SafeClientErrorKind kind, long originatingGeneration)
        {
            var owner = session();
            if (disposed || owner == null || originatingGeneration != generation() || !Enum.IsDefined(typeof(SafeClientErrorKind), kind)) return null;
            pending.RemoveWhere(item => !Current(item, owner));
            var timestamp = now();
            if (timestamp >= window.AddMinutes(1)) { window = timestamp; count = 0; }
            if (pending.Count >= 25 || count >= 60) return null;
            var snapshot = build() ?? new AppBuildSnapshot(null, null);
            var key = Guid.NewGuid().ToString("N");
            var eventJson = new JObject
            {
                ["kind"] = kind == SafeClientErrorKind.UnityError ? "UNITY_ERROR" : kind == SafeClientErrorKind.UnityException ? "UNITY_EXCEPTION" : "UNITY_ASSERT",
                ["occurredAt"] = GameApiJson.UtcText(timestamp), ["occurrenceCount"] = 1,
                ["appVersion"] = snapshot.AppVersion, ["appVersionCode"] = snapshot.AppVersionCode?.ToString(CultureInfo.InvariantCulture),
                ["scope"] = "AUTHENTICATED_CURRENT_SESSION"
            };
            var request = new Request
            {
                AccountId = owner.AccountId, Generation = originatingGeneration, IdempotencyKey = key,
                Http = new RunApiHttpRequest("POST", "/v1/client-errors",
                    new JObject { ["events"] = new JArray(eventJson) }.ToString(Formatting.None),
                    new[] { new RunApiHttpHeader("Idempotency-Key", key) })
            };
            count++; pending.Add(request); return request;
        }

        public async Task<ClientErrorDelivery> SendAsync(Request request)
        {
            var owner = session();
            if (request == null || !pending.Contains(request) || !Current(request, owner))
            {
                if (request != null) pending.Remove(request);
                return ClientErrorDelivery.Stale;
            }
            if (request.Sending) return ClientErrorDelivery.Busy;
            if (backoffAccount == owner.AccountId && now() < nextSendAt) return ClientErrorDelivery.RateLimited;
            request.Sending = true;
            try
            {
                if (journal != null)
                {
                    if (request.Receipt == null) request.Receipt = ReceiptMetadata(request);
                    var saved = await journal.PutAsync(request.Receipt);
                    owner = session();
                    if (!Current(request, owner)) { pending.Remove(request); return ClientErrorDelivery.Stale; }
                    if (!saved) return ClientErrorDelivery.Unknown;
                }
                var result = await transport.SendAsync(request.Http, owner.AccessToken);
                if (!Current(request, session())) { pending.Remove(request); return ClientErrorDelivery.Stale; }
                if (result == null || result.Outcome != GameApiAuthenticatedHttpOutcome.Succeeded || result.Response == null)
                    return ClientErrorDelivery.Unknown;
                var json = GameApiJson.ReadObject(result.Response.BodyJson);
                if(result.Response.StatusCode==400 && !GameApiJson.Boolean(json,"accepted")
                    && GameApiJson.String(json,"errorCode",true)=="contract_violation")
                {
                    // A fixed schema rejection is definitive. Keep recovery metadata,
                    // but never retry or re-key this frozen invalid event body.
                    pending.Remove(request);
                    return ClientErrorDelivery.Rejected;
                }
                if (result.Response.StatusCode == 429 && !GameApiJson.Boolean(json, "accepted")
                    && GameApiJson.String(json, "errorCode", true) == "client_error_ingest_rate_limited")
                {
                    backoffAccount = owner.AccountId; nextSendAt = now().AddSeconds(GameApiJson.RetryDelaySeconds(json));
                    return ClientErrorDelivery.RateLimited;
                }
                if (result.Response.StatusCode != 200) return ClientErrorDelivery.Unknown;
                var status = GameApiJson.String(json, "ingestStatus");
                if (!GameApiJson.Boolean(json, "accepted") || (status != "RECEIVED" && status != "DUPLICATE_REPLAY")) return ClientErrorDelivery.Unknown;
                GameApiJson.String(json, "ingestId"); GameApiJson.UtcTimestamp(json, "serverReceivedAt");
                if (json["acceptedEventCount"]?.Type != JTokenType.Integer || json["droppedEventCount"]?.Type != JTokenType.Integer)
                    return ClientErrorDelivery.Unknown;
                if ((int)json["acceptedEventCount"] != (status == "RECEIVED" ? 1 : 0) || (int)json["droppedEventCount"] != (status == "RECEIVED" ? 0 : 1))
                    return ClientErrorDelivery.Unknown;
                pending.Remove(request);
                // A confirmed delivery cannot become a resend because metadata cleanup failed.
                if (journal != null)
                {
                    try { await journal.RemoveAsync(request.AccountId, PendingReceiptKind.ClientError, request.IdempotencyKey); }
                    catch { }
                }
                return ClientErrorDelivery.Received;
            }
            catch
            {
                if (Current(request, session())) return ClientErrorDelivery.Unknown;
                pending.Remove(request); return ClientErrorDelivery.Stale;
            }
            finally { request.Sending = false; }
        }

        private static PendingReceipt ReceiptMetadata(Request request)
        {
            var root = GameApiJson.ReadObject(request.Http.BodyJson);
            if (!(root["events"] is JArray events) || events.Count != 1 || !(events[0] is JObject item))
                throw new InvalidOperationException("Current-session receipt requires one frozen event.");
            var category = GameApiJson.String(item, "kind");
            var version = GameApiJson.String(item, "appVersion", true);
            var codeText = GameApiJson.String(item, "appVersionCode", true);
            long? code = codeText == null ? (long?)null : long.Parse(codeText, NumberStyles.None, CultureInfo.InvariantCulture);
            var occurred = GameApiJson.UtcTimestamp(item, "occurredAt");
            // Error events have one frozen timestamp; recovery never recaptures the current build/time.
            return new PendingReceipt(PendingReceiptKind.ClientError, request.AccountId, request.IdempotencyKey,
                category, new AppBuildSnapshot(version, code), occurred, occurred);
        }

        public Task ReconcilePendingAsync()
        {
            var owner = session();
            if (disposed || journal == null || owner == null) return Task.CompletedTask;
            var account = owner.AccountId;
            var origin = generation();
            var previous = reconciliation;
            if (previous != null && !previous.IsCompleted && reconciliationAccount == account
                && reconciliationGeneration == origin) return previous;
            var completion = new TaskCompletionSource<bool>();
            // Publish the queued owner before awaiting: repeated Ready notifications join this slot.
            reconciliationAccount = account;
            reconciliationGeneration = origin;
            reconciliation = completion.Task;
            _ = ReconcileAsync(completion, previous, account, origin);
            return completion.Task;
        }

        private async Task ReconcileAsync(TaskCompletionSource<bool> completion, Task previous, string account, long origin)
        {
            try
            {
                if (previous != null)
                {
                    // The old caller still observes its fault; another owner's recovery is independent.
                    try { await previous; }
                    catch { }
                }
                if (!CurrentOwner(account, origin)) return;
                // Corrupt/unavailable storage faults this explicit operation, never appears empty.
                var records = await journal.ReadAsync(account);
                if (!CurrentOwner(account, origin)) return;
                if (records == null || records.Count > 64) throw new InvalidOperationException("Invalid pending receipt journal result.");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var record in records)
                {
                    if (!CurrentOwner(account, origin)) return;
                    if (record == null || record.Kind != PendingReceiptKind.ClientError || record.AccountId != account
                        || !seen.Add(record.IdempotencyKey)) continue;
                    var request = new RunApiHttpRequest("GET", "/v1/client-errors/receipt", null,
                        new[] { new RunApiHttpHeader("Idempotency-Key", record.IdempotencyKey) });
                    bool found;
                    try
                    {
                        var result = await transport.SendAsync(request, session().AccessToken);
                        if (!CurrentOwner(account, origin)) return;
                        found = result != null && result.Outcome == GameApiAuthenticatedHttpOutcome.Succeeded
                            && result.Response != null && result.Response.StatusCode == 200
                            && ValidFound(GameApiJson.ReadObject(result.Response.BodyJson), account);
                    }
                    catch { if (!CurrentOwner(account, origin)) return; continue; }
                    if (!found) continue;
                    pending.RemoveWhere(item => item.AccountId == account && item.IdempotencyKey == record.IdempotencyKey);
                    try { await journal.RemoveAsync(account, PendingReceiptKind.ClientError, record.IdempotencyKey); }
                    catch { }
                }
            }
            catch (Exception error) { completion.TrySetException(error); }
            finally { completion.TrySetResult(true); }
        }

        private static bool ValidFound(JObject json, string account)
        {
            if (!GameApiJson.Boolean(json, "accepted") || GameApiJson.String(json, "receiptStatus") != "FOUND") return false;
            if (json["accountId"] != null && GameApiJson.String(json, "accountId") != account) return false;
            var ingest = GameApiJson.String(json, "ingestId");
            if (ingest.Length > 128 || json["acceptedEventCount"]?.Type != JTokenType.Integer) return false;
            var accepted = (long)json["acceptedEventCount"];
            if (accepted < 0 || accepted > 25) return false;
            GameApiJson.UtcTimestamp(json, "serverReceivedAt");
            return true;
        }

        private bool CurrentOwner(string account, long origin) => !disposed && generation() == origin
            && string.Equals(session()?.AccountId, account, StringComparison.Ordinal);

        private bool Current(Request request, GameApiAuthSessionSnapshot owner) => !disposed && owner != null
            && request.Generation == generation() && string.Equals(request.AccountId, owner.AccountId, StringComparison.Ordinal);
        public void Dispose() { disposed = true; pending.Clear(); }
    }
}
