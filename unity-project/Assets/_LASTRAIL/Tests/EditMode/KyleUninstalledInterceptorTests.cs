using System;
using System.IO;
using System.Reflection;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class KyleUninstalledInterceptorTests {
 const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
 [Test] public void ObserveUnmodifiedFiveThroughNativeEncounter(){
  const string root="D:/Project/LASTRAIL/QA/KyleTauntThree-20261109/";
  var input=JObject.Parse(File.ReadAllText(root+"uninstalled-source.json"));
  var original=OrdinaryLiveExecutionTests.CreateFactory(input);
  var factory=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithLocalUninstalledInterceptor",Hidden).Invoke(original,null);
  var model=factory.Create();model.SetRailThrottle(7000);var events=new JArray();
  var initial=model.ReadHudState();var latest=initial;string failure=null;int previous=0,maxTaunts=0,count=0;long expiry=0;bool depleted=false;
  try{for(int i=0;i<1500&&model.Outcome==BasicCombatOutcome.Running;i++){
   model.Advance(500000,1);var hud=model.ReadHudState();latest=hud;
   var effects=(JArray)typeof(BasicCombatSimulation).GetField("ordinaryEffects",Hidden).GetValue(model);
   var taunts=(JArray)typeof(BasicCombatSimulation).GetField("ordinaryTaunts",Hidden).GetValue(model);
   var actor=hud["actors"].Single(a=>(string)a["id"]=="op_kyle");int shield=(int)actor["kyleGuardShield"];
   maxTaunts=Math.Max(maxTaunts,taunts.Count);
   var added=effects.Skip(count).ToArray();foreach(var e in added.Where(e=>(string)e["sourceId"]=="op_kyle"&&(string)e["kind"]=="SHIELD_ISSUE"))expiry=(long)e["actualResult"]["expiry"];
   if(previous>0&&shield==0&&model.Tick<expiry&&(int)actor["hp"]>0&&added.Any(e=>(string)e["targetId"]=="op_kyle"&&(long?)e["actualResult"]?["absorbed"]>0))depleted=true;
   if(previous!=shield||taunts.Count>=3)events.Add(new JObject{["tick"]=model.Tick,["shield"]=shield,["expiry"]=expiry,["taunts"]=taunts.DeepClone(),["actors"]=hud["actors"].DeepClone()});
   previous=shield;count=effects.Count;
  }}catch(Exception error){failure=error.ToString();throw;}finally{File.WriteAllText(root+"normal-five-observation.json",new JObject{["initial"]=initial,["lastSuccessfulHud"]=latest,["tick"]=model.Tick,["failure"]=failure,["maxTaunts"]=maxTaunts,["depletionObserved"]=depleted,["events"]=events}.ToString());}
  Assert.AreEqual(5,initial["actors"].Count(a=>!(bool)a["enemy"]&&!(bool)a["train"]&&!(bool)a["device"]&&(int)a["hp"]>0));
 }
 [TestCase("uninstalled")][TestCase("pressure")] public void NewAuthoredInputAdvancesWholeCombatWithoutInterceptionFailure(string scenario){
  var input=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/KyleTauntThree-20261109/"+scenario+"-source.json"));
  var original=OrdinaryLiveExecutionTests.CreateFactory(input);
  var configured=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithLocalUninstalledInterceptor",Hidden).Invoke(original,null);
  var model=configured.Create();model.SetRailThrottle(7000);var trace=new JArray();
  bool three=false,expired=false,depleted=false;int before=0,effectCount=0;long expiry=0;
  try {
   for(int i=0;i<1200&&model.Outcome==BasicCombatOutcome.Running;i++){
    model.Advance(500000,1);
    var taunts=(JArray)typeof(BasicCombatSimulation).GetField("ordinaryTaunts",Hidden).GetValue(model);
    var effects=(JArray)typeof(BasicCombatSimulation).GetField("ordinaryEffects",Hidden).GetValue(model);
    foreach(var effect in effects.Skip(effectCount).Where(e=>(string)e["kind"]=="SHIELD_ISSUE"))expiry=(long)effect["actualResult"]["expiry"];
    var kyle=model.ReadHudState()["actors"].Single(a=>(string)a["id"]=="op_kyle");int amount=(int)kyle["kyleGuardShield"];
    foreach(var actor in model.ReadHudState()["actors"].Where(a=>!(bool)a["enemy"]&&!(bool)a["train"]&&!(bool)a["device"]))
     Assert.LessOrEqual((long)actor["availableAt"],model.Tick,"Attack cooldown must never hide a deployed operator: "+actor["id"]);
    if(taunts.Count==3){three=true;foreach(var t in taunts)Assert.AreEqual("op_kyle",typeof(BasicCombatSimulation).GetMethod("OrdinaryTauntTarget",Hidden).Invoke(model,new object[]{(string)t["target"],(long)t["generation"]}));}
    Assert.LessOrEqual(taunts.Count,3);
    if(before>0&&amount==0&&(int)kyle["hp"]>0){if(model.Tick==expiry)expired=true;else if(model.Tick<expiry&&effects.Skip(effectCount).Any(e=>(string)e["targetId"]=="op_kyle"&&(long?)e["actualResult"]?["absorbed"]>0))depleted=true;}
    before=amount;effectCount=effects.Count;
    if(i%20==0)trace.Add(model.ReadHudState());
   }
  }finally{File.WriteAllText("D:/Project/LASTRAIL/QA/KyleTauntThree-20261109/"+scenario+"-full-combat-trace.json",new JObject{["tick"]=model.Tick,["state"]=JObject.Parse(model.CaptureLocalState()),["samples"]=trace}.ToString());}
  Assert.Greater(model.Tick,500);
  if(scenario=="pressure"){Assert.IsTrue(three,"Three actual active taunt targets required");Assert.IsTrue(depleted,"Actual enemy damage must deplete shield before expiry");}
  else Assert.IsTrue(expired,"Shield must survive actual damage until its original expiry");
 }
 [Test] public void ExplicitLocalUninstalledConfigurationIsSeparateAndSurvivesCopy(){
  var input=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/kyle-right-01/boarding-session-preparation-v01/prepared-input.json"));
  var original=OrdinaryLiveExecutionTests.CreateFactory(input);
  var bind=typeof(OrdinaryFiveCombatFactory).GetMethod("WithLocalUninstalledInterceptor",Hidden);
  Assert.NotNull(bind,"The explicit local no-equipment configuration must exist; missing source is not equivalent to uninstalled.");
  var configured=(OrdinaryFiveCombatFactory)bind.Invoke(original,null);
  Assert.AreNotEqual(original.ConfigurationVersion,configured.ConfigurationVersion);
  var model=configured.Create();
  var read=typeof(BasicCombatSimulation).GetMethod("ReadOrdinaryInterception",Hidden);
  var decision=(JObject)read.Invoke(model,new object[]{"test-shot",0L});
  Assert.AreEqual("CLEAR",(string)decision["status"]);
  Assert.AreEqual("EXPLICIT_LOCAL_UNINSTALLED",(string)decision["equipmentState"]);
  Assert.IsFalse((bool)typeof(OrdinaryFiveCombatFactory).GetProperty("LocalInterceptorUninstalled",Hidden).GetValue(original));
  var copy=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithSceneMotion",Hidden).Invoke(configured,new object[]{(Action<long,long>)((t,f)=>{})});
  Assert.IsTrue((bool)typeof(OrdinaryFiveCombatFactory).GetProperty("LocalInterceptorUninstalled",Hidden).GetValue(copy));
 }
}
