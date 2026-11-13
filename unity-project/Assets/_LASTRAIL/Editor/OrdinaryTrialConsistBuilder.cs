#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Approved role assets and measured selection composition, not server-issued physical instances.
public static class OrdinaryTrialConsistBuilder {
 const string Source="Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoring12mV01/";
 const string Rail="Assets/_LASTRAIL/Content/Battle/OrdinaryFiniteRail500mV01/";
 const string Target="Assets/_LASTRAIL/Content/Battle/OrdinaryTrialConsistV01/";
 const string Qa="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/EngineConsist796/";
 const string Fixture="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/TrainInitial777/consumed-export.json";
 static void Need(bool ok,string error){if(!ok)throw new InvalidOperationException(error);}
 static string Hash(string p){using var s=SHA256.Create();return BitConverter.ToString(s.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
 static JObject Measure(GameObject root,JArray mappings){
  var rows=new JArray();float previousRear=0;
  foreach(JObject row in mappings){int order=(int)row["order"];var frame=root.transform.Find((string)row["objectName"]);var body=frame.Find("BodyCollider").GetComponent<BoxCollider>();
   Need(root.transform.lossyScale==Vector3.one&&frame.lossyScale==Vector3.one&&body.transform.lossyScale==Vector3.one,"scale");
   Need(body.size==new Vector3(12,3.2f,3),"body_dimensions");
   float front=frame.position.x+6+25,rear=frame.position.x-6+25;
   if(order>0)Need(Math.Abs(previousRear-front-1)<.00001f,"coupler_gap");previousRear=rear;
   Need(frame.Find("CouplerFront").localPosition==new Vector3(6,3.2f,0)&&frame.Find("CouplerRear").localPosition==new Vector3(-6,3.2f,0),"coupler_orientation");
   var ids=frame.Cast<Transform>().Where(t=>t.name!="BodyVisual"&&t.name!="RoofVisual"&&t.name!="BodyCollider").Select(t=>t.name).ToArray();Need(ids.Length==17,"anchor_count");
   var probe=new GameObject("ConsistMeasurement");try{var source=probe.AddComponent<BPhysicalSceneSource>();source.Bind("ordinary-trial-v01-"+(string)row["role"],"b-physical-authoring-millimeter-v01",1000,frame,body,ids,ids.Select(id=>frame.Find(id)).ToArray());
    rows.Add(new JObject{["role"]=row["role"].DeepClone(),["selectionId"]=row["selectionId"].DeepClone(),["order"]=order,["frontArcMm"]=front*1000,["rearArcMm"]=rear*1000,["measurement"]=source.CaptureExport()});
   }finally{UnityEngine.Object.DestroyImmediate(probe);}
  }
  Need(rows.Count==3&&(float)rows[0]["frontArcMm"]==58000&&(float)rows[2]["rearArcMm"]==20000,"consist_endpoints");
  return new JObject{["vehicles"]=rows,["totalLengthMm"]=38000,["tailArcMm"]=20000,["headArcMm"]=58000,["carriageFootAnchors"]=2,["requiredOperators"]=5,["fiveActorPlacementVerified"]=false};
 }
 public static void Build(){
  Need(Application.isBatchMode&&!Directory.Exists(Target),"fresh_batch_output_required");
  var preserved=Directory.GetFiles(Source).Concat(Directory.GetFiles(Rail)).ToDictionary(p=>p,Hash);
  var fixture=JObject.Parse(File.ReadAllText(Fixture));var start=(JObject)fixture["originalStart"];
  var original=OrdinaryFiveStartInput.Read(start,(string)fixture["accountId"],(string)fixture["runId"]);
  var selection=(JObject)start["admission"]["loadout"];var cars=TrainLoadoutWrite.ValidateSelection(selection);
  Need(original.Train.TrainId=="train_a"&&(string)selection["trainId"]=="train_a"&&cars[0]=="car_combat"&&cars[1]=="car_gun"&&cars.Skip(2).All(c=>c==null),"fixture_selection_changed");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Directory.CreateDirectory(Target);Directory.CreateDirectory(Qa);
  var root=new GameObject("OrdinaryTrialConsistV01");var definitions=new JArray();
  var source=PrefabUtility.LoadPrefabContents(Source+"BPhysicalAuthoring12mV01.prefab");
  try{string[] roles={"Engine","Combat","Gun"};string[] selected={original.Train.TrainId,cars[0],cars[1]};
   for(int i=0;i<roles.Length;i++){
    var role=UnityEngine.Object.Instantiate(source.transform.Find("CarTemplate_0").gameObject);role.name=roles[i];role.transform.SetParent(null);role.transform.position=Vector3.zero;
    PrefabUtility.SaveAsPrefabAsset(role,Target+roles[i]+".prefab");UnityEngine.Object.DestroyImmediate(role);
    var placed=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Target+roles[i]+".prefab"),scene);placed.transform.SetParent(root.transform,false);placed.transform.localPosition=new Vector3(27-i*13,0,0);
    definitions.Add(new JObject{["role"]=roles[i],["selectionId"]=selected[i],["order"]=i,["sourceSlot"]=i==0?JValue.CreateNull():new JValue(i-1),["objectName"]=roles[i],["assetPath"]=Target+roles[i]+".prefab",["bodyLengthMm"]=12000,["bodyWidthMm"]=3000,["bodyHeightMm"]=3200,["gapAfterMm"]=i==2?0:1000,["physicalInstanceId"]=JValue.CreateNull(),["operatorPlacementAuthorized"]=i==0?new JValue(false):JValue.CreateNull()});
   }
   for(int i=0;i<2;i++){var connection=UnityEngine.Object.Instantiate(source.transform.Find("Connection_0_1").gameObject);connection.name="Connection_"+i;connection.transform.SetParent(root.transform,false);connection.transform.localPosition=new Vector3(20.5f-i*13,3.2f,0);}
  }finally{PrefabUtility.UnloadPrefabContents(source);}
  var rail=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Rail+"OrdinaryFiniteRail500mV01.prefab"),scene);
  Need(rail.transform.Find("Rail/RailStart").position.x==-25&&rail.transform.Find("Rail/RailEnd").position.x==475,"original_rail");
  var measured=Measure(root,definitions);PrefabUtility.SaveAsPrefabAsset(root,Target+"OrdinaryTrialConsistV01.prefab");
  var document=new JObject{["version"]="ordinary.trial-consist-authoring.v1.20261027",["approval"]="ORDINARY-ENGINE-CARRIAGE-MAPPING-v1.20261027",["classification"]="NEW_APPROVED_TEST_ENGINE_NOT_HISTORICAL_MEASUREMENT",["status"]="MEASURED_SELECTION_ASSET_MAPPING_PENDING_SERVER_BINDING",["sourceStartHash"]=original.StartSnapshotHash,["sourceFixtureSha256"]=Hash(Fixture),["selectedLoadout"]=selection.DeepClone(),["mappings"]=definitions,["railDefinitionSha256"]=Hash(Rail+"definition.json"),["measuredComposition"]=measured};
  File.WriteAllText(Target+"definition.json",document.ToString());
  var camera=new GameObject("AuthoringCamera").AddComponent<Camera>();camera.transform.position=new Vector3(14,28,-34);camera.transform.LookAt(new Vector3(14,1.6f,0));camera.orthographic=true;camera.orthographicSize=14;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.08f,.09f);
  new GameObject("AuthoringLight").AddComponent<Light>().type=LightType.Directional;
  EditorSceneManager.SaveScene(scene,Target+"OrdinaryTrialConsistV01.unity");AssetDatabase.Refresh();
  var saved=PrefabUtility.LoadPrefabContents(Target+"OrdinaryTrialConsistV01.prefab");try{Need(JToken.DeepEquals(Measure(saved,definitions),measured),"prefab_reload");}finally{PrefabUtility.UnloadPrefabContents(saved);}
  var reopened=EditorSceneManager.OpenScene(Target+"OrdinaryTrialConsistV01.unity",OpenSceneMode.Single);Need(JToken.DeepEquals(Measure(reopened.GetRootGameObjects().Single(g=>g.name=="OrdinaryTrialConsistV01"),definitions),measured),"scene_reload");
  camera=reopened.GetRootGameObjects().Select(g=>g.GetComponent<Camera>()).Single(c=>c!=null);var rt=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);var prior=RenderTexture.active;
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Qa+"reference-only-preview.png",texture.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
  foreach(var p in preserved)Need(Hash(p.Key)==p.Value,"source_changed");
  File.WriteAllText(Qa+"verification.json",new JObject{["result"]="PASSED",["sourceUnchanged"]=true,["savedPrefabSceneEqual"]=true,["measuredComposition"]=measured,["assetHashes"]=JObject.FromObject(Directory.GetFiles(Target).Where(p=>!p.EndsWith(".meta")).ToDictionary(p=>p,Hash)),["serverPhysicalInstanceBinding"]=false,["playerTested"]=false,["fiveActorPlacementVerified"]=false}.ToString());
 }
}
#endif
