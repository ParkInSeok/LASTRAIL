using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 public sealed partial class BasicCombatSimulation {
  internal JArray CaptureOrdinaryHealthChanges()=>ordinaryHealth.CaptureOrdinaryHealthChanges();
  OrdinaryLiveGunnerMotion ordinaryLiveGunnerMotion;OrdinaryBoardingExecution ordinaryBoarding;
  void InitializeOrdinaryLiveSources(){
   if(ordinaryFactory.BoardingSource!=null&&ordinaryBoarding==null)ordinaryBoarding=new OrdinaryBoardingExecution(ordinaryFactory,ordinaryHealth);
   ordinaryLiveGunnerMotion=new OrdinaryLiveGunnerMotion(ordinaryFactory,ordinaryFactory.LiveDefinitionJson);
   ordinaryLiveNumbers=new OrdinaryLiveNumericAuthority(ordinaryFactory,ordinaryHealth.Hp,ReadOrdinaryCurrentEffects,ReadOrdinaryKyleMode);
   ordinaryBoarding?.BindEffectivePeriod(ordinaryLiveNumbers.EffectivePeriod);
   if(ordinaryFactory.Original.Actors.Any(a=>a.OperatorId=="op_nyx"))ordinaryNyx=new NyxZoneExecution("nyx-owner:"+Digest(ConfigurationVersion),ordinaryHealth,ordinaryFactory.Step/10000,ordinaryLiveNumbers.NyxZoneAttack,ordinaryLiveNumbers.Defense);
   if(ordinaryFactory.NyxSources!=null)ordinaryNyxDispatch=new NyxZoneDispatch(ordinaryNyx,ordinaryFactory.NyxSources);
   ordinaryLiveSources=new OrdinaryLiveCombatSources(ordinaryFactory,ordinaryFactory.LiveDefinitionJson,
    tick=>{if(tick!=Tick)throw new InvalidOperationException("Current owned geometry tick required.");return ordinaryFront;},
    (id,tick)=>ordinaryBoarding?.Contains(id)==true?ordinaryBoarding.ProjectileTargetGeneration(id):1,ordinaryLiveNumbers.EffectivePeriod,ordinaryLiveNumbers.BasicAttack,ordinaryLiveNumbers.GunnerAttack,ordinaryLiveNumbers.Defense,ordinaryLiveNumbers.KyleMode,ReadOrdinaryInterception,
    (id,tick)=>ordinaryLiveGunnerMotion.MotionStatus(id,tick),(id,tick)=>ordinaryLiveGunnerMotion.NoTargetStatus(id,tick));
   if(ordinaryBoarding!=null){ordinaryLiveSources.BindBoarding(ordinaryBoarding);ordinaryBoarding.BindPartyReadiness(id=>!OrdinarySkillBusy(id)&&!OrdinaryBasicBusy(id));}
   ordinaryLiveSources.BindEntityOffsets((id,tick)=>ordinaryFactory.Original.Rail.LocalFiveCar&&ordinaryFactory.Original.Actors.Any(a=>a.OperatorId==id)?ordinaryBoarding.PartyOffset(id):ordinaryLiveGunnerMotion.Offset(id,tick));
   BindOrdinaryLiveBarrage();
   ordinaryObstacleExecution=new OrdinaryNodeObstacleExecution(ordinaryFactory,ordinaryHealth,JObject.Parse(ordinaryFactory.LiveDefinitionJson),tick=>ordinaryLiveNumbers.Defense(ordinaryFactory.TrainId,tick));
  }
  void StartOrdinaryLiveWave(){
   if(ordinaryBasics!=null||ordinaryGunners!=null)throw new InvalidOperationException("Original wave already started.");
   ordinaryBoarding?.Start(Tick);
   ordinaryBasics=new OrdinaryNodeBasicExecution(ordinaryFactory,ordinaryHealth,ordinaryProjectiles,ordinaryLiveSources.Basic,Tick);
   ordinaryGunners=new OrdinaryNodeGunnerExecution(ordinaryFactory,ordinaryHealth,ordinaryLiveSources.Gunner,Tick);
  }
  JObject ReadOrdinaryCurrentEffects(long tick){
   if(tick!=Tick)throw new InvalidOperationException("Effects must be read at the owned logical tick.");
   if(ordinarySentry!=null)throw new NotSupportedException("Live sentry effect source is not implemented.");
   var a=ordinaryFactory.Attempt;var shields=new JArray();
   foreach(var actor in allies.Concat(enemies))foreach(var layer in actor.Vitals.ReadShieldLayers(tick))shields.Add(new JObject{["targetId"]=actor.Identity,["amount"]=layer.Amount,["expiryTick"]=layer.ExpiryTick});
   var active=new JArray(ordinaryTaunts.Where(t=>(long)t["expiry"]>tick).Select(t=>new JObject{["sourceId"]=(string)t["source"]+":taunt",["ownerId"]=t["source"].DeepClone(),["targetId"]=t["target"].DeepClone(),["kind"]="TAUNT",["valueBps"]=0,["expiresAtTick"]=t["expiry"].DeepClone()}));
   if(ordinaryNyx!=null)foreach(var effect in ordinaryNyx.CaptureCurrentEffects(tick))active.Add(effect);
   return new JObject{["version"]=ordinaryNyx==null?OrdinaryLiveNumericAuthority.CurrentVersion:OrdinaryLiveNumericAuthority.NyxCurrentVersion,["accountId"]=a.AccountId,["runId"]=a.RunId,["nodeId"]=a.NodeId,["snapshotHash"]=a.SnapshotHash,["attemptGeneration"]=a.Generation,
    ["entryEffectsHash"]=ordinaryLiveNumbers.EntryEffectsHash,["revision"]=checked(Tick+1),["atTick"]=tick,["activeEffects"]=active,["shields"]=shields,["interceptions"]=new JArray()};
  }
  JObject ReadOrdinaryInterception(string shot,long tick){
   if(ordinaryFactory.LocalInterceptorUninstalled){
    var clear=ordinaryLiveNumbers.Interception(shot,tick);clear["equipmentState"]="EXPLICIT_LOCAL_UNINSTALLED";return clear;
   }
   if(ordinaryProjectiles.HasInterceptionCandidates(tick)||ordinaryBarrage?.HasPending==true)throw new NotSupportedException("Live projectile interception contact source is required when an actual interception candidate exists.");
   return ordinaryLiveNumbers.Interception(shot,tick);
  }
  string ReadOrdinaryKyleMode(string target,long tick){
   var observation=ordinaryLiveSources.ObserveBasic("op_kyle",tick);var row=observation["targets"].Single(t=>(string)t["id"]==target);
   var source=ordinaryLiveSources.CombatBody("op_kyle",tick);var point=row["anchorMm"];double squared=0;for(int i=0;i<3;i++){double delta=(long)source[i]-(long)point[i];squared+=delta*delta;}
   var profile=ordinaryFactory.Original.Basic.Profiles.Single(p=>p.OperatorId=="op_kyle");
   // Near-range Kyle requires his separate contact owner, never silently replace melee with a shot.
   if(profile.MeleeMaximumDistanceMm.HasValue&&squared<=Math.Pow(profile.MeleeMaximumDistanceMm.Value,2)&&(long)row["slot"]==(long)observation["source"]["slot"]&&(ordinaryBoarding?.Contains(target)!=true||ordinaryBoarding.Category(target)=="ROOFTOP")){if(!ordinaryLiveSources.HasKyleContactAnchor)throw new NotSupportedException("Kyle near-range contact execution required.");return "MELEE";}
   return "RANGED";
  }
 }
}
