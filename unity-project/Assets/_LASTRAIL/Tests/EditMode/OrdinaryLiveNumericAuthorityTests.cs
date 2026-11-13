using System;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryLiveNumericAuthorityTests {
 static CombatExactValue Exact(long numerator,long denominator)=>CombatExactValue.From(new JObject{["numerator"]=numerator.ToString(System.Globalization.CultureInfo.InvariantCulture),["denominator"]=denominator.ToString(System.Globalization.CultureInfo.InvariantCulture)});
 static object Call(string name,params object[] args){try{return typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryLiveNumericAuthority").GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static JObject Context()=>new JObject{["accountId"]="account",["runId"]="run",["nodeId"]="node",["startSnapshotHash"]=new string('a',64),["snapshotHash"]=new string('b',64),["attemptGeneration"]=2};
 [Test] public void NyxCurrentSourceRequiresExplicitVersionKindsAndUniqueIdentity(){
  var c=Context();c["entryEffectsHash"]=(string)Call("ValidateEntry",Entry(),c);var view=View(c,3);view["version"]="ordinary.local-current-effects.v2.20261107";
  var row=new JObject{["sourceId"]="zone:mitigation",["sourceKind"]="NYX_ZONE",["ownerId"]="op_nyx",["targetId"]="enemy",["targetGeneration"]=1,["kind"]="MITIGATION_RATE_REDUCTION",["valueBps"]=800,["expiresAtTick"]=10};((JArray)view["activeEffects"]).Add(row);
  Assert.DoesNotThrow(()=>Call("ValidateCurrent",view,c,3L));((JArray)view["activeEffects"]).Add(row.DeepClone());Assert.Throws<ArgumentException>(()=>Call("ValidateCurrent",view,c,3L));((JArray)view["activeEffects"])[1].Remove();
  view["version"]="ordinary.local-current-effects.v1.20261106";Assert.Throws<NotSupportedException>(()=>Call("ValidateCurrent",view,c,3L));
 }
 static JObject Entry()=>new JObject{["version"]="ordinary.local-entry-effects.v1.20261106",["accountId"]="account",["runId"]="run",["nodeId"]="node",["startSnapshotHash"]=new string('a',64),["originGeneration"]=1,["revision"]=1,["atTick"]=0,["activeEffects"]=new JArray(),["shields"]=new JArray(),["interceptions"]=new JArray()};
 static JObject View(JObject context,long tick)=>new JObject{["version"]="ordinary.local-current-effects.v1.20261106",["accountId"]="account",["runId"]="run",["nodeId"]="node",["snapshotHash"]=context["snapshotHash"].DeepClone(),["attemptGeneration"]=2,["entryEffectsHash"]=context["entryEffectsHash"].DeepClone(),["revision"]=1,["atTick"]=tick,["activeEffects"]=new JArray(),["shields"]=new JArray(),["interceptions"]=new JArray()};
 // Numeric contract fixture only; no real geometry/START admission or completed local-execution input.
 internal static OrdinaryFiveCombatFactory CreateNumericFactory(){
  var f=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
  var node=(JObject)f.Response["receipt"]["nodeStart"];var effects=Entry();effects["accountId"]=f.Start.AccountId;effects["runId"]=f.Start.RunId;effects["nodeId"]=node["nodeId"].DeepClone();effects["startSnapshotHash"]=f.Start.StartSnapshotHash;
  node["snapshot"]["runtime"]["effects"]=effects;node["snapshotHash"]=BDirectAttackInput.Digest(node["snapshot"]).Substring(7);var result=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
  return OrdinaryFiveCombatFactory.FromNodeStart(f.Start,result.NodeStart,result.Scaling,170000);
 }
 [Test] public void RealFactoryNumericPortReadsCurrentOwnerForEachRequestAndTrainDefense(){
  var factory=CreateNumericFactory();int reads=0;bool unsupported=false;var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryLiveNumericAuthority");object authority=null;
  JObject Current(long tick){reads++;var v=new JObject{["version"]="ordinary.local-current-effects.v1.20261106",["accountId"]=factory.Attempt.AccountId,["runId"]=factory.Attempt.RunId,["nodeId"]=factory.Attempt.NodeId,["snapshotHash"]=factory.Attempt.SnapshotHash,["attemptGeneration"]=factory.Attempt.Generation,["entryEffectsHash"]=(string)type.GetProperty("EntryEffectsHash",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(authority),["revision"]=reads,["atTick"]=tick,["activeEffects"]=new JArray(),["shields"]=new JArray(),["interceptions"]=new JArray()};if(unsupported)((JArray)v["activeEffects"]).Add(new JObject{["kind"]="SENTRY"});return v;}
  authority=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{factory,(Func<string,int>)(_=>70),(Func<long,JObject>)Current,(Func<string,long,string>)((id,t)=>"RANGED")},null);
  var method=type.GetMethod("Defense",BindingFlags.Instance|BindingFlags.NonPublic);var defense=(JObject)method.Invoke(authority,new object[]{factory.Original.Train.TrainId,1L});Assert.AreEqual(factory.Original.Train.PermanentDefense,(long)CombatExactValue.From((JObject)defense["defense"]).Floor());
  method.Invoke(authority,new object[]{"op_kyle",2L});Assert.AreEqual(2,reads);unsupported=true;var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(authority,new object[]{"op_kyle",3L}));Assert.IsInstanceOf<NotSupportedException>(error.InnerException);Assert.AreEqual(3,reads);
 }
 [Test] public void OnlyExplicitEmptyOriginalEntryIsAccepted(){var context=Context();var entry=Entry();Assert.AreEqual(BDirectAttackInput.Digest(entry),Call("ValidateEntry",entry,context));entry["activeEffects"] = new JArray(new JObject{["kind"]="unknown"});Assert.Throws<ArgumentException>(()=>Call("ValidateEntry",entry,context));Assert.Throws<ArgumentException>(()=>Call("ValidateEntry",new JObject{["unsupportedEffect"]="retain-original"},context));}
 [Test] public void CurrentNumericPortConsumesExplicitMitigationAndNewReservationDelay(){
  var factory=CreateNumericFactory();var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryLiveNumericAuthority");object authority=null;bool active=true;
  JObject Current(long tick){var a=factory.Attempt;var effects=new JArray();if(active)foreach(string kind in new[]{"MITIGATION_RATE_REDUCTION","NEW_BASIC_PERIOD_INCREASE"})effects.Add(new JObject{["sourceId"]="zone:"+kind,["sourceKind"]="NYX_ZONE",["ownerId"]="op_nyx",["targetId"]="op_kyle",["targetGeneration"]=1,["kind"]=kind,["valueBps"]=kind=="MITIGATION_RATE_REDUCTION"?800:1500,["expiresAtTick"]=10});return new JObject{["version"]="ordinary.local-current-effects.v2.20261107",["accountId"]=a.AccountId,["runId"]=a.RunId,["nodeId"]=a.NodeId,["snapshotHash"]=a.SnapshotHash,["attemptGeneration"]=a.Generation,["entryEffectsHash"]=type.GetProperty("EntryEffectsHash",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(authority).ToString(),["revision"]=tick+1,["atTick"]=tick,["activeEffects"]=effects,["shields"]=new JArray(),["interceptions"]=new JArray()};}
  authority=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{factory,(Func<string,int>)(_=>1),(Func<long,JObject>)Current,(Func<string,long,string>)((id,t)=>"RANGED")},null);
  var period=type.GetMethod("EffectivePeriod",BindingFlags.Instance|BindingFlags.NonPublic);var defense=type.GetMethod("Defense",BindingFlags.Instance|BindingFlags.NonPublic);
  var slowed=(CombatExactValue)period.Invoke(authority,new object[]{"op_kyle",1L});var arrival=(JObject)defense.Invoke(authority,new object[]{"op_kyle",1L});Assert.AreEqual(800,(int)arrival["mitigationRateReduction"][0]["valueBps"]);
  var launch=(JObject)Call("Launch",Exact(1000,1),new JArray());int reduced=CombatProjectileAttackSnapshot.Capture(launch).Evaluate(arrival).ScaleAndFloor(10000);
  active=false;var normal=(CombatExactValue)period.Invoke(authority,new object[]{"op_kyle",2L});var absent=(JObject)defense.Invoke(authority,new object[]{"op_kyle",2L});Assert.IsEmpty((JArray)absent["mitigationRateReduction"]);Assert.Greater(reduced,CombatProjectileAttackSnapshot.Capture(launch).Evaluate(absent).ScaleAndFloor(10000));Assert.IsTrue(JToken.DeepEquals(normal.Multiply(Exact(115,100)).Snapshot(),slowed.Snapshot()));
 }
 [Test] public void CurrentTickAndAttemptCannotReuseEntryOrEarlierObservation(){var c=Context();c["entryEffectsHash"]=(string)Call("ValidateEntry",Entry(),c);var v=View(c,4);Call("ValidateCurrent",v,c,4L);Assert.Throws<ArgumentException>(()=>Call("ValidateCurrent",v,c,5L));v["attemptGeneration"]=1;Assert.Throws<ArgumentException>(()=>Call("ValidateCurrent",v,c,4L));Assert.Throws<ArgumentException>(()=>Call("ValidateCurrent",Entry(),c,0L));}
 [Test] public void LaterUnsupportedEffectDoesNotBecomePermanentNoEffects(){var c=Context();c["entryEffectsHash"]=(string)Call("ValidateEntry",Entry(),c);var v=View(c,10);var first=(JObject)Call("ValidateCurrent",v,c,10L);((JArray)v["activeEffects"]).Add(new JObject{["kind"]="SENTRY"});Assert.Throws<NotSupportedException>(()=>Call("ValidateCurrent",v,c,10L));Assert.AreEqual(0,((JArray)first["activeEffects"]).Count);}
 [TestCase(50,250)][TestCase(49,333)][TestCase(0,333)] public void KyleConditionalDefenseUsesActualHpStrictlyBelowHalf(int hp,int expected){var result=(CombatExactValue)Call("ConditionalDefense",Exact(250,1),83L,hp,100L);Assert.AreEqual(expected,(int)result.Floor());}
 [Test] public void MiraOutgoingPassiveIsAppliedExactlyOnceAcrossProjectileSnapshot(){
  var effects=new JArray(new JObject{["sourceId"]="op_mira.passive.OWN_BASIC",["group"]="NON_EQUIPMENT_OUTGOING",["valueBps"]=1000});var launch=(JObject)Call("Launch",Exact(100,1),effects);
  var defense=new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=Exact(0,1).Snapshot(),["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
  Assert.AreEqual(110,CombatProjectileAttackSnapshot.Capture(launch).Evaluate(defense).ScaleAndFloor(10000));
 }
}

