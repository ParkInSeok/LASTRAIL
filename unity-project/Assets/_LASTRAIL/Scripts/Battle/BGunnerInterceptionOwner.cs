using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns action phases/reservations only. Session owns the adjusted basic due time.
 internal sealed class BGunnerInterceptionOwner {
  sealed class ActionState {
   internal string Actor,Shot,Phase="PREPARATION";internal long Accepted,Ready,Recovery,End;internal JObject TimingSource;
   internal ActionState Copy(){var s=(ActionState)MemberwiseClone();s.TimingSource=(JObject)TimingSource.DeepClone();return s;}
   internal JObject Capture()=>new JObject{["actorId"]=Actor,["shotId"]=Shot,["phase"]=Phase,["acceptedTick"]=Accepted,["readyTick"]=Ready,["recoveryTicks"]=Recovery,["endTick"]=End,["timingSource"]=TimingSource.DeepClone()};
  }
  readonly BDirectAttackBasicLoopBinding binding;readonly int capacity;readonly List<ActionState> actions=new List<ActionState>();readonly Dictionary<string,long> manual=new Dictionary<string,long>();readonly JArray events=new JArray();bool terminal;internal bool NamedSourceBound;
  internal BGunnerInterceptionOwner(BDirectAttackBasicLoopBinding b,int cap){binding=b;capacity=cap;}
  BGunnerInterceptionOwner(BGunnerInterceptionOwner p){binding=p.binding;capacity=p.capacity;terminal=p.terminal;NamedSourceBound=p.NamedSourceBound;actions.AddRange(p.actions.Select(a=>a.Copy()));foreach(var e in p.events)events.Add(e.DeepClone());foreach(var pair in p.manual)manual.Add(pair.Key,pair.Value);}
  internal BGunnerInterceptionOwner Clone()=>new BGunnerInterceptionOwner(this);
  internal bool Busy(string actor,long tick)=>actions.Any(a=>a.Actor==actor&&(a.Phase=="PREPARATION"||a.Phase=="RECOVERY"&&tick<a.End))||manual.TryGetValue(actor,out long end)&&tick<end;
  void Event(string kind,string actor,string shot,long tick,Func<long> next){BuffWire.Need(events.Count<capacity,"b_intercept_event_capacity");events.Add(new JObject{["kind"]=kind,["actorId"]=actor,["shotId"]=shot,["tick"]=tick,["sequence"]=next()});}
  bool Eligible(string actor,BGunnerProjectileOwner.Shot shot,BDirectAttackBuffSession session)=>session.Hp(actor)>0&&session.Hp(shot.Target)>0&&shot.TargetGeneration==session.ProjectileTargetGeneration(shot.Target)&&session.SpatialProtection(actor,shot.Target);
  internal void ObserveManual(string actor,long until,BDirectAttackBuffSession session,Func<long> next){BuffWire.Need(binding.Composition.Attack.SchemaVersion==BDirectAttackInput.GunnerInterceptLocalInputVersion&&binding.Composition.Attack.Entities.ContainsKey(actor)&&binding.Composition.Attack.Entities[actor].Kind=="OPERATOR"&&until>session.Tick&&until<=9007199254740991,"b_intercept_local_manual_boundary");BuffWire.Need(!Busy(actor,session.Tick)&&session.Hp(actor)>0,"b_intercept_preserve_accepted_action");manual[actor]=until;Event("EXPLICIT_LOCAL_EXISTING_MANUAL_OCCUPATION",actor,null,session.Tick,next);}
  internal void BeforeActions(BGunnerProjectileOwner projectiles,BDirectAttackBuffSession session,Func<long> next){
   foreach(var a in actions.Where(a=>a.Phase=="PREPARATION"||a.Phase=="RECOVERY").ToArray()){
    var shot=projectiles.All.Single(s=>s.Id==a.Shot);
    if(session.Hp(a.Actor)==0){shot.ReservedBy=null;a.Phase="DOWN_TERMINAL";Event("DOWN_TERMINAL",a.Actor,a.Shot,session.Tick,next);continue;}
    if(a.Phase=="RECOVERY"){if(session.Tick>=a.End){a.Phase="DONE";Event("RECOVERY_END",a.Actor,a.Shot,session.Tick,next);}continue;}
    bool valid=shot.Terminal=="FLYING"&&Eligible(a.Actor,shot,session);
    if(!valid||session.Tick>=a.Ready){
     if(valid&&a.Ready<=shot.Contact){shot.Terminal="INTERCEPTED";Event("INTERCEPTED",a.Actor,a.Shot,session.Tick,next);}else Event("PREPARATION_FAILED",a.Actor,a.Shot,session.Tick,next);
     shot.ReservedBy=null;long end=checked(session.Tick+a.Recovery);session.AdjustBasicOccupation(a.Actor,checked(end-a.End));a.End=end;a.Phase="RECOVERY";
    }
   }
  }
  internal void Accept(BGunnerProjectileOwner projectiles,BDirectAttackBuffSession session,Func<long> next){
   if(terminal||NamedSourceBound)return;var input=binding.Composition.Attack;
   foreach(var shot in projectiles.Pending.ToArray()){
    if(shot.ReservedBy!=null||session.Tick>=shot.Contact)continue;
    var candidates=input.Entities.Values.Where(e=>e.Kind=="OPERATOR"&&(e.Definition=="op_kyle"||e.Definition=="op_sable")&&!Busy(e.Id,session.Tick)&&Eligible(e.Id,shot,session)).Select(e=>{
     string id="local-intercept:"+shot.Id+":"+e.Id+":"+session.Tick;var source=session.PreviewInterception(id,e.Slot);var phases=BInterceptApprovedLocalPolicy.Ticks(e.Definition,input.StepTimeSpanTicks/10,(long)source["interceptionHasteBps"]);return new{actor=e,id,source,phases};
    }).Where(c=>checked(session.Tick+c.phases[0])<=shot.Contact).OrderBy(c=>c.phases[0]).ThenBy(c=>c.actor.Slot).ThenBy(c=>c.actor.Id,StringComparer.Ordinal).ToArray();
    if(candidates.Length==0)continue;var c=candidates[0];BuffWire.Need(actions.Count<capacity,"b_intercept_action_capacity");var timing=session.CaptureInterception(c.id,c.actor.Slot);BuffWire.Need(JToken.DeepEquals(timing,c.source),"b_intercept_preview_conflict");
    long total=checked(c.phases[0]+c.phases[1]);session.BeginBasicOccupation(c.actor.Id,total);shot.ReservedBy=c.actor.Id;
    actions.Add(new ActionState{Actor=c.actor.Id,Shot=shot.Id,Accepted=session.Tick,Ready=checked(session.Tick+c.phases[0]),Recovery=c.phases[1],End=checked(session.Tick+total),TimingSource=timing});Event("ACCEPTED",c.actor.Id,shot.Id,session.Tick,next);
   }
  }
  internal void AcceptSource(string shotId,LastRail.Run.BInterceptTimingSnapshot receipt,BGunnerProjectileOwner projectiles,BDirectAttackBuffSession session,Func<long> next){
   string actor=receipt.OperatorId;var shot=projectiles.All.SingleOrDefault(s=>s.Id==shotId);var input=binding.Composition.Attack;
   BuffWire.Need(NamedSourceBound&&!terminal&&!session.Paused&&input.Entities.ContainsKey(actor)&&input.Entities[actor].Kind=="OPERATOR"&&receipt.StepMicroseconds==input.StepTimeSpanTicks/10,"b_intercept_native_step_or_holder");
   BuffWire.Need(shot!=null&&shot.Terminal=="FLYING"&&shot.ReservedBy==null&&!Busy(actor,session.Tick)&&Eligible(actor,shot,session)&&session.Tick<shot.Contact&&checked(session.Tick+receipt.PreparationTicks)<=shot.Contact,"b_intercept_native_reject_before_acceptance");
   BuffWire.Need(!actions.Any(a=>a.TimingSource["timing"]!=null&&(string)a.TimingSource["timing"]["eventId"]==receipt.EventId),"b_intercept_receipt_used");BuffWire.Need(actions.Count<capacity,"b_intercept_action_capacity");session.AcceptReceivedInterception(receipt);
   long total=checked(receipt.PreparationTicks+receipt.RecoveryTicks);session.BeginBasicOccupation(actor,total);shot.ReservedBy=actor;actions.Add(new ActionState{Actor=actor,Shot=shotId,Accepted=session.Tick,Ready=checked(session.Tick+receipt.PreparationTicks),Recovery=receipt.RecoveryTicks,End=checked(session.Tick+total),TimingSource=receipt.Capture()});Event("ACCEPTED_SOURCE_V3",actor,shotId,session.Tick,next);
  }
  internal void CancelLocalShot(string shotId,BGunnerProjectileOwner projectiles,BDirectAttackBuffSession session,Func<long> next){var shot=projectiles.All.SingleOrDefault(s=>s.Id==shotId);BuffWire.Need(shot!=null,"b_intercept_unknown_shot");if(shot.Terminal!="FLYING")return;shot.Terminal="MISS";Event("EXPLICIT_LOCAL_EXTERNAL_TERMINAL",null,shotId,session.Tick,next);BeforeActions(projectiles,session,next);}
  internal void Terminal(BGunnerProjectileOwner projectiles,BDirectAttackBuffSession session,Func<long> next){if(terminal)return;terminal=true;manual.Clear();foreach(var a in actions.Where(a=>a.Phase=="PREPARATION"||a.Phase=="RECOVERY")){projectiles.All.Single(s=>s.Id==a.Shot).ReservedBy=null;a.Phase="TERMINAL";Event("TERMINAL",a.Actor,a.Shot,session.Tick,next);}}
  internal JObject Capture()=>new JObject{["terminal"]=terminal,["policyVersion"]=BInterceptApprovedLocalPolicy.Version,["actions"]=new JArray(actions.Select(a=>a.Capture())),["localManualOccupations"]=JObject.FromObject(manual),["events"]=events.DeepClone()};
 }
}


