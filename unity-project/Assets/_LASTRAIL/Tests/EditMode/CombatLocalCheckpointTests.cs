using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class CombatLocalCheckpointTests
    {
        static BasicCombatSimulation FreshFor(string account)
        {
            var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));
            d["ownedSelection"]["accountId"]=account;
            return BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],account,1);
        }
        static BasicCombatSimulation Fresh()=>FreshFor("a");
        [Test] public void RestoreDuringCastsAndStatusesFinishesWithIdenticalHistory()
        {
            var original=Fresh();original.Advance(500000*12,12);
            foreach(var id in new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"})original.RequestSkill(id,id,id+".active");
            original.Advance(500000*9,9);
            var restored=BasicCombatSimulation.RestoreLocalState(original.CaptureLocalState(),Fresh);
            Assert.AreEqual(original.CaptureLocalState(),restored.CaptureLocalState());
            original.Advance(500000*20000L,20000);restored.Advance(500000*20000L,20000);
            Assert.AreEqual(original.Outcome,restored.Outcome);Assert.AreEqual(15,restored.CompletedWaves);
            Assert.AreEqual(original.CaptureLocalState(),restored.CaptureLocalState());
            Assert.AreEqual("APPLIED",restored.RequestSkill("op_reina","op_reina","op_reina.active"));
        }
        [Test] public void WholeDebtPauseAndSpeedSurviveRoundtrip()
        {
            var s=Fresh();s.SetSpeed(3);s.Advance(500000*3,1);s.SetPaused(true);
            var r=BasicCombatSimulation.RestoreLocalState(s.CaptureLocalState(),Fresh);Assert.AreEqual(3,r.PendingSteps);r.Advance(500000,10);Assert.AreEqual(1,r.Tick);
            r.SetPaused(false);r.Advance(0,10);Assert.AreEqual(4,r.Tick);
        }
        [Test] public void CorruptOrMismatchedStateIsRejectedWithoutChangingOriginal()
        {
            var s=Fresh();var raw=s.CaptureLocalState();var d=JObject.Parse(raw);d["configurationSha256"]=new string('0',64);
            Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(d.ToString(),Fresh));
            d=JObject.Parse(raw);d["unexpected"]=true;Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(d.ToString(),Fresh));
            Assert.AreEqual(raw,s.CaptureLocalState());
            Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(raw,()=>FreshFor("different_owner")));
        }
    }
}
