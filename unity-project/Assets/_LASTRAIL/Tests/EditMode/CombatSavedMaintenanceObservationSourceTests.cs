#if UNITY_EDITOR
using LastRail.Battle;
using NUnit.Framework;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
namespace LastRail.Tests.EditMode {
 public sealed class CombatSavedMaintenanceObservationSourceTests {
  const string Qa="D:/Project/LASTRAIL/QA/CombatBMigration-20261004/SafeMaintenanceObservation-20261005/";
  static BasicCombatSimulation Fresh(bool rail=true){var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));d["ownedSelection"]["accountId"]="a";var s=BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1);if(rail){var c=global::CombatRailJourneyTests.Config();c["offers"]=new JArray();s.ConfigureRailJourney(c);}return s;}
  sealed class Host {
   internal string saved;internal long revision;internal bool fail,owner=true;internal bool rail;
   internal CombatSceneDriver driver;
   internal Host(bool hasRail=true){rail=hasRail;driver=New();driver.Open();}
   internal CombatSceneDriver New()=>new CombatSceneDriver(()=>Fresh(rail),()=>saved==null?null:(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{BasicCombatSimulation.RestoreLocalState(saved,()=>Fresh(rail)),revision},null),(s,r)=>{if(fail)throw new IOException("LOCAL save refusal");Assert.AreEqual(revision,r);saved=s.CaptureLocalState();return ++revision;},()=>owner);
  }
  [Test] public void ExistingSavedRailActuallyMovesStopsAndResumesWithoutNewClock(){var h=new Host();using(var s=new CombatSavedMaintenanceObservationSource(h.driver)){h.driver.Advance(500000*12,12);var moved=s.ObserveCommitted(1);Assert.That((long)moved["state"]["railJourney"]["distanceMm"],Is.GreaterThan(0));h.driver.SetPaused(true);var stopped=s.ObserveCommitted(2);h.driver.Advance(500000*200,200);Assert.IsTrue(JToken.DeepEquals(stopped,s.ObserveCommitted(3)));Assert.IsTrue((bool)stopped["travelStopped"]);h.driver.SetPaused(false);var resumed=s.ObserveCommitted(3);Assert.IsFalse((bool)resumed["travelStopped"]);h.driver.Advance(500000*12,12);var movedAgain=s.ObserveCommitted(4);Assert.That((long)movedAgain["state"]["railJourney"]["distanceMm"],Is.GreaterThan((long)stopped["state"]["railJourney"]["distanceMm"]));Assert.AreEqual(JTokenType.Null,movedAgain["residualHazards"].Type);File.WriteAllText(Qa+"actual-rail-stages.json",new JObject{["moved"]=moved,["stopped"]=stopped,["resumed"]=resumed,["movedAgain"]=movedAgain}.ToString());}}
  [Test] public void MissingActualRailNeverBecomesStoppedTrue(){var h=new Host(false);h.driver.SetPaused(true);using(var s=new CombatSavedMaintenanceObservationSource(h.driver)){Assert.IsTrue((bool)s.Capture()["combatStopped"]);Assert.AreEqual(JTokenType.Null,s.Capture()["travelStopped"].Type);Assert.Throws<InvalidOperationException>(()=>s.RequireTrustedMaintenance());}}
  [Test] public void FailedSaveAndChangedOwnerCannotPublishCandidate(){var h=new Host();using(var s=new CombatSavedMaintenanceObservationSource(h.driver)){var p=s.Capture();h.fail=true;Assert.Throws<IOException>(()=>h.driver.SetPaused(true));Assert.Throws<InvalidOperationException>(()=>s.ObserveCommitted(1));Assert.Throws<InvalidOperationException>(()=>s.RequireUnchanged(1,(string)p["stateHash"]));Assert.IsTrue(JToken.DeepEquals(p,s.Capture()));h.fail=false;h.driver.Open();s.RequireUnchanged(1,(string)p["stateHash"]);h.owner=false;Assert.Throws<InvalidOperationException>(()=>s.ObserveCommitted(1));}}
  [Test] public void StoredOriginalRevisionAndStateRestoreWithoutNewSourceTruth(){var h=new Host();using(var s=new CombatSavedMaintenanceObservationSource(h.driver)){h.driver.Advance(500000*12,12);var p=s.ObserveCommitted(1);var restored=h.New();restored.Open();using(var r=CombatSavedMaintenanceObservationSource.Restore(restored,p,BDirectAttackInput.Digest(p))){Assert.AreEqual(h.revision,r.Revision);Assert.IsTrue(JToken.DeepEquals(p,r.Capture()));p["state"]["clock"]["paused"]=true;Assert.IsFalse((bool)r.Capture()["combatStopped"]);}Assert.Throws<ArgumentException>(()=>CombatSavedMaintenanceObservationSource.Restore(restored,p,BDirectAttackInput.Digest(p)));}}
  [Test] public void SourceFenceRejectsResumeAndSkippedSavedRevisions(){var h=new Host();using(var s=new CombatSavedMaintenanceObservationSource(h.driver)){var p=s.Capture();h.driver.SetPaused(true);Assert.Throws<InvalidOperationException>(()=>s.RequireUnchanged(1,(string)p["stateHash"]));h.driver.SetPaused(false);Assert.Throws<InvalidOperationException>(()=>s.ObserveCommitted(1));Assert.AreEqual(1,s.Revision);}}
  [Test] public void DisposedObservationLeavesDriverAliveAndRejectsLateRefresh(){var h=new Host();var s=new CombatSavedMaintenanceObservationSource(h.driver);s.Dispose();s.Dispose();h.driver.SetPaused(true);Assert.Throws<ObjectDisposedException>(()=>s.ObserveCommitted(1));Assert.Throws<ObjectDisposedException>(()=>s.Capture());Assert.Throws<ObjectDisposedException>(()=>s.RequireUnchanged(1,"old"));Assert.IsTrue((bool)JObject.Parse(h.driver.StateJson)["clock"]["paused"]);}
 }
}
#endif

