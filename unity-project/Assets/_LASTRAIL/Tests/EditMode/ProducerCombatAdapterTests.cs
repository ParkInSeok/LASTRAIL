using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class ProducerCombatAdapterTests
    {
        static readonly IReadOnlyDictionary<string,string> CarTypes=new Dictionary<string,string>{{"car_combat","Combat"},{"car_gun","Gun"}};
        static JObject Input()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));
        [Test] public void ActualOwnedProfileLoadsAndEnemyEntranceGatesTargets()
        {
            var d=Input();var s=BasicCombatSimulation.FromProducerDraftForLocalTest(d,(JObject)d["ownedSelection"],"a",1u,CarTypes);
            Assert.AreEqual(1400,s.GetHp("op_kyle"));Assert.AreEqual(5000,s.GetHp("train_a"));
            Assert.IsFalse(s.SetEnemyFocus("wave.1.interior.1"));s.Advance(500000*12,12);Assert.IsTrue(s.SetEnemyFocus("wave.1.interior.1"));
            s.RequestSkill("active","op_reina","op_reina.active");s.Advance(500000*15,15);Assert.AreEqual("APPLIED",s.CommandStatus("active"));
            Assert.That(s.GetHp("wave.1.interior.1"),Is.LessThan(494));
        }
        [Test] public void CarDevicesAttackCorrectLanesAndUseDistinctPackageFilters()
        {
            var d=Input();var s=BasicCombatSimulation.FromProducerDraftForLocalTest(d,(JObject)d["ownedSelection"],"a",1u,CarTypes);s.Advance(500000*42,42);
            var combat=s.Events.First(x=>x.Source=="car_combat");var gun=s.Events.First(x=>x.Source=="car_gun");
            StringAssert.Contains("interior",combat.Target);StringAssert.Contains("exterior",gun.Target);
            Assert.AreEqual(22,combat.HpLoss);Assert.AreEqual(41,gun.HpLoss);
        }
        [Test] public void OwnerOrRankRevisionMismatchCannotCreateSimulation()
        {
            var d=Input();var current=(JObject)d["ownedSelection"].DeepClone();current["formationRevision"]=2;
            Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.FromProducerDraftForLocalTest(d,current,"a",1u,CarTypes));
            Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.FromProducerDraftForLocalTest(d,(JObject)d["ownedSelection"],"other",1u,CarTypes));
            d["draft"]["template"]["operators"][0]["skillRank"]=2;
            Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.FromProducerDraftForLocalTest(d,(JObject)d["ownedSelection"],"a",1u,CarTypes));
        }
        [Test] public void AdoptedFifteenWaveProfileCompletesWithExplicitScriptedCommands()
        {
            var d=Input();var s=BasicCombatSimulation.FromProducerDraftForLocalTest(d,(JObject)d["ownedSelection"],"a",1u,CarTypes);
            for(var block=0;block<2000&&s.Outcome==BasicCombatOutcome.Running;block++)
            {
                if(block%50==1)
                {
                    foreach(var id in new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"})s.RequestSkill(block+id,id,id+".active");
                    s.RequestSkill(block+"ultimate","op_reina","op_reina.ultimate");
                    s.RequestSkill(block+"fire","train_a","support_fire");
                }
                s.Advance(500000*10,10);
            }
            var evidence=new JObject{["scope"]="HEADLESS_ADOPTED_PROFILE_SCRIPTED_TEST",["actualAccountRun"]=false,["completedWaves"]=s.CompletedWaves,["outcome"]=s.Outcome.ToString(),["tick"]=s.Tick,["trainHp"]=s.GetHp("train_a"),["hitCount"]=s.Events.Count};
            File.WriteAllText(Path.Combine(UnityEngine.Application.dataPath,"../QA/ScreenIntegration/adopted-profile-combat.json"),evidence.ToString());
            Assert.AreEqual(BasicCombatOutcome.Victory,s.Outcome);Assert.AreEqual(15,s.CompletedWaves);Assert.That(s.Events.Count,Is.GreaterThan(100));
        }
        [Test] public void SwappedOwnedCarIdsCannotInheritWrongDeviceType()
        {
            var d=Input();var cars=(JArray)d["ownedSelection"]["loadout"]["carriages"];
            cars[0]["carriageId"]="car_gun";cars[1]["carriageId"]="car_combat";
            Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.FromProducerDraftForLocalTest(d,(JObject)d["ownedSelection"],"a",1u,CarTypes));
        }
        [Test] public void PreparedCarOwnershipIsJoinedByIdAndRevision()
        {
            var d=Input();var s=BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1);Assert.AreEqual(1100,s.GetHp("car_combat"));
            d["carriageOwnership"]["carriages"][0]["revision"]=999;
            Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1));
        }
    }
}
