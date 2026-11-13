using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // A captured supplier packet is restored against independently retained bytes and hash.
 // Neither a caller-provided digest nor current account data can authorize a replacement.
 public sealed class BNamedOperatorSource {
  public const string Version="combat.b-named-operator-source.v1.20261004";
  public const string DefinitionVersion="combat.b-named-operator-definition.v1.20261004";
  readonly JObject original;
  public string Hash {get;}
  public string AccountId=>(string)original["accountId"];
  public JObject Capture()=>(JObject)original.DeepClone();
  public JObject Actor(string id){var actor=((JArray)original["actors"]).SingleOrDefault(a=>(string)a["operatorId"]==id);if(actor==null)throw new ArgumentException("b_named_actor_not_supplied:"+id);return (JObject)actor.DeepClone();}
  BNamedOperatorSource(JObject packet,string hash){original=(JObject)packet.DeepClone();Hash=hash;}
  public static BNamedOperatorSource Read(JObject input,JObject trustedOriginal,string independentlyPinnedHash){
   if(input==null||trustedOriginal==null||Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(input))>512*1024)throw new ArgumentException("b_named_source_capacity_or_missing");
   BDirectAttackInput.Fields(input,"version","accountId","definition","definitionHash","ownedProfile","ownedProfileHash","formationRevision","loadoutRevision","ownedSourceHash","refit","actors","sourceStatus","runtimeSupported","canStart","packetHash");
   var body=(JObject)input.DeepClone();body.Remove("packetHash");
   if(!System.Text.RegularExpressions.Regex.IsMatch(independentlyPinnedHash??"","^[a-f0-9]{64}$")||
    (string)input["packetHash"]!=independentlyPinnedHash||(string)trustedOriginal["packetHash"]!=independentlyPinnedHash||BDirectAttackInput.Digest(body)!="sha256:"+independentlyPinnedHash||!JToken.DeepEquals(input,trustedOriginal))throw new ArgumentException("b_named_source_original_conflict");
   if((string)input["version"]!=Version||(string)input["definition"]?["version"]!=DefinitionVersion||
    (string)input["sourceStatus"]!="VERIFIED_OWNED_SELECTION_READ_ONLY_CANDIDATE_NOT_START"||input["runtimeSupported"]?.Type!=JTokenType.Boolean||(bool)input["runtimeSupported"]||input["canStart"]?.Type!=JTokenType.Boolean||(bool)input["canStart"])throw new ArgumentException("b_named_source_contract");
   BDirectAttackInput.Text(input,"accountId");BDirectAttackInput.Nat(input,"formationRevision",1);BDirectAttackInput.Nat(input,"loadoutRevision",1);
   if(BDirectAttackInput.Digest(input["ownedProfile"])!="sha256:"+(string)input["ownedProfileHash"]||(string)input["ownedProfile"]?["accountId"]!=(string)input["accountId"]||(string)input["definition"]?["definitionHash"]!=(string)input["definitionHash"])throw new ArgumentException("b_named_source_owned_or_definition");
   if((string)input["definitionHash"]!="08c88c65406c55a00847fcd7042ec90fae4d834c725d105ba6e574f5d9b7ecbd")throw new ArgumentException("b_named_source_definition_pin");
   var definition=(JObject)input["definition"].DeepClone();definition.Remove("definitionHash");if(BDirectAttackInput.Digest(definition)!="sha256:"+(string)input["definitionHash"])throw new ArgumentException("b_named_source_definition_digest");
   var actors=input["actors"] as JArray;if(actors==null||actors.Count<1||actors.Count>2||actors.Select(a=>(string)a["operatorId"]).Distinct().Count()!=actors.Count)throw new ArgumentException("b_named_source_roster");
   foreach(JObject a in actors){
    BDirectAttackInput.Fields(a,"actorId","operatorId","profileVersion","definitionVersion","definitionHash","baseDefinition","owned","permanentStats","effectiveStats","equipment","placement","bcb","basic","interception","skills","capabilities","missing");
    string id=(string)a["operatorId"];if((id!="op_kyle"&&id!="op_sable")||(string)a["actorId"]!=id||(string)a["profileVersion"]!="combat.b-"+id.Replace('_','-')+"-owned-base.v1.20261004"||(string)a["definitionVersion"]!=DefinitionVersion||(string)a["definitionHash"]!=(string)input["definitionHash"])throw new ArgumentException("b_named_source_actor_identity");
    BDirectAttackInput.Fields(a["baseDefinition"] as JObject,"hp","attack","defense","basicIntervalMs");
    BDirectAttackInput.Fields(a["owned"] as JObject,"level","promotion","skillTraining","sources","statInputs","skillMagnitude");
    BDirectAttackInput.Fields(a["basic"] as JObject,"physicalAdjacentCars","targetCategory","geometryRequired","maximumDistanceMm","intervalMs","attackCoefficient","statStage");
    BDirectAttackInput.Fields(a["interception"] as JObject,"definition","originalDefinitionHash","acceptedTiming","originalHasteSources","owner");
    BDirectAttackInput.Fields(a["capabilities"] as JObject,"basic","intercept","active","ultimate","movement");
    foreach(string action in new[]{"basic","intercept","active","ultimate","movement"}){var cap=a["capabilities"][action] as JObject;if(action=="basic"||action=="intercept")BDirectAttackInput.Fields(cap,"definitionAvailable","nativeSupported","ownedSelected");else BDirectAttackInput.Fields(cap,"definitionAvailable","nativeSupported");if(cap["definitionAvailable"]?.Type!=JTokenType.Boolean||cap["nativeSupported"]?.Type!=JTokenType.Boolean||(bool)cap["nativeSupported"]||(action=="basic"||action=="intercept")&&(cap["ownedSelected"]?.Type!=JTokenType.Boolean||!(bool)cap["ownedSelected"]))throw new ArgumentException("b_named_source_capability");}
    var approved=((JArray)input["definition"]["operators"]).Single(o=>(string)o["operatorId"]==id);
    if(!JToken.DeepEquals(a["baseDefinition"],approved["base"])||!JToken.DeepEquals(a["skills"],approved["skills"])||(long)a["basic"]["intervalMs"]!=(long)approved["base"]["basicIntervalMs"]||(string)a["interception"]["owner"]!="S23_ORIGINAL_ACCEPTED_INTERCEPTION_SOURCE_OWNER")throw new ArgumentException("b_named_source_approved_actor_conflict");
    foreach(string key in new[]{"equipment","effectiveStats"})if(a[key]?.Type!=JTokenType.Null)throw new ArgumentException("b_named_source_unsupported:"+key);
    foreach(string key in new[]{"acceptedTiming","originalHasteSources"})if(a["interception"]?[key]?.Type!=JTokenType.Null)throw new ArgumentException("b_named_source_unsupported:"+key);
    if(a["basic"]?["maximumDistanceMm"]?.Type!=JTokenType.Null||(int)a["basic"]["physicalAdjacentCars"]!=0)throw new ArgumentException("b_named_source_geometry");
    if((string)a["owned"]?["statInputs"]?["sourceOwner"]!=(string)input["accountId"]||(string)a["owned"]?["statInputs"]?["targetDefinitionId"]!=id)throw new ArgumentException("b_named_source_stat_owner");
    if(a["placement"].Type!=JTokenType.Null){BDirectAttackInput.Fields(a["placement"] as JObject,"physicalSlot","carriageInstanceId","carriageDefinitionId");BDirectAttackInput.Fields(a["bcb"] as JObject,"type","rank","rankRevision","selectionRevision","sourceStatus");var departure=input["refit"]?["departure"];var car=(departure?["cars"] as JArray)?.SingleOrDefault(c=>(string)c["instanceId"]==(string)a["placement"]["carriageInstanceId"]);if(car==null||(string)car["occupant"]?["operatorId"]!=id||!JToken.DeepEquals(car["slot"],a["placement"]["physicalSlot"])||!JToken.DeepEquals(car["carriageId"],a["placement"]["carriageDefinitionId"])||!JToken.DeepEquals(car["rank"],a["bcb"]["rank"])||!JToken.DeepEquals(car["type"],a["bcb"]["type"])||!JToken.DeepEquals(departure["rankRevision"],a["bcb"]["rankRevision"])||!JToken.DeepEquals(departure["selectionRevision"],a["bcb"]["selectionRevision"]))throw new ArgumentException("b_named_source_refit_conflict");}
    if(a["permanentStats"].Type!=JTokenType.Null){var s=(JObject)a["permanentStats"];BDirectAttackInput.Fields(s,"maxHp","attack","defense","basicIntervalMs");BDirectAttackInput.Nat(s,"maxHp",1,int.MaxValue);BDirectAttackInput.Nat(s,"defense");BDirectAttackInput.Nat(s,"basicIntervalMs",1);CombatExactValue.From(s["attack"] as JObject);}
   }
   return new BNamedOperatorSource(input,independentlyPinnedHash);
  }
 }
}
