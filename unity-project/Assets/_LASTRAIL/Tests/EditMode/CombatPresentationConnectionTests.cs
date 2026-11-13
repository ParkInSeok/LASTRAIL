using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatPresentationConnectionTests
{
 public static BasicCombatSimulation Fresh(){
  var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));
  d["ownedSelection"]["accountId"]="a";
  var s=BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1);
  var method=typeof(BasicCombatSimulation).GetMethod("ConfigureLocalLeaderCharge");
  Assert.NotNull(method,"Explicit versioned charge policy missing");method.Invoke(s,new object[]{4});return s;
 }
 [Test] public void ChargeRequiresResolvedHitsAndSurvivesRestoreWithoutReplay(){
  var s=Fresh();Assert.AreEqual(0,(int)s.ReadHudState()["ultimate"]["charge"]);
  s.SetPaused(true);s.Advance(10000000,100);Assert.AreEqual(0,(int)s.ReadHudState()["ultimate"]["charge"]);s.SetPaused(false);
  for(int i=0;i<600&&(int)s.ReadHudState()["ultimate"]["charge"]<100;i++)s.Advance(500000,1);
  Assert.AreEqual(100,(int)s.ReadHudState()["ultimate"]["charge"]);
  var copy=BasicCombatSimulation.RestoreLocalState(s.CaptureLocalState(),Fresh);Assert.AreEqual(s.CaptureLocalState(),copy.CaptureLocalState());
  var u=copy.ReadHudState()["ultimate"];copy.RequestSkill("charge-cast",(string)u["actor"],(string)u["ability"]);
  for(int i=0;i<100&&copy.CommandStatus("charge-cast")=="QUEUED";i++)copy.Advance(500000,1);
  Assert.AreEqual("CASTING",copy.CommandStatus("charge-cast"));Assert.Less((int)copy.ReadHudState()["ultimate"]["charge"],100);
  var before=copy.CaptureLocalState();copy.RequestSkill("charge-cast",(string)u["actor"],(string)u["ability"]);Assert.AreEqual(before,copy.CaptureLocalState());
 }
 [Test] public void PresentationUsesExactActorsHitsAndCastingState(){
  var s=Fresh();for(int i=0;i<80;i++)s.Advance(500000,1);
  var view=s.ReadHudState();Assert.NotNull(view["actors"]);Assert.NotNull(view["hits"]);Assert.NotNull(view["casts"]);
  Assert.AreEqual(5,((JArray)view["actors"]).Count(x=>!(bool)x["enemy"]&&!(bool)x["train"]&&!(bool)x["device"]));
  Assert.AreEqual(s.Events.Count,((JArray)view["hits"]).Count);Assert.IsTrue(((JArray)view["actors"]).Any(x=>(bool)x["enemy"]));
 }
 [Test] public void FailedPersistNeverPublishesConsumedGauge(){
  bool fail=false;var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>fail?throw new IOException():r+1,()=>true);driver.Open();
  for(int i=0;i<600&&(int)driver.HudState["ultimate"]["charge"]<100;i++)driver.Advance(500000,1);
  var u=driver.HudState["ultimate"];driver.RequestSkill("failed",(string)u["actor"],(string)u["ability"]);
  var before=driver.StateJson;fail=true;
  Assert.Throws<IOException>(()=>driver.Advance(500000,1));
  Assert.AreEqual(before,driver.StateJson);Assert.IsTrue(driver.RecoveryRequired);
 }
}
