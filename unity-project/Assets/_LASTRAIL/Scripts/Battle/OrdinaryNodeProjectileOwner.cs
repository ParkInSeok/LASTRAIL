using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Owned by the node transaction. Observations and numeric sources come from its
 // authority adapter, never UI; this component does not select targets or grant firing opportunities.
 internal sealed class OrdinaryNodeProjectileOwner {
  readonly OrdinaryFiveCombatFactory factory;
  readonly BCombatLocalModel health;
  readonly Func<bool> current;
  readonly Dictionary<string,JObject> shots=new Dictionary<string,JObject>(StringComparer.Ordinal);
  internal bool UsesHealth(BCombatLocalModel candidate)=>ReferenceEquals(health,candidate);
  internal bool HasSameState(OrdinaryNodeProjectileOwner other)=>other!=null&&JToken.DeepEquals(Attempt(),other.Attempt())&&shots.Count==other.shots.Count
   &&shots.All(pair=>other.shots.TryGetValue(pair.Key,out var value)&&JToken.DeepEquals(pair.Value,value));
  internal OrdinaryNodeProjectileOwner(OrdinaryFiveCombatFactory factory,BCombatLocalModel health,Func<bool> currentAttempt){
   this.factory=factory??throw new ArgumentNullException(nameof(factory));this.health=health??throw new ArgumentNullException(nameof(health));
   current=currentAttempt??throw new ArgumentNullException(nameof(currentAttempt));
   if(factory.NodeInput==null||factory.Attempt==null)throw new ArgumentException("Original node attempt required.");
   var state=health.CaptureIdentity();var d=state["definition"];var attempt=factory.Attempt;
   Need((string)state["schema"]=="unity.combat-b.node-health-local.v1.20261102"&&d!=null&&(string)d["accountId"]==attempt.AccountId&&(string)d["runId"]==attempt.RunId&&(string)d["nodeId"]==attempt.NodeId&&(string)d["snapshotHash"]==attempt.SnapshotHash&&(long?)d["generation"]==attempt.Generation&&(long?)d["attemptRevision"]==attempt.Revision);
  }
  internal OrdinaryNodeProjectileOwner CloneOwned(BCombatLocalModel newHealth)=>CloneOwned(newHealth,current);
  internal OrdinaryNodeProjectileOwner CloneOwned(BCombatLocalModel newHealth,Func<bool> newCurrentAttempt){
   Need(newHealth!=null&&!ReferenceEquals(health,newHealth)&&health.HasSameState(newHealth));
   var copy=new OrdinaryNodeProjectileOwner(factory,newHealth,newCurrentAttempt);
   foreach(var pair in shots)copy.shots.Add(pair.Key,(JObject)pair.Value.DeepClone());return copy;
  }
  void Running(){if(!current())throw new InvalidOperationException("Node projectile attempt is retired.");}
  JObject Attempt()=>new JObject{["nodeId"]=factory.Attempt.NodeId,["generation"]=factory.Attempt.Generation,["revision"]=factory.Attempt.Revision,["snapshotHash"]=factory.Attempt.SnapshotHash};
  static void Need(bool value){if(!value)throw new ArgumentException("Original node projectile source differs.");}
  OrdinaryBasicSource.Profile Observe(JObject observation,string actor){
   Need(observation!=null&&observation["tick"]?.Type==JTokenType.Integer&&(long)observation["tick"]>=0&&observation["revision"]?.Type==JTokenType.Integer&&(long)observation["revision"]>=0);
   Need((string)observation["nodeId"]==factory.Attempt.NodeId&&(string)observation["snapshotHash"]==factory.Attempt.SnapshotHash&&(long?)observation["attemptGeneration"]==factory.Attempt.Generation);
   var p=factory.Original.Basic.Profiles.SingleOrDefault(x=>x.OperatorId==actor);Need(p!=null);
   Need(factory.Original.Actors.Any(x=>x.OperatorId==actor));
   var profile=observation["profile"];var b=profile?["ballistics"];var source=observation["source"];
   Need(profile!=null&&BDirectAttackInput.Digest(profile)==(string)observation["profileHash"]&&(string)profile["operatorId"]==actor&&(string)profile["abilityId"]==p.AbilityId&&(string)profile["launchSource"]==p.LaunchSource);
   // The solver clips exact distance separately. Ceil converts a rational lifetime
   // to the first integer microsecond at/after expiry without shortening its ray.
   long maxUs=checked((p.FlightNumeratorMs*1000+p.FlightDenominator-1)/p.FlightDenominator);
   Need(b!=null&&(long?)b["speedMmPerSecond"]==p.SpeedMmPerSecond&&(long?)b["radiusMm"]==p.RadiusMm&&(long?)b["maxDistanceMm"]==p.MaximumFlightDistanceMm&&(long?)b["adjacentCars"]==p.AdjacentCars&&(long?)b["maxFlightUs"]==maxUs);
   Need(source!=null&&(string)source["id"]==actor&&(long?)source["generation"]>=1);
   BCombatFormation.Identity((string)source["executionEntityId"]);
   Need(observation["targets"] is JArray);
   foreach(var target in (JArray)observation["targets"]){
    Need(factory.EnemyIds.Contains((string)target["id"]));
    Need((long?)target["generation"]>=1);
   }
   checked{_=(long)observation["tick"]*(factory.Step/10);}
   return p;
  }
  internal bool Launch(string eventId,string actorId,string projectileId,string targetId,JObject observation,string observationHash,JObject completedAttackSnapshot){
   Running();BCombatFormation.Identity(eventId);BCombatFormation.Identity(projectileId);BCombatFormation.Identity(targetId);
   Need(observation!=null&&BDirectAttackInput.Digest(observation)==observationHash);
   var profile=Observe(observation,actorId);
   var command=new JObject{["eventId"]=eventId,["actorId"]=actorId,["projectileId"]=projectileId,["targetId"]=targetId,["observation"]=observation.DeepClone(),["observationHash"]=observationHash,["attack"]=completedAttackSnapshot?.DeepClone()};
   if(shots.TryGetValue(projectileId,out var old)){Need(old["launchHash"]!=null?BDirectAttackInput.Digest(command)==(string)old["launchHash"]:JToken.DeepEquals(old["launch"],command));return false;}
   Need(!shots.Values.Any(x=>(string)x["eventId"]==eventId));
   if(shots.Count>=10000)throw new InvalidOperationException("Node projectile capacity reached.");
   Need(health.OperatorAlive(actorId)&&health.Hp(targetId)>0);
   _=CombatProjectileAttackSnapshot.Capture(completedAttackSnapshot);
   var ray=MiraJudgementGeometry.CreateRay(observation,observationHash,targetId);
   var source=observation["source"];var target=((JArray)observation["targets"]).Single(t=>(string)t["id"]==targetId);
   var muzzle=(JArray)source["muzzleMm"];var anchor=(JArray)target["anchorMm"];
   System.Numerics.BigInteger square=0;for(int i=0;i<3;i++){var distance=(System.Numerics.BigInteger)(long)muzzle[i]-(long)anchor[i];square+=distance*distance;}
   Need(square<=(System.Numerics.BigInteger)profile.MaximumDistanceMm*profile.MaximumDistanceMm);
   shots.Add(projectileId,new JObject{["eventId"]=eventId,["actorId"]=actorId,["abilityId"]=profile.AbilityId,["executorId"]=source["executionEntityId"].DeepClone(),["sourceGeneration"]=source["generation"].DeepClone(),["projectileId"]=projectileId,["targetId"]=targetId,["targetGeneration"]=target["generation"].DeepClone(),["attempt"]=Attempt(),["launchTick"]=observation["tick"].DeepClone(),["lastTick"]=observation["tick"].DeepClone(),["latestObservation"]=observation.DeepClone(),["launch"]=command,["ray"]=ray,["rayHash"]=BDirectAttackInput.Digest(ray),["status"]="FLYING",["lastAdvance"]=null,["result"]=null});
   return true;
  }
  internal JObject Advance(string projectileId,JObject begin,JObject end,JObject targetDefenseSnapshot){
   Running();if(!shots.TryGetValue(projectileId,out var shot))throw new ArgumentException("Unknown node projectile.");
   var command=new JObject{["begin"]=begin?.DeepClone(),["end"]=end?.DeepClone(),["arrival"]=targetDefenseSnapshot?.DeepClone()};
   if(shot["lastAdvanceHash"]!=null?BDirectAttackInput.Digest(command)==(string)shot["lastAdvanceHash"]:JToken.DeepEquals(shot["lastAdvance"],command))return (JObject)shot["result"].DeepClone();
   if((string)shot["status"]!="FLYING")throw new InvalidOperationException("Node projectile already resolved.");
   string actor=(string)shot["actorId"],target=(string)shot["targetId"];
   Observe(begin,actor);Observe(end,actor);
   Need(JToken.DeepEquals(begin,shot["latestObservation"]));
   Need((long)begin["tick"]==(long)shot["lastTick"]&&(long)end["tick"]>(long)begin["tick"]);
   Need((string)begin["source"]["executionEntityId"]==(string)shot["executorId"]&&(string)end["source"]["executionEntityId"]==(string)shot["executorId"]);
   long launch=(long)shot["launchTick"],unit=factory.Step/10;
   var trace=MiraJudgementGeometry.TraceReserved((JObject)shot["ray"],(string)shot["rayHash"],begin,end,checked(((long)begin["tick"]-launch)*unit),checked(((long)end["tick"]-launch)*unit),target,(long)shot["targetGeneration"]);
   string kind=(string)trace["kind"];
   var result=new JObject{["kind"]=kind,["eventId"]=shot["eventId"].DeepClone(),["actorId"]=actor,["abilityId"]=shot["abilityId"].DeepClone(),["executorId"]=shot["executorId"].DeepClone(),["projectileId"]=projectileId,["targetId"]=target,["targetGeneration"]=shot["targetGeneration"].DeepClone(),["attempt"]=Attempt(),["contact"]=trace,["receipt"]=null};
   if(kind=="HIT"){
    if(health.Hp(target)<=0){kind="TARGET_LOST";result["kind"]=kind;}
    else{
     var amount=CombatProjectileAttackSnapshot.Capture((JObject)shot["launch"]["attack"]).Evaluate(targetDefenseSnapshot).ScaleAndFloor(10000);
     string receiptId="node-projectile:"+BDirectAttackInput.Digest(new JArray(factory.ConfigurationVersion,(string)shot["eventId"],projectileId,target)).Substring(7);
     var receipt=health.ApplyResolvedDamage(receiptId,target,amount,(long)end["tick"]);
     result["receipt"]=new JObject{["eventId"]=receipt.EventId,["targetId"]=receipt.TargetId,["tick"]=receipt.Tick,["hpLoss"]=receipt.HpLoss,["hpAfter"]=receipt.HpAfter,["absorbed"]=receipt.Absorbed,["trainDefeated"]=receipt.TrainDefeated};
    }
   }
   shot["lastTick"]=end["tick"].DeepClone();shot["latestObservation"]=end.DeepClone();shot["lastAdvance"]=command;shot["result"]=result;
   if(kind=="HIT"||kind=="BLOCKED"||kind=="EXPIRED"){
    shot["status"]=kind;
    // Terminal flights need replay proofs and their receipt, not copied observation/geometry buffers.
    shot["launchHash"]=BDirectAttackInput.Digest(shot["launch"]);shot["lastAdvanceHash"]=BDirectAttackInput.Digest(command);
    foreach(string field in new[]{"launch","latestObservation","lastAdvance","ray","rayHash"})shot.Remove(field);
   }
   return (JObject)result.DeepClone();
  }
  internal JArray ReadPresentationShots(long tick)=>new JArray(shots.Values.Where(s=>(string)s["status"]=="FLYING").Select(s=>new JObject{["projectileId"]=s["projectileId"].DeepClone(),["actorId"]=s["actorId"].DeepClone(),["pointMm"]=MiraJudgementGeometry.PresentationPoint((JObject)s["ray"],checked((tick-(long)s["launchTick"])*(factory.Step/10))),["originMm"]=s["ray"]["originMm"].DeepClone(),["anchorMm"]=s["ray"]["anchorMm"].DeepClone()}));
  internal bool HasFlying=>shots.Values.Any(s=>(string)s["status"]=="FLYING");
  internal bool HasInterceptionCandidates(long tick)=>shots.Values.Any(s=>(string)s["status"]=="FLYING"||(long)s["lastTick"]==tick);
  internal JObject Capture()=>new JObject{["version"]="ordinary.node-projectiles.v2.20261109",["attempt"]=Attempt(),["shots"]=new JArray(shots.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>x.Value.DeepClone()))};
 }
}
