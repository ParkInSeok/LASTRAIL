#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class CombatRailAspectCapture {
 public static void Run(){
  string output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
  if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new Exception("Fresh D output required");
  Directory.CreateDirectory(output);
  string source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/SecondFinal-20260913/ActualCapture03/";
  var identity=JObject.Parse(File.ReadAllText(source+"session/session.json"));
  var prep=new Ce03RailPreparation(JObject.Parse(File.ReadAllText(source+"session/preparation.json")),(string)identity["owner"],1);
  Render(output,"aspect-20-9",720,1600,File.ReadAllText(source+"ultimate-ready.state.json"),prep);
  Render(output,"final-terminal",720,1280,File.ReadAllText(source+"executed-model.json"),prep);
  var low=JObject.Parse(File.ReadAllText(source+"ultimate-ready.state.json"));string train=(string)((JArray)prep.CreateModel(1).ReadHudState()["actors"]).Single(a=>(bool)a["train"])["id"];((JArray)low["actors"]).Single(a=>(string)a["id"]==train)["vitals"]["hp"]=1000;
  Render(output,"diagnostic-low-hp",720,1280,Ce03CommandContract.Canonical(low),prep);
  EditorApplication.Exit(0);
 }
 static void Render(string output,string name,int width,int height,string raw,Ce03RailPreparation prep){
  var session=new Ce03RailSession(prep,"static-"+name,1);string checkpoint=null;var store=new CombatCheckpointStore(output+"/"+name+".checkpoint.json",()=>checkpoint,s=>checkpoint=s);store.SaveRailSession(session,BasicCombatSimulation.RestoreLocalState(raw,session.CreateModel),0);var driver=session.CreateDriver(store,()=>true);driver.Open();
  var cg=new GameObject("AspectCamera",typeof(Camera));var camera=cg.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
  var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;
  var go=new GameObject("AspectCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
  Canvas.ForceUpdateCanvases();view.Refresh();Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(output+"/"+name+".png",pixels.EncodeToPNG());
  if(raw!=driver.StateJson||driver.RecoveryRequired)throw new Exception("Static state changed or recovery required");
  File.WriteAllText(output+"/"+name+".scope.json",new JObject{["scope"]=name.StartsWith("diagnostic")?"Explicit local low-HP setup (train1000), not naturally observed HTTP event":"Static actual HTTP03 checkpoint; no advancement, not physical device QA",["width"]=width,["height"]=height,["modelSha256"]=Ce03CommandContract.Sha(raw),["stateUnchanged"]=true,["recoveryRequired"]=false}.ToString());
  RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cg);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(pixels);
 }
}
#endif
