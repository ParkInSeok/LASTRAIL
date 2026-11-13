using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Submissions;
using Newtonsoft.Json.Linq;

namespace LastRail.Run
{
    // Durable original receipts, separate from the bounded pending-operation journal.
    // No cache or automatic pruning: retention changes require an explicit policy.
    public sealed class ConfirmedRunReceiptArchive
    {
        static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        readonly IAndroidKeystoreNativeStoragePort crypto;
        readonly string directory;

        public ConfirmedRunReceiptArchive(IAndroidKeystoreNativeStoragePort crypto, string directory)
        {
            this.crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
            if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathRooted(directory) || directory.Contains("://"))
                throw new ArgumentException("Archive requires an absolute local directory.");
            this.directory = Path.GetFullPath(directory);
            if (this.directory.StartsWith(@"\\", StringComparison.Ordinal)) throw new ArgumentException("Remote archive is forbidden.");
        }

        public async Task<bool> StoreAsync(PendingRunOperation operation)
        {
            if (operation?.AcceptedResponseJson == null || operation.Kind != PendingRunKind.Claim) return false;
            await Gate.WaitAsync();
            try
            {
                operation.Confirmed(operation.AcceptedResponseJson);
                var journal = Journal(operation.AccountId, operation.TargetId);
                var records = await journal.ReadAsync(operation.AccountId);
                var existing = records.SingleOrDefault();
                if (existing != null && (!existing.SameRequest(operation) ||
                    existing.AcceptedResponseJson != null && existing.AcceptedResponseJson != operation.AcceptedResponseJson)) return false;
                if (existing?.AcceptedResponseJson == null)
                {
                    var request = new PendingRunOperation(operation.Kind, operation.AccountId, operation.TargetId,
                        operation.IdempotencyKey, operation.BodyJson, operation.Build, operation.ComposedAt, null, operation.ResponseVersion);
                    if (!await journal.PutAsync(request) || !await journal.ConfirmAsync(request, operation.AcceptedResponseJson)) return false;
                }
                // A successful write alone is not permission to remove the pending source.
                var restored = (await Journal(operation.AccountId, operation.TargetId).ReadAsync(operation.AccountId)).SingleOrDefault();
                return restored != null && restored.SameRequest(operation) && restored.AcceptedResponseJson == operation.AcceptedResponseJson;
            }
            catch { return false; } // Caller retains the source and exposes a storage retry.
            finally { Gate.Release(); }
        }

        public async Task<PendingRunOperation> ReadAsync(string account, string runId)
        {
            await Gate.WaitAsync();
            try
            {
                var row = (await Journal(account, runId).ReadAsync(account)).SingleOrDefault();
                if (row != null && (row.TargetId != runId || row.Kind != PendingRunKind.Claim)) throw new FormatException("Archived run identity mismatch.");
                return row?.AcceptedResponseJson == null ? null : row;
            }
            finally { Gate.Release(); }
        }

        EncryptedRunOperationJournal Journal(string account, string runId)
        {
            if (!PendingReceipt.Identifier(account, 1) || !PendingReceipt.Identifier(runId, 1))
                throw new ArgumentException("Invalid archived receipt identity.");
            var identity = new JArray(account, runId).ToString(Newtonsoft.Json.Formatting.None);
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-", "").ToLowerInvariant();
            return new EncryptedRunOperationJournal(new AndroidEncryptedRunFilePort(crypto, Path.Combine(directory, hash + ".enc")));
        }
    }
}
