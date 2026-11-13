#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using LastRail.Battle;
// Additional closed resource manifest is reviewed inside the full source bundle.
// It does not extend r6 wire roles or issue an admission.
public static class Ce03DynamicResourceClosure {
 public const string ManifestPath="QA/Ce03CommandIntegration/dynamic-resource-closure.json";
 static SortedDictionary<string,string> Expected(){
  var result=new SortedDictionary<string,string>(StringComparer.Ordinal);
  foreach(string id in CombatAbilityAudioSet.InitialAbilityIds){result.Add("CombatPresentation/Audio/"+id,"AudioClip");result.Add("CombatPresentation/EffectAudio/"+id+"-0","AudioClip");}
  foreach(string id in new[]{"op_kyle.active-1","op_sera.active-1","op_sera.active-2","op_sera.ultimate-1","op_mira.ultimate-1"})result.Add("CombatPresentation/EffectAudio/"+id,"AudioClip");
  result.Add("CombatPresentation/Audio/levels","TextAsset");result.Add("CombatPresentation/EffectAudio/bindings","TextAsset");result.Add("CombatPresentation/EffectAudio/ApprovedResults","ApprovedResultAudio");return result;
 }
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 public static void Export(){
  var rows=new JArray();foreach(var pair in Expected()){
   var asset=LastRail.Resources.PreparedContent.Load(pair.Key);if(asset==null||asset.GetType().Name!=pair.Value||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long localId))throw new InvalidOperationException("Dynamic typed load failed: "+pair.Key);
   string path=AssetDatabase.GetAssetPath(asset);rows.Add(new JObject{["resourceKey"]=pair.Key,["path"]=LastRail.Resources.RemoteContentCatalog.LegacyAddress(pair.Key),["type"]=pair.Value,["guid"]=guid,["localFileId"]=localId.ToString(System.Globalization.CultureInfo.InvariantCulture),["rawSha256"]=Hash(path),["metaSha256"]=Hash(path+".meta")});
  }
  Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath));File.WriteAllText(ManifestPath,Ce03CommandContract.Canonical(new JObject{["schema"]="ce03-dynamic-resource-closure-v1",["unityVersion"]=Application.unityVersion,["rows"]=rows}));Validate();
 }
 public static void Validate(){
  JObject root;using(var reader=new Newtonsoft.Json.JsonTextReader(new StringReader(File.ReadAllText(ManifestPath))){MaxDepth=24}){root=JObject.Load(reader,new Newtonsoft.Json.Linq.JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});if(reader.Read())throw new FormatException("Trailing dynamic closure data");}
  Ce03CommandContract.Fields(root,"schema","unityVersion","rows");if((string)root["schema"]!="ce03-dynamic-resource-closure-v1"||(string)root["unityVersion"]!=Application.unityVersion||root["rows"] is not JArray rows)throw new FormatException("Dynamic closure header mismatch");
  var expected=Expected();if(!rows.Select(r=>(string)r["resourceKey"]).SequenceEqual(expected.Keys))throw new FormatException("Dynamic resource closed set mismatch");
  foreach(JObject row in rows){
   Ce03CommandContract.Fields(row,"resourceKey","path","type","guid","localFileId","rawSha256","metaSha256");string key=(string)row["resourceKey"],path=(string)row["path"];
   string wanted=LastRail.Resources.RemoteContentCatalog.LegacyAddress(key);if(path!=wanted)throw new FormatException("Dynamic resource provenance mismatch: "+key);path=LastRail.Resources.RemoteContentCatalog.AssetPath(key);if(AssetDatabase.GUIDToAssetPath((string)row["guid"])!=path||Hash(path)!=(string)row["rawSha256"]||Hash(path+".meta")!=(string)row["metaSha256"])throw new FormatException("Dynamic resource physical mismatch: "+key);
   var asset=LastRail.Resources.PreparedContent.Load(key);if(asset==null||asset.GetType().Name!=expected[key]||(string)row["type"]!=expected[key]||AssetDatabase.GetAssetPath(asset)!=path||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long id)||guid!=(string)row["guid"]||id.ToString(System.Globalization.CultureInfo.InvariantCulture)!=(string)row["localFileId"])throw new FormatException("Dynamic resource object mismatch: "+key);
  }
 }
}
#endif
