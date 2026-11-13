using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Captured applicability/provenance. This class never calculates runtime effects.
 public sealed class BNamedEffectiveSource {
  public const string Version="combat.b-named-effective-source.v1.20261004";
  readonly JObject original;
  public string Hash {get;}
  public string AccountId=>(string)original["accountId"];
  public string EffectSourceHash=>(string)original["effectSourceHash"];
  public string ProviderStatus=>(string)original["refitProviderStatus"];
  public string CommanderStatus=>(string)original["applicability"]["commander"]["status"];
  readonly BNamedOperatorSource named;
  BNamedEffectiveSource(JObject packet,string hash,BNamedOperatorSource source){original=(JObject)packet.DeepClone();Hash=hash;named=source;}
  public JObject Capture()=>(JObject)original.DeepClone();
  public BNamedOperatorSource RequirePredepartureBase(){
   if(CommanderStatus!="VERIFIED_NO_ACTIVE_EFFECTS")throw new ArgumentException("b_effective_commander_execution_unavailable");
   if(((JArray)original["actors"]).Any(a=>a["resolvedPredepartureStats"].Type==JTokenType.Null))throw new ArgumentException("b_effective_owned_base_unavailable");
   return named;
  }
  public static BNamedEffectiveSource Read(JObject packet,JObject trustedOriginal,string independentlyPinnedHash,string expectedAccount,string expectedEffectSourceHash=null){
   if(packet==null||trustedOriginal==null||Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(packet))>768*1024)throw new ArgumentException("b_effective_source_capacity_or_missing");
   BDirectAttackInput.Fields(packet,"version","accountId","namedSource","originalNamedPacketHash","applicability","effectSourceHash","refitProviderStatus","actors","runtimeSupported","canStart","packetHash");
   var body=(JObject)packet.DeepClone();body.Remove("packetHash");
   if(!System.Text.RegularExpressions.Regex.IsMatch(independentlyPinnedHash??"","^[a-f0-9]{64}$")||(string)packet["packetHash"]!=independentlyPinnedHash||(string)trustedOriginal["packetHash"]!=independentlyPinnedHash||BDirectAttackInput.Digest(body)!="sha256:"+independentlyPinnedHash||!JToken.DeepEquals(packet,trustedOriginal))throw new ArgumentException("b_effective_source_original_conflict");
   if((string)packet["version"]!=Version||(string)packet["accountId"]!=expectedAccount||string.IsNullOrWhiteSpace(expectedAccount)||packet["runtimeSupported"]?.Type!=JTokenType.Boolean||(bool)packet["runtimeSupported"]||packet["canStart"]?.Type!=JTokenType.Boolean||(bool)packet["canStart"])throw new ArgumentException("b_effective_source_version_or_owner");
   var named=BNamedOperatorSource.Read(packet["namedSource"] as JObject,trustedOriginal["namedSource"] as JObject,(string)packet["originalNamedPacketHash"]);
   if(named.AccountId!=expectedAccount)throw new ArgumentException("b_effective_named_owner");
   string provider=(string)packet["refitProviderStatus"];
   if(provider!="ORIGINAL_PERSISTED_PROVIDER_BOUND"&&provider!="OPERATING_PROVIDER_UNAVAILABLE"||provider=="OPERATING_PROVIDER_UNAVAILABLE"&&named.Capture()["refit"].Type!=JTokenType.Null||provider=="ORIGINAL_PERSISTED_PROVIDER_BOUND"&&named.Capture()["refit"].Type!=JTokenType.Object)throw new ArgumentException("b_effective_provider_status");
   var applicability=packet["applicability"] as JObject;BDirectAttackInput.Fields(applicability,"equipment","research","commander");
   if(BDirectAttackInput.Digest(applicability)!="sha256:"+(string)packet["effectSourceHash"]||expectedEffectSourceHash!=null&&(string)packet["effectSourceHash"]!=expectedEffectSourceHash)throw new ArgumentException("b_effective_effect_hash");
   var equipment=applicability["equipment"] as JObject;BDirectAttackInput.Fields(equipment,"lifecycle","owner","source","status","predepartureRequired","items","verifiedEmptyRunInventory");
   if((string)equipment["lifecycle"]!="RUN_OWNED"||(string)equipment["status"]!="NOT_APPLICABLE_BEFORE_RUN"||equipment["items"].Type!=JTokenType.Null||equipment["verifiedEmptyRunInventory"].Type!=JTokenType.Boolean||(bool)equipment["verifiedEmptyRunInventory"]||equipment["predepartureRequired"].Type!=JTokenType.Boolean||(bool)equipment["predepartureRequired"]||(string)equipment["owner"]!="EquipmentInventoryStore/EquipmentEffectStateStore")throw new ArgumentException("b_effective_equipment_lifecycle");
   var research=applicability["research"] as JObject;BDirectAttackInput.Fields(research,"source","status","predepartureRequired","modifiers");
   if((string)research["status"]!="NO_APPROVED_ACTIVE_NODE_CATALOG"||research["modifiers"].Type!=JTokenType.Null||research["predepartureRequired"].Type!=JTokenType.Boolean||(bool)research["predepartureRequired"])throw new ArgumentException("b_effective_research_lifecycle");
   var commander=applicability["commander"] as JObject;BDirectAttackInput.Fields(commander,"owner","status","errorCode","authority","snapshot","sourceHash","appliedToBase");
   string status=(string)commander["status"];if((status!="VERIFIED_NO_ACTIVE_EFFECTS"&&status!="UNSUPPORTED_SELECTED_EFFECTS")||(string)commander["owner"]!="readCommanderDeparture/readCommanderAuthority"||commander["appliedToBase"].Type!=JTokenType.Boolean||(bool)commander["appliedToBase"]||BDirectAttackInput.Digest(commander["authority"])!="sha256:"+(string)commander["sourceHash"])throw new ArgumentException("b_effective_commander_authority");
   if(status=="VERIFIED_NO_ACTIVE_EFFECTS"&&(commander["errorCode"].Type!=JTokenType.Null||commander["snapshot"].Type!=JTokenType.Null&&(!(commander["snapshot"]?["ranks"] is JObject ranks)||ranks.Count!=0))||status=="UNSUPPORTED_SELECTED_EFFECTS"&&(commander["errorCode"].Type!=JTokenType.String||commander["snapshot"].Type!=JTokenType.Null))throw new ArgumentException("b_effective_commander_status");
   var actors=packet["actors"] as JArray;if(actors==null||actors.Count<1||actors.Count>2||actors.Select(a=>(string)a["actorId"]).Distinct().Count()!=actors.Count||!actors.Select(a=>(string)a["actorId"]).OrderBy(id=>id).SequenceEqual(((JArray)named.Capture()["actors"]).Select(a=>(string)a["actorId"]).OrderBy(id=>id)))throw new ArgumentException("b_effective_roster");
   foreach(JObject actor in actors){
    BDirectAttackInput.Fields(actor,"actorId","originalActorHash","resolvedPredepartureStats","effectiveStats","statStage","modifierApplications","missing");
    var source=named.Actor((string)actor["actorId"]);
    if(BDirectAttackInput.Digest(source)!="sha256:"+(string)actor["originalActorHash"]||actor["effectiveStats"].Type!=JTokenType.Null||(string)actor["statStage"]!="PREDEPARTURE_OWNED_BASE_NO_APPLICABLE_RUN_EFFECTS_NOT_RUNTIME"||BDirectAttackInput.Nat(actor,"modifierApplications")!=0||!(actor["missing"] is JArray missing)||missing.Count>10||missing.Any(m=>m.Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)m)))throw new ArgumentException("b_effective_actor_source_or_execution");
    var expected=status=="VERIFIED_NO_ACTIVE_EFFECTS"?source["permanentStats"]:JValue.CreateNull();if(!JToken.DeepEquals(actor["resolvedPredepartureStats"],expected))throw new ArgumentException("b_effective_predeparture_base_conflict");
   }
   return new BNamedEffectiveSource(packet,independentlyPinnedHash,named);
  }
 }
}
