using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatSkillLifestealResolutionTests
{
    // Explicit test authority, NOT a production grant/ownership producer.
    sealed class Authority:ICombatSkillLifestealAuthority
    {
        public string BindingId=>"TEST_ONLY_ORIGINAL_O22";
        public bool Missing,Owned=true,Allowed=true,Generation=true;
        public int Multiplier=10000;
        public CombatSkillLifestealProof ResolveOriginal(ConfirmedSkillDamage d,long tick)
        {
            if(Missing)throw new InvalidOperationException("Original proof unavailable");
            Assert.AreEqual("op_reina",d.OperatorId);Assert.AreEqual("cast",d.ParentCommandId);
            return new CombatSkillLifestealProof("TEST_ONLY_APPLIED_RECEIPT","TEST_ONLY_UNCAPPED",Owned,Generation,Allowed,Multiplier);
        }
    }
    static BasicCombatSimulation Fresh(Authority authority)
    {
        var s=(BasicCombatSimulation)typeof(Ce03EffectValueTests).GetMethod("Make",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"op_reina"});
        if(authority!=null)s.ConfigureSkillLifesteal(authority);return s;
    }
    static BasicCombatSimulation Ready(Authority a,int hp=100)
    {
        var raw=JObject.Parse(Fresh(null).CaptureLocalState());
        foreach(var actor in (JArray)raw["actors"])if((string)actor["id"]=="op_reina")actor["vitals"]["hp"]=hp;
        foreach(var r in (JArray)raw["skillReady"])r["ready"]=0;
        var s=BasicCombatSimulation.RestoreLocalState(raw.ToString(),()=>Fresh(null));s.ConfigureSkillLifesteal(a);return s;
    }
    static void Cast(BasicCombatSimulation s){Assert.AreEqual("QUEUED",s.RequestSkill("cast","op_reina","op_reina.active"));s.Advance(12500000,25);}
    static JObject Row(BasicCombatSimulation s)=>(JObject)((JArray)s.ReadHudState()["effectResults"]).Single();
    [TestCase(1000,10000,120)][TestCase(10000,10000,1200)][TestCase(1000,5000,60)]
    public void OriginalDamagePipelineHasNoTimeCapAndAppliesModifierOnce(int damage,int multiplier,int gain)
    {
        // Explicit synthetic stats; original skill execution and Vitals are real.
        var template=(JObject)Ce03CommandContractTests.Prepared()["draft"]["template"];
        foreach(var augment in (JArray)template["package"]["augments"])augment["valueBps"]=0;
        template["package"]["tactic"]["valueBps"]=0;
        var reina=((JArray)template["operators"]).Single(o=>(string)o["operatorId"]=="op_reina");
        reina["active"]["effects"][0]["coefficient"]=new JObject{["numerator"]=1,["denominator"]=1};
        reina["active"]["firstReadyMs"]=0;
        BasicCombatSimulation Make(){
            var allies=((JArray)template["operators"]).Select((o,i)=>new BasicCombatActor((string)o["operatorId"],false,false,false,i,20000,damage,0,10000,2000,0,BasicCombatTarget.EnemyAny,4000)).Concat(new[]{new BasicCombatActor("train",false,true,false,5,5000,60,0,10000,2000,0,BasicCombatTarget.EnemyAny,4000)}).ToArray();
            var s=new BasicCombatSimulation("TEST_ONLY_RECOVERY_NUMBERS",500000,1,allies,new[]{new[]{new BasicCombatActor("enemy",true,false,false,0,100000,0,0,10000,2000,0,BasicCombatTarget.Train,4000)}});s.ConfigureSkills(template,"op_reina",50);return s;
        }
        var initial=JObject.Parse(Make().CaptureLocalState());((JArray)initial["actors"]).Single(x=>(string)x["id"]=="op_reina")["vitals"]["hp"]=100;
        var model=BasicCombatSimulation.RestoreLocalState(initial.ToString(),Make);model.ConfigureSkillLifesteal(new Authority{Multiplier=multiplier});Cast(model);
        Assert.AreEqual(damage,(int)Row(model)["actualResult"]["hpLoss"]);Assert.AreEqual(gain,(int)Row(model)["lifesteal"]["actualHpGain"]);Assert.AreEqual(100+gain,model.GetHp("op_reina"));
    }
    [TestCase("missingJournal")][TestCase("emptyRows")][TestCase("hpOnly")][TestCase("receiptOnly")]
    public void PartialCheckpointCannotSeparateHpFromRecoveryReceipt(string mutation)
    {
        var a=new Authority();var s=Ready(a);Cast(s);var raw=JObject.Parse(s.CaptureLocalState());
        if(mutation=="missingJournal")raw.Remove("effectJournal");
        if(mutation=="emptyRows")((JArray)raw["effectJournal"]["rows"]).Clear();
        if(mutation=="hpOnly")((JArray)raw["actors"]).Single(x=>(string)x["id"]=="op_reina")["vitals"]["hp"]=100;
        if(mutation=="receiptOnly")raw["effectJournal"]["rows"][0]["lifesteal"]["actualHpGain"]=27;
        Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(raw.ToString(),()=>Fresh(a)));
    }
    [Test]public void MissingProofDiscardsCandidateAndBlocksFurtherFrames()
    {
        var a=new Authority();var initial=Ready(a);string saved=initial.CaptureLocalState();long revision=1;
        var driver=new CombatSceneDriver(()=>Fresh(a),()=> (RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{BasicCombatSimulation.RestoreLocalState(saved,()=>Fresh(a)),revision},null),(s,r)=>{saved=s.CaptureLocalState();return ++revision;},()=>true);
        driver.Open();driver.RequestSkill("cast","op_reina","op_reina.active");string before=driver.StateJson;
        a.Missing=true;Assert.Throws<InvalidOperationException>(()=>driver.Advance(12500000,25));
        Assert.AreEqual(before,driver.StateJson);Assert.AreEqual(before,saved);Assert.IsTrue(driver.RecoveryRequired);
        Assert.Throws<InvalidOperationException>(()=>driver.Advance(500000,1));
        a.Missing=false;driver.Open();Assert.IsTrue((bool)driver.HudState["paused"]);driver.SetPaused(false);driver.Advance(12500000,25);Assert.AreEqual(28,(int)((JArray)driver.HudState["effectResults"])[0]["lifesteal"]["actualHpGain"]);
    }
    [Test]public void ActualDamageAndHealingPersistTogetherAndRestoreNeverReheals()
    {
        var a=new Authority();var s=Ready(a);Cast(s);
        Assert.AreEqual(128,s.GetHp("op_reina"));Assert.AreEqual(28,(int)Row(s)["lifesteal"]["actualHpGain"]);
        var saved=s.CaptureLocalState();var restored=BasicCombatSimulation.RestoreLocalState(saved,()=>Fresh(a));
        Assert.AreEqual(saved,restored.CaptureLocalState());Assert.AreEqual("APPLIED",restored.RequestSkill("cast","op_reina","op_reina.active"));Assert.AreEqual(128,restored.GetHp("op_reina"));
    }
    [TestCase(false,true,true,100,0,"NOT_OWNED")]
    [TestCase(true,false,true,100,0,"HEALING_BLOCKED")]
    [TestCase(true,true,false,100,0,"GENERATION_CHANGED")]
    [TestCase(true,true,true,1000,0,"FULL_HP")]
    [TestCase(true,true,true,990,10,"HEALED")]
    public void VerifiedZeroAndDeficitAreTerminalResults(bool owned,bool allowed,bool generation,int hp,int gain,string reason)
    {
        var a=new Authority{Owned=owned,Allowed=allowed,Generation=generation};var s=Ready(a,hp);Cast(s);
        Assert.AreEqual(hp+gain,s.GetHp("op_reina"));Assert.AreEqual(reason,(string)Row(s)["lifesteal"]["outcome"]);
        Assert.AreEqual(gain,(int)Row(s)["lifesteal"]["actualHpGain"]);
    }
    [Test]public void SaveFailureAndLostAcknowledgementDoNotPublishOrDoubleHeal()
    {
        var a=new Authority();string saved=null;long revision=0;bool fail=false,lost=false;
        Func<BasicCombatSimulation> fresh=()=>Fresh(a);
        Func<RestoredCombatCheckpoint> load=()=>saved==null?null:(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{BasicCombatSimulation.RestoreLocalState(saved,fresh),revision},null);
        var initial=Ready(a);saved=initial.CaptureLocalState();revision=1;
        var driver=new CombatSceneDriver(fresh,load,(s,r)=>{Assert.AreEqual(revision,r);if(fail)throw new IOException();saved=s.CaptureLocalState();revision++;if(lost)throw new IOException();return revision;},()=>true);
        driver.Open();driver.RequestSkill("cast","op_reina","op_reina.active");var before=driver.StateJson;
        fail=true;Assert.Throws<IOException>(()=>driver.Advance(12500000,25));Assert.AreEqual(before,driver.StateJson);Assert.IsTrue(driver.RecoveryRequired);
        fail=false;driver.Open();Assert.IsTrue((bool)driver.HudState["paused"]);driver.SetPaused(false);lost=true;Assert.Throws<IOException>(()=>driver.Advance(12500000,25));Assert.AreEqual(before,driver.StateJson);
        lost=false;driver.Open();Assert.AreEqual(128,BasicCombatSimulation.RestoreLocalState(driver.StateJson,fresh).GetHp("op_reina"));
        driver.SetPaused(false);Assert.AreEqual("APPLIED",driver.RequestSkill("cast","op_reina","op_reina.active"));Assert.AreEqual(saved,driver.StateJson);
    }
}
