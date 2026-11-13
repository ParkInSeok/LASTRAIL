using System.Linq;
using LastRail.Presentation.Startup;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode {
 public class AllScreensLandscapeTests {
  [Test] public void PlatformAllowsOnlyLandscape(){Assert.That(UnityEditor.PlayerSettings.allowedAutorotateToPortrait,Is.False);Assert.That(UnityEditor.PlayerSettings.allowedAutorotateToPortraitUpsideDown,Is.False);Assert.That(UnityEditor.PlayerSettings.allowedAutorotateToLandscapeLeft&&UnityEditor.PlayerSettings.allowedAutorotateToLandscapeRight,Is.True);}
  [TestCase(1600,900)][TestCase(2100,900)][TestCase(1200,900)]
  public void StartupErrorActionsFitSafeViewport(float width,float height){
   var root=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
   try{root.GetComponent<CanvasScaler>().enabled=false;root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)root.transform).sizeDelta=new Vector2(width,height);
    var view=root.GetComponent<StartupScreenRootBehaviour>();view.Render(StartupScreenViewState.DownloadError("연결을 확인한 뒤 다시 시도해 주세요."));Canvas.ForceUpdateCanvases();
    var safe=(RectTransform)root.transform.Find("SafeArea");var corners=new Vector3[4];
    foreach(var button in root.GetComponentsInChildren<Button>().Where(b=>b.gameObject.activeInHierarchy)){((RectTransform)button.transform).GetWorldCorners(corners);foreach(var c in corners){var local=safe.InverseTransformPoint(c);Assert.That(local.x,Is.InRange(safe.rect.xMin-1,safe.rect.xMax+1),button.name);Assert.That(local.y,Is.InRange(safe.rect.yMin-1,safe.rect.yMax+1),button.name);}}
   }finally{Object.DestroyImmediate(root);}
  }
 }
}
