#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using UnityEngine.TestTools;
using System.Linq;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public class EquipmentMultishotVisualAcceptanceTests {
  [UnityTest]public IEnumerator ActualServerProjectionRendersThroughExistingPopupAndIndependentText(){
   string output=Environment.GetEnvironmentVariable("LASTRAIL_EN17_DISPLAY_QA_OUTPUT");Assert.That(output,Is.Not.Null.And.StartsWith("D:/Project/LASTRAIL/QA/MiraEquipped-20261016/"));Assert.That(Directory.Exists(output),Is.False,"Fresh visual output required");Directory.CreateDirectory(output);
   Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
   var entry=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/MiraEquipped-20261016/PresentationSource/server-packets.json"))["packets"]["grown"]["presentations"];
   var current=(JObject)entry["highest"]["current"];var departure=(JObject)entry["one"]["departure"];
   EquipmentMultishotModel Model(JObject p)=>EquipmentMultishotModel.Read(p,p,BDirectAttackInput.Digest(p),(string)p["accountId"],(string)p["runId"]);
   var cameraObject=new GameObject("EN17PopupCamera");var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.01f,.015f,.02f);camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;
   var render=new RenderTexture(1600,900,24);camera.targetTexture=render;
   var canvasObject=new GameObject("EN17ExistingPopupCanvas",typeof(RectTransform),typeof(Canvas));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.pixelPerfect=true;
   var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");Assert.That(font,Is.Not.Null);
   var pages=LobbyDestinationPages.Create(canvasObject.transform,font,()=>{});using var controller=new EquipmentMultishotController("a",(string)current["runId"]);
   void Draw(string name){Canvas.ForceUpdateCanvases();typeof(LobbyDestinationPages).GetMethod("Fit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(pages,null);foreach(var text in pages.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate();Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=render;var texture=new Texture2D(1600,900,TextureFormat.RGBA32,false);try{texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());var pixels=texture.GetPixels32();int Bright(string name){var label=pages.GetComponentsInChildren<TMP_Text>().Single(t=>t.name==name);var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);var low=camera.WorldToScreenPoint(corners[0]);var high=camera.WorldToScreenPoint(corners[2]);Assert.That(low.x,Is.GreaterThanOrEqualTo(0),name);Assert.That(high.x,Is.LessThanOrEqualTo(1600),name);Assert.That(low.y,Is.GreaterThanOrEqualTo(0),name);Assert.That(high.y,Is.LessThanOrEqualTo(900),name);int count=0;for(int y=Mathf.CeilToInt(low.y);y<Mathf.FloorToInt(high.y);y++)for(int x=Mathf.CeilToInt(low.x);x<Mathf.FloorToInt(high.x);x++){var color=pixels[y*1600+x];if(color.r>170&&color.g>170&&color.b>150)count++;}return count;}foreach(string label in new[]{"currentBasis","departureBasis","currentSummary","departureSummary"})Assert.That(Bright(label),Is.GreaterThan(50),label+" must reach rendered landscape pixels");}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}}
   try{pages.Open(LobbyPage.Operators,true);controller.Bind(pages);controller.RefreshAsync("CURRENT_VERIFIED_EQUIPMENT",_=>System.Threading.Tasks.Task.FromResult(Model(current))).GetAwaiter().GetResult();controller.RefreshAsync("ORIGINAL_LOCAL_TRIAL_START",_=>System.Threading.Tasks.Task.FromResult(Model(departure))).GetAwaiter().GetResult();yield return null;yield return null;Draw("current-and-departure.png");var texts=pages.GetComponentsInChildren<TMP_Text>();Assert.That(texts.Single(t=>t.name=="currentStatus2").text,Is.EqualTo("동일 효과 중복 · 1개 적용"));Assert.That(texts.Single(t=>t.name=="departureCoefficient0").text,Is.EqualTo("추가 피해15%"));controller.RefreshAsync("CURRENT_VERIFIED_EQUIPMENT",_=>System.Threading.Tasks.Task.FromResult(Model((JObject)entry["afterUnequip"]))).GetAwaiter().GetResult();controller.RefreshAsync("CURRENT_VERIFIED_STORED_ITEM",_=>System.Threading.Tasks.Task.FromResult(Model((JObject)entry["stored"]))).GetAwaiter().GetResult();yield return null;yield return null;Draw("stored-and-current.png");
    File.WriteAllText(Path.Combine(output,"source-projections.json"),new JObject{["current"]=current.DeepClone(),["departure"]=departure.DeepClone(),["afterUnequip"]=entry["afterUnequip"].DeepClone(),["stored"]=entry["stored"].DeepClone()}.ToString());
    controller.Unbind();var upgraded=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/en17-presentation-example-20261016.json"));using(var other=new EquipmentMultishotController("a",(string)upgraded["departure"]["runId"])){other.Bind(pages);other.RefreshAsync("CURRENT_VERIFIED_EQUIPMENT",_=>System.Threading.Tasks.Task.FromResult(Model((JObject)upgraded["current"]))).GetAwaiter().GetResult();other.RefreshAsync("ORIGINAL_LOCAL_TRIAL_START",_=>System.Threading.Tasks.Task.FromResult(Model((JObject)upgraded["departure"]))).GetAwaiter().GetResult();yield return null;yield return null;Draw("upgraded-item-procedure-star.png");Assert.That(pages.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="departureCurrentStar0").text,Is.EqualTo("장비 ★2"));Assert.That(pages.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="departureAppliedStar0").text,Is.EqualTo("시술 당시 ★1"));}
    File.WriteAllText(Path.Combine(output,"acceptance.json"),new JObject{["classification"]="ACTUAL_ISOLATED_SERVER_PRESENTATION_TO_EXISTING_POPUP_NOT_OPERATING_HTTP_OR_FINAL_MOBILE_ACCEPTANCE",["graphics"]=SystemInfo.graphicsDeviceName,["projectionSourceHash"]=current["sourceHash"].DeepClone(),["independentTextCount"]=texts.Length,["width"]=1600,["height"]=900}.ToString());}
   finally{controller.Unbind();UnityEngine.Object.DestroyImmediate(canvasObject);camera.targetTexture=null;render.Release();UnityEngine.Object.DestroyImmediate(render);UnityEngine.Object.DestroyImmediate(cameraObject);}
  }
 }
}
#endif
