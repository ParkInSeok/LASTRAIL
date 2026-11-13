using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class CombatSceneDriverTests
    {
        [Test] public void HudSnapshotsCannotLeakMutationsOrSurviveCommittedStateChanges(){
            var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
            var first=driver.HudState;first["tick"]=999999;((JArray)first["actors"]).Clear();
            Assert.AreEqual(driver.Tick,(long)driver.HudState["tick"]);Assert.Greater(((JArray)driver.HudState["actors"]).Count,0);
            driver.SetPaused(true);Assert.IsTrue((bool)driver.HudState["paused"]);driver.SetPaused(false);driver.Advance(500000*3,3);
            Assert.AreEqual(driver.Tick,(long)driver.HudState["tick"]);Assert.IsFalse((bool)driver.HudState["paused"]);
            driver.Retire();Assert.IsNull(driver.HudState);
        }
        static BasicCombatSimulation Fresh(){var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));d["ownedSelection"]["accountId"]="a";return BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1);}
        [Test] public void FailedSaveDoesNotPublishOrContinueAndReloadResumesSavedTick()
        {
            string saved=null;long revision=0;bool fail=false;
            Func<RestoredCombatCheckpoint> load=()=>saved==null?null:(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{BasicCombatSimulation.RestoreLocalState(saved,Fresh),revision},null);
            Func<BasicCombatSimulation,long,long> save=(s,r)=>{if(fail)throw new IOException();Assert.AreEqual(revision,r);saved=s.CaptureLocalState();return ++revision;};
            var driver=new CombatSceneDriver(Fresh,load,save,()=>true);driver.Open();driver.Advance(500000*12,12);var before=driver.StateJson;
            fail=true;Assert.Throws<IOException>(()=>driver.RequestSkill("c1","op_reina","op_reina.active"));
            Assert.AreEqual(before,driver.StateJson);Assert.IsTrue(driver.RecoveryRequired);
            Assert.Throws<InvalidOperationException>(()=>driver.Advance(500000,1));
            Assert.Throws<IOException>(()=>driver.Open());Assert.AreEqual(before,driver.StateJson);Assert.AreEqual(before,saved);Assert.IsTrue(driver.RecoveryRequired);
            fail=false;driver.Open();var expected=BasicCombatSimulation.RestoreLocalState(before,Fresh);expected.SetPaused(true);Assert.AreEqual(expected.CaptureLocalState(),driver.StateJson);
            var stoppedReload=new CombatSceneDriver(Fresh,load,save,()=>true);stoppedReload.Open();stoppedReload.Advance(500000*200,200);Assert.AreEqual(driver.StateJson,stoppedReload.StateJson);Assert.Throws<InvalidOperationException>(()=>stoppedReload.RequestSkill("reloaded-stop","op_reina","op_reina.active"));
            var tick=driver.Tick;driver.Advance(500000*200,200);Assert.AreEqual(tick,driver.Tick);Assert.Throws<InvalidOperationException>(()=>driver.RequestSkill("during-stop","op_reina","op_reina.active"));Assert.IsFalse(driver.RecoveryRequired);
            driver.SetPaused(false);driver.Advance(0,200);Assert.AreEqual(tick,driver.Tick);
            Assert.AreEqual("QUEUED",driver.RequestSkill("c1","op_reina","op_reina.active"));
            var restored=new CombatSceneDriver(Fresh,load,save,()=>true);restored.Open();Assert.AreEqual(driver.StateJson,restored.StateJson);
        }
        [Test] public void OwnerChangeBlocksInputAndPausePreventsOfflineProgress()
        {
            bool owner=true;var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>owner);driver.Open();driver.SetPaused(true);var tick=driver.Tick;
            driver.Advance(500000*200,200);Assert.AreEqual(tick,driver.Tick);driver.SetPaused(false);driver.Advance(0,200);Assert.AreEqual(tick,driver.Tick);
            owner=false;Assert.Throws<InvalidOperationException>(()=>driver.RequestSkill("c","op_reina","op_reina.active"));Assert.IsTrue(driver.RecoveryRequired);
        }
        [Test] public void LostOriginalConfigurationBlocksFurtherFrames()
        {
            bool fail=false;var driver=new CombatSceneDriver(()=>{if(fail)throw new IOException();return Fresh();},()=>null,(s,r)=>r+1,()=>true);
            driver.Open();fail=true;Assert.Throws<IOException>(()=>driver.Advance(500000,1));Assert.IsTrue(driver.RecoveryRequired);
            Assert.Throws<InvalidOperationException>(()=>driver.Advance(500000,1));
        }
        [Test] public void MissingCheckpointAfterFailureNeverCreatesInitialState()
        {
            int creates=0,saves=0;bool fail=false;
            var driver=new CombatSceneDriver(()=>{creates++;return Fresh();},()=>null,(s,r)=>{saves++;if(fail)throw new IOException();return r+1;},()=>true);
            driver.Open();fail=true;Assert.Throws<IOException>(()=>driver.Advance(500000,1));var before=driver.StateJson;int count=creates,writes=saves;
            Assert.Throws<InvalidOperationException>(()=>driver.Open());Assert.AreEqual(count,creates);Assert.AreEqual(writes,saves);Assert.AreEqual(before,driver.StateJson);Assert.IsTrue(driver.RecoveryRequired);
        }
        [Test] public void FailedStateReadStopsTheSameExecutionOwner()
        {
            var f=OrdinaryFiveSeraFactoryTests.Sources();var factory=new OrdinaryFiveSeraFactory(f.Input,f.Stats,500000);
            var driver=new CombatSceneDriver(factory.Create,()=>null,(s,r)=>r+1,()=>true);driver.Open();var before=driver.StateJson;
            Assert.Throws<InvalidOperationException>(()=>{var ignored=driver.HudState;});Assert.IsTrue(driver.RecoveryRequired);
            Assert.Throws<InvalidOperationException>(()=>driver.Advance(500000,1));Assert.Throws<InvalidOperationException>(()=>driver.ScheduleResolvedSeraCommand(OrdinaryFiveSeraFactoryTests.Cast()));Assert.AreEqual(before,driver.StateJson);
        }
        [Test] public void DuplicateResumeDoesNotAppendAnotherSeraControlOrSave()
        {
            var f=OrdinaryFiveSeraFactoryTests.Sources();var factory=new OrdinaryFiveSeraFactory(f.Input,f.Stats,500000);int writes=0;
            var driver=new CombatSceneDriver(factory.Create,()=>null,(s,r)=>{writes++;return r+1;},()=>true);driver.Open();driver.SetPaused(true);var paused=driver.StateJson;int count=writes;
            driver.SetPaused(true);driver.Advance(500000*200,200);Assert.AreEqual(paused,driver.StateJson);Assert.AreEqual(count,writes);
            driver.SetPaused(false);var resumed=driver.StateJson;count=writes;driver.SetPaused(false);Assert.AreEqual(resumed,driver.StateJson);Assert.AreEqual(count,writes);
        }
    }
}
