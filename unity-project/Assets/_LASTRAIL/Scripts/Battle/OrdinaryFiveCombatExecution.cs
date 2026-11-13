using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Core.Simulation;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Trusted scene-source contract, not a UI readiness flag. Implementations must retain the
 // original measured geometry and generations across checkpoint replay before being installed.
 internal interface IOrdinaryFiveNativeGeometry {
  IReadOnlyList<(string Id,long Generation)> KyleTauntCandidates(string actorId,long tick);
  bool KyleTauntStillEligible(string actorId,string targetId,long generation,long tick);
 }
 public sealed partial class BasicCombatSimulation {
  OrdinaryFiveCombatFactory ordinaryFactory;
  BCombatLocalModel ordinaryHealth;
  OrdinaryNodeProjectileOwner ordinaryProjectiles;
  OrdinaryNodeBasicExecution ordinaryBasics;
  OrdinaryNodeGunnerExecution ordinaryGunners;
  OrdinaryReinaBarrageExecution ordinaryBarrage;
  NyxZoneExecution ordinaryNyx;
  LastRail.Run.OrdinaryObstacleSequence ordinaryObstacle;
  OrdinaryNodeObstacleExecution ordinaryObstacleExecution;
  OrdinaryLiveCombatSources ordinaryLiveSources;
  OrdinaryLiveNumericAuthority ordinaryLiveNumbers;
  bool ordinaryExecutionStarted;
  internal bool HasOrdinaryExecutionStarted=>ordinaryExecutionStarted;
  bool ordinaryNativeRetired;
  internal void RetireOrdinaryNativeOwners(){ordinaryNativeRetired=true;ordinaryBoarding?.Retire();ordinaryNyxDispatch?.Cancel();ordinaryNyxDispatch=null;ordinaryNyx?.Dispose();}
  readonly Dictionary<string,long> ordinaryReady=new Dictionary<string,long>(StringComparer.Ordinal);
  readonly Dictionary<string,int> ordinaryCharge=new Dictionary<string,int>(StringComparer.Ordinal);
  readonly JArray ordinaryActions=new JArray(),ordinaryEffects=new JArray(),ordinaryOperations=new JArray();
  long ordinaryFront,ordinarySpeed=20000,ordinaryTarget=20000,ordinaryDistanceRemainder;
  bool ordinaryUltimateAuto;
  JObject ordinarySentry;
  readonly JArray ordinaryTaunts=new JArray();
  // Geometry source supplies no muzzle/hit volume yet. Absence is never a hit or a free charge.
  bool OrdinaryReady=>ordinaryFactory?.Encounter!=null&&ordinaryFactory.Geometry!=null;
  long OrdinaryMs=>checked(Tick*(ordinaryFactory.Step/10000));
  internal BasicCombatSimulation(OrdinaryFiveCombatFactory factory,bool initializeLiveSources=true) {
   ordinaryFactory=factory;ConfigurationVersion=factory.ConfigurationVersion;LocalOwnerAccount=factory.Original.AccountId;
   if(factory.NodeInput?.Source.Obstacle!=null)ordinaryObstacle=new LastRail.Run.OrdinaryObstacleSequence(factory.NodeInput.Source.Obstacle,factory.Attempt);
   ordinaryFront=factory.Original.Rail.InitialFrontArcMm??throw new ArgumentException("Original initial rail front required.");
   clock=new FixedStepDebt(factory.Step);random=factory.NodeInput==null?new RunRandom(1):factory.NodeInput.Source.ExecutionOrigin.CreateInitialRandom();configuredRandomState=random.ExportState();approvedVitals=true;
   if(factory.NodeInput!=null){ordinaryHealth=new BCombatLocalModel(factory.NodeInput,10000,factory.BoardingSource);ordinaryProjectiles=new OrdinaryNodeProjectileOwner(factory,ordinaryHealth,()=>!ordinaryNativeRetired);if(factory.BasicSources!=null)ordinaryBasics=new OrdinaryNodeBasicExecution(factory,ordinaryHealth,ordinaryProjectiles,factory.BasicSources);if(factory.GunnerSources!=null)ordinaryGunners=new OrdinaryNodeGunnerExecution(factory,ordinaryHealth,factory.GunnerSources);}
   if(factory.ReinaBarrageSources!=null)ordinaryBarrage=new OrdinaryReinaBarrageExecution(factory,ordinaryHealth,factory.ReinaBarrageSources,(int)factory.Original.ReinaBarrage["cost"]);
   waves=Array.Empty<BasicCombatActor[]>();allies=factory.Original.Actors.Select(a=>ordinaryHealth==null?new Actor(a.OperatorId,checked((int)factory.ActorStats(a.OperatorId).MaxHp)):new Actor(a.OperatorId,()=>ordinaryHealth.BorrowVitals(a.OperatorId))).ToList();
   foreach(var a in allies){var s=factory.ActorStats(a.Identity);var action=factory.NodeInput?.Actions.Actors.Single(x=>x.OperatorId==a.Identity);if(ordinaryHealth==null&&s.CurrentHp<s.MaxHp)a.Vitals.ApplyTransferredHpLoss(checked((int)(s.MaxHp-s.CurrentHp)),0);ordinaryReady.Add(a.Identity,action?.ActiveCooldownRemainingTicks??0);ordinaryCharge.Add(a.Identity,action?.UltimateCharge??0);}
   allies.Add(ordinaryHealth==null?new Actor(factory.TrainId,factory.TrainMaxHp):new Actor(factory.TrainId,()=>ordinaryHealth.BorrowVitals(factory.TrainId)));
   if(factory.NodeInput!=null){
    var node=factory.NodeInput;ordinaryUltimateAuto=node.Actions.UltimateAuto;
    foreach(string id in factory.EnemyIds)enemies.Add(new Actor(id,()=>ordinaryHealth.BorrowVitals(id)));
   }else if(factory.Encounter!=null){
    if(factory.Encounter.IssuedGunnerIds?.Count!=2||factory.Encounter.GunnerHp.Denominator!=1)throw new ArgumentException("Original issued gunner identities and integral HP required.");
    foreach(string id in factory.Encounter.IssuedGunnerIds)enemies.Add(new Actor(id,checked((int)factory.Encounter.GunnerHp.Floor())));
   }
   if(initializeLiveSources&&factory.LiveDefinitionJson!=null)InitializeOrdinaryLiveSources();
  }
  void RecordOrdinary(JObject operation,bool meaningful=true){if(ordinaryOperations.Count>=10000)throw new InvalidOperationException("Ordinary execution operation limit reached.");ordinaryOperations.Add(operation);ObserveOrdinaryInput(operation,meaningful);}
  public bool IsOrdinaryCombat=>ordinaryFactory!=null;
  // shortcut: BASIC sources alone cannot open combat; bind approved geometry and the remaining native owners first.
  internal bool IsExecutionReady=>ordinaryFactory==null||ordinaryLiveSources!=null&&ordinaryObstacleExecution!=null&&(ordinaryFactory.Original.KitSourceVersion!=OrdinaryFiveStartInput.KitV3Version||ordinaryNyxDispatch!=null);
  void AdvanceOrdinary(long elapsed,int budget) {
   if(ordinaryNativeRetired)throw new InvalidOperationException("Ordinary native owner retired.");
   // shortcut: intake is not execution; open only with original obstacle and absolute-tick wave adapters.
   if(ordinaryObstacle!=null&&!IsExecutionReady)return;
   if((!OrdinaryReady&&ordinaryBasics==null&&ordinaryGunners==null&&ordinaryBarrage==null&&ordinaryObstacleExecution==null)||Outcome!=BasicCombatOutcome.Running||clock.IsPaused)return;
   ordinaryExecutionStarted=true;
   var advance=new JObject{["kind"]="ADVANCE",["elapsed"]=elapsed,["budget"]=budget};RecordOrdinary(advance);clock.Credit(elapsed);AutomaticOrdinarySupport();
   StepOrdinaryNative();
   if(Outcome!=BasicCombatOutcome.Running)return;
   if(BarrageCommitBoundary){advance["budget"]=0;return;}
   for(int i=0;i<budget&&clock.TryConsumeStep();i++) {
    long ms=ordinaryFactory.Step/10000;
    var motion=ordinaryObstacleExecution!=null&&ordinaryObstacle.Phase=="OBSTACLE"
     ?ordinaryObstacleExecution.Advance(OrdinaryMs-ms,ms,Tick,ordinaryFront,ordinarySpeed,ordinaryTarget,ordinaryDistanceRemainder)
     :OrdinaryRailKinematics.Advance(ordinaryFront,ordinarySpeed,ordinaryTarget,ordinaryDistanceRemainder,ms);
    long front=motion.Front;
    // Include the authored following enemy train and the original finite end margin.
    if(front+16000+20000>ordinaryFactory.Original.Rail.TrackLengthMm)throw new InvalidOperationException("Ordinary finite rail capacity exhausted; encounter remains unresolved.");
    ordinaryFront=front;ordinaryDistanceRemainder=motion.Remainder;ordinarySpeed=motion.Speed;
    if(ordinaryObstacleExecution!=null&&ordinaryObstacle.Phase=="OBSTACLE"){
     var resolution=ordinaryObstacleExecution.Resolution(OrdinaryMs,ordinaryFront);
     if(resolution!=null){ResolveOrdinaryObstacle(resolution);if(ordinaryObstacle.Phase=="COMBAT")StartOrdinaryLiveWave();}
    }
    foreach(var actor in allies)actor.Vitals.ShieldRemaining(Tick);
    foreach(var taunt in ordinaryTaunts.Where(t=>(long)t["expiry"]<=Tick||GetHp((string)t["source"])<=0||GetHp((string)t["target"])<=0).ToArray())taunt.Remove();
    if(ordinarySentry!=null&&(long)ordinarySentry["expiry"]<=Tick)ordinarySentry=null;
    foreach(var action in ordinaryActions.OfType<JObject>().Where(a=>(string)a["status"]=="CASTING"&&(bool?)a["reinaBarrage"]!=true&&(bool?)a["nyxZone"]!=true&&(long)a["due"]<=Tick).ToArray())ResolveOrdinarySupport(action);
    StepOrdinaryNative();
    AutomaticOrdinarySupport();
    if(Outcome!=BasicCombatOutcome.Running)break;
    // Publish the complete commit tick before presentation can borrow the clock; retain credited debt.
    if(BarrageCommitBoundary){advance["budget"]=i+1;break;}
   }
  }
  void StepOrdinaryNative(){
   ordinaryFactory.SceneMotion?.Invoke(Tick,ordinaryFront);
   StepOrdinaryNyx();
   if(ordinaryObstacle!=null&&ordinaryObstacle.Phase!="COMBAT"){StepOrdinaryBarrage();return;}
   ordinaryBoarding?.BindTauntTargets(OrdinaryTauntTarget);
   if(ordinaryGunners!=null)ordinaryGunners.TauntTarget=OrdinaryTauntTarget;
   ordinaryBoarding?.BeginStep(Tick);
   if(ordinaryLiveGunnerMotion!=null)ordinaryLiveGunnerMotion.Step(Tick,ordinaryGunners.ReadActor,ordinaryLiveSources.ObserveGunner,id=>ordinaryHealth.Hp(id)>0,ordinaryLiveSources.CaptureBlockers);
   ordinaryGunners?.BeginStep(Tick);
   StepOrdinaryBarrage();
   if(ordinaryBasics!=null)foreach(var contact in ordinaryBasics.Step(Tick,id=>OrdinarySkillBusy(id)||(ordinaryBoarding?.PartyMoving(id)??false)))RecordOrdinaryNativeDamage(contact,"BASIC");
   if(ordinaryGunners!=null)foreach(var contact in ordinaryGunners.ResolveStep(Tick))RecordOrdinaryNativeDamage(contact,"ENEMY_BASIC");
   if(ordinaryBoarding!=null)foreach(var contact in ordinaryBoarding.ResolveAttacks(ordinaryLiveNumbers.Defense,ReadOrdinaryInterception))RecordOrdinaryNativeDamage(contact,"ENEMY_BASIC");
   ordinaryBoarding?.EndStep(Tick,ordinaryProjectiles.HasFlying);
   ResolveOrdinaryOutcome();
  }
  void ResolveOrdinaryOutcome(){
   if(ordinaryBoarding!=null)CompletedWaves=ordinaryBoarding.CompletedWaves;
   if(ordinaryHealth.TrainDefeated){Outcome=BasicCombatOutcome.Failure;ordinaryBoarding?.Retire();}
   else if(ordinaryBoarding?.Complete==true&&ordinaryFactory.NodeInput.Source.Enemies.Gunners.All(g=>ordinaryHealth.Hp(g.InstanceId)==0)&&!ordinaryBoarding.HasFlying&&!ordinaryProjectiles.HasFlying&&ordinaryGunners?.HasFlying!=true&&ordinaryBarrage?.HasPending!=true){
    Outcome=BasicCombatOutcome.Victory;ordinaryBoarding.Retire();
   }
  }
  void RecordOrdinaryNativeDamage(JObject contact,string sourceKind){
    var receipt=(JObject)contact["receipt"];
    var row=new JObject{["runId"]=ordinaryFactory.Original.RunId,["profileSha256"]=Digest(ConfigurationVersion),["configurationSha256"]=Digest(ConfigurationVersion),
     ["journalSequence"]=ordinaryEffects.Count+1,["globalSequence"]=ordinaryEffects.Count+1,["commandId"]=contact["eventId"].DeepClone(),["abilityId"]=contact["abilityId"].DeepClone(),
     ["effectIndex"]=0,["targetId"]=contact["targetId"].DeepClone(),["pulseIndex"]=0,["kind"]="DAMAGE",["tick"]=receipt["tick"].DeepClone(),["sourceId"]=contact["actorId"].DeepClone(),["sourceKind"]=sourceKind,
     ["actualResult"]=new JObject{["hpLoss"]=receipt["hpLoss"].DeepClone(),["absorbed"]=receipt["absorbed"].DeepClone()},["healthReceipt"]=receipt.DeepClone(),
     ["executorId"]=contact["executorId"].DeepClone(),["projectileId"]=contact["projectileId"].DeepClone(),["nodeId"]=ordinaryFactory.Attempt.NodeId,["attemptGeneration"]=ordinaryFactory.Attempt.Generation};
    row["id"]=EffectIdentity(row);ValidateActualResult("DAMAGE",(JObject)row["actualResult"]);
    string actorId=(string)contact["actorId"],actionId=(string)contact["eventId"];
    if(sourceKind=="BASIC"&&ordinaryCharge.TryGetValue(actorId,out int charge)&&ordinaryHealth.Hp(actorId)>0&&(int)receipt["hpLoss"]>0&&
     !ordinaryEffects.Any(e=>(string)e["sourceKind"]=="BASIC"&&(string)e["sourceId"]==actorId&&(string)e["commandId"]==actionId&&(int?)e["actualResult"]?["hpLoss"]>0))
     ordinaryCharge[actorId]=ChargeAfterBasicHpLoss(charge,(int)receipt["hpLoss"]);
    ordinaryEffects.Add(row);
  }
  bool IndependentOrdinaryHealing=>ordinaryHealth!=null&&ordinaryExecutionStarted&&(ordinaryBasics!=null||ordinaryGunners!=null||ordinaryBarrage!=null);
  bool OrdinarySkillBusy(string actor)=>ordinaryActions.Any(a=>(string)a["actor"]==actor&&((string)a["status"]=="CASTING"||((long?)a["recoverAt"]??0)>Tick));
  bool OrdinaryBasicBusy(string actor){var state=ordinaryBasics?.ReadActor(actor);return state!=null&&((string)state["phase"]=="PREPARING"||((string)state["phase"]=="RECOVERY"&&(long?)state["recoveryEnd"]>Tick));}
  void AutomaticOrdinarySupport(){
   if(ordinaryNyxDispatch!=null){if(ordinaryUltimateAuto)RequestOrdinarySkill("auto:"+Tick+":op_nyx.ultimate","op_nyx","op_nyx.ultimate",false);RequestOrdinarySkill("auto:"+Tick+":op_nyx.active","op_nyx","op_nyx.active",false);}
   if(!OrdinaryReady&&!IndependentOrdinaryHealing&&!IndependentOrdinaryGuard)return;
   foreach(var actor in ordinaryFactory.Original.Actors.Where(a=>a.OperatorId=="op_kyle"||a.OperatorId=="op_eve"||a.OperatorId=="op_sera"))
    RequestOrdinarySkill("auto:"+Tick+":"+actor.OperatorId,actor.OperatorId,actor.OperatorId+".active",false);
   if(ordinaryBarrage!=null&&ordinaryUltimateAuto)RequestOrdinarySkill("auto:"+Tick+":op_reina.ultimate","op_reina","op_reina.ultimate",false);
  }
  CombatHealingTarget OrdinaryHealTarget()=>CombatHealingTargetSelection.Select(ordinaryFactory.Original.Actors.Select(a=>new CombatHealingTarget(a.OperatorId,a.Slot,allies.Single(h=>h.Identity==a.OperatorId).Vitals)),false);
  string RequestOrdinarySkill(string commandId,string actorId,string abilityId,bool record=true) {
   if(string.IsNullOrWhiteSpace(commandId))throw new ArgumentException("Command identity required.");
   var prior=ordinaryActions.SingleOrDefault(a=>(string)a["id"]==commandId);
   if(prior!=null){if((string)prior["actor"]!=actorId||(string)prior["ability"]!=abilityId)throw new ArgumentException("Command identity conflict.");return (string)prior["status"];}
   var source=ordinaryFactory.Original.Actors.SingleOrDefault(a=>a.OperatorId==actorId);
   if(source==null||!(abilityId==actorId+".active"||abilityId==actorId+".ultimate"))return "REJECTED_ABILITY";
   var ability=source.Ability(abilityId);
   if(actorId=="op_nyx")return RequestOrdinaryNyx(commandId,abilityId,record);
   if(actorId=="op_reina"&&abilityId=="op_reina.ultimate"&&(string)ability["effect"]=="REINA_BARRAGE")return RequestOrdinaryBarrage(commandId,record);
   if(!OrdinaryReady&&!((string)ability["effect"]=="HEAL_ONE"&&IndependentOrdinaryHealing)&&!((string)ability["effect"]=="SHIELD_AND_TAUNT"&&IndependentOrdinaryGuard))return "REJECTED_PREPARATION";
   if(clock.IsPaused||Outcome!=BasicCombatOutcome.Running)return "REJECTED_STOPPED";
   var caster=allies.Single(a=>a.Identity==actorId);if(caster.Hp==0)return "REJECTED_DOWN";
   if(ordinaryReady[actorId]>Tick||OrdinarySkillBusy(actorId)||OrdinaryBasicBusy(actorId)||(ordinaryBoarding?.PartyMoving(actorId)??false))return "REJECTED_BUSY";
   bool ultimate=abilityId.EndsWith(".ultimate",StringComparison.Ordinal);
   if(ultimate&&ordinaryCharge[actorId]<(int)ability["cost"])return "REJECTED_CHARGE";
   string effect=(string)ability["effect"];
   // Full original effects remain blocked where their required targeting/device owner is absent.
   if(effect!="HEAL_ONE"&&effect!="SHIELD_AND_TAUNT"&&effect!="SENTRY_CREATE")return "REJECTED_NATIVE_GEOMETRY_UNAVAILABLE";
   var target=effect=="HEAL_ONE"?OrdinaryHealTarget():new CombatHealingTarget(actorId,source.Slot,caster.Vitals);if(target==null)return "REJECTED_TARGET";
   var tauntTargets=effect=="SHIELD_AND_TAUNT"?KyleTargets(actorId,ability):null;
   if(tauntTargets!=null&&(tauntTargets.Count==0||tauntTargets.Count>(int)ability["maximumFrontTargets"]||tauntTargets.Select(t=>t.Id).Distinct().Count()!=tauntTargets.Count))return "REJECTED_TARGET";
   if(effect=="SENTRY_CREATE"&&ordinarySentry!=null)return "REJECTED_DEVICE_EXISTS";
   if(record)RecordOrdinary(new JObject{["kind"]="SKILL",["id"]=commandId,["actor"]=actorId,["ability"]=abilityId});
   long step=ordinaryFactory.Step/10000;long T(long value)=>(value+step-1)/step;
   var action=new JObject{["id"]=commandId,["actor"]=actorId,["ability"]=abilityId,["kind"]=ultimate?"ULTIMATE":"ACTIVE",["windupTicks"]=T((long)ability["windupMs"]),["target"]=target.Id,["targets"]=new JArray(target.Id),["due"]=checked(Tick+T((long)ability["windupMs"])),["status"]="CASTING"};
   action["recoverAt"]=checked((long)action["due"]+T((long)ability["recoveryMs"]));
   action["tauntTargets"]=tauntTargets==null?new JArray():new JArray(tauntTargets.Select(t=>new JObject{["id"]=t.Id,["generation"]=t.Generation}));
   ordinaryReady[actorId]=checked(Tick+T((long)ability["cooldownMs"]));ordinaryActions.Add(action);return "CASTING";
  }
  void ResolveOrdinarySupport(JObject action) {
   if(ordinaryNativeRetired||(string)action["status"]!="CASTING")return;
   var caster=allies.Single(a=>a.Identity==(string)action["actor"]);var target=allies.Single(a=>a.Identity==(string)action["target"]);
   if(caster.Hp==0||target.Hp==0){action["status"]="CANCELLED_DOWN";return;}
   var original=ordinaryFactory.Original.Actors.Single(a=>a.OperatorId==caster.Identity);var ability=original.Ability((string)action["ability"]);var stats=ordinaryFactory.ActorStats(caster.Identity);
   long step=ordinaryFactory.Step/10000;long T(long value)=>(value+step-1)/step;
   if((string)ability["effect"]=="SHIELD_AND_TAUNT"){
    int shieldAmount=checked((int)CombatExactValue.Create(caster.Vitals.MaxHp,1).Multiply(CombatExactValue.Create((int)ability["shieldCasterHpBps"],10000)).Multiply(stats.SkillMagnitude).Floor());
    var source=new CombatShieldSource(caster.Identity,(string)action["ability"],0);long expiry=checked(Tick+T((long)ability["shieldDurationMs"]));
    var old=caster.Vitals.ReadShieldLayers(Tick).SingleOrDefault(l=>l.Source.Same(source));int before=old?.Amount??0;long priorExpiry=old?.ExpiryTick??0;
    if(ordinaryHealth==null)caster.Vitals.Shield(source,shieldAmount,expiry,Tick);else action["healthReceipt"]=OrdinaryHealthReceipt(ordinaryHealth.ApplyResolvedShield((string)action["id"],caster.Identity,source,shieldAmount,expiry,Tick));var layer=caster.Vitals.ReadShieldLayers(Tick).SingleOrDefault(l=>l.Source.Same(source));int after=layer?.Amount??0;
    RecordOrdinaryEffect(action,caster.Identity,"SHIELD_ISSUE",new JObject{["previousRemaining"]=before,["resultingRemaining"]=after,["addedAmount"]=Math.Max(0,after-before),["previousExpiry"]=priorExpiry,["expiry"]=layer?.ExpiryTick??0,["outcome"]=before==0?"ADD":after==before&&priorExpiry==(layer?.ExpiryTick??0)?"UNCHANGED":"REFRESH"});
    foreach(var enemy in (JArray)action["tauntTargets"])
     if(KyleTargetStillEligible(caster.Identity,(string)enemy["id"],(long)enemy["generation"],ability)){
      long previousExpiry=ordinaryTaunts.Where(t=>(string)t["source"]==caster.Identity&&(string)t["target"]==(string)enemy["id"]&&(long)t["generation"]==(long)enemy["generation"]&&(long)t["expiry"]>Tick).Select(t=>(long)t["expiry"]).DefaultIfEmpty(0).Max();
      long tauntExpiry=checked(Tick+T((long)ability["tauntDurationMs"]));
      ordinaryTaunts.Add(new JObject{["source"]=caster.Identity,["target"]=enemy["id"].DeepClone(),["generation"]=enemy["generation"].DeepClone(),["expiry"]=tauntExpiry});
      RecordOrdinaryEffect(action,(string)enemy["id"],previousExpiry==0?"STATUS_ADD":"STATUS_REFRESH",new JObject{["category"]="taunt",["magnitude"]=0,["previousExpiry"]=previousExpiry,["expiry"]=Math.Max(previousExpiry,tauntExpiry),["accepted"]=true},effectIndex:1);
     }
    action["status"]="APPLIED";return;
   }
   if((string)ability["effect"]=="SENTRY_CREATE"){
    int hp=Math.Max((int)ability["minimumHp"],checked((int)((long)caster.Vitals.MaxHp*(int)ability["hpFromCasterMaxHpBps"]/10000)));
    var roof=ordinaryFactory.Original.Roof.Slots.Single(s=>s.OperatorId==caster.Identity);
    ordinarySentry=new JObject{["id"]=Digest(StableJson(new JArray(ordinaryFactory.Original.RunId,caster.Identity,action["id"],"SENTRY"))),["owner"]=caster.Identity,["physicalInstanceId"]=roof.PhysicalInstanceId,["hp"]=hp,["maxHp"]=hp,["spawnTick"]=Tick,["expiry"]=checked(Tick+T((long)ability["lifeMs"])),["intervalMs"]=ability["intervalMs"].DeepClone(),["damageBps"]=ability["damageBps"].DeepClone(),["attack"]=stats.Attack.Snapshot(),["fireStatus"]="IDLE_GEOMETRY_UNAVAILABLE"};
    action["status"]="APPLIED";return;
   }
   var amount=stats.Attack.Multiply(CombatExactValue.Create((int)ability["healBps"],10000)).Multiply(stats.SkillMagnitude).Multiply(CombatExactValue.Create(10000+(int)original.Passive["valueBps"],10000));
   int gain;
   if(ordinaryHealth==null)gain=target.Vitals.RestoreHp(checked((int)amount.Floor()),10000);
   else{var receipt=ordinaryHealth.ApplyResolvedLivingSkillHeal((string)action["id"],target.Identity,checked((int)amount.Floor()),Tick);gain=receipt.HpGain;action["healthReceipt"]=OrdinaryHealthReceipt(receipt);}
   action["status"]="APPLIED";
   RecordOrdinaryEffect(action,target.Identity,"HEAL",new JObject{["actualHpGain"]=gain});
  }
  static JObject OrdinaryHealthReceipt(BCombatSingleTargetReceipt receipt)=>new JObject{["eventId"]=receipt.EventId,["targetId"]=receipt.TargetId,["tick"]=receipt.Tick,["hpAfter"]=receipt.HpAfter,["hpLoss"]=receipt.HpLoss,["absorbed"]=receipt.Absorbed,["hpGain"]=receipt.HpGain,["derivedExcess"]=receipt.DerivedExcess,["shieldAdded"]=receipt.ShieldAdded};
  void RecordOrdinaryEffect(JObject action,string target,string kind,JObject result,string sourceKind="SKILL",int pulseIndex=0,JObject healthReceipt=null,int effectIndex=0){
   var row=new JObject{["runId"]=ordinaryFactory.Original.RunId,["profileSha256"]=Digest(ConfigurationVersion),["configurationSha256"]=Digest(ConfigurationVersion),["journalSequence"]=ordinaryEffects.Count+1,["globalSequence"]=ordinaryActions.IndexOf(action)+1,["commandId"]=action["id"].DeepClone(),["abilityId"]=action["ability"].DeepClone(),["effectIndex"]=effectIndex,["targetId"]=target,["pulseIndex"]=pulseIndex,["kind"]=kind,["tick"]=Tick,["sourceId"]=action["actor"].DeepClone(),["sourceKind"]=sourceKind,["actualResult"]=result};
   if(ordinaryFactory.Attempt!=null){row["nodeId"]=ordinaryFactory.Attempt.NodeId;row["attemptGeneration"]=ordinaryFactory.Attempt.Generation;row["executorId"]=action["actor"].DeepClone();if(effectIndex==0&&action["healthReceipt"]!=null)row["healthReceipt"]=action["healthReceipt"].DeepClone();}
   if(healthReceipt!=null)row["healthReceipt"]=healthReceipt.DeepClone();row["id"]=EffectIdentity(row);ValidateActualResult(kind,result);ordinaryEffects.Add(row);
  }
  void OrdinaryControl(string kind,JToken value) {
   bool changed;
   if(kind=="PAUSE"||kind=="ULTIMATE_AUTO"){
    if(value?.Type!=JTokenType.Boolean)throw new ArgumentException("Boolean control required.");
    changed=(bool)value!=(kind=="PAUSE"?clock.IsPaused:ordinaryUltimateAuto);
   }else if(kind=="SPEED"||kind=="THROTTLE"){
    if(value?.Type!=JTokenType.Integer)throw new ArgumentException("Integer control required.");
    long number=(long)value;
    if(kind=="SPEED"&&number!=2&&number!=3&&number!=4||kind=="THROTTLE"&&(number<7000||number>13000))throw new ArgumentOutOfRangeException(nameof(value));
    changed=kind=="SPEED"?number!=clock.SpeedHalfUnits:20000L*number/10000!=ordinaryTarget;
   }else throw new ArgumentException("Unknown ordinary control.");
   // Old checkpoints may contain repeated controls; preserve their replay bytes, omit new no-op writes.
   if(!changed&&!ordinaryReplaying)return;
   RecordOrdinary(new JObject{["kind"]=kind,["value"]=value.DeepClone()},changed);
   if(kind=="PAUSE")clock.SetPaused((bool)value);else if(kind=="SPEED")clock.SetSpeed((int)value);else if(kind=="ULTIMATE_AUTO")ordinaryUltimateAuto=(bool)value;
   else if(kind=="THROTTLE"){int bps=(int)value;if(bps<7000||bps>13000)throw new ArgumentOutOfRangeException(nameof(value));ordinaryTarget=20000L*bps/10000;}
   else throw new ArgumentException("Unknown ordinary control.");
  }
  internal JObject ResolveOrdinaryObstacle(JObject resolution){
   if(ordinaryNativeRetired||ordinaryObstacle==null)throw new InvalidOperationException("Current issued obstacle owner required.");
   if(ordinaryObstacle.Phase=="OBSTACLE"){
    if((long?)resolution?["atMs"]!=OrdinaryMs||(int?)resolution?["trainHp"]!=ordinaryHealth.Hp(ordinaryFactory.TrainId))throw new ArgumentException("ordinary_obstacle_owner_state_conflict");
   }
   var receipt=ordinaryObstacle.Resolve(resolution);
   if(ordinaryObstacle.Phase=="DEFEAT")Outcome=BasicCombatOutcome.Failure;
   return receipt;
  }
  string CaptureOrdinary()=>StableJson(CaptureOrdinaryState());
  JObject CaptureOrdinaryState() {
   var c=clock.Capture();var capture=new JObject{["schema"]=OrdinaryFiveCombatFactory.CheckpointVersion,["configurationSha256"]=Digest(ConfigurationVersion),
    ["clock"]=new JObject{["step"]=c.StepTicks,["debt"]=c.DebtHalfTicks,["completed"]=c.CompletedSteps,["speed"]=c.SpeedHalfUnits,["paused"]=c.IsPaused},
    ["actors"]=new JArray(allies.Concat(enemies).Select(a=>new JObject{["id"]=a.Identity,["vitals"]=a.Vitals.CaptureLocal()})),
    ["ready"]=JObject.FromObject(ordinaryReady),["charge"]=JObject.FromObject(ordinaryCharge),["actions"]=ordinaryActions.DeepClone(),["effects"]=ordinaryEffects.DeepClone(),
    ["motion"]=new JObject{["front"]=ordinaryFront,["speed"]=ordinarySpeed,["target"]=ordinaryTarget,["remainder"]=ordinaryDistanceRemainder},["sentry"]=ordinarySentry?.DeepClone(),["taunts"]=ordinaryTaunts.DeepClone(),["ultimateAuto"]=ordinaryUltimateAuto,["operations"]=ordinaryOperations.DeepClone()};
   if(ordinaryObstacle!=null)capture["obstacleSequence"]=ordinaryObstacle.CaptureLocal();
   if(ordinaryObstacleExecution!=null)capture["obstacleExecution"]=ordinaryObstacleExecution.Capture();
   if(ordinaryLiveGunnerMotion!=null)capture["gunnerMotion"]=ordinaryLiveGunnerMotion.Capture();
   if(ordinaryHealth!=null){capture["health"]=ordinaryHealth.CaptureState();capture["projectiles"]=ordinaryProjectiles.Capture();if(ordinaryBasics!=null)capture["basicExecution"]=ordinaryBasics.Capture();if(ordinaryGunners!=null)capture["gunnerExecution"]=ordinaryGunners.Capture();if(ordinaryBarrage!=null)capture["reinaBarrage"]=ordinaryBarrage.Capture();if(ordinaryBoarding!=null)capture["boardingExecution"]=ordinaryBoarding.Capture();capture["executionStarted"]=ordinaryExecutionStarted;capture["random"]=random.ExportState();capture["outcome"]=Outcome.ToString();capture["completedWaves"]=CompletedWaves;}return capture;
  }
  internal JToken CaptureSceneState()=>ordinaryFactory?.NodeInput!=null?(JToken)CaptureOrdinaryState():new JValue(CaptureLocalState());
  // Scene transactions copy only this live owner; external node checkpoints remain restart-only.
  internal int OrdinaryOperationCount=>ordinaryOperations.Count;
  internal BasicCombatSimulation CloneOwnedForScene(Func<BasicCombatSimulation> factory,out JToken captured,bool captureComparison=true) {
   captured=captureComparison||ordinaryFactory?.NodeInput==null?CaptureSceneState():null;
   if(ordinaryFactory?.NodeInput==null)return RestoreLocalState((string)captured,factory);
   var model=factory();ordinaryFactory.Validate(model);
   if(model.ordinaryFactory?.NodeInput==null||model.Tick!=0||!ReferenceEquals(ordinaryFactory.BasicSources,model.ordinaryFactory.BasicSources)||!ReferenceEquals(ordinaryFactory.GunnerSources,model.ordinaryFactory.GunnerSources)||!ReferenceEquals(ordinaryFactory.ReinaBarrageSources,model.ordinaryFactory.ReinaBarrageSources)||!ReferenceEquals(ordinaryFactory.SceneMotion,model.ordinaryFactory.SceneMotion)||!ReferenceEquals(ordinaryFactory.NyxSources,model.ordinaryFactory.NyxSources))throw new ArgumentException("Original node factory and native sources required.");
   model.clock=FixedStepDebt.Restore(clock.Capture());model.random=random;model.Outcome=Outcome;model.CompletedWaves=CompletedWaves;
   model.ordinaryHealth=ordinaryHealth.CloneOwnedNode();model.ordinaryObstacle=ordinaryObstacle?.CloneOwned();
   model.ordinaryProjectiles=ordinaryProjectiles.CloneOwned(model.ordinaryHealth,()=>!model.ordinaryNativeRetired);
   if(ordinaryLiveSources!=null){model.ordinaryBoarding=ordinaryBoarding?.CloneOwned(model.ordinaryHealth);model.InitializeOrdinaryLiveSources();model.ordinaryLiveGunnerMotion=ordinaryLiveGunnerMotion.CloneOwned();}
   model.ordinaryObstacleExecution=ordinaryObstacleExecution?.CloneOwned(model.ordinaryHealth,model.ordinaryLiveNumbers==null?null:new Func<long,JObject>(t=>model.ordinaryLiveNumbers.Defense(model.ordinaryFactory.TrainId,t)));
   model.ordinaryBasics=ordinaryBasics?.CloneOwnedWithSources(model.ordinaryHealth,model.ordinaryProjectiles,model.ordinaryLiveSources?.Basic??ordinaryFactory.BasicSources);
   model.ordinaryGunners=ordinaryGunners?.CloneOwnedWithSources(model.ordinaryHealth,model.ordinaryLiveSources?.Gunner??ordinaryFactory.GunnerSources);model.ordinaryExecutionStarted=ordinaryExecutionStarted;
   model.ordinaryBarrage=ordinaryBarrage?.CloneOwned(model.ordinaryHealth,model.ordinaryLiveBarrageSources);
   if(ordinaryNyx!=null){model.ordinaryNyx?.Dispose();model.ordinaryNyx=ordinaryNyx.CloneOwned(model.ordinaryHealth,model.ordinaryLiveNumbers.NyxZoneAttack,model.ordinaryLiveNumbers.Defense);model.ordinaryNyxDispatch=ordinaryNyxDispatch?.CloneOwned(model.ordinaryNyx);}
   model.ordinaryReady.Clear();foreach(var entry in ordinaryReady)model.ordinaryReady.Add(entry.Key,entry.Value);
   model.ordinaryCharge.Clear();foreach(var entry in ordinaryCharge)model.ordinaryCharge.Add(entry.Key,entry.Value);
   void Copy(JArray source,JArray target){target.Clear();foreach(var entry in source)target.Add(entry.DeepClone());}
   Copy(ordinaryActions,model.ordinaryActions);Copy(ordinaryEffects,model.ordinaryEffects);Copy(ordinaryOperations,model.ordinaryOperations);Copy(ordinaryTaunts,model.ordinaryTaunts);
   model.ordinaryFront=ordinaryFront;model.ordinarySpeed=ordinarySpeed;model.ordinaryTarget=ordinaryTarget;model.ordinaryDistanceRemainder=ordinaryDistanceRemainder;
   model.ordinaryUltimateAuto=ordinaryUltimateAuto;model.ordinarySentry=(JObject)ordinarySentry?.DeepClone();
   foreach(var entry in ordinaryReplayInputs){
    int index=ordinaryOperations.IndexOf(entry.Command);if(index<0)throw new InvalidOperationException("Owned replay input lost its operation.");
    model.ordinaryReplayInputs.Add((entry.Tick,(JObject)model.ordinaryOperations[index]));
   }
   if(captured!=null&&!JToken.DeepEquals(model.CaptureOrdinaryState(),captured))throw new InvalidOperationException("Owned node copy differs.");
   return model;
  }
  static BasicCombatSimulation RestoreOrdinary(JObject saved,Func<BasicCombatSimulation> factory) {
   var model=factory();if(model?.ordinaryFactory==null||model.Tick!=0)throw new ArgumentException("Original ordinary factory required.");
   if(model.ordinaryFactory.NodeInput!=null)throw new InvalidOperationException("Node preparation is restart-only; saved mid-node state cannot be replayed.");
   var operations=saved["operations"] as JArray;if(operations==null||operations.Count>10000)throw new ArgumentException("Ordinary operation limit.");
   model.ordinaryReplaying=true;
   foreach(var token in operations){var op=token as JObject;string kind=Text(op,"kind");
    if(kind=="ADVANCE"){Fields(op,"kind","elapsed","budget");model.AdvanceOrdinary(Nat(op,"elapsed"),checked((int)Nat(op,"budget",int.MaxValue)));}
    else if(kind=="SKILL"){Fields(op,"kind","id","actor","ability");model.RequestOrdinarySkill(Text(op,"id"),Text(op,"actor"),Text(op,"ability"));}
    else {Fields(op,"kind","value");model.OrdinaryControl(kind,op["value"]);}
   }
   model.ordinaryReplaying=false;
   if(model.CaptureOrdinary()!=StableJson(saved))throw new ArgumentException("Ordinary checkpoint replay differs.");return model;
  }
  JObject ReadOrdinaryHud() {
   var time=clock.Capture();var original=ordinaryFactory.Original;var rows=new JArray();
   foreach(var a in original.Actors){var health=allies.Single(x=>x.Identity==a.OperatorId);var roof=original.Roof.Slots.Single(x=>x.OperatorId==a.OperatorId);var basic=original.Basic.Profiles.Single(x=>x.OperatorId==a.OperatorId);
    rows.Add(new JObject{["id"]=a.OperatorId,["visual"]=a.OperatorId,["enemy"]=false,["train"]=false,["device"]=false,["deviceType"]=null,["exterior"]=false,["slot"]=a.Slot,["hp"]=health.Hp,["maxHp"]=health.Vitals.MaxHp,["shield"]=health.Vitals.ShieldRemaining(Tick),["kyleGuardShield"]=KyleGuardAmount(health.Vitals,Tick),["availableAt"]=0,["recoverAt"]=0,["protected"]=false,["attackDue"]=-1,["attackWindup"]=basic.WindupMs.Value*10000/ordinaryFactory.Step,["attackTarget"]=null,["attacking"]=false,["casting"]=ordinaryActions.Any(c=>(string)c["actor"]==a.OperatorId&&(string)c["status"]=="CASTING"),["physicalInstanceId"]=roof.PhysicalInstanceId,["physicalOrder"]=roof.PhysicalOrder,["localXmm"]=roof.LocalXmm,["basicStatus"]="IDLE_GEOMETRY_UNAVAILABLE",["basicForm"]=basic.AttackForm,["maximumDistanceMm"]=basic.MaximumDistanceMm});
   }
   if(ordinaryFactory.NodeInput!=null)foreach(var action in ordinaryFactory.NodeInput.Actions.Actors){
    var row=(JObject)rows.Single(r=>(string)r["id"]==action.OperatorId);row["basicNextOpportunityInTicks"]=action.BasicNextOpportunityInTicks;
    if(ordinaryBasics==null)continue;
    var state=ordinaryBasics.ReadActor(action.OperatorId);bool preparing=(string)state["phase"]=="PREPARING";
    row["basicStatus"]=state["phase"].DeepClone();row["basicNextOpportunityInTicks"]=Math.Max(0,(long)state["nextOpportunity"]-Tick);row["recoverAt"]=(long?)state["recoveryEnd"]??0;
    row["attackDue"]=preparing?(long)state["commit"]:-1;row["attackWindup"]=state["startedAt"]?.Type==JTokenType.Integer?(long)state["commit"]-(long)state["startedAt"]:0;
    row["attackTarget"]=state["targetId"]?.DeepClone();row["attacking"]=preparing;
    row["basicStartedAt"]=state["startedAt"]?.DeepClone();row["basicCommitAt"]=state["commit"]?.DeepClone();
   }
   var train=allies.Single(x=>x.Identity==ordinaryFactory.TrainId);
   rows.Add(new JObject{["id"]=train.Identity,["visual"]=train.Identity,["enemy"]=false,["train"]=true,["device"]=false,["deviceType"]=null,["exterior"]=false,["slot"]=0,["hp"]=train.Hp,["maxHp"]=train.Vitals.MaxHp,["shield"]=train.Vitals.ShieldRemaining(Tick),["availableAt"]=0,["recoverAt"]=0,["protected"]=false,["attackDue"]=-1,["attackWindup"]=0,["attackTarget"]=null,["attacking"]=false,["casting"]=false});
   for(int i=0;i<enemies.Count&&(ordinaryObstacle==null||ordinaryObstacle.Phase=="COMBAT");i++){var e=enemies[i];if(ordinaryBoarding?.Contains(e.Identity)==true&&!ordinaryBoarding.Registered(e.Identity))continue;var row=new JObject{["id"]=e.Identity,["visual"]="enemy_spike_cart_gunner",["enemy"]=true,["train"]=false,["device"]=false,["exterior"]=true,["slot"]=i,["hp"]=e.Hp,["maxHp"]=e.Vitals.MaxHp,["shield"]=0,["availableAt"]=0,["protected"]=false,["attackDue"]=-1,["attackWindup"]=0,["attacking"]=false,["casting"]=false,["physicalInstanceId"]=ordinaryFactory.Encounter.IssuedEnemyTrainId,["frontRelativeXmm"]=6000+i*4000,["basicStatus"]="IDLE_GEOMETRY_UNAVAILABLE"};
    if(ordinaryBoarding?.Contains(e.Identity)==true){row["visual"]=ordinaryFactory.BoardingSource.Boarding.OriginalSource.Waves.SelectMany(w=>w.Instances).Single(x=>x.InstanceId==e.Identity).DefinitionId;row["physicalInstanceId"]=ordinaryFactory.Original.Rail.PhysicalInstanceIds[ordinaryBoarding.Slot(e.Identity)];row["slot"]=ordinaryBoarding.Slot(e.Identity);row["generation"]=ordinaryBoarding.ProjectileTargetGeneration(e.Identity);row["basicStatus"]=ordinaryBoarding.Category(e.Identity);row["boarding"]=true;row["hitHeightMm"]=ordinaryBoarding.HitHeightMm(e.Identity);row["hitRadiusMm"]=ordinaryBoarding.HitRadiusMm(e.Identity);row["tauntedBy"]=OrdinaryTauntTarget(e.Identity,ordinaryBoarding.ProjectileTargetGeneration(e.Identity));}
    else if(ordinaryGunners!=null){var state=ordinaryGunners.ReadActor(e.Identity);bool preparing=(string)state["phase"]=="WINDUP";
     row["basicStatus"]=state["phase"].DeepClone();row["availableAt"]=ordinaryGunners.StartTick;row["nextLaunch"]=state["nextLaunch"].DeepClone();row["recoverAt"]=(long?)state["recoveryEnd"]??0;
     row["attacking"]=preparing;row["attackDue"]=preparing?(long)state["commit"]:-1;row["attackWindup"]=preparing?(long)state["commit"]-(long)state["startedAt"]:0;row["attackTarget"]=preparing?state["targetId"].DeepClone():JValue.CreateNull();
    }if(ordinaryLiveSources!=null)row["exterior"]=false;rows.Add(row);
   }
   if(ordinaryLiveSources!=null)foreach(var row in rows.OfType<JObject>().Where(r=>(bool?)r["train"]!=true)){var foot=ordinaryLiveSources.Foot((string)row["id"],Tick);row["footRelativeToFrontMm"]=new JArray(foot[0]-ordinaryFront,foot[1],foot[2]);if(ordinaryFactory.Original.Rail.LocalFiveCar&&(bool?)row["enemy"]==false){row["partyMoving"]=ordinaryBoarding.PartyMoving((string)row["id"]);row["physicalInstanceId"]=ordinaryFactory.Original.Rail.PhysicalInstanceIds[ordinaryBoarding.PartyCarOrder((string)row["id"])];}}
   // Until node actions are supplied, internal preparation placeholders are not confirmed display values.
   JObject Skill(OrdinaryFiveStartInput.Actor actor,bool ultimate){var id=actor.OperatorId;var ability=actor.Ability(id+(ultimate?".ultimate":".active"));var hp=allies.Single(a=>a.Identity==id);long remaining=Math.Max(0,ordinaryReady[id]-Tick)*(ordinaryFactory.Step/10000);bool supported=new[]{"HEAL_ONE","SHIELD_AND_TAUNT","SENTRY_CREATE"}.Contains((string)ability["effect"]);
    bool nyx=id=="op_nyx";if(nyx&&ultimate)remaining=0;bool nyxReady=nyx&&ordinaryNyxDispatch!=null&&IsExecutionReady&&(ordinaryObstacle==null||ordinaryObstacle.Phase=="COMBAT");
    string nyxBlocked=nyxReady?NyxCastBlocker(ultimate,Tick,ordinaryReady[id],OrdinarySkillBusy(id)||OrdinaryBasicBusy(id),hp.Hp>0,ordinaryCharge[id],ultimate?(int)ability["cost"]:0):null;
    bool nyxEnabled=nyxReady&&!clock.IsPaused&&Outcome==BasicCombatOutcome.Running&&nyxBlocked==null;
    bool guardEnabled=!ultimate&&(string)ability["effect"]=="SHIELD_AND_TAUNT"&&IndependentOrdinaryGuard&&!clock.IsPaused&&Outcome==BasicCombatOutcome.Running&&hp.Hp>0&&remaining==0&&!OrdinarySkillBusy(id)&&!OrdinaryBasicBusy(id)&&!(ordinaryBoarding?.PartyMoving(id)??false);
    bool reina=id=="op_reina"&&ultimate&&(string)ability["effect"]=="REINA_BARRAGE";
    string reinaStatus=!reina?null:ordinaryBarrage==null||!ordinaryExecutionStarted||ordinaryObstacle?.Phase!="COMBAT"?"PREPARING":hp.Hp<=0?"DOWN":ordinaryCharge[id]<(int)ability["cost"]?"CHARGING":OrdinarySkillBusy(id)||OrdinaryBasicBusy(id)||(ordinaryBoarding?.PartyMoving(id)??false)?"BUSY":clock.IsPaused||Outcome!=BasicCombatOutcome.Running?"STOPPED":"READY";
    string nyxStatus=!nyxReady?"PREPARING":nyxBlocked!=null?nyxBlocked.Substring("REJECTED_".Length):nyxEnabled?"READY":"STOPPED";
    return new JObject{["actor"]=id,["ability"]=ability["id"].DeepClone(),["effect"]=ability["effect"].DeepClone(),["hp"]=hp.Hp,["maxHp"]=hp.Vitals.MaxHp,["shield"]=hp.Vitals.ShieldRemaining(Tick),["chargeMode"]=ultimate,["charge"]=ordinaryFactory.NodeInput==null?JValue.CreateNull():new JValue(ordinaryCharge[id]),["cooldownMs"]=ordinaryFactory.NodeInput==null?JValue.CreateNull():new JValue(remaining),["cooldownTotalMs"]=ultimate?0:(long)ability["cooldownMs"],["enabled"]=reina?reinaStatus=="READY":nyx?nyxEnabled:guardEnabled,["status"]=reina?reinaStatus:nyx?nyxStatus:!OrdinaryReady&&!((string)ability["effect"]=="SHIELD_AND_TAUNT"&&IndependentOrdinaryGuard)&&!((string)ability["effect"]=="HEAL_ONE"&&IndependentOrdinaryHealing)?"PREPARING":!supported?"NATIVE_GEOMETRY_UNAVAILABLE":remaining>0?"COOLDOWN":"READY"};}
   var unsupported=new JArray("NATIVE_MUZZLE_HIT_VOLUME_LOS","DIRECT_DAMAGE_AND_BURSTS","ULTIMATE_EXECUTION","SENTRY_FIRE","ARTILLERY_HOST","OPERATOR_DOWN_RECOVERY","SERVER_COMPLETION_OWNER");if(ordinaryFactory.NodeInput!=null)unsupported.Add("NODE_EFFECTS_AND_SPATIAL");
   return new JObject{["localParty"]=ordinaryFactory.Original.Rail.LocalFiveCar?new JObject{["phase"]=ordinaryBoarding?.PartyPhase}:null,["ordinaryObstacle"]=ordinaryObstacle?.Capture(),["ordinaryObstacleExecution"]=ordinaryObstacleExecution?.CaptureView(OrdinaryMs),["schema"]=OrdinaryFiveCombatFactory.Version,["displayOperationId"]=original.DisplayOperationId,["executionPhase"]=IsExecutionReady?null:ordinaryFactory.Encounter==null?"AWAITING_ENCOUNTER":"AWAITING_NATIVE_GEOMETRY",["encounterReady"]=ordinaryFactory.Encounter!=null,["fullCombatReady"]=IsExecutionReady,
    ["unsupported"]=unsupported,["ordinarySentry"]=ordinarySentry?.DeepClone(),["unresolvedReinaBarrage"]=ordinaryBarrage?.HasPending??false,["reinaBarrages"]=ordinaryBarrage?.ReadFeedback()??new JArray(),["ordinaryGunnerProjectiles"]=ordinaryGunners?.ReadPresentationShots(Tick)??new JArray(),
    ["ordinaryBasicProjectiles"]=ordinaryProjectiles?.ReadPresentationShots(Tick)??new JArray(),["actors"]=rows,["tick"]=Tick,["stepTimeSpanTicks"]=ordinaryFactory.Step,["motionSeconds"]=OrdinaryMs/1000d,["wave"]=ordinaryObstacle!=null&&ordinaryObstacle.Phase!="COMBAT"?0:1,["totalWaves"]=1,["outcome"]=Outcome.ToString(),["paused"]=time.IsPaused,["speedHalfUnits"]=time.SpeedHalfUnits,
    ["trainHp"]=train.Hp,["trainMaxHp"]=train.Vitals.MaxHp,["skills"]=new JArray(original.Actors.Select(a=>Skill(a,false))),["ultimates"]=new JArray(original.Actors.Select(a=>Skill(a,true))),["ultimate"]=null,["ultimateAuto"]=ordinaryUltimateAuto,["generalAuto"]=true,["manuals"]=new JArray(),["manualsAvailability"]="UNAVAILABLE",["frontSlots"]=new JArray(0,1,2),["hits"]=new JArray(),["casts"]=ordinaryActions.DeepClone(),["effectResults"]=ordinaryEffects.DeepClone(),
    ["ordinaryRail"]=new JObject{["minimumBps"]=7000,["maximumBps"]=13000,["throttleBps"]=ordinaryTarget/2,["frontArcMm"]=ordinaryFront,["tailArcMm"]=ordinaryFront-original.Rail.TotalLengthMm,["speedMmPerSecond"]=ordinarySpeed,["targetSpeedMmPerSecond"]=ordinaryTarget,["lengthMm"]=original.Rail.TrackLengthMm,["physicalInstanceIds"]=new JArray(original.Rail.PhysicalInstanceIds)}};
  }
 }
}
