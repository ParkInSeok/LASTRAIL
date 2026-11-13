using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Submissions
{
    public sealed class EncryptedPendingReceiptJournal : IPendingReceiptJournal
    {
        private const int Capacity = 64;
        // Multiple owners in one process must not lose each other's read/modify/write updates.
        private static readonly SemaphoreSlim StorageGate = new SemaphoreSlim(1, 1);
        private readonly IAndroidKeystoreNativeStoragePort storage;
        public EncryptedPendingReceiptJournal(IAndroidKeystoreNativeStoragePort storage)
        { this.storage = storage ?? throw new ArgumentNullException(nameof(storage)); }

        public async Task<bool> PutAsync(PendingReceipt record)
        {
            if (record == null) return false;
            await StorageGate.WaitAsync();
            try
            {
                var records = Load();
                var original = records.Find(r => Identity(r, record.AccountId, record.Kind, record.IdempotencyKey));
                if (original != null)
                {
                    if (!JToken.DeepEquals(Encode(original), Encode(record))) return false;
                }
                else
                {
                    if (records.Count >= Capacity) return false;
                    records.Add(record);
                }
                // Even a matching value may only exist in a native cache after failed commit.
                return Save(records);
            }
            catch { return false; }
            finally { StorageGate.Release(); }
        }

        public async Task<IReadOnlyList<PendingReceipt>> ReadAsync(string accountId)
        {
            if (!PendingReceipt.Identifier(accountId, 1)) throw new ArgumentException("Invalid account.");
            await StorageGate.WaitAsync();
            try { return Array.AsReadOnly(Load().Where(r => r.AccountId == accountId).ToArray()); }
            finally { StorageGate.Release(); }
        }

        public async Task<bool> RemoveAsync(string accountId, PendingReceiptKind kind, string key)
        {
            if (!PendingReceipt.Identifier(accountId, 1) || !PendingReceipt.Identifier(key, 16)
                || !Enum.IsDefined(typeof(PendingReceiptKind), kind)) return false;
            await StorageGate.WaitAsync();
            try
            {
                var records = Load(); records.RemoveAll(r => Identity(r, accountId, kind, key));
                return Save(records);
            }
            catch { return false; }
            finally { StorageGate.Release(); }
        }

        private void RequireAvailable()
        {
            if (!storage.IsAndroidRuntime || !storage.IsKeystoreAvailable || storage.UsedPlayerPrefsFallback)
                throw new InvalidOperationException("Secure receipt storage is unavailable.");
        }
        private List<PendingReceipt> Load()
        {
            RequireAvailable();
            var encrypted = storage.Load();
            if (encrypted == null) return new List<PendingReceipt>();
            var plaintext = storage.Decrypt(encrypted);
            if (string.IsNullOrWhiteSpace(plaintext) || plaintext.Length > 131072) throw new FormatException("Invalid receipt journal.");
            var root = GameApiJson.ReadObject(plaintext);
            Fields(root, "schema", "records");
            if (root["schema"]?.Type != JTokenType.Integer || (int)root["schema"] != 1
                || !(root["records"] is JArray entries) || entries.Count > Capacity) throw new FormatException("Invalid receipt journal schema.");
            var records = new List<PendingReceipt>();
            foreach (var entry in entries)
            {
                if (!(entry is JObject item)) throw new FormatException("Invalid receipt metadata.");
                Fields(item, "kind", "accountId", "idempotencyKey", "category", "appVersion", "appVersionCode", "composedAt", "submittedAt");
                if (!Enum.TryParse(GameApiJson.String(item, "kind"), out PendingReceiptKind kind)
                    || !Enum.IsDefined(typeof(PendingReceiptKind), kind)) throw new FormatException("Invalid receipt kind.");
                var version = GameApiJson.String(item, "appVersion", true);
                var textCode = GameApiJson.String(item, "appVersionCode", true);
                long? code = null;
                if (textCode != null)
                {
                    if (!long.TryParse(textCode, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                        || parsed <= 0 || parsed.ToString(CultureInfo.InvariantCulture) != textCode) throw new FormatException("Invalid receipt build.");
                    code = parsed;
                }
                var build = new AppBuildSnapshot(version, code);
                if (build.AppVersion != version) throw new FormatException("Invalid receipt version.");
                var record = new PendingReceipt(kind, GameApiJson.String(item, "accountId"),
                    GameApiJson.String(item, "idempotencyKey"), GameApiJson.String(item, "category"), build,
                    GameApiJson.UtcTimestamp(item, "composedAt"), GameApiJson.UtcTimestamp(item, "submittedAt"));
                if (records.Any(r => Identity(r, record.AccountId, record.Kind, record.IdempotencyKey)))
                    throw new FormatException("Duplicate receipt identity.");
                records.Add(record);
            }
            return records;
        }
        private bool Save(List<PendingReceipt> records)
        {
            RequireAvailable();
            var json = new JObject { ["schema"] = 1, ["records"] = new JArray(records.Select(Encode)) };
            var encrypted = storage.Encrypt(json.ToString(Formatting.None));
            return encrypted != null && storage.Save(encrypted);
        }
        private static JObject Encode(PendingReceipt value) => new JObject
        {
            ["kind"] = value.Kind.ToString(), ["accountId"] = value.AccountId, ["idempotencyKey"] = value.IdempotencyKey,
            ["category"] = value.Category, ["appVersion"] = value.Build.AppVersion,
            ["appVersionCode"] = value.Build.AppVersionCode?.ToString(CultureInfo.InvariantCulture),
            ["composedAt"] = GameApiJson.UtcText(value.ComposedAt), ["submittedAt"] = GameApiJson.UtcText(value.SubmittedAt)
        };
        private static bool Identity(PendingReceipt record, string accountId, PendingReceiptKind kind, string key) =>
            record.AccountId == accountId && record.Kind == kind && record.IdempotencyKey == key;
        private static void Fields(JObject value, params string[] expected)
        {
            if (value.Count != expected.Length || value.Properties().Any(p => !expected.Contains(p.Name)))
                throw new FormatException("Unexpected receipt metadata fields.");
        }
    }
}
