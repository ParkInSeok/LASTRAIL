#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Independent finite rail asset; runtime identity, threats and consist placement are not inferred.
public static class OrdinaryFiniteRailBuilder {
 const string Source="Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoring12mV01/";
 const string Target="Assets/_LASTRAIL/Content/Battle/OrdinaryFiniteRail500mV01/";
 const string Qa="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/FiniteRail795/";
 static void Need(bool value,string error){if(!value)throw new InvalidOperationException(error);}
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static JArray V(Vector3 p)=>new JArray(p.x,p.y,p.z);
 public static void InspectDetails()=>InspectDetails(500,Target,Qa);
 public static void Inspect1200Details()=>InspectDetails(1200,"Assets/_LASTRAIL/Content/Battle/OrdinaryFiniteRail1200mV01/","D:/Project/LASTRAIL/QA/OperatorInformation-20261019/PlayerBudget798/");
 static void InspectDetails(int length,string target,string qa){
  Need(Application.isBatchMode,"inspection_requires_batch");
  var scene=EditorSceneManager.OpenScene(target+"OrdinaryFiniteRail"+length+"mV01.unity",OpenSceneMode.Single);
  var camera=scene.GetRootGameObjects().Select(g=>g.GetComponent<Camera>()).Single(c=>c!=null);
  foreach(var end in new[]{false,true}){
   float x=end?length-30:-20;camera.transform.position=new Vector3(x,15,-15);camera.transform.LookAt(new Vector3(x,0,0));camera.orthographicSize=12;
   var rt=new RenderTexture(1200,800,24);var texture=new Texture2D(1200,800,TextureFormat.RGB24,false);var prior=RenderTexture.active;
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1200,800),0,0);texture.Apply();File.WriteAllBytes(qa+(end?"reference-only-end.png":"reference-only-start.png"),texture.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
  }
 }
 static JObject Measure(GameObject root,int length=500){
  Need(root.transform.lossyScale==Vector3.one,"root_scale");var rail=root.transform.Find("Rail");Need(rail.lossyScale==Vector3.one,"rail_parent_scale");
  var start=rail.Find("RailStart").position;var end=rail.Find("RailEnd").position;
  Need(start==new Vector3(-25,0,0)&&end==new Vector3(length-25,0,0),"endpoints");
  var rows=new JArray();foreach(int side in new[]{-1,1}){var t=rail.Find("Rail_"+side);var collider=t.GetComponent<BoxCollider>();var visual=t.GetComponent<Renderer>();
   Need(t.localScale==new Vector3(length,.15f,.12f)&&t.position==new Vector3(length/2f-25,-.1f,side),"rail_shape");
   Need(collider&&collider.size==Vector3.one&&collider.center==Vector3.zero,"collider_shape");
   var min=t.TransformPoint(new Vector3(-.5f,-.5f,-.5f));var max=t.TransformPoint(new Vector3(.5f,.5f,.5f));
   Need(Math.Abs(min.x-start.x)<.00001f&&Math.Abs(max.x-end.x)<.00001f,"collider_endpoint");
   visual.enabled=false;Need(collider.enabled,"visual_collider_coupling");visual.enabled=true;
   rows.Add(new JObject{["path"]="Rail/"+t.name,["minimumMm"]=V(min*1000),["maximumMm"]=V(max*1000)});
  }
  Need(root.transform.Find("WarningFrame").position==start,"warning_frame");Need(root.transform.Find("WarningFrame").childCount==0,"unissued_threat");
  return new JObject{["rootScale"]=V(root.transform.lossyScale),["millimetersPerUnit"]=1000,["axis"]="X_FORWARD_Y_UP_Z_SIDE",["startMm"]=V(start*1000),["endMm"]=V(end*1000),["lengthMm"]=(end.x-start.x)*1000,["collisionBounds"]=rows,["warningFrameOriginMm"]=V(start*1000),["issuedHazardCount"]=0};
 }
 public static void Build()=>BuildVariant(500,Target,Qa);
 public static void Build1200()=>BuildVariant(1200,"Assets/_LASTRAIL/Content/Battle/OrdinaryFiniteRail1200mV01/","D:/Project/LASTRAIL/QA/OperatorInformation-20261019/PlayerBudget798/");
 static void BuildVariant(int length,string Target,string Qa){
  string assetName="OrdinaryFiniteRail"+length+"mV01";float center=length/2f-25;
  Need(!Directory.Exists(Target),"preserve_existing_rail");Directory.CreateDirectory(Qa);
  var before=Directory.GetFiles(Source).Concat(length==1200?Directory.GetFiles(OrdinaryFiniteRailBuilder.Target):Array.Empty<string>()).ToDictionary(p=>p,Hash);
  var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var mode=NewSceneMode.Additive;
  if(string.IsNullOrEmpty(active.path)){Need(Application.isBatchMode&&!active.isDirty&&active.rootCount==0,"preserve_unsaved_scene");mode=NewSceneMode.Single;}
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,mode);
  UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
  var original=PrefabUtility.LoadPrefabContents(Source+"BPhysicalAuthoring12mV01.prefab");GameObject rail;
  try{rail=UnityEngine.Object.Instantiate(original.transform.Find("Rail").gameObject);rail.name="Rail";UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rail,scene);}finally{PrefabUtility.UnloadPrefabContents(original);}
  var root=new GameObject(assetName);rail.transform.SetParent(root.transform,false);
  rail.transform.Find("RailEnd").localPosition=new Vector3(length-25,0,0);
  foreach(int side in new[]{-1,1}){var t=rail.transform.Find("Rail_"+side);t.localPosition=new Vector3(center,-.1f,side);t.localScale=new Vector3(length,.15f,.12f);t.gameObject.AddComponent<BoxCollider>();}
  var warning=new GameObject("WarningFrame").transform;warning.SetParent(root.transform,false);warning.localPosition=new Vector3(-25,0,0);
  var measurement=Measure(root,length);Directory.CreateDirectory(Target);
  var definition=new JObject{["version"]="ordinary.finite-rail-authoring.v1.20261027",["approval"]="ORDINARY-FINITE-RAIL-CAPACITY-v1.20261027",["status"]="AUTHORED_ASSET_NOT_START_BOUND",["measurement"]=measurement,["railSegmentId"]=JValue.CreateNull(),["runGeometryBinding"]=JValue.CreateNull(),["initialConsistPosition"]=JValue.CreateNull(),["capacityPolicy"]=new JObject{["minimumMm"]=500000,["tailMarginMm"]=20000,["forecastMs"]=10000,["endMarginMm"]=20000,["requiresOriginalConsistAndSpeedEffects"]=true,["requiresPreWarningTravelBudget"]=true},["warningFramePath"]="WarningFrame",["sourcePrefabSha256"]=Hash(Source+"BPhysicalAuthoring12mV01.prefab")};
  if(length==1200){definition["version"]="ordinary.finite-rail-authoring.v2.20261028";definition["approval"]="ORDINARY-FIRST-PLAYER-TRAVEL-BUDGET-v1.20261028";definition["capacityPolicy"]["minimumMm"]=1200000;definition["capacityPolicy"]["qaObservationMs"]=42000;definition["capacityPolicy"]["productTimeLimit"]=false;}
  File.WriteAllText(Target+"definition.json",definition.ToString());PrefabUtility.SaveAsPrefabAsset(root,Target+assetName+".prefab");
  var camera=new GameObject("AuthoringCamera").AddComponent<Camera>();camera.transform.position=new Vector3(center,240,-180);camera.transform.LookAt(new Vector3(center,0,0));camera.orthographic=true;camera.orthographicSize=length*.32f;camera.farClipPlane=1200;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.08f,.09f);
  new GameObject("AuthoringLight").AddComponent<Light>().type=LightType.Directional;
  EditorSceneManager.SaveScene(scene,Target+assetName+".unity");AssetDatabase.Refresh();
  var prefab=PrefabUtility.LoadPrefabContents(Target+assetName+".prefab");try{Need(JToken.DeepEquals(Measure(prefab,length),measurement),"prefab_reload");}finally{PrefabUtility.UnloadPrefabContents(prefab);}
  var reopened=EditorSceneManager.OpenScene(Target+assetName+".unity",OpenSceneMode.Single);
  try{Need(JToken.DeepEquals(Measure(reopened.GetRootGameObjects().Single(g=>g.name==assetName),length),measurement),"scene_reload");camera=reopened.GetRootGameObjects().Select(g=>g.GetComponent<Camera>()).Single(c=>c!=null);
   var rt=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);var prior=RenderTexture.active;
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Qa+"reference-only-preview.png",texture.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
  }finally{EditorSceneManager.CloseScene(reopened,true);}
  foreach(var p in before)Need(Hash(p.Key)==p.Value,"source_modified");
  File.WriteAllText(Qa+"verification.json",new JObject{["result"]="PASSED",["measurement"]=measurement,["sourceUnchanged"]=true,["prefabSceneReloadEqual"]=true,["assetHashes"]=JObject.FromObject(Directory.GetFiles(Target).Where(p=>!p.EndsWith(".meta")).ToDictionary(p=>p,Hash)),["runtimeCapacityVerified"]=false,["playerTested"]=false,["missing"]=new JArray("ACTUAL_CONSIST_LENGTH_AND_ID_MAPPING","RUN_SPEED_EFFECTS_AND_PRE_WARNING_BUDGET","START_GEOMETRY_BINDING")}.ToString());
 }
}
#endif
