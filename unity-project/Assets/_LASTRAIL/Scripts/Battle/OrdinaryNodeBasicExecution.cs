using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Synchronous, read-only domain inputs. A product adapter must supply original
 // geometry/effect provenance; absent callbacks or snapshots are never no-effects defaults.
 internal sealed class OrdinaryNodeBasicSources {
  internal readonly Func<string,long,JObject> Observe;
  internal readonly Func<string,long,CombatExactValue> EffectivePeriod;
  internal readonly Func<string,string,long,JObject> CaptureAttack;
  internal readonly Func<string,long,JObject> CaptureDefense;
  internal readonly Func<string,long,string> KyleMode;
  internal readonly Func<string,long,bool> KyleContact;
  internal OrdinaryNodeBasicSources(Func<string,long,JObject> observe,Func<string,long,CombatExactValue> effectivePeriod,
   Func<string,string,long,JObject> captureAttack,Func<string,long,JObject> captureDefense,Func<string,long,string> kyleMode)
   :this(observe,effectivePeriod,captureAttack,captureDefense,kyleMode,null){}
  internal OrdinaryNodeBasicSources(Func<string,long,JObject> observe,Func<string,long,CombatExactValue> effectivePeriod,
   Func<string,string,long,JObject> captureAttack,Func<string,long,JObject> captureDefense,Func<string,long,string> kyleMode,Func<string,long,bool> kyleContact){
   Observe=observe??throw new ArgumentNullException(nameof(observe));EffectivePeriod=effectivePeriod??throw new ArgumentNullException(nameof(effectivePeriod));
   CaptureAttack=captureAttack??throw new ArgumentNullException(nameof(captureAttack));CaptureDefense=captureDefense??throw new ArgumentNullException(nameof(captureDefense));KyleMode=kyleMode??throw new ArgumentNullException(nameof(kyleMode));KyleContact=kyleContact;
  }
 }
 // Node transaction owns this scheduler, projectile aggregate and HP kernel together.
 // No wall clock, UI eligibility, separate damage ledger or checkpoint restoration.
 internal sealed class OrdinaryNodeBasicExecution {
  readonly OrdinaryFiveCombatFactory factory;
  readonly BCombatLocalModel health;
  readonly OrdinaryNodeProjectileOwner projectiles;
  readonly OrdinaryNodeBasicSources sources;
  readonly List<JObject> actors=new List<JObject>();
  readonly List<JObject> flights=new List<JObject>();
  readonly long startTick;
  long tick;
  internal OrdinaryNodeBasicExecution(OrdinaryFiveCombatFactory factory,BCombatLocalModel health,OrdinaryNodeProjectileOwner projectiles,OrdinaryNodeBasicSources sources,long startTick=0){
   this.factory=factory??throw new ArgumentNullException(nameof(factory));this.health=health??throw new ArgumentNullException(nameof(health));
   this.projectiles=projectiles??throw new ArgumentNullException(nameof(projectiles));this.sources=sources??throw new ArgumentNullException(nameof(sources));
   Require(factory.NodeInput!=null&&factory.Attempt!=null&&projectiles.UsesHealth(health));
   Require(startTick>=0);this.startTick=startTick;tick=startTick-1;
   // This pinned source currently has exactly one primary shot. A future emission
   // policy needs its own opportunity/branch handling rather than guessed extra shots.
   Require(factory.Original.Basic.MiraPrimaryShotsPerOpportunity==1);
   foreach(var a in factory.NodeInput.Actions.Actors.OrderBy(a=>a.Slot))actors.Add(new JObject{
    ["actorId"]=a.OperatorId,["phase"]="READY",["nextOpportunity"]=checked(startTick+a.BasicNextOpportunityInTicks),
    ["startedAt"]=null,["commit"]=null,["recoveryEnd"]=null,["targetId"]=null,["targetGeneration"]=null,["mode"]=null,["opportunity"]=0});
  }
  static void Require(bool value){if(!value)throw new ArgumentException("Original node BASIC execution source differs.");}
  internal OrdinaryNodeBasicExecution CloneOwned(BCombatLocalModel newHealth,OrdinaryNodeProjectileOwner newProjectiles)=>CloneOwnedWithSources(newHealth,newProjectiles,sources);
  internal OrdinaryNodeBasicExecution CloneOwnedWithSources(BCombatLocalModel newHealth,OrdinaryNodeProjectileOwner newProjectiles,OrdinaryNodeBasicSources newSources){
   Require(newHealth!=null&&!ReferenceEquals(health,newHealth)&&!ReferenceEquals(projectiles,newProjectiles));
   Require(health.HasSameState(newHealth)&&projectiles.HasSameState(newProjectiles));
   var copy=new OrdinaryNodeBasicExecution(factory,newHealth,newProjectiles,newSources,startTick){tick=tick};copy.actors.Clear();
   foreach(var actor in actors)copy.actors.Add((JObject)actor.DeepClone());foreach(var flight in flights)copy.flights.Add((JObject)flight.DeepClone());return copy;
  }
  JObject Observe(string actor,long at){
   var observation=sources.Observe(actor,at);Require(observation!=null&&(long?)observation["tick"]==at);
   Require((string)observation["nodeId"]==factory.Attempt.NodeId&&(string)observation["snapshotHash"]==factory.Attempt.SnapshotHash&&(long?)observation["attemptGeneration"]==factory.Attempt.Generation);
   Require(observation["targets"] is JArray&&(string)observation["source"]?["id"]==actor);
   return (JObject)observation.DeepClone();
  }
  IEnumerable<JObject> OrderedCandidates(JObject observation){
   foreach(string enemy in factory.EnemyIds){
    var row=((JArray)observation["targets"]).SingleOrDefault(t=>(string)t["id"]==enemy) as JObject;
    if(row!=null&&(bool?)row["eligible"]==true&&health.Hp(enemy)>0)yield return row;
   }
  }
  bool ProjectileEligible(JObject observation,string target,long generation){
   var row=((JArray)observation["targets"]).SingleOrDefault(t=>(string)t["id"]==target);
   if(row==null||(long?)row["generation"]!=generation||(bool?)row["eligible"]!=true||health.Hp(target)<=0)return false;
   try{MiraJudgementGeometry.CreateRay(observation,BDirectAttackInput.Digest(observation),target);return true;}
   catch(InvalidOperationException){return false;} // Actual range/domain/LOS failure; malformed sources still throw.
  }
  JObject Select(string actor,JObject observation,bool choosingMode){
   var candidates=OrderedCandidates(observation).ToArray();
   if(actor=="op_kyle"&&choosingMode){
    JObject selected=null;
    foreach(var candidate in candidates){var mode=sources.KyleMode((string)candidate["id"],(long)observation["tick"]);
     Require(mode=="MELEE"||mode=="RANGED");
     if(mode=="MELEE"&&sources.KyleContact==null)throw new NotSupportedException("Original Kyle melee contact execution is unavailable.");
     if(selected==null&&(mode=="MELEE"||ProjectileEligible(observation,(string)candidate["id"],(long)candidate["generation"])))selected=candidate;
    }
    return selected;
   }
   return candidates.FirstOrDefault(c=>ProjectileEligible(observation,(string)c["id"],(long)c["generation"]));
  }
  internal IReadOnlyList<JObject> Step(long nextTick)=>Step(nextTick,null);
  internal IReadOnlyList<JObject> Step(long nextTick,Func<string,bool> actorBusy){
   Require(nextTick>=startTick);
   if(nextTick==tick&&tick>=0)return Array.Empty<JObject>();
   Require(nextTick>=0&&nextTick==tick+1);
   var contacts=new List<JObject>();
   // Existing flights resolve before new same-tick opportunities in stable launch order.
   foreach(var flight in flights.ToArray()){
    string actor=(string)flight["actorId"],target=(string)flight["targetId"];
    var end=Observe(actor,nextTick);var defense=sources.CaptureDefense(target,nextTick);Require(defense!=null);
    var result=projectiles.Advance((string)flight["projectileId"],(JObject)flight["observation"],end,defense);
    if((string)result["kind"]=="HIT"&&result["receipt"] is JObject)contacts.Add((JObject)result.DeepClone());
    flight["observation"]=end;
    if(new[]{"HIT","BLOCKED","EXPIRED"}.Contains((string)result["kind"]))flights.Remove(flight);
   }
   foreach(var actor in actors){
    string id=(string)actor["actorId"];var profile=factory.Original.Basic.Profiles.Single(p=>p.OperatorId==id);
    if(!health.OperatorAlive(id)){
     if((string)actor["phase"]=="PREPARING"){actor["phase"]="RECOVERY";actor["targetId"]=null;actor["targetGeneration"]=null;}
     continue;
    }
    if((string)actor["phase"]=="RECOVERY"&&nextTick>=(long)actor["recoveryEnd"])actor["phase"]="WAITING";
    if(((string)actor["phase"]=="READY"||(string)actor["phase"]=="WAITING")&&nextTick>=(long)actor["nextOpportunity"]){
     if(actorBusy?.Invoke(id)==true)continue;
     var observation=Observe(id,nextTick);var target=Select(id,observation,true);
     if(target==null)continue; // Re-evaluate next authority step; do not mint missed opportunities.
     var period=sources.EffectivePeriod(id,nextTick);Require(period!=null&&period.Numerator>0);
     var boundaries=profile.Boundaries(nextTick,period,factory.Step/10);
     actor["startedAt"]=nextTick;
     actor["commit"]=boundaries.Commit;actor["recoveryEnd"]=boundaries.RecoveryEnd;actor["nextOpportunity"]=boundaries.NextOpportunity;
     actor["targetId"]=target["id"].DeepClone();actor["targetGeneration"]=target["generation"].DeepClone();actor["mode"]=id=="op_kyle"?sources.KyleMode((string)target["id"],nextTick):"RANGED";
     actor["opportunity"]=(long)actor["opportunity"]+1;actor["phase"]="PREPARING";
    }
    if((string)actor["phase"]!="PREPARING"||nextTick<(long)actor["commit"])continue;
    var commitObservation=Observe(id,nextTick);string targetId=(string)actor["targetId"];long generation=(long)actor["targetGeneration"];
    if((string)actor["mode"]=="MELEE"){
     var row=((JArray)commitObservation["targets"]).SingleOrDefault(t=>(string)t["id"]==targetId);
     if(row!=null&&(long?)row["generation"]==generation&&(bool?)row["eligible"]==true&&health.Hp(targetId)>0&&sources.KyleContact(targetId,nextTick)){
      var attackAtContact=CombatProjectileAttackSnapshot.Capture(sources.CaptureAttack(id,targetId,nextTick));
      int damage=attackAtContact.Evaluate(sources.CaptureDefense(targetId,nextTick)).ScaleAndFloor(10000);
      string contactId="node-melee:"+BDirectAttackInput.Digest(new JArray(factory.ConfigurationVersion,id,(long)actor["opportunity"])).Substring(7);
      var receipt=health.ApplyResolvedDamage(contactId,targetId,damage,nextTick);
      contacts.Add(new JObject{["kind"]="HIT",["eventId"]=contactId,["actorId"]=id,["abilityId"]=profile.AbilityId,["executorId"]=id,["projectileId"]=JValue.CreateNull(),["targetId"]=targetId,["targetGeneration"]=generation,
       ["receipt"]=new JObject{["eventId"]=receipt.EventId,["targetId"]=receipt.TargetId,["tick"]=receipt.Tick,["hpLoss"]=receipt.HpLoss,["hpAfter"]=receipt.HpAfter,["absorbed"]=receipt.Absorbed,["trainDefeated"]=receipt.TrainDefeated}});
     }
     actor["phase"]="RECOVERY";continue;
    }
    if(!ProjectileEligible(commitObservation,targetId,generation)){
     // Kyle and Nyx prepared mode/target are fixed. Failure consumes the original recovery
     // and retains its next opportunity; it never changes to another attack mid-windup.
     var replacement=id=="op_kyle"||id=="op_nyx"?null:Select(id,commitObservation,false);
     if(replacement==null){actor["phase"]="RECOVERY";actor["targetId"]=null;actor["targetGeneration"]=null;continue;}
     targetId=(string)replacement["id"];actor["targetId"]=targetId;actor["targetGeneration"]=replacement["generation"].DeepClone();
    }
    var attack=sources.CaptureAttack(id,targetId,nextTick);Require(attack!=null);
    string eventId="node-basic:"+BDirectAttackInput.Digest(new JArray(factory.ConfigurationVersion,id,(long)actor["opportunity"])).Substring(7);
    string projectileId=eventId+":primary";
    projectiles.Launch(eventId,id,projectileId,targetId,commitObservation,BDirectAttackInput.Digest(commitObservation),attack);
    flights.Add(new JObject{["actorId"]=id,["targetId"]=targetId,["projectileId"]=projectileId,["observation"]=commitObservation});
    actor["phase"]="RECOVERY";
   }
   tick=nextTick;
   return contacts.AsReadOnly();
  }
  internal JObject ReadActor(string actorId)=>(JObject)actors.Single(a=>(string)a["actorId"]==actorId).DeepClone();
  internal JObject Capture()=>new JObject{["version"]="ordinary.node-basic-execution.v1.20261102",["configuration"]=factory.ConfigurationVersion,["startTick"]=startTick,["tick"]=tick,["actors"]=new JArray(actors.Select(a=>a.DeepClone())),["flights"]=new JArray(flights.Select(f=>f.DeepClone()))};
 }
}
