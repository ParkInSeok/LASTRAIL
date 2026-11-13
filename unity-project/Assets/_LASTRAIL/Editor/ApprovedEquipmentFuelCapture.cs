#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ApprovedEquipmentFuelCapture {
 public static void Run(){
  const string output="D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/departure/";
  var font=CombatEditorFonts.Resolve(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset"));
  var cameraNode=new GameObject("Equipment fuel capture camera",typeof(Camera));var camera=cameraNode.GetComponent<Camera>();camera.backgroundColor=Color.black;camera.clearFlags=CameraClearFlags.SolidColor;camera.cullingMask=1<<31;
  var target=new RenderTexture(1280,720,24);camera.targetTexture=target;var old=RenderTexture.active;GameObject root=null;
  try{
   Canvas Host(){root=new GameObject("Fixture display host",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();return canvas;}
   void Save(string name){foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<TMP_Text>())t.ForceMeshUpdate();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);try{pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(output+name+".png",pixels.EncodeToPNG());}finally{Object.DestroyImmediate(pixels);}}
   Host();var page=LobbyDestinationPages.Create(root.transform,font,()=>{});page.Open(LobbyPage.Operators,true);
   var entry=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/MiraEquipped-20261016/PresentationSource/server-packets.json"))["packets"]["grown"]["presentations"];
   JObject Display(JObject p)=>EquipmentMultishotModel.Read(p,p,BDirectAttackInput.Digest(p),(string)p["accountId"],(string)p["runId"]).Render();
   page.PresentEquipmentMultishot(new JObject{["version"]=EquipmentMultishotController.FrameVersion,["current"]=Display((JObject)entry["highest"]["current"]),["departure"]=Display((JObject)entry["one"]["departure"]),["stored"]=Display((JObject)entry["stored"])});
   typeof(LobbyDestinationPages).GetMethod("Fit",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(page,null);Save("equipment-frames-existing-projection-fixture");Object.DestroyImmediate(root);
   Host();var departure=DeparturePreparationView.Create(root.transform,font,null,null,null,null,null,null,null,null,null,null,null);departure.Render("Display fixture","I","",new[]{"80","2400","150"},"10",true,true,false,true,true,"Start");departure.SetViewportHeight(900);Save("departure-fuel-display-fixture");
   var formation=departure.transform.Find("NumberedFormation");formation.gameObject.SetActive(false);Save("departure-scene-layers");
   var driver=departure.transform.Find("PreparationScene/PreparationDriver");driver.gameObject.SetActive(false);Save("departure-background-only");driver.gameObject.SetActive(true);formation.gameObject.SetActive(true);
   File.WriteAllText(output+"equipment-fuel-capture-scope.json",new JObject{["classification"]="EXISTING_VIEWS_WITH_RETAINED_EQUIPMENT_PROJECTION_AND_EXAMPLE_DEPARTURE_VALUES_NOT_LIVE_RUNTIME",["width"]=1280,["height"]=720}.ToString());
  }finally{RenderTexture.active=old;if(root!=null)Object.DestroyImmediate(root);Object.DestroyImmediate(cameraNode);target.Release();Object.DestroyImmediate(target);}
 }
}
#endif
