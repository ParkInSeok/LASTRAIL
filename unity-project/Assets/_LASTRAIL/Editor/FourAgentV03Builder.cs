#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace LastRail.Editor {
 public static class FourAgentV03Builder {
  public const string Source="D:/Project/LASTRAIL/QA/DirectionImpact-20260921/four-agent-final-review-v01/";
  public const string Crops="D:/Project/LASTRAIL/QA/PlanImpact-20261015/eve-drone-motion-review-v01/five-agent-combat-fx-v02/";
  public const string Destination="Assets/_LASTRAIL/UI/CombatAnimations/FourAgentV03";
  public const string CatalogPath=Destination+"/ApprovedFourAgentV03.asset";
  static string Hash(string p){using var h=SHA256.Create();return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
  static Vector2 Size(string p){var b=File.ReadAllBytes(p);int N(int i)=>(b[i]<<24)|(b[i+1]<<16)|(b[i+2]<<8)|b[i+3];return new Vector2(N(16),N(20));}
  public static JObject Build(){
   if(Path.GetFullPath(Application.dataPath).Replace('\\','/')!="D:/Project/LASTRAIL/unity-project/Assets")throw new InvalidOperationException("Canonical Unity D project required.");
   var original=JObject.Parse(File.ReadAllText(Source+"unity-clips.json"));var crops=JObject.Parse(File.ReadAllText(Crops+"manifest.json"));var projectile=JObject.Parse(File.ReadAllText(Crops+"projectile.json"));
   foreach(var f in original["clips"].SelectMany(c=>c["frames"]))if(Hash((string)f["source"])!=(string)f["sha256"])throw new InvalidOperationException("Approved original changed: "+f["source"]);
   var model=FourAgentV03Catalog.Read(original,crops,Crops,Size);
   var fxType=typeof(FourAgentV03Catalog).Assembly.GetType("LastRail.Presentation.Battle.FourAgentV03Effects")??throw new InvalidOperationException("V03 independent effects required before import.");
   var extra=(string[])fxType.GetProperty("TexturePaths").GetValue(null);
   var legacy=new HashSet<string>(StringComparer.Ordinal);foreach(var d in crops["directions"]){foreach(var r in d["reina"].SelectMany(r=>r).Concat(d["impact"]))if(r.Type!=JTokenType.Null)legacy.Add(Crops+(string)r["src"]);}legacy.Add(Crops+"kyle-projectile-source.png");
   var paths=model.TexturePaths.Concat(extra).Concat(legacy).Distinct(StringComparer.Ordinal).ToArray();
   Directory.CreateDirectory(Destination+"/Textures");var audit=new JArray();var added=new List<string>();
   foreach(string path in paths){string full=Path.GetFullPath(path);if(!full.StartsWith("D:\\Project\\LASTRAIL\\",StringComparison.OrdinalIgnoreCase)||!path.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(path).Contains("background"))throw new ArgumentException("Independent D texture required.");string hash=Hash(path),asset=Destination+"/Textures/"+hash+".png";
    if(!File.Exists(asset)){File.Copy(path,asset,false);added.Add(asset);}else if(Hash(asset)!=hash)throw new InvalidOperationException("Owned imported texture conflict.");
    var size=Size(path);audit.Add(new JObject{["source"]=path,["sha256"]=hash,["asset"]=asset,["width"]=size.x,["height"]=size.y});
   }
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   foreach(string path in audit.Select(r=>(string)r["asset"]).Distinct()){var importer=(TextureImporter)AssetImporter.GetAtPath(path);var natural=Size(path);importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=Mathf.NextPowerOfTwo((int)Mathf.Max(natural.x,natural.y));importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();}
   var entries=audit.Select(r=>new FiveAgentVisualAssets.TextureEntry{Path=(string)r["source"],Texture=AssetDatabase.LoadAssetAtPath<Texture2D>((string)r["asset"]),NativeSize=new Vector2((float)r["width"],(float)r["height"])}).ToArray();foreach(var e in entries)if(e.Texture==null||e.Texture.width!=(int)e.NativeSize.x||e.Texture.height!=(int)e.NativeSize.y)throw new InvalidOperationException("Imported canvas dimensions changed: "+e.Path+" expected "+e.NativeSize+" actual "+(e.Texture==null?"missing":e.Texture.width+"x"+e.Texture.height));
   var catalog=AssetDatabase.LoadAssetAtPath<FourAgentV03Assets>(CatalogPath);if(catalog==null){catalog=ScriptableObject.CreateInstance<FourAgentV03Assets>();catalog.Initialize(model.Capture(),crops,projectile,entries);AssetDatabase.CreateAsset(catalog,CatalogPath);}else if(!JToken.DeepEquals(catalog.CreateCatalog().Capture(),model.Capture())||!JToken.DeepEquals(catalog.Crops,crops)||!JToken.DeepEquals(catalog.Projectile,projectile)||entries.Any(e=>catalog.Texture(e.Path)!=e.Texture||catalog.Size(e.Path)!=e.NativeSize))throw new InvalidOperationException("Preserve previous imported version.");
   AssetDatabase.SaveAssets();var result=new JObject{["version"]=FourAgentV03Catalog.Version,["sourceManifestHash"]=Hash(Source+"unity-clips.json"),["verifiedOriginalFrames"]=400,["logicalClips"]=32,["textureReferences"]=entries.Length,["uniqueTextures"]=audit.Select(r=>(string)r["sha256"]).Distinct().Count(),["catalog"]=CatalogPath,["backgroundImported"]=false,["entries"]=audit};
   string output=Environment.GetEnvironmentVariable("LASTRAIL_V03_OUTPUT");if(string.IsNullOrWhiteSpace(output)||!Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Explicit D QA output required.");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"import.json"),result.ToString());return result;
  }
  public static void BuildCommand()=>Build();
 }
}
#endif
