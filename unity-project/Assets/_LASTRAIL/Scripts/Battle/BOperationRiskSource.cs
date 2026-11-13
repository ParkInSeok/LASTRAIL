using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Verified immutable raw-source mapper. It neither rewrites actor ATK nor authorizes mission/spawn/AI.
 public sealed class BOperationRiskSource {
  public const string Version="combat.b-operation-risk.v1.20261003";
  const string DefinitionHash="sha256:7889e8553ee2a64b383294e8f30edf87f78691096935cc74abf0b34b6652a5ce";
  const string SchemasHash="sha256:797f899cd53e54b4a590ed6740de18e72887799470465d2ade04f640e06abe88";
  readonly JObject packet;
  BOperationRiskSource(JObject value){packet=(JObject)value.DeepClone();}
  public JObject Capture()=>(JObject)packet.DeepClone();
  public JObject Binding=>(JObject)packet["binding"].DeepClone();
  public JObject ScaledHp=>(JObject)packet["scaled"]["hp"].DeepClone();
  public JObject ScaledDamage=>(JObject)packet["scaled"]["damage"].DeepClone();
  public bool MissionSupported=>false;public bool HpSpawnConversionSupported=>false;
  static void Need(bool condition)=>BuffWire.Need(condition,"b_operation_risk_source_conflict");
  static void Id(JToken token){string id=BuffWire.Text(token);Need(id.Length<=128);}
  static void Null(JToken token)=>Need(token!=null&&token.Type==JTokenType.Null);
  static void ValidateBinding(JObject b){BuffWire.Fields(b,"version","operationId","runId","contextId","risk","progress","rawSourceHash","revision","provenance");Need((string)b["version"]==Version&&(string)b["provenance"]=="BOUNDED_LOCAL_EXPLICIT_NOT_MISSION_START"&&BuffWire.Number(b["revision"])==0&&new[]{"I","II","III","IV","V"}.Contains((string)b["risk"])&&new[]{"EARLY","MIDDLE","LATE"}.Contains((string)b["progress"]));foreach(var key in new[]{"operationId","runId","contextId"})Id(b[key]);Need(System.Text.RegularExpressions.Regex.IsMatch(BuffWire.Text(b["rawSourceHash"]),"^sha256:[a-f0-9]{64}$"));}
  static CombatExactValue Factor(JObject packet,string group,string id,string value){var row=((JArray)packet["definition"][group]).Single(r=>(string)r["id"]==id);return CombatExactValue.From(BuffWire.Object(row[value]));}
  static JObject Scale(JObject p,bool hp){var raw=BuffWire.Object(p["original"][hp?"hp":"damage"]);var exact=CombatExactValue.From(BuffWire.Object(raw["value"])).Multiply(Factor(p,"risks",(string)p["binding"]["risk"],hp?"hp":"damage"));if(!hp)exact=exact.Multiply(Factor(p,"progress",(string)p["binding"]["progress"],"damage"));var result=new JObject{["sourceId"]=raw["sourceId"].DeepClone(),["kind"]=raw["kind"].DeepClone(),["exact"]=exact.Snapshot(),["riskAppliedVersion"]=Version,["progressAppliedVersion"]=hp?JValue.CreateNull():new JValue(Version)};if(hp)result["spawnConversion"]="NOT_PERFORMED";return result;}
  public static BOperationRiskSource Read(JObject input,JObject independentlyTrustedBinding,string independentlyTrustedPacketHash){
   Need(input!=null&&independentlyTrustedBinding!=null);
   var p=(JObject)input.DeepClone();var b=(JObject)independentlyTrustedBinding.DeepClone();ValidateBinding(b);BuffWire.Fields(p,"version","definition","binding","original","scaled","schemas","support","ownership","missingMissionFields","startReady","packetHash");var unsigned=(JObject)p.DeepClone();unsigned.Remove("packetHash");Need(BDirectAttackInput.Digest(unsigned)==independentlyTrustedPacketHash&&(string)p["packetHash"]==independentlyTrustedPacketHash&&(string)p["version"]==Version);ValidateBinding(BuffWire.Object(p["binding"]));Need(JToken.DeepEquals(p["binding"],b));
   var definition=BuffWire.Object(p["definition"]);var payload=(JObject)definition.DeepClone();payload.Remove("definitionHash");Need((string)definition["definitionHash"]==DefinitionHash&&BDirectAttackInput.Digest(payload)==DefinitionHash&&BDirectAttackInput.Digest(p["schemas"])==SchemasHash);
   var original=BuffWire.Object(p["original"]);BuffWire.Fields(original,"hp","damage");var hp=BuffWire.Object(original["hp"]);var damage=BuffWire.Object(original["damage"]);BuffWire.Fields(hp,"sourceId","kind","value","riskAppliedVersion");BuffWire.Fields(damage,"sourceId","kind","value","riskAppliedVersion","progressAppliedVersion");Id(hp["sourceId"]);Id(damage["sourceId"]);Need(new[]{"BASE_HP","EHP_BUDGET"}.Contains((string)hp["kind"])&&(string)damage["kind"]=="RAW_ACTION_DAMAGE");Null(hp["riskAppliedVersion"]);Null(damage["riskAppliedVersion"]);Null(damage["progressAppliedVersion"]);CombatExactValue.From(BuffWire.Object(hp["value"]));CombatExactValue.From(BuffWire.Object(damage["value"]));Need(BDirectAttackInput.Digest(original)==(string)b["rawSourceHash"]);
   var scaled=BuffWire.Object(p["scaled"]);BuffWire.Fields(scaled,"hp","damage");Need(JToken.DeepEquals(scaled["hp"],Scale(p,true))&&JToken.DeepEquals(scaled["damage"],Scale(p,false)));
   var support=new JObject{["sourceBound"]=true,["localProjection"]=true,["missionStart"]=false,["enemyAuto"]=false,["hpSpawnConversion"]=false};var missing=new JArray("AUTHENTICATED_MISSION_DEPARTURE_RUN_ID","ORIGINAL_OPERATION_PROFILE_VERSION_HASH","SERVER_SELECTED_UNLOCK_ELIGIBLE_RISK","PINNED_UNSCALED_HP_OR_EHP_AND_RAW_ACTION_PROFILE","ACTUAL_PROGRESS_BAND_AND_SOURCE_APPLIED_FLAGS");Need(JToken.DeepEquals(p["support"],support)&&JToken.DeepEquals(p["missingMissionFields"],missing)&&(string)p["ownership"]=="TRUSTED_LOCAL_RAW_SOURCE_ONLY_UNITY_SOLO_HP_OWNER"&&!BuffWire.Flag(p["startReady"]));return new BOperationRiskSource(p);
  }
  // Final BASE_HP allocation magnitude only. This creates no entity, and EHP conversion requires its original profile.
  public int BaseHpAllocationMagnitude(){Need((string)packet["original"]["hp"]["kind"]=="BASE_HP");int magnitude=checked((int)CombatExactValue.From(BuffWire.Object(ScaledHp["exact"])).Floor());Need(magnitude>0);return magnitude;}
  public CombatNumericDefenseResult ResolveOriginalAction(JObject originalAction){
   Need(originalAction!=null);
   var action=(JObject)originalAction.DeepClone();BuffWire.Fields(action,"sourceId","kind","pipeline","riskAppliedVersion","progressAppliedVersion");Need((string)action["sourceId"]==(string)packet["original"]["damage"]["sourceId"]&&(string)action["kind"]=="RAW_ACTION_DAMAGE");Null(action["riskAppliedVersion"]);Null(action["progressAppliedVersion"]);var pipeline=BuffWire.Object(action["pipeline"]);Need(JToken.DeepEquals(CombatExactValue.From(BuffWire.Object(pipeline["base"])).Snapshot(),CombatExactValue.From(BuffWire.Object(packet["original"]["damage"]["value"])).Snapshot()));pipeline["base"]=ScaledDamage["exact"].DeepClone();return CombatNumericDefenseBudget.Resolve(pipeline);
  }
 }
}
