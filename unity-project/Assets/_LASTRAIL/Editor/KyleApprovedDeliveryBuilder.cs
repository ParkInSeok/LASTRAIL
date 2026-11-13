#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation.Battle;
namespace LastRail.Editor {
 // Scoped import of approved bytes only. Never invokes the all-character foundation builder.
 public static class KyleApprovedDeliveryBuilder {
  public const string Root="Assets/_LASTRAIL/UI/CombatAnimations/KyleDeliveryV01";
  const string Delivery="D:/Project/LASTRAIL/QA/DirectionImpact-20260921/eve-combat-v01/kyle-delivery-v01/";
  const string ApprovedZip="943db4ac6c6240d97b54150fbc0d4a6e9630daa3546a03e8b5672bff4012aa1d";
  static string Hash(byte[] bytes){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  static void Need(bool ok){if(!ok)throw new InvalidDataException("Kyle approved delivery mismatch");}
  [MenuItem("LASTRAIL/Approved Art/Import Kyle Delivery V01 Only")]
  public static void Build(){
   var manifestBytes=File.ReadAllBytes(Delivery+"manifest.json");var m=JObject.Parse(System.Text.Encoding.UTF8.GetString(manifestBytes));Need((string)m["version"]=="kyle-delivery-v01"&&(string)m["character"]=="op_kyle");
   var identity=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/Content/RemoteResources/OperatorIdentity/catalog.json"));Need(identity.Descendants().OfType<JProperty>().Any(x=>x.Name=="operatorId"&&(string)x.Value=="op_kyle"));
   var zipBytes=File.ReadAllBytes(Delivery+"kyle-approved-resources-v01.zip");Need(Hash(zipBytes)==ApprovedZip);
   using var memory=new MemoryStream(zipBytes);using var zip=new ZipArchive(memory,ZipArchiveMode.Read);
   Need(zip.Entries.Count==111&&zip.Entries.Select(e=>e.FullName).Distinct().Count()==111);
   // Validate complete portable inventory before writing/importing any asset.
   foreach(var row in (JArray)m["inventory"]){string path=(string)row["path"];Need(!path.Contains("..")&&!Path.IsPathRooted(path));var e=zip.GetEntry(path);Need(e!=null);using var stream=e.Open();using var bytes=new MemoryStream();stream.CopyTo(bytes);Need(Hash(bytes.ToArray())==(string)row["sha256"]&&bytes.Length==(long)row["bytes"]);}
   using(var stream=zip.GetEntry("manifest.json").Open()){using var bytes=new MemoryStream();stream.CopyTo(bytes);Need(bytes.ToArray().SequenceEqual(manifestBytes));}
   Directory.CreateDirectory(Root+"/Sources");Directory.CreateDirectory(Root+"/Clips");Directory.CreateDirectory(Root+"/Controllers");
   foreach(var row in (JArray)m["inventory"]){string path=(string)row["path"];if(!(path.StartsWith("actors/",StringComparison.Ordinal)||path.StartsWith("fx/tool/",StringComparison.Ordinal))||!path.EndsWith(".png",StringComparison.Ordinal))continue;
    string target=Root+"/Sources/"+path;Directory.CreateDirectory(Path.GetDirectoryName(target));using var stream=zip.GetEntry(path).Open();using var bytes=new MemoryStream();stream.CopyTo(bytes);var original=bytes.ToArray();if(File.Exists(target))Need(File.ReadAllBytes(target).SequenceEqual(original));else File.WriteAllBytes(target,original);
   }
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   foreach(var path in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Sources/fx/tool"}).Select(AssetDatabase.GUIDToAssetPath))Configure(path,new Vector2(.5f,.5f));
   var fixture=new GameObject("Kyle approved four-direction fixture",typeof(RectTransform));var map=new JArray();
   try{
    foreach(var raw in (JArray)m["directions"]){var d=(JObject)raw;int direction=(int)d["direction"];var foot=d["foot_anchor_normalized"];var pivot=new Vector2((float)foot[0],1-(float)foot[1]);var canvas=d["canvas"];float width=(float)canvas[0],height=(float)canvas[1];float visibleHeight=(float)d["idle_bbox_alpha128"][3]-(float)d["idle_bbox_alpha128"][1];float scale=(float)m["review_actor_height_scene_px"]/visibleHeight;
     foreach(string motion in new[]{"idle","attack"})for(int n=1;n<=12;n++)Configure(Root+"/Sources/actors/d"+direction+"/"+motion+"-"+n.ToString("00")+".png",pivot);
     var go=new GameObject("Direction"+direction,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Animator));go.transform.SetParent(fixture.transform,false);var rect=go.GetComponent<RectTransform>();rect.pivot=pivot;rect.sizeDelta=new Vector2(width*scale,height*scale);rect.anchoredPosition=new Vector2((direction-2.5f)*120,0);go.GetComponent<Image>().raycastTarget=false;go.GetComponent<Image>().sprite=SpriteAt(direction,"idle",1);
     string controllerPath=Root+"/Controllers/d"+direction+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
     foreach(string motion in new[]{"idle","attack"}){var clip=Clip(direction,motion,(JArray)m[motion+"_durations_ms"]);var state=machine.AddState(motion);state.motion=clip;if(motion=="idle")machine.defaultState=state;}go.GetComponent<Animator>().runtimeAnimatorController=controller;
     float mx=(float)d["projectile"]["muzzle"][0],my=(float)d["projectile"]["muzzle"][1];var presentation=go.AddComponent<KyleApprovedActorView>();presentation.Configure(go.GetComponent<Image>(),Enumerable.Range(1,12).Select(n=>SpriteAt(direction,"idle",n)).ToArray(),Enumerable.Range(1,12).Select(n=>SpriteAt(direction,"attack",n)).ToArray(),((JArray)m["idle_durations_ms"]).Select(n=>(int)n).ToArray(),((JArray)m["attack_durations_ms"]).Select(n=>(int)n).ToArray(),AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sources/fx/tool/projectile.png"),Enumerable.Range(1,8).Select(n=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sources/fx/tool/impact/"+n.ToString("000")+".png")).ToArray(),(KyleFacing)direction,new Vector2((mx-(float)foot[0])*width*scale,((float)foot[1]-my)*height*scale),-(float)d["projectile"]["angle_deg"]);map.Add(new JObject{["direction"]=direction,["semantic"]=new[]{"FrontOpponentCarriage","SideOpponentCarriage","RearOpponentCarriage","TrainForward"}[direction-1],["pivotYUp"]=new JArray(pivot.x,pivot.y),["canvasDisplaySize"]=new JArray(width*scale,height*scale),["muzzleLocalYUp"]=new JArray((mx-(float)foot[0])*width*scale,((float)foot[1]-my)*height*scale),["rotationYUpDegrees"]=-(float)d["projectile"]["angle_deg"],["displayHeight"]=m["review_actor_height_scene_px"].DeepClone()});
    }
    foreach(var path in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Sources/fx/tool"}).Select(AssetDatabase.GUIDToAssetPath))Configure(path,new Vector2(.5f,.5f));
    PrefabUtility.SaveAsPrefabAsset(fixture,Root+"/KyleFourDirectionFixture.prefab");
    File.WriteAllText(Root+"/presentation-binding.json",new JObject{["version"]="kyle-delivery-v01",["operatorId"]="op_kyle",["sourceZipSha256"]=ApprovedZip,["directionAdapter"]=map,["fireAttackMs"]=380,["attackDurationMs"]=1040,["idleDurationMs"]=1440,["projectile"]="Sources/fx/tool/projectile.png",["impactFrames"]=new JArray(Enumerable.Range(1,8).Select(n=>"Sources/fx/tool/impact/"+n.ToString("000")+".png")),["flash"]="Independent KyleMuzzleFlashGraphic port; original Canvas reference preserved, visual comparison pending",["scope"]="IMPORT_AND_EXPLICIT_2D_FIXTURE_NOT_GAMEPLAY_CONNECTION"}.ToString());
    AssetDatabase.SaveAssets();AssetDatabase.Refresh();
   }finally{UnityEngine.Object.DestroyImmediate(fixture);}
  }
  static Sprite SpriteAt(int direction,string motion,int frame)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sources/actors/d"+direction+"/"+motion+"-"+frame.ToString("00")+".png");
  static void Configure(string path,Vector2 pivot){var i=(TextureImporter)AssetImporter.GetAtPath(path);Need(i!=null);var current=new TextureImporterSettings();i.ReadTextureSettings(current);if(i.textureType==TextureImporterType.Sprite&&i.spriteImportMode==SpriteImportMode.Single&&i.spritePixelsPerUnit==100&&i.alphaIsTransparency&&i.sRGBTexture&&!i.mipmapEnabled&&i.wrapMode==TextureWrapMode.Clamp&&i.filterMode==FilterMode.Bilinear&&i.maxTextureSize==2048&&i.textureCompression==TextureImporterCompression.Uncompressed&&current.spriteAlignment==(int)SpriteAlignment.Custom&&current.spritePivot==pivot&&current.spriteMeshType==SpriteMeshType.FullRect)return;i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=100;i.alphaIsTransparency=true;i.sRGBTexture=true;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.filterMode=FilterMode.Bilinear;i.maxTextureSize=2048;i.textureCompression=TextureImporterCompression.Uncompressed;var settings=new TextureImporterSettings();i.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(settings);i.SaveAndReimport();}
  static AnimationClip Clip(int direction,string motion,JArray durations){Need(durations.Count==12);string path=Root+"/Clips/d"+direction+"_"+motion+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip==null){clip=new AnimationClip{name="Kyle_d"+direction+"_"+motion};AssetDatabase.CreateAsset(clip,path);}clip.frameRate=1000;var keys=new ObjectReferenceKeyframe[13];int elapsedMs=0;for(int n=0;n<12;n++){keys[n]=new ObjectReferenceKeyframe{time=elapsedMs/1000f,value=SpriteAt(direction,motion,n+1)};elapsedMs+=(int)durations[n];}// Unity includes one PPtr sample after its final key. Keep last exposure through the approved end.
keys[12]=new ObjectReferenceKeyframe{time=(elapsedMs-1)/1000f,value=SpriteAt(direction,motion,12)};AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(Image),"m_Sprite"),keys);var config=AnimationUtility.GetAnimationClipSettings(clip);config.loopTime=motion=="idle";AnimationUtility.SetAnimationClipSettings(clip,config);EditorUtility.SetDirty(clip);return clip;}
 }
}
#endif