#if UNITY_EDITOR
using LastRail.Run;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class BNativeTwoWaveTests {
  [Test] public void ExistingNativeRoofMoveMustBeReachableFromRouteController(){Assert.NotNull(typeof(BNodeSessionController).GetMethod("MoveOrdinaryOperator"));Assert.NotNull(typeof(BRouteSessionController).GetMethod("MoveOrdinaryOperator"));}
 }
}
#endif
