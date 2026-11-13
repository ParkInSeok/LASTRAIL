using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace LastRail.Tests.EditMode
{
    public sealed class SafeClientErrorQueueTests
    {
        private string directory;
        private string path;
        private const long Now = 1788669600000;
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "lastrail-safe-errors-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); path = Path.Combine(directory, "queue.json");
        }
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test]
        public void EditorHasNoInstalledBuildAndInvalidVersionCannotCarryPrivateText()
        {
            var captured = AppBuildSnapshot.CaptureInstalled();
            Assert.That(captured.AppVersionCode, Is.Null); Assert.That(captured.AppVersion, Is.Null);
            Assert.That(new AppBuildSnapshot("user@example.com", -1).AppVersion, Is.Null);
            Assert.That(new AppBuildSnapshot("1.0\npassword", null).AppVersion, Is.Null);
            Assert.That(new AppBuildSnapshot(new string('a', 65), null).AppVersion, Is.Null);
            Assert.That(new AppBuildSnapshot("1.2.3-rc.1+build", long.MaxValue).AppVersionCode, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void RestartAndUpgradePreserveOriginalVersionAndUnknownCode()
        {
            var first = new SafeClientErrorQueue(path);
            first.Record(SafeClientErrorKind.UnityException, new AppBuildSnapshot("1.0.0", 10), Now);
            first.Record(SafeClientErrorKind.UnityError, new AppBuildSnapshot(null, null), Now + 1);
            Assert.That(first.Save(Now + 2), Is.True);
            var second = new SafeClientErrorQueue(path); Assert.That(second.Load(Now + 3), Is.True);
            second.Record(SafeClientErrorKind.UnityException, new AppBuildSnapshot("2.0.0", 20), Now + 4);
            Assert.That(second.Snapshot()[0].Build.AppVersion, Is.EqualTo("1.0.0"));
            Assert.That(second.Snapshot()[0].Build.AppVersionCode, Is.EqualTo(10));
            Assert.That(second.Snapshot()[1].Build.AppVersionCode, Is.Null);
            Assert.That(second.Snapshot()[2].Build.AppVersionCode, Is.EqualTo(20));
            Assert.That(second.TransportAvailable, Is.False);
        }

        [Test]
        public void FloodIsCappedAndDuplicatesAreAggregated()
        {
            var queue = new SafeClientErrorQueue(path); var build = new AppBuildSnapshot("1", 1);
            Parallel.For(0, 1000, _ => queue.Record(SafeClientErrorKind.UnityError, build, Now));
            Assert.That(queue.Snapshot().Count, Is.EqualTo(1)); Assert.That(queue.Snapshot()[0].Occurrences, Is.EqualTo(60));
            Assert.That(queue.Record((SafeClientErrorKind)999, build, Now), Is.False);
        }

        [Test]
        public void FileSizeCountAndTtlStayBounded()
        {
            var queue = new SafeClientErrorQueue(path);
            for (var i = 0; i < 200; i++) queue.Record(SafeClientErrorKind.UnityError, new AppBuildSnapshot("1", i + 1), Now + i * 60001L);
            Assert.That(queue.Snapshot().Count, Is.EqualTo(SafeClientErrorQueue.MaxEntries));
            Assert.That(queue.Save(Now + 200 * 60001L), Is.True);
            Assert.That(new FileInfo(path).Length, Is.LessThanOrEqualTo(SafeClientErrorQueue.MaxFileBytes));
            Assert.That(queue.Save(Now + 200 * 60001L + SafeClientErrorQueue.RetentionMs + 1), Is.True);
            Assert.That(queue.Snapshot(), Is.Empty);
        }

        [TestCase("not json")][TestCase("{\"schema\":99,\"events\":[]}")]
        public void BrokenFileRecoversWithoutRetainingRawContents(string corrupt)
        {
            File.WriteAllText(path, corrupt);
            var queue = new SafeClientErrorQueue(path); Assert.That(queue.Load(Now), Is.False);
            Assert.That(queue.Snapshot(), Is.Empty); Assert.That(queue.Save(Now), Is.True);
            Assert.That(new SafeClientErrorQueue(path).Load(Now), Is.True);
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [Test]
        public void OversizedFileIsRejectedAndFailedReplacementRetainsCommittedSnapshot()
        {
            File.WriteAllText(path, new string('x', SafeClientErrorQueue.MaxFileBytes + 1));
            var queue = new SafeClientErrorQueue(path); Assert.That(queue.Load(Now), Is.False);
            Assert.That(queue.Save(Now), Is.True);
            var committed = File.ReadAllText(path);
            queue.Record(SafeClientErrorKind.UnityError, new AppBuildSnapshot("1", 1), Now);
            using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)) Assert.That(queue.Save(Now), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(committed));
            Assert.That(queue.Save(Now), Is.True);
        }

        [Test]
        public void ThreadedHookDiscardsOriginalTextAndStack()
        {
            var host = new GameObject("LocalErrorCaptureFixture");
            try
            {
                var capture = host.AddComponent<LocalClientErrorCapture>();
                var queue = new SafeClientErrorQueue(path);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(LocalClientErrorCapture).GetField("queue", flags).SetValue(capture, queue);
                typeof(LocalClientErrorCapture).GetField("build", flags).SetValue(capture, new AppBuildSnapshot("1", 7));
                var method = typeof(LocalClientErrorCapture).GetMethod("Capture", flags);
                Task.Run(() => method.Invoke(capture, new object[] { "password=secret user@example.com", "Authorization: Bearer forbidden-stack", LogType.Exception })).GetAwaiter().GetResult();
                Assert.That(queue.Save(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), Is.True);
                var json = File.ReadAllText(path);
                foreach (var denied in new[] { "password", "secret", "example.com", "Bearer", "Authorization", "stack", "accountId" }) Assert.That(json, Does.Not.Contain(denied));
                Assert.That(queue.Snapshot().Single().Kind, Is.EqualTo(SafeClientErrorKind.UnityException));
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
    }
}
