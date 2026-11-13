using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryNodeObstacleExecutionTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static object Call(object owner,string name,params object[] args){try{return owner.GetType().GetMethod(name,Hidden).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static (object Owner,OrdinaryFiveCombatFactory Factory) Fixture(){
  var f=OrdinaryObstacleSequenceTests.Fixture();var arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
  var factory=OrdinaryFiveCombatFactory.FromNodeStart(f.Start,arrival.NodeStart,arrival.Scaling,170000);var model=factory.Create();
  var health=typeof(BasicCombatSimulation).GetField("ordinaryHealth",Hidden).GetValue(model);
  var geometry=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/Content/Battle/OrdinaryLiveGeometryV01/definition.json"));
  Func<long,JObject> defense=t=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=new JObject{["numerator"]="111",["denominator"]="1"},["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
  var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryNodeObstacleExecution");
  return(Activator.CreateInstance(type,Hidden,null,new object[]{factory,health,geometry,defense},null),factory);
 }
 [TestCase(14000,"DESTROYED")][TestCase(26000,"PASSED")]
 public void ActualSweepsProduceResolutionOnlyAfterWholeTrainAndHazardsClear(long target,string expected){
  var f=Fixture();long front=58000,speed=20000,remainder=0,at=0,tick=0;JObject result=null;
  while(at<35000&&result==null){tick++;var motion=((long Front,long Speed,long Remainder))Call(f.Owner,"Advance",at,17L,tick,front,speed,target,remainder);front=motion.Front;speed=motion.Speed;remainder=motion.Remainder;at+=17;result=(JObject)Call(f.Owner,"Resolution",at,front);}
  Assert.NotNull(result);Assert.AreEqual(expected,(string)result["outcome"]);Assert.GreaterOrEqual(at,16320);Assert.Greater(front-38000,341000);Assert.AreEqual(800,(int)result["trainHp"]);
  var state=(JObject)Call(f.Owner,"Capture");Assert.Greater(((JArray)state["events"]).Count,0);Assert.AreEqual((int)state["hp"],(int)result["obstacleHp"]);if(expected=="DESTROYED")Assert.AreEqual(0,(int)state["hp"]);else Assert.Greater((int)state["hp"],0);
 }
 [Test] public void CruiseContactWritesOriginalHpReceiptOnce(){
  var f=Fixture();long front=58000,speed=20000,remainder=0,at=0,tick=0;JObject result=null;
  while(at<35000&&result==null){tick++;var m=((long Front,long Speed,long Remainder))Call(f.Owner,"Advance",at,17L,tick,front,speed,20000L,remainder);front=m.Front;speed=m.Speed;remainder=m.Remainder;at+=17;result=(JObject)Call(f.Owner,"Resolution",at,front);}
  Assert.NotNull(result);Assert.AreEqual("COLLISION",(string)result["outcome"]);Assert.Less((int)result["trainHp"],800);Assert.Greater((int)result["trainHp"],0);
  var first=(JObject)Call(f.Owner,"Resolution",at,front);Assert.IsTrue(JToken.DeepEquals(result,first));
  var state=(JObject)Call(f.Owner,"Capture");int count=0;foreach(var e in state["events"])if(((string)e["kind"]).EndsWith("_CONTACT"))count++;Assert.AreEqual(1,count);
 }
}
