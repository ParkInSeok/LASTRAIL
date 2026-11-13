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

// Local judgement authoring only; template names never substitute for physical instance IDs.
public static class OrdinaryBoardingGeometryBuilder {
 const string OriginalFolder="Assets/_LASTRAIL/Content/Battle/OrdinaryLiveGeometryV02/";
 const string Original=OriginalFolder+"OrdinaryLiveGeometryV02.prefab";
 const string Folder="Assets/_LASTRAIL/Content/Battle/OrdinaryBoardingV01/";
 const string Prefab=Folder+"OrdinaryBoardingV01.prefab", Scene=Folder+"OrdinaryBoardingV01.unity";
 const string Qa="D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/kyle-right-01/boarding-geometry-v01/";
 const string Scope="LOCAL_AUTHORED_JUDGEMENT_GEOMETRY_NOT_APPROVED_ART_CALIBRATION";
 static readonly string[] Cars={"Engine","Combat","Gun"};
 static readonly string[] Enemies={"enemy_raider_scout","enemy_spike_cart_gunner"};
 static void Need(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static long Mm(float value)=>checked((long)Math.Round(value*1000d,MidpointRounding.AwayFromZero));
 static JArray Point(Vector3 value)=>new JArray(Mm(value.x),Mm(value.y),Mm(value.z));
 static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 static Transform Child(Transform parent,string name,Vector3 point){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=point;return t;}
 static Vector3 Local(Transform frame,Transform point)=>frame.InverseTransformPoint(point.position);
 static Vector3[] Points(Transform frame,Transform path)=>path.Cast<Transform>().Select(p=>Local(frame,p)).ToArray();
 static JArray ExportPoints(Transform frame,Transform path)=>new JArray(Points(frame,path).Select(Point));
 static void Path(Transform parent,string name,params Vector3[] points){var path=Child(parent,name,Vector3.zero);for(int i=0;i<points.Length;i++)Child(path,"Point_"+i,points[i]);}

 static void Author(Transform root){
  foreach(string name in Cars){
   var frame=root.Find("Allies/"+name);var box=frame.Find("BodyCollider").GetComponent<BoxCollider>();
   var center=frame.InverseTransformPoint(box.transform.TransformPoint(box.center));
   // Keep the original operator foot plane (13 mm above the collider roof).
   float roofY=root.Find("Allies/Combat/SpawnFoot_0").position.y-frame.position.y;
   Need(roofY>=center.y+box.size.y/2,"Original foot plane below roof");
   var authoring=Child(frame,"Boarding",Vector3.zero);
   Child(authoring,"RoofMin",new Vector3(center.x-box.size.x/2+.3f,roofY,center.z-box.size.z/2+.3f));
   Child(authoring,"RoofMax",new Vector3(center.x+box.size.x/2-.3f,roofY,center.z+box.size.z/2-.3f));
   var gunner=Child(authoring,"GunnerAnchors",Vector3.zero);
   for(int i=0;i<4;i++)Child(gunner,"Anchor_"+i,new Vector3(i<2?-4:4,roofY,i%2==0?-1:1));
   foreach(string side in new[]{"left","right"}){
    float sign=side=="left"?-1:1;var socket=Child(authoring,side,Vector3.zero);
    float baseZ=center.z+sign*(box.size.z/2+.5f);
    float socketX=center.x+5.2f;
    var attached=new Vector3(socketX,0,baseZ);
    Path(socket,"Approach",new Vector3(socketX,0,baseZ+sign*2),attached);
    Path(socket,"Climb",attached,new Vector3(socketX,roofY,baseZ),new Vector3(socketX,roofY,center.z+sign*1.1f));
   }
  }
  var templates=Child(root,"BoardingEnemyTemplates",Vector3.zero);
  foreach(string id in Enemies){
   var frame=Child(templates,id,Vector3.zero);var body=frame.gameObject.AddComponent<CapsuleCollider>();
   body.direction=1;body.center=new Vector3(0,.9f,0);body.height=1.8f;body.radius=.3f;
   Child(frame,"Foot",Vector3.zero);Child(frame,"Muzzle",new Vector3(-.5f,1.4f,0));Child(frame,"HitAnchor",new Vector3(0,.9f,0));
  }
  // These are measured templates, not already spawned enemy instances.
  templates.gameObject.SetActive(false);
 }

 static JObject Snapshot(Transform root){
  var rows=new JObject();foreach(var t in root.GetComponentsInChildren<Transform>(true)){
   string path=AnimationUtility.CalculateTransformPath(t,root);
   var row=new JObject{["position"]=Point(t.localPosition),["rotation"]=new JArray(t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w),["scale"]=new JArray(t.localScale.x,t.localScale.y,t.localScale.z),["active"]=t.gameObject.activeSelf};
   var colliders=new JArray();foreach(var collider in t.GetComponents<Collider>()){
    var shape=new JObject{["type"]=collider.GetType().Name,["enabled"]=collider.enabled,["trigger"]=collider.isTrigger};
    if(collider is BoxCollider box){shape["center"]=Point(box.center);shape["size"]=Point(box.size);}
    else if(collider is CapsuleCollider capsule){shape["center"]=Point(capsule.center);shape["radius"]=Mm(capsule.radius);shape["height"]=Mm(capsule.height);shape["direction"]=capsule.direction;}
    else throw new NotSupportedException("Preservation check requires box or capsule collider");
    colliders.Add(shape);
   }
   row["colliders"]=colliders;rows[path]=row;
  }return rows;
 }
 static void VerifyPreserved(Transform root,JObject original){
  var current=Snapshot(root);foreach(var property in original.Properties())Need(JToken.DeepEquals(property.Value,current[property.Name]),"Original transform/collider changed: "+property.Name);
 }
 static bool Inside(Vector3 point,Vector3 min,Vector3 max)=>point.x>=min.x&&point.x<=max.x&&point.y>=min.y&&point.y<=max.y&&point.z>=min.z&&point.z<=max.z;
 static void VerifyPath(Vector3[] path,Bounds body){
  Need(path.Length>=2,"Path needs at least two measured points");
  for(int i=1;i<path.Length;i++){
   var a=path[i-1];var b=path[i];Need(a!=b,"Duplicate path segment");
   // Each authored segment is axis aligned; the capsule's swept bounds must stay outside the hull.
   Need((a.x==b.x?0:1)+(a.y==b.y?0:1)+(a.z==b.z?0:1)==1,"Only authored axis-aligned paths supported");
   var low=Vector3.Min(a,b)+new Vector3(-.3f,0,-.3f);var high=Vector3.Max(a,b)+new Vector3(.3f,1.8f,.3f);
   bool intersects=low.x<body.max.x&&high.x>body.min.x&&low.y<body.max.y&&high.y>body.min.y&&low.z<body.max.z&&high.z>body.min.z;
   Need(!intersects,"Swept enemy body enters carriage hull");
  }
 }
 static JObject Measure(Transform root){
  var cars=new JArray();var socketIds=new HashSet<string>();
  for(int order=0;order<Cars.Length;order++){
   var frame=root.Find("Allies/"+Cars[order]);var box=frame.Find("BodyCollider").GetComponent<BoxCollider>();
   Need(box.size==new Vector3(12,3.2f,3)&&box.transform.lossyScale==Vector3.one,"Original carriage size/scale changed");
   var body=new Bounds(frame.InverseTransformPoint(box.transform.TransformPoint(box.center)),box.size);
   var authored=frame.Find("Boarding");var min=Local(frame,authored.Find("RoofMin"));var max=Local(frame,authored.Find("RoofMax"));
   Need(Mm(min.y)==Mm(max.y)&&Mm(min.y)>=Mm(body.max.y)&&Mm(min.x)>=Mm(body.min.x)+300&&Mm(max.x)<=Mm(body.max.x)-300&&Mm(min.z)>=Mm(body.min.z)+300&&Mm(max.z)<=Mm(body.max.z)-300,"Roof center bounds must preserve capsule clearance");
   var sockets=new JObject();foreach(string side in new[]{"left","right"}){
    var socket=authored.Find(side);var approach=Points(frame,socket.Find("Approach"));var climb=Points(frame,socket.Find("Climb"));
    VerifyPath(approach,body);VerifyPath(climb,body);
    Need(approach.Last()==climb.First(),"Approach/climb discontinuity");Need(Inside(climb.Last(),min,max),"Climb does not end on safe roof");
    Need(Mm(approach.First().x)==5200&&approach.Concat(climb).All(p=>Mm(p.x)==5200),"Forward socket path changed");
    if(Cars[order]=="Combat")Need(Mm(Local(frame,frame.Find("SpawnFoot_0/op_kyle/Foot")).x)<Mm(approach.First().x),"Kyle's original foot must lie behind Combat boarding socket");
    string id=Cars[order]+":"+side;Need(socketIds.Add(id),"Duplicate socket ID");
    sockets[side]=new JObject{["socketId"]=id,["framePath"]=PathOf(socket),["approachPathMm"]=ExportPoints(frame,socket.Find("Approach")),["climbPathMm"]=ExportPoints(frame,socket.Find("Climb"))};
   }
   var anchors=Points(frame,authored.Find("GunnerAnchors"));Need(anchors.All(p=>Inside(p,min,max)),"Gunner anchor outside safe roof");
   cars.Add(new JObject{["carOrder"]=order,["authoringId"]=Cars[order],["framePath"]=PathOf(frame),["roofMinMm"]=Point(min),["roofMaxMm"]=Point(max),["gunnerAnchorsMm"]=new JArray(anchors.Select(Point)),["sockets"]=sockets});
  }
  var templates=new JArray();foreach(string id in Enemies){
   var frame=root.Find("BoardingEnemyTemplates/"+id);var body=frame.GetComponent<CapsuleCollider>();
   Need(!frame.parent.gameObject.activeSelf&&body.direction==1&&Mm(body.radius)==300&&Mm(body.height)==1800,"Unexpected enemy template");
   var anchors=new JArray();foreach(var pair in new[]{("Foot","FOOT"),("Muzzle","EMISSION"),("HitAnchor","HIT")}){
    var row=new JObject{["id"]=pair.Item1,["role"]=pair.Item2,["positionMm"]=Point(Local(frame,frame.Find(pair.Item1)))};
    if(pair.Item2=="EMISSION")row["abilityId"]=id+".basic";anchors.Add(row);
   }
   templates.Add(new JObject{["definitionRef"]=id,["framePath"]=PathOf(frame),["assetPath"]=Prefab,["assetHash"]=Hash(Prefab),["anchors"]=anchors,
    ["shapes"]=new JArray(new JObject{["id"]=id,["role"]="HIT_VOLUME",["kind"]="CAPSULE_Y",["centerMm"]=Point(body.center),["radiusMm"]=Mm(body.radius),["heightMm"]=Mm(body.height)})});
  }
  return new JObject{["version"]="ordinary.authored-boarding-geometry.v1.20261107",["scope"]=Scope,["coordinateSpace"]="CARRIAGE_FRAME_LOCAL",["units"]="MILLIMETERS",["cars"]=cars,["bridges"]=new JArray(),["enemyTemplates"]=templates};
 }

 public static void Build(){
  Need(Application.isBatchMode,"Batch authoring required");Need(!Directory.Exists(Folder)&&!Directory.Exists(Qa),"Preserve prior authored output; destination already exists");
  Need(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"Preserve unsaved scene");
  var definition=JObject.Parse(File.ReadAllText(OriginalFolder+"definition.json"));
  var originals=((JObject)definition["source"]["originalAssetHashes"]).Properties().ToDictionary(p=>p.Name,p=>Hash(p.Name));
  originals[Original]=Hash(Original);originals[OriginalFolder+"definition.json"]=Hash(OriginalFolder+"definition.json");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Original),scene);
  PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  root.name="OrdinaryBoardingV01";var originalState=Snapshot(root.transform);Author(root.transform);VerifyPreserved(root.transform,originalState);
  Directory.CreateDirectory(Folder);Directory.CreateDirectory(Qa);
  Need(PrefabUtility.SaveAsPrefabAsset(root,Prefab)!=null,"Prefab save failed");Need(EditorSceneManager.SaveScene(scene,Scene),"Scene save failed");AssetDatabase.SaveAssets();
  var loaded=PrefabUtility.LoadPrefabContents(Prefab);JObject measured;
  try{VerifyPreserved(loaded.transform,originalState);measured=Measure(loaded.transform);}finally{PrefabUtility.UnloadPrefabContents(loaded);}
  definition["definitionId"]="ordinary-live-boarding-v1";var source=(JObject)definition["source"];
  source["prefabPath"]=Prefab;source["prefabHash"]=Hash(Prefab);source["scenePath"]=Scene;source["sceneHash"]=Hash(Scene);
  source["originalAssetHashes"]=JObject.FromObject(originals);source["boardingGeometry"]=measured;
  foreach(var entity in ((JArray)definition["entities"]).OfType<JObject>()){
   entity["framePath"]=((string)entity["framePath"]).Replace("OrdinaryLiveGeometryV02/","OrdinaryBoardingV01/");entity["assetPath"]=Prefab;entity["assetHash"]=Hash(Prefab);
  }
  Need(((JArray)definition["entities"]).Count==12,"Do not preposition planned boarding actors");
  foreach(var pair in originals)Need(Hash(pair.Key)==pair.Value,"Original asset changed: "+pair.Key);
  File.WriteAllText(Folder+"definition.json",definition.ToString());File.WriteAllText(Qa+"definition.json",definition.ToString());
  File.WriteAllText(Qa+"verification.json",new JObject{["scope"]=Scope,["nativeAuthoringSaved"]=true,["prefabReloadVerified"]=true,["sourceUnchanged"]=true,["existingTransformsAndCollidersPreserved"]=true,["continuousCapsuleClearPathsVerified"]=true,["physicalCars"]=3,["sockets"]=6,["enemyTemplates"]=2,["entityCount"]=12,["definitionPath"]=Folder+"definition.json",["definitionSha256"]=Hash(Folder+"definition.json"),["prefabSha256"]=Hash(Prefab),["sceneSha256"]=Hash(Scene),["artCalibrationVerified"]=false,["combatExecutionVerified"]=false,["playerVisualVerified"]=false}.ToString());
  AssetDatabase.Refresh();
 }
 public static void RepairKyleContact(){
  const string target="Assets/_LASTRAIL/Content/Battle/OrdinaryBoardingKyleContactV02/";
  Need(Application.isBatchMode&&!Directory.Exists(target),"Fresh batch output required");
  Need(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"Preserve unsaved scene");
  var definition=JObject.Parse(File.ReadAllText(Folder+"definition.json"));string oldHash=Hash(Prefab);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);
  PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  var preserved=Snapshot(root.transform);var kyle=root.transform.Find("Allies/Combat/SpawnFoot_0/op_kyle");
  OrdinaryLiveGeometryBuilder.AuthorKyleContact(kyle);VerifyPreserved(root.transform,preserved);
  Directory.CreateDirectory(target);string prefab=target+"OrdinaryBoardingKyleContactV02.prefab",scenePath=target+"OrdinaryBoardingKyleContactV02.unity";
  Need(PrefabUtility.SaveAsPrefabAsset(root,prefab)!=null&&EditorSceneManager.SaveScene(scene,scenePath),"Contact source save failed");AssetDatabase.SaveAssets();
  var loaded=PrefabUtility.LoadPrefabContents(prefab);
  try{
   VerifyPreserved(loaded.transform,preserved);var contact=OrdinaryLiveGeometryBuilder.ExportKyleContact(loaded.transform.Find("Allies/Combat/SpawnFoot_0/op_kyle"));
   Need(JToken.DeepEquals(contact["positionMm"],new JArray(609,1310,0)),"Reloaded contact differs");
   var actor=((JArray)definition["entities"]).Single(e=>(string)e["authoringId"]=="op_kyle");
   Need(!((JArray)actor["anchors"]).Any(a=>(string)a["role"]=="MELEE_CONTACT"),"Do not duplicate existing contact");((JArray)actor["anchors"]).Add(contact);
   definition["source"]["boardingGeometry"]=Measure(loaded.transform);
  }finally{PrefabUtility.UnloadPrefabContents(loaded);}
  definition["definitionId"]="ordinary-live-boarding-kyle-contact-v2";var source=(JObject)definition["source"];
  source["prefabPath"]=prefab;source["prefabHash"]=Hash(prefab);source["scenePath"]=scenePath;source["sceneHash"]=Hash(scenePath);source["kyleFistCalibration"]=OrdinaryLiveGeometryBuilder.KyleContactCalibration();
  foreach(var entity in ((JArray)definition["entities"]).Concat((JArray)source["boardingGeometry"]["enemyTemplates"])) {entity["assetPath"]=prefab;entity["assetHash"]=Hash(prefab);}
  Need(Hash(Prefab)==oldHash,"Historical prefab changed");File.WriteAllText(target+"definition.json",definition.ToString());
  File.WriteAllText(target+"verification.json",new JObject{["originalPrefabPreserved"]=true,["existingTransformsCollidersPreserved"]=true,["contactReloadVerified"]=true,["definitionSha256"]=Hash(target+"definition.json"),["scope"]="CONTACT_SOURCE_REPAIR_ONLY_NO_START_OR_ACTION_CHANGES"}.ToString());AssetDatabase.Refresh();
 }
}
#endif
