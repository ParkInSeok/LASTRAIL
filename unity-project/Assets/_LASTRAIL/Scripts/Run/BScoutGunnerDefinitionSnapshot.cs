using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using static LastRail.Run.RunWaveManifestReader;
namespace LastRail.Run {
 public sealed class BScoutGunnerTypeDefinition {
  readonly JObject source;
  public string DefinitionId {get;}public string TypeHash {get;}public CombatExactValue RawAction {get;}public CombatExactValue DamageCoefficient {get;}public CombatExactValue RelativeEhpWeight {get;}public int BasicPeriodMilliseconds {get;}public int Defense {get;}public int PreparationMilliseconds {get;}public int RecoveryMilliseconds {get;}public int MaximumDistanceMillimeters {get;}public int MovementMillimetersPerSecond {get;}
  internal BScoutGunnerTypeDefinition(JObject p){source=(JObject)p.DeepClone();DefinitionId=(string)p["definitionId"];TypeHash=(string)p["typeHash"];RawAction=CombatExactValue.From((JObject)p["rawAction"]["value"]);DamageCoefficient=CombatExactValue.From((JObject)p["damageCoefficient"]);RelativeEhpWeight=CombatExactValue.From((JObject)p["relativeEhpWeight"]);BasicPeriodMilliseconds=(int)p["basicPeriodMs"];Defense=(int)p["defense"];PreparationMilliseconds=(int)p["preparationMs"];RecoveryMilliseconds=(int)p["recoveryMs"];MaximumDistanceMillimeters=(int)p["spatial"]["maximumDistanceMm"];MovementMillimetersPerSecond=(int)p["spatial"]["movementSpeedMmPerSecond"];}
  public JObject Capture()=>(JObject)source.DeepClone();
 }
 public sealed class BScoutGunnerDefinitionSnapshot {
  public const string Version="combat.b-scout-gunner-source.v1.20261003";
  public const string DefinitionVersion="combat.b-scout-gunner-definition.v1.20261003";
  // Exact immutable approved S21 definition. New definitions require a new reviewed version.
  public const string ApprovedDefinitionHash="f6afc8b589f4856def4016f213b34061f7767820929238f33bb68645b28193b5";
  public const string ApprovedPacketHash="ec622db1e6d258eb239aa912bf9dea704de7193a7769c0d0cd9804af76d7990d";
  readonly JObject packet;public IReadOnlyList<BScoutGunnerTypeDefinition> Types {get;}public bool DefinitionAvailable=>true;public bool RuntimeSupported=>false;public bool CanStart=>false;
  BScoutGunnerDefinitionSnapshot(JObject p){packet=(JObject)p.DeepClone();Types=Array.AsReadOnly(((JArray)p["definition"]["types"]).Select(t=>new BScoutGunnerTypeDefinition((JObject)t)).ToArray());}
  public JObject Capture()=>(JObject)packet.DeepClone();
  internal static void ValidateDefinition(JObject d){Need((string)d["version"]==DefinitionVersion&&(string)d["definitionHash"]==ApprovedDefinitionHash);SelfHash(d,"definitionHash");var types=Rows(d["types"],2,2);Need((string)types[0]["definitionId"]=="enemy_raider_scout"&&(string)types[1]["definitionId"]=="enemy_spike_cart_gunner");foreach(JObject t in types)SelfHash(t,"typeHash");}
  public static BScoutGunnerDefinitionSnapshot Read(JObject input,string independentlyExpectedPacketHash){Need(input!=null);var p=(JObject)input.DeepClone();Fields(p,"version","definition","definitionAvailable","runtimeSupported","canStart","packetHash");Need(Hash(new JValue(independentlyExpectedPacketHash))==ApprovedPacketHash&&(string)p["packetHash"]==independentlyExpectedPacketHash&&(string)p["version"]==Version);SelfHash(p,"packetHash");ValidateDefinition(Obj(p["definition"]));Need(p["definitionAvailable"].Type==JTokenType.Boolean&&(bool)p["definitionAvailable"]&&p["runtimeSupported"].Type==JTokenType.Boolean&&!(bool)p["runtimeSupported"]&&p["canStart"].Type==JTokenType.Boolean&&!(bool)p["canStart"]);return new BScoutGunnerDefinitionSnapshot(p);}
  public static BScoutGunnerDefinitionSnapshot ReadJson(string json,string independentlyExpectedPacketHash)=>Read(Parse(json),independentlyExpectedPacketHash);
 }
}
