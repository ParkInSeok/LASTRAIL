using System;
using System.Collections.Generic;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class NyxZoneDispatchTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static object Call(object owner,string name,params object[] args){try{return owner.GetType().GetMethod(name,Hidden).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 [TestCase(false,false,100,"REJECTED_COOLDOWN")][TestCase(true,false,100,null)][TestCase(true,true,100,"REJECTED_BUSY")][TestCase(true,false,99,"REJECTED_CHARGE")]
 public void UltimateReplacementIgnoresActiveCooldownButPreservesSharedQueueAndCharge(bool ultimate,bool busy,int charge,string expected){
  var method=typeof(BasicCombatSimulation).GetMethod("NyxCastBlocker",Hidden);Assert.NotNull(method);Assert.AreEqual(expected,method.Invoke(null,new object[]{ultimate,10L,100L,busy,true,charge,ultimate?100:0}));
 }
 static JObject Observation(long tick,string zone="zone-1",bool present=true)=>new JObject{["bindingId"]="test-node-generation-1",["policyHash"]=new string('a',64),["tick"]=tick,["revision"]=tick+1,["present"]=present,["eligible"]=true,["footWorldMm"]=new JArray(0,0,0),["centerWorldMm"]=new JArray(45000,0,0),["lineOfSight"]=true,
  ["zone"]=new JObject{["id"]=zone,["ownerGeneration"]=1,["frameId"]="frame",["frameGeneration"]=1,["carId"]="car",["scope"]="ENEMY_CARRIAGE",["minimumMm"]=new JArray(-1000,0,-1000),["maximumMm"]=new JArray(1000,2000,1000),["representativeCenterMm"]=new JArray(0,1000,0)},
  ["focusTargetId"]=null,["members"]=new JArray(new JObject{["id"]="op_kyle",["generation"]=1,["delayImmune"]=false,["protected"]=false,["mitigationRateImmune"]=false})};
 sealed class Fixture:IDisposable {
  internal readonly BCombatLocalModel Health;internal readonly NyxZoneExecution Core;internal readonly object Dispatch;
  internal bool Confirm=true,Present=true;internal string Zone="zone-1";internal Action<JObject> Alter;internal int Selects,Creates,RevisionOffset;
  internal Fixture(){var tests=typeof(LastRail.Tests.EditMode.NyxZoneExecutionTests);Health=(BCombatLocalModel)tests.GetMethod("Health",Hidden).Invoke(null,null);Core=(NyxZoneExecution)tests.GetMethod("Owner",Hidden).Invoke(null,new object[]{Health});
   var sourceType=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.NyxZoneSources");Assert.NotNull(sourceType);
   JObject Read(long tick){var o=Observation(tick,Zone,Present);o["revision"]=tick+1+RevisionOffset;Alter?.Invoke(o);return o;}
   var sources=Activator.CreateInstance(sourceType,Hidden,null,new object[]{"test-node-generation-1",new string('a',64),(Func<string,bool,long,JObject>)((id,ult,t)=>{Selects++;return Read(t);}),(Func<string,JObject,long,JObject>)((id,zone,t)=>{Creates++;return Confirm?Read(t):null;}),(Func<JObject,long,JObject>)((zone,t)=>Read(t))},null);
   var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.NyxZoneDispatch");Assert.NotNull(type);Dispatch=Activator.CreateInstance(type,Hidden,null,new[]{(object)Core,sources},null);
  }
  public void Dispose()=>Core.Dispose();
 }
 [Test] public void OnlyConfirmedCreationReplacesAndSameTickExitDoesNotRepeatDamage(){
  using var f=new Fixture();Assert.AreEqual(true,Call(f.Dispatch,"Prepare","a",false,0L));Assert.AreEqual(true,Call(f.Dispatch,"Commit","a",0L));Call(f.Dispatch,"Step",1L,true);Assert.AreEqual(9940,f.Health.Hp("op_kyle"));
  f.Zone="zone-2";f.Confirm=false;Assert.AreEqual(true,Call(f.Dispatch,"Prepare","b",true,1L));Assert.AreEqual(false,Call(f.Dispatch,"Commit","b",1L));f.Zone="zone-1";Assert.IsTrue(f.Core.Active);
  Call(f.Dispatch,"Step",1L,true);Assert.AreEqual(9940,f.Health.Hp("op_kyle"));f.Present=false;f.RevisionOffset++;Call(f.Dispatch,"Step",1L,true);Assert.IsFalse(f.Core.Active);Assert.AreEqual(9940,f.Health.Hp("op_kyle"));
 }
 [TestCase("scope")][TestCase("generation")][TestCase("immunity")][TestCase("tick")][TestCase("policy")]
 public void InvalidOrIncompleteObservationCannotCreateZone(string fault){using var f=new Fixture();f.Alter=o=>{if(fault=="scope")o["zone"]["scope"]="ALL_CARS";if(fault=="generation")o["zone"]["ownerGeneration"]=0;if(fault=="immunity")((JObject)o["members"][0]).Remove("delayImmune");if(fault=="tick")o["tick"]=99;if(fault=="policy")o["policyHash"]=new string('b',64);};Assert.Throws<ArgumentException>(()=>Call(f.Dispatch,"Prepare","a",false,0L));Assert.IsFalse(f.Core.Active);Assert.AreEqual(10000,f.Health.Hp("op_kyle"));}
 [Test] public void SelectionFailureAndNodeEndCannotManufactureCreation(){using var f=new Fixture();f.Alter=o=>o["lineOfSight"]=false;Assert.AreEqual(false,Call(f.Dispatch,"Prepare","a",false,0L));Assert.AreEqual(0,f.Creates);f.Alter=null;Call(f.Dispatch,"Prepare","b",false,0L);Call(f.Dispatch,"Commit","b",0L);Call(f.Dispatch,"Step",1L,false);Assert.IsFalse(f.Core.Active);Assert.AreEqual(10000,f.Health.Hp("op_kyle"));}
 [TestCase(48000,0,0,true)][TestCase(48001,0,0,false)][TestCase(0,48000,0,true)][TestCase(0,0,48001,false)][TestCase(28800,38400,0,true)][TestCase(28800,38400,1,false)]
 public void SelectionUsesFootToCenterThreeDimensional48m(int x,int y,int z,bool accepted){using var f=new Fixture();f.Alter=o=>o["centerWorldMm"]=new JArray(x,y,z);Assert.AreEqual(accepted,Call(f.Dispatch,"Prepare","a",false,0L));}
 [Test] public void AcceptedZoneDoesNotRetargetOrRecheckDistanceAtCreation(){using var f=new Fixture();Assert.AreEqual(true,Call(f.Dispatch,"Prepare","a",false,0L));f.Alter=o=>{o["centerWorldMm"]=new JArray(60000,0,0);o["lineOfSight"]=false;};Assert.AreEqual(true,Call(f.Dispatch,"Commit","a",1L));Assert.AreEqual(1,f.Selects);Assert.IsTrue(f.Core.Active);}
 [Test] public void IneligibleZoneIsNotAccepted(){using var f=new Fixture();f.Alter=o=>o["eligible"]=false;Assert.AreEqual(false,Call(f.Dispatch,"Prepare","a",false,0L));Assert.AreEqual(0,f.Creates);}
 [Test] public void ChangedGenerationCancelsReplacementAndPreservesExistingZone(){using var f=new Fixture();Call(f.Dispatch,"Prepare","a",false,0L);Call(f.Dispatch,"Commit","a",0L);f.Zone="zone-2";Call(f.Dispatch,"Prepare","b",true,1L);f.Alter=o=>o["zone"]["ownerGeneration"]=2;Assert.AreEqual(false,Call(f.Dispatch,"Commit","b",1L));Assert.IsTrue(f.Core.Active);Assert.AreEqual("a",f.Dispatch.GetType().GetProperty("ActiveCommandId",Hidden).GetValue(f.Dispatch));}
}
