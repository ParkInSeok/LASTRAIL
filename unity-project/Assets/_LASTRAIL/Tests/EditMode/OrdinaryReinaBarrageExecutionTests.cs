using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryReinaBarrageExecutionTests {
 sealed class Authority:ICombatSkillLifestealAuthority {
  internal bool Owned;
  public string BindingId=>"TEST_ONLY_REINA_EXPLICIT_NOT_OWNED";
  public CombatSkillLifestealProof ResolveOriginal(ConfirmedSkillDamage damage,long tick)=>new CombatSkillLifestealProof("TEST_ONLY_REINA_PROOF","TEST_ONLY_ORIGINAL_PROOF",Owned,true,true,10000);
 }
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object value,string name)=>value.GetType().GetField(name,Hidden).GetValue(value);
 static object Call(object owner,string method,params object[] args){try{return owner.GetType().GetMethods(Hidden).Single(m=>m.Name==method&&m.GetParameters().Length==args.Length).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 sealed class Fixture {
  internal readonly OrdinaryFiveCombatFactory Factory;internal readonly BCombatLocalModel Health;internal readonly object Execution;
  internal readonly string Primary,Extra;internal readonly List<JObject> Results=new List<JObject>();
  internal readonly Authority Proof=new Authority();
  internal long PrimaryX=5000,ExtraX=7000,TargetGeneration=1,FrameGeneration=1,AttemptOffset=0;
  internal bool FrameAlive=true,Los=true,Eligible=true,Blocked=false,MissingAttack=false;
  internal int Captures,Observations;internal long AttackValue=10;internal string Selected;internal Action<JObject> Mutate;internal Func<string,long,JObject> DefenseSource;
  internal Fixture(long step=170000){
   var basis=Activator.CreateInstance(typeof(OrdinaryNodeProjectileOwnerTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,new object[]{false},null);
   Factory=(OrdinaryFiveCombatFactory)Get(basis,"Factory");Health=(BCombatLocalModel)Get(basis,"Health");Primary=(string)Get(basis,"Target");
   if(step!=170000){
    var original=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    var node=original.Response["receipt"]["nodeStart"];node["snapshot"]["runtime"]["actions"]["stepTimeSpanTicks"]=step;node["snapshotHash"]=BDirectAttackInput.Digest(node["snapshot"]).Substring(7);
    var parsed=BRouteCommandResult.Read(200,original.Response.ToString(),original.Command);Factory=OrdinaryFiveCombatFactory.FromNodeStart(original.Start,parsed.NodeStart,parsed.Scaling,step);Health=(BCombatLocalModel)Get(Factory.Create(),"ordinaryHealth");
   }
   Extra=((JObject)JObject.Parse(Health.CaptureLocal())["vitals"]).Properties().Select(p=>p.Name).Single(id=>!id.StartsWith("op_")&&id!=Factory.Original.Train.TrainId&&id!=Primary);Selected=Primary;
   var sourcesType=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryReinaBarrageSources");Assert.NotNull(sourcesType,"Missing independent Reina barrage sources.");
   var sources=Activator.CreateInstance(sourcesType,Hidden,null,new object[]{(Func<string,long,JObject>)Observe,(Func<string,long,JObject>)((id,tick)=>{Captures++;return MissingAttack?null:Attack(AttackValue);}), (Func<string,long,JObject>)((id,tick)=>DefenseSource==null?Defense():DefenseSource(id,tick)),Proof},null);
   var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryReinaBarrageExecution");Assert.NotNull(type);
   Execution=Activator.CreateInstance(type,Hidden,null,new object[]{Factory,Health,sources,100},null);
  }
  internal JObject Observe(string command,long tick){
   Observations++;
   JObject Target(string id,long x,int order)=>new JObject{["id"]=id,["generation"]=TargetGeneration,["eligible"]=Eligible,["protected"]=false,["battleOrder"]=order,["carDelta"]=0,["frameId"]="synthetic-frame",["localAnchorMm"]=new JArray(x,500,0),["worldAnchorMm"]=new JArray(x,500,0),["castLineOfSight"]=Los};
   var o=new JObject{["tick"]=tick,["revision"]=tick+1,["nodeId"]=Factory.Attempt.NodeId,["snapshotHash"]=Factory.Attempt.SnapshotHash,["attemptGeneration"]=Factory.Attempt.Generation+AttemptOffset,
    ["source"]=new JObject{["id"]="op_reina",["generation"]=1,["executorId"]="op_reina:flare",["abilityId"]="op_reina.ultimate",["actionEligible"]=true,["muzzleMm"]=new JArray(0,500,0)},["primaryTargetId"]=Selected,
    ["frame"]=new JObject{["id"]="synthetic-frame",["generation"]=FrameGeneration,["alive"]=FrameAlive,["originMm"]=new JArray(0,0,0),["basis"]=new JArray(new JArray(1,0,0),new JArray(0,1,0),new JArray(0,0,1))},
    ["targets"]=new JArray(Target(Primary,PrimaryX,0),Target(Extra,ExtraX,1)),["solidOccluders"]=Blocked?new JArray(new JObject{["id"]="explicit-test-solid",["centerMm"]=new JArray(6000,500,0),["sizeMm"]=new JArray(100,1000,1000)}):new JArray()};Mutate?.Invoke(o);return o;
  }
  internal string Request(string id="cast-one",int charge=100)=>(string)Call(Execution,"Request",id,0L,charge);
  internal void Step(long tick,int charge=100){foreach(var e in (IReadOnlyList<JObject>)Call(Execution,"Step",tick,charge))Results.Add(e);}
  internal void Through(long from,long to){for(long t=from;t<=to;t++)Step(t);}
  internal JObject Capture()=>(JObject)Call(Execution,"Capture");
  internal IEnumerable<JObject> Damage=>Results.Where(r=>(string)r["kind"]=="DAMAGE");
 }
 static JObject Attack(long value)=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=new JObject{["numerator"]=value.ToString(),["denominator"]="1"},["appliedOffense"]=new JArray(),["penetration"]=new JArray()};
 static JObject Defense()=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=new JObject{["numerator"]="0",["denominator"]="1"},["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
 static JArray Feedback(Fixture f){var method=f.Execution.GetType().GetMethod("ReadFeedback",Hidden);Assert.NotNull(method,"Missing readonly barrage area projection");return (JArray)method.Invoke(f.Execution,null);}
 static JToken FeedbackTarget(JArray rows,string id)=>rows[0]["targets"].Single(t=>(string)t["id"]==id);
 [Test] public void FeedbackIsCommitOnlyReusesObservationAndCannotChangeOwner(){
  var f=new Fixture();Assert.IsEmpty(Feedback(f));f.Request();f.Through(0,47);Assert.IsEmpty(Feedback(f));f.Step(48);
  int observed=f.Observations;var before=f.Capture();var hp=f.Health.CaptureLocal();var rows=Feedback(f);
  Assert.AreEqual(1,rows.Count);Assert.AreEqual("cast-one",(string)rows[0]["commandId"]);Assert.AreEqual(3000,(int)rows[0]["radiusMm"]);Assert.AreEqual(0,(int)rows[0]["nextPulse"]);
  rows[0]["centerMm"][0]=1;rows[0]["frame"]["originMm"][0]=1;rows[0]["targets"][0]["eligible"]=false;
  Assert.AreEqual(5000,(long)Feedback(f)[0]["centerMm"][0]);Assert.IsTrue((bool)FeedbackTarget(Feedback(f),f.Primary)["eligible"]);
  Assert.AreEqual(observed,f.Observations);Assert.IsTrue(JToken.DeepEquals(before,f.Capture()));Assert.AreEqual(hp,f.Health.CaptureLocal());
 }
 [Test] public void FeedbackTracksFrameButNotPrimaryAndRetainsSelectedTargetsAcrossExit(){
  var f=new Fixture();f.Request();f.Through(0,48);f.PrimaryX=9000;
  f.Mutate=o=>{o["frame"]["originMm"]=new JArray(50000,0,0);foreach(var t in (JArray)o["targets"])t["worldAnchorMm"][0]=(long)t["localAnchorMm"][0]+50000;};f.Step(49);
  var rows=Feedback(f);Assert.AreEqual(50000,(long)rows[0]["frame"]["originMm"][0]);Assert.AreEqual(5000,(long)rows[0]["centerMm"][0]);Assert.AreEqual(2,rows[0]["targets"].Count());Assert.IsFalse((bool)FeedbackTarget(rows,f.Primary)["eligible"]);
  f.PrimaryX=5000;f.Step(50);Assert.IsTrue((bool)FeedbackTarget(Feedback(f),f.Primary)["eligible"]);
 }
 [Test] public void FeedbackUsesSameThreeDimensionalBoundaryAndCenterOcclusion(){
  var f=new Fixture();f.Request();f.Through(0,48);f.ExtraX=8000;f.Step(49);Assert.IsTrue((bool)FeedbackTarget(Feedback(f),f.Extra)["eligible"]);
  f.Mutate=o=>{o["targets"][1]["localAnchorMm"][1]=501;o["targets"][1]["worldAnchorMm"][1]=501;};f.Step(50);Assert.IsFalse((bool)FeedbackTarget(Feedback(f),f.Extra)["eligible"]);
  f.Mutate=null;f.ExtraX=7000;f.Blocked=true;f.Step(51);Assert.IsFalse((bool)FeedbackTarget(Feedback(f),f.Extra)["eligible"]);f.Blocked=false;f.Step(52);Assert.IsTrue((bool)FeedbackTarget(Feedback(f),f.Extra)["eligible"]);
 }
 [Test] public void FeedbackKeepsProtectionSeparateAndNeverAddsUnselectedEntrants(){
  var f=new Fixture();f.ExtraX=9000;f.Request();f.Through(0,48);f.ExtraX=7000;f.Mutate=o=>o["targets"][0]["protected"]=true;f.Step(49);
  var rows=Feedback(f);Assert.AreEqual(1,rows[0]["targets"].Count());Assert.IsTrue((bool)FeedbackTarget(rows,f.Primary)["eligible"]);Assert.IsTrue((bool)FeedbackTarget(rows,f.Primary)["protected"]);
 }
 [TestCase(false)][TestCase(true)] public void FeedbackDoesNotBorrowReplacementOrMissingTargetProtection(bool missing){
  var f=new Fixture();f.Request();f.Through(0,48);f.Mutate=o=>{if(missing)((JArray)o["targets"]).RemoveAt(0);else{o["targets"][0]["generation"]=2;o["targets"][0]["protected"]=true;}};f.Step(49);
  var target=FeedbackTarget(Feedback(f),f.Primary);Assert.AreEqual(1,(long)target["generation"]);Assert.IsFalse((bool)target["eligible"]);Assert.AreEqual(JTokenType.Null,target["protected"].Type);
 }
 [Test] public void FeedbackSurvivesCasterDownButReadsTargetDeathAndResolvedZeroDamageProgress(){
  var f=new Fixture();f.Request();f.Through(0,48);f.Health.ApplyResolvedDamage("down-source","op_reina",10000,48);f.Health.ApplyResolvedDamage("down-target",f.Primary,10000,48);
  Assert.AreEqual(1,Feedback(f).Count);Assert.IsFalse((bool)FeedbackTarget(Feedback(f),f.Primary)["eligible"]);f.ExtraX=9000;f.Through(49,131);
  Assert.AreEqual(0,f.Damage.Count());Assert.AreEqual(1,(int)Feedback(f)[0]["nextPulse"]);f.Through(132,272);Assert.IsEmpty(Feedback(f));
 }
 [TestCase("frame")][TestCase("generation")][TestCase("retire")]public void FeedbackClearsOnlyWhenOriginalAreaOrOwnerEnds(string cause){
  var f=new Fixture();f.Request();f.Through(0,48);if(cause=="retire")Call(f.Execution,"Retire");else{if(cause=="frame")f.FrameAlive=false;else f.FrameGeneration=2;f.Step(49);}Assert.IsEmpty(Feedback(f));
 }
 [Test] public void EmptyResolvedPulsesStillPublishOnceButFrameCancellationDoesNot(){var f=new Fixture();f.Request();f.Through(0,48);f.PrimaryX=50000;f.ExtraX=60000;f.Through(49,131);Assert.AreEqual(0,f.Damage.Count());Assert.AreEqual(1,f.Results.Count(r=>(string)r["kind"]=="IMPACT"));f.Step(131);Assert.AreEqual(1,f.Results.Count(r=>(string)r["kind"]=="IMPACT"));f.FrameAlive=false;f.Through(132,272);Assert.AreEqual(1,f.Results.Count(r=>(string)r["kind"]=="IMPACT"));}
 [Test] public void FrameLossReportsOneCancellationWithoutResolvingRemainingPulses(){var f=new Fixture();f.Request();f.Through(0,131);f.FrameAlive=false;f.Through(132,272);var cancelled=f.Results.Where(r=>(string)r["kind"]=="CANCEL").ToArray();Assert.AreEqual(1,cancelled.Length);Assert.AreEqual("FRAME_LOST",(string)cancelled[0]["reason"]);Assert.AreEqual(1,(int)cancelled[0]["resolvedPulses"]);Assert.AreEqual(3,(int)cancelled[0]["cancelledPulses"]);Assert.AreEqual(1,f.Results.Count(r=>(string)r["kind"]=="IMPACT"));Assert.AreEqual(2,f.Damage.Count());}
 [Test] public void ImpactPresentationUsesCurrentOriginalFrameWithFrozenLocalCenter(){var f=new Fixture();f.Request();f.Through(0,48);f.Mutate=o=>{o["frame"]["originMm"]=new JArray(50000,0,0);foreach(var t in (JArray)o["targets"])t["worldAnchorMm"][0]=(long)t["localAnchorMm"][0]+50000;};f.Through(49,131);var pulse=f.Results.Single(r=>(string)r["kind"]=="IMPACT");Assert.AreEqual(50000,(long)pulse["frame"]["originMm"][0]);Assert.AreEqual(5000,(long)pulse["centerMm"][0]);}
 [Test] public void CommitAndFourPulsesUseOriginalSeventeenMillisecondStepsAndOneCost(){
  var f=new Fixture();int p=f.Health.Hp(f.Primary),e=f.Health.Hp(f.Extra);Assert.AreEqual("ACCEPTED",f.Request());f.Through(0,47);Assert.AreEqual(0,f.Captures);Assert.AreEqual(0,f.Results.Count);
  f.Step(48);Assert.AreEqual(1,f.Captures);Assert.AreEqual("COMMIT",(string)f.Results.Single()["kind"]);Assert.AreEqual(100,(int)f.Results.Single()["cost"]);
  f.AttackValue=1000;f.Through(49,130);Assert.AreEqual(p,f.Health.Hp(f.Primary));
  f.Step(131);Assert.AreEqual(p-25,f.Health.Hp(f.Primary));Assert.AreEqual(e-7,f.Health.Hp(f.Extra));f.Step(131);Assert.AreEqual(2,f.Damage.Count());
  f.Through(132,272);Assert.AreEqual(8,f.Damage.Count());Assert.AreEqual(p-100,f.Health.Hp(f.Primary));Assert.AreEqual(e-28,f.Health.Hp(f.Extra));
  CollectionAssert.AreEqual(new long[]{131,178,225,272},f.Damage.Where(r=>(string)r["targetId"]==f.Primary).Select(r=>(long)r["receipt"]["tick"]));
  Assert.AreEqual(1,f.Results.Count(r=>(string)r["kind"]=="COMMIT"));
 }
 [TestCase("source")][TestCase("generation")][TestCase("range")][TestCase("los")][TestCase("charge")]
 public void BeforeCommitFailureReleasesReservationWithoutCostOrDamage(string change){var f=new Fixture();f.Request();f.Through(0,47);if(change=="source")f.Health.ApplyResolvedDamage("source-down","op_reina",10000,47);if(change=="generation")f.TargetGeneration=2;if(change=="range")f.PrimaryX=24001;if(change=="los")f.Los=false;f.Step(48,change=="charge"?0:100);f.Through(49,272);Assert.AreEqual(0,f.Results.Count);Assert.AreEqual(0,f.Captures);}
 [Test] public void PreparationLocksOriginalPrimaryWithoutRetargeting(){var f=new Fixture();f.Request();f.Step(0);f.Selected=f.Extra;f.Through(1,131);Assert.AreEqual(f.Primary,(string)f.Damage.First()["targetId"]);}
 [Test] public void CommittedAttackSurvivesSourceDownAndDoesNotRecheckMuzzleRange(){var f=new Fixture();f.Request();f.Through(0,48);f.Health.ApplyResolvedDamage("source-down","op_reina",10000,48);f.Los=false;f.Through(49,131);Assert.AreEqual(2,f.Damage.Count());}
 [Test] public void FixedSetCanExitAndReenterWithoutRedistribution(){var f=new Fixture();f.Request();f.Through(0,48);f.ExtraX=8001;f.Through(49,131);Assert.AreEqual(1,f.Damage.Count());f.ExtraX=8000;f.Through(132,178);Assert.AreEqual(3,f.Damage.Count());}
 [Test] public void CenterOccluderBlocksExtraOnlyForThatPulse(){var f=new Fixture();f.Request();f.Through(0,48);f.Blocked=true;f.Through(49,131);Assert.AreEqual(1,f.Damage.Count());f.Blocked=false;f.Through(132,178);Assert.AreEqual(3,f.Damage.Count());}
 [TestCase(false)][TestCase(true)] public void FrameLossCancelsRemainingWithoutRefund(bool generation){var f=new Fixture();f.Request();f.Through(0,131);if(generation)f.FrameGeneration=2;else f.FrameAlive=false;f.Through(132,272);Assert.AreEqual(2,f.Damage.Count());Assert.AreEqual(1,f.Results.Count(x=>(string)x["kind"]=="COMMIT"));}
 [Test] public void PrimaryDeathDoesNotCancelAdditionalTarget(){var f=new Fixture();f.Request();f.Through(0,48);f.Health.ApplyResolvedDamage("primary-down",f.Primary,10000,48);f.Through(49,131);Assert.AreEqual(f.Extra,(string)f.Damage.Single()["targetId"]);}
 [Test] public void DuplicatesAndReservedInputCannotConsumeAgain(){var f=new Fixture();Assert.AreEqual("ACCEPTED",f.Request());Assert.AreEqual("DUPLICATE",f.Request());Assert.AreEqual("BUSY",f.Request("second"));f.Through(0,272);Assert.AreEqual("DUPLICATE",(string)Call(f.Execution,"Request","cast-one",272L,100));}
 [Test] public void MissingAttackAndForeignAttemptFailClosed(){var f=new Fixture();f.Request();f.Through(0,47);f.MissingAttack=true;Assert.Throws<ArgumentException>(()=>f.Step(48));Assert.AreEqual(0,f.Results.Count);var other=new Fixture();other.AttemptOffset=1;Assert.Throws<ArgumentException>(()=>other.Request());}
 [TestCase(false)][TestCase(true)] public void CloneRetainsOwnedScheduleAndCannotMutateOriginalHp(bool committed){var f=new Fixture();f.Request();long at=committed?48:20;f.Through(0,at);var before=f.Capture();var health=(BCombatLocalModel)Call(f.Health,"CloneOwnedNode");var copy=Call(f.Execution,"CloneOwned",health);Assert.IsTrue(JToken.DeepEquals(before,(JObject)Call(copy,"Capture")));for(long t=at+1;t<=131;t++)Call(copy,"Step",t,100);Assert.Less(health.Hp(f.Primary),f.Health.Hp(f.Primary));Assert.IsTrue(JToken.DeepEquals(before,f.Capture()));Assert.Throws<ArgumentException>(()=>Call(f.Execution,"CloneOwned",f.Health));}
 [Test] public void RetireDiscardsScheduleAndRejectsLateInput(){var f=new Fixture();f.Request();f.Through(0,48);Call(f.Execution,"Retire");f.Through(49,272);Assert.AreEqual(0,f.Damage.Count());Assert.AreEqual("RETIRED",(string)Call(f.Execution,"Request","late",272L,100));}
 [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
 public void PureCommitSelectionHasAtMostThreeExtrasWithoutInventingHpOwners(int count){
  var f=new Fixture();var observation=f.Observe("selection",0);var targets=(JArray)observation["targets"];while(targets.Count>1)targets.RemoveAt(1);
  for(int i=1;i<count;i++){var t=(JObject)targets[0].DeepClone();t["id"]="selection-only-"+i;t["battleOrder"]=count-i;t["localAnchorMm"]=new JArray(6000,500,0);t["worldAnchorMm"]=new JArray(6000,500,0);targets.Add(t);}
  var select=f.Execution.GetType().GetMethod("SelectCommitTargets",BindingFlags.Static|BindingFlags.NonPublic);
  var selected=(JArray)select.Invoke(null,new object[]{observation,f.Primary,(Func<string,bool>)(_=>true)});
  Assert.AreEqual(Math.Min(count,4),selected.Count);Assert.AreEqual(f.Primary,(string)selected[0]["id"]);
  if(count>1)Assert.AreEqual("selection-only-"+(count-1),(string)selected[1]["id"]);
 }
 [TestCase("scale")][TestCase("reflection")][TestCase("world")][TestCase("missing")]
 public void InvalidOrMissingOriginalGeometryFailsClosed(string mode){var f=new Fixture();f.Mutate=o=>{if(mode=="scale")o["frame"]["basis"][0][0]=2;if(mode=="reflection")o["frame"]["basis"][0][0]=-1;if(mode=="world")o["targets"][0]["worldAnchorMm"][0]=5001;if(mode=="missing")((JObject)o["targets"][0]).Remove("generation");};Assert.Throws<ArgumentException>(()=>f.Request());}
 [TestCase(24000,1,"ACCEPTED")][TestCase(24001,1,"NO_TARGET")][TestCase(24000,2,"NO_TARGET")]
 public void CastRangeAndAdjacentCarBoundaries(long range,int delta,string status){var f=new Fixture();f.PrimaryX=range;f.Mutate=o=>o["targets"][0]["carDelta"]=delta;Assert.AreEqual(status,f.Request());}
 [Test] public void FrameTranslationMovesAreaWithoutFollowingOriginalTarget(){var f=new Fixture();f.Request();f.Through(0,48);f.Mutate=o=>{o["frame"]["originMm"]=new JArray(50000,0,0);foreach(var t in (JArray)o["targets"])t["worldAnchorMm"][0]=(long)t["localAnchorMm"][0]+50000;};f.Through(49,131);Assert.AreEqual(2,f.Damage.Count());}
 [Test] public void RecoveryReleasesInputAfterThreeHundredMillisecondsWhileEffectsRemain(){var f=new Fixture();f.Request();f.Through(0,65);Assert.AreEqual("RECOVERY",(string)((JObject)Call(f.Execution,"ReadActor","op_reina"))["phase"]);f.Step(66);Assert.AreEqual("IDLE",(string)((JObject)Call(f.Execution,"ReadActor","op_reina"))["phase"]);Assert.IsTrue((bool)f.Execution.GetType().GetProperty("HasPending",Hidden).GetValue(f.Execution));}
 [Test] public void PrimaryMayChangePhysicalFrameBeforeCommitThenFreezeNewFrame(){var f=new Fixture();f.Request();f.Through(0,47);f.FrameGeneration=2;f.Mutate=o=>{o["frame"]["id"]="new-explicit-frame";foreach(var t in (JArray)o["targets"])t["frameId"]="new-explicit-frame";};f.Through(48,131);Assert.AreEqual(1,f.Captures);Assert.AreEqual(2,f.Damage.Count());}
 [Test] public void SeveralRoundedPulsesDueOnOneLogicalTickAreNotDeferred(){var f=new Fixture(10000000);f.Request();f.Through(0,5);Assert.AreEqual(8,f.Damage.Count());CollectionAssert.AreEqual(new long[]{3,4,4,5},f.Damage.Where(r=>(string)r["targetId"]==f.Primary).Select(r=>(long)r["tick"]));}
 [Test] public void DefenseIsReadImmediatelyBeforeEachDamageAfterEarlierTargetEffects(){var f=new Fixture();int hp=f.Health.Hp(f.Primary);f.DefenseSource=(id,tick)=>{var d=Defense();if(id==f.Extra&&f.Health.Hp(f.Primary)<hp)d["defense"]=new JObject{["numerator"]="1000000",["denominator"]="1"};return d;};f.Request();f.Through(0,131);Assert.AreEqual(0,(int)f.Damage.Single(r=>(string)r["targetId"]==f.Extra)["receipt"]["hpLoss"]);}
 [TestCase(false)][TestCase(true)] public void ExplicitCancelReleasesOnlyUncommittedAction(bool committed){var f=new Fixture();f.Request();f.Through(0,committed?48:47);Assert.AreEqual(!committed,(bool)Call(f.Execution,"Cancel","cast-one"));f.Through(committed?49:48,272);Assert.AreEqual(committed?8:0,f.Damage.Count());Assert.AreEqual(committed?1:0,f.Results.Count(r=>(string)r["kind"]=="COMMIT"));}
 [Test] public void CurrentExplicitProtectionPreventsBothHpLossAndOwnedLifesteal(){var f=new Fixture();f.Proof.Owned=true;f.Request();f.Through(0,48);int hp=f.Health.Hp("op_reina");f.Mutate=o=>{foreach(var t in (JArray)o["targets"])t["protected"]=true;};f.Through(49,131);Assert.AreEqual(2,f.Damage.Count());Assert.IsTrue(f.Damage.All(r=>(int)r["receipt"]["hpLoss"]==0&&(int)r["lifesteal"]["actualHpGain"]==0));Assert.AreEqual(hp,f.Health.Hp("op_reina"));}
 [Test] public void MissingProtectionEvidenceCannotDefaultToUnprotected(){var f=new Fixture();f.Mutate=o=>((JObject)o["targets"][0]).Remove("protected");Assert.Throws<ArgumentException>(()=>f.Request());}
 [Test] public void ExistingHpOwnerShieldAbsorbsPulseWithoutNewShieldLedger(){var f=new Fixture();int hp=f.Health.Hp(f.Primary);f.Health.ApplyResolvedShield("test-original-shield",f.Primary,new CombatShieldSource(f.Primary,"test-original-shield",0),25,200,0);f.Request();f.Through(0,131);var r=f.Damage.Single(x=>(string)x["targetId"]==f.Primary);Assert.AreEqual(25,(int)r["receipt"]["absorbed"]);Assert.AreEqual(0,(int)r["receipt"]["hpLoss"]);Assert.AreEqual(hp,f.Health.Hp(f.Primary));}
 [Test] public void EachConfirmedPulseTargetFeedsOriginalOwnedLifestealExactlyOnce(){var f=new Fixture();f.Proof.Owned=true;int hp=f.Health.Hp("op_reina");f.Request();Assert.IsTrue((bool)Call(f.Execution,"IsPreparing","cast-one"));f.Through(0,48);Assert.IsFalse((bool)Call(f.Execution,"IsPreparing","cast-one"));f.Through(49,131);var main=f.Damage.Single(r=>(string)r["targetId"]==f.Primary);var extra=f.Damage.Single(r=>(string)r["targetId"]==f.Extra);Assert.AreEqual(25,(int)main["receipt"]["hpLoss"]);Assert.AreEqual(3,(int)main["lifesteal"]["actualHpGain"]);Assert.AreEqual(7,(int)extra["receipt"]["hpLoss"]);Assert.AreEqual(0,(int)extra["lifesteal"]["actualHpGain"]);Assert.AreEqual(hp+3,f.Health.Hp("op_reina"));f.Step(131);Assert.AreEqual(hp+3,f.Health.Hp("op_reina"));}
 [Test] public void EveryTargetAndPulseRetainsTheOriginalCommandSequence(){var f=new Fixture();f.Request();f.Through(0,272);CollectionAssert.AreEqual(new long[]{1},f.Damage.Select(r=>(long)r["globalSequence"]).Distinct());}
 [Test] public void HostSequenceIsBoundToOriginalCommandWithoutRewritingNativeEventId(){var f=new Fixture();f.Request();var emitted=new List<JObject>();Func<string,long> order=command=>{Assert.AreEqual("cast-one",command);return 57;};for(long tick=0;tick<=272;tick++)emitted.AddRange((IReadOnlyList<JObject>)Call(f.Execution,"Step",tick,100,order));var damage=emitted.Where(r=>(string)r["kind"]=="DAMAGE").ToArray();Assert.AreEqual(8,damage.Length);Assert.IsTrue(damage.All(r=>(long)r["globalSequence"]==57));Assert.AreEqual(8,damage.Select(r=>(string)r["eventId"]).Distinct().Count());var ordinary=new Fixture();ordinary.Request();ordinary.Through(0,272);CollectionAssert.AreEqual(ordinary.Damage.Select(r=>(string)r["eventId"]),damage.Select(r=>(string)r["eventId"]));}
 [Test] public void CloneAfterFirstPulsePreservesRecoveryDecisionsAndFutureSchedule(){var f=new Fixture();f.Proof.Owned=true;f.Request();f.Through(0,131);var before=f.Capture();var health=(BCombatLocalModel)Call(f.Health,"CloneOwnedNode");var copy=Call(f.Execution,"CloneOwned",health);Assert.IsTrue(JToken.DeepEquals(before,(JObject)Call(copy,"Capture")));int hp=f.Health.Hp(f.Primary);for(long t=132;t<=178;t++)Call(copy,"Step",t,100);Assert.AreEqual(hp-25,health.Hp(f.Primary));Assert.AreEqual(hp,f.Health.Hp(f.Primary));Assert.IsTrue(JToken.DeepEquals(before,f.Capture()));}
}
