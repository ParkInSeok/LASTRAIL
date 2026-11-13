using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

// Actual prepared input and measured geometry; injected resolved damage below tests lifecycle only, not attack execution.
public sealed class OrdinaryBoardingIntegrationTests {
 const string InputPath="D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/kyle-right-01/boarding-session-preparation-v01/prepared-input.json";
 const string InputHash="fe35c1e71a437f963804ffd7b26199a8c47580f82de47d11303d9ca08ceaf611";
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object owner,string name){var property=owner.GetType().GetProperty(name,Hidden);return property!=null?property.GetValue(owner):Field(owner,name);}
 static object Field(object owner,string name)=>owner.GetType().GetField(name,Hidden).GetValue(owner);
 static object Call(object owner,string name,params object[] args){
  try{return owner.GetType().GetMethods(Hidden).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);}
  catch(TargetInvocationException e){throw e.InnerException;}
 }
 static object Construct(Type type,params object[] args){
  try{return type.GetConstructors(Hidden).Single(c=>c.GetParameters().Length==args.Length&&c.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(x=>x)).Invoke(args);}
  catch(TargetInvocationException e){throw e.InnerException;}
 }
 static OrdinaryLocalBoardingSource Source(OrdinaryFiveCombatFactory factory)=>(OrdinaryLocalBoardingSource)Get(factory,"BoardingSource");
 // Match existing integration tests without widening the production owner's visibility.
 sealed class Boarding {
  readonly object owner;
  internal Boarding(object owner){this.owner=owner;}
  internal long Tick=>(long)Get(owner,"Tick");
  internal bool Complete=>(bool)Get(owner,"Complete");
  internal int Hp(string id)=>(int)Call(owner,"Hp",id);
  internal long ProjectileTargetGeneration(string id)=>(long)Call(owner,"ProjectileTargetGeneration",id);
  internal bool Registered(string id)=>(bool)Call(owner,"Registered",id);
  internal bool Targetable(string id)=>(bool)Call(owner,"Targetable",id);
  internal int Slot(string id)=>(int)Call(owner,"Slot",id);
  internal void Start(long at)=>Call(owner,"Start",at);
  internal void BeginStep(long at)=>Call(owner,"BeginStep",at);
  internal void EndStep(long at,bool flying)=>Call(owner,"EndStep",at,flying);
  internal void Retire()=>Call(owner,"Retire");
  internal JObject Capture()=>(JObject)Call(owner,"Capture");
  internal JObject Target(string id,long at,long front)=>(JObject)Call(owner,"Target",id,at,front);
  internal Boarding CloneOwned(BCombatLocalModel health)=>new Boarding(Call(owner,"CloneOwned",health));
 }
 static JObject Input(){
  byte[] bytes=File.ReadAllBytes(InputPath);using var sha=SHA256.Create();
  Assert.AreEqual(InputHash,BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(),"Use the immutable prepared source, not an older 8/7 wave fixture");
  return JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
 }
 static OrdinaryFiveCombatFactory Factory()=>OrdinaryLiveExecutionTests.CreateFactory(Input());
 static (OrdinaryFiveCombatFactory factory,BCombatLocalModel health,Boarding execution) Create(){
  var factory=Factory();var health=(BCombatLocalModel)Construct(typeof(BCombatLocalModel),Get(factory,"NodeInput"),1000,Source(factory));
  var type=typeof(BCombatLocalModel).Assembly.GetType("LastRail.Battle.OrdinaryBoardingExecution",true);
  return(factory,health,new Boarding(Construct(type,factory,health)));
 }
 static JArray Lives(Boarding execution)=>(JArray)execution.Capture()["boarding"]["lives"];
 static JObject Life(Boarding execution,string id)=>Lives(execution).OfType<JObject>().Single(x=>(string)x["id"]==id);
 static JArray Events(Boarding execution)=>(JArray)execution.Capture()["boarding"]["events"];

 [Test] public void PublicReplayAcceptsActualPreparedInputWithoutCommands(){
  var input=Input();Assert.AreEqual(0,((JArray)input["inputs"]).Count);
  var result=OrdinaryLocalExecutionReplay.Execute(input);
  Assert.AreEqual(0,(long)result["tick"]);Assert.AreEqual(0,(long)result["lastSequence"]);
  Assert.AreEqual((string)input["runId"],(string)result["runId"]);
  Assert.AreEqual((long)input["currentAttempt"]["generation"],(long)result["attemptGeneration"]);
 }

 [Test] public void NaturalPreparedModelLaunchesKyleProjectileAtActualBoardingTarget(){
  var factory=Factory();var model=factory.Create();model.SetRailThrottle(7000);
  long step=(long)Get(factory,"Step");JObject launch=null;
  for(int count=0;count<2000&&launch==null;count++){
   model.Advance(step,1);var basics=Field(model,"ordinaryBasics");if(basics==null)continue;
   launch=((JArray)((JObject)Call(basics,"Capture"))["flights"]).OfType<JObject>().FirstOrDefault(f=>(string)f["actorId"]=="op_kyle");
  }
  Assert.NotNull(launch,"No actual Kyle launch within the bounded original-clock progression");
  string target=(string)launch["targetId"];
  CollectionAssert.Contains(Source(factory).Boarding.OriginalSource.Waves.SelectMany(w=>w.Instances).Select(i=>i.InstanceId).ToArray(),target);
  var boardingOwner=Field(model,"ordinaryBoarding");var boarding=new Boarding(boardingOwner);var health=(BCombatLocalModel)Field(model,"ordinaryHealth");
  Assert.AreSame(health,Field(boardingOwner,"health"));Assert.IsTrue(boarding.Registered(target));Assert.Greater(health.Hp(target),0);
  Assert.AreEqual(health.Hp(target),boarding.Hp(target));var observation=launch["observation"];
  var observedTarget=observation["targets"].Single(t=>(string)t["id"]==target);
  Assert.AreEqual(boarding.ProjectileTargetGeneration(target),(long)observedTarget["generation"]);
  Assert.GreaterOrEqual((long)observation["source"]["generation"],1);
  Assert.AreEqual(boarding.Slot(target)+1,(int)observedTarget["slot"]);
 }

 [Test] public void PreparedSourceBindsOriginalPhysicalIdsAndOneHealthOwnerForAllThirteenEnemies(){
  var f=Create();var source=Source(f.factory);
  CollectionAssert.AreEqual(new[]{7,6},source.Boarding.OriginalSource.Waves.Select(w=>w.Instances.Count));
  CollectionAssert.AreEqual(f.factory.Original.Rail.PhysicalInstanceIds,source.PhysicalHostIds);
  Assert.AreEqual(Get(Get(Get(f.factory,"NodeInput"),"Source"),"BoardingSourceHash"),source.SourceHash);
  var instances=source.Boarding.OriginalSource.Waves.SelectMany(w=>w.Instances).ToArray();
  Assert.AreEqual(13,instances.Select(i=>i.InstanceId).Distinct().Count());
  foreach(var i in instances){
   Assert.AreEqual(checked((int)i.Hp),f.health.Hp(i.InstanceId));Assert.AreEqual(f.health.Hp(i.InstanceId),f.execution.Hp(i.InstanceId));
   Assert.IsFalse(f.execution.Registered(i.InstanceId));Assert.AreEqual(0,f.execution.ProjectileTargetGeneration(i.InstanceId));
  }
  var id=instances[0].InstanceId;int hp=f.health.Hp(id);
  var receipt=f.health.ApplyResolvedDamage("boarding-health-owner-test",id,1,0);
  Assert.AreEqual(hp-1,f.execution.Hp(id));Assert.AreEqual(Get(Call(f.health,"BorrowVitals",id),"Hp"),f.execution.Hp(id));
  Assert.AreSame(receipt,f.health.ApplyResolvedDamage("boarding-health-owner-test",id,1,0));
  Assert.AreEqual(hp-1,f.execution.Hp(id),"Duplicate receipt must not subtract HP twice");
 }

 [Test] public void ActualPathsReachRoofWithoutResettingHpOrGenerationAndUsePhysicalHostSlot(){
  var f=Create();string id=Source(f.factory).Boarding.OriginalSource.Waves[0].Instances[0].InstanceId;
  int originalHp=f.health.Hp(id);f.execution.Start(0);Assert.IsTrue(f.execution.Registered(id));Assert.IsFalse(f.execution.Targetable(id));
  Assert.AreEqual("Entrance",(string)Life(f.execution,id)["phase"]);var phases=new HashSet<string>{"Entrance"};
  for(long tick=1;tick<=3000;tick++){
   f.execution.BeginStep(tick);f.execution.EndStep(tick,false);var life=Life(f.execution,id);phases.Add((string)life["phase"]);
   Assert.AreEqual(originalHp,f.execution.Hp(id));Assert.AreEqual(1,f.execution.ProjectileTargetGeneration(id));
   if((string)life["phase"]=="Rooftop")break;
  }
  CollectionAssert.IsSubsetOf(new[]{"Entrance","Approach","Attached","Climbing","Rooftop"},phases);
  Assert.AreEqual("Rooftop",(string)Life(f.execution,id)["phase"]);Assert.IsTrue(f.execution.Targetable(id));
  Assert.AreEqual(1,f.execution.Slot(id),"First assigned actual host is Combat, physical order 1");
  long front=f.factory.Original.Rail.InitialFrontArcMm.Value;
  var target=f.execution.Target(id,f.execution.Tick,front);var moved=f.execution.Target(id,f.execution.Tick,front+1234);
  Assert.AreEqual(f.execution.Slot(id)+1,(int)target["slot"]);Assert.AreEqual(3213,(long)target["feetMm"][1]);
  Assert.AreEqual(1234,(long)moved["feetMm"][0]-(long)target["feetMm"][0]);
  Assert.AreEqual((long)target["feetMm"][2],(long)moved["feetMm"][2]);
  Assert.AreEqual(1,Events(f.execution).Count(e=>(string)e["instanceId"]==id&&(string)e["kind"]=="REGISTER"));
  Assert.AreEqual(1,Events(f.execution).Count(e=>(string)e["instanceId"]==id&&(string)e["kind"]=="SOCKET_RETURN_ROOF"));
  Assert.IsFalse(f.execution.Complete,"Unkilled planned/active enemies prevent completion");
 }

 [Test] public void CloneDeathAndRetirementDoNotMutateOriginalOwnerOrDuplicateSocketReturns(){
  var f=Create();string id=Source(f.factory).Boarding.OriginalSource.Waves[0].Instances[0].InstanceId;f.execution.Start(0);
  for(long tick=1;tick<=3000&&(string)Life(f.execution,id)["phase"]!="Climbing";tick++){f.execution.BeginStep(tick);f.execution.EndStep(tick,false);}
  Assert.AreEqual("Climbing",(string)Life(f.execution,id)["phase"]);
  string original=f.execution.Capture().ToString();var health=(BCombatLocalModel)Call(f.health,"CloneOwnedNode");var copy=f.execution.CloneOwned(health);
  Assert.IsTrue(JToken.DeepEquals(f.execution.Capture(),copy.Capture()));
  health.ApplyResolvedDamage("boarding-clone-death-test",id,health.Hp(id),copy.Tick);copy.EndStep(copy.Tick,false);
  Assert.AreEqual(0,copy.Hp(id));Assert.Greater(f.execution.Hp(id),0);Assert.AreEqual(original,f.execution.Capture().ToString());
  Assert.IsFalse(copy.Registered(id));Assert.IsFalse(copy.Targetable(id));Assert.AreEqual(2,copy.ProjectileTargetGeneration(id));
  Assert.AreEqual("Dead",(string)Life(copy,id)["phase"]);copy.EndStep(copy.Tick,false);
  Assert.AreEqual(1,Events(copy).Count(e=>(string)e["instanceId"]==id&&(string)e["kind"]=="SOCKET_RETURN_DEATH"));
  Assert.AreEqual(1,Events(copy).Count(e=>(string)e["instanceId"]==id&&(string)e["kind"]=="UNREGISTER_DEATH"));
  copy.Retire();string retired=copy.Capture().ToString();copy.Retire();Assert.AreEqual(retired,copy.Capture().ToString());
  Assert.Throws<InvalidOperationException>(()=>copy.BeginStep(copy.Tick+1));
  Assert.Throws<InvalidOperationException>(()=>copy.Target(id,copy.Tick,f.factory.Original.Rail.InitialFrontArcMm.Value));
  Assert.AreEqual(original,f.execution.Capture().ToString(),"Retiring internal copy must leave original active");
 }

 [Test] public void OriginalSevenAndSixWaveScheduleRegistersEachIdOnceAndCompletesOnlyAfterDeaths(){
  var f=Create();f.execution.Start(0);Assert.IsFalse(f.execution.Complete);
  for(long tick=0;tick<=10000&&!f.execution.Complete;tick++){
   f.execution.BeginStep(tick);
   foreach(var life in Lives(f.execution).Where(x=>(bool)x["registered"])){
    string id=(string)life["id"];f.health.ApplyResolvedDamage("boarding-wave-death-"+(int)life["ordinal"],id,f.health.Hp(id),tick);
   }
   f.execution.EndStep(tick,false);
  }
  Assert.IsTrue(f.execution.Complete);var events=Events(f.execution);
  Assert.AreEqual(13,events.Count(e=>(string)e["kind"]=="REGISTER"));
  Assert.AreEqual(13,events.Count(e=>(string)e["kind"]=="UNREGISTER_DEATH"));
  Assert.AreEqual(13,events.Where(e=>(string)e["kind"]=="REGISTER").Select(e=>(string)e["instanceId"]).Distinct().Count());
  Assert.AreEqual(2,((JArray)f.execution.Capture()["entry"]["events"]).Count(e=>(string)e["kind"]=="WAVE_CLEAR"));
  Assert.IsTrue(Lives(f.execution).All(x=>(string)x["phase"]=="Dead"&&(long)x["generation"]==2));
 }
}
