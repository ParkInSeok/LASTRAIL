#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Diagnostics {
// Serialized at build time. The trusted process adapter separately pins the entire binary closure.
public sealed class Ce03PlayerSourcePin {
 readonly Dictionary<string,string> hashes=new Dictionary<string,string>(StringComparer.Ordinal);
 readonly string buildManifestPath;
 public Ce03PlayerSourcePin(JObject pin){
  if((string)pin["version"]!="ce03.compiled-sources.v1")throw new FormatException("Unsupported compiled source pin.");
  buildManifestPath=(string)pin["buildManifestPath"];
  foreach(var row in (JArray)pin["files"])hashes.Add((string)row["path"],(string)row["sha256"]);
  if(hashes.Count==0||string.IsNullOrWhiteSpace(buildManifestPath)||hashes.ContainsKey(buildManifestPath))throw new FormatException("Invalid source set.");
 }
 public void Verify(JObject bundle){
  var files=bundle["files"] as JArray;
  if((string)bundle["schemaVersion"]!="ce03-source-bundle-v1"||files==null||files.Count!=hashes.Count+1)throw new FormatException("Source bundle is incomplete.");
  var seen=new HashSet<string>(StringComparer.Ordinal);
  foreach(var row in files){string path=(string)row["path"],content=(string)row["content"],hash=(string)row["sha256"];
   if(path==null||content==null||!seen.Add(path)||Ce03CommandContract.Sha(content)!=hash||
     (path!=buildManifestPath&&(!hashes.TryGetValue(path,out var expected)||hash!=expected)))throw new FormatException("Executing Player differs from supplied source: "+path);
  }
  if(!seen.Contains(buildManifestPath)||hashes.Keys.Any(p=>!seen.Contains(p)))throw new FormatException("Compiled source omitted.");
 }
}
}
#endif
