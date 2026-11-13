using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Submissions;

namespace LastRail.Support
{
    public sealed class ReceiptTrackingSupportClient : ISupportInquiryClient, ISupportInquiryReconciliationClient
    {
        private readonly SupportInquiryHttpClient inner;
        private readonly IPendingReceiptJournal journal;
        private readonly Func<long> generation;
        private readonly Func<GameApiAuthSessionSnapshot> currentSession;
        private readonly ConditionalWeakTable<InquirySubmission, Tracking> submissions = new ConditionalWeakTable<InquirySubmission, Tracking>();
        private sealed class Tracking
        {
            public PendingReceipt Metadata;
            public long Generation;
            public bool Busy;
        }

        public ReceiptTrackingSupportClient(SupportInquiryHttpClient inner, IPendingReceiptJournal journal,
            Func<long> generation, Func<GameApiAuthSessionSnapshot> currentSession)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            this.generation = generation ?? throw new ArgumentNullException(nameof(generation));
            this.currentSession = currentSession ?? throw new ArgumentNullException(nameof(currentSession));
        }

        public async Task<InquiryReceipt> SubmitAsync(InquirySubmission submission, GameApiAuthSessionSnapshot session)
        {
            if (submission == null || submission.ReceiptOnly || submission.Content == null || session == null) return Unknown();
            var stamp = generation();
            if (!Current(stamp, session)) return Unknown();
            Tracking tracking;
            try
            {
                tracking = submissions.GetValue(submission, value => new Tracking
                {
                    Metadata = new PendingReceipt(PendingReceiptKind.Support, session.AccountId, value.IdempotencyKey,
                        value.Category, value.Build, value.ComposedAt, value.SubmittedAt),
                    Generation = stamp
                });
            }
            catch { return Unknown(); }
            if (tracking.Busy || tracking.Generation != stamp
                || !string.Equals(tracking.Metadata.AccountId, session.AccountId, StringComparison.Ordinal)) return Unknown();
            tracking.Busy = true;
            try
            {
                if (!await journal.PutAsync(tracking.Metadata) || !Current(stamp, session)) return Unknown();
                var result = await inner.SubmitAsync(submission, session);
                if (!Current(stamp, session)) return Unknown();
                if (result?.Delivery == InquiryDelivery.Received)
                {
                    await RemoveAsync(tracking.Metadata);
                    return Current(stamp, session) ? result : Unknown();
                }
                if (result?.Delivery == InquiryDelivery.Rejected)
                {
                    var removed = await RemoveAsync(tracking.Metadata);
                    return removed && Current(stamp, session) ? result : Unknown();
                }
                return result ?? Unknown();
            }
            catch { return Unknown(); }
            finally { tracking.Busy = false; }
        }

        public async Task<InquiryReceipt> ReconcileAsync(InquirySubmission original, GameApiAuthSessionSnapshot session)
        {
            if (original == null || session == null) return Unknown();
            var stamp = generation();
            if (!Current(stamp, session)) return Unknown();
            if (!submissions.TryGetValue(original, out var tracking))
            {
                var restored = original.ReceiptMetadata;
                if (restored == null || restored.Kind != PendingReceiptKind.Support
                    || !string.Equals(restored.AccountId, session.AccountId, StringComparison.Ordinal)) return Unknown();
                tracking = new Tracking { Metadata = restored, Generation = stamp };
                submissions.Add(original, tracking);
            }
            if (tracking.Busy || tracking.Generation != stamp
                || !string.Equals(tracking.Metadata.AccountId, session.AccountId, StringComparison.Ordinal)) return Unknown();
            tracking.Busy = true;
            try
            {
                var result = await inner.ReconcileAsync(original, session);
                if (!Current(stamp, session)) return Unknown();
                if (result?.Delivery == InquiryDelivery.Received) await RemoveAsync(tracking.Metadata);
                return Current(stamp, session) ? result ?? Unknown() : Unknown();
            }
            catch { return Unknown(); }
            finally { tracking.Busy = false; }
        }

        private bool Current(long stamp, GameApiAuthSessionSnapshot session) =>
            stamp == generation() && ReferenceEquals(session, currentSession());
        private async Task<bool> RemoveAsync(PendingReceipt record)
        {
            try { return await journal.RemoveAsync(record.AccountId, record.Kind, record.IdempotencyKey); }
            catch { return false; }
        }
        private static InquiryReceipt Unknown() => new InquiryReceipt(InquiryDelivery.Unknown);
    }
}
