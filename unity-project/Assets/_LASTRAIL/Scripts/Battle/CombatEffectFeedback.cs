using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Presentation-only cursor, owned by one view for its lifetime. Restored rows are
// acknowledged without replay. Keep fingerprints, not duplicate result payloads.
public sealed class CombatEffectFeedback {
 readonly Dictionary<string,string> seen=new Dictionary<string,string>(StringComparer.Ordinal);
 readonly HashSet<string> audioGroups=new HashSet<string>(StringComparer.Ordinal);
 static string AudioGroup(JObject r)=>new JArray(r["runId"],r["profileSha256"],r["globalSequence"],r["commandId"],r["abilityId"],r["effectIndex"],r["pulseIndex"]).ToString(Newtonsoft.Json.Formatting.None);
 static (string Id,string Hash) Identity(JObject row){
  if(row==null||row["id"]?.Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)row["id"]))throw new ArgumentException("Original effect identity required.");
  if(row["kind"]?.Type!=JTokenType.String)throw new ArgumentException("Typed effect kind required.");
  BasicCombatSimulation.ValidateActualResult((string)row["kind"],row["actualResult"] as JObject);
  return ((string)row["id"],BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(row)));
 }
 static bool Known(Dictionary<string,string> source,string id,string hash){
  if(!source.TryGetValue(id,out var original))return false;
  if(original!=hash)throw new InvalidOperationException("Effect presentation event content conflict.");return true;
 }
 public void Seed(JArray rows){
  if(rows==null)throw new ArgumentNullException(nameof(rows));
  var staged=new Dictionary<string,string>(StringComparer.Ordinal);var groups=new HashSet<string>(StringComparer.Ordinal);
  foreach(JObject row in rows){var key=Identity(row);Known(seen,key.Id,key.Hash);if(!Known(staged,key.Id,key.Hash))staged.Add(key.Id,key.Hash);groups.Add(AudioGroup(row));}
  // Validate the complete restore batch before acknowledging any row/audio group.
  foreach(var pair in staged)seen[pair.Key]=pair.Value;audioGroups.UnionWith(groups);
 }
 public bool Observe(JObject row){var key=Identity(row);if(Known(seen,key.Id,key.Hash))return false;bool positive=Positive(row);seen.Add(key.Id,key.Hash);return positive;}
 // Audio is downstream of Observe (or Seed), not an event intake. It must not
 // consume an unseen visual event merely because an audio caller probes it.
 public bool ReserveAudio(JObject row){var key=Identity(row);Known(seen,key.Id,key.Hash);return Positive(row)&&audioGroups.Add(AudioGroup(row));}
 public static bool Positive(JObject row){var r=row["actualResult"] as JObject;if(r==null)return false;
  switch((string)row["kind"]){
   case "DAMAGE":return (int?)r["hpLoss"]>0||(int?)r["absorbed"]>0;
   case "HEAL":case "REPAIR":return (int?)r["actualHpGain"]>0;
   case "SHIELD_ISSUE":return (int?)r["addedAmount"]>0;
   case "STATUS_ADD":case "STATUS_REFRESH":return (bool?)r["accepted"]==true;
   case "INTERRUPT":return r["cancelledActionIds"] is JArray ids&&ids.Count>0;
   default:return false;
  }
 }
}
}
