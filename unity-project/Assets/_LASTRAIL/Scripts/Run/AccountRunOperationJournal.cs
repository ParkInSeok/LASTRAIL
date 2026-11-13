using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Submissions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Run
{
    // Owns account partition selection and one-time legacy import. Encryption,
    // validation and atomic file persistence remain with the existing ports.
    public sealed class AccountRunOperationJournal : IRunOperationJournal,IOrdinaryCheckpointJournal,ITravelConflictJournal
    {
        static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        readonly IAndroidKeystoreNativeStoragePort crypto;
        readonly string legacyPath, directory;

        public AccountRunOperationJournal(IAndroidKeystoreNativeStoragePort crypto, string legacyPath, string directory)
        {
            this.crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
            this.legacyPath = LocalPath(legacyPath);
            this.directory = LocalPath(directory);
            if (this.legacyPath == this.directory) throw new ArgumentException("Partition root cannot be the legacy file.");
        }

        public async Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account)
        {
            await Gate.WaitAsync();
            try { return await (await SelectAsync(account)).ReadAsync(account); }
            finally { Gate.Release(); }
        }
        public Task<bool> PutAsync(PendingRunOperation operation) => operation == null ? Task.FromResult(false)
            : WriteAsync(operation.AccountId, journal => journal.PutAsync(operation));
        public Task<bool> ConfirmAsync(PendingRunOperation operation, string responseJson) => operation == null ? Task.FromResult(false)
            : WriteAsync(operation.AccountId, journal => journal.ConfirmAsync(operation, responseJson));
        public Task<bool> RemoveConfirmedAsync(string account, PendingRunKind kind, string key) =>
            WriteAsync(account, journal => journal.RemoveConfirmedAsync(account, kind, key));
        public Task<bool> ConfirmFirstCheckpointAsync(PendingRunOperation operation,long revision)=>operation==null?Task.FromResult(false)
            :WriteAsync(operation.AccountId,journal=>journal.ConfirmFirstCheckpointAsync(operation,revision));
        public Task<bool> ReplaceRejectedTravelAsync(PendingRunOperation rejected,PendingRunOperation recovery,string conflictJson)=>rejected==null?Task.FromResult(false)
            :WriteAsync(rejected.AccountId,journal=>journal.ReplaceRejectedTravelAsync(rejected,recovery,conflictJson));

        async Task<bool> WriteAsync(string account, Func<EncryptedRunOperationJournal, Task<bool>> write)
        {
            await Gate.WaitAsync();
            try { return await write(await SelectAsync(account)); }
            catch { return false; } // Existing client exposes storage retry and retains the original request.
            finally { Gate.Release(); }
        }

        async Task<EncryptedRunOperationJournal> SelectAsync(string account)
        {
            if (!PendingReceipt.Identifier(account, 1)) throw new ArgumentException("Invalid run account.");
            if (!crypto.IsAndroidRuntime || !crypto.IsKeystoreAvailable || crypto.UsedPlayerPrefsFallback)
                throw new InvalidOperationException("Secure run storage is unavailable.");
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(account))).Replace("-", "").ToLowerInvariant();
            var published = Path.Combine(directory, hash);
            var anchor = new AndroidEncryptedRunFilePort(crypto, Path.Combine(directory, hash + ".anchor.enc"));
            var savedAnchor = anchor.Load();
            if (savedAnchor != null)
            {
                var value = GameApiJson.ReadObject(anchor.Decrypt(savedAnchor));
                if (value.Count != 2 || GameApiJson.String(value, "version") != "account-run-anchor-v1"
                    || GameApiJson.String(value, "accountId") != account)
                    throw new FormatException("Account run initialization marker is invalid.");
            }
            if (Directory.Exists(published))
            {
                // An empty journal is explicit. Missing is never treated as empty
                // or permission to resurrect records from the preserved legacy.
                if (!File.Exists(Path.Combine(published, "operations.enc")))
                    throw new IOException("Published account run journal is missing; original recovery is required.");
                var journal = Scoped(published, account);
                if (savedAnchor == null)
                {
                    // A crash between directory publication and its small marker
                    // is recovered from the published snapshot, never the legacy.
                    await journal.ReadAsync(account);
                    SaveAnchor(anchor, account);
                }
                return journal;
            }
            if (savedAnchor != null) throw new IOException("Published account run directory is missing; original recovery is required.");

            var legacy = new EncryptedRunOperationJournal(new AndroidEncryptedRunFilePort(crypto, legacyPath));
            var selected = await legacy.ReadAsync(account);
            // Only this first import needs the old all-account snapshot. No cache
            // retains it and subsequent reads never open another account's file.
            Directory.CreateDirectory(directory);
            var staging = Path.Combine(directory, ".import-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            try
            {
                var journal = Scoped(staging, account);
                if (!await journal.CreateScopedSnapshotAsync(selected)) throw new IOException("Account journal import could not be persisted.");
                var restored = await journal.ReadAsync(account);
                if (restored.Count != selected.Count || selected.Any(original => !restored.Any(row =>
                    row.SameRequest(original) && row.AcceptedResponseJson == original.AcceptedResponseJson)))
                    throw new IOException("Account journal import did not preserve original records.");
                // Same-parent directory rename publishes all records together.
                // A competing publisher fails here; never overwrite its state.
                Directory.Move(staging, published);
                // The independent marker detects loss of the whole account folder.
                // No command may write until both publication steps are durable.
                SaveAnchor(anchor, account);
                return Scoped(published, account);
            }
            finally
            {
                // This invocation owns this unpublished GUID directory only.
                // Interrupted-process staging is non-authoritative and untouched.
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }

        EncryptedRunOperationJournal Scoped(string folder, string account) => new EncryptedRunOperationJournal(
            new AndroidEncryptedRunFilePort(crypto, Path.Combine(folder, "operations.enc")), account);

        static void SaveAnchor(AndroidEncryptedRunFilePort anchor, string account)
        {
            var value = anchor.Encrypt(new JObject { ["version"] = "account-run-anchor-v1", ["accountId"] = account }.ToString(Formatting.None));
            if (value == null || !anchor.Save(value)) throw new IOException("Account run initialization marker could not be persisted.");
        }

        static string LocalPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Path.IsPathRooted(value) || value.Contains("://"))
                throw new ArgumentException("Run storage requires an absolute local path.");
            var full = Path.GetFullPath(value);
            if (full.StartsWith(@"\\", StringComparison.Ordinal)) throw new ArgumentException("Remote run storage is forbidden.");
            return full;
        }
    }
}
