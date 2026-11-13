using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Prepared commands are not admission authority. Executing an admitted run also
// requires the independently verified r5 profile/content/snapshot/seed envelope.
public sealed class Ce03CommandContract {
 public const string Version="ce03-command-contract-v1";
 public long Revision {get;}
 public string[] SupportIds=>ids.ToArray();
 readonly string[] ids;readonly string commandJson;readonly bool individual,generalAuto;
 public Ce03CommandContract(JObject data){
  commandJson=Canonical(data);individual=(string)data["version"]=="ce03-command-contract-v2"||(string)data["version"]=="ce03-command-contract-v3";generalAuto=(string)data["version"]=="ce03-command-contract-v3";Fields(data,individual?(generalAuto?new[]{"version","support","supportChannel","ultimates","actives"}:new[]{"version","support","supportChannel","ultimates"}):new[]{"version","support","supportChannel","leader"});if(!individual)Equal(data,"version",Version);Equal(data,"supportChannel","SHARED_TRAIN_MANUAL_CAST");
  var support=data["support"] as JObject;Fields(support,"rulesVersion","catalogVersion","revision","slots");Equal(support,"rulesVersion","ce03-support-selection-v1");Equal(support,"catalogVersion","ce03-local-support-roles-v1");Revision=Positive(support,"revision");
  var slots=support["slots"] as JArray;if(slots==null||slots.Count!=3)throw new FormatException("Three saved support slots required.");
  ids=new string[3];for(int i=0;i<3;i++){var row=slots[i] as JObject;Fields(row,"slot","skillId");if(row["slot"]?.Type!=JTokenType.Integer||(int)row["slot"]!=i)throw new FormatException("Support order mismatch.");ids[i]=(string)row["skillId"];if(!new[]{"support_fire","barrier","emergency_repair"}.Contains(ids[i]))throw new NotSupportedException("Support effect not implemented.");}
  if(ids.Distinct().Count()!=3)throw new FormatException("Duplicate support skill.");
  if(individual){if(Sha(Canonical(data["ultimates"]))!="c3eea7f8f65c7bf5b2dd20c0e63c40ad3380fdaf7ab2994697d87f7487bdd421")throw new FormatException("Unsupported per-operator ultimate contract.");if(generalAuto&&Sha(Canonical(data["actives"]))!="5d0d66ac75954cca7001f20c432f91b7aedcb7c8eb6cefad43adc04ed5384853")throw new FormatException("Unsupported general auto contract.");}
  else {var leader=data["leader"] as JObject;Fields(leader,"version","scope","perHit","overrides");Equal(leader,"version","basic-hp-hit-v1");Equal(leader,"scope","LOCAL_QA_OPT_IN");Equal(leader,"overrides","ULTIMATE_FIRST_READY_AND_COOLDOWN");if(Positive(leader,"perHit")!=4)throw new NotSupportedException("Unverified leader policy.");}
 }
 public void ValidatePrepared(JObject prepared,string owner,long expectedRevision){
  if(prepared?["commandContract"]==null||Canonical(prepared["commandContract"])!=commandJson)throw new FormatException("Command contract changed.");
  if(string.IsNullOrWhiteSpace(owner)||Revision!=expectedRevision||(string)prepared?["ownedSelection"]?["accountId"]!=owner)throw new FormatException("Owner/support revision mismatch.");
  if(!individual&&new[]{"ce03.execution.2026-09-13.002","ce03.execution.2026-09-13.003"}.Contains((string)prepared?["draft"]?["simulationVersion"]))throw new FormatException("Legacy command cannot select an individual execution policy.");
  if(individual&&(string)prepared?["draft"]?["simulationVersion"]!=(generalAuto?"ce03.execution.2026-09-13.003":"ce03.execution.2026-09-13.002"))throw new FormatException("Command execution version mismatch.");
  var manual=prepared?["draft"]?["template"]?["train"]?["manual"] as JArray;
  if(manual==null||!manual.Select(r=>(string)r["id"]).SequenceEqual(ids))throw new FormatException("Prepared support order mismatch.");
  var hashed=new JObject();foreach(var key in new[]{"ownedSelection","carriageOwnership","draft","commandContract"}){if(prepared[key]==null)throw new FormatException();hashed[key]=prepared[key].DeepClone();}
  if(prepared.Property("presentationManifest")!=null){var manifest=new Ce03PresentationManifest(prepared["presentationManifest"] as JObject);hashed["presentationManifest"]=manifest.Copy();}
  if(prepared.Property("playerAssetBinding")!=null){if(prepared.Property("presentationManifest")!=null)throw new FormatException("Mixed asset resolvers.");Ce03PlayerAssetContract.Validate(prepared["playerAssetBinding"] as JObject,prepared["ownedSelection"] as JObject);hashed["playerAssetBinding"]=prepared["playerAssetBinding"].DeepClone();}
  if((string)prepared["prepareSha256"]!=Sha(Canonical(hashed)))throw new FormatException("Prepare digest mismatch.");
 }
 public BasicCombatSimulation CreateLocalTest(JObject prepared,string owner,long expectedRevision,uint seed){
  ValidatePrepared(prepared,owner,expectedRevision);
  var simulation=BasicCombatSimulation.FromPreparedProfileForLocalTest(prepared,(JObject)prepared["ownedSelection"],owner,seed);if(!individual)simulation.ConfigureLocalLeaderCharge(4);return simulation;
 }
 public static string Canonical(JToken token)=>Ce03CanonicalJson.Write(token);
 public static string Sha(string value){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(new UTF8Encoding(false,true).GetBytes(value))).Replace("-","").ToLowerInvariant();}
 internal static void Fields(JObject obj,params string[] fields){if(obj==null||!obj.Properties().Select(p=>p.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(fields.OrderBy(x=>x,StringComparer.Ordinal)))throw new FormatException("Unexpected contract fields.");}
 internal static void Equal(JObject obj,string key,string value){if(obj?[key]?.Type!=JTokenType.String||(string)obj[key]!=value)throw new FormatException("Unsupported "+key);}
 internal static long Positive(JObject obj,string key){if(obj?[key]?.Type!=JTokenType.Integer||(long)obj[key]<1)throw new FormatException("Invalid "+key);return (long)obj[key];}
}
}

