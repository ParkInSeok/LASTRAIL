#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class CombatActionTimingTests
    {
        static Type Calculator=>typeof(CombatVitals).Assembly.GetType("LastRail.Battle.CombatActionTiming");
        static Type SourceType=>typeof(CombatVitals).Assembly.GetType("LastRail.Battle.CombatTimingSource");
        static object Source(string id,long value) {
            Assert.That(SourceType,Is.Not.Null,"Timing source contract missing.");
            try { return Activator.CreateInstance(SourceType,id,value); } catch(TargetInvocationException e) { throw e.InnerException; }
        }
        static Array Sources(params (string,long)[] values) {
            Assert.That(SourceType,Is.Not.Null,"Timing source contract missing.");
            var result=Array.CreateInstance(SourceType,values.Length);
            for(int i=0;i<values.Length;i++)result.SetValue(Source(values[i].Item1,values[i].Item2),i);
            return result;
        }
        static long Call(string method,params object[] args) {
            Assert.That(Calculator,Is.Not.Null,"Approved action timing calculator missing.");
            try { return (long)Calculator.GetMethod(method).Invoke(null,args); } catch(TargetInvocationException e) { throw e.InnerException; }
        }
        static long Attack(long bps,long step=1000)=>Call("BasicAttackPeriodTicks",2000000L,step,Sources(("eq",bps)),Sources(),Sources());
        static long Skill(long bps)=>Call("AutomaticSkillCooldownTicks",10000000L,1000L,Sources(("eq",bps)),Sources(),null);
        [TestCase(500,1905)][TestCase(1200,1786)][TestCase(3000,1539)][TestCase(7000,1177)][TestCase(15000,800)]
        public void ApprovedAttackSpeedGrades(long bps,long ticks)=>Assert.That(Attack(bps),Is.EqualTo(ticks));
        [TestCase(500,9524)][TestCase(1200,8929)][TestCase(3000,7693)][TestCase(7000,5883)][TestCase(15000,4000)]
        public void ApprovedSkillHasteGrades(long bps,long ticks)=>Assert.That(Skill(bps),Is.EqualTo(ticks));
        [Test] public void EquipmentSpeedAndHasteAreSummedWithoutOldCaps() {
            var eq=Sources(("a",15000),("b",15000),("c",15000));
            Assert.That(Call("BasicAttackPeriodTicks",2000000L,1000L,eq,Sources(),Sources()),Is.EqualTo(364));
            Assert.That(Call("AutomaticSkillCooldownTicks",10000000L,1000L,eq,Sources(),null),Is.EqualTo(1819));
            Assert.That(Call("BasicAttackPeriodTicks",2000000L,1000L,Sources(("eq",45000),("set",2300),("unique",3600),("enchant",2400)),Sources(),Sources()),Is.EqualTo(316));
        }
        [Test] public void CdrAndEnchantAreSeparateAndOnlyFinalDurationIsRounded() {
            Assert.That(Call("AutomaticSkillCooldownTicks",10000000L,1000L,Sources(("eq",45000)),Sources(("SET03",300)),Source("EN08",2300)),Is.EqualTo(1358));
            Assert.That(Call("AutomaticSkillCooldownTicks",10000000L,1000L,Sources(("eq",45000)),Sources(("a",4000),("b",1000)),Source("EN08",2300)),Is.EqualTo(840));
        }
        [Test] public void NonEquipmentCapAndStrongestSlowRemainIndependent() {
            Assert.That(Call("BasicAttackPeriodTicks",2000000L,1000L,Sources(),Sources(("buff",5000)),Sources(("slow1",4000),("slow2",2000))),Is.EqualTo(2286));
        }
        [Test] public void RepeatedSourceDoesNotStackAndConflictsReject() {
            Assert.That(Call("BasicAttackPeriodTicks",2000000L,1000L,Sources(("same",15000),("same",15000)),Sources(),Sources()),Is.EqualTo(800));
            Assert.Throws<ArgumentException>(()=>Call("BasicAttackPeriodTicks",2000000L,1000L,Sources(("same",15000),("same",500)),Sources(),Sources()));
            Assert.Throws<ArgumentException>(()=>Call("BasicAttackPeriodTicks",2000000L,1000L,Sources(("same",100)),Sources(("same",100)),Sources()));
            Assert.Throws<ArgumentException>(()=>Call("AutomaticSkillCooldownTicks",10000000L,1000L,Sources(),Sources(("same",500)),Source("same",500)));
        }
        [Test] public void TickResolutionAndRepresentationAreExplicit() {
            Assert.That(Attack(45000,2000),Is.EqualTo(182));
            Assert.That(Attack(9007199254740991),Is.EqualTo(1));
            Assert.That(Call("FixedPeriodTicks",2000000L,1000L),Is.EqualTo(2000));
            Assert.Throws<ArgumentException>(()=>Call("FixedPeriodTicks",0L,1000L));
            Assert.Throws<ArgumentException>(()=>Call("FixedPeriodTicks",2000000L,0L));
            Assert.Throws<ArgumentException>(()=>Source("bad",-1));
            Assert.Throws<OverflowException>(()=>Call("BasicAttackPeriodTicks",long.MaxValue,1L,Sources(),Sources(),Sources(("slow",3000))));
        }
        // Timing calculator input is microseconds; existing SimulationClock uses100ns TimeSpan ticks.
        [Test] public void MissingOversizedAndUnsupportedTimingSourcesFailClosed() {
            Assert.Throws<ArgumentNullException>(()=>Call("BasicAttackPeriodTicks",2000000L,1000L,null,Sources(),Sources()));
            var oversized=Array.CreateInstance(SourceType,1025); for(int i=0;i<oversized.Length;i++)oversized.SetValue(Source("owner/effect/"+i,100),i);
            Assert.Throws<ArgumentException>(()=>Call("BasicAttackPeriodTicks",2000000L,1000L,oversized,Sources(),Sources()));
            Assert.Throws<ArgumentException>(()=>Call("AutomaticSkillCooldownTicks",10000000L,1000L,Sources(),Sources(),Source("EN08",2301)));
        }
        static BasicCombatSimulation SkillFresh(long haste=45000) {
            // Historical CE03 template supplies a supported test-only action. No new catalog is issued.
            var template=(Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(
                "D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"))["draft"]["template"];
            var row=((Newtonsoft.Json.Linq.JArray)template["operators"]).Single(x=>(string)x["operatorId"]=="op_reina");
            row["active"]["firstReadyMs"]=0;
            row["active"]["cooldownMs"]=checked((int)Call("AutomaticSkillCooldownTicks",10000000L,1000L,Sources(("eq",haste)),Sources(("SET03",300)),Source("EN08",2300)));
            template["execution"]["stepMs"]=1;
            var allies=((Newtonsoft.Json.Linq.JArray)template["operators"]).Select((x,i)=>new BasicCombatActor((string)x["operatorId"],false,false,false,i,1000,50,0,100000,0,0,BasicCombatTarget.EnemyAny,4000))
                .Concat(new[]{new BasicCombatActor("train",false,true,false,5,5000,0,0,100000,0,0,BasicCombatTarget.EnemyAny,4000)}).ToArray();
            var enemy=new BasicCombatActor("enemy",true,false,false,0,100000,0,0,100000,0,0,BasicCombatTarget.Train,0);
            var simulation=new BasicCombatSimulation("TEST_ONLY_RESOLVED_COOLDOWN",10000,1u,allies,new[]{new[]{enemy}});
            simulation.ConfigureSkills(template,"op_reina",1);return simulation;
        }
        [Test] public void IssuedCooldownDeadlineSurvivesRestoreAndRejectsReinterpretedDefinition() {
            var original=SkillFresh(); Assert.That(original.RequestSkill("original-cast","op_reina","op_reina.active"),Is.EqualTo("QUEUED"));
            original.Advance(10000,1);
            var first=Newtonsoft.Json.Linq.JObject.Parse(original.CaptureLocalState());
            var ready=((Newtonsoft.Json.Linq.JArray)first["skillReady"]).Single(x=>(string)x["key"]=="op_reina/op_reina.active");
            Assert.That((long)ready["ready"],Is.EqualTo(1359));
            original.Advance(1000000,100);var saved=original.CaptureLocalState();
            var restored=BasicCombatSimulation.RestoreLocalState(saved,()=>SkillFresh());
            var before=Newtonsoft.Json.Linq.JObject.Parse(saved);
            var after=Newtonsoft.Json.Linq.JObject.Parse(restored.CaptureLocalState());
            Assert.That(Newtonsoft.Json.Linq.JToken.DeepEquals(before["skillReady"],after["skillReady"]),Is.True);
            Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(saved,()=>SkillFresh(90000)));
            original.Advance(1000000,100);restored.Advance(1000000,100);
            Assert.That(restored.CaptureLocalState(),Is.EqualTo(original.CaptureLocalState()));
        }
        static BasicCombatSimulation Fresh(int windup=0,int recovery=0) {
            int ticks=checked((int)Call("BasicAttackPeriodTicks",2000000L,1000L,Sources(("equipment",45000)),Sources(),Sources()));
            var train=new BasicCombatActor("train",false,true,false,0,100000,10,0,ticks,windup,recovery,BasicCombatTarget.EnemyAny,0);
            var enemy=new BasicCombatActor("enemy",true,false,false,0,100000,0,0,1000,0,0,BasicCombatTarget.Train,0);
            return new BasicCombatSimulation("TEST_ONLY_APPROVED_PERIOD",10000,1u,new[]{train},new[]{new[]{enemy}});
        }
        [Test] public void ResolvedPeriodReusesExistingReservationsAndRecoveryOccupancy() {
            var simulation=Fresh(500,200); simulation.Advance(20000000,3000);
            var ticks=simulation.Events.Where(e=>e.Source=="train").Select(e=>e.Tick).ToArray();
            Assert.That(ticks.Length,Is.GreaterThan(1));
            Assert.That(ticks.Zip(ticks.Skip(1),(a,b)=>b-a).All(delta=>delta>=700),Is.True);
        }
        [Test] public void SavedOriginalPeriodAndPendingAttackContinueWithoutCatchupDuplicate() {
            var simulation=Fresh(); simulation.Advance(4000000,500);var state=simulation.CaptureLocalState();
            var restored=BasicCombatSimulation.RestoreLocalState(state,()=>Fresh());
            simulation.Advance(16000000,2000); restored.Advance(16000000,2000);
            Assert.That(restored.CaptureLocalState(),Is.EqualTo(simulation.CaptureLocalState()));
            Assert.That(restored.Events.Where(e=>e.Source=="train").Count(),Is.EqualTo(simulation.Events.Where(e=>e.Source=="train").Count()));
        }
    }
}
#endif

