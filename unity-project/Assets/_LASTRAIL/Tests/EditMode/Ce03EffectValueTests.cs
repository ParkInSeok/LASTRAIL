using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class Ce03EffectValueTests {
 static BasicCombatSimulation Make(string leader){
  var template=(JObject)Ce03CommandContractTests.Prepared()["draft"]["template"];
  var allies=((JArray)template["operators"]).Select((o,i)=>new BasicCombatActor((string)o["operatorId"],false,false,false,i,1000,50,0,10000,2000,0,BasicCombatTarget.EnemyAny,4000)).Concat(new[]{new BasicCombatActor("train",false,true,false,5,5000,60,0,10000,2000,0,BasicCombatTarget.EnemyAny,4000)}).ToArray();
  var enemies=new[]{new BasicCombatActor("inside",true,false,false,0,100000,0,0,10000,1000,0,BasicCombatTarget.Train,4000),new BasicCombatActor("outside",true,false,true,1,100000,0,0,10000,1000,0,BasicCombatTarget.Train,4000)};
  var s=new BasicCombatSimulation("TEST_ONLY_EFFECT_VALUES",500000,1,allies,new[]{enemies});s.ConfigureSkills(template,leader,50);return s;
 }
 static BasicCombatSimulation Fixture(string leader){
  var raw=JObject.Parse(Make(leader).CaptureLocalState());
  foreach(var a in (JArray)raw["actors"]){if((string)a["id"]=="op_kyle")a["vitals"]["hp"]=300;if((string)a["id"]=="train")a["vitals"]["hp"]=3000;}
  // Explicit isolated effect fixture: initial ultimate wait is zeroed in checkpoint.
  // Separate charge/timing conformance tests cover the unmodified live contract.
  foreach(var r in (JArray)raw["skillReady"])r["ready"]=0;
  return BasicCombatSimulation.RestoreLocalState(raw.ToString(),()=>Make(leader));
 }
 static JObject Cast(BasicCombatSimulation s,string actor,string ability){Assert.AreEqual("QUEUED",s.RequestSkill("effect",actor,ability));s.Advance(500000*25,25);Assert.AreEqual("APPLIED",s.CommandStatus("effect"));return JObject.Parse(s.CaptureLocalState());}
 static JToken Actor(JObject raw,string id)=>((JArray)raw["actors"]).Single(a=>(string)a["id"]==id);
 [TestCase("op_reina","active",240)][TestCase("op_reina","ultimate",540)]
 [TestCase("op_sera","active",108)][TestCase("op_sera","ultimate",189)]
 [TestCase("train","support_fire",259)]
 public void AuthoredDamageProducesExactHpLoss(string actor,string skill,int expected){var s=Fixture(actor=="train"?"op_reina":actor);Cast(s,actor,actor=="train"?skill:actor+"."+skill);Assert.AreEqual(expected,s.Events.Single(e=>e.Source==actor).HpLoss);}
 [TestCase("active",198)][TestCase("ultimate",242)]
 public void ShieldHasExactMagnitudeAndCorrectRecipients(string skill,int expected){var s=Fixture("op_kyle");var raw=Cast(s,"op_kyle","op_kyle."+skill);var actors=(JArray)raw["actors"];var shielded=actors.Where(a=>((JArray)a["vitals"]["layers"]).Count>0).ToArray();Assert.AreEqual(skill=="active"?1:5,shielded.Length);foreach(var a in shielded)Assert.AreEqual(expected,((JArray)a["vitals"]["layers"]).Sum(l=>(int)l["remaining"]));if(skill=="active"){Assert.AreEqual(1,((JArray)raw["statuses"]).Count(r=>(string)r["type"]=="taunt"));}}
 [TestCase("active",685)][TestCase("ultimate",740)]
 public void HealChangesDamagedHpExactly(string skill,int expected){var s=Fixture("op_eve");var raw=Cast(s,"op_eve","op_eve."+skill);Assert.AreEqual(expected,(int)Actor(raw,"op_kyle")["vitals"]["hp"]);}
 [TestCase("active",3275)][TestCase("ultimate",3550)]
 public void RepairAndBuffApplyToTrain(string skill,int hp){var s=Fixture("op_mira");var raw=Cast(s,"op_mira","op_mira."+skill);Assert.AreEqual(hp,s.GetHp("train"));if(skill=="ultimate"){var status=((JArray)raw["statuses"]).Single(r=>(string)r["type"]=="train_damage_buff");Assert.AreEqual(1500,(int)status["value"]);}}
 [Test] public void TrainShieldAndRepairHaveDistinctOutcomes(){var s=Fixture("op_reina");var raw=Cast(s,"train","barrier");Assert.AreEqual(440,((JArray)Actor(raw,"train")["vitals"]["layers"]).Sum(l=>(int)l["remaining"]));Assert.AreEqual(3000,s.GetHp("train"));s=Fixture("op_reina");Cast(s,"train","emergency_repair");Assert.AreEqual(3400,s.GetHp("train"));}
 [Test] public void MarkValueAndInterruptRemoveFutureEnemyAttack(){var s=Fixture("op_sera");var raw=Cast(s,"op_sera","op_sera.active");var status=((JArray)raw["statuses"]).Single(r=>(string)r["type"]=="mark");Assert.AreEqual(1000,(int)status["value"]);string target=(string)status["target"];Assert.IsFalse(((JArray)raw["attacks"]).Any(a=>(string)a["source"]==target));s.Advance(500000*1000,1000);Assert.IsFalse(s.Events.Any(e=>e.Source==target));}
}

