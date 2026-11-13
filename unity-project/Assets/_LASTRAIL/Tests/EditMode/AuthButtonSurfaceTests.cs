using System.Linq;
using LastRail.Auth;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public sealed class AuthButtonSurfaceTests {
 [TestCase(true)][TestCase(false)]public void IndependentLayersPreserveAtlasAndLeavePlateHoleTransparent(bool primary){
  var go=new GameObject("button-surface-test",typeof(RectTransform),typeof(Canvas));var cameraObject=new GameObject("button-camera",typeof(Camera));var target=new RenderTexture(620,156,24);var previous=RenderTexture.active;
  try{
   var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=78;camera.aspect=620f/156;camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=Color.clear;camera.clearFlags=CameraClearFlags.SolidColor;camera.targetTexture=target;
   ((RectTransform)go.transform).sizeDelta=new Vector2(620,156);var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
   var texture=Resources.Load<Texture2D>("EntryUi/"+(primary?"primary":"secondary"));
   var old=AuthFormVisuals.Rect("original",go.transform).gameObject.AddComponent<RawImage>();old.texture=texture;
   Color32[] Read(){Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(620,156,TextureFormat.RGBA32,false);try{pixels.ReadPixels(new Rect(0,0,620,156),0,0);pixels.Apply();return pixels.GetPixels32();}finally{Object.DestroyImmediate(pixels);}}
   var before=Read();old.enabled=false;var surface=AuthFormVisuals.Rect("composed",go.transform).gameObject.AddComponent<AuthButtonSurface>();surface.raycastTarget=false;surface.Configure(texture,primary);var after=Read();
   int different=before.Zip(after,(a,b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b))>3?1:0).Sum();Assert.Less(different,620*156/100,"Composed atlas should retain original appearance, allowing subpixel edges.");
   surface.SetLayerVisible("Backplate",false);var frame=Read();Assert.Less(frame[78*620+310].a,3,"Frame and lamps must leave the central plate hole transparent");
   surface.SetLayerVisible("Backplate",true);var restored=Read();Assert.AreEqual(after,restored);foreach(var layer in surface.GetComponentsInChildren<Graphic>())Assert.IsFalse(layer.raycastTarget);
   var button=surface.gameObject.AddComponent<Button>();button.targetGraphic=surface;var states=button.colors;states.fadeDuration=0;states.disabledColor=new Color(.3f,.3f,.3f,1);button.colors=states;
   button.interactable=false;var disabled=Read();Assert.Less(disabled[78*620+310].r,restored[78*620+310].r,"Disabled button must visibly tint its plate");
   Assert.That(surface.GetLayer("Frame").canvasRenderer.GetColor(),Is.EqualTo(Color.white),"Button states preserve frame artwork");
   button.interactable=true;Assert.AreEqual(restored,Read(),"Normal state restores visible layers");
   states.pressedColor=new Color(.5f,.5f,.5f,1);button.colors=states;button.navigation=new Navigation{mode=Navigation.Mode.None};
   var pointer=new UnityEngine.EventSystems.PointerEventData(null){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
   button.OnPointerEnter(pointer);button.OnPointerDown(pointer);var pressed=Read();Assert.Less(pressed[78*620+310].r,restored[78*620+310].r,"Pointer press must visibly tint the plate");
   Assert.That(surface.GetLayer("Frame").canvasRenderer.GetColor(),Is.EqualTo(Color.white));
   button.OnPointerUp(pointer);button.OnPointerExit(pointer);Assert.AreEqual(restored,Read(),"Pointer release restores normal appearance");
  }finally{RenderTexture.active=previous;Object.DestroyImmediate(go);cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(cameraObject);}
 }
}
