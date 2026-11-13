#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Byte-preserving intake of the instructed candidate; historical approval registries stay intact.
public static class KyleActiveMotionImport {
 const string Folder="Assets/_LASTRAIL/Content/Battle/KyleRightActive02/";
 const string Configuration="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryPresentation.asset";
 public static void Apply(){
  var configuration=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>(Configuration);
  if(configuration==null||configuration.OperatorFrames==null)throw new InvalidOperationException("Existing ordinary roster required");
  var intake=JObject.Parse(File.ReadAllText(Folder+"intake.json"));
  var rows=(JArray)intake["frames"];
  string[] kinds={"idle","basic","active"};int[] revisions={535,912,69};
  string[] projects={"3024176caf444fa3972992d557f02465","e8f4f88f2069447ea9c3f95acbf47746","7c231a638b3340bd80cf9fcfa98ee3c9"};
  int[] activeTiming={400,120,130,130,130,350,650,130,130,130,120,580};
  for(int k=0;k<kinds.Length;k++){
   var clip=rows.Where(r=>(string)r["kind"]==kinds[k]).ToArray();
   if(clip.Length!=12)throw new InvalidDataException("Incomplete source clip: "+kinds[k]);
   for(int i=0;i<clip.Length;i++){
    var r=clip[i];string path=Folder+kinds[k]+"-"+i.ToString("00")+".png";
    if((int)r["index"]!=i||(string)r["path"]!=path||(string)r["project"]!=projects[k]||(int)r["revision"]!=revisions[k]||(int)r["durationMs"]!=(k==2?activeTiming[i]:100))throw new InvalidDataException("Source identity or timing mismatch");
    using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant()!=(string)r["sha256"])throw new InvalidDataException("Source bytes changed: "+path);
   }
  }
  var set=UnityEngine.Object.Instantiate(configuration.OperatorFrames);
  try{
   var kyle=set.Find("op_kyle");if(kyle==null)throw new InvalidOperationException("Kyle roster slot required");
   Texture2D[] Clip(string kind)=>rows.Where(r=>(string)r["kind"]==kind).Select(r=>Import((string)r["path"])).ToArray();
   int[] Timing(string kind)=>rows.Where(r=>(string)r["kind"]==kind).Select(r=>(int)r["durationMs"]).ToArray();
   kyle.IdleFrames=Clip("idle");kyle.DurationsMs=Timing("idle");kyle.BasicFrames=Clip("basic");kyle.BasicDurationsMs=Timing("basic");kyle.BasicFireFrame=4;
   kyle.ActiveFrames=Clip("active");kyle.ActiveDurationsMs=Timing("active");kyle.FacesRight=true;
   kyle.CanvasSize=new Vector2(1254,1254);kyle.FootAnchorTopLeft=new Vector2(686,1238);kyle.BodySpan=1224;
   kyle.Emblem=null; // Current authored body already contains its emblem; preserve the approved portrait.
   set.Validate();string target=Folder+"Roster.asset";var existing=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>(target);
   if(existing!=null){EditorUtility.CopySerialized(set,existing);EditorUtility.SetDirty(existing);}else{AssetDatabase.CreateAsset(set,target);existing=set;}
   configuration.OperatorFrames=existing;EditorUtility.SetDirty(configuration);AssetDatabase.SaveAssets();
   Debug.Log("Kyle active candidate registry installed; 36 byte-preserved frames, 3000ms active presentation. No art approval changed.");
  }finally{if(!AssetDatabase.Contains(set))UnityEngine.Object.DestroyImmediate(set);}
 }
 static Texture2D Import(string path){
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
  return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
}
#endif
