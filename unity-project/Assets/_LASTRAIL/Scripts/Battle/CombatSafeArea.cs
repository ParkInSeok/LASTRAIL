using UnityEngine;
namespace LastRail.Battle {
// Fit the whole fixed-design console inside safe pixels; no cropping of controls.
[ExecuteAlways]
public sealed class CombatSafeArea:MonoBehaviour {
 RectTransform content; Canvas canvas;
 Rect previousSafe;Vector2 previousSize;bool measured;
 public void Bind(RectTransform value,Canvas owner){content=value;canvas=owner;Apply();}
 public static Rect Fit(Rect safe)=>FitDesign(safe,new Vector2(720,1280));
 public static Rect FitDesign(Rect safe,Vector2 design){if(design.x<=0||design.y<=0)throw new System.ArgumentOutOfRangeException(nameof(design));float scale=Mathf.Min(safe.width/design.x,safe.height/design.y);var size=design*scale;return new Rect(safe.center-size*.5f,size);}
 void OnEnable()=>Canvas.willRenderCanvases+=Apply;
 void OnDisable()=>Canvas.willRenderCanvases-=Apply;
 bool FitRenderTarget(){
  if(canvas.renderMode!=RenderMode.ScreenSpaceCamera||canvas.worldCamera==null||canvas.worldCamera.targetTexture==null)return false;
  var camera=canvas.worldCamera;var corners=new Vector3[4];content.GetWorldCorners(corners);var a=camera.WorldToScreenPoint(corners[0]);var b=camera.WorldToScreenPoint(corners[2]);var viewport=camera.pixelRect;
  if(b.x-a.x<=0||b.y-a.y<=0)return true;
  float factor=Mathf.Min(viewport.width/(b.x-a.x),viewport.height/(b.y-a.y));
  if(Mathf.Abs(factor-1)>.00001f)content.localScale*=factor;
  var parent=(RectTransform)canvas.transform;if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,viewport.center,camera,out var center))content.anchoredPosition=center-parent.rect.center;
  return true;
 }
 public void Apply(){if(content==null||canvas==null)return;var currentSize=((RectTransform)canvas.transform).rect.size;var currentSafe=Screen.safeArea;if(measured&&(previousSafe!=currentSafe||previousSize!=currentSize))GetComponent<ApprovedCombatView>()?.CancelPendingInput();previousSafe=currentSafe;previousSize=currentSize;measured=true;if(FitRenderTarget())return;var size=((RectTransform)canvas.transform).rect.size;if(size.x<=0||size.y<=0)return;var local=new Rect(0,0,size.x,size.y);bool capture=canvas.renderMode==RenderMode.ScreenSpaceCamera&&canvas.worldCamera!=null&&canvas.worldCamera.targetTexture!=null;if(capture){size=new Vector2(canvas.worldCamera.targetTexture.width,canvas.worldCamera.targetTexture.height)/canvas.scaleFactor;local=new Rect(0,0,size.x,size.y);}if(!capture&&Screen.width>0&&Screen.height>0){var safe=Screen.safeArea;if(safe.width>0&&safe.height>0)local=new Rect(safe.x/Screen.width*size.x,safe.y/Screen.height*size.y,safe.width/Screen.width*size.x,safe.height/Screen.height*size.y);}var fit=FitDesign(local,content.sizeDelta);content.localScale=Vector3.one*(fit.width/content.sizeDelta.x);content.anchoredPosition=fit.center-size*.5f;}
}
}
