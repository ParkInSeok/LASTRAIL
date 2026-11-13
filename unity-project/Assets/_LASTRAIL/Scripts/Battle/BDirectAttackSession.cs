using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Core.Simulation;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed class BDirectAttackReceipt {
  readonly JObject calculation;
  public string EventId {get;}public string SourceId {get;}public long Tick {get;}public int BeforeShield {get;}
  public BCombatSingleTargetReceipt Hp {get;}
  readonly JObject recovery;
  public JObject Lifesteal=>recovery==null?null:(JObject)recovery.DeepClone();
  public JObject Calculation=>(JObject)calculation.DeepClone();
  internal BDirectAttackReceipt(string id,string source,long tick,int amount,JObject result,BCombatSingleTargetReceipt hp,JObject lifesteal=null){EventId=id;SourceId=source;Tick=tick;BeforeShield=amount;calculation=(JObject)result.DeepClone();Hp=hp;recovery=lifesteal==null?null:(JObject)lifesteal.DeepClone();}
 }
 // Owns the LOCAL clock and HP roots. It accepts explicit committed BASIC impacts, never selects an AI target.
 public sealed partial class BDirectAttackSession {
  readonly BDirectAttackInput input;readonly BCombatLocalModel allies;readonly int capacity;readonly bool modified;
  readonly Dictionary<string,CombatVitals> enemies=new Dictionary<string,CombatVitals>(StringComparer.Ordinal);
  readonly Dictionary<string,long> next=new Dictionary<string,long>(StringComparer.Ordinal);
  readonly Dictionary<string,(JObject command,BDirectAttackReceipt receipt)> originals=new Dictionary<string,(JObject,BDirectAttackReceipt)>(StringComparer.Ordinal);
  readonly JArray events=new JArray();Dictionary<string,long> projectileTargetGenerations;FixedStepDebt clock;IBWaveEnemyPerception waveEnemies;
  internal void BindWaveEnemies(IBWaveEnemyPerception perception)=>waveEnemies=perception;
  internal string WaveCategory(string id)=>input.Entities[id].Kind=="LOCAL_ENEMY"?waveEnemies?.CategoryAtImpact(id):null;
  public long Tick=>clock.CompletedSteps;public bool Paused=>clock.IsPaused;public bool TrainDefeated=>allies.TrainDefeated;
  public int Hp(string target)=>enemies.TryGetValue(target,out var e)?e.Hp:allies.Hp(target);
  internal const string ModifiedSchema="unity.combat-b.direct-modifier-local.v2";
  public BDirectAttackSession(BDirectAttackInput originalInput,int eventCapacity):this(originalInput,eventCapacity,false){}
  internal static BDirectAttackSession CreateModified(BDirectAttackInput input,int capacity)=>new BDirectAttackSession(input,capacity,true);
  BDirectAttackSession(BDirectAttackInput originalInput,int eventCapacity,bool withModifier){
   modified=withModifier;
   input=originalInput??throw new ArgumentNullException(nameof(originalInput));
   if(eventCapacity<1||eventCapacity>10000)throw new ArgumentOutOfRangeException(nameof(eventCapacity));capacity=eventCapacity;
   allies=BCombatLocalModel.CreateWithCarriageTransfer(input.Formation,input.Entities.Values.Where(e=>e.Kind!="LOCAL_ENEMY").Select(e=>new BCombatHpBinding(e.Id,e.MaxHp)),eventCapacity,BCombatAuthority.UnitySolo);
   clock=new FixedStepDebt(input.StepTimeSpanTicks);
   foreach(var e in input.Entities.Values){if(e.Kind=="LOCAL_ENEMY")enemies.Add(e.Id,new CombatVitals(e.MaxHp,0,true));if(e.Attack!=null)next.Add(e.Id,0);}
   int n=0;
   foreach(var s in (JArray)input.Capture()["initialShields"]){
    string target=(string)s["targetId"];var source=new CombatShieldSource((string)s["sourceEntity"],(string)s["sourceAbility"],(int)s["effectIndex"]);
    if(enemies.TryGetValue(target,out var v))v.Shield(source,(int)s["amount"],(long)s["expiryTick"],0);
    else allies.ApplyResolvedShield("initial-shield:"+n,target,source,(int)s["amount"],(long)s["expiryTick"],0);
    n++;
   }
  }
  // Private trusted-owner copy for atomic LOCAL staging. No external cache is accepted here.
  internal BDirectAttackSession CloneOwnedModified()=>new BDirectAttackSession(this);
  BDirectAttackSession(BDirectAttackSession owner){
   if(!owner.modified)throw new InvalidOperationException("b_attack_original_budget_mode");
   input=owner.input;capacity=owner.capacity;modified=true;clock=FixedStepDebt.Restore(owner.clock.Capture());
   allies=BCombatLocalModel.RestoreLocal(owner.allies.CaptureLocal(),input.Formation,input.Entities.Values.Where(e=>e.Kind!="LOCAL_ENEMY").Select(e=>new BCombatHpBinding(e.Id,e.MaxHp)),capacity,BCombatAuthority.UnitySolo);
   foreach(var pair in owner.enemies){var e=input.Entities[pair.Key];var copy=new CombatVitals(e.MaxHp,0,true);copy.RestoreLocal(pair.Value.CaptureLocal(),owner.Tick);enemies.Add(pair.Key,copy);}
   foreach(var pair in owner.next)next.Add(pair.Key,pair.Value);
   foreach(var row in owner.events)events.Add(row.DeepClone());
   foreach(var pair in owner.originals)originals.Add(pair.Key,((JObject)pair.Value.command.DeepClone(),pair.Value.receipt));
   activeState=owner.activeState==null?null:(JObject)owner.activeState.DeepClone();
   waveEnemies=owner.waveEnemies;o22Application=owner.o22Application==null?null:(JObject)owner.o22Application.DeepClone();if(owner.projectileTargetGenerations!=null)projectileTargetGenerations=new Dictionary<string,long>(owner.projectileTargetGenerations,StringComparer.Ordinal);
  }
  void Room(){if(events.Count>=capacity)throw new InvalidOperationException("b_attack_journal_full");}
  internal long NextBasicDue(string source)=>next[source];
  internal void ScheduleEnemyBasic(string source,long due){Room();BuffWire.Need(input.Entities.ContainsKey(source)&&input.Entities[source].Kind=="LOCAL_ENEMY"&&next.ContainsKey(source)&&due>=Tick&&due<=9007199254740991,"b_enemy_timing_schedule");next[source]=due;events.Add(new JObject{["kind"]="ENEMY_BASIC_SCHEDULE",["sourceId"]=source,["tick"]=Tick,["due"]=due});}
  internal void BindProjectileTargetLifetimes(){BuffWire.Need(projectileTargetGenerations==null,"b_gunner_lifetime_already_bound");Room();projectileTargetGenerations=input.Entities.Values.Where(e=>e.Kind=="OPERATOR").ToDictionary(e=>e.Id,e=>1L,StringComparer.Ordinal);events.Add(new JObject{["kind"]="GUNNER_TARGET_LIFETIME_BIND",["inputHash"]=input.OriginalHash});}
  internal long ProjectileTargetGeneration(string id){BuffWire.Need(projectileTargetGenerations!=null&&projectileTargetGenerations.ContainsKey(id),"b_gunner_target_lifetime_unbound");return projectileTargetGenerations[id];}
  internal void BeginBasicOccupation(string actor,long duration){BuffWire.Need(duration>0&&input.Entities[actor].Kind=="OPERATOR","b_intercept_occupation_boundary");AdjustBasicOccupation(actor,checked(Math.Max(0,Tick-next[actor])+duration));}
  internal void AdjustBasicOccupation(string actor,long delta){Room();long due=checked(next[actor]+delta);BuffWire.Need(input.Entities[actor].Kind=="OPERATOR"&&due>=Tick&&due<=9007199254740991,"b_intercept_next_boundary");next[actor]=due;events.Add(new JObject{["kind"]="INTERCEPT_BASIC_OCCUPATION",["sourceId"]=actor,["tick"]=Tick,["deltaTicks"]=delta,["due"]=due});}
  internal bool TryGeometry(string source,string target,out JObject frame){if(waveEnemies is IBWaveSpatialPerception spatial&&spatial.HasSpatial)return spatial.TryGeometry(source,target,Tick,out frame);return input.TryGetGeometry(source,target,out frame);}
  internal string SpatialTarget(string source)=>waveEnemies is IBWaveSpatialPerception spatial&&spatial.HasSpatial?spatial.SpatialTarget(source):null;
  internal bool HasSpatial=>waveEnemies is IBWaveSpatialPerception spatial&&spatial.HasSpatial;
  internal bool SpatialProtection(string actor,string target)=>waveEnemies is IBWaveSpatialPerception spatial&&spatial.HasSpatial?spatial.Protection(actor,target):BCombatRoleRanges.For(input.Entities[actor].Definition).InterceptionIncludesSlot(input.Entities[actor].Slot,input.Entities[target].Slot,input.Formation.Slots.Count);
  bool BasicGeometryEligible(string source,string target){
   if(input.IsWaveRuntime){
    if(waveEnemies==null)return false;
    if(input.Entities[source].Kind=="LOCAL_ENEMY"&&(!waveEnemies.AttackEligible(source)||(!HasSpatial&&waveEnemies.Slot(source)!=input.Entities[source].Slot)))return false;
    if(input.Entities[source].Definition=="enemy_raider_scout"&&(HasSpatial?((IBWaveSpatialPerception)waveEnemies).TryGeometry(source,target,Tick,out var slotFrame)&&(int)slotFrame["targetSlot"]!=waveEnemies.Slot(source):input.Entities[target].Slot!=waveEnemies.Slot(source)))return false;
    if(input.Entities[target].Kind=="LOCAL_ENEMY"&&(!waveEnemies.Targetable(target)||(!HasSpatial&&waveEnemies.Slot(target)!=input.Entities[target].Slot)))return false;
   }
   return TryGeometry(source,target,out var g)&&(bool)g["eligible"]&&Tick>=(long)g["observedTick"]&&Tick<=(long)g["validUntilTick"]&&(long)g["distanceMm"]<=(long)g["maximumDistanceMm"];
  }
  bool BasicSlotEligible(string source,string target){int sourceSlot=input.Entities[source].Slot,targetSlot=input.Entities[target].Slot;if(HasSpatial){if(!TryGeometry(source,target,out var frame))return false;sourceSlot=(int)frame["sourceSlot"];targetSlot=(int)frame["targetSlot"];}return BCombatRoleRanges.For(input.Entities[source].Definition).BasicAttackIncludesSlot(sourceSlot,targetSlot,input.Formation.Slots.Count);}
  internal bool LoopTargetEligible(string source,string target)=>Hp(source)>0&&Hp(target)>0&&BasicGeometryEligible(source,target)&&
   (input.Entities[source].Kind!="OPERATOR"||BasicSlotEligible(source,target));
  internal bool EnemyBasicTargetEligible(string source,string target)=>!Paused&&!TrainDefeated&&input.Entities[source].Kind=="LOCAL_ENEMY"&&Hp(source)>0&&Hp(target)>0&&BasicGeometryEligible(source,target);
  void ClockRange(long elapsed,int maximum){if(elapsed<0||elapsed>9007199254740991||maximum<0||maximum>10000||Tick>9007199254740991-maximum)throw new ArgumentException("b_attack_clock_range");}
  // Internal owned driver: the same clock consumes each step before the composed systems run.
  internal void AdvanceDriven(long elapsed,int maximum,Func<bool> running,Action step){
   Room();ClockRange(elapsed,maximum);clock.Credit(elapsed);
   events.Add(new JObject{["kind"]="AUTO_ADVANCE",["elapsed"]=elapsed,["maximumSteps"]=maximum});
   for(int n=0;n<maximum&&running()&&clock.TryConsumeStep();n++)step();
  }
  public void Advance(long elapsedTimeSpanTicks,int maximumSteps){
   Room();ClockRange(elapsedTimeSpanTicks,maximumSteps);
   clock.Credit(elapsedTimeSpanTicks);for(int n=0;n<maximumSteps&&clock.TryConsumeStep();n++){}
   events.Add(new JObject{["kind"]="ADVANCE",["elapsed"]=elapsedTimeSpanTicks,["maximumSteps"]=maximumSteps});
  }
  public void SetPaused(bool value){Room();clock.SetPaused(value);events.Add(new JObject{["kind"]="PAUSE",["value"]=value});}
  public void SetSpeed(int halfUnits){Room();clock.SetSpeed(halfUnits);events.Add(new JObject{["kind"]="SPEED",["value"]=halfUnits});}
  public BDirectAttackReceipt ApplyBasicImpact(string eventId,string sourceId,string targetId,long originalTick,string ability="BASIC")=>Impact(eventId,sourceId,targetId,originalTick,ability,null);
  internal BDirectAttackReceipt ApplyBasicImpactWithBudget(string eventId,string sourceId,string targetId,long tick,string ability,IBDirectAttackBudget budget)=>Impact(eventId,sourceId,targetId,tick,ability,budget);
  BDirectAttackReceipt Impact(string eventId,string sourceId,string targetId,long originalTick,string ability,IBDirectAttackBudget budget){
   if(modified!=(budget!=null))throw new InvalidOperationException("b_attack_original_budget_mode");
   BCombatFormation.Identity(eventId);BCombatFormation.Identity(sourceId);BCombatFormation.Identity(targetId);
   if(eventId.StartsWith("initial-shield:",StringComparison.Ordinal))throw new ArgumentException("b_attack_reserved_key");
   var command=new JObject{["kind"]="BASIC",["eventId"]=eventId,["sourceId"]=sourceId,["targetId"]=targetId,["tick"]=originalTick,["ability"]=ability};
   if(originals.TryGetValue(eventId,out var old)){if(!JToken.DeepEquals(old.command,command))throw new InvalidOperationException("b_attack_original_changed");return old.receipt;}
   Room();
   if(ability!="BASIC")throw new InvalidOperationException("b_attack_ability_unsupported");
   if(originalTick!=Tick||Paused||TrainDefeated)throw new InvalidOperationException("b_attack_clock_or_terminated");
   if(!input.Entities.TryGetValue(sourceId,out var source)||!input.Entities.TryGetValue(targetId,out var target)||sourceId==targetId||source.Attack==null)
    throw new ArgumentException("b_attack_actor_unsupported");
   if(Hp(sourceId)==0||Hp(targetId)==0||!((JArray)source.Original["allowedTargets"]).Any(t=>(string)t==target.Kind))
    throw new InvalidOperationException("b_attack_target_ineligible");
   if(!BasicGeometryEligible(sourceId,targetId))throw new InvalidOperationException("b_attack_geometry_ineligible");
   if(input.IsWaveRuntime&&source.Definition=="enemy_spike_cart_gunner")throw new InvalidOperationException("b_wave_projectile_executor_missing");
   if(source.Kind=="OPERATOR"&&!BasicSlotEligible(sourceId,targetId))
    throw new InvalidOperationException("b_attack_slot_out_of_range");
   if(Tick<next[sourceId])throw new InvalidOperationException("b_attack_period_not_ready");
   var period=CombatActionTiming.BasicAttackPeriodTicks(source.Interval,input.StepTimeSpanTicks/10,Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>());
   if(period>9007199254740991-Tick)throw new InvalidOperationException("b_attack_period_range");
   var attack=source.Id=="op_mira"&&activeState!=null?ActiveAttack:CombatStatResolver.Attack(source.Id,source.Attack,(JArray)source.Original["attackSources"]);
   var numeric=input.NumericInput(source,target,attack);if(activeState!=null&&targetId==(string)activeState["target"]["targetId"])numeric["defense"]=activeState["defense"].DeepClone();
   var calculation=budget==null?(activeState==null?input.Budget(source,target,attack):CombatNumericDefenseBudget.Resolve(numeric)):budget.Resolve(source,target,numeric);int magnitude=calculation.ToVitalsMagnitude();
   var receipt=CommitResolvedDamage(eventId,sourceId,targetId,command,calculation);next[sourceId]=Tick+period;return receipt;
  }
  BDirectAttackReceipt CommitResolvedDamage(string eventId,string sourceId,string targetId,JObject command,CombatNumericDefenseResult calculation){
   int magnitude=calculation.ToVitalsMagnitude();int before=Hp(targetId);BCombatSingleTargetReceipt hp;
   if(enemies.TryGetValue(targetId,out var v)){var hit=v.Damage(magnitude,Tick,activeState!=null&&targetId==(string)activeState["target"]["targetId"]&&(bool)activeState["immune"]);hp=new BCombatSingleTargetReceipt(eventId,targetId,Tick,hit.HpLoss,v.Hp,hit.Absorbed);}
   else hp=allies.ApplyResolvedDamage(eventId,targetId,magnitude,Tick);
   if(before>0&&hp.HpAfter==0&&projectileTargetGenerations!=null&&projectileTargetGenerations.ContainsKey(targetId))projectileTargetGenerations[targetId]=checked(projectileTargetGenerations[targetId]+1);
   var recovery=ResolveNativeLifesteal(eventId,sourceId,targetId,hp);
   var receipt=new BDirectAttackReceipt(eventId,sourceId,Tick,magnitude,calculation.Snapshot(),hp,recovery);events.Add(command);originals.Add(eventId,(command,receipt));return receipt;
  }
  internal BDirectAttackReceipt ApplyLaunchedGunnerImpact(string id,string sourceId,string targetId,JObject attack,IBDirectAttackBudget budget){
   BuffWire.Need(modified&&budget!=null&&!Paused&&!TrainDefeated&&input.IsWaveRuntime&&input.Entities[sourceId].Definition=="enemy_spike_cart_gunner"&&input.Entities[targetId].Kind=="OPERATOR"&&Hp(targetId)>0,"b_gunner_impact_contract");
   Room();BuffWire.Need(!originals.ContainsKey(id),"b_gunner_duplicate_impact");var numeric=input.NumericInput(input.Entities[sourceId],input.Entities[targetId],CombatExactValue.From(attack));
   var calculation=budget.Resolve(input.Entities[sourceId],input.Entities[targetId],numeric);return CommitResolvedDamage(id,sourceId,targetId,new JObject{["kind"]="GUNNER_PROJECTILE_IMPACT",["eventId"]=id,["sourceId"]=sourceId,["targetId"]=targetId,["tick"]=Tick,["attack"]=attack.DeepClone()},calculation);
  }
  public string CaptureLocal(){
   var s=clock.Capture();
   var capture=new JObject{["schema"]=modified?ModifiedSchema:input.SchemaVersion,["inputHash"]=input.OriginalHash,["capacity"]=capacity,["events"]=events.DeepClone(),
    ["allies"]=JObject.Parse(allies.CaptureLocal()),["enemies"]=JObject.FromObject(enemies.ToDictionary(e=>e.Key,e=>e.Value.CaptureLocal())),
    ["next"]=JObject.FromObject(next),["clock"]=new JObject{["stepTicks"]=s.StepTicks,["debtHalfTicks"]=s.DebtHalfTicks,["completedSteps"]=s.CompletedSteps,["speedHalfUnits"]=s.SpeedHalfUnits,["paused"]=s.IsPaused},["activeEntityState"]=activeState==null?JValue.CreateNull():(JToken)activeState.DeepClone()}.WithoutInactiveEntityState(activeState!=null);if(projectileTargetGenerations!=null)capture["projectileTargetGenerations"]=JObject.FromObject(projectileTargetGenerations);if(o22Application!=null)capture["o22Application"]=o22Application.DeepClone();return capture.ToString(Newtonsoft.Json.Formatting.None);
  }
  public static BDirectAttackSession RestoreLocal(string json,BDirectAttackInput originalInput,int capacity){
   if(json==null||json.Length>67108864)throw new ArgumentException("b_attack_cache_limit");
   var x=JObject.Parse(json);BDirectAttackInput.Fields(x,"schema","inputHash","capacity","events","allies","enemies","next","clock");
   BDirectAttackInput.Need((string)x["schema"]==originalInput.SchemaVersion&&(string)x["inputHash"]==originalInput.OriginalHash&&BDirectAttackInput.Nat(x,"capacity",1,10000)==capacity&&x["events"] is JArray rows&&rows.Count<=capacity);
   var restored=new BDirectAttackSession(originalInput,capacity);
   foreach(var token in (JArray)x["events"]){
    var e=token as JObject;switch((string)e?["kind"]){
     case "BASIC":BDirectAttackInput.Fields(e,"kind","eventId","sourceId","targetId","tick","ability");restored.ApplyBasicImpact(BDirectAttackInput.Text(e,"eventId"),BDirectAttackInput.Text(e,"sourceId"),BDirectAttackInput.Text(e,"targetId"),BDirectAttackInput.Nat(e,"tick"),BDirectAttackInput.Text(e,"ability"));break;
     case "ADVANCE":BDirectAttackInput.Fields(e,"kind","elapsed","maximumSteps");restored.Advance(BDirectAttackInput.Nat(e,"elapsed"),(int)BDirectAttackInput.Nat(e,"maximumSteps",0,10000));break;
     case "PAUSE":BDirectAttackInput.Fields(e,"kind","value");BDirectAttackInput.Need(e["value"].Type==JTokenType.Boolean);restored.SetPaused((bool)e["value"]);break;
     case "SPEED":BDirectAttackInput.Fields(e,"kind","value");restored.SetSpeed((int)BDirectAttackInput.Nat(e,"value",0,4));break;
     default:throw new ArgumentException("b_attack_saved_action_unsupported");
    }
   }
   if(!JToken.DeepEquals(JObject.Parse(restored.CaptureLocal()),x))throw new ArgumentException("b_attack_original_replay_mismatch");return restored;
  }
 }
}





