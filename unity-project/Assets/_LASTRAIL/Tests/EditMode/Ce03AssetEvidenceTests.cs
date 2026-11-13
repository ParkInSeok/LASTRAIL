using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace LastRail.Tests.EditMode {public sealed class Ce03AssetEvidenceTests {
 [Test,Explicit("Local Direction evidence import verification, not battle readiness.")]
 public void DirectionEvidenceAssetsLoadWithPinnedHashes(){
  var source="D:/Project/LASTRAIL/WorkspaceRepo/docs/ops/ce03-content-20260911/ce03-minimum-operation-manifest.json";
  string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
  Assert.That(Hash(source),Is.EqualTo("034115a76319ed4abd8c21593a6533440a4d0dbe9cd62da182ac6c711e94b5a2"));var data=JObject.Parse(File.ReadAllText(source));var rows=(JArray)data["assets"];Assert.That(rows.Count,Is.EqualTo(227));int sprites=0,clips=0,controllers=0;
  foreach(var item in rows){string path=(string)item["path"];Assert.That(Hash(path),Is.EqualTo((string)item["sha256"]),path);Assert.That(Hash(path+".meta"),Is.EqualTo((string)item["metaSha256"]),path+".meta");
   var relative=path.Replace('\\','/');var prefix=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).Replace('\\','/')+"/";Assert.That(relative.StartsWith(prefix,StringComparison.OrdinalIgnoreCase),Is.True);relative=relative.Substring(prefix.Length);
   if(relative.StartsWith("Library/PackageCache/")){continue;}
   Assert.That(AssetDatabase.AssetPathToGUID(relative),Is.EqualTo((string)item["guid"]));
   if(path.EndsWith(".png")){var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(relative);Assert.That(sprite,Is.Not.Null,path);Assert.That(sprite.texture.width,Is.GreaterThan(0));sprites++;}
   else if(path.EndsWith(".anim")){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(relative);Assert.That(clip,Is.Not.Null,path);foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)){var values=AnimationUtility.GetObjectReferenceCurve(clip,binding);Assert.That(values.All(x=>x.value!=null),Is.True,path);}clips++;}
   else if(path.EndsWith(".controller")){Assert.That(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(relative),Is.Not.Null,path);controllers++;}
   else Assert.That(AssetDatabase.LoadMainAssetAtPath(relative),Is.Not.Null,path);
  }
  Assert.That(sprites,Is.EqualTo(178));Assert.That(clips,Is.EqualTo(40));Assert.That(controllers,Is.EqualTo(8));
  File.WriteAllText(Path.Combine(Application.dataPath,"../QA/ScreenIntegration/ce03-asset-load-verification.json"),new JObject{["sourceRawSha256"]=Hash(source),["files"]=rows.Count,["spritesLoaded"]=sprites,["clipsLoaded"]=clips,["controllersLoaded"]=controllers,["actualBattleReady"]=false}.ToString());
 }
} }
