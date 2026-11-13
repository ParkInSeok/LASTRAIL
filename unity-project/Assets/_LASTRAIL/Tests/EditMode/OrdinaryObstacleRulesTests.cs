using System;
using LastRail.Battle;
using NUnit.Framework;

public sealed class OrdinaryObstacleRulesTests {
 [Test] public void DamageRuleIsAvailableOutsideTheDiagnosticOwner(){
  var type=typeof(OrdinaryRailKinematics).Assembly.GetType("LastRail.Battle.OrdinaryObstacleRules");
  Assert.NotNull(type,"Obstacle adjudication must be available without creating trial HP or a trial clock.");
  var damage=type.GetMethod("RawDamage");
  var partial=(CombatExactValue)damage.Invoke(null,new object[]{false,50,100,20000L});
  Assert.AreEqual(300L,(long)partial.Floor());
  var falling=(CombatExactValue)damage.Invoke(null,new object[]{true,25,100,26000L});
  Assert.AreEqual(225L,(long)falling.Floor());
 }
 [Test] public void CollisionPreservesLowSpeedAndDestroyedObstacleDoesNotSlow(){
  Assert.AreEqual(10000,OrdinaryObstacleRules.CollisionSpeed(10000,100,100));
  Assert.AreEqual(14000,OrdinaryObstacleRules.CollisionSpeed(26000,100,100));
  Assert.AreEqual(26000,OrdinaryObstacleRules.CollisionSpeed(26000,0,100));
  Assert.AreEqual(0L,(long)OrdinaryObstacleRules.RawDamage(false,0,100,26000).Floor());
  Assert.Throws<ArgumentOutOfRangeException>(()=>OrdinaryObstacleRules.RawDamage(false,101,100,20000));
  Assert.Throws<ArgumentOutOfRangeException>(()=>OrdinaryObstacleRules.CollisionSpeed(double.NaN,50,100));
 }
 [Test] public void AimRetainsPhysicalFlightAndFallingHeight(){
  Func<double,double> height=t=>OrdinaryObstacleRules.FallingHeight(16000+t*1000,12200,3000,15400,10000);
  var aim=OrdinaryObstacleRules.Aim(50000,4000,height,300000,.4);
  Assert.Greater(aim.FlightSeconds,0);Assert.Less(aim.FlightSeconds,.4);
  Assert.AreEqual(300000,Math.Sqrt(aim.X*aim.X+aim.Y*aim.Y),.00001);
  Assert.AreEqual(50000,aim.X*aim.FlightSeconds,.001);
  Assert.AreEqual(height(aim.FlightSeconds),4000+aim.Y*aim.FlightSeconds,.001);
  Assert.AreEqual(6200,height(0));Assert.AreEqual(3000,height(1));
 }
}
