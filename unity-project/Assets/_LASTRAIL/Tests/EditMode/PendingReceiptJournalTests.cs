using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class PendingReceiptJournalTests
    {
        private const string Account = "account_receipt_fixture";
        private const string Key = "00112233445566778899aabbccddeeff";
        private static readonly DateTimeOffset Time = DateTimeOffset.Parse("2026-09-06T00:00:00Z");
        private sealed class Native : IAndroidKeystoreNativeStoragePort
        {
            public bool Available = true, FailSave, CacheOnFailure;
            public int Saves, Clears, Loads;
            public string LastPlain;
            public AndroidKeystoreEncryptedTokenPayload Stored, DiskStored;
            public readonly Dictionary<string, string> Text = new Dictionary<string, string>();
            public bool IsAndroidRuntime => Available;
            public bool IsKeystoreAvailable => Available;
            public bool UsedPlayerPrefsFallback => false;
            public string ProviderName => "explicit_test_port";
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string value)
            {
                LastPlain = value; var id = Guid.NewGuid().ToString("N"); Text[id] = value;
                return new AndroidKeystoreEncryptedTokenPayload(id, "fixture_iv");
            }
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload value) => Text[value.CipherTextBase64];
            public AndroidKeystoreEncryptedTokenPayload Load() { Loads++; return Stored; }
            public bool Save(AndroidKeystoreEncryptedTokenPayload value)
            {
                Saves++;
                if (FailSave) { if (CacheOnFailure) Stored = value; return false; }
                Stored = DiskStored = value; return true;
            }
            public bool Clear() { Clears++; Stored = DiskStored = null; return true; }
        }
        private static Type Type(string name)
        {
            var result = typeof(PlayerAuthOwner).Assembly.GetType("LastRail.Submissions." + name);
            Assert.That(result, Is.Not.Null, "Missing receipt metadata/journal type: " + name); return result;
        }
        private static object Kind(string name) => Enum.Parse(Type("PendingReceiptKind"), name);
        private static object Record(string account = Account, string key = Key, string kind = "Support", string category = null,
            AppBuildSnapshot build = null) => Activator.CreateInstance(Type("PendingReceipt"), Kind(kind), account, key,
                category ?? (kind == "Support" ? "BUG_OTHER" : "UNITY_ERROR"), build ?? new AppBuildSnapshot("1.0", 42), Time, Time.AddSeconds(1));
        private static object Journal(Native native) => Activator.CreateInstance(Type("EncryptedPendingReceiptJournal"), native);
        private static object Get(object value, string property) => value.GetType().GetProperty(property).GetValue(value);
        private static async Task<object> Call(object target, string method, params object[] args)
        {
            var task = (Task)target.GetType().GetMethod(method).Invoke(target, args); await task;
            return task.GetType().GetProperty("Result").GetValue(task);
        }
        private static async Task<object[]> Read(object journal, string account = Account) =>
            ((IEnumerable)await Call(journal, "ReadAsync", account)).Cast<object>().ToArray();

        [Test]
        public async Task NewJournalInstanceRecoversOnlyImmutableMetadata()
        {
            var native = new Native(); var journal = Journal(native); var record = Record();
            Assert.That(await Call(journal, "PutAsync", record), Is.True);
            var recovered = await Read(Journal(native));
            Assert.That(recovered.Length, Is.EqualTo(1));
            foreach (var property in new[] { "AccountId", "IdempotencyKey", "Category", "ComposedAt", "SubmittedAt" })
                Assert.That(Get(recovered[0], property), Is.EqualTo(Get(record, property)), property);
            Assert.That(Get(Get(recovered[0], "Build"), "AppVersionCode"), Is.EqualTo(42));
            var names = Type("PendingReceipt").GetProperties().Select(p => p.Name).ToArray();
            Assert.That(names.Any(n => n.Contains("Body") || n.Contains("Content") || n.Contains("Token") || n.Contains("Stack")), Is.False);
            Assert.That(Type("PendingReceipt").GetProperties().All(p => p.GetSetMethod() == null), Is.True);
            Assert.That(native.Stored.CipherTextBase64, Does.Not.Contain(Account));
            Assert.That(JObject.Parse(native.LastPlain)["records"].Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task ReadFiltersAccountAndReceiptKindsCannotDeleteEachOther()
        {
            var native = new Native(); var journal = Journal(native);
            Assert.That(await Call(journal, "PutAsync", Record()), Is.True);
            Assert.That(await Call(journal, "PutAsync", Record(kind: "ClientError")), Is.True);
            Assert.That(await Call(journal, "PutAsync", Record(account: "account_other")), Is.True);
            Assert.That((await Read(journal)).Length, Is.EqualTo(2));
            Assert.That(await Call(journal, "RemoveAsync", Account, Kind("Support"), Key), Is.True);
            Assert.That((await Read(journal)).Single().GetType().GetProperty("Kind").GetValue((await Read(journal)).Single()).ToString(), Is.EqualTo("ClientError"));
            Assert.That((await Read(journal, "account_other")).Length, Is.EqualTo(1));
        }

        [Test]
        public async Task SameKeyCannotChangeOriginalMetadataAndRecommitIsDurable()
        {
            var native = new Native(); var journal = Journal(native);
            Assert.That(await Call(journal, "PutAsync", Record()), Is.True);
            Assert.That(await Call(journal, "PutAsync", Record(build: new AppBuildSnapshot("2.0", 43))), Is.False);
            Assert.That(Get(Get((await Read(journal)).Single(), "Build"), "AppVersion"), Is.EqualTo("1.0"));
            var before = native.Saves;
            Assert.That(await Call(journal, "PutAsync", Record()), Is.True);
            Assert.That(native.Saves, Is.GreaterThan(before), "A matching cached value must still receive a durable commit acknowledgement");
        }

        [Test]
        public async Task FailedCachedSaveMustBeRecommittedBeforeSuccess()
        {
            var native = new Native { FailSave = true, CacheOnFailure = true }; var journal = Journal(native);
            Assert.That(await Call(journal, "PutAsync", Record()), Is.False); Assert.That(native.DiskStored, Is.Null);
            native.FailSave = false;
            Assert.That(await Call(journal, "PutAsync", Record()), Is.True);
            Assert.That(native.Saves, Is.EqualTo(2)); Assert.That(native.DiskStored, Is.Not.Null);
        }

        [Test]
        public async Task FailedDeleteRetainsOriginalAndRepeatedDeleteIsSafe()
        {
            var native = new Native(); var journal = Journal(native); await Call(journal, "PutAsync", Record());
            native.FailSave = true;
            Assert.That(await Call(journal, "RemoveAsync", Account, Kind("Support"), Key), Is.False);
            Assert.That((await Read(Journal(native))).Length, Is.EqualTo(1));
            native.FailSave = false;
            Assert.That(await Call(journal, "RemoveAsync", Account, Kind("Support"), Key), Is.True);
            Assert.That(await Call(journal, "RemoveAsync", Account, Kind("Support"), Key), Is.True);
            Assert.That(await Read(Journal(native)), Is.Empty);
        }

        [Test]
        public async Task CapacityDoesNotEvictHeldRecordsOrBlockIndependentEntriesBeforeFull()
        {
            var native = new Native(); var journal = Journal(native);
            for (var i = 0; i < 64; i++) Assert.That(await Call(journal, "PutAsync", Record(key: i.ToString("x32"))), Is.True);
            Assert.That(await Call(journal, "PutAsync", Record(key: "overflow_receipt_key_000000000001")), Is.False);
            Assert.That((await Read(journal)).Length, Is.EqualTo(64));
            Assert.That(await Call(journal, "RemoveAsync", Account, Kind("Support"), 0.ToString("x32")), Is.True);
            Assert.That(await Call(journal, "PutAsync", Record(key: "new_independent_receipt_000000001")), Is.True);
        }

        [Test]
        public async Task UnavailableOrLostKeyNeverBecomesEmptySuccessfulStorage()
        {
            var native = new Native(); var journal = Journal(native); await Call(journal, "PutAsync", Record());
            native.Available = false;
            Assert.That(await Call(journal, "PutAsync", Record()), Is.False);
            Assert.CatchAsync<Exception>(async () => await Read(journal));
            native.Available = true; native.Text.Clear();
            Assert.CatchAsync<Exception>(async () => await Read(Journal(native)));
            Assert.That(await Call(journal, "RemoveAsync", Account, Kind("Support"), Key), Is.False);
            Assert.That(native.Stored, Is.Not.Null);
        }

        [TestCase("{malformed")]
        [TestCase("{\"schema\":999,\"records\":[]}")]
        [TestCase("{\"schema\":1,\"records\":[{\"body\":\"forbidden\"}]}")]
        public void MalformedMetadataIsHeldWithoutSilentOverwrite(string corrupt)
        {
            var native = new Native(); native.Stored = native.Encrypt(corrupt); var before = native.Stored;
            var journal = Journal(native);
            Assert.CatchAsync<Exception>(async () => await Read(journal));
            Assert.That(native.Stored, Is.SameAs(before)); Assert.That(native.Clears, Is.Zero);
        }

        [Test]
        public void ReceiptFileUsesSeparateCiphertextAndNeverCallsAuthSaveOrLoad()
        {
            var native = new Native(); var path = Path.Combine(Path.GetTempPath(), "receipt-file-" + Guid.NewGuid().ToString("N") + ".enc");
            try
            {
                var portType = Type("AndroidEncryptedReceiptFilePort");
                var port = (IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(portType, native, path);
                var encrypted = port.Encrypt("metadata_fixture_secret");
                Assert.That(port.Save(encrypted), Is.True);
                Assert.That(native.Saves, Is.Zero); Assert.That(native.Stored, Is.Null);
                Assert.That(File.ReadAllText(path), Does.Not.Contain("metadata_fixture_secret"));
                var reopened = (IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(portType, native, path);
                Assert.That(reopened.Decrypt(reopened.Load()), Is.EqualTo("metadata_fixture_secret"));
                Assert.That(native.Loads, Is.Zero);
                Assert.That(reopened.Clear(), Is.True); Assert.That(File.Exists(path), Is.False);
                Assert.That(native.Clears, Is.Zero);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Test]
        public void LockedFileReplacementFailsWithoutLosingCommittedMetadata()
        {
            var native = new Native(); var path = Path.Combine(Path.GetTempPath(), "receipt-atomic-" + Guid.NewGuid().ToString("N") + ".enc");
            try
            {
                var port = (IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(Type("AndroidEncryptedReceiptFilePort"), native, path);
                Assert.That(port.Save(port.Encrypt("original_metadata")), Is.True);
                using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    Assert.That(port.Save(port.Encrypt("replacement_metadata")), Is.False);
                Assert.That(port.Decrypt(port.Load()), Is.EqualTo("original_metadata"));
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Test]
        public void DirectoryAtReceiptPathIsReadFailureNotAnEmptyJournal()
        {
            var path = Path.Combine(Path.GetTempPath(), "receipt-directory-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try
            {
                var port = (IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(Type("AndroidEncryptedReceiptFilePort"), new Native(), path);
                Assert.Catch(() => port.Load(), "An inaccessible/non-file record must not be reported as absent.");
                Assert.That(Directory.Exists(path), Is.True);
            }
            finally { Directory.Delete(path); }
        }

        [Test]
        public void MissingReceiptIsEmptyButLockedReceiptRemainsRecoverable()
        {
            var path = Path.Combine(Path.GetTempPath(), "receipt-read-" + Guid.NewGuid().ToString("N") + ".enc");
            var port = (IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(Type("AndroidEncryptedReceiptFilePort"), new Native(), path);
            try
            {
                Assert.That(port.Load(), Is.Null);
                Assert.That(port.Save(port.Encrypt("original_metadata")), Is.True);
                var original = File.ReadAllBytes(path);
                using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    Assert.Catch<IOException>(() => port.Load());
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
                Assert.That(port.Decrypt(port.Load()), Is.EqualTo("original_metadata"));
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Test]
        public void ReceiptCiphertextCannotAcceptAnAuthDomainPayload()
        {
            var native = new Native(); var path = Path.Combine(Path.GetTempPath(), "receipt-domain-" + Guid.NewGuid().ToString("N") + ".enc");
            var port = (IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(Type("AndroidEncryptedReceiptFilePort"), native, path);
            Assert.Catch(() => port.Decrypt(native.Encrypt("{\"accessToken\":\"fixture_auth_only\"}")));
            Assert.That(File.Exists(path), Is.False);
        }
    }
}
