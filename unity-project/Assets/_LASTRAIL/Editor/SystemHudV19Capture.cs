#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Resources;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SystemHudV19Capture
{
 public static void Run()
 {
  const string output="D:/Project/LASTRAIL/QA/DirectionImpact-20260921/combat-carriage-durability-v01/unity-v19";
  if(Directory.Exists(output))throw new InvalidOperationException("Use a fresh capture directory.");
  Directory.CreateDirectory(output);
  BasicCombatSimulation Fresh(){var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));d["ownedSelection"]["accountId"]="a";return BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1);}
  var driver=new CombatSceneDriver(Fresh,()=>null,(_,r)=>r+1,()=>true);driver.Open();string before=driver.StateJson;
  var cameraNode=new GameObject("HUD capture camera",typeof(Camera));var camera=cameraNode.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<31;
  var host=new GameObject("Actual HUD local fixture",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
  var prior=RenderTexture.active;RenderTexture target=null;
  try{
   var view=host.AddComponent<ApprovedCombatView>();var art=PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");view.Bind(driver,art);view.UseLandscapeLayout();
   var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   var results=new JArray();
   foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(2100,900),new Vector2Int(1200,900)}){
    target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
    view.BindOrRefresh(driver,art);Canvas.ForceUpdateCanvases();view.Refresh();
    foreach(var t in host.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
    foreach(var t in host.GetComponentsInChildren<TMP_Text>())t.ForceMeshUpdate();
    Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
    var pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
    try{pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();File.WriteAllBytes(output+"/hud-"+size.x+"x"+size.y+".png",pixels.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(pixels);}
    var meters=host.GetComponentsInChildren<CombatMeter>(true);var train=meters.Single(m=>m.name=="TrainHp");var slots=meters.Where(m=>m.name.StartsWith("SystemDurability",StringComparison.Ordinal)).OrderBy(m=>m.name).ToArray();
    Vector3[] Corners(RectTransform r){var c=new Vector3[4];r.GetWorldCorners(c);return c;}
    float left=Mathf.Abs(Corners(train.rectTransform)[0].x-Corners(slots[0].rectTransform)[0].x),right=Mathf.Abs(Corners(train.rectTransform)[2].x-Corners(slots[2].rectTransform)[2].x);
    if(left>.001f||right>.001f||slots.Length!=3||slots.Any(m=>m.Value!=0)||before!=driver.StateJson)throw new InvalidOperationException("HUD alignment/state verification failed.");
    results.Add(new JObject{["width"]=size.x,["height"]=size.y,["leftErrorWorld"]=left,["rightErrorWorld"]=right,["missingSourcePreserved"]=true});
    RenderTexture.active=prior;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;
   }
   File.WriteAllText(output+"/verification.json",new JObject{["scope"]="ACTUAL_UNITY_EDITOR_VIEW_LOCAL_FIXTURE_NOT_LIVE_ACCOUNT_OR_PLAYER_BUILD",["modelUnchanged"]=before==driver.StateJson,["captures"]=results}.ToString());
  }finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(cameraNode);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}}
 }
}
#endif
