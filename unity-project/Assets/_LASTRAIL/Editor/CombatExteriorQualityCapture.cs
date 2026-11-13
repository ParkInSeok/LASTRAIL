#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class CombatExteriorQualityCapture {
 static string output;static Ce03RailPreparation prep;static JArray records;
 public static void Run(){try{
  output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new Exception("Fresh D output required");Directory.CreateDirectory(output);records=new JArray();
  prep=new Ce03RailPreparation(Ce03NormalUiCapture.Read("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-RailJourney-20260914/synthetic-rail-preparation.json"),"a",1);
  Capture(prep.CreateModel(1).CaptureLocalState(),"normal",false);
  const string prior="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/SecondFinal-20260913/ActualCapture03/";
  Capture(File.ReadAllText(prior+"artillery-visible.state.json"),"active-artillery",true);
  Capture(File.ReadAllText(prior+"artillery-passed.state.json"),"passed",false);
  Capture(File.ReadAllText(prior+"artillery-destroyed.state.json"),"destroyed",false);
  Capture(File.ReadAllText(prior+"selection-pending.state.json"),"selection",false);
  prep=new Ce03RailPreparation(Ce03NormalUiCapture.Read("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-RailJourney-20260914/synthetic-rail-preparation-four-cars.json"),"a",1);Capture(prep.CreateModel(1).CaptureLocalState(),"synthetic-four-cars",false);
  File.WriteAllText(output+"/complete.json",new JObject{["scope"]="Fresh actual Unity render of approved zone02. Explicit restored historical actual model states and bounded local60frame continuation, not a new HTTP run or natural encounter discovery. Inputs and exact reference comparison are recorded.",["captures"]=records}.ToString());EditorApplication.Exit(0);
 }catch(Exception e){if(output!=null)File.WriteAllText(output+"/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
 static void Capture(string raw,string name,bool clip){
  string cp=null;var session=new Ce03RailSession(prep,"exterior-"+name,1);var store=new CombatCheckpointStore(output+"/"+name+".cp",()=>cp,s=>cp=s);store.SaveRailSession(session,BasicCombatSimulation.RestoreLocalState(raw,session.CreateModel),0);var d=session.CreateDriver(store,()=>true);d.Open();
  var cameraGo=new GameObject("ExteriorCamera",typeof(Camera),typeof(AudioListener));var camera=cameraGo.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var target=new RenderTexture(720,1280,24);camera.targetTexture=target;
  var go=new GameObject("ExteriorCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var view=go.AddComponent<ApprovedCombatView>();view.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
  try{
   Still(name,d,view,camera,target);
   if(clip){
    var bodies=go.transform.Find("ApprovedBattle/ExteriorViewport/SelectedRailConsist").GetComponentsInChildren<RawImage>();foreach(var body in bodies)if(body.name=="Body")body.color=new Color(.83f,.86f,.87f);Still(name+"-old-body-tint-diagnostic",d,view,camera,target);foreach(var body in bodies)if(body.name=="Body")body.color=Color.white;
    Directory.CreateDirectory(output+"/frames");var reference=BasicCombatSimulation.RestoreLocalState(d.StateJson,session.CreateModel);using(var trace=new StreamWriter(output+"/frames.jsonl")){for(int i=0;i<60;i++){d.Advance(500000,8);reference.Advance(500000,8);if(d.StateJson!=reference.CaptureLocalState())throw new Exception("Reference mismatch");Still("frames/"+i.ToString("D4"),d,view,camera,target,false);trace.WriteLine(new JObject{["frame"]=i,["inputTicks100ns"]=500000,["tick"]=d.Tick,["referenceExact"]=true,["modelSha256"]=Ce03CommandContract.Sha(d.StateJson)}.ToString(Newtonsoft.Json.Formatting.None));}}
    camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);target=new RenderTexture(720,1600,24);camera.targetTexture=target;Still(name+"-20-9",d,view,camera,target);
   }
  }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cameraGo);target.Release();UnityEngine.Object.DestroyImmediate(target);}
 }
 static void Still(string name,CombatSceneDriver d,ApprovedCombatView view,Camera camera,RenderTexture target,bool record=true){string before=d.StateJson;Canvas.ForceUpdateCanvases();view.Refresh();Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(output+"/"+name+".png",pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);if(record){var crop=new Texture2D(720,273,TextureFormat.RGB24,false);crop.ReadPixels(new Rect(0,(target.height-1280)/2+1280-339,720,273),0,0);crop.Apply();File.WriteAllBytes(output+"/"+name+"-exterior.png",crop.EncodeToPNG());UnityEngine.Object.DestroyImmediate(crop);}RenderTexture.active=previous;if(before!=d.StateJson||d.RecoveryRequired)throw new Exception("Render mutated state");if(record){File.WriteAllText(output+"/"+name+".state.json",before);File.WriteAllText(output+"/"+name+".hud.json",d.HudState.ToString());records.Add(new JObject{["name"]=name,["tick"]=d.Tick,["width"]=target.width,["height"]=target.height,["stateUnchanged"]=true});}}
}
#endif
