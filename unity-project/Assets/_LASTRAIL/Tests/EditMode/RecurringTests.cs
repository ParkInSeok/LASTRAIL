using LastRail.Presentation.MissionShell;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {public sealed class RecurringTests {
 [Test] public void RecurringEntryAndReadAreSeparate(){Assert.That(typeof(AccountScreenRuntime).GetMethod("EnterRecurringAsync"),Is.Not.Null);Assert.That(typeof(AccountScreenRuntime).GetMethod("RefreshRecurringAsync"),Is.Not.Null);}
} }
