using UnityEngine;
namespace LastRail.Presentation.Battle {
 // Only coordinate conversion on borrowed actual scene objects. The caller owns their lifetime,
 // reference-surface layout, camera policy and the meaning of the supplied anchor (roof/hit/feet).
 public static class FiveAgentVisualAnchorProjection {
  // Returns surface-local UI units from its top-left. This is NOT a world PPU or a roof-anchor definition.
  public static bool TryProject(RectTransform surface,Transform originalAnchor,out Vector2 point){
   point=default;
   if(surface==null||originalAnchor==null||!surface.gameObject.activeInHierarchy||!originalAnchor.gameObject.activeInHierarchy||surface.rect.width<=0||surface.rect.height<=0)return false;
   var canvas=surface.GetComponentInParent<Canvas>();if(canvas==null)return false;canvas=canvas.rootCanvas;
   if(!canvas.isActiveAndEnabled)return false;
   Camera camera=null;
   if(canvas.renderMode!=RenderMode.ScreenSpaceOverlay){camera=canvas.worldCamera;if(camera==null||!camera.isActiveAndEnabled)return false;var projected=camera.WorldToScreenPoint(originalAnchor.position);if(projected.z<=0||!float.IsFinite(projected.x)||!float.IsFinite(projected.y))return false;}
   var screen=RectTransformUtility.WorldToScreenPoint(camera,originalAnchor.position);
   if(!float.IsFinite(screen.x)||!float.IsFinite(screen.y)||!RectTransformUtility.ScreenPointToLocalPointInRectangle(surface,screen,camera,out var local)||!float.IsFinite(local.x)||!float.IsFinite(local.y))return false;
   point=new Vector2(local.x-surface.rect.xMin,surface.rect.yMax-local.y);return true;
  }
 }
}
