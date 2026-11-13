using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryLiveGunnerMotionTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static object Call(object owner,string method,params object[] args){try{return owner.GetType().GetMethod(method,Hidden).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 sealed class Harness {
  internal object Owner;internal string[] Ids;internal long StepTicks;internal string Phase="IDLE";internal long RecoveryEnd;internal bool TargetAlive=true;internal long TargetX=42000;internal JArray Obstacles=new JArray();
  internal Harness(){var input=OrdinaryLiveExecutionTests.CreateInput();var factory=OrdinaryLiveExecutionTests.CreateFactory(input);var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryLiveGunnerMotion");Owner=Activator.CreateInstance(type,Hidden,null,new object[]{factory,(string)input["geometryDefinition"]},null);var captured=(JObject)Call(Owner,"Capture");Ids=((JArray)captured["actors"]).Select(a=>(string)a["id"]).ToArray();StepTicks=(long)factory.GetType().GetProperty("Step",Hidden).GetValue(factory);}
  internal long X(int i,long tick)=>((long[])Call(Owner,"Offset",Ids[i],tick))[0];
  internal void Step(long tick){
   Func<string,JObject> state=id=>new JObject{["phase"]=Phase,["recoveryEnd"]=RecoveryEnd};
   Func<string,long,JObject> observe=(id,t)=>{int i=Array.IndexOf(Ids,id);return new JObject{["tick"]=t,["source"]=new JObject{["id"]=id,["muzzleMm"]=new JArray((i==0?63500:67500)+X(i,t),4613,0)},["targets"]=new JArray(new JObject{["id"]="target",["pointMm"]=new JArray(TargetX,4100,0),["lineOfSight"]=true})};};
   Func<string,bool> alive=id=>id!="target"||TargetAlive;Func<long,JArray> blockers=t=>Obstacles;
   Call(Owner,"Step",tick,state,observe,alive,blockers);
  }
 }
 [Test] public void MovesOnMeasuredRoofAtOriginalSpeedAndCloneIsIndependent(){
  var h=new Harness();h.Step(50);Assert.AreEqual(0,h.X(1,50));Assert.AreEqual("MOVING",Call(h.Owner,"MotionStatus",h.Ids[1],50));
  h.Step(51);Assert.AreEqual(-(1500*h.StepTicks/10000000),h.X(1,51));Assert.AreEqual(0,h.X(0,51));
  var copy=Call(h.Owner,"CloneOwned");string saved=((JObject)Call(copy,"Capture")).ToString();h.Step(52);Assert.AreEqual(saved,((JObject)Call(copy,"Capture")).ToString());
  string current=((JObject)Call(h.Owner,"Capture")).ToString();h.Step(52);Assert.AreEqual(current,((JObject)Call(h.Owner,"Capture")).ToString());
 }
 [Test] public void PreparationAndRecoveryHoldUntilExactRecoveryBoundary(){
  var h=new Harness();h.Step(20);h.Phase="WINDUP";h.Step(21);Assert.AreEqual(0,h.X(1,21));h.Phase="RECOVERY";h.RecoveryEnd=23;h.Step(22);Assert.AreEqual(0,h.X(1,22));h.Step(23);Assert.Less(h.X(1,23),0);
 }
 [Test] public void NoLivingTargetProducesOwnedSafeStopAndCanResume(){
  var h=new Harness{TargetAlive=false};h.Step(0);Assert.AreEqual("ORIGINAL_SAFE_STOP_OWNER",Call(h.Owner,"NoTargetStatus",h.Ids[1],0));h.TargetAlive=true;h.Step(1);Assert.Less(h.X(1,1),0);
 }
 [Test] public void OccupiedFrontGunnerPreventsCrossingAndOutsideRoofAttackPoint(){
  var h=new Harness{TargetX=39000};h.Step(0); // Rear gunner cannot pass the front capsule to reach the required muzzle range.
  Assert.AreEqual("ORIGINAL_SAFE_STOP_OWNER",Call(h.Owner,"NoTargetStatus",h.Ids[1],0));Assert.AreEqual(0,h.X(1,0));
 }
 [Test] public void ActualLosBlockerPreventsFabricatedReachablePoint(){
  var h=new Harness();h.Obstacles.Add(new JObject{["centerMm"]=new JArray(55000,4500,0),["sizeMm"]=new JArray(1000,10000,10000)});h.Step(0);Assert.AreEqual("ORIGINAL_SAFE_STOP_OWNER",Call(h.Owner,"NoTargetStatus",h.Ids[1],0));
 }
}
