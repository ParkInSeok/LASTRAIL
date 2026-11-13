using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using LastRail.Run;
namespace LastRail.Battle {
 // Immutable adapter for the existing owned-intake + native LOCAL contracts.
 // It projects supplied permanent stats; it never calculates growth or grants ownership.
 public sealed class BNamedOperatorNativeAdapter {
  readonly JObject capabilities;readonly BInterceptTimingSnapshot timing;readonly BBoardingEventsNodeSnapshot boarding;readonly BNamedOperatorSource namedSource;readonly string buffHash;
  public BDirectAttackInput Input {get;}
  public JObject CaptureCapabilities()=>(JObject)capabilities.DeepClone();
  BNamedOperatorNativeAdapter(BDirectAttackInput input,BInterceptTimingSnapshot receipt,JObject status,BBoardingEventsNodeSnapshot node=null,BNamedOperatorSource named=null,string originalBuffHash=null){namedSource=named;buffHash=originalBuffHash;boarding=node??receipt?.OriginalSource;Input=input;timing=receipt;capabilities=(JObject)status.DeepClone();}
  public static BNamedOperatorNativeAdapter ReadNamedSource(JObject template,string templateHash,BNamedOperatorSource source,CombatDefinitionIndex policy,BCombatFormation formation,BBoardingEventsNodeSnapshot node,BCarriageBuffDefinitions originalBuffs){
   var wave=node?.OriginalSource;
   if(source==null||originalBuffs==null||policy==null||wave==null||formation==null||template==null||BDirectAttackInput.Digest(template)!=templateHash)throw new ArgumentException("b_named_native_source_missing");
   var packet=(JObject)template.DeepClone();var captured=source.Capture();var actors=new JArray();
   if(source.AccountId!=(string)wave.Capture()["origin"]["accountId"])throw new ArgumentException("b_named_native_wave_owner");
   if(!(packet["initialShields"] is JArray shields)||shields.Count!=0)throw new ArgumentException("b_named_native_shield_unbound");
   foreach(var slot in formation.Slots.Where(s=>s.OperatorId!=null)){
    var actor=source.Actor(slot.OperatorId);var stats=actor["permanentStats"] as JObject;if(stats==null)throw new ArgumentException("b_named_native_permanent_stats_missing:"+slot.OperatorId);
    if(actor["placement"].Type==JTokenType.Null||(string)actor["placement"]["carriageInstanceId"]!=slot.CarriageId||(int)actor["placement"]["physicalSlot"]!=Array.IndexOf(formation.Slots.ToArray(),slot))throw new ArgumentException("b_named_native_placement_missing_or_conflict");
    var entity=((JArray)packet["entities"]).Single(e=>(string)e["id"]==slot.OperatorId);
    foreach(string field in new[]{"attackSources","defenseSources","effects","weakening","penetration"})if(!(entity[field] is JArray values)||values.Count!=0)throw new ArgumentException("b_named_native_effect_unbound");
    foreach(string field in new[]{"equipment","guardStance","independent"})if(!(entity["reductions"]?[field] is JArray values)||values.Count!=0)throw new ArgumentException("b_named_native_effect_unbound");
    entity["maxHp"]=stats["maxHp"].DeepClone();entity["attack"]=stats["attack"].DeepClone();entity["defense"]=CombatExactValue.Create(BDirectAttackInput.Nat(stats,"defense"),1).Snapshot();entity["basicIntervalMicroseconds"]=checked(BDirectAttackInput.Nat(stats,"basicIntervalMs",1)*1000);
    actors.Add(new JObject{["operatorId"]=slot.OperatorId,["sourceHash"]=source.Hash,["permanentStats"]=stats.DeepClone(),["basicAttackBound"]=true,["interceptionDefinitionBound"]=true,["interceptionAcceptedTiming"]=JValue.CreateNull(),["effectiveStats"]=JValue.CreateNull(),["equipment"]=JValue.CreateNull(),["maximumDistanceMm"]=JValue.CreateNull(),["activeBound"]=false,["ultimateBound"]=false,["fullActorSupported"]=false});
   }
   packet["version"]=BDirectAttackInput.NamedLocalInputVersion;packet["ownedProfileHash"]="sha256:"+(string)captured["ownedProfileHash"];
   var evidence=(JArray)packet["sourceEvidence"];evidence.Add(new JObject{["sourceId"]="ORIGINAL_NAMED_PERMANENT_STATS_LOCAL_NOT_EFFECTIVE",["sha256"]=source.Hash});
   return new BNamedOperatorNativeAdapter(BDirectAttackInput.ReadNamedRuntime(packet,BDirectAttackInput.Digest(packet),formation,policy,wave,source),null,new JObject{["boundary"]="NAMED_SOURCE_PERMANENT_STATS_LOCAL_NOT_EFFECTIVE",["actors"]=actors,["sourceHash"]=source.Hash,["operationallyExecutable"]=false},node,source,originalBuffs.Hash);
  }
  public static BNamedOperatorNativeAdapter Read(JObject template,string templateHash,JObject owned,string ownedHash,CombatDefinitionIndex definition,BCombatFormation formation,BInterceptTimingSnapshot receipt){
   if(template==null||BDirectAttackInput.Digest(template)!=templateHash)throw new ArgumentException("b_named_actor_template_hash");
   if(owned==null||BDirectAttackInput.Digest(owned)!=ownedHash)throw new ArgumentException("b_named_actor_owned_source_missing_or_hash");
   if(definition==null||formation==null)throw new ArgumentException("b_named_actor_definition_or_selection_missing");
   if(receipt==null)throw new ArgumentException("b_named_actor_interception_source_missing");
   var profile=(JObject)owned.DeepClone();var packet=(JObject)template.DeepClone();var selected=formation.Slots.Where(s=>s.OperatorId=="op_kyle"||s.OperatorId=="op_sable").Select(s=>s.OperatorId).ToArray();
   if(selected.Length==0||!selected.Contains(receipt.OperatorId))throw new ArgumentException("b_named_actor_interception_holder_selection");
   if(!(packet["initialShields"] is JArray shields)||shields.Count!=0)throw new ArgumentException("b_named_actor_initial_shield_source_unbound");
   var states=new JArray();foreach(string actor in selected){
    if(!definition.OperatorDefinitionIds.Contains(actor))throw new ArgumentException("b_named_actor_definition_missing:"+actor);
    var rows=(profile["operators"] as JArray)?.Where(p=>(string)p["operatorId"]==actor).ToArray();if(rows==null||rows.Length!=1)throw new ArgumentException("b_named_actor_owned_selection_missing:"+actor);
    var row=rows[0];var stats=row["permanentStats"] as JObject;foreach(string field in new[]{"maxHp","attack","defense","basicIntervalMs"})if(stats?[field]==null||stats[field].Type==JTokenType.Null)throw new ArgumentException("b_named_actor_stat_source_missing:"+actor+":"+field);
    if((string)row["statInputs"]?["sourceOwner"]!=(string)profile["accountId"]||(string)row["statInputs"]?["targetDefinitionId"]!=actor||row["sources"]?["operator"]?["sourceId"]?.Type!=JTokenType.String)throw new ArgumentException("b_named_actor_stat_owner:"+actor);
    var entities=(packet["entities"] as JArray)?.Where(e=>(string)e["id"]==actor).ToArray();if(entities==null||entities.Length!=1)throw new ArgumentException("b_named_actor_entity_missing:"+actor);var entity=(JObject)entities[0];
    foreach(string field in new[]{"attackSources","defenseSources","effects","weakening","penetration"})if(!(entity[field] is JArray array)||array.Count!=0)throw new ArgumentException("b_named_actor_effect_source_unbound:"+actor+":"+field);
    foreach(string field in new[]{"equipment","guardStance","independent"})if(!(entity["reductions"]?[field] is JArray array)||array.Count!=0)throw new ArgumentException("b_named_actor_effect_source_unbound:"+actor+":"+field);
    entity["maxHp"]=stats["maxHp"].DeepClone();entity["attack"]=stats["attack"].DeepClone();entity["defense"]=CombatExactValue.Create(BDirectAttackInput.Nat(stats,"defense"),1).Snapshot();entity["basicIntervalMicroseconds"]=checked(BDirectAttackInput.Nat(stats,"basicIntervalMs",1)*1000);
    states.Add(new JObject{["operatorId"]=actor,["ownedProfileHash"]=ownedHash,["definitionHash"]=definition.ContentHash,["statSource"]=row["sources"]["operator"].DeepClone(),["permanentStats"]=stats.DeepClone(),["basicAttackBound"]=true,["interceptionSourceBound"]=actor==receipt.OperatorId,["interceptionAbilityId"]=actor==receipt.OperatorId?receipt.Capture()["timing"]["abilityId"].DeepClone():JValue.CreateNull(),["activeBound"]=false,["ultimateBound"]=false,["operatingMovementBound"]=false,["effectiveStatProfileBound"]=false,["fullActorSupported"]=false});
   }
   packet["ownedProfileHash"]=ownedHash;var evidence=packet["sourceEvidence"] as JArray;if(evidence==null)throw new ArgumentException("b_named_actor_source_evidence_missing");if(evidence.Count>=16)throw new ArgumentException("b_named_actor_source_evidence_capacity");evidence.Add(new JObject{["sourceId"]="NAMED_PERMANENT_STATS_FROM_ORIGINAL_OWNED_INTAKE_NOT_ADMITTED_PROFILE",["sha256"]=ownedHash.Substring(7)});
   var input=BDirectAttackInput.ReadGunnerInterceptLocalRuntime(packet,BDirectAttackInput.Digest(packet),formation,profile,ownedHash,definition,receipt.OriginalSource.OriginalSource);
   if(input.StepTimeSpanTicks!=checked(receipt.StepMicroseconds*10))throw new ArgumentException("b_named_actor_native_step_mismatch");
   return new BNamedOperatorNativeAdapter(input,receipt,new JObject{["boundary"]="EXISTING_OWNED_INTAKE_TO_LOCAL_NATIVE_PERMANENT_STATS_ONLY",["actors"]=states,["timingSourceHash"]=receipt.Hash,["operationallyExecutable"]=false});
  }
  public BDirectAttackBasicLoop CreateRuntime(BDirectAttackBasicLoopBinding binding,BOrdinaryGeometryBinding geometry,int capacity){if(binding==null||binding.Composition.Attack.OriginalHash!=Input.OriginalHash)throw new ArgumentException("b_named_actor_native_binding_mismatch");if(namedSource!=null&&(!JToken.DeepEquals(binding.Composition.Capture()["departure"],namedSource.Capture()["refit"]?["departure"])||binding.Composition.Definitions.Hash!=buffHash))throw new ArgumentException("b_named_native_original_refit_conflict");return BDirectAttackBasicLoop.CreateOrdinaryGeometryRuntime(binding,boarding,geometry,capacity,timing!=null);}
 }
}
