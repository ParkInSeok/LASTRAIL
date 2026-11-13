using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class ObstacleNumericTrialTests {
 static ObstacleTrialModel Create(string name,int shots=int.MaxValue,bool landed=false,int shield=0,int hp=5000)=>new ObstacleTrialModel(name,new[]{"engine","combat","gun"},111,hp,shield,shots,landed);
 static JArray Run(ObstacleTrialModel model,int throttle=10000,int partition=50){
  model.Advance(1000);model.SetThrottle(throttle);
  for(int i=0;i<30000/partition&&!model.Finished;i++)model.Advance(partition);
  Assert.IsTrue(model.Finished);return (JArray)model.Capture()["events"];
 }
 [Test] public void SlowDestroysByRealHitsInFlightAndOnGround(){
  var m=Create("low");var e=Run(m,7000);
  Assert.AreEqual(0,m.Hp);Assert.AreEqual(5000,m.TrainHp);
  var hits=e.Where(x=>(string)x["kind"]=="HIT").ToArray();Assert.AreEqual(4,hits.Length);
  Assert.IsTrue(hits.Any(x=>(string)x["phaseAtHit"]=="FALLING"));
  Assert.IsTrue(hits.Any(x=>(string)x["phaseAtHit"]=="LANDED"));
  Assert.IsTrue(hits.All(x=>(double)x["flightMs"]>0&&(double)x["flightMs"]<=400));
  Assert.IsFalse(e.Any(x=>((string)x["kind"]).EndsWith("CONTACT")));
 }
 [Test] public void FastTailClearsBeforeHazardWithoutDestroying(){
  var m=Create("fast");var e=Run(m,13000);
  Assert.AreEqual(25,m.Hp);Assert.AreEqual(5000,m.TrainHp);
  Assert.IsTrue((bool)e.Single(x=>(string)x["kind"]=="TAIL_CLEAR")["beforeHazard"]);
 }
 [Test] public void CruisingRearCarTakesOnlyOneShieldedFallingHit(){
  var m=Create("rear",shield:300);var e=Run(m);
  var hit=e.Single(x=>(string)x["kind"]=="FALLING_CONTACT");
  Assert.AreEqual(2,(int)hit["vehicleOrder"]);Assert.Greater((int)hit["absorbed"],0);
  Assert.AreEqual(5000,m.TrainHp);Assert.Less((long)hit["slowedSpeed"],20000);
  Assert.IsFalse((bool)hit["duplicateApplied"]);
 }
 [Test] public void PartialLandedCollisionUsesSpeedAndArmor(){
  var m=Create("partial",2,true);var e=Run(m);
  var hit=e.Single(x=>(string)x["kind"]=="HORIZONTAL_CONTACT");
  Assert.AreEqual(50,(int)hit["obstacleHp"]);Assert.AreEqual(270,(int)hit["hpLoss"]);Assert.AreEqual(4730,m.TrainHp);
 }
 [Test] public void RenderPartitionsDoNotChangeResults(){
  var a=Create("same");var b=Create("same");Run(a,10000,1);Run(b,10000,200);
  Assert.IsTrue(JToken.DeepEquals(a.Capture(),b.Capture()));
 }
 [Test] public void DefeatAndRetiredAttemptCannotContinue(){
  var m=Create("defeat",0,false,0,100);Run(m);Assert.AreEqual(0,m.TrainHp);
  long before=m.Milliseconds;m.Advance(1000);Assert.AreEqual(before,m.Milliseconds);
  m.Retire();Assert.Throws<ObjectDisposedException>(()=>m.Advance(1));
 }
 [Test] public void SharedMotionPreservesTrapezoidAndRemainder(){
  var v=OrdinaryRailKinematics.Advance(58000,20000,14000,0,50);
  Assert.AreEqual(58993,v.Front);Assert.AreEqual(19750,v.Speed);Assert.AreEqual(1500,v.Remainder);
 }
}
