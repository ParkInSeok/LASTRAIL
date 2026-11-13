#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public sealed class BMiraFirstLiveTests {
  BDirectAttackBasicLoop root;BDirectAttackBasicLoopBinding binding;BMiraFirstTrialContext context;BBoardingEventsNodeSnapshot node;BWaveBoardingLocalPaths paths;MiraJudgementSceneAdapter scene;BTravelRuntimeConfiguration motion;
  long eventIndex;
  static string Hash(JObject p)=>BDirectAttackInput.Digest(p);
  static MethodInfo Method(Type t,string name){var m=t.GetMethod(name);Assert.That(m,Is.Not.Null,name+" native first-trial connection missing.");return m;}
  static object Call(object o,string name,params object[] args){try{return Method(o.GetType(),name).Invoke(o,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  static object Static(string name,params object[] args){try{return Method(typeof(BDirectAttackBasicLoop),name).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  void Create(int capacity=10000){
   Method(typeof(BDirectAttackBasicLoop),"CreateMiraFirstTrialRuntime");
   binding=BMiraFirstNativeFixture.Binding();node=BMiraFirstNativeFixture.Node();paths=BMiraFirstNativeFixture.Paths(binding.Composition.Attack);
   context=BMiraFirstTrialContext.Read(BMiraFirstNativeFixture.Source(),BMiraFirstTrialContextTests.Policy());
   scene=BMiraFirstNativeFixture.Scene();scene.AddApprovedTestBox("FIRST_LIVE_BOX",16800,3000);
   var rail=global::CombatRailJourneyTests.Config();rail["offers"]=new JArray();rail["throttleMaximumBps"]=13000;rail["throttleCruiseBps"]=10000;
   motion=BTravelRuntimeConfiguration.LocalValidation(rail,Hash(rail));
   root=(BDirectAttackBasicLoop)Static("CreateMiraFirstTrialRuntime",binding,node,paths,context,scene,capacity,motion);
  }
  JObject Mira(BDirectAttackBasicLoop owner=null)=>(JObject)(owner??root).Capture()["miraFirstTrial"];
  JArray Shots(BDirectAttackBasicLoop owner=null)=>(JArray)Mira(owner)["shots"];
  long Next(BDirectAttackBasicLoop owner=null)=>(long)(owner??root).Capture()["state"]["direct"]["next"]["op_mira"];
  Transform Target()=>scene.GetComponentsInChildren<Transform>().Single(t=>t.name==BMiraFirstNativeFixture.TargetId);
  void Frame(BDirectAttackBasicLoop owner=null,string id=null)=>Call(owner??root,"AdvanceMiraFirstFrame",id??"frame-"+(++eventIndex),binding.Composition.Attack.StepTimeSpanTicks,scene);
  void Fire(){for(int i=0;i<200&&Shots().Count==0;i++)Frame();Assert.That(Shots().Count,Is.EqualTo(1),"Expected original targetable fire opportunity");}
  void Resolve(){for(int i=0;i<70&&(string)Shots()[0]["status"]=="FLYING";i++)Frame();Assert.That((string)Shots()[0]["status"],Is.Not.EqualTo("FLYING"));}
  void Observe(string operation,JObject fields){
   var p=(JObject)fields.DeepClone();p["kind"]="LOCAL_ORIGINAL";p["eventId"]="original-"+(++eventIndex);p["operation"]=operation;
   Call(root,"ObserveMiraFirstOriginal",(string)p["eventId"],p);
  }
  BDirectAttackBasicLoop Restore(JObject p,int capacity=10000)=>(BDirectAttackBasicLoop)Static("RestoreMiraFirstTrialRuntime",p,Hash(p),binding,node,paths,context,scene,capacity,motion);
  static JObject Trust(JObject node)=>new JObject(new[]{"origin","nativeContextId","seedHash","sourceHash","nodeId","visitDepth","priorSourceHash"}.Select(k=>new JProperty(k,node[k].DeepClone())));
  BNodeSessionController Controller(){var p=node.OriginalSource.Capture();var c=new BNodeSessionController((string)p["origin"]["accountId"],node.OriginalSource.RunId);c.BindVerifiedNode(p,Trust(p),0);c.AttachLocalTravel(node,motion,(n,m)=>root,c.Revision);return c;}
  BNodeSessionController RestoreNode(JObject saved){var t=typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Run.BMiraFirstNodeCheckpoint");Assert.That(t,Is.Not.Null,"Whole node + native pose coordinator missing");try{return (BNodeSessionController)Method(t,"Restore").Invoke(null,new object[]{saved,Hash(saved),Trust(node.OriginalSource.Capture()),binding,node,paths,context,scene,10000,motion});}catch(TargetInvocationException e){throw e.InnerException;}}
  [TestCase(false,0)][TestCase(false,1)][TestCase(false,2)][TestCase(true,1)]public void WholeNodeCheckpointValidatesBeforePublishingNativePoses(bool corruptObservation,int checkpoint){
   Create();if(checkpoint>0)Fire();if(checkpoint>1)Resolve();using(var c=Controller()){
    var original=c.Capture();var saved=JObject.Parse(original.ToString());Assert.That(Hash(saved),Is.EqualTo(Hash(original)),"Serialized whole node pin must be stable");var pose=Target().position;Target().position+=Vector3.right*5;var moved=scene.CaptureNativeState(root.Tick,99);
    if(corruptObservation){saved["observation"]["originalObservation"]["sourceRevision"]=999;Assert.Throws<ArgumentException>(()=>RestoreNode(saved));Assert.That(JToken.DeepEquals(scene.CaptureNativeState(root.Tick,99),moved),Is.True);}
    else using(var restored=RestoreNode(saved)){Assert.That(JToken.DeepEquals(saved,restored.Capture()),Is.True);Assert.That(Target().position,Is.EqualTo(pose));}
   }
  }
  [Test]public void OriginalNodeControllerOwnsFirstFrameAndNoViewWritesCombat(){
   Create();using(var c=Controller()){var before=c.Capture();Call(c,"AdvanceMiraFirstFrame","controller-frame",binding.Composition.Attack.StepTimeSpanTicks,scene,c.Revision);Assert.That((long)c.Capture()["runtime"]["state"]["direct"]["clock"]["completedSteps"],Is.EqualTo(1));Assert.That((long)c.Capture()["observation"]["originalObservation"]["sourceRevision"],Is.EqualTo(1));}
  }
  [Test]public void OrdinalTargetWinsOverCloserEnemyAndNoEmptyFieldBacklogIsFired(){
   Create();var targets=scene.GetComponentsInChildren<CapsuleCollider>();var old=targets.ToDictionary(t=>t.name,t=>t.transform.position);
   foreach(var target in targets)target.transform.position+=Vector3.forward*50;
   for(int i=0;i<85;i++)Frame();Assert.That(Shots().Count,Is.Zero);Assert.That(Next(),Is.Zero);
   foreach(var target in targets)target.transform.position=old[target.name];
   targets[1].transform.position=Target().position-new Vector3(0,0,2);Fire();Assert.That((string)Shots()[0]["targetId"],Is.EqualTo(BMiraFirstNativeFixture.TargetId));
   Assert.That(Next(),Is.EqualTo(root.Tick+180));for(int i=0;i<10;i++)Frame();Assert.That(Shots().Count,Is.EqualTo(1));
  }
  [Test]public void InvalidFirstCandidateBeforeLaunchUsesNextOriginalOrdinalWithoutExtraFire(){
   Create();Target().position+=Vector3.forward*50;Fire();var first=node.OriginalSource.Waves[0].Instances[1].InstanceId;
   Assert.That((string)Shots()[0]["targetId"],Is.EqualTo(first));Assert.That(Next(),Is.EqualTo(root.Tick+180));
   var saved=root.Capture();var restored=Restore(saved);Assert.That(JToken.DeepEquals(saved,restored.Capture()),Is.True);
  }
  [TestCase(6000,125000,116458.33333333333,12)][TestCase(12000,250000,241458.33333333333,25)][TestCase(24000,500000,491458.33333333333,50)]
  public void ActualNativeSixTwelveTwentyFourMetreShotsKeepAnchorAndContactTimesSeparate(int distance,long anchorUs,double contactUs,long contactStep){
   Create();foreach(var target in scene.GetComponentsInChildren<CapsuleCollider>())target.transform.position+=Vector3.forward*50;
   var muzzle=scene.GetComponentsInChildren<Transform>().Single(t=>t.name=="JudgementMuzzle_NotArtSocket");var targetRow=((JArray)scene.Observe(0,1)["targets"]).Single(t=>(string)t["id"]==BMiraFirstNativeFixture.TargetId);Assert.That((string)targetRow["definitionId"],Is.EqualTo("enemy_spike_cart_gunner"));float anchorHeight=((long)targetRow["anchorMm"][1]-(long)targetRow["feetMm"][1])/1000f;Target().position=new Vector3(muzzle.position.x,muzzle.position.y-anchorHeight,muzzle.position.z+distance/1000f);
   int hp=root.Hp(BMiraFirstNativeFixture.TargetId);Fire();long launch=(long)Shots()[0]["launchTick"];Resolve();var shot=Shots()[0];Assert.That((double)shot["ray"]["anchorTravelUs"],Is.EqualTo(anchorUs).Within(.0001));Assert.That((double)shot["contact"]["elapsedUs"],Is.EqualTo(contactUs).Within(.0001));Assert.That(root.Tick-launch,Is.EqualTo(contactStep));Assert.That(root.Hp(BMiraFirstNativeFixture.TargetId),Is.EqualTo(hp-43));
  }
  [Test]public void NativeMiraLethalContactPublishesHpAndOneOriginalDeathGenerationTogether(){
   Create();int hp=root.Hp(BMiraFirstNativeFixture.TargetId);Observe("DAMAGE",new JObject{["targetId"]=BMiraFirstNativeFixture.TargetId,["amount"]=hp-20});Fire();Resolve();Assert.That(root.Hp(BMiraFirstNativeFixture.TargetId),Is.Zero);
   var saved=root.Capture();var life=((JArray)saved["boarding"]["lives"]).Single(l=>(string)l["id"]==BMiraFirstNativeFixture.TargetId);Assert.That((long)life["generation"],Is.EqualTo(2));Assert.That(((JArray)saved["boarding"]["events"]).Count(e=>(string)e["kind"]=="UNREGISTER_DEATH"&&(string)e["instanceId"]==BMiraFirstNativeFixture.TargetId),Is.EqualTo(1));Assert.That(JToken.DeepEquals(saved,Restore(saved).Capture()),Is.True);
  }
  [TearDown]public void Cleanup(){if(scene!=null)UnityEngine.Object.DestroyImmediate(scene.gameObject);}
  [Test]public void ExistingNativeBindingFixtureUsesSameActualAccountRunPlacementAndNode(){
   Create();var m=Mira();Assert.That((string)m["context"]["runId"],Is.EqualTo(node.OriginalSource.RunId));
   Assert.That((string)m["context"]["critical"]["status"],Is.EqualTo("NOT_APPLICABLE"));
   Assert.That((string)m["lastObservation"]["source"]["id"],Is.EqualTo("op_mira"));Assert.That((int)m["lastObservation"]["source"]["slot"],Is.EqualTo(1));
   Assert.That(root.Capture()["travel"],Is.Not.Null);Assert.That((bool)context.Capture()["firstSource"]["canStart"],Is.False);
  }
  [Test]public void ActualGrownAttackOwnBasicAndPeriodAreConsumedAtLaunchWithoutHpDamage(){
   Create();int hp=root.Hp(BMiraFirstNativeFixture.TargetId);Fire();var shot=Shots()[0];
   Assert.That(root.Hp(BMiraFirstNativeFixture.TargetId),Is.EqualTo(hp));Assert.That((string)shot["attack"]["completedAttack"]["numerator"],Is.EqualTo("182259"));
   Assert.That((string)shot["attack"]["completedAttack"]["denominator"],Is.EqualTo("4000"));
   Assert.That(Next(),Is.EqualTo((long)shot["launchTick"]+180));Assert.That((long)shot["sourceGeneration"],Is.EqualTo(1));
   Assert.That(((JArray)shot["attack"]["appliedOffense"]).Count(t=>(string)t["sourceId"]=="passive:op_mira:OWN_BASIC"),Is.EqualTo(1));
  }
  [Test]public void ContactAppliesFinalDamageExactlyOnceWithoutRescheduling(){
   Create();int hp=root.Hp(BMiraFirstNativeFixture.TargetId);Fire();long next=Next();Resolve();
   Assert.That((string)Shots()[0]["status"],Is.EqualTo("HIT"));Assert.That(root.Hp(BMiraFirstNativeFixture.TargetId),Is.EqualTo(hp-43));Assert.That(Next(),Is.EqualTo(next));
   var before=root.Capture();string oldId=(string)((JArray)before["commands"]).Last["eventId"];
   Frame(id:oldId);Assert.That(JToken.DeepEquals(root.Capture(),before),Is.True);
  }
  [Test]public void AttackIsFrozenAndDefenseIsEvaluatedOnlyAtArrival(){
   Create();int hp=root.Hp(BMiraFirstNativeFixture.TargetId);Fire();Observe("ATTACK",new JObject{["value"]=new JObject{["numerator"]="999",["denominator"]="1"}});
   Observe("DEFENSE",new JObject{["value"]=new JObject{["numerator"]="1000",["denominator"]="1"}});Resolve();
   Assert.That(root.Hp(BMiraFirstNativeFixture.TargetId),Is.EqualTo(hp-22));Assert.That((string)Shots()[0]["attack"]["completedAttack"]["numerator"],Is.EqualTo("182259"));
  }
  [TestCase(100,0,43)][TestCase(10,33,10)]
  public void NativeShieldAndHpUseTheExistingVitalsOwner(int shield,int loss,int absorbed){
   Create();Observe("SHIELD",new JObject{["amount"]=shield,["expiry"]=10000});int hp=root.Hp(BMiraFirstNativeFixture.TargetId);Fire();Resolve();
   Assert.That(root.Hp(BMiraFirstNativeFixture.TargetId),Is.EqualTo(hp-loss));
   var receipt=Mira()["contacts"][0]["hp"];Assert.That((int)receipt["absorbed"],Is.EqualTo(absorbed));
  }
  [Test]public void SourceDownAfterCommittedFireDoesNotCancelTheExistingProjectile(){
   Create();Fire();Observe("DAMAGE",new JObject{["targetId"]="op_mira",["amount"]=1428});Assert.That(root.Hp("op_mira"),Is.EqualTo(0));Resolve();
   Assert.That((string)Shots()[0]["status"],Is.EqualTo("HIT"));
  }
  [Test]public void NativeMovedTargetMissConsumesOnlyTheOriginalOpportunity(){
   Create();int hp=root.Hp(BMiraFirstNativeFixture.TargetId);Fire();long next=Next();Target().position+=Vector3.right*10;Resolve();
   Assert.That((string)Shots()[0]["status"],Is.EqualTo("MISS"));Assert.That(root.Hp(BMiraFirstNativeFixture.TargetId),Is.EqualTo(hp));Assert.That(Next(),Is.EqualTo(next));
  }
  [Test]public void NativeApprovedSolidEnteringAfterLaunchStopsBeforeTarget(){
   Create();int hp=root.Hp(BMiraFirstNativeFixture.TargetId);Fire();var muzzle=scene.GetComponentsInChildren<Transform>().Single(t=>t.name=="JudgementMuzzle_NotArtSocket");
   var box=scene.GetComponentsInChildren<Transform>().Single(t=>t.name=="FIRST_LIVE_BOX");box.position=new Vector3(muzzle.position.x,.8f,3);
   Resolve();Assert.That((string)Shots()[0]["status"],Is.EqualTo("BLOCKED"));Assert.That(root.Hp(BMiraFirstNativeFixture.TargetId),Is.EqualTo(hp));
  }
  [Test]public void OriginalDeathGenerationIsNotRetargetedAndDeathJournalIsNotRepeated(){
   Create();Fire();Observe("DAMAGE",new JObject{["targetId"]=BMiraFirstNativeFixture.TargetId,["amount"]=int.MaxValue});Resolve();
   Assert.That((string)Shots()[0]["status"],Is.EqualTo("MISS"));
   Assert.That(((JArray)root.Capture()["boarding"]["events"]).Count(t=>(string)t["instanceId"]==BMiraFirstNativeFixture.TargetId&&(string)t["kind"]=="UNREGISTER_DEATH"),Is.EqualTo(1));
  }
  [Test]public void HoldFreezesExistingClockTravelProjectileAndHpThenResumeContinues(){
   Create();Fire();root.SetPaused("hold",true);var before=root.Capture();for(int i=0;i<10;i++)Frame();
   Assert.That(root.Tick,Is.EqualTo((long)before["state"]["direct"]["clock"]["completedSteps"]));
   Assert.That(JToken.DeepEquals(root.Capture()["state"]["direct"]["enemies"],before["state"]["direct"]["enemies"]),Is.True);
   Assert.That(JToken.DeepEquals(Shots(),before["miraFirstTrial"]["shots"]),Is.True);Assert.That(JToken.DeepEquals(root.Capture()["travel"]["motion"],before["travel"]["motion"]),Is.True);
   root.SetPaused("resume",false);Resolve();Assert.That((string)Shots()[0]["status"],Is.EqualTo("HIT"));
  }
  [TestCase(0)][TestCase(1)][TestCase(2)]
  public void WholeNativeReplayBeforeDuringAfterFlightMatchesContinuousExecution(int checkpoint){
   Create();if(checkpoint>0)Fire();if(checkpoint>1)Resolve();var original=root.Capture();var p=JObject.Parse(original.ToString());Assert.That(Hash(p),Is.EqualTo(Hash(original)),"Serialized checkpoint pin must be stable");var r=Restore(p);Assert.That(JToken.DeepEquals(r.Capture(),p),Is.True);
   for(int i=0;i<20;i++){string id="continuation-"+i;Frame(root,id);Frame(r,id);}
   Assert.That(JToken.DeepEquals(root.Capture(),r.Capture()),Is.True);
  }
  [Test]public void VersionlessOrDifferentCriticalPolicyCheckpointCannotBeUpgradedOrPartiallyApplied(){
   Create();Fire();var before=root.Capture();var bad=(JObject)before.DeepClone();bad["miraFirstTrial"]["context"]["critical"]["status"]="NON_CRITICAL";
   Assert.Throws<ArgumentException>(()=>Restore(bad));Assert.That(JToken.DeepEquals(root.Capture(),before),Is.True);
   bad=(JObject)before.DeepClone();((JObject)bad["miraFirstTrial"]["context"]).Remove("critical");Assert.Throws<ArgumentException>(()=>Restore(bad));
  }
  [Test]public void CapacityFailureCannotPublishClockOpportunityHpOrProjectilePartialState(){
   Create(100);bool rejected=false;
   for(int i=0;i<130;i++){var before=root.Capture();try{Frame();}catch(InvalidOperationException){Assert.That(JToken.DeepEquals(before,root.Capture()),Is.True);rejected=true;break;}}
   Assert.That(rejected,Is.True);
  }
  [Test]public void ExistingImmediateBasicAndOrdinaryAdvanceCannotBypassFirstProjectileMode(){
   Create();var before=root.Capture();Assert.Throws<InvalidOperationException>(()=>root.Advance("bad-advance",100000,1));
   Assert.Catch(()=>root.ApplyWaveBasic("bad-basic","op_mira",BMiraFirstNativeFixture.TargetId,1));Assert.That(JToken.DeepEquals(root.Capture(),before),Is.True);
  }
  [Test]public void NativeFrameProducerMustExposeReadOnlyAuthoringAndKeepOriginalPackets(){
   Create();var p=(JObject)Call(scene,"CaptureAuthoring");Assert.That((string)p["profileHash"],Is.EqualTo((string)Mira()["authoring"]["profileHash"]));
   Assert.That((bool)context.Capture()["firstSource"]["canStart"],Is.False);Assert.That(context.Capture()["firstSource"]["critical"],Is.Null);
  }
 }
}
#endif