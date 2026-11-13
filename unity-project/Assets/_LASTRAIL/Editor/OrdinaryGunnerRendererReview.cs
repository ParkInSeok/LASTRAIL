#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Read-only product renderer inspection with an explicitly local actor fixture.
public static class OrdinaryGunnerRendererReview {
 const string Output="D:/Project/LASTRAIL/QA/CommanderS1-20260922/GunnerRenderer836/visible/";
 const string Prefab="Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/OrdinaryEnemyTrainV01.prefab";
 const string RejectedArt="Assets/_LASTRAIL/UI/CombatResources/Enemies/enemy_spike_cart_gunner.png";
 static void Need(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
 static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static JArray Point(Vector3 p)=>new JArray(p.x,p.y,p.z);
 static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 public static void Run(){
  Need(Application.isBatchMode&&!Directory.Exists(Output),"Fresh batch review required");
  Need(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"Preserve dirty scene");
  Directory.CreateDirectory(Output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var assets=UnityEngine.Resources.Load<CombatPresentationAssets>("CombatPresentation/approved");Need(assets!=null&&assets.ActorAtlas("enemy_spike_cart_gunner")==null,"Inspect changed actor resource mapping");
  var sourcePaths=new[]{Prefab,RejectedArt,AssetDatabase.GetAssetPath(assets),AssetDatabase.GetAssetPath(assets.Buggy)};var hashes=sourcePaths.ToDictionary(p=>p,Hash);
  var original=PrefabUtility.LoadPrefabContents(Prefab);var feet=new JArray();
  try{for(int i=0;i<2;i++){var foot=original.transform.Find("GunnerFoot_"+i);Need(foot!=null,"Missing source foot");Need(foot.GetComponentsInChildren<Renderer>(true).Length==0,"Source gained a renderer: inspect it instead");feet.Add(new JObject{["path"]=PathOf(foot),["localPositionUnits"]=Point(foot.localPosition),["descendantRenderers"]=0});}}
  finally{PrefabUtility.UnloadPrefabContents(original);}
  var prepared=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/ce03-frontline-policy-UtU7yJ/prepared.json"));
  BasicCombatSimulation Fresh(){var model=BasicCombatSimulation.FromPreparedProfileForLocalTest(prepared,(JObject)prepared["ownedSelection"],(string)prepared["ownedSelection"]["accountId"],1);var visuals=(Dictionary<string,string>)typeof(BasicCombatSimulation).GetField("actorVisualIds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(model);Need(visuals.ContainsKey("wave.1.exterior.0"),"Local exterior fixture changed");visuals["wave.1.exterior.0"]="enemy_spike_cart_gunner";return model;}
  int writes=0;var driver=new CombatSceneDriver(Fresh,()=>null,(state,revision)=>{writes++;return revision+1;},()=>true);driver.Open();driver.Advance(TimeSpan.FromSeconds(3).Ticks,60);
  var host=new GameObject("ActualRendererLocalReview",typeof(RectTransform),typeof(Canvas));var camera=new GameObject("ReviewCamera").AddComponent<Camera>();var rt=new RenderTexture(1600,900,24);var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);var prior=RenderTexture.active;
  try{
   var view=host.AddComponent<ApprovedCombatView>();view.Bind(driver,assets);view.UseLandscapeLayout();
   var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.targetTexture=rt;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;Canvas.ForceUpdateCanvases();view.Refresh();Canvas.ForceUpdateCanvases();
   string before=driver.StateJson;int baseline=writes;
   var art=host.GetComponentsInChildren<RawImage>(true).Single(r=>r.name=="Unit"&&r.transform.parent.name=="wave.1.exterior.0");Need(art.texture==assets.Buggy,"Actual vehicle renderer differs from selected resource");
   Need(art.gameObject.activeInHierarchy&&art.GetComponentInParent<CanvasGroup>().alpha>.9f,"Actor not visible yet");
   var rect=art.rectTransform;var samples=new JArray();
   foreach(var uv in new[]{Vector2.zero,Vector2.one,new Vector2(.5f,.5f)}){
    var local=new Vector3(rect.rect.xMin+uv.x*rect.rect.width,rect.rect.yMin+uv.y*rect.rect.height,0);var world=rect.TransformPoint(local);var restored=rect.InverseTransformPoint(world);Need((restored-local).magnitude<.001f,"UI transform roundtrip failed");
    samples.Add(new JObject{["normalizedRect"]=new JArray(uv.x,uv.y),["localUiUnits"]=Point(local),["canvasWorldUnits"]=Point(world),["roundTripErrorUiUnits"]=(restored-local).magnitude});
   }
   // Bounds markers identify the actual displayed composite, never a gameplay hit volume.
   foreach(var side in new[]{0,1}){var mark=new GameObject("CompositeBounds_NotHitVolume",typeof(RectTransform),typeof(Image));var r=mark.GetComponent<RectTransform>();r.SetParent(rect,false);r.anchorMin=new Vector2(side,0);r.anchorMax=new Vector2(side,1);r.sizeDelta=new Vector2(2,0);r.anchoredPosition=Vector2.zero;mark.GetComponent<Image>().color=Color.cyan;mark.GetComponent<Image>().raycastTarget=false;}
   Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();File.WriteAllBytes(Output+"actual-ui-local-fixture.png",pixels.EncodeToPNG());
   Need(before==driver.StateJson&&baseline==writes,"Review mutated combat state");foreach(var pair in hashes)Need(Hash(pair.Key)==pair.Value,"Source asset changed");
   File.WriteAllText(Output+"renderer-review.json",new JObject{["passed"]=true,["scope"]="ACTUAL_PRODUCT_UI_BRANCH_LOCAL_CE03_FIXTURE_NOT_ORDINARY_PLAYER",["sourceHashes"]=JObject.FromObject(hashes),["physicalFootObjects"]=feet,["actorId"]="wave.1.exterior.0",["visualId"]="enemy_spike_cart_gunner",["actualTexturePath"]=AssetDatabase.GetAssetPath(art.texture),["rendererType"]=art.GetType().FullName,["parentPath"]=PathOf(rect.parent),["pivot"]=new JArray(rect.pivot.x,rect.pivot.y),["rectSizeUiUnits"]=new JArray(rect.rect.width,rect.rect.height),["lossyScale"]=Point(rect.lossyScale),["uvRect"]=new JArray(art.uvRect.x,art.uvRect.y,art.uvRect.width,art.uvRect.height),["samples"]=samples,["worldMmTransform"]=null,["physicalRendererBound"]=false,["rejected835PngUsedByThisBranch"]=false,["displayWrites"]=writes-baseline,["candidateApproved"]=false,["playerTested"]=false}.ToString());
  }finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
 }
}
#endif
