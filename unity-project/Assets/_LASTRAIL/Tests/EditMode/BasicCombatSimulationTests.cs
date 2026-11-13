using System;
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class BasicCombatSimulationTests
    {
        static BasicCombatActor Actor(string id, bool enemy, bool train, int hp, int attack, int armor, int period, int windup, BasicCombatTarget target, int slot=0)
            => new BasicCombatActor(id, enemy, train, false, slot, hp, attack, armor, period, windup, 0, target,4000);
        static BasicCombatSimulation Sim(BasicCombatActor[] allies, BasicCombatActor[][] waves)
            => new BasicCombatSimulation("TEST_ONLY_basic_v1", 500000, 123u, allies, waves);

        [Test] public void ArmorDamageDeathAndFifteenWavesAreDrivenByAttacks()
        {
            var waves=Enumerable.Range(1,15).Select(i=>new[]{Actor("enemy_"+i,true,false,15,0,5000,100,0,BasicCombatTarget.Train)}).ToArray();
            var sim=Sim(new[]{Actor("train",false,true,100,20,0,1,0,BasicCombatTarget.EnemyAny)},waves);
            sim.Advance(500000*100,100);
            Assert.AreEqual(BasicCombatOutcome.Victory,sim.Outcome);
            Assert.AreEqual(15,sim.CompletedWaves);
            Assert.AreEqual(30,sim.Events.Count(x=>x.HpLoss>0));
            Assert.AreEqual(10,sim.Events.First(x=>x.HpLoss>0).HpLoss);
            Assert.AreEqual(5,sim.Events.Where(x=>x.HpLoss>0).Skip(1).First().HpLoss);
        }
        [Test] public void CommittedHitAfterSourceDeathMakesFailureWinTie()
        {
            var sim=Sim(new[]{Actor("train",false,true,10,10,0,10,1,BasicCombatTarget.EnemyAny)},
                new[]{new[]{Actor("enemy",true,false,10,10,0,10,1,BasicCombatTarget.Train)}});
            sim.Advance(500000*3,3);
            Assert.AreEqual(BasicCombatOutcome.Failure,sim.Outcome);
            Assert.AreEqual(2,sim.Events.Count(x=>x.HpLoss==10));
        }
        [Test] public void FramePartitionAndBudgetDoNotChangeCombat()
        {
            BasicCombatSimulation Make()=>Sim(new[]{Actor("train",false,true,100,5,0,2,1,BasicCombatTarget.EnemyAny)},
                new[]{new[]{Actor("enemy",true,false,30,2,0,3,1,BasicCombatTarget.Train)}});
            var whole=Make(); var split=Make(); whole.Advance(500000*50,50);
            for(var i=0;i<100;i++)split.Advance(250000,1);
            CollectionAssert.AreEqual(whole.Events.Select(x=>x.ToString()),split.Events.Select(x=>x.ToString()));
            Assert.AreEqual(whole.Outcome,split.Outcome);
            var budget=Make(); budget.Advance(500000*50,1); while(budget.Outcome==BasicCombatOutcome.Running && budget.PendingSteps>0)budget.Advance(0,1);
            CollectionAssert.AreEqual(whole.Events.Select(x=>x.ToString()),budget.Events.Select(x=>x.ToString()));
        }
        [Test] public void WaveWaitsForCommittedHarmfulImpact()
        {
            var sim=Sim(new[]{Actor("train",false,true,100,10,0,10,0,BasicCombatTarget.EnemyAny)},
                new[]{new[]{Actor("enemy",true,false,10,7,0,10,3,BasicCombatTarget.Train)}});
            sim.Advance(500000,1); Assert.AreEqual(BasicCombatOutcome.Running,sim.Outcome); Assert.AreEqual(0,sim.CompletedWaves);
            sim.Advance(500000*3,3); Assert.AreEqual(93,sim.GetHp("train")); Assert.AreEqual(BasicCombatOutcome.Victory,sim.Outcome);
        }
        [Test] public void TerminalIsLatchedAndNoTimerCompletesAnUnkillableWave()
        {
            var sim=Sim(new[]{Actor("train",false,true,10,0,0,1,0,BasicCombatTarget.EnemyAny)},new[]{new[]{Actor("enemy",true,false,10,0,0,1,0,BasicCombatTarget.Train)}});
            sim.Advance(500000*100,100); Assert.AreEqual(0,sim.CompletedWaves); Assert.AreEqual(BasicCombatOutcome.Running,sim.Outcome);
            var loss=Sim(new[]{Actor("train",false,true,1,0,0,1,0,BasicCombatTarget.EnemyAny)},new[]{new[]{Actor("enemy",true,false,10,1,0,1,0,BasicCombatTarget.Train)}});
            loss.Advance(500000,1); var count=loss.Events.Count; loss.Advance(500000*100,100); Assert.AreEqual(count,loss.Events.Count); Assert.AreEqual(BasicCombatOutcome.Failure,loss.Outcome);
        }
        [Test] public void MissingAndUnsupportedConfigRejectsBeforeSimulation()
        {
            Assert.Throws<ArgumentException>(()=>Sim(Array.Empty<BasicCombatActor>(),Array.Empty<BasicCombatActor[]>()));
            Assert.Throws<ArgumentException>(()=>Actor("x",false,true,10,1,0,0,0,BasicCombatTarget.EnemyAny));
            Assert.Throws<ArgumentException>(()=>Actor("x",true,false,10,1,10001,1,0,BasicCombatTarget.Train));
        }
        [Test] public void ResolvedEffectsChangeActualAttackOutcome()
        {
            var sim=Sim(new[]{Actor("train",false,true,100,0,0,1,0,BasicCombatTarget.EnemyAny)},new[]{new[]{Actor("enemy",true,false,100,50,0,1,0,BasicCombatTarget.Train)}});
            sim.ApplyResolvedShield("train",new CombatShieldSource("operator","active",0),30,10);
            sim.Advance(500000,1);Assert.AreEqual(80,sim.GetHp("train"));Assert.AreEqual(30,sim.Events.Last().Absorbed);
            Assert.AreEqual(10,sim.ApplyResolvedRepair("train",100,1000));Assert.AreEqual(90,sim.GetHp("train"));
            Assert.AreEqual(10,sim.ApplyResolvedHeal("train",100));Assert.AreEqual(100,sim.GetHp("train"));
        }
        [Test] public void DeadSourceCannotRetargetCommittedHitOntoAnotherEnemy()
        {
            var sim=Sim(new[]{Actor("train",false,true,100,10,0,3,0,BasicCombatTarget.EnemyAny),Actor("operator",false,false,10,5,0,3,0,BasicCombatTarget.EnemyAny)},
                new[]{new[]{Actor("first",true,false,25,10,0,20,3,BasicCombatTarget.Operators),Actor("second",true,false,100,0,0,20,0,BasicCombatTarget.Train,1)}});
            sim.Advance(500000*4,4);Assert.AreEqual(0,sim.GetHp("operator"));Assert.AreEqual(100,sim.GetHp("second"));
        }
    }
}
