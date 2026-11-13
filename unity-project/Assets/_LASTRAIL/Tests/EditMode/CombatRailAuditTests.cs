using System;
using System.Collections.Generic;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatRailAuditTests {
 static BasicCombatSimulation Fresh(){var train=new BasicCombatActor("train",false,true,false,0,1000,0,0,1000,0,0,BasicCombatTarget.EnemyAny,10000);var enemy=new BasicCombatActor("enemy",true,false,false,0,1000,0,0,1000,0,0,BasicCombatTarget.Train,0);var m=new BasicCombatSimulation("audit-test",500000,1,new[]{train},new[]{new[]{enemy}});var c=CombatRailJourneyTests.Config();c["offers"]=new JArray();var gun=CombatRailArtilleryTests.Config();gun["encounters"][0]["spawnWave"]=1;c["artillery"]=gun;m.ConfigureRailJourney(c);return m;}
 [Test] public void PerTickArtilleryInputsAreEmittedOnlyAfterDurableCommit(){
  bool fail=false;var d=new CombatSceneDriver(Fresh,()=>null,(s,r)=>{if(fail)throw new IOException("injected");return r+1;},()=>true);var e=typeof(CombatSceneDriver).GetEvent("RailArtilleryStepCommitted");Assert.NotNull(e);var rows=new List<JObject>();e.AddEventHandler(d,(Action<JObject>)(row=>{Assert.AreEqual(d.Tick,(long)row["tick"]);rows.Add(row);}));d.Open();d.Advance(500000,8);Assert.AreEqual(1,rows.Count);Assert.AreEqual(1,(int)rows[0]["wave"]);Assert.AreEqual(500,(long)rows[0]["distanceMm"]);Assert.AreEqual(0,(int)rows[0]["damageTakenById"]["gun-2"]);fail=true;Assert.Throws<IOException>(()=>d.Advance(500000,8));Assert.AreEqual(1,rows.Count);Assert.AreEqual(1,d.Tick);
 }
}
