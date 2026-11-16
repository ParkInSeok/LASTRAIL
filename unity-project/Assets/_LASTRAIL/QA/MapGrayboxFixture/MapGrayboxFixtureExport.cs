using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
namespace LastRail.Qa {
 public static class MapGrayboxFixtureExport {
  public const string ScenePath="Assets/_LASTRAIL/QA/MapGrayboxFixture/MapGrayboxFixture.unity";
  public static MapGrayboxFixtureController Build(out Camera camera,out EventSystem events){
   var cg=new GameObject("FixtureCamera",typeof(Camera));camera=cg.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=550;camera.aspect=1600f/1100;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
   var eg=new GameObject("FixtureEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events=eg.GetComponent<EventSystem>();eg.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
   var root=new GameObject("MapGrayboxFixture_TEST_ONLY",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(1600,1100);
   var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
   var controller=root.AddComponent<MapGrayboxFixtureController>();controller.Initialize(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"));return controller;
  }
  public static void Click(MapGrayboxFixtureController controller,Camera camera,EventSystem events,string name){
   Canvas.ForceUpdateCanvases();var button=controller.GetComponentsInChildren<Button>().Single(b=>b.name==name);var rect=(RectTransform)button.transform;
   var data=new PointerEventData(events){position=camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();controller.GetComponent<GraphicRaycaster>().Raycast(data,hits);
   if(hits.Count==0||hits[0].gameObject!=button.gameObject)throw new InvalidOperationException("Native raycast missed "+name);
   ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
  }
  public static void Run(){try{Capture();EditorApplication.Exit(0);}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
  static void Capture(){
   var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-kyleOutput");if(index<0||index+1>=args.Length)throw new ArgumentException("Output required");string output=Path.GetFullPath(args[index+1]);
   var report=MapGrayboxFixtureReplay.RunAll(output);var buildBefore=EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled).ToArray();var previous=SceneManager.GetActiveScene();
   bool freshBatchScene=Application.isBatchMode&&string.IsNullOrEmpty(previous.path)&&!previous.isDirty;
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,freshBatchScene?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
   RenderTexture target=null;Texture2D texture=null;var oldTarget=RenderTexture.active;
   try{
    var controller=Build(out var camera,out var events);target=new RenderTexture(1600,1100,24);camera.targetTexture=target;texture=new Texture2D(1600,1100,TextureFormat.RGB24,false);var captures=new JArray();
    void Shot(string id){controller.Refresh();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1600,1100),0,0);texture.Apply();string file=id+".png";File.WriteAllBytes(Path.Combine(output,file),texture.EncodeToPNG());captures.Add(new JObject{["file"]=file,["sha256"]=MapGrayboxFixtureGeometry.Hash(Path.Combine(output,file)),["state"]=controller.Model.Capture()});}
    void Click(string name)=>MapGrayboxFixtureExport.Click(controller,camera,events,name);
    Shot("01-geometry-default");controller.Bind();controller.Bind();if(controller.BindingCount!=16)throw new Exception("Binding count");Click("Step40");Click("Direct");if(controller.CommandCount!=2||controller.Model.Route!=0)throw new Exception("Duplicate/native route input");Shot("02-warning-direct");
    controller.Unbind();Click("Bypass");if(controller.Model.Route!=0||controller.CommandCount!=2)throw new Exception("Unbind failed");controller.Bind();Click("Step40");Click("Bypass");if(controller.Model.Route!=0)throw new Exception("Lock failed");Shot("03-locked");
    controller.Advance(280);Shot("04-tail-hit");controller.Advance(31);Shot("05-recovery-complete");controller.LoadScenario();Click("WaveComplete");controller.Advance(1100);Shot("06-bypass-safe-hold");
    controller.LoadScenario(401);controller.Advance(40);Click("Direct");controller.Advance(401);Shot("07-onset441-unresolved");
    controller.LoadScenario();camera.targetTexture=null;EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
    if(!buildBefore.SequenceEqual(EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled)))throw new Exception("Build settings changed");
    report["captures"]=captures;report["scene"]=ScenePath;report["nativeRaycastAndOwnedRebind"]="PASS";report["buildSettingsUnchanged"]=true;report["captureMode"]="Unity Editor camera render with native GraphicRaycaster/PointerClick";File.WriteAllText(Path.Combine(output,"verification.json"),report.ToString());
   }finally{RenderTexture.active=oldTarget;if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(previous.IsValid()){EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
 }
}
