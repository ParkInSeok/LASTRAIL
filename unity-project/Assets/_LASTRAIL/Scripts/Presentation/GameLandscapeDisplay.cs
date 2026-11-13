using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation {
 // Game-owned display policy. External OS/payment windows keep their own orientation.
 public static class GameLandscapeDisplay {
  public static readonly Vector2 DesignSize=new Vector2(1600,900);
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Initialize(){ApplyOrientation();}
  public static void ApplyOrientation(){
   if(!Application.isMobilePlatform)return;
   Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
   Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;
   Screen.orientation=ScreenOrientation.AutoRotation;
  }
  public static void Configure(CanvasScaler scaler){if(scaler==null)return;scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=DesignSize;scaler.matchWidthOrHeight=.5f;}
 }
}
