#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode {
 public class FiveAgentVisualGraphicsTests {
  const string Catalog="Assets/_LASTRAIL/UI/CombatAnimations/FiveAgentReferenceV02/ApprovedVisualAssets.asset";
  static string Output=>Environment.GetEnvironmentVariable("LASTRAIL_FIVE_AGENT_GPU_OUTPUT");
  static FiveAgentVisualAssets Assets(){var a=AssetDatabase.LoadAssetAtPath<FiveAgentVisualAssets>(Catalog);Assert.That(a,Is.Not.Null,"Run approved scoped importer first");return a;}
  [UnityTearDown]public IEnumerator LeaveRuntimeAfterLifecycleTest(){if(Application.isPlaying)yield return new ExitPlayMode();}
  [UnityTest]public IEnumerator ViewDisableAndPoolReuseClearOnlyOwnedPresentationWithoutReplay(){
   // Plain EditMode MonoBehaviours do not receive runtime lifecycle callbacks.
   // Enter the actual player loop instead of invoking OnDisable by reflection.
   yield return new EnterPlayMode();
   RunRuntimePoolScenario();
  }
  // Keep closures out of the iterator state restored across the EnterPlayMode domain reload.
  static void RunRuntimePoolScenario(){
   var root=new GameObject("FiveAgentPoolFixture",typeof(RectTransform),typeof(FiveAgentVisualView));
   var view=root.GetComponent<FiveAgentVisualView>();
   Assert.That(view,Is.Not.Null,"View component after runtime creation");
   var assets=Assets();
   view.Configure(assets);
   Debug.Log($"FIVE_AGENT_RUNTIME_CREATED play={Application.isPlaying} view={view!=null}");
   int bindingEnded=0;view.BindingEnded+=()=>bindingEnded++;
   var feed=new FiveAgentVisualFeed();using var controller=new FiveAgentVisualController(assets.CreateModel());
   try{
    var map=new Dictionary<string,int>{{"op_eve",2}};
    for(int i=0;i<4;i++){
     controller.BindView(feed,view,map,"same-original-node",0);
     feed.PublishPose(new CombatUnitAnimationState("op_eve",CombatUnitRole.Operator,"attack","original"),3,1700);
     Assert.That(view.VisibleLayers,Is.GreaterThan(0));
     root.SetActive(false);
     Assert.That(bindingEnded,Is.EqualTo(i+1),"One runtime disable notification per return");
     Assert.That(view.VisibleLayers,Is.Zero,"Immediately after root disable");
     feed.PublishPose(new CombatUnitAnimationState("op_eve",CombatUnitRole.Operator,"attack","after-disable"),0,1850);
     root.SetActive(true);
     Assert.That(view.VisibleLayers,Is.Zero,"After reactivation without explicit rebind");
    }
    controller.Unbind();Assert.That(root.GetComponentsInChildren<Graphic>().Length,Is.Zero);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [UnityTest]public IEnumerator CaptureFourDirectionsAndCriticalPhasesOnExplicitReferenceCamera(){if(string.IsNullOrEmpty(Output))Assert.Ignore("Explicit D graphics output required");Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));string output=Path.GetFullPath(Output);Assert.That(output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||output.StartsWith("D:/Project/LASTRAIL/QA/",StringComparison.OrdinalIgnoreCase),Is.True);Directory.CreateDirectory(output);
   var assets=Assets();var model=assets.CreateModel();var cameraObject=new GameObject("ExplicitReferenceUICamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.04f,.06f);camera.nearClipPlane=.1f;camera.farClipPlane=100;
   var canvasObject=new GameObject("ReferenceCanvas_NotGameCameraOrPPU",typeof(RectTransform),typeof(Canvas));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;canvasObject.GetComponent<RectTransform>().sizeDelta=new Vector2(941,1672);
   var background=new GameObject("REFERENCE_ONLY_ApprovedCompositeBackdrop",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));background.transform.SetParent(canvasObject.transform,false);var br=background.GetComponent<RectTransform>();br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;var backdrop=new Texture2D(2,2);backdrop.LoadImage(File.ReadAllBytes(FiveAgentVisualReferenceTests.Source+"background.png"));background.GetComponent<RawImage>().texture=backdrop;
   var viewObject=new GameObject("Consumer",typeof(RectTransform),typeof(FiveAgentVisualView));viewObject.transform.SetParent(canvasObject.transform,false);var vr=viewObject.GetComponent<RectTransform>();vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=vr.offsetMax=Vector2.zero;var view=viewObject.GetComponent<FiveAgentVisualView>();view.Configure(assets);
   var captures=new JArray();RenderTexture target=null;Texture2D pixels=null;
   try{foreach(var resolution in new[]{new Vector2Int(941,1672),new Vector2Int(640,1136),new Vector2Int(1672,941)}){target=new RenderTexture(resolution.x,resolution.y,24);target.Create();camera.targetTexture=target;camera.aspect=(float)resolution.x/resolution.y;camera.orthographicSize=Mathf.Max(1672/2f,941/(2*camera.aspect));var phases=resolution.x==941?new long[]{0,1200,1700,1850,2130,2500,3100}:new long[]{1850};foreach(int direction in Enumerable.Range(0,4))foreach(long phase in phases){view.Present(model.SampleReference(direction,phase,true));Canvas.ForceUpdateCanvases();yield return null;yield return null;camera.Render();var old=RenderTexture.active;RenderTexture.active=target;pixels=new Texture2D(resolution.x,resolution.y,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,resolution.x,resolution.y),0,0);pixels.Apply();RenderTexture.active=old;string filename=$"d{direction+1}-{phase}-{resolution.x}x{resolution.y}.png";using(var file=new FileStream(Path.Combine(output,filename),FileMode.CreateNew,FileAccess.Write)){var bytes=pixels.EncodeToPNG();file.Write(bytes,0,bytes.Length);}captures.Add(new JObject{["file"]=filename,["direction"]=direction,["phaseMs"]=phase,["resolution"]=new JArray(resolution.x,resolution.y),["visibleLayers"]=view.VisibleLayers});Assert.That(view.VisibleLayers,Is.GreaterThan(10));UnityEngine.Object.DestroyImmediate(pixels);pixels=null;}camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
    var report=new JObject{["classification"]="UNITY_REFERENCE_CAMERA_AND_VISUAL_FIXTURE_NOT_GAME_SCENE_PPU_OR_REAL_FIVE_ROSTER",["graphics"]=SystemInfo.graphicsDeviceName,["sourceContractHash"]=assets.ContractHash,["background"]="REFERENCE_ONLY_LOADED_BY_TEST_NOT_RUNTIME_CATALOG",["captures"]=captures};using(var file=new FileStream(Path.Combine(output,"acceptance.json"),FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(file))writer.Write(report.ToString());
   }finally{camera.targetTexture=null;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}UnityEngine.Object.DestroyImmediate(canvasObject);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(backdrop);}
  }
 }
}
#endif