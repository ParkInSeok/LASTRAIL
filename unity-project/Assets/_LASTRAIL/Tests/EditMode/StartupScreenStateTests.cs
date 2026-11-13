using System;
using LastRail.Presentation.AuthGate;
using LastRail.Presentation.Startup;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class StartupScreenStateTests
    {
        [Test]
        public void AuthModel_UsesExistingLabelsWithoutCredentials()
        {
            var auth = AuthGateUxState.FirstLaunch();
            var state = StartupScreenViewState.FromAuth(auth);
            Assert.That(state.Title, Is.EqualTo(auth.Title));
            Assert.That(state.PrimaryLabel, Is.EqualTo(auth.PrimaryActionLabel));
            Assert.That(state.SecondaryLabel, Is.EqualTo(auth.SecondaryActionLabel));
            Assert.That(state.Progress, Is.Null);
        }

        [Test]
        public void Download_RequiresReportedBytesAndDoesNotInferCompletion()
        {
            Assert.That(StartupScreenViewState.Downloading(20, null).Progress, Is.Null);
            Assert.That(StartupScreenViewState.Downloading(20, 80).Progress, Is.EqualTo(0.25f));
            var allBytes = StartupScreenViewState.Downloading(80, 80);
            Assert.That(allBytes.Kind, Is.EqualTo(StartupScreenKind.Downloading));
            Assert.That(allBytes.PrimaryLabel, Is.Null);
        }

        [TestCase(-1, 100)]
        [TestCase(101, 100)]
        [TestCase(0, 0)]
        public void InvalidProgress_IsRejected(long received, long total)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StartupScreenViewState.Downloading(received, total));
        }
    }
}
