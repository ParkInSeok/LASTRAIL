using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class ClientErrorCaptureContractTests
    {
        [Test]
        public void ErrorTimeBuildSnapshotAndBoundedQueueExist()
        {
            var assembly = typeof(LastRail.Resources.DownloadCoordinator).Assembly;
            Assert.That(assembly.GetType("LastRail.Diagnostics.AppBuildSnapshot"), Is.Not.Null);
            Assert.That(assembly.GetType("LastRail.Diagnostics.SafeClientErrorQueue"), Is.Not.Null);
        }
    }
}
