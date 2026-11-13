#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Creates a NEW editable authoring baseline. Never publishes runtime authority.
public static class BPhysicalAuthoringV01Builder {
 const string Root="Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoringV01";
 const string Unit="b-physical-authoring-millimeter-v01";
 static Transform Child(Transform parent,string name,Vector3 position) {
  var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;
 }
 static GameObject Box(Transform parent,string name,Vector3 center,Vector3 size,Material material,bool collider) {
  var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
  g.transform.localPosition=center;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
  if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
 }
 static Material Material(string name,Color color) {
  var shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
  if(!shader)throw new InvalidOperationException("authoring_shader_missing");
  var material=new Material(shader){name=name,color=color};
  AssetDatabase.CreateAsset(material,Root+"/"+name+".mat");return material;
 }
 public static void Build() {
  var absolute=Path.GetFullPath(Root);
  if(Directory.Exists(absolute))throw new InvalidOperationException("authoring_output_exists_preserve_original");
  Directory.CreateDirectory(absolute);AssetDatabase.Refresh();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var root=new GameObject("BPhysicalAuthoringV01");
  var bodyMaterial=Material("Body",new Color(.18f,.26f,.31f));
  var roofMaterial=Material("Roof",new Color(.4f,.55f,.6f));
  var railMaterial=Material("Rail",new Color(.3f,.32f,.34f));
  var exports=new JArray();var bindings=new JArray();var connections=new JArray();
  for(int slot=0;slot<5;slot++){
   var frame=Child(root.transform,"CarTemplate_"+slot,new Vector3(slot*11,0,0));
   // Visual mesh and physical body are separate objects, with the same authored shape.
   Box(frame,"BodyVisual",new Vector3(0,1.6f,0),new Vector3(10,3.2f,3),bodyMaterial,false);
   Box(frame,"RoofVisual",new Vector3(0,3.2f,0),new Vector3(10,.025f,3),roofMaterial,false);
   var body=Child(frame,"BodyCollider",Vector3.zero).gameObject.AddComponent<BoxCollider>();
   body.size=new Vector3(10,3.2f,3);body.center=new Vector3(0,1.6f,0);
   var points=new (string,Vector3)[]{
    ("RoofRearLeft",new Vector3(-5,3.2f,-1.5f)),("RoofFrontLeft",new Vector3(5,3.2f,-1.5f)),
    ("RoofFrontRight",new Vector3(5,3.2f,1.5f)),("RoofRearRight",new Vector3(-5,3.2f,1.5f)),
    ("Foot",new Vector3(0,3.2f,0)),("Fire",new Vector3(.35f,4.5f,0)),("Hit",new Vector3(0,4.1f,0)),
    ("LeftApproachStart",new Vector3(0,0,-4)),("LeftApproachEnd",new Vector3(0,0,-1.5f)),
    ("LeftClimbStart",new Vector3(0,0,-1.5f)),("LeftClimbEnd",new Vector3(0,3.2f,-1.5f)),
    ("RightApproachStart",new Vector3(0,0,4)),("RightApproachEnd",new Vector3(0,0,1.5f)),
    ("RightClimbStart",new Vector3(0,0,1.5f)),("RightClimbEnd",new Vector3(0,3.2f,1.5f)),
    ("CouplerRear",new Vector3(-5,3.2f,0)),("CouplerFront",new Vector3(5,3.2f,0))
   };
   var anchors=points.Select(p=>Child(frame,p.Item1,p.Item2)).ToArray();
   var probe=new GameObject("MeasurementExport");
   JObject packet,export;
   try {
    var source=probe.AddComponent<BPhysicalSceneSource>();
    source.Bind("b-authoring-v01-car-"+slot,Unit,1000,frame,body,points.Select(p=>p.Item1).ToArray(),anchors);
    packet=source.Capture();
    export=source.CaptureExport();
    if(!JToken.DeepEquals(packet,source.Capture()))throw new InvalidOperationException("measurement_not_reused");
   }finally{UnityEngine.Object.DestroyImmediate(probe);}
   exports.Add(new JObject{["templateSlot"]=slot,["measurement"]=export});
   bindings.Add(new JObject{["templateSlot"]=slot,["framePath"]=frame.name,["colliderPath"]=frame.name+"/BodyCollider",
    ["unitContractId"]=Unit,["millimetersPerUnit"]=1000,["anchorIds"]=new JArray(points.Select(p=>p.Item1)),
    ["frameToNative"]=new JArray(1,0,0,slot*11000,0,1,0,0,0,0,1,0,0,0,0,1),
    ["transformTranslationUnit"]="MILLIMETERS",["runtimeCarriageInstanceId"]=null});
   if(slot>0){
    Box(root.transform,"Connection_"+(slot-1)+"_"+slot,new Vector3(slot*11-5.5f,3.2f,0),new Vector3(1,.025f,.8f),roofMaterial,false);
    connections.Add(new JObject{["fromTemplateSlot"]=slot-1,["toTemplateSlot"]=slot,["fromAnchorId"]="CouplerFront",["toAnchorId"]="CouplerRear"});
   }
  }
  var rail=Child(root.transform,"Rail",Vector3.zero);
  Child(rail,"RailStart",new Vector3(-25,0,0));Child(rail,"RailEnd",new Vector3(75,0,0));
  for(int side=-1;side<=1;side+=2)Box(rail,"Rail_"+side,new Vector3(25,-.1f,side),new Vector3(100,.15f,.12f),railMaterial,false);
  var definition=new JObject{["version"]="b.physical-authoring.v1.20261020",["status"]="AUTHORED_DRAFT_NOT_RUNTIME_ADMITTED",
   ["unitContractId"]=Unit,["millimetersPerUnit"]=1000,["axis"]="X_FORWARD_Y_UP_Z_SIDE",
   ["dimensionOrigin"]="NEW_AUTHORING_BASELINE_NOT_HISTORICAL_APPROVAL_OR_ART_MEASUREMENT",
   ["carLengthMm"]=10000,["carWidthMm"]=3000,["carBodyHeightMm"]=3200,["gapMm"]=1000,
   ["bindings"]=bindings,["connections"]=connections,["railStartPath"]="Rail/RailStart",["railEndPath"]="Rail/RailEnd",
   ["railSpeed"]=null,["stepTicks"]=null,["runtimeNodeBoundaries"]=null,
   ["missingRuntimeBindings"]=new JArray("ACCOUNT_CARRIAGE_INSTANCES","NATIVE_GEOMETRY_CONSUMER","PINNED_RAIL_SPEED_AND_NODE_BOUNDARIES","AUTHENTICATED_AUTHORING_PUBLICATION")};
  File.WriteAllText(Root+"/definition.json",definition.ToString());
  File.WriteAllText(Root+"/measurements.json",new JObject{["version"]="b.physical-authoring-export.v1.20261020",["instances"]=exports}.ToString());
  PrefabUtility.SaveAsPrefabAsset(root,Root+"/BPhysicalAuthoringV01.prefab");
  var cameraObject=new GameObject("AuthoringCamera");var camera=cameraObject.AddComponent<Camera>();
  camera.transform.position=new Vector3(22,39,-45);camera.transform.LookAt(new Vector3(22,1.5f,0));
  camera.orthographic=true;camera.orthographicSize=18;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.065f,.08f);
  var light=new GameObject("AuthoringLight").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-30,0);light.intensity=1.4f;
  EditorSceneManager.SaveScene(scene,Root+"/BPhysicalAuthoringV01.unity");
  var target=new RenderTexture(1600,900,24);var previous=RenderTexture.active;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
   File.WriteAllBytes(Root+"/reference-only-preview.png",texture.EncodeToPNG());
  }finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);}
  if(root.GetComponentsInChildren<BoxCollider>().Length!=5)throw new InvalidOperationException("authoring_collider_count");
  AssetDatabase.Refresh();
  File.WriteAllText(Root+"/verification.json",new JObject{["generated"]=true,["colliders"]=5,["anchorsPerCar"]=17,["measurementReuse"]=true,
   ["unityImportAndRender"]=true,["serverReaderVerified"]=false,["runtimeAdmission"]=false,["playerVerified"]=false}.ToString());
 }
 public static void VerifySaved() {
  var definition=JObject.Parse(File.ReadAllText(Root+"/definition.json"));
  var exports=(JArray)JObject.Parse(File.ReadAllText(Root+"/measurements.json"))["instances"];
  var root=PrefabUtility.LoadPrefabContents(Root+"/BPhysicalAuthoringV01.prefab");
  try {
   if(root.GetComponentsInChildren<BoxCollider>(true).Length!=5)throw new InvalidOperationException("saved_collider_count");
   foreach(JObject binding in (JArray)definition["bindings"]){
    int slot=(int)binding["templateSlot"];
    var frame=root.transform.Find((string)binding["framePath"]);
    var body=root.transform.Find((string)binding["colliderPath"]).GetComponent<BoxCollider>();
    var ids=((JArray)binding["anchorIds"]).Values<string>().ToArray();
    var anchors=ids.Select(id=>frame.Find(id)).ToArray();
    if(anchors.Any(t=>!t))throw new InvalidOperationException("saved_anchor_missing");
    var probe=new GameObject("SavedMeasurementVerification");
    try{
     var source=probe.AddComponent<BPhysicalSceneSource>();
     source.Bind("b-authoring-v01-car-"+slot,Unit,1000,frame,body,ids,anchors);
     if(!JToken.DeepEquals(source.CaptureExport(),exports[slot]["measurement"]))
      throw new InvalidOperationException("saved_measurement_changed");
    }finally{UnityEngine.Object.DestroyImmediate(probe);}
    var visual=frame.Find("BodyVisual").gameObject;var roof=frame.Find("RoofVisual").gameObject;
    visual.SetActive(false);
    if(!roof.activeInHierarchy||!body.enabled||!body.gameObject.activeInHierarchy)throw new InvalidOperationException("visual_physics_coupled");
    visual.SetActive(true);roof.SetActive(false);
    if(!visual.activeInHierarchy||!body.gameObject.activeInHierarchy)throw new InvalidOperationException("roof_body_coupled");
    roof.SetActive(true);
   }
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  var verification=JObject.Parse(File.ReadAllText(Root+"/verification.json"));
  verification["savedPrefabReloadMeasured"]=true;verification["independentBodyRoofColliderVisibility"]=true;
  File.WriteAllText(Root+"/verification.json",verification.ToString());
 }
}
#endif
