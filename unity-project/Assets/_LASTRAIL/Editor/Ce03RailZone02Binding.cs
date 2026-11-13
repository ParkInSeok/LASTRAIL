#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
public static class Ce03RailZone02Binding {
 static void Need(bool value,string reason){if(!value)throw new InvalidOperationException("Zone02 source binding differs: "+reason);}
 static string Hash(string path){using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 public static void Verify(JObject manifest){
  string path=Ce03RailPresentation.Zone02SelectionFile;Need(File.Exists(path)&&Hash(path)==(string)manifest["zone02SelectionManifestSha256"],"selection file digest");var selected=JObject.Parse(File.ReadAllText(path));Need(Ce03CommandContract.Canonical(selected)==Ce03CommandContract.Canonical(manifest["zone02Selection"]),"embedded selection");
  string direction=Ce03RailPresentation.Zone02DirectionFile;Need(File.Exists(direction)&&Hash(direction)==(string)selected["directionManifest"]["sha256"],"independent Direction manifest");var source=JObject.Parse(File.ReadAllText(direction));
  foreach(var r in (JArray)selected["resources"]){if(r["sourceFile"].Type==JTokenType.Null)continue;string file=(string)r["sourceFile"];var row=((JArray)source["assets"]).SingleOrDefault(a=>(string)a["file"]==file);Need(row!=null&&((string)row["sha256"]).ToLowerInvariant()==(string)r["sourceRawSha256"],"Direction resource entry");string original=Path.Combine(Path.GetDirectoryName(direction),file);Need(Hash(original)==(string)r["sourceRawSha256"],"original source bytes");}
 }
}
#endif
