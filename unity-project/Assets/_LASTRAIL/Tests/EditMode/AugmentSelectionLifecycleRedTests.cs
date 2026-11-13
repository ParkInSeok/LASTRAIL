using LastRail.Presentation.MissionShell;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class AugmentSelectionLifecycleRedTests
    {
        [Test] public void SelectionHasOneInjectedCommandOwnerAndExplicitLifetime()
        {
            var type=typeof(AccountScreenRuntime).Assembly.GetType("LastRail.Presentation.AugmentSelection.AugmentSelectionController");
            Assert.That(type,Is.Not.Null,"Selection controller missing; no production host is connected");
            foreach(var name in new[]{"OpenAsync","RefreshAsync","SelectAsync","RerollAsync","RetryAsync","Reset","Dispose"})
                Assert.That(type.GetMethod(name),Is.Not.Null,name);
        }
    }
}
