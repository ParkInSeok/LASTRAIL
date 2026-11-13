using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Read-only authority adapters. Interception is an already ordered decision,
 // not a UI request and not permission to guess an absent event-order source.
 internal sealed class OrdinaryNodeGunnerSources {
  internal readonly Func<string,long,JObject> Observe;
  internal readonly Func<string,CombatExactValue,long,JObject> CaptureAttack;
  internal readonly Func<string,long,JObject> CaptureDefense;
  internal readonly Func<string,long,JObject> Interception;
  internal readonly Func<string,long,CombatExactValue> EffectivePeriod;
  internal OrdinaryNodeGunnerSources(Func<string,long,JObject> observe,Func<string,CombatExactValue,long,JObject> captureAttack,
   Func<string,long,JObject> captureDefense,Func<string,long,JObject> interception):this(observe,captureAttack,captureDefense,interception,null){}
  internal OrdinaryNodeGunnerSources(Func<string,long,JObject> observe,Func<string,CombatExactValue,long,JObject> captureAttack,
   Func<string,long,JObject> captureDefense,Func<string,long,JObject> interception,Func<string,long,CombatExactValue> effectivePeriod){
   Observe=observe??throw new ArgumentNullException(nameof(observe));CaptureAttack=captureAttack??throw new ArgumentNullException(nameof(captureAttack));
   CaptureDefense=captureDefense??throw new ArgumentNullException(nameof(captureDefense));Interception=interception??throw new ArgumentNullException(nameof(interception));
   EffectivePeriod=effectivePeriod;
  }
 }
 // Original v05 fixed-arrival projectiles. This is not the allied sweep policy.
 // Host transaction order: BeginStep -> allied actions/contacts -> ResolveStep.
 internal sealed class OrdinaryNodeGunnerExecution {
  readonly OrdinaryFiveCombatFactory factory;
  readonly BCombatLocalModel health;
  readonly OrdinaryNodeGunnerSources sources;
  readonly JObject definition;
  readonly long period,windup,recovery,stepUs,speed,range;
  readonly string actionId;
  readonly List<JObject> actors=new List<JObject>(),shots=new List<JObject>();
  readonly long startTick;
  long begun,resolved;
  internal OrdinaryNodeGunnerExecution(OrdinaryFiveCombatFactory factory,BCombatLocalModel health,OrdinaryNodeGunnerSources sources,long startTick=0){
   this.factory=factory??throw new ArgumentNullException(nameof(factory));this.health=health??throw new ArgumentNullException(nameof(health));this.sources=sources??throw new ArgumentNullException(nameof(sources));
   Need(factory.NodeInput!=null&&factory.Attempt!=null);RequireHealth(health);
   Need(startTick>=0);this.startTick=startTick;begun=resolved=startTick-1;
   definition=factory.NodeInput.Source.Enemies.CaptureActionDefinition();var spatial=definition["spatial"];var projectile=spatial?["projectile"];
   Need((string)definition["definitionId"]=="enemy_spike_cart_gunner"&&(long?)definition["basicPeriodMs"]==2000&&(long?)definition["preparationMs"]==500&&(long?)definition["recoveryMs"]==300);
   Need((string)spatial?["targetPolicy"]=="FARTHEST_LIVING_OPERATOR_IN_RANGE_AND_LOS_CAR_ORDER_THEN_ENTITY_ID"&&(bool?)spatial["lineOfSightRequired"]==true&&(bool?)spatial["stopDuringPreparationAndRecovery"]==true);
   Need((string)projectile?["periodBasis"]=="LAUNCH_TO_LAUNCH"&&(string)projectile["firstLaunch"]=="TARGETABLE_PLUS_FULL_PERIOD"&&(string)projectile["flight"]=="EXACT_DISTANCE_OVER_SPEED_CEIL_ONCE_AT_ORIGINAL_STEP_MINIMUM_ONE"&&(string)projectile["distanceExitAfterLaunch"]=="KEEP_PROJECTILE"&&(string)projectile["targetGenerationLoss"]=="MISS"&&(bool?)projectile["survivesSourceDeath"]==true);
   stepUs=factory.Step/10;period=Ticks((long)definition["basicPeriodMs"]);windup=Ticks((long)definition["preparationMs"]);recovery=Ticks((long)definition["recoveryMs"]);
   speed=(long)projectile["speedMmPerSecond"];range=(long)spatial["maximumDistanceMm"];Need(speed==24000&&range==24000&&period>=windup+recovery);
   actionId=(string)definition["rawAction"]?["sourceId"];BCombatFormation.Identity(actionId);
   foreach(var gunner in factory.NodeInput.Source.Enemies.Gunners)actors.Add(new JObject{["actorId"]=gunner.InstanceId,["phase"]="IDLE",["targetId"]=null,["targetGeneration"]=null,["sourceGeneration"]=null,["nextLaunch"]=null,["commit"]=null,["startedAt"]=null,["recoveryEnd"]=null,["launchCount"]=0});
  }
  long Ticks(long ms)=>checked((ms*1000+stepUs-1)/stepUs);
  static void Need(bool valid){if(!valid)throw new ArgumentException("Original node gunner execution source differs.");}
  void RequireHealth(BCombatLocalModel candidate){
   Need(candidate!=null);var state=candidate.CaptureIdentity();var d=state["definition"];var a=factory.Attempt;
   Need((string)state["schema"]=="unity.combat-b.node-health-local.v1.20261102"&&d!=null&&(string)d["accountId"]==a.AccountId&&(string)d["runId"]==a.RunId&&(string)d["nodeId"]==a.NodeId&&(string)d["snapshotHash"]==a.SnapshotHash&&(long?)d["generation"]==a.Generation&&(long?)d["attemptRevision"]==a.Revision);
  }
  internal OrdinaryNodeGunnerExecution CloneOwned(BCombatLocalModel newHealth)=>CloneOwnedWithSources(newHealth,sources);
  internal OrdinaryNodeGunnerExecution CloneOwnedWithSources(BCombatLocalModel newHealth,OrdinaryNodeGunnerSources newSources){
   Need(newHealth!=null&&!ReferenceEquals(health,newHealth)&&health.HasSameState(newHealth));
   var copy=new OrdinaryNodeGunnerExecution(factory,newHealth,newSources,startTick){begun=begun,resolved=resolved};copy.actors.Clear();
   foreach(var actor in actors)copy.actors.Add((JObject)actor.DeepClone());foreach(var shot in shots)copy.shots.Add((JObject)shot.DeepClone());return copy;
  }
  JObject Attempt()=>new JObject{["nodeId"]=factory.Attempt.NodeId,["generation"]=factory.Attempt.Generation,["revision"]=factory.Attempt.Revision,["snapshotHash"]=factory.Attempt.SnapshotHash};
  static long[] Point(JToken point){Need(point is JArray a&&a.Count==3&&a.All(x=>x.Type==JTokenType.Integer));var values=point.Select(x=>(long)x).ToArray();Need(values.All(x=>x>=-1000000000&&x<=1000000000));return values;}
  static BigInteger Square(JToken from,JToken to){var a=Point(from);var b=Point(to);BigInteger result=0;for(int i=0;i<3;i++){BigInteger d=(BigInteger)a[i]-b[i];result+=d*d;}return result;}
  static BigInteger CeilRoot(BigInteger square){if(square==0)return 0;BigInteger value=square,next=(value+1)/2;while(next<value){value=next;next=(value+square/value)/2;}return value*value==square?value:value+1;}
  JObject Observe(string gunner,long tick){
   var o=sources.Observe(gunner,tick);Need(o!=null&&(long?)o["tick"]==tick&&(long?)o["revision"]>=0&&(string)o["nodeId"]==factory.Attempt.NodeId&&(string)o["snapshotHash"]==factory.Attempt.SnapshotHash&&(long?)o["attemptGeneration"]==factory.Attempt.Generation);
   var s=o["source"];Need(s!=null&&(string)s["id"]==gunner&&(long?)s["generation"]>=1);Point(s["muzzleMm"]);BCombatFormation.Identity((string)s["executorId"]);
   Need(new[]{"STATIONARY","MOVING"}.Contains((string)s["motionStatus"])&&o["targets"] is JArray);
   var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var target in (JArray)o["targets"]){
    string id=(string)target["id"];Need(factory.Original.Actors.Any(a=>a.OperatorId==id)&&ids.Add(id)&&(long?)target["generation"]>=1&&(long?)target["carOrder"]>=0&&target["lineOfSight"]?.Type==JTokenType.Boolean);Point(target["pointMm"]);
   }
   return (JObject)o.DeepClone();
  }
  bool Eligible(JObject o,JToken target)=>target!=null&&health.OperatorAlive((string)target["id"])&&(bool)target["lineOfSight"]&&Square(o["source"]["muzzleMm"],target["pointMm"])<=(BigInteger)range*range;
  internal Func<string,long,string> TauntTarget;
  JObject Select(JObject o){
   string forced=TauntTarget?.Invoke((string)o["source"]["id"],(long)o["source"]["generation"]);
   return ((JArray)o["targets"]).OfType<JObject>().Where(t=>Eligible(o,t)&&(forced==null||(string)t["id"]==forced)).OrderByDescending(t=>Square(o["source"]["muzzleMm"],t["pointMm"])).ThenBy(t=>(long)t["carOrder"]).ThenBy(t=>(string)t["id"],StringComparer.Ordinal).FirstOrDefault();
  }
  void NoTarget(JObject observation){
   string state=(string)observation["noTargetStatus"];
   if(state=="WAITING_FOR_RETURN"||state=="ORIGINAL_SAFE_STOP_OWNER")return;
   if(state=="MOVING_ON_CONFIRMED_PATH"&&(string)observation["source"]["motionStatus"]=="MOVING")return;
   throw new NotSupportedException("Original gunner reachable path or return-state source is unavailable.");
  }
  long Reserved(JObject actor,string field,long fallback)=>(long?)actor[field]??fallback;
  void Reserve(JObject actor,long tick){
   if(sources.EffectivePeriod==null)return;
   var effective=sources.EffectivePeriod((string)actor["actorId"],tick);Need(effective!=null&&effective.Numerator>0);
   var timing=CombatActionTiming.BasicOpportunityBoundaries(0,checked((long)definition["basicPeriodMs"]*1000),checked((long)definition["preparationMs"]*1000),checked((long)definition["recoveryMs"]*1000),effective,stepUs);
   actor["reservedPeriod"]=timing.NextOpportunity;actor["reservedWindup"]=timing.Commit;actor["reservedRecovery"]=timing.RecoveryEnd-timing.Commit;
  }
  void CancelPreparation(JObject actor,long tick){actor["phase"]="RECOVERY";actor["recoveryEnd"]=checked(tick+Reserved(actor,"reservedRecovery",recovery));actor["nextLaunch"]=checked(tick+Reserved(actor,"reservedPeriod",period));actor["targetId"]=null;actor["targetGeneration"]=null;}
  internal void BeginStep(long tick){
   Need(tick>=startTick);
   if(tick==begun&&begun>=0)return;Need(tick>=0&&begun==resolved&&tick==resolved+1);
   foreach(var actor in actors){
    string id=(string)actor["actorId"];
    if(health.Hp(id)<=0||health.TrainDefeated){actor["phase"]="DOWN";continue;}
    var o=Observe(id,tick);var source=o["source"];
    if((string)actor["phase"]=="RECOVERY"){
     if(tick<(long)actor["recoveryEnd"]){Need((string)source["motionStatus"]=="STATIONARY");continue;}actor["phase"]="IDLE";
    }
    if((string)actor["phase"]=="WINDUP"){
     Need((string)source["motionStatus"]=="STATIONARY");
     var target=((JArray)o["targets"]).SingleOrDefault(t=>(string)t["id"]==(string)actor["targetId"]);
     string forced=TauntTarget?.Invoke(id,(long)source["generation"]);
     if(forced!=null&&forced!=(string)actor["targetId"]){CancelPreparation(actor,tick);continue;}
     if(!Eligible(o,target)||(long?)target["generation"]!=(long?)actor["targetGeneration"]||(long?)source["generation"]!=(long?)actor["sourceGeneration"]){CancelPreparation(actor,tick);continue;}
     if(tick>=(long)actor["commit"])Launch(actor,o,(JObject)target,tick);
     continue;
    }
    var selected=Select(o);
    if(selected==null){NoTarget(o);actor["targetId"]=null;actor["targetGeneration"]=null;actor["nextLaunch"]=null;continue;}
    if((string)source["motionStatus"]!="STATIONARY"){
     if((string)o["noTargetStatus"]!="MOVING_ON_CONFIRMED_PATH")throw new NotSupportedException("Original gunner movement owner is unavailable.");
     continue;
    }
    if((string)actor["targetId"]!=(string)selected["id"]||(long?)actor["targetGeneration"]!=(long?)selected["generation"]||(long?)actor["sourceGeneration"]!=(long?)source["generation"]){
     Reserve(actor,tick);
     actor["targetId"]=selected["id"].DeepClone();actor["targetGeneration"]=selected["generation"].DeepClone();actor["sourceGeneration"]=source["generation"].DeepClone();
     actor["nextLaunch"]=Math.Max((long?)actor["nextLaunch"]??0,checked(tick+Reserved(actor,"reservedPeriod",period)));
    }
    long reservedWindup=Reserved(actor,"reservedWindup",windup);
    if(tick>=(long)actor["nextLaunch"]-reservedWindup){actor["phase"]="WINDUP";actor["startedAt"]=tick;actor["commit"]=checked(tick+reservedWindup);}
   }
   begun=tick;
  }
  void Launch(JObject actor,JObject observation,JObject target,long tick){
   Need(shots.Count<10000);string id=(string)actor["actorId"];
   var raw=factory.NodeInput.Source.Enemies.Gunners.Single(g=>g.InstanceId==id).RawDamage;
   var attack=sources.CaptureAttack(id,raw,tick);Need(attack!=null);_=CombatProjectileAttackSnapshot.Capture(attack);
   var square=Square(observation["source"]["muzzleMm"],target["pointMm"]);BigInteger unit=(BigInteger)speed*stepUs;
   long flight=checked((long)BigInteger.Max(1,(CeilRoot(square*1000000000000L)+unit-1)/unit));
   long ordinal=checked((long)actor["launchCount"]+1);string eventId="node-gunner:"+BDirectAttackInput.Digest(new JArray(factory.ConfigurationVersion,id,ordinal)).Substring(7);
   shots.Add(new JObject{["eventId"]=eventId,["projectileId"]=eventId+":shot",["sequence"]=shots.Count+1,["actorId"]=id,["abilityId"]=actionId,["executorId"]=observation["source"]["executorId"].DeepClone(),
    ["sourceGeneration"]=observation["source"]["generation"].DeepClone(),["targetId"]=target["id"].DeepClone(),["targetGeneration"]=target["generation"].DeepClone(),["attempt"]=Attempt(),
    ["launchTick"]=tick,["contactTick"]=checked(tick+flight),["distanceSquaredMm"]=square.ToString(),["launchObservation"]=observation.DeepClone(),["rawDamage"]=raw.Snapshot(),["attack"]=attack.DeepClone(),["status"]="FLYING",["interception"]=null,["receipt"]=null});
   actor["launchCount"]=ordinal;actor["phase"]="RECOVERY";actor["recoveryEnd"]=checked(tick+Reserved(actor,"reservedRecovery",recovery));Reserve(actor,tick);actor["nextLaunch"]=checked(tick+Reserved(actor,"reservedPeriod",period));
  }
  internal IReadOnlyList<JObject> ResolveStep(long tick){
   Need(tick>=startTick);
   if(tick==resolved&&resolved>=0)return Array.Empty<JObject>();Need(tick>=0&&tick==begun&&resolved==tick-1);
   var results=new List<JObject>();
   foreach(var shot in shots.Where(s=>(string)s["status"]=="FLYING").OrderBy(s=>(long)s["contactTick"]).ThenBy(s=>(long)s["sequence"])){
    string projectile=(string)shot["projectileId"],target=(string)shot["targetId"];
    var order=sources.Interception(projectile,tick);
    if(order==null||(string)order["status"]=="UNRESOLVED")throw new NotSupportedException("Original gunner interception event ordering is unavailable.");
    Need((string)order["shotId"]==projectile&&(long?)order["tick"]==tick);BCombatFormation.Identity((string)order["eventId"]);
    string decision=(string)order["status"];Need(decision=="CLEAR"||decision=="INTERCEPTED");
    shot["interception"]=order.DeepClone();
    if(decision=="INTERCEPTED"){shot["status"]="INTERCEPTED";continue;}
    if(tick<(long)shot["contactTick"])continue;
    var observation=Observe((string)shot["actorId"],tick);var live=((JArray)observation["targets"]).SingleOrDefault(t=>(string)t["id"]==target);
    // No post-launch distance or LOS check: v05 fixes arrival and retains the shot.
    if(live==null||(long?)live["generation"]!=(long?)shot["targetGeneration"]||!health.OperatorAlive(target)){shot["status"]="MISS";continue;}
    var arrival=sources.CaptureDefense(target,tick);Need(arrival!=null);
    int magnitude=CombatProjectileAttackSnapshot.Capture((JObject)shot["attack"]).Evaluate(arrival).ScaleAndFloor(10000);
    var receipt=health.ApplyResolvedDamage((string)shot["eventId"]+":hp",target,magnitude,tick);
    var hp=new JObject{["eventId"]=receipt.EventId,["targetId"]=receipt.TargetId,["tick"]=receipt.Tick,["hpLoss"]=receipt.HpLoss,["hpAfter"]=receipt.HpAfter,["absorbed"]=receipt.Absorbed,["trainDefeated"]=receipt.TrainDefeated};
    shot["receipt"]=hp;shot["status"]="HIT";
    results.Add(new JObject{["kind"]="HIT",["eventId"]=shot["eventId"].DeepClone(),["actorId"]=shot["actorId"].DeepClone(),["abilityId"]=actionId,["executorId"]=shot["executorId"].DeepClone(),["projectileId"]=projectile,["targetId"]=target,["targetGeneration"]=shot["targetGeneration"].DeepClone(),["attempt"]=Attempt(),["receipt"]=hp.DeepClone()});
   }
   resolved=tick;return results.AsReadOnly();
  }
  internal IReadOnlyList<JObject> Step(long tick){BeginStep(tick);return ResolveStep(tick);}
  internal long StartTick=>startTick;
  internal bool HasFlying=>shots.Any(s=>(string)s["status"]=="FLYING");
  internal JArray ReadPresentationShots(long tick){
   long recentTicks=(1000000+stepUs-1)/stepUs;
   var rows=new JArray();
   foreach(var shot in shots){
    string status=(string)shot["status"];
    long finished=status=="INTERCEPTED"?(long)shot["interception"]["tick"]:(long)shot["contactTick"];
    if(status!="FLYING"&&tick-finished>recentTicks)continue;
    var observation=shot["launchObservation"];var target=observation["targets"].Single(t=>(string)t["id"]==(string)shot["targetId"]);
    rows.Add(new JObject{["projectileId"]=shot["projectileId"].DeepClone(),["sourceId"]=shot["actorId"].DeepClone(),["targetId"]=shot["targetId"].DeepClone(),
     ["launchTick"]=shot["launchTick"].DeepClone(),["contactTick"]=shot["contactTick"].DeepClone(),["status"]=status,
     ["muzzleMm"]=observation["source"]["muzzleMm"].DeepClone(),["targetPointMm"]=target["pointMm"].DeepClone()});
   }
   return rows;
  }
  internal JObject ReadActor(string id)=>(JObject)actors.Single(a=>(string)a["actorId"]==id).DeepClone();
  internal JObject Capture()=>new JObject{["version"]="ordinary.node-gunner-execution.v1.20261102",["configuration"]=factory.ConfigurationVersion,["attempt"]=Attempt(),["definition"]=definition.DeepClone(),["startTick"]=startTick,["begunTick"]=begun,["resolvedTick"]=resolved,["actors"]=new JArray(actors.Select(a=>a.DeepClone())),["shots"]=new JArray(shots.Select(s=>s.DeepClone()))};
 }
}
