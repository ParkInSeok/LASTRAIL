using System;
using System.IO;
using System.Reflection;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryReinaLiveTests {
 [Test] public void NormalEntryChargesFromRealBasicHitsAndCompletesBarrage(){
  const string root="D:/Project/LASTRAIL/QA/CutsceneProduction-20261020/reina/runway-final-v01/";
  var input=JObject.Parse(File.ReadAllText(root+"normal-contact-input.json"));
  var factory=OrdinaryLiveExecutionTests.CreateFactory(input);
  factory=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithLocalUninstalledInterceptor",Hidden).Invoke(factory,null);
  factory=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithAugmentOwnership",Hidden).Invoke(factory,new object[]{input["augmentOwnership"]});
  var model=factory.Create();model.SetRailThrottle(7000);model.SetUltimateAuto(true);var initial=model.ReadHudState();
  try{
   Assert.AreEqual(0,(int)initial["ultimates"].Single(x=>(string)x["actor"]=="op_reina")["charge"]);
   for(int i=0;i<1800&&model.Outcome==BasicCombatOutcome.Running;i++){
    model.Advance(500000,1);
    var actions=(JArray)typeof(BasicCombatSimulation).GetField("ordinaryActions",Hidden).GetValue(model);
    var completed=actions.FirstOrDefault(a=>a["barrageCommit"]!=null&&model.Tick>=(long)a["barrageCommit"]["schedule"].Last);
    if(completed!=null)break;
   }
   var state=JObject.Parse(model.CaptureLocalState());var effects=(JArray)state["effects"];
   Assert.IsTrue(effects.Any(e=>(string)e["sourceId"]=="op_kyle"),"Kyle must execute without missing contact failure");
   Assert.IsTrue(effects.Any(e=>(bool?)e["reinaBarrage"]==true),"Normal basic hits must charge and trigger Reina barrage");
   int hits=effects.Where(e=>(string)e["sourceId"]=="op_reina"&&(string)e["sourceKind"]=="BASIC"&&(int)e["actualResult"]["hpLoss"]>0).Select(e=>(string)e["commandId"]).Distinct().Count();
   Assert.GreaterOrEqual(hits,25);Assert.AreEqual((hits-25)*4,(int)state["charge"]["op_reina"],"Barrage damage must not recharge itself");
  }finally{File.WriteAllText(root+"normal-entry-trace.json",new JObject{["initial"]=initial,["state"]=JObject.Parse(model.CaptureLocalState())}.ToString());}
 }
 [TestCase("BASIC",0,0)][TestCase("ENEMY_BASIC",9,0)][TestCase("BASIC",9,4)]
 public void NormalChargeIgnoresShieldOnlyForeignAndRepeatedDamage(string kind,int loss,int expected){
  var model=ActualFactory().Create();
  var method=typeof(BasicCombatSimulation).GetMethod("RecordOrdinaryNativeDamage",Hidden);
  int before=(int)JObject.Parse(model.CaptureLocalState())["charge"]["op_kyle"];
  var contact=new JObject{["eventId"]="charge-test-action",["actorId"]="op_kyle",["abilityId"]="op_kyle.basic",["executorId"]="op_kyle",["projectileId"]="charge-test-projectile",["targetId"]="charge-test-target",["receipt"]=new JObject{["tick"]=0,["hpLoss"]=loss,["absorbed"]=9-loss}};
  method.Invoke(model,new object[]{contact,kind});method.Invoke(model,new object[]{contact,kind});
  Assert.AreEqual(before+expected,(int)JObject.Parse(model.CaptureLocalState())["charge"]["op_kyle"]);
 }
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 internal static OrdinaryFiveCombatFactory ActualFactory(bool owned=false){
  const string root="D:/Project/LASTRAIL/QA/";
  var input=JObject.Parse(File.ReadAllText(root+"CutsceneProduction-20261020/reina/runway-final-v01/live-cinematic-input.json"));
  if(owned){
   input["nodeStart"]["snapshot"]["actors"].Single(a=>(string)a["operatorId"]=="op_reina")["hp"]=500;
   string hash=BDirectAttackInput.Digest(input["nodeStart"]["snapshot"]).Substring(7);input["nodeStart"]["snapshotHash"]=hash;input["currentAttempt"]["snapshotHash"]=hash;
  }
  var factory=OrdinaryLiveExecutionTests.CreateFactory(input);
  factory=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithLocalUninstalledInterceptor",Hidden).Invoke(factory,null);
  return (OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithAugmentOwnership",Hidden).Invoke(factory,new object[]{JObject.Parse(File.ReadAllText(root+"CutsceneProduction-20261020/reina/runway-final-v01/"+(owned?"owned-selection-fixture.json":"actual-augment-ownership.json")))});
 }
 [TestCase(false)][TestCase(true)] public void AuthoredChargedEntryCommitsAndImpactsThroughActualTransactionalDriver(bool owned){
  var driver=CombatSceneDriver.ForOrdinaryNode(ActualFactory(owned),()=>true);driver.Open();driver.SetRailThrottle(7000);driver.SetUltimateAuto(true);
  var events=new JArray();driver.BarragePresentationEvent+=row=>events.Add(row.DeepClone());
  try{
   for(int i=0;i<1200&&events.Count(e=>(string)e["kind"]=="IMPACT")<4;i++)driver.Advance(500000,1);
   Assert.AreEqual(1,events.Count(e=>(string)e["kind"]=="COMMIT"));Assert.AreEqual(4,events.Count(e=>(string)e["kind"]=="IMPACT"));
   var effects=driver.HudState["effectResults"].Where(e=>(bool?)e["reinaBarrage"]==true).ToArray();Assert.IsNotEmpty(effects);
   if(owned){Assert.IsTrue(effects.All(e=>(string)e["lifesteal"]["outcome"]!="NOT_OWNED"));Assert.Greater(effects.Sum(e=>(int)e["lifesteal"]["actualHpGain"]),0);}
   else Assert.IsTrue(effects.All(e=>(string)e["lifesteal"]["outcome"]=="NOT_OWNED"));
  }finally{
   File.WriteAllText("D:/Project/LASTRAIL/QA/CutsceneProduction-20261020/reina/runway-final-v01/live-driver-"+(owned?"owned":"empty")+"-trace.json",new JObject{["events"]=events,["state"]=JObject.Parse(driver.StateJson),["hud"]=driver.HudState}.ToString());driver.Retire();
  }
 }
 [TestCase(false)][TestCase(true)] public void SnapshotFreeNodeCopyRemainsIsolatedAndMatchesNormalExecution(bool sceneCopy){
  var factory=ActualFactory();var original=factory.Create();original.SetRailThrottle(7000);original.SetUltimateAuto(true);
  for(int i=0;i<50;i++)original.Advance(500000,1);
  string before=original.CaptureLocalState();var method=typeof(BasicCombatSimulation).GetMethod("CloneOwnedForScene",Hidden);
  Func<BasicCombatSimulation> create=sceneCopy?()=> (BasicCombatSimulation)typeof(OrdinaryFiveCombatFactory).GetMethod("CreateForSceneCopy",Hidden).Invoke(factory,null):factory.Create;
  var args=new object[]{create,null,false};
  var copy=(BasicCombatSimulation)method.Invoke(original,args);
  Assert.IsNull(args[1]);Assert.AreEqual(before,copy.CaptureLocalState());
  for(int i=0;i<30;i++)copy.Advance(500000,1);
  Assert.AreEqual(before,original.CaptureLocalState(),"Candidate must not mutate the published owner.");
  for(int i=0;i<30;i++)original.Advance(500000,1);
  Assert.AreEqual(original.CaptureLocalState(),copy.CaptureLocalState());
 }
 [Test] public void CurrentEffectsReuseValidatedEntryHashWithoutCopyingNodeSnapshot(){
  var model=ActualFactory().Create();
  var read=typeof(BasicCombatSimulation).GetMethod("ReadOrdinaryCurrentEffects",Hidden);
  var input=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CutsceneProduction-20261020/reina/runway-final-v01/live-cinematic-input.json"));
  var snapshot=input["nodeStart"];
  string expected=BDirectAttackInput.Digest(snapshot["snapshot"]["runtime"]["effects"]);
  read.Invoke(model,new object[]{model.Tick});snapshot.DeepClone();
  var watch=System.Diagnostics.Stopwatch.StartNew();
  for(int i=0;i<256;i++)snapshot.DeepClone();
  double snapshotMs=watch.Elapsed.TotalMilliseconds;watch.Restart();
  for(int i=0;i<256;i++)Assert.AreEqual(expected,(string)((JObject)read.Invoke(model,new object[]{model.Tick}))["entryEffectsHash"]);
  double readMs=watch.Elapsed.TotalMilliseconds;
  TestContext.WriteLine("currentEffectsMs="+readMs+" nodeCopyMs="+snapshotMs);
  Assert.Less(readMs,snapshotMs/2,"Reading dynamic effects must not recopy the immutable node for its already validated hash.");
 }
 [Test] public void LiveBarrageRequiresOriginalAugmentOwnership(){
  var input=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/kyle-right-01/boarding-session-preparation-v01/prepared-input.json"));
  var factory=OrdinaryLiveExecutionTests.CreateFactory(input);
  var bind=typeof(OrdinaryFiveCombatFactory).GetMethod("WithAugmentOwnership",Hidden);
  Assert.NotNull(bind,"Actual run ownership must be bound before enabling live Reina");
  var error=Assert.Throws<TargetInvocationException>(()=>bind.Invoke(factory,new object[]{null}));
  Assert.IsInstanceOf<ArgumentException>(error.InnerException);
  var packet=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CutsceneProduction-20261020/reina/runway-final-v01/actual-augment-ownership.json"));
  packet["runId"]="another-run";var body=(JObject)packet.DeepClone();body.Remove("packetHash");packet["packetHash"]=BDirectAttackInput.Digest(body).Substring(7);
  Assert.Throws<TargetInvocationException>(()=>bind.Invoke(factory,new object[]{packet}));
 }
}
