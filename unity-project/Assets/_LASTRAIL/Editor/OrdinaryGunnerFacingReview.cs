using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Inspect the stored candidate in its source scene. The reference sprite exists only during this review.
public static class OrdinaryGunnerFacingReview {
 const string Candidate="Assets/_LASTRAIL/Content/Battle/OrdinaryNativeGeometryV01/";
 const string Scene=Candidate+"OrdinaryNativeGeometryV01.unity";
 const string Art="Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_spike_cart_gunner.png";
 const string MotionArt="Assets/_LASTRAIL/UI/CombatAnimations/Enemies/enemy_spike_cart_gunner/attack/enemy_spike_cart_gunner_attack_002.png";
 const string Output="D:/Project/LASTRAIL/QA/CommanderS1-20260922/GunnerFacing846/";
 static void Need(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static JArray Mm(Vector3 point)=>new JArray(Math.Round(point.x*1000,MidpointRounding.AwayFromZero),Math.Round(point.y*1000,MidpointRounding.AwayFromZero),Math.Round(point.z*1000,MidpointRounding.AwayFromZero));
 static void Capture(Camera camera,string name){var rt=RenderTexture.GetTemporary(960,720,24);var prior=RenderTexture.active;var pixels=new Texture2D(960,720,TextureFormat.RGB24,false);
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,720),0,0);pixels.Apply();File.WriteAllBytes(Output+name,pixels.EncodeToPNG());}
  finally{camera.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(pixels);}}
 public static void Run(){
  Need(Application.isBatchMode&&!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"Fresh batch and clean initial scene required");
  Directory.CreateDirectory(Output);Need(!File.Exists(Output+"facing-measurements.json"),"Preserve completed review");
  var paths=new[]{Scene,Candidate+"definition.json",Candidate+"OrdinaryNativeGeometryV01.prefab",Art,MotionArt,"Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/OrdinaryEnemyTrainV01.prefab"};var hashes=paths.ToDictionary(path=>path,Hash);
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);var root=scene.GetRootGameObjects().Single(g=>g.name=="OrdinaryNativeGeometryV01");var parent=root.transform.Find("Enemy");
  var source=JObject.Parse(File.ReadAllText(Candidate+"definition.json"));var art=AssetDatabase.LoadAssetAtPath<Texture2D>(Art);Need(art&&parent,"Original scene and artwork required");
  var comparison=new GameObject("Comparison camera").AddComponent<Camera>();comparison.clearFlags=CameraClearFlags.SolidColor;comparison.backgroundColor=new Color(.12f,.12f,.12f);comparison.orthographic=true;comparison.orthographicSize=1.37f;
  var samples=new JArray();try{for(int i=0;i<2;i++){
   var foot=parent.Find("GunnerFoot_"+i);var host=foot.Find("GunnerGeometryCandidate_"+i);var collider=host.GetComponent<CapsuleCollider>();var muzzle=host.Find("MuzzleCandidate_NotApproved");
   Need(host&&collider&&muzzle&&foot.GetComponentsInChildren<Renderer>(true).All(r=>r is LineRenderer),"Real actor renderer changed; inspect new source");
   var candidate=source["enemyCandidates"][i]["muzzleCandidate"];var pivot=candidate["candidateFootPx"];var top=candidate["candidateBodyTopPx"];
   // Reconstruct only the historical 835 comparison basis to expose its disagreement with the person.
   var overlaySprite=Sprite.Create(art,new Rect(0,0,art.width,art.height),new Vector2((float)pivot[0]/art.width,1f-(float)pivot[1]/art.height),((float)pivot[1]-(float)top[1])/collider.height);
   var overlay=new GameObject("Historical composite reference only").AddComponent<SpriteRenderer>();overlay.transform.SetParent(host,false);overlay.sprite=overlaySprite;overlay.sortingOrder=-5;
   var original=host.localRotation;try{foreach(int facing in new[]{1,-1}){
    host.localRotation=original*Quaternion.Euler(0,facing>0?0:180,0);Physics.SyncTransforms();var anchor=Mm(foot.InverseTransformPoint(muzzle.position));var center=Mm(foot.InverseTransformPoint(collider.transform.TransformPoint(collider.center)));
    Need((long)anchor[0]==facing*(long)candidate["localMm"][0]&&(long)anchor[1]==(long)candidate["localMm"][1],"Parent-space muzzle differs from original candidate");
    var row=new JObject{["foot"]=foot.name,["facingX"]=facing,["authoringYawDegrees"]=facing>0?0:180,["muzzleFootLocalMm"]=anchor,["muzzleEnemyBodyLocalMm"]=Mm(parent.InverseTransformPoint(muzzle.position)),["muzzleWorldMm"]=Mm(muzzle.position),["capsuleCenterFootLocalMm"]=center,["capsuleRadiusMm"]=Math.Round(collider.radius*1000),["capsuleHeightMm"]=Math.Round(collider.height*1000),["capsuleAxis"]=collider.direction};
    if(i==0){comparison.transform.position=foot.position+new Vector3(0,.85f,-8);comparison.transform.rotation=Quaternion.identity;var name=facing>0?"right":"left";overlay.enabled=false;Capture(comparison,name+"-source-scene.png");overlay.enabled=true;Capture(comparison,name+"-historical-overlay.png");row["sourceSceneImage"]=name+"-source-scene.png";row["referenceComparisonImage"]=name+"-historical-overlay.png";}samples.Add(row);
   }}finally{host.localRotation=original;UnityEngine.Object.DestroyImmediate(overlay.gameObject);UnityEngine.Object.DestroyImmediate(overlaySprite);}
  }}finally{UnityEngine.Object.DestroyImmediate(comparison.gameObject);}
  foreach(var pair in hashes)Need(pair.Value==Hash(pair.Key),"Source bytes changed during review");
  File.WriteAllText(Output+"facing-measurements.json",new JObject{["scope"]="TWO_AUTHORED_FACINGS_OF_EXISTING_CANDIDATE_SCENE_NOT_LIVE_ACTOR_POSES",["sourceHashes"]=JObject.FromObject(hashes),["millimetersPerUnit"]=1000,["actorOrWeaponRendererCount"]=0,["samples"]=samples,["referenceMapping"]="835 candidate foot/body landmark ratio, reproduced only for comparison",["physicalActorAnimationTick"]=JValue.CreateNull(),["actualActorHitVolume"]=JValue.CreateNull(),["registered"]=false,["gameplayVerified"]=false}.ToString());
 }
}
