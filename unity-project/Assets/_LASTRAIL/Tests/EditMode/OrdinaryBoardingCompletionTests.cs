using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

// Completion-boundary tests arrange owner state; they are not natural-combat clear evidence.
public sealed class OrdinaryBoardingCompletionTests {
 const string InputPath="D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/kyle-right-01/boarding-session-preparation-v01/prepared-input.json";
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Field(object owner,string name)=>owner.GetType().GetField(name,Hidden).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Hidden).SetValue(owner,value);
 static object Invoke(object owner,string name,params object[] args){try{return owner.GetType().GetMethods(Hidden).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static BasicCombatSimulation Create(out OrdinaryFiveCombatFactory factory){
  factory=OrdinaryLiveExecutionTests.CreateFactory(JObject.Parse(File.ReadAllText(InputPath)));
  var model=factory.Create();Invoke(model,"StartOrdinaryLiveWave");return model;
 }
 static void Kill(BasicCombatSimulation model,string id){var health=Field(model,"ordinaryHealth");Invoke(health,"ApplyResolvedDamage","completion-test:"+id,id,int.MaxValue,0L,false);}
 static void CompleteBoarding(BasicCombatSimulation model){
  var boarding=Field(model,"ordinaryBoarding");var entry=Field(boarding,"entry");Set(entry,"finished",true);
  var history=(JArray)Field(entry,"history");history.Add(new JObject{["kind"]="WAVE_CLEAR"});history.Add(new JObject{["kind"]="WAVE_CLEAR"});
 }
 static void KillOriginalGunners(BasicCombatSimulation model,OrdinaryFiveCombatFactory factory){var encounter=(FirstPlayableScaling)factory.GetType().GetProperty("Encounter",Hidden).GetValue(factory);foreach(string id in encounter.IssuedGunnerIds)Kill(model,id);}
 [Test] public void OriginalGunnerDeathsDoNotClearPendingBoarding(){
  var model=Create(out var factory);KillOriginalGunners(model,factory);Invoke(model,"ResolveOrdinaryOutcome");
  Assert.AreEqual(BasicCombatOutcome.Running,model.Outcome);Assert.AreEqual(0,model.CompletedWaves);
 }
 [Test] public void BoardingClearWaitsForOriginalGunnersThenPublishesTwoWaves(){
  var model=Create(out var factory);CompleteBoarding(model);Invoke(model,"ResolveOrdinaryOutcome");
  Assert.AreEqual(BasicCombatOutcome.Running,model.Outcome);Assert.AreEqual(2,model.CompletedWaves);
  KillOriginalGunners(model,factory);Invoke(model,"ResolveOrdinaryOutcome");Assert.AreEqual(BasicCombatOutcome.Victory,model.Outcome);
 }
 [TestCase("allied")][TestCase("gunner")][TestCase("boarding")]
 public void LastEnemyDeathWaitsForEachProjectileOwner(string owner){
  var model=Create(out var factory);CompleteBoarding(model);KillOriginalGunners(model,factory);Action resolve;
  if(owner=="allied"){
   var shots=(Dictionary<string,JObject>)Field(Field(model,"ordinaryProjectiles"),"shots");var shot=new JObject{["status"]="FLYING"};shots.Add("completion-test",shot);resolve=()=>shot["status"]="EXPIRED";
  }else if(owner=="gunner"){
   var shots=(IList)Field(Field(model,"ordinaryGunners"),"shots");var shot=new JObject{["status"]="FLYING"};shots.Add(shot);resolve=()=>shot["status"]="MISS";
  }else{
   var shots=(IList)Field(Field(Field(model,"ordinaryBoarding"),"projectiles"),"shots");var shot=Activator.CreateInstance(shots.GetType().GetGenericArguments()[0],true);Set(shot,"Terminal","FLYING");shots.Add(shot);resolve=()=>Set(shot,"Terminal","MISS");
  }
  Invoke(model,"ResolveOrdinaryOutcome");Assert.AreEqual(BasicCombatOutcome.Running,model.Outcome);
  resolve();Invoke(model,"ResolveOrdinaryOutcome");Assert.AreEqual(BasicCombatOutcome.Victory,model.Outcome);
 }
 [Test] public void TrainDefeatWinsOverBoardingClear(){
  var model=Create(out var factory);CompleteBoarding(model);KillOriginalGunners(model,factory);Kill(model,(string)Field(Field(model,"ordinaryHealth"),"trainId"));
  Invoke(model,"ResolveOrdinaryOutcome");Assert.AreEqual(BasicCombatOutcome.Failure,model.Outcome);
 }
 [Test] public void NoBoardingRetainsExistingOutcome(){
  var model=OrdinaryLiveExecutionTests.CreateFactory(OrdinaryLiveExecutionTests.CreateInput()).Create();
  Invoke(model,"ResolveOrdinaryOutcome");Assert.AreEqual(BasicCombatOutcome.Running,model.Outcome);Assert.AreEqual(0,model.CompletedWaves);
 }
}
