using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LastRail.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Read-only original owner projection. This type provides no native combat context or scaling input.
 public sealed class RunRiskBindingSnapshot {
  public const string Version="combat.b-risk-run-binding.v1.20261003";
  static readonly string[] Missing={"ORIGINAL_PINNED_RISK_APPLICATION_VERSION","ORIGINAL_NATIVE_COMBAT_CONTEXT","ORIGINAL_UNSCALED_HP_OR_EHP","ORIGINAL_RAW_ACTION_DAMAGE","ORIGINAL_RISK_PROGRESS_APPLIED_FLAGS","SUPPORTED_B_RUNTIME_PROFILE"};
  readonly JObject packet;
  RunRiskBindingSnapshot(JObject value){packet=(JObject)value.DeepClone();}
  public JObject Capture()=>(JObject)packet.DeepClone();public JObject Frozen=>(JObject)packet["frozen"].DeepClone();public JObject Progress=>(JObject)packet["progress"].DeepClone();
  public string RunId=>(string)packet["frozen"]["runId"];public bool ActiveRun=>(bool)packet["support"]["activeRun"];public bool ScalingSupported()=>false;
  public void RequireScalingSource()=>throw new InvalidOperationException("combat_b_risk_run_raw_profile_unavailable");
  internal static void Need(bool value){if(!value)throw new ArgumentException("combat_b_risk_run_pinned_conflict");}
  static JObject Object(JToken t)=>t as JObject??throw new ArgumentException("combat_b_risk_run_packet_invalid");
  static void Fields(JObject o,params string[] names)=>Need(o!=null&&o.Count==names.Length&&names.All(k=>o[k]!=null));
  internal static string Id(JToken t){Need(t?.Type==JTokenType.String&&((string)t).Length>0&&((string)t).Length<=200);return (string)t;}
  static void NullableId(JToken t){Need(t!=null);if(t.Type!=JTokenType.Null)Id(t);}
  static void Hash(JToken t)=>Need(t?.Type==JTokenType.String&&Regex.IsMatch((string)t,"\\A[a-f0-9]{64}\\z"));
  static void NullableHash(JToken t){Need(t!=null);if(t.Type!=JTokenType.Null)Hash(t);}
  static long Number(JToken t,long min,long max){Need(t?.Type==JTokenType.Integer);long value=(long)t;Need(value>=min&&value<=max);return value;}
  static void One(JToken t,params string[] choices)=>Need(t?.Type==JTokenType.String&&choices.Contains((string)t));
  static void Null(JToken t)=>Need(t?.Type==JTokenType.Null);
  internal static string Canonical(JToken t){if(t is JObject o)return "{"+string.Join(",",o.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>JsonConvert.SerializeObject(p.Name)+":"+Canonical(p.Value)))+"}";if(t is JArray a)return "["+string.Join(",",a.Select(Canonical))+"]";return t.ToString(Formatting.None);}
  internal static string Digest(JToken t){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(Canonical(t))).Select(b=>b.ToString("x2")));}
  internal static JObject TrustedCopy(JObject input){Need(input!=null);var t=(JObject)input.DeepClone();Fields(t,"accountId","runId","startSnapshotHash","progressRevision","packetHash");Id(t["accountId"]);Id(t["runId"]);Hash(t["startSnapshotHash"]);Hash(t["packetHash"]);Number(t["progressRevision"],1,9007199254740991);return t;}
  public static RunRiskBindingSnapshot Read(JObject input,JObject independentlyTrustedOwner){
   Need(input!=null);var p=(JObject)input.DeepClone();var trust=TrustedCopy(independentlyTrustedOwner);Fields(p,"version","frozen","progress","originalProfile","runStatus","support","missingSources","packetHash");Need((string)p["version"]==Version);Hash(p["packetHash"]);
   var f=Object(p["frozen"]);Fields(f,"accountId","runId","operationPreviewId","operationId","selectedRisk","balanceVersion","contentVersion","departureVersion","startSnapshotHash","originalCombatContextId","riskApplicationVersion");foreach(var key in new[]{"accountId","runId","operationPreviewId","operationId","balanceVersion","departureVersion"})Id(f[key]);NullableId(f["contentVersion"]);One(f["selectedRisk"],"I","II","III","IV","V");Hash(f["startSnapshotHash"]);Null(f["originalCombatContextId"]);Null(f["riskApplicationVersion"]);
   var progress=Object(p["progress"]);Fields(progress,"revision","stateHash","phase","currentNodeId","visitDepth","band","bandStatus","pendingEdgeId","blockedByPendingTransactions");Number(progress["revision"],1,9007199254740991);Hash(progress["stateHash"]);One(progress["phase"],"READY","TRAVELLING","ACTIVE","FINAL_NODE_COMPLETE");Id(progress["currentNodeId"]);long depth=Number(progress["visitDepth"],0,12);NullableId(progress["pendingEdgeId"]);Need(progress["blockedByPendingTransactions"].Type==JTokenType.Boolean);string band=depth==0||depth==12?null:depth<=4?"EARLY":depth<=8?"MIDDLE":"LATE";Need(JToken.DeepEquals(progress["band"],band==null?JValue.CreateNull():new JValue(band))&&(string)progress["bandStatus"]==(depth==0?"NO_VISITED_NODE":depth==12?"BOSS_SEPARATE_PROFILE":"VISITED_NODE"));
   var profile=Object(p["originalProfile"]);Fields(profile,"profile","definitionVersion","planHash","publicationVersion","publicationHash","currentEncounterRef","currentEncounterVersion","currentEncounterDefinitionHash");Need((string)profile["profile"]=="ORDINARY_12_NODE");Id(profile["definitionVersion"]);Hash(profile["planHash"]);NullableId(profile["publicationVersion"]);NullableHash(profile["publicationHash"]);NullableId(profile["currentEncounterRef"]);NullableId(profile["currentEncounterVersion"]);NullableHash(profile["currentEncounterDefinitionHash"]);
   One(p["runStatus"],"ACTIVE","FINISH_PENDING","COMPLETED","FAILED","QUARANTINED","EXPIRED","ABANDONED");var support=Object(p["support"]);Fields(support,"metadata","activeRun","scaling","missionStart");Need(JToken.DeepEquals(support,new JObject{["metadata"]=true,["activeRun"]=(string)p["runStatus"]=="ACTIVE",["scaling"]=false,["missionStart"]=false})&&JToken.DeepEquals(p["missingSources"],new JArray(Missing)));
   foreach(var key in new[]{"accountId","runId","startSnapshotHash"})Need(JToken.DeepEquals(f[key],trust[key]));Need(JToken.DeepEquals(progress["revision"],trust["progressRevision"])&&JToken.DeepEquals(p["packetHash"],trust["packetHash"]));var unsigned=(JObject)p.DeepClone();unsigned.Remove("packetHash");Need(Digest(unsigned)==(string)trust["packetHash"]);return new RunRiskBindingSnapshot(p);
  }
 }
}
