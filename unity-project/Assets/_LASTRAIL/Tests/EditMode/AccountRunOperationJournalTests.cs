using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class AccountRunOperationJournalTests
    {
        // Explicit test double, not a product crypto implementation or fallback.
        sealed class Crypto : IAndroidKeystoreNativeStoragePort
        {
            readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public bool Available = true, FailEncrypt;
            public int EncryptCalls, FailOnEncryptCall;
            public readonly List<string> Decrypted = new List<string>();
            public bool IsAndroidRuntime => Available;
            public bool IsKeystoreAvailable => Available;
            public bool UsedPlayerPrefsFallback => false;
            public string ProviderName => "explicit_isolation_test_double";
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string text)
            {
                EncryptCalls++;
                if (FailEncrypt || EncryptCalls == FailOnEncryptCall) throw new IOException("Injected encryption failure");
                var id = Guid.NewGuid().ToString("N"); values.Add(id, text);
                return new AndroidKeystoreEncryptedTokenPayload(id, "test-iv");
            }
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload value)
            { var text = values[value.CipherTextBase64]; Decrypted.Add(text); return text; }
            public AndroidKeystoreEncryptedTokenPayload Load() => throw new InvalidOperationException("File port owns I/O");
            public bool Save(AndroidKeystoreEncryptedTokenPayload value) => throw new InvalidOperationException("File port owns I/O");
            public bool Clear() => throw new InvalidOperationException("Never clear the platform key");
        }
        const string A = "account_a", B = "account_b", Key = "original_start_key_1234";
        const string Response = "{\"accepted\":true,\"startStatus\":\"STARTED\",\"runId\":\"run_a\",\"operationPreviewId\":\"preview_a\",\"balanceVersion\":\"1\",\"serverStartedAt\":\"2026-09-06T00:00:00.000Z\",\"runVersionSnapshot\":null}";
        Crypto crypto;
        string root, legacy, partitions;
        [SetUp] public void Setup()
        {
            crypto = new Crypto();
            root = Path.Combine("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/AccountJournalFiles", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root); legacy = Path.Combine(root, "operations-v1.enc"); partitions = Path.Combine(root, "accounts-v2");
        }
        [TearDown] public void Cleanup() { if (Directory.Exists(root)) Directory.Delete(root, true); }
        static PendingRunOperation Request(string account = A, string preview = "preview_a") => PendingRunOperation.Start(account,
            RunStartRequest.Create(preview, Key), new AppBuildSnapshot("1.0", 10), DateTimeOffset.Parse("2026-09-06T00:00:00.1234567Z"));
        EncryptedRunOperationJournal Legacy() => new EncryptedRunOperationJournal(new AndroidEncryptedRunFilePort(crypto, legacy));
        IRunOperationJournal Journal()
        {
            var type = typeof(PendingRunOperation).Assembly.GetType("LastRail.Run.AccountRunOperationJournal");
            Assert.That(type, Is.Not.Null, "Missing scoped product journal");
            return (IRunOperationJournal)Activator.CreateInstance(type, crypto, legacy, partitions);
        }
        string[] PublishedFiles() => Directory.GetFiles(partitions, "operations.enc", SearchOption.AllDirectories);

        [Test] public async Task RestartReadsOnlySelectedAccountAndPreservesLegacyAndOriginalReceipt()
        {
            var old = Legacy(); var a = Request();
            Assert.IsTrue(await old.PutAsync(a)); Assert.IsTrue(await old.ConfirmAsync(a, Response));
            Assert.IsTrue(await old.PutAsync(Request(B, "preview_b")));
            var bytes = File.ReadAllBytes(legacy);
            var result = (await Journal().ReadAsync(A)).Single();
            Assert.AreEqual(Key, result.IdempotencyKey); Assert.AreEqual(Response, result.AcceptedResponseJson);
            Assert.AreEqual(a.ComposedAt, result.ComposedAt); Assert.AreEqual(a.BodyJson, result.BodyJson);
            crypto.Decrypted.Clear();
            Assert.AreEqual(Response, (await Journal().ReadAsync(A)).Single().AcceptedResponseJson);
            Assert.That(crypto.Decrypted, Is.Not.Empty);
            Assert.IsFalse(crypto.Decrypted.Any(x => x.Contains(B)), "Steady state read parsed another account");
            Assert.AreEqual(B, (await Journal().ReadAsync(B)).Single().AccountId);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(legacy));
        }
        [Test] public async Task RemovedConfirmedRecordStaysRemovedAcrossRestartWithoutReimportingLegacy()
        {
            var a = Request(); await Legacy().PutAsync(a); await Legacy().ConfirmAsync(a, Response);
            var journal = Journal(); await journal.ReadAsync(A);
            Assert.IsTrue(await journal.RemoveConfirmedAsync(A, PendingRunKind.Start, Key));
            Assert.IsEmpty(await Journal().ReadAsync(A));
            Assert.AreEqual(Response, (await Legacy().ReadAsync(A)).Single().AcceptedResponseJson);
        }
        [Test] public async Task MissingPublishedFileFailsClosedAndDoesNotResurrectLegacy()
        {
            await Legacy().PutAsync(Request()); await Journal().ReadAsync(A);
            File.Delete(PublishedFiles().Single());
            Assert.ThrowsAsync<IOException>(async () => await Journal().ReadAsync(A));
            Assert.IsFalse(await Journal().PutAsync(Request()));
            Assert.That(PublishedFiles(), Is.Empty);
        }
        [Test] public async Task SwappedEmptyAccountCiphertextCannotEraseAnotherAccount()
        {
            await Journal().ReadAsync(B); var b = PublishedFiles().Single();
            Assert.IsTrue(await Journal().PutAsync(Request())); var a = PublishedFiles().Single(p => p != b);
            File.Copy(b, a, true);
            Assert.ThrowsAsync<FormatException>(async () => await Journal().ReadAsync(A));
            var swapped = File.ReadAllBytes(a);
            Assert.IsFalse(await Journal().PutAsync(Request()));
            CollectionAssert.AreEqual(swapped, File.ReadAllBytes(a));
        }
        [Test] public async Task MissingPublishedDirectoryCannotReimportCompletedLegacy()
        {
            await Legacy().PutAsync(Request()); await Legacy().ConfirmAsync(Request(), Response);
            var journal = Journal(); await journal.ReadAsync(A);
            Assert.IsTrue(await journal.RemoveConfirmedAsync(A, PendingRunKind.Start, Key));
            Directory.Delete(Path.GetDirectoryName(PublishedFiles().Single()), true);
            Assert.ThrowsAsync<IOException>(async () => await Journal().ReadAsync(A));
            Assert.IsFalse(await Journal().PutAsync(Request()));
        }
        [Test] public async Task FailedMigrationKeepsLegacyAndCanRetryWithoutPublishingPartialRecords()
        {
            await Legacy().PutAsync(Request()); var original = File.ReadAllBytes(legacy);
            crypto.FailEncrypt = true;
            Assert.CatchAsync<Exception>(async () => await Journal().ReadAsync(A));
            Assert.That(Directory.Exists(partitions) ? PublishedFiles() : Array.Empty<string>(), Is.Empty);
            CollectionAssert.AreEqual(original, File.ReadAllBytes(legacy));
            crypto.FailEncrypt = false;
            Assert.AreEqual(Key, (await Journal().ReadAsync(A)).Single().IdempotencyKey);
        }
        [Test] public async Task UnavailableSecureProviderCannotCreateAnyPartition()
        {
            crypto.Available = false;
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Journal().ReadAsync(A));
            Assert.IsFalse(await Journal().PutAsync(Request()));
            Assert.IsFalse(Directory.Exists(partitions));
        }
        [Test] public async Task InterruptedMarkerPublicationRecoversPublishedSnapshotWithoutLegacyRead()
        {
            await Legacy().PutAsync(Request());
            crypto.FailOnEncryptCall = crypto.EncryptCalls + 2;
            Assert.CatchAsync<Exception>(async () => await Journal().ReadAsync(A));
            Assert.That(PublishedFiles().Length, Is.EqualTo(1));
            File.WriteAllText(legacy, "deliberately unreadable old fixture");
            crypto.FailOnEncryptCall = 0;
            Assert.AreEqual(Key, (await Journal().ReadAsync(A)).Single().IdempotencyKey);
        }
        [Test] public async Task VersionedClaimMigrationPreservesOriginalReceiptAndResponseVersion()
        {
            var pending = ConfirmedRunReceiptArchiveTests.Claim(false);
            var confirmed = ConfirmedRunReceiptArchiveTests.Claim();
            Assert.IsTrue(await Legacy().PutAsync(pending));
            Assert.IsTrue(await Legacy().ConfirmAsync(pending, confirmed.AcceptedResponseJson));
            var restored = (await Journal().ReadAsync(pending.AccountId)).Single();
            Assert.AreEqual(pending.ResponseVersion, restored.ResponseVersion);
            Assert.AreEqual(pending.IdempotencyKey, restored.IdempotencyKey);
            Assert.AreEqual(pending.BodyJson, restored.BodyJson);
            Assert.AreEqual(confirmed.AcceptedResponseJson, restored.AcceptedResponseJson);
        }
        [Test] public async Task ConcurrentInstancesPreserveOneOriginalOperation()
        {
            var results = await Task.WhenAll(Journal().PutAsync(Request()), Journal().PutAsync(Request()));
            Assert.That(results.All(x => x), Is.True);
            Assert.That((await Journal().ReadAsync(A)).Count, Is.EqualTo(1));
        }
        [Test] public async Task SameKeyWithConflictingRequestCannotOverwriteOriginal()
        {
            Assert.IsTrue(await Journal().PutAsync(Request()));
            Assert.IsFalse(await Journal().PutAsync(Request(preview: "conflicting_preview")));
            Assert.AreEqual("preview_a", (await Journal().ReadAsync(A)).Single().TargetId);
        }
    }
}
