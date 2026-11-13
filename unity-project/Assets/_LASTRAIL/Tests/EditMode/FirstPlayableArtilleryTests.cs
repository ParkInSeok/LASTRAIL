using System;
using System.IO;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class FirstPlayableArtilleryTests {
 sealed class Fixture {
  internal readonly CombatVitals Train=new CombatVitals(1000,10000,true),Launcher=new CombatVitals(1250,0,true);
  internal readonly JObject Frame,Numeric;
  internal readonly FirstPlayableArtillery Owner;
  internal Fixture(bool includeTrain=true,bool latest=false,bool wrongHazard=false){
   var f=FirstPlayableScalingTests.Fixture(5,includeTrain,latest);var start=OrdinaryFiveStartInput.Read(f.Start,"a",(string)f.Start["runId"]);var scale=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).Scaling;
   string trainId=start.Train?.TrainId??"historical-unbound-train";
   var definition=new JObject{["version"]=RailArtilleryHazard.ExactVersion,["runId"]=start.RunId,["eventId"]="fixture-shot",["trainId"]="fixture-train",["railSegmentId"]="fixture-rail",["carriageIds"]=new JArray("front","rear"),["startMm"]=10000,["endMm"]=16000,["previewAtMs"]=0,["timing"]=new JObject{["kind"]="SINGLE",["impactAtMs"]=8000},["damage"]=scale.ArtilleryRawDamage.Snapshot()};
   definition["trainId"]=trainId;
   if(scale.IssuedHazardId!=null&&!wrongHazard)definition["eventId"]=scale.IssuedHazardId;
   Owner=new FirstPlayableArtillery(scale,start,f.Init.ToString(),definition,trainId,Train,Launcher,50);
   Frame=new JObject{["runId"]=start.RunId,["trainId"]="fixture-train",["atMs"]=8000,["spans"]=new JArray(new JObject{["carriageId"]="front",["railSegmentId"]="fixture-rail",["startMm"]=14000,["endMm"]=20000},new JObject{["carriageId"]="rear",["railSegmentId"]="fixture-rail",["startMm"]=8000,["endMm"]=14000})};
   Frame["trainId"]=trainId;
   Numeric=new JObject{["version"]=CombatNumericDefenseBudget.Version,["base"]=scale.ArtilleryRawDamage.Snapshot(),["effects"]=new JArray(),["defense"]=new JObject{["numerator"]="1000",["denominator"]="1"},["weakening"]=new JArray(),["penetration"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
  }
 }
 [Test] public void HistoricalMissingTrainSourceCannotCreateNewArtillery(){Assert.Throws<ArgumentException>(()=>new Fixture(false));}
 [Test] public void ApprovedSourceUsesOriginalArrivalHazardIdentity(){var f=new Fixture(latest:true);f.Owner.CommitLaunch(150);Assert.IsTrue(f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false).Applied);Assert.Throws<ArgumentException>(()=>new Fixture(latest:true,wrongHazard:true));}
 [TestCase("hit")][TestCase("miss")][TestCase("cancel")][TestCase("fatal")]
 public void ApprovedEncounterObjectiveReadsOriginalHpAndResolvedSingleThreat(string outcome){
  var f=new Fixture();var first=new CombatVitals(240,0,true);var second=new CombatVitals(240,0,true);
  Assert.IsFalse(f.Owner.IsEncounterObjectiveSatisfied(first,second));first.Damage(240,0,false);
  Assert.IsFalse(f.Owner.IsEncounterObjectiveSatisfied(first,second));second.Damage(240,0,false);
  Assert.IsFalse(f.Owner.IsEncounterObjectiveSatisfied(first,second));
  if(outcome=="cancel"){f.Launcher.Damage(1250,149,false);f.Owner.ObserveLauncherDefeat(149);}
  else{if(outcome=="fatal")f.Train.Damage(980,0,false);f.Owner.CommitLaunch(150);Assert.IsFalse(f.Owner.IsEncounterObjectiveSatisfied(first,second));if(outcome=="miss")foreach(var span in (JArray)f.Frame["spans"])span["railSegmentId"]="other";f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false);}
  Assert.AreEqual(outcome!="fatal",f.Owner.IsEncounterObjectiveSatisfied(first,second));
  if(outcome=="hit"||outcome=="miss")Assert.AreEqual(1250,f.Launcher.Hp,"No automatic launcher destruction");
  var saved=f.Owner.Capture();var restored=new Fixture();restored.Owner.Restore(saved,BDirectAttackInput.Digest(saved),160);
  Assert.AreEqual(outcome!="fatal",restored.Owner.IsEncounterObjectiveSatisfied(first,second));Assert.IsTrue(JToken.DeepEquals(saved,restored.Owner.Capture()));
 }
 [Test] public void DestroyedLauncherDoesNotResolveLaunchedProjectileOrAliasGunners(){var f=new Fixture();var a=new CombatVitals(240,0,true);var b=new CombatVitals(240,0,true);a.Damage(240,0,false);b.Damage(240,0,false);f.Owner.CommitLaunch(150);f.Launcher.Damage(1250,150,false);f.Owner.ObserveLauncherDefeat(150);Assert.IsFalse(f.Owner.IsEncounterObjectiveSatisfied(a,b));Assert.Throws<ArgumentException>(()=>f.Owner.IsEncounterObjectiveSatisfied(a,a));Assert.Throws<ArgumentException>(()=>f.Owner.IsEncounterObjectiveSatisfied(a,f.Launcher));Assert.Throws<ArgumentException>(()=>f.Owner.IsEncounterObjectiveSatisfied(a,null));}
 [TestCase(149)][TestCase(150)] public void ActualDestructionBeforeLaunchCancelsWithoutReissue(int tick){var f=new Fixture();f.Launcher.Damage(1250,tick,false);f.Owner.ObserveLauncherDefeat(tick);f.Owner.CommitLaunch(150);Assert.IsTrue(f.Owner.ThreatResolved);Assert.IsFalse(f.Owner.WarningVisible);Assert.AreEqual(FirstPlayableArtilleryPhase.Cancelled,f.Owner.Phase);Assert.Throws<ArgumentException>(()=>f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false));Assert.AreEqual(1000,f.Train.Hp);}
 [Test] public void LaunchBeforeSameTickDestructionKeepsWarningAndOneShieldHit(){var f=new Fixture();f.Train.Shield(new CombatShieldSource("train","shield",0),100,200,0);f.Owner.CommitLaunch(150);f.Launcher.Damage(1250,150,false);f.Owner.ObserveLauncherDefeat(150);Assert.IsTrue(f.Owner.WarningVisible);Assert.IsFalse(f.Owner.ThreatResolved);var hit=f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false);Assert.IsTrue(hit.Applied);Assert.AreEqual("HIT",(string)hit.Result["collision"]["outcome"]);Assert.AreEqual(31,(int)hit.Result["absorbed"]);Assert.AreEqual(1000,f.Train.Hp);var after=f.Owner.Capture();Assert.IsFalse(f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false).Applied);Assert.IsTrue(JToken.DeepEquals(after,f.Owner.Capture()));Assert.IsFalse(f.Owner.WarningVisible);}
 [Test] public void RequestOrLateDestructionCannotCancelAnUnprocessedLaunch(){var f=new Fixture();Assert.Throws<ArgumentException>(()=>f.Owner.ObserveLauncherDefeat(149));f.Launcher.Damage(1250,151,false);Assert.Throws<ArgumentException>(()=>f.Owner.ObserveLauncherDefeat(151));Assert.Throws<ArgumentException>(()=>f.Owner.Capture());}
 [Test] public void ImpactValidationFailureLeavesHazardAndHpUncommitted(){var f=new Fixture();f.Owner.CommitLaunch(150);var before=f.Owner.Capture();var bad=(JObject)f.Numeric.DeepClone();bad["base"]["numerator"]="252";Assert.Throws<ArgumentException>(()=>f.Owner.ResolveImpact(f.Frame,bad,160,false));Assert.IsTrue(JToken.DeepEquals(before,f.Owner.Capture()));var hit=f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false);Assert.AreEqual(31,(int)hit.Result["hpLoss"]);Assert.AreEqual(969,f.Train.Hp);}
 [TestCase(true)][TestCase(false)] public void ExistingOrderedActionsDecideSameTickBoundary(bool launchFirst){
  var f=new Fixture();var hidden=System.Reflection.BindingFlags.NonPublic;var type=typeof(BasicCombatSimulation).GetNestedType("DueAction",hidden);var actions=Array.CreateInstance(type,2);
  object Action(long sequence,System.Action apply)=>Activator.CreateInstance(type,hidden|System.Reflection.BindingFlags.Instance,null,new object[]{150L,sequence,apply},null);
  actions.SetValue(Action(launchFirst?1:2,()=>f.Owner.CommitLaunch(150)),0);actions.SetValue(Action(launchFirst?2:1,()=>{f.Launcher.Damage(1250,150,false);f.Owner.ObserveLauncherDefeat(150);}),1);
  var ordered=(System.Collections.IEnumerable)typeof(BasicCombatSimulation).GetMethod("OrderDueActions",hidden|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{actions});
  foreach(var action in ordered)((System.Action)type.GetField("Execute",hidden|System.Reflection.BindingFlags.Instance).GetValue(action))();
  Assert.AreEqual(launchFirst?FirstPlayableArtilleryPhase.Launched:FirstPlayableArtilleryPhase.Cancelled,f.Owner.Phase);
 }
 [Test] public void PartialShieldAndInvalidRestorePreserveOneAuthoritativeHpOwner(){
  var f=new Fixture();f.Train.Shield(new CombatShieldSource("train","shield",0),13,200,0);f.Owner.CommitLaunch(150);var hit=f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false);Assert.AreEqual(13,(int)hit.Result["absorbed"]);Assert.AreEqual(18,(int)hit.Result["hpLoss"]);Assert.AreEqual(982,f.Train.Hp);
  var fresh=new Fixture();var before=fresh.Owner.Capture();var invalid=f.Owner.Capture();invalid["launcher"]["hp"]=-1;Assert.Throws<ArgumentException>(()=>fresh.Owner.Restore(invalid,BDirectAttackInput.Digest(invalid),160));Assert.IsTrue(JToken.DeepEquals(before,fresh.Owner.Capture()));
 }
 [TestCase("phase")][TestCase("impact")] public void InvalidSavedStateCannotBeSilentlyNormalized(string key){var f=new Fixture();f.Owner.CommitLaunch(150);var before=f.Owner.Capture();var saved=(JObject)before.DeepClone();saved[key]=key=="phase"?"2":"invalid";Assert.Throws<ArgumentException>(()=>f.Owner.Restore(saved,BDirectAttackInput.Digest(saved),150));Assert.IsTrue(JToken.DeepEquals(before,f.Owner.Capture()));}
 [Test] public void FatalImpactSignalsOnlyOnFirstCommitAndMissDoesNotDamage(){var f=new Fixture();f.Train.Damage(980,0,false);f.Owner.CommitLaunch(150);Assert.IsTrue(f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false).TrainDefeatedNow);Assert.IsFalse(f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false).TrainDefeatedNow);var miss=new Fixture();miss.Owner.CommitLaunch(150);foreach(var span in (JArray)miss.Frame["spans"])span["railSegmentId"]="other";var result=miss.Owner.ResolveImpact(miss.Frame,miss.Numeric,160,false);Assert.AreEqual("MISSED",(string)result.Result["collision"]["outcome"]);Assert.AreEqual(1000,miss.Train.Hp);Assert.IsTrue(miss.Owner.ThreatResolved);}
 [TestCase("preview",149)][TestCase("launched",150)][TestCase("resolved",160)][TestCase("cancelled",149)]
 public void FreshOwnerRestoresOriginalThreatAndCurrentHpWithoutMultiplying(string phase,int tick){
  var f=new Fixture();f.Train.Damage(123,0,false);if(phase=="cancelled"){f.Launcher.Damage(1250,149,false);f.Owner.ObserveLauncherDefeat(149);}if(phase=="launched"||phase=="resolved")f.Owner.CommitLaunch(150);if(phase=="resolved")f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false);
  var saved=f.Owner.Capture();var fresh=new Fixture();fresh.Owner.Restore(saved,BDirectAttackInput.Digest(saved),tick);Assert.IsTrue(JToken.DeepEquals(saved,fresh.Owner.Capture()));Assert.AreEqual(f.Train.Hp,fresh.Train.Hp);
  if(phase=="preview")fresh.Owner.CommitLaunch(150);if(phase!="cancelled"){var result=fresh.Owner.ResolveImpact(fresh.Frame,fresh.Numeric,160,false);Assert.AreEqual(phase!="resolved",result.Applied);Assert.AreEqual(846,fresh.Train.Hp);}
  var before=fresh.Owner.Capture();saved["train"]["hp"]=1000;Assert.Throws<ArgumentException>(()=>fresh.Owner.Restore(saved,"sha256:"+new string('0',64),160));Assert.IsTrue(JToken.DeepEquals(before,fresh.Owner.Capture()));
 }
 [TestCase(159,0)][TestCase(160,0)][TestCase(161,31)] public void ImpactUsesActualShieldExpiryBoundary(int expiry,int absorbed){var f=new Fixture();f.Train.Shield(new CombatShieldSource("train","shield",0),40,expiry,0);f.Owner.CommitLaunch(150);var hit=f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false);Assert.AreEqual(absorbed,(int)hit.Result["absorbed"]);Assert.AreEqual(1000-(31-absorbed),f.Train.Hp);}
 [TestCase(159)][TestCase(160)] public void ShieldCreatedAfterLaunchBeforeImpactParticipates(int effectTick){var f=new Fixture();f.Owner.CommitLaunch(150);f.Train.Shield(new CombatShieldSource("train","shield",0),40,200,effectTick);Assert.AreEqual(31,(int)f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false).Result["absorbed"]);Assert.AreEqual(9,f.Train.ShieldRemaining(160));}
 [Test] public void LateEffectDoesNotUndoAnAlreadyCommittedImpact(){var f=new Fixture();f.Owner.CommitLaunch(150);var hit=f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false);Assert.AreEqual(969,f.Train.Hp);f.Train.Shield(new CombatShieldSource("train","shield",0),40,200,160);Assert.IsFalse(f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false).Applied);Assert.AreEqual(969,f.Train.Hp);Assert.AreEqual(40,f.Train.ShieldRemaining(160));Assert.Throws<ArgumentOutOfRangeException>(()=>f.Train.Shield(new CombatShieldSource("train","late",0),40,200,159));}
 [Test] public void MissDoesNotConsumeShieldAndNaturalExpiryRemainsSeparate(){var f=new Fixture();f.Train.Shield(new CombatShieldSource("train","shield",0),40,159,0);f.Owner.CommitLaunch(150);foreach(var span in (JArray)f.Frame["spans"])span["railSegmentId"]="other";Assert.AreEqual(0,(int)f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false).Result["absorbed"]);Assert.AreEqual(40,(int)f.Owner.Capture()["train"]["layers"][0]["remaining"]);Assert.AreEqual(0,f.Train.ShieldRemaining(160));Assert.AreEqual(1000,f.Train.Hp);}
 [Test] public void DefenseIsReadAtImpactWithoutChangingLaunchedRaw(){var f=new Fixture();f.Owner.CommitLaunch(150);f.Numeric["defense"]["numerator"]="2000";var hit=f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false);Assert.AreEqual(21,(int)hit.Result["hpLoss"]);Assert.AreEqual("253",(string)hit.Result["collision"]["rawDamageIntent"]["numerator"]);Assert.AreEqual("4",(string)hit.Result["collision"]["rawDamageIntent"]["denominator"]);}
 const string ProcessFile="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/ArtilleryProcess779/component-checkpoints.json";
 [Test] public void WriteComponentCheckpointsForSeparateProcessVerification(){
  var rows=new JArray();foreach(string phase in new[]{"preview","launched","resolved","cancelled","shield-before","shield-after","shield-expired","miss-shield"}){
   var f=new Fixture();f.Train.Damage(123,0,false);long tick=149;
   if(phase=="cancelled"){f.Launcher.Damage(1250,tick,false);f.Owner.ObserveLauncherDefeat(tick);}
   bool shield=phase.StartsWith("shield-",StringComparison.Ordinal)||phase=="miss-shield";
   if(phase=="launched"||phase=="resolved"||shield){tick=150;f.Owner.CommitLaunch(tick);f.Launcher.Damage(1250,tick,false);f.Owner.ObserveLauncherDefeat(tick);}
   if(shield){tick=159;f.Train.Shield(new CombatShieldSource("train","shield",0),40,phase=="shield-expired"?160:200,tick);}
   if(phase=="miss-shield")foreach(var span in (JArray)f.Frame["spans"])span["railSegmentId"]="other";
   if(phase=="resolved"||phase=="shield-after"||phase=="miss-shield"){tick=160;f.Owner.ResolveImpact(f.Frame,f.Numeric,tick,false);}
   var state=f.Owner.Capture();rows.Add(new JObject{["phase"]=phase,["tick"]=tick,["state"]=state,["hash"]=BDirectAttackInput.Digest(state)});
  }
  Directory.CreateDirectory(Path.GetDirectoryName(ProcessFile));File.WriteAllText(ProcessFile,new JObject{["producerPid"]=System.Diagnostics.Process.GetCurrentProcess().Id,["classification"]="SYNTHETIC_COMPONENT_CHECKPOINT_NOT_PRODUCT_PLAYER",["cases"]=rows}.ToString());Assert.AreEqual(8,rows.Count);
 }
 [Test,Explicit("Run in a second Unity process after checkpoint production.")]
 public void ReadComponentCheckpointsInSeparateProcess(){
  if(!File.Exists(ProcessFile))Assert.Ignore("Run checkpoint production first.");var data=JObject.Parse(File.ReadAllText(ProcessFile));if((int)data["producerPid"]==System.Diagnostics.Process.GetCurrentProcess().Id)Assert.Ignore("Requires a second Unity process.");Assert.AreNotEqual((int)data["producerPid"],System.Diagnostics.Process.GetCurrentProcess().Id);
  foreach(var row in (JArray)data["cases"]){var f=new Fixture();var state=(JObject)row["state"];f.Owner.Restore(state,(string)row["hash"],(long)row["tick"]);Assert.IsTrue(JToken.DeepEquals(state,f.Owner.Capture()));string phase=(string)row["phase"];
   if(phase=="preview")f.Owner.CommitLaunch(150);
   if(phase=="miss-shield")foreach(var span in (JArray)f.Frame["spans"])span["railSegmentId"]="other";
   if(phase!="cancelled"){
    var hit=f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false);Assert.AreEqual(phase!="resolved"&&phase!="shield-after"&&phase!="miss-shield",hit.Applied);
    Assert.AreEqual(phase=="shield-before"||phase=="shield-after"||phase=="miss-shield"?877:846,f.Train.Hp);
    Assert.AreEqual(phase=="shield-before"||phase=="shield-after"?9:phase=="miss-shield"?40:0,f.Train.ShieldRemaining(160));
    Assert.IsFalse(f.Owner.ResolveImpact(f.Frame,f.Numeric,160,false).Applied);
   }else Assert.AreEqual(877,f.Train.Hp);
  }
 }
}
