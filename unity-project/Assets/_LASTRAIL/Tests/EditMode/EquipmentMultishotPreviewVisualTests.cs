#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public class EquipmentMultishotPreviewVisualTests {
  [UnityTest]public IEnumerator ActualPreviewAndOriginalReplayUseExistingPopupWithoutCombatWrites(){
   string output=Environment.GetEnvironmentVariable("LASTRAIL_EN17_PREVIEW_QA_OUTPUT");Assert.That(output,Is.Not.Null.And.StartsWith("D:/Project/LASTRAIL/QA/MiraEquipped-20261016/"));Assert.That(Directory.Exists(output),Is.False);Directory.CreateDirectory(output);Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
   var e=(JObject)JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/MiraEquipped-20261016/PlacementPreviewSource/server-packets.json"))["packets"]["grown"];
   var cameraObject=new GameObject("EN17PreviewCamera");var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.01f,.015f,.02f);camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;
   var render=new RenderTexture(941,1672,24);camera.targetTexture=render;
   var canvasObject=new GameObject("EN17PreviewCanvas",typeof(RectTransform),typeof(Canvas));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.pixelPerfect=true;
   var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");var pages=LobbyDestinationPages.Create(canvasObject.transform,font,()=>{});pages.Open(LobbyPage.Operators,true);
   void Draw(string name){Canvas.ForceUpdateCanvases();typeof(LobbyDestinationPages).GetMethod("Fit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(pages,null);foreach(var text in pages.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate();Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=render;var texture=new Texture2D(941,1672,TextureFormat.RGBA32,false);try{texture.ReadPixels(new Rect(0,0,941,1672),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}}
   try{
    foreach(string key in new[]{"remove","replace","upgraded","unsupported","alreadyCommitted"}){var p=(JObject)e["previews"][key];var command=(JObject)e["previewCommands"][key];var beforeBytes=p.ToString();var model=EquipmentMultishotPlacementPreviewModel.Read(p,p,BDirectAttackInput.Digest(p),"a","LOCAL_B12_RUN",command);
     using(var controller=new EquipmentMultishotController("a","LOCAL_B12_RUN")){controller.Bind(pages);controller.RefreshPreviewAsync(_=>System.Threading.Tasks.Task.FromResult(model)).GetAwaiter().GetResult();yield return null;yield return null;Draw(key+".png");var texts=pages.GetComponentsInChildren<TMP_Text>();Assert.That(texts.Single(t=>t.name=="PreviewStatus").text,Is.EqualTo(key=="alreadyCommitted"?"이미 처리된 장착 요청":"확정 전 미리보기"));if(key=="alreadyCommitted")Assert.That(texts.Any(t=>t.name=="previewAfterBasis"),Is.False);else{Assert.That(texts.Single(t=>t.name=="previewBeforeBasis").text,Is.EqualTo("장착 전"));Assert.That(texts.Single(t=>t.name=="previewAfterBasis").text,Is.EqualTo("장착 후 예상"));}Assert.That(p.ToString(),Is.EqualTo(beforeBytes));Assert.That(model.CanApplyToCombat,Is.False);controller.Unbind();}
    }
    File.WriteAllText(Path.Combine(output,"acceptance.json"),new JObject{["classification"]="ACTUAL_ISOLATED_SERVER_OWNED_PREVIEW_TO_EXISTING_POPUP_NOT_OPERATING_HTTP_OR_HP_ADJUSTMENT",["graphics"]=SystemInfo.graphicsDeviceName,["width"]=941,["height"]=1672,["cases"]=new JArray("remove","replace","upgraded","unsupported","alreadyCommitted"),["staticStatsOnly"]=true,["nativeWrites"]=false}.ToString());
   }finally{UnityEngine.Object.DestroyImmediate(canvasObject);camera.targetTexture=null;render.Release();UnityEngine.Object.DestroyImmediate(render);UnityEngine.Object.DestroyImmediate(cameraObject);}
  }
 }
}
#endif
