using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryReinaBarrageHostTests {
 [Test] public void SceneTransactionSnapshotMatchesExportAndRemainsIndependent(){
  var driver=Driver();driver.Advance(170000L*10,10);
  var snapshot=(JObject)Call(Field(driver,"current"),"CaptureSceneState");
  Assert.IsTrue(JToken.DeepEquals(JObject.Parse(driver.StateJson),snapshot));var before=snapshot.DeepClone();
  driver.Advance(170000L*10,10);Assert.IsTrue(JToken.DeepEquals(before,snapshot));driver.Retire();
 }
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Field(object owner,string key)=>owner.GetType().GetField(key,Hidden).GetValue(owner);
 static object Call(object owner,string method,params object[] args){try{var m=owner.GetType().GetMethod(method,Hidden);Assert.NotNull(m,"Missing new-definition native host binding");return m.Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static void Pin(JObject row,string name){row.Remove(name);row[name]=BDirectAttackInput.Digest(row).Substring(7);}
 internal static OrdinaryFiveCombatFactory NewFactory(string geometryDefinitionJson=null){
  var f=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
  var raw=OrdinaryReinaBarrageSourceTests.NewStart(FirstPlayableScalingTests.Fixture(staticSource:true).Start);JObject geometryReference=null;
  if(geometryDefinitionJson!=null){var definition=JObject.Parse(geometryDefinitionJson);using var sha=System.Security.Cryptography.SHA256.Create();geometryReference=new JObject{["version"]=definition["version"].DeepClone(),["definitionId"]=definition["definitionId"].DeepClone(),["definitionHash"]=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(geometryDefinitionJson))).Replace("-","").ToLowerInvariant()};raw["ordinaryGeometryReference"]=geometryReference;}
  var start=OrdinaryFiveStartInput.Read(raw,f.Start.AccountId,f.Start.RunId);
  var route=f.Command.Binding.Capture();route["startSnapshotHash"]=start.StartSnapshotHash;Pin(route,"packetHash");var binding=BRouteBinding.Read(route.ToString(),(string)route["packetHash"],start.AccountId,start.RunId);
  var command=BRouteCommand.FromOriginal(binding,(JObject)f.Command.Capture()["command"]);
  var scale=(JObject)f.Response["receipt"]["firstPlayableScaling"];scale["startSnapshotHash"]=start.StartSnapshotHash;Pin(scale,"bindingHash");
  var entry=(JObject)f.Response["receipt"]["nodeStart"];var snapshot=entry["snapshot"];snapshot["startSnapshotHash"]=start.StartSnapshotHash;snapshot["definitions"]["kitHash"]=start.KitHash;snapshot["definitions"]["relocationHash"]=start.Relocation.RelocationHash;
  if(geometryReference!=null)snapshot["runtime"]["spatial"]["definitionBinding"]=new JObject{["version"]="ordinary.node-geometry-binding.v1.20261031",["accountId"]=entry["accountId"].DeepClone(),["runId"]=entry["runId"].DeepClone(),["nodeId"]=entry["nodeId"].DeepClone(),["entryMapRevision"]=entry["entryMapRevision"].DeepClone(),["originGeneration"]=1,["definitionRef"]=geometryReference};
  snapshot["runtime"]["enemies"]["scalingBindingHash"]=scale["bindingHash"].DeepClone();snapshot["runtime"]["actions"]["actors"].Single(a=>(string)a["operatorId"]=="op_reina")["ultimateCharge"]=100;
  entry["snapshotHash"]=BDirectAttackInput.Digest(snapshot).Substring(7);var result=BRouteCommandResult.Read(200,f.Response.ToString(),command);return OrdinaryFiveCombatFactory.FromNodeStart(start,result.NodeStart,result.Scaling,170000);
 }
 static CombatSceneDriver Driver(bool legacy=false){
  var f=Activator.CreateInstance(typeof(OrdinaryReinaBarrageExecutionTests).GetNestedType("Fixture",BindingFlags.NonPublic),Hidden,null,new object[]{170000L},null);
  var factory=legacy?(OrdinaryFiveCombatFactory)Field(f,"Factory"):NewFactory();var sources=Field(Field(f,"Execution"),"sources");var observe=(Func<string,long,JObject>)Field(sources,"Observe");
  sources=Activator.CreateInstance(sources.GetType(),Hidden,null,new object[]{(Func<string,long,JObject>)((command,tick)=>{var o=observe(command,tick);o["nodeId"]=factory.Attempt.NodeId;o["snapshotHash"]=factory.Attempt.SnapshotHash;o["attemptGeneration"]=factory.Attempt.Generation;return o;}),Field(sources,"CaptureAttack"),Field(sources,"CaptureDefense"),Field(sources,"Lifesteal")},null);
  var bound=(OrdinaryFiveCombatFactory)Call(factory,"WithReinaBarrageSources",sources);var driver=CombatSceneDriver.ForOrdinaryNode(bound,()=>true);driver.Open();driver.SetUltimateAuto(false);return driver;
 }
 static int Charge(CombatSceneDriver d)=>(int)JObject.Parse(d.StateJson)["charge"]["op_reina"];
 [Test] public void CommitPublishesAtSafeTickAndOwnedHoldPreservesMenuPause(){
  var d=Driver();IDisposable first=null,second=null;int commits=0,impacts=0;
  var signal=typeof(CombatSceneDriver).GetEvent("BarragePresentationEvent");Assert.NotNull(signal,"Missing committed presentation boundary");
  Action<JObject> receive=row=>{if((string)row["kind"]=="COMMIT"){commits++;Assert.AreEqual(48,d.Tick);Assert.AreEqual(0,Charge(d));first=(IDisposable)Call(d,"AcquirePresentationHold");second=(IDisposable)Call(d,"AcquirePresentationHold");}else if((string)row["kind"]=="IMPACT")impacts++;};signal.AddEventHandler(d,receive);
  d.RequestSkill("cast","op_reina","op_reina.ultimate");d.Advance(170000L*272,272);Assert.AreEqual(48,d.Tick);Assert.AreEqual(1,commits);Assert.AreEqual(0,impacts);
  string held=d.StateJson;d.Advance(170000L*500,500);Assert.AreEqual(held,d.StateJson);Assert.Throws<InvalidOperationException>(()=>d.RequestSkill("other","op_reina","op_reina.ultimate"));
  first.Dispose();d.Advance(0,300);Assert.AreEqual(48,d.Tick);d.SetPaused(true);second.Dispose();d.Advance(0,300);Assert.AreEqual(48,d.Tick);d.SetPaused(false);d.Advance(0,224);Assert.AreEqual(4,impacts);Assert.AreEqual(272,d.Tick);d.Retire();first.Dispose();second.Dispose();
 }
 [Test] public void LegacyRunCannotInstallNewBarrageOwner(){Assert.Throws<ArgumentException>(()=>Driver(true));}
 [Test] public void PresentationFailureReleasesOnlyItsHoldAndDoesNotInvalidateCommittedCombat(){
  var d=Driver();var signal=typeof(CombatSceneDriver).GetEvent("BarragePresentationEvent");int second=0;IDisposable good=null;
  signal.AddEventHandler(d,(Action<JObject>)(r=>{if((string)r["kind"]!="COMMIT")return;Call(d,"AcquirePresentationHold");throw new InvalidOperationException("synthetic playback failure");}));
  signal.AddEventHandler(d,(Action<JObject>)(r=>{if((string)r["kind"]!="COMMIT")return;second++;Assert.AreEqual(48,d.Tick);good=(IDisposable)Call(d,"AcquirePresentationHold");}));
  d.RequestSkill("cast","op_reina","op_reina.ultimate");Assert.DoesNotThrow(()=>d.Advance(170000L*48,48));Assert.IsFalse(d.RecoveryRequired);Assert.AreEqual(1,second);Assert.NotNull(typeof(CombatSceneDriver).GetProperty("LastPresentationFailure").GetValue(d));good.Dispose();d.Advance(170000L*224,224);Assert.AreEqual(272,d.Tick);d.Retire();
 }
 [Test] public void PresentationCallbackCannotAdvanceOrMutateThePublishedCommit(){
  var d=Driver();int next=0;var signal=typeof(CombatSceneDriver).GetEvent("BarragePresentationEvent");
  signal.AddEventHandler(d,(Action<JObject>)(r=>{if((string)r["kind"]!="COMMIT")return;Assert.Throws<InvalidOperationException>(()=>d.Advance(170000,1));Assert.Throws<InvalidOperationException>(()=>d.SetPaused(true));}));
  signal.AddEventHandler(d,(Action<JObject>)(r=>{if((string)r["kind"]=="COMMIT"){next++;Assert.AreEqual(48,d.Tick);}}));
  d.RequestSkill("cast","op_reina","op_reina.ultimate");d.Advance(170000L*48,48);Assert.AreEqual(1,next);Assert.AreEqual(48,d.Tick);Assert.IsFalse(d.RecoveryRequired);Assert.IsFalse((bool)d.HudState["paused"]);Assert.IsNull(typeof(CombatSceneDriver).GetProperty("LastPresentationFailure").GetValue(d));d.Retire();
 }
 [Test] public void TargetDamageCannotReplayLegacyUltimateExplosionForEveryVictim(){var d=Driver();d.RequestSkill("cast","op_reina","op_reina.ultimate");d.Advance(170000L*131,131);var hit=(JObject)d.HudState["effectResults"].First(r=>(string)r["abilityId"]=="op_reina.ultimate");var audio=CombatEffectAudio.LoadResources();Assert.IsFalse(audio.Find(hit,out _,out _));var old=(JObject)hit.DeepClone();old.Remove("reinaBarrage");Assert.IsTrue(audio.Find(old,out _,out _));d.Retire();}
 [Test] public void NativeHostConsumesOnlyAtCommitAndPublishesFourPulseReceipts(){var d=Driver();Assert.AreEqual("CASTING",d.RequestSkill("cast","op_reina","op_reina.ultimate"));Assert.AreEqual(100,Charge(d));d.Advance(170000L*47,47);Assert.AreEqual(100,Charge(d));d.Advance(170000,1);Assert.AreEqual(0,Charge(d));Assert.AreEqual("APPLIED",d.RequestSkill("cast","op_reina","op_reina.ultimate"));d.Advance(170000L*224,224);
  var hits=d.HudState["effectResults"].Where(r=>(string)r["abilityId"]=="op_reina.ultimate"&&(string)r["kind"]=="DAMAGE").ToArray();Assert.AreEqual(8,hits.Length);CollectionAssert.AreEquivalent(new[]{0,1,2,3},hits.Select(r=>(int)r["pulseIndex"]).Distinct());foreach(var row in hits){Assert.NotNull(row["healthReceipt"]);Assert.NotNull(row["lifesteal"]);Assert.AreEqual((int)row["healthReceipt"]["hpLoss"],(int)row["actualResult"]["hpLoss"]);}Assert.IsFalse((bool)d.HudState["unresolvedReinaBarrage"]);Assert.IsFalse(d.IsExecutionReady);d.Retire();}
 [Test] public void PauseAndOwnedCopiesPreserveCommittedScheduleAndRetirementClearsIt(){var d=Driver();d.RequestSkill("cast","op_reina","op_reina.ultimate");d.Advance(170000L*48,48);Assert.IsTrue((bool)d.HudState["unresolvedReinaBarrage"]);d.SetPaused(true);var paused=d.StateJson;d.Advance(170000L*300,300);Assert.AreEqual(paused,d.StateJson);d.SetPaused(false);d.Advance(170000L*83,83);Assert.AreEqual(2,d.HudState["effectResults"].Count(r=>(string)r["abilityId"]=="op_reina.ultimate"));d.Retire();Assert.IsNull(d.StateJson);}
}
