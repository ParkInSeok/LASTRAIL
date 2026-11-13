using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 internal sealed partial class OrdinaryBoardingExecution:IBEnemyBasicTimingContext,IBGunnerProjectileContext {
  BEnemyBasicTimingOwner timing;BGunnerProjectileOwner projectiles;
  Func<string,long,CombatExactValue> effectivePeriod;
  internal void BindEffectivePeriod(Func<string,long,CombatExactValue> source){if(source==null||effectivePeriod!=null)throw new InvalidOperationException("Boarding period owner must be bound once.");effectivePeriod=source;timing=timing.CloneWithEffectivePeriod((id,at)=>effectivePeriod(id,at));}
  readonly Dictionary<string,long> nextBasic=new Dictionary<string,long>(StringComparer.Ordinal);
  Dictionary<string,JObject> enemyTypes;long resolvedAttackTick=-1;
  void InitializeAttacks(){
   enemyTypes=((JArray)factory.BoardingSource.Boarding.OriginalSource.Capture()["definition"]["types"]).OfType<JObject>().ToDictionary(t=>(string)t["definitionId"],StringComparer.Ordinal);
   string initial=factory.Original.Actors.OrderBy(a=>a.Slot).First().OperatorId;int ordinal=0;
   var sources=factory.BoardingSource.Boarding.OriginalSource.Waves.SelectMany(w=>w.Instances).Select(i=>new BEnemyBasicTimingSource(i.InstanceId,initial,checked((long)enemyTypes[i.DefinitionId]["basicPeriodMs"]*1000),0,i.DefinitionId=="enemy_spike_cart_gunner",ordinal++)).ToArray();
   timing=new BEnemyBasicTimingOwner(factory.BoardingSource.SourceHash,roster.StepMicroseconds,sources,this);
   timing.ConfigureAdmission(BEnemyAttackAdmissionPolicy.ForBand(BWaveBoardingPolicy.FromReceived(factory.BoardingSource.Boarding.OriginalSource.VisitDepth,factory.BoardingSource.Boarding.Definition).Band));
   projectiles=new BGunnerProjectileOwner(roster.StepMicroseconds,10000);
  }
  void CloneAttacks(OrdinaryBoardingExecution other){timing=other.effectivePeriod==null?other.timing.Clone():other.timing.CloneWithEffectivePeriod((id,at)=>effectivePeriod(id,at));projectiles=other.projectiles.Clone();enemyTypes=other.enemyTypes;foreach(var p in other.nextBasic)nextBasic.Add(p.Key,p.Value);resolvedAttackTick=other.resolvedAttackTick;}
  public long NextBasicDue(string id)=>nextBasic[id];
  public void ScheduleEnemyBasic(string id,long due){if(!Contains(id)||due<0)throw new ArgumentException("Original boarding schedule required.");nextBasic[id]=due;}
  public bool HasSpatial=>true;
  public string GunnerTarget(string source)=>boarding.SpatialTarget(source);
  public bool TryGeometry(string source,string target,out JObject observation)=>boarding.TryGeometry(source,target,tick,out observation);
  public bool EnemyBasicTargetEligible(string source,string target)=>!retired&&started&&Hp(source)>0&&Hp(target)>0&&boarding.AttackEligible(source)&&GunnerTarget(source)==target&&TryGeometry(source,target,out var g)&&(bool)g["eligible"];
  public JObject CaptureGunnerAttack(string source){var type=enemyTypes[roster.Entities[source].Definition];return new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=type["rawAction"]["value"].DeepClone(),["appliedOffense"]=new JArray(),["penetration"]=new JArray()};}
  void ObserveAttackLifetimes(){
   foreach(string id in roster.Entities.Values.Where(e=>e.Kind=="LOCAL_ENEMY").Select(e=>e.Id)){timing.RetargetWave(id,boarding.SpatialTarget(id),this,Next);string target=timing.Target(id);timing.ObserveWaveLifetime(id,boarding.Generation(id),ProjectileTargetGeneration(target),boarding.AttackEligible(id),this,Next);}
   foreach(var roof in boarding.NewRoof())timing.ScheduleFirstRoofBasic(roof.id,roof.tick,this);
  }
  internal IReadOnlyList<JObject> ResolveAttacks(Func<string,long,JObject> defense,Func<string,long,JObject> interception){
   Active();if(!started||resolvedAttackTick==tick)return Array.Empty<JObject>();var results=new List<JObject>();
   if(boarding.Spatial.HasTauntChange(this))boarding.ObserveSpatial(this);
   ObserveAttackLifetimes();
   void Impact(string id,string source,string target,JObject attack){
    int amount=CombatProjectileAttackSnapshot.Capture(attack).Evaluate(defense(target,tick)).ScaleAndFloor(10000);var receipt=health.ApplyResolvedDamage(id+":hp",target,amount,tick);
    results.Add(new JObject{["kind"]="HIT",["eventId"]=id,["actorId"]=source,["targetId"]=target,["targetGeneration"]=ProjectileTargetGeneration(target),["abilityId"]=enemyTypes[roster.Entities[source].Definition]["rawAction"]["sourceId"].DeepClone(),["executorId"]=source,["projectileId"]=id,["receipt"]=new JObject{["eventId"]=receipt.EventId,["targetId"]=target,["tick"]=tick,["hpLoss"]=receipt.HpLoss,["hpAfter"]=receipt.HpAfter,["absorbed"]=receipt.Absorbed,["trainDefeated"]=receipt.TrainDefeated}});
   }
   foreach(var shot in projectiles.Pending.ToArray()){
    var decision=interception(shot.Id,tick);if(decision==null||(string)decision["shotId"]!=shot.Id||(long?)decision["tick"]!=tick||((string)decision["status"]!="CLEAR"&&(string)decision["status"]!="INTERCEPTED"))throw new InvalidOperationException("Original boarding projectile interception decision required.");
    if((string)decision["status"]=="INTERCEPTED"){shot.Terminal="INTERCEPTED";shot.ReservedBy=null;}
   }
   projectiles.Resolve(this,Next,Impact);
   var due=new List<BasicCombatSimulation.DueAction>();var launches=new List<BasicCombatSimulation.DueAction>();
   timing.Collect(this,Next,due,(id,source,target)=>{Impact(id,source,target,CaptureGunnerAttack(source));timing.ScheduleFirstRoofBasic(source,tick,this);},(source,at)=>projectiles.Launch(source,at,boarding.Generation(source),this,Next),launches);
   foreach(var action in BasicCombatSimulation.OrderDueActions(launches))action.Execute();
   foreach(var action in BasicCombatSimulation.OrderDueActions(due)){if(health.TrainDefeated)break;action.Execute();}
   resolvedAttackTick=tick;return results.AsReadOnly();
  }
  internal bool HasFlying=>projectiles.Flying;
  JObject CaptureAttacks()=>new JObject{["timing"]=timing.Capture(),["projectiles"]=projectiles.Capture(),["nextBasic"]=JObject.FromObject(nextBasic),["resolvedTick"]=resolvedAttackTick};
  void RetireAttacks(){timing.Terminal(this,Next);projectiles.Terminal(this,Next);}
 }
}
