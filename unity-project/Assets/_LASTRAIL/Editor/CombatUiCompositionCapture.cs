#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class CombatUiCompositionCapture {
 public static void Run(){string output=null;try{
  output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!Application.isBatchMode||!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new Exception("Fresh D QA output required");Directory.CreateDirectory(output);
  var prep=new Ce03RailPreparation(Ce03NormalUiCapture.Read("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-RailJourney-20260914/synthetic-rail-preparation.json"),"a",1);var records=new JArray();
  foreach(int height in new[]{1280,1600}){
   var driver=new CombatSceneDriver(()=>prep.CreateModel(1),()=>null,(s,r)=>r+1,()=>true);driver.Open();var cg=new GameObject("CompositionCamera",typeof(Camera));var cam=cg.GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;var target=new RenderTexture(720,height,24);cam.targetTexture=target;
   var go=new GameObject("CompositionCanvas",typeof(RectTransform),typeof(Canvas));var view=go.AddComponent<ApprovedCombatView>();view.SetCapturePresentationClock(()=>0);view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
   var prior=RenderTexture.active;try{var before=driver.StateJson;view.Refresh();var owners=go.GetComponentsInChildren<Graphic>().Where(g=>g is CombatTitleBarFrame||g is CombatMeter||g is CombatConceptPanel).ToArray();
    void Save(string name){Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=target;var p=new Texture2D(720,height,TextureFormat.RGB24,false);p.ReadPixels(new Rect(0,0,720,height),0,0);p.Apply();File.WriteAllBytes(output+"/"+name+"-"+height+".png",p.EncodeToPNG());UnityEngine.Object.DestroyImmediate(p);}
    Save("composed");var layers=owners.SelectMany(g=>g.transform.Cast<Transform>()).Where(t=>t.name.StartsWith("Layer_")).Select(t=>t.GetComponent<Graphic>()).Where(g=>g!=null).ToArray();
    if(layers.Length>0){foreach(string role in new[]{"Frame","Backplate","Track","Fill","Glow","Core","Shield","StateEffect"}){var chosen=layers.Where(g=>g.name=="Layer_"+role).ToArray();foreach(var g in chosen)g.enabled=false;Save("hide-"+role);foreach(var g in chosen)g.enabled=true;
      var matching=owners.OfType<CombatLayeredGraphic>().Where(o=>o.transform.Find("Layer_"+role)!=null).ToArray();foreach(var owner in matching)owner.SetLayerRenderer(role,(mesh,r,c)=>{mesh.Clear();mesh.AddVert(new Vector3(r.x,r.y),Color.magenta,Vector2.zero);mesh.AddVert(new Vector3(r.xMax,r.y),Color.magenta,Vector2.zero);mesh.AddVert(new Vector3(r.xMax,r.yMax),Color.magenta,Vector2.zero);mesh.AddVert(new Vector3(r.x,r.yMax),Color.magenta,Vector2.zero);mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);});Save("swap-"+role);foreach(var owner in matching)owner.SetLayerRenderer(role,null);
     }foreach(var g in layers)g.enabled=g.name=="Layer_Frame";Save("solo-Frame");foreach(var g in layers)g.enabled=true;
     foreach(float fraction in new[]{0f,.5f,1f}){foreach(var meter in owners.OfType<CombatMeter>())meter.SetValue(fraction);Save("diagnostic-value-"+(int)(fraction*100));}foreach(var meter in owners.OfType<CombatMeter>())meter.SetHealth(100,100,50);Save("diagnostic-shield");view.Refresh();Save("restored");}
    if(driver.StateJson!=before||driver.RecoveryRequired)throw new Exception("Display capture mutated model");File.WriteAllText(output+"/state-"+height+".json",before);records.Add(new JObject{["height"]=height,["ownerCount"]=owners.Length,["layerCount"]=layers.Length,["stateUnchanged"]=true,["recoveryRequired"]=false});
   }finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cg);target.Release();UnityEngine.Object.DestroyImmediate(target);}
  }
  File.WriteAllText(output+"/complete.json",new JObject{["captures"]=records,["scope"]="Fixed local state; no live action or HTTP verification"}.ToString());EditorApplication.Exit(0);
 }catch(Exception e){if(output!=null&&Directory.Exists(output))File.WriteAllText(output+"/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
