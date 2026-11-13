#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
// Editor-only actual load verification. No provider or admission is minted here.
public static class Ce03PresentationManifestLoader {
 public static CombatPresentationAssets Validate(Ce03PresentationManifest manifest){
  var m=manifest.Copy();var objects=new Dictionary<string,Object>(StringComparer.Ordinal);var paths=new Dictionary<string,string>(StringComparer.Ordinal);
  var rows=((JArray)m["bindings"]).Cast<JObject>().ToArray();
  foreach(var b in rows){
   string id=(string)b["bindingId"],guid=(string)b["locator"]["guid"],path=AssetDatabase.GUIDToAssetPath(guid);
   if(string.IsNullOrEmpty(path))throw new FormatException("Unresolved presentation GUID: "+id);
   long localId=long.Parse((string)b["locator"]["localFileId"],CultureInfo.InvariantCulture);
   var matches=AssetDatabase.LoadAllAssetsAtPath(path).Where(o=>o!=null&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string g,out long n)&&g==guid&&n==localId).ToArray();
   if(matches.Length!=1||TypeName(matches[0])!=(string)b["assetType"])throw new FormatException("Presentation object/type mismatch: "+id);
   if(Hash(path)!=(string)b["assetRawSha256"]||Hash(path+".meta")!=(string)b["metaRawSha256"])throw new FormatException("Presentation physical hash mismatch: "+id);
   if((string)b["importValidation"]["unityVersion"]!=Application.unityVersion)throw new FormatException("Presentation Unity version mismatch.");
   // Native importer version was not available in this project's audit.
   if(b["importValidation"]["importerVersion"].Type!=JTokenType.Null)throw new NotSupportedException("Importer version cannot be independently verified.");
   objects.Add(id,matches[0]);paths.Add(id,path);
  }
  var byPath=paths.GroupBy(x=>x.Value,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.Select(x=>x.Key).ToArray(),StringComparer.Ordinal);
  foreach(var b in rows){
   string id=(string)b["bindingId"],path=paths[id];var expected=new HashSet<string>(byPath[path].Where(x=>x!=id),StringComparer.Ordinal);
   // Contract closure includes all typed subassets of each direct dependency,
   // and all sibling subassets of the same file, matching Server projection.
   foreach(string p in AssetDatabase.GetDependencies(path,false).Where(x=>x!=path)){
    if(!byPath.TryGetValue(p,out var deps))throw new FormatException("Missing actual dependency: "+p);
    expected.UnionWith(deps.Where(x=>x!=id));
   }
   if(!expected.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(((JArray)b["dependencies"]).Select(x=>(string)x)))throw new FormatException("Actual dependency closure mismatch: "+id);
   var allAtPath=AssetDatabase.LoadAllAssetsAtPath(path).Where(x=>x!=null).ToArray();
   if(allAtPath.Length!=byPath[path].Length||allAtPath.Any(o=>!byPath[path].Any(k=>objects[k]==o)))throw new FormatException("Unlisted actual subasset: "+path);
  }
  foreach(var s in (JArray)m["sourceFiles"])if(Hash(Ce03PresentationManifest.SourcePaths[(string)s["logicalName"]])!=(string)s["rawSha256"])throw new FormatException("Renderer code changed: "+(string)s["logicalName"]);
  var roles=(JObject)m["roles"];var config=(CombatPresentationAssets)objects[(string)roles["configuration"]];
  if(AssetDatabase.GetAssetPath(config)!=LastRail.Resources.RemoteContentCatalog.AssetPath("CombatPresentation/approved"))throw new FormatException("Wrong Resources presentation root.");
  void Match(string role,Object o){if(objects[(string)roles[role]]!=o)throw new FormatException("Serialized role mismatch: "+role);}
  Match("font",config.Font);Match("console",config.Console);Match("icons",config.IconAtlas);Match("map",config.ExteriorMap);Match("trainExterior",config.ExteriorTrain);Match("interior",config.Interior);Match("defaultTurret",config.DefaultTurret);Match("turretRig",config.TurretRig);Match("buggy",config.Buggy);Match("bike",config.Bike);Match("hit",config.Hit);Match("skill",config.Skill);Match("shield",config.Shield);Match("frame",config.Frame);Match("primary",config.Primary);Match("secondary",config.Secondary);
  if(config.OperatorIds?.Length!=6||config.OperatorAtlases?.Length!=6||config.OperatorIds.Distinct().Count()!=6||config.Keys?.Length!=160||config.Frames?.Length!=160||config.Keys.Distinct().Count()!=160)throw new FormatException("Serialized array identity mismatch.");
  foreach(var a in (JArray)roles["actorAtlases"])if(config.ActorAtlas((string)a["actorId"])!=objects[(string)a["bindingId"]])throw new FormatException("Serialized actor atlas mismatch.");
  foreach(var f in (JArray)roles["frames"]){int i=Array.IndexOf(config.Keys,(string)f["key"]);if(i<0||config.Frames[i]!=objects[(string)f["bindingId"]])throw new FormatException("Serialized frame mismatch.");}
  Ce03DynamicResourceClosure.Validate();return config;
 }
 static string TypeName(Object o)=>o is CombatPresentationAssets?"CombatPresentationAssets":o is TMPro.TMP_FontAsset?"TMP_FontAsset":o.GetType().Name;
 static string Physical(string path){
  if(path.StartsWith("Packages/",StringComparison.Ordinal)){
   var p=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
   if(p==null)throw new FormatException("Unresolved package.");
   return Path.Combine(p.resolvedPath,path.Substring(("Packages/"+p.name+"/").Length));
  }
  return Path.GetFullPath(path);
 }
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Physical(path)))).Replace("-","").ToLowerInvariant();}
}
#endif
