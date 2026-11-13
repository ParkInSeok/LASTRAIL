#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class CombatFreshQualityCapture {
 public static void Run(){
  string output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
  if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new Exception("Fresh D output required");
  Directory.CreateDirectory(output);
  string source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/SecondFinal-20260913/CurrentProfileSmoke01/";
  var identity=JObject.Parse(File.ReadAllText(source+"session/session.json"));
  var prep=new Ce03RailPreparation(JObject.Parse(File.ReadAllText(source+"session/preparation.json")),(string)identity["owner"],1);
  string raw=File.ReadAllText(source+"final-model.json"); Render(output,"native-720x1280",720,1280,raw,prep); Render(output,"native-720x1600",720,1600,raw,prep);
  EditorApplication.Exit(0);
 }
 static void Render(string output,string name,int width,int height,string raw,Ce03RailPreparation prep){
  var session=new Ce03RailSession(prep,"static-"+name,1);string checkpoint=null;var store=new CombatCheckpointStore(output+"/"+name+".checkpoint.json",()=>checkpoint,s=>checkpoint=s);store.SaveRailSession(session,BasicCombatSimulation.RestoreLocalState(raw,session.CreateModel),0);var driver=session.CreateDriver(store,()=>true);driver.Open();
  var cg=new GameObject("AspectCamera",typeof(Camera));var camera=cg.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
  var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;
  var go=new GameObject("AspectCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var view=go.AddComponent<ApprovedCombatView>();view.SetCapturePresentationClock(()=>10d);view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
  Canvas.ForceUpdateCanvases();view.Refresh();Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(output+"/"+name+".png",pixels.EncodeToPNG()); var header=new Texture2D(720,66,TextureFormat.RGB24,false);header.ReadPixels(new Rect(0,height-(height-1280)/2-66,720,66),0,0);header.Apply();File.WriteAllBytes(output+"/"+name+"-header.png",header.EncodeToPNG());UnityEngine.Object.DestroyImmediate(header);
  if(raw!=driver.StateJson||driver.RecoveryRequired)throw new Exception("Static state changed or recovery required");
  File.WriteAllText(output+"/"+name+".scope.json",new JObject{["scope"]=name.StartsWith("diagnostic")?"Explicit local low-HP setup (train1000), not naturally observed HTTP event":"Fresh latest-source Unity rendering of retained CurrentProfileSmoke01 current-profile checkpoint; no advancement or HTTP request; RenderTexture dimensions, not physical device QA",["width"]=width,["height"]=height,["modelSha256"]=Ce03CommandContract.Sha(raw),["stateUnchanged"]=true,["recoveryRequired"]=false}.ToString());
  if(height==1280)CaptureHeaderStates(output,driver,view,camera,rt,go.transform.Find("ApprovedBattle"));
  RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cg);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(pixels);
 }
 static void CaptureHeaderStates(string output,CombatSceneDriver driver,ApprovedCombatView view,Camera camera,RenderTexture rt,Transform root){
  var records=new JArray();
  Action<string> still=name=>{string before=driver.StateJson;view.Refresh();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var pixels=new Texture2D(720,66,TextureFormat.RGB24,false);try{pixels.ReadPixels(new Rect(0,1214,720,66),0,0);pixels.Apply();File.WriteAllBytes(output+"/state-"+name+"-header.png",pixels.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(pixels);}if(before!=driver.StateJson)throw new Exception("Presentation changed model");records.Add(new JObject{["name"]=name,["modelSha256"]=Ce03CommandContract.Sha(before),["auto"]=driver.HudState["ultimateAuto"],["speedHalfUnits"]=driver.HudState["speedHalfUnits"],["paused"]=driver.HudState["paused"],["tick"]=driver.Tick,["stateUnchangedByRender"]=true});};
  root.Find("UltimateAuto").GetComponent<Button>().onClick.Invoke();still("auto-on-1-5x");
  root.Find("Speed").GetComponent<Button>().onClick.Invoke();still("auto-on-2x");
  root.Find("Speed").GetComponent<Button>().onClick.Invoke();still("auto-on-1x");
  root.Find("Pause").GetComponent<Button>().onClick.Invoke();string paused=driver.StateJson;driver.Advance(10000000,8);if(paused!=driver.StateJson)throw new Exception("Pause advanced");still("paused-play-glyph");root.Find("Pause").GetComponent<Button>().onClick.Invoke();still("resumed-pause-glyph");
  var speedFrame=root.Find("Speed/ControlHousing/MachinedFrame").GetComponent<CombatTitleBarFrame>();speedFrame.SetPressed(true);still("speed-pressed");speedFrame.SetPressed(false);
  var frames=root.GetComponentsInChildren<CombatTitleBarFrame>();foreach(var frame in frames)frame.SetLayerVisible("Frame",false);still("frames-hidden");foreach(var frame in frames)frame.SetLayerVisible("Frame",true);
  foreach(var frame in frames)frame.SetLayerVisible("Backplate",false);still("backplates-hidden");foreach(var frame in frames)frame.SetLayerVisible("Backplate",true);
  root.Find("TitleEmblem").gameObject.SetActive(false);still("logo-hidden");root.Find("TitleEmblem").gameObject.SetActive(true);
  foreach(var glyph in root.GetComponentsInChildren<CombatHeaderGlyph>())glyph.enabled=false;still("glyphs-hidden");foreach(var glyph in root.GetComponentsInChildren<CombatHeaderGlyph>())glyph.enabled=true;
  speedFrame.SetLayerRenderer("Backplate",(mesh,r,c)=>{mesh.AddVert(new Vector2(r.x,r.y),new Color(.1f,.25f,.3f),Vector2.zero);mesh.AddVert(new Vector2(r.xMax,r.y),new Color(.1f,.25f,.3f),Vector2.zero);mesh.AddVert(new Vector2(r.xMax,r.yMax),new Color(.1f,.25f,.3f),Vector2.zero);mesh.AddVert(new Vector2(r.x,r.yMax),new Color(.1f,.25f,.3f),Vector2.zero);mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);});still("speed-backplate-replaced");speedFrame.SetLayerRenderer("Backplate",null);still("restored-composition");
  File.WriteAllText(output+"/header-states.json",new JObject{["scope"]="Retained model, local command callbacks and display-only hide/replace probes. No time advance except paused invariant; not physical device input.",["captures"]=records}.ToString());
 }
}
#endif
