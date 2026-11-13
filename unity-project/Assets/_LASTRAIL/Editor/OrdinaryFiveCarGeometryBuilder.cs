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

// New measured formation only. Runtime physical identities and equipment rights remain server-owned.
public static class OrdinaryFiveCarGeometryBuilder {
 const string OriginalFolder="Assets/_LASTRAIL/Content/Battle/OrdinaryBoardingV01/";
 const string Original=OriginalFolder+"OrdinaryBoardingV01.prefab";
 const string Folder="Assets/_LASTRAIL/Content/Battle/OrdinaryFiveCarV01/";
 const string Prefab=Folder+"OrdinaryFiveCarV01.prefab", Scene=Folder+"OrdinaryFiveCarV01.unity";
 const string Qa="D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/kyle-right-01/five-car-geometry-v01/";
 const string Scope="LOCAL_AUTHORED_JUDGEMENT_GEOMETRY_NOT_APPROVED_ART_CALIBRATION";
 static readonly string[] Operators={"op_kyle","op_reina","op_eve","op_sera","op_mira"};
 static readonly string[] Cars={"Engine","Car0","Car1","Car2","Car3","Car4"};
 static void Need(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
 static long Mm(float x)=>checked((long)Math.Round(x*1000d,MidpointRounding.AwayFromZero));
 static JArray Point(Vector3 p)=>new JArray(Mm(p.x),Mm(p.y),Mm(p.z));
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 static Vector3 Local(Transform frame,Transform point)=>frame.InverseTransformPoint(point.position);
 static Transform Copy(Transform original,Transform parent,string name){var t=UnityEngine.Object.Instantiate(original.gameObject,parent,false).transform;t.name=name;return t;}
 static JArray PathPoints(Transform frame,Transform path)=>new JArray(path.Cast<Transform>().Select(t=>Point(Local(frame,t))));
 static JObject Shape(Transform frame,Collider collider,string role){
  Need(collider.transform.lossyScale==Vector3.one&&collider.transform.rotation==frame.rotation,"Axis aligned unit shape required");JObject row;
  if(collider is BoxCollider b)row=new JObject{["kind"]="BOX",["centerMm"]=Point(frame.InverseTransformPoint(b.transform.TransformPoint(b.center))),["sizeMm"]=Point(b.size)};
  else if(collider is CapsuleCollider c){Need(c.direction==1,"Y capsule required");row=new JObject{["kind"]="CAPSULE_Y",["centerMm"]=Point(frame.InverseTransformPoint(c.transform.TransformPoint(c.center))),["radiusMm"]=Mm(c.radius),["heightMm"]=Mm(c.height)};}
  else throw new NotSupportedException("Unexpected shape");row["id"]=collider.name;row["role"]=role;return row;
 }
 static JObject Anchor(Transform frame,Transform point,string role,string ability=null){var row=new JObject{["id"]=point.name,["role"]=role,["positionMm"]=Point(Local(frame,point))};if(ability!=null)row["abilityId"]=ability;return row;}
 static JObject Actor(Transform frame,string definition,string role)=>new JObject{["authoringId"]=frame.name,["role"]=role,["definitionRef"]=definition,["framePath"]=PathOf(frame),["assetPath"]=Prefab,["assetHash"]=Hash(Prefab),
  ["anchors"]=new JArray(Anchor(frame,frame.Find("Foot"),"FOOT"),Anchor(frame,frame.Find("Muzzle"),"EMISSION",definition+".basic"),Anchor(frame,frame.Find("HitAnchor"),"HIT")),["shapes"]=new JArray(Shape(frame,frame.GetComponent<CapsuleCollider>(),"HIT_VOLUME"))};
 static JObject Layout(Transform root,Transform frame,long front){var p=Local(root,frame);var relative=Point(p);relative[0]=Mm(p.x)-front;return new JObject{["originMm"]=Point(p),["basis"]=new JArray(new JArray(1,0,0),new JArray(0,1,0),new JArray(0,0,1)),["relativeToInitialFrontMm"]=relative};}
 static bool Inside(JArray p,JArray min,JArray max)=>Enumerable.Range(0,3).All(i=>(long)p[i]>=(long)min[i]&&(long)p[i]<=(long)max[i]);
 static bool Blocked(Vector3 a,Vector3 b,IEnumerable<BoxCollider> boxes){var direction=b-a;float distance=direction.magnitude;Need(distance>0,"Nonzero LOS segment required");var ray=new Ray(a,direction.normalized);return boxes.Any(box=>box.bounds.IntersectRay(ray,out float entry)&&entry<distance);}
 static void VerifyPath(JArray path,BoxCollider box,Transform frame){
  Need(path.Count>=2,"Path needs two points");var center=Point(frame.InverseTransformPoint(box.transform.TransformPoint(box.center)));var size=Point(box.size);
  for(int n=1;n<path.Count;n++){
   var a=(JArray)path[n-1];var b=(JArray)path[n];Need(Enumerable.Range(0,3).Count(i=>(long)a[i]!=(long)b[i])==1,"Axis aligned nonzero path segment required");
   long[] low={Math.Min((long)a[0],(long)b[0])-300,Math.Min((long)a[1],(long)b[1]),Math.Min((long)a[2],(long)b[2])-300};
   long[] high={Math.Max((long)a[0],(long)b[0])+300,Math.Max((long)a[1],(long)b[1])+1800,Math.Max((long)a[2],(long)b[2])+300};
   Need(!Enumerable.Range(0,3).All(i=>low[i]<(long)center[i]+(long)size[i]/2&&high[i]>(long)center[i]-(long)size[i]/2),"Swept boarding capsule enters body");
  }
 }
 static JObject MeasureBoarding(Transform root,JObject original){
  var cars=new JArray();var socketsSeen=new HashSet<string>();
  for(int order=0;order<Cars.Length;order++){
   var frame=root.Find("Allies/"+Cars[order]);var authored=frame.Find("Boarding");var body=frame.Find("BodyCollider").GetComponent<BoxCollider>();
   var min=Point(Local(frame,authored.Find("RoofMin")));var max=Point(Local(frame,authored.Find("RoofMax")));
   Need(JToken.DeepEquals(min,new JArray(-5700,3213,-1200))&&JToken.DeepEquals(max,new JArray(5700,3213,1200)),"Original safe roof bounds changed");var sockets=new JObject();
   foreach(string side in new[]{"left","right"}){
    var socket=authored.Find(side);var approach=PathPoints(frame,socket.Find("Approach"));var climb=PathPoints(frame,socket.Find("Climb"));
    VerifyPath(approach,body,frame);VerifyPath(climb,body,frame);Need(JToken.DeepEquals(approach.Last,climb.First)&&Inside((JArray)climb.Last,min,max),"Climb continuity/roof endpoint changed");
    string id=Cars[order]+":"+side;Need(socketsSeen.Add(id),"Duplicate socket");sockets[side]=new JObject{["socketId"]=id,["framePath"]=PathOf(socket),["approachPathMm"]=approach,["climbPathMm"]=climb};
   }
   var gunner=PathPoints(frame,authored.Find("GunnerAnchors"));Need(gunner.Cast<JArray>().All(p=>Inside(p,min,max)),"Gunner anchor outside roof");
   cars.Add(new JObject{["carOrder"]=order,["authoringId"]=Cars[order],["framePath"]=PathOf(frame),["roofMinMm"]=min,["roofMaxMm"]=max,["gunnerAnchorsMm"]=gunner,["sockets"]=sockets});
  }
  var result=(JObject)original.DeepClone();result["cars"]=cars;result["bridges"]=new JArray();var templates=new JArray();
  foreach(Transform frame in root.Find("BoardingEnemyTemplates")){
   Need(!frame.parent.gameObject.activeSelf,"Templates must remain inactive");var row=Actor(frame,frame.name,"ENEMY");row.Remove("authoringId");row.Remove("role");templates.Add(row);
  }result["enemyTemplates"]=templates;return result;
 }
 static void RenderOverview(GameObject loaded,string outputPath){
  // Temporary QA-only copy: never bake path markers or flat inspection colors into the authored prefab.
  var copy=UnityEngine.Object.Instantiate(loaded);copy.name="FiveCarGeometryOverview_QAOnly";
  var renderScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();Need(renderScene.IsValid()&&renderScene.isLoaded,"Active normal scene required for QA rendering");
  UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,renderScene);copy.SetActive(true);
  var shader=Shader.Find("Unlit/Color");Need(shader!=null,"QA unlit shader unavailable");
  var bodyMaterial=new Material(shader){color=new Color(.35f,.48f,.60f)};
  var footMaterial=new Material(shader){color=new Color(.1f,1,.45f)};
  var pathMaterial=new Material(shader){color=new Color(1,.65f,.1f)};
  var cameraObject=new GameObject("FiveCarGeometryOverviewCamera_QAOnly");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,renderScene);
  var target=new RenderTexture(2200,800,24);var image=new Texture2D(2200,800,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  bool active=loaded.activeSelf;loaded.SetActive(false);
  try{
   foreach(var renderer in copy.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=Enumerable.Repeat(bodyMaterial,renderer.sharedMaterials.Length).ToArray();
   copy.transform.Find("Enemy").gameObject.SetActive(false);copy.transform.Find("FallingObstacle").gameObject.SetActive(false);
   for(int order=0;order<Cars.Length;order++){
    var frame=copy.transform.Find("Allies/"+Cars[order]);
    foreach(string side in new[]{"left","right"})foreach(string phase in new[]{"Approach","Climb"}){
     var path=frame.Find("Boarding/"+side+"/"+phase);var line=path.gameObject.AddComponent<LineRenderer>();line.sharedMaterial=pathMaterial;line.widthMultiplier=.12f;line.useWorldSpace=true;line.positionCount=path.childCount;line.SetPositions(path.Cast<Transform>().Select(t=>t.position).ToArray());
    }
    if(order==0)continue;var foot=frame.Find("SpawnFoot_"+(order-1)+"/"+Operators[order-1]+"/Foot");
    var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="Foot_QAOnly";marker.transform.SetParent(copy.transform,false);marker.transform.position=foot.position;marker.transform.localScale=Vector3.one*.65f;marker.GetComponent<Renderer>().sharedMaterial=footMaterial;
   }
   foreach(var t in copy.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
   var camera=cameraObject.AddComponent<Camera>();camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=16;camera.aspect=2200f/800;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.06f);camera.nearClipPlane=.1f;camera.farClipPlane=250;
   camera.transform.position=new Vector3(58.5f,28,-60);camera.transform.LookAt(new Vector3(58.5f,1.6f,0));camera.targetTexture=target;
   camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,2200,800),0,0);image.Apply();
   var pixels=image.GetPixels32();var background=pixels[0];Need(pixels.Count(p=>Math.Abs(p.r-background.r)>8||Math.Abs(p.g-background.g)>8||Math.Abs(p.b-background.b)>8)>=100,"QA render is blank; preserve prior output");
   File.WriteAllBytes(outputPath,image.EncodeToPNG());
  }finally{
   loaded.SetActive(active);RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(copy);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(bodyMaterial);UnityEngine.Object.DestroyImmediate(footMaterial);UnityEngine.Object.DestroyImmediate(pathMaterial);
  }
 }
 public static void RenderSavedOverview(){
  const string output=Qa+"geometry-overview-v02.png";Need(!File.Exists(output),"Preserve existing v02 overview");
  var preserved=new[]{Prefab,Scene,Folder+"definition.json",Qa+"definition.json",Qa+"geometry-overview.png"}.ToDictionary(p=>p,Hash);
  var loaded=PrefabUtility.LoadPrefabContents(Prefab);try{RenderOverview(loaded,output);}finally{PrefabUtility.UnloadPrefabContents(loaded);}
  foreach(var pair in preserved)Need(Hash(pair.Key)==pair.Value,"Saved artifact changed during preview: "+pair.Key);
 }
 public static void Build(){
  Need(Application.isBatchMode&&!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"Clean batch authoring required");Need(!Directory.Exists(Folder)&&!Directory.Exists(Qa),"Preserve previous outputs");
  var definition=JObject.Parse(File.ReadAllText(OriginalFolder+"definition.json"));var originalEntities=((JArray)definition["entities"]).OfType<JObject>().ToDictionary(e=>(string)e["authoringId"]);
  var originalHashes=((JObject)definition["source"]["originalAssetHashes"]).Properties().ToDictionary(p=>p.Name,p=>Hash(p.Name));originalHashes[Original]=Hash(Original);originalHashes[OriginalFolder+"definition.json"]=Hash(OriginalFolder+"definition.json");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Original),scene);
  PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.name="OrdinaryFiveCarV01";
  var allies=root.transform.Find("Allies");var engine=allies.Find("Engine");var combat=allies.Find("Combat");var gun=allies.Find("Gun");
  var length=Mm(engine.Find("BodyCollider").GetComponent<BoxCollider>().size.x);var spacing=Mm(engine.position.x-combat.position.x);long gap=spacing-length;
  Need(length==12000&&gap==1000,"Original body/gap source differs");long total=6*length+5*gap,front=20000+total,delta=front-(long)definition["source"]["obstacle"]["initialFront"];
  for(int i=0;i<5;i++){
   var template=i<3?combat:gun;var car=Copy(template,allies,"Car"+i);
   foreach(var child in car.Cast<Transform>().Where(t=>t.name.StartsWith("SpawnFoot_")).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
   Copy(template.Find("SpawnFoot_"+i),car,"SpawnFoot_"+i);
   car.position=new Vector3((front-length/2-(i+1)*spacing)/1000f,engine.position.y,engine.position.z);
  }
  UnityEngine.Object.DestroyImmediate(combat.gameObject);UnityEngine.Object.DestroyImmediate(gun.gameObject);
  engine.position+=Vector3.right*(delta/1000f);root.transform.Find("Enemy").position+=Vector3.right*(delta/1000f);root.transform.Find("FallingObstacle").position+=Vector3.right*(delta/1000f);
  Directory.CreateDirectory(Folder);Directory.CreateDirectory(Qa);Need(PrefabUtility.SaveAsPrefabAsset(root,Prefab)!=null,"Prefab save failed");Need(EditorSceneManager.SaveScene(scene,Scene),"Scene save failed");AssetDatabase.SaveAssets();
  var loaded=PrefabUtility.LoadPrefabContents(Prefab);try{
   var entities=new JArray();var layout=new JObject();var boxes=new List<BoxCollider>();var footprints=new JArray();
   void Add(JObject row,Transform frame,int? order=null){entities.Add(row);var p=Layout(loaded.transform,frame,front);if(order.HasValue)p["carOrder"]=order.Value;layout[frame.name]=p;}
   for(int order=0;order<Cars.Length;order++){
    var frame=loaded.transform.Find("Allies/"+Cars[order]);var box=frame.Find("BodyCollider").GetComponent<BoxCollider>();boxes.Add(box);
    Need(JToken.DeepEquals(Point(box.size),new JArray(12000,3200,3000)),"Body dimensions changed");Need(frame.GetComponentsInChildren<CapsuleCollider>(true).Length==(order==0?0:1),"One operator per car and none on locomotive required");
    var anchors=new JArray();if(order==0)anchors.Add(Anchor(frame,frame.Find("TrainMuzzle"),"EMISSION","train.basic"));else anchors.Add(Anchor(frame,frame.Find("SpawnFoot_"+(order-1)),"FOOT"));
    Add(new JObject{["authoringId"]=frame.name,["role"]="TRAIN",["definitionRef"]=order==0?"train_a":"LOCAL_CARRIAGE_TEMPLATE_NOT_AUTHORITY",["framePath"]=PathOf(frame),["assetPath"]=Prefab,["assetHash"]=Hash(Prefab),["anchors"]=anchors,["shapes"]=new JArray(Shape(frame,box,"OCCLUDER"))},frame,order);
    if(order==0)continue;int slot=order-1;var actor=frame.Find("SpawnFoot_"+slot+"/"+Operators[slot]);var actorRow=Actor(actor,actor.name,"OPERATOR");
    Need(JToken.DeepEquals(actorRow["anchors"],originalEntities[actor.name]["anchors"])&&JToken.DeepEquals(actorRow["shapes"],originalEntities[actor.name]["shapes"]),"Original operator offsets/body changed");
    var localFoot=Point(Local(frame,actor.Find("Foot")));Need((long)localFoot[1]>=3200&&Math.Abs((long)localFoot[0])+300<=6000&&Math.Abs((long)localFoot[2])+300<=1500,"Operator body/feet outside roof");
    var originalLayout=definition["source"]["liveLayout"];var originalFrame=originalLayout[slot<3?"Combat":"Gun"]["originMm"];var originalFoot=originalLayout[actor.name]["originMm"];Need(JToken.DeepEquals(localFoot,new JArray(Enumerable.Range(0,3).Select(i=>(long)originalFoot[i]-(long)originalFrame[i]))),"Original operator foot offset changed");
    Add(actorRow,actor,order);layout[actor.name]["formationSlot"]=slot;layout[actor.name]["gunnerOrdinal"]=JValue.CreateNull();footprints.Add(new JObject{["operatorId"]=actor.name,["carOrder"]=order,["formationSlot"]=slot,["footLocalMm"]=localFoot});
   }
   for(int i=0;i<boxes.Count;i++)for(int j=i+1;j<boxes.Count;j++)Need(!boxes[i].bounds.Intersects(boxes[j].bounds),"Carriage bodies overlap");
   for(int i=1;i<boxes.Count;i++)Need(Mm(boxes[i-1].bounds.min.x-boxes[i].bounds.max.x)==gap,"Measured coupling gap changed");
   Need(Mm(boxes[0].bounds.max.x)==front&&Mm(boxes.Last().bounds.min.x)==20000,"Measured front/tail placement differs");
   foreach(string name in new[]{"Enemy","Gunner0","Gunner1","FallingObstacle"}){
    var old=originalEntities[name];string relative=((string)old["framePath"]).Substring("OrdinaryBoardingV01/".Length);var frame=loaded.transform.Find(relative);var row=(JObject)old.DeepClone();
    row["framePath"]=PathOf(frame);row["assetPath"]=Prefab;row["assetHash"]=Hash(Prefab);
    if(name.StartsWith("Gunner"))row=Actor(frame,"enemy_spike_cart_gunner","ENEMY");else row["shapes"]=new JArray(Shape(frame,name=="Enemy"?frame.Find("BodyCollider").GetComponent<BoxCollider>():frame.GetComponent<BoxCollider>(),"OCCLUDER"));
    Add(row,frame,name=="FallingObstacle"?(int?)null:6);if(name.StartsWith("Gunner")){layout[name]["formationSlot"]=JValue.CreateNull();layout[name]["gunnerOrdinal"]=int.Parse(name.Substring(6));}
   }
   var enemyBox=loaded.transform.Find("Enemy/BodyCollider").GetComponent<BoxCollider>();Need(boxes.All(b=>!b.bounds.Intersects(enemyBox.bounds)),"Enemy train overlaps new formation");
   var occluders=boxes.Concat(new[]{enemyBox}).ToArray();foreach(var box in boxes)Need(Blocked(box.bounds.center-Vector3.right*7,box.bounds.center+Vector3.right*7,occluders),"Authored body must occlude a through-hull segment");
   for(int slot=0;slot<5;slot++){var frame=loaded.transform.Find("Allies/Car"+slot);var operatorMuzzle=frame.Find("SpawnFoot_"+slot+"/"+Operators[slot]+"/Muzzle");foreach(string side in new[]{"left","right"}){var climb=frame.Find("Boarding/"+side+"/Climb");Need(!Blocked(operatorMuzzle.position,climb.GetChild(climb.childCount-1).position+Vector3.up*.9f,occluders),"Own roof target LOS enters a carriage hull");}}
   Need((long)layout["Enemy"]["relativeToInitialFrontMm"][0]==10000,"Original enemy relative-front placement changed");
   var obstacle=loaded.transform.Find("FallingObstacle");var expectedObstacle=(JArray)definition["source"]["obstacle"]["initialCenterMm"].DeepClone();expectedObstacle[0]=(long)expectedObstacle[0]+delta;Need(JToken.DeepEquals(Point(obstacle.position),expectedObstacle),"Original obstacle relative-front placement changed");
   var source=(JObject)definition["source"];source["boardingGeometry"]=MeasureBoarding(loaded.transform,(JObject)source["boardingGeometry"]);source["liveLayout"]=layout;
   source["formationGeometry"]=new JObject{["status"]="LOCAL_AUTHORING_INITIAL_PLACEMENT_PENDING_SERVER_START_PIN",["physicalCarCount"]=6,["operatorCarCount"]=5,["bodyLengthMm"]=length,["gapMm"]=gap,["totalLengthMm"]=total,["initialFrontArcMm"]=front,["initialTailArcMm"]=20000,["operatorFootprints"]=footprints,["physicalIdentityBinding"]="SERVER_ORIGINAL_PHYSICAL_IDS_BY_CAR_ORDER_REQUIRED",["equipmentRightsBinding"]="SERVER_SELECTED_TYPES_RANKS_REVISIONS_REQUIRED",["enemyPlacement"]="ORIGINAL_RELATIVE_TO_FRONT_PLUS_10000_MM",["obstaclePlacement"]="ORIGINAL_RELATIVE_TO_FRONT_PLUS_280000_MM"};
   foreach(string field in new[]{"targetXmm","zoneStart","zoneEnd"})source["obstacle"][field]=(long)source["obstacle"][field]+delta;source["obstacle"]["initialCenterMm"]=Point(obstacle.position);
   source["obstacle"]["initialFront"]=front;var muzzle=loaded.transform.Find("Allies/Engine/TrainMuzzle");source["obstacle"]["muzzleWorldMm"]=Point(muzzle.position);var relativeMuzzle=Point(muzzle.position);relativeMuzzle[0]=Mm(muzzle.position.x)-front;source["obstacle"]["muzzleRelativeToInitialFrontMm"]=relativeMuzzle;
   source["prefabPath"]=Prefab;source["prefabHash"]=Hash(Prefab);source["scenePath"]=Scene;source["sceneHash"]=Hash(Scene);source["originalAssetHashes"]=JObject.FromObject(originalHashes);definition["definitionId"]="ordinary-five-car-boarding-v1";definition["entities"]=entities;
   Need(entities.Count==15&&layout.Count==entities.Count&&footprints.Count==5,"New formation coverage differs");RenderOverview(loaded,Qa+"geometry-overview.png");
  }finally{PrefabUtility.UnloadPrefabContents(loaded);}
  foreach(var p in originalHashes)Need(Hash(p.Key)==p.Value,"Original source changed: "+p.Key);
  File.WriteAllText(Folder+"definition.json",definition.ToString());File.WriteAllText(Qa+"definition.json",definition.ToString());
  File.WriteAllText(Qa+"verification.json",new JObject{["scope"]=Scope,["prefabReloadMeasured"]=true,["originalAssetsPreserved"]=true,["oneOperatorPerCarVerified"]=true,["capsuleClearBoardingPathsVerified"]=true,["authoredHullAndOwnRoofLosProbesVerified"]=true,["physicalCarCount"]=6,["socketCount"]=12,["entityCount"]=15,["definitionSha256"]=Hash(Folder+"definition.json"),["formationGeometry"]=definition["source"]["formationGeometry"].DeepClone(),["serverIdentityBindingVerified"]=false,["playerVisualVerified"]=false,["combatLosVerified"]=false,["artCalibrationVerified"]=false}.ToString());AssetDatabase.Refresh();
 }
}
#endif
