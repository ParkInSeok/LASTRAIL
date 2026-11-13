#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LastRail.Battle;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
namespace LastRail.Editor {
 // Explicit LOCAL authoring only. Original measured prefab remains untouched.
 public static class BMeasuredAcquisitionPlayerBuilder {
  const string Physical="Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoringV01/BPhysicalAuthoringV01.prefab";
  static void WriteNew(string path,string text){using(var f=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var w=new StreamWriter(f,new UTF8Encoding(false)))w.Write(text);}
  static JObject VerifyPrefab(GameObject world,BMeasuredPhysicalGeometry geometry){
   var checkedSlots=new JArray();
   foreach(var row in geometry.Capture()["measurements"]["instances"]){
    int slot=(int)row["templateSlot"];var expected=row["measurement"];var packet=expected["packet"];
    var frame=world.transform.Find("CarTemplate_"+slot)??throw new InvalidOperationException("Prefab template missing");
    string[] ids=packet["anchors"].Select(p=>(string)p["id"]).ToArray();
    var probe=new GameObject("LOCAL borrowed prefab measurement").AddComponent<BPhysicalSceneSource>();
    try{
     probe.Bind((string)packet["sourceId"],(string)packet["unitContractId"],(double)packet["millimetersPerUnit"],frame,frame.Find("BodyCollider").GetComponent<BoxCollider>(),ids,ids.Select(id=>frame.Find(id)).ToArray());
     if(!JToken.DeepEquals(probe.CaptureExport(),expected))throw new InvalidOperationException("Prefab no longer matches pinned measurement: "+slot);
     foreach(string id in ids){
      var p=frame.Find(id).position;var actual=new JArray((long)Math.Round(p.x*1000d,MidpointRounding.AwayFromZero),(long)Math.Round(p.y*1000d,MidpointRounding.AwayFromZero),(long)Math.Round(p.z*1000d,MidpointRounding.AwayFromZero));
      if(!JToken.DeepEquals(actual,geometry.WorldAnchor(slot,id)))throw new InvalidOperationException("Prefab world placement differs: "+slot+"/"+id);
     }
     checkedSlots.Add(slot);
    }finally{UnityEngine.Object.DestroyImmediate(probe.gameObject);}
   }
   return new JObject{["authoringHash"]=geometry.Hash,["prefab"]=Physical,["verifiedTemplateSlots"]=checkedSlots,["frameColliderAnchorsMatch"]=true,["worldPlacementMatches"]=true,["operatingAdmission"]=false};
  }
  public static void Build(){
   if(Path.GetFullPath(Application.dataPath).Replace('\\','/')!="D:/Project/LASTRAIL/unity-project/Assets")throw new InvalidOperationException("Canonical D Unity project required");
   string output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_MEASURED_PLAYER_OUTPUT")??throw new ArgumentException("Explicit output required"));
   if(!output.StartsWith(@"D:\Project\LASTRAIL\QA\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new ArgumentException("Fresh D QA output required");
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LastRail.Tests.EditMode.BMeasuredAcquisitionLocalSourceTests",false)).FirstOrDefault(t=>t!=null)
    ??throw new InvalidOperationException("Editor source factory missing");
   var factory=type.GetMethod("Envelope",BindingFlags.Static|BindingFlags.Public)??throw new MissingMethodException(type.FullName,"Envelope");
   var envelope=(JObject)factory.Invoke(null,null);string hash=BDirectAttackInput.Digest(envelope);
   var source=BMeasuredAcquisitionLocalSource.Read(envelope,hash);
   var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset")??throw new InvalidOperationException("Existing font missing");
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Physical)??throw new InvalidOperationException("Authored prefab missing");
   string token=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N");
   string folder="Assets/_LASTRAIL/QA/BMeasuredAcquisition/"+token;
   Directory.CreateDirectory(folder);Directory.CreateDirectory(output);
   string sourcePath=folder+"/OriginalLocalSource.json",scenePath=folder+"/MeasuredAcquisition.unity";
   WriteNew(sourcePath,envelope.ToString());AssetDatabase.ImportAsset(sourcePath,ImportAssetOptions.ForceSynchronousImport);
   var text=AssetDatabase.LoadAssetAtPath<TextAsset>(sourcePath);
   Scene prior=SceneManager.GetActiveScene(),created=default;
   try{
    var mode=NewSceneMode.Additive;
    if(string.IsNullOrEmpty(prior.path)){
     if(!Application.isBatchMode||prior.isDirty||prior.rootCount!=0||SceneManager.sceneCount!=1)throw new InvalidOperationException("Preserve unsaved scene");
     mode=NewSceneMode.Single;
    }
    created=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,mode);SceneManager.SetActiveScene(created);
    var world=(GameObject)PrefabUtility.InstantiatePrefab(prefab,created);
    WriteNew(output+"/prefab-measurement.json",VerifyPrefab(world,source.Geometry).ToString());
    int occupied=source.Binding.Composition.Attack.Formation.Slots.Count;
    foreach(Transform child in world.transform){
     if(child.name.StartsWith("CarTemplate_")){int slot=int.Parse(child.name.Substring("CarTemplate_".Length));child.gameObject.SetActive(slot<occupied);}
     else if(child.name.StartsWith("Connection_")){var ids=child.name.Split('_');child.gameObject.SetActive(int.Parse(ids[1])<occupied&&int.Parse(ids[2])<occupied);}
    }
    var camera=new GameObject("MeasuredDraftCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=7;
    camera.transform.position=new Vector3(5,12,-13);camera.transform.LookAt(new Vector3(0,2,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.065f);
    var light=new GameObject("MeasuredDraftLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(45,-30,0);
    var events=new GameObject("MeasuredLocalInput",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    var playerRoot=new GameObject("MeasuredAcquisitionLocalOwner");
    playerRoot.AddComponent<BMeasuredAcquisitionLocalPlayer>().Initialize(text,hash,font,output+"/evidence");
    if(Environment.GetEnvironmentVariable("LASTRAIL_MEASURED_AUDIO_TRIAL")=="1")BMeasuredAudioTrialAuthoring.Configure(playerRoot,folder,camera,output);
    if(!EditorSceneManager.SaveScene(created,scenePath))throw new IOException("Scene save failed");AssetDatabase.SaveAssets();
    string executable=output+"/MeasuredAcquisition.exe";
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName=executable,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=new[]{"LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER"}});
    WriteNew(output+"/build-authoring.json",new JObject{["version"]="unity.measured-acquisition-local-build.v1.20261020",["status"]=report.summary.result.ToString(),["classification"]="EXPLICIT_LOCAL_TEST_NOT_ADMITTED_RUN",["sourceHash"]=hash,["sourceAsset"]=sourcePath,["scene"]=scenePath,["physicalPrefab"]=Physical,["occupiedOriginalFormationSlots"]=occupied,["player"]=executable,["storage"]="LOCAL_QA_FILE_NOT_ANDROID_KEYSTORE",["fixtureIssuer"]=true,["independentArrivalVerified"]=false,["playerExecuted"]=false,["errors"]=report.summary.totalErrors,["warnings"]=report.summary.totalWarnings}.ToString());
    if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("LOCAL build failed: "+report.summary.result);
   }finally{
    if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);
    if(created.IsValid()&&created.isLoaded){if(SceneManager.sceneCount==1)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);else EditorSceneManager.CloseScene(created,true);}
   }
  }
 }
}
#endif
