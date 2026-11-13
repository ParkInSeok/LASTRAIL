using System;
using LastRail.Core.Simulation;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Original Session remains the only clock/period/stat/HP owner. These seams require
 // the upper native transaction to establish physical eligibility and publish flight together.
 public sealed partial class BDirectAttackSession {
  internal long PreviewMiraTick(long elapsed){ClockRange(elapsed,1);var copy=FixedStepDebt.Restore(clock.Capture());copy.Credit(elapsed);return checked(Tick+(copy.TryConsumeStep()?1:0));}
  internal JObject CommitMiraLaunch(string id,string target,JObject critical){
   Room();BuffWire.Need(modified&&input.IsWaveRuntime&&!Paused&&!TrainDefeated&&Hp("op_mira")>0&&Hp(target)>0&&Tick>=next["op_mira"],"mira_fire_unavailable");
   BuffWire.Need((string)critical["status"]=="NOT_APPLICABLE"&&((string)critical["policyVersion"]==BMiraFirstTrialContext.CriticalPolicyVersion&&input.SchemaVersion!=BDirectAttackInput.EquippedMiraInputVersion||(string)critical["policyVersion"]==BMiraEquippedTrialContext.Policy&&input.SchemaVersion==BDirectAttackInput.EquippedMiraInputVersion),"mira_critical_policy");
   var source=input.Entities["op_mira"];var numeric=input.NumericInput(source,input.Entities[target],ActiveAttack);
   numeric["defense"]=CombatExactValue.Create(0,1).Snapshot();numeric["weakening"]=new JArray();numeric["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()};
   var calculation=CombatNumericDefenseBudget.Resolve(numeric);
   var attack=new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=calculation.Snapshot()["exact"].DeepClone(),["appliedOffense"]=numeric["effects"].DeepClone(),["penetration"]=numeric["penetration"].DeepClone()};
   CombatProjectileAttackSnapshot.Capture(attack);
   long period=CombatActionTiming.BasicAttackPeriodTicks(source.Interval,input.StepTimeSpanTicks/10,Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>());
   next["op_mira"]=checked(Tick+period);
   events.Add(new JObject{["kind"]="MIRA_FIRST_FIRE",["opportunityId"]=id,["sourceId"]="op_mira",["targetId"]=target,["tick"]=Tick,["next"]=next["op_mira"],["attack"]=attack.DeepClone(),["critical"]=critical.DeepClone()});return attack;
  }
  internal bool MiraArrivalEligible(string target)=>Hp(target)>0&&(activeState==null||target!=(string)activeState["target"]["targetId"]||(bool)activeState["present"]&&!(bool)activeState["immune"]);
  internal JObject MiraArrival(string target){
   var numeric=input.NumericInput(input.Entities["op_mira"],input.Entities[target],CombatExactValue.Create(0,1),false);
   if(target==(string)activeState["target"]["targetId"])numeric["defense"]=activeState["defense"].DeepClone();
   return new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=numeric["defense"].DeepClone(),["effects"]=new JArray(),["weakening"]=numeric["weakening"].DeepClone(),["reductions"]=numeric["reductions"].DeepClone()};
  }
  internal BDirectAttackReceipt CommitMiraContact(string id,string target,FriendlyProjectileContact contact,int arrivalScaleBps=10000){
   Room();BuffWire.Need(contact.NewlyResolved&&contact.Outcome=="HIT"&&MiraArrivalEligible(target)&&!Paused&&!TrainDefeated&&!originals.ContainsKey(id),"mira_contact_not_fresh");
   var reference=(JObject)contact.Snapshot()["reference"];CombatProjectileAttackSnapshot.Coefficient(arrivalScaleBps);var exact=CombatExactValue.From((JObject)reference["exact"]).Multiply(CombatExactValue.Create(arrivalScaleBps,10000));var calculation=new CombatNumericDefenseResult(CombatExactValue.From((JObject)reference["effectiveDefense"]),exact);
   BuffWire.Need(calculation.ToVitalsMagnitude()==contact.Damage,"mira_contact_budget_conflict");
   return CommitResolvedDamage(id,"op_mira",target,new JObject{["kind"]="MIRA_FIRST_CONTACT",["eventId"]=id,["targetId"]=target,["tick"]=Tick,["contact"]=contact.Snapshot()},calculation);
  }
 }
 public sealed partial class BDirectAttackBuffSession {
  internal long PreviewMiraTick(long elapsed)=>direct.PreviewMiraTick(elapsed);
  internal bool MiraArrivalEligible(string target)=>direct.MiraArrivalEligible(target);
  internal JObject MiraArrival(string target)=>direct.MiraArrival(target);
  internal JObject CommitMiraLaunch(string id,string target,JObject critical){
   BuffWire.Need(events.Count<eventCapacity,"b_attack_buff_capacity");
   var accepted=buffs.Accept(new JObject{["eventId"]=id,["expectedRevision"]=buffs.Revision,["slot"]=binding.Attack.Entities["op_mira"].Slot,["kind"]="BASIC",["ability"]="BASIC",["timing"]="NONE",["target"]=direct.WaveCategory(target)??binding.Category(target),["direct"]=true,["originalEligible"]=true});
   BuffWire.Need((long?)accepted["damageBps"]==0,"mira_first_no_effect_buff");
   var attack=direct.CommitMiraLaunch(id,target,critical);events.Add(new JObject{["kind"]="MIRA_FIRST_FIRE",["opportunityId"]=id,["tick"]=Tick,["buffSource"]=accepted});return attack;
  }
  internal BDirectAttackBuffReceipt CommitMiraContact(string id,string opportunity,string target,FriendlyProjectileContact contact,int arrivalScaleBps=10000){
   BuffWire.Need(events.Count<eventCapacity,"b_attack_buff_capacity");var receipt=new BDirectAttackBuffReceipt(direct.CommitMiraContact(id,target,contact,arrivalScaleBps),buffs.Source(opportunity));
   var command=new JObject{["kind"]="MIRA_FIRST_CONTACT",["eventId"]=id,["opportunityId"]=opportunity,["tick"]=Tick};events.Add(command);receipts.Add(id,(command,receipt));return receipt;
  }
 }
}