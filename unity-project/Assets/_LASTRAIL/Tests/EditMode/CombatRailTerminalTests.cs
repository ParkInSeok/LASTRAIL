using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatRailTerminalTests {
 static BasicCombatSimulation ImpactFailureFresh(){var train=new BasicCombatActor("train",false,true,false,0,50,0,0,1000,0,0,BasicCombatTarget.EnemyAny,10000);var enemy=new BasicCombatActor("enemy",true,false,false,0,1000,0,0,1000,0,0,BasicCombatTarget.Train,0);var m=new BasicCombatSimulation("fatal-impact",500000,1,new[]{train},new[]{new[]{enemy}});var c=CombatRailJourneyTests.Config();c["offers"]=new JArray();var gun=CombatRailArtilleryTests.Config();gun["encounters"][0]["spawnWave"]=1;c["artillery"]=gun;m.ConfigureRailJourney(c);return m;}
 [Test] public void LethalArtilleryImpactCheckpointRemainsRestorable(){var m=ImpactFailureFresh();for(int i=0;i<30&&m.Outcome==BasicCombatOutcome.Running;i++)m.Advance(500000,8);Assert.AreEqual(BasicCombatOutcome.Failure,m.Outcome);Assert.AreEqual(m.CaptureLocalState(),BasicCombatSimulation.RestoreLocalState(m.CaptureLocalState(),ImpactFailureFresh).CaptureLocalState());}
 static BasicCombatSimulation Fresh(){
  var train=new BasicCombatActor("train",false,true,false,0,10000,10,0,1,0,0,BasicCombatTarget.EnemyAny,10000);
  var waves=Enumerable.Range(0,4).Select(i=>new[]{new BasicCombatActor("enemy"+i,true,false,false,0,1,0,0,1000,0,0,BasicCombatTarget.Train,0)}).ToArray();
  var model=new BasicCombatSimulation("terminal-drain",500000,1,new[]{train},waves);var config=CombatRailJourneyTests.Config();config["offers"]=new JArray();var gun=CombatRailArtilleryTests.Config();gun["definition"]["windupMs"]=50;gun["encounters"][0]["spawnWave"]=1;config["artillery"]=gun;model.ConfigureRailJourney(config);return model;
 }
 [Test] public void FinalWaveStopsNewLaunchesAndRestoresPendingFlightBeforeVictory(){
  var model=Fresh();for(int i=0;i<20&&model.CompletedWaves<4;i++)model.Advance(500000,8);
  Assert.AreEqual(4,model.CompletedWaves);Assert.AreEqual(BasicCombatOutcome.Running,model.Outcome,"Terminal candidate must drain existing flight");
  var checkpoint=model.CaptureLocalState();var state=JObject.Parse(checkpoint);Assert.IsTrue((bool)state["railJourney"]["terminalPending"]);
  long launches=(long)state["railJourney"]["artillery"]["sequence"];Assert.Greater(launches,0);
  var resumed=BasicCombatSimulation.RestoreLocalState(checkpoint,Fresh);
  for(int i=0;i<20&&model.Outcome==BasicCombatOutcome.Running;i++){model.Advance(500000,8);resumed.Advance(500000,8);Assert.AreEqual(model.CaptureLocalState(),resumed.CaptureLocalState());}
  Assert.AreEqual(BasicCombatOutcome.Victory,model.Outcome);Assert.AreEqual(10000-50*launches,model.GetHp("train"));Assert.AreEqual(launches,(long)JObject.Parse(model.CaptureLocalState())["railJourney"]["artillery"]["sequence"]);
 }
 [Test] public void RestoreRejectsCrossClockDistanceWaveAndTerminalMismatch(){
  var model=Fresh();model.Advance(500000,8);var good=JObject.Parse(model.CaptureLocalState());
  foreach(string field in new[]{"distanceMm","wave","terminalRequested"}){var bad=(JObject)good.DeepClone();bad["railJourney"]["artillery"][field]=field=="terminalRequested"?new JValue(true):new JValue((long)bad["railJourney"]["artillery"][field]+1);Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(bad.ToString(),Fresh),field);}
 }
}
