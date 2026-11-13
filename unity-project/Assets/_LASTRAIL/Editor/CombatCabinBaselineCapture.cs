#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class CombatCabinBaselineCapture {
 static string output;static JArray records;
 public static void Run(){try{
  output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!Application.isBatchMode||!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new Exception("Fresh D QA output required");Directory.CreateDirectory(output);records=new JArray();
  const string source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Design/Zone04-Kyle-Baseline-20260914/kyle-fullbody-v05.png";var bytes=File.ReadAllBytes(source);using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!="b7a0c4cf91226f131bf3365b77087c748e95786ad2b136ab747dd117efc27457")throw new Exception("Staged baseline changed");
  var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!texture.LoadImage(bytes))throw new Exception("Invalid baseline PNG");texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
  var prep=new Ce03RailPreparation(Ce03NormalUiCapture.Read("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-RailJourney-20260914/synthetic-rail-preparation.json"),"a",1);
  foreach(int height in new[]{1280,1600}){
   var driver=new CombatSceneDriver(()=>prep.CreateModel(1),()=>null,(s,r)=>r+1,()=>true);driver.Open();var cg=new GameObject("CabinBaselineCamera",typeof(Camera),typeof(AudioListener));var camera=cg.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var target=new RenderTexture(720,height,24);camera.targetTexture=target;
   var go=new GameObject("CabinBaselineCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   try{Still("current-"+height,driver,view,camera,target);var rig=go.GetComponentsInChildren<CombatMultipartRig>().Single(r=>r.name=="op_kylePartsDiagnostic");var unit=(RectTransform)rig.transform.parent;var tint=rig.Tint;rig.gameObject.SetActive(false);
    var node=new GameObject("KyleStaticStyleDiagnostic",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));node.transform.SetParent(unit,false);var rect=node.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
    // Preserved visible-content crop. Source sole midpoint x830/y1000 maps to existing local(75,-160).
    const float scale=155f/991;rect.sizeDelta=new Vector2(593*scale,155);rect.anchoredPosition=new Vector2(75-(830-463)*scale,-160+(1000-10)*scale);
    var art=node.GetComponent<RawImage>();art.texture=texture;art.uvRect=new Rect(463f/1536,23f/1024,593f/1536,991f/1024);art.raycastTarget=false;art.color=Color.white;node.transform.SetSiblingIndex(rig.transform.GetSiblingIndex());Still("candidate-neutral-"+height,driver,view,camera,target);
    art.color=tint;Still("candidate-current-light-"+height,driver,view,camera,target);
   }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cg);target.Release();UnityEngine.Object.DestroyImmediate(target);}
  }
  UnityEngine.Object.DestroyImmediate(texture);File.WriteAllText(output+"/complete.json",new JObject{["scope"]="Static style diagnostic only. Existing six-part rig remains production source; candidate PNG loaded from D staging without import. Same fresh local fixture, unchanged portrait/other actors/background. Not an animation replacement or new HTTP run.",["captures"]=records}.ToString());EditorApplication.Exit(0);
 }catch(Exception e){if(output!=null&&Directory.Exists(output))File.WriteAllText(output+"/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
 static void Still(string name,CombatSceneDriver driver,ApprovedCombatView view,Camera camera,RenderTexture target){string before=driver.StateJson;view.Refresh();Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;try{
  foreach(bool crop in new[]{false,true}){int h=crop?367:target.height;var pixels=new Texture2D(720,h,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,crop?target.height-(target.height-1280)/2-756:0,720,h),0,0);pixels.Apply();File.WriteAllBytes(output+"/"+name+(crop?"-cabin":"")+".png",pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);}
 }finally{RenderTexture.active=previous;}if(before!=driver.StateJson||driver.RecoveryRequired)throw new Exception("Diagnostic changed model");File.WriteAllText(output+"/"+name+".state.json",before);records.Add(new JObject{["name"]=name,["stateUnchanged"]=true,["recoveryRequired"]=false,["width"]=720,["height"]=target.height});}
}
#endif
