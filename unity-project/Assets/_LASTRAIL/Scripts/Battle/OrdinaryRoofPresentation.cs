using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Battle {
 // Display-only projection of the existing authored roof. No physics, muzzle or combat authority.
 public sealed class OrdinaryRoofPresentation:IDisposable {
  const int RenderLayer=31;
  readonly Dictionary<string,Transform> anchors=new Dictionary<string,Transform>(StringComparer.Ordinal);
  readonly string[] physicalIds;
  readonly List<Vector3> roofSurfaces=new List<Vector3>();
  readonly List<Sprite> skins=new List<Sprite>();
  GameObject stage,enemyModel;
  readonly List<Material> ownedMaterials=new List<Material>();
  readonly List<Mesh> ownedMeshes=new List<Mesh>();
  readonly Transform[] enemyCapsules=new Transform[2];
  readonly Vector3[] capsuleCenters=new Vector3[2];
  readonly float[] capsuleHeightsMm=new float[2];
  public float GunnerHeightMm(int slot)=>capsuleHeightsMm[slot];
  readonly Dictionary<string,GameObject> boardingCapsules=new Dictionary<string,GameObject>(StringComparer.Ordinal);
  string enemyState;
  Camera camera;
  RenderTexture texture;
  public RenderTexture Texture=>texture;
  public float WheelViewportY { get; private set; }
  public int AnchorCount=>anchors.Count;
  public OrdinaryRoofPresentation(OrdinaryRoofPresentationAssets assets,JObject rail){
   if(assets==null||assets.Prefab==null||assets.Definition==null)throw new InvalidOperationException("Original roof presentation assets are required.");
   var definition=JObject.Parse(assets.Definition.text);
   Need((string)definition["version"]=="ordinary.roof-spawn-authoring.v1.20261027"&&(string)definition["physicalDefinitionHash"]==OrdinaryRailSource.ConsistHash&&(int)definition["millimetersPerUnit"]==1000,"Roof definition differs from original physical consist.");
   physicalIds=((JArray)rail["physicalInstanceIds"]).Values<string>().ToArray();Need(physicalIds.Length==3&&physicalIds.All(id=>!string.IsNullOrWhiteSpace(id))&&physicalIds.Distinct().Count()==3,"Original three physical instance IDs required.");
   try{
    stage=new GameObject("OwnedOrdinaryRoofRenderStage"){hideFlags=HideFlags.DontSave};stage.SetActive(false);stage.transform.position=new Vector3(10000,0,0);
    var model=UnityEngine.Object.Instantiate(assets.Prefab,stage.transform,false);model.name="OriginalRoofConsist";
    foreach(var collider in model.GetComponentsInChildren<Collider>(true))collider.enabled=false;
    Need(model.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Presentation source cannot execute scene behaviours.");
    foreach(var transform in model.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=RenderLayer;
    var roles=new[]{"Engine","Combat","Gun"};
    Vector3 wheelContact = Vector3.zero;
    for(int order=0;order<roles.Length;order++){
     var frame=model.transform.Find(roles[order]);Need(frame!=null&&frame.localScale==Vector3.one,"Missing original vehicle frame.");
     var body=frame.Find("BodyCollider")?.GetComponent<BoxCollider>();Need(body!=null&&body.size==new Vector3(12,3.2f,3),"Original vehicle dimensions changed.");
     Need(Mathf.Abs(frame.localPosition.x-(27-order*13))<.0001f,"Original vehicle order or gap changed.");
     RegisterRoof(frame);
     if (order == 1) wheelContact = body.transform.TransformPoint(body.center - Vector3.up * body.size.y / 2);
     if(assets.CarBodies!=null&&assets.CarBodies.Length>0){
      Need(assets.CarBodies.Length==3&&assets.CarBounds?.Length==3,"Three independently supplied car bodies required.");
      var art=assets.CarBodies[order];var bounds=assets.CarBounds[order];
      Need(art!=null&&bounds.width>0&&bounds.height>0&&bounds.xMin>=0&&bounds.yMin>=0&&bounds.xMax<=art.width&&bounds.yMax<=art.height,"Car art bounds differ.");
      // Display skin keeps its aspect and 12m width; it does not replace collision or roof geometry.
      var sprite=Sprite.Create(art,bounds,new Vector2(.5f,1),bounds.width/12,0,SpriteMeshType.FullRect);skins.Add(sprite);
      var skin=new GameObject("ApprovedCarBody",typeof(SpriteRenderer));skin.transform.SetParent(frame,false);skin.layer=RenderLayer;skin.transform.localPosition=new Vector3(0,3.2125f,-.02f);
      var renderer=skin.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.flipX=order==0;
      if (order == 1) wheelContact = skin.transform.TransformPoint(new Vector3(0, sprite.bounds.min.y, 0));

     }
    }
    // Skins replace the entire blockout, including the two root-level roof-gap guides.
    if(skins.Count==roles.Length)
     foreach(var primitive in model.GetComponentsInChildren<MeshRenderer>(true))primitive.enabled=false;
    foreach(JObject row in (JArray)definition["slots"]){
     int order=(int)row["vehicleOrder"];Need(order==1||order==2,"Engine roof deployment is forbidden.");
     var foot=model.transform.Find((string)row["anchorPath"]);var position=(JArray)row["localPositionMm"];
     Need(foot!=null&&foot.parent==model.transform.Find(roles[order]),"Missing original vehicle foot anchor.");
     Need(Vector3.Distance(foot.localPosition,new Vector3((float)position[0],(float)position[1],(float)position[2])/1000)<.00001f,"Original foot position differs from authored definition.");
     var roof=foot.parent.Find("RoofVisual");var mesh=roof?.GetComponent<MeshFilter>();Need(mesh!=null&&mesh.sharedMesh!=null,"Actual roof mesh required.");
     float surface=roof.localPosition.y+roof.localScale.y*mesh.sharedMesh.bounds.max.y;
     Need(foot.localPosition.y>=surface&&foot.localPosition.y-surface<=.001f&&foot.localPosition.z==0,"Foot must rest on the measured roof surface.");
     anchors.Add(Key(physicalIds[order],(int)position[0]),foot);
    }
    Need(anchors.Count==5,"Five authored roof anchors required.");
    camera=new GameObject("OwnedOrdinaryRoofCamera",typeof(Camera)).GetComponent<Camera>();camera.transform.SetParent(stage.transform,false);
    // Extend only the right edge: original left edge, vertical framing and pixels per metre remain unchanged.
    const float centerX=14+(2100-1520)*8.5f/470;
    // Supplied 2D bodies already contain their authored perspective; a pitched display camera compresses them again.
    camera.transform.localPosition=new Vector3(centerX,skins.Count==roles.Length?1.6f:15,-36);camera.transform.LookAt(stage.transform.TransformPoint(new Vector3(centerX,1.6f,0)));
    camera.orthographic=true;camera.orthographicSize=8.5f;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.cullingMask=1<<RenderLayer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.enabled=false;
    var light=new GameObject("OwnedOrdinaryRoofLight",typeof(Light)).GetComponent<Light>();light.transform.SetParent(stage.transform,false);light.type=LightType.Directional;light.transform.localRotation=Quaternion.Euler(45,-30,0);light.intensity=1.2f;light.cullingMask=1<<RenderLayer;
    texture=new RenderTexture(2100,470,24,RenderTextureFormat.ARGB32){name="OwnedOrdinaryRoofTexture"};texture.Create();camera.targetTexture=texture;camera.aspect=2100f/470;
    WheelViewportY = camera.WorldToViewportPoint(wheelContact).y;
    if(assets.EnemyPrefab!=null&&assets.LiveDefinition!=null){BindEnemy(assets.EnemyPrefab,JObject.Parse(assets.LiveDefinition.text));BindEnemyRearSkin(assets);}
    // Other stages stay inactive; the host camera never observes this private render stage.
    stage.SetActive(true);try{camera.Render();}finally{stage.SetActive(false);}
   }catch{Dispose();throw;}
  }
  void BindEnemy(GameObject prefab,JObject definition){
   var entities=((JArray)definition["entities"]).OfType<JObject>().ToDictionary(e=>(string)e["authoringId"]);var layout=definition["source"]["liveLayout"];
   Need((string)entities["Enemy"]["assetPath"]=="Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/OrdinaryEnemyTrainV01.prefab","Original enemy prefab identity required");
   enemyModel=UnityEngine.Object.Instantiate(prefab,stage.transform,false);enemyModel.name="OriginalEnemyTrainPresentation";
   Need(enemyModel.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Enemy presentation cannot execute behaviours");
   var origin=layout["Enemy"]["relativeToInitialFrontMm"];
   enemyModel.transform.localPosition=new Vector3(33+(float)origin[0]/1000,(float)origin[1]/1000,(float)origin[2]/1000);
   RegisterRoof(enemyModel.transform);
   foreach(var collider in enemyModel.GetComponentsInChildren<Collider>(true))collider.enabled=false;
   foreach(var t in enemyModel.GetComponentsInChildren<Transform>(true))t.gameObject.layer=RenderLayer;
   for(int i=0;i<2;i++){
    var capsule=((JArray)entities["Gunner"+i]["shapes"]).Single(s=>(string)s["kind"]=="CAPSULE_Y"&&(string)s["role"]=="HIT_VOLUME");
    capsuleHeightsMm[i]=(float)capsule["heightMm"];var center=capsule["centerMm"];capsuleCenters[i]=new Vector3((float)center[0],(float)center[1],(float)center[2])/1000;
    float radius=(float)capsule["radiusMm"]/1000,height=(float)capsule["heightMm"]/1000;
    var body=CreateCapsule("LocalAuthoredGunnerCapsule"+i,radius,height);body.GetComponent<MeshRenderer>().sharedMaterial.color=new Color(.95f,.29f,.16f);enemyCapsules[i]=body.transform;body.SetActive(false);
   }
   enemyModel.SetActive(false);
  }
  GameObject CreateCapsule(string name,float radius,float height){
   var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name=name;body.transform.SetParent(stage.transform,false);body.layer=RenderLayer;body.GetComponent<Collider>().enabled=false;
   var mesh=UnityEngine.Object.Instantiate(body.GetComponent<MeshFilter>().sharedMesh);var vertices=mesh.vertices;for(int v=0;v<vertices.Length;v++){vertices[v]*=radius*2;vertices[v].y+=Mathf.Sign(vertices[v].y)*(height/2-2*radius);}mesh.vertices=vertices;mesh.RecalculateBounds();body.GetComponent<MeshFilter>().sharedMesh=mesh;ownedMeshes.Add(mesh);
   var material=new Material(body.GetComponent<MeshRenderer>().sharedMaterial);body.GetComponent<MeshRenderer>().sharedMaterial=material;ownedMaterials.Add(material);return body;
  }
  public void RefreshEnemies(JArray actors){
   if(camera==null)throw new ObjectDisposedException(nameof(OrdinaryRoofPresentation));
   var rows=actors.OfType<JObject>().Where(a=>(bool?)a["enemy"]==true&&(bool?)a["boarding"]!=true&&a["footRelativeToFrontMm"] is JArray).OrderBy(a=>(int)a["slot"]).ToArray();
   Need(rows.Length==0||enemyModel!=null&&rows.Length==2,"Original enemy presentation binding required");
   string state=new JObject{["boarding"]=new JArray(actors.Where(a=>(bool?)a["boarding"]==true).Select(r=>new JObject{["id"]=(string)r["id"],["alive"]=(int)r["hp"]>0,["height"]=r["hitHeightMm"].DeepClone(),["radius"]=r["hitRadiusMm"].DeepClone(),["foot"]=r["footRelativeToFrontMm"].DeepClone(),["tauntedBy"]=(string)r["tauntedBy"]})),["gunners"]=new JArray(rows.Select(r=>new JObject{["slot"]=r["slot"].DeepClone(),["alive"]=(int)r["hp"]>0,["foot"]=r["footRelativeToFrontMm"].DeepClone()}))}.ToString(Newtonsoft.Json.Formatting.None);
   if(state==enemyState)return;enemyState=state;
   if(enemyModel!=null)enemyModel.SetActive(rows.Length>0);
   for(int i=0;i<enemyCapsules.Length;i++)if(enemyCapsules[i]!=null){bool show=rows.Length>i&&(int)rows[i]["hp"]>0;enemyCapsules[i].gameObject.SetActive(show);if(show){var p=rows[i]["footRelativeToFrontMm"];enemyCapsules[i].localPosition=new Vector3(33+(float)p[0]/1000,(float)p[1]/1000,(float)p[2]/1000)+capsuleCenters[i];}}
   var live=new HashSet<string>(StringComparer.Ordinal);
   foreach(var row in actors.Where(a=>(bool?)a["boarding"]==true)){
    string id=(string)row["id"];live.Add(id);float height=(float)row["hitHeightMm"]/1000,radius=(float)row["hitRadiusMm"]/1000;
    Need(height>=2*radius&&radius>0,"Original boarding capsule dimensions required");
    if(!boardingCapsules.TryGetValue(id,out var body)){
     body=CreateCapsule("LocalAuthoredBoardingCapsule:"+id,radius,height);boardingCapsules.Add(id,body);
    }
    body.SetActive((int)row["hp"]>0);var foot=row["footRelativeToFrontMm"];body.transform.localPosition=new Vector3(33+(float)foot[0]/1000,(float)foot[1]/1000+height/2,(float)foot[2]/1000);
    body.GetComponent<MeshRenderer>().sharedMaterial.color=(string)row["tauntedBy"]=="op_kyle"?new Color(1,.75f,.12f):new Color(.85f,.26f,.17f);
   }
   foreach(var pair in boardingCapsules)if(!live.Contains(pair.Key))pair.Value.SetActive(false);
   stage.SetActive(true);try{camera.Render();}finally{stage.SetActive(false);}
  }
  void BindEnemyRearSkin(OrdinaryRoofPresentationAssets assets){
   if(assets.CarBodies==null||assets.CarBodies.Length!=3)return;
   var body=enemyModel.transform.Find("BodyCollider").GetComponent<BoxCollider>();
   var bounds=assets.CarBounds[1];var art=assets.CarBodies[1];
   // Reuse the same roof angle and metres-per-pixel as our carriage; geometry stays authoritative.
   var sprite=Sprite.Create(art,bounds,new Vector2(.5f,1),bounds.width/body.size.x,0,SpriteMeshType.FullRect);skins.Add(sprite);
   foreach(var mesh in enemyModel.GetComponentsInChildren<MeshRenderer>(true))mesh.enabled=false;
   var skin=new GameObject("ReusedEnemyRearCarBody",typeof(SpriteRenderer));skin.transform.SetParent(enemyModel.transform,false);skin.layer=RenderLayer;
   skin.transform.localPosition=new Vector3(0,3.2125f,-.02f);var renderer=skin.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=new Color(1,.68f,.60f);
   float rear=body.center.x-body.size.x/2;
   // Rear-facing red marker and end handrail distinguish the trailing end without moving the hit box.
   void Detail(string name,Vector3 position,Vector3 scale,Color color){
    var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(enemyModel.transform,false);part.layer=RenderLayer;
    part.transform.localPosition=position;part.transform.localScale=scale;part.GetComponent<Collider>().enabled=false;
    var material=new Material(part.GetComponent<MeshRenderer>().sharedMaterial);material.color=color;part.GetComponent<MeshRenderer>().sharedMaterial=material;ownedMaterials.Add(material);
   }
   Detail("RearEndHandrail",new Vector3(rear+.45f,1.75f,-.12f),new Vector3(.12f,1.65f,.12f),new Color(.8f,.67f,.35f));
   Detail("RearRedTailMarker",new Vector3(rear+.25f,2.5f,-.18f),new Vector3(.38f,.45f,.12f),new Color(1,.06f,.025f));
  }
  static string Key(string physicalId,int localXmm)=>physicalId+":"+localXmm;
  static void Need(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
  public Vector2 Project(JToken actor){
   if(camera==null)throw new ObjectDisposedException(nameof(OrdinaryRoofPresentation));
   string id=(string)actor["physicalInstanceId"];int order=(int)actor["physicalOrder"];
   Need(order>0&&order<physicalIds.Length&&physicalIds[order]==id,"Actor physical identity/order differs from original consist.");
   Need(anchors.TryGetValue(Key(id,(int)actor["localXmm"]),out var foot),"Current actor position has no supplied authored roof anchor; do not reset to initial placement.");
   var point=camera.WorldToViewportPoint(foot.position);Need(point.z>0&&point.x>=0&&point.x<=1&&point.y>=0&&point.y<=1,"Roof foot lies outside presentation camera.");return new Vector2(point.x,point.y);
  }
  public Vector2 ProjectRelativeToFront(double xMm,double yMm,double zMm){
   if(camera==null||stage==null)throw new ObjectDisposedException(nameof(OrdinaryRoofPresentation));
   Need(new[]{xMm,yMm,zMm}.All(v=>!double.IsNaN(v)&&!double.IsInfinity(v)&&Math.Abs(v)<=1000000000),"Finite original millimeter coordinate required.");
   // Original prefab front is world 33m; the rail arc front is 58m because its origin is world -25m.
   var world=stage.transform.TransformPoint(new Vector3(33f+(float)(xMm/1000),(float)(yMm/1000),(float)(zMm/1000)));
   var point=camera.WorldToViewportPoint(world);Need(point.z>0,"Projected point lies behind the physical roof camera.");
   return new Vector2(point.x,point.y); // Preserve offscreen positions; clipping belongs to the display, never geometry.
  }
  void RegisterRoof(Transform car){
   var roof=car.Find("RoofVisual");var mesh=roof?.GetComponent<MeshFilter>();
   Need(mesh!=null&&mesh.sharedMesh!=null,"Authored roof surface required for ground FX");
   var bounds=mesh.sharedMesh.bounds;
   var left=stage.transform.InverseTransformPoint(roof.TransformPoint(new Vector3(bounds.min.x,bounds.max.y,0)));
   var right=stage.transform.InverseTransformPoint(roof.TransformPoint(new Vector3(bounds.max.x,bounds.max.y,0)));
   roofSurfaces.Add(new Vector3(Mathf.Min(left.x,right.x),Mathf.Max(left.x,right.x),left.y));
  }
  public Vector2? ProjectRoofRelativeToFront(double xMm,double zMm){
   if(stage==null)throw new ObjectDisposedException(nameof(OrdinaryRoofPresentation));
   double x=33+xMm/1000;
   foreach(var surface in roofSurfaces)if(x>=surface.x&&x<=surface.y)return ProjectRelativeToFront(xMm,surface.z*1000d,zMm);
   return null;
  }
  public void Dispose(){
   if(camera!=null)camera.targetTexture=null;
   if(stage!=null){stage.SetActive(false);Destroy(stage);stage=null;}camera=null;boardingCapsules.Clear();
   if(texture!=null){texture.Release();Destroy(texture);texture=null;}anchors.Clear();roofSurfaces.Clear();
   foreach(var skin in skins)Destroy(skin);skins.Clear();foreach(var material in ownedMaterials)Destroy(material);ownedMaterials.Clear();foreach(var mesh in ownedMeshes)Destroy(mesh);ownedMeshes.Clear();
  }
  static void Destroy(UnityEngine.Object value){if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
 }
}
