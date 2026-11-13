#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace LastRail.Editor {
 public static class FiveAgentVisualReferenceBuilder {
  public const string Source="D:/Project/LASTRAIL/QA/DirectionImpact-20260921/eve-combat-v01/five-agent-combat-fx-v02/";
  public const string Destination="Assets/_LASTRAIL/UI/CombatAnimations/FiveAgentReferenceV02";
  static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
  static Vector2 Size(string path){var bytes=File.ReadAllBytes(path);int N(int i)=>(bytes[i]<<24)|(bytes[i+1]<<16)|(bytes[i+2]<<8)|bytes[i+3];return new Vector2(N(16),N(20));}
  [MenuItem("LASTRAIL/Visual QA/Build Approved Five Agent Reference V02")]
  public static void BuildMenu()=>Build();
  public static JObject Build(){
   if(!Path.GetFullPath(Application.dataPath).Replace('\\','/').StartsWith("D:/Project/LASTRAIL/unity-project/Assets",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Canonical D Unity project required.");
   string cj=File.ReadAllText(Source+"unity-reference-contract.json"),mj=File.ReadAllText(Source+"manifest.json"),pj=File.ReadAllText(Source+"projectile.json");var contract=JObject.Parse(cj);var rows=(JArray)contract["inventory"];FiveAgentVisualReference.Read(contract,JObject.Parse(mj),JObject.Parse(pj),BDirectAttackInput.Digest(contract),p=>Size(Source+p));
   foreach(var item in rows){string relative=(string)item["path"],path=Path.GetFullPath(Source+relative);if(!path.StartsWith(Path.GetFullPath(Source),StringComparison.OrdinalIgnoreCase)||!File.Exists(path)||Hash(path)!=(string)item["sha256"])throw new InvalidOperationException("Direction reference mismatch: "+relative);}
   var byHash=new Dictionary<string,string>(StringComparer.Ordinal);foreach(string abs in Directory.EnumerateFiles(Application.dataPath,"*.png",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal)){string asset="Assets"+abs.Substring(Application.dataPath.Length).Replace('\\','/');if(AssetDatabase.LoadAssetAtPath<Texture2D>(asset)==null)continue;string hash=Hash(abs);if(!byHash.ContainsKey(hash))byHash.Add(hash,asset);}
   Directory.CreateDirectory(Destination+"/Textures");var unique=new Dictionary<string,string>();var added=new List<string>();var audit=new JArray();int reused=0;
   foreach(var item in rows.Where(r=>((string)r["path"]).EndsWith(".png",StringComparison.OrdinalIgnoreCase))){string hash=(string)item["sha256"],relative=(string)item["path"];if(relative=="background.png"||relative.IndexOf("hud",StringComparison.OrdinalIgnoreCase)>=0)throw new InvalidOperationException("Reference-only composite cannot enter runtime catalog.");if(!unique.TryGetValue(hash,out var target)){if(byHash.TryGetValue(hash,out target))reused++;else{target=Destination+"/Textures/"+hash+".png";if(File.Exists(target)){if(Hash(target)!=hash)throw new InvalidOperationException("Owned texture conflict.");}else File.Copy(Source+relative,target,false);added.Add(target);}unique.Add(hash,target);}audit.Add(new JObject{["source"]=relative,["sha256"]=hash,["asset"]=target});}
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   foreach(var path in added){var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new InvalidOperationException("Texture import missing.");importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();}
   var entries=audit.Select(r=>new FiveAgentVisualAssets.TextureEntry{Path=(string)r["source"],Texture=AssetDatabase.LoadAssetAtPath<Texture2D>((string)r["asset"]),NativeSize=Size(Source+(string)r["source"])}).ToArray();if(entries.Any(e=>e.Texture==null))throw new InvalidOperationException("Texture binding unresolved.");
   string catalogPath=Destination+"/ApprovedVisualAssets.asset";var catalog=AssetDatabase.LoadAssetAtPath<FiveAgentVisualAssets>(catalogPath);if(catalog==null){catalog=ScriptableObject.CreateInstance<FiveAgentVisualAssets>();catalog.Initialize(cj,mj,pj,entries);AssetDatabase.CreateAsset(catalog,catalogPath);}else if(catalog.ContractHash!=BDirectAttackInput.Digest(contract))throw new InvalidOperationException("Preserve old catalog; approved contract changed.");catalog.CreateModel();
   string prefabPath=Destination+"/ApprovedVisualConsumer.prefab";if(!File.Exists(prefabPath)){var go=new GameObject("ApprovedFiveAgentVisualConsumer",typeof(RectTransform),typeof(FiveAgentVisualView));try{var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(941,1672);go.GetComponent<FiveAgentVisualView>().Configure(catalog);PrefabUtility.SaveAsPrefabAsset(go,prefabPath);}finally{UnityEngine.Object.DestroyImmediate(go);}}
   AssetDatabase.SaveAssets();var report=new JObject{["version"]="unity.five-agent-visual-import.v1.20261017",["verifiedReferences"]=rows.Count,["pngPaths"]=entries.Length,["uniquePngBytes"]=unique.Count,["reusedTextures"]=reused,["newTextures"]=added.Count,["referenceBackgroundImported"]=false,["contractHash"]=BDirectAttackInput.Digest(contract),["catalog"]=catalogPath,["prefab"]=prefabPath,["entries"]=audit};
   string output=Environment.GetEnvironmentVariable("LASTRAIL_FIVE_AGENT_QA_OUTPUT");if(!string.IsNullOrEmpty(output)){string full=Path.GetFullPath(output);if(!full.StartsWith("D:/Project/LASTRAIL/QA/",StringComparison.OrdinalIgnoreCase)&&!full.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("QA output must stay on project D.");Directory.CreateDirectory(full);string path=Path.Combine(full,"import-audit-"+Guid.NewGuid().ToString("N")+".json");using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(stream))writer.Write(report.ToString());}
   return report;
  }
 }
}
#endif