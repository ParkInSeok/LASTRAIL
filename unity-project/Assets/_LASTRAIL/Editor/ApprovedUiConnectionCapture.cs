#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using LastRail.Presentation.MissionShell;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
public static class ApprovedUiConnectionCapture {
 public static void Run(){
  const string output="D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/continuation/";
  var font=LastRail.Battle.CombatEditorFonts.Resolve(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset"));
  var cameraNode=new GameObject("UI capture camera",typeof(Camera));var camera=cameraNode.GetComponent<Camera>();camera.backgroundColor=Color.black;camera.clearFlags=CameraClearFlags.SolidColor;camera.cullingMask=1<<31;
  var target=new RenderTexture(1280,720,24);camera.targetTexture=target;var old=RenderTexture.active;GameObject root=null;
  try{
   void Save(Canvas canvas,string name){foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);try{pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(output+name+".png",pixels.EncodeToPNG());}finally{Object.DestroyImmediate(pixels);}}
   root=new GameObject("Travel host");var panel=OrdinaryTravelPanel.Create(root.transform,font);panel.ShowChoices(new[]{("a","Combat","COMBAT"),("b","Elite","ELITE"),("c","Boss","BOSS")},true,"Choose the next destination","Confirmed station");Save(panel.GetComponent<Canvas>(),"map-combat-elite-boss");
   panel.ShowChoices(new[]{("d","Equipment","EQUIPMENT"),("e","Enchant","ENCHANT"),("f","Merchant","MERCHANT")},false,"Pending confirmation");Save(panel.GetComponent<Canvas>(),"map-equipment-enchant-unknown");Object.DestroyImmediate(root);
   root=new GameObject("Lobby host",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
   var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
   var page=LobbyDestinationPages.Create(root.transform,font,()=>{});page.Open(LobbyPage.Formation,true);
   typeof(LobbyDestinationPages).GetMethod("Notice",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(page,new object[]{"Notice","Approved independent frame and existing close action."});typeof(LobbyDestinationPages).GetMethod("Fit",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(page,null);Save(canvas,"common-notice");
  }finally{RenderTexture.active=old;if(root!=null)Object.DestroyImmediate(root);Object.DestroyImmediate(cameraNode);target.Release();Object.DestroyImmediate(target);}
 }
}
#endif
