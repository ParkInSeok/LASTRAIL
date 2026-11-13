#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
namespace LastRail.Editor {
 // Read-only asset inspection. Does not move files or alter Addressables settings.
 public static class RemoteResourceMigrationPlan {
  static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
  static string Key(string path){var at=path.IndexOf("/Resources/",StringComparison.Ordinal);string value=path.Substring(at+11);return value.Substring(0,value.Length-Path.GetExtension(value).Length);}
  public static void Inspect(){
   if(LastRail.Resources.RemoteContentCatalog.Entries.Any(e=>!e.Local&&e.Path.StartsWith("Assets/_LASTRAIL/Content/RemoteResources/",StringComparison.Ordinal)))
    throw new InvalidOperationException("Migration already applied; preserve its approved plan/catalog and use ReconcileProductionGroups for an audit.");
   var keys=new HashSet<string>(OfflineBootstrapResources.ResourceKeys,StringComparer.Ordinal);
   var resources=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)&&p.Contains("/Resources/")&&!AssetDatabase.IsValidFolder(p)&&AssetDatabase.GetMainAssetTypeAtPath(p)!=null&&!p.EndsWith(".cs",StringComparison.Ordinal)&&!p.EndsWith(".asmdef",StringComparison.Ordinal)&&!p.EndsWith(".dll",StringComparison.Ordinal)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
   var localPaths=new HashSet<string>(OfflineBootstrapResources.DirectAssetPaths,StringComparer.Ordinal);
   foreach(var path in resources)if(keys.Contains(Key(path)))localPaths.Add(path);
   foreach(var path in AssetDatabase.GetDependencies(localPaths.ToArray(),true))localPaths.Add(path);
   var bootstrap=new HashSet<string>(AssetDatabase.GetDependencies("Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity",true));
   var rows=new JArray();var seen=new HashSet<string>();
   var source="// Current imported identities; relocation is separately gated by migration-plan.json.\nnamespace LastRail.Resources { public static partial class RemoteContentCatalog { public static readonly Entry[] Entries=new Entry[] {\n";
   foreach(var path in resources){
    var key=Key(path);if(!seen.Add(key))throw new InvalidOperationException("Duplicate key: "+key);
    bool local=keys.Contains(key);string target=local?path:"Assets/_LASTRAIL/Content/RemoteResources/"+key+Path.GetExtension(path);
    var guid=AssetDatabase.AssetPathToGUID(path);var type=AssetDatabase.GetMainAssetTypeAtPath(path);var entry=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
    rows.Add(new JObject{["key"]=key,["source"]=path,["target"]=target,["local"]=local,["guid"]=guid,["sha256"]=Hash(path),["metaSha256"]=Hash(path+".meta"),["type"]=type.AssemblyQualifiedName,["existingGroup"]=entry?.parentGroup.Name,["existingAddress"]=entry?.address,["bootstrapDependency"]=bootstrap.Contains(path),["offlineDependency"]=localPaths.Contains(path),["targetConflict"]=!local&&File.Exists(target)});
    source+="new Entry("+JsonConvert.ToString(key)+","+JsonConvert.ToString(path)+","+JsonConvert.ToString(path)+","+JsonConvert.ToString(type.AssemblyQualifiedName)+","+(local?"true":"false")+"),\n";
   }
   source+="}; } }\n";
   var plan=new JObject{["mode"]="READ_ONLY_ASSET_INSPECTION_NO_MOVES_OR_GROUP_CHANGES",["resourceCount"]=rows.Count,["localCount"]=rows.Count(r=>(bool)r["local"]),["moveCount"]=rows.Count(r=>!(bool)r["local"]),["targetConflicts"]=rows.Count(r=>(bool)r["targetConflict"]),["unexpectedOfflineDependencies"]=new JArray(rows.Where(r=>!(bool)r["local"]&&(bool)r["offlineDependency"])),["rows"]=rows,["directLocalDependencies"]=new JArray(localPaths.OrderBy(p=>p,StringComparer.Ordinal))};
   File.WriteAllText("../QA/ResourceIntake-20261110/connection/all-remote-migration-plan.json",plan.ToString(Formatting.Indented));
   File.WriteAllText("Assets/_LASTRAIL/Scripts/Resources/RemoteContentCatalog.Generated.cs",source);
   Debug.Log("REMOTE_INSPECTION moves="+plan["moveCount"]+" local="+plan["localCount"]+" conflicts="+plan["targetConflicts"]);
  }
 }
}
#endif
