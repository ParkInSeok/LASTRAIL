using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Submissions;

namespace LastRail.Support
{
    public enum InquiryState { Editing, Sending, Uncertain, Rejected, Received, Detached }
    public enum InquiryDelivery { Received, Rejected, Unknown }

    // Local domain snapshot, not an API schema. The confirmed adapter owns wire serialization.
    public sealed class InquirySubmission
    {
        public string Category { get; }
        public string Content { get; }
        public string IdempotencyKey { get; }
        public AppBuildSnapshot Build { get; }
        public DateTimeOffset ComposedAt { get; }
        public DateTimeOffset SubmittedAt { get; }
        public bool ReceiptOnly => ReceiptMetadata != null;
        internal PendingReceipt ReceiptMetadata { get; }
        internal InquirySubmission(string category, string content, string key, AppBuildSnapshot build,
            DateTimeOffset composedAt, DateTimeOffset submittedAt)
        {
            Category = category; Content = content; IdempotencyKey = key; Build = build;
            ComposedAt = composedAt; SubmittedAt = submittedAt;
        }
        internal InquirySubmission(PendingReceipt record)
            : this(record.Category, null, record.IdempotencyKey, record.Build, record.ComposedAt, record.SubmittedAt)
        { ReceiptMetadata = record; }
    }

    public sealed class InquiryReceipt
    {
        public InquiryDelivery Delivery { get; }
        public string TicketId { get; }
        public DateTimeOffset? ServerReceivedAt { get; }
        public int? RetryAfterSeconds { get; }
        public InquiryReceipt(InquiryDelivery delivery, string ticketId = null, DateTimeOffset? serverReceivedAt = null, int? retryAfterSeconds = null)
        {
            Delivery = delivery; TicketId = ticketId; ServerReceivedAt = serverReceivedAt; RetryAfterSeconds = retryAfterSeconds;
        }
    }

    public interface ISupportInquiryClient
    {
        Task<InquiryReceipt> SubmitAsync(InquirySubmission submission, GameApiAuthSessionSnapshot session);
    }

    public interface ISupportInquiryReconciliationClient
    {
        Task<InquiryReceipt> ReconcileAsync(InquirySubmission original, GameApiAuthSessionSnapshot session);
    }

    // Main-thread owner; never logs or persists draft, tokens, or request bodies.
    public sealed class SupportInquiryController
    {
        private readonly ISupportInquiryClient client;
        private readonly Func<AppBuildSnapshot> capture;
        private readonly Func<DateTimeOffset> now;
        private readonly string accountId;
        private GameApiAuthSessionSnapshot session;
        private long generation;
        private bool inFlight;
        private DateTimeOffset? composedAt;
        private DateTimeOffset? retryAt;
        public int RetryWaitSeconds => retryAt.HasValue ? (int)Math.Max(0, Math.Ceiling((retryAt.Value - now()).TotalSeconds)) : 0;
        public TimeSpan? NextPresentationChangeDelay
        {
            get
            {
                if (session == null || !retryAt.HasValue) return null;
                var remaining = (retryAt.Value - now()).TotalSeconds;
                return remaining > 0 ? TimeSpan.FromSeconds(remaining - (Math.Ceiling(remaining) - 1)) : (TimeSpan?)null;
            }
        }
        public IReadOnlyList<string> Categories { get; }
        public int MaxContentLength { get; }
        public int MinContentLength { get; }
        public string Category { get; private set; }
        public string Content { get; private set; } = "";
        public InquiryState State { get; private set; } = InquiryState.Editing;
        public InquirySubmission Pending { get; private set; }
        public InquiryReceipt Receipt { get; private set; }
        public bool CanEdit => session != null && (State == InquiryState.Editing || State == InquiryState.Rejected);
        public bool CanReconcile => client is ISupportInquiryReconciliationClient && !inFlight && RetryWaitSeconds == 0
            && session != null && Pending != null && State == InquiryState.Uncertain;
        public bool CanSubmit => !inFlight && Pending?.ReceiptOnly != true && RetryWaitSeconds == 0 && session != null && (State == InquiryState.Uncertain ||
            (CanEdit && !string.IsNullOrWhiteSpace(Content) && Content.Trim().Length >= MinContentLength && Content.Length <= MaxContentLength));
        public event Action Changed;

        public SupportInquiryController(GameApiAuthSessionSnapshot authenticatedSession, ISupportInquiryClient client,
            IEnumerable<string> categories, int maxContentLength, Func<AppBuildSnapshot> capture,
            Func<DateTimeOffset> now = null, int minContentLength = 1)
        {
            session = authenticatedSession ?? throw new ArgumentNullException(nameof(authenticatedSession));
            accountId = session.AccountId;
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.capture = capture ?? throw new ArgumentNullException(nameof(capture));
            this.now = now ?? (() => DateTimeOffset.UtcNow);
            var options = categories?.ToArray() ?? throw new ArgumentNullException(nameof(categories));
            if (options.Length == 0 || options.Any(string.IsNullOrWhiteSpace) || options.Distinct().Count() != options.Length)
                throw new ArgumentException("Invalid category options.", nameof(categories));
            if (maxContentLength < 1 || minContentLength < 1 || minContentLength > maxContentLength) throw new ArgumentOutOfRangeException(nameof(maxContentLength));
            MinContentLength = minContentLength;
            Categories = Array.AsReadOnly(options); Category = options[0]; MaxContentLength = maxContentLength;
        }

        public bool Edit(string category, string content)
        {
            if (!CanEdit || !Categories.Contains(category) || content == null || content.Length > MaxContentLength) return false;
            if (Category == category && Content == content) return true;
            composedAt = composedAt ?? now();
            Category = category; Content = content; Pending = null; Receipt = null; State = InquiryState.Editing;
            Changed?.Invoke(); return true;
        }

        public void SetSession(GameApiAuthSessionSnapshot value)
        {
            generation++;
            session = value != null && string.Equals(value.AccountId, accountId, StringComparison.Ordinal) ? value : null;
            if (session == null) State = InquiryState.Detached;
            else State = Receipt != null ? InquiryState.Received : Pending != null ? InquiryState.Uncertain : InquiryState.Editing;
            Changed?.Invoke();
        }

        public async Task SubmitAsync()
        {
            if (!CanSubmit) return;
            if (Pending == null)
            {
                var timestamp = now();
                var build = capture() ?? new AppBuildSnapshot(null, null);
                Pending = new InquirySubmission(Category, Content, Guid.NewGuid().ToString("N"), build,
                    composedAt ?? timestamp, timestamp);
            }
            var owner = generation;
            var auth = session;
            retryAt = null;
            inFlight = true; State = InquiryState.Sending; Changed?.Invoke();
            if (owner != generation || session == null || !ReferenceEquals(session, auth))
            {
                inFlight = false; Changed?.Invoke(); return;
            }
            InquiryReceipt result;
            try { result = await client.SubmitAsync(Pending, auth); }
            catch { result = null; }
            inFlight = false;
            // Rate limits belong to the account, including an in-flight response across token rotation.
            if (session != null && string.Equals(session.AccountId, auth.AccountId, StringComparison.Ordinal)
                && result?.RetryAfterSeconds > 0 && result.RetryAfterSeconds <= 86400)
                retryAt = now().AddSeconds(result.RetryAfterSeconds.Value);
            if (owner != generation) { Changed?.Invoke(); return; }
            if (result != null && result.Delivery == InquiryDelivery.Received &&
                !string.IsNullOrWhiteSpace(result.TicketId) && result.ServerReceivedAt.HasValue)
            {
                Receipt = result; State = InquiryState.Received;
            }
            else if (result != null && result.Delivery == InquiryDelivery.Rejected)
            {
                Pending = null; State = InquiryState.Rejected;
            }
            else State = InquiryState.Uncertain;
            Changed?.Invoke();
        }

        public async Task ReconcileAsync()
        {
            if (!CanReconcile) return;
            var reconciliation = (ISupportInquiryReconciliationClient)client;
            var original = Pending;
            var owner = generation;
            var auth = session;
            inFlight = true; State = InquiryState.Sending; Changed?.Invoke();
            if (owner != generation || session == null || !ReferenceEquals(session, auth))
            {
                inFlight = false; Changed?.Invoke(); return;
            }
            InquiryReceipt result;
            try { result = await reconciliation.ReconcileAsync(original, auth); }
            catch { result = null; }
            inFlight = false;
            if (session != null && string.Equals(session.AccountId, auth.AccountId, StringComparison.Ordinal)
                && result?.RetryAfterSeconds > 0 && result.RetryAfterSeconds <= 86400)
            {
                var nextRetry = now().AddSeconds(result.RetryAfterSeconds.Value);
                if (!retryAt.HasValue || nextRetry > retryAt.Value) retryAt = nextRetry;
            }
            if (owner != generation || !ReferenceEquals(Pending, original)) { Changed?.Invoke(); return; }
            if (result != null && result.Delivery == InquiryDelivery.Received &&
                !string.IsNullOrWhiteSpace(result.TicketId) && result.ServerReceivedAt.HasValue)
            {
                Receipt = result; State = InquiryState.Received;
            }
            // Lookup failure, including NOT_FOUND, never rejects or replaces the frozen submission.
            else State = InquiryState.Uncertain;
            Changed?.Invoke();
        }

        public bool RestorePendingReceipt(InquirySubmission lookup)
        {
            if (inFlight || !CanEdit || Pending != null || !string.IsNullOrEmpty(Content)
                || lookup == null || !lookup.ReceiptOnly || lookup.Content != null
                || !Categories.Contains(lookup.Category)
                || !string.Equals(lookup.ReceiptMetadata.AccountId, accountId, StringComparison.Ordinal)) return false;
            Pending = lookup; Category = lookup.Category; Content = ""; Receipt = null;
            composedAt = lookup.ComposedAt; State = InquiryState.Uncertain;
            Changed?.Invoke(); return true;
        }

        public bool StartIndependentInquiry()
        {
            if (inFlight || session == null || State != InquiryState.Uncertain || Pending == null) return false;
            // The journal still holds the old receipt; this is a new empty draft, not a replay.
            Content = ""; Category = Categories[0]; composedAt = null; Pending = null; Receipt = null;
            State = InquiryState.Editing; Changed?.Invoke(); return true;
        }

        public bool StartAnother()
        {
            if (State != InquiryState.Received || session == null) return false;
            Content = ""; Category = Categories[0]; composedAt = null; Pending = null; Receipt = null;
            State = InquiryState.Editing; Changed?.Invoke(); return true;
        }
    }
}
