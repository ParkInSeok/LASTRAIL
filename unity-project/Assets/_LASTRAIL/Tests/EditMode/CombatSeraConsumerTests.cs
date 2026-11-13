#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class CombatSeraConsumerTests {
  static JObject Observed()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-sera-queue-observed-sql-test-20261001.json"));
  static JObject Input()=>(JObject)Observed()["input"];
  static BasicCombatSimulation Fresh(long step=500000)=>Create(Input(),step);
  static BasicCombatSimulation Create(JObject i,long step=500000)=>BasicCombatSimulation.FromTrustedSeraHealInputForLocalValidation(i,(string)i["inputHash"],"a",step);
  static string Id(string def)=> (string)Input()["entities"].Single(e=>(string)e["definitionId"]==def)["entityId"];
  static JObject Req(string kind,string eid,string aid,long ms,long seq,long? effect=null){
   var c=new JObject{["kind"]=kind,["eventId"]=eid,["actionId"]=aid,["atMs"]=ms};
   if(kind=="CAST"){c["sourceId"]=Id("op_sera");c["abilityId"]="op_sera.active";}
   if(kind=="OBSERVE_DAMAGE_HP"){c["actorId"]=Id("op_kyle");c["hp"]=100;}
   return new JObject{["command"]=c,["sequence"]=seq,["effectSequence"]=effect.HasValue?new JValue(effect.Value):JValue.CreateNull()};
  }
  static void OriginalSchedule(BasicCombatSimulation m){
   foreach(var o in Observed()["pendingCheckpoint"]["operations"].Where(o=>(string)o["kind"]=="SCHEDULE"))m.ScheduleResolvedSeraCommand((JObject)o["request"]);
  }
  static void Ready(BasicCombatSimulation m,long effect=3){m.ScheduleResolvedSeraCommand(Req("OBSERVE_DAMAGE_HP","d","d",0,1));m.ScheduleResolvedSeraCommand(Req("CAST","c","heal",0,2,effect));m.Advance(0,0);}
  static JObject State(BasicCombatSimulation m)=>JObject.Parse(m.CaptureLocalState());
  [Test] public void ActualNativeHpAndAutomaticDueReceiptMatchServerObserved(){
   var m=Fresh();OriginalSchedule(m);m.Advance(0,0);Assert.AreEqual(100,m.GetHp(Id("op_kyle")));
   Assert.IsTrue(JToken.DeepEquals(Observed()["accepted"],m.ReadSeraEmissions()));
   m.Advance(3990000,100);Assert.AreEqual(100,m.GetHp(Id("op_kyle")));var saved=m.CaptureLocalState();
   var restored=BasicCombatSimulation.RestoreLocalState(saved,()=>Fresh());m.Advance(10000,100);restored.Advance(10000,100);
   Assert.AreEqual(170,m.GetHp(Id("op_kyle")));Assert.AreEqual(m.CaptureLocalState(),restored.CaptureLocalState());
   Assert.IsTrue(JToken.DeepEquals(Observed()["effects"][0],m.ReadSeraEmissions().Last()));
  }
  [Test] public void NonAlignedDeadlineUsesOriginalMillisecondsAndSequence(){var m=Fresh();m.ScheduleResolvedSeraCommand(Req("OBSERVE_DAMAGE_HP","d","d",1,1));m.ScheduleResolvedSeraCommand(Req("CAST","c","h",17,2,3));m.Advance(4500000,20);Assert.AreEqual(170,m.GetHp(Id("op_kyle")));Assert.AreEqual(417,(long)m.ReadSeraEmissions().Last()["dueMs"]);Assert.AreEqual(3,(long)m.ReadSeraEmissions().Last()["sequence"]);}
  [Test] public void BigStepProcessesAcceptedEffectOnceAtOriginalDue(){var m=Fresh(10000000);Ready(m);m.Advance(50000000,10);Assert.AreEqual(170,m.GetHp(Id("op_kyle")));Assert.AreEqual(3,m.ReadSeraEmissions().Count);Assert.AreEqual(400,(long)m.ReadSeraEmissions().Last()["dueMs"]);m.Advance(50000000,10);Assert.AreEqual(3,m.ReadSeraEmissions().Count);}
  [Test] public void BudgetDebtAndRemainderSurvivePauseAndRestore(){var m=Fresh();Ready(m);m.Advance(3990001,0);Assert.AreEqual(100,m.GetHp(Id("op_kyle")));m.SetPaused(true);var paused=m.CaptureLocalState();m.Advance(10000000,100);Assert.AreEqual((long)JObject.Parse(paused)["clock"]["debt"],(long)State(m)["clock"]["debt"]);var r=BasicCombatSimulation.RestoreLocalState(m.CaptureLocalState(),()=>Fresh());m.SetPaused(false);r.SetPaused(false);m.Advance(9999,100);r.Advance(9999,100);Assert.AreEqual(170,m.GetHp(Id("op_kyle")));Assert.AreEqual(m.CaptureLocalState(),r.CaptureLocalState());}
  [Test] public void HalfSpeedRemainderIsExactAcrossRestore(){var m=Fresh();Ready(m);m.SetSpeed(3);m.Advance(2666666,100);Assert.AreEqual(100,m.GetHp(Id("op_kyle")));var r=BasicCombatSimulation.RestoreLocalState(m.CaptureLocalState(),()=>Fresh());m.Advance(1,100);r.Advance(1,100);Assert.AreEqual(170,m.GetHp(Id("op_kyle")));Assert.AreEqual(m.CaptureLocalState(),r.CaptureLocalState());}
  [Test] public void CancellationBeforeDueRemovesPendingWithoutRefund(){var m=Fresh();Ready(m);m.ScheduleResolvedSeraCommand(Req("CANCEL","x","heal",399,4));m.Advance(5000000,100);Assert.AreEqual(100,m.GetHp(Id("op_kyle")));Assert.AreEqual("CANCELED",(string)m.ReadSeraEmissions().Last()["receipt"]["status"]);m.ScheduleResolvedSeraCommand(Req("CAST","c2","h2",501,5,6));m.Advance(0,0);m.Advance(500000,10);Assert.AreEqual("SOURCE_NOT_READY",(string)m.ReadSeraEmissions().Last()["receipt"]["reason"]);}
  [TestCase("op_kyle")][TestCase("op_sera")]
  public void SameTickDownBeforeEffectCancels(string def){var m=Fresh();Ready(m,4);var d=Req("OBSERVE_DAMAGE_HP","down","down",400,3);d["command"]["actorId"]=Id(def);d["command"]["hp"]=0;m.ScheduleResolvedSeraCommand(d);m.Advance(4000000,100);Assert.AreEqual("CANCELED",(string)m.ReadSeraEmissions().Last()["receipt"]["status"]);Assert.AreEqual(def=="op_kyle"?0:100,m.GetHp(Id("op_kyle")));}
  [Test] public void SameTickEffectBeforeDownPreservesAppliedReceipt(){var m=Fresh();Ready(m);var d=Req("OBSERVE_DAMAGE_HP","down","down",400,4);d["command"]["hp"]=0;m.ScheduleResolvedSeraCommand(d);m.Advance(4000000,100);Assert.AreEqual("APPLIED",(string)m.ReadSeraEmissions()[2]["receipt"]["status"]);Assert.AreEqual(170,(int)m.ReadSeraEmissions()[2]["receipt"]["afterHp"]);Assert.AreEqual(0,m.GetHp(Id("op_kyle")));}
  [Test] public void DuplicateRetryIsOriginalReceiptAndNoSecondEffect(){var m=Fresh();var r=Req("CAST","c","h",0,1,2);m.ScheduleResolvedSeraCommand(r);Assert.IsTrue((bool)m.ScheduleResolvedSeraCommand(r)["replayed"]);m.Advance(0,0);Assert.AreEqual(1,m.ReadSeraEmissions().Count);r["command"]["actionId"]="changed";var before=m.CaptureLocalState();Assert.Throws<InvalidOperationException>(()=>m.ScheduleResolvedSeraCommand(r));Assert.AreEqual(before,m.CaptureLocalState());}
  [Test] public void NoEligibleTargetConsumesNoCooldown(){var m=Fresh();m.ScheduleResolvedSeraCommand(Req("CAST","c","h",0,1,2));m.Advance(0,0);Assert.AreEqual("NO_ELIGIBLE_TARGET",(string)m.ReadSeraEmissions()[0]["receipt"]["reason"]);m.ScheduleResolvedSeraCommand(Req("OBSERVE_DAMAGE_HP","d","d",1,3));m.ScheduleResolvedSeraCommand(Req("CAST","c2","h2",1,4,5));m.Advance(500000,10);Assert.AreEqual("ACCEPTED",(string)m.ReadSeraEmissions().Last()["receipt"]["status"]);}
  [Test] public void SelfHealAndOverhealUseActualNativeCap(){var m=Fresh();var d=Req("OBSERVE_DAMAGE_HP","d","d",0,1);d["command"]["actorId"]=Id("op_sera");d["command"]["hp"]=919;m.ScheduleResolvedSeraCommand(d);m.ScheduleResolvedSeraCommand(Req("CAST","c","h",0,2,3));m.Advance(4000000,100);Assert.AreEqual(920,m.GetHp(Id("op_sera")));Assert.AreEqual(1,(int)m.ReadSeraEmissions().Last()["receipt"]["actualAmount"]);Assert.AreEqual(70,(int)m.ReadSeraEmissions().Last()["receipt"]["attemptedAmount"]);}
  [Test] public void FailedDrainPublishesNoPartialHpClockOrReceipt(){var m=Fresh();m.ScheduleResolvedSeraCommand(Req("OBSERVE_DAMAGE_HP","d","d",0,1));var bad=Req("CAST","c","h",0,2,3);bad["command"]["sourceId"]="FORGED";m.ScheduleResolvedSeraCommand(bad);var before=m.CaptureLocalState();Assert.Throws<InvalidOperationException>(()=>m.Advance(1000000,10));Assert.AreEqual(before,m.CaptureLocalState());Assert.AreEqual(1400,m.GetHp(Id("op_kyle")));}
  [Test] public void ClosedTickAndReservedSequenceRejectWithoutMutation(){var m=Fresh();Ready(m);var before=m.CaptureLocalState();Assert.Throws<InvalidOperationException>(()=>m.ScheduleResolvedSeraCommand(Req("CANCEL","x","heal",0,4)));Assert.Throws<InvalidOperationException>(()=>m.ScheduleResolvedSeraCommand(Req("CANCEL","y","heal",1,3)));Assert.AreEqual(before,m.CaptureLocalState());}
  [Test] public void ReservedEffectIdentityCannotBecomeCommand(){var m=Fresh();var c=Req("CAST","c","h",0,1,2);m.ScheduleResolvedSeraCommand(c);string effect="[\"queue-attempt-1\",\"h\",\"op_sera.active\",\"HEAL_ONE\"]";Assert.Throws<InvalidOperationException>(()=>m.ScheduleResolvedSeraCommand(Req("CANCEL",effect,"h",1,3)));}
  [Test] public void ManualResolveCannotBypassAutomaticDue(){var m=Fresh();Assert.Throws<ArgumentException>(()=>m.ScheduleResolvedSeraCommand(Req("RESOLVE","x","h",0,1)));}
  [TestCase("actors")][TestCase("commands")][TestCase("input")][TestCase("clock")]
  public void TamperedSnapshotRejects(string part){var m=Fresh();Ready(m);var s=State(m);if(part=="actors")s[part][0]["vitals"]["hp"]=1399;else if(part=="commands")s[part]["pending"][0]["sequence"]=99;else if(part=="input")s[part]["scope"]["simulationId"]="changed";else s[part]["debt"]=1;Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(s.ToString(),()=>Fresh()));}
  [Test] public void OriginalOwnerAndAuthorityAreNotSelfDeclared(){var input=Input();Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.FromTrustedSeraHealInputForLocalValidation(input,(string)input["inputHash"],"other",500000));input["authority"]["combatOwner"]="SERVER";Assert.Throws<ArgumentException>(()=>Create(input));}
  // Stage via the real domain Schedule boundary, then exercise the model's transaction publication boundary.
  // Contract-derived host packet; the real SQL observed sample remains SOLO, not a deployed coop claim.
  [Test] public void HostLogicalOwnerUsesSameNativeEffectAndPendingRestore(){
   var input=Input();input["scope"]["mode"]="ORDINARY_COOP";input["authority"]["mode"]="ORDINARY_COOP";
   input["authority"]["combatOwner"]="UNITY_HOST";input["authority"]["combatClock"]="HOST_LOGICAL";
   var body=(JObject)input.DeepClone();body.Remove("inputHash");var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
   var canonical=typeof(BasicCombatSimulation).GetMethod("StableJson",flags).Invoke(null,new object[]{body});
   input["inputHash"]="sha256:"+typeof(BasicCombatSimulation).GetMethod("Digest",flags).Invoke(null,new[]{canonical});
   var m=Create(input);Ready(m);m.Advance(3990000,100);
   var r=BasicCombatSimulation.RestoreLocalState(m.CaptureLocalState(),()=>Create(input));m.Advance(10000,100);r.Advance(10000,100);
   Assert.AreEqual(170,r.GetHp(Id("op_kyle")));Assert.AreEqual(m.CaptureLocalState(),r.CaptureLocalState());
   input["authority"]["combatClock"]="LOCAL_LOGICAL";Assert.Throws<ArgumentException>(()=>Create(input));
  }
  [Test] public void OversizedPendingCheckpointCannotBePublished(){
   var owner=Fresh();var candidate=Fresh();var before=owner.CaptureLocalState();
   var commands=typeof(BasicCombatSimulation).GetField("seraCommands",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(candidate);
   var schedule=commands.GetType().GetMethod("Schedule",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   for(int i=0;i<6000;i++)schedule.Invoke(commands,new object[]{Req("OBSERVE_DAMAGE_HP",i.ToString("D5").PadRight(400,'x'),i.ToString("D5").PadRight(100,'a'),100000, i+1)});
   Assert.Greater(candidate.CaptureLocalState().Length,8*1024*1024,"Valid pending commands exceed old restore limit");
   var adopt=typeof(BasicCombatSimulation).GetMethod("AdoptSera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   var e=Assert.Throws<System.Reflection.TargetInvocationException>(()=>adopt.Invoke(owner,new object[]{candidate}));
   Assert.IsInstanceOf<InvalidOperationException>(e.InnerException);Assert.AreEqual(before,owner.CaptureLocalState());
  }
  [Test] public void ValidationSliceCannotBindHudOrUseUntrackedHpEffect(){var m=Fresh();Assert.Throws<InvalidOperationException>(()=>m.ReadHudState());Assert.Throws<InvalidOperationException>(()=>m.ApplyResolvedHeal(Id("op_kyle"),10));}
 }
}
#endif