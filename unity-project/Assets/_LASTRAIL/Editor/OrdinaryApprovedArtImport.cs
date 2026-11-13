#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Explicit approved-source intake. Historical CE03 art and admission manifests remain unchanged.
public static class OrdinaryApprovedArtImport {
 const string Project="D:/Project/LASTRAIL/",Destination="Assets/_LASTRAIL/Content/Battle/Approved821/";
 const string Configuration="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryPresentation.asset";
 const string Binding="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryBinding.asset";
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static Texture2D Load(string source,string name,string expected,JArray provenance,Dictionary<string,Texture2D> imported){
  string input=Path.GetFullPath(Project+source);if(!input.StartsWith(Path.GetFullPath(Project),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Outside source root.");
  string hash=Hash(input),path=Destination+name+".png";
  if(expected!=null&&hash!=expected)throw new InvalidOperationException("Approved source changed: "+source);
  if(imported.TryGetValue(hash,out var shared)){provenance.Add(new JObject{["source"]=source,["path"]=AssetDatabase.GetAssetPath(shared),["sha256"]=hash,["pixelsAltered"]=false,["sharedExposure"]=true});return shared;}
  if(File.Exists(path)){if(Hash(path)!=hash)throw new InvalidOperationException("Preserve existing imported source: "+path);}else File.Copy(input,path);
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
  provenance.Add(new JObject{["source"]=source,["path"]=path,["sha256"]=hash,["pixelsAltered"]=false});var result=AssetDatabase.LoadAssetAtPath<Texture2D>(path);imported.Add(hash,result);return result;
 }
 public static void ApplyKyleRight(){
  var intake=JObject.Parse(File.ReadAllText(Project+"QA/ObstacleTrial-20261106/kyle-right-01/intake.json"));
  if((string)intake["operatorId"]!="op_kyle")throw new InvalidDataException("Registered Kyle identity required.");
  var set=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>(Configuration).OperatorFrames);var kyle=set.Find("op_kyle");
  var provenance=new JArray();var imported=new Dictionary<string,Texture2D>(StringComparer.Ordinal);
  Texture2D[] Clip(string kind)=>((JArray)intake["frames"]).Where(f=>(string)f["kind"]==kind).Select(f=>Load(Path.GetRelativePath(Project,(string)f["source"]),"op_kyle-right-"+kind+"-"+(int)f["index"],(string)f["sha256"],provenance,imported)).ToArray();
  int[] Durations(string kind)=>((JArray)intake["frames"]).Where(f=>(string)f["kind"]==kind).Select(f=>(int)f["durationMs"]).ToArray();
  kyle.IdleFrames=Clip("idle");kyle.DurationsMs=Durations("idle");kyle.BasicFrames=Clip("basic");kyle.BasicDurationsMs=Durations("basic");kyle.BasicFireFrame=(int)intake["fireFrame"];
  kyle.CanvasSize=new Vector2(1254,1254);kyle.FootAnchorTopLeft=new Vector2((float)intake["foot"][0],(float)intake["foot"][1]);kyle.BodySpan=(float)intake["bodySpan"];
  kyle.BasicProjectile=Load(Path.GetRelativePath(Project,(string)intake["projectile"]),"op_kyle-right-qi-v03",(string)intake["projectileSha256"],provenance,imported);
  // The approved drawing already contains its garment mark; the old independent overlay would double it.
  kyle.Emblem=null;set.Validate();string path=Destination+"KyleRight01.asset";
  var previous=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>(path);if(previous!=null){EditorUtility.CopySerialized(set,previous);UnityEngine.Object.DestroyImmediate(set);EditorUtility.SetDirty(previous);}else AssetDatabase.CreateAsset(set,path);
  AssetDatabase.SaveAssets();File.WriteAllText(Project+"QA/ObstacleTrial-20261106/kyle-right-01/import.json",provenance.ToString());
 }
 public static void ApplyRequestedRosterRight(){
  var original=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>(Destination+"RequestedRoster01.asset");
  if(original==null)throw new InvalidOperationException("Existing requested roster supply required.");
  var intake=JObject.Parse(File.ReadAllText(Project+"QA/ObstacleTrial-20261106/requested-roster-01/right-idle-intake.json"));
  if((string)intake["project"]!="b255bd4ed968489b8e47de3c7003f6a8"||(int)intake["revision"]!=14)throw new InvalidDataException("Approved right idle source differs.");
  var set=UnityEngine.Object.Instantiate(original);var reina=set.Find("op_reina");
  var provenance=new JArray();var imported=new Dictionary<string,Texture2D>(StringComparer.Ordinal);var rows=(JArray)intake["frames"];
  reina.IdleFrames=rows.Select((f,i)=>Load((string)f["source"],"op_reina-right-idle-"+i,(string)f["sha256"],provenance,imported)).ToArray();
  reina.DurationsMs=rows.Select(f=>(int)f["durationMs"]).ToArray();
  reina.FootAnchorTopLeft=new Vector2((float)intake["foot"][0],(float)intake["foot"][1]);
  set.Validate();string path=Destination+"RequestedRosterRight01.asset";var previous=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>(path);
  if(previous!=null){EditorUtility.CopySerialized(set,previous);UnityEngine.Object.DestroyImmediate(set);EditorUtility.SetDirty(previous);}else AssetDatabase.CreateAsset(set,path);
  AssetDatabase.SaveAssets();File.WriteAllText(Project+"QA/ObstacleTrial-20261106/requested-roster-01/right-import.json",provenance.ToString());
 }
 public static void ApplyRequestedRoster(){
  var existing=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>(Destination+"Operators.asset");
  if(existing==null)throw new InvalidOperationException("Existing approved four-person supply required.");
  var provenance=new JArray();var imported=new Dictionary<string,Texture2D>(StringComparer.Ordinal);
  var supply=JObject.Parse(File.ReadAllText(Project+"Tools/FrameStudio/data/reina-direction2-underlap-v14/landscape-supply.json"));
  var clip=((JArray)supply["source_clips"]).Single(c=>(string)c["clip_id"]=="Nyx-idle");
  if((string)clip["studio_project_id"]!="d4c4ba167d044a50bd67b70399bbf06d"||(string)clip["source_version"]!="camera-d2-idle-wind-v04")throw new InvalidDataException("Nyx source version differs.");
  var rows=(JArray)clip["frames"];
  var textures=rows.Select((f,i)=>Load((string)f["source_path"],"op_nyx-idle-"+i,(string)f["sha256"],provenance,imported)).ToArray();
  var nyx=new CombatOperatorFrames.Entry{OperatorId="op_nyx",CanvasSize=new Vector2(1263,1246),FootAnchorTopLeft=new Vector2(904,1223),BodySpan=1182,IdleFrames=textures,DurationsMs=rows.Select(f=>(int)f["duration_ms"]).ToArray(),Portrait=textures[0],PortraitUv=new Rect(.36f,.69f,.28f,.28f)};
  var set=UnityEngine.Object.Instantiate(existing);
  set.Entries=new[]{set.Find("op_reina"),set.Find("op_kyle"),set.Find("op_eve"),set.Find("op_sera"),nyx};
  // Registration belongs to this requested display; original exposures and historical bindings stay intact.
  set.Entries[0].FootAnchorTopLeft=new Vector2(860,1033);set.Entries[0].BodySpan=755;
  set.Entries[0].PortraitUv=new Rect(510f/1254,1-560f/1254,300f/1254,300f/1254);
  set.Entries[1].FootAnchorTopLeft=new Vector2(900,1230);
  set.Entries[2].FootAnchorTopLeft=new Vector2(913,1224);set.Entries[2].BodySpan=1183;
  set.Entries[3].FootAnchorTopLeft=new Vector2(791,1508);set.Entries[3].BodySpan=1464;
  set.Validate();
  string path=Destination+"RequestedRoster01.asset";var previous=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>(path);
  if(previous!=null){EditorUtility.CopySerialized(set,previous);UnityEngine.Object.DestroyImmediate(set);EditorUtility.SetDirty(previous);}else AssetDatabase.CreateAsset(set,path);
  AssetDatabase.SaveAssets();
  File.WriteAllText(Project+"QA/ObstacleTrial-20261106/requested-roster-01/intake.json",new JObject{["scope"]="DISPLAY_SUPPLY_ONLY_NOT_START_OR_KIT_REPLACEMENT",["order"]=new JArray("op_reina","op_kyle","op_eve","op_sera","op_nyx"),["sourceClip"]=clip,["files"]=provenance,["footRegistration"]="Shared opaque boot support across all supplied exposures; see shared-boot-registration.json",["registrations"]=new JArray(set.Entries.Select(e=>new JObject{["id"]=e.OperatorId,["footTopLeft"]=new JArray(e.FootAnchorTopLeft.x,e.FootAnchorTopLeft.y),["bodySpan"]=e.BodySpan})),["facing"]="Original D2 left-facing, no mirror or new view"}.ToString());
 }
 public static void Apply(){
  if(Path.GetFullPath(Application.dataPath).Replace('\\','/')!=Project+"unity-project/Assets")throw new InvalidOperationException("Canonical Unity workspace required.");
  Directory.CreateDirectory(Destination);var provenance=new JArray();var imported=new Dictionary<string,Texture2D>(StringComparer.Ordinal);
  Texture2D Import(string source,string name,string expected=null)=>Load(source,name,expected,provenance,imported);
  var supply=JObject.Parse(File.ReadAllText(Project+"Tools/FrameStudio/data/reina-direction2-underlap-v14/landscape-supply.json"));
  CombatOperatorFrames.Entry Clip(string id,string label,Vector2 canvas,Vector2 foot,float span,Rect portrait){
   var source=((JArray)supply["source_clips"]).Single(c=>(string)c["clip_id"]==label);var frames=(JArray)source["frames"];
   var textures=frames.Select((f,i)=>Import((string)(f["source_path"].Type==JTokenType.String?f["source_path"]:f["asset_path"]),id+"-idle-"+i,(string)f["sha256"])).ToArray();
   return new CombatOperatorFrames.Entry{OperatorId=id,CanvasSize=canvas,FootAnchorTopLeft=foot,BodySpan=span,IdleFrames=textures,DurationsMs=frames.Select(f=>(int)f["duration_ms"]).ToArray(),Portrait=textures[0],PortraitUv=portrait};
  }
  var kyle=Import("QA/PlanImpact-20261015/eve-drone-motion-review-v01/kyle-pure-martial-art-v01/collar-fold-v04/candidate.png","op_kyle-idle","6e5be957c0d30507717624be4387b8434fa109db4f2fc435eb8846ccc36ee9a9");
  var set=ScriptableObject.CreateInstance<CombatOperatorFrames>();
  set.Entries=new[]{
   new CombatOperatorFrames.Entry{OperatorId="op_kyle",CanvasSize=new Vector2(1254,1254),FootAnchorTopLeft=new Vector2(901,1230),BodySpan=1209,IdleFrames=new[]{kyle},DurationsMs=new[]{1000},Portrait=kyle,PortraitUv=new Rect(.36f,.74f,.28f,.25f),Emblem=Import("QA/PlanImpact-20261015/eve-drone-motion-review-v01/kyle-pure-martial-art-v01/emblem-v02/emblem.png","kyle-emblem","3fa3f30b21a43a0c4ade2e7062a7b07340c85570ec3a5178a2fefe8c565d1175"),EmblemRectTopLeft=new Rect(660.5f,325.5f,61,61)},
   Clip("op_reina","Reina-idle",new Vector2(1254,1254),new Vector2(619,1081),891,new Rect(.32f,.58f,.36f,.30f)),
   Clip("op_eve","Eve-idle",new Vector2(1263,1245),new Vector2(642,1245),1194,new Rect(.35f,.70f,.30f,.28f)),
   Clip("op_sera","Sera-idle",new Vector2(1024,1536),new Vector2(548.5f,1510),1536,new Rect(.27f,.74f,.43f,.25f)),
   new CombatOperatorFrames.Entry{OperatorId="op_mira",Portrait=Import("QA/DirectionImpact-20260921/operator-bible-portraits-v01/assets/mira.png","mira-portrait","1ff79f72a03ff702696f99c9a96f5e606813bea84cb7ba9288744c71b0e1da05"),PortraitUv=new Rect(0,0,1,1)}
  };
  set.Validate();string setPath=Destination+"Operators.asset";
  const string roofPath="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryRoof.asset";
  var roof=AssetDatabase.LoadAssetAtPath<OrdinaryRoofPresentationAssets>(roofPath);if(roof==null)throw new InvalidOperationException("Existing physical roof binding required.");
  string roofCopy=Project+"QA/CommanderS1-20260922/ApprovedArt821/previous-roof.asset";if(!File.Exists(roofCopy))File.Copy(roofPath,roofCopy);
  roof.CarBodies=new[]{Import("QA/DirectionImpact-20260921/train-modular-v01/assets/engine.png","car-engine","e263a62bfc2abbb8e2bcdca56aa846a826e89a665bbd3ca1e28b9632bf986f7e"),Import("QA/DirectionImpact-20260921/train-modular-v01/assets/Combat.png","car-combat","9ea6ac4aea051e64c5ba27d8d66f458bf24e3a0f5a08d5cfbe4885e710d7519c"),Import("QA/DirectionImpact-20260921/gun-carriage-two-turrets-v01/assets/chassis.png","car-gun-chassis","f197db2607660ffca1927c65c58e429cebdefe5104111990a5999edbb61679a1")};
  roof.CarBounds=new[]{new Rect(7,836-125-589,1845,589),new Rect(14,819-84-659,1897,659),new Rect(27,887-177-638,1719,638)};EditorUtility.SetDirty(roof);
  var previous=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>(setPath);if(previous!=null){EditorUtility.CopySerialized(set,previous);UnityEngine.Object.DestroyImmediate(set);set=previous;EditorUtility.SetDirty(set);}else AssetDatabase.CreateAsset(set,setPath);
  if(!File.Exists(Configuration)&&!AssetDatabase.CopyAsset(Ce03RailPresentation.Root,Configuration))throw new InvalidOperationException("Could not preserve/create ordinary configuration.");
  var config=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>(Configuration);config.OperatorFrames=set;config.OrdinaryRoof=roof;EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
  File.WriteAllText(Destination+"provenance.json",new JObject{["scope"]="APPROVED_D2_IDLE_AND_UI_CROPS_NOT_FULL_COMBAT_ART",["approvalSource"]="QA/DirectionImpact-20260921/four-agent-final-review-v01/UNITY-APPLICATION.md; Kyle collar-fold-v04 approved_source; Mira bible portraits manifest",["anchorScope"]="FIXED_DISPLAY_REGISTRATION_NOT_MUZZLE_OR_PHYSICS",["sources"]=provenance}.ToString());AssetDatabase.Refresh();
  var baked=CombatPlayerAssetBindingBuilder.BakeConfiguration(config);var old=AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>(Binding);
  File.WriteAllText(Project+"QA/CommanderS1-20260922/ApprovedArt821/art-binding.json",baked.ManifestJson);
  if(old!=null){string saved=Project+"QA/CommanderS1-20260922/ApprovedArt821/previous-binding.asset";if(!File.Exists(saved))File.Copy(Binding,saved);EditorUtility.CopySerialized(baked,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(baked);}else AssetDatabase.CreateAsset(baked,Binding);
  AssetDatabase.SaveAssets();
 }
}
#endif
