using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 public sealed class RunWaveRawAction {
  public string SourceId {get;} public CombatExactValue Value {get;}
  internal RunWaveRawAction(JObject p){SourceId=(string)p["sourceId"];Value=CombatExactValue.From((JObject)p["value"]);}
 }
 public sealed class RunWaveEnemyTemplate {
  readonly JObject definition;
  public string TemplateId {get;}public string Version {get;}public string DefinitionId {get;}public string TemplateHash {get;}public string HpSourceId {get;}public string HpKind {get;}public CombatExactValue Hp {get;}
  public IReadOnlyList<RunWaveRawAction> Actions {get;}
  public JObject CaptureDefinition()=>(JObject)definition.DeepClone();
  internal RunWaveEnemyTemplate(JObject p){TemplateId=(string)p["templateId"];Version=(string)p["version"];DefinitionId=(string)p["definitionId"];TemplateHash=(string)p["templateHash"];definition=(JObject)p["definition"].DeepClone();HpSourceId=(string)p["hp"]["sourceId"];HpKind=(string)p["hp"]["kind"];Hp=CombatExactValue.From((JObject)p["hp"]["value"]);Actions=Array.AsReadOnly(((JArray)p["actions"]).Select(a=>new RunWaveRawAction((JObject)a)).ToArray());}
 }
 public sealed class RunWaveEnemyInstance {
  public string InstanceId {get;}public long Ordinal {get;}public string TemplateId {get;}public string TemplateVersion {get;}public string TemplateHash {get;}public long EntryGroup {get;}public long EntryNotBeforeMilliseconds {get;}
  internal RunWaveEnemyInstance(JObject p){InstanceId=(string)p["instanceId"];Ordinal=(long)p["ordinal"];TemplateId=(string)p["templateRef"]["templateId"];TemplateVersion=(string)p["templateRef"]["version"];TemplateHash=(string)p["templateRef"]["templateHash"];EntryGroup=(long)p["entryGroup"];EntryNotBeforeMilliseconds=(long)p["entryNotBeforeMs"];}
 }
 public sealed class RunWaveManifestWave {
  public string NodeId {get;}public int VisitDepth {get;}public int WaveOrdinal {get;}public IReadOnlyList<RunWaveEnemyInstance> Instances {get;}
  internal RunWaveManifestWave(JObject p){NodeId=(string)p["nodeId"];VisitDepth=(int)p["visitDepth"];WaveOrdinal=(int)p["waveOrdinal"];Instances=Array.AsReadOnly(((JArray)p["instances"]).Select(i=>new RunWaveEnemyInstance((JObject)i)).ToArray());}
 }
 // Original immutable metadata only. It owns neither runtime HP, a spawn clock nor admission authority.
 public sealed class RunWaveManifestSnapshot {
  public const string Version="combat.b-wave-manifest.v1.20261003";
  readonly JObject packet;
  public string RunId=>(string)packet["runId"];public string RunStatus=>(string)packet["runStatus"];public bool HasManifest=>(bool)packet["manifestPinned"];public bool CanStart=>false;public bool FullCombatSupported=>false;
  public IReadOnlyList<RunWaveEnemyTemplate> Templates {get;}public IReadOnlyList<RunWaveManifestWave> Waves {get;}
  internal RunWaveManifestSnapshot(JObject verified){packet=(JObject)verified.DeepClone();Templates=Array.AsReadOnly(HasManifest?((JArray)packet["manifest"]["templates"]).Select(t=>new RunWaveEnemyTemplate((JObject)t)).ToArray():Array.Empty<RunWaveEnemyTemplate>());Waves=Array.AsReadOnly(HasManifest?((JArray)packet["manifest"]["waves"]).Select(w=>new RunWaveManifestWave((JObject)w)).ToArray():Array.Empty<RunWaveManifestWave>());}
  public JObject Capture()=>(JObject)packet.DeepClone();
  internal JObject Frozen=>new JObject{["accountId"]=packet["accountId"].DeepClone(),["runId"]=packet["runId"].DeepClone(),["startSnapshotHash"]=packet["startSnapshotHash"].DeepClone(),["parentPinHash"]=packet["parentPinHash"].DeepClone(),["manifestHash"]=HasManifest?packet["manifest"]["manifestHash"].DeepClone():JValue.CreateNull()};
  public static RunWaveManifestSnapshot Read(JObject p,JObject independentlyTrustedOwner)=>new RunWaveManifestSnapshot(RunWaveManifestReader.Read(p,independentlyTrustedOwner));
  public static RunWaveManifestSnapshot ReadJson(string json,JObject independentlyTrustedOwner)=>Read(RunWaveManifestReader.Parse(json),independentlyTrustedOwner);
 }
}
