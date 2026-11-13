#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class LocalContentBaselineArchiveTests
    {
        private const string Identity = "LOCAL_TEST|StandaloneWindows64|unity-fixture|addressables-fixture";
        private string workspace, root, state, runtime, remote;
        private MethodInfo archive, validate;

        [SetUp]
        public void SetUp()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("LastRail.Editor.LocalContentBaselineArchive"))
                .FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, "RED missing Editor LocalContentBaselineArchive");
            archive = type.GetMethod("Archive", BindingFlags.Public | BindingFlags.Static, null,
                Enumerable.Repeat(typeof(string), 7).ToArray(), null);
            validate = type.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static, null,
                Enumerable.Repeat(typeof(string), 3).ToArray(), null);
            Assert.That(archive, Is.Not.Null); Assert.That(validate, Is.Not.Null);
            Assert.That(archive.ReturnType, Is.EqualTo(typeof(string)));
            Assert.That(validate.ReturnType, Is.EqualTo(typeof(string)));
            workspace = Path.Combine(Path.GetTempPath(), "lastrail-baseline-test-" + Guid.NewGuid().ToString("N"));
            root = Path.Combine(workspace, "archives");
            runtime = Path.Combine(workspace, "source-runtime");
            remote = Path.Combine(workspace, "source-remote");
            Directory.CreateDirectory(runtime); Directory.CreateDirectory(Path.Combine(remote, "nested"));
            state = Path.Combine(workspace, "generated.bin");
            File.WriteAllBytes(state, new byte[] { 1, 7, 3, 9 });
            File.WriteAllText(Path.Combine(runtime, "settings.json"), "{\"local\":true}");
            File.WriteAllText(Path.Combine(runtime, "catalog.json"), "{\"catalog\":1}");
            File.WriteAllBytes(Path.Combine(remote, "nested", "content.bundle"), new byte[] { 5, 8, 13 });
            File.WriteAllText(Path.Combine(remote, "catalog.hash"), "fixture-catalog-hash");
        }

        [TearDown]
        public void TearDown()
        {
            if (workspace == null) return;
            var full = Path.GetFullPath(workspace);
            var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(full.StartsWith(temp, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(full).StartsWith("lastrail-baseline-test-", StringComparison.Ordinal), Is.True);
            if (Directory.Exists(full)) Directory.Delete(full, true);
            workspace = null;
        }

        private string Archive(string id = "full_A", string baseline = null, string identity = Identity) =>
            Invoke(archive, root, id, state, runtime, remote, identity, baseline);
        private string Validate(string id = "full_A", string identity = Identity) => Invoke(validate, root, id, identity);
        private static string Invoke(MethodInfo method, params string[] args)
        {
            try { return (string)method.Invoke(null, args.Cast<object>().ToArray()); }
            catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; }
        }
        private static string Hash(string file)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file))).Replace("-", "").ToLowerInvariant(); }
        private static string[] Snapshot(string directory) => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal).Select(p => p.Substring(directory.Length) + "=" + Hash(p)).ToArray();

        [Test]
        public void FullArchiveCopiesEveryFileAndManifestRecordsTheirHashesAndLengths()
        {
            var folder = Archive();
            Assert.That(Path.GetFullPath(folder), Is.EqualTo(Path.GetFullPath(Path.Combine(root, "full_A"))));
            Assert.That(Path.GetFullPath(Validate()), Is.EqualTo(Path.Combine(Path.GetFullPath(folder), "state.bin")));
            var manifest = File.ReadAllText(Path.Combine(folder, "manifest.json"));
            Assert.That(manifest, Does.Contain(Identity));
            foreach (var relative in new[] { "state.bin", "runtime/settings.json", "runtime/catalog.json", "remote/nested/content.bundle", "remote/catalog.hash" })
            {
                var file = Path.Combine(folder, relative);
                Assert.That(File.Exists(file), Is.True, relative);
                Assert.That(manifest.ToLowerInvariant(), Does.Contain(Hash(file)), relative);
                Assert.That(manifest, Does.Contain(new FileInfo(file).Length.ToString(System.Globalization.CultureInfo.InvariantCulture)), relative);
            }
            Assert.That(File.ReadAllBytes(Path.Combine(folder, "state.bin")), Is.EqualTo(File.ReadAllBytes(state)));
            Assert.That(Directory.GetDirectories(root).Select(Path.GetFileName).ToArray(), Is.EqualTo(new[] { "full_A" }));
        }

        [Test]
        public void SourceChangesAndLaterFullBuildCannotRewriteExplicitEarlierBaseline()
        {
            var first = Archive(); var before = Snapshot(first);
            File.WriteAllText(state, "new full state");
            File.WriteAllText(Path.Combine(runtime, "settings.json"), "changed");
            Archive("full_B");
            Assert.That(Snapshot(first), Is.EqualTo(before));
            Assert.That(Validate(), Is.EqualTo(Path.Combine(first, "state.bin")));
        }

        [Test]
        public void DuplicateIdFailsWithoutChangingOriginalArchive()
        {
            var first = Archive(); var before = Snapshot(first);
            File.WriteAllText(state, "replacement");
            Assert.Catch(() => Archive());
            Assert.That(Snapshot(first), Is.EqualTo(before));
        }

        [Test]
        public void UpdateArchivePreservesFullButCannotBecomeSelectedBaseline()
        {
            var first = Archive(); var before = Snapshot(first);
            var update = Archive("update_A1", "full_A");
            Assert.That(File.ReadAllText(Path.Combine(update, "manifest.json")), Does.Contain("full_A"));
            Assert.Catch(() => Validate("update_A1"));
            Assert.That(Snapshot(first), Is.EqualTo(before));
            Assert.That(Validate(), Is.EqualTo(Path.Combine(first, "state.bin")));
        }

        [TestCase("state.bin")]
        [TestCase("runtime/catalog.json")]
        [TestCase("remote/nested/content.bundle")]
        public void SameLengthTamperingFailsValidation(string relative)
        {
            var folder = Archive(); var file = Path.Combine(folder, relative);
            var bytes = File.ReadAllBytes(file); bytes[0] ^= 1; File.WriteAllBytes(file, bytes);
            Assert.Catch(() => Validate());
        }

        [TestCase("state.bin")]
        [TestCase("runtime/settings.json")]
        [TestCase("remote/catalog.hash")]
        [TestCase("manifest.json")]
        public void MissingArchiveFileFailsValidation(string relative)
        {
            var folder = Archive(); File.Delete(Path.Combine(folder, relative));
            Assert.Catch(() => Validate());
        }

        [Test]
        public void IdentityMismatchAndMissingExplicitSelectionNeverFallBackToNewest()
        {
            Archive(); Archive("full_B");
            Assert.Catch(() => Validate(identity: Identity + "-changed"));
            Assert.Catch(() => Validate("absent"));
            Assert.Catch(() => Validate(null));
            Assert.Catch(() => Validate(""));
        }

        [TestCase("../escape")]
        [TestCase("..\\escape")]
        [TestCase("nested/id")]
        [TestCase("C:\\outside")]
        [TestCase("name:stream")]
        [TestCase("trailing.")]
        public void UnsafeIdRejectedForArchiveAndValidation(string id)
        {
            Assert.Catch(() => Archive(id));
            Assert.Catch(() => Validate(id));
            Assert.That(Directory.Exists(Path.Combine(workspace, "escape")), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        public void MissingIdentityCannotPublish(string identity)
        {
            Assert.Catch(() => Archive(identity: identity));
            Assert.That(Directory.Exists(Path.Combine(root, "full_A")), Is.False);
        }

        [Test]
        public void MissingSourceFailsWithoutPublishingPartialArchiveAndCanRetry()
        {
            File.Delete(Path.Combine(remote, "catalog.hash"));
            File.Delete(state);
            Assert.Catch(() => Archive());
            Assert.That(Directory.Exists(Path.Combine(root, "full_A")), Is.False);
            File.WriteAllBytes(state, new byte[] { 4, 2 });
            Archive(); Assert.That(File.Exists(Validate()), Is.True);
        }

        [Test]
        public void UncAndTraversalSourcePathsRejectedBeforeAnyArchivePublication()
        {
            Assert.Catch(() => Invoke(archive, @"\\invalid-host\share", "full_A", state, runtime, remote, Identity, null));
            Assert.Catch(() => Invoke(archive, root, "full_A", @"\\invalid-host\share\state.bin", runtime, remote, Identity, null));
            var traversal = Path.Combine(runtime, "..", Path.GetFileName(state));
            Assert.That(File.Exists(traversal), Is.True);
            Assert.Catch(() => Invoke(archive, root, "full_A", traversal, runtime, remote, Identity, null));
            Assert.That(Directory.Exists(Path.Combine(root, "full_A")), Is.False);
        }
    }
}
#endif
