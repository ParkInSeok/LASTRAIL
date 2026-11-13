using System;
using System.IO;
using System.Text;
using LastRail.Auth;

namespace LastRail.Submissions
{
    public sealed class AndroidEncryptedReceiptFilePort : IAndroidKeystoreNativeStoragePort, ISecureStorageWriteDiagnostics
    {
        private readonly string domain;
        private readonly IAndroidKeystoreNativeStoragePort crypto;
        private readonly string path;
        private int MaximumFileBytes => domain == "lastrail_combat_checkpoint_v1:" ? 64 * 1024 * 1024 : domain == "lastrail_run_operations_v1:" || domain == "lastrail_o22_application_original_v1:" || domain == "lastrail_augment_selection_v1:" ? 16 * 1024 * 1024 : 524288;
        public AndroidEncryptedReceiptFilePort(IAndroidKeystoreNativeStoragePort crypto, string path)
            : this(crypto, path, "lastrail_receipt_journal_v1:") { }
        internal AndroidEncryptedReceiptFilePort(IAndroidKeystoreNativeStoragePort crypto, string path, string domain)
        {
            if (domain != "lastrail_receipt_journal_v1:" && domain != "lastrail_run_operations_v1:" && domain != "lastrail_combat_checkpoint_v1:" && domain != "lastrail_o22_application_original_v1:" && domain != "lastrail_augment_selection_v1:")
                throw new ArgumentException("Unrecognized encrypted storage domain.");
            this.domain = domain;
            this.crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || path.Contains("://"))
                throw new ArgumentException("Receipt storage requires an absolute local path.");
            this.path = Path.GetFullPath(path);
            if (this.path.StartsWith(@"\\", StringComparison.Ordinal)) throw new ArgumentException("Remote receipt storage is forbidden.");
        }
        public bool IsAndroidRuntime => crypto.IsAndroidRuntime;
        public bool IsKeystoreAvailable => crypto.IsKeystoreAvailable;
        public bool UsedPlayerPrefsFallback => crypto.UsedPlayerPrefsFallback;
        public string ProviderName => "android_keystore_receipt_file_v1";
        public SecureStorageWriteFailure LastWriteFailure { get; private set; }
        public static SecureStorageWriteFailure ClassifyWriteFailure(Exception error)
        {
            if (error is UnauthorizedAccessException || error is System.Security.SecurityException) return SecureStorageWriteFailure.AccessDenied;
            // Only recognized HRESULTs establish disk exhaustion; never infer it from message text.
            if (error is IOException && (error.HResult == unchecked((int)0x80070070) || error.HResult == unchecked((int)0x80070027))) return SecureStorageWriteFailure.DiskFull;
            return SecureStorageWriteFailure.Unknown;
        }
        public AndroidKeystoreEncryptedTokenPayload Encrypt(string plaintext)
        {
            if (string.IsNullOrWhiteSpace(plaintext)) throw new ArgumentException("Receipt metadata is empty.");
            return crypto.Encrypt(domain + plaintext);
        }
        public string Decrypt(AndroidKeystoreEncryptedTokenPayload encrypted)
        {
            var value = crypto.Decrypt(encrypted);
            if (value == null || !value.StartsWith(domain, StringComparison.Ordinal))
                throw new FormatException("Ciphertext does not belong to the receipt domain.");
            return value.Substring(domain.Length);
        }
        public bool Save(AndroidKeystoreEncryptedTokenPayload encrypted)
        {
            LastWriteFailure = SecureStorageWriteFailure.None;
            if (encrypted == null || !IsAndroidRuntime || !IsKeystoreAvailable || UsedPlayerPrefsFallback)
            { LastWriteFailure = SecureStorageWriteFailure.Unavailable; return false; }
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                var bytes = Encoding.UTF8.GetBytes(AndroidKeystoreEncryptedTokenPayloadJsonAdapter.ToJson(encrypted));
                if (bytes.Length > MaximumFileBytes) { LastWriteFailure = SecureStorageWriteFailure.LogicalLimit; return false; }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    4096, FileOptions.WriteThrough))
                { output.Write(bytes, 0, bytes.Length); output.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception error) { LastWriteFailure = ClassifyWriteFailure(error); return false; }
            finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
        }
        public AndroidKeystoreEncryptedTokenPayload Load()
        {
            string json;
            // File.Exists also returns false for access errors and directories. Only a
            // missing file is an empty journal; every other read failure must propagate.
            try
            {
                using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (input.Length > MaximumFileBytes) throw new FormatException("Receipt ciphertext exceeds its limit.");
                    using (var reader = new StreamReader(input)) json = reader.ReadToEnd();
                }
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
            if (string.IsNullOrWhiteSpace(json)) throw new FormatException("Receipt ciphertext is empty.");
            return AndroidKeystoreEncryptedTokenPayloadJsonAdapter.FromJson(json)
                ?? throw new FormatException("Receipt ciphertext is invalid.");
        }
        public bool Clear()
        {
            try { if (File.Exists(path)) File.Delete(path); return true; }
            catch { return false; }
        }
    }
}
