using Newtonsoft.Json.Linq;
using static LastRail.Run.RunWaveManifestReader;
namespace LastRail.Run {
 public sealed class BInterceptDefinitionSnapshot {
  public const string Version="combat.b-scout-gunner-source.v3.20261004",DefinitionVersion="combat.b-scout-gunner-definition.v3.20261004";
  public const string ApprovedDefinitionHash="40224481e914e78ef103be378300600942588d50e121a89cf71cedf9edd94970",ApprovedPacketHash="ae963c15db6cc7ab33af417fe014082769aa0d9943d1b5fb1a15dba32acbcf8c";
  readonly JObject packet;
  public bool RuntimeSupported=>false;public bool CanStart=>false;
  BInterceptDefinitionSnapshot(JObject p){packet=(JObject)p.DeepClone();}
  internal static void Validate(JObject d){BBoardingEventsDefinitionSnapshot.Bounded(d);Fields(d,"version","approval","originalDefinition","originalDefinitionHash","abilities","registrySource","timingPolicy","actionPolicy","definitionAvailable","timingDefinitionAvailable","runtimeSupported","canStart","definitionHash");Need((string)d["version"]==DefinitionVersion&&(string)d["definitionHash"]==ApprovedDefinitionHash&&(string)d["originalDefinitionHash"]==BBoardingEventsDefinitionSnapshot.ApprovedDefinitionHash);SelfHash(d,"definitionHash");BBoardingEventsDefinitionSnapshot.ValidateDefinition(Obj(d["originalDefinition"]));BBoardingEventsDefinitionSnapshot.Flags(d);Need(d["timingDefinitionAvailable"]?.Type==JTokenType.Boolean&&(bool)d["timingDefinitionAvailable"]);foreach(var a in Rows(d["abilities"],2,2))SelfHash(Obj(a),"abilityHash");}
  public JObject Capture()=>(JObject)packet.DeepClone();
  public static BInterceptDefinitionSnapshot Read(JObject input,string independentHash){BBoardingEventsDefinitionSnapshot.Bounded(input);var p=(JObject)input.DeepClone();Fields(p,"version","definition","definitionAvailable","runtimeSupported","canStart","packetHash");Need(Hash(new JValue(independentHash))==ApprovedPacketHash&&(string)p["version"]==Version&&(string)p["packetHash"]==independentHash);SelfHash(p,"packetHash");Validate(Obj(p["definition"]));BBoardingEventsDefinitionSnapshot.Flags(p);return new BInterceptDefinitionSnapshot(p);}
  public static BInterceptDefinitionSnapshot ReadJson(string json,string hash)=>Read(Parse(json),hash);
 }
}
