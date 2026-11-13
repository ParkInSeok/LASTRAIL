#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class OrdinaryRoofSpawnBuilder {
 const string Original="Assets/_LASTRAIL/Content/Battle/OrdinaryTrialConsistV01/";
 const string Target="Assets/_LASTRAIL/Content/Battle/OrdinaryRoofSpawnV01/";
 const string Qa="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/RailRoof797/";
 static void Need(bool v,string error){if(!v)throw new InvalidOperationException(error);}
 static string Hash(string p){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
 static void Check(GameObject root,JArray rows){
  Need(rows.Count==5&&rows.Select(r=>(string)r["operatorId"]).Distinct().Count()==5,"five_unique_actors");
  foreach(var row in rows){var foot=root.transform.Find((string)row["anchorPath"]);Need(foot,"missing_foot");var mm=(JArray)row["localPositionMm"];var expected=new Vector3((float)mm[0]/1000,(float)mm[1]/1000,(float)mm[2]/1000);Need(Vector3.Distance(foot.localPosition,expected)<.00001f,"foot_mismatch");var frame=foot.parent;var roof=frame.Find("RoofVisual");var surface=roof.localPosition.y+roof.localScale.y*roof.GetComponent<MeshFilter>().sharedMesh.bounds.max.y;Need(foot.localPosition.y>=surface&&foot.localPosition.y-surface<=.001f,"foot_surface");Need(Math.Abs(foot.localPosition.x)<=3&&foot.localPosition.z==0&&frame.lossyScale==Vector3.one,"roof_bounds");}
  Need(root.transform.Find("Engine").Cast<Transform>().All(t=>!t.name.StartsWith("SpawnFoot_")),"engine_deployment_forbidden");
 }
 public static void Build(){
  Need(Application.isBatchMode&&!Directory.Exists(Target),"fresh_output_required");
  var originalHashes=Directory.GetFiles(Original).ToDictionary(p=>p,Hash);var fixture=JObject.Parse(File.ReadAllText(Qa+"source.json"));var start=OrdinaryFiveStartInput.Read((JObject)fixture["originalStart"],(string)fixture["accountId"],(string)fixture["runId"]);Need(start.Rail?.GeometryBound==true,"bound_rail_required");
  var scene=EditorSceneManager.OpenScene(Original+"OrdinaryTrialConsistV01.unity",OpenSceneMode.Single);var root=scene.GetRootGameObjects().Single(g=>g.name=="OrdinaryTrialConsistV01");root.name="OrdinaryRoofSpawnV01";
  var slots=new JArray();var example=new JArray();var ordered=start.Actors.OrderBy(a=>a.Slot).ToArray();
  for(int i=0;i<5;i++){
   int order=i<3?1:2;string role=i<3?"Combat":"Gun";var frame=root.transform.Find(role);var roof=frame.Find("RoofVisual");float surface=roof.localPosition.y+roof.localScale.y*roof.GetComponent<MeshFilter>().sharedMesh.bounds.max.y;
   long y=(long)Math.Round(surface*1000,MidpointRounding.AwayFromZero);float x=i<3?3-i*3:i==3?3:-3;
   var foot=new GameObject("SpawnFoot_"+i).transform;foot.SetParent(frame,false);foot.localPosition=new Vector3(x,y/1000f,0);
   slots.Add(new JObject{["formationSlot"]=i,["vehicleOrder"]=order,["role"]=role,["anchorPath"]=role+"/"+foot.name,["localPositionMm"]=new JArray((long)(x*1000),y,0),["measuredRoofSurfaceLocalYmm"]=surface*1000,["quantization"]="NEAREST_MM_MIDPOINT_AWAY_FROM_ZERO"});
   var mapping=(JObject)slots.Last.DeepClone();mapping["operatorId"]=ordered[i].OperatorId;mapping["physicalInstanceId"]=start.Rail.PhysicalInstanceIds[order];example.Add(mapping);
  }
  Check(root,example);Directory.CreateDirectory(Target);
  var definition=new JObject{["version"]="ordinary.roof-spawn-authoring.v1.20261027",["approval"]="ORDINARY-FIVE-ROOF-THREE-TWO-v1.20261027",["physicalDefinitionHash"]=OrdinaryRailSource.ConsistHash,["millimetersPerUnit"]=1000,["slots"]=slots,["existingExplicitPlacementAndProgressTakePriority"]=true,["newMovementPermission"]=false,["engineDeployment"]=false};
  File.WriteAllText(Target+"definition.json",definition.ToString());PrefabUtility.SaveAsPrefabAsset(root,Target+"OrdinaryRoofSpawnV01.prefab");EditorSceneManager.SaveScene(scene,Target+"OrdinaryRoofSpawnV01.unity");AssetDatabase.Refresh();
  var saved=PrefabUtility.LoadPrefabContents(Target+"OrdinaryRoofSpawnV01.prefab");try{Check(saved,example);}finally{PrefabUtility.UnloadPrefabContents(saved);}
  scene=EditorSceneManager.OpenScene(Target+"OrdinaryRoofSpawnV01.unity",OpenSceneMode.Single);root=scene.GetRootGameObjects().Single(g=>g.name=="OrdinaryRoofSpawnV01");Check(root,example);
  // Temporary reference markers are not saved as actors or shipped in the prefab/scene.
  var markers=new GameObject("ReferenceOnlyFootMarkers");foreach(var row in example){var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.transform.SetParent(markers.transform);sphere.transform.position=root.transform.Find((string)row["anchorPath"]).position+Vector3.up*.25f;sphere.transform.localScale=Vector3.one*.5f;UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());}
  var camera=scene.GetRootGameObjects().Select(g=>g.GetComponent<Camera>()).Single(c=>c!=null);var rt=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Qa+"reference-only-roof.png",texture.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(markers);}
  foreach(var p in originalHashes)Need(Hash(p.Key)==p.Value,"original_changed");
  File.WriteAllText(Qa+"roof-verification.json",new JObject{["result"]="PASSED",["sourceUnchanged"]=true,["savedPrefabAndSceneChecked"]=true,["fixtureSha256"]=Hash(Qa+"source.json"),["railSourceHash"]=start.Rail.SourceHash,["exampleOriginalActorMapping"]=example,["mappingAuthority"]="AUTHORING_EXAMPLE_SERVER_ISSUANCE_REQUIRED",["assetHashes"]=JObject.FromObject(Directory.GetFiles(Target).Where(p=>!p.EndsWith(".meta")).ToDictionary(p=>p,Hash)),["actualActorWeaponAndDeviceClearanceTested"]=false,["playerTested"]=false}.ToString());
 }
}
#endif
