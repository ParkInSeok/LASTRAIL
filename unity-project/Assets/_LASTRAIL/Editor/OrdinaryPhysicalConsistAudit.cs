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

// Read-only asset evidence; does not publish a runtime geometry or invent account mappings.
public static class OrdinaryPhysicalConsistAudit {
 const string Root="Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoringV01/";
 const string Output="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/PhysicalConsist793/measurement.json";
 static JArray V(Vector3 p)=>new JArray(p.x,p.y,p.z);
 static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static JObject Measure(GameObject root,JObject definition,JArray exports){
  Require(root.transform.lossyScale==Vector3.one,"root_scale_changed");
  var cars=new JArray();float previousFront=0;bool first=true;
  foreach(JObject binding in (JArray)definition["bindings"]){
   int slot=(int)binding["templateSlot"];var frame=root.transform.Find((string)binding["framePath"]);
   var body=root.transform.Find((string)binding["colliderPath"]).GetComponent<BoxCollider>();
   Require(frame.lossyScale==Vector3.one&&body.transform.lossyScale==Vector3.one,"car_scale_changed");
   Require(frame.rotation==Quaternion.identity&&body.transform.rotation==Quaternion.identity,"axis_changed");
   var ids=((JArray)binding["anchorIds"]).Values<string>().ToArray();var probe=new GameObject("ReadOnlyMeasurement");
   try{var source=probe.AddComponent<BPhysicalSceneSource>();source.Bind("b-authoring-v01-car-"+slot,(string)binding["unitContractId"],(float)binding["millimetersPerUnit"],frame,body,ids,ids.Select(id=>frame.Find(id)).ToArray());
    Require(JToken.DeepEquals(source.CaptureExport(),exports[slot]["measurement"]),"saved_export_mismatch");
   }finally{UnityEngine.Object.DestroyImmediate(probe);}
   var center=body.transform.TransformPoint(body.center);float rear=(center.x-body.size.x/2)*1000,front=(center.x+body.size.x/2)*1000;
   Require(body.size==new Vector3(10,3.2f,3),"body_dimension_changed");
   if(!first)Require(rear-previousFront==1000,"gap_changed");
   cars.Add(new JObject{["templateSlot"]=slot,["framePath"]=frame.name,["worldPositionUnits"]=V(frame.position),["worldScale"]=V(frame.lossyScale),["bodySizeMm"]=V(body.size*1000),["worldRearXmm"]=rear,["worldFrontXmm"]=front,["previousBodyGapMm"]=first?JValue.CreateNull():new JValue(rear-previousFront),["runtimeCarriageInstanceId"]=binding["runtimeCarriageInstanceId"].DeepClone(),["anchorCount"]=ids.Length});
   previousFront=front;first=false;
  }
  Require(cars.Count==5,"template_count_changed");
  var start=root.transform.Find((string)definition["railStartPath"]).position;var end=root.transform.Find((string)definition["railEndPath"]).position;
  Require(start==new Vector3(-25,0,0)&&end==new Vector3(75,0,0),"rail_endpoints_changed");
  float min=cars.Min(c=>(float)c["worldRearXmm"]),max=cars.Max(c=>(float)c["worldFrontXmm"]);
  Require(min>=start.x*1000&&max<=end.x*1000,"authored_body_outside_rail");
  return new JObject{["rootScale"]=V(root.transform.lossyScale),["millimetersPerUnit"]=1000,["axis"]=definition["axis"].DeepClone(),["templates"]=cars,["railStartUnits"]=V(start),["railEndUnits"]=V(end),["railLengthMm"]=Vector3.Distance(start,end)*1000,["authoredEnvelopeMm"]=max-min,["authoredPositiveXFrontArcMm"]=max-start.x*1000,["authoredTailArcMm"]=min-start.x*1000,["locomotiveBinding"]=JValue.CreateNull(),["ordinaryRuntimeGeometry"]=false};
 }
 public static void Run(){
  Require(!File.Exists(Output),"preserve_prior_audit");
  var files=new[]{Root+"definition.json",Root+"measurements.json",Root+"BPhysicalAuthoringV01.prefab",Root+"BPhysicalAuthoringV01.unity"};
  var hashes=files.ToDictionary(p=>p,Hash);var definition=JObject.Parse(File.ReadAllText(files[0]));var exports=(JArray)JObject.Parse(File.ReadAllText(files[1]))["instances"];
  var prefab=PrefabUtility.LoadPrefabContents(files[2]);JObject measured;
  try{measured=Measure(prefab,definition,exports);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
  var scene=EditorSceneManager.OpenScene(files[3],OpenSceneMode.Additive);
  try{var root=scene.GetRootGameObjects().Single(g=>g.name=="BPhysicalAuthoringV01");Require(JToken.DeepEquals(measured,Measure(root,definition,exports)),"scene_prefab_mismatch");}finally{EditorSceneManager.CloseScene(scene,true);}
  foreach(var entry in hashes)Require(entry.Value==Hash(entry.Key),"source_modified");
  const string fixture="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/TrainInitial777/consumed-export.json";
  var start=(JObject)JObject.Parse(File.ReadAllText(fixture))["originalStart"];
  Directory.CreateDirectory(Path.GetDirectoryName(Output));
  File.WriteAllText(Output,new JObject{["classification"]="MEASURED_AUTHORING_DRAFT_NOT_RUNTIME_GEOMETRY",["result"]="PASSED",["sourceHashes"]=JObject.FromObject(hashes),["sceneMatchesPrefab"]=true,["assetsUnchanged"]=true,["measurement"]=measured,["originalStartFixtureSha256"]=Hash(fixture),["selectedLoadout"]=start["admission"]["loadout"].DeepClone(),["selectedOwnedCarriages"]=start["admission"]["ownedState"]["carriages"].DeepClone(),["selectedToTemplateMapping"]=JValue.CreateNull(),["missing"]=new JArray("LOCOMOTIVE_ASSET_AND_DIMENSIONS","ACCOUNT_ID_TO_PHYSICAL_ASSET_MAPPING","APPROVED_12M_ALIGNMENT","ORDINARY_RAIL_GEOMETRY_BINDING_AND_VALID_INITIAL_POSITION"),["playerTested"]=false}.ToString());
 }
}
#endif
