#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;

public sealed class Ce03ConsumerEffectChecks {
 static class Checks { public static void AreEqual(object expected,object actual){if(!object.Equals(expected,actual))throw new InvalidOperationException("Effect value mismatch: expected="+expected+", actual="+actual);} public static void IsFalse(bool value){if(value)throw new InvalidOperationException("Unexpected effect outcome.");} }
 readonly JObject prepared; public Ce03ConsumerEffectChecks(JObject prepared){this.prepared=(JObject)prepared.DeepClone();}
 BasicCombatSimulation Make(string leader){
  var template=(JObject)prepared["draft"]["template"];
  var allies=((JArray)template["operators"]).Select((o,i)=>new BasicCombatActor((string)o["operatorId"],false,false,false,i,1000,50,0,10000,2000,0,BasicCombatTarget.EnemyAny,4000)).Concat(new[]{new BasicCombatActor("train",false,true,false,5,5000,60,0,10000,2000,0,BasicCombatTarget.EnemyAny,4000)}).ToArray();
  var enemies=new[]{new BasicCombatActor("inside",true,false,false,0,100000,0,0,10000,1000,0,BasicCombatTarget.Train,4000),new BasicCombatActor("outside",true,false,true,1,100000,0,0,10000,1000,0,BasicCombatTarget.Train,4000)};
  var s=new BasicCombatSimulation("TEST_ONLY_EFFECT_VALUES",500000,1,allies,new[]{enemies});s.ConfigureSkills(template,leader,50);return s;
 }
 BasicCombatSimulation Fixture(string leader){
  var raw=JObject.Parse(Make(leader).CaptureLocalState());
  foreach(var a in (JArray)raw["actors"]){if((string)a["id"]=="op_kyle")a["vitals"]["hp"]=300;if((string)a["id"]=="train")a["vitals"]["hp"]=3000;}
  // Explicit isolated effect fixture: initial ultimate wait is zeroed in checkpoint.
  // Separate charge/timing conformance tests cover the unmodified live contract.
  foreach(var r in (JArray)raw["skillReady"])r["ready"]=0;
  return BasicCombatSimulation.RestoreLocalState(raw.ToString(),()=>Make(leader));
 }
 static JObject Cast(BasicCombatSimulation s,string actor,string ability){Checks.AreEqual("QUEUED",s.RequestSkill("effect",actor,ability));s.Advance(500000*25,25);Checks.AreEqual("APPLIED",s.CommandStatus("effect"));return JObject.Parse(s.CaptureLocalState());}
 static JToken Actor(JObject raw,string id)=>((JArray)raw["actors"]).Single(a=>(string)a["id"]==id);
 
 
 
 public void AuthoredDamageProducesExactHpLoss(string actor,string skill,int expected){var s=Fixture(actor=="train"?"op_reina":actor);Cast(s,actor,actor=="train"?skill:actor+"."+skill);Checks.AreEqual(expected,s.Events.Single(e=>e.Source==actor).HpLoss);}
 
 public void ShieldHasExactMagnitudeAndCorrectRecipients(string skill,int expected){var s=Fixture("op_kyle");var raw=Cast(s,"op_kyle","op_kyle."+skill);var actors=(JArray)raw["actors"];var shielded=actors.Where(a=>((JArray)a["vitals"]["layers"]).Count>0).ToArray();Checks.AreEqual(skill=="active"?1:5,shielded.Length);foreach(var a in shielded)Checks.AreEqual(expected,((JArray)a["vitals"]["layers"]).Sum(l=>(int)l["remaining"]));if(skill=="active"){Checks.AreEqual(1,((JArray)raw["statuses"]).Count(r=>(string)r["type"]=="taunt"));}}
 
 public void HealChangesDamagedHpExactly(string skill,int expected){var s=Fixture("op_eve");var raw=Cast(s,"op_eve","op_eve."+skill);Checks.AreEqual(expected,(int)Actor(raw,"op_kyle")["vitals"]["hp"]);}
 
 public void RepairAndBuffApplyToTrain(string skill,int hp){var s=Fixture("op_mira");var raw=Cast(s,"op_mira","op_mira."+skill);Checks.AreEqual(hp,s.GetHp("train"));if(skill=="ultimate"){var status=((JArray)raw["statuses"]).Single(r=>(string)r["type"]=="train_damage_buff");Checks.AreEqual(1500,(int)status["value"]);}}
  public void TrainShieldAndRepairHaveDistinctOutcomes(){var s=Fixture("op_reina");var raw=Cast(s,"train","barrier");Checks.AreEqual(440,((JArray)Actor(raw,"train")["vitals"]["layers"]).Sum(l=>(int)l["remaining"]));Checks.AreEqual(3000,s.GetHp("train"));s=Fixture("op_reina");Cast(s,"train","emergency_repair");Checks.AreEqual(3400,s.GetHp("train"));}
  public void MarkValueAndInterruptRemoveFutureEnemyAttack(){var s=Fixture("op_sera");var raw=Cast(s,"op_sera","op_sera.active");var status=((JArray)raw["statuses"]).Single(r=>(string)r["type"]=="mark");Checks.AreEqual(1000,(int)status["value"]);string target=(string)status["target"];Checks.IsFalse(((JArray)raw["attacks"]).Any(a=>(string)a["source"]==target));s.Advance(500000*1000,1000);Checks.IsFalse(s.Events.Any(e=>e.Source==target));}
}


#endif
