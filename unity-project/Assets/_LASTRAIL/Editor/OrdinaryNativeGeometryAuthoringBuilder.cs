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
public static class OrdinaryNativeGeometryAuthoringBuilder {
 const string Folder="Assets/_LASTRAIL/Content/Battle/OrdinaryNativeGeometryV01/";
 const string Qa="D:/Project/LASTRAIL/QA/CommanderS1-20260922/NativeGeometry835/";
 const string Allies="Assets/_LASTRAIL/Content/Battle/OrdinaryRoofSpawnV01/OrdinaryRoofSpawnV01.prefab";
 const string Enemy="Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/OrdinaryEnemyTrainV01.prefab";
 const string Mira="Assets/_LASTRAIL/QA/MiraJudgementV1/MiraApprovedJudgementV1.prefab";
 const string EnemyArt="Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_spike_cart_gunner.png";
 static void Need(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static JArray Mm(Vector3 p)=>new JArray(Math.Round(p.x*1000,MidpointRounding.AwayFromZero),Math.Round(p.y*1000,MidpointRounding.AwayFromZero),Math.Round(p.z*1000,MidpointRounding.AwayFromZero));
 static void Wire(Transform parent,string name,Vector3[] points,Material material){var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.useWorldSpace=false;line.sharedMaterial=material;line.startWidth=line.endWidth=.035f;line.positionCount=points.Length;line.SetPositions(points);}
 static void Preview(GameObject root){
  var material=new Material(Shader.Find("Sprites/Default")){color=new Color(.2f,1,.65f)};AssetDatabase.CreateAsset(material,Folder+"CandidateWire.mat");
  foreach(var collider in root.GetComponentsInChildren<CapsuleCollider>())for(int plane=0;plane<2;plane++){
   var points=Enumerable.Range(0,65).Select(i=>{float angle=i*Mathf.PI*2/64,cos=Mathf.Cos(angle),r=collider.radius*Mathf.Sin(angle),y=cos*collider.radius+Mathf.Sign(cos)*(collider.height*.5f-collider.radius);return collider.center+new Vector3(plane==0?r:0,y,plane==1?r:0);}).ToArray();Wire(collider.transform,"CandidateCapsuleWire",points,material);
  }
  foreach(var socket in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="MuzzleCandidate_NotApproved")){
   Wire(socket,"CandidateMuzzleMarker",new[]{Vector3.left*.15f,Vector3.right*.15f},material);Wire(socket,"CandidateMuzzleHeight",new[]{Vector3.down*.15f,Vector3.up*.15f},material);
  }
 }
 static JObject Measure(Transform frame,string id){
  var collider=frame.Find("BodyCollider").GetComponent<BoxCollider>();
  var anchors=frame.Cast<Transform>().Where(t=>t.name.StartsWith("SpawnFoot_")||t.name.StartsWith("GunnerFoot_")||t.name.StartsWith("Roof")&&t.GetComponent<Renderer>()==null||t.name.StartsWith("Coupler")).ToArray();
  Need(anchors.Length>0,"Original anchors missing");
  var reader=frame.gameObject.AddComponent<BPhysicalSceneSource>();
  try{reader.Bind(id,"b-physical-authoring-millimeter-v01",1000,frame,collider,anchors.Select(t=>t.name).ToArray(),anchors);var export=reader.CaptureExport();
   var m=frame.localToWorldMatrix;var matrix=new JArray();for(int row=0;row<4;row++)for(int col=0;col<4;col++)matrix.Add(col==3&&row<3?m[row,col]*1000:m[row,col]);
   Physics.SyncTransforms();var center=collider.bounds.center;
   foreach(float side in new[]{-1f,1f}){var ray=new Ray(center+Vector3.right*side*20,-Vector3.right*side);Need(collider.Raycast(ray,out var hit,40)&&Math.Abs(hit.distance-14)<.001f,"Original 12m body ray mismatch");}
   return new JObject{["role"]=id,["sourceFrame"]=frame.name,["frameToAuthoringWorldMm"]=matrix,["measurement"]=export,["twoDirectionBodyRaycastVerified"]=true};
  }finally{UnityEngine.Object.DestroyImmediate(reader);}
 }
 static JObject Candidate(Transform foot,CapsuleCollider original,int ordinal,Vector3 muzzle){
  var host=new GameObject("GunnerGeometryCandidate_"+ordinal);host.transform.SetParent(foot,false);
  var capsule=host.AddComponent<CapsuleCollider>();capsule.center=original.center;capsule.radius=original.radius;capsule.height=original.height;capsule.direction=original.direction;
  var socket=new GameObject("MuzzleCandidate_NotApproved").transform;socket.SetParent(host.transform,false);socket.localPosition=muzzle;
  return new JObject{["role"]="enemy_spike_cart_gunner",["authoringOrdinal"]=ordinal,["footPath"]=foot.name,["physicalInstanceId"]=null,["generation"]=null,
   ["hitVolume"]=new JObject{["kind"]="CAPSULE",["centerMm"]=Mm(capsule.center),["radiusMm"]=Math.Round(capsule.radius*1000),["heightMm"]=Math.Round(capsule.height*1000),["axis"]="Y",["status"]="REUSED_ISOLATED_APPROVED_TEST_SHAPE_PENDING_FIRST_BATTLE_REGISTRATION",["sourcePrefab"]=Mira,["sourceObject"]="FIRST_TEST_GUNNER"},
   ["muzzleCandidate"]=new JObject{["localMm"]=Mm(socket.localPosition),["status"]="MANUALLY_AUTHORED_VISUAL_LANDMARK_CANDIDATE_NOT_APPROVED",["sourceAsset"]=EnemyArt,["sourceTopLeftPx"]=new JArray(676,174),["candidateFootPx"]=new JArray(370,620),["candidateBodyTopPx"]=new JArray(370,50),["scaleBasis"]="1800mm isolated test capsule height / 570 source pixels; candidate mapping, not measured art scale",["depthMm"]=0,["depthStatus"]="CANDIDATE_PLANAR_DEPTH_NOT_MEASURED"}};
 }
 static void CheckCandidates(GameObject root,JObject definition){
  Need(root.GetComponentsInChildren<CapsuleCollider>(true).Length==2,"Saved candidate count differs");
  foreach(JObject row in definition["enemyCandidates"]){
   var foot=root.transform.Find("Enemy/"+(string)row["footPath"]);Need(foot!=null,"Saved foot missing");
   var capsule=foot.GetComponentInChildren<CapsuleCollider>();Need(capsule!=null,"Saved candidate missing");
   Need(JToken.DeepEquals(Mm(capsule.center),row["hitVolume"]["centerMm"])&&Math.Round(capsule.radius*1000)==(double)row["hitVolume"]["radiusMm"]&&Math.Round(capsule.height*1000)==(double)row["hitVolume"]["heightMm"]&&capsule.direction==1,"Saved capsule differs");
   var socket=capsule.transform.Find("MuzzleCandidate_NotApproved");Need(socket!=null&&JToken.DeepEquals(Mm(socket.localPosition),row["muzzleCandidate"]["localMm"]),"Saved muzzle differs");
   Need(capsule.transform.localPosition==Vector3.zero&&capsule.transform.localRotation==Quaternion.identity&&capsule.transform.localScale==Vector3.one,"Saved candidate frame differs");
  }
 }
 public static void VerifySaved(){
  Need(Application.isBatchMode,"Batch verification required");var definition=JObject.Parse(File.ReadAllText(Folder+"definition.json"));
  var loaded=PrefabUtility.LoadPrefabContents(Folder+"OrdinaryNativeGeometryV01.prefab");try{CheckCandidates(loaded,definition);}finally{PrefabUtility.UnloadPrefabContents(loaded);}
  var scene=EditorSceneManager.OpenScene(Folder+"OrdinaryNativeGeometryV01.unity",OpenSceneMode.Single);CheckCandidates(scene.GetRootGameObjects().Single(g=>g.name=="OrdinaryNativeGeometryV01"),definition);
  foreach(var pair in (JObject)definition["sourceHashes"])Need(Hash(pair.Key)==(string)pair.Value,"Original hash differs");
  File.WriteAllText(Qa+"saved-candidate-verification.json",new JObject{["passed"]=true,["prefabAndSceneCandidateCount"]=2,["capsuleAndMuzzleLocalValuesMatch"]=true,["sourceUnchanged"]=true,["definitionSha256"]=Hash(Folder+"definition.json"),["runtimeRegistered"]=false}.ToString());
 }
 public static void Build(){
  Need(Application.isBatchMode&&!Directory.Exists(Folder),"Fresh batch authoring output required");
  Need(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"Preserve unsaved scene");
  var originals=new[]{Allies,Enemy,Mira,EnemyArt}.ToDictionary(p=>p,Hash);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var root=new GameObject("OrdinaryNativeGeometryV01");
  var allies=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Allies),scene);allies.transform.SetParent(root.transform,true);allies.name="Allies";
  var enemy=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Enemy),scene);enemy.transform.SetParent(root.transform,true);enemy.name="Enemy";
  var measurements=new JArray();foreach(string role in new[]{"Engine","Combat","Gun"})measurements.Add(Measure(allies.transform.Find(role),role));measurements.Add(Measure(enemy.transform,"Enemy"));
  var reference=PrefabUtility.LoadPrefabContents(Mira);var candidates=new JArray();JObject miraSource;
  try{var adapter=reference.GetComponent<MiraJudgementSceneAdapter>();Need(adapter!=null,"Original Mira authoring component missing");miraSource=adapter.CaptureAuthoring();var capsule=reference.GetComponentsInChildren<CapsuleCollider>(true).Single(c=>c.name=="FIRST_TEST_GUNNER");Need(capsule.direction==1&&Math.Abs(capsule.height-1.8f)<.00001f&&Math.Abs(capsule.radius-.35f)<.00001f,"Original gunner capsule changed");
   // Candidate scale is explicit; it cannot authorize runtime geometry or replace source policy.
   var point=new Vector3((676-370)*1.8f/570,(620-174)*1.8f/570,0);
   for(int i=0;i<2;i++)candidates.Add(Candidate(enemy.transform.Find("GunnerFoot_"+i),capsule,i,point));
  }finally{PrefabUtility.UnloadPrefabContents(reference);}
  var unbound=new GameObject("UnboundMiraReferenceFeet");unbound.transform.SetParent(root.transform,false);var muzzle=new GameObject("ApprovedIsolatedJudgementMuzzle_NotArtSocket").transform;muzzle.SetParent(unbound.transform,false);var raw=(JArray)miraSource["profile"]["muzzleMm"];muzzle.localPosition=new Vector3((float)raw[0],(float)raw[1],(float)raw[2])/1000;
  var normal=Mm(muzzle.localPosition);unbound.transform.localRotation=Quaternion.Euler(0,180,0);Need(Mm(muzzle.position)[0].Value<double>()==-normal[0].Value<double>()&&Mm(muzzle.position)[2].Value<double>()==-normal[2].Value<double>(),"Single yaw transform mismatch");unbound.transform.localRotation=Quaternion.identity;
  Directory.CreateDirectory(Folder);Directory.CreateDirectory(Qa);
  var definition=new JObject{["version"]="unity.ordinary-native-authoring.candidate.v1.20261031",["scope"]="AUTHORING_MEASUREMENTS_AND_EXPLICIT_CANDIDATES_NOT_REGISTERED_RUNTIME",["executionVerified"]=false,["axis"]="X_FORWARD_Y_UP_Z_SIDE",["millimetersPerUnit"]=1000,["sourceHashes"]=JObject.FromObject(originals),["bodies"]=measurements,["enemyCandidates"]=candidates,
   ["miraMuzzleReference"]=new JObject{["localMm"]=normal,["sourceProfileHash"]=miraSource["profileHash"],["sourcePrefab"]=Mira,["status"]="APPROVED_ISOLATED_TEST_ONLY_NOT_FULL5_OR_ART_SOCKET",["boundFormationSlot"]=null},
   ["missing"]=new JArray("op_kyle: current martial contact shape/origin and own hit volume","op_reina: source visual muzzle-to-world transform and own hit volume","op_eve: moving drone emitter-to-world transform and own hit volume","op_sera: three moving crystal emitters-to-world transform and own hit volume","op_mira: current art socket and own hit volume; isolated judgement origin retained","enemy gunners: candidate muzzle and first-battle capsule registration","authoritative node/attempt/physical instance generation")};
  File.WriteAllText(Folder+"definition.json",definition.ToString());Preview(root);PrefabUtility.SaveAsPrefabAsset(root,Folder+"OrdinaryNativeGeometryV01.prefab");
  var camera=new GameObject("AuthoringInspectionCamera").AddComponent<Camera>();camera.transform.position=new Vector3(22,30,-40);camera.transform.LookAt(new Vector3(22,2,0));camera.orthographic=true;camera.orthographicSize=18;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.05f,.07f);
  var light=new GameObject("AuthoringLight").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(40,-20,0);
  EditorSceneManager.SaveScene(scene,Folder+"OrdinaryNativeGeometryV01.unity");AssetDatabase.Refresh();
  var loaded=PrefabUtility.LoadPrefabContents(Folder+"OrdinaryNativeGeometryV01.prefab");try{for(int i=0;i<3;i++)Need(JToken.DeepEquals(measurements[i],Measure(loaded.transform.Find("Allies/"+new[]{"Engine","Combat","Gun"}[i]),new[]{"Engine","Combat","Gun"}[i])),"Prefab measured body differs");Need(JToken.DeepEquals(measurements[3],Measure(loaded.transform.Find("Enemy"),"Enemy")),"Enemy body reload differs");}finally{PrefabUtility.UnloadPrefabContents(loaded);}
  scene=EditorSceneManager.OpenScene(Folder+"OrdinaryNativeGeometryV01.unity",OpenSceneMode.Single);var restored=scene.GetRootGameObjects().Single(g=>g.name=="OrdinaryNativeGeometryV01");Need(JToken.DeepEquals(measurements[3],Measure(restored.transform.Find("Enemy"),"Enemy")),"Scene enemy body differs");
  camera=scene.GetRootGameObjects().Select(g=>g.GetComponent<Camera>()).Single(c=>c!=null);var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var prior=RenderTexture.active;
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Qa+"reference-only-authoring.png",image.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
  foreach(var pair in originals)Need(Hash(pair.Key)==pair.Value,"Original modified");
  File.WriteAllText(Qa+"verification.json",new JObject{["passed"]=true,["sourceUnchanged"]=true,["measuredBodies"]=4,["prefabReloaded"]=true,["sourcePacketsUseExistingMeasurementOwner"]=true,["bodyRaycastOnlyNotCombatHit"]=true,["singleYawMirrorChecked"]=true,["candidateApproval"]=false,["runtimeRegistered"]=false,["playerTested"]=false,["definitionPath"]=Folder+"definition.json",["definitionSha256"]=Hash(Folder+"definition.json")}.ToString());
 }
}
#endif
