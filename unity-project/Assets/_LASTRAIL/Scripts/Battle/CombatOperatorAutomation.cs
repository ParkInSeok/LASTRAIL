using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
public sealed partial class BasicCombatSimulation {
 bool individualUltimates,generalSkillAuto,ultimateAuto;long automaticSequence;
 readonly Dictionary<string,int> operatorCharge=new Dictionary<string,int>(StringComparer.Ordinal);
 // Explicit local policy opt-in; prepared admission is separately version/hash checked.
 public void ConfigureLocalOperatorAutomation(bool generalAutomatic){
  if(Tick!=0||commands.Count!=0||individualUltimates||leaderChargePerHit!=0)throw new ArgumentException("Configure automation once before simulation.");
  var operators=skills.Values.Where(s=>Text(s.Data,"kind")=="ULTIMATE").Select(s=>s.Caster.Config.Id).Distinct().ToArray();if(operators.Length!=5)throw new ArgumentException("Five ultimate operators required.");
  foreach(var id in operators)operatorCharge.Add(id,0);individualUltimates=true;generalSkillAuto=generalAutomatic;selectedUltimate=null;
 }
 public void SetUltimateAuto(bool enabled){if(ordinaryFactory!=null){if(ordinaryUltimateAuto!=enabled)OrdinaryControl("ULTIMATE_AUTO",new JValue(enabled));return;}if(!individualUltimates)throw new InvalidOperationException("Individual ultimate policy required.");if(Outcome!=BasicCombatOutcome.Running||clock.Capture().IsPaused)return;ultimateAuto=enabled;}
 bool ChargedSkill(Skill s)=>(individualUltimates||leaderChargePerHit>0)&&Text(s.Data,"kind")=="ULTIMATE";
 int SkillCharge(Skill s)=>individualUltimates?operatorCharge[s.Caster.Config.Id]:leaderCharge;
 void ConsumeCharge(Skill s){if(individualUltimates)operatorCharge[s.Caster.Config.Id]=0;else leaderCharge=0;}
 static int ChargeAfterBasicHpLoss(int current,int hpLoss)=>hpLoss>0?Math.Min(100,checked(current+4)):current;
 void ChargeBasic(Actor source,int hpLoss){if(source.Config.Enemy||source.Config.Train||source.Device||source.Hp<=0||hpLoss<=0)return;if(individualUltimates){if(operatorCharge.TryGetValue(source.Config.Id,out int n))operatorCharge[source.Config.Id]=ChargeAfterBasicHpLoss(n,hpLoss);}else if(leaderChargePerHit>0)leaderCharge=Math.Min(100,leaderCharge+leaderChargePerHit);}
 bool AutoEligible(Skill s)=>s.Caster.Hp>0&&s.Caster.AvailableAt<=Tick&&!SkillBusy(s.Caster)&&!attacks.Any(a=>a.Source==s.Caster)&&(ChargedSkill(s)?SkillCharge(s)==100:s.Ready<=Tick)&&SkillTargets(s.Caster,Text(s.Data,"target")).Length>0;
 void AcceptSkill(Command cmd,Actor[] targets){
  // Stage all fallible calculations before charge/sequence/Ready and source evidence commit.
  long seq=checked(sequence+1),due=checked(Tick+ToTicks(Number(cmd.Skill.Data,"windupMs"))),ready=checked(Tick+ToTicks(Number(cmd.Skill.Data,"cooldownMs")));
  var timing=localActiveTiming;
  if(timing!=null&&timing.Binds(cmd.Skill.Caster.Config.Id,Text(cmd.Skill.Data,"kind")))timing=timing.Stage(cmd.Id,cmd.Skill.Caster.Config.Id+"/"+Text(cmd.Skill.Data,"id"),Tick,(long)Number(cmd.Skill.Data,"cooldownMs")*1000,(long)skillStepMs*1000,seq,due,out ready);
  if(ChargedSkill(cmd.Skill))ConsumeCharge(cmd.Skill);sequence=seq;cmd.Seq=seq;cmd.Due=due;cmd.Skill.Ready=ready;cmd.Targets=targets;cmd.Status="CASTING";localActiveTiming=timing;
 }
 void AutomaticSkills(string kind){
  foreach(var skill in skills.Values.Where(s=>Text(s.Data,"kind")==kind).OrderBy(s=>s.Caster.Config.Slot).ThenBy(s=>s.Caster.Config.Id,StringComparer.Ordinal)){
   if(!AutoEligible(skill))continue;string id;long autoSeq=automaticSequence;do{id="auto:"+checked(++autoSeq);}while(commands.ContainsKey(id));
   var command=new Command{Id=id,Skill=skill,Status="QUEUED",InputOrder=checked(inputSequence+1),Expiry=checked(Tick+ToTicks(queueExpiryMs))};AcceptSkill(command,SkillTargets(skill.Caster,Text(skill.Data,"target")));commands.Add(id,command);automaticSequence=autoSeq;inputSequence=command.InputOrder;
  }
 }
 string OperatorAutomationSchema=>generalSkillAuto?"ce03-headless-operator-auto-state-v1":"ce03-headless-operator-ultimate-state-v1";
 JObject CaptureOperatorAutomation()=>new JObject{["generalAuto"]=generalSkillAuto,["ultimateAuto"]=ultimateAuto,["sequence"]=automaticSequence,["charge"]=new JObject(operatorCharge.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new JProperty(p.Key,p.Value)))};
 void RestoreOperatorAutomation(JObject state){
  if(!individualUltimates){if(state!=null)throw new ArgumentException("Unexpected automation checkpoint.");return;}
  Fields(state,"generalAuto","ultimateAuto","sequence","charge");if(Boolean(state,"generalAuto")!=generalSkillAuto)throw new ArgumentException("Automatic general policy mismatch.");
  var charge=state["charge"] as JObject;Fields(charge,operatorCharge.Keys.ToArray());foreach(var id in operatorCharge.Keys.ToArray())operatorCharge[id]=(int)Nat(charge,id,100);ultimateAuto=Boolean(state,"ultimateAuto");automaticSequence=Nat(state,"sequence");
 }
}
}
