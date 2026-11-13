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

// Approved length-only authoring variant. Account identity and locomotive remain unbound.
public static class BPhysicalAuthoring12mBuilder {
 const string Original="Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoringV01/";
 const string Target="Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoring12mV01/";
 const string Qa="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/Carriage12m794/";
 static void Need(bool value,string error){if(!value)throw new InvalidOperationException(error);}
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static bool EndAnchor(string name)=>name.StartsWith("Roof",StringComparison.Ordinal)||name.StartsWith("Coupler",StringComparison.Ordinal);
 static JArray Capture(GameObject root,JObject definition){
  var result=new JArray();Need(root.transform.lossyScale==Vector3.one,"root_scale");
  var before=JObject.Parse(File.ReadAllText(Original+"measurements.json"));
  foreach(JObject b in (JArray)definition["bindings"]){
   int slot=(int)b["templateSlot"];var frame=root.transform.Find((string)b["framePath"]);var body=frame.Find("BodyCollider").GetComponent<BoxCollider>();
   Need(frame.localPosition==new Vector3(slot*13,0,0)&&frame.lossyScale==Vector3.one&&body.transform.lossyScale==Vector3.one,"frame_position_scale");
   Need(body.size==new Vector3(12,3.2f,3)&&body.center==new Vector3(0,1.6f,0),"collider_size");
   var visual=frame.Find("BodyVisual");var roof=frame.Find("RoofVisual");
   Need(visual.localScale==new Vector3(12,3.2f,3)&&roof.localScale==new Vector3(12,.025f,3),"visual_size");
   Need(frame.Find("Foot").localPosition==new Vector3(0,3.2f,0)&&frame.Find("Fire").localPosition==new Vector3(.35f,4.5f,0)&&frame.Find("Hit").localPosition==new Vector3(0,4.1f,0),"fixed_anchor_changed");
   var ids=((JArray)b["anchorIds"]).Values<string>().ToArray();
   foreach(var id in ids){var old=((JArray)before["instances"][slot]["measurement"]["packet"]["anchors"]).Single(a=>(string)a["id"]==id)["positionMm"];
    var expected=new Vector3((float)old[0]/1000,(float)old[1]/1000,(float)old[2]/1000);if(EndAnchor(id))expected.x=Math.Sign(expected.x)*6;
    Need(Vector3.Distance(frame.Find(id).localPosition,expected)<.00001f,"anchor_role_"+id);
   }
   visual.gameObject.SetActive(false);Need(roof.gameObject.activeInHierarchy&&body.enabled,"body_visibility_coupled");visual.gameObject.SetActive(true);
   roof.gameObject.SetActive(false);Need(visual.gameObject.activeInHierarchy&&body.enabled,"roof_visibility_coupled");roof.gameObject.SetActive(true);
   var probe=new GameObject("Measure12m");try{var source=probe.AddComponent<BPhysicalSceneSource>();source.Bind("b-authoring-12m-v01-car-"+slot,(string)b["unitContractId"],1000,frame,body,ids,ids.Select(id=>frame.Find(id)).ToArray());result.Add(new JObject{["templateSlot"]=slot,["measurement"]=source.CaptureExport()});}finally{UnityEngine.Object.DestroyImmediate(probe);}
   if(slot>0){var previous=root.transform.Find("CarTemplate_"+(slot-1));Need(Math.Abs((frame.position.x-6)-(previous.position.x+6)-1)<.00001f,"body_gap");var connection=root.transform.Find("Connection_"+(slot-1)+"_"+slot);Need(connection.localPosition==new Vector3(slot*13-6.5f,3.2f,0)&&connection.localScale==new Vector3(1,.025f,.8f),"connection_alignment");}
  }
  Need(root.transform.Find("Rail/RailStart").localPosition==new Vector3(-25,0,0)&&root.transform.Find("Rail/RailEnd").localPosition==new Vector3(75,0,0),"rail_changed");return result;
 }
 public static void Build(){
  Need(!Directory.Exists(Target),"preserve_existing_variant");Directory.CreateDirectory(Qa);
  var originals=Directory.GetFiles(Original).ToDictionary(p=>p,Hash);
  var scene=EditorSceneManager.OpenScene(Original+"BPhysicalAuthoringV01.unity",OpenSceneMode.Single);
  var root=scene.GetRootGameObjects().Single(g=>g.name=="BPhysicalAuthoringV01");root.name="BPhysicalAuthoring12mV01";
  var definition=JObject.Parse(File.ReadAllText(Original+"definition.json"));
  definition["version"]="b.physical-authoring.12m.v1.20261027";definition["carLengthMm"]=12000;
  definition["approval"]="ORDINARY-CARRIAGE-12M-ALIGNMENT-v1.20261027";
  definition["derivedFrom"]=new JObject{["path"]=Original,["prefabSha256"]=Hash(Original+"BPhysicalAuthoringV01.prefab")};
  definition["anchorPolicy"]=new JObject{["endAnchors"]="MOVE_X_TO_BODY_END_KEEP_Y_Z",["footFireHitAndSideApproachClimb"]="PRESERVE_LOCAL_POSITION"};
  foreach(JObject b in (JArray)definition["bindings"]){
   int slot=(int)b["templateSlot"];var frame=root.transform.Find((string)b["framePath"]);frame.localPosition=new Vector3(slot*13,0,0);
   foreach(string name in new[]{"BodyVisual","RoofVisual"}){var t=frame.Find(name);var size=t.localScale;size.x=12;t.localScale=size;}
   var body=frame.Find("BodyCollider").GetComponent<BoxCollider>();var dimensions=body.size;dimensions.x=12;body.size=dimensions;
   foreach(var id in ((JArray)b["anchorIds"]).Values<string>().Where(EndAnchor)){var t=frame.Find(id);var p=t.localPosition;p.x=Math.Sign(p.x)*6;t.localPosition=p;}
   b["frameToNative"][3]=slot*13000;
   if(slot>0)root.transform.Find("Connection_"+(slot-1)+"_"+slot).localPosition=new Vector3(slot*13-6.5f,3.2f,0);
  }
  var measured=Capture(root,definition);Directory.CreateDirectory(Target);
  File.WriteAllText(Target+"definition.json",definition.ToString());File.WriteAllText(Target+"measurements.json",new JObject{["version"]="b.physical-authoring-export.12m.v1.20261027",["instances"]=measured}.ToString());
  PrefabUtility.SaveAsPrefabAsset(root,Target+"BPhysicalAuthoring12mV01.prefab");
  var camera=scene.GetRootGameObjects().Select(g=>g.GetComponent<Camera>()).Single(c=>c!=null);camera.transform.position=new Vector3(26,42,-51);camera.transform.LookAt(new Vector3(26,1.5f,0));camera.orthographicSize=21;
  EditorSceneManager.SaveScene(scene,Target+"BPhysicalAuthoring12mV01.unity");AssetDatabase.Refresh();
  var saved=PrefabUtility.LoadPrefabContents(Target+"BPhysicalAuthoring12mV01.prefab");try{Need(JToken.DeepEquals(Capture(saved,definition),measured),"prefab_reload_mismatch");}finally{PrefabUtility.UnloadPrefabContents(saved);}
  var reopened=EditorSceneManager.OpenScene(Target+"BPhysicalAuthoring12mV01.unity",OpenSceneMode.Single);root=reopened.GetRootGameObjects().Single(g=>g.name=="BPhysicalAuthoring12mV01");Need(JToken.DeepEquals(Capture(root,definition),measured),"scene_reload_mismatch");
  camera=reopened.GetRootGameObjects().Select(g=>g.GetComponent<Camera>()).Single(c=>c!=null);
  var rt=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);var previousRt=RenderTexture.active;
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Qa+"reference-only-preview.png",texture.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=previousRt;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
  foreach(var p in originals)Need(Hash(p.Key)==p.Value,"original_modified");
  File.WriteAllText(Qa+"verification.json",new JObject{["result"]="PASSED",["prefabAndSceneReloadEqual"]=true,["sourceFilesUnchanged"]=true,["bodyLengthMm"]=12000,["bodyGapMm"]=1000,["rootScale"]=new JArray(1,1,1),["anchorCount"]=85,["templateEnvelopeMm"]=64000,["railLengthMm"]=100000,["originalHashes"]=JObject.FromObject(originals),["variantHashes"]=JObject.FromObject(Directory.GetFiles(Target).Where(p=>!p.EndsWith(".meta")).ToDictionary(p=>p,Hash)),["dependencies"]=new JArray(Original+"Body.mat",Original+"Roof.mat",Original+"Rail.mat"),["runtimeMapped"]=false,["actualWeaponAndActorAlignmentTested"]=false,["playerTested"]=false}.ToString());
 }
}
#endif
