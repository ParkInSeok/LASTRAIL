using System;
using System.Linq;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BDirectAttackBasicLoop {
  public const string WaveVersion="unity.combat-b.wave-boarding-native-local.v1.20261004";
  public const string BoardingEventsWaveVersion="unity.combat-b.boarding-events-native-local.v2.20261004";
  BBoardingEventsNodeSnapshot receivedSource;
  BScoutGunnerNodeSnapshot waveSource;BWaveBoardingLocalPaths wavePaths;BWaveBoardingPolicy wavePolicy;BEnemyBoardingOwner boarding;BWaveEntryOwner waveEntry;
  public bool OrdinaryRuntimeGeometrySupported=>boarding?.HasSpatial==true;
  public static BDirectAttackBasicLoop CreateWaveRuntime(BDirectAttackBasicLoopBinding binding,BScoutGunnerNodeSnapshot source,BWaveBoardingLocalPaths paths,int capacity){
   if(source==null)throw new ArgumentNullException(nameof(source));return CreateWaveRuntimeCore(binding,source,paths,capacity,BWaveBoardingPolicy.ForDepth(source.VisitDepth));
  }
  public static BDirectAttackBasicLoop CreateBoardingEventsRuntime(BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot source,BWaveBoardingLocalPaths paths,int capacity){if(source==null)throw new ArgumentNullException(nameof(source));var owner=CreateWaveRuntimeCore(binding,source.OriginalSource,paths,capacity,BWaveBoardingPolicy.FromReceived(source.OriginalSource.VisitDepth,source.Definition));owner.receivedSource=source;return owner;}
  static BDirectAttackBasicLoop CreateWaveRuntimeCore(BDirectAttackBasicLoopBinding binding,BScoutGunnerNodeSnapshot source,BWaveBoardingLocalPaths paths,int capacity,BWaveBoardingPolicy policy){
   if(source==null)throw new ArgumentNullException(nameof(source));var input=binding.Composition.Attack;
   BuffWire.Need(input.IsWaveRuntime&&(string)input.Capture()["waveSourceHash"]==(string)source.Capture()["sourceHash"],"b_wave_source_binding");
   BuffWire.Need(paths==null||(string)paths.Capture()["inputHash"]==input.OriginalHash,"b_wave_path_input_binding");
   BuffWire.Need(new System.Collections.Generic.HashSet<string>(binding.OrderedSources.Where(id=>input.Entities[id].Kind=="LOCAL_ENEMY")).SetEquals(source.Waves.SelectMany(w=>w.Instances).Select(x=>x.InstanceId)),"b_wave_missing_enemy_route");
   var owner=new BDirectAttackBasicLoop(binding,capacity,true){waveSource=source,wavePaths=paths,wavePolicy=policy};
   owner.boarding=new BEnemyBoardingOwner(source,input,owner.wavePolicy,paths);owner.waveEntry=new BWaveEntryOwner(source,owner.wavePolicy,input.StepTimeSpanTicks/10);owner.session.BindWaveEnemies(owner.boarding);
   var timing=new JObject{["version"]=BEnemyBasicTimingOwner.WaveVersion,["provenance"]="EXPLICIT_LOCAL_TIMING_NOT_OPERATING_ENEMY_SOURCE",["bindingHash"]=binding.OriginalHash,["timings"]=new JArray(binding.OrderedSources.Where(id=>input.Entities[id].Kind=="LOCAL_ENEMY").Select(id=>new JObject{["sourceId"]=id,["targetId"]=binding.Target(id),["targetableAt"]=0,["periodMicroseconds"]=input.Entities[id].Interval,["windupMs"]=500,["recoveryMs"]=300}))};
   owner.enemyTiming=new BEnemyBasicTimingOwner(binding,timing,BDirectAttackInput.Digest(timing),owner.session);owner.enemyTiming.ConfigureAdmission(BEnemyAttackAdmissionPolicy.ForBand(owner.wavePolicy.Band));
   Func<long> next=()=>checked(++owner.sequence);owner.waveEntry.BeforeStep(owner.boarding,owner.session,next);owner.SyncWave(owner.boarding,owner.session,owner.enemyTiming,next);return owner;
  }
  void SyncWave(BEnemyBoardingOwner board,BDirectAttackBuffSession native,BEnemyBasicTimingOwner timing,Func<long> next){foreach(var actor in waveSource.Waves.SelectMany(w=>w.Instances)){if(board.HasSpatial)timing.RetargetWave(actor.InstanceId,board.SpatialTarget(actor.InstanceId),native,next);timing.ObserveWaveLifetime(actor.InstanceId,board.Generation(actor.InstanceId),native.Hp(timing.Target(actor.InstanceId))==0?1:0,board.AttackEligible(actor.InstanceId),native,next);}foreach(var roof in board.NewRoof()){long period=CombatActionTiming.BasicAttackPeriodTicks(binding.Composition.Attack.Entities[roof.id].Interval,binding.Composition.Attack.StepTimeSpanTicks/10,Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>());native.ScheduleEnemyBasic(roof.id,Math.Max(native.NextBasicDue(roof.id),checked(roof.tick+period)));}}
  string WaveTarget(string source,BEnemyBoardingOwner board,BDirectAttackBuffSession native)=>board.TargetableOrder().FirstOrDefault(id=>native.LoopTargetEligible(source,id));
  public BDirectAttackBasicLoopResult ApplyWaveBasic(string eventId,string sourceId,string targetId,long targetGeneration)=>Commit(new JObject{["kind"]="WAVE_BASIC",["eventId"]=eventId,["sourceId"]=sourceId,["targetId"]=targetId,["targetGeneration"]=targetGeneration});
  void CaptureWave(JObject root){if(waveEntry==null)return;root["version"]=WaveVersion;root["waveSourceHash"]=waveSource.Capture()["sourceHash"].DeepClone();root["boardingPolicyVersion"]=BWaveBoardingPolicy.Version;root["ordinaryRuntimeGeometrySupported"]=OrdinaryRuntimeGeometrySupported;if(boarding?.Spatial!=null)root["spatialSource"]=boarding.Spatial.Capture();root["boarding"]=boarding.Capture();root["waveEntry"]=waveEntry.Capture();if(receivedSource!=null){root["version"]=BoardingEventsWaveVersion;root["boardingEventsSourceHash"]=receivedSource.SourceHash;root["receivedDefinitionHash"]=wavePolicy.ReceivedDefinitionHash;root["receivedBoardingPolicyVersion"]=receivedSource.Definition.Boarding.Version;root["receivedEventPolicyVersion"]=receivedSource.Definition.Events.Version;root["boardingEventsSource"]=receivedSource.Capture();}}
  public static BDirectAttackBasicLoop RestoreBoardingEventsRuntime(JObject cache,string independentlyTrustedHash,BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot source,BWaveBoardingLocalPaths paths,int capacity){BuffWire.Need(cache!=null&&BDirectAttackInput.Digest(cache)==independentlyTrustedHash,"b_boarding_events_cache_hash");BuffWire.Fields(cache,"version","bindingHash","capacity","sequence","commands","state","results","enemyTiming","waveSourceHash","boardingPolicyVersion","ordinaryRuntimeGeometrySupported","boarding","waveEntry","boardingEventsSourceHash","receivedDefinitionHash","receivedBoardingPolicyVersion","receivedEventPolicyVersion","boardingEventsSource");var owner=CreateBoardingEventsRuntime(binding,source,paths,capacity);foreach(var command in BuffWire.Array(cache["commands"],0,capacity))owner.Commit(BuffWire.Object(command));BuffWire.Need(JToken.DeepEquals(cache,owner.Capture()),"b_boarding_events_cache_replay");return owner;}
  public static BDirectAttackBasicLoop RestoreWaveRuntime(JObject cache,string independentlyTrustedHash,BDirectAttackBasicLoopBinding binding,BScoutGunnerNodeSnapshot source,BWaveBoardingLocalPaths paths,int capacity){
   BuffWire.Need(cache!=null&&BDirectAttackInput.Digest(cache)==independentlyTrustedHash,"b_wave_cache_hash");BuffWire.Fields(cache,"version","bindingHash","capacity","sequence","commands","state","results","enemyTiming","waveSourceHash","boardingPolicyVersion","ordinaryRuntimeGeometrySupported","boarding","waveEntry");var owner=CreateWaveRuntime(binding,source,paths,capacity);foreach(var command in BuffWire.Array(cache["commands"],0,capacity))owner.Commit(BuffWire.Object(command));BuffWire.Need(JToken.DeepEquals(cache,owner.Capture()),"b_wave_cache_replay");return owner;
  }
 }
}

