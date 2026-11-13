#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// New local judgement geometry, explicitly authored independently of character art calibration.
public static class OrdinaryLiveGeometryBuilder {
 const string Folder="Assets/_LASTRAIL/Content/Battle/OrdinaryLiveGeometryV02/";
 const string Qa="D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/live-geometry/v02/";
 const string Allies="Assets/_LASTRAIL/Content/Battle/OrdinaryRoofSpawnV01/OrdinaryRoofSpawnV01.prefab";
 const string Enemy="Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/OrdinaryEnemyTrainV01.prefab";
 const string Rail="Assets/_LASTRAIL/Content/Battle/OrdinaryFiniteRail1200mV01/OrdinaryFiniteRail1200mV01.prefab";
 const string Prefab=Folder+"OrdinaryLiveGeometryV02.prefab", Scene=Folder+"OrdinaryLiveGeometryV02.unity";
 const string Scope="LOCAL_AUTHORED_JUDGEMENT_GEOMETRY_NOT_APPROVED_ART_CALIBRATION";
 const long InitialFront=58000;
 static void Need(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static long Mm(float v)=>checked((long)Math.Round(v*1000d,MidpointRounding.AwayFromZero));
 static JArray Point(Vector3 v)=>new JArray(Mm(v.x),Mm(v.y),Mm(v.z));
 static JArray Direction(Vector3 v)=>new JArray(v.x,v.y,v.z);
 static Transform Child(Transform parent,string name,Vector3 point){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=point;return t;}
 static string PathOf(Transform frame){return frame.parent==null?frame.name:PathOf(frame.parent)+"/"+frame.name;}
 static JObject Anchor(Transform frame,Transform point,string role,string ability=null){var row=new JObject{["id"]=point.name,["role"]=role,["positionMm"]=Point(frame.InverseTransformPoint(point.position))};if(ability!=null)row["abilityId"]=ability;return row;}
 static JObject Shape(Transform frame,Collider collider,string role){
  Need(collider.transform==frame||collider.transform.parent==frame,"Unexpected local shape hierarchy");
  Need(collider.transform.rotation==frame.rotation&&collider.transform.lossyScale==Vector3.one,"Unsupported local shape scale/rotation");
  JObject row;
  if(collider is BoxCollider box)row=new JObject{["kind"]="BOX",["centerMm"]=Point(frame.InverseTransformPoint(box.transform.TransformPoint(box.center))),["sizeMm"]=Point(box.size)};
  else if(collider is CapsuleCollider capsule){Need(capsule.direction==1,"Y capsule required");row=new JObject{["kind"]="CAPSULE_Y",["centerMm"]=Point(frame.InverseTransformPoint(capsule.transform.TransformPoint(capsule.center))),["radiusMm"]=Mm(capsule.radius),["heightMm"]=Mm(capsule.height)};}
  else throw new NotSupportedException("BOX or Y capsule required");
  row["id"]=collider.name;row["role"]=role;return row;
 }
 static JObject Entity(Transform frame,string id,string role,string definition,string asset,JArray anchors,JArray shapes)=>new JObject{
  ["authoringId"]=id,["role"]=role,["definitionRef"]=definition,["framePath"]=PathOf(frame),["assetPath"]=asset,["assetHash"]=Hash(asset),["anchors"]=anchors,["shapes"]=shapes};
 static JObject Layout(Transform root,Transform frame){var p=root.InverseTransformPoint(frame.position);var relative=p;relative.x-=InitialFront/1000f;return new JObject{
  ["originMm"]=Point(p),["basis"]=new JArray(Direction(root.InverseTransformDirection(frame.right)),Direction(root.InverseTransformDirection(frame.up)),Direction(root.InverseTransformDirection(frame.forward))),["relativeToInitialFrontMm"]=Point(relative)};}
 internal static void AuthorKyleContact(Transform actor){Need(actor.name=="op_kyle"&&actor.Find("FistContactBasic04")==null,"Fresh Kyle contact required");Child(actor,"FistContactBasic04",new Vector3(.609f,1.310f,0));}
 internal static JObject ExportKyleContact(Transform actor)=>Anchor(actor,actor.Find("FistContactBasic04"),"MELEE_CONTACT","op_kyle.basic");
 internal static JObject KyleContactCalibration(){const string art="Assets/_LASTRAIL/Content/Battle/KyleRightActive02/basic-04.png";return new JObject{["scope"]="RIGHT_BASIC_COMMIT_ANCHOR_NOT_ALL_POSE_CALIBRATION",["source"]=art,["sourceSha256"]=Hash(art),["fistPixel"]=new JArray(1100,347),["footPixel"]=new JArray(686,1238),["bodySpanPixel"]=1224,["bodyHeightMm"]=1800};}
 static Transform Actor(Transform foot,string id,bool enemy){var actor=Child(foot,id,Vector3.zero);var body=actor.gameObject.AddComponent<CapsuleCollider>();body.direction=1;body.center=new Vector3(0,.9f,0);body.height=1.8f;body.radius=.3f;Child(actor,"Foot",Vector3.zero);Child(actor,"Muzzle",new Vector3(enemy?-.5f:.5f,1.4f,0));Child(actor,"HitAnchor",new Vector3(0,.9f,0));if(id=="op_kyle")AuthorKyleContact(actor);return actor;}
 static void Verify(GameObject root,bool close=false){
  var capsules=root.GetComponentsInChildren<CapsuleCollider>(true);Need(capsules.Length==7,"Seven new local bodies required");
  foreach(var capsule in capsules){Need(Mm(capsule.height)==1800&&Mm(capsule.radius)==300&&Point(capsule.center).ToString()==new JArray(0,900,0).ToString(),"Local body changed");Need(capsule.transform.Find("Muzzle")&&capsule.transform.Find("HitAnchor"),"Emission/hit anchors missing");}
  foreach(string name in new[]{"Engine","Combat","Gun"}){var box=root.transform.Find("Allies/"+name+"/BodyCollider").GetComponent<BoxCollider>();Need(Point(box.size).ToString()==new JArray(12000,3200,3000).ToString(),"Original train geometry changed");}
  Need(Mm(root.transform.Find("Enemy").position.x)==(close?58000:68000)&&Mm(root.transform.Find("Enemy").position.z)==(close?6000:0),"Measured enemy QA placement differs");
  if(close){var enemy=root.transform.Find("Enemy/BodyCollider").GetComponent<BoxCollider>();foreach(string name in new[]{"Engine","Combat","Gun"})Need(!enemy.bounds.Intersects(root.transform.Find("Allies/"+name+"/BodyCollider").GetComponent<BoxCollider>().bounds),"Parallel QA train bodies overlap");}
  Need(Point(root.transform.Find("FallingObstacle").GetComponent<BoxCollider>().size).ToString()==new JArray(6000,6000,6000).ToString(),"Obstacle shape differs");
 }
 public static void Build()=>Build(false);
 public static void BuildKyleClose()=>Build(true);
 static void Build(bool close){
  string Folder=close?"Assets/_LASTRAIL/Content/Battle/OrdinaryKyleCloseV01/":OrdinaryLiveGeometryBuilder.Folder;
  string Qa=close?"D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/kyle-right-01/close-geometry-v01/":OrdinaryLiveGeometryBuilder.Qa;
  string Prefab=close?Folder+"OrdinaryKyleCloseV01.prefab":OrdinaryLiveGeometryBuilder.Prefab,Scene=close?Folder+"OrdinaryKyleCloseV01.unity":OrdinaryLiveGeometryBuilder.Scene;
  Need(Application.isBatchMode,"Batch authoring required");Need(!Directory.Exists(Folder),"Preserve prior authored output: folder already exists");Need(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"Preserve unsaved scene");
  var originalHashes=new[]{Allies,Enemy,Rail}.ToDictionary(p=>p,Hash);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var root=new GameObject("OrdinaryLiveGeometryV02");
  var allies=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Allies),scene);allies.name="Allies";allies.transform.SetParent(root.transform,true);
  var enemy=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Enemy),scene);enemy.name="Enemy";enemy.transform.SetParent(root.transform,true);
  // Original rail arc zero is world X=-25m; rebase both originals together to the explicit arc basis.
  allies.transform.position+=Vector3.right*25;enemy.transform.position+=Vector3.right*25;
  // Separate parallel QA lane; retain original shapes, sockets, stats and firing rules.
  if(close)enemy.transform.position+=new Vector3(-10,0,6);
  string[] ids={"op_kyle","op_reina","op_eve","op_sera","op_mira"};var actors=new List<Transform>();
  for(int i=0;i<5;i++)actors.Add(Actor(allies.transform.Find((i<3?"Combat":"Gun")+"/SpawnFoot_"+i),ids[i],false));
  for(int i=0;i<2;i++)actors.Add(Actor(enemy.transform.Find("GunnerFoot_"+i),"Gunner"+i,true));
  var socket=Child(allies.transform.Find("Engine"),"TrainMuzzle",Vector3.zero);socket.position=new Vector3((InitialFront-6000)/1000f,4,0);
  var obstacle=Child(root.transform,"FallingObstacle",new Vector3((InitialFront+280000)/1000f,12.2f,0));var obstacleBody=obstacle.gameObject.AddComponent<BoxCollider>();obstacleBody.size=new Vector3(6,6,6);
  Verify(root,close);Directory.CreateDirectory(Folder);Directory.CreateDirectory(Qa);PrefabUtility.SaveAsPrefabAsset(root,Prefab);Need(EditorSceneManager.SaveScene(scene,Scene),"Scene save failed");AssetDatabase.SaveAssets();
  var loaded=PrefabUtility.LoadPrefabContents(Prefab);try{Verify(loaded,close);}finally{PrefabUtility.UnloadPrefabContents(loaded);}
  var entities=new JArray();var layout=new JObject();
  foreach(string name in new[]{"Engine","Combat","Gun","Enemy"}){
   var frame=name=="Enemy"?enemy.transform:allies.transform.Find(name);var anchors=new JArray();
   foreach(Transform child in frame)if(child.name.StartsWith("SpawnFoot_")||child.name.StartsWith("GunnerFoot_"))anchors.Add(Anchor(frame,child,"FOOT"));
   if(name=="Engine")anchors.Add(Anchor(frame,socket,"EMISSION","train.basic"));
   var shape=frame.Find("BodyCollider").GetComponent<BoxCollider>();entities.Add(Entity(frame,name,"TRAIN",name=="Engine"?"train_a":name=="Combat"?"car_combat":name=="Gun"?"car_gun":"enemy_train_first_battle",name=="Enemy"?Enemy:Prefab,anchors,new JArray(Shape(frame,shape,"OCCLUDER"))));layout[name]=Layout(root.transform,frame);
  }
  for(int i=0;i<actors.Count;i++){
   var actor=actors[i];bool hostile=i>=5;string ability=hostile?"enemy_spike_cart_gunner.basic":actor.name+".basic";
   var anchors=new JArray(Anchor(actor,actor.Find("Foot"),"FOOT"),Anchor(actor,actor.Find("Muzzle"),"EMISSION",ability),Anchor(actor,actor.Find("HitAnchor"),"HIT"));
   if(actor.name=="op_kyle")anchors.Add(ExportKyleContact(actor));
   entities.Add(Entity(actor,actor.name,hostile?"ENEMY":"OPERATOR",hostile?"enemy_spike_cart_gunner":actor.name,Prefab,anchors,new JArray(Shape(actor,actor.GetComponent<CapsuleCollider>(),"HIT_VOLUME"))));
   var row=Layout(root.transform,actor);row["carOrder"]=hostile?3:i<3?1:2;row["formationSlot"]=hostile?JValue.CreateNull():new JValue(i);row["gunnerOrdinal"]=hostile?new JValue(i-5):JValue.CreateNull();layout[actor.name]=row;
  }
  entities.Add(Entity(obstacle,obstacle.name,"WORLD",obstacle.name,Prefab,new JArray(),new JArray(Shape(obstacle,obstacleBody,"OCCLUDER"))));layout[obstacle.name]=Layout(root.transform,obstacle);
  var muzzle=root.transform.InverseTransformPoint(socket.position);var relativeMuzzle=muzzle;relativeMuzzle.x-=InitialFront/1000f;
  var definition=new JObject{["version"]="ordinary.native-geometry-definition.v1.20261031",["definitionId"]=close?"ordinary-kyle-close-parallel-v1":"ordinary-live-obstacle-and-five-v2",["units"]="MILLIMETERS",["millimetersPerUnit"]=1000,["axis"]="X_FORWARD_Y_UP_Z_SIDE",["coordinateSpace"]="BOUND_FRAME_LOCAL",
   ["source"]=new JObject{["scope"]=Scope,["originalWorldToArcOffsetMm"]=25000,["scenePath"]=Scene,["sceneHash"]=Hash(Scene),["prefabPath"]=Prefab,["prefabHash"]=Hash(Prefab),["originalAssetHashes"]=JObject.FromObject(originalHashes),["liveLayout"]=layout,
    ["obstacle"]=new JObject{["targetXmm"]=Mm(obstacle.position.x),["zoneStart"]=335000,["zoneEnd"]=341000,["fallStartMs"]=15400,["fallSpeedMmPerSecond"]=10000,["landedCenterY"]=3000,["initialFront"]=InitialFront,["trackLength"]=1200000,["initialCenterMm"]=Point(obstacle.position),["sizeMm"]=Point(obstacleBody.size),["muzzleWorldMm"]=Point(muzzle),["muzzleRelativeToInitialFrontMm"]=Point(relativeMuzzle)}},["entities"]=entities};
  definition["source"]["kyleFistCalibration"]=KyleContactCalibration();
  File.WriteAllText(Folder+"definition.json",definition.ToString());File.WriteAllText(Qa+"definition.json",File.ReadAllText(Folder+"definition.json"));
  foreach(var pair in originalHashes)Need(Hash(pair.Key)==pair.Value,"Original asset changed");
  Need(entities.Count==12&&((JArray)definition["entities"]).OfType<JObject>().Count(e=>(string)e["role"]=="OPERATOR")==5,"Export entity coverage differs");
  File.WriteAllText(Qa+"verification.json",new JObject{["scope"]=Scope,["nativeAuthoringSaved"]=true,["prefabReloadVerified"]=true,["sourceUnchanged"]=true,["entityCount"]=entities.Count,["localCapsules"]=7,["definitionPath"]=Folder+"definition.json",["definitionSha256"]=Hash(Folder+"definition.json"),["sceneSha256"]=Hash(Scene),["prefabSha256"]=Hash(Prefab),["artCalibrationVerified"]=false,["combatExecutionVerified"]=false}.ToString());AssetDatabase.Refresh();
 }
}
#endif
