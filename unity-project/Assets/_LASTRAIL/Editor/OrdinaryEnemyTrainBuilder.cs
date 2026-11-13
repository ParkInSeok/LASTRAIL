#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Approved spatial test asset. No actor, muzzle, damage or server identity is fabricated.
public static class OrdinaryEnemyTrainBuilder {
 const string Source="Assets/_LASTRAIL/Content/Battle/OrdinaryTrialConsistV01/Combat.prefab";
 const string Allies="Assets/_LASTRAIL/Content/Battle/OrdinaryRoofSpawnV01/OrdinaryRoofSpawnV01.prefab";
 const string Rail="Assets/_LASTRAIL/Content/Battle/OrdinaryFiniteRail1200mV01/OrdinaryFiniteRail1200mV01.prefab";
 const string Target="Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/";
 const string Qa="D:/Project/LASTRAIL/QA/CommanderS1-20260922/EnemySpatial805/";
 static void Need(bool value,string error){if(!value)throw new InvalidOperationException(error);}
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static JObject Measure(GameObject enemy,GameObject allies,GameObject rail){
  var body=enemy.transform.Find("BodyCollider").GetComponent<BoxCollider>();
  var engine=allies.transform.Find("Engine/BodyCollider").GetComponent<BoxCollider>();
  var roof=enemy.transform.Find("RoofVisual").GetComponent<MeshRenderer>();
  Need(enemy.transform.lossyScale==Vector3.one&&body.transform.lossyScale==Vector3.one,"enemy_scale");
  Need(body.size==new Vector3(12,3.2f,3),"enemy_dimensions");
  Physics.SyncTransforms();
  float rear=body.bounds.min.x,front=body.bounds.max.x,gap=rear-engine.bounds.max.x;
  Need(Math.Abs(gap-4)<.0001f,"enemy_gap");
  var feet=new JArray();
  for(int i=0;i<2;i++){
   var foot=enemy.transform.Find("GunnerFoot_"+i);float offset=i==0?2:6;
   Need(Math.Abs(foot.position.x-rear-offset)<.0001f&&Math.Abs(foot.position.z)<.0001f,"gunner_foot_horizontal");
   Need(foot.position.y>=roof.bounds.max.y&&foot.position.y-roof.bounds.max.y<=.001f,"gunner_foot_roof");
   feet.Add(new JObject{["ordinal"]=i,["arcMm"]=(foot.position.x+25)*1000,["worldYmm"]=foot.position.y*1000,["muzzle"]=null,["hitVolume"]=null});
  }
  Need(front<rail.transform.Find("Rail/RailEnd").position.x,"initial_rail_capacity");
  return new JObject{["rearArcMm"]=(rear+25)*1000,["frontArcMm"]=(front+25)*1000,["gapMm"]=gap*1000,["roofSurfaceYmm"]=roof.bounds.max.y*1000,["feet"]=feet};
 }
 public static void Build(){
  Need(Application.isBatchMode&&!Directory.Exists(Target),"fresh_batch_output_required");
  var preserved=new[]{Source,Allies,Rail}.ToDictionary(p=>p,Hash);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var allies=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Allies),scene);
  var rail=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Rail),scene);
  string alliesName=allies.name,railName=rail.name;
  var enemy=new GameObject("OrdinaryEnemyTrainV01");
  var source=PrefabUtility.LoadPrefabContents(Source);
  try{
   foreach(string name in new[]{"BodyVisual","RoofVisual","BodyCollider"}){
    var part=UnityEngine.Object.Instantiate(source.transform.Find(name).gameObject,enemy.transform,false);part.name=name;
   }
  }finally{PrefabUtility.UnloadPrefabContents(source);}
  // Arc zero is the authored rail start at world X=-25m.
  enemy.transform.position=new Vector3(43,0,0);
  var roof=enemy.transform.Find("RoofVisual").GetComponent<MeshRenderer>();
  long roofMm=(long)Math.Ceiling(roof.bounds.max.y*1000);
  for(int i=0;i<2;i++){var foot=new GameObject("GunnerFoot_"+i).transform;foot.SetParent(enemy.transform,false);foot.localPosition=new Vector3(i==0?-4:0,roofMm/1000f,0);}
  var measured=Measure(enemy,allies,rail);Directory.CreateDirectory(Target);Directory.CreateDirectory(Qa);
  var definition=new JObject{["version"]="ordinary.enemy-train-authoring.v1.20261028",["approval"]="ORDINARY-ENEMY-TRAIN-SPATIAL-v1.20261028",["classification"]="APPROVED_SPATIAL_TEST_ASSET_NOT_FINAL_ART",["lengthMm"]=12000,["widthMm"]=3000,["heightMm"]=3200,["followingGapMm"]=4000,["relativeSpeedMmPerSecond"]=0,["movementSource"]="PLAYER_ACTUAL_MOTION",["gunnerRearOffsetsMm"]=new JArray(2000,6000),["originalBodyPrefabSha256"]=preserved[Source],["measurement"]=measured,["physicalInstanceId"]=null,["gunnerModelsBound"]=false};
  File.WriteAllText(Target+"definition.json",definition.ToString());PrefabUtility.SaveAsPrefabAsset(enemy,Target+"OrdinaryEnemyTrainV01.prefab");
  var camera=new GameObject("SpatialInspectionCamera").AddComponent<Camera>();camera.transform.position=new Vector3(22,36,-45);camera.transform.LookAt(new Vector3(22,2,0));camera.orthographic=true;camera.orthographicSize=20;camera.backgroundColor=new Color(.07f,.08f,.09f);camera.clearFlags=CameraClearFlags.SolidColor;
  var light=new GameObject("SpatialInspectionLight").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-30,0);
  EditorSceneManager.SaveScene(scene,Target+"OrdinaryEnemyTrainV01.unity");AssetDatabase.Refresh();
  var saved=PrefabUtility.LoadPrefabContents(Target+"OrdinaryEnemyTrainV01.prefab");try{Need(JToken.DeepEquals(measured,Measure(saved,allies,rail)),"prefab_reload");}finally{PrefabUtility.UnloadPrefabContents(saved);}
  scene=EditorSceneManager.OpenScene(Target+"OrdinaryEnemyTrainV01.unity",OpenSceneMode.Single);
  var roots=scene.GetRootGameObjects();Need(JToken.DeepEquals(measured,Measure(roots.Single(g=>g.name=="OrdinaryEnemyTrainV01"),roots.Single(g=>g.name==alliesName),roots.Single(g=>g.name==railName))),"scene_reload");
  camera=roots.Select(g=>g.GetComponent<Camera>()).Single(c=>c!=null);
  var rt=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);var prior=RenderTexture.active;
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Qa+"reference-only-spatial.png",texture.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
  foreach(var pair in preserved)Need(Hash(pair.Key)==pair.Value,"source_changed");
  File.WriteAllText(Qa+"verification.json",new JObject{["result"]="PASSED",["measurement"]=measured,["sourceUnchanged"]=true,["prefabSceneReloaded"]=true,["actualMotionConnected"]=false,["muzzleAndLosVerified"]=false,["playerTested"]=false,["assetHashes"]=JObject.FromObject(Directory.GetFiles(Target).Where(p=>!p.EndsWith(".meta")).ToDictionary(p=>p,Hash))}.ToString());
 }
}
#endif
