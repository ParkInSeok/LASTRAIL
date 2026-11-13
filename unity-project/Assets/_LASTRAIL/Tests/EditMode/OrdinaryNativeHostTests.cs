using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryNativeHostTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static object Field(object owner,string name)=>owner.GetType().GetField(name,Hidden)?.GetValue(owner);
 static bool Begun(CombatSceneDriver driver){var property=typeof(CombatSceneDriver).GetProperty("HasBegunExecution");Assert.NotNull(property,"Retired preparation must retain whether combat actually began");return (bool)property.GetValue(driver);}
 static object Call(object owner,string name,params object[] args){try{return owner.GetType().GetMethod(name,Hidden).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static OrdinaryFiveCombatFactory Factory(bool fullHp=false){
  var f=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
  if(fullHp){var node=f.Response["receipt"]["nodeStart"];foreach(var actor in node["snapshot"]["actors"])actor["hp"]=actor["effectiveStats"]["maxHp"].DeepClone();node["snapshotHash"]=BDirectAttackInput.Digest(node["snapshot"]).Substring(7);}
  var result=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
  return OrdinaryFiveCombatFactory.FromNodeStart(f.Start,result.NodeStart,result.Scaling,170000);
 }
 static CombatSceneDriver GunnerDriver(bool fullHp){
  var fixture=Activator.CreateInstance(typeof(OrdinaryNodeGunnerExecutionTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,Array.Empty<object>(),null);
  var factory=fullHp?Factory(true):(OrdinaryFiveCombatFactory)Field(fixture,"Factory");var sources=Field(fixture,"Sources");
  if(fullHp){
   var observe=(Func<string,long,JObject>)Field(sources,"Observe");
   sources=Activator.CreateInstance(sources.GetType(),Hidden,null,new object[]{(Func<string,long,JObject>)((id,tick)=>{var frame=observe(id,tick);frame["snapshotHash"]=factory.Attempt.SnapshotHash;return frame;}),Field(sources,"CaptureAttack"),Field(sources,"CaptureDefense"),Field(sources,"Interception")},null);
  }
  var method=typeof(OrdinaryFiveCombatFactory).GetMethod("WithGunnerSources",Hidden);Assert.NotNull(method,"Node gunner execution must join the existing native clock");
  var bound=(OrdinaryFiveCombatFactory)method.Invoke(factory,new[]{sources});var driver=CombatSceneDriver.ForOrdinaryNode(bound,()=>true);driver.Open();return driver;
 }
 [Test] public void GunnerClockPublishesItsSingleHpReceiptToExistingFeedback(){
  var driver=GunnerDriver(false);driver.Advance(170000L*148,148);
  var hits=((JArray)driver.HudState["effectResults"]).Where(e=>(string)e["sourceKind"]=="ENEMY_BASIC").ToArray();Assert.IsNotEmpty(hits);
  foreach(var hit in hits){Assert.AreEqual((string)hit["targetId"],(string)hit["healthReceipt"]["targetId"]);Assert.Greater((int)hit["actualResult"]["hpLoss"],0);}
  StringAssert.Contains("gunnerExecution",driver.StateJson);Assert.IsTrue(Begun(driver));driver.Retire();
 }
 [Test] public void NativeGunnerAbsorptionUsesApprovedContactOnceAndDoesNotReplayOnReentry(){
  var driver=GunnerDriver(false);
  try{
   var health=(BCombatLocalModel)Field(Field(driver,"current"),"ordinaryHealth");
   foreach(var actor in driver.HudState["actors"].Where(a=>!(bool)a["enemy"])){
    string id=(string)actor["id"];health.ApplyResolvedShield("audio-test-shield:"+id,id,new CombatShieldSource(id,"audio-test-shield",0),1500,2000,0);
   }
   driver.Advance(170000L*148,148);
   var contacts=((JArray)driver.HudState["effectResults"]).OfType<JObject>().Where(e=>(string)e["sourceKind"]=="ENEMY_BASIC"&&(int)e["actualResult"]["absorbed"]>0).ToArray();Assert.IsNotEmpty(contacts);
   var audio=CombatEffectAudio.LoadResources();var feedback=new CombatEffectFeedback();
   foreach(var contact in contacts){Assert.AreEqual(contact["healthReceipt"]["absorbed"],contact["actualResult"]["absorbed"]);Assert.IsTrue(audio.Find(contact,out var clip,out var gain));Assert.AreEqual("shield-warning-local.v02.shield.20261021",clip.name);Assert.AreEqual(.85f,gain);Assert.IsTrue(feedback.Observe(contact));Assert.IsTrue(feedback.ReserveAudio(contact));Assert.IsFalse(feedback.Observe(contact));Assert.IsFalse(feedback.ReserveAudio(contact));}
   var reopened=new CombatEffectFeedback();reopened.Seed((JArray)driver.HudState["effectResults"]);foreach(var contact in contacts){Assert.IsFalse(reopened.Observe(contact));Assert.IsFalse(reopened.ReserveAudio(contact));}
  }finally{driver.Retire();}
 }
 [Test] public void GunnerHudUsesItsOwnedWindupAndTarget(){
  var driver=GunnerDriver(false);driver.Advance(170000L*90,90);
  var actor=driver.HudState["actors"].First(a=>(bool)a["enemy"]);
  Assert.AreEqual("WINDUP",(string)actor["basicStatus"]);Assert.IsTrue((bool)actor["attacking"]);
  Assert.AreEqual(118,(long)actor["attackDue"]);Assert.AreEqual(30,(long)actor["attackWindup"]);
  Assert.IsNotEmpty((string)actor["attackTarget"]);driver.Retire();
 }
 [Test] public void SeraSupportUsesActualGunnerDamageAndTheSameHpReceipt(){
  var driver=GunnerDriver(true);driver.Advance(170000L*200,200);var effects=(JArray)driver.HudState["effectResults"];
  var hits=effects.Where(e=>(string)e["sourceKind"]=="ENEMY_BASIC").ToArray();Assert.IsNotEmpty(hits);
  var heal=effects.Single(e=>(string)e["abilityId"]=="op_sera.active"&&(string)e["kind"]=="HEAL");
  Assert.IsTrue(hits.Any(h=>(string)h["targetId"]==(string)heal["targetId"]&&(long)h["tick"]<(long)heal["tick"]));
  Assert.AreEqual((int)heal["actualResult"]["actualHpGain"],(int)heal["healthReceipt"]["hpGain"]);
  Assert.IsTrue(CombatEffectAudio.LoadResources().Find((JObject)heal,out var healClip,out var healGain));Assert.AreEqual("heal-new",healClip.name);Assert.AreEqual(.178893f,healGain);
  Assert.AreEqual((int)heal["healthReceipt"]["hpAfter"],(int)driver.HudState["actors"].Single(a=>(string)a["id"]==(string)heal["targetId"])["hp"]);driver.Retire();
 }
 // A real node clock with explicitly synthetic observations, never a product force-start flag.
 public static CombatSceneDriver CreateBegunNodeDriver(OrdinaryFiveCombatFactory factory){
  var fixture=Activator.CreateInstance(typeof(OrdinaryNodeProjectileOwnerTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,new object[]{false},null);
  JObject Observe(string actor,long tick){var frame=(JObject)Call(fixture,"Observation",tick,actor);frame["nodeId"]=factory.Attempt.NodeId;frame["attemptGeneration"]=factory.Attempt.Generation;frame["snapshotHash"]=factory.Attempt.SnapshotHash;return frame;}
  var sourceType=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryNodeBasicSources");
  var sources=Activator.CreateInstance(sourceType,Hidden,null,new object[]{(Func<string,long,JObject>)Observe,
   (Func<string,long,CombatExactValue>)((actor,tick)=>CombatExactValue.From(new JObject{["numerator"]=(factory.Original.Basic.Profiles.Single(p=>p.OperatorId==actor).OriginalIntervalMs*1000).ToString(),["denominator"]="1"})),
   (Func<string,string,long,JObject>)((actor,target,tick)=>throw new AssertionException("Only the first preparation step is needed")),
   (Func<string,long,JObject>)((target,tick)=>throw new AssertionException("No projectile can arrive during this first step")),
   (Func<string,long,string>)((target,tick)=>"RANGED")},null);
  var bound=(OrdinaryFiveCombatFactory)Call(factory,"WithBasicSources",sources);var driver=CombatSceneDriver.ForOrdinaryNode(bound,()=>true);driver.Open();
  driver.Advance((long)typeof(OrdinaryFiveCombatFactory).GetProperty("Step",Hidden).GetValue(factory),1);Assert.IsTrue(Begun(driver));return driver;
 }
 [Test] public void DriverClockRunsOriginalBasicScheduleAndContactIntoItsOwnHp(){
  var fixture=Activator.CreateInstance(typeof(OrdinaryNodeBasicExecutionTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,new object[]{"op_reina"},null);
  var sourceFactory=(OrdinaryFiveCombatFactory)Field(fixture,"Factory");
  var method=typeof(OrdinaryFiveCombatFactory).GetMethod("WithBasicSources",Hidden);Assert.NotNull(method,"The original node driver must own the BASIC execution path");
  var factory=(OrdinaryFiveCombatFactory)method.Invoke(sourceFactory,new[]{Field(fixture,"Sources")});
  var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();
  Assert.IsFalse(Begun(driver));
  int hp=driver.HudState["actors"].Where(a=>(bool)a["enemy"]).Sum(a=>(int)a["hp"]);
  driver.Advance(170000L*50,50);Assert.AreEqual(50,driver.Tick);
  Assert.IsTrue(Begun(driver));
  Assert.Less(driver.HudState["actors"].Where(a=>(bool)a["enemy"]).Sum(a=>(int)a["hp"]),hp);
  var committed=JObject.Parse(driver.StateJson);Assert.NotNull(committed["basicExecution"]);Assert.IsTrue(((JArray)committed["projectiles"]["shots"]).Any());
  var effects=(JArray)driver.HudState["effectResults"];Assert.IsTrue(effects.Any(e=>(string)e["kind"]=="DAMAGE"),"Native contact must reach the existing result consumer");
  foreach(var effect in effects.Where(e=>(string)e["kind"]=="DAMAGE")){
   Assert.AreEqual("BASIC",(string)effect["sourceKind"]);Assert.AreEqual((string)effect["targetId"],(string)effect["healthReceipt"]["targetId"]);
   Assert.AreEqual((int)effect["actualResult"]["hpLoss"],(int)effect["healthReceipt"]["hpLoss"]);Assert.AreEqual(factory.Attempt.Generation,(long)effect["attemptGeneration"]);
   StringAssert.EndsWith(".basic",(string)effect["abilityId"]);Assert.IsNotEmpty((string)effect["executorId"]);
   Assert.IsTrue(CombatEffectAudio.LoadResources().Find((JObject)effect,out var contactClip,out var contactGain));Assert.AreEqual("hit-local.v03.C.20261021",contactClip.name);Assert.AreEqual(.85f,contactGain);
  }
  var feedback=new CombatEffectFeedback();var reentry=new CombatEffectFeedback();reentry.Seed(effects);foreach(JObject effect in effects){Assert.IsTrue(feedback.Observe(effect));Assert.IsTrue(feedback.ReserveAudio(effect));Assert.IsFalse(feedback.Observe(effect));Assert.IsFalse(feedback.ReserveAudio(effect));Assert.IsFalse(reentry.Observe(effect));Assert.IsFalse(reentry.ReserveAudio(effect));}
  driver.Advance(0,0);Assert.AreEqual(effects.Count,((JArray)driver.HudState["effectResults"]).Count);
  driver.SetPaused(true);long tick=driver.Tick;driver.Advance(170000L*50,50);Assert.AreEqual(tick,driver.Tick);
  Assert.IsFalse(driver.IsExecutionReady,"Synthetic BASIC sources do not supply the missing full combat owners or approved scene geometry");driver.Retire();Assert.IsTrue(Begun(driver));
 }
 [Test] public void PreparationAndUnboundClockNeverCountAsCombatBeginning(){
  var driver=CombatSceneDriver.ForOrdinaryNode(Factory(),()=>true);driver.Open();Assert.IsFalse(Begun(driver));
  driver.SetPaused(true);driver.SetPaused(false);driver.Advance(170000L*20,20);
  Assert.AreEqual(0,driver.Tick);Assert.IsFalse(Begun(driver));driver.Retire();Assert.IsFalse(Begun(driver));Assert.IsNull(driver.StateJson);
 }
 [Test] public void DriverHudReportsOwnedBasicPreparationRatherThanMissingGeometryPlaceholder(){
  var fixture=Activator.CreateInstance(typeof(OrdinaryNodeBasicExecutionTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,new object[]{"op_reina"},null);
  var factory=(OrdinaryFiveCombatFactory)Call(Field(fixture,"Factory"),"WithBasicSources",Field(fixture,"Sources"));
  var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();driver.Advance(170000L*5,5);
  var actor=driver.HudState["actors"].Single(a=>(string)a["id"]=="op_reina");
  Assert.AreEqual("PREPARING",(string)actor["basicStatus"]);Assert.IsTrue((bool)actor["attacking"]);Assert.Greater((long)actor["attackDue"],driver.Tick);
  Assert.IsNotNull((string)actor["attackTarget"]);Assert.Greater((long)actor["attackWindup"],0);driver.Retire();
 }
 [Test] public void NodeProjectileOwnerFollowsSceneCopyAndRetirement(){
  var factory=Factory();var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();
  var first=Field(driver,"current");var projectiles=Field(first,"ordinaryProjectiles");Assert.NotNull(projectiles,"Native flight must belong to the live node, not a parallel HP owner");
  var fixture=Activator.CreateInstance(typeof(OrdinaryNodeProjectileOwnerTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,new object[]{false},null);
  var observation=(JObject)Call(fixture,"Observation",0L,"op_reina");var attack=(JObject)typeof(OrdinaryNodeProjectileOwnerTests).GetMethod("Attack",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
  object[] launch={"native-flight","op_reina","native-projectile",Field(fixture,"Target"),observation,BDirectAttackInput.Digest(observation),attack};
  Assert.IsTrue((bool)Call(projectiles,"Launch",launch));
  var capture=Call(projectiles,"Capture");driver.SetPaused(true);
  var next=Field(Field(driver,"current"),"ordinaryProjectiles");Assert.AreNotSame(projectiles,next);Assert.IsTrue(JToken.DeepEquals((JToken)capture,(JToken)Call(next,"Capture")));
  Assert.Throws<InvalidOperationException>(()=>Call(projectiles,"Launch",launch));Assert.IsFalse((bool)Call(next,"Launch",launch),"The replacement owns the pending flight and original replay");
  StringAssert.Contains("projectiles",driver.StateJson);driver.Retire();Assert.IsNull(Field(driver,"current"));
  Assert.Throws<InvalidOperationException>(()=>Call(next,"Launch",launch));
 }
 [Test] public void SupportPublishesOriginalAttributionAndOneAuthoritativeResult(){
  var factory=Factory();var model=factory.Create();var actions=(JArray)Field(model,"ordinaryActions");var health=(BCombatLocalModel)Field(model,"ordinaryHealth");
  var action=new JObject{["id"]="native-support",["actor"]="op_sera",["ability"]="op_sera.active",["target"]="op_reina",["status"]="CASTING",["due"]=0};actions.Add(action);
  int before=health.Hp("op_reina");Call(model,"ResolveOrdinarySupport",action);
  var effects=(JArray)Field(model,"ordinaryEffects");Assert.AreEqual(1,effects.Count);var effect=effects.Single();
  Assert.AreEqual("op_sera",(string)effect["sourceId"]);Assert.AreEqual("op_sera.active",(string)effect["abilityId"]);Assert.AreEqual("op_reina",(string)effect["targetId"]);
  Assert.AreEqual(factory.Attempt.NodeId,(string)effect["nodeId"]);Assert.AreEqual(factory.Attempt.Generation,(long?)effect["attemptGeneration"]);
  Assert.AreEqual("op_sera",(string)effect["executorId"]);Assert.AreEqual(health.Hp("op_reina"),(int?)effect["healthReceipt"]?["hpAfter"]);
  Assert.AreEqual(health.Hp("op_reina")-before,(int)effect["actualResult"]["actualHpGain"]);
  string committed=model.CaptureLocalState();Call(model,"ResolveOrdinarySupport",action);Assert.AreEqual(committed,model.CaptureLocalState(),"A resolved cast must not publish another effect");
 }
}
