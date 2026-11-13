#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
public static class Ce03RailPresentationBinding {
 // Preserve signed logical provenance while resolving only the approved relocated asset.
 public static string PhysicalAssetPath(string path){
  const string prefix="Assets/_LASTRAIL/Resources/";
  if(!path.StartsWith(prefix,StringComparison.Ordinal))return path;
  string key=path.Substring(prefix.Length);key=key.Substring(0,key.Length-Path.GetExtension(key).Length);
  if(LastRail.Resources.RemoteContentCatalog.LegacyAddress(key)!=path)throw new InvalidOperationException("Unapproved resource provenance: "+path);
  return LastRail.Resources.RemoteContentCatalog.AssetPath(key);
 }
 public static string LogicalAssetPath(string path){
  const string prefix="Assets/_LASTRAIL/Content/RemoteResources/";
  if(!path.StartsWith(prefix,StringComparison.Ordinal))return path;
  string key=path.Substring(prefix.Length);key=key.Substring(0,key.Length-Path.GetExtension(key).Length);
  if(LastRail.Resources.RemoteContentCatalog.AssetPath(key)!=path)throw new InvalidOperationException("Unapproved resource location: "+path);
  return LastRail.Resources.RemoteContentCatalog.LegacyAddress(key);
 }
 static void Need(bool ok,string reason){if(!ok)throw new InvalidOperationException("Current rail presentation binding differs: "+reason);}
 static string FilePath(string path){
  path=PhysicalAssetPath(path);string root=Path.GetFullPath(Application.dataPath+"/..");string full;
  if(path.StartsWith("Assets/",StringComparison.Ordinal))full=Path.GetFullPath(Path.Combine(root,path));
  else {var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);Need(package!=null,"package");root=Path.GetFullPath(package.resolvedPath);full=Path.GetFullPath(Path.Combine(root,path.Substring(package.assetPath.Length+1)));}
  Need(full.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&File.Exists(full),"file path");return full;
 }
 static string Hash(string path){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static string Key(UnityEngine.Object obj){Need(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj,out string guid,out long id),"object locator");return guid+":"+id;}
 public static void Verify(Ce03RailPreparation preparation){
  if(preparation.Presentation==null)return;
  var m=(JObject)preparation.Presentation.Copy()["manifest"];if((string)m["profile"]=="second-final-zone02-v1")Ce03RailZone02Binding.Verify(m);var root=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");Need(root!=null&&AssetDatabase.GetAssetPath(root)==PhysicalAssetPath(Ce03RailPresentation.Root),"approved root");
  var files=new HashSet<string>(StringComparer.Ordinal);var objects=new Dictionary<string,UnityEngine.Object>(StringComparer.Ordinal);
  foreach(var b in (JArray)m["bindings"]){string p=(string)b["path"];if(files.Add(p)){string f=FilePath(p);Need(Hash(f)==(string)b["assetRawSha256"]&&Hash(f+".meta")== (string)b["metaRawSha256"],"raw/meta "+p);var deps=AssetDatabase.GetDependencies(PhysicalAssetPath(p),false).Select(LogicalAssetPath).Where(d=>d!=p).OrderBy(d=>d,StringComparer.Ordinal);Need(deps.SequenceEqual(((JArray)b["directDependencies"]).Select(d=>(string)d).OrderBy(d=>d,StringComparer.Ordinal)),"dependencies "+p);}
   string key=(string)b["guid"]+":"+(string)b["localFileId"];var obj=AssetDatabase.LoadAllAssetsAtPath(PhysicalAssetPath(p)).FirstOrDefault(o=>o!=null&&Key(o)==key);Need(obj!=null&&obj.GetType().FullName==(string)b["type"]&&obj.name==(string)b["name"],"loaded object "+p);objects.Add(key,obj);
  }
  Need(files.SetEquals(AssetDatabase.GetDependencies(PhysicalAssetPath(Ce03RailPresentation.Root),true).Select(LogicalAssetPath)),"root dependency graph");
  var actualRoles=new JArray();var serialized=new SerializedObject(root);var property=serialized.GetIterator();while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue!=null){var obj=property.objectReferenceValue;Need(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj,out string guid,out long id),"role locator");actualRoles.Add(new JObject{["role"]="approved."+property.propertyPath,["path"]=LogicalAssetPath(AssetDatabase.GetAssetPath(obj)),["guid"]=guid,["localFileId"]=id.ToString(),["type"]=obj.GetType().FullName});}
  Need(Ce03CommandContract.Canonical(actualRoles)==Ce03CommandContract.Canonical(m["roles"]),"serialized roles");
  var selected=(JArray)m["selectedResources"];Need(root.SecondFinalResources!=null&&root.SecondFinalResources.Length==selected.Count,"selected length");
  for(int i=0;i<selected.Count;i++){var r=selected[i];string key=(string)r["guid"]+":"+(string)r["localFileId"];var texture=root.SecondFinalResources[i];Need(texture!=null&&Key(texture)==key&&objects[key]==texture&&LastRail.Resources.PreparedContent.Load<Texture2D>((string)r["resourceKey"])==texture&&texture.width==(int)r["width"]&&texture.height==(int)r["height"],"selected reference");}
  foreach(var s in (JArray)m["sourceFiles"])Need(Hash(FilePath((string)s["path"]))==(string)s["sha256"],"source "+(string)s["path"]);
  var actualSources=Directory.GetFiles(Application.dataPath+"/_LASTRAIL","*.cs",SearchOption.AllDirectories).Select(p=>"Assets"+p.Substring(Application.dataPath.Length).Replace(Path.DirectorySeparatorChar,'/'));Need(new HashSet<string>(actualSources,StringComparer.Ordinal).SetEquals(((JArray)m["sourceFiles"]).Select(s=>(string)s["path"])),"complete source inventory");
 }
}
#endif
