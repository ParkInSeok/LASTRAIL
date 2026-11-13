using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Diagnostics;
using LastRail.Support;

namespace LastRail.Submissions
{
    public enum PendingReceiptKind { Support, ClientError }

    public sealed class PendingReceipt
    {
        public PendingReceiptKind Kind { get; }
        public string AccountId { get; }
        public string IdempotencyKey { get; }
        public string Category { get; }
        public AppBuildSnapshot Build { get; }
        public DateTimeOffset ComposedAt { get; }
        public DateTimeOffset SubmittedAt { get; }
        public PendingReceipt(PendingReceiptKind kind, string accountId, string idempotencyKey,
            string category, AppBuildSnapshot build, DateTimeOffset composedAt, DateTimeOffset submittedAt)
        {
            if (!Enum.IsDefined(typeof(PendingReceiptKind), kind) || !Identifier(accountId, 1)
                || !Identifier(idempotencyKey, 16)) throw new ArgumentException("Invalid receipt identity.");
            if (kind == PendingReceiptKind.Support ? !SupportInquiryHttpClient.Categories.Contains(category)
                : category != "UNITY_ERROR" && category != "UNITY_EXCEPTION" && category != "UNITY_ASSERT")
                throw new ArgumentException("Invalid receipt category.");
            Kind = kind; AccountId = accountId; IdempotencyKey = idempotencyKey; Category = category;
            Build = build ?? new AppBuildSnapshot(null, null);
            ComposedAt = composedAt.ToUniversalTime(); SubmittedAt = submittedAt.ToUniversalTime();
        }
        internal static bool Identifier(string value, int minimum) => value != null && value.Length >= minimum
            && value.Length <= 128 && value.All(c => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z'
                || c >= '0' && c <= '9' || c == '_' || c == '-');
    }

    public interface IPendingReceiptJournal
    {
        Task<bool> PutAsync(PendingReceipt record);
        Task<IReadOnlyList<PendingReceipt>> ReadAsync(string accountId);
        Task<bool> RemoveAsync(string accountId, PendingReceiptKind kind, string key);
    }
}
