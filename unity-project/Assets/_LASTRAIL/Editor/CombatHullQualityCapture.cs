#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class CombatHullQualityCapture {
 static string output;
 public static void Run(){try{
  output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!Application.isBatchMode||!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new Exception("Fresh D QA output required");Directory.CreateDirectory(output);
  var prep=new Ce03RailPreparation(Ce03NormalUiCapture.Read("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-RailJourney-20260914/synthetic-rail-preparation.json"),"a",1);var records=new JArray();
  foreach(int hp in new[]{5000,3127,1500,700,0})foreach(int height in new[]{1280,1600}){
   string name="hp-"+hp+"-"+height;var state=JObject.Parse(prep.CreateModel(1).CaptureLocalState());((JArray)state["actors"]).Single(a=>(string)a["id"]=="train_a")["vitals"]["hp"]=hp==0?1:hp;
   if(hp==0){var model=BasicCombatSimulation.RestoreLocalState(state.ToString(),()=>prep.CreateModel(1));for(int i=0;i<1000&&model.Outcome==BasicCombatOutcome.Running;i++)model.Advance(1000000,8);if(model.Outcome!=BasicCombatOutcome.Failure)throw new Exception("Zero-health diagnostic did not terminate");state=JObject.Parse(model.CaptureLocalState());}
   var session=new Ce03RailSession(prep,"hull-"+name,1);string cp=null;var store=new CombatCheckpointStore(output+"/"+name+".cp",()=>cp,s=>cp=s);store.SaveRailSession(session,BasicCombatSimulation.RestoreLocalState(state.ToString(),session.CreateModel),0);var driver=session.CreateDriver(store,()=>true);driver.Open();
   var cg=new GameObject("HullCamera",typeof(Camera),typeof(AudioListener));var camera=cg.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var target=new RenderTexture(720,height,24);camera.targetTexture=target;
   var go=new GameObject("HullCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   var prior=RenderTexture.active;try{string before=driver.StateJson;view.Refresh();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
    var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(output+"/"+name+".png",pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);
    var crop=new Texture2D(720,53,TextureFormat.RGB24,false);crop.ReadPixels(new Rect(0,height-(height-1280)/2-392,720,53),0,0);crop.Apply();File.WriteAllBytes(output+"/"+name+"-hull.png",crop.EncodeToPNG());UnityEngine.Object.DestroyImmediate(crop);
    if(before!=driver.StateJson||driver.RecoveryRequired)throw new Exception("Diagnostic changed state/recovery");File.WriteAllText(output+"/"+name+".state.json",before);records.Add(new JObject{["name"]=name,["hp"]=driver.HudState["trainHp"],["maxHp"]=driver.HudState["trainMaxHp"],["height"]=height,["stateUnchanged"]=true,["recoveryRequired"]=false});
   }finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cg);target.Release();UnityEngine.Object.DestroyImmediate(target);}
  }
  File.WriteAllText(output+"/complete.json",new JObject{["scope"]="Actual Unity rendering of explicitly seeded hull-health diagnostics, not natural damage or a new HTTP run. Existing selected emblem reused without bitmap edits.",["captures"]=records}.ToString());EditorApplication.Exit(0);
 }catch(Exception e){if(output!=null&&Directory.Exists(output))File.WriteAllText(output+"/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
