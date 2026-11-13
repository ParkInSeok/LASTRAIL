using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatSkillCommandTests
    {
        static JObject Template()=> (JObject)JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"))["draft"]["template"];
        static BasicCombatActor Actor(string id,bool enemy,bool train,int hp,int atk,int slot)=>new BasicCombatActor(id,enemy,train,false,slot,hp,atk,0,1000,1,0,enemy?BasicCombatTarget.Train:BasicCombatTarget.EnemyAny,4000);
        static BasicCombatSimulation Make(string ultimate="op_reina",bool exterior=false,JObject template=null,bool lethalOperatorEnemy=false)
        {
            var t=template??Template();var actors=((JArray)t["operators"]).Select((x,i)=>Actor((string)x["operatorId"],false,false,1000,50,i)).Concat(new[]{Actor("train",false,true,5000,60,5)}).ToArray();
            var enemy=new BasicCombatActor("enemy",true,false,exterior,0,100000,lethalOperatorEnemy?2000:0,0,1000,1,0,lethalOperatorEnemy?BasicCombatTarget.Operators:BasicCombatTarget.Train,4000);
            var sim=new BasicCombatSimulation("TEST_ONLY_commands_v1",500000,1,actors,new[]{new[]{enemy}});
            sim.ConfigureSkills(t,ultimate,50);return sim;
        }
        [Test] public void DuplicateCommandAppliesDamageOnceAndCooldownRejectsSecond()
        {
            var s=Make();Assert.AreEqual("QUEUED",s.RequestSkill("cmd1","op_reina","op_reina.active"));
            s.Advance(500000*12,12);Assert.AreEqual("APPLIED",s.CommandStatus("cmd1"));
            var hp=s.GetHp("enemy");Assert.AreEqual("APPLIED",s.RequestSkill("cmd1","op_reina","op_reina.active"));Assert.AreEqual(hp,s.GetHp("enemy"));
            Assert.AreEqual(240,s.Events.First(x=>x.Source=="op_reina").HpLoss,"50 ATK *4 *(1+0.08+0.12), rank already resolved");
            s.RequestSkill("cmd2","op_reina","op_reina.active");s.Advance(500000,1);Assert.AreEqual("REJECTED_COOLDOWN",s.CommandStatus("cmd2"));
            Assert.Throws<InvalidOperationException>(()=>s.RequestSkill("cmd1","op_eve","op_eve.active"));
        }
        [Test] public void OnlySelectedUltimateAndFirstReadyAreEnforced()
        {
            var s=Make();Assert.AreEqual("REJECTED_ULTIMATE",s.RequestSkill("a","op_kyle","op_kyle.ultimate"));
            s.RequestSkill("b","op_reina","op_reina.ultimate");s.Advance(500000,1);Assert.AreEqual("REJECTED_COOLDOWN",s.CommandStatus("b"));
            s.Advance(500000*300,300);s.RequestSkill("c","op_reina","op_reina.ultimate");s.Advance(500000*20,20);Assert.AreEqual("APPLIED",s.CommandStatus("c"));
        }
        [Test] public void AllFiveSelectedKitsAndTrainAbilitiesParseAndExecute()
        {
            foreach(var id in new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"})
            {
                var s=Make(id);s.RequestSkill("active",id,id+".active");s.Advance(500000*25,25);Assert.AreEqual("APPLIED",s.CommandStatus("active"),id);
                s.Advance(500000*300,300);s.RequestSkill("ultimate",id,id+".ultimate");s.Advance(500000*25,25);Assert.AreEqual("APPLIED",s.CommandStatus("ultimate"),id);
            }
            foreach(var id in new[]{"barrier","emergency_repair"}){var s=Make();s.RequestSkill("train","train",id);s.Advance(500000*25,25);Assert.AreEqual("APPLIED",s.CommandStatus("train"));}
            var noExterior=Make();noExterior.RequestSkill("fire","train","support_fire");noExterior.Advance(500000,1);Assert.AreEqual("REJECTED_TARGET",noExterior.CommandStatus("fire"));
        }
        [Test] public void UnknownEffectIsRejectedBeforeBindingAnySkills()
        {
            var t=Template();t["operators"][0]["active"]["effects"][0]["type"]="invented";
            var s=new BasicCombatSimulation("TEST_ONLY",500000,1,new[]{Actor("train",false,true,10,0,0)},new[]{new[]{Actor("enemy",true,false,10,0,0)}});
            Assert.Throws<ArgumentException>(()=>s.ConfigureSkills(t,"op_reina",50));
        }
        [Test] public void TrainSupportUsesExteriorTargetAndTrainPackage()
        {
            var s=Make(exterior:true);s.RequestSkill("fire","train","support_fire");s.Advance(500000*15,15);
            Assert.AreEqual("APPLIED",s.CommandStatus("fire"));Assert.AreEqual(259,s.Events.First(x=>x.Source=="train").HpLoss);
        }
        [Test] public void MarkAndConditionalPassiveApplyOnceAtImpact()
        {
            var s=Make();s.RequestSkill("mark","op_sera","op_sera.active");s.Advance(500000*10,10);
            s.RequestSkill("reina","op_reina","op_reina.active");s.Advance(500000*15,15);
            Assert.AreEqual("APPLIED",s.CommandStatus("reina"));Assert.AreEqual(286,s.Events.Last(x=>x.Source=="op_reina").HpLoss);
        }
        [Test] public void OneQueuedCommandWaitsForBasicAndCannotBeReplaced()
        {
            var s=Make();s.Advance(500000,1);Assert.AreEqual("QUEUED",s.RequestSkill("one","op_reina","op_reina.active"));
            Assert.AreEqual("REJECTED_BUSY",s.RequestSkill("two","op_reina","op_reina.active"));
            s.Advance(500000*15,15);Assert.AreEqual("APPLIED",s.CommandStatus("one"));Assert.AreEqual("REJECTED_BUSY",s.CommandStatus("two"));
        }
        [Test] public void RelabelledUltimateCannotBypassCommanderSelection()
        {
            var t=Template();t["operators"][0]["ultimate"]["kind"]="ACTIVE";
            Assert.Throws<ArgumentException>(()=>Make(template:t));
        }
        [Test] public void OperatorRecoversAfterSixSecondsAndDeathCancelsCast()
        {
            var s=Make(lethalOperatorEnemy:true);s.RequestSkill("cast","op_kyle","op_kyle.active");
            s.Advance(500000*3,3);Assert.AreEqual(0,s.GetHp("op_kyle"));Assert.AreEqual("CANCELLED_DOWN",s.CommandStatus("cast"));Assert.AreEqual(1,s.OperatorDownCount("op_kyle"));
            s.Advance(500000*118,118);Assert.AreEqual(0,s.GetHp("op_kyle"));
            s.Advance(500000,1);Assert.AreEqual(500,s.GetHp("op_kyle"));Assert.IsTrue(s.IsRecoveryProtected("op_kyle"));
            s.Advance(500000*40,40);Assert.IsFalse(s.IsRecoveryProtected("op_kyle"));
        }
        [Test] public void RepeatedDownsUseSixNineTwelveAndKeepTwelveThereafter()
        {
            var s=Make(lethalOperatorEnemy:true);var downTicks=new[]{2,1002,2002,3002};var delays=new[]{120,180,240,240};
            for(var i=0;i<downTicks.Length;i++)
            {
                s.Advance((downTicks[i]-s.Tick)*500000,10000);Assert.AreEqual(0,s.GetHp("op_kyle"));Assert.AreEqual(i+1,s.OperatorDownCount("op_kyle"));
                s.Advance((delays[i]-1)*500000,10000);Assert.AreEqual(0,s.GetHp("op_kyle"));
                s.Advance(500000,1);Assert.AreEqual(500,s.GetHp("op_kyle"));
            }
        }
    }
}
