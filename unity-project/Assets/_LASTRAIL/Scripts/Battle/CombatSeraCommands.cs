using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Core.Simulation;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Only commands/receipts/reservations live here. Native HP belongs to the existing model's CombatVitals.
 // This bounded owner does not arbitrate general basic/ultimate actions or enable a START profile.
 internal sealed class CombatSeraCommands {
  sealed class Pending {internal JObject Command;internal long Sequence;}
  readonly CombatSeraInput input;readonly IReadOnlyDictionary<string,CombatVitals> health;
  readonly OrdinaryDamageTargets ordinaryTargets;
  internal bool Terminal{get;private set;}
  readonly List<Pending> pending=new List<Pending>();readonly List<JObject> scheduled=new List<JObject>(),actions=new List<JObject>(),emitted=new List<JObject>(),operations=new List<JObject>();
  long clockMs,cooldown,busy;bool closed;
  internal CombatSeraCommands(CombatSeraInput input,IReadOnlyDictionary<string,CombatVitals> nativeHealth,OrdinaryDamageTargets targets=null){this.input=input;health=nativeHealth;ordinaryTargets=targets;}
  internal JArray Operations=>new JArray(operations.Select(o=>o.DeepClone()));
  internal JArray Emissions=>new JArray(emitted.Select(o=>o.DeepClone()));
  void Limit(){if(operations.Count>=10000)throw new InvalidOperationException("combat_sera_queue_event_limit");}
  static string EffectId(CombatSeraInput input,JToken command)=>BasicCombatSimulation.StableJson(new JArray(input.SimulationId,(string)command["actionId"],"op_sera.active","HEAL_ONE"));
  static string Str(JToken o,string k)=>CombatSeraInput.Text(o,k);
  static long Nat(JToken o,string k,long min=0)=>CombatSeraInput.Nat(o,k,min);
  static void Conflict(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
  static JObject Normalize(JObject raw){
   CombatSeraInput.Need(raw!=null);var r=(JObject)raw.DeepClone();if(r["effectSequence"]==null)r["effectSequence"]=JValue.CreateNull();CombatSeraInput.Fields(r,"command","sequence","effectSequence");
   var c=r["command"] as JObject;var kind=Str(c,"kind");
   var fields=new List<string>{"kind","eventId","actionId","atMs"};
   if(kind=="CAST")fields.AddRange(new[]{"sourceId","abilityId"});else if(kind=="OBSERVE_DAMAGE_HP")fields.AddRange(new[]{"actorId","hp"});
   else if(kind=="ORDINARY_DAMAGE"||kind=="ORDINARY_SHIELD"){fields.AddRange(new[]{"accountId","runId","targetKind","healthTargetId"});fields.AddRange(kind=="ORDINARY_DAMAGE"?new[]{"numeric","protected"}:new[]{"sourceId","abilityId","effectIndex","amount","expiryMs"});}
   else CombatSeraInput.Need(kind=="CANCEL","combat_sera_queue_command_invalid");
   CombatSeraInput.Fields(c,fields.ToArray());var eid=Str(c,"eventId");var aid=Str(c,"actionId");CombatSeraInput.Need(eid.Length<=400&&aid.Length<=100&&aid.All(ch=>ch>='!'&&ch<='~'));
   Nat(c,"atMs");Nat(r,"sequence",1);
   if(kind=="CAST"){Str(c,"sourceId");Str(c,"abilityId");Nat(r,"effectSequence",1);}else CombatSeraInput.Need(r["effectSequence"].Type==JTokenType.Null);
   if(kind=="OBSERVE_DAMAGE_HP"){Str(c,"actorId");Nat(c,"hp");}return r;
  }
  internal JObject Schedule(JObject raw){
   var r=Normalize(raw);var c=r["command"];string eid=Str(c,"eventId");var old=scheduled.FirstOrDefault(s=>Str(s["command"],"eventId")==eid);
   if(Str(c,"kind")=="ORDINARY_DAMAGE"||Str(c,"kind")=="ORDINARY_SHIELD"){if(ordinaryTargets==null)throw new InvalidOperationException("Original six-target owner required.");ordinaryTargets.Validate((JObject)c);}
   if(old!=null){Conflict(JToken.DeepEquals(old,r),"combat_sera_queue_event_conflict");return new JObject{["eventId"]=eid,["replayed"]=true};}
   if(Terminal)return new JObject{["eventId"]=eid,["replayed"]=false,["status"]="REJECTED_TERMINAL"};
   Limit();var claims=scheduled.SelectMany(s=>Str(s["command"],"kind")=="CAST"?new[]{Str(s["command"],"eventId"),EffectId(input,s["command"])}:new[]{Str(s["command"],"eventId")}).ToArray();
   var incoming=Str(c,"kind")=="CAST"?new[]{eid,EffectId(input,c)}:new[]{eid};
   Conflict(incoming.Distinct(StringComparer.Ordinal).Count()==incoming.Length&&!incoming.Any(claims.Contains),"combat_sera_queue_event_conflict");
   var orders=scheduled.SelectMany(s=>s["effectSequence"].Type==JTokenType.Null?new[]{Nat(s,"sequence")}:new[]{Nat(s,"sequence"),Nat(s,"effectSequence")}).ToArray();
   long seq=Nat(r,"sequence");Conflict(!orders.Contains(seq)&&(r["effectSequence"].Type==JTokenType.Null||Nat(r,"effectSequence")!=seq&&!orders.Contains(Nat(r,"effectSequence"))),"combat_logical_queue_sequence_conflict");
   if(Str(c,"kind")=="CAST")Conflict(!scheduled.Any(s=>Str(s["command"],"kind")=="CAST"&&Str(s["command"],"actionId")==Str(c,"actionId")),"combat_sera_action_conflict");
   Conflict(Nat(c,"atMs")>clockMs||Nat(c,"atMs")==clockMs&&!closed,"combat_logical_queue_closed_tick");
   scheduled.Add(r);pending.Add(new Pending{Command=(JObject)c.DeepClone(),Sequence=seq});operations.Add(new JObject{["kind"]="SCHEDULE",["request"]=r.DeepClone()});return new JObject{["eventId"]=eid,["replayed"]=false};
  }
  internal void RecordControl(string kind,JToken value){Limit();operations.Add(new JObject{["kind"]=kind,["value"]=value.DeepClone()});}
  internal void Advance(FixedStepDebt clock,long elapsed,int budget){
   if(Terminal)return;
   Limit();clock.Credit(elapsed);
   if(!clock.IsPaused){Drain(checked(clock.CompletedSteps*clock.Capture().StepTicks)/10000);for(int i=0;i<budget&&!Terminal&&clock.TryConsumeStep();i++)Drain(checked(clock.CompletedSteps*clock.Capture().StepTicks)/10000);}
   operations.Add(new JObject{["kind"]="ADVANCE",["elapsedTicks"]=elapsed,["maximumSteps"]=budget});
  }
  void Drain(long until){
   Conflict(until>=clockMs,"combat_logical_queue_clock_rewind");if(closed&&until==clockMs)return;
   while(true){
    var choices=pending.Where(p=>Nat(p.Command,"atMs")<=until).Select(p=>new BasicCombatSimulation.DueAction(Nat(p.Command,"atMs"),p.Sequence,()=>Dispatch(p)));
    var next=BasicCombatSimulation.OrderDueActions(choices).FirstOrDefault();if(next==null)break;next.Execute();
   }
   clockMs=until;closed=true;Terminal=ordinaryTargets?.TrainDefeated==true;
  }
  CombatVitals Hp(string id)=>health.TryGetValue(id,out var h)?h:throw new InvalidOperationException("combat_sera_actor_unknown");
  void Dispatch(Pending p){
   var c=p.Command;var kind=Str(c,"kind");var at=Nat(c,"atMs");clockMs=at;closed=false;
   var r=new JObject{["eventId"]=c["eventId"].DeepClone(),["actionId"]=c["actionId"].DeepClone()};
   var source=input.Source;var sourceHp=Hp(source.Id);
   if(kind=="ORDINARY_DAMAGE"||kind=="ORDINARY_SHIELD"){
    var result=ordinaryTargets.Apply(c);foreach(var property in result.Properties())r[property.Name]=property.Value.DeepClone();
   }else if(kind=="OBSERVE_DAMAGE_HP"){
    var h=Hp(Str(c,"actorId"));long after=Nat(c,"hp");Conflict(after<=h.Hp,"combat_sera_damage_direction_invalid");
    r["status"]="APPLIED";r["targetId"]=c["actorId"].DeepClone();r["beforeHp"]=h.Hp;h.ApplyTransferredHpLoss(checked(h.Hp-(int)after),at);r["afterHp"]=h.Hp;
   }else if(kind=="CAST"){
    Conflict(Str(c,"sourceId")==source.Id,"combat_sera_source_mismatch");Conflict(Str(c,"abilityId")=="op_sera.active","combat_sera_effect_unsupported");
    if(sourceHp.Hp==0||at<cooldown||at<busy){r["status"]="REJECTED";r["reason"]="SOURCE_NOT_READY";}
    else{
     var target=CombatHealingTargetSelection.Select(input.Entities.Select(e=>new CombatHealingTarget(e.Id,e.Slot,Hp(e.Id))),false);
     if(target==null){r["status"]="REJECTED";r["reason"]="NO_ELIGIBLE_TARGET";}
     else{
      long due=checked(at+400);Conflict(due<=9007199254740991L&&at<=9007199254728991L,"combat_sera_command_invalid");cooldown=at+12000;busy=due+300;string eid=EffectId(input,c);
      var action=new JObject{["actionId"]=c["actionId"].DeepClone(),["sourceId"]=source.Id,["targetId"]=target.Id,["acceptedAtMs"]=at,["effectAtMs"]=due,["effectEventId"]=eid,["status"]="PENDING"};actions.Add(action);
      r["status"]="ACCEPTED";r["sourceId"]=source.Id;r["targetId"]=target.Id;r["effectAtMs"]=due;r["effectEventId"]=eid;
      var request=scheduled.Single(s=>Str(s["command"],"eventId")==Str(c,"eventId"));
      pending.Add(new Pending{Sequence=Nat(request,"effectSequence"),Command=new JObject{["kind"]="RESOLVE",["eventId"]=eid,["actionId"]=c["actionId"].DeepClone(),["atMs"]=due}});
     }
    }
   }else{
    var action=actions.SingleOrDefault(a=>Str(a,"actionId")==Str(c,"actionId"));Conflict(action!=null&&Str(action,"status")=="PENDING","combat_sera_action_conflict");
    var h=Hp(Str(action,"targetId"));r["sourceId"]=source.Id;r["targetId"]=action["targetId"].DeepClone();
    if(kind=="CANCEL"||h.Hp==0||sourceHp.Hp==0){
     action["status"]="CANCELED";r["status"]="CANCELED";r["reason"]=kind=="CANCEL"?"ACTION_INTERRUPTED":h.Hp==0?"LOCKED_TARGET_DOWN":"CASTER_DOWN";
     if(kind=="CANCEL")pending.RemoveAll(x=>Str(x.Command,"eventId")==Str(action,"effectEventId"));
    }else{
     Conflict(kind=="RESOLVE"&&Str(c,"eventId")==Str(action,"effectEventId")&&at==Nat(action,"effectAtMs"),"combat_sera_effect_tick_mismatch");
     var amount=checked((int)source.Attack.Multiply(CombatExactValue.Create(16000,10000)).Multiply(source.Rank).Multiply(CombatExactValue.Create(11000,10000)).Floor());
     int before=h.Hp;int actual=h.RestoreHp(amount,10000);action["status"]="APPLIED";r["status"]="APPLIED";r["attemptedAmount"]=amount;r["actualAmount"]=actual;r["beforeHp"]=before;r["afterHp"]=h.Hp;
    }
   }
   r["replayed"]=false;pending.Remove(p);emitted.Add(new JObject{["eventId"]=c["eventId"].DeepClone(),["dueMs"]=at,["sequence"]=p.Sequence,["receipt"]=r});
  }
  internal JObject Capture()=>new JObject{["version"]="combat.sera-unity-commands.v1",["clockMs"]=clockMs,["closed"]=closed,["cooldownUntilMs"]=cooldown,["busyUntilMs"]=busy,
   ["pending"]=new JArray(pending.OrderBy(p=>Nat(p.Command,"atMs")).ThenBy(p=>p.Sequence).Select(p=>new JObject{["command"]=p.Command.DeepClone(),["sequence"]=p.Sequence})),
   ["scheduled"]=new JArray(scheduled.Select(s=>s.DeepClone())),["actions"]=new JArray(actions.Select(a=>a.DeepClone())),["receipts"]=Emissions,["operations"]=Operations};
 }
}
