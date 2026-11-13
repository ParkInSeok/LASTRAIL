using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatRecoveryOrderTests
{
    sealed class Authority:ICombatSkillLifestealAuthority
    {
        public string BindingId=>"TEST_ONLY_ORDER";
        public CombatSkillLifestealProof ResolveOriginal(ConfirmedSkillDamage damage,long tick)=>new CombatSkillLifestealProof("TEST_ONLY_APPLIED","TEST_ONLY_V7",true,true,true,10000);
    }
    static BasicCombatSimulation Make(int enemyWindup,bool terminal=false)
    {
        var t=(JObject)Ce03CommandContractTests.Prepared()["draft"]["template"];
        foreach(var a in (JArray)t["package"]["augments"])a["valueBps"]=0;t["package"]["tactic"]["valueBps"]=0;
        var reina=((JArray)t["operators"]).Single(x=>(string)x["operatorId"]=="op_reina");
        reina["active"]["firstReadyMs"]=0;reina["active"]["windupMs"]=0;
        reina["active"]["effects"][0]["coefficient"]=new JObject{["numerator"]=1,["denominator"]=1};
        var allies=((JArray)t["operators"]).OrderBy(x=>(string)x["operatorId"]=="op_reina"?0:1).Select((x,i)=>new BasicCombatActor((string)x["operatorId"],false,false,false,i,1000,1000,0,10000,0,0,BasicCombatTarget.EnemyAny,10000,new BasicCombatTiming(0,10000,null,false,null))).Concat(new[]{new BasicCombatActor("train",false,true,false,5,5000,0,0,10000,10000,0,BasicCombatTarget.EnemyAny,0)}).ToArray();
        if(terminal)allies[5]=new BasicCombatActor("train",false,true,false,5,5000,0,0,10000,10000,0,BasicCombatTarget.EnemyAny,0,new BasicCombatTiming(0,10000,null,false,null));
        var s=new BasicCombatSimulation("TEST_ONLY_RECOVERY_ORDER",500000,1,allies,new[]{new[]{new BasicCombatActor("enemy",true,false,false,0,terminal?500:10000,terminal?0:150,0,10000,enemyWindup,0,BasicCombatTarget.Operators,10000,terminal?new BasicCombatTiming(0,10000,null,false,null):null)}});s.ConfigureSkills(t,"op_reina",50);return s;
    }
    [Test]public void LastActualDamageCompletesRecoveryBeforeVictoryAndRestoreCannotReplayIt()
    {
        var raw=JObject.Parse(Make(0,true).CaptureLocalState());((JArray)raw["actors"]).Single(x=>(string)x["id"]=="op_reina")["vitals"]["hp"]=200;
        var s=BasicCombatSimulation.RestoreLocalState(raw.ToString(),()=>Make(0,true));s.ConfigureSkillLifesteal(new Authority());
        s.RequestSkill("last","op_reina","op_reina.active");s.Advance(500000,1);
        Assert.AreEqual(BasicCombatOutcome.Victory,s.Outcome);Assert.AreEqual(260,s.GetHp("op_reina"));
        var receipt=((JArray)s.ReadHudState()["effectResults"])[0];Assert.AreEqual(500,(int)receipt["actualResult"]["hpLoss"]);Assert.AreEqual(60,(int)receipt["lifesteal"]["actualHpGain"]);
        var saved=s.CaptureLocalState();var restored=BasicCombatSimulation.RestoreLocalState(saved,()=>{var n=Make(0,true);n.ConfigureSkillLifesteal(new Authority());return n;});
        Assert.AreEqual("APPLIED",restored.RequestSkill("last","op_reina","op_reina.active"));restored.Advance(5000000,10);Assert.AreEqual(saved,restored.CaptureLocalState());
    }
    [TestCase(false,70,2)][TestCase(true,0,2)][TestCase(false,70,4)][TestCase(true,0,4)]
    public void OriginalSequenceCompletesRecoveryBeforeNextHitAndEarlierDownCannotBeUndone(bool enemyFirst,int expected,int speed)
    {
        int windup=enemyFirst?1:0;var raw=JObject.Parse(Make(windup).CaptureLocalState());
        ((JArray)raw["actors"]).Single(x=>(string)x["id"]=="op_reina")["vitals"]["hp"]=100;
        var s=BasicCombatSimulation.RestoreLocalState(raw.ToString(),()=>Make(windup));s.ConfigureSkillLifesteal(new Authority());
        s.SetSpeed(speed);long elapsed=1000000/speed;s.SetPaused(true);s.Advance(5000000,10);Assert.AreEqual(100,s.GetHp("op_reina"));s.SetPaused(false);
        if(enemyFirst)s.Advance(elapsed,1);
        Assert.AreEqual("QUEUED",s.RequestSkill("cast","op_reina","op_reina.active"));s.Advance(elapsed,1);
        Assert.AreEqual(expected,s.GetHp("op_reina"));
        if(enemyFirst)Assert.AreEqual("CANCELLED_DOWN",s.RequestSkill("cast","op_reina","op_reina.active"));
        else Assert.AreEqual(120,(int)((JArray)s.ReadHudState()["effectResults"])[0]["lifesteal"]["actualHpGain"]);
        var saved=s.CaptureLocalState();var restored=BasicCombatSimulation.RestoreLocalState(saved,()=>{var n=Make(windup);n.ConfigureSkillLifesteal(new Authority());return n;});Assert.AreEqual(saved,restored.CaptureLocalState());
    }
    [Test]public void ExistingScheduledRecoveryDoesNotReplayCancelledDamageAndNewCastHealsNormally()
    {
        var raw=JObject.Parse(Make(1).CaptureLocalState());((JArray)raw["actors"]).Single(x=>(string)x["id"]=="op_reina")["vitals"]["hp"]=100;
        var s=BasicCombatSimulation.RestoreLocalState(raw.ToString(),()=>Make(1));s.ConfigureSkillLifesteal(new Authority());
        s.Advance(500000,1);s.RequestSkill("old","op_reina","op_reina.active");s.Advance(500000,1);Assert.AreEqual(0,s.GetHp("op_reina"));
        s.Advance(500000*1000L,1000);int recovered=s.GetHp("op_reina");Assert.Greater(recovered,0);
        Assert.AreEqual("CANCELLED_DOWN",s.RequestSkill("old","op_reina","op_reina.active"));Assert.AreEqual(recovered,s.GetHp("op_reina"));
        Assert.AreEqual(0,((JArray)s.ReadHudState()["effectResults"]).Count);
        Assert.AreEqual("QUEUED",s.RequestSkill("new","op_reina","op_reina.active"));s.Advance(500000,1);
        Assert.AreEqual(Math.Min(1000,recovered+120),s.GetHp("op_reina"));
    }
    [TestCase(10000,0)][TestCase(20000,2)]
    public void IndependentSmallDamageFractionsNeverAccumulate(int multiplier,int expected)
    {
        var owner=new CombatVitals(1000,0);owner.Damage(500,0,false);var enemy=new CombatVitals(1000,0);
        var gain=new CombatAttributedLifesteal(owner).Apply(new[]{enemy.Damage(5,0,false),enemy.Damage(5,0,false)},multiplier,true);
        Assert.AreEqual(expected,gain.ActualHpGain);Assert.AreEqual(500+expected,owner.Hp);Assert.AreEqual(0,gain.Excess);
    }
    [Test]public void IndependentDamageUsesActualLossAndCannotReviveDownRecipient()
    {
        var owner=new CombatVitals(1000,0);owner.Damage(900,0,false);var life=new CombatAttributedLifesteal(owner);
        var hits=new[]{new CombatVitals(500,0).Damage(1000,0,false),new CombatVitals(300,0).Damage(1000,0,false),new CombatVitals(1000,0).Damage(0,0,false)};
        Assert.AreEqual(96,life.Apply(hits,10000,true).ActualHpGain);Assert.AreEqual(196,owner.Hp);
        owner.Damage(1000,0,false);Assert.AreEqual(0,life.Apply(hits,10000,true).ActualHpGain);Assert.AreEqual(0,owner.Hp);
    }
}
