using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns the single actor's ACTIVE command/reservations, not HP, clock, target lifecycle or damage arithmetic.
 internal sealed class BMiraActiveCommandOwner {
  internal sealed class Due {internal long Tick,Sequence;internal string Kind;internal int Index;}
  readonly BActiveProjectileSourcePacket source;
  BTargetLifecycleJournal lifecycle;JObject lifecycleLock;
  internal void BindLifecycle(BTargetLifecycleJournal verified){lifecycle=verified;}
  internal void TargetReplaced(BDirectAttackBuffSession session,Func<long> next){if(locked!=null&&!session.ActiveEligible(locked,true))Cancel(session.Tick,next);}
  string command,phase="IDLE",status="IDLE";long? accepted,recovery;long ready,acceptSequence,recoverySequence;int cursor;
  JObject locked,attack,timingSource;readonly JArray shots=new JArray(),events=new JArray();readonly List<Due> reservations=new List<Due>();
  internal bool Auto=true;
  internal BMiraActiveCommandOwner(BActiveProjectileSourcePacket original){source=original;}
  internal BMiraActiveCommandOwner Clone(){var s=new BMiraActiveCommandOwner(source){lifecycle=lifecycle,lifecycleLock=lifecycleLock==null?null:(JObject)lifecycleLock.DeepClone(),command=command,phase=phase,status=status,accepted=accepted,recovery=recovery,ready=ready,acceptSequence=acceptSequence,recoverySequence=recoverySequence,cursor=cursor,locked=locked==null?null:(JObject)locked.DeepClone(),attack=attack==null?null:(JObject)attack.DeepClone(),timingSource=timingSource==null?null:(JObject)timingSource.DeepClone(),Auto=Auto};foreach(var row in shots)s.shots.Add(row.DeepClone());foreach(var row in events)s.events.Add(row.DeepClone());foreach(var r in reservations)s.reservations.Add(new Due{Tick=r.Tick,Sequence=r.Sequence,Kind=r.Kind,Index=r.Index});return s;}
  internal bool Busy(long tick)=>phase=="WINDUP"||phase=="BURST"||phase=="RECOVERY"&&tick<recovery;
  static string Identity(string prefix,JObject value)=>prefix+BDirectAttackInput.Digest(value).Substring(7);
  internal string AutoId(long tick)=>Identity("MIRA_AUTO:",new JObject{["contextId"]=source.Context,["tick"]=tick});
  internal string Request(string id,BDirectAttackBuffSession session,Func<long> next){
   if(phase=="TERMINAL")return "REJECTED_TERMINAL";if(session.Paused)return "REJECTED_PAUSED";if(session.Hp("op_mira")==0)return "REJECTED_DOWN";if(Busy(session.Tick))return "REJECTED_BUSY";if(session.Tick<ready)return "REJECTED_COOLDOWN";
   var current=lifecycle==null?source.Target:session.CurrentActiveTarget;if(!session.ActiveEligible(current,true))return "REJECTED_TARGET";
   string timingId=Identity("MIRA_CAST:",new JObject{["contextId"]=source.Context,["commandId"]=id});var frozen=session.CaptureActiveModifier(timingId,true,(int)source.Actor["slot"]);
   long cd=session.ActiveCooldownTicks(timingId,(long)source.Ability["cooldownMs"]*1000);ready=checked(session.Tick+cd);command=id;accepted=session.Tick;locked=current;attack=null;timingSource=frozen;cursor=0;shots.Clear();reservations.Clear();acceptSequence=next();if(lifecycle!=null){lifecycleLock=lifecycle.CommandLock(id,session.Tick,acceptSequence);BuffWire.Need(JToken.DeepEquals(lifecycleLock["target"],locked),"b_lifecycle_acceptance_target");}phase="WINDUP";status="ACCEPTED";
   var offsets=source.ShotOffsets;for(int i=0;i<6;i++)reservations.Add(new Due{Tick=checked(session.Tick+offsets[i]),Sequence=next(),Kind="EMIT",Index=i});recovery=checked(session.Tick+offsets[5]+source.RecoveryTicks);recoverySequence=next();
   events.Add(new JObject{["kind"]="ACCEPT",["commandId"]=id,["tick"]=session.Tick,["sequence"]=acceptSequence,["ready"]=ready,["timingSource"]=frozen.DeepClone()});return "ACCEPTED";
  }
  internal IEnumerable<Due> Pending(long tick){foreach(var r in reservations)if(r.Tick<=tick)yield return r;for(int i=0;i<shots.Count;i++)if((string)shots[i]["status"]=="IN_FLIGHT"&&(long)shots[i]["emission"]["impactDue"]<=tick)yield return new Due{Tick=(long)shots[i]["emission"]["impactDue"],Sequence=(long)shots[i]["impactReservationSequence"],Kind="IMPACT",Index=i};if(recovery.HasValue&&phase!="IDLE"&&phase!="TERMINAL"&&recovery<=tick)yield return new Due{Tick=recovery.Value,Sequence=recoverySequence,Kind="RECOVERY"};}
  internal void Execute(Due due,BDirectAttackBuffSession session,Func<long> next,List<BDirectAttackBuffReceipt> hits){
   if(phase=="TERMINAL")return;
   if(due.Kind=="EMIT"){
    if(!reservations.Contains(due))return;
    if(!session.ActiveEligible(locked,true)){Cancel(session.Tick,next);return;}
    if(attack==null)attack=session.CurrentActiveAttack.Snapshot();
    var shotId=Identity("MIRA_SHOT:",new JObject{["contextId"]=source.Context,["commandId"]=command,["shotIndex"]=due.Index});var modifier=session.CaptureActiveModifier(shotId,false,(int)source.Actor["slot"]);long seq=next();long impactDue=checked(session.Tick+source.Travel);long impactSequence=next();
    var emission=new JObject{["version"]=BActiveProjectileSourcePacket.Wire,["sourcePacketHash"]=source.Hash,["contextId"]=source.Context,["projectileId"]=source.Ability["projectileId"].DeepClone(),["effectId"]=source.Ability["effectId"].DeepClone(),["shotId"]=shotId,["commandId"]=command,["actorId"]="op_mira",["instanceId"]=source.Actor["instanceId"].DeepClone(),["slot"]=source.Actor["slot"].DeepClone(),["abilityId"]="op_mira.active",["acceptedTick"]=accepted,["emittedTick"]=session.Tick,["impactDue"]=impactDue,["sequence"]=seq,["shotIndex"]=due.Index,["target"]=locked.DeepClone(),["attackSnapshot"]=attack.DeepClone(),["damageBps"]=source.Ability["damageBps"].DeepClone(),["modifierSourceHash"]=BDirectAttackInput.Digest(modifier),["modifierEventId"]=shotId,["modifierSnapshot"]=session.BuffSnapshot()};
    if(lifecycle!=null){emission["version"]=BTargetLifecycleJournal.Wire;emission["lifecycleLock"]=lifecycleLock.DeepClone();}
    shots.Add(new JObject{["emission"]=emission,["status"]="IN_FLIGHT",["resolution"]=null,["impactReservationSequence"]=impactSequence});cursor++;reservations.Remove(due);phase=cursor==6?"RECOVERY":"BURST";events.Add(new JObject{["kind"]="EMIT",["shotId"]=shotId,["tick"]=session.Tick,["sequence"]=seq});
   }else if(due.Kind=="IMPACT"){
    var shot=(JObject)shots[due.Index];if((string)shot["status"]!="IN_FLIGHT")return;var emission=shot["emission"];bool eligible=session.ActiveEligible((JObject)emission["target"],false);string receiptId=null;if(eligible){receiptId="MIRA_IMPACT:"+((string)emission["shotId"]).Substring("MIRA_SHOT:".Length);hits.Add(session.ResolveActiveImpact(receiptId,(string)emission["shotId"],(string)emission["target"]["targetId"],CombatExactValue.From((JObject)emission["attackSnapshot"])));}
    long seq=next();shot["status"]=eligible?"RESOLVED_HIT":"RESOLVED_MISS";shot["resolution"]=new JObject{["tick"]=session.Tick,["sequence"]=seq,["result"]=eligible?"RESOLVED_HIT":"MISS_TARGET_INVALID",["originalImpactReceiptId"]=receiptId};events.Add(new JObject{["kind"]=eligible?"IMPACT":"MISS",["shotId"]=emission["shotId"].DeepClone(),["tick"]=session.Tick,["sequence"]=seq,["receiptId"]=receiptId});
   }else if(due.Kind=="RECOVERY"){phase="IDLE";status=status=="CANCELLED_UNFIRED"?status:"APPLIED";events.Add(new JObject{["kind"]="RECOVERY_END",["tick"]=session.Tick,["sequence"]=next()});}
  }
  void Cancel(long tick,Func<long> next){if(reservations.Count==0)return;reservations.Clear();phase="RECOVERY";status="CANCELLED_UNFIRED";recovery=checked(tick+source.RecoveryTicks);recoverySequence=next();events.Add(new JObject{["kind"]="CANCEL_UNFIRED",["tick"]=tick,["sequence"]=next()});}
  internal void CheckCaster(BDirectAttackBuffSession session,Func<long> next){if(session.Hp("op_mira")==0&&reservations.Count>0)Cancel(session.Tick,next);}
  internal void Terminal(long tick,Func<long> next){if(phase=="TERMINAL")return;phase="TERMINAL";status="TERMINAL";reservations.Clear();long seq=next();foreach(var row in shots.Where(r=>(string)r["status"]=="IN_FLIGHT")){row["status"]="CLEANED_TERMINAL";row["resolution"]=new JObject{["tick"]=tick,["sequence"]=seq,["result"]="CLEANED_TERMINAL",["originalImpactReceiptId"]=null};}events.Add(new JObject{["kind"]="TERMINAL",["tick"]=tick,["sequence"]=seq});}
  JObject WithLifecycle(JObject value,bool wire){if(lifecycle!=null){if(wire)value["version"]=BTargetLifecycleJournal.Wire;value["lifecycleLock"]=lifecycleLock==null?JValue.CreateNull():(JToken)lifecycleLock.DeepClone();}return value;}
  internal JObject Wire(BDirectAttackBuffSession session,long sequence)=>WithLifecycle(new JObject{["version"]=BActiveProjectileSourcePacket.Wire,["sourcePacketHash"]=source.Hash,["contextId"]=source.Context,["clockTick"]=session.Tick,["clockDebtTimeSpanTicks"]=session.OriginalDebtHalfTicks/2,["sequenceCursor"]=sequence,["readyTick"]=ready,["commandId"]=command,["acceptedTick"]=accepted,["phase"]=phase,["nextShotIndex"]=cursor,["lockedTarget"]=locked==null?JValue.CreateNull():(JToken)locked.DeepClone(),["attackSnapshot"]=attack==null?JValue.CreateNull():(JToken)attack.DeepClone(),["recoveryUntilTick"]=recovery,["shots"]=new JArray(shots.Select(r=>new JObject{["emission"]=r["emission"].DeepClone(),["status"]=r["status"].DeepClone(),["resolution"]=r["resolution"].DeepClone()}))},true);
  internal JObject Capture()=>WithLifecycle(new JObject{["auto"]=Auto,["commandId"]=command,["phase"]=phase,["status"]=status,["acceptedTick"]=accepted,["acceptedSequence"]=acceptSequence,["readyTick"]=ready,["recoveryUntilTick"]=recovery,["recoverySequence"]=recoverySequence,["lockedTarget"]=locked==null?JValue.CreateNull():(JToken)locked.DeepClone(),["attackSnapshot"]=attack==null?JValue.CreateNull():(JToken)attack.DeepClone(),["timingSource"]=timingSource==null?JValue.CreateNull():(JToken)timingSource.DeepClone(),["nextShotIndex"]=cursor,["reservations"]=new JArray(reservations.Select(r=>new JObject{["due"]=r.Tick,["sequence"]=r.Sequence,["kind"]=r.Kind,["index"]=r.Index})),["shots"]=shots.DeepClone(),["events"]=events.DeepClone()},false);
 }
}
