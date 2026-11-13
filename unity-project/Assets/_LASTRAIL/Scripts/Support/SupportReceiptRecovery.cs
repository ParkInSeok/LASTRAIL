using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Submissions;

namespace LastRail.Support
{
    public sealed class SupportReceiptRecovery : IDisposable
    {
        private readonly SupportInquiryController controller;
        private readonly SupportInquiryHttpClient inner;
        private readonly IPendingReceiptJournal journal;
        private readonly Func<GameApiAuthSessionSnapshot> currentSession;
        private readonly Func<long> generation;
        private readonly string account;
        private readonly long origin;
        private bool disposed, initialized;
        public IReadOnlyList<PendingReceipt> Records { get; private set; } = Array.Empty<PendingReceipt>();
        public PendingReceipt Selected { get; private set; }
        public bool Busy { get; private set; }
        public bool StorageFailed { get; private set; }
        public bool ManagingDraft => Selected != null && controller.CanEdit && !string.IsNullOrEmpty(controller.Content);
        public string Notice { get; private set; } = "";
        public event Action Changed;

        public SupportReceiptRecovery(SupportInquiryController controller, SupportInquiryHttpClient inner,
            IPendingReceiptJournal journal, Func<GameApiAuthSessionSnapshot> currentSession, Func<long> generation)
        {
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            this.currentSession = currentSession ?? throw new ArgumentNullException(nameof(currentSession));
            this.generation = generation ?? throw new ArgumentNullException(nameof(generation));
            account = currentSession()?.AccountId ?? throw new InvalidOperationException("Receipt recovery requires an account.");
            origin = generation();
        }
        private bool Current => !disposed && origin == generation() && currentSession()?.AccountId == account;
        private bool CanChange => Current && !Busy && !StorageFailed && controller.State != InquiryState.Sending;
        public async Task RefreshAsync()
        {
            if (!Current || Busy || controller.State == InquiryState.Sending) return;
            Busy = true; Changed?.Invoke();
            try
            {
                var records = await journal.ReadAsync(account);
                if (!Current) return;
                if (records == null || records.Count > 64) throw new InvalidOperationException("Invalid receipt list.");
                Records = Array.AsReadOnly(records.Where(r => r != null && r.AccountId == account).ToArray());
                StorageFailed = false;
                Selected = Records.FirstOrDefault(r => Same(r, Selected));
                Notice = Records.Count == 0 ? "\uBCF4\uB958 \uAE30\uB85D\uC774 \uC5C6\uC2B5\uB2C8\uB2E4."
                    : Records.Count >= 64 ? "\uBCF4\uB958 \uAE30\uB85D 64\uAC1C. \uC0C8 \uC811\uC218 \uC804 \uAE30\uB85D \uC815\uB9AC\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4."
                    : "\uC811\uC218 \uC5EC\uBD80\uAC00 \uBBF8\uD655\uC815\uC778 \uAE30\uB85D\uC785\uB2C8\uB2E4.";
                if (!initialized)
                {
                    initialized = true;
                    var first = Records.FirstOrDefault(r => r.Kind == PendingReceiptKind.Support);
                    Busy = false;
                    if (first != null && controller.Pending == null && string.IsNullOrEmpty(controller.Content)) Select(first);
                }
            }
            catch
            {
                if (Current)
                {
                    StorageFailed = true;
                    Notice = "\uBCF4\uB958 \uAE30\uB85D\uC744 \uC77D\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uBCF4\uC548 \uC800\uC7A5\uC18C\uB97C \uD655\uC778\uD55C \uD6C4 \uB2E4\uC2DC \uC2DC\uB3C4\uD574 \uC8FC\uC138\uC694.";
                }
            }
            finally { Busy = false; if (Current) Changed?.Invoke(); }
        }
        public bool Select(PendingReceipt record)
        {
            if (!CanChange) return false;
            if (record == null)
            {
                if (!ManagingDraft) return false;
                Selected = null; Changed?.Invoke(); return true;
            }
            var selected = Records.FirstOrDefault(r => Same(r, record));
            if (selected == null || selected.AccountId != account) return false;
            if (Same(Selected, selected)) return true;
            if (controller.CanEdit && !string.IsNullOrEmpty(controller.Content))
            {
                // Selecting metadata for cleanup must not replace a separate unsent draft.
                Selected = selected;
                Notice = "\uC791\uC131 \uC911\uC778 \uBB38\uC758\uB294 \uC720\uC9C0\uB429\uB2C8\uB2E4. \uC120\uD0DD\uD55C \uBCF4\uB958 \uAE30\uB85D\uC740 \uC0AD\uC81C\uB9CC \uAC00\uB2A5\uD569\uB2C8\uB2E4.";
                Changed?.Invoke(); return true;
            }
            if (controller.State == InquiryState.Uncertain) { if (!controller.StartIndependentInquiry()) return false; }
            else if (controller.State == InquiryState.Received) { if (!controller.StartAnother()) return false; }
            else if (!controller.CanEdit || !string.IsNullOrEmpty(controller.Content)) return false;
            if (selected.Kind == PendingReceiptKind.Support && !controller.RestorePendingReceipt(inner.RestoreReceiptLookup(selected))) return false;
            Selected = selected; Changed?.Invoke(); return true;
        }
        public bool StartIndependentInquiry()
        {
            if (!CanChange) return false;
            if (ManagingDraft) return Select(null);
            if (controller.State == InquiryState.Uncertain) { if (!controller.StartIndependentInquiry()) return false; }
            else if (controller.State == InquiryState.Received) { if (!controller.StartAnother()) return false; }
            else if (!controller.CanEdit || !string.IsNullOrEmpty(controller.Content)) return false;
            Selected = null; initialized = true; Changed?.Invoke(); return true;
        }
        public async Task<bool> ForgetSelectedAsync(bool confirmed)
        {
            if (!confirmed || !CanChange || Selected == null) return false;
            var selected = Selected;
            Busy = true; Changed?.Invoke();
            try
            {
                var removed = await journal.RemoveAsync(account, selected.Kind, selected.IdempotencyKey);
                if (!Current) return false;
                if (!removed) throw new InvalidOperationException("Receipt deletion failed.");
                Records = Array.AsReadOnly(Records.Where(r => !Same(r, selected)).ToArray());
                Selected = null;
                if (controller.Pending?.IdempotencyKey == selected.IdempotencyKey && selected.Kind == PendingReceiptKind.Support)
                {
                    if (controller.State == InquiryState.Received) controller.StartAnother();
                    else controller.StartIndependentInquiry();
                }
                Notice = "\uC774 \uAE30\uAE30\uC758 \uAE30\uB85D\uB9CC \uC0AD\uC81C\uD588\uC2B5\uB2C8\uB2E4. \uC11C\uBC84 \uC811\uC218\uB97C \uCDE8\uC18C\uD55C \uAC83\uC740 \uC544\uB2D9\uB2C8\uB2E4.";
                return true;
            }
            catch
            {
                if (Current) Notice = "\uAE30\uB85D\uC744 \uC0AD\uC81C\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uAE30\uC874 \uBCF4\uB958 \uAE30\uB85D\uC740 \uC720\uC9C0\uB429\uB2C8\uB2E4.";
                return false;
            }
            finally { Busy = false; if (Current) Changed?.Invoke(); }
        }
        private static bool Same(PendingReceipt a, PendingReceipt b) => a != null && b != null
            && a.AccountId == b.AccountId && a.Kind == b.Kind && a.IdempotencyKey == b.IdempotencyKey;
        public void Dispose() { disposed = true; Records = Array.Empty<PendingReceipt>(); Selected = null; Changed = null; }
    }
}
