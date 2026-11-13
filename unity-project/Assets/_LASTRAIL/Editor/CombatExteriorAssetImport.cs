#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
public static class CombatExteriorAssetImport {
 public static void Run(){string output=null;try{
  output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new Exception("Fresh D output required");Directory.CreateDirectory(output);
  const string staged="QA/Unity1Artifacts/Design/Zone02-03-Quality-20260914/",dest="Assets/_LASTRAIL/UI/ApprovedCombat/ExteriorQuality/";Directory.CreateDirectory(dest);
  var manifest=JObject.Parse(File.ReadAllText(staged+"manifest.json"));var imports=new JArray();
  foreach(string name in new[]{"canyon-far-v02.png","canyon-mid-ridge-v02.png","canyon-road-v02.png","exterior-bezel-v02.png"}){
   var row=((JArray)manifest["assets"]).Single(a=>(string)a["file"]==name);using var sha=System.Security.Cryptography.SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(staged+name))).Replace("-","");if(!string.Equals(hash,(string)row["sha256"],StringComparison.OrdinalIgnoreCase))throw new Exception("Staged hash mismatch "+name);
   if(File.Exists(dest+name))throw new Exception("Preserve existing asset "+name);File.Copy(staged+name,dest+name);AssetDatabase.ImportAsset(dest+name,ImportAssetOptions.ForceSynchronousImport);
   var importer=(TextureImporter)AssetImporter.GetAtPath(dest+name);importer.textureType=TextureImporterType.Default;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.isReadable=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();imports.Add(new JObject{["path"]=dest+name,["sourceSha256"]=hash.ToLowerInvariant()});
  }
  var config=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>(Ce03RailPresentation.Root);File.Copy(Ce03RailPresentation.Root,output+"/approved.before.asset.txt");File.Copy(Ce03RailPresentation.Root+".meta",output+"/approved.before.meta.txt");
  var layout=JObject.Parse(config.PursuitLayout.text);File.WriteAllText(output+"/pursuit-layout.before.json",layout.ToString());
  var decorations=new JArray(((JArray)layout["decorations"]).Where(d=>(string)d["kind"]=="roadside").Select(d=>d.DeepClone()));
  decorations.Insert(0,new JObject{["id"]="quality-ridge",["kind"]="ridge",["uv"]=new JArray(16f/2035,54f/773,2003f/2035,657f/773),["width"]=640,["height"]=210,["period"]=820,["x"]=-70,["y"]=18,["speed"]=4});layout["decorations"]=decorations;layout["roadUv"]=new JArray(0,0,1,1);
  string layoutPath=dest+"pursuit-layout-v02.json";File.WriteAllText(layoutPath,layout.ToString());AssetDatabase.ImportAsset(layoutPath,ImportAssetOptions.ForceSynchronousImport);
  config.DistantFar=config.ExteriorMap=AssetDatabase.LoadAssetAtPath<Texture2D>(dest+"canyon-far-v02.png");config.PursuitRidges=AssetDatabase.LoadAssetAtPath<Texture2D>(dest+"canyon-mid-ridge-v02.png");config.PursuitRoad=AssetDatabase.LoadAssetAtPath<Texture2D>(dest+"canyon-road-v02.png");config.ExteriorBezel=AssetDatabase.LoadAssetAtPath<Texture2D>(dest+"exterior-bezel-v02.png");config.PursuitLayout=AssetDatabase.LoadAssetAtPath<TextAsset>(layoutPath);EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
  File.WriteAllText(output+"/complete.json",new JObject{["imports"]=imports,["root"]=Ce03RailPresentation.Root,["selectedResourceCount"]=config.SecondFinalResources.Length,["newRole"]="approved.ExteriorBezel",["scope"]="Only approved zone02 candidates imported; original assets and selected22 unchanged. New strict profile required before HTTP preparation."}.ToString());EditorApplication.Exit(0);
 }catch(Exception e){if(output!=null)File.WriteAllText(output+"/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
