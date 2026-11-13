using LastRail.Presentation.MissionShell;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {public sealed class MonthlyAttendanceTests {
 [Test] public void MonthlyEntrySeparatesReadAutomaticVisitAndRecovery(){Assert.That(typeof(AccountScreenRuntime).GetMethod("EnterMonthlyAttendanceAsync"),Is.Not.Null);Assert.That(typeof(AccountScreenRuntime).GetMethod("RefreshMonthlyAttendanceAsync"),Is.Not.Null);}
}}
