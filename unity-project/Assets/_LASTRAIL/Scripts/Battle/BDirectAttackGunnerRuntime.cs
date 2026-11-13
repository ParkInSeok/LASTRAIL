using System;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BDirectAttackBasicLoop {
  public const string GunnerInterceptLocalVersion="unity.combat-b.gunner-intercept-native-local.v1.20261004";
  BGunnerProjectileOwner projectiles;BGunnerInterceptionOwner interception;
  public BDirectAttackBasicLoopResult AcceptInterceptionSource(string id,string shot,BInterceptTimingSnapshot receipt){if(receipt==null)throw new ArgumentNullException(nameof(receipt));return Commit(new JObject{["kind"]="INTERCEPT_SOURCE",["eventId"]=id,["shotId"]=shot,["source"]=receipt.Capture(),["sourceHash"]=receipt.Hash});}
  public static BDirectAttackBasicLoop CreateInterceptSourceRuntime(BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot source,BWaveBoardingLocalPaths paths,int capacity){var owner=CreateGunnerInterceptLocalRuntime(binding,source,paths,capacity);owner.interception.NamedSourceBound=true;return owner;}
  BInterceptTimingSnapshot ReadInterceptionCommand(JObject command){BuffWire.Need(interception?.NamedSourceBound==true,"b_intercept_named_source_unbound");var p=waveSource.Capture();var trust=new JObject();foreach(string key in new[]{"origin","nativeContextId","seedHash","sourceHash","nodeId","visitDepth","priorSourceHash"})trust[key]=p[key].DeepClone();var receipt=BInterceptTimingSnapshot.Read(BuffWire.Object(command["source"]),BuffWire.Text(command["sourceHash"]),trust,binding.Composition.Definitions);BuffWire.Need(JToken.DeepEquals(receipt.OriginalSource.Capture(),receivedSource.Capture()),"b_intercept_original_node_conflict");return receipt;}
  public static BDirectAttackBasicLoop RestoreInterceptSourceRuntime(JObject cache,string hash,BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot source,BWaveBoardingLocalPaths paths,int capacity){BuffWire.Need(cache!=null&&BDirectAttackInput.Digest(cache)==hash,"b_intercept_cache_hash");var owner=CreateInterceptSourceRuntime(binding,source,paths,capacity);foreach(var command in BuffWire.Array(cache["commands"],0,capacity))owner.Commit(BuffWire.Object(command));BuffWire.Need(JToken.DeepEquals(owner.Capture(),cache),"b_intercept_source_replay");return owner;}
  public BDirectAttackBasicLoopResult ObserveLocalManualOccupation(string id,string actor,long untilTick)=>Commit(new JObject{["kind"]="LOCAL_MANUAL_OCCUPATION",["eventId"]=id,["actorId"]=actor,["untilTick"]=untilTick});
  public BDirectAttackBasicLoopResult CancelLocalGunnerShot(string id,string shot)=>Commit(new JObject{["kind"]="LOCAL_SHOT_TERMINAL",["eventId"]=id,["shotId"]=shot});
  public static BDirectAttackBasicLoop CreateGunnerInterceptLocalRuntime(BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot source,BWaveBoardingLocalPaths paths,int capacity){var owner=CreateBoardingEventsRuntime(binding,source,paths,capacity);owner.session.BindProjectileTargetLifetimes();owner.projectiles=new BGunnerProjectileOwner(binding,capacity);owner.interception=new BGunnerInterceptionOwner(binding,capacity);return owner;}
  void CaptureGunner(JObject root){if(projectiles==null)return;root["version"]=GunnerInterceptLocalVersion;root["gunnerIntercept"]=projectiles.Capture();root["gunnerIntercept"]["interception"]=interception.Capture();root["namedInterceptionAbilityBound"]=interception.NamedSourceBound;}
  public static BDirectAttackBasicLoop RestoreGunnerInterceptLocalRuntime(JObject cache,string trustedHash,BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot source,BWaveBoardingLocalPaths paths,int capacity){BuffWire.Need(cache!=null&&BDirectAttackInput.Digest(cache)==trustedHash,"b_gunner_cache_hash");var owner=CreateGunnerInterceptLocalRuntime(binding,source,paths,capacity);foreach(var command in BuffWire.Array(cache["commands"],0,capacity))owner.Commit(BuffWire.Object(command));BuffWire.Need(JToken.DeepEquals(cache,owner.Capture()),"b_gunner_cache_replay");return owner;}
 }
}


