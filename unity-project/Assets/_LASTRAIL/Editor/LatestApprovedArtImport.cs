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
using Entry=LastRail.Battle.CombatOperatorFrames.Entry;

public static class LatestApprovedArtImport {
 const string Root="D:/Project/LASTRAIL/",Qa=Root+"QA/ResourceIntake-20261110/";
 const string Destination="Assets/_LASTRAIL/Content/Battle/LatestApproved20261110/";
 const string ConfigPath="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryPresentation.asset";
 const string RegistryPath=Destination+"Roster.asset";
 static string Hash(string path){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static string Source(JToken frame){var path=Path.GetFullPath(Root+(string)frame["source"]);if(!path.StartsWith(Path.GetFullPath(Root),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Source outside project");return path;}
 static JObject Manifest()=>JObject.Parse(File.ReadAllText(Qa+"characters.json"));
 static Entry Copy(Entry e)=>JsonUtility.FromJson<Entry>(JsonUtility.ToJson(e));
 static string Key(string id,bool right)=>id+":"+right;
 static void Backup(string path){if(!File.Exists(path))return;string relative=path.Replace('\\','/');if(Path.IsPathRooted(path))relative=Path.GetRelativePath(Root,path).Replace('\\','/');string target=Qa+"before/"+relative;Directory.CreateDirectory(Path.GetDirectoryName(target));if(!File.Exists(target))File.Copy(path,target);if(File.Exists(path+".meta")&&!File.Exists(target+".meta"))File.Copy(path+".meta",target+".meta");}
 static IEnumerable<JToken> Exposures(JToken clip)=>clip["frames"].Children();
 static void CheckManifest(JObject input){
  var keys=new HashSet<string>();
  foreach(var clip in input["clips"]){
   string key=(string)clip["operatorId"]+":"+(string)clip["direction"]+":"+(string)clip["role"];
   if(!keys.Add(key)||(bool?)clip["selectedAssetsVerified"]!=true||(int?)clip["currentRevision"]!=(int?)clip["approvedRevision"])throw new InvalidDataException("Unverified or duplicate clip "+key);
   if((bool?)clip["mirror"]==true)throw new InvalidDataException("New mirror prohibited");
   foreach(var exclusion in input["exclusions"]){if(exclusion["projectId"]!=null&&(string)exclusion["projectId"]==(string)clip["projectId"])throw new InvalidDataException("Excluded project in intake");string excluded=(string)exclusion["source"];if(excluded!=null&&((string)clip["approvalReference"]).StartsWith(excluded+"/",StringComparison.OrdinalIgnoreCase)&&!((string)clip["approvalReference"]).Contains("hem-wind-v01/"))throw new InvalidDataException("Excluded approval source in intake");}
   foreach(var frame in Exposures(clip))if((int)frame["durationMs"]<=0||Hash(Source(frame))!=(string)frame["sha256"])throw new InvalidDataException("Source hash or duration differs: "+key);
  }
 }
 static Texture2D Import(JToken frame,Dictionary<string,Texture2D> known,JArray log){
  string source=Source(frame),hash=Hash(source);if(hash!=(string)frame["sha256"])throw new InvalidDataException("Changed source "+source);
  if(!known.TryGetValue(hash,out var texture)){
   string path=Destination+hash+".png";
   if(File.Exists(path)){if(Hash(path)!=hash)throw new InvalidDataException("Existing imported asset differs "+path);}else File.Copy(source,path);
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.isReadable=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
   texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);known.Add(hash,texture);
  }
  log.Add(new JObject{["source"]=(string)frame["source"],["sha256"]=hash,["asset"]=AssetDatabase.GetAssetPath(texture),["pixelsAltered"]=false});return texture;
 }
 static Entry.Registration Registration(JToken clip,Entry entry,bool sameFacing,JArray log){
  var size=clip["canvasSize"]??clip["frames"][0]["canvasSize"];var canvas=new Vector2((float)size[0],(float)size[1]);var supplied=clip["registration"];
  if(supplied!=null&&supplied.Type!=JTokenType.Null&&supplied["footAnchorTopLeft"]!=null)return new Entry.Registration{CanvasSize=canvas,FootAnchorTopLeft=new Vector2((float)supplied["footAnchorTopLeft"][0],(float)supplied["footAnchorTopLeft"][1]),BodySpan=(float)supplied["bodySpan"]};
  if(sameFacing&&entry.HasIdle&&entry.CanvasSize==canvas)return new Entry.Registration{CanvasSize=canvas,FootAnchorTopLeft=entry.FootAnchorTopLeft,BodySpan=entry.BodySpan};
  // Local display fallback only: replace with measured producer registration when supplied.
  var temp=new Texture2D(2,2,TextureFormat.RGBA32,false);
  try{
   if(!ImageConversion.LoadImage(temp,File.ReadAllBytes(Source(clip["frames"][0])),false))throw new InvalidDataException("Cannot decode neutral pose");
   var pixels=temp.GetPixels32();int minY=temp.height,maxY=-1,minX=temp.width,maxX=-1;
   for(int y=0;y<temp.height;y++)for(int x=0;x<temp.width;x++)if(pixels[y*temp.width+x].a>=128){minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
   if(maxY<0)throw new InvalidDataException("Empty neutral pose");
   for(int y=minY;y<=Math.Min(maxY,minY+12);y++)for(int x=0;x<temp.width;x++)if(pixels[y*temp.width+x].a>=128){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);}
   var value=new Entry.Registration{CanvasSize=canvas,FootAnchorTopLeft=new Vector2((minX+maxX+1)*.5f,temp.height-minY),BodySpan=maxY-minY+1};
   log.Add(new JObject{["operatorId"]=(string)clip["operatorId"],["direction"]=(string)clip["direction"],["role"]=(string)clip["role"],["scope"]="LOCAL_DISPLAY_ALPHA_NEUTRAL_NOT_COLLISION_OR_ARTIST_APPROVED",["foot"]=new JArray(value.FootAnchorTopLeft.x,value.FootAnchorTopLeft.y),["span"]=value.BodySpan,["source"]=(string)clip["frames"][0]["source"]});return value;
  }finally{UnityEngine.Object.DestroyImmediate(temp);}
 }
 // Authored display phase boundaries; native impact/commit timing remains authoritative.
 static int FireFrame(JToken clip){switch((string)clip["operatorId"]){case "op_kyle":return 4;case "op_sera":return 5;case "op_reina":return (string)clip["direction"]=="right"?14:10;case "op_eve":return 5;default:throw new InvalidDataException("No release display contract");}}
 public static void Apply(){
  if(Path.GetFullPath(Application.dataPath).Replace('\\','/')!=Root+"unity-project/Assets")throw new InvalidOperationException("Canonical Unity project required");
  var input=Manifest();CheckManifest(input);var config=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>(ConfigPath);if(config?.OperatorFrames==null)throw new InvalidOperationException("Current registry missing");
  var original=config.OperatorFrames;Backup(ConfigPath);Backup(AssetDatabase.GetAssetPath(original));Backup(RegistryPath);Directory.CreateDirectory(Destination);
  var known=new Dictionary<string,Texture2D>();
  foreach(string path in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(original),true))if(path.EndsWith(".png",StringComparison.OrdinalIgnoreCase)&&File.Exists(path)){var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture!=null&&!known.ContainsKey(Hash(path)))known.Add(Hash(path),texture);}
  var set=UnityEngine.Object.Instantiate(original);var directional=new Dictionary<string,Entry>();
  foreach(var entry in original.Entries.Concat(original.DirectionalEntries??Array.Empty<Entry>()))directional[Key(entry.OperatorId,entry.FacesRight)]=Copy(entry);
  var log=new JArray();var registrationLog=new JArray();
  foreach(var group in input["clips"].GroupBy(c=>Key((string)c["operatorId"],(string)c["direction"]=="right"))){
   var first=group.First();string id=(string)first["operatorId"];bool right=(string)first["direction"]=="right";
   var baseline=original.Find(id);if(baseline==null)throw new InvalidDataException("Unknown runtime identity "+id);
   bool existing=directional.TryGetValue(group.Key,out var entry);
   if(!existing){entry=Copy(baseline);entry.FacesRight=right;entry.IdleFrames=Array.Empty<Texture2D>();entry.DurationsMs=Array.Empty<int>();entry.BasicFrames=Array.Empty<Texture2D>();entry.BasicDurationsMs=Array.Empty<int>();entry.ActiveFrames=Array.Empty<Texture2D>();entry.ActiveDurationsMs=Array.Empty<int>();entry.UltimateFrames=Array.Empty<Texture2D>();entry.UltimateDurationsMs=Array.Empty<int>();entry.BasicProjectile=null;entry.Emblem=null;entry.BasicRegistration=entry.ActiveRegistration=entry.UltimateRegistration=null;directional.Add(group.Key,entry);}
   foreach(var clip in group.OrderBy(c=>(string)c["role"]=="idle"?0:1)){
    var registration=Registration(clip,entry,existing||entry.HasIdle,registrationLog);var rows=Exposures(clip).ToArray();var textures=rows.Select(f=>Import(f,known,log)).ToArray();var durations=rows.Select(f=>(int)f["durationMs"]).ToArray();
    switch((string)clip["role"]){
     case "idle":entry.IdleFrames=textures;entry.DurationsMs=durations;entry.CanvasSize=registration.CanvasSize;entry.FootAnchorTopLeft=registration.FootAnchorTopLeft;entry.BodySpan=registration.BodySpan;break;
     case "basic":entry.BasicFrames=textures;entry.BasicDurationsMs=durations;entry.BasicRegistration=registration;entry.BasicFireFrame=FireFrame(clip);break;
     case "active":entry.ActiveFrames=textures;entry.ActiveDurationsMs=durations;entry.ActiveRegistration=registration;break;
     case "ultimate":entry.UltimateFrames=textures;entry.UltimateDurationsMs=durations;entry.UltimateRegistration=registration;break;
     default:throw new InvalidDataException("Unsupported actor role");
    }
   }
  }
  set.Entries=original.Entries.Select(e=>directional[Key(e.OperatorId,e.FacesRight)]).ToArray();set.DirectionalEntries=directional.Values.ToArray();set.Validate();Verify(set,input,original);
  var fx=new JArray();foreach(var effect in input["independentFx"]){
   var rows=effect["frames"]??effect["textures"];if(rows==null)continue;var files=new JArray();var textures=new List<Texture2D>();
   foreach(var frame in rows){var texture=Import(frame,known,log);textures.Add(texture);files.Add(new JObject{["asset"]=AssetDatabase.GetAssetPath(texture),["sha256"]=(string)frame["sha256"],["durationMs"]=frame["durationMs"]});}
   bool connected=(string)effect["operatorId"]=="op_eve"&&effect["frames"]!=null;
   if(connected){var entry=set.Find("op_eve",(string)effect["direction"]=="right");var durations=rows.Select(f=>(int)f["durationMs"]).ToArray();if((string)effect["role"]=="active_fx"){entry.ActiveEffectFrames=textures.ToArray();entry.ActiveEffectDurationsMs=durations;}else if((string)effect["role"]=="ultimate_fx"){entry.UltimateEffectFrames=textures.ToArray();entry.UltimateEffectDurationsMs=durations;}else throw new InvalidDataException("Unknown Eve FX role");}
   fx.Add(new JObject{["operatorId"]=(string)effect["operatorId"],["direction"]=(string)effect["direction"],["role"]=(string)effect["role"],["status"]=connected?"INDEPENDENT_ACTION_OVERLAY_NOT_NATIVE_DRONE_SPAWN":"INDEPENDENT_CATALOG_ONLY_NO_RUNTIME_FX_CONSUMER",["files"]=files});
  }
  VerifyEffects(set,input);set.Validate();
  var saved=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>(RegistryPath);if(saved!=null){EditorUtility.CopySerialized(set,saved);UnityEngine.Object.DestroyImmediate(set);set=saved;EditorUtility.SetDirty(set);}else AssetDatabase.CreateAsset(set,RegistryPath);
  config.SeraCastingCrystal=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_LASTRAIL/Content/Battle/SeraFx20261110/casting-crystal.png");
  config.SeraIdleCrystal=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_LASTRAIL/Content/Battle/SeraFx20261110/idle-crystal-camera.png");
  config.SeraCrystalShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/_LASTRAIL/Content/Battle/SeraFx20261110/CrystalOcclusion.shader");
  if(config.SeraCastingCrystal==null||config.SeraIdleCrystal==null||config.SeraCrystalShader==null)throw new InvalidDataException("Sera independent FX dependencies missing");
  config.OperatorFrames=set;EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
  File.WriteAllText(Qa+"actor-import.json",new JObject{["registry"]=RegistryPath,["sources"]=log,["localRegistrations"]=registrationLog,["independentFx"]=fx,["exclusions"]=input["exclusions"],["nativeRulesChanged"]=false}.ToString());
  VerifyInstalled();
 }
 static void Verify(CombatOperatorFrames set,JObject input,CombatOperatorFrames original=null){
  set.Validate();foreach(var clip in input["clips"]){var entry=set.Find((string)clip["operatorId"],(string)clip["direction"]=="right");Texture2D[] frames;int[] durations;
   switch((string)clip["role"]){case "idle":frames=entry.IdleFrames;durations=entry.DurationsMs;break;case "basic":frames=entry.BasicFrames;durations=entry.BasicDurationsMs;break;case "active":frames=entry.ActiveFrames;durations=entry.ActiveDurationsMs;break;default:frames=entry.UltimateFrames;durations=entry.UltimateDurationsMs;break;}
   var expected=Exposures(clip).ToArray();if(frames.Length!=expected.Length)throw new InvalidDataException("Installed count differs");
   for(int i=0;i<frames.Length;i++)if(Hash(AssetDatabase.GetAssetPath(frames[i]))!=(string)expected[i]["sha256"]||durations[i]!=(int)expected[i]["durationMs"])throw new InvalidDataException("Installed exposure differs");
  }
  if(original!=null)foreach(var before in original.Entries){var after=set.Find(before.OperatorId);if(after.FacesRight!=before.FacesRight||after.Portrait!=before.Portrait||after.PortraitUv!=before.PortraitUv)throw new InvalidDataException("Default facing or portrait changed");if(before.OperatorId=="op_kyle"&&!after.ActiveFrames.SequenceEqual(before.ActiveFrames))throw new InvalidDataException("Previously authorized Kyle active lost");}
 }
 static void VerifyEffects(CombatOperatorFrames set,JObject input){foreach(var effect in input["independentFx"].Where(e=>(string)e["operatorId"]=="op_eve")){
  var entry=set.Find("op_eve",(string)effect["direction"]=="right");bool ultimate=(string)effect["role"]=="ultimate_fx";var textures=ultimate?entry.UltimateEffectFrames:entry.ActiveEffectFrames;var durations=ultimate?entry.UltimateEffectDurationsMs:entry.ActiveEffectDurationsMs;var frames=effect["frames"].ToArray();var registration=ultimate?entry.UltimateRegistration:entry.ActiveRegistration;var canvas=registration?.CanvasSize??entry.CanvasSize;
  if(textures.Length!=frames.Length||durations.Length!=frames.Length)throw new InvalidDataException("FX exposure count differs");for(int i=0;i<frames.Length;i++)if(Hash(AssetDatabase.GetAssetPath(textures[i]))!=(string)frames[i]["sha256"]||durations[i]!=(int)frames[i]["durationMs"]||textures[i].width!=canvas.x||textures[i].height!=canvas.y)throw new InvalidDataException("FX exposure or actor canvas differs");
 }}
 public static void FinalizeAndCapture(){
  VerifyInstalled();
  const string path="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryBinding.asset";
  var config=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>(ConfigPath);Backup(path);
  var baked=CombatPlayerAssetBindingBuilder.BakeConfiguration(config);var existing=AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>(path);
  if(existing!=null){EditorUtility.CopySerialized(baked,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(baked);}else{AssetDatabase.CreateAsset(baked,path);existing=baked;}
  AssetDatabase.SaveAssets();
  using(var lease=existing.Acquire(existing.ManifestHash,new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"}))File.WriteAllText(Qa+"player-binding.json",existing.ManifestJson);
  LatestApprovedArtCapture.Run();
 }
 public static void VerifyInstalled(){var input=Manifest();CheckManifest(input);var config=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>(ConfigPath);if(AssetDatabase.GetAssetPath(config.OperatorFrames)!=RegistryPath)throw new InvalidDataException("Latest registry not bound");Verify(config.OperatorFrames,input);VerifyEffects(config.OperatorFrames,input);File.WriteAllText(Qa+"actor-installed-verification.json",new JObject{["passed"]=true,["clipCount"]=input["clips"].Count(),["sourceHashesCountsTimingAndRegistryValidated"]=true,["independentEveFxTimelinesVerified"]=true,["exclusionsChecked"]=true,["visualAndPlayerVerification"]=false}.ToString());}
}
#endif
