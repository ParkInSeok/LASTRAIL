using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns phase/reservations only. The native session remains the sole clock, HP and next-impact owner.
 internal sealed class BEnemyBasicTimingOwner {
  const long WindupMilliseconds=500,RecoveryMilliseconds=300;
  internal const string Version="unity.combat-b.enemy-basic-timing-local.v1";
  internal const string WaveVersion="unity.combat-b.wave-enemy-timing-local.v1.20261004";
  sealed class Actor {internal string Id,Target,Phase="IDLE";internal long Targetable,Period,Windup,Recovery,Due,Reservation,OccupiedUntil;internal bool RefreshPeriod;internal Actor Copy()=>(Actor)MemberwiseClone();}
  readonly Func<string,long,CombatExactValue> effectivePeriod;readonly string[] routedIds;readonly JObject config;readonly string bindingHash;readonly long stepMicroseconds;readonly Dictionary<string,BEnemyBasicTimingSource> sources=new Dictionary<string,BEnemyBasicTimingSource>(StringComparer.Ordinal);readonly List<Actor> actors=new List<Actor>();readonly JArray history=new JArray();BEnemyAttackAdmissionOwner admission;
  internal bool HasAdmission=>admission!=null;
  internal string Hash {get;}
  internal BEnemyBasicTimingOwner(string sourceHash,long stepMicroseconds,IEnumerable<BEnemyBasicTimingSource> timingSources,IBEnemyBasicTimingContext session):this(sourceHash,stepMicroseconds,timingSources,session,null){}
  internal BEnemyBasicTimingOwner(string sourceHash,long stepMicroseconds,IEnumerable<BEnemyBasicTimingSource> timingSources,IBEnemyBasicTimingContext session,Func<string,long,CombatExactValue> effectivePeriod){
   this.effectivePeriod=effectivePeriod;
   BuffWire.Need(!string.IsNullOrEmpty(sourceHash)&&stepMicroseconds>0,"b_enemy_timing_binding");bindingHash=sourceHash;this.stepMicroseconds=stepMicroseconds;
   var rows=timingSources.OrderBy(x=>x.Ordinal).ToArray();BuffWire.Need(rows.Length>0&&rows.Select(x=>x.Id).Distinct().Count()==rows.Length&&rows.Select(x=>x.Ordinal).Distinct().Count()==rows.Length,"b_enemy_timing_route");
   routedIds=rows.Select(x=>x.Id).ToArray();
   config=new JObject{["bindingHash"]=sourceHash,["stepMicroseconds"]=stepMicroseconds,["timings"]=new JArray(rows.Select(x=>new JObject{["sourceId"]=x.Id,["targetId"]=x.Target,["periodMicroseconds"]=x.PeriodMicroseconds,["targetableAt"]=x.TargetableAt,["projectile"]=x.Projectile,["ordinal"]=x.Ordinal}))};Hash=BDirectAttackInput.Digest(config);
   foreach(var row in rows){var actor=new Actor{Id=row.Id,Target=row.Target,Targetable=row.TargetableAt,Period=Duration(row.PeriodMicroseconds,stepMicroseconds),Windup=Duration(WindupMilliseconds*1000,stepMicroseconds),Recovery=Duration(RecoveryMilliseconds*1000,stepMicroseconds)};BuffWire.Need(actor.Period>=SafeAdd(actor.Windup,actor.Recovery),"b_enemy_timing_unsupported_overlap");actors.Add(actor);sources.Add(row.Id,row);}
   foreach(var actor in actors){ReserveCycle(actor,session.Tick);session.ScheduleEnemyBasic(actor.Id,SafeAdd(actor.Targetable,actor.Period));}
  }
  internal void ScheduleFirstRoofBasic(string id,long roofTick,IBEnemyBasicTimingContext session){var actor=actors.Single(x=>x.Id==id);if(actor.Phase=="IDLE")ReserveCycle(actor,roofTick);session.ScheduleEnemyBasic(id,Math.Max(session.NextBasicDue(id),SafeAdd(roofTick,actor.Period)));}
  void ReserveCycle(Actor actor,long tick){
   if(effectivePeriod==null)return;
   var timing=CombatActionTiming.BasicOpportunityBoundaries(0,sources[actor.Id].PeriodMicroseconds,WindupMilliseconds*1000,RecoveryMilliseconds*1000,effectivePeriod(actor.Id,tick),stepMicroseconds);
   actor.Period=timing.NextOpportunity;actor.Windup=timing.Commit;actor.Recovery=timing.RecoveryEnd-timing.Commit;actor.RefreshPeriod=false;
  }
  void CancelCycle(Actor actor,IBEnemyBasicTimingContext session,Func<long> next){
   bool recovery=effectivePeriod!=null&&(actor.Phase=="WINDUP"||actor.Phase=="RECOVERY");long end=recovery?(actor.Phase=="RECOVERY"?actor.Due:SafeAdd(session.Tick,actor.Recovery)):0;
   actor.Phase=recovery?"RECOVERY":"IDLE";actor.Due=recovery?end:0;actor.Reservation=recovery?next():0;actor.OccupiedUntil=0;actor.RefreshPeriod=effectivePeriod!=null;
  }
  static long SafeAdd(long a,long b){BuffWire.Need(a>=0&&b>=0&&a<=9007199254740991-b,"b_enemy_timing_overflow");return a+b;}
  static long Duration(long micros,long step)=>CombatActionTiming.BasicAttackPeriodTicks(micros,step,Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>());
#if !LASTRAIL_ORDINARY_CORE
  internal BEnemyBasicTimingOwner(BDirectAttackBasicLoopBinding original,JObject packet,string trustedHash,IBEnemyBasicTimingContext session){
   bool wave=original.Composition.Attack.IsWaveRuntime;
   var binding=original;routedIds=binding.OrderedSources.Where(id=>binding.Composition.Attack.Entities[id].Kind=="LOCAL_ENEMY").ToArray();bindingHash=original.OriginalHash;stepMicroseconds=original.Composition.Attack.StepTimeSpanTicks/10;config=(JObject)packet.DeepClone();Hash=trustedHash;BuffWire.Need(BDirectAttackInput.Digest(config)==trustedHash,"b_enemy_timing_hash");BuffWire.Fields(config,"version","provenance","bindingHash","timings");BuffWire.Need((string)config["version"]==(wave?WaveVersion:Version)&&(string)config["provenance"]=="EXPLICIT_LOCAL_TIMING_NOT_OPERATING_ENEMY_SOURCE"&&(string)config["bindingHash"]==binding.OriginalHash,"b_enemy_timing_binding");
   foreach(var token in BuffWire.Array(config["timings"],1,wave?32:16)){var row=BuffWire.Object(token);BuffWire.Fields(row,"sourceId","targetId","targetableAt","periodMicroseconds","windupMs","recoveryMs");string id=BuffWire.Text(row["sourceId"]),target=BuffWire.Text(row["targetId"]);BuffWire.Need(binding.OrderedSources.Contains(id)&&binding.Composition.Attack.Entities[id].Kind=="LOCAL_ENEMY"&&target==binding.Target(id)&&!actors.Any(a=>a.Id==id),"b_enemy_timing_route");var entity=binding.Composition.Attack.Entities[id];BuffWire.Need(BuffWire.Number(row["periodMicroseconds"],1)==entity.Interval&&BuffWire.Number(row["windupMs"])==WindupMilliseconds&&BuffWire.Number(row["recoveryMs"])==RecoveryMilliseconds,"b_enemy_timing_original_rule");long step=binding.Composition.Attack.StepTimeSpanTicks/10;
    var actor=new Actor{Id=id,Target=target,Targetable=BuffWire.Number(row["targetableAt"]),Period=Duration(entity.Interval,step),Windup=Duration(WindupMilliseconds*1000,step),Recovery=Duration(RecoveryMilliseconds*1000,step)};BuffWire.Need(actor.Period>=SafeAdd(actor.Windup,actor.Recovery),"b_enemy_timing_unsupported_overlap");session.ScheduleEnemyBasic(id,SafeAdd(actor.Targetable,actor.Period));actors.Add(actor);sources.Add(id,new BEnemyBasicTimingSource(id,target,entity.Interval,actor.Targetable,entity.Definition=="enemy_spike_cart_gunner",Array.IndexOf(((JArray)binding.Composition.Attack.Capture()["entities"]).Select(e=>(string)e["id"]).ToArray(),id)));
   }
   actors.Sort((a,b)=>Array.IndexOf(binding.OrderedSources.ToArray(),a.Id).CompareTo(Array.IndexOf(binding.OrderedSources.ToArray(),b.Id)));
  }
#endif
  internal void ConfigureAdmission(BEnemyAttackAdmissionPolicy policy){BuffWire.Need(admission==null,"b_admission_already_bound");BuffWire.Need(new HashSet<string>(routedIds).SetEquals(actors.Select(a=>a.Id)),"b_admission_partial_timing_roster");admission=new BEnemyAttackAdmissionOwner(policy,stepMicroseconds,actors.Select(a=>sources[a.Id]));}
  BEnemyBasicTimingOwner(BEnemyBasicTimingOwner value,Func<string,long,CombatExactValue> effectivePeriod){this.effectivePeriod=effectivePeriod;routedIds=(string[])value.routedIds.Clone();config=value.config;bindingHash=value.bindingHash;stepMicroseconds=value.stepMicroseconds;foreach(var pair in value.sources)sources.Add(pair.Key,pair.Value);Hash=value.Hash;actors.AddRange(value.actors.Select(a=>a.Copy()));foreach(var row in value.history)history.Add(row.DeepClone());admission=value.admission?.Clone();}
  internal BEnemyBasicTimingOwner Clone(){BuffWire.Need(effectivePeriod==null,"b_enemy_timing_clone_requires_rebound_period");return new BEnemyBasicTimingOwner(this,null);}
  internal BEnemyBasicTimingOwner CloneWithEffectivePeriod(Func<string,long,CombatExactValue> rebound){BuffWire.Need(rebound!=null,"b_enemy_timing_missing_rebound_period");return new BEnemyBasicTimingOwner(this,rebound);}
  internal bool Contains(string source)=>actors.Any(a=>a.Id==source);
  internal void RetargetWave(string id,string target,IBEnemyBasicTimingContext session,Func<long> next){var a=actors.Single(x=>x.Id==id);if(target==null||a.Target==target)return;admission.Retarget(id,target,session.Hp(target)==0?1:0,session.Tick);a.Target=target;if(effectivePeriod!=null){CancelCycle(a,session,next);if(a.Phase=="IDLE")ReserveCycle(a,session.Tick);}else{a.Phase=a.OccupiedUntil>session.Tick?"OCCUPIED":"IDLE";a.Due=0;a.Reservation=0;}session.ScheduleEnemyBasic(id,Math.Max(session.NextBasicDue(id),SafeAdd(Math.Max(session.Tick,a.OccupiedUntil),a.Period)));Event(a,"SPATIAL_RETARGET",session.Tick,next());}
  internal string Target(string id)=>actors.Single(x=>x.Id==id).Target;
  internal void ObserveWaveLifetime(string id,long sourceGeneration,long targetGeneration,bool eligible,IBEnemyBasicTimingContext session,Func<long> next){if(!admission.SameLifetime(id,sourceGeneration,targetGeneration,0,eligible)){bool entering=eligible&&!admission.Eligible(id);ObserveAdmission(id,sourceGeneration,targetGeneration,0,eligible,session.Tick,session,next);var actor=actors.Single(a=>a.Id==id);if(effectivePeriod!=null&&(entering||actor.RefreshPeriod)&&actor.Phase=="IDLE"){ReserveCycle(actor,session.Tick);session.ScheduleEnemyBasic(id,Math.Max(session.NextBasicDue(id),SafeAdd(session.Tick,actor.Period)));}}}
  internal void Terminal(IBEnemyBasicTimingContext session,Func<long> next){admission?.Terminal();foreach(var a in actors){if(a.Phase=="TERMINAL")continue;a.Phase="TERMINAL";a.Due=0;a.Reservation=0;a.OccupiedUntil=0;Event(a,"TERMINAL",session.Tick,next());}}
  void Event(Actor a,string kind,long tick,long sequence)=>history.Add(new JObject{["kind"]=kind,["sourceId"]=a.Id,["targetId"]=a.Target,["tick"]=tick,["sequence"]=sequence});
  internal void Occupy(string id,long recoveryEnd,IBEnemyBasicTimingContext session,Func<long> next){var a=actors.SingleOrDefault(x=>x.Id==id);BuffWire.Need(a!=null&&recoveryEnd>=session.Tick,"b_enemy_timing_occupation");long due=SafeAdd(recoveryEnd,a.Period);if(admission!=null){due=Math.Max(due,session.NextBasicDue(id));recoveryEnd=Math.Max(recoveryEnd,a.OccupiedUntil);}admission?.Occupy(id,recoveryEnd,session.Tick);a.Phase="OCCUPIED";a.OccupiedUntil=recoveryEnd;a.Reservation=0;a.Due=0;session.ScheduleEnemyBasic(id,due);Event(a,"OCCUPIED",session.Tick,next());}
  internal void ObserveAdmission(string id,long sourceGeneration,long targetGeneration,long fieldGeneration,bool eligible,long hazardUntil,IBEnemyBasicTimingContext session,Func<long> next){BuffWire.Need(admission!=null,"b_admission_unbound");foreach(var cancelled in admission.Observe(id,sourceGeneration,targetGeneration,fieldGeneration,eligible,hazardUntil,session.Tick)){var a=actors.Single(x=>x.Id==cancelled);long originalDue=session.NextBasicDue(a.Id);CancelCycle(a,session,next);session.ScheduleEnemyBasic(a.Id,Math.Max(originalDue,SafeAdd(session.Tick,a.Period)));Event(a,"LIFETIME_CANCEL",session.Tick,next());}}
  void StartDueWindup(Actor a,IBEnemyBasicTimingContext session,Func<long> next){
   if(admission!=null)return;
   if(a.Phase!="IDLE"||session.Tick<session.NextBasicDue(a.Id)-a.Windup)return;
   if(!session.EnemyBasicTargetEligible(a.Id,a.Target)){session.ScheduleEnemyBasic(a.Id,SafeAdd(session.Tick,a.Period));Event(a,"SKIP_INVALID",session.Tick,next());return;}
   a.Phase="WINDUP";a.Due=session.NextBasicDue(a.Id);a.Reservation=next();Event(a,"WINDUP",session.Tick,next());
  }
  void PrepareAdmission(IBEnemyBasicTimingContext session,Func<long> next){
    foreach(var a in actors){bool valid=admission.Eligible(a.Id)&&session.EnemyBasicTargetEligible(a.Id,a.Target);if(!valid){admission.ClearWaiting(a.Id);if(a.Phase=="WINDUP"){admission.Finish(a.Id,session.Tick);CancelCycle(a,session,next);session.ScheduleEnemyBasic(a.Id,SafeAdd(session.Tick,a.Period));Event(a,"CANCEL_INVALID",session.Tick,next());}continue;}if(a.Phase=="IDLE"&&session.Tick>=session.NextBasicDue(a.Id)-a.Windup)admission.Waiting(a.Id,session.Tick);}
    foreach(var id in admission.OrderedWaiting()){var a=actors.Single(x=>x.Id==id);if(a.Phase!="IDLE"||!session.EnemyBasicTargetEligible(a.Id,a.Target))continue;if(!admission.TryStart(id,session.Tick,a.Period))continue;a.Phase="WINDUP";a.Due=SafeAdd(session.Tick,a.Windup);a.Reservation=next();session.ScheduleEnemyBasic(a.Id,a.Due);Event(a,"WINDUP",session.Tick,next());}
  }
#if !LASTRAIL_ORDINARY_CORE
  internal void Collect(BDirectAttackBuffSession session,Func<long> next,List<BasicCombatSimulation.DueAction> due,List<BDirectAttackBuffReceipt> hits,Action<string,long> unboundLaunch=null,List<BasicCombatSimulation.DueAction> launchDue=null)=>Collect((IBEnemyBasicTimingContext)session,next,due,(eventId,source,target)=>hits.Add(session.ResolveLoopBasic(eventId,source,target)),unboundLaunch,launchDue);
#endif
  internal void Collect(IBEnemyBasicTimingContext session,Func<long> next,List<BasicCombatSimulation.DueAction> due,Action<string,string,string> resolveHit,Action<string,long> unboundLaunch=null,List<BasicCombatSimulation.DueAction> launchDue=null){
   if(admission!=null)PrepareAdmission(session,next);
   foreach(var actor in actors){var a=actor;
    if(a.Phase=="OCCUPIED"&&session.Tick>=a.OccupiedUntil){a.Phase="IDLE";Event(a,"OCCUPATION_END",session.Tick,next());}
    StartDueWindup(a,session,next);
    long reservation=a.Reservation;
    if(a.Phase=="WINDUP"&&session.Tick>=a.Due)(launchDue!=null&&sources[a.Id].Projectile?launchDue:due).Add(new BasicCombatSimulation.DueAction(a.Due,a.Reservation,()=>{
     if(a.Phase!="WINDUP"||a.Reservation!=reservation)return;
     long sequence=next();if((admission==null||admission.Eligible(a.Id))&&session.EnemyBasicTargetEligible(a.Id,a.Target)){if(unboundLaunch!=null&&sources[a.Id].Projectile){unboundLaunch(a.Id,session.Tick);session.ScheduleEnemyBasic(a.Id,SafeAdd(session.Tick,a.Period));Event(a,launchDue==null?"UNSUPPORTED_PROJECTILE_LAUNCH":"PROJECTILE_LAUNCH",session.Tick,sequence);}else{resolveHit(BasicImpactId(a.Id,session.Tick),a.Id,a.Target);Event(a,"IMPACT",session.Tick,sequence);}}else {session.ScheduleEnemyBasic(a.Id,SafeAdd(session.Tick,a.Period));Event(a,"MISS_TARGET_INVALID",session.Tick,sequence);}
     a.Phase="RECOVERY";a.Due=SafeAdd(session.Tick,a.Recovery);a.Reservation=next();if(effectivePeriod!=null){ReserveCycle(a,session.Tick);session.ScheduleEnemyBasic(a.Id,SafeAdd(session.Tick,a.Period));}
    }));
    else if(a.Phase=="RECOVERY"&&session.Tick>=a.Due)due.Add(new BasicCombatSimulation.DueAction(a.Due,a.Reservation,()=>{if(a.Phase!="RECOVERY"||a.Reservation!=reservation)return;admission?.Finish(a.Id,session.Tick);a.Phase="IDLE";a.Due=0;a.Reservation=0;if(a.RefreshPeriod){ReserveCycle(a,session.Tick);session.ScheduleEnemyBasic(a.Id,Math.Max(session.NextBasicDue(a.Id),SafeAdd(session.Tick,a.Period)));}Event(a,"RECOVERY_END",session.Tick,next());if(admission!=null)PrepareAdmission(session,next);else StartDueWindup(a,session,next);}));
   }
  }
  string BasicImpactId(string source,long tick)=>"enemy-basic:"+BDirectAttackInput.Digest(new JObject{["bindingHash"]=bindingHash,["timingHash"]=Hash,["sourceId"]=source,["tick"]=tick}).Substring(7);
  internal JObject Capture(){var value=new JObject{["version"]=Version,["timingHash"]=Hash,["actors"]=new JArray(actors.Select(a=>new JObject{["sourceId"]=a.Id,["targetId"]=a.Target,["phase"]=a.Phase,["due"]=a.Due,["reservationSequence"]=a.Reservation,["occupiedUntil"]=a.OccupiedUntil})),["events"]=history.DeepClone()};if(effectivePeriod!=null)foreach(var row in (JArray)value["actors"]){var actor=actors.Single(a=>a.Id==(string)row["sourceId"]);row["reservedPeriod"]=actor.Period;row["reservedWindup"]=actor.Windup;row["reservedRecovery"]=actor.Recovery;row["refreshPeriod"]=actor.RefreshPeriod;}if(admission!=null)value["admission"]=admission.Capture();return value;}
 }
}



