using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BDirectAttackBasicLoop {
  const string EnemyTimingLoopVersion="unity.combat-b.enemy-timing-loop-local.v1";
  const string AdmissionLoopVersion="unity.combat-b.attack-admission-loop-local.v1";
  BEnemyBasicTimingOwner enemyTiming;
  public static BDirectAttackBasicLoop CreateEnemyTimingValidation(BDirectAttackBasicLoopBinding binding,JObject timing,string trustedHash,int capacity){var owner=new BDirectAttackBasicLoop(binding,capacity);owner.enemyTiming=new BEnemyBasicTimingOwner(binding,timing,trustedHash,owner.session);return owner;}
  public static BDirectAttackBasicLoop CreateAttackAdmissionValidation(BDirectAttackBasicLoopBinding binding,JObject timing,string trustedHash,BEnemyAttackAdmissionPolicy policy,int capacity){var owner=CreateEnemyTimingValidation(binding,timing,trustedHash,capacity);owner.enemyTiming.ConfigureAdmission(policy);return owner;}
  // Explicit LOCAL lifetime/eligibility boundary only. Does not spawn, reset HP or implement a server wire.
  public BDirectAttackBasicLoopResult RecordEnemyAdmissionState(string eventId,string sourceId,long sourceGeneration,long targetGeneration,long battlefieldGeneration,bool eligible,long hazardUntil)=>Commit(new JObject{["kind"]="ENEMY_ADMISSION_STATE",["eventId"]=eventId,["sourceId"]=sourceId,["sourceGeneration"]=sourceGeneration,["targetGeneration"]=targetGeneration,["battlefieldGeneration"]=battlefieldGeneration,["eligible"]=eligible,["hazardUntil"]=hazardUntil});
  public static BDirectAttackBasicLoop RestoreAttackAdmissionValidation(JObject cache,BDirectAttackBasicLoopBinding binding,JObject timing,string trustedHash,BEnemyAttackAdmissionPolicy policy,int capacity){BuffWire.Fields(cache,"version","bindingHash","capacity","sequence","commands","state","results","enemyTiming");BuffWire.Need((string)cache["version"]==AdmissionLoopVersion&&(string)cache["bindingHash"]==binding.OriginalHash&&BuffWire.Number(cache["capacity"],1,10000)==capacity,"b_admission_cache_binding");var owner=CreateAttackAdmissionValidation(binding,timing,trustedHash,policy,capacity);foreach(var row in BuffWire.Array(cache["commands"],0,capacity))owner.Commit(BuffWire.Object(row));BuffWire.Need(JToken.DeepEquals(cache,owner.Capture()),"b_admission_cache_replay");return owner;}
  // Explicit original action's recovery-end boundary, not a special ability or an AI target decision.
  public BDirectAttackBasicLoopResult RecordEnemyRecoveryEnd(string eventId,string sourceId,long recoveryEndTick)=>Commit(new JObject{["kind"]="ENEMY_RECOVERY_END",["eventId"]=eventId,["sourceId"]=sourceId,["recoveryEndTick"]=recoveryEndTick});
  public static BDirectAttackBasicLoop RestoreEnemyTimingValidation(JObject cache,BDirectAttackBasicLoopBinding binding,JObject timing,string trustedHash,int capacity){
   BuffWire.Fields(cache,"version","bindingHash","capacity","sequence","commands","state","results","enemyTiming");BuffWire.Need((string)cache["version"]==EnemyTimingLoopVersion&&(string)cache["bindingHash"]==binding.OriginalHash&&BuffWire.Number(cache["capacity"],1,10000)==capacity,"b_enemy_timing_cache_binding");var owner=CreateEnemyTimingValidation(binding,timing,trustedHash,capacity);foreach(var row in BuffWire.Array(cache["commands"],0,capacity))owner.Commit(BuffWire.Object(row));BuffWire.Need(JToken.DeepEquals(cache,owner.Capture()),"b_enemy_timing_cache_replay");return owner;
  }
 }
}
