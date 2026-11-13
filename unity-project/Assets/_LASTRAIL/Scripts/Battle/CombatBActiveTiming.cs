using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Isolated modifier evidence; original simulation still owns Ready, commands and clock.
 // No B skill producer, target policy or effect dispatcher is supplied here.
 internal sealed class CombatBActiveTiming {
  internal const string Version="unity.combat-b.active-timing-local.v1";
  readonly JObject original;readonly string actor;readonly int slot;
  readonly CombatTimingSource[] haste,cdr;readonly CombatTimingSource en08;
  BCarriageBuffModel buffs;readonly JArray receipts=new JArray();
  internal CombatBActiveTiming(BCarriageBuffModel model,string id,int originalSlot,CombatTimingSource[] h,CombatTimingSource[] c,CombatTimingSource e){
   if(model==null||h==null||c==null)throw new ArgumentException("Explicit original timing inputs required.");
   buffs=model.CloneOwned();original=buffs.Snapshot();actor=id;slot=originalSlot;haste=h.ToArray();cdr=c.ToArray();en08=e;
   BuffWire.Need(((JArray)original["journal"]).Count==0,"combat_b_pristine_timing_source_required");
   var car=((JArray)original["departure"]["cars"]).SingleOrDefault(x=>(int)x["slot"]==slot);
   BuffWire.Need(car!=null&&(string)car["occupant"]["kind"]=="OPERATOR"&&(string)car["occupant"]["operatorId"]==actor,"combat_b_timing_binding_invalid");
   CombatActionTiming.AutomaticSkillCooldownTicks(1,1,haste,cdr,en08);
  }
  CombatBActiveTiming(CombatBActiveTiming s){original=s.original;actor=s.actor;slot=s.slot;haste=s.haste;cdr=s.cdr;en08=s.en08;buffs=s.buffs.CloneOwned();foreach(var r in s.receipts)receipts.Add(r.DeepClone());}
  internal bool Binds(string id,string kind)=>id==actor&&kind=="ACTIVE";
  static JObject Source(CombatTimingSource s)=>s==null?null:new JObject{["id"]=s.SourceId,["bps"]=s.ValueBps};
  internal JObject Configuration=>new JObject{["version"]=Version,["definitionSha256"]=buffs.DefinitionHash,["original"]=original.DeepClone(),["actor"]=actor,["slot"]=slot,["haste"]=new JArray(haste.Select(Source)),["cdr"]=new JArray(cdr.Select(Source)),["en08"]=Source(en08)};
  internal CombatBActiveTiming Stage(string id,string skill,long tick,long duration,long step,long seq,long due,out long ready){
   var s=new CombatBActiveTiming(this);
   s.buffs.Accept(new JObject{["eventId"]=id,["expectedRevision"]=s.buffs.Revision,["slot"]=slot,["kind"]="ACTIVE",["ability"]="ACTIVE",["timing"]="TIME_COOLDOWN",["target"]="NONE",["direct"]=false,["originalEligible"]=true});
   ready=checked(tick+new BCarriageBuffCalculator(s.buffs).ActiveCooldownTicks(id,duration,step,haste,cdr,en08));
   s.receipts.Add(new JObject{["id"]=id,["skill"]=skill,["tick"]=tick,["duration"]=duration,["step"]=step,["sequence"]=seq,["due"]=due,["ready"]=ready,["reschedules"]=new JArray()});return s;
  }
  internal CombatBActiveTiming Reschedule(string id,long tick,long due){var s=new CombatBActiveTiming(this);var receipt=s.receipts.Single(r=>(string)r["id"]==id);var rows=(JArray)receipt["reschedules"];BuffWire.Need(rows.Count<BCarriageBuffModel.JournalLimit,"combat_b_timing_reschedule_capacity");rows.Add(new JObject{["tick"]=tick,["due"]=due});return s;}
  internal void Transition(JObject input){var staged=buffs.CloneOwned();staged.Transition(input);buffs=staged;}
  internal JObject Capture()=>new JObject{["version"]=Version,["buffs"]=buffs.Snapshot(),["receipts"]=receipts.DeepClone()};
  internal JArray Receipts=>(JArray)receipts.DeepClone();
  static long Long(JToken obj,string key,long minimum=0){BuffWire.Need(obj?[key]?.Type==JTokenType.Integer,"combat_b_timing_integer_required");long n=(long)obj[key];BuffWire.Need(n>=minimum,"combat_b_timing_integer_range");return n;}
  internal CombatBActiveTiming Restore(JObject state){
   BuffWire.Fields(state,"version","buffs","receipts");BuffWire.Need((string)state["version"]==Version,"combat_b_timing_version_invalid");
   var s=new CombatBActiveTiming(this);s.buffs=buffs.RestoreOwned(BuffWire.Object(state["buffs"]));
   BuffWire.Need(JToken.DeepEquals(s.buffs.Snapshot()["departure"],original["departure"])&&JToken.DeepEquals(s.buffs.Snapshot()["initial"],original["initial"]),"combat_b_timing_original_mismatch");
   var seen=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);long priorTick=-1,priorSeq=0;
   foreach(var row in BuffWire.Array(state["receipts"],0,BCarriageBuffModel.JournalLimit)){
    var r=BuffWire.Object(row);BuffWire.Fields(r,"id","skill","tick","duration","step","sequence","due","ready","reschedules");string id=BuffWire.Text(r["id"]);BuffWire.Text(r["skill"]);
    long tick=Long(r,"tick"),duration=Long(r,"duration",1),step=Long(r,"step",1),seq=Long(r,"sequence",1);Long(r,"due",tick);BuffWire.Array(r["reschedules"],0,BCarriageBuffModel.JournalLimit);
    BuffWire.Need(seen.Add(id)&&tick>=priorTick&&seq>priorSeq,"combat_b_timing_receipt_order");priorTick=tick;priorSeq=seq;
    var action=((JArray)s.buffs.Snapshot()["actions"]).Single(a=>(string)a["input"]["eventId"]==id);var input=action["input"];
    BuffWire.Need((int)input["slot"]==slot&&(string)input["kind"]=="ACTIVE"&&(string)input["ability"]=="ACTIVE"&&(string)input["timing"]=="TIME_COOLDOWN"&&(string)input["target"]=="NONE"&&!(bool)input["direct"]&&(bool)input["originalEligible"],"combat_b_timing_action_conflict");
    long ready=checked(tick+new BCarriageBuffCalculator(s.buffs).ActiveCooldownTicks(id,duration,step,haste,cdr,en08));BuffWire.Need(Long(r,"ready",tick)==ready,"combat_b_timing_receipt_conflict");s.receipts.Add(r.DeepClone());
   }
   BuffWire.Need(((JArray)s.buffs.Snapshot()["actions"]).Select(a=>(string)a["input"]["eventId"]).SequenceEqual(s.receipts.Select(r=>(string)r["id"])),"combat_b_timing_source_receipt_mismatch");return s;
  }
 }
 public sealed partial class BasicCombatSimulation {
  CombatBActiveTiming localActiveTiming;
  // Local-only opt-in. Replace this validation entry when the original B ACTIVE producer supplies
  // actor/ability/target/effect/acceptance/restore contracts. It never advertises B execution support.
  public void ConfigureLocalActiveTimingForValidation(BCarriageBuffModel original,string id,int slot,CombatTimingSource[] haste,CombatTimingSource[] cdr,CombatTimingSource en08){
   if(Tick!=0||commands.Count!=0||localActiveTiming!=null||seraCommands!=null||railJourney!=null||!skills.Values.Any(s=>s.Caster.Config.Id==id&&Text(s.Data,"kind")=="ACTIVE"))throw new ArgumentException("Configure isolated original timed ACTIVE before simulation.");
   localActiveTiming=new CombatBActiveTiming(original,id,slot,haste,cdr,en08);
  }
  public void ApplyLocalActiveTimingTransition(JObject input){if(localActiveTiming==null)throw new InvalidOperationException("No local ACTIVE timing binding.");localActiveTiming.Transition(input);}
  void RestoreLocalActiveTiming(JObject state){
   if(localActiveTiming==null){if(state!=null)throw new ArgumentException("Unexpected local timing state.");return;}
   var restored=localActiveTiming.Restore(state);var evidence=restored.Receipts;
   foreach(var row in evidence){
    if(!commands.TryGetValue((string)row["id"],out var cmd)||!restored.Binds(cmd.Skill.Caster.Config.Id,Text(cmd.Skill.Data,"kind"))||(string)row["skill"]!=cmd.Skill.Caster.Config.Id+"/"+Text(cmd.Skill.Data,"id")||cmd.Seq!=(long)row["sequence"]||(long)row["duration"]!=(long)Number(cmd.Skill.Data,"cooldownMs")*1000||(long)row["step"]!=(long)skillStepMs*1000||(long)row["tick"]>Tick||(long)row["due"]!=checked((long)row["tick"]+ToTicks(Number(cmd.Skill.Data,"windupMs")))||cmd.Status=="QUEUED"||cmd.Status.StartsWith("REJECTED",StringComparison.Ordinal)||cmd.Status=="EXPIRED")throw new ArgumentException("Original command timing evidence mismatch.");
    long due=(long)row["due"];foreach(var token in (JArray)row["reschedules"]){var r=BuffWire.Object(token);Fields(r,"tick","due");long tick=Nat(r,"tick",Tick),next=Nat(r,"due");if(tick<due||next!=checked(tick+ToTicks(Number(cmd.Skill.Data,"windupMs"))))throw new ArgumentException("Original retarget timing mismatch.");due=next;}if(cmd.Due!=due)throw new ArgumentException("Original command Due mismatch.");
   }
   var accepted=commands.Values.Where(c=>restored.Binds(c.Skill.Caster.Config.Id,Text(c.Skill.Data,"kind"))&&c.Seq>0).Select(c=>c.Id).OrderBy(x=>x,StringComparer.Ordinal);
   if(!accepted.SequenceEqual(evidence.Select(r=>(string)r["id"]).OrderBy(x=>x,StringComparer.Ordinal)))throw new ArgumentException("Missing accepted timing evidence.");
   foreach(var skill in skills.Where(p=>restored.Binds(p.Value.Caster.Config.Id,Text(p.Value.Data,"kind")))){var last=evidence.LastOrDefault(r=>(string)r["skill"]==skill.Key);if(skill.Value.Ready!=(last==null?ToTicks(Number(skill.Value.Data,"firstReadyMs")):(long)last["ready"]))throw new ArgumentException("Original Ready mismatch.");}
   localActiveTiming=restored;
  }
 }
}
