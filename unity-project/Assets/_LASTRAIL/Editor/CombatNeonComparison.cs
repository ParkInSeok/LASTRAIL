#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LastRail.Battle;
// Exact source crops shown by a Unity Canvas; no generative redraw of either image.
public static class CombatNeonComparison {
 public static void SaveValues(Camera camera,string folder){
  var meters=UnityEngine.Object.FindObjectsByType<CombatMeter>(FindObjectsSortMode.None);
  var values=Array.ConvertAll(meters,m=>m.Value);var prior=RenderTexture.active;
  float[] samples={0,.25f,.99f,1,0};string[] names={"zero","quarter","almost-full","full","consumed-zero"};
  for(int j=0;j<samples.Length;j++){
   foreach(var meter in meters)meter.SetValue(samples[j]);Canvas.ForceUpdateCanvases();camera.Render();
   RenderTexture.active=camera.targetTexture;var pixels=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGB24,false);
   pixels.ReadPixels(new UnityEngine.Rect(0,0,pixels.width,pixels.height),0,0);pixels.Apply();File.WriteAllBytes(folder+"/meter-fixture-"+names[j]+".png",pixels.EncodeToPNG());UnityEngine.Object.Destroy(pixels);
  }
  for(int i=0;i<meters.Length;i++)meters[i].SetValue(values[i]);Canvas.ForceUpdateCanvases();RenderTexture.active=prior;
  File.WriteAllText(folder+"/meter-fixture-NOTE.txt","Renderer-only ratio samples 0/25/99/100/0; authoritative gameplay and text unchanged. consumed-zero is a UI reset fixture, not a skill-use replay.");
 }
 public static void Save(Camera camera,GameObject oldHud,string actualPath,string output){
  var reference=new Texture2D(2,2);reference.LoadImage(File.ReadAllBytes("D:/Project/LASTRAIL/Worktrees/5f93/LASTRAIL/QA/Design/CombatCandidates-20260911/A-final-charge-gauge-v02.png"));
  var actual=new Texture2D(2,2);actual.LoadImage(File.ReadAllBytes(actualPath));
  var oldTarget=camera.targetTexture;var target=new RenderTexture(1200,720,24);camera.targetTexture=target;oldHud.SetActive(false);
  var go=new GameObject("ExactNeonComparison",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
  var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
  var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1200,720);scaler.matchWidthOrHeight=1;
  var font=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved").Font;
  RectTransform Rect(string name,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(go.transform,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  void Label(string value,float x,float y,float w,int size){var t=Rect(value,x,y,w,35).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.color=Color.white;t.text=value;}
  void Crop(Texture2D texture,UnityEngine.Rect crop,float x,float y){float w=480,h=480*crop.height/crop.width;var image=Rect("ExactCrop",x,y,480,h).gameObject.AddComponent<RawImage>();image.texture=texture;image.uvRect=new UnityEngine.Rect(crop.x/texture.width,1-(crop.y+crop.height)/texture.height,crop.width/texture.width,crop.height/texture.height);}
  Label("승인 최종 시안",45,22,500,25);Label("Unity 실제 화면",645,22,500,25);Label("동일 폭으로 확대 · 수치는 각 화면의 실제 상태",45,65,1100,17);
  string[] labels={"열차 HP","대원 머리 위 HP","카드 HP","분대장 게이지"};
  UnityEngine.Rect[] refs={new UnityEngine.Rect(190,460,356,27),new UnityEngine.Rect(54,562,96,18),new UnityEngine.Rect(32,1248,112,17),new UnityEngine.Rect(722,1255,191,40)};
  UnityEngine.Rect[] lives={new UnityEngine.Rect(171,352,344,31),new UnityEngine.Rect(26,513,113,17),new UnityEngine.Rect(29,970,87,18),new UnityEngine.Rect(540,992,153,25)};
  for(int i=0;i<4;i++){float y=112+i*145;Label(labels[i],45,y,500,19);Label(labels[i],645,y,500,19);Crop(reference,refs[i],45,y+40);Crop(actual,lives[i],645,y+40);}
  Canvas.ForceUpdateCanvases();camera.Render();var prior=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(1200,720,TextureFormat.RGB24,false);pixels.ReadPixels(new UnityEngine.Rect(0,0,1200,720),0,0);pixels.Apply();RenderTexture.active=prior;File.WriteAllBytes(output,pixels.EncodeToPNG());
  camera.targetTexture=oldTarget;UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(reference);UnityEngine.Object.Destroy(actual);UnityEngine.Object.Destroy(pixels);target.Release();oldHud.SetActive(true);
 }
}
#endif
