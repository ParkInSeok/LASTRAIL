using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using LastRail.Battle;
using LastRail.Core.Simulation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class KyleNativeGuardTests {
 const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
 const string InputPath="D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/kyle-right-01/boarding-session-preparation-v01/prepared-input.json";
 const string TracePath="D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/continuation/kyle-native-support-test.json";
 static object Field(object owner,string name)=>owner.GetType().GetField(name,Hidden).GetValue(owner);
 static object Call(object owner,string name,params object[] args){try{return owner.GetType().GetMethods(Hidden).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static BasicCombatSimulation Model(){
  byte[] bytes=File.ReadAllBytes(InputPath);using var sha=SHA256.Create();
  Assert.AreEqual("fe35c1e71a437f963804ffd7b26199a8c47580f82de47d11303d9ca08ceaf611",BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant());
  var factory=OrdinaryLiveExecutionTests.CreateFactory(JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes)));
  // Isolated new-policy consumer; immutable transport/pin acceptance is tested by OrdinaryFiveStartInputTests.
  var actor=factory.Original.Actors.Single(a=>a.OperatorId=="op_kyle");var kit=(JObject)Field(actor,"kit");
  var active=(JObject)kit["abilities"].Single(a=>(string)a["id"]=="op_kyle.active");
  active["maximumFrontTargets"]=3;active["tauntPolicyVersion"]=KyleTauntPolicy.Version;active["tauntRangeMm"]=12000;
  active["tauntLineOfSightRequired"]=true;active["tauntScope"]="CURRENT_CARRIAGE_THREATS";active["tauntTargetOrder"]="DISTANCE_THEN_ORIGINAL_ORDER_THEN_ID";
  return factory.Create();
 }
 [TestCase(false)][TestCase(true)] public void NativeSupportActuallyCastsAndShieldOutlastsRecovery(bool staleTargetGeneration){
  var trace=new JObject{["scope"]="ISOLATED_NATIVE_SUPPORT_AND_BOARDING_GEOMETRY_NOT_FULL_ADVANCE",["inputPath"]=InputPath};var observations=new JArray();trace["observations"]=observations;
  try{
   var model=Model();model.Advance(0,0);Call(model,"StartOrdinaryLiveWave");
   var boarding=Field(model,"ordinaryBoarding");var clock=(FixedStepDebt)Field(model,"clock");long step=clock.Capture().StepTicks;
   // Drive original boarding paths/clock without unrelated basic attacks, projectiles or obstacle travel.
   void Step(){clock.Credit(step);Assert.IsTrue(clock.TryConsumeStep());Call(boarding,"BeginStep",model.Tick);Call(boarding,"EndStep",model.Tick,false);}
   IReadOnlyList<KyleTauntPolicy.Candidate> Observe()=>(IReadOnlyList<KyleTauntPolicy.Candidate>)Call(model,"ObserveKyleTargets","op_kyle");
   JObject action=null;
   for(int i=0;i<3000&&action==null;i++){
    Step();var rows=Observe();var selected=KyleTauntPolicy.Select(rows,3,12000);
    if(i%60==0||selected.Count>0)observations.Add(new JObject{["tick"]=model.Tick,["candidates"]=new JArray(rows.Select(r=>new JObject{["id"]=r.Id,["generation"]=r.Generation,["distanceSquared"]=r.DistanceSquared.ToString(),["order"]=r.Order,["threat"]=r.ThreatensCarriage,["los"]=r.LineOfSight,["immune"]=r.Immune})),["selected"]=new JArray(selected.Select(r=>r.Id))});
    if(selected.Count==0)continue;
    Assert.AreEqual("CASTING",Call(model,"RequestOrdinarySkill","native-kyle-guard","op_kyle","op_kyle.active",true));
    action=((JArray)Field(model,"ordinaryActions")).OfType<JObject>().Single(a=>(string)a["id"]=="native-kyle-guard");
    CollectionAssert.AreEqual(selected.Select(r=>r.Id),action["tauntTargets"].Select(t=>(string)t["id"]));Assert.That(selected.Count,Is.InRange(1,3));
   }
   Assert.NotNull(action,"Original boarding paths yielded no eligible current-carriage target; inspect bounded candidate trace.");
   if(staleTargetGeneration)foreach(var target in action["tauntTargets"])target["generation"]=(long)target["generation"]+1;
   while(model.Tick<(long)action["due"])Step();Call(model,"ResolveOrdinarySupport",action);Assert.AreEqual("APPLIED",(string)action["status"]);
   var effect=((JArray)Field(model,"ordinaryEffects")).Single(e=>(string)e["commandId"]=="native-kyle-guard"&&(string)e["kind"]=="SHIELD_ISSUE");
   long expiry=(long)effect["actualResult"]["expiry"];Assert.Greater((int)effect["actualResult"]["addedAmount"],0);
   var taunts=(JArray)Field(model,"ordinaryTaunts");var effects=(JArray)Field(model,"ordinaryEffects");
   var statuses=effects.OfType<JObject>().Where(e=>(string)e["commandId"]=="native-kyle-guard"&&(string)e["kind"]=="STATUS_ADD").ToArray();
   if(staleTargetGeneration){Assert.IsEmpty(taunts);Assert.IsEmpty(statuses);trace["result"]="PASS_STALE_GENERATION_NO_TAUNT_RECEIPT";return;}
   Assert.That(taunts.Count,Is.InRange(1,3));Assert.AreEqual(taunts.Count,taunts.Select(t=>(string)t["target"]).Distinct().Count());
   CollectionAssert.AreEquivalent(taunts.Select(t=>(string)t["target"]),statuses.Select(t=>(string)t["targetId"]));
   var feedback=new CombatEffectFeedback();var audio=CombatEffectAudio.LoadResources();int sounds=0;
   foreach(var status in statuses){
    Assert.AreEqual(1,(int)status["effectIndex"]);Assert.AreEqual("taunt",(string)status["actualResult"]["category"]);Assert.AreEqual(0,(long)status["actualResult"]["previousExpiry"]);Assert.IsTrue((bool)status["actualResult"]["accepted"]);Assert.IsNull(status["healthReceipt"],"A taunt must not inherit the caster shield receipt");
    var taunt=taunts.Single(t=>(string)t["target"]==(string)status["targetId"]);Assert.AreEqual((long)taunt["expiry"],(long)status["actualResult"]["expiry"]);
    Assert.AreEqual(effect["nodeId"],status["nodeId"]);Assert.AreEqual(effect["attemptGeneration"],status["attemptGeneration"]);
    Assert.IsTrue(feedback.Observe(status));Assert.IsTrue(audio.Find(status,out var clip,out var gain));Assert.AreEqual("debuff-new",clip.name);Assert.AreEqual(.65f,gain);if(feedback.ReserveAudio(status))sounds++;Assert.IsFalse(feedback.Observe(status));
   }
   Assert.AreEqual(1,sounds,"Multi-target taunt plays one sound per cast effect");
   int published=effects.Count;Call(model,"ResolveOrdinarySupport",action);Assert.AreEqual(published,effects.Count);
   var refresh=(JObject)action.DeepClone();refresh["id"]="native-kyle-refresh";refresh["status"]="CASTING";refresh.Remove("healthReceipt");((JArray)Field(model,"ordinaryActions")).Add(refresh);Call(model,"ResolveOrdinarySupport",refresh);
   var refreshed=effects.OfType<JObject>().Where(e=>(string)e["commandId"]=="native-kyle-refresh"&&(string)e["kind"]=="STATUS_REFRESH").ToArray();Assert.AreEqual(statuses.Length,refreshed.Length);
   foreach(var status in refreshed){var prior=statuses.Single(s=>(string)s["targetId"]==(string)status["targetId"]);Assert.AreEqual(prior["actualResult"]["expiry"],status["actualResult"]["previousExpiry"]);}
   published=effects.Count;var cancelled=(JObject)action.DeepClone();cancelled["status"]="CANCELLED_DOWN";Call(model,"ResolveOrdinarySupport",cancelled);Assert.AreEqual(published,effects.Count);
   trace["action"]=action.DeepClone();trace["shield"]=effect.DeepClone();trace["taunts"]=taunts.DeepClone();
   while(model.Tick<(long)action["recoverAt"])Step();
   JObject Kyle()=>(JObject)model.ReadHudState()["actors"].Single(a=>(string)a["id"]=="op_kyle");
   Assert.IsFalse((bool)Kyle()["casting"]);Assert.Greater((int)Kyle()["kyleGuardShield"],0);
   while(model.Tick<expiry)Step();Assert.AreEqual(0,(int)Kyle()["kyleGuardShield"]);
   foreach(var taunt in taunts)Assert.IsNull(Call(model,"OrdinaryTauntTarget",(string)taunt["target"],(long)taunt["generation"]));
   Call(model,"RetireOrdinaryNativeOwners");var retired=(JObject)action.DeepClone();retired["status"]="CASTING";published=effects.Count;Call(model,"ResolveOrdinarySupport",retired);Assert.AreEqual(published,effects.Count);
   trace["result"]="PASS_ISOLATED_SUPPORT";
  }catch(Exception e){trace["result"]="FAIL";trace["error"]=e.ToString();throw;}
  finally{File.WriteAllText(staleTargetGeneration?"D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/continuation/kyle-stale-target-test.json":TracePath,trace.ToString());}
 }
}
